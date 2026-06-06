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

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-map7d-load-result.ps1 `
        -LogPath .\.local\map7d-logs\console-map7d-20260606.txt `
        -Output .\.local\map7d-analysis
#>

param(
    [Parameter(Mandatory=$true)][string]$LogPath,
    [Parameter(Mandatory=$true)][string]$Output
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

# Map folder scan
$mapFoldersListEmpty = $false
$mapFoldersListCount = 0
$mapFoldersScanFound = $logContent -match 'Looking in these map folders:'
if ($mapFoldersScanFound) {
    $mfMatch = [regex]::Match($logContent, 'Looking in these map folders:\r?\n(.*?)<End of map-folders list>',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($mfMatch.Success) {
        $folderSection = $mfMatch.Groups[1].Value.Trim()
        if ($folderSection.Length -eq 0) {
            $mapFoldersListEmpty = $true
            $mapFoldersListCount = 0
        } else {
            $folderLines = @($folderSection -split '\r?\n' | Where-Object { $_.Trim() -ne '' })
            $mapFoldersListCount = $folderLines.Count
            $mapFoldersListEmpty = $false
        }
    } elseif ($logContent -match "Looking in these map folders:\r?\n<End of map-folders list>") {
        $mapFoldersListEmpty = $true
        $mapFoldersListCount = 0
    }
}

# Spawn building warning
$spawnBuildingWarning          = $logContent -match 'no room or building at'
$noRoomOrBuildingAtSpawn       = $spawnBuildingWarning

# Miscellaneous
$mannequinWarning              = $logContent -match 'mannequin'

# ---------------------------------------------------------------------------
# Classification
# ---------------------------------------------------------------------------

$classification = 'MAP7D_LOAD_TEST_INCONCLUSIVE'

if ($timeoutWaitingPlayerData) {
    $classification = 'MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA'
} elseif ($lexstateTokenStr) {
    $classification = 'MAP7D_LOAD_TEST_FAIL_LUA_BOM_OR_LEXSTATE'
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

$report = [ordered]@{
    schema                             = 'pzmapforge.build42-map7d-load-result.v0.1'
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
    spawn_building_warning_found       = $spawnBuildingWarning
    no_room_or_building_at_spawn_found = $noRoomOrBuildingAtSpawn
    mannequin_warning_found            = $mannequinWarning
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
