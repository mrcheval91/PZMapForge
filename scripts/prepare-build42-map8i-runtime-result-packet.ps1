#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-8I: Records the dual spawnpoint keys runtime result.

    MAP-8I manually patched both spawnpoints.lua files in the Workshop source
    to add both keys (unemployed + Profession_Unemployed). The patch removed
    the MAP-8H profession-key error. Player spawned at 10746,8288,0 which
    matches the intended worldX=35, worldY=27 spawnpoint exactly.

    IsoMetaGrid still did not mount the PZMapForge parent folder.
    Visual result was vanilla/fallback terrain.
    Classification: MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED

    Does NOT run Project Zomboid.
    Does NOT upload to Steam Workshop.
    Does NOT write to Steam/PZ folders.
    Does NOT copy third-party files.
    Does NOT change binary writer behavior.
    All output under .local/ only.

.PARAMETER Output
    Required. Path under .local/.
#>

param(
    [Parameter(Mandatory=$true)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $Output '-Output'

New-Item -ItemType Directory -Force -Path $Output | Out-Null

Write-Output "MAP-8I: Dual Spawnpoint Keys Runtime Result Packet"
Write-Output "Output: $Output"
Write-Output ""

# ---------------------------------------------------------------------------
# Result packet JSON
# ---------------------------------------------------------------------------

$resultJsonPath = Join-Path $Output 'map8i-result.json'
$result = [ordered]@{
    schema                                               = 'pzmapforge.map8i-result.v0.1'
    classification                                       = 'MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED'
    source_basis                                         = 'MAP-8H_parent_child_contract_patch'
    workshop_id                                          = '3740642200'
    workshop_ready                                       = $true
    server_map_line_correct                              = $true
    parent_child_layout_downloaded                       = $true
    dual_spawnpoint_keys_present                         = $true
    spawnpoint_profession_error_removed                  = $true
    player_spawn_coordinate                              = '10746,8288,0'
    player_disconnect_coordinate                         = '10773,8288,0'
    spawn_coordinate_matches_35_27                       = $true
    spawn_worldX                                         = 35
    spawn_worldY                                         = 27
    spawn_posX                                           = 246
    spawn_posY                                           = 188
    spawn_math_check                                     = '35*300+246=10746 27*300+188=8288'
    iso_meta_grid_map_folder_list_empty                  = $true
    spawned_in_fallback_or_unconfirmed_generated_content = $true
    isopropertytype_missing_property_noise               = $true
    isopropertytype_errors_linked_to_cell_blocker        = $false
    next_branch                                          = 'parent_metadata_or_binary_cell_mount_contract'
    playable_claim_allowed                               = $false
    binary_writer_gate_closed                            = $true
    no_pz_run_by_claude                                  = $true
    no_workshop_upload_by_claude                         = $true
    no_third_party_files_copied                          = $true
}
$result | ConvertTo-Json -Depth 3 | Set-Content -Path $resultJsonPath -Encoding ASCII
Write-Output "Wrote: map8i-result.json"

# ---------------------------------------------------------------------------
# Result packet MD
# ---------------------------------------------------------------------------

$resultMdPath = Join-Path $Output 'map8i-result.md'
Set-Content -Path $resultMdPath -Value @"
# MAP-8I Result Packet

``````text
MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
classification:                                    MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED
workshop_ready:                                    true
server_map_line_correct:                           true
parent_child_layout_downloaded:                    true
dual_spawnpoint_keys_present:                      true
spawnpoint_profession_error_removed:               true
player_spawn_coordinate:                           10746,8288,0
player_disconnect_coordinate:                      10773,8288,0
spawn_coordinate_matches_35_27:                    true
spawn_math:                                        35*300+246=10746  27*300+188=8288
iso_meta_grid_map_folder_list_empty:               true
spawned_in_fallback_or_unconfirmed_content:        true
isopropertytype_errors_linked_to_cell_blocker:     false
next_branch:                                       parent_metadata_or_binary_cell_mount_contract
playable_claim_allowed:                            false
binary_writer_gate_closed:                         true
``````
"@ -Encoding ASCII
Write-Output "Wrote: map8i-result.md"

# ---------------------------------------------------------------------------
# Packet doc
# ---------------------------------------------------------------------------

$packetPath = Join-Path $Output 'MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT_PACKET.md'
Set-Content -Path $packetPath -Value @"
# MAP-8I: Dual Spawnpoint Keys Runtime Result Packet

``````text
MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED
DUAL_SPAWNPOINT_KEYS_PRESENT=true
SPAWNPOINT_PROFESSION_ERROR_REMOVED=true
PLAYER_SPAWN_COORDINATE=10746,8288,0
SPAWN_COORDINATE_MATCHES_35_27=true
ISO_META_GRID_MAP_FOLDER_LIST_EMPTY=true
SPAWNED_IN_FALLBACK_OR_UNCONFIRMED_GENERATED_CONTENT=true
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
``````

## Patch summary

Both spawnpoints.lua files in Workshop source patched to add:
  unemployed + Profession_Unemployed keys at worldX=35, worldY=27

Files patched:
  common\media\maps\PZMapForge\spawnpoints.lua
  common\media\maps\pzmapforge_build42_candidate_v4_001\spawnpoints.lua

## Runtime

  Server Map: pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
  City selected: pzmapforge_build42_candidate_v4_001
  Player connected:    10746,8288,0
  Player disconnected: 10773,8288,0

## Coordinate proof

  35 * 300 + 246 = 10746
  27 * 300 + 188 = 8288
  Spawn = worldX=35, worldY=27, posX=246, posY=188

## Resolved

  Workshop, mod loading, Map line, city selector, spawnpoint profession error.

## Remaining blocker

  IsoMetaGrid map folder list empty. Fallback/vanilla terrain.
  Classification: MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED
  next_branch=parent_metadata_or_binary_cell_mount_contract

## Packet files

  MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT_PACKET.md  (this file)
  map8i-result.json
  map8i-result.md
"@ -Encoding ASCII
Write-Output "Wrote: MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT_PACKET.md"

Write-Output ""
Write-Output "MAP-8I packet complete."
Write-Output "classification: MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED"
Write-Output "spawnpoint_profession_error_removed: true"
Write-Output "spawn_coordinate_matches_35_27: true"
Write-Output "iso_meta_grid_map_folder_list_empty: true"
Write-Output "binary_writer_gate_closed: true"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
