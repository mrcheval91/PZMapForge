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

$schema = 'pzmapforge.map8r-result.v0.1'

$spValues = [System.Collections.ArrayList]::new()
foreach ($v in @('Polygon','highway','primary','trail','natural','forest','water','river','tertiary','building','Residential','secondary')) {
    [void]$spValues.Add($v)
}

$headerModel = [System.Collections.ArrayList]::new()
[void]$headerModel.Add('0x00 char[4] magic=IGMB')
[void]$headerModel.Add('0x04 u32le version=2')
[void]$headerModel.Add('0x08 u32le unknown_a=256')
[void]$headerModel.Add('0x0C u32le unknown_b=59')
[void]$headerModel.Add('0x10 u32le unknown_c=68')
[void]$headerModel.Add('0x14 u32le probable_string_pool_count=12')
[void]$headerModel.Add('0x18 string_pool_start offset=24')
[void]$headerModel.Add('string_format: U16LE_byte_length+ASCII_UTF8_bytes')
[void]$headerModel.Add('confidence: medium (string count matches header field)')
[void]$headerModel.Add('full_format_not_confirmed_from_4096_bytes')

$result = [ordered]@{
    schema                                            = $schema
    reference_path_operator_provided                  = $true
    candidate_path_operator_provided                  = $true
    reference_size_bytes                              = 283881
    bytes_read_count                                  = 4096
    max_bytes_allowed                                 = 4096
    full_file_read                                    = $false
    magic                                             = 'IGMB'
    version_le_u32                                    = 2
    header_unknown_a_offset_8_u32le                   = 256
    header_unknown_b_offset_12_u32le                  = 59
    header_unknown_c_offset_16_u32le                  = 68
    header_probable_string_pool_count_offset_20_u32le = 12
    string_pool_start_offset_candidate                = 24
    string_pool_detected_count                        = 12
    string_pool_count_matches_header_offset_20        = $true
    string_pool_values                                = $spValues
    string_pool_end_offset_candidate                  = 133
    probable_string_format                            = 'U16LE length prefix + ASCII/UTF-8 bytes'
    probable_partial_header_model                     = $headerModel
    partial_header_model_confidence                   = 'medium'
    full_format_understood                            = $false
    geometry_payload_understood                       = $false
    cell_index_understood                             = $false
    writer_implementation_allowed                     = $false
    binary_writer_gate_closed                         = $true
    playable_claim_allowed                            = $false
    third_party_files_copied                          = $false
    next_branch                                       = 'igmb_cell_index_boundary_research_pending_operator_approval'
}

$jsonPath   = Join-Path $outDir 'map8r-real-igmb-structure-result.json'
$mdPath     = Join-Path $outDir 'map8r-real-igmb-structure-result.md'
$packetPath = Join-Path $outDir 'MAP_8R_REAL_IGMB_STRUCTURE_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8R: Real IGMB Structure Result')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add("reference_size_bytes: $($result.reference_size_bytes)")
[void]$mdLines.Add("bytes_read_count: $($result.bytes_read_count)")
[void]$mdLines.Add("magic: $($result.magic)")
[void]$mdLines.Add("version_le_u32: $($result.version_le_u32)")
[void]$mdLines.Add("string_pool_detected_count: $($result.string_pool_detected_count)")
[void]$mdLines.Add("string_pool_count_matches_header_offset_20: $($result.string_pool_count_matches_header_offset_20)")
[void]$mdLines.Add("string_pool_end_offset_candidate: $($result.string_pool_end_offset_candidate)")
[void]$mdLines.Add("partial_header_model_confidence: $($result.partial_header_model_confidence)")
[void]$mdLines.Add("binary_writer_gate_closed: $($result.binary_writer_gate_closed)")
[void]$mdLines.Add("playable_claim_allowed: $($result.playable_claim_allowed)")
[void]$mdLines.Add('')
[void]$mdLines.Add('## String pool values')
[void]$mdLines.Add('')
foreach ($v in $spValues) {
    [void]$mdLines.Add("  - $v")
}
[void]$mdLines.Add('')
[void]$mdLines.Add('## Partial header model')
[void]$mdLines.Add('')
foreach ($h in $headerModel) {
    [void]$mdLines.Add("  $h")
}

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

$packet = @"
# MAP-8R Real IGMB Structure Result Packet

MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED
OPERATOR_RAN_MAP8Q_INSPECTOR=true
STRING_POOL_COUNT_MATCHES_HEADER_OFFSET_20=true
PARTIAL_HEADER_MODEL_CONFIDENCE=medium
FULL_FORMAT_UNDERSTOOD=false
GEOMETRY_PAYLOAD_UNDERSTOOD=false
WRITER_IMPLEMENTATION_ALLOWED=false
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
BINARY_CONTENTS_READ_SCOPE=first_4096_bytes_only
BINARY_CONTENTS_FULL_READ=false
MAX_BYTES_ALLOWED=4096

next_branch=igmb_cell_index_boundary_research_pending_operator_approval
"@
$packet | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8r-real-igmb-structure-result.json -> $jsonPath"
Write-Host "map8r-real-igmb-structure-result.md   -> $mdPath"
Write-Host "MAP_8R_REAL_IGMB_STRUCTURE_RESULT_PACKET.md -> $packetPath"
exit 0
