#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7E: Analyzes a Build 42 candidate load test log and classifies the result.

    Reads the provided log file. Does NOT read PZ install assets.
    Does NOT write outside .local/.

    Writes:
      <Output>/map7d-load-result.json
      <Output>/map7d-load-result.md

.PARAMETER LogPath
    Path to the console.txt log file to analyze.

.PARAMETER Output
    Path under .local/ for report output.

.PARAMETER ExpectedMapId
    Optional. When provided, the analyzer checks whether this map ID appears in
    the map folder scan list. Used with -VariantLabel to produce variant-specific
    classification labels.

.PARAMETER VariantLabel
    Optional. When provided together with -ExpectedMapId and an empty map folder
    scan, the classification becomes MAP7F_<VARIANT_KEY>_MAP_FOLDER_SCAN_EMPTY.
    Example: -VariantLabel VariantA -> MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-map7d-load-result.ps1 `
        -LogPath .\.local\map7f-logs\DebugLog-variant-A.txt `
        -Output .\.local\map7f-analysis\variant-A `
        -ExpectedMapId pzmapforge_build42_candidate_v4_001 `
        -VariantLabel VariantA
#>

param(
    [Parameter(Mandatory=$true)][string]$LogPath,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$ExpectedMapId = '',
    [string]$VariantLabel  = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $Output '-Output'

if (-not (Test-Path $LogPath)) {
    Write-Error "LogPath not found: $LogPath"
    exit 1
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

# ---------------------------------------------------------------------------
# Read log
# ---------------------------------------------------------------------------

$logContent = [System.IO.File]::ReadAllText($LogPath)
Write-Output "Analyzing: $LogPath"

# ---------------------------------------------------------------------------
# Extract booleans
# ---------------------------------------------------------------------------

# Candidate load detection
$candidateLoaded       = $logContent -match 'loading pzmapforge_build42_candidate'
$candidateMapIdLoaded  = ''
if ($logContent -match 'loading (pzmapforge_build42_candidate\S+)') { $candidateMapIdLoaded = $Matches[1] }

# Lua/BOM blockers
$lexstateTokenStr              = $logContent -match 'LexState\.token2str'
$candidateObjectsLuaError      = ($logContent -match 'objects\.lua') -and $lexstateTokenStr
$serverSpawnRegionsLuaError    = ($logContent -match 'spawnregions\.lua') -and $lexstateTokenStr

# Spawn region / player data
$spawnRegionNullError          = $logContent -match '(Cannot invoke KahluaTable\.iterator\(\) because orig is null|getSpawnRegionsAux)'
$timeoutWaitingPlayerData      = $logContent -match 'Timed out waiting for the server to send player data'
$playerDataReceived            = $logContent -match 'Player data received from the server'
$gameLoadingCompleted          = $logContent -match 'game loading took'
$enteredIngameState            = $logContent -match 'STATE: exit zombie\.gameStates\.GameLoadingState'
$exitedIngameState             = $logContent -match 'STATE: exit zombie\.gameStates\.IngameState'

# Map folder scan — handles bare and real PZ DebugLog formats.
#
# Real PZ DebugLog format (Build 42):
#   [date time.ms] LOG  : category      f:N st:N> Message text.
#   [date time.ms] WARN : category      f:N st:N at Class.method          > Message text.
#   (st:N may be absent on early startup lines; st:N,M,P possible in multiplayer)
#
# Bare format: Message text (no timestamp prefix)
# Both may have a trailing period on the map-folder messages.

function Strip-DebugLogPrefix {
    param([string]$Line)
    # Real PZ format: [date time] LOG/WARN : category      f:N [st:N] [at class.method] >
    $s = $Line -replace '^\[.*?\]\s+\w+\s*:\s+\w+\s+f:\d+[^>]*>\s*', ''
    return $s.TrimEnd('.')
}

$mapFoldersScanFound         = $false
$mapFoldersListEmpty         = $false
$mapFoldersListCount         = 0
$mapFolderLines              = [string[]]@()
$mapFolderParserStrategy     = 'not_found'
$timestampedDebuglogDetected = $false

$logLines  = $logContent -split '\r?\n'
$startIdx  = -1
$endIdx    = -1

for ($i = 0; $i -lt $logLines.Count; $i++) {
    $raw      = $logLines[$i]
    $semantic = Strip-DebugLogPrefix $raw
    if ($semantic -match 'Looking in these map folders') {
        $startIdx = $i
        if ($raw -match '^\[.*?\]\s+LOG') { $timestampedDebuglogDetected = $true }
    }
    if ($startIdx -ge 0 -and $semantic -match '^<End of map-folders list>') {
        $endIdx = $i
        break
    }
}

if ($startIdx -ge 0 -and $endIdx -ge 0) {
    $mapFoldersScanFound     = $true
    $mapFolderParserStrategy = if ($timestampedDebuglogDetected) { 'timestamped_debuglog' } else { 'bare_console' }
    $innerList = [System.Collections.Generic.List[string]]::new()
    for ($i = $startIdx + 1; $i -lt $endIdx; $i++) {
        $s = (Strip-DebugLogPrefix $logLines[$i]).Trim()
        if ($s.Length -gt 0) { $innerList.Add($s) }
    }
    $mapFolderLines      = [string[]]$innerList.ToArray()
    $mapFoldersListCount = $mapFolderLines.Count
    $mapFoldersListEmpty = ($mapFolderLines.Count -eq 0)
} elseif ($startIdx -ge 0) {
    $mapFoldersScanFound     = $true
    $mapFolderParserStrategy = if ($timestampedDebuglogDetected) { 'timestamped_debuglog_no_end' } else { 'bare_console_no_end' }
}

# Spawn building warning
$spawnBuildingWarning          = $logContent -match 'no room or building at'
$noRoomOrBuildingAtSpawn       = $spawnBuildingWarning

# Lotheader file discovery failure (future diagnostic: scan found path but no .lotheader files).
# MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING distinguishes from MAP7F_*_MAP_FOLDER_SCAN_EMPTY:
#   SCAN_EMPTY = IsoMetaGrid found zero map folder paths (our current blocker A-E).
#   LOTHEADER_MISSING = IsoMetaGrid found a map folder path but no .lotheader files in it.
# This is a later-stage failure, not our current result.
$lotheaderFilesMissing         = $logContent -match 'Failed to find any \.lotheader files'

# Miscellaneous
$mannequinWarning              = $logContent -match 'mannequin'

# ---------------------------------------------------------------------------
# MAP-7Q: Build 42 Workshop / runtime signals for DruMapBaseline
# ---------------------------------------------------------------------------

$workshopId3355966216Seen   = $logContent -match '3355966216'
$workshopDownloadSeen        = $logContent -match 'Workshop.*[Dd]ownload|[Dd]ownloading.*Workshop|[Dd]ownload.*3355966216'
$workshopInstalledSeen       = $logContent -match 'Workshop.*Installed|Installed.*3355966216'
$workshopReadySeen            = $logContent -match 'Workshop.*Ready|Ready.*3355966216'
$multiplayerReached           = $logContent -match 'Game Mode: Multiplayer'
$lotheaderMetaEvidenceFound   = $logContent -match '\.lotheader'
$lotheaderMetaPathsOrNames    = [string[]]@()
if ($lotheaderMetaEvidenceFound) {
    $lhMatches = [regex]::Matches($logContent, '[\w]+\.lotheader')
    $lhSet     = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($m in $lhMatches) { [void]$lhSet.Add($m.Value) }
    $lotheaderMetaPathsOrNames = [string[]]($lhSet | Sort-Object)
}

# Expected mod loaded -- when ExpectedMapId provided, detect "loading <ExpectedMapId>"
$expectedModLoaded = $false
if ($ExpectedMapId -ne '') {
    $expectedModLoaded = $logContent -match ('loading\s+' + [regex]::Escape($ExpectedMapId))
}

# Runtime success evidence composite (conservative multi-signal)
$runtimeSuccessEvidenceFound = (
    ($workshopInstalledSeen -or $workshopReadySeen) -and
    $expectedModLoaded -and
    $playerDataReceived -and
    $gameLoadingCompleted -and
    ($multiplayerReached -or $lotheaderMetaEvidenceFound)
)

# ---------------------------------------------------------------------------
# Classification
# ---------------------------------------------------------------------------

$classification = 'MAP7D_LOAD_TEST_INCONCLUSIVE'

# Compute variant key for classification label when -VariantLabel is supplied.
# Converts camelCase to UPPER_SNAKE: VariantA -> VARIANT_A, VariantB -> VARIANT_B.
$variantClassification = ''
if ($VariantLabel -ne '') {
    $variantKey            = ($VariantLabel -creplace '(?<=[a-zA-Z])([A-Z])', '_$1').ToUpper()
    $variantClassification = "MAP7F_${variantKey}_MAP_FOLDER_SCAN_EMPTY"
}

if ($timeoutWaitingPlayerData) {
    $classification = 'MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA'
} elseif ($lexstateTokenStr) {
    $classification = 'MAP7D_LOAD_TEST_FAIL_LUA_BOM_OR_LEXSTATE'
} elseif ($mapFoldersScanFound -and (-not $mapFoldersListEmpty) -and $lotheaderFilesMissing) {
    $classification = 'MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING'
} elseif ($VariantLabel -eq 'DruMapBaseline' -and $ExpectedMapId -ne '' -and
          $runtimeSuccessEvidenceFound) {
    $classification = 'MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS'
} elseif ($VariantLabel -eq 'DruMapBaseline' -and $ExpectedMapId -ne '' -and
          $mapFoldersScanFound -and (-not $mapFoldersListEmpty)) {
    $foundInList = @($mapFolderLines | Where-Object { $_ -match [regex]::Escape($ExpectedMapId) })
    if ($foundInList.Count -gt 0) {
        $classification = 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND'
    }
} elseif ($VariantLabel -eq 'DruMapBaseline' -and $ExpectedMapId -ne '' -and
          $mapFoldersScanFound -and $mapFoldersListEmpty) {
    $classification = 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY'
} elseif ($ExpectedMapId -ne '' -and $VariantLabel -ne '' -and $mapFoldersScanFound -and $mapFoldersListEmpty) {
    $classification = $variantClassification
} elseif ($candidateLoaded -and $playerDataReceived -and $gameLoadingCompleted -and
          $enteredIngameState -and (-not $timeoutWaitingPlayerData) -and
          ($mapFoldersListEmpty -or $spawnBuildingWarning)) {
    $classification = 'MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD'
} elseif ($candidateLoaded -and $playerDataReceived -and $gameLoadingCompleted -and $enteredIngameState) {
    $classification = 'MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME'
}

Write-Output "Classification: $classification"

$statusLabels = [string[]]@(
    $classification,
    "candidate_loaded=$($candidateLoaded.ToString().ToLower())",
    "player_data_received=$($playerDataReceived.ToString().ToLower())",
    "game_loading_completed=$($gameLoadingCompleted.ToString().ToLower())",
    "entered_ingame_state=$($enteredIngameState.ToString().ToLower())",
    "map_folders_list_empty=$($mapFoldersListEmpty.ToString().ToLower())",
    "spawn_building_warning=$($spawnBuildingWarning.ToString().ToLower())",
    'public_playable_claim_allowed=false',
    'LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT'
)

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$emptyClientScanDecisive = -not ($VariantLabel -eq 'DruMapBaseline' -and $runtimeSuccessEvidenceFound)

$report = [ordered]@{
    schema                             = 'pzmapforge.build42-map7d-load-result.v0.4'
    log_path                           = $LogPath
    classification                     = $classification
    candidate_loaded                   = $candidateLoaded
    candidate_map_id_loaded            = $candidateMapIdLoaded
    lexstate_token2str_found           = $lexstateTokenStr
    candidate_objects_lua_error_found  = $candidateObjectsLuaError
    server_spawnregions_lua_error_found = $serverSpawnRegionsLuaError
    spawn_region_null_error_found      = $spawnRegionNullError
    timeout_waiting_player_data        = $timeoutWaitingPlayerData
    player_data_received               = $playerDataReceived
    game_loading_completed             = $gameLoadingCompleted
    entered_ingame_state               = $enteredIngameState
    exited_ingame_state                = $exitedIngameState
    map_folders_scan_found             = $mapFoldersScanFound
    map_folders_list_empty             = $mapFoldersListEmpty
    map_folders_list_count             = $mapFoldersListCount
    map_folder_lines                   = $mapFolderLines
    map_folder_parser_strategy         = $mapFolderParserStrategy
    timestamped_debuglog_detected      = $timestampedDebuglogDetected
    spawn_building_warning_found       = $spawnBuildingWarning
    no_room_or_building_at_spawn_found = $noRoomOrBuildingAtSpawn
    lotheader_files_missing            = $lotheaderFilesMissing
    mannequin_warning_found            = $mannequinWarning
    expected_map_id                    = $ExpectedMapId
    variant_label                      = $VariantLabel
    variant_classification             = $variantClassification
    expected_mod_loaded                = $expectedModLoaded
    workshop_id_3355966216_seen        = $workshopId3355966216Seen
    workshop_download_seen             = $workshopDownloadSeen
    workshop_installed_seen            = $workshopInstalledSeen
    workshop_ready_seen                = $workshopReadySeen
    multiplayer_reached                = $multiplayerReached
    lotheader_meta_evidence_found      = $lotheaderMetaEvidenceFound
    lotheader_meta_paths_or_names      = $lotheaderMetaPathsOrNames
    runtime_success_evidence_found     = $runtimeSuccessEvidenceFound
    empty_client_map_folder_scan_decisive = $emptyClientScanDecisive
    visual_confirmation_required       = ($VariantLabel -eq 'DruMapBaseline')
    status_labels                      = $statusLabels
    public_playable_claim_allowed      = $false
}

$jsonPath = Join-Path $Output 'map7d-load-result.json'
$mdPath   = Join-Path $Output 'map7d-load-result.md'

$report | ConvertTo-Json -Depth 4 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence = '```'
$md = @"
# MAP-7E Build 42 Candidate Load Result Analysis

${fence}text
$classification
candidate_loaded=$($candidateLoaded.ToString().ToLower())
player_data_received=$($playerDataReceived.ToString().ToLower())
game_loading_completed=$($gameLoadingCompleted.ToString().ToLower())
entered_ingame_state=$($enteredIngameState.ToString().ToLower())
map_folders_list_empty=$($mapFoldersListEmpty.ToString().ToLower())
spawn_building_warning=$($spawnBuildingWarning.ToString().ToLower())
public_playable_claim_allowed=false
${fence}

## Detection summary

| Field | Value |
|---|---|
| classification | $classification |
| candidate_loaded | $candidateLoaded |
| lexstate_token2str_found | $lexstateTokenStr |
| candidate_objects_lua_error | $candidateObjectsLuaError |
| server_spawnregions_error | $serverSpawnRegionsLuaError |
| spawn_region_null_error | $spawnRegionNullError |
| timeout_waiting_player_data | $timeoutWaitingPlayerData |
| player_data_received | $playerDataReceived |
| game_loading_completed | $gameLoadingCompleted |
| entered_ingame_state | $enteredIngameState |
| map_folders_list_empty | $mapFoldersListEmpty |
| map_folders_list_count | $mapFoldersListCount |
| spawn_building_warning | $spawnBuildingWarning |
| mannequin_warning | $mannequinWarning |

## Non-claims

- This analysis is research only. No load test was performed by this script.
- public_playable_claim_allowed=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD:   $mdPath"
Write-Output ""
Write-Output "classification:              $classification"
Write-Output "candidate_loaded:            $candidateLoaded"
Write-Output "player_data_received:        $playerDataReceived"
Write-Output "game_loading_completed:      $gameLoadingCompleted"
Write-Output "map_folders_list_empty:      $mapFoldersListEmpty"
Write-Output "spawn_building_warning:      $spawnBuildingWarning"
Write-Output "public_playable_claim_allowed=false"
Write-Output "Done."
