[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $Output.Contains('.local')) {
    Write-Error "-Output must be a path under .local/ (got: $Output)"
    exit 1
}

$outDir = $Output
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$schema = 'pzmapforge.map8l-result.v0.1'

$result = [ordered]@{
    schema                                    = $schema
    map8l_worldmap_xml_deployed               = $true
    downloaded_workshop_worldmap_xml_was_map8l = $true
    server_config_correct                     = $true
    candidate_loaded                          = $true
    player_spawn_coordinate                   = '10746,8288,0'
    player_disconnect_coordinate              = '10777,8287,0'
    spawn_coordinate_matches_35_27            = $true
    iso_meta_grid_map_folder_list_empty       = $true
    parent_folder_listed_by_isometagrid       = $false
    worldmap_xml_asset_failed_to_load         = $true
    child_worldmap_xml_failed_to_load         = $true
    parent_worldmap_xml_failed_to_load        = $true
    worldmap_xml_bin_present                  = $false
    invalid_magic_logged                      = $false
    lotheader_parse_attempt_logged            = $false
    generated_cell_content_mounted            = $false
    playable_claim_allowed                    = $false
    binary_writer_gate_closed                 = $true
    next_branch                               = 'worldmap_xml_bin_binary_format_investigation'
}

$jsonPath = Join-Path $outDir 'map8l-result.json'
$mdPath   = Join-Path $outDir 'map8l-result.md'
$packetPath = Join-Path $outDir 'MAP_8L_WORLDMAP_XML_RUNTIME_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = @(
    '# MAP-8L Runtime Result Packet'
    ''
    "Schema: ``$schema``"
    ''
    '| Field | Value |'
    '|-------|-------|'
)
foreach ($key in $result.Keys) {
    $val = $result[$key]
    $display = if ($val -is [bool]) { $val.ToString().ToLower() } else { [string]$val }
    $mdLines += "| $key | $display |"
}
$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

$packetLines = @(
    '# MAP-8L Worldmap XML Runtime Result Packet'
    ''
    '```text'
    'MAP8L_WORLDMAP_XML_FAILED_TO_MOUNT'
    'BINARY_WRITER_GATE_STILL_CLOSED'
    'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'
    '```'
    ''
    "Packet generated from: $jsonPath"
    ''
    "See map8l-result.md for field table."
)
$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8l-result.json  -> $jsonPath"
Write-Host "map8l-result.md    -> $mdPath"
Write-Host "packet             -> $packetPath"
exit 0
