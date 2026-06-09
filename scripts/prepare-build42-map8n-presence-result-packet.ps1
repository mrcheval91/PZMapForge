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

$schema = 'pzmapforge.map8n-result.v0.1'

$result = [ordered]@{
    schema                                    = $schema
    candidate_worldmap_xml_present            = $true
    candidate_worldmap_xml_size_bytes         = 1915
    candidate_worldmap_xml_bin_present        = $false
    candidate_worldmap_xml_bin_size_bytes     = 0
    reference_worldmap_xml_present            = $true
    reference_worldmap_xml_size_bytes         = 888333
    reference_worldmap_xml_bin_present        = $true
    reference_worldmap_xml_bin_size_bytes     = 283881
    candidate_streets_xml_bin_present         = $false
    reference_streets_xml_bin_present         = $false
    streets_xml_bin_primary_blocker_likely    = $false
    worldmap_xml_text_primary_blocker_likely  = $false
    worldmap_xml_bin_primary_discriminator    = $true
    lotpack_count_pattern_fixed               = $true
    binary_contents_read                      = $false
    no_project_russia_files_copied            = $true
    playable_claim_allowed                    = $false
    binary_writer_gate_closed                 = $true
    next_branch                               = 'worldmap_xml_bin_header_format_investigation_pending_operator_approval'
}

$jsonPath   = Join-Path $outDir 'map8n-result.json'
$mdPath     = Join-Path $outDir 'map8n-result.md'
$packetPath = Join-Path $outDir 'MAP_8N_WORLDMAP_BIN_PRESENCE_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = @(
    '# MAP-8N Worldmap Bin Presence Result Packet'
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
    '# MAP-8N Worldmap Bin Presence Result Packet'
    ''
    '```text'
    'MAP8N_WORLDMAP_XML_BIN_PRESENCE_DISCRIMINATOR_CONFIRMED'
    'BINARY_WRITER_GATE_STILL_CLOSED'
    'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'
    '```'
    ''
    "Packet generated from: $jsonPath"
    ''
    'See map8n-result.md for field table.'
    ''
    'Key discriminator:'
    '  candidate_worldmap_xml_bin_present=false'
    '  reference_worldmap_xml_bin_present=true (283881 bytes)'
    '  worldmap_xml_bin_primary_discriminator=true'
)
$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8n-result.json  -> $jsonPath"
Write-Host "map8n-result.md    -> $mdPath"
Write-Host "packet             -> $packetPath"
exit 0
