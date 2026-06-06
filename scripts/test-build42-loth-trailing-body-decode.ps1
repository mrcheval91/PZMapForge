#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for decode-build42-loth-trailing-body.ps1 (MAP-6V).

    Creates synthetic LOTH fixtures with u32-aligned trailing bodies and
    string-table reference values. Validates decode report hypotheses.
    Expected assertion count: 17
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$decScript   = Join-Path $repoRoot 'scripts\decode-build42-loth-trailing-body.ps1'
$tempRoot    = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Dec {
    param([string]$ReferenceRoot, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $decScript `
        -ReferenceRoot $ReferenceRoot `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-loth-trailing-body-decode.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic LOTH fixtures
# ---------------------------------------------------------------------------

$testBase  = Join-Path $tempRoot ('pzmf-t6v-' + [System.IO.Path]::GetRandomFileName())
$refDir    = Join-Path $testBase '.local\reference'
$outDir    = Join-Path $testBase '.local\output'
$badPath   = Join-Path $tempRoot 'pzmf-t6v-bad-no-local'

New-Item -ItemType Directory -Force -Path $refDir  | Out-Null
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Make-LothDecFixture {
    param([string]$Path, [int]$EntryCount, [string[]]$Entries, [uint32[]]$TrailingU32s)
    $magic  = [byte[]]@(0x4C, 0x4F, 0x54, 0x48)
    $ver    = [byte[]]@(0x01, 0x00, 0x00, 0x00)
    $cnt    = [BitConverter]::GetBytes([uint32]$EntryCount)
    $body   = [byte[]]::new(0)
    foreach ($e in $Entries) { $body = $body + [System.Text.Encoding]::ASCII.GetBytes($e + "`n") }
    $trail  = [byte[]]::new($TrailingU32s.Count * 4)
    for ($i = 0; $i -lt $TrailingU32s.Count; $i++) {
        [Array]::Copy([BitConverter]::GetBytes($TrailingU32s[$i]), 0, $trail, $i*4, 4)
    }
    $all = $magic + $ver + $cnt + $body + $trail
    [System.IO.File]::WriteAllBytes($Path, [byte[]]$all)
}

# ref1: field8=3, 3 entries, trailing u32s = 0,1,2,3,30,0,1
# Words 1,2,3 are < field8=3 -> references string table
Make-LothDecFixture `
    -Path (Join-Path $refDir '10_5.lotheader') `
    -EntryCount 3 `
    -Entries @('entry_0','entry_1','entry_2') `
    -TrailingU32s @([uint32]0,[uint32]1,[uint32]2,[uint32]3,[uint32]30,[uint32]0,[uint32]1)

# ref2: field8=4, 4 entries, trailing u32s = 0,1,2,3,30,1,0
Make-LothDecFixture `
    -Path (Join-Path $refDir '11_5.lotheader') `
    -EntryCount 4 `
    -Entries @('tileset_0','tileset_1','tileset_2','tileset_3') `
    -TrailingU32s @([uint32]0,[uint32]1,[uint32]2,[uint32]3,[uint32]30,[uint32]1,[uint32]0)

# ---------------------------------------------------------------------------
# Test 1: ReferenceRoot outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: ReferenceRoot outside .local refused ---'
$t1Exit = Invoke-Dec -ReferenceRoot $badPath -Output $outDir
Assert-True ($t1Exit -ne 0) 'Test1: ReferenceRoot outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Output outside .local refused ---'
$t2Exit = Invoke-Dec -ReferenceRoot $refDir -Output $badPath
Assert-True ($t2Exit -ne 0) 'Test2: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 3-16: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 3-16: valid run ---'
$t3Exit = Invoke-Dec -ReferenceRoot $refDir -Output $outDir

$jsonFile = Join-Path $outDir 'build42-loth-trailing-body-decode.json'
$mdFile   = Join-Path $outDir 'build42-loth-trailing-body-decode.md'

Assert-True ($t3Exit -eq 0)       'Test3: script exits 0'
Assert-True (Test-Path $jsonFile) 'Test4: JSON report exists'
Assert-True (Test-Path $mdFile)   'Test5: MD report exists'

$data      = if (Test-Path $jsonFile) { Get-Content $jsonFile -Raw | ConvertFrom-Json } else { $null }
$mdContent = if (Test-Path $mdFile)   { Get-Content $mdFile   -Raw } else { '' }

# Test 6: magic LOTH detected
$magicRecs = @(@($data.records) | Where-Object { $null -ne $_.field8_u32le })
Assert-True ($magicRecs.Count -gt 0) 'Test6: LOTH records detected (field8 present)'

# Test 7: ASCII entry count detected
$asciiRecs = @(@($data.records) | Where-Object { [int]$_.ascii_entry_count -gt 0 })
Assert-True ($asciiRecs.Count -gt 0) 'Test7: ascii_entry_count > 0 detected'

# Test 8: trailing_bytes_count detected
$trailRecs = @(@($data.records) | Where-Object { [int]$_.trailing_bytes_count -gt 0 })
Assert-True ($trailRecs.Count -gt 0) 'Test8: trailing_bytes_count > 0 detected'

# Test 9: trailing_bytes_mod4 recorded (0 for u32-aligned fixtures)
$mod4Recs = @(@($data.records) | Where-Object { $null -ne $_.trailing_bytes_mod4 })
Assert-True ($mod4Recs.Count -gt 0) 'Test9: trailing_bytes_mod4 recorded'
Assert-True ([int]$mod4Recs[0].trailing_bytes_mod4 -eq 0) 'Test9b: trailing_bytes_mod4 == 0 for u32-aligned fixture'

# Test 10: first_64_trailing_u32le recorded in summary records
# (check via JSON that the detail record has first_64_trailing_u32le in the full JSON)
$raw = Get-Content $jsonFile -Raw
Assert-True ($raw -match 'first_64_trailing_u32le') 'Test10: first_64_trailing_u32le recorded in JSON'

# Test 11: HYPOTHESIS_TRAILER_U32_RECORDS emitted (all fixtures are u32-aligned)
$overallHyps = [string[]]$data.overall_hypotheses
Assert-True ($overallHyps -contains 'HYPOTHESIS_TRAILER_U32_RECORDS') 'Test11: overall hypothesis includes HYPOTHESIS_TRAILER_U32_RECORDS'

# Test 12: HYPOTHESIS_TRAILER_REFERENCES_STRING_TABLE emitted (words 1,2 < field8=3)
Assert-True ($overallHyps -contains 'HYPOTHESIS_TRAILER_REFERENCES_STRING_TABLE') `
    'Test12: overall hypothesis includes HYPOTHESIS_TRAILER_REFERENCES_STRING_TABLE'

# Tests 13-16: status labels in MD
Assert-True ($mdContent -match 'BUILD42_LOTH_TRAILING_BODY_DECODED')  'Test13: BUILD42_LOTH_TRAILING_BODY_DECODED in MD'
Assert-True ($mdContent -match 'WRITER_NOT_CHANGED')                  'Test14: WRITER_NOT_CHANGED in MD'
Assert-True ($mdContent -match 'LOAD_TEST_NOT_PERFORMED')             'Test15: LOAD_TEST_NOT_PERFORMED in MD'
Assert-True ($mdContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'Test16: PLAYABLE_EXPORT_CLAIM_ALLOWED=false in MD'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
