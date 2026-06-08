#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-8F: Writes the lots=self runtime result packet.

    Records the MAP-8F runtime observation:
    - lots=<MapId> in map.info causes city selector to list candidate
    - Player spawned in Muldraugh/fallback, not PZMapForge content
    - IsoMetaGrid map folder list still empty
    - WorldMapDataAssetManager failed to load worldmap XML files

    Does NOT run PZ. Does NOT upload to Workshop. Does NOT write to Steam/PZ folders.
    Does NOT copy third-party files. Does NOT change binary writer behavior.
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

Write-Output "MAP-8F: lots=self Runtime Result Packet"
Write-Output "Output: $Output"
Write-Output ""

$candidateMapId = 'pzmapforge_build42_candidate_v4_001'
$workshopId     = '3740642200'

# ---------------------------------------------------------------------------
# Result fields (all from MAP-8F manual runtime observation and log capture)
# ---------------------------------------------------------------------------

$resultJson = [ordered]@{
    schema                          = 'pzmapforge.map8f-result-packet.v0.1'
    candidate_map_id                = $candidateMapId
    workshop_id                     = $workshopId
    server_preset                   = 'PZMF_B42_MAP8F_LOTS_SELF_001'
    classification                  = 'MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED'
    map_info_change                 = 'lots=NONE -> lots=pzmapforge_build42_candidate_v4_001'
    workshop_ready                  = $true
    mod_loaded                      = $true
    lots_self                       = $true
    city_selector_visible           = $true
    invalid_bin_stubs_absent        = $true
    invalid_magic_error_removed     = $true
    worldmap_xml_failed_to_load     = $true
    worldmap_xml_failure_message    = 'WorldMapDataAssetManager failed to load worldmap-forest.xml and worldmap.xml'
    player_fully_connected          = $true
    player_spawn_coordinate         = '10851,9846,0'
    player_disconnect_coordinate    = '10856,9850,0'
    spawned_in_muldraugh_or_fallback = $true
    iso_meta_grid_map_folder_list_empty = $true
    binary_files_read               = $false
    playable_claim_allowed          = $false
    binary_writer_gate_closed       = $true
    no_binary_writer_changes        = $true
    no_third_party_files_copied     = $true
    no_pz_run_by_script             = $true
    no_automatic_workshop_upload    = $true
    next_branch                     = 'known_working_build42_map_contract_comparator'
}

$jsonPath = Join-Path $Output 'map8f-result-packet.json'
$resultJson | ConvertTo-Json -Depth 3 | Set-Content -Path $jsonPath -Encoding ASCII
Write-Output "Wrote: map8f-result-packet.json"

# ---------------------------------------------------------------------------
# Result markdown
# ---------------------------------------------------------------------------

$mdPath = Join-Path $Output 'map8f-result-packet.md'
Set-Content -Path $mdPath -Value @"
# MAP-8F: lots=self Runtime Result

``````text
MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED
MAP8F_CITY_SELECTOR_VISIBLE
MAP8F_ISO_META_GRID_MAP_FOLDER_LIST_EMPTY
MAP8F_WORLDMAP_XML_FAILED_TO_LOAD
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## map.info change

``````text
Before (MAP-8D): lots=NONE
After  (MAP-8F): lots=pzmapforge_build42_candidate_v4_001
``````

## Workshop

``````text
workshop_id:  $workshopId
mod_loaded:   true
lots_self:    true
``````

## City selector

``````text
city_selector_visible:         true
spawned_in_muldraugh_or_fallback: true
``````

## Worldmap loader

``````text
invalid_bin_stubs_absent:      true
invalid_magic_error_removed:   true
worldmap_xml_failed_to_load:   true
``````

## Player

``````text
player_fully_connected:        true
player_spawn_coordinate:       10851,9846,0
player_disconnect_coordinate:  10856,9850,0
``````

## IsoMetaGrid

``````text
iso_meta_grid_map_folder_list_empty: true
binary_files_read: false
``````

## Binary writer gate

``````text
binary_writer_gate_closed:     true
no_binary_writer_changes:      true
no_third_party_files_copied:   true
``````

## Next branch

``````text
next_branch: known_working_build42_map_contract_comparator
``````

## Claim boundary

``````text
playable_claim_allowed: false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map8f-result-packet.md"

# ---------------------------------------------------------------------------
# Main packet doc
# ---------------------------------------------------------------------------

$packetPath = Join-Path $Output 'MAP_8F_LOTS_SELF_RUNTIME_RESULT_PACKET.md'
Set-Content -Path $packetPath -Value @"
# MAP-8F: lots=self Runtime Result Packet

``````text
MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED
MAP8F_CITY_SELECTOR_VISIBLE
MAP8F_ISO_META_GRID_MAP_FOLDER_LIST_EMPTY
MAP8F_WORLDMAP_XML_FAILED_TO_LOAD
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````

## Key finding

lots=<MapId> in map.info causes the city selector to list the candidate.
This is the first time the PZMapForge candidate appears in the city/spawn UI.

Selecting the PZMapForge city still spawns the player in Muldraugh / fallback.
IsoMetaGrid does not list the candidate map folder. Binary files not read.

## Packet files

``````text
MAP_8F_LOTS_SELF_RUNTIME_RESULT_PACKET.md  (this file)
map8f-result-packet.json
map8f-result-packet.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
BINARY_WRITER_GATE_STILL_CLOSED
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_8F_LOTS_SELF_RUNTIME_RESULT_PACKET.md"

Write-Output ""
Write-Output "MAP-8F result packet complete."
Write-Output "classification: MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED"
Write-Output "city_selector_visible: true"
Write-Output "iso_meta_grid_map_folder_list_empty: true"
Write-Output "worldmap_xml_failed_to_load: true"
Write-Output "binary_writer_gate_closed: true"
Write-Output "next_branch: known_working_build42_map_contract_comparator"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
