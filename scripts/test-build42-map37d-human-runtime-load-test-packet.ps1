#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

$assertCount = 0
$failCount   = 0

function Assert-True([bool]$cond, [string]$msg) {
    $script:assertCount++
    if (-not $cond) {
        $script:failCount++
        Write-Output "  FAIL: $msg"
    } else {
        Write-Output "  PASS: $msg"
    }
}

$prepareScript  = Join-Path $scriptDir 'prepare-build42-map37d-human-runtime-load-test-packet.ps1'
$testOutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map37d-human-runtime-load-test-packet-test'

$jsonPath        = Join-Path $testOutputRoot 'map37d-human-runtime-load-test-packet.json'
$mdPath          = Join-Path $testOutputRoot 'map37d-human-runtime-load-test-packet.md'
$packetDocPath   = Join-Path $testOutputRoot 'MAP_37D_HUMAN_RUNTIME_LOAD_TEST_PACKET.md'
$installDocPath  = Join-Path $testOutputRoot 'MAP_37D_HUMAN_INSTALL_STEPS.md'
$wiringDocPath   = Join-Path $testOutputRoot 'MAP_37D_SERVER_WIRING.md'
$logDocPath      = Join-Path $testOutputRoot 'MAP_37D_LOG_CAPTURE_COMMANDS.md'
$criteriaDocPath = Join-Path $testOutputRoot 'MAP_37D_SUCCESS_FAILURE_CRITERIA.md'
$resultDocPath   = Join-Path $testOutputRoot 'MAP_37D_RUNTIME_RESULT_RECORD.md'

Write-Output ""
Write-Output "--- MAP-37D human runtime load-test packet tests ---"

# Test 1: Guard exits nonzero for path outside .local
try {
    & powershell -ExecutionPolicy Bypass -File $prepareScript -OutputRoot 'C:\forbidden_test_path_map37d' *>$null
} catch { }
Assert-True ($LASTEXITCODE -ne 0) "Guard: exits nonzero for path outside .local"

# Test 2: Prepare script exits 0 with valid .local path
Remove-Item -Recurse -Force $testOutputRoot -ErrorAction SilentlyContinue
& powershell -ExecutionPolicy Bypass -File $prepareScript -OutputRoot $testOutputRoot
Assert-True ($LASTEXITCODE -eq 0) "Prepare script exits 0 with valid .local path"

# Test 3: Preflight JSON exists
Assert-True (Test-Path $jsonPath) "Preflight JSON exists"

# Test 4: Preflight MD exists
Assert-True (Test-Path $mdPath) "Preflight MD exists"

# Test 5: MAP_37D_HUMAN_RUNTIME_LOAD_TEST_PACKET.md exists
Assert-True (Test-Path $packetDocPath) "MAP_37D_HUMAN_RUNTIME_LOAD_TEST_PACKET.md exists"

# Test 6: MAP_37D_HUMAN_INSTALL_STEPS.md exists
Assert-True (Test-Path $installDocPath) "MAP_37D_HUMAN_INSTALL_STEPS.md exists"

# Test 7: MAP_37D_SERVER_WIRING.md exists
Assert-True (Test-Path $wiringDocPath) "MAP_37D_SERVER_WIRING.md exists"

# Test 8: MAP_37D_LOG_CAPTURE_COMMANDS.md exists
Assert-True (Test-Path $logDocPath) "MAP_37D_LOG_CAPTURE_COMMANDS.md exists"

# Test 9: MAP_37D_SUCCESS_FAILURE_CRITERIA.md exists
Assert-True (Test-Path $criteriaDocPath) "MAP_37D_SUCCESS_FAILURE_CRITERIA.md exists"

# Test 10: MAP_37D_RUNTIME_RESULT_RECORD.md exists
Assert-True (Test-Path $resultDocPath) "MAP_37D_RUNTIME_RESULT_RECORD.md exists"

# Read and parse preflight JSON
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 11: Schema field correct
Assert-True ($p.schema -eq 'pzmapforge.map37d-human-runtime-load-test-packet.v0.1') `
    "schema == 'pzmapforge.map37d-human-runtime-load-test-packet.v0.1'"

# Test 12: PLAYABLE_EXPORT_CLAIM_ALLOWED = false
Assert-True ($p.PLAYABLE_EXPORT_CLAIM_ALLOWED -eq $false) "PLAYABLE_EXPORT_CLAIM_ALLOWED == false"

# Test 13: HUMAN_ONLY_INSTALL_REQUIRED = true
Assert-True ($p.HUMAN_ONLY_INSTALL_REQUIRED -eq $true) "HUMAN_ONLY_INSTALL_REQUIRED == true"

# Test 14: CLAUDE_RAN_PZ = false
Assert-True ($p.CLAUDE_RAN_PZ -eq $false) "CLAUDE_RAN_PZ == false"

# Test 15: CLAUDE_WROTE_STEAM = false
Assert-True ($p.CLAUDE_WROTE_STEAM -eq $false) "CLAUDE_WROTE_STEAM == false"

# Test 16: CLAUDE_WROTE_WORKSHOP = false
Assert-True ($p.CLAUDE_WROTE_WORKSHOP -eq $false) "CLAUDE_WROTE_WORKSHOP == false"

# Test 17: map37c_binary_evidence_referenced = true
Assert-True ($p.map37c_binary_evidence_referenced -eq $true) "map37c_binary_evidence_referenced == true"

# Read doc content for sentinel checks
$installContent  = Get-Content $installDocPath  -Raw
$wiringContent   = Get-Content $wiringDocPath   -Raw
$criteriaContent = Get-Content $criteriaDocPath -Raw

# Test 18: Install steps doc contains "HUMAN-ONLY"
Assert-True ($installContent -match 'HUMAN-ONLY') "Install steps doc contains 'HUMAN-ONLY'"

# Test 19: Install steps doc contains "chunkdata_35_27.bin"
Assert-True ($installContent -match 'chunkdata_35_27\.bin') "Install steps doc contains 'chunkdata_35_27.bin'"

# Test 20: Server wiring doc contains "Mods=pzmapforge_map37c"
Assert-True ($wiringContent -match 'Mods=pzmapforge_map37c') "Server wiring doc contains 'Mods=pzmapforge_map37c'"

# Test 21: Server wiring doc contains "Map=pzmapforge_map37c;Muldraugh, KY"
Assert-True ($wiringContent -match [regex]::Escape('Map=pzmapforge_map37c;Muldraugh, KY')) `
    "Server wiring doc contains 'Map=pzmapforge_map37c;Muldraugh, KY'"

# Test 22: Server wiring doc contains spawn PZ world X coordinate "10650"
Assert-True ($wiringContent -match '10650') "Server wiring doc contains PZ world X '10650'"

# Test 23: Criteria doc contains MOD_NOT_LOADED
Assert-True ($criteriaContent -match 'MOD_NOT_LOADED') "Criteria doc contains 'MOD_NOT_LOADED'"

# Test 24: Criteria doc contains SPAWN_METADATA_FAIL
Assert-True ($criteriaContent -match 'SPAWN_METADATA_FAIL') "Criteria doc contains 'SPAWN_METADATA_FAIL'"

# Test 25: Criteria doc contains MAP_FOLDER_REGISTRATION_FAIL
Assert-True ($criteriaContent -match 'MAP_FOLDER_REGISTRATION_FAIL') `
    "Criteria doc contains 'MAP_FOLDER_REGISTRATION_FAIL'"

# Test 26: Criteria doc contains CHUNKDATA_PARSE_FAIL
Assert-True ($criteriaContent -match 'CHUNKDATA_PARSE_FAIL') "Criteria doc contains 'CHUNKDATA_PARSE_FAIL'"

# Test 27: Criteria doc contains MULDRAUGH_FALLBACK
Assert-True ($criteriaContent -match 'MULDRAUGH_FALLBACK') "Criteria doc contains 'MULDRAUGH_FALLBACK'"

# Test 28: Criteria doc contains EMPTY_WORLD
Assert-True ($criteriaContent -match 'EMPTY_WORLD') "Criteria doc contains 'EMPTY_WORLD'"

# Test 29: Criteria doc contains TERRAIN_MOUNT_SUCCESS
Assert-True ($criteriaContent -match 'TERRAIN_MOUNT_SUCCESS') "Criteria doc contains 'TERRAIN_MOUNT_SUCCESS'"

# Cleanup
Remove-Item -Recurse -Force $testOutputRoot -ErrorAction SilentlyContinue

Write-Output ""
Write-Output "MAP-37D tests: $assertCount assertions, $failCount failures"

if ($failCount -gt 0) {
    Write-Error "MAP-37D tests FAILED: $failCount of $assertCount assertions failed."
    exit 1
}

Write-Output "MAP-37D tests PASSED."
exit 0
