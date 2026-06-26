#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

$assertCount  = 0
$failCount    = 0

function Assert-True([bool]$cond, [string]$msg) {
    $script:assertCount++
    if (-not $cond) {
        $script:failCount++
        Write-Output "  FAIL: $msg"
    } else {
        Write-Output "  PASS: $msg"
    }
}

$prepareScript = Join-Path $scriptDir 'prepare-build42-map37c-chunkdata-staged-packet.ps1'
$testOutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map37c-chunkdata-staged-packet-test'
$mapId          = 'pzmapforge_map37c'
$chunkRel       = "${mapId}_build42_candidate\42\media\maps\${mapId}\chunkdata_35_27.bin"
$chunkPath      = Join-Path $testOutputRoot "run1\$chunkRel"
$jsonPath       = Join-Path $testOutputRoot 'map37c-chunkdata-staged-packet.json'
$mdPath         = Join-Path $testOutputRoot 'map37c-chunkdata-staged-packet.md'

Write-Output ""
Write-Output "--- MAP-37C chunkdata staged packet tests ---"

# Test 1: Guard exits nonzero for path outside .local
try {
    & powershell -ExecutionPolicy Bypass -File $prepareScript -OutputRoot 'C:\forbidden_test_path_map37c' *>$null
} catch { }
Assert-True ($LASTEXITCODE -ne 0) "Guard: exits nonzero for path outside .local"

# Test 2: Exits 0 with valid .local path
Remove-Item -Recurse -Force $testOutputRoot -ErrorAction SilentlyContinue
& powershell -ExecutionPolicy Bypass -File $prepareScript -OutputRoot $testOutputRoot
Assert-True ($LASTEXITCODE -eq 0) "Prepare script exits 0 with valid .local path"

# Test 3: Preflight JSON exists
Assert-True (Test-Path $jsonPath) "Preflight JSON exists"

# Test 4: Preflight MD exists
Assert-True (Test-Path $mdPath) "Preflight MD exists"

# Test 5: chunkdata_35_27.bin exists in run1 output
Assert-True (Test-Path $chunkPath) "chunkdata_35_27.bin exists in run1 output"

# Read binary for structural checks
$bytes = [System.IO.File]::ReadAllBytes($chunkPath)

# Test 6: File size = 1026
Assert-True ($bytes.Length -eq 1026) "chunkdata_35_27.bin size == 1026"

# Test 7: header byte 0 == 0x00
Assert-True ([int]$bytes[0] -eq 0) "chunkdata header byte 0 == 0x00"

# Test 8: header byte 1 == 0x01
Assert-True ([int]$bytes[1] -eq 1) "chunkdata header byte 1 == 0x01"

# Test 9: body length (1026 - 2 = 1024) divisible by 8
$bodyLen = $bytes.Length - 2
Assert-True ($bodyLen % 8 -eq 0) "chunkdata body length ($bodyLen) divisible by 8"

# Test 10: record count = 128
$recordCount = $bodyLen / 8
Assert-True ($recordCount -eq 128) "chunkdata record count == 128"

# Read and parse preflight JSON
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 11: Schema field correct
Assert-True ($p.schema -eq 'pzmapforge.map37c-chunkdata-staged-packet.v0.1') "schema == 'pzmapforge.map37c-chunkdata-staged-packet.v0.1'"

# Test 12: chunkdata_header_size = 2
Assert-True ([int]$p.chunkdata_header_size -eq 2) "chunkdata_header_size == 2"

# Test 13: chunkdata_record_width = 8
Assert-True ([int]$p.chunkdata_record_width -eq 8) "chunkdata_record_width == 8"

# Test 14: chunkdata_record_count = 128
Assert-True ([int]$p.chunkdata_record_count -eq 128) "chunkdata_record_count == 128"

# Test 15: chunkdata_exact_fit = true
Assert-True ($p.chunkdata_exact_fit -eq $true) "chunkdata_exact_fit == true"

# Test 16: chunkdata_sha256_run1 is 64-char lowercase hex
Assert-True ([string]$p.chunkdata_sha256_run1 -match '^[0-9a-f]{64}$') "chunkdata_sha256_run1 is 64-char hex"

# Test 17: chunkdata_sha256_run2 is 64-char lowercase hex
Assert-True ([string]$p.chunkdata_sha256_run2 -match '^[0-9a-f]{64}$') "chunkdata_sha256_run2 is 64-char hex"

# Test 18: run1 SHA256 == run2 SHA256 (deterministic)
Assert-True ([string]$p.chunkdata_sha256_run1 -eq [string]$p.chunkdata_sha256_run2) "chunkdata SHA256 run1 == run2 (deterministic)"

# Test 19: chunkdata_deterministic = true
Assert-True ($p.chunkdata_deterministic -eq $true) "chunkdata_deterministic == true"

# Test 20: PLAYABLE_EXPORT_CLAIM_ALLOWED = false
Assert-True ($p.PLAYABLE_EXPORT_CLAIM_ALLOWED -eq $false) "PLAYABLE_EXPORT_CLAIM_ALLOWED == false"

# Cleanup
Remove-Item -Recurse -Force $testOutputRoot -ErrorAction SilentlyContinue

Write-Output ""
Write-Output "MAP-37C tests: $assertCount assertions, $failCount failures"

if ($failCount -gt 0) {
    Write-Error "MAP-37C tests FAILED: $failCount of $assertCount assertions failed."
    exit 1
}

Write-Output "MAP-37C tests PASSED."
exit 0
