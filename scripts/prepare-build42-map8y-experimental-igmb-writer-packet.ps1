#Requires -Version 5.1
<#
.SYNOPSIS
    Prepares the MAP-8Y experimental IGMB writer packet.
    Outputs packet JSON, MD, and the MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_PACKET.md sentinel doc.

.PARAMETER Output
    Required. Output directory. Must contain '.local'.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$absOutput = [System.IO.Path]::GetFullPath($Output)
if (-not $absOutput.Contains('.local')) {
    Write-Error "-Output must be under .local/. Got: $absOutput"
    exit 1
}

if (-not (Test-Path -LiteralPath $absOutput)) {
    New-Item -ItemType Directory -Path $absOutput -Force | Out-Null
}

$generatedAt = [System.DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')

$packet = [ordered]@{
    schema                          = 'pzmapforge.map8y-experimental-igmb-writer-packet.v0.1'
    generated_at_utc                = $generatedAt
    experimental_writer_added       = $true
    writes_worldmap_xml_bin         = $true
    output_scope                    = '.local_only'
    generated_from_scratch          = $true
    payload_kind                    = 'synthetic_pzmapforge_owned_u16le_pairs'
    payload_source                  = 'generated_from_scratch_not_copied'
    third_party_bytes_copied        = $false
    project_russia_file_read        = $false
    pz_run_performed                = $false
    workshop_upload_performed       = $false
    playable_claim_allowed          = $false
    load_test_performed             = $false
    writer_status                   = 'experimental_skeleton_not_load_proven'
    full_igmb_format_understood     = $false
    cell_index_understood           = $false
    geometry_payload_understood     = $false
    triplet_fields_proven           = $false
    transition_offset_used          = 6389
    string_pool_end_offset_used     = 133
    total_bytes_default             = 65536
    basis                           = 'observed_igmb_structure_map8q_through_map8x'
    next_branch                     = 'map8z_controlled_install_packet_pending_operator_approval'
}

$jsonPath = Join-Path $absOutput 'map8y-experimental-igmb-writer-packet.json'
$packet | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$mdLines = [System.Collections.ArrayList]::new()
[void]$mdLines.Add('# MAP-8Y Experimental IGMB Writer Packet')
[void]$mdLines.Add('')
[void]$mdLines.Add("Generated: $generatedAt")
[void]$mdLines.Add('')
[void]$mdLines.Add("schema: pzmapforge.map8y-experimental-igmb-writer-packet.v0.1")
[void]$mdLines.Add("experimental_writer_added: true")
[void]$mdLines.Add("writes_worldmap_xml_bin: true")
[void]$mdLines.Add("output_scope: .local_only")
[void]$mdLines.Add("writer_status: experimental_skeleton_not_load_proven")
[void]$mdLines.Add("playable_claim_allowed: false")
$mdPath = Join-Path $absOutput 'map8y-experimental-igmb-writer-packet.md'
[System.IO.File]::WriteAllLines($mdPath, $mdLines)

$packetDocLines = [System.Collections.ArrayList]::new()
[void]$packetDocLines.Add('# MAP-8Y Experimental IGMB Writer Skeleton Packet')
[void]$packetDocLines.Add('')
[void]$packetDocLines.Add('```text')
[void]$packetDocLines.Add('MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED')
[void]$packetDocLines.Add('EXPERIMENTAL_WRITER_LOCAL_ONLY')
[void]$packetDocLines.Add('WRITES_WORLDMAP_XML_BIN=true')
[void]$packetDocLines.Add('OUTPUT_SCOPE=.local_only')
[void]$packetDocLines.Add('WRITER_STATUS=experimental_skeleton_not_load_proven')
[void]$packetDocLines.Add('THIRD_PARTY_BYTES_COPIED=false')
[void]$packetDocLines.Add('PROJECT_RUSSIA_FILE_READ=false')
[void]$packetDocLines.Add('PZ_RUN_PERFORMED=false')
[void]$packetDocLines.Add('WORKSHOP_UPLOAD_PERFORMED=false')
[void]$packetDocLines.Add('PLAYABLE_CLAIM_ALLOWED=false')
[void]$packetDocLines.Add('LOAD_TEST_PERFORMED=false')
[void]$packetDocLines.Add('FULL_IGMB_FORMAT_UNDERSTOOD=false')
[void]$packetDocLines.Add('CELL_INDEX_UNDERSTOOD=false')
[void]$packetDocLines.Add('GEOMETRY_PAYLOAD_UNDERSTOOD=false')
[void]$packetDocLines.Add('TRIPLET_FIELDS_PROVEN=false')
[void]$packetDocLines.Add('PUBLIC_PLAYABLE_CLAIM_ALLOWED=false')
[void]$packetDocLines.Add('NO_PZ_RUN_BY_CLAUDE')
[void]$packetDocLines.Add('NO_WORKSHOP_UPLOAD_BY_CLAUDE')
[void]$packetDocLines.Add('NO_THIRD_PARTY_FILES_COPIED')
[void]$packetDocLines.Add('```')
[void]$packetDocLines.Add('')
[void]$packetDocLines.Add('next_branch=map8z_controlled_install_packet_pending_operator_approval')
$packetDocPath = Join-Path $absOutput 'MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_PACKET.md'
[System.IO.File]::WriteAllLines($packetDocPath, $packetDocLines)

Write-Output "OK: wrote map8y-experimental-igmb-writer-packet.json"
Write-Output "OK: wrote map8y-experimental-igmb-writer-packet.md"
Write-Output "OK: wrote MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_PACKET.md"
Write-Output "MAP8Y: experimental IGMB writer packet complete"
exit 0
