#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7O: Generates an experiment-I candidate using the Dru_map-aligned
    contract: root mod.info + 42/mod.info + NO common/mod.info +
    common/media/maps/<MapId>/ + map.info lots=NONE + zoomX/Y/S fields.

    All output under .local/ only.
    Does NOT copy files to PZ folders.
    Does NOT write outside .local/.
    Does NOT run PZ.
    Does NOT change LOTH/LOTP/chunkdata binary content.

.PARAMETER Output
    Required. Path under .local/ for packet output.

.PARAMETER MapId
    Map ID. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ModFolderName
    Mod folder name for reference. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ServerName
    Suggested server preset name. Default: PZMF_B42_METADATA_V4_VARIANT_I_001
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001',
    [string]$ServerName    = 'PZMF_B42_METADATA_V4_VARIANT_I_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

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

$fence = '```'

# ---------------------------------------------------------------------------
# Step 1: Generate empty_grass_v4 candidate
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v4 candidate..."

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $MapId `
    --output $candidateOut `
    --build42-candidate-writer `
    --build42-candidate-profile empty_grass_v4

if ($LASTEXITCODE -ne 0) {
    Write-Error "CLI candidate generation failed (exit $LASTEXITCODE)"
    exit 1
}

$candDir    = Join-Path $candidateOut ($MapId + '_build42_candidate')
$src42Dir   = Join-Path $candDir '42'
$srcMapData = Join-Path $src42Dir "media\maps\$MapId"

# ---------------------------------------------------------------------------
# Step 2: Build experiment-I candidate (Dru_map-aligned layout)
# ---------------------------------------------------------------------------

Write-Output "Building experiment-I candidate (Dru_map-aligned layout)..."

$expIBase         = Join-Path $Output ('experiment-i-candidate\' + $MapId)
$expI42Dir        = Join-Path $expIBase '42'
$expICommonDir    = Join-Path $expIBase 'common'
$expICommonMaps   = Join-Path $expICommonDir "media\maps\$MapId"
$expIMapsSubdir   = Join-Path $expICommonMaps 'maps'

New-Item -ItemType Directory -Force -Path $expI42Dir      | Out-Null
New-Item -ItemType Directory -Force -Path $expICommonMaps | Out-Null
New-Item -ItemType Directory -Force -Path $expIMapsSubdir | Out-Null

# root mod.info (from 42/mod.info, no-BOM)
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $expIBase 'mod.info') -Force
# 42/mod.info
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $expI42Dir 'mod.info') -Force
# NO common/mod.info (intentional -- Dru_map alignment)

# Map data files under common/media/maps/<MapId>/
$mapDataFiles = @('map.info', 'spawnpoints.lua', 'objects.lua',
                  '0_0.lotheader', 'world_0_0.lotpack', 'chunkdata_0_0.bin')
foreach ($f in $mapDataFiles) {
    $src = Join-Path $srcMapData $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $expICommonMaps $f) -Force
    }
}

# Modify map.info: lots=NONE, add zoomX/Y/S
$mapInfoPath = Join-Path $expICommonMaps 'map.info'
if (Test-Path -LiteralPath $mapInfoPath) {
    $mapInfoContent = [System.IO.File]::ReadAllText($mapInfoPath,
        [System.Text.UTF8Encoding]::new($false))
    # Fix lots= value
    $mapInfoContent = [regex]::Replace($mapInfoContent, '(?m)^lots=.*$', 'lots=NONE')
    # Add zoom fields if absent (metadata alignment placeholders -- see MAP-7N Dru_map comparison)
    # Ensure trailing newline before appending so each field starts on its own line.
    $mapInfoContent = $mapInfoContent.TrimEnd() + "`n"
    if ($mapInfoContent -notmatch '(?m)^zoomX=') { $mapInfoContent += "zoomX=0`n" }
    if ($mapInfoContent -notmatch '(?m)^zoomY=') { $mapInfoContent += "zoomY=0`n" }
    if ($mapInfoContent -notmatch '(?m)^zoomS=') { $mapInfoContent += "zoomS=1`n" }
    [System.IO.File]::WriteAllText($mapInfoPath, $mapInfoContent,
        [System.Text.UTF8Encoding]::new($false))
    Write-Output "  map.info: lots=NONE, zoomX/Y/S added"
}

# Placeholder files (thumb.png, worldmap*.xml, biomemap)
$thumbPath = Join-Path $expICommonMaps 'thumb.png'
try {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
    $bmp = New-Object System.Drawing.Bitmap(1, 1)
    $bmp.SetPixel(0, 0, [System.Drawing.Color]::White)
    $bmp.Save($thumbPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
} catch { [System.IO.File]::WriteAllBytes($thumbPath, [byte[]]@()) }

[System.IO.File]::WriteAllText((Join-Path $expICommonMaps 'worldmap.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $expICommonMaps 'worldmap-forest.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))

$biomemapPath = Join-Path $expIMapsSubdir 'biomemap_0_0.png'
try {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
    $bmp2 = New-Object System.Drawing.Bitmap(1, 1)
    $bmp2.SetPixel(0, 0, [System.Drawing.Color]::White)
    $bmp2.Save($biomemapPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp2.Dispose()
} catch { [System.IO.File]::WriteAllBytes($biomemapPath, [byte[]]@()) }

Write-Output "Experiment-I candidate: $expIBase"

# Verify no common/mod.info
$noCommonMod = -not (Test-Path -LiteralPath (Join-Path $expIBase 'common\mod.info'))
Write-Output "  no common/mod.info: $noCommonMod"

# Verify no-BOM on text files
function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}
$rootModNoBom  = -not (Test-HasBom (Join-Path $expIBase 'mod.info'))
$v42ModNoBom   = -not (Test-HasBom (Join-Path $expI42Dir 'mod.info'))
$mapNoBom      = -not (Test-HasBom $mapInfoPath)
Write-Output "  root/mod.info no-BOM: $rootModNoBom"
Write-Output "  42/mod.info no-BOM:   $v42ModNoBom"
Write-Output "  map.info no-BOM:      $mapNoBom"

# ---------------------------------------------------------------------------
# Step 3: Run discovery path inspector
# ---------------------------------------------------------------------------

$discoveryScript = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
Write-Output "Running discovery path inspector..."

& powershell -ExecutionPolicy Bypass -File $discoveryScript `
    -CandidateRoot $expIBase `
    -Output $Output `
    -MapId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Discovery path inspection failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Step 4: Run metadata contract inspector
# ---------------------------------------------------------------------------

$metaScript = Join-Path $repoRoot 'scripts\inspect-build42-map-metadata-contract.ps1'
Write-Output "Running metadata contract inspector..."

& powershell -ExecutionPolicy Bypass -File $metaScript `
    -CandidateRoot $expIBase `
    -Output $Output `
    -MapId $MapId `
    -ModId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Metadata contract inspection failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Step 5: Optionally run comparator against Dru_map if available
# ---------------------------------------------------------------------------

$druMapRef = Join-Path (Split-Path -Parent $Output) 'map7m-packet\reference-known-working-map\Dru_map'
$comparatorRun = $false
$comparatorSkippedReferenceMissing = $true

if (Test-Path -LiteralPath $druMapRef) {
    $comparatorScript = Join-Path $repoRoot 'scripts\inspect-build42-known-working-map-contract.ps1'
    $compOut = Join-Path $Output 'comparison-dru-map'
    New-Item -ItemType Directory -Force -Path $compOut | Out-Null
    Write-Output "Running comparator against Dru_map reference..."
    & powershell -ExecutionPolicy Bypass -File $comparatorScript `
        -CandidateRoot $expIBase `
        -ReferenceRoot $druMapRef `
        -Output $compOut `
        -MapId $MapId `
        -ReferenceMapId 'Dru_map' | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $comparatorRun = $true
        $comparatorSkippedReferenceMissing = $false
        Write-Output "Comparator: OK"
    }
} else {
    Write-Output "Comparator: skipped (Dru_map reference not found at $druMapRef)"
}

# Load inspector results
$discoveryJson = Join-Path $Output 'map-discovery-path-report.json'
$metaJson      = Join-Path $Output 'map-metadata-contract-report.json'
$disc = if (Test-Path $discoveryJson) { Get-Content $discoveryJson -Raw | ConvertFrom-Json } else { $null }
$meta = if (Test-Path $metaJson)      { Get-Content $metaJson      -Raw | ConvertFrom-Json } else { $null }

$hasDrumapAligned = if ($null -ne $disc) { [bool]$disc.has_drumap_aligned_layout } else { $false }
$riskLabel        = if ($null -ne $disc) { [string]$disc.map_folder_discovery_risk } else { 'UNKNOWN' }
$lotsIsNone       = if ($null -ne $meta) { [bool]$meta.map_info_lots_is_none } else { $false }
$hasZoomX         = if ($null -ne $meta) { [bool]$meta.map_info_has_zoomX    } else { $false }

# ---------------------------------------------------------------------------
# Packet files
# ---------------------------------------------------------------------------

# MAP_7O_DRUMAP_ALIGNED_PACKET.md
$packetMd = Join-Path $Output 'MAP_7O_DRUMAP_ALIGNED_PACKET.md'
Set-Content -Path $packetMd -Encoding ASCII -Value @"
# MAP-7O: Dru_map-Aligned Experiment I Packet

${fence}text
DRUMAP_ALIGNED_EXPERIMENT_I_PREPARED
has_drumap_aligned_layout=$($hasDrumapAligned.ToString().ToLower())
map_info_lots_is_none=$($lotsIsNone.ToString().ToLower())
map_info_has_zoomX=$($hasZoomX.ToString().ToLower())
map_folder_discovery_risk=$riskLabel
comparator_run=$($comparatorRun.ToString().ToLower())
comparator_skipped_reference_missing=$($comparatorSkippedReferenceMissing.ToString().ToLower())
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## Experiment I layout (Dru_map-aligned)

${fence}text
$MapId/
  mod.info              (root -- Dru_map alignment)
  42/mod.info
  common/media/maps/$MapId/
    map.info            (lots=NONE, zoomX=0, zoomY=0, zoomS=1)
    objects.lua, spawnpoints.lua (text, no-BOM)
    0_0.lotheader, world_0_0.lotpack, chunkdata_0_0.bin (binary, unchanged)
    thumb.png, worldmap.xml, worldmap-forest.xml (placeholders)
    maps/biomemap_0_0.png (placeholder)
NOTE: NO common/mod.info (Dru_map alignment)
${fence}

has_drumap_aligned_layout: $hasDrumapAligned
map_info_lots_is_none: $lotsIsNone
map_folder_discovery_risk: $riskLabel

## Success condition

${fence}text
Looking in these map folders:
  ...$MapId...
<End of map-folders list>
${fence}

map_folders_list_empty=false.

## Failure condition

MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY (still empty, same as A-H)

## Progress condition

MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING (map registered, lotheader next)

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@

# MAP_7O_EXPERIMENT_I_MANUAL_INSTALL_COMMANDS.md
$installMd = Join-Path $Output 'MAP_7O_EXPERIMENT_I_MANUAL_INSTALL_COMMANDS.md'
Set-Content -Path $installMd -Encoding ASCII -Value @"
# MAP-7O: Experiment I Manual Install Commands (HUMAN-ONLY)

## Candidate source

${fence}text
$expIBase
${fence}

Structure:
${fence}text
$MapId/
  mod.info              (root)
  42/mod.info
  common/media/maps/$MapId/  (map data -- Dru_map aligned)
${fence}

## Step 1: Delete old install (HUMAN-ONLY)

Delete existing $ModFolderName from PZ mods directory.

## Step 2: Copy candidate (HUMAN-ONLY)

${fence}powershell
Copy-Item -Recurse -Force `
  "$expIBase" `
  "C:\Users\YourUser\Zomboid\mods\$ModFolderName"
${fence}

Verify: destination has mod.info at ROOT and 42/mod.info. NO common/mod.info.

## Step 3: Server configuration (HUMAN-ONLY -- no-BOM UTF-8)

${fence}ini
Map=$MapId;Muldraugh, KY
Mods=$MapId
${fence}

Server: $ServerName

## Step 4: Launch and capture log

${fence}text
C:\Users\YourUser\Zomboid\Logs\*_DebugLog.txt -> .local\map7o-logs\DebugLog-variant-I.txt
${fence}

## Step 5: Analyze

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
  -LogPath .\.local\map7o-logs\DebugLog-variant-I.txt ``
  -Output .\.local\map7o-analysis\variant-I ``
  -ExpectedMapId $MapId ``
  -VariantLabel VariantI
${fence}

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7O_EXPERIMENT_I_LOG_CAPTURE_COMMANDS.md
$logMd = Join-Path $Output 'MAP_7O_EXPERIMENT_I_LOG_CAPTURE_COMMANDS.md'
Set-Content -Path $logMd -Encoding ASCII -Value @"
# MAP-7O: Experiment I Log Capture Commands

${fence}powershell
New-Item -ItemType Directory -Force -Path .\.local\map7o-logs | Out-Null
Copy-Item "C:\Users\YourUser\Zomboid\Logs\*_DebugLog.txt" ``
  .\.local\map7o-logs\DebugLog-variant-I.txt
${fence}

## Analyze

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
  -LogPath .\.local\map7o-logs\DebugLog-variant-I.txt ``
  -Output .\.local\map7o-analysis\variant-I ``
  -ExpectedMapId $MapId ``
  -VariantLabel VariantI
${fence}

## Expected success

${fence}text
Looking in these map folders:
  $MapId
<End of map-folders list>
classification: map_folders_list_empty=false
${fence}

## Distinguishing outcomes

Success: map_folders_list_empty=false
MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY: still empty (A-H pattern)
MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING: PROGRESS (lotheader next)

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7O_EXPERIMENT_I_MANUAL_RESULT.local-template.md
$resultMd = Join-Path $Output 'MAP_7O_EXPERIMENT_I_MANUAL_RESULT.local-template.md'
Set-Content -Path $resultMd -Encoding ASCII -Value @"
# MAP-7O: Experiment I Manual Result (local template)

Do not commit. Fill in after Experiment I retest.

## Metadata

Date:
PZ version:
Server: $ServerName
Map= line: Map=$MapId;Muldraugh, KY
Layout: Dru_map-aligned (root mod.info, NO common/mod.info, common/media/maps/)

## Pre-test checks (HUMAN-ONLY)

- [ ] root mod.info present (no common/mod.info!)
- [ ] 42/mod.info present
- [ ] common/media/maps/$MapId/ present
- [ ] map.info has lots=NONE (verify by opening file)
- [ ] map.info has zoomX, zoomY, zoomS
- [ ] Server preset updated (no-BOM UTF-8)
- [ ] Old log files deleted

## Map folder scan

${fence}text
(paste IsoMetaGrid scan section from DebugLog here)
${fence}

- Did $MapId appear? YES / NO
- map_folders_list_empty:

## Analyzer output

classification:
map_folders_list_empty:
map_folders_list_count:

## Result

SUCCESS: map_folders_list_empty=false
MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY: still empty
MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING: map registered, lotheader issue

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7O_DRUMAP_COMPARISON_SUMMARY.md
$compSummaryMd = Join-Path $Output 'MAP_7O_DRUMAP_COMPARISON_SUMMARY.md'
Set-Content -Path $compSummaryMd -Encoding ASCII -Value @"
# MAP-7O: Dru_map Comparison Summary

Comparator run: $($comparatorRun.ToString().ToLower())
Comparator skipped (reference missing): $($comparatorSkippedReferenceMissing.ToString().ToLower())

## Key findings (from MAP-7N comparison)

mod.info:
- Dru_map uses root mod.info + 42/mod.info (NOT common/mod.info)
- Candidate experiment-H used common/mod.info + 42/mod.info (NOT root)
- mod.info fields: no gap (candidate has all Dru_map fields plus extras)

map.info:
- Dru_map has zoomX, zoomY, zoomS that candidate lacked
- Dru_map uses lots=NONE; candidate used lots=<MapId> (wrong)
- Both have title, lots (value differs), fixed2x, description

Layout:
- Both use 42/ (not 42.0/)
- Both use common/media/maps/<MapId>/
- world_0_0.lotpack naming: same in both

## Untested combination tested by Experiment I

root mod.info + 42/mod.info + NO common/mod.info + common/media/maps/ +
lots=NONE + zoomX/Y/S -- the exact Dru_map layout.

This was never tested before (A-H had other layouts).

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# ---------------------------------------------------------------------------
# map7o-drumap-aligned-preflight.json
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $Output 'map7o-drumap-aligned-preflight.json'
$preflight = [ordered]@{
    schema                                = 'pzmapforge.map7o-drumap-aligned-preflight.v0.1'
    map_id                                = $MapId
    mod_folder_name                       = $ModFolderName
    server_name                           = $ServerName
    experiment_i_candidate                = $true
    drumap_aligned_layout                 = $true
    has_drumap_aligned_layout             = $hasDrumapAligned
    common_mod_info_absent                = $noCommonMod
    root_mod_info_present                 = (Test-Path -LiteralPath (Join-Path $expIBase 'mod.info'))
    v42_mod_info_present                  = (Test-Path -LiteralPath (Join-Path $expI42Dir 'mod.info'))
    map_info_lots_is_none                 = $lotsIsNone
    map_info_has_zoomX                    = $hasZoomX
    map_folder_discovery_risk             = $riskLabel
    comparator_run                        = $comparatorRun
    comparator_skipped_reference_missing  = $comparatorSkippedReferenceMissing
    suggested_map_line                    = "Map=$MapId;Muldraugh, KY"
    suggested_analyzer_expected_map_id    = $MapId
    suggested_analyzer_variant_label      = 'VariantI'
    binary_writer_not_changed             = $true
    all_pz_folder_writes_human_only       = $true
    load_test_not_performed               = $true
    public_playable_claim_allowed         = $false
}
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJson -Encoding UTF8
Write-Output "JSON: $preflightJson"

# map7o-drumap-aligned-preflight.md
$preflightMd = Join-Path $Output 'map7o-drumap-aligned-preflight.md'
Set-Content -Path $preflightMd -Encoding ASCII -Value @"
# MAP-7O Dru_map-Aligned Preflight

${fence}text
map_id=$MapId
experiment_i_candidate=true
drumap_aligned_layout=true
has_drumap_aligned_layout=$($hasDrumapAligned.ToString().ToLower())
common_mod_info_absent=$($noCommonMod.ToString().ToLower())
map_info_lots_is_none=$($lotsIsNone.ToString().ToLower())
map_info_has_zoomX=$($hasZoomX.ToString().ToLower())
comparator_run=$($comparatorRun.ToString().ToLower())
binary_writer_not_changed=true
all_pz_folder_writes_human_only=true
load_test_not_performed=true
public_playable_claim_allowed=false
${fence}

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@
Write-Output "MD: $preflightMd"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "MAP_7O_DRUMAP_ALIGNED_PACKET.md:              $(Test-Path $packetMd)"
Write-Output "MAP_7O_EXPERIMENT_I_MANUAL_INSTALL_COMMANDS:  $(Test-Path $installMd)"
Write-Output "MAP_7O_EXPERIMENT_I_LOG_CAPTURE_COMMANDS:     $(Test-Path $logMd)"
Write-Output "MAP_7O_EXPERIMENT_I_MANUAL_RESULT.local-template: $(Test-Path $resultMd)"
Write-Output "MAP_7O_DRUMAP_COMPARISON_SUMMARY.md:          $(Test-Path $compSummaryMd)"
Write-Output "map7o-drumap-aligned-preflight.json:          $(Test-Path $preflightJson)"
Write-Output "map7o-drumap-aligned-preflight.md:            $(Test-Path $preflightMd)"
Write-Output "map-discovery-path-report.json:               $(Test-Path $discoveryJson)"
Write-Output "map-discovery-path-report.md:                 $(Test-Path (Join-Path $Output 'map-discovery-path-report.md'))"
Write-Output "map-metadata-contract-report.json:            $(Test-Path $metaJson)"
Write-Output "map-metadata-contract-report.md:              $(Test-Path (Join-Path $Output 'map-metadata-contract-report.md'))"
Write-Output ""
Write-Output "DRUMAP_ALIGNED_EXPERIMENT_I_PREPARED"
Write-Output "has_drumap_aligned_layout=$hasDrumapAligned"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
