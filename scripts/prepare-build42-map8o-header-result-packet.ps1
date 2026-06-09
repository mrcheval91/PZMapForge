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

$schema = 'pzmapforge.map8o-result.v0.1'

$result = [ordered]@{
    schema                                   = $schema
    operator_approved_header_only_inspection = $true
    max_bytes_allowed                        = 64
    binary_contents_read_scope               = 'first_64_bytes_only'
    binary_contents_full_read                = $false
    no_project_russia_files_copied           = $true
    playable_claim_allowed                   = $false
    binary_writer_gate_closed                = $true
    worldmap_xml_bin_primary_discriminator   = $true
    next_branch                              = 'run_header_inspector_then_evaluate_signature'
}

$jsonPath   = Join-Path $outDir 'map8o-header-result.json'
$mdPath     = Join-Path $outDir 'map8o-header-result.md'
$packetPath = Join-Path $outDir 'MAP_8O_WORLDMAP_BIN_HEADER_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = @(
    '# MAP-8O Worldmap Bin Header Result Packet'
    ''
    "Schema: ``$schema``"
    ''
    '| Field | Value |'
    '|-------|-------|'
)
foreach ($key in $result.Keys) {
    $val = $result[$key]
    $display = if ($val -is [bool]) { $val.ToString().ToLower() } elseif ($val -is [int]) { [string]$val } else { [string]$val }
    $mdLines += "| $key | $display |"
}
$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

$packetLines = @(
    '# MAP-8O Worldmap Bin Header Result Packet'
    ''
    '```text'
    'MAP8O_WORLDMAP_XML_BIN_HEADER_INSPECTION_DEFINED'
    'OPERATOR_APPROVED_HEADER_ONLY_INSPECTION=true'
    'BINARY_WRITER_GATE_STILL_CLOSED'
    'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'
    '```'
    ''
    "Packet generated from: $jsonPath"
    ''
    'See map8o-header-result.md for field table.'
    ''
    'Approval scope:'
    '  operator_approved_header_only_inspection=true'
    '  max_bytes_allowed=64'
    '  binary_contents_read_scope=first_64_bytes_only'
    '  binary_contents_full_read=false'
    ''
    'Key discriminator (from MAP-8N):'
    '  worldmap_xml_bin_primary_discriminator=true (leading hypothesis, not proven fact)'
    ''
    'Next branch:'
    '  run_header_inspector_then_evaluate_signature'
)
$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8o-header-result.json -> $jsonPath"
Write-Host "map8o-header-result.md   -> $mdPath"
Write-Host "packet                   -> $packetPath"
exit 0
