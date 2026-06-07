#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7H: Inspects a Build 42 candidate mod layout to diagnose why
    IsoMetaGrid does not discover the custom map folder.

    Checks for versioned (42/media/maps/) and root (media/maps/) layouts.
    Parses map.info and mod.info fields.
    Checks no-BOM status on game-read text files.
    Records structure hypothesis fields.

    Does NOT read PZ install assets.
    Does NOT write outside .local/.
    Does NOT run PZ.

.PARAMETER CandidateRoot
    Path to the candidate mod root (the folder that contains 42/mod.info
    or media/maps/ directly). May be under .local/ or repo.

.PARAMETER Output
    Path under .local/ for report output.

.PARAMETER MapId
    Map ID to inspect. Default: pzmapforge_build42_candidate_v4_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-map-discovery-path.ps1 `
        -CandidateRoot .\.local\map7h-packet\candidate\pzmapforge_build42_candidate_v4_001_build42_candidate `
        -Output .\.local\map7h-packet `
        -MapId pzmapforge_build42_candidate_v4_001
#>

param(
    [Parameter(Mandatory=$true)][string]$CandidateRoot,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId = 'pzmapforge_build42_candidate_v4_001'
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

$forbidden = @(
    'C:\Users\Palmacede\Zomboid',
    'C:\Users\Palmacede\Zomboid\mods',
    'C:\Users\Palmacede\Zomboid\Server',
    'media\maps'
)
foreach ($f in $forbidden) {
    if ($CandidateRoot -match [regex]::Escape($f)) {
        Write-Error "CandidateRoot must not reference PZ user or install paths: $CandidateRoot"
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

function Get-FileRecord {
    param([string]$Path, [bool]$IsText = $false)
    $exists = Test-Path -LiteralPath $Path
    $rec = [ordered]@{
        path   = $Path
        exists = $exists
        size   = if ($exists) { (Get-Item -LiteralPath $Path).Length } else { 0 }
    }
    if ($IsText) {
        $rec['has_bom'] = if ($exists) { Test-HasBom $Path } else { $false }
        $rec['no_bom']  = if ($exists) { -not (Test-HasBom $Path) } else { $false }
    }
    return $rec
}

function Parse-IniLike {
    param([string]$Path)
    $result = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Path)) { return $result }
    Get-Content -LiteralPath $Path | ForEach-Object {
        if ($_ -match '^(\w[\w\s]*?)\s*=\s*(.*)$') {
            $result[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
    return $result
}

# ---------------------------------------------------------------------------
# Define paths — versioned (42/) layout
# ---------------------------------------------------------------------------

$v42Dir          = Join-Path $CandidateRoot '42'
$v42ModInfo      = Join-Path $v42Dir 'mod.info'
$v42MediaMaps    = Join-Path $v42Dir "media\maps\$MapId"
$v42MapInfo      = Join-Path $v42MediaMaps 'map.info'
$v42SpawnPts     = Join-Path $v42MediaMaps 'spawnpoints.lua'
$v42Objects      = Join-Path $v42MediaMaps 'objects.lua'
$v42Lotheader    = Join-Path $v42MediaMaps '0_0.lotheader'
$v42Lotpack      = Join-Path $v42MediaMaps 'world_0_0.lotpack'
$v42Chunkdata    = Join-Path $v42MediaMaps 'chunkdata_0_0.bin'

# Define paths — root (non-versioned) layout
$rootModInfo     = Join-Path $CandidateRoot 'mod.info'
$rootMediaMaps   = Join-Path $CandidateRoot "media\maps\$MapId"
$rootMapInfo     = Join-Path $rootMediaMaps 'map.info'
$rootSpawnPts    = Join-Path $rootMediaMaps 'spawnpoints.lua'
$rootObjects     = Join-Path $rootMediaMaps 'objects.lua'
$rootLotheader   = Join-Path $rootMediaMaps '0_0.lotheader'

# ---------------------------------------------------------------------------
# Inspect versioned layout
# ---------------------------------------------------------------------------

Write-Output "Inspecting: $CandidateRoot"
Write-Output "MapId:      $MapId"

$v42Files = [ordered]@{
    mod_info         = Get-FileRecord $v42ModInfo   $true
    map_info         = Get-FileRecord $v42MapInfo   $true
    spawnpoints_lua  = Get-FileRecord $v42SpawnPts  $true
    objects_lua      = Get-FileRecord $v42Objects   $true
    lotheader        = Get-FileRecord $v42Lotheader $false
    lotpack          = Get-FileRecord $v42Lotpack   $false
    chunkdata        = Get-FileRecord $v42Chunkdata $false
}

$hasVersioned42MediaMaps = (Test-Path -LiteralPath $v42MediaMaps)
$has42ModInfo            = (Test-Path -LiteralPath $v42ModInfo)
$hasAllVersionedFiles    = ($v42Files.mod_info.exists -and
                            $v42Files.map_info.exists -and
                            $v42Files.spawnpoints_lua.exists -and
                            $v42Files.objects_lua.exists -and
                            $v42Files.lotheader.exists -and
                            $v42Files.lotpack.exists -and
                            $v42Files.chunkdata.exists)

# ---------------------------------------------------------------------------
# Inspect root layout
# ---------------------------------------------------------------------------

$rootFiles = [ordered]@{
    mod_info        = Get-FileRecord $rootModInfo   $true
    map_info        = Get-FileRecord $rootMapInfo   $true
    spawnpoints_lua = Get-FileRecord $rootSpawnPts  $true
    objects_lua     = Get-FileRecord $rootObjects   $true
    lotheader       = Get-FileRecord $rootLotheader $false
}

$hasRootModInfo   = (Test-Path -LiteralPath $rootModInfo)
$hasRootMediaMaps = (Test-Path -LiteralPath $rootMediaMaps)
$hasRootMapInfo   = (Test-Path -LiteralPath $rootMapInfo)

# ---------------------------------------------------------------------------
# Inspect common/ layout (Build 42 documented structure)
# Documented: <mod>/common/mod.info + <mod>/common/media/maps/<MapId>/
# ---------------------------------------------------------------------------

$commonDir              = Join-Path $CandidateRoot 'common'
$commonModInfo          = Join-Path $commonDir 'mod.info'
$commonMediaMaps        = Join-Path $commonDir "media\maps\$MapId"
$commonMapInfo          = Join-Path $commonMediaMaps 'map.info'
$commonMapsSubdir       = Join-Path $commonMediaMaps 'maps'
$commonWorldmapXml      = Join-Path $commonMediaMaps 'worldmap.xml'
$commonWorldmapForestXml = Join-Path $commonMediaMaps 'worldmap-forest.xml'
$commonThumbPng         = Join-Path $commonMediaMaps 'thumb.png'

$hasCommonModInfo            = Test-Path -LiteralPath $commonModInfo
$hasCommonMediaMaps          = Test-Path -LiteralPath $commonMediaMaps
$hasCommonMapInfo            = Test-Path -LiteralPath $commonMapInfo
$hasCommonMapsSubfolder      = Test-Path -LiteralPath $commonMapsSubdir
$hasCommonWorldmapXml        = Test-Path -LiteralPath $commonWorldmapXml
$hasCommonWorldmapForestXml  = Test-Path -LiteralPath $commonWorldmapForestXml
$hasCommonThumbPng           = Test-Path -LiteralPath $commonThumbPng
$hasCommonBiomemapFolder     = $hasCommonMapsSubfolder

# Lotpack naming convention detection
# Build 42 documented: world_X_Y.lotpack (e.g. world_0_0.lotpack)
# Legacy: X_Y.lotpack (e.g. 0_0.lotpack)
$hasWorldXYLotpackPattern = (Test-Path -LiteralPath (Join-Path $commonMediaMaps  'world_0_0.lotpack')) -or
                            (Test-Path -LiteralPath (Join-Path $v42MediaMaps      'world_0_0.lotpack')) -or
                            (Test-Path -LiteralPath (Join-Path $rootMediaMaps     'world_0_0.lotpack'))
$hasPlainXYLotpackPattern = (Test-Path -LiteralPath (Join-Path $commonMediaMaps  '0_0.lotpack')) -or
                            (Test-Path -LiteralPath (Join-Path $v42MediaMaps      '0_0.lotpack')) -or
                            (Test-Path -LiteralPath (Join-Path $rootMediaMaps     '0_0.lotpack'))

$commonMediaMapsRecommended = -not $hasCommonMediaMaps

# ---------------------------------------------------------------------------
# Parse map.info and mod.info content
# ---------------------------------------------------------------------------

$v42MapInfoFields  = Parse-IniLike $v42MapInfo
$v42ModInfoFields  = Parse-IniLike $v42ModInfo
$rootMapInfoFields = Parse-IniLike $rootMapInfo
$rootModInfoFields = Parse-IniLike $rootModInfo

# ---------------------------------------------------------------------------
# Discovery risk analysis
# ---------------------------------------------------------------------------

$rootModInfoMissing              = -not $hasRootModInfo
$rootMediaMapsMissing            = -not $hasRootMediaMaps
$hasDualModInfoLayout            = $has42ModInfo -and $hasRootModInfo
$hasDualMediaMapsLayout          = $hasVersioned42MediaMaps -and $hasRootMediaMaps
$possibleVersionLayerNotScanned  = $hasVersioned42MediaMaps -and (-not $hasRootMediaMaps)

# Variant D finding: root media/maps without root mod.info is insufficient.
$experimentDRootMediaMapsResult  =
    if    ($hasRootMediaMaps -and $hasRootModInfo)  { 'HAS_BOTH_ROOT_LAYOUTS' }
    elseif ($hasRootMediaMaps -and -not $hasRootModInfo) { 'ROOT_MEDIA_MAPS_WITHOUT_ROOT_MOD_INFO_INSUFFICIENT' }
    else  { 'NO_ROOT_MEDIA_MAPS' }

$experimentERootModInfoRecommended = -not $hasRootModInfo

$mapFolderDiscoveryRisk =
    if    ($hasCommonMediaMaps -and $hasCommonModInfo)           { 'LOWER_COMMON_FULL_LAYOUT' }
    elseif ($hasDualMediaMapsLayout -and $hasDualModInfoLayout)  { 'LOWER_DUAL_FULL_LAYOUT' }
    elseif ($hasRootMediaMaps -and -not $hasRootModInfo)         { 'ROOT_MEDIA_MAPS_WITHOUT_ROOT_MOD_INFO_INSUFFICIENT' }
    elseif ($possibleVersionLayerNotScanned)                     { 'HIGH_42_VERSION_LAYER_MAY_NOT_BE_SCANNED' }
    elseif ($hasRootMediaMaps)                                   { 'LOWER_ROOT_MEDIA_MAPS_PRESENT' }
    else                                                         { 'UNKNOWN' }

# Variant G and A-G exhaustion constants
$variantGResult         = 'MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY'
$variantsAbcdefgExhausted = $true

# ---------------------------------------------------------------------------
# Build report
# ---------------------------------------------------------------------------

$report = [ordered]@{
    schema                                         = 'pzmapforge.build42-map-discovery-path.v0.3'
    candidate_root                                 = $CandidateRoot
    map_id                                         = $MapId
    has_versioned_42_media_maps                    = $hasVersioned42MediaMaps
    has_42_mod_info                                = $has42ModInfo
    has_all_versioned_files                        = $hasAllVersionedFiles
    has_root_mod_info                              = $hasRootModInfo
    has_root_media_maps                            = $hasRootMediaMaps
    has_root_map_info                              = $hasRootMapInfo
    has_dual_mod_info_layout                       = $hasDualModInfoLayout
    has_dual_media_maps_layout                     = $hasDualMediaMapsLayout
    root_mod_info_missing                          = $rootModInfoMissing
    root_media_maps_missing                        = $rootMediaMapsMissing
    possible_build42_version_layer_not_scanned_by_isometagrid = $possibleVersionLayerNotScanned
    experiment_d_root_media_maps_result            = $experimentDRootMediaMapsResult
    experiment_e_root_mod_info_recommended         = $experimentERootModInfoRecommended
    has_common_mod_info                            = $hasCommonModInfo
    has_common_media_maps                          = $hasCommonMediaMaps
    has_common_map_info                            = $hasCommonMapInfo
    has_common_maps_subfolder                      = $hasCommonMapsSubfolder
    has_common_worldmap_xml                        = $hasCommonWorldmapXml
    has_common_worldmap_forest_xml                 = $hasCommonWorldmapForestXml
    has_common_thumb_png                           = $hasCommonThumbPng
    has_common_biomemap_folder                     = $hasCommonBiomemapFolder
    has_world_xy_lotpack_pattern                   = $hasWorldXYLotpackPattern
    has_plain_xy_lotpack_pattern                   = $hasPlainXYLotpackPattern
    common_media_maps_recommended                  = $commonMediaMapsRecommended
    variant_g_result                               = $variantGResult
    variants_abcdefg_exhausted                     = $variantsAbcdefgExhausted
    map_folder_discovery_risk                      = $mapFolderDiscoveryRisk
    versioned_42_files                             = $v42Files
    root_files                                     = $rootFiles
    v42_map_info_fields                            = $v42MapInfoFields
    v42_mod_info_fields                            = $v42ModInfoFields
    root_map_info_fields                           = $rootMapInfoFields
    root_mod_info_fields                           = $rootModInfoFields
    public_playable_claim_allowed                  = $false
    load_test_not_performed                        = $true
    pz_assets_read                                 = $false
}

$jsonPath = Join-Path $Output 'map-discovery-path-report.json'
$mdPath   = Join-Path $Output 'map-discovery-path-report.md'

$report | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonPath -Encoding UTF8
Write-Output "JSON: $jsonPath"

# ---------------------------------------------------------------------------
# Markdown
# ---------------------------------------------------------------------------

$fence = '```'
$md = @"
# MAP-7H/7I Build 42 Map Discovery Path Inspection

${fence}text
map_id=$MapId
has_versioned_42_media_maps=$($hasVersioned42MediaMaps.ToString().ToLower())
has_root_media_maps=$($hasRootMediaMaps.ToString().ToLower())
has_root_mod_info=$($hasRootModInfo.ToString().ToLower())
has_dual_mod_info_layout=$($hasDualModInfoLayout.ToString().ToLower())
has_dual_media_maps_layout=$($hasDualMediaMapsLayout.ToString().ToLower())
root_mod_info_missing=$($rootModInfoMissing.ToString().ToLower())
root_media_maps_missing=$($rootMediaMapsMissing.ToString().ToLower())
has_common_mod_info=$($hasCommonModInfo.ToString().ToLower())
has_common_media_maps=$($hasCommonMediaMaps.ToString().ToLower())
has_common_map_info=$($hasCommonMapInfo.ToString().ToLower())
has_world_xy_lotpack_pattern=$($hasWorldXYLotpackPattern.ToString().ToLower())
common_media_maps_recommended=$($commonMediaMapsRecommended.ToString().ToLower())
variant_g_result=$variantGResult
variants_abcdefg_exhausted=$($variantsAbcdefgExhausted.ToString().ToLower())
map_folder_discovery_risk=$mapFolderDiscoveryRisk
public_playable_claim_allowed=false
load_test_not_performed=true
${fence}

## Versioned layout (42/)

| File | Exists | Size | No-BOM |
|---|---|---|---|
| mod.info | $($v42Files.mod_info.exists) | $($v42Files.mod_info.size) | $($v42Files.mod_info.no_bom) |
| map.info | $($v42Files.map_info.exists) | $($v42Files.map_info.size) | $($v42Files.map_info.no_bom) |
| spawnpoints.lua | $($v42Files.spawnpoints_lua.exists) | $($v42Files.spawnpoints_lua.size) | $($v42Files.spawnpoints_lua.no_bom) |
| objects.lua | $($v42Files.objects_lua.exists) | $($v42Files.objects_lua.size) | $($v42Files.objects_lua.no_bom) |
| 0_0.lotheader | $($v42Files.lotheader.exists) | $($v42Files.lotheader.size) | n/a |
| world_0_0.lotpack | $($v42Files.lotpack.exists) | $($v42Files.lotpack.size) | n/a |
| chunkdata_0_0.bin | $($v42Files.chunkdata.exists) | $($v42Files.chunkdata.size) | n/a |

## Root layout (non-versioned)

| File | Exists | Size |
|---|---|---|
| mod.info | $($rootFiles.mod_info.exists) | $($rootFiles.mod_info.size) |
| media/maps/$MapId/map.info | $($rootFiles.map_info.exists) | $($rootFiles.map_info.size) |
| media/maps/$MapId/spawnpoints.lua | $($rootFiles.spawnpoints_lua.exists) | $($rootFiles.spawnpoints_lua.size) |
| media/maps/$MapId/objects.lua | $($rootFiles.objects_lua.exists) | $($rootFiles.objects_lua.size) |
| media/maps/$MapId/0_0.lotheader | $($rootFiles.lotheader.exists) | $($rootFiles.lotheader.size) |

## Discovery risk

$mapFolderDiscoveryRisk

possible_build42_version_layer_not_scanned_by_isometagrid: $possibleVersionLayerNotScanned
root_media_maps_missing: $rootMediaMapsMissing

## Non-claims

- No load test was performed by this script.
- public_playable_claim_allowed=false: binding.
"@

Set-Content -Path $mdPath -Value $md -Encoding ASCII
Write-Output "MD: $mdPath"
Write-Output ""
Write-Output "has_versioned_42_media_maps:  $hasVersioned42MediaMaps"
Write-Output "has_root_media_maps:          $hasRootMediaMaps"
Write-Output "root_media_maps_missing:      $rootMediaMapsMissing"
Write-Output "map_folder_discovery_risk:    $mapFolderDiscoveryRisk"
Write-Output "public_playable_claim_allowed=false"
Write-Output "Done."
