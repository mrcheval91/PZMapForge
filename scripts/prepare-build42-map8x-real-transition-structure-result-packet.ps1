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

$schema = 'pzmapforge.map8x-result.v0.1'

$result = [ordered]@{
    schema                                         = $schema
    operator_ran_map8w_inspector                   = $true
    reference_size_bytes                           = 283881
    bytes_read_count                               = 65536
    max_bytes_allowed                              = 65536
    full_file_read                                 = $false
    transition_offset                              = 6389
    transition_offset_in_range                     = $true
    transition_window_before_all_ff                = $true
    candidate_header_u32_triplet_first             = 30
    candidate_header_u32_triplet_second            = 26
    candidate_header_u32_triplet_third             = 9
    candidate_header_triplet_interpretation        = 'observed_only_unconfirmed'
    candidate_header_triplet_confidence            = 'low'
    post_triplet_payload_resembles_packed_u16le_pairs = $true
    printable_ascii_runs_near_transition_count     = 0
    entropy_estimate_transition_window             = 4.0713
    small_value_cluster_observed                   = $true
    ff_or_null_sentinels_observed                  = $true
    monotonic_sequences_observed                   = $true
    strongest_current_hypothesis                   = 'transition_immediately_follows_ff_padding_first_12_bytes_are_3_u32le_fields_bytes_after_resemble_packed_u16le_pairs_no_hypothesis_confirmed'
    transition_structure_understood                = $false
    full_format_understood                         = $false
    cell_index_understood                          = $false
    geometry_payload_understood                    = $false
    writer_implementation_allowed                  = $false
    binary_writer_gate_closed                      = $true
    playable_claim_allowed                         = $false
    third_party_files_copied                       = $false
    no_pz_run_by_claude                            = $true
    no_workshop_upload_by_claude                   = $true
    next_branch                                    = 'igmb_transition_model_hypothesis_review_pending_operator_approval'
}

$jsonPath   = Join-Path $outDir 'map8x-real-transition-structure-result.json'
$mdPath     = Join-Path $outDir 'map8x-real-transition-structure-result.md'
$packetPath = Join-Path $outDir 'MAP_8X_REAL_TRANSITION_STRUCTURE_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8X Real Transition Structure Result')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add("operator_ran_map8w_inspector: $($result.operator_ran_map8w_inspector)")
[void]$mdLines.Add("reference_size_bytes: $($result.reference_size_bytes)")
[void]$mdLines.Add("bytes_read_count: $($result.bytes_read_count)")
[void]$mdLines.Add("max_bytes_allowed: $($result.max_bytes_allowed)")
[void]$mdLines.Add("full_file_read: $($result.full_file_read)")
[void]$mdLines.Add("transition_offset: $($result.transition_offset)")
[void]$mdLines.Add("transition_offset_in_range: $($result.transition_offset_in_range)")
[void]$mdLines.Add("transition_window_before_all_ff: $($result.transition_window_before_all_ff)")
[void]$mdLines.Add("candidate_header_u32_triplet: first=$($result.candidate_header_u32_triplet_first) second=$($result.candidate_header_u32_triplet_second) third=$($result.candidate_header_u32_triplet_third) ($($result.candidate_header_triplet_interpretation))")
[void]$mdLines.Add("candidate_header_triplet_confidence: $($result.candidate_header_triplet_confidence)")
[void]$mdLines.Add("post_triplet_payload_resembles_packed_u16le_pairs: $($result.post_triplet_payload_resembles_packed_u16le_pairs)")
[void]$mdLines.Add("printable_ascii_runs_near_transition_count: $($result.printable_ascii_runs_near_transition_count)")
[void]$mdLines.Add("entropy_estimate_transition_window: $($result.entropy_estimate_transition_window)")
[void]$mdLines.Add("small_value_cluster_observed: $($result.small_value_cluster_observed)")
[void]$mdLines.Add("ff_or_null_sentinels_observed: $($result.ff_or_null_sentinels_observed)")
[void]$mdLines.Add("monotonic_sequences_observed: $($result.monotonic_sequences_observed)")
[void]$mdLines.Add("transition_structure_understood: $($result.transition_structure_understood)")
[void]$mdLines.Add("full_format_understood: $($result.full_format_understood)")
[void]$mdLines.Add("cell_index_understood: $($result.cell_index_understood)")
[void]$mdLines.Add("binary_writer_gate_closed: $($result.binary_writer_gate_closed)")
[void]$mdLines.Add("playable_claim_allowed: $($result.playable_claim_allowed)")
[void]$mdLines.Add("third_party_files_copied: $($result.third_party_files_copied)")
[void]$mdLines.Add('')
[void]$mdLines.Add("next_branch: $($result.next_branch)")

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

$packetLines = [System.Collections.ArrayList]::new()
[void]$packetLines.Add('# MAP-8X Real Transition Structure Result Packet')
[void]$packetLines.Add('')
[void]$packetLines.Add('```text')
[void]$packetLines.Add('MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED')
[void]$packetLines.Add('OPERATOR_RAN_MAP8W_INSPECTOR=true')
[void]$packetLines.Add('BYTES_READ_COUNT=65536')
[void]$packetLines.Add('MAX_BYTES_ALLOWED=65536')
[void]$packetLines.Add('FULL_FILE_READ=false')
[void]$packetLines.Add('TRANSITION_OFFSET=6389')
[void]$packetLines.Add('TRANSITION_OFFSET_IN_RANGE=true')
[void]$packetLines.Add('TRANSITION_WINDOW_BEFORE_ALL_FF=true')
[void]$packetLines.Add('CANDIDATE_HEADER_U32_TRIPLET=30_26_9_OBSERVED_ONLY')
[void]$packetLines.Add('POST_TRIPLET_PAYLOAD_RESEMBLES_PACKED_U16LE_PAIRS=true')
[void]$packetLines.Add('ENTROPY_ESTIMATE_TRANSITION_WINDOW=4.0713')
[void]$packetLines.Add('TRANSITION_STRUCTURE_UNDERSTOOD=false')
[void]$packetLines.Add('FULL_FORMAT_UNDERSTOOD=false')
[void]$packetLines.Add('CELL_INDEX_UNDERSTOOD=false')
[void]$packetLines.Add('BINARY_WRITER_GATE_STILL_CLOSED')
[void]$packetLines.Add('PUBLIC_PLAYABLE_CLAIM_ALLOWED=false')
[void]$packetLines.Add('NO_PZ_RUN_BY_CLAUDE')
[void]$packetLines.Add('NO_WORKSHOP_UPLOAD_BY_CLAUDE')
[void]$packetLines.Add('NO_THIRD_PARTY_FILES_COPIED')
[void]$packetLines.Add('```')
[void]$packetLines.Add('')
[void]$packetLines.Add("Schema: ``$schema``")
[void]$packetLines.Add('')
[void]$packetLines.Add('Operator ran scripts/inspect-build42-igmb-transition-structure.ps1 against Project Russia')
[void]$packetLines.Add('worldmap.xml.bin (283881 bytes, first 65536 bytes read).')
[void]$packetLines.Add('Transition at offset 6389. First 12 bytes: three U32LE fields (30, 26, 9), observed-only.')
[void]$packetLines.Add('Post-triplet bytes resemble packed U16LE pairs. No field meaning confirmed.')
[void]$packetLines.Add('Monotonic sequences and small-value clusters observed in first 128 bytes.')
[void]$packetLines.Add('entropy_estimate_transition_window=4.0713. No printable ASCII runs.')
[void]$packetLines.Add('No model confirmed. No cell index understood. No geometry payload understood.')
[void]$packetLines.Add('')
[void]$packetLines.Add('next_branch=igmb_transition_model_hypothesis_review_pending_operator_approval')
[void]$packetLines.Add('Binary writer gate remains CLOSED.')
[void]$packetLines.Add('No playable claim is allowed.')

$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8x-real-transition-structure-result.json -> $jsonPath"
Write-Host "map8x-real-transition-structure-result.md   -> $mdPath"
Write-Host "MAP_8X_REAL_TRANSITION_STRUCTURE_RESULT_PACKET.md -> $packetPath"
exit 0
