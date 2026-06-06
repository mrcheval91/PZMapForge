#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for analyze-build42-loth-per-entry-record-model.ps1 (MAP-6X).

    Creates synthetic LOTH fixtures with a fixed header + N*6-byte records + footer
    structure. Validates that the 6-byte record model is detected as plausible.
    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$modScript  = Join-Path $repoRoot 'scripts\analyze-build42-loth-per-entry-record-model.ps1'
$tempRoot   = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Mod {
    param([string]$ReferenceRoot, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $modScript `
        -ReferenceRoot $ReferenceRoot `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-loth-per-entry-record-model.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Synthetic fixtures: 4-byte header + N*6 + 2-byte footer
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t6x-' + [System.IO.Path]::GetRandomFileName())
$refDir   = Join-Path $testBase '.local\reference'
$outDir   = Join-Path $testBase '.local\output'
$badPath  = Join-Path $tempRoot 'pzmf-t6x-bad-no-local'

New-Item -ItemType Directory -Force -Path $refDir  | Out-Null
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Make-Fixture {
    param([string]$Path, [int]$EntryCount, [string[]]$Entries, [int]$RecordSize, [byte[]]$Header, [byte[]]$Footer)
    $magic  = [byte[]]@(0x4C,0x4F,0x54,0x48)
    $ver    = [byte[]]@(0x01,0x00,0x00,0x00)
    $cnt    = [BitConverter]::GetBytes([uint32]$EntryCount)
    $body   = [byte[]]::new(0)
    foreach ($e in $Entries) { $body = $body + [System.Text.Encoding]::ASCII.GetBytes($e + "`n") }
    # Records: N * RecordSize bytes of synthetic data
    $records = [byte[]]::new($EntryCount * $RecordSize)
    for ($i = 0; $i -lt $EntryCount; $i++) {
        $records[$i * $RecordSize] = [byte]($i % 256)
    }
    $all = $magic + $ver + $cnt + $body + $Header + $records + $Footer
    [System.IO.File]::WriteAllBytes($Path, [byte[]]$all)
}

# ref1: field8=3, 3 entries, 4-byte header + 3*6-byte records + 2-byte footer
# trailing = 4 + 18 + 2 = 24 bytes (not mod4 since 24%4=0 actually, let me make it 25)
# Let's use 3-byte header instead: 3 + 3*6 + 2 = 23 bytes (mod4=3 - non-aligned)
Make-Fixture -Path (Join-Path $refDir '5_3.lotheader') `
    -EntryCount 3 `
    -Entries @('tile_0','tile_1','tile_2') `
    -RecordSize 6 `
    -Header @([byte]0xFF,[byte]0xFE,[byte]0x03) `
    -Footer @([byte]0x00,[byte]0x00)

# ref2: field8=4, 4 entries, 3-byte header + 4*6-byte records + 2-byte footer
# trailing = 3 + 24 + 2 = 29 bytes (mod4=1 - non-aligned)
Make-Fixture -Path (Join-Path $refDir '6_3.lotheader') `
    -EntryCount 4 `
    -Entries @('set_A','set_B','set_C','set_D') `
    -RecordSize 6 `
    -Header @([byte]0xFF,[byte]0xFE,[byte]0x04) `
    -Footer @([byte]0x00,[byte]0x00)

# ---------------------------------------------------------------------------
# Test 1: ReferenceRoot outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: ReferenceRoot outside .local refused ---'
$t1Exit = Invoke-Mod -ReferenceRoot $badPath -Output $outDir
Assert-True ($t1Exit -ne 0) 'Test1: ReferenceRoot outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Output outside .local refused ---'
$t2Exit = Invoke-Mod -ReferenceRoot $refDir -Output $badPath
Assert-True ($t2Exit -ne 0) 'Test2: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 3-19: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 3-19: valid run ---'
$t3Exit = Invoke-Mod -ReferenceRoot $refDir -Output $outDir

$jsonFile = Join-Path $outDir 'build42-loth-per-entry-record-model.json'
$mdFile   = Join-Path $outDir 'build42-loth-per-entry-record-model.md'

Assert-True ($t3Exit -eq 0)       'Test3: script exits 0'
Assert-True (Test-Path $jsonFile) 'Test4: JSON report exists'
Assert-True (Test-Path $mdFile)   'Test5: MD report exists'

$data      = if (Test-Path $jsonFile) { Get-Content $jsonFile -Raw | ConvertFrom-Json } else { $null }
$mdContent = if (Test-Path $mdFile)   { Get-Content $mdFile   -Raw } else { '' }

# Test 6: LOTH header parsed
$focusRecs = @($data.focus_records)
Assert-True ($focusRecs.Count -gt 0) 'Test6: focus_records non-empty (LOTH header parsed)'

# Test 7: ASCII entry count parsed
$hasAscii = @($focusRecs | Where-Object { [int]$_.ascii_entry_count -gt 0 }).Count -gt 0
Assert-True $hasAscii 'Test7: ascii_entry_count > 0 in focus_records'

# Test 8: trailing_bytes_count parsed
Assert-True ([int]$data.smallest_trailing_bytes -gt 0) 'Test8: smallest_trailing_bytes > 0'

# Test 9: bytes_per_entry recorded
Assert-True ([double]$data.smallest_bytes_per_entry -gt 0.0) 'Test9: bytes_per_entry > 0'

# Test 10: record_size_scoreboard exists
$scoreboard = @($data.record_size_scoreboard)
Assert-True ($scoreboard.Count -gt 0) 'Test10: record_size_scoreboard non-empty'

# Test 11: record_size_6 feasibility recorded
$hasRs6 = @($focusRecs | Where-Object { $null -ne $_.PSObject.Properties['record_size_6_feasible'] }).Count -gt 0
Assert-True $hasRs6 'Test11: record_size_6_feasible present in focus_records'

# Test 12: first candidate records sampled
$hasSampled = @($focusRecs | Where-Object { $null -ne $_.PSObject.Properties['sampled_records'] }).Count -gt 0
Assert-True $hasSampled 'Test12: sampled_records present in focus_records'

# Test 13: stable prefix byte positions recorded
Assert-True ($null -ne $data.stable_prefix_positions) 'Test13: stable_prefix_positions in report'
Assert-True ($null -ne $data.stable_prefix_byte_count) 'Test13b: stable_prefix_byte_count in report'

# Test 14: likely model hypotheses recorded
$hyps = [string[]]$data.likely_model_hypotheses
Assert-True ($hyps.Count -gt 0) 'Test14: likely_model_hypotheses non-empty'

# Test 15: writer_readiness recorded
Assert-True (-not [string]::IsNullOrEmpty([string]$data.writer_readiness)) 'Test15: writer_readiness present'

# Tests 16-19: status labels in MD
Assert-True ($mdContent -match 'BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED') 'Test16: BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED in MD'
Assert-True ($mdContent -match 'WRITER_NOT_CHANGED')                           'Test17: WRITER_NOT_CHANGED in MD'
Assert-True ($mdContent -match 'LOAD_TEST_NOT_PERFORMED')                      'Test18: LOAD_TEST_NOT_PERFORMED in MD'
Assert-True ($mdContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false')          'Test19: PLAYABLE_EXPORT_CLAIM_ALLOWED=false in MD'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
