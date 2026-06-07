#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7T: Compares two Workshop payload roots to identify structural
    differences between a known-working and a non-working map mod.

    Reads only the two explicit roots provided by the operator.
    Does NOT crawl arbitrary Steam/PZ directories.
    Output is under .local/ only.

    Writes:
      <Output>/workshop-runtime-payload-comparison.json
      <Output>/workshop-runtime-payload-comparison.md

.PARAMETER CandidateWorkshopRoot
    Path to the PZMapForge candidate Workshop payload root.
    May be under .local/ or an explicit Steam Workshop content path.

.PARAMETER ReferenceWorkshopRoot
    Path to the Dru_map (or other reference) Workshop payload root.
    May be under .local/ or an explicit path.

.PARAMETER CandidateMapId
    Map/mod ID to check inside the candidate root.

.PARAMETER ReferenceMapId
    Map/mod ID to check inside the reference root.

.PARAMETER Output
    Must be under .local/. Receives JSON and MD comparison reports.

.EXAMPLE
    powershell -ExecutionPolicy Bypass `
        -File .\scripts\inspect-build42-workshop-runtime-payload.ps1 `
        -CandidateWorkshopRoot "D:\...\workshop\content\108600\3740642200" `
        -ReferenceWorkshopRoot ".\.local\map7m-packet\reference-known-working-map\Dru_map" `
        -CandidateMapId pzmapforge_build42_candidate_v4_001 `
        -ReferenceMapId Dru_map `
        -Output .\.local\map7t-comparison
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateWorkshopRoot,
    [Parameter(Mandatory=$true)][string]$ReferenceWorkshopRoot,
    [Parameter(Mandatory=$true)][string]$CandidateMapId,
    [Parameter(Mandatory=$true)][string]$ReferenceMapId,
    [Parameter(Mandatory=$true)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $Output '-Output'

New-Item -ItemType Directory -Force -Path $Output | Out-Null

Write-Output "Workshop runtime payload comparison"
Write-Output "Candidate: $CandidateWorkshopRoot"
Write-Output "Reference: $ReferenceWorkshopRoot"
Write-Output ""

# ---------------------------------------------------------------------------
# Layout inspection helper
# ---------------------------------------------------------------------------

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

function Get-PayloadLayout {
    param([string]$Root, [string]$MapId)

    $r = [ordered]@{
        root_exists                       = (Test-Path -LiteralPath $Root)
        root_mod_info_present             = $false
        versioned_42_mod_info_present     = $false
        common_mod_info_present           = $false
        mods_subdir_present               = $false
        contents_mods_subdir_present      = $false
        root_media_maps_present           = $false
        common_media_maps_present         = $false
        map_info_present                  = $false
        map_info_lots_is_none             = $false
        map_info_has_zoom_fields          = $false
        spawnpoints_lua_present           = $false
        objects_lua_present               = $false
        worldmap_xml_present              = $false
        worldmap_forest_xml_present       = $false
        biomemap_present                  = $false
        lotheader_files                   = [string[]]@()
        lotpack_files                     = [string[]]@()
        chunkdata_files                   = [string[]]@()
        bom_violations                    = [string[]]@()
        top_level_entries                 = [string[]]@()
        map_data_dir_found                = ''
    }

    if (-not $r.root_exists) { return $r }

    # Top-level entries
    $topItems = @(Get-ChildItem -LiteralPath $Root -ErrorAction SilentlyContinue)
    $r.top_level_entries = [string[]]@($topItems | ForEach-Object { $_.Name })

    # mod.info locations
    $r.root_mod_info_present         = Test-Path -LiteralPath (Join-Path $Root 'mod.info')
    $r.versioned_42_mod_info_present = Test-Path -LiteralPath (Join-Path $Root '42\mod.info')
    $r.common_mod_info_present       = Test-Path -LiteralPath (Join-Path $Root 'common\mod.info')

    # Subdirectory layouts
    $r.mods_subdir_present          = Test-Path -LiteralPath (Join-Path $Root "mods\$MapId")
    $r.contents_mods_subdir_present = Test-Path -LiteralPath (Join-Path $Root "Contents\mods\$MapId")

    # Media/maps paths
    $r.root_media_maps_present   = Test-Path -LiteralPath (Join-Path $Root "media\maps\$MapId")
    $r.common_media_maps_present = Test-Path -LiteralPath (Join-Path $Root "common\media\maps\$MapId")

    # Find map data directory (check candidate paths in order)
    $mapDataCandidates = @(
        (Join-Path $Root "common\media\maps\$MapId"),
        (Join-Path $Root "media\maps\$MapId"),
        (Join-Path $Root "42\media\maps\$MapId"),
        (Join-Path $Root "mods\$MapId\media\maps\$MapId"),
        (Join-Path $Root "Contents\mods\$MapId\media\maps\$MapId")
    )
    $mapDataDir = ''
    foreach ($d in $mapDataCandidates) {
        if (Test-Path -LiteralPath $d) { $mapDataDir = $d; break }
    }
    $r.map_data_dir_found = $mapDataDir

    if ($mapDataDir -ne '') {
        $r.map_info_present         = Test-Path -LiteralPath (Join-Path $mapDataDir 'map.info')
        $r.spawnpoints_lua_present  = Test-Path -LiteralPath (Join-Path $mapDataDir 'spawnpoints.lua')
        $r.objects_lua_present      = Test-Path -LiteralPath (Join-Path $mapDataDir 'objects.lua')
        $r.worldmap_xml_present     = Test-Path -LiteralPath (Join-Path $mapDataDir 'worldmap.xml')
        $r.worldmap_forest_xml_present = Test-Path -LiteralPath (Join-Path $mapDataDir 'worldmap-forest.xml')
        $r.biomemap_present         = Test-Path -LiteralPath (Join-Path $mapDataDir 'maps\biomemap_0_0.png')

        $mapInfoPath = Join-Path $mapDataDir 'map.info'
        if (Test-Path -LiteralPath $mapInfoPath) {
            $mi = [System.IO.File]::ReadAllText($mapInfoPath)
            $r.map_info_lots_is_none   = ($mi -match '(?m)^lots=NONE')
            $r.map_info_has_zoom_fields = ($mi -match '(?m)^zoomX=' -and $mi -match '(?m)^zoomY=')
        }
    }

    # Binary files (scan entire root, bounded)
    $lhFiles = @(Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.lotheader' -ErrorAction SilentlyContinue)
    $r.lotheader_files = [string[]]@($lhFiles | ForEach-Object { "$($_.Name) ($($_.Length) bytes)" })

    $lpFiles = @(Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.lotpack' -ErrorAction SilentlyContinue)
    $r.lotpack_files = [string[]]@($lpFiles | ForEach-Object { "$($_.Name) ($($_.Length) bytes)" })

    $cdFiles = @(Get-ChildItem -LiteralPath $Root -Recurse -Filter 'chunkdata_*.bin' -ErrorAction SilentlyContinue)
    $r.chunkdata_files = [string[]]@($cdFiles | ForEach-Object { "$($_.Name) ($($_.Length) bytes)" })

    # BOM checks
    $textPaths = @(
        (Join-Path $Root 'mod.info'),
        (Join-Path $Root '42\mod.info'),
        (Join-Path $Root 'common\mod.info')
    )
    if ($mapDataDir -ne '') {
        $textPaths += @(
            (Join-Path $mapDataDir 'map.info'),
            (Join-Path $mapDataDir 'spawnpoints.lua'),
            (Join-Path $mapDataDir 'objects.lua')
        )
    }
    $bomViol = [System.Collections.Generic.List[string]]::new()
    foreach ($tp in $textPaths) {
        if ((Test-Path -LiteralPath $tp) -and (Test-HasBom $tp)) {
            $bomViol.Add($tp)
        }
    }
    $r.bom_violations = [string[]]@($bomViol.ToArray())

    return $r
}

# ---------------------------------------------------------------------------
# Inspect both roots
# ---------------------------------------------------------------------------

Write-Output "Inspecting candidate: $CandidateWorkshopRoot"
$candidateLayout = Get-PayloadLayout -Root $CandidateWorkshopRoot -MapId $CandidateMapId

Write-Output "Inspecting reference: $ReferenceWorkshopRoot"
$referenceLayout = Get-PayloadLayout -Root $ReferenceWorkshopRoot -MapId $ReferenceMapId

# ---------------------------------------------------------------------------
# Comparison
# ---------------------------------------------------------------------------

$boolFields = @(
    'root_mod_info_present', 'versioned_42_mod_info_present', 'common_mod_info_present',
    'mods_subdir_present', 'contents_mods_subdir_present',
    'root_media_maps_present', 'common_media_maps_present',
    'map_info_present', 'map_info_lots_is_none', 'map_info_has_zoom_fields',
    'spawnpoints_lua_present', 'objects_lua_present',
    'worldmap_xml_present', 'worldmap_forest_xml_present', 'biomemap_present'
)

$fieldsRefHasCandLacks  = [System.Collections.Generic.List[string]]::new()
$fieldsCandHasRefLacks  = [System.Collections.Generic.List[string]]::new()
foreach ($f in $boolFields) {
    $cv = [bool]$candidateLayout[$f]
    $rv = [bool]$referenceLayout[$f]
    if ($rv -and -not $cv) { $fieldsRefHasCandLacks.Add($f) }
    if ($cv -and -not $rv) { $fieldsCandHasRefLacks.Add($f) }
}

$layoutMatch = ($fieldsRefHasCandLacks.Count -eq 0 -and $fieldsCandHasRefLacks.Count -eq 0)

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                          = 'pzmapforge.workshop-runtime-payload-comparison.v0.1'
    candidate_root                  = $CandidateWorkshopRoot
    reference_root                  = $ReferenceWorkshopRoot
    candidate_map_id                = $CandidateMapId
    reference_map_id                = $ReferenceMapId
    candidate                       = $candidateLayout
    reference                       = $referenceLayout
    comparison = [ordered]@{
        layout_match                         = $layoutMatch
        fields_in_reference_not_candidate    = [string[]]@($fieldsRefHasCandLacks.ToArray())
        fields_in_candidate_not_reference    = [string[]]@($fieldsCandHasRefLacks.ToArray())
        candidate_bom_violations_count       = @($candidateLayout.bom_violations).Count
        reference_bom_violations_count       = @($referenceLayout.bom_violations).Count
        candidate_lotheader_count            = @($candidateLayout.lotheader_files).Count
        reference_lotheader_count            = @($referenceLayout.lotheader_files).Count
    }
    public_playable_claim_allowed   = $false
    load_test_performed_by_script   = $false
    binary_writer_changed           = $false
}

$jsonPath = Join-Path $Output 'workshop-runtime-payload-comparison.json'
$mdPath   = Join-Path $Output 'workshop-runtime-payload-comparison.md'

$report | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence = '```'
function BoolIcon { param([bool]$v) if ($v) { return 'YES' } else { return 'no' } }

$md = @"
# Workshop Runtime Payload Comparison

## Roots

| | Path |
|---|---|
| Candidate | $CandidateWorkshopRoot |
| Reference | $ReferenceWorkshopRoot |

## Layout comparison

| Field | Candidate ($CandidateMapId) | Reference ($ReferenceMapId) |
|---|---|---|
| root_mod_info | $(BoolIcon $candidateLayout.root_mod_info_present) | $(BoolIcon $referenceLayout.root_mod_info_present) |
| 42/mod.info | $(BoolIcon $candidateLayout.versioned_42_mod_info_present) | $(BoolIcon $referenceLayout.versioned_42_mod_info_present) |
| common/mod.info | $(BoolIcon $candidateLayout.common_mod_info_present) | $(BoolIcon $referenceLayout.common_mod_info_present) |
| mods/<id> | $(BoolIcon $candidateLayout.mods_subdir_present) | $(BoolIcon $referenceLayout.mods_subdir_present) |
| Contents/mods/<id> | $(BoolIcon $candidateLayout.contents_mods_subdir_present) | $(BoolIcon $referenceLayout.contents_mods_subdir_present) |
| root media/maps | $(BoolIcon $candidateLayout.root_media_maps_present) | $(BoolIcon $referenceLayout.root_media_maps_present) |
| common/media/maps | $(BoolIcon $candidateLayout.common_media_maps_present) | $(BoolIcon $referenceLayout.common_media_maps_present) |
| map.info | $(BoolIcon $candidateLayout.map_info_present) | $(BoolIcon $referenceLayout.map_info_present) |
| lots=NONE | $(BoolIcon $candidateLayout.map_info_lots_is_none) | $(BoolIcon $referenceLayout.map_info_lots_is_none) |
| zoomX/Y | $(BoolIcon $candidateLayout.map_info_has_zoom_fields) | $(BoolIcon $referenceLayout.map_info_has_zoom_fields) |
| spawnpoints.lua | $(BoolIcon $candidateLayout.spawnpoints_lua_present) | $(BoolIcon $referenceLayout.spawnpoints_lua_present) |
| objects.lua | $(BoolIcon $candidateLayout.objects_lua_present) | $(BoolIcon $referenceLayout.objects_lua_present) |
| worldmap.xml | $(BoolIcon $candidateLayout.worldmap_xml_present) | $(BoolIcon $referenceLayout.worldmap_xml_present) |
| worldmap-forest.xml | $(BoolIcon $candidateLayout.worldmap_forest_xml_present) | $(BoolIcon $referenceLayout.worldmap_forest_xml_present) |

## Binary files

| | Candidate | Reference |
|---|---|---|
| lotheader count | $(@($candidateLayout.lotheader_files).Count) | $(@($referenceLayout.lotheader_files).Count) |
| lotpack count | $(@($candidateLayout.lotpack_files).Count) | $(@($referenceLayout.lotpack_files).Count) |
| chunkdata count | $(@($candidateLayout.chunkdata_files).Count) | $(@($referenceLayout.chunkdata_files).Count) |

## Fields in reference but not candidate

$(if ($fieldsRefHasCandLacks.Count -eq 0) { '(none)' } else { ($fieldsRefHasCandLacks | ForEach-Object { "- $_ " }) -join "`n" })

## Non-claims

${fence}text
public_playable_claim_allowed=false
load_test_performed_by_script=false
binary_writer_changed=false
${fence}
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD:   $mdPath"
Write-Output ""
Write-Output "layout_match:                $layoutMatch"
Write-Output "fields_ref_has_cand_lacks:   $($fieldsRefHasCandLacks.Count)"
Write-Output "candidate_lotheader_count:   $(@($candidateLayout.lotheader_files).Count)"
Write-Output "reference_lotheader_count:   $(@($referenceLayout.lotheader_files).Count)"
Write-Output "public_playable_claim_allowed=false"
Write-Output "Done."
