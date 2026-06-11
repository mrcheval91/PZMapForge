#Requires -Version 5.1
<#
.SYNOPSIS
    Writes the MAP-9B canary writer unblock packet (Outcome B) under .local/ only.
    Produces: map9b-canary-writer-unblock-packet.json,
              map9b-canary-writer-unblock-packet.md,
              MAP_9B_CANARY_WRITER_UNBLOCK_PACKET.md.
    Does not run Project Zomboid. Does not write to Steam, Workshop, or PZ folders.
    Does not copy third-party files. Does not claim playable export.
    Exits 0 on success, exits 1 on error.
.PARAMETER Output
    Output directory. Must be under .local/ in the repo.
#>

param(
    [string]$Output = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

if (-not $Output) {
    $Output = Join-Path $repoRoot '.local\map9b-canary-unblock-packet'
}

# .local/ guard
$localDir  = Join-Path $repoRoot '.local'
$absOutput = [System.IO.Path]::GetFullPath($Output)
$absLocal  = [System.IO.Path]::GetFullPath($localDir)
if (-not $absOutput.StartsWith($absLocal)) {
    Write-Error "prepare-build42-map9b-canary-writer-unblock-packet: output must be under .local/ in the repo. Refused: $Output"
    exit 1
}

$forbidden = @('media\maps','media/maps','Steam','workshop','ProjectZomboid','C:\Program Files','D:\Program Files')
foreach ($seg in $forbidden) {
    if ($absOutput -like "*$seg*") {
        Write-Error "prepare-build42-map9b-canary-writer-unblock-packet: forbidden path segment '$seg' in output: $Output"
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$packet = [ordered]@{
    schema                                     = 'pzmapforge.map9b-canary-writer-unblock-packet.v0.1'
    generated_at_utc                           = $generatedAt
    outcome                                    = 'B'
    outcome_label                              = 'canary_impossible_with_current_writer'
    canary_writer_available                    = $false
    canary_writer_blocked                      = $true
    visible_tile_encoding_supported            = $false
    canary_strategy_available                  = $false
    inspected_repo_only                        = $true
    pz_assets_read                             = $false
    lotp_chunk_payload_format_understood       = $false
    lotheader_tile_position_mapping_understood  = $false
    chunkdata_format_understood                = $false
    tile_placement_record_model_exists         = $false
    all_profiles_produce_same_tile_data        = $true
    profiles_inspected                         = @('empty_grass_v0','empty_grass_v1','empty_grass_v2','empty_grass_v3','empty_grass_v4')
    blockers                                   = @(
        'lotp_chunk_payload_format_not_understood',
        'lotheader_tile_table_visual_mapping_not_understood',
        'chunkdata_format_not_understood',
        'no_tile_placement_record_model'
    )
    playable_claim_allowed                     = $false
    pz_run_performed                           = $false
    workshop_upload_performed                  = $false
    steam_write_performed                      = $false
    third_party_files_copied                   = $false
    staged_output_local_only                   = $true
    next_research_branch                       = 'map9b_lotp_chunk_payload_format_research'
}

$jsonPath   = Join-Path $Output 'map9b-canary-writer-unblock-packet.json'
$mdPath     = Join-Path $Output 'map9b-canary-writer-unblock-packet.md'
$overlayDoc = Join-Path $Output 'MAP_9B_CANARY_WRITER_UNBLOCK_PACKET.md'

$packet | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8

$md = @"
# MAP-9B Canary Writer Unblock Packet

Schema:    pzmapforge.map9b-canary-writer-unblock-packet.v0.1
Generated: $generatedAt

## Outcome

**Outcome B -- canary impossible with current writer.**

outcome=B
canary_writer_available=false
canary_writer_blocked=true
visible_tile_encoding_supported=false

## Inspection scope

inspected_repo_only=true
pz_assets_read=false
pz_run_performed=false
workshop_upload_performed=false
steam_write_performed=false
third_party_files_copied=false

## Blockers

- lotp_chunk_payload_format_not_understood
- lotheader_tile_table_visual_mapping_not_understood
- chunkdata_format_not_understood
- no_tile_placement_record_model

## Safety

playable_claim_allowed=false
staged_output_local_only=true

## Next research branch

next_research_branch=map9b_lotp_chunk_payload_format_research
"@

Set-Content -Path $mdPath -Value $md -Encoding UTF8

$overlayContent = @"
# MAP-9B Canary Writer Unblock Packet

Schema:    pzmapforge.map9b-canary-writer-unblock-packet.v0.1
Generated: $generatedAt

## Classification

MAP9B_CANARY_WRITER_UNBLOCK_OUTCOME_B
OUTCOME=B
CANARY_WRITER_AVAILABLE=false
CANARY_WRITER_BLOCKED=true
VISIBLE_TILE_ENCODING_SUPPORTED=false
CANARY_STRATEGY_AVAILABLE=false
INSPECTED_REPO_ONLY=true
PZ_ASSETS_READ=false
PZ_RUN_PERFORMED=false
WORKSHOP_UPLOAD_PERFORMED=false
STEAM_WRITE_PERFORMED=false
THIRD_PARTY_FILES_COPIED=false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NEXT_RESEARCH_BRANCH=map9b_lotp_chunk_payload_format_research

## Blockers

- lotp_chunk_payload_format_not_understood
- lotheader_tile_table_visual_mapping_not_understood
- chunkdata_format_not_understood
- no_tile_placement_record_model

## Next step

Research the LOTP chunk payload format from a reference PZ map mod.
Identify tile ID encoding, tile position encoding, and lotheader-to-LOTP index mapping.
Implement a non-zero LOTP payload that encodes a known tile pattern.
No canary claim until a real visually distinctive test result is achieved.
"@

Set-Content -Path $overlayDoc -Value $overlayContent -Encoding UTF8

Write-Output "MAP-9B packet JSON: $jsonPath"
Write-Output "MAP-9B packet MD:   $mdPath"
Write-Output "MAP-9B overlay doc: $overlayDoc"
Write-Output "outcome:            $($packet.outcome)"
Write-Output "Status: OK"
exit 0
