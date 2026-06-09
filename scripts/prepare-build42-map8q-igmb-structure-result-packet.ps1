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

$schema = 'pzmapforge.map8q-result.v0.1'

$result = [ordered]@{
    schema                                      = $schema
    operator_approved_igmb_structure_research   = $true
    max_bytes_allowed                           = 4096
    binary_contents_read_scope                  = 'first_4096_bytes_only'
    binary_contents_full_read                   = $false
    third_party_files_copied                    = $false
    playable_claim_allowed                      = $false
    binary_writer_gate_closed                   = $true
    next_branch                                 = 'igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient'
}

$jsonPath   = Join-Path $outDir 'map8q-igmb-structure-result.json'
$mdPath     = Join-Path $outDir 'map8q-igmb-structure-result.md'
$packetPath = Join-Path $outDir 'MAP_8Q_IGMB_STRUCTURE_RESEARCH_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = @(
    '# MAP-8Q IGMB Structure Research Result Packet'
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
    '# MAP-8Q IGMB Structure Research Packet'
    ''
    '```text'
    'MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED'
    'OPERATOR_APPROVED_IGMB_STRUCTURE_RESEARCH=true'
    'MAX_BYTES_ALLOWED=4096'
    'BINARY_CONTENTS_FULL_READ=false'
    'THIRD_PARTY_FILES_COPIED=false'
    'BINARY_WRITER_GATE_STILL_CLOSED'
    'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'
    '```'
    ''
    "Packet generated from: $jsonPath"
    ''
    'See map8q-igmb-structure-result.md for field table.'
    ''
    'Inspector: scripts/inspect-build42-igmb-structure.ps1'
    'Max read: 4096 bytes'
    'Scope: first_4096_bytes_only'
    ''
    'Next branch:'
    '  igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient'
)
$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8q-igmb-structure-result.json  -> $jsonPath"
Write-Host "map8q-igmb-structure-result.md    -> $mdPath"
Write-Host "packet                            -> $packetPath"
exit 0
