#Requires -Version 5.1
<#
.SYNOPSIS
    Inspects the Build 42 repo (repo-only) to identify map folder registration
    candidates and hypotheses for making PZMapForge appear in the IsoMetaGrid
    map-folder scan.
    Produces a registration inspector report JSON and MD under .local/ only.
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
    $Output = Join-Path $repoRoot '.local\map9c-registration-inspector'
}

# .local/ guard
$localDir  = Join-Path $repoRoot '.local'
$absOutput = [System.IO.Path]::GetFullPath($Output)
$absLocal  = [System.IO.Path]::GetFullPath($localDir)
if (-not $absOutput.StartsWith($absLocal)) {
    Write-Error "inspect-build42-map-folder-registration: output must be under .local/ in the repo. Refused: $Output"
    exit 1
}

$forbidden = @('media\maps','media/maps','Steam','workshop','ProjectZomboid','C:\Program Files','D:\Program Files')
foreach ($seg in $forbidden) {
    if ($absOutput -like "*$seg*") {
        Write-Error "inspect-build42-map-folder-registration: forbidden path segment '$seg' in output: $Output"
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

# Repo-only inspection: locate relevant scripts and docs
$scriptsDir = Join-Path $repoRoot 'scripts'
$docsDir    = Join-Path $repoRoot 'docs'

$relatedDocs    = [System.Collections.ArrayList]::new()
$relatedScripts = [System.Collections.ArrayList]::new()

$docPatterns = @(
    'MAP_7M_','MAP_7N_','MAP_7O_','MAP_7P_','MAP_7Q_','MAP_7R_','MAP_7S_',
    'MAP_7T_','MAP_7U_','MAP_7V_','MAP_7W_','MAP_7X_','MAP_7Y_',
    'MAP_8','MAP_9'
)
foreach ($pat in $docPatterns) {
    Get-ChildItem -Path $docsDir -Filter "*.md" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "*$pat*" } |
        ForEach-Object { [void]$relatedDocs.Add($_.Name) }
}

$scriptPatterns = @(
    'inspect-build42-map','prepare-build42-map7','prepare-build42-map8','prepare-build42-map9',
    'inspect-build42-canary','inspect-build42-parent','inspect-build42-runtime'
)
foreach ($pat in $scriptPatterns) {
    Get-ChildItem -Path $scriptsDir -Filter "*.ps1" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "*$pat*" } |
        ForEach-Object { [void]$relatedScripts.Add($_.Name) }
}

# Check for known key registration-related terms in repo scripts/docs
$hasCommonMediaMaps   = $false
$has42MediaMaps       = $false
$hasMediaMapsRoot     = $false
$hasLotsField         = $false
$hasMapLine           = $false
$hasSpawnregions      = $false
$hasMapInfoId         = $false

$scriptFiles = Get-ChildItem -Path $scriptsDir -Filter "*.ps1" -ErrorAction SilentlyContinue
foreach ($f in $scriptFiles) {
    $content = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { continue }
    if ($content -match 'common.media.maps')       { $hasCommonMediaMaps = $true }
    if ($content -match '42.media.maps')            { $has42MediaMaps = $true }
    if ($content -match 'media.maps' -and -not ($content -match 'common.media.maps')) { $hasMediaMapsRoot = $true }
    if ($content -match 'lots=')                    { $hasLotsField = $true }
    if ($content -match 'Map=')                     { $hasMapLine = $true }
    if ($content -match 'spawnregions')             { $hasSpawnregions = $true }
    if ($content -match 'map\.info.*id=|id=.*map\.info') { $hasMapInfoId = $true }
}

$candidateMapFolderNames = @(
    'PZMapForge',
    'pzmapforge_build42_candidate_v4_001'
)

$candidateLayouts = @(
    'common/media/maps/PZMapForge',
    'common/media/maps/pzmapforge_build42_candidate_v4_001',
    '42/media/maps/PZMapForge',
    '42/media/maps/pzmapforge_build42_candidate_v4_001',
    'media/maps/PZMapForge'
)

$registrationHypotheses = @(
    'H1: IsoMetaGrid reads map folders from Workshop item common/media/maps/ subtree',
    'H2: Map= token value must match subfolder name under common/media/maps/',
    'H3: map.info id= field must match subfolder name for registration',
    'H4: Build 42 uses 42/ versioned subtree instead of root common/',
    'H5: lots= field in map.info required for IsoMetaGrid to list the folder',
    'H6: Missing spawnregions.lua prevents registration even if folder exists',
    'H7: IsoMetaGrid scans by parent map index -- not pure filesystem discovery'
)

$recommendedProbeOrder = @(
    'Variant A: common/media/maps/PZMapForge (current; verify folder name matches Map= token)',
    'Variant B: common/media/maps/pzmapforge_build42_candidate_v4_001 (child map-id match)',
    'Variant C: 42/media/maps/PZMapForge (Build 42 versioned layout)',
    'Variant D: 42/media/maps/pzmapforge_build42_candidate_v4_001 (versioned child map-id)',
    'Variant E: media/maps/PZMapForge (legacy root layout)'
)

$report = [ordered]@{
    schema                                         = 'pzmapforge.map9c-map-folder-registration-inspector.v0.1'
    generated_at_utc                               = $generatedAt
    inspected_repo_only                            = $true
    pz_assets_read                                 = $false
    steam_write_performed                          = $false
    workshop_upload_performed                      = $false
    pz_run_performed                               = $false
    third_party_files_copied                       = $false
    map9b_debug_blocker_recorded                   = $true
    mod_load_confirmed_by_debug_logs               = $true
    workshop_runtime_cache_confirmed_by_debug_logs = $true
    spawn_metadata_works                           = $true
    isometagrid_map_folder_list_empty              = $true
    current_server_map_line                        = 'Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY'
    muldraugh_bootstrap_required                   = $true
    no_muldraugh_strategy_rejected                 = $true
    common_media_maps_references_found             = $hasCommonMediaMaps
    versioned_42_media_maps_references_found       = $has42MediaMaps
    legacy_media_maps_root_references_found        = $hasMediaMapsRoot
    lots_field_references_found                    = $hasLotsField
    map_line_references_found                      = $hasMapLine
    spawnregions_references_found                  = $hasSpawnregions
    candidate_map_folder_names_considered          = $candidateMapFolderNames
    candidate_layouts_considered                   = $candidateLayouts
    registration_hypotheses                        = $registrationHypotheses
    recommended_probe_order                        = $recommendedProbeOrder
    related_docs_found                             = @($relatedDocs | Sort-Object -Unique)
    related_scripts_found                          = @($relatedScripts | Sort-Object -Unique)
    success_signal                                 = 'PZMapForge_or_candidate_folder_appears_in_IsoMetaGrid_map_folder_list'
    failure_signal                                 = 'IsoMetaGrid_map_folder_list_empty_or_only_vanilla'
    playable_claim_allowed                         = $false
    recommended_next_branch                        = 'map9c_isometagrid_registration_probe_human_runtime_test_pending'
}

$jsonPath = Join-Path $Output 'build42-map-folder-registration-inspector.json'
$mdPath   = Join-Path $Output 'build42-map-folder-registration-inspector.md'

$report | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 -Path $jsonPath
Write-Output "Registration inspector JSON: $jsonPath"

$md = @"
# MAP-9C Map Folder Registration Inspector

Generated: $generatedAt

## Blocker carried from MAP-9B

- mod_load_confirmed_by_debug_logs: $($report.mod_load_confirmed_by_debug_logs)
- isometagrid_map_folder_list_empty: $($report.isometagrid_map_folder_list_empty)
- spawn_metadata_works: $($report.spawn_metadata_works)

## Candidate layouts

$($candidateLayouts | ForEach-Object { "- $_" } | Out-String)

## Registration hypotheses

$($registrationHypotheses | ForEach-Object { "- $_" } | Out-String)

## Recommended probe order

$($recommendedProbeOrder | ForEach-Object { "- $_" } | Out-String)

## Safety

- inspected_repo_only: $($report.inspected_repo_only)
- pz_run_performed: $($report.pz_run_performed)
- playable_claim_allowed: $($report.playable_claim_allowed)
"@

$md | Set-Content -Encoding UTF8 -Path $mdPath
Write-Output "Registration inspector MD:   $mdPath"
Write-Output "MAP9C: map folder registration inspector complete."
