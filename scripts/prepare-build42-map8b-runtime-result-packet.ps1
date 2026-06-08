#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-8B: Writes the version-scoped media path runtime result packet.

    Records the MAP-8B partial registration breakthrough:
    - 42\media path visible to worldmap loader
    - worldmap.xml.bin / worldmap-forest.xml.bin actively read but invalid magic
    - IsoMetaGrid map folder list still empty
    - Player fully connected (fallback world, not confirmed PZMapForge content)

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

Write-Output "MAP-8B: Version-Scoped Media Path Runtime Result Packet"
Write-Output "Output: $Output"
Write-Output ""

$candidateMapId = 'pzmapforge_build42_candidate_v4_001'
$workshopId     = '3740642200'

# ---------------------------------------------------------------------------
# Result fields (all from MAP-8B manual runtime observation and log capture)
# ---------------------------------------------------------------------------

$nextBranchCandidates = @(
    'remove_invalid_worldmap_bin_stubs_rely_on_xml_png_only',
    'investigate_map_info_lots_iso_meta_grid_registration_contract',
    'inspect_build42_known_working_version_scoped_structure_without_copying_third_party'
)

$resultJson = [ordered]@{
    schema                                             = 'pzmapforge.map8b-result-packet.v0.1'
    candidate_map_id                                   = $candidateMapId
    workshop_id                                        = $workshopId
    pz_build                                           = '42.19.0'
    classification                                     = 'MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH'
    workshop_ready                                     = $true
    mod_loaded                                         = $true
    visual_custom_city_selector_visible                = $true
    player_fully_connected                             = $true
    player_spawn_coordinate                            = '10878,10028,0'
    player_death_coordinate                            = '10885,10056,0'
    iso_meta_grid_map_folder_list_empty                = $true
    version_scoped_media_path_visible_to_worldmap_loader = $true
    worldmap_bin_invalid_magic                         = $true
    worldmap_binary_error                              = 'java.io.IOException: invalid format (magic doesn''t match)'
    worldmap_binary_stack                              = 'zombie.worldMap.WorldMapBinary.read'
    playable_claim_allowed                             = $false
    binary_writer_gate_closed                          = $true
    no_binary_writer_changes                           = $true
    no_third_party_files_copied                        = $true
    no_pz_run_by_script                                = $true
    no_automatic_workshop_upload                       = $true
    next_branch_candidates                             = $nextBranchCandidates
}

$jsonPath = Join-Path $Output 'map8b-result-packet.json'
$resultJson | ConvertTo-Json -Depth 3 | Set-Content -Path $jsonPath -Encoding ASCII
Write-Output "Wrote: map8b-result-packet.json"

# ---------------------------------------------------------------------------
# Result markdown
# ---------------------------------------------------------------------------

$mdPath = Join-Path $Output 'map8b-result-packet.md'
Set-Content -Path $mdPath -Value @"
# MAP-8B: Version-Scoped Media Path Runtime Result

``````text
MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH
MAP8B_VERSION_42_MEDIA_PATH_VISIBLE_TO_WORLDMAP_LOADER
WORLDMAP_BIN_INVALID_MAGIC
ISO_META_GRID_MAP_FOLDER_LIST_EMPTY
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Workshop

``````text
workshop_id:     $workshopId
candidate_map_id: $candidateMapId
workshop_ready:  true
mod_loaded:      true
``````

## Player

``````text
visual_custom_city_selector_visible: true
player_fully_connected:              true
player_spawn_coordinate:             10878,10028,0
player_death_coordinate:             10885,10056,0
``````

## IsoMetaGrid

``````text
iso_meta_grid_map_folder_list_empty: true
``````

## Worldmap asset loader

``````text
version_scoped_media_path_visible_to_worldmap_loader: true
worldmap_bin_invalid_magic: true
error: java.io.IOException: invalid format (magic doesn't match)
stack: zombie.worldMap.WorldMapBinary.read
       zombie.worldMap.FileTask_LoadWorldMapBinary.call
``````

## Binary writer gate

``````text
binary_writer_gate_closed:     true
no_binary_writer_changes:      true
no_third_party_files_copied:   true
no_pz_run_by_script:           true
no_automatic_workshop_upload:  true
``````

## Next branch candidates

``````text
1. remove_invalid_worldmap_bin_stubs_rely_on_xml_png_only
2. investigate_map_info_lots_iso_meta_grid_registration_contract
3. inspect_build42_known_working_version_scoped_structure_without_copying_third_party
``````

## Claim boundary

``````text
playable_claim_allowed:         false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map8b-result-packet.md"

# ---------------------------------------------------------------------------
# Main packet doc
# ---------------------------------------------------------------------------

$packetPath = Join-Path $Output 'MAP_8B_VERSION_MEDIA_RUNTIME_RESULT_PACKET.md'
Set-Content -Path $packetPath -Value @"
# MAP-8B: Version-Scoped Media Path Runtime Result Packet

``````text
MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH
MAP8B_VERSION_42_MEDIA_PATH_VISIBLE_TO_WORLDMAP_LOADER
WORLDMAP_BIN_INVALID_MAGIC
ISO_META_GRID_MAP_FOLDER_LIST_EMPTY
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````

## Breakthrough

42\media path is now visible to the worldmap / city-selection asset loader.
The client attempted to read worldmap.xml.bin and worldmap-forest.xml.bin from:

``````text
\mods\pzmapforge_build42_candidate_v4_001\42\media\maps\pzmapforge_build42_candidate_v4_001\
``````

Both reads failed with: invalid format (magic doesn't match)

IsoMetaGrid still shows an empty map folder list.
Binary writer gate remains closed.

## Packet files

``````text
MAP_8B_VERSION_MEDIA_RUNTIME_RESULT_PACKET.md  (this file)
map8b-result-packet.json
map8b-result-packet.md
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
Write-Output "Wrote: MAP_8B_VERSION_MEDIA_RUNTIME_RESULT_PACKET.md"

Write-Output ""
Write-Output "MAP-8B result packet complete."
Write-Output "classification: MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH"
Write-Output "iso_meta_grid_map_folder_list_empty: true"
Write-Output "version_scoped_media_path_visible_to_worldmap_loader: true"
Write-Output "worldmap_bin_invalid_magic: true"
Write-Output "binary_writer_gate_closed: true"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
