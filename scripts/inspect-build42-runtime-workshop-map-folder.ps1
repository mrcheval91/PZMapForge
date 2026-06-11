#Requires -Version 5.1
<#
.SYNOPSIS
    Read-only inventory of the candidate Workshop item map folder structure.
    Accepts operator-supplied Workshop item path. Must contain 3740642200.
    Refuses output outside .local/. Does not copy files. Does not dump binary contents.
    Records path, size, timestamp, and optional SHA-256 for binary-like files.
    Reads text content of: mod.info, map.info, spawnregions.lua, spawnpoints.lua, objects.lua.
    Produces runtime-workshop-map-folder-inventory.json and .md under .local/.
    Exits 0 on success, exits 1 on error.
.PARAMETER WorkshopItemPath
    Path to the Workshop item folder. Must contain '3740642200'.
.PARAMETER Output
    Output directory. Must be under .local/ in the repo.
#>

param(
    [string]$WorkshopItemPath = '',
    [string]$Output = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

if (-not $Output) {
    $Output = Join-Path $repoRoot '.local\map9c-runtime-workshop-inventory'
}

# .local/ guard
$localDir  = Join-Path $repoRoot '.local'
$absOutput = [System.IO.Path]::GetFullPath($Output)
$absLocal  = [System.IO.Path]::GetFullPath($localDir)
if (-not $absOutput.StartsWith($absLocal)) {
    Write-Error "inspect-build42-runtime-workshop-map-folder: output must be under .local/ in the repo. Refused: $Output"
    exit 1
}

$forbiddenOutput = @('Steam','workshop','ProjectZomboid','C:\Program Files','D:\Program Files')
foreach ($seg in $forbiddenOutput) {
    if ($absOutput -like "*$seg*") {
        Write-Error "inspect-build42-runtime-workshop-map-folder: forbidden path segment '$seg' in output: $Output"
        exit 1
    }
}

# Workshop item path guard
if (-not $WorkshopItemPath) {
    Write-Error "inspect-build42-runtime-workshop-map-folder: -WorkshopItemPath is required."
    exit 1
}
if ($WorkshopItemPath -notlike '*3740642200*') {
    Write-Error "inspect-build42-runtime-workshop-map-folder: -WorkshopItemPath must contain '3740642200'. Refused: $WorkshopItemPath"
    exit 1
}
if (-not (Test-Path $WorkshopItemPath -PathType Container)) {
    Write-Error "inspect-build42-runtime-workshop-map-folder: -WorkshopItemPath does not exist: $WorkshopItemPath"
    exit 1
}

$absWorkshop = [System.IO.Path]::GetFullPath($WorkshopItemPath)

# Forbidden write guard (writing anywhere under these paths is refused)
$forbiddenWrite = @('Steam','workshop','ProjectZomboid','media\maps','media/maps')
foreach ($seg in $forbiddenWrite) {
    if ($absOutput -like "*$seg*") {
        Write-Error "inspect-build42-runtime-workshop-map-folder: output cannot be under forbidden path '$seg'"
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

# Text file extensions (may read content)
$textExtensions  = @('.info', '.lua', '.xml', '.txt', '.json', '.md')
# Binary-like extensions (record metadata only)
$binaryExtensions = @('.bin', '.lotheader', '.lotpack', '.png', '.jpg')

function Get-FileHash256 {
    param([string]$Path)
    try {
        $h = Get-FileHash -LiteralPath $Path -Algorithm SHA256
        return $h.Hash.ToLower()
    } catch { return $null }
}

function Inventory-Files {
    param([string]$Root, [string[]]$NameFilter)
    $results = [System.Collections.ArrayList]::new()
    if (-not (Test-Path $Root -PathType Container)) { return @($results) }
    Get-ChildItem -Path $Root -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            if ($NameFilter.Count -eq 0) { $true }
            else { $NameFilter | Where-Object { $_ -eq $_.Name } }
        } |
        ForEach-Object {
            $ext = $_.Extension.ToLower()
            $entry = [ordered]@{
                name          = $_.Name
                relative_path = $_.FullName.Substring($absWorkshop.Length).TrimStart('\','/').Replace('\','/')
                size_bytes    = $_.Length
                last_write_utc = $_.LastWriteTimeUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
            }
            if ($binaryExtensions -contains $ext) {
                $entry.sha256 = Get-FileHash256 $_.FullName
                $entry.binary_content_dumped = $false
            }
            [void]$results.Add($entry)
        }
    return @($results)
}

function Find-Files {
    param([string]$Root, [string[]]$Names)
    $results = [System.Collections.ArrayList]::new()
    if (-not (Test-Path $Root -PathType Container)) { return @($results) }
    foreach ($name in $Names) {
        Get-ChildItem -Path $Root -Recurse -Filter $name -File -ErrorAction SilentlyContinue |
            ForEach-Object {
                $ext = $_.Extension.ToLower()
                $entry = [ordered]@{
                    name           = $_.Name
                    relative_path  = $_.FullName.Substring($absWorkshop.Length).TrimStart('\','/').Replace('\','/')
                    size_bytes     = $_.Length
                    last_write_utc = $_.LastWriteTimeUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
                }
                if ($textExtensions -contains $ext) {
                    try { $entry.text_content = [string](Get-Content -LiteralPath $_.FullName -Raw) }
                    catch { $entry.text_content = $null }
                } elseif ($binaryExtensions -contains $ext) {
                    $entry.sha256 = Get-FileHash256 $_.FullName
                    $entry.binary_content_dumped = $false
                }
                [void]$results.Add($entry)
            }
    }
    return @($results)
}

# Detect layout presence
$commonMediaMapsPath = Join-Path $absWorkshop 'common\media\maps'
$v42MediaMapsPath    = Join-Path $absWorkshop '42\media\maps'
$rootMediaMapsPath   = Join-Path $absWorkshop 'media\maps'

$candidateCommonMediaMapsFound = Test-Path $commonMediaMapsPath -PathType Container
$candidate42MediaMapsFound     = Test-Path $v42MediaMapsPath    -PathType Container
$candidateMediaMapsFound       = Test-Path $rootMediaMapsPath   -PathType Container

# Detect mod folder
$modFolderFound = (Test-Path (Join-Path $absWorkshop 'common\mod.info') -PathType Leaf) -or
                  (Test-Path (Join-Path $absWorkshop 'mod.info') -PathType Leaf)

# Collect text file inventories
$modInfoFiles       = @(Find-Files $absWorkshop @('mod.info'))
$mapInfoFiles       = @(Find-Files $absWorkshop @('map.info'))
$spawnregionsFiles  = @(Find-Files $absWorkshop @('spawnregions.lua'))
$spawnpointsFiles   = @(Find-Files $absWorkshop @('spawnpoints.lua'))
$objectsLuaFiles    = @(Find-Files $absWorkshop @('objects.lua'))

# Collect binary file inventories (metadata only)
$lotheaderFiles  = @(Find-Files $absWorkshop @('*.lotheader'))
$lotpackFiles    = @(Find-Files $absWorkshop @('*.lotpack'))
$chunkedataFiles = @(Find-Files $absWorkshop @('chunkdata_*.bin'))
$worldmapFiles   = @(Find-Files $absWorkshop @('worldmap.xml','worldmap.xml.bin','worldmap-forest.xml'))

# Map folder subfolder names
$mapFolderNames = [System.Collections.ArrayList]::new()
foreach ($base in @($commonMediaMapsPath,$v42MediaMapsPath,$rootMediaMapsPath)) {
    if (Test-Path $base -PathType Container) {
        Get-ChildItem -Path $base -Directory -ErrorAction SilentlyContinue |
            ForEach-Object { [void]$mapFolderNames.Add($_.Name) }
    }
}

$inventorySummary = [ordered]@{
    common_media_maps_present  = $candidateCommonMediaMapsFound
    versioned_42_media_maps_present = $candidate42MediaMapsFound
    legacy_media_maps_present  = $candidateMediaMapsFound
    mod_folder_found           = $modFolderFound
    map_folder_subfolders      = @($mapFolderNames)
    mod_info_count             = $modInfoFiles.Count
    map_info_count             = $mapInfoFiles.Count
    lotheader_count            = $lotheaderFiles.Count
    lotpack_count              = $lotpackFiles.Count
    chunkdata_count            = $chunkedataFiles.Count
    worldmap_count             = $worldmapFiles.Count
}

$registrationRiskSummary = [ordered]@{
    pzmapforge_folder_name_in_map_folders = ($mapFolderNames -contains 'PZMapForge')
    child_map_id_in_map_folders           = ($mapFolderNames -contains 'pzmapforge_build42_candidate_v4_001')
    spawnregions_present                  = ($spawnregionsFiles.Count -gt 0)
    lotheader_present                     = ($lotheaderFiles.Count -gt 0)
    map_info_present                      = ($mapInfoFiles.Count -gt 0)
    registration_risk_note                = 'IsoMetaGrid did not list PZMapForge in MAP-9B debug run; layout may need to match expected scan path'
}

$inventory = [ordered]@{
    schema                          = 'pzmapforge.map9c-runtime-workshop-map-folder-inventory.v0.1'
    generated_at_utc                = $generatedAt
    workshop_item_id                = '3740642200'
    workshop_item_path              = $WorkshopItemPath
    read_only                       = $true
    copied_files                    = $false
    binary_contents_dumped          = $false
    steam_write_performed           = $false
    pz_run_performed                = $false
    workshop_upload_performed       = $false
    candidate_mod_folder_found      = $modFolderFound
    candidate_common_media_maps_found = $candidateCommonMediaMapsFound
    candidate_42_media_maps_found   = $candidate42MediaMapsFound
    candidate_media_maps_found      = $candidateMediaMapsFound
    candidate_mod_info_files        = @($modInfoFiles)
    candidate_map_info_files        = @($mapInfoFiles)
    candidate_spawnregions_files    = @($spawnregionsFiles)
    candidate_spawnpoints_files     = @($spawnpointsFiles)
    candidate_objects_lua_files     = @($objectsLuaFiles)
    candidate_lotheader_files       = @($lotheaderFiles)
    candidate_lotpack_files         = @($lotpackFiles)
    candidate_chunkdata_files       = @($chunkedataFiles)
    candidate_worldmap_files        = @($worldmapFiles)
    map_folder_inventory_summary    = $inventorySummary
    registration_risk_summary       = $registrationRiskSummary
}

$jsonPath = Join-Path $Output 'runtime-workshop-map-folder-inventory.json'
$mdPath   = Join-Path $Output 'runtime-workshop-map-folder-inventory.md'

$inventory | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 -Path $jsonPath
Write-Output "Workshop inventory JSON: $jsonPath"

$md = @"
# MAP-9C Runtime Workshop Map Folder Inventory

Generated: $generatedAt
Workshop item: 3740642200
Path: $WorkshopItemPath

## Layout detection

- common/media/maps present: $candidateCommonMediaMapsFound
- 42/media/maps present:     $candidate42MediaMapsFound
- media/maps present:        $candidateMediaMapsFound

## Map folder subfolders

$($mapFolderNames | ForEach-Object { "- $_" } | Out-String)

## File counts

| Type | Count |
|---|---|
| mod.info | $($modInfoFiles.Count) |
| map.info | $($mapInfoFiles.Count) |
| spawnregions.lua | $($spawnregionsFiles.Count) |
| spawnpoints.lua | $($spawnpointsFiles.Count) |
| objects.lua | $($objectsLuaFiles.Count) |
| .lotheader | $($lotheaderFiles.Count) |
| .lotpack | $($lotpackFiles.Count) |
| chunkdata | $($chunkedataFiles.Count) |
| worldmap files | $($worldmapFiles.Count) |

## Registration risk

- PZMapForge in map folder subfolders: $($registrationRiskSummary.pzmapforge_folder_name_in_map_folders)
- Child map-id in map folder subfolders: $($registrationRiskSummary.child_map_id_in_map_folders)
- spawnregions present: $($registrationRiskSummary.spawnregions_present)
- lotheader present: $($registrationRiskSummary.lotheader_present)

## Safety

- read_only: $($inventory.read_only)
- copied_files: $($inventory.copied_files)
- binary_contents_dumped: $($inventory.binary_contents_dumped)
- steam_write_performed: $($inventory.steam_write_performed)
"@

$md | Set-Content -Encoding UTF8 -Path $mdPath
Write-Output "Workshop inventory MD:   $mdPath"
Write-Output "MAP9C: runtime workshop map folder inventory complete."
