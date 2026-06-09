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

$schema = 'pzmapforge.map8v-result.v0.1'

$result = [ordered]@{
    schema                                                 = $schema
    operator_ran_map8u_scanner                             = $true
    reference_size_bytes                                   = 283881
    bytes_read_count                                       = 65536
    max_bytes_allowed                                      = 65536
    full_file_read                                         = $false
    string_pool_end_offset                                 = 133
    first_non_ff_found                                     = $true
    first_non_ff_offset                                    = 6389
    first_non_ff_relative_offset_after_string_pool         = 6256
    ff_run_length_until_first_non_ff                       = 6256
    transition_offset_is_4_byte_aligned                    = $false
    transition_offset_is_2_byte_aligned                    = $false
    hex_window_after_transition_starts_with                = '1e 00 00 00 1a 00 00 00 09 00 00 00'
    exact_u32le_at_transition_0                            = 30
    exact_u32le_at_transition_4                            = 26
    exact_u32le_at_transition_8                            = 9
    exact_offset_decoding_required                         = $true
    aligned_u32le_values_are_context_only                  = $true
    transition_structure_understood                        = $false
    full_format_understood                                 = $false
    cell_index_understood                                  = $false
    geometry_payload_understood                            = $false
    writer_implementation_allowed                          = $false
    binary_writer_gate_closed                              = $true
    playable_claim_allowed                                 = $false
    third_party_files_copied                               = $false
    next_branch                                            = 'igmb_transition_structure_analysis_pending_operator_approval'
}

$jsonPath   = Join-Path $outDir 'map8v-real-first-non-ff-transition-result.json'
$mdPath     = Join-Path $outDir 'map8v-real-first-non-ff-transition-result.md'
$packetPath = Join-Path $outDir 'MAP_8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8V Real First Non-FF Transition Result')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add("operator_ran_map8u_scanner: $($result.operator_ran_map8u_scanner)")
[void]$mdLines.Add("reference_size_bytes: $($result.reference_size_bytes)")
[void]$mdLines.Add("bytes_read_count: $($result.bytes_read_count)")
[void]$mdLines.Add("first_non_ff_found: $($result.first_non_ff_found)")
[void]$mdLines.Add("first_non_ff_offset: $($result.first_non_ff_offset)")
[void]$mdLines.Add("ff_run_length_until_first_non_ff: $($result.ff_run_length_until_first_non_ff)")
[void]$mdLines.Add("transition_offset_is_4_byte_aligned: $($result.transition_offset_is_4_byte_aligned)")
[void]$mdLines.Add("transition_offset_is_2_byte_aligned: $($result.transition_offset_is_2_byte_aligned)")
[void]$mdLines.Add("exact_u32le_at_transition_0: $($result.exact_u32le_at_transition_0)")
[void]$mdLines.Add("exact_u32le_at_transition_4: $($result.exact_u32le_at_transition_4)")
[void]$mdLines.Add("exact_u32le_at_transition_8: $($result.exact_u32le_at_transition_8)")
[void]$mdLines.Add("aligned_u32le_values_are_context_only: $($result.aligned_u32le_values_are_context_only)")
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
[void]$packetLines.Add('# MAP-8V Real First Non-FF Transition Result Packet')
[void]$packetLines.Add('')
[void]$packetLines.Add('```text')
[void]$packetLines.Add('MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED')
[void]$packetLines.Add('OPERATOR_RAN_MAP8U_SCANNER=true')
[void]$packetLines.Add('FIRST_NON_FF_OFFSET=6389')
[void]$packetLines.Add('FF_RUN_LENGTH=6256')
[void]$packetLines.Add('TRANSITION_OFFSET_IS_4_BYTE_ALIGNED=false')
[void]$packetLines.Add('EXACT_U32LE_AT_TRANSITION_0=30')
[void]$packetLines.Add('EXACT_U32LE_AT_TRANSITION_4=26')
[void]$packetLines.Add('EXACT_U32LE_AT_TRANSITION_8=9')
[void]$packetLines.Add('ALIGNED_U32LE_VALUES_ARE_CONTEXT_ONLY=true')
[void]$packetLines.Add('TRANSITION_STRUCTURE_UNDERSTOOD=false')
[void]$packetLines.Add('BINARY_WRITER_GATE_STILL_CLOSED')
[void]$packetLines.Add('PUBLIC_PLAYABLE_CLAIM_ALLOWED=false')
[void]$packetLines.Add('NO_PZ_RUN_BY_CLAUDE')
[void]$packetLines.Add('NO_WORKSHOP_UPLOAD_BY_CLAUDE')
[void]$packetLines.Add('NO_THIRD_PARTY_FILES_COPIED')
[void]$packetLines.Add('```')
[void]$packetLines.Add('')
[void]$packetLines.Add("Schema: ``$schema``")
[void]$packetLines.Add('')
[void]$packetLines.Add('The operator ran scripts/inspect-build42-igmb-first-non-ff-transition.ps1 against')
[void]$packetLines.Add('Project Russia worldmap.xml.bin. The first non-FF byte is at offset 6389.')
[void]$packetLines.Add('The FF region extends 6256 bytes after string_pool_end_offset=133.')
[void]$packetLines.Add('Exact U32LE values at transition (unaligned): 30, 26, 9. Observed-only.')
[void]$packetLines.Add('')
[void]$packetLines.Add('next_branch=igmb_transition_structure_analysis_pending_operator_approval')
[void]$packetLines.Add('Binary writer gate remains CLOSED.')
[void]$packetLines.Add('No playable claim is allowed.')

$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8v-real-first-non-ff-transition-result.json -> $jsonPath"
Write-Host "map8v-real-first-non-ff-transition-result.md   -> $mdPath"
Write-Host "MAP_8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_PACKET.md -> $packetPath"
exit 0
