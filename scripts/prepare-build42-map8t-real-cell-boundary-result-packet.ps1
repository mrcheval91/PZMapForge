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

$schema = 'pzmapforge.map8t-result.v0.1'

$result = [ordered]@{
    schema                                              = $schema
    operator_ran_map8s_inspector                        = $true
    reference_size_bytes                                = 283881
    bytes_read_count                                    = 4096
    max_bytes_allowed                                   = 4096
    full_file_read                                      = $false
    magic                                               = 'IGMB'
    version_le_u32                                      = 2
    string_pool_end_offset                              = 133
    post_string_pool_window_start                       = 133
    post_string_pool_window_bytes_available             = 3963
    first_128_bytes_after_string_pool_all_ff            = $true
    first_256_bytes_after_string_pool_all_ff            = $true
    observed_u32le_values_after_string_pool_are_minus_one = $true
    observed_u16le_values_after_string_pool_are_65535   = $true
    observed_float32le_values_after_string_pool_are_nan = $true
    plausible_count_fields_after_string_pool_count      = 0
    plausible_offset_table_candidates_count             = 0
    plausible_cell_coordinate_candidates_count          = 0
    zero_run_candidates_count                           = 0
    post_string_pool_region_interpretation              = 'ff_padding_or_sentinel_observed_within_4096_byte_window'
    immediate_cell_index_after_string_pool_supported    = $false
    first_non_ff_offset_known                           = $false
    first_non_ff_offset                                 = $null
    full_format_understood                              = $false
    cell_index_understood                               = $false
    geometry_payload_understood                         = $false
    writer_implementation_allowed                       = $false
    binary_writer_gate_closed                           = $true
    playable_claim_allowed                              = $false
    third_party_files_copied                            = $false
    next_branch                                         = 'igmb_first_non_ff_transition_scan_pending_operator_approval'
}

$jsonPath   = Join-Path $outDir 'map8t-real-cell-boundary-result.json'
$mdPath     = Join-Path $outDir 'map8t-real-cell-boundary-result.md'
$packetPath = Join-Path $outDir 'MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8T Real Cell Boundary FF Sentinel Result')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add('## Inspector run')
[void]$mdLines.Add('')
[void]$mdLines.Add("operator_ran_map8s_inspector: $($result.operator_ran_map8s_inspector)")
[void]$mdLines.Add("reference_size_bytes: $($result.reference_size_bytes)")
[void]$mdLines.Add("bytes_read_count: $($result.bytes_read_count)")
[void]$mdLines.Add("max_bytes_allowed: $($result.max_bytes_allowed)")
[void]$mdLines.Add("full_file_read: $($result.full_file_read)")
[void]$mdLines.Add("magic: $($result.magic)")
[void]$mdLines.Add("version_le_u32: $($result.version_le_u32)")
[void]$mdLines.Add("string_pool_end_offset: $($result.string_pool_end_offset)")
[void]$mdLines.Add("post_string_pool_window_start: $($result.post_string_pool_window_start)")
[void]$mdLines.Add("post_string_pool_window_bytes_available: $($result.post_string_pool_window_bytes_available)")
[void]$mdLines.Add('')
[void]$mdLines.Add('## FF region observations')
[void]$mdLines.Add('')
[void]$mdLines.Add("first_128_bytes_after_string_pool_all_ff: $($result.first_128_bytes_after_string_pool_all_ff)")
[void]$mdLines.Add("first_256_bytes_after_string_pool_all_ff: $($result.first_256_bytes_after_string_pool_all_ff)")
[void]$mdLines.Add("observed_u32le_values_after_string_pool_are_minus_one: $($result.observed_u32le_values_after_string_pool_are_minus_one)")
[void]$mdLines.Add("observed_u16le_values_after_string_pool_are_65535: $($result.observed_u16le_values_after_string_pool_are_65535)")
[void]$mdLines.Add("observed_float32le_values_after_string_pool_are_nan: $($result.observed_float32le_values_after_string_pool_are_nan)")
[void]$mdLines.Add("plausible_count_fields_after_string_pool_count: $($result.plausible_count_fields_after_string_pool_count)")
[void]$mdLines.Add("plausible_offset_table_candidates_count: $($result.plausible_offset_table_candidates_count)")
[void]$mdLines.Add("plausible_cell_coordinate_candidates_count: $($result.plausible_cell_coordinate_candidates_count)")
[void]$mdLines.Add("zero_run_candidates_count: $($result.zero_run_candidates_count)")
[void]$mdLines.Add("post_string_pool_region_interpretation: $($result.post_string_pool_region_interpretation)")
[void]$mdLines.Add('')
[void]$mdLines.Add('## Format understanding')
[void]$mdLines.Add('')
[void]$mdLines.Add("immediate_cell_index_after_string_pool_supported: $($result.immediate_cell_index_after_string_pool_supported)")
[void]$mdLines.Add("first_non_ff_offset_known: $($result.first_non_ff_offset_known)")
[void]$mdLines.Add("first_non_ff_offset: $($result.first_non_ff_offset)")
[void]$mdLines.Add("full_format_understood: $($result.full_format_understood)")
[void]$mdLines.Add("cell_index_understood: $($result.cell_index_understood)")
[void]$mdLines.Add("geometry_payload_understood: $($result.geometry_payload_understood)")
[void]$mdLines.Add('')
[void]$mdLines.Add('## Safety')
[void]$mdLines.Add('')
[void]$mdLines.Add("writer_implementation_allowed: $($result.writer_implementation_allowed)")
[void]$mdLines.Add("binary_writer_gate_closed: $($result.binary_writer_gate_closed)")
[void]$mdLines.Add("playable_claim_allowed: $($result.playable_claim_allowed)")
[void]$mdLines.Add("third_party_files_copied: $($result.third_party_files_copied)")
[void]$mdLines.Add('')
[void]$mdLines.Add("next_branch: $($result.next_branch)")

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

$packetLines = [System.Collections.ArrayList]::new()
[void]$packetLines.Add('# MAP-8T Real Cell Boundary FF Sentinel Result Packet')
[void]$packetLines.Add('')
[void]$packetLines.Add('```text')
[void]$packetLines.Add('MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED')
[void]$packetLines.Add('OPERATOR_RAN_MAP8S_INSPECTOR=true')
[void]$packetLines.Add('FIRST_128_BYTES_AFTER_STRING_POOL_ALL_FF=true')
[void]$packetLines.Add('FIRST_256_BYTES_AFTER_STRING_POOL_ALL_FF=true')
[void]$packetLines.Add('IMMEDIATE_CELL_INDEX_AFTER_STRING_POOL_SUPPORTED=false')
[void]$packetLines.Add('FIRST_NON_FF_OFFSET_KNOWN=false')
[void]$packetLines.Add('BINARY_WRITER_GATE_STILL_CLOSED')
[void]$packetLines.Add('PUBLIC_PLAYABLE_CLAIM_ALLOWED=false')
[void]$packetLines.Add('NO_PZ_RUN_BY_CLAUDE')
[void]$packetLines.Add('NO_WORKSHOP_UPLOAD_BY_CLAUDE')
[void]$packetLines.Add('NO_THIRD_PARTY_FILES_COPIED')
[void]$packetLines.Add('BINARY_CONTENTS_FULL_READ=false')
[void]$packetLines.Add('MAX_BYTES_ALLOWED=4096')
[void]$packetLines.Add('FULL_FORMAT_UNDERSTOOD=false')
[void]$packetLines.Add('CELL_INDEX_UNDERSTOOD=false')
[void]$packetLines.Add('```')
[void]$packetLines.Add('')
[void]$packetLines.Add("Schema: ``$schema``")
[void]$packetLines.Add('')
[void]$packetLines.Add('The operator ran the MAP-8S IGMB cell boundary inspector against the actual')
[void]$packetLines.Add('Project Russia worldmap.xml.bin (283881 bytes, first 4096 bytes read).')
[void]$packetLines.Add('The bytes immediately after string_pool_end_offset=133 are all 0xFF within')
[void]$packetLines.Add('the observed 4096-byte window.')
[void]$packetLines.Add('')
[void]$packetLines.Add('This does NOT prove the full file is FF after offset 133.')
[void]$packetLines.Add('Only the first 4096 bytes were read. The file is 283881 bytes.')
[void]$packetLines.Add('The cell index or geometry section likely exists beyond the observed window.')
[void]$packetLines.Add('')
[void]$packetLines.Add('next_branch=igmb_first_non_ff_transition_scan_pending_operator_approval')
[void]$packetLines.Add('Binary writer gate remains CLOSED.')
[void]$packetLines.Add('No playable claim is allowed.')

$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8t-real-cell-boundary-result.json -> $jsonPath"
Write-Host "map8t-real-cell-boundary-result.md   -> $mdPath"
Write-Host "MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_PACKET.md -> $packetPath"
exit 0
