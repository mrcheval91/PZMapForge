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

$prepareScript  = Join-Path $scriptDir 'prepare-build42-map38b-differential-control-test-packet.ps1'
$testOutputRoot = Join-Path $repoRoot '.local\deadmtl-authoring\map38b-differential-control-test-packet-test'

$jsonPath        = Join-Path $testOutputRoot 'map38b-differential-control-test-packet.json'
$mdPath          = Join-Path $testOutputRoot 'map38b-differential-control-test-packet.md'
$packetDocPath   = Join-Path $testOutputRoot 'MAP_38B_DIFFERENTIAL_CONTROL_TEST.md'
$installDocPath  = Join-Path $testOutputRoot 'MAP_38B_HUMAN_INSTALL_STEPS.md'
$wiringDocPath   = Join-Path $testOutputRoot 'MAP_38B_SERVER_WIRING.md'
$logDocPath      = Join-Path $testOutputRoot 'MAP_38B_LOG_CAPTURE_COMMANDS.md'
$criteriaDocPath = Join-Path $testOutputRoot 'MAP_38B_SUCCESS_FAILURE_CRITERIA.md'
$resultDocPath   = Join-Path $testOutputRoot 'MAP_38B_RUNTIME_RESULT_RECORD.md'

$runAMapFolder = Join-Path $testOutputRoot 'run-a-files-present\pzmapforge_map38b_build42_candidate\common\media\maps\pzmapforge_map38b'
$runBMapFolder = Join-Path $testOutputRoot 'run-b-files-removed\pzmapforge_map38b_build42_candidate\common\media\maps\pzmapforge_map38b'

Write-Output ""
Write-Output "--- MAP-38B differential control test packet tests ---"

# Test 1: Guard exits nonzero for path outside .local
try {
    & powershell -ExecutionPolicy Bypass -File $prepareScript -OutputRoot 'C:\forbidden_test_path_map38b' *>$null
} catch { }
Assert-True ($LASTEXITCODE -ne 0) "Guard: exits nonzero for path outside .local"

# Test 2: Prepare script exits 0 with valid .local path
Remove-Item -Recurse -Force $testOutputRoot -ErrorAction SilentlyContinue
& powershell -ExecutionPolicy Bypass -File $prepareScript -OutputRoot $testOutputRoot
Assert-True ($LASTEXITCODE -eq 0) "Prepare script exits 0 with valid .local path"

# Test 3-10: 8 docs exist
Assert-True (Test-Path $jsonPath)        "Preflight JSON exists"
Assert-True (Test-Path $mdPath)          "Preflight MD exists"
Assert-True (Test-Path $packetDocPath)   "MAP_38B_DIFFERENTIAL_CONTROL_TEST.md exists"
Assert-True (Test-Path $installDocPath)  "MAP_38B_HUMAN_INSTALL_STEPS.md exists"
Assert-True (Test-Path $wiringDocPath)   "MAP_38B_SERVER_WIRING.md exists"
Assert-True (Test-Path $logDocPath)      "MAP_38B_LOG_CAPTURE_COMMANDS.md exists"
Assert-True (Test-Path $criteriaDocPath) "MAP_38B_SUCCESS_FAILURE_CRITERIA.md exists"
Assert-True (Test-Path $resultDocPath)   "MAP_38B_RUNTIME_RESULT_RECORD.md exists"

# Read and parse preflight JSON
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 11: Schema field correct
Assert-True ($p.schema -eq 'pzmapforge.map38b-differential-control-test-packet.v0.1') `
    "schema == 'pzmapforge.map38b-differential-control-test-packet.v0.1'"

# Test 12-17: claim boundary fields
Assert-True ($p.PLAYABLE_EXPORT_CLAIM_ALLOWED -eq $false) "PLAYABLE_EXPORT_CLAIM_ALLOWED == false"
Assert-True ($p.HUMAN_ONLY_INSTALL_REQUIRED -eq $true)    "HUMAN_ONLY_INSTALL_REQUIRED == true"
Assert-True ($p.CLAUDE_RAN_PZ -eq $false)                 "CLAUDE_RAN_PZ == false"
Assert-True ($p.CLAUDE_WROTE_STEAM -eq $false)             "CLAUDE_WROTE_STEAM == false"
Assert-True ($p.CLAUDE_WROTE_WORKSHOP -eq $false)          "CLAUDE_WROTE_WORKSHOP == false"
Assert-True ($p.staged_output_local_only -eq $true)        "staged_output_local_only == true"

# Test 18: differential_control_test flag
Assert-True ($p.differential_control_test -eq $true) "differential_control_test == true"

# Test 19: muldraugh_chain_present == false (server ini Map= line, distinct from map.info lots=)
Assert-True ($p.muldraugh_chain_present -eq $false) "muldraugh_chain_present == false"

# Test 20: map_info_lots_value == Muldraugh, KY (MAP-38A fix)
Assert-True ($p.map_info_lots_value -eq 'Muldraugh, KY') "map_info_lots_value == 'Muldraugh, KY'"

# Test 21: map_folder_structure_fixed / map_token_matches_staged_folder
Assert-True ($p.map_folder_structure_fixed -eq $true) "map_folder_structure_fixed == true"
Assert-True ($p.map_token_matches_staged_folder -eq $true) "map_token_matches_staged_folder == true"

# Test 22: expected_spawn_point_ini
Assert-True ($p.expected_spawn_point_ini -eq 'SpawnPoint=10746,8288,0') `
    "expected_spawn_point_ini == 'SpawnPoint=10746,8288,0'"

# Test 23: expected_map_line has no Muldraugh suffix
Assert-True ($p.expected_map_line -eq 'Map=pzmapforge_map38b') "expected_map_line == 'Map=pzmapforge_map38b'"
Assert-True ($p.expected_map_line -notmatch 'Muldraugh') "expected_map_line does not contain 'Muldraugh'"

# Test 25: FALLBACK_INDISTINGUISHABLE_defined
Assert-True ($p.FALLBACK_INDISTINGUISHABLE_defined -eq $true) "FALLBACK_INDISTINGUISHABLE_defined == true"

# Test 26: RENDERABLE_MOUNT_SUCCESS_narrowed
Assert-True ($p.RENDERABLE_MOUNT_SUCCESS_narrowed -eq $true) "RENDERABLE_MOUNT_SUCCESS_narrowed == true"

# Test 27: outcomes_defined == 8
Assert-True ($p.outcomes_defined -eq 8) "outcomes_defined == 8"

# Test 28: distinctive marker tile fields
Assert-True ($p.distinctive_marker_tile -eq 'unofficial_fork_map_0') "distinctive_marker_tile == 'unofficial_fork_map_0'"
Assert-True ($p.distinctive_marker_tile_index -eq 4) "distinctive_marker_tile_index == 4"

# Test 29-30: Run A binary evidence present
Assert-True ($p.run_a_chunkdata_sha256 -match '^[0-9a-f]{64}$') "run_a_chunkdata_sha256 is 64-char hex"
Assert-True ($p.run_a_lotheader_sha256 -match '^[0-9a-f]{64}$') "run_a_lotheader_sha256 is 64-char hex"

# Test 31-33: Run B absence flags
Assert-True ($p.run_b_lotheader_present -eq $false) "run_b_lotheader_present == false"
Assert-True ($p.run_b_lotpack_present -eq $false)   "run_b_lotpack_present == false"
Assert-True ($p.run_b_chunkdata_present -eq $false) "run_b_chunkdata_present == false"

# Test 34-36: Run A files physically present on disk, under common/ (MAP-38A folder fix)
Assert-True (Test-Path (Join-Path $runAMapFolder 'chunkdata_35_27.bin'))   "Run A chunkdata_35_27.bin present on disk (common/)"
Assert-True (Test-Path (Join-Path $runAMapFolder '35_27.lotheader'))      "Run A 35_27.lotheader present on disk (common/)"
Assert-True (Test-Path (Join-Path $runAMapFolder 'world_35_27.lotpack'))  "Run A world_35_27.lotpack present on disk (common/)"

# Test 37-39: Run B files physically absent on disk
Assert-True (-not (Test-Path (Join-Path $runBMapFolder 'chunkdata_35_27.bin')))  "Run B chunkdata_35_27.bin absent on disk"
Assert-True (-not (Test-Path (Join-Path $runBMapFolder '35_27.lotheader')))     "Run B 35_27.lotheader absent on disk"
Assert-True (-not (Test-Path (Join-Path $runBMapFolder 'world_35_27.lotpack'))) "Run B world_35_27.lotpack absent on disk"

# Read doc content for sentinel checks
$installContent  = Get-Content $installDocPath  -Raw
$wiringContent   = Get-Content $wiringDocPath   -Raw
$criteriaContent = Get-Content $criteriaDocPath -Raw
$resultContent   = Get-Content $resultDocPath   -Raw
$packetContent   = Get-Content $packetDocPath   -Raw

# Test 40: Install steps doc contains "HUMAN-ONLY"
Assert-True ($installContent -match 'HUMAN-ONLY') "Install steps doc contains 'HUMAN-ONLY'"

# Test 41-42: Install steps doc separates Run A and Run B
Assert-True ($installContent -match 'Run A -- files-present')  "Install steps doc contains 'Run A -- files-present'"
Assert-True ($installContent -match 'Run B -- files-removed')  "Install steps doc contains 'Run B -- files-removed'"

# Test 43: Server wiring doc contains single-token Map= line
Assert-True ($wiringContent -match [regex]::Escape('Map=pzmapforge_map38b')) `
    "Server wiring doc contains 'Map=pzmapforge_map38b'"

# Test 44: Server wiring doc does NOT contain a Muldraugh-chained Map= line
Assert-True ($wiringContent -notmatch [regex]::Escape('Map=pzmapforge_map38b;Muldraugh')) `
    "Server wiring doc does not contain 'Map=pzmapforge_map38b;Muldraugh'"
Assert-True ($wiringContent -match [regex]::Escape('lots=Muldraugh, KY')) "Server wiring doc contains 'lots=Muldraugh, KY' (MAP-38A fix)"

# Test 46: Server wiring doc contains direct SpawnPoint= line
Assert-True ($wiringContent -match [regex]::Escape('SpawnPoint=10746,8288,0')) `
    "Server wiring doc contains 'SpawnPoint=10746,8288,0'"

# Test 47: Criteria doc contains all 8 outcome sentinels
Assert-True ($criteriaContent -match 'MOD_NOT_LOADED') "Criteria doc contains 'MOD_NOT_LOADED'"
Assert-True ($criteriaContent -match 'SPAWN_METADATA_FAIL') "Criteria doc contains 'SPAWN_METADATA_FAIL'"
Assert-True ($criteriaContent -match 'MAP_FOLDER_REGISTRATION_FAIL') "Criteria doc contains 'MAP_FOLDER_REGISTRATION_FAIL'"
Assert-True ($criteriaContent -match 'CHUNKDATA_PARSE_FAIL') "Criteria doc contains 'CHUNKDATA_PARSE_FAIL'"
Assert-True ($criteriaContent -match 'MULDRAUGH_FALLBACK') "Criteria doc contains 'MULDRAUGH_FALLBACK'"
Assert-True ($criteriaContent -match 'EMPTY_WORLD') "Criteria doc contains 'EMPTY_WORLD'"
Assert-True ($criteriaContent -match 'RENDERABLE_MOUNT_SUCCESS') "Criteria doc contains 'RENDERABLE_MOUNT_SUCCESS'"
Assert-True ($criteriaContent -match 'FALLBACK_INDISTINGUISHABLE') "Criteria doc contains 'FALLBACK_INDISTINGUISHABLE'"

# Test 55: RENDERABLE_MOUNT_SUCCESS requires the specific grid-aligned signal, not just any difference
Assert-True ($criteriaContent -match 'not merely "different-looking terrain') `
    "Criteria doc narrows RENDERABLE_MOUNT_SUCCESS to the specific grid-aligned signal"

# Test 56: FALLBACK_INDISTINGUISHABLE documented as the reproduction-failure outcome
Assert-True ($criteriaContent -match 'does not reproduce') `
    "Criteria doc documents FALLBACK_INDISTINGUISHABLE as a reproduction failure"

# Test 57: Result record contains both outcome checkboxes
Assert-True ($resultContent -match 'FALLBACK_INDISTINGUISHABLE') "Result record contains 'FALLBACK_INDISTINGUISHABLE' checkbox"
Assert-True ($resultContent -match 'RENDERABLE_MOUNT_SUCCESS') "Result record contains 'RENDERABLE_MOUNT_SUCCESS' checkbox"

# Test 58: Result record requires no playable claim
Assert-True ($resultContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') `
    "Result record contains 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false'"

# Test 59-60: Packet overview doc references MAP-38A and MAP-37E lineage
Assert-True ($packetContent -match 'MAP-38A') "Packet doc references MAP-38A"
Assert-True ($packetContent -match 'MAP-37E') "Packet doc references MAP-37E"

# Test 61: No claim of proven authored terrain rendering anywhere in the packet
$allDocsText = ($jsonPath, $mdPath, $packetDocPath, $installDocPath, $wiringDocPath, $logDocPath, $criteriaDocPath, $resultDocPath | `
    ForEach-Object { Get-Content $_ -Raw }) -join "`n"
Assert-True ($allDocsText -notmatch 'playable_terrain_mount_proven\s*[:=]\s*true') `
    "No doc claims playable_terrain_mount_proven=true"
Assert-True ($allDocsText -notmatch 'PLAYABLE_EXPORT_CLAIM_ALLOWED\s*[:=]\s*true') `
    "No doc claims PLAYABLE_EXPORT_CLAIM_ALLOWED=true"

# Cleanup
Remove-Item -Recurse -Force $testOutputRoot -ErrorAction SilentlyContinue

Write-Output ""
Write-Output "MAP-38B tests: $assertCount assertions, $failCount failures"

if ($failCount -gt 0) {
    Write-Error "MAP-38B tests FAILED: $failCount of $assertCount assertions failed."
    exit 1
}

Write-Output "MAP-38B tests PASSED."
exit 0
