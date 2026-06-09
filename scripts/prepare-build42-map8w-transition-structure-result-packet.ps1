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

$schema = 'pzmapforge.map8w-result.v0.1'

$result = [ordered]@{
    schema                                         = $schema
    operator_approved_transition_structure_analysis = $true
    transition_offset                              = 6389
    max_bytes_allowed                              = 65536
    full_file_read                                 = $false
    binary_contents_read_scope                     = 'first_65536_bytes_only'
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
    next_branch                                    = 'igmb_transition_model_record_pending_operator_review'
}

$jsonPath   = Join-Path $outDir 'map8w-transition-structure-result.json'
$mdPath     = Join-Path $outDir 'map8w-transition-structure-result.md'
$packetPath = Join-Path $outDir 'MAP_8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8W Transition Structure Result')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add("operator_approved_transition_structure_analysis: $($result.operator_approved_transition_structure_analysis)")
[void]$mdLines.Add("transition_offset: $($result.transition_offset)")
[void]$mdLines.Add("max_bytes_allowed: $($result.max_bytes_allowed)")
[void]$mdLines.Add("full_file_read: $($result.full_file_read)")
[void]$mdLines.Add("transition_structure_understood: $($result.transition_structure_understood)")
[void]$mdLines.Add("full_format_understood: $($result.full_format_understood)")
[void]$mdLines.Add("cell_index_understood: $($result.cell_index_understood)")
[void]$mdLines.Add("geometry_payload_understood: $($result.geometry_payload_understood)")
[void]$mdLines.Add("binary_writer_gate_closed: $($result.binary_writer_gate_closed)")
[void]$mdLines.Add("playable_claim_allowed: $($result.playable_claim_allowed)")
[void]$mdLines.Add("third_party_files_copied: $($result.third_party_files_copied)")
[void]$mdLines.Add("no_pz_run_by_claude: $($result.no_pz_run_by_claude)")
[void]$mdLines.Add("no_workshop_upload_by_claude: $($result.no_workshop_upload_by_claude)")
[void]$mdLines.Add('')
[void]$mdLines.Add("next_branch: $($result.next_branch)")

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

$packetLines = [System.Collections.ArrayList]::new()
[void]$packetLines.Add('# MAP-8W IGMB Transition Structure Analysis Packet')
[void]$packetLines.Add('')
[void]$packetLines.Add('```text')
[void]$packetLines.Add('MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED')
[void]$packetLines.Add('TRANSITION_OFFSET=6389')
[void]$packetLines.Add('MAX_BYTES_ALLOWED=65536')
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
[void]$packetLines.Add('Operator approved bounded IGMB transition structure analysis around offset 6389.')
[void]$packetLines.Add('Inspector: scripts/inspect-build42-igmb-transition-structure.ps1')
[void]$packetLines.Add('Default params: -TransitionOffset 6389 -MaxBytes 65536')
[void]$packetLines.Add('Reads at most 65536 bytes. Does not copy source files.')
[void]$packetLines.Add('Observe-only analysis. No encoder. No writer. No playable claim.')
[void]$packetLines.Add('')
[void]$packetLines.Add('next_branch=igmb_transition_model_record_pending_operator_review')
[void]$packetLines.Add('Binary writer gate remains CLOSED.')
[void]$packetLines.Add('No playable claim is allowed.')

$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8w-transition-structure-result.json -> $jsonPath"
Write-Host "map8w-transition-structure-result.md   -> $mdPath"
Write-Host "MAP_8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_PACKET.md -> $packetPath"
exit 0
