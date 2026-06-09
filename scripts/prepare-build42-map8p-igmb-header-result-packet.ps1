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

$schema = 'pzmapforge.map8p-result.v0.1'

$result = [ordered]@{
    schema                                          = $schema
    candidate_present                               = $false
    reference_present                               = $true
    reference_size_bytes                            = 283881
    reference_bytes_read_count                      = 64
    reference_first_16_bytes_hex                    = '49 47 4D 42 02 00 00 00 00 01 00 00 3B 00 00 00'
    reference_first_64_bytes_hex                    = '49 47 4D 42 02 00 00 00 00 01 00 00 3B 00 00 00 44 00 00 00 0C 00 00 00 07 00 50 6F 6C 79 67 6F 6E 07 00 68 69 67 68 77 61 79 07 00 70 72 69 6D 61 72 79 05 00 74 72 61 69 6C 07 00 6E 61 74 75'
    reference_ascii_preview                         = 'IGMB........;...D.........Polygon..highway..primary..trail..natu'
    reference_detected_signature                    = 'igmb'
    igmb_magic_detected                             = $true
    appears_compressed                              = $false
    appears_custom_binary_worldmap_format           = $true
    likely_little_endian_fields                     = $true
    possible_version_value                          = 2
    possible_length_prefixed_strings                = $true
    possible_string_length_prefix_width             = '16-bit'
    visible_string_tokens                           = 'Polygon,highway,primary,trail,natu_prefix'
    community_layout_notes_recorded_as_unverified   = $true
    big_endian_claim_contradicted_by_observed_header = $true
    max_bytes_allowed                               = 64
    binary_contents_read_scope                      = 'first_64_bytes_only'
    binary_contents_full_read                       = $false
    third_party_files_copied                        = $false
    playable_claim_allowed                          = $false
    binary_writer_gate_closed                       = $true
    next_branch                                     = 'igmb_structure_research_pending_operator_approval'
}

$jsonPath   = Join-Path $outDir 'map8p-igmb-header-result.json'
$mdPath     = Join-Path $outDir 'map8p-igmb-header-result.md'
$packetPath = Join-Path $outDir 'MAP_8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = @(
    '# MAP-8P IGMB Worldmap Bin Header Result Packet'
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
    '# MAP-8P IGMB Worldmap Bin Header Result Packet'
    ''
    '```text'
    'MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED'
    'IGMB_MAGIC_DETECTED=true'
    'APPEARS_COMPRESSED=false'
    'LIKELY_LITTLE_ENDIAN_FIELDS=true'
    'BIG_ENDIAN_CLAIM_CONTRADICTED_BY_OBSERVED_HEADER=true'
    'COMMUNITY_LAYOUT_NOTES_RECORDED_AS_UNVERIFIED=true'
    'BINARY_WRITER_GATE_STILL_CLOSED'
    'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'
    '```'
    ''
    "Packet generated from: $jsonPath"
    ''
    'See map8p-igmb-header-result.md for field table.'
    ''
    'Key observations (first 64 bytes only, not a format specification):'
    '  Magic: 49 47 4D 42 = IGMB'
    '  Apparent version (little-endian U32): 02 00 00 00 = 2'
    '  Visible string tokens: Polygon, highway, primary, trail, natu_prefix'
    '  Apparent U16LE length-prefixed strings: 07 00 + string content'
    ''
    'CORRECTION: community note claimed Java big-endian.'
    'Observed header is consistent with little-endian fields only.'
    ''
    'Next branch:'
    '  igmb_structure_research_pending_operator_approval'
)
$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8p-igmb-header-result.json  -> $jsonPath"
Write-Host "map8p-igmb-header-result.md    -> $mdPath"
Write-Host "packet                         -> $packetPath"
exit 0
