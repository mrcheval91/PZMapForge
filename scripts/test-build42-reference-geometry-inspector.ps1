#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for inspect-build42-reference-geometry.ps1 (MAP-6F).

    Runs the inspector against synthetic fixture files under a temp .local
    directory and validates the output report fields.
    Does not use real PZ map files.
    Does not copy any PZ assets.
    Does not perform a load test.
    Does not claim playable export.

    Expected assertion count: 10
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$inspector   = Join-Path $repoRoot 'scripts\inspect-build42-reference-geometry.ps1'
$tempRoot    = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-reference-geometry-inspector.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Create temp .local source and output dirs under system temp
# ---------------------------------------------------------------------------

$testBase   = Join-Path $tempRoot ('pzmapforge-geo-test-' + [System.IO.Path]::GetRandomFileName())
$testSource = Join-Path $testBase '.local\source\testmap'
$testOutput = Join-Path $testBase '.local\output'
$badPath    = Join-Path $tempRoot 'not-local-at-all'

New-Item -ItemType Directory -Force -Path $testSource | Out-Null
New-Item -ItemType Directory -Force -Path $testOutput | Out-Null
New-Item -ItemType Directory -Force -Path $badPath    | Out-Null

# ---------------------------------------------------------------------------
# Synthesize binary fixture files
# ---------------------------------------------------------------------------

# Synthetic lotpack: hdrA=900 (0x84 0x03 0x00 0x00), hdrB=7204 (0x24 0x1C 0x00 0x00)
$lotpackBytes = [byte[]]::new(7208)
$lotpackBytes[0] = 0x84; $lotpackBytes[1] = 0x03  # hdrA = 900 LE
$lotpackBytes[4] = 0x24; $lotpackBytes[5] = 0x1C  # hdrB = 7204 LE
[System.IO.File]::WriteAllBytes((Join-Path $testSource 'world_0_0.lotpack'), $lotpackBytes)

# Synthetic chunkdata 902 bytes: body = 900 → 30x30 model
$chunkdata902 = [byte[]]::new(902)
$chunkdata902[0] = 0x00; $chunkdata902[1] = 0x01
[System.IO.File]::WriteAllBytes((Join-Path $testSource 'chunkdata_0_0.bin'), $chunkdata902)

# Synthetic chunkdata 1026 bytes: body = 1024 → 32x32 candidate (256-model)
$chunkdata1026 = [byte[]]::new(1026)
$chunkdata1026[0] = 0x00; $chunkdata1026[1] = 0x01
[System.IO.File]::WriteAllBytes((Join-Path $testSource 'chunkdata_0_1.bin'), $chunkdata1026)

# Synthetic lotheader (8 bytes)
$lotheaderBytes = [byte[]]::new(8)
[System.IO.File]::WriteAllBytes((Join-Path $testSource '0_0.lotheader'), $lotheaderBytes)

# Text fixtures
Set-Content -Path (Join-Path $testSource 'map.info')       -Value 'title=TestMap' -Encoding UTF8
Set-Content -Path (Join-Path $testSource 'mod.info')       -Value 'id=testmap'    -Encoding UTF8

# ---------------------------------------------------------------------------
# Create LOTP source directory (Build 42 reference format)
# ---------------------------------------------------------------------------

$testSourceLotp = Join-Path $testBase '.local\source-lotp\testmap-lotp'
New-Item -ItemType Directory -Force -Path $testSourceLotp | Out-Null

# Synthetic LOTP lotpack: first 4 bytes = 4C 4F 54 50 ("LOTP"), bytes 4-7 = 01 00 00 00 (version=1)
$lotpBytes = [byte[]]::new(64)
$lotpBytes[0] = 0x4C; $lotpBytes[1] = 0x4F; $lotpBytes[2] = 0x54; $lotpBytes[3] = 0x50  # LOTP magic
$lotpBytes[4] = 0x01; $lotpBytes[5] = 0x00; $lotpBytes[6] = 0x00; $lotpBytes[7] = 0x00  # version = 1
[System.IO.File]::WriteAllBytes((Join-Path $testSourceLotp 'world_0_0.lotpack'), $lotpBytes)

# ---------------------------------------------------------------------------
# Create deep LOTH source (LOTH lotheader + LOTP lotpack + 1026-byte chunkdata)
# This exercises all three Build 42 format detections together.
# ---------------------------------------------------------------------------

$testSourceLtzh = Join-Path $testBase '.local\source-ltzh\testmap-ltzh'
New-Item -ItemType Directory -Force -Path $testSourceLtzh | Out-Null

# Synthetic LOTH lotheader: first 4 bytes = 4C 54 5A 48 ("LOTH"), bytes 4-7 = 01 00 00 00 (version=1)
$ltzhBytes = [byte[]]::new(64)
$ltzhBytes[0] = 0x4C; $ltzhBytes[1] = 0x4F; $ltzhBytes[2] = 0x54; $ltzhBytes[3] = 0x48  # LOTH magic (4C 4F 54 48 = "LOTH")
$ltzhBytes[4] = 0x01; $ltzhBytes[5] = 0x00; $ltzhBytes[6] = 0x00; $ltzhBytes[7] = 0x00  # version = 1
[System.IO.File]::WriteAllBytes((Join-Path $testSourceLtzh '0_0.lotheader'), $ltzhBytes)

# Also add a LOTP lotpack so we can test BUILD42_256_MODEL_STRONGLY_SUPPORTED
$lotpBytesForLtzh = [byte[]]::new(64)
$lotpBytesForLtzh[0] = 0x4C; $lotpBytesForLtzh[1] = 0x4F; $lotpBytesForLtzh[2] = 0x54; $lotpBytesForLtzh[3] = 0x50
$lotpBytesForLtzh[4] = 0x01
[System.IO.File]::WriteAllBytes((Join-Path $testSourceLtzh 'world_0_0.lotpack'), $lotpBytesForLtzh)

# Also add 1026-byte chunkdata (body=1024, grid=32x32)
$chunkdata1026ForLtzh = [byte[]]::new(1026)
$chunkdata1026ForLtzh[0] = 0x00; $chunkdata1026ForLtzh[1] = 0x01
[System.IO.File]::WriteAllBytes((Join-Path $testSourceLtzh 'chunkdata_0_0.bin'), $chunkdata1026ForLtzh)

# ---------------------------------------------------------------------------
# Helper: run inspector without stopping on nonzero exit
# ---------------------------------------------------------------------------

function Invoke-Inspector {
    param([string[]]$ArgList)
    $savedPref = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $null = & powershell -ExecutionPolicy Bypass -File $inspector @ArgList
    $ec   = $LASTEXITCODE
    $ErrorActionPreference = $savedPref
    return $ec
}

# ---------------------------------------------------------------------------
# Test 1: Source outside .local exits nonzero
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Source outside .local exits nonzero ---'
$ec1 = Invoke-Inspector @('-Source', $badPath, '-Output', $testOutput)
Assert-True ($ec1 -ne 0) 'Source outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local exits nonzero
# ---------------------------------------------------------------------------

Write-Output '--- Test 2: Output outside .local exits nonzero ---'
$ec2 = Invoke-Inspector @('-Source', $testSource, '-Output', $badPath)
Assert-True ($ec2 -ne 0) 'Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 3: Valid source and output exits 0
# ---------------------------------------------------------------------------

Write-Output '--- Test 3: Valid run exits 0 ---'
$validOutput = Join-Path $testBase '.local\output\run1'
$ec3 = Invoke-Inspector @('-Source', $testSource, '-Output', $validOutput, '-MaxFiles', '10')
Assert-True ($ec3 -eq 0) 'Valid run exits 0'

# ---------------------------------------------------------------------------
# Test 4: Report JSON written
# ---------------------------------------------------------------------------

Write-Output '--- Test 4: Report JSON written ---'
$jsonFile = Join-Path $validOutput 'build42-reference-geometry-report.json'
Assert-True (Test-Path $jsonFile) 'build42-reference-geometry-report.json exists'

# ---------------------------------------------------------------------------
# Test 5: Report MD written
# ---------------------------------------------------------------------------

Write-Output '--- Test 5: Report MD written ---'
$mdFile = Join-Path $validOutput 'build42-reference-geometry-report.md'
Assert-True (Test-Path $mdFile) 'build42-reference-geometry-report.md exists'

# ---------------------------------------------------------------------------
# Parse report JSON for remaining assertions
# ---------------------------------------------------------------------------

$report = Get-Content $jsonFile -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Test 6: Synthetic lotpack hdrA parsed as 900
# ---------------------------------------------------------------------------

Write-Output '--- Test 6: lotpack hdrA_u32le = 900 ---'
$lotRec = $report.lotpack_records | Where-Object { $_.file -match 'world_0_0' } | Select-Object -First 1
Assert-True ($null -ne $lotRec -and [int]$lotRec.hdrA_u32le -eq 900) 'lotpack hdrA_u32le == 900'

# ---------------------------------------------------------------------------
# Test 7: Synthetic chunkdata 902 bytes → body_bytes = 900
# ---------------------------------------------------------------------------

Write-Output '--- Test 7: chunkdata 902b body_bytes = 900 ---'
$cd902 = $report.chunkdata_records | Where-Object { $_.file -match 'chunkdata_0_0' } | Select-Object -First 1
Assert-True ($null -ne $cd902 -and [int]$cd902.body_bytes -eq 900) 'chunkdata_0_0 body_bytes == 900'

# ---------------------------------------------------------------------------
# Test 8: Synthetic chunkdata 1026 bytes → body_bytes = 1024
# ---------------------------------------------------------------------------

Write-Output '--- Test 8: chunkdata 1026b body_bytes = 1024 ---'
$cd1026 = $report.chunkdata_records | Where-Object { $_.file -match 'chunkdata_0_1' } | Select-Object -First 1
Assert-True ($null -ne $cd1026 -and [int]$cd1026.body_bytes -eq 1024) 'chunkdata_0_1 body_bytes == 1024'

# ---------------------------------------------------------------------------
# Test 9: reference_files_copied = false
# ---------------------------------------------------------------------------

Write-Output '--- Test 9: reference_files_copied = false ---'
Assert-True ($report.safety.reference_files_copied -eq $false) 'safety.reference_files_copied == false'

# ---------------------------------------------------------------------------
# Test 10: playable_export_claimed = false
# ---------------------------------------------------------------------------

Write-Output '--- Test 10: playable_export_claimed = false ---'
Assert-True ($report.safety.playable_export_claimed -eq $false) 'safety.playable_export_claimed == false'

# ---------------------------------------------------------------------------
# LOTP tests: run inspector against LOTP source
# ---------------------------------------------------------------------------

$lotpOutput = Join-Path $testBase '.local\output\run-lotp'
$ec11 = Invoke-Inspector @('-Source', $testSourceLotp, '-Output', $lotpOutput, '-MaxFiles', '10')

# Test 11: LOTP source exits 0 (no crash despite LOTP magic)
Write-Output '--- Test 11: LOTP lotpack source exits 0 ---'
Assert-True ($ec11 -eq 0) 'LOTP source run exits 0'

$lotpJsonFile = Join-Path $lotpOutput 'build42-reference-geometry-report.json'
$lotpReport   = Get-Content $lotpJsonFile -Raw | ConvertFrom-Json

# Test 12: lotpack_format = LOTP in record
Write-Output '--- Test 12: lotpack_format = LOTP ---'
$lotpRec = $lotpReport.lotpack_records | Where-Object { $_.file -match 'world_0_0' } | Select-Object -First 1
Assert-True ($null -ne $lotpRec -and $lotpRec.lotpack_format -eq 'LOTP') 'lotpack_format == LOTP'

# Test 13: lotpack_magic = LOTP in record
Write-Output '--- Test 13: lotpack_magic = LOTP ---'
Assert-True ($null -ne $lotpRec -and $lotpRec.lotpack_magic -eq 'LOTP') 'lotpack_magic == LOTP'

# Test 14: lotpack_lotp_count >= 1 in report
Write-Output '--- Test 14: lotpack_lotp_count >= 1 ---'
Assert-True ([int]$lotpReport.lotpack_lotp_count -ge 1) 'lotpack_lotp_count >= 1'

# Test 15: geometry_status = BUILD42_LOTP_FORMAT_OBSERVED
Write-Output '--- Test 15: geometry_status = BUILD42_LOTP_FORMAT_OBSERVED ---'
Assert-True ($lotpReport.geometry_status -eq 'BUILD42_LOTP_FORMAT_OBSERVED') 'geometry_status == BUILD42_LOTP_FORMAT_OBSERVED'

# ---------------------------------------------------------------------------
# LOTP deep field tests (from the LOTP run output)
# ---------------------------------------------------------------------------

# Test 16: first_16_bytes_hex exists in LOTP lotpack record
Write-Output '--- Test 16: LOTP lotpack record has first_16_bytes_hex ---'
$lotpRecDeep = $lotpReport.lotpack_records | Where-Object { $_.lotpack_format -eq 'LOTP' } | Select-Object -First 1
Assert-True ($null -ne $lotpRecDeep -and -not [string]::IsNullOrEmpty($lotpRecDeep.first_16_bytes_hex)) 'LOTP record has first_16_bytes_hex'

# Test 17: first_32_bytes_hex exists in LOTP lotpack record
Write-Output '--- Test 17: LOTP lotpack record has first_32_bytes_hex ---'
Assert-True ($null -ne $lotpRecDeep -and -not [string]::IsNullOrEmpty($lotpRecDeep.first_32_bytes_hex)) 'LOTP record has first_32_bytes_hex'

# ---------------------------------------------------------------------------
# LOTH tests: run inspector against LOTH + LOTP + chunkdata1024 source
# ---------------------------------------------------------------------------

$ltzhOutput = Join-Path $testBase '.local\output\run-ltzh'
$ec16 = Invoke-Inspector @('-Source', $testSourceLtzh, '-Output', $ltzhOutput, '-MaxFiles', '10')

# Test 18: LOTH source exits 0
Write-Output '--- Test 18: LOTH/LOTP/chunkdata1024 source exits 0 ---'
Assert-True ($ec16 -eq 0) 'LOTH source run exits 0'

$ltzhJsonFile = Join-Path $ltzhOutput 'build42-reference-geometry-report.json'
$ltzhReport   = Get-Content $ltzhJsonFile -Raw | ConvertFrom-Json

# Test 19: lotheader_format = LOTH in record
Write-Output '--- Test 19: lotheader_format = LOTH ---'
$ltzhRec = $ltzhReport.lotheader_records | Where-Object { $_.lotheader_format -eq 'LOTH' } | Select-Object -First 1
Assert-True ($null -ne $ltzhRec) 'lotheader_format == LOTH record found'

# Test 20: lotheader_magic = LOTH
Write-Output '--- Test 20: lotheader_magic = LOTH ---'
Assert-True ($null -ne $ltzhRec -and $ltzhRec.lotheader_magic -eq 'LOTH') 'lotheader_magic == LOTH'

# Test 21: lotheader_ltz_count >= 1
Write-Output '--- Test 21: lotheader_loth_count >= 1 ---'
Assert-True ([int]$ltzhReport.lotheader_loth_count -ge 1) 'lotheader_loth_count >= 1'

# Test 22: BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED in geometry_statuses
Write-Output '--- Test 22: BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED in geometry_statuses ---'
$ltzhStatuses = @($ltzhReport.geometry_statuses)
Assert-True ($ltzhStatuses -contains 'BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED') 'geometry_statuses contains BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED'

# Test 23: BUILD42_32X32_CHUNK_GRID_OBSERVED in geometry_statuses
Write-Output '--- Test 23: BUILD42_32X32_CHUNK_GRID_OBSERVED in geometry_statuses ---'
Assert-True ($ltzhStatuses -contains 'BUILD42_32X32_CHUNK_GRID_OBSERVED') 'geometry_statuses contains BUILD42_32X32_CHUNK_GRID_OBSERVED'

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
