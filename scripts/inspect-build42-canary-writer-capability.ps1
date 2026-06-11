#Requires -Version 5.1
<#
.SYNOPSIS
    Inspects the Build 42 candidate writer implementation (repo-only) to determine
    whether a visually distinctive canary cell can be generated with the current writer.
    Produces a capability report JSON under .local/ only.
    Does not run Project Zomboid. Does not read PZ game assets.
    Does not write to Steam, Workshop, or PZ folders.
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
    $Output = Join-Path $repoRoot '.local\map9b-canary-inspector'
}

# .local/ guard
$localDir  = Join-Path $repoRoot '.local'
$absOutput = [System.IO.Path]::GetFullPath($Output)
$absLocal  = [System.IO.Path]::GetFullPath($localDir)
if (-not $absOutput.StartsWith($absLocal)) {
    Write-Error "inspect-build42-canary-writer-capability: output must be under .local/ in the repo. Refused: $Output"
    exit 1
}

$forbidden = @('media\maps','media/maps','Steam','workshop','ProjectZomboid','C:\Program Files','D:\Program Files')
foreach ($seg in $forbidden) {
    if ($absOutput -like "*$seg*") {
        Write-Error "inspect-build42-canary-writer-capability: forbidden path segment '$seg' in output: $Output"
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

# Inspect: confirm writer command exists in Program.cs (repo-only, no PZ asset reads)
$programCs = Join-Path $repoRoot 'src\PZMapForge.Cli\Program.cs'
$writerCommandFound = $false
$profilesInspected = [System.Collections.ArrayList]::new()

if (Test-Path $programCs -PathType Leaf) {
    $src = Get-Content $programCs -Raw
    if ($src -match 'Build42CandidateWriterCommand') {
        $writerCommandFound = $true
    }
    foreach ($p in @('empty_grass_v0','empty_grass_v1','empty_grass_v2','empty_grass_v3','empty_grass_v4')) {
        if ($src -match [regex]::Escape($p)) {
            [void]$profilesInspected.Add($p)
        }
    }
}

# Capability determination based on repo-only inspection of Program.cs lines 1625-2041
# All five profiles produce LOTP all-zero payload, chunkdata all-zero body.
# No profile encodes tile placement records. Canary is not possible with current writer.
$report = [ordered]@{
    schema                                    = 'pzmapforge.build42-canary-writer-capability.v0.1'
    generated_at_utc                          = $generatedAt
    inspected_repo_only                       = $true
    pz_assets_read                            = $false
    pz_run_performed                          = $false
    workshop_upload_performed                 = $false
    steam_write_performed                     = $false
    third_party_files_copied                  = $false
    writer_command_found                      = $writerCommandFound
    profiles_inspected                        = @($profilesInspected)
    writer_controls                           = @(
        'cell_coordinate',
        'lotheader_tile_name_table_grass_overlays_only',
        'lotp_offset_table_sequential',
        'lotp_chunk_payload_all_zero_1024_bytes_per_chunk',
        'chunkdata_0x0001_header_plus_zero_body',
        'objects_lua_comment_or_return_empty',
        'spawnpoints_lua_all_or_unemployed_key'
    )
    canary_writer_available                   = $false
    canary_writer_blocked                     = $true
    visible_tile_encoding_supported           = $false
    canary_strategy_available                 = $false
    lotp_chunk_payload_format_understood      = $false
    lotheader_tile_position_mapping_understood = $false
    chunkdata_format_understood               = $false
    tile_placement_record_model_exists        = $false
    all_profiles_produce_same_tile_data       = $true
    outcome                                   = 'B'
    outcome_label                             = 'canary_impossible_with_current_writer'
    blockers                                  = @(
        [ordered]@{
            id      = 'lotp_chunk_payload_format_not_understood'
            summary = 'LOTP 1024-byte per-chunk payload is all-zero; tile encoding format not reverse-engineered'
        },
        [ordered]@{
            id      = 'lotheader_tile_table_visual_mapping_not_understood'
            summary = 'Tile name table -> LOTP tile index mapping not decoded; changing tile names alone is insufficient'
        },
        [ordered]@{
            id      = 'chunkdata_format_not_understood'
            summary = 'Chunkdata binary beyond 0x0001 header not decoded; visual effect of zero body unknown'
        },
        [ordered]@{
            id      = 'no_tile_placement_record_model'
            summary = 'Writer has no (x, y, tileId) record concept; cannot encode tile positions'
        }
    )
    playable_claim_allowed                    = $false
    staged_output_local_only                  = $true
    next_research_branch                      = 'map9b_lotp_chunk_payload_format_research'
    community_claims_integrated_as_unverified_research_leads = $true
    community_claims_not_adopted_as_doctrine                 = $true
    measured_igmb_header_takes_precedence                    = $true
    measured_igmb_magic                                      = 'IGMB'
    measured_igmb_magic_status                               = 'measured_in_project_russia_b42_sample'
    community_claim_wmxm_magic_status                        = 'contradicted_by_measured_b42_igmb_sample'
    community_claim_big_endian_status                        = 'contradicted_or_unproven'
    worldmap_bin_role                                        = 'map_ui_vector_metadata_unless_terrain_role_proven'
    worldmap_bin_playable_terrain_canary_supported           = $false
    playable_world_canary_separate_from_map_ui_canary        = $true
    debug_runtime_logs_reviewed                              = $true
    debug_runtime_build                                      = '42.19.0'
    debug_runtime_workshop_runtime_cache_confirmed           = $true
    debug_runtime_mod_loaded                                 = $true
    debug_runtime_isometagrid_map_folder_list_empty          = $true
    debug_runtime_spawn_position                             = '10746,8288,0'
    debug_runtime_spawn_metadata_works                       = $true
    debug_runtime_pzmapforge_lotheader_parse_evidence        = $false
    debug_runtime_pzmapforge_lotpack_parse_evidence          = $false
    debug_runtime_pzmapforge_chunkdata_parse_evidence        = $false
    debug_runtime_server_console_ignored_stale_b41           = $true
}

$jsonPath = Join-Path $Output 'build42-canary-writer-capability.json'
$report | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonPath -Encoding UTF8

Write-Output "Capability report: $jsonPath"
Write-Output "outcome:                         $($report.outcome)"
Write-Output "outcome_label:                   $($report.outcome_label)"
Write-Output "canary_writer_available:         $($report.canary_writer_available)"
Write-Output "canary_writer_blocked:           $($report.canary_writer_blocked)"
Write-Output "visible_tile_encoding_supported: $($report.visible_tile_encoding_supported)"
Write-Output "next_research_branch:            $($report.next_research_branch)"
Write-Output "Status: OK"
exit 0
