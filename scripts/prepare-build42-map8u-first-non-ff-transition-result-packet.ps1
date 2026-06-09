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

$schema = 'pzmapforge.map8u-result.v0.1'

$result = [ordered]@{
    schema                                 = $schema
    operator_approved_first_non_ff_scan    = $true
    max_bytes_allowed                      = 65536
    string_pool_end_offset                 = 133
    scan_start_offset                      = 133
    binary_contents_read_scope             = 'first_65536_bytes_only'
    full_file_read                         = $false
    no_pz_run_by_claude                    = $true
    no_workshop_upload_by_claude           = $true
    full_format_understood                 = $false
    cell_index_understood                  = $false
    geometry_payload_understood            = $false
    writer_implementation_allowed          = $false
    binary_writer_gate_closed              = $true
    playable_claim_allowed                 = $false
    third_party_files_copied               = $false
    next_branch                            = 'igmb_transition_structure_analysis_pending_operator_approval_if_non_ff_found'
}

$jsonPath   = Join-Path $outDir 'map8u-first-non-ff-transition-result.json'
$mdPath     = Join-Path $outDir 'map8u-first-non-ff-transition-result.md'
$packetPath = Join-Path $outDir 'MAP_8U_FIRST_NON_FF_TRANSITION_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8U First Non-FF Transition Result')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add("operator_approved_first_non_ff_scan: $($result.operator_approved_first_non_ff_scan)")
[void]$mdLines.Add("max_bytes_allowed: $($result.max_bytes_allowed)")
[void]$mdLines.Add("string_pool_end_offset: $($result.string_pool_end_offset)")
[void]$mdLines.Add("scan_start_offset: $($result.scan_start_offset)")
[void]$mdLines.Add("full_format_understood: $($result.full_format_understood)")
[void]$mdLines.Add("cell_index_understood: $($result.cell_index_understood)")
[void]$mdLines.Add("geometry_payload_understood: $($result.geometry_payload_understood)")
[void]$mdLines.Add("writer_implementation_allowed: $($result.writer_implementation_allowed)")
[void]$mdLines.Add("binary_writer_gate_closed: $($result.binary_writer_gate_closed)")
[void]$mdLines.Add("playable_claim_allowed: $($result.playable_claim_allowed)")
[void]$mdLines.Add("third_party_files_copied: $($result.third_party_files_copied)")
[void]$mdLines.Add("no_pz_run_by_claude: $($result.no_pz_run_by_claude)")
[void]$mdLines.Add("no_workshop_upload_by_claude: $($result.no_workshop_upload_by_claude)")
[void]$mdLines.Add('')
[void]$mdLines.Add("next_branch: $($result.next_branch)")

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

$packetLines = [System.Collections.ArrayList]::new()
[void]$packetLines.Add('# MAP-8U First Non-FF Transition Result Packet')
[void]$packetLines.Add('')
[void]$packetLines.Add('```text')
[void]$packetLines.Add('MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED')
[void]$packetLines.Add('OPERATOR_APPROVED_FIRST_NON_FF_SCAN=true')
[void]$packetLines.Add('MAX_BYTES_ALLOWED=65536')
[void]$packetLines.Add('BINARY_WRITER_GATE_STILL_CLOSED')
[void]$packetLines.Add('PUBLIC_PLAYABLE_CLAIM_ALLOWED=false')
[void]$packetLines.Add('NO_PZ_RUN_BY_CLAUDE')
[void]$packetLines.Add('NO_WORKSHOP_UPLOAD_BY_CLAUDE')
[void]$packetLines.Add('NO_THIRD_PARTY_FILES_COPIED')
[void]$packetLines.Add('FULL_FORMAT_UNDERSTOOD=false')
[void]$packetLines.Add('CELL_INDEX_UNDERSTOOD=false')
[void]$packetLines.Add('BINARY_CONTENTS_FULL_READ=false')
[void]$packetLines.Add('```')
[void]$packetLines.Add('')
[void]$packetLines.Add("Schema: ``$schema``")
[void]$packetLines.Add('')
[void]$packetLines.Add('The operator approved a bounded first non-FF transition scan starting at')
[void]$packetLines.Add('string_pool_end_offset=133, reading at most 65536 bytes.')
[void]$packetLines.Add('Source basis: MAP-8T found all bytes 133-4095 are 0xFF.')
[void]$packetLines.Add('This packet records the approval. The operator runs the scanner.')
[void]$packetLines.Add('')
[void]$packetLines.Add('next_branch=igmb_transition_structure_analysis_pending_operator_approval_if_non_ff_found')
[void]$packetLines.Add('Binary writer gate remains CLOSED.')
[void]$packetLines.Add('No playable claim is allowed.')

$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8u-first-non-ff-transition-result.json -> $jsonPath"
Write-Host "map8u-first-non-ff-transition-result.md   -> $mdPath"
Write-Host "MAP_8U_FIRST_NON_FF_TRANSITION_RESULT_PACKET.md -> $packetPath"
exit 0
