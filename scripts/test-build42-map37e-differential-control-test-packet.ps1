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

$prepareScript  = Join-Path $scriptDir 'prepare-build42-map37e-differential-control-test-packet.ps1'
$testOutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map37e-differential-control-test-packet-test'

$jsonPath        = Join-Path $testOutputRoot 'map37e-differential-control-test-packet.json'
$mdPath          = Join-Path $testOutputRoot 'map37e-differential-control-test-packet.md'
$packetDocPath   = Join-Path $testOutputRoot 'MAP_37E_DIFFERENTIAL_CONTROL_TEST.md'
$installDocPath  = Join-Path $testOutputRoot 'MAP_37E_HUMAN_INSTALL_STEPS.md'
$wiringDocPath   = Join-Path $testOutputRoot 'MAP_37E_SERVER_WIRING.md'
$logDocPath      = Join-Path $testOutputRoot 'MAP_37E_LOG_CAPTURE_COMMANDS.md'
$criteriaDocPath = Join-Path $testOutputRoot 'MAP_37E_SUCCESS_FAILURE_CRITERIA.md'
$resultDocPath   = Join-Path $testOutputRoot 'MAP_37E_RUNTIME_RESULT_RECORD.md'

$runAMapFolder = Join-Path $testOutputRoot 'run-a-files-present\pzmapforge_map37c_build42_candidate\42\media\maps\pzmapforge_map37c'
$runBMapFolder = Join-Path $testOutputRoot 'run-b-files-removed\pzmapforge_map37c_build42_candidate\42\media\maps\pzmapforge_map37c'

Write-Output ""
Write-Output "--- MAP-37E differential control test packet tests ---"

# Test 1: Guard exits nonzero for path outside .local
try {
    & powershell -ExecutionPolicy Bypass -File $prepareScript -OutputRoot 'C:\forbidden_test_path_map37e' *>$null
} catch { }
Assert-True ($LASTEXITCODE -ne 0) "Guard: exits nonzero for path outside .local"

# Test 2: Prepare script exits 0 with valid .local path
Remove-Item -Recurse -Force $testOutputRoot -ErrorAction SilentlyContinue
& powershell -ExecutionPolicy Bypass -File $prepareScript -OutputRoot $testOutputRoot
Assert-True ($LASTEXITCODE -eq 0) "Prepare script exits 0 with valid .local path"

# Test 3-10: 8 docs exist
Assert-True (Test-Path $jsonPath)        "Preflight JSON exists"
Assert-True (Test-Path $mdPath)          "Preflight MD exists"
Assert-True (Test-Path $packetDocPath)   "MAP_37E_DIFFERENTIAL_CONTROL_TEST.md exists"
Assert-True (Test-Path $installDocPath)  "MAP_37E_HUMAN_INSTALL_STEPS.md exists"
Assert-True (Test-Path $wiringDocPath)   "MAP_37E_SERVER_WIRING.md exists"
Assert-True (Test-Path $logDocPath)      "MAP_37E_LOG_CAPTURE_COMMANDS.md exists"
Assert-True (Test-Path $criteriaDocPath) "MAP_37E_SUCCESS_FAILURE_CRITERIA.md exists"
Assert-True (Test-Path $resultDocPath)   "MAP_37E_RUNTIME_RESULT_RECORD.md exists"

# Read and parse preflight JSON
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 11: Schema field correct
Assert-True ($p.schema -eq 'pzmapforge.map37e-differential-control-test-packet.v0.1') `
    "schema == 'pzmapforge.map37e-differential-control-test-packet.v0.1'"

# Test 12-17: claim boundary fields
Assert-True ($p.PLAYABLE_EXPORT_CLAIM_ALLOWED -eq $false) "PLAYABLE_EXPORT_CLAIM_ALLOWED == false"
Assert-True ($p.HUMAN_ONLY_INSTALL_REQUIRED -eq $true)    "HUMAN_ONLY_INSTALL_REQUIRED == true"
Assert-True ($p.CLAUDE_RAN_PZ -eq $false)                 "CLAUDE_RAN_PZ == false"
Assert-True ($p.CLAUDE_WROTE_STEAM -eq $false)             "CLAUDE_WROTE_STEAM == false"
Assert-True ($p.CLAUDE_WROTE_WORKSHOP -eq $false)          "CLAUDE_WROTE_WORKSHOP == false"
Assert-True ($p.staged_output_local_only -eq $true)        "staged_output_local_only == true"

# Test 18: differential_control_test flag
Assert-True ($p.differential_control_test -eq $true) "differential_control_test == true"

# Test 19: muldraugh_chain_present == false
Assert-True ($p.muldraugh_chain_present -eq $false) "muldraugh_chain_present == false"

# Test 20: map_token_matches_staged_folder == true
Assert-True ($p.map_token_matches_staged_folder -eq $true) "map_token_matches_staged_folder == true"

# Test 21: expected_spawn_point_ini
Assert-True ($p.expected_spawn_point_ini -eq 'SpawnPoint=10746,8288,0') `
    "expected_spawn_point_ini == 'SpawnPoint=10746,8288,0'"

# Test 22: expected_map_line has no Muldraugh suffix
Assert-True ($p.expected_map_line -eq 'Map=pzmapforge_map37c') "expected_map_line == 'Map=pzmapforge_map37c'"
Assert-True ($p.expected_map_line -notmatch 'Muldraugh') "expected_map_line does not contain 'Muldraugh'"

# Test 24: FALLBACK_INDISTINGUISHABLE_defined
Assert-True ($p.FALLBACK_INDISTINGUISHABLE_defined -eq $true) "FALLBACK_INDISTINGUISHABLE_defined == true"

# Test 25: TERRAIN_MOUNT_SUCCESS_narrowed
Assert-True ($p.TERRAIN_MOUNT_SUCCESS_narrowed -eq $true) "TERRAIN_MOUNT_SUCCESS_narrowed == true"

# Test 26: outcomes_defined == 8
Assert-True ($p.outcomes_defined -eq 8) "outcomes_defined == 8"

# Test 27-28: Run A binary evidence present
Assert-True ($p.run_a_chunkdata_sha256 -match '^[0-9a-f]{64}$') "run_a_chunkdata_sha256 is 64-char hex"
Assert-True ($p.run_a_chunkdata_size -eq 1026) "run_a_chunkdata_size == 1026"

# Test 29-31: Run B absence flags
Assert-True ($p.run_b_lotheader_present -eq $false) "run_b_lotheader_present == false"
Assert-True ($p.run_b_lotpack_present -eq $false)   "run_b_lotpack_present == false"
Assert-True ($p.run_b_chunkdata_present -eq $false) "run_b_chunkdata_present == false"

# Test 32-34: Run A files physically present on disk
Assert-True (Test-Path (Join-Path $runAMapFolder 'chunkdata_35_27.bin'))   "Run A chunkdata_35_27.bin present on disk"
Assert-True (Test-Path (Join-Path $runAMapFolder '35_27.lotheader'))      "Run A 35_27.lotheader present on disk"
Assert-True (Test-Path (Join-Path $runAMapFolder 'world_35_27.lotpack'))  "Run A world_35_27.lotpack present on disk"

# Test 35-37: Run B files physically absent on disk
Assert-True (-not (Test-Path (Join-Path $runBMapFolder 'chunkdata_35_27.bin')))  "Run B chunkdata_35_27.bin absent on disk"
Assert-True (-not (Test-Path (Join-Path $runBMapFolder '35_27.lotheader')))     "Run B 35_27.lotheader absent on disk"
Assert-True (-not (Test-Path (Join-Path $runBMapFolder 'world_35_27.lotpack'))) "Run B world_35_27.lotpack absent on disk"

# Read doc content for sentinel checks
$installContent  = Get-Content $installDocPath  -Raw
$wiringContent   = Get-Content $wiringDocPath   -Raw
$criteriaContent = Get-Content $criteriaDocPath -Raw
$resultContent   = Get-Content $resultDocPath   -Raw
$packetContent   = Get-Content $packetDocPath   -Raw

# Test 38: Install steps doc contains "HUMAN-ONLY"
Assert-True ($installContent -match 'HUMAN-ONLY') "Install steps doc contains 'HUMAN-ONLY'"

# Test 39-40: Install steps doc separates Run A and Run B
Assert-True ($installContent -match 'Run A -- files-present')  "Install steps doc contains 'Run A -- files-present'"
Assert-True ($installContent -match 'Run B -- files-removed')  "Install steps doc contains 'Run B -- files-removed'"

# Test 41: Server wiring doc contains single-token Map= line
Assert-True ($wiringContent -match [regex]::Escape('Map=pzmapforge_map37c')) `
    "Server wiring doc contains 'Map=pzmapforge_map37c'"

# Test 42: Server wiring doc does NOT contain a Muldraugh-chained Map= line
Assert-True ($wiringContent -notmatch [regex]::Escape('Map=pzmapforge_map37c;Muldraugh')) `
    "Server wiring doc does not contain 'Map=pzmapforge_map37c;Muldraugh'"
Assert-True ($wiringContent -match 'NO ;Muldraugh, KY chain') "Server wiring doc explicitly states no Muldraugh chain"

# Test 44: Server wiring doc contains direct SpawnPoint= line
Assert-True ($wiringContent -match [regex]::Escape('SpawnPoint=10746,8288,0')) `
    "Server wiring doc contains 'SpawnPoint=10746,8288,0'"

# Test 45: Criteria doc contains all 8 outcome sentinels
Assert-True ($criteriaContent -match 'MOD_NOT_LOADED') "Criteria doc contains 'MOD_NOT_LOADED'"
Assert-True ($criteriaContent -match 'SPAWN_METADATA_FAIL') "Criteria doc contains 'SPAWN_METADATA_FAIL'"
Assert-True ($criteriaContent -match 'MAP_FOLDER_REGISTRATION_FAIL') "Criteria doc contains 'MAP_FOLDER_REGISTRATION_FAIL'"
Assert-True ($criteriaContent -match 'CHUNKDATA_PARSE_FAIL') "Criteria doc contains 'CHUNKDATA_PARSE_FAIL'"
Assert-True ($criteriaContent -match 'MULDRAUGH_FALLBACK') "Criteria doc contains 'MULDRAUGH_FALLBACK'"
Assert-True ($criteriaContent -match 'EMPTY_WORLD') "Criteria doc contains 'EMPTY_WORLD'"
Assert-True ($criteriaContent -match 'TERRAIN_MOUNT_SUCCESS') "Criteria doc contains 'TERRAIN_MOUNT_SUCCESS'"
Assert-True ($criteriaContent -match 'FALLBACK_INDISTINGUISHABLE') "Criteria doc contains 'FALLBACK_INDISTINGUISHABLE'"

# Test 53: TERRAIN_MOUNT_SUCCESS is explicitly narrowed (not reachable from spawn+visible alone)
Assert-True ($criteriaContent -match 'is NOT sufficient by itself') `
    "Criteria doc explicitly narrows TERRAIN_MOUNT_SUCCESS (spawn + visible terrain is NOT sufficient)"

# Test 54: FALLBACK_INDISTINGUISHABLE documented as expected/most-likely result
Assert-True ($criteriaContent -match 'expected result per MAP-9N/MAP-9Q') `
    "Criteria doc documents FALLBACK_INDISTINGUISHABLE as the expected result"

# Test 55: Result record contains both outcome checkboxes
Assert-True ($resultContent -match 'FALLBACK_INDISTINGUISHABLE') "Result record contains 'FALLBACK_INDISTINGUISHABLE' checkbox"
Assert-True ($resultContent -match 'TERRAIN_MOUNT_SUCCESS') "Result record contains 'TERRAIN_MOUNT_SUCCESS' checkbox"

# Test 57: Result record requires no playable claim
Assert-True ($resultContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') `
    "Result record contains 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'"

# Test 58: Packet overview doc references MAP-9N/MAP-9Q as the reason this test exists
Assert-True ($packetContent -match 'MAP-9N') "Packet doc references MAP-9N"
Assert-True ($packetContent -match 'MAP-9Q') "Packet doc references MAP-9Q"

# Test 60: No claim of proven authored terrain rendering anywhere in the packet
$allDocsText = ($jsonPath, $mdPath, $packetDocPath, $installDocPath, $wiringDocPath, $logDocPath, $criteriaDocPath, $resultDocPath | `
    ForEach-Object { Get-Content $_ -Raw }) -join "`n"
Assert-True ($allDocsText -notmatch 'playable_terrain_mount_proven\s*[:=]\s*true') `
    "No doc claims playable_terrain_mount_proven=true"

# Cleanup
Remove-Item -Recurse -Force $testOutputRoot -ErrorAction SilentlyContinue

Write-Output ""
Write-Output "MAP-37E tests: $assertCount assertions, $failCount failures"

if ($failCount -gt 0) {
    Write-Error "MAP-37E tests FAILED: $failCount of $assertCount assertions failed."
    exit 1
}

Write-Output "MAP-37E tests PASSED."
exit 0
