#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for derive-build42-format-design-matrix.ps1 (MAP-6I).

    Creates a synthetic MAP-6H-style inspection report under temp .local and
    verifies the design matrix output. Does not use real PZ files.
    Expected assertion count: 13
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$matrixScript = Join-Path $repoRoot 'scripts\derive-build42-format-design-matrix.ps1'
$tempRoot   = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-format-design-matrix.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: temp .local dirs
# ---------------------------------------------------------------------------

$testBase  = Join-Path $tempRoot ('pzmapforge-matrix-test-' + [System.IO.Path]::GetRandomFileName())
$testInput = Join-Path $testBase '.local\input'
$testOut   = Join-Path $testBase '.local\output'
$badPath   = Join-Path $tempRoot 'not-local-matrix'

New-Item -ItemType Directory -Force -Path $testInput | Out-Null
New-Item -ItemType Directory -Force -Path $testOut   | Out-Null
New-Item -ItemType Directory -Force -Path $badPath   | Out-Null

# ---------------------------------------------------------------------------
# Synthetic inspection report (v0.2)
# Two LOTP records: words[0]=1347702604, words[1]=1, words[2]=1024, words[3]=8204, rest stable
# Two LOTH records: words[0]=1213484876, words[1]=1, words[2] VARIABLE (47/85), rest identical
# One chunkdata record: body=1024
# ---------------------------------------------------------------------------

$lotpWords0 = @(1347702604, 1, 1024, 8204, 0, 9228, 0, 10252, 0, 11276, 0, 12300, 0, 13324, 0, 14348)
$lotpWords1 = @(1347702604, 1, 1024, 8204, 0, 9228, 0, 10252, 0, 11276, 0, 12300, 0, 13324, 0, 14348)

$lothWords0 = @(1213484876, 1, 47, 1852140642, 1734308708, 1936941426, 1919252079, 1937334636, 1597059167, 1818364464, 1935961701, 1634887519, 1987015539, 1634497125, 811561849, 171007793)
$lothWords1 = @(1213484876, 1, 85, 1852140642, 1734308708, 1936941426, 1919252079, 1937334636, 1597059167, 1818364464, 1935961701, 1634887519, 1987015539, 1634497125, 811561849, 171007793)

$syntheticReport = [ordered]@{
    schema              = 'pzmapforge.build42-reference-geometry-report.v0.2'
    geometry_status     = 'BUILD42_256_MODEL_STRONGLY_SUPPORTED'
    geometry_statuses   = @('BUILD42_LOTP_FORMAT_OBSERVED','BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED','BUILD42_32X32_CHUNK_GRID_OBSERVED','BUILD42_256_MODEL_STRONGLY_SUPPORTED','GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED','PLAYABLE_EXPORT_CLAIM_ALLOWED=false')
    source_path         = '/synthetic/test'
    lotpack_count       = 2
    lotpack_lotp_count  = 2
    lotheader_count     = 2
    lotheader_loth_count = 2
    chunkdata_count     = 1
    chunkdata_body_1024_count = 1
    chunkdata_body_900_count  = 0
    REFERENCE_GEOMETRY_OBSERVED = $true
    PLAYABLE_EXPORT_CLAIM_ALLOWED = 'false'
    lotpack_records = @(
        [ordered]@{
            file='world_0_0.lotpack'; size_bytes=1057348; lotpack_format='LOTP'
            lotpack_magic='LOTP'; lotpack_version_field_u32le=1
            first_16_bytes_hex='4c4f545001000000000400000c200000'
            first_32_bytes_hex='4c4f545001000000000400000c200000'+'00000000'+'0c240000'+'00000000'
            first_64_bytes_hex='4c4f545001000000000400000c20000000000000'+'0c24000000000000'+'0c28000000000000'+'0c2c000000000000'
            u32le_words_first_64 = $lotpWords0
        },
        [ordered]@{
            file='world_0_1.lotpack'; size_bytes=1057348; lotpack_format='LOTP'
            lotpack_magic='LOTP'; lotpack_version_field_u32le=1
            first_16_bytes_hex='4c4f545001000000000400000c200000'
            first_32_bytes_hex='4c4f545001000000000400000c200000'+'00000000'+'0c240000'+'00000000'
            first_64_bytes_hex='4c4f545001000000000400000c20000000000000'+'0c24000000000000'+'0c28000000000000'+'0c2c000000000000'
            u32le_words_first_64 = $lotpWords1
        }
    )
    lotheader_records = @(
        [ordered]@{
            file='0_0.lotheader'; size_bytes=3459; lotheader_format='LOTH'
            lotheader_magic='LOTH'; lotheader_version_field_u32le=1
            first_16_bytes_hex='4c4f5448010000002f000000'+'62'*4
            first_32_bytes_hex='4c4f5448010000002f000000'+'626c656e64735f67726173736f7665726c6179735f30315f300a'
            first_64_bytes_hex='4c4f5448010000002f000000'+'626c656e64735f67726173736f7665726c6179735f30315f300a626c656e64735f67726173736f7665726c6179735f30315f310a'
            u32le_words_first_64 = $lothWords0
        },
        [ordered]@{
            file='0_1.lotheader'; size_bytes=5400; lotheader_format='LOTH'
            lotheader_magic='LOTH'; lotheader_version_field_u32le=1
            first_16_bytes_hex='4c4f544801000000550000006'+'2'*4
            first_32_bytes_hex='4c4f544801000000550000006'+'2626c656e64735f67726173736f7665726c6179735f30315f300a'
            first_64_bytes_hex='4c4f54480100000055000000'+'626c656e64735f67726173736f7665726c6179735f30315f300a626c656e64735f67726173736f7665726c6179735f30315f310a'
            u32le_words_first_64 = $lothWords1
        }
    )
    chunkdata_records = @(
        [ordered]@{
            file='chunkdata_0_0.bin'; size_bytes=1026; body_bytes=1024; chunk_grid_candidate='32x32_1024'
            header_byte_0='0x00'; header_byte_1='0x01'
            first_32_bytes_hex='0001'+'00'*30
            u32le_words_first_32 = @(256, 0, 0, 0, 0, 0, 0, 0)
        }
    )
    text_files_found = @()
    safety = [ordered]@{ reference_files_copied=$false; pz_assets_copied=$false; playable_export_claimed=$false; load_test_performed=$false }
}

$reportPath = Join-Path $testInput 'build42-reference-geometry-report.json'
$syntheticReport | ConvertTo-Json -Depth 8 | Set-Content -Path $reportPath -Encoding UTF8

# ---------------------------------------------------------------------------
# Helper: run script without stopping on nonzero exit
# ---------------------------------------------------------------------------

function Invoke-Matrix {
    param([string[]]$ArgList)
    $savedPref = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $null = & powershell -ExecutionPolicy Bypass -File $matrixScript @ArgList
    $ec   = $LASTEXITCODE; $ErrorActionPreference = $savedPref; return $ec
}

# ---------------------------------------------------------------------------
# Tests 1–2: path guards
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: InspectionReport outside .local exits nonzero ---'
$ec1 = Invoke-Matrix @('-InspectionReport', (Join-Path $badPath 'report.json'), '-Output', $testOut)
Assert-True ($ec1 -ne 0) 'InspectionReport outside .local exits nonzero'

Write-Output '--- Test 2: Output outside .local exits nonzero ---'
$ec2 = Invoke-Matrix @('-InspectionReport', $reportPath, '-Output', $badPath)
Assert-True ($ec2 -ne 0) 'Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 3: valid run exits 0
# ---------------------------------------------------------------------------

Write-Output '--- Test 3: Valid run exits 0 ---'
$validOut = Join-Path $testBase '.local\matrix-out'
$ec3 = Invoke-Matrix @('-InspectionReport', $reportPath, '-Output', $validOut)
Assert-True ($ec3 -eq 0) 'Valid run exits 0'

$jsonFile = Join-Path $validOut 'build42-format-design-matrix.json'
$mdFile   = Join-Path $validOut 'build42-format-design-matrix.md'
$matrix   = Get-Content $jsonFile -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Tests 4–5: files written
# ---------------------------------------------------------------------------

Write-Output '--- Test 4: JSON written ---'
Assert-True (Test-Path $jsonFile) 'build42-format-design-matrix.json exists'

Write-Output '--- Test 5: MD written ---'
Assert-True (Test-Path $mdFile) 'build42-format-design-matrix.md exists'

# ---------------------------------------------------------------------------
# Tests 6–7: LOTP stable magic and version
# ---------------------------------------------------------------------------

Write-Output '--- Test 6: LOTP word[0] is stable_magic ---'
$lotpW0 = @($matrix.lotp_lotpack.word_stability) | Where-Object { $_.position -eq 0 } | Select-Object -First 1
Assert-True ($null -ne $lotpW0 -and $lotpW0.label -eq 'stable_magic') 'LOTP word[0] label == stable_magic'

Write-Output '--- Test 7: LOTP word[1] is stable_version ---'
$lotpW1 = @($matrix.lotp_lotpack.word_stability) | Where-Object { $_.position -eq 1 } | Select-Object -First 1
Assert-True ($null -ne $lotpW1 -and $lotpW1.label -eq 'stable_version') 'LOTP word[1] label == stable_version'

# ---------------------------------------------------------------------------
# Tests 8–9: LOTH stable magic and version
# ---------------------------------------------------------------------------

Write-Output '--- Test 8: LOTH word[0] is stable_magic ---'
$lothW0 = @($matrix.loth_lotheader.word_stability) | Where-Object { $_.position -eq 0 } | Select-Object -First 1
Assert-True ($null -ne $lothW0 -and $lothW0.label -eq 'stable_magic') 'LOTH word[0] label == stable_magic'

Write-Output '--- Test 9: LOTH word[1] is stable_version ---'
$lothW1 = @($matrix.loth_lotheader.word_stability) | Where-Object { $_.position -eq 1 } | Select-Object -First 1
Assert-True ($null -ne $lothW1 -and $lothW1.label -eq 'stable_version') 'LOTH word[1] label == stable_version'

# ---------------------------------------------------------------------------
# Test 10: LOTH word[2] is variable (entry_count differs across files)
# ---------------------------------------------------------------------------

Write-Output '--- Test 10: LOTH word[2] is variable_entry_count ---'
$lothW2 = @($matrix.loth_lotheader.word_stability) | Where-Object { $_.position -eq 2 } | Select-Object -First 1
Assert-True ($null -ne $lothW2 -and -not $lothW2.stable) 'LOTH word[2] is variable (entry_count differs per cell)'

# ---------------------------------------------------------------------------
# Test 11: chunkdata 1024 dominance
# ---------------------------------------------------------------------------

Write-Output '--- Test 11: chunkdata dominant model is 32x32_1024 ---'
Assert-True ($matrix.chunkdata.dominant_model -eq '32x32_1024') 'chunkdata.dominant_model == 32x32_1024'

# ---------------------------------------------------------------------------
# Tests 12–13: safety flags in JSON
# ---------------------------------------------------------------------------

Write-Output '--- Test 12: WRITER_NOT_IMPLEMENTED = true ---'
Assert-True ($matrix.WRITER_NOT_IMPLEMENTED -eq $true) 'WRITER_NOT_IMPLEMENTED == true'

Write-Output '--- Test 13: PLAYABLE_EXPORT_CLAIM_ALLOWED = false ---'
Assert-True ($matrix.PLAYABLE_EXPORT_CLAIM_ALLOWED -eq 'false') 'PLAYABLE_EXPORT_CLAIM_ALLOWED == false'

# ---------------------------------------------------------------------------
# Cleanup
# ---------------------------------------------------------------------------

try { Remove-Item -LiteralPath $testBase -Recurse -Force -ErrorAction SilentlyContinue } catch {}
try { Remove-Item -LiteralPath $badPath  -Recurse -Force -ErrorAction SilentlyContinue } catch {}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
