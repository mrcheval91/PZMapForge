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
