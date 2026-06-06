#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for inspect-build42-loth-structure.ps1 (MAP-6R).

    Creates synthetic LOTH fixtures under temp .local dirs and validates
    the structure report. Does not read PZ folders.
    Expected assertion count: 14
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$inspScript   = Join-Path $repoRoot 'scripts\inspect-build42-loth-structure.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

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

Write-Output 'test-build42-loth-structure.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic LOTH fixtures under temp .local dirs
# ---------------------------------------------------------------------------

$testBase  = Join-Path $tempRoot ('pzmf-t6r-' + [System.IO.Path]::GetRandomFileName())
$refDir    = Join-Path $testBase '.local\reference'
$outDir    = Join-Path $testBase '.local\output'
$badPath   = Join-Path $tempRoot 'pzmf-t6r-bad-no-local'

New-Item -ItemType Directory -Force -Path $refDir  | Out-Null
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# Helper: build a synthetic LOTH file
#   magic(4) + version(4) + entry_count(4) + null_padding(N) + entries
function Make-Loth {
    param([string]$Path, [int]$EntryCount, [int]$ExtraPaddingBytes, [string[]]$Entries)
    $magic   = [byte[]]@(0x4C, 0x4F, 0x54, 0x48)       # LOTH
    $ver     = [byte[]]@(0x01, 0x00, 0x00, 0x00)        # version=1
    $cnt     = [BitConverter]::GetBytes([uint32]$EntryCount)
    $padding = [byte[]]::new($ExtraPaddingBytes)
    $body    = [byte[]]::new(0)
    foreach ($e in $Entries) {
        $eb = [System.Text.Encoding]::ASCII.GetBytes($e + "`n")
        $body = $body + $eb
    }
    $all = $magic + $ver + $cnt + $padding + $body
    [System.IO.File]::WriteAllBytes($Path, [byte[]]$all)
}

# Reference 1: LOTH, version=1, field8=3, 4 bytes padding, 3 entries (total > 38 bytes)
Make-Loth -Path (Join-Path $refDir 'ref1.lotheader') `
          -EntryCount 3 -ExtraPaddingBytes 4 `
          -Entries @('blends_grassoverlays_01_0', 'blends_grassoverlays_01_1', 'blends_grassoverlays_01_2')

# Reference 2: LOTH, version=1, field8=3, 8 bytes padding, 3 entries (different padding)
Make-Loth -Path (Join-Path $refDir 'ref2.lotheader') `
          -EntryCount 3 -ExtraPaddingBytes 8 `
          -Entries @('tileset_pack_A_0', 'tileset_pack_A_1', 'tileset_pack_A_2')

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
# Tests 3-14: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 3-14: valid run ---'
$t3Exit = Invoke-Insp -ReferenceRoot $refDir -Output $outDir

$jsonFile = Join-Path $outDir 'build42-loth-structure-report.json'
$mdFile   = Join-Path $outDir 'build42-loth-structure-report.md'

Assert-True ($t3Exit -eq 0)       'Test3: script exits 0'
Assert-True (Test-Path $jsonFile) 'Test4: JSON report exists'
Assert-True (Test-Path $mdFile)   'Test5: MD report exists'

$data      = if (Test-Path $jsonFile) { Get-Content $jsonFile -Raw | ConvertFrom-Json } else { $null }
$mdContent = if (Test-Path $mdFile)   { Get-Content $mdFile   -Raw } else { '' }

# Test 6: magic LOTH detected
$magicMatches = @(@($data.magic_counts) | Where-Object { $_.magic -eq 'LOTH' })
Assert-True ($magicMatches.Count -gt 0) 'Test6: magic LOTH detected in magic_counts'

# Test 7: version 1 detected
$ver1Matches = @(@($data.version_counts) | Where-Object { [string]$_.version -eq '1' })
Assert-True ($ver1Matches.Count -gt 0) 'Test7: version 1 detected in version_counts'

# Test 8: first printable offset recorded in at least one record
$hasFpOff = @(@($data.records) | Where-Object { $null -ne $_.first_printable_offset }).Count -gt 0
Assert-True $hasFpOff 'Test8: first_printable_offset recorded in records'

# Test 9: newline count recorded in at least one record
$hasNlCount = @(@($data.records) | Where-Object { $null -ne $_.newline_count_in_prefix }).Count -gt 0
Assert-True $hasNlCount 'Test9: newline_count_in_prefix recorded in records'

# Test 10: u32le_words_first_128 present in smallest_reference_record
$hasWords = $null -ne $data.smallest_reference_record -and
            $null -ne $data.smallest_reference_record.PSObject.Properties['u32le_words_first_128']
Assert-True $hasWords 'Test10: u32le_words_first_128 in smallest_reference_record'

# Tests 11-14: status labels in MD
Assert-True ($mdContent -match 'BUILD42_LOTH_STRUCTURE_INSPECTED')       'Test11: BUILD42_LOTH_STRUCTURE_INSPECTED in MD'
Assert-True ($mdContent -match 'CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED') 'Test12: CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED in MD'
Assert-True ($mdContent -match 'WRITER_NOT_CHANGED')                      'Test13: WRITER_NOT_CHANGED in MD'
Assert-True ($mdContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false')     'Test14: PLAYABLE_EXPORT_CLAIM_ALLOWED=false in MD'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
