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

$schema = 'pzmapforge.map8s-result.v0.1'

$result = [ordered]@{
    schema                                         = $schema
    operator_approved_cell_index_boundary_research = $true
    string_pool_end_offset                         = 133
    max_bytes_allowed                              = 4096
    full_file_read                                 = $false
    binary_contents_read_scope                     = 'first_4096_bytes_only'
    focus_region                                   = 'after_string_pool_offset_133'
    full_format_understood                         = $false
    cell_index_understood                          = $false
    geometry_payload_understood                    = $false
    writer_implementation_allowed                  = $false
    binary_writer_gate_closed                      = $true
    playable_claim_allowed                         = $false
    third_party_files_copied                       = $false
    no_pz_run_by_claude                            = $true
    no_workshop_upload_by_claude                   = $true
    next_branch                                    = 'igmb_cell_index_model_research_pending_operator_approval_if_boundary_evidence_sufficient'
}

$jsonPath   = Join-Path $outDir 'map8s-cell-boundary-result.json'
$mdPath     = Join-Path $outDir 'map8s-cell-boundary-result.md'
$packetPath = Join-Path $outDir 'MAP_8S_CELL_BOUNDARY_RESULT_PACKET.md'

$result | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $jsonPath

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8S Cell Boundary Result')
[void]$mdLines.Add('')
[void]$mdLines.Add("Schema: ``$schema``")
[void]$mdLines.Add('')
[void]$mdLines.Add('## Approval scope')
[void]$mdLines.Add('')
[void]$mdLines.Add("operator_approved_cell_index_boundary_research: $($result.operator_approved_cell_index_boundary_research)")
[void]$mdLines.Add("string_pool_end_offset: $($result.string_pool_end_offset)")
[void]$mdLines.Add("max_bytes_allowed: $($result.max_bytes_allowed)")
[void]$mdLines.Add("full_file_read: $($result.full_file_read)")
[void]$mdLines.Add("binary_contents_read_scope: $($result.binary_contents_read_scope)")
[void]$mdLines.Add("focus_region: $($result.focus_region)")
[void]$mdLines.Add('')
[void]$mdLines.Add('## Format understanding')
[void]$mdLines.Add('')
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
[void]$mdLines.Add("no_pz_run_by_claude: $($result.no_pz_run_by_claude)")
[void]$mdLines.Add("no_workshop_upload_by_claude: $($result.no_workshop_upload_by_claude)")
[void]$mdLines.Add('')
[void]$mdLines.Add("next_branch: $($result.next_branch)")

$mdLines | Set-Content -Encoding UTF8 -Path $mdPath

$packetLines = [System.Collections.ArrayList]::new()
[void]$packetLines.Add('# MAP-8S Cell Boundary Result Packet')
[void]$packetLines.Add('')
[void]$packetLines.Add('```text')
[void]$packetLines.Add('MAP8S_CELL_BOUNDARY_RESEARCH_DEFINED')
[void]$packetLines.Add('OPERATOR_APPROVED_CELL_INDEX_BOUNDARY_RESEARCH=true')
[void]$packetLines.Add('MAX_BYTES_ALLOWED=4096')
[void]$packetLines.Add('BINARY_CONTENTS_FULL_READ=false')
[void]$packetLines.Add('THIRD_PARTY_FILES_COPIED=false')
[void]$packetLines.Add('BINARY_WRITER_GATE_STILL_CLOSED')
[void]$packetLines.Add('PUBLIC_PLAYABLE_CLAIM_ALLOWED=false')
[void]$packetLines.Add('NO_PZ_RUN_BY_CLAUDE')
[void]$packetLines.Add('NO_WORKSHOP_UPLOAD_BY_CLAUDE')
[void]$packetLines.Add('NO_THIRD_PARTY_FILES_COPIED')
[void]$packetLines.Add('CONFIDENCE_LEVEL=low')
[void]$packetLines.Add('```')
[void]$packetLines.Add('')
[void]$packetLines.Add("Schema: ``$schema``")
[void]$packetLines.Add('')
[void]$packetLines.Add('This packet records the approval metadata for MAP-8S cell index')
[void]$packetLines.Add('boundary research. The operator approved bounded inspection of bytes')
[void]$packetLines.Add('after string_pool_end_offset=133 within the first 4096 bytes.')
[void]$packetLines.Add('')
[void]$packetLines.Add('The cell index structure beyond the string pool is NOT confirmed.')
[void]$packetLines.Add('Inspector observations require the operator to run the inspector')
[void]$packetLines.Add('against the reference worldmap.xml.bin and record the result.')
[void]$packetLines.Add('')
[void]$packetLines.Add('Binary writer gate remains CLOSED.')
[void]$packetLines.Add('No playable claim is allowed.')

$packetLines | Set-Content -Encoding UTF8 -Path $packetPath

Write-Host "map8s-cell-boundary-result.json  -> $jsonPath"
Write-Host "map8s-cell-boundary-result.md    -> $mdPath"
Write-Host "MAP_8S_CELL_BOUNDARY_RESULT_PACKET.md -> $packetPath"
exit 0
