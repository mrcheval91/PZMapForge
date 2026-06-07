#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7L: Generates an experiment-H candidate using the documented Build 42
    common/media/maps layout and produces a diagnostic packet.

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
    Suggested server preset name. Default: PZMF_B42_METADATA_V4_VARIANT_H_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\prepare-build42-map7l-common-layout-experiment-packet.ps1 `
        -Output .\.local\map7l-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001',
    [string]$ServerName    = 'PZMF_B42_METADATA_V4_VARIANT_H_001'
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

# Record binary file sizes before copy (SHA-256 to confirm content unchanged)
function Get-FileSha256 {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $st  = [System.IO.File]::OpenRead($Path)
    try { $h = $sha.ComputeHash($st); return (($h | ForEach-Object { $_.ToString('x2') }) -join '') }
    finally { $st.Dispose(); $sha.Dispose() }
}

$srcLothPath     = Join-Path $srcMapData '0_0.lotheader'
$srcLotpPath     = Join-Path $srcMapData 'world_0_0.lotpack'
$srcChunkPath    = Join-Path $srcMapData 'chunkdata_0_0.bin'
$srcLothSha      = Get-FileSha256 $srcLothPath
$srcLotpSha      = Get-FileSha256 $srcLotpPath
$srcChunkSha     = Get-FileSha256 $srcChunkPath
$srcLothSize     = if (Test-Path $srcLothPath)  { (Get-Item $srcLothPath).Length  } else { 0 }
$srcLotpSize     = if (Test-Path $srcLotpPath)  { (Get-Item $srcLotpPath).Length  } else { 0 }
$srcChunkSize    = if (Test-Path $srcChunkPath) { (Get-Item $srcChunkPath).Length } else { 0 }

# ---------------------------------------------------------------------------
# Step 2: Build experiment-H candidate (common/ layout)
# ---------------------------------------------------------------------------

Write-Output "Building experiment-H candidate (common/media/maps layout)..."

$expHBase         = Join-Path $Output ('experiment-h-candidate\' + $MapId)
$expH42Dir        = Join-Path $expHBase '42'
$expHCommonDir    = Join-Path $expHBase 'common'
$expHCommonMaps   = Join-Path $expHCommonDir "media\maps\$MapId"
$expHMapsSubdir   = Join-Path $expHCommonMaps 'maps'

New-Item -ItemType Directory -Force -Path $expH42Dir      | Out-Null
New-Item -ItemType Directory -Force -Path $expHCommonMaps | Out-Null
New-Item -ItemType Directory -Force -Path $expHMapsSubdir | Out-Null

# 42/ mod.info (kept for Build 42 versioning compatibility)
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $expH42Dir 'mod.info') -Force

# common/ mod.info (copied from 42/mod.info -- same content, no-BOM)
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $expHCommonDir 'mod.info') -Force

# Map data files under common/media/maps/<MapId>/ (byte-exact copies)
$mapDataFiles = @('map.info', 'spawnpoints.lua', 'objects.lua',
                  '0_0.lotheader', 'world_0_0.lotpack', 'chunkdata_0_0.bin')
foreach ($f in $mapDataFiles) {
    $src = Join-Path $srcMapData $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $expHCommonMaps $f) -Force
    }
}

# thumb.png placeholder (minimal 1x1 PNG via System.Drawing)
$thumbPath = Join-Path $expHCommonMaps 'thumb.png'
try {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
    $bmp = New-Object System.Drawing.Bitmap(1, 1)
    $bmp.SetPixel(0, 0, [System.Drawing.Color]::White)
    $bmp.Save($thumbPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "  Generated thumb.png placeholder"
} catch {
    [System.IO.File]::WriteAllBytes($thumbPath, [byte[]]@())
    Write-Output "  thumb.png fallback (empty)"
}

# worldmap.xml placeholder (minimal XML)
$worldmapXml = Join-Path $expHCommonMaps 'worldmap.xml'
[System.IO.File]::WriteAllText($worldmapXml,
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  Generated worldmap.xml placeholder"

# worldmap-forest.xml placeholder (minimal XML)
$worldmapForestXml = Join-Path $expHCommonMaps 'worldmap-forest.xml'
[System.IO.File]::WriteAllText($worldmapForestXml,
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  Generated worldmap-forest.xml placeholder"

# biomemap_0_0.png placeholder (minimal 1x1 PNG)
$biomemapPath = Join-Path $expHMapsSubdir 'biomemap_0_0.png'
try {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
    $bmp2 = New-Object System.Drawing.Bitmap(1, 1)
    $bmp2.SetPixel(0, 0, [System.Drawing.Color]::White)
    $bmp2.Save($biomemapPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp2.Dispose()
    Write-Output "  Generated biomemap_0_0.png placeholder"
} catch {
    [System.IO.File]::WriteAllBytes($biomemapPath, [byte[]]@())
    Write-Output "  biomemap_0_0.png fallback (empty)"
}

Write-Output "Experiment-H candidate at: $expHBase"

# Verify binary content unchanged after copy
$dstLothPath  = Join-Path $expHCommonMaps '0_0.lotheader'
$dstLotpPath  = Join-Path $expHCommonMaps 'world_0_0.lotpack'
$dstChunkPath = Join-Path $expHCommonMaps 'chunkdata_0_0.bin'
$dstLothSha   = Get-FileSha256 $dstLothPath
$dstLotpSha   = Get-FileSha256 $dstLotpPath
$dstChunkSha  = Get-FileSha256 $dstChunkPath
$lothUnchanged  = ($srcLothSha -eq $dstLothSha)   -and ($srcLothSha  -ne '')
$lotpUnchanged  = ($srcLotpSha -eq $dstLotpSha)   -and ($srcLotpSha  -ne '')
$chunkUnchanged = ($srcChunkSha -eq $dstChunkSha) -and ($srcChunkSha -ne '')
Write-Output "  lotheader unchanged: $lothUnchanged (SHA: $srcLothSha)"
Write-Output "  lotpack unchanged:   $lotpUnchanged"
Write-Output "  chunkdata unchanged: $chunkUnchanged"

# Verify no-BOM on text files in common/
function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

$commonModNoBom  = -not (Test-HasBom (Join-Path $expHCommonDir 'mod.info'))
$commonMapNoBom  = -not (Test-HasBom (Join-Path $expHCommonMaps 'map.info'))
$commonSpawnNoBom = -not (Test-HasBom (Join-Path $expHCommonMaps 'spawnpoints.lua'))
$commonObjNoBom  = -not (Test-HasBom (Join-Path $expHCommonMaps 'objects.lua'))
Write-Output "  common/mod.info no-BOM: $commonModNoBom"
Write-Output "  common/map.info no-BOM: $commonMapNoBom"

# ---------------------------------------------------------------------------
# Step 3: Run discovery path inspector
# ---------------------------------------------------------------------------

$discoveryScript = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
Write-Output "Running discovery path inspector..."

& powershell -ExecutionPolicy Bypass -File $discoveryScript `
    -CandidateRoot $expHBase `
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
    -CandidateRoot $expHBase `
    -Output $Output `
    -MapId $MapId `
    -ModId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Metadata contract inspection failed (exit $LASTEXITCODE)"
    exit 1
}

# Load inspector results
$discoveryJson = Join-Path $Output 'map-discovery-path-report.json'
$metaJson      = Join-Path $Output 'map-metadata-contract-report.json'
$disc = if (Test-Path $discoveryJson) { Get-Content $discoveryJson -Raw | ConvertFrom-Json } else { $null }
$meta = if (Test-Path $metaJson)      { Get-Content $metaJson      -Raw | ConvertFrom-Json } else { $null }

$hasCommon    = if ($null -ne $disc) { [bool]$disc.has_common_media_maps } else { $false }
$riskLabel    = if ($null -ne $disc) { [string]$disc.map_folder_discovery_risk } else { 'UNKNOWN' }
$hasWXY       = if ($null -ne $disc) { [bool]$disc.has_world_xy_lotpack_pattern } else { $false }

# ---------------------------------------------------------------------------
# Packet files
# ---------------------------------------------------------------------------

# MAP_7L_COMMON_LAYOUT_PACKET.md
$packetMd = Join-Path $Output 'MAP_7L_COMMON_LAYOUT_PACKET.md'
Set-Content -Path $packetMd -Encoding ASCII -Value @"
# MAP-7L: Build 42 common/media/maps Layout Experiment Packet

${fence}text
MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY
H8_MOD_INFO_MAP_FIELD_RULED_OUT
VARIANTS_ABCDEFG_EXHAUSTED
COMMON_LAYOUT_PIVOT
has_common_media_maps=$($hasCommon.ToString().ToLower())
has_world_xy_lotpack_pattern=$($hasWXY.ToString().ToLower())
map_folder_discovery_risk=$riskLabel
loth_unchanged=$lothUnchanged
lotp_unchanged=$lotpUnchanged
chunkdata_unchanged=$chunkUnchanged
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## Variants A through G exhausted

VARIANTS_ABCDEFG_EXHAUSTED: all seven experiments produced empty
IsoMetaGrid map folder scan.

| Variant | Change | Result |
|---|---|---|
| A | Map= candidate;Muldraugh | SCAN_EMPTY |
| B | Map= candidate only | SCAN_EMPTY |
| C | Map= Muldraugh;candidate | SCAN_EMPTY |
| D | + root media/maps/ | SCAN_EMPTY |
| E | + root mod.info | SCAN_EMPTY |
| F | folder name == mod.info id | SCAN_EMPTY |
| G | mod.info map= field | SCAN_EMPTY |

## Operator-provided Build 42 structure evidence

Documented Build 42 mod layout (operator-provided screenshots):

${fence}text
MyMapMod/
  42.0/            (or 42/)
    mod.info
    poster.png
  common/
    mod.info
    media/
      maps/
        MyMap/
          0_0.lotheader
          chunkdata_0_0.bin
          map.info
          objects.lua
          spawnpoints.lua
          thumb.png
          world_0_0.lotpack
          worldmap.xml
          worldmap-forest.xml
          maps/
            biomemap_0_0.png
${fence}

## Experiment-H candidate (common/ layout)

The experiment-H candidate follows the documented structure.

${fence}text
experiment-h-candidate/$MapId/
  42/mod.info              (versioning layer)
  common/mod.info          (shared content)
  common/media/maps/$MapId/
    map.info
    objects.lua
    spawnpoints.lua
    0_0.lotheader           (binary, unchanged from empty_grass_v4)
    world_0_0.lotpack       (binary, unchanged)
    chunkdata_0_0.bin       (binary, unchanged)
    thumb.png               (1x1 placeholder PNG)
    worldmap.xml            (minimal XML placeholder)
    worldmap-forest.xml     (minimal XML placeholder)
    maps/
      biomemap_0_0.png      (1x1 placeholder PNG)
${fence}

has_common_media_maps: $hasCommon
has_world_xy_lotpack_pattern: $hasWXY
Binary files unchanged: lotheader=$lothUnchanged, lotpack=$lotpUnchanged, chunkdata=$chunkUnchanged

## Success condition for Experiment H

${fence}text
Looking in these map folders:
  $MapId
<End of map-folders list>
${fence}

map_folders_list_empty should be false.

## Diagnostic distinction

MAP_FOLDER_SCAN_EMPTY = discovery failure (current blocker for all A-G).
MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING = later-stage file failure.
These are DISTINCT. If Experiment H produces a non-empty scan but the
.lotheader is rejected, that is MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING
and represents PROGRESS (map registered, binary format issue next).

## Non-claims

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@

# MAP_7L_VARIANT_G_RESULT_SUMMARY.md
$variantGMd = Join-Path $Output 'MAP_7L_VARIANT_G_RESULT_SUMMARY.md'
Set-Content -Path $variantGMd -Encoding ASCII -Value @"
# MAP-7L: Variant G Result Summary

MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY
H8_MOD_INFO_MAP_FIELD_RULED_OUT
VARIANTS_ABCDEFG_EXHAUSTED

Variant G:
  Hypothesis: H8 -- mod.info map=<MapId> registers media path
  Change: map=pzmapforge_build42_candidate_v4_001 in root and 42/ mod.info
  Result: IsoMetaGrid map-folder scan still empty
  candidate_loaded: true
  player_data_received: true
  map_folders_list_empty: true
  Musical Menu Framework errors: client-side noise, not discovery blocker

Conclusion: mod.info map= field alone does not fix the registration.
Next: Experiment H tests common/media/maps layout (documented Build 42 structure).

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7L_OPERATOR_STRUCTURE_EVIDENCE.md
$evidenceMd = Join-Path $Output 'MAP_7L_OPERATOR_STRUCTURE_EVIDENCE.md'
Set-Content -Path $evidenceMd -Encoding ASCII -Value @"
# MAP-7L: Operator-Provided Build 42 Structure Evidence

Source: Build 42 mapping/mod structure documentation (operator-provided screenshots).

## Documented Build 42 layout

${fence}text
MyMapMod/
  42.0/            (version folder -- simplified to 42/)
    mod.info       (mod metadata for Build 42)
    poster.png
  common/          (shared content, all PZ versions)
    mod.info
    media/
      maps/
        MyMap/     (map folder name = Map= entry)
          0_0.lotheader
          chunkdata_0_0.bin
          map.info
          objects.lua
          spawnpoints.lua
          thumb.png
          world_0_0.lotpack
          worldmap.xml
          worldmap-forest.xml
          maps/
            biomemap_0_0.png
${fence}

## Key findings

- Map files go under common/media/maps/<MapName>/, not 42/media/maps/.
- 42.0/ (simplified to 42/) contains only mod versioning files.
- common/ is the shared-content layer accessible by IsoMetaGrid.
- Lotpack filename: world_X_Y.lotpack (our writer already uses this).
- Cell size: Build 42 exports at 256x256. Our binary files use 300x300-based
  sizes. This is a SEPARATE investigation item not addressed in this slice.
- Additional files documented: thumb.png, worldmap.xml, worldmap-forest.xml,
  maps/biomemap_X_Y.png. Experiment H includes placeholder versions.

## Diagnosis

All previous experiments placed map files under:
  42/media/maps/<MapId>/     -- versioning layer, IsoMetaGrid may not scan
  root media/maps/<MapId>/   -- not the documented structure
  dual root+42/              -- still not common/

Experiment H tests the documented common/media/maps/<MapId>/ placement.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED
"@

# MAP_7L_EXPERIMENT_H_MANUAL_INSTALL_COMMANDS.md
$installMd = Join-Path $Output 'MAP_7L_EXPERIMENT_H_MANUAL_INSTALL_COMMANDS.md'
Set-Content -Path $installMd -Encoding ASCII -Value @"
# MAP-7L: Experiment H Manual Install Commands (HUMAN-ONLY)

All steps are HUMAN-ONLY. The packet script does NOT copy to PZ folders.

## Candidate source location

${fence}text
$expHBase
${fence}

Structure after copy:
${fence}text
$MapId/
  42/mod.info
  common/mod.info
  common/media/maps/$MapId/
    map.info, spawnpoints.lua, objects.lua (text, no-BOM)
    0_0.lotheader, world_0_0.lotpack, chunkdata_0_0.bin (binary, unchanged)
    thumb.png, worldmap.xml, worldmap-forest.xml (placeholders)
    maps/biomemap_0_0.png (placeholder)
${fence}

## Step 1: Delete old install (HUMAN-ONLY)

Remove any existing $ModFolderName folder from your PZ mods directory.

## Step 2: Copy experiment-H candidate (HUMAN-ONLY)

${fence}powershell
Copy-Item -Recurse -Force `
  "$expHBase" `
  "C:\Users\YourUser\Zomboid\mods\$ModFolderName"
${fence}

Verify: the destination should have both 42/mod.info and common/mod.info.

## Step 3: Server configuration (HUMAN-ONLY -- no-BOM UTF-8)

${fence}ini
Map=$MapId;Muldraugh, KY
Mods=$MapId
${fence}

Server name: $ServerName

## Step 4: Launch and capture

Run PZ server and client. Copy DebugLog after loading:
${fence}text
C:\Users\YourUser\Zomboid\Logs\*_DebugLog.txt -> .local\map7l-logs\DebugLog-variant-H.txt
${fence}

## Step 5: Analyze

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
  -LogPath .\.local\map7l-logs\DebugLog-variant-H.txt ``
  -Output .\.local\map7l-analysis\variant-H ``
  -ExpectedMapId $MapId ``
  -VariantLabel VariantH
${fence}

## Success target

${fence}text
Looking in these map folders:
  $MapId
<End of map-folders list>
${fence}

classification target: map_folders_list_empty=false
If scan non-empty but lotheader error: MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING
  -- this is PROGRESS, not failure. Binary format is the next focus.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7L_EXPERIMENT_H_LOG_CAPTURE_COMMANDS.md
$logMd = Join-Path $Output 'MAP_7L_EXPERIMENT_H_LOG_CAPTURE_COMMANDS.md'
Set-Content -Path $logMd -Encoding ASCII -Value @"
# MAP-7L: Experiment H Log Capture Commands

${fence}powershell
New-Item -ItemType Directory -Force -Path .\.local\map7l-logs | Out-Null
Copy-Item "C:\Users\YourUser\Zomboid\Logs\*_DebugLog.txt" ``
  .\.local\map7l-logs\DebugLog-variant-H.txt
${fence}

## Run analyzer

${fence}powershell
powershell -ExecutionPolicy Bypass ``
  -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
  -LogPath .\.local\map7l-logs\DebugLog-variant-H.txt ``
  -Output .\.local\map7l-analysis\variant-H ``
  -ExpectedMapId $MapId ``
  -VariantLabel VariantH
${fence}

## Expected scan section (success)

${fence}text
Looking in these map folders:
  $MapId
<End of map-folders list>
${fence}

## Distinguishing outcomes

Success: map_folders_list_empty=false, $MapId appears in list.
Failure: MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY (same as A-G, common/ not the fix).
Progress: MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING (map registered, lotheader issue next).

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7L_EXPERIMENT_H_MANUAL_RESULT.local-template.md
$resultMd = Join-Path $Output 'MAP_7L_EXPERIMENT_H_MANUAL_RESULT.local-template.md'
Set-Content -Path $resultMd -Encoding ASCII -Value @"
# MAP-7L: Experiment H Manual Result (local template)

Fill in after Experiment H manual retest. Do not commit.

## Test metadata

Date:
PZ version:
Server: $ServerName
Map= line: Map=$MapId;Muldraugh, KY
Layout: common/media/maps/$MapId/ (documented Build 42 structure)

## Pre-test checks (HUMAN-ONLY)

- [ ] common/mod.info present and no-BOM
- [ ] 42/mod.info present and no-BOM
- [ ] common/media/maps/$MapId/ directory present
- [ ] Binary files copied: 0_0.lotheader, world_0_0.lotpack, chunkdata_0_0.bin
- [ ] Server preset updated (no-BOM)
- [ ] Old log files deleted

## Map folder scan section

${fence}text
(paste here)
${fence}

- Did $MapId appear in map folder scan? YES / NO
- map_folders_list_empty:

## Analyzer output

classification:
map_folders_list_empty:
map_folders_list_count:

## Result

SUCCESS: map_folders_list_empty=false, $MapId in scan list
PROGRESS: MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING (lotheader issue)
FAILURE: MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY (still empty)

## Any new errors observed

${fence}text
(paste lotheader/lotpack/chunkdata errors if scan non-empty)
${fence}

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# ---------------------------------------------------------------------------
# map7l-common-layout-preflight.json
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $Output 'map7l-common-layout-preflight.json'
$preflight = [ordered]@{
    schema                               = 'pzmapforge.map7l-common-layout-preflight.v0.1'
    map_id                               = $MapId
    mod_folder_name                      = $ModFolderName
    server_name                          = $ServerName
    variant_g_result                     = 'MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY'
    h8_mod_info_map_field_ruled_out      = $true
    variants_abcdefg_exhausted           = $true
    common_media_maps_layout             = $true
    has_common_media_maps                = $hasCommon
    has_world_xy_lotpack_pattern         = $hasWXY
    map_folder_discovery_risk            = $riskLabel
    loth_sha256_unchanged                = $lothUnchanged
    lotp_sha256_unchanged                = $lotpUnchanged
    chunkdata_sha256_unchanged           = $chunkUnchanged
    loth_size_bytes                      = $srcLothSize
    lotp_size_bytes                      = $srcLotpSize
    chunkdata_size_bytes                 = $srcChunkSize
    suggested_map_line                   = "Map=$MapId;Muldraugh, KY"
    suggested_analyzer_expected_map_id   = $MapId
    suggested_analyzer_variant_label     = 'VariantH'
    all_pz_folder_writes_human_only      = $true
    load_test_not_performed              = $true
    public_playable_claim_allowed        = $false
    binary_writer_not_changed            = $true
}
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJson -Encoding UTF8
Write-Output "JSON: $preflightJson"

# map7l-common-layout-preflight.md
$preflightMd = Join-Path $Output 'map7l-common-layout-preflight.md'
Set-Content -Path $preflightMd -Encoding ASCII -Value @"
# MAP-7L Common Layout Preflight

${fence}text
map_id=$MapId
variant_g_result=MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY
h8_mod_info_map_field_ruled_out=true
variants_abcdefg_exhausted=true
common_media_maps_layout=true
has_common_media_maps=$($hasCommon.ToString().ToLower())
has_world_xy_lotpack_pattern=$($hasWXY.ToString().ToLower())
map_folder_discovery_risk=$riskLabel
loth_sha256_unchanged=$lothUnchanged
lotp_sha256_unchanged=$lotpUnchanged
chunkdata_sha256_unchanged=$chunkUnchanged
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
Write-Output "MAP_7L_COMMON_LAYOUT_PACKET.md:                 $(Test-Path $packetMd)"
Write-Output "MAP_7L_VARIANT_G_RESULT_SUMMARY.md:             $(Test-Path $variantGMd)"
Write-Output "MAP_7L_OPERATOR_STRUCTURE_EVIDENCE.md:          $(Test-Path $evidenceMd)"
Write-Output "MAP_7L_EXPERIMENT_H_MANUAL_INSTALL_COMMANDS.md: $(Test-Path $installMd)"
Write-Output "MAP_7L_EXPERIMENT_H_LOG_CAPTURE_COMMANDS.md:    $(Test-Path $logMd)"
Write-Output "MAP_7L_EXPERIMENT_H_MANUAL_RESULT.local-template: $(Test-Path $resultMd)"
Write-Output "map7l-common-layout-preflight.json:             $(Test-Path $preflightJson)"
Write-Output "map7l-common-layout-preflight.md:               $(Test-Path $preflightMd)"
Write-Output "map-discovery-path-report.json:                 $(Test-Path $discoveryJson)"
Write-Output "map-discovery-path-report.md:                   $(Test-Path (Join-Path $Output 'map-discovery-path-report.md'))"
Write-Output "map-metadata-contract-report.json:              $(Test-Path $metaJson)"
Write-Output "map-metadata-contract-report.md:                $(Test-Path (Join-Path $Output 'map-metadata-contract-report.md'))"
Write-Output ""
Write-Output "MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY"
Write-Output "VARIANTS_ABCDEFG_EXHAUSTED"
Write-Output "COMMON_LAYOUT_PIVOT"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
