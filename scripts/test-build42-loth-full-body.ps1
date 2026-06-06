#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for inspect-build42-loth-full-body.ps1 (MAP-6U).

    Creates synthetic LOTH fixtures with an ASCII string table followed by
    binary trailing bytes. Validates that the script detects the trailing body
    and emits LOTH_REQUIRES_TRAILING_BINARY_BODY.
    Expected assertion count: 14
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$inspScript = Join-Path $repoRoot 'scripts\inspect-build42-loth-full-body.ps1'
$tempRoot   = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Insp {
    param([string]$ReferenceRoot, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $inspScript `
        -ReferenceRoot $ReferenceRoot `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-loth-full-body.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic LOTH fixtures with trailing binary bytes
# ---------------------------------------------------------------------------

$testBase  = Join-Path $tempRoot ('pzmf-t6u-' + [System.IO.Path]::GetRandomFileName())
$refDir    = Join-Path $testBase '.local\reference'
$outDir    = Join-Path $testBase '.local\output'
$badPath   = Join-Path $tempRoot 'pzmf-t6u-bad-no-local'

New-Item -ItemType Directory -Force -Path $refDir  | Out-Null
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Make-LothWithTrailing {
    param([string]$Path, [int]$EntryCount, [string[]]$Entries, [byte[]]$Trailing)
    $magic   = [byte[]]@(0x4C, 0x4F, 0x54, 0x48)       # LOTH
    $ver     = [byte[]]@(0x01, 0x00, 0x00, 0x00)
    $cnt     = [BitConverter]::GetBytes([uint32]$EntryCount)
    $body    = [byte[]]::new(0)
    foreach ($e in $Entries) {
        $body = $body + [System.Text.Encoding]::ASCII.GetBytes($e + "`n")
    }
    $all = $magic + $ver + $cnt + $body + $Trailing
    [System.IO.File]::WriteAllBytes($Path, [byte[]]$all)
}

# Trailing binary: 16 bytes (4 x U32 = 4, 0, 0, 1)
$trailingBytes = [byte[]]@(
    0x04, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00,
    0x01, 0x00, 0x00, 0x00
)

# Reference 1: 3 entries + 16 trailing bytes
Make-LothWithTrailing `
    -Path (Join-Path $refDir 'ref1.lotheader') `
    -EntryCount 3 `
    -Entries @('blends_grassoverlays_01_0','blends_grassoverlays_01_1','blends_grassoverlays_01_2') `
    -Trailing $trailingBytes

# Reference 2: 4 entries + 16 trailing bytes
Make-LothWithTrailing `
    -Path (Join-Path $refDir 'ref2.lotheader') `
    -EntryCount 4 `
    -Entries @('tileset_A_0','tileset_A_1','tileset_A_2','tileset_A_3') `
    -Trailing $trailingBytes

# ---------------------------------------------------------------------------
# Test 1: ReferenceRoot outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: ReferenceRoot outside .local refused ---'
$t1Exit = Invoke-Insp -ReferenceRoot $badPath -Output $outDir
Assert-True ($t1Exit -ne 0) 'Test1: ReferenceRoot outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Output outside .local refused ---'
$t2Exit = Invoke-Insp -ReferenceRoot $refDir -Output $badPath
Assert-True ($t2Exit -ne 0) 'Test2: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 3-14: Valid run with trailing-body fixtures
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 3-14: valid run with trailing fixtures ---'
$t3Exit = Invoke-Insp -ReferenceRoot $refDir -Output $outDir

$jsonFile = Join-Path $outDir 'build42-loth-full-body-report.json'
$mdFile   = Join-Path $outDir 'build42-loth-full-body-report.md'

Assert-True ($t3Exit -eq 0)       'Test3: script exits 0'
Assert-True (Test-Path $jsonFile) 'Test4: JSON report exists'
Assert-True (Test-Path $mdFile)   'Test5: MD report exists'

$data      = if (Test-Path $jsonFile) { Get-Content $jsonFile -Raw | ConvertFrom-Json } else { $null }
$mdContent = if (Test-Path $mdFile)   { Get-Content $mdFile   -Raw } else { '' }

# Test 6: LOTH magic detected in records
$magicRecs = @(@($data.records) | Where-Object { $_.magic_ascii -eq 'LOTH' })
Assert-True ($magicRecs.Count -gt 0) 'Test6: LOTH magic detected in records'

# Test 7: field8 detected
$f8Recs = @(@($data.records) | Where-Object { $null -ne $_.field8_u32le -and [int]$_.field8_u32le -gt 0 })
Assert-True ($f8Recs.Count -gt 0) 'Test7: field8 > 0 detected in records'

# Test 8: ASCII entry region detected
$asciiRecs = @(@($data.records) | Where-Object { [int]$_.ascii_entry_count -gt 0 })
Assert-True ($asciiRecs.Count -gt 0) 'Test8: ASCII entry region detected'

# Test 9: trailing_bytes_count recorded (should be 16 for our fixtures)
$trailingRecs = @(@($data.records) | Where-Object { [int]$_.trailing_bytes_count -gt 0 })
Assert-True ($trailingRecs.Count -gt 0) 'Test9: trailing_bytes_count > 0 in records'

# Test 10: report detects trailing body exists
Assert-True ([int]$data.count_with_trailing_body -gt 0) 'Test10: count_with_trailing_body > 0'

# Test 11: hypothesis is LOTH_REQUIRES_TRAILING_BINARY_BODY (all fixtures have trailing)
Assert-True ([string]$data.hypothesis -eq 'LOTH_REQUIRES_TRAILING_BINARY_BODY') `
    'Test11: hypothesis == LOTH_REQUIRES_TRAILING_BINARY_BODY'

# Tests 12-14: status labels in MD
Assert-True ($mdContent -match 'WRITER_NOT_CHANGED')              'Test12: WRITER_NOT_CHANGED in MD'
Assert-True ($mdContent -match 'LOAD_TEST_NOT_PERFORMED')         'Test13: LOAD_TEST_NOT_PERFORMED in MD'
Assert-True ($mdContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'Test14: PLAYABLE_EXPORT_CLAIM_ALLOWED=false in MD'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
