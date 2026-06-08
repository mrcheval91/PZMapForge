#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-8D: Generates a staged Workshop package without invalid worldmap .bin stubs.

    Builds a staged Workshop package using the MAP-8B version-scoped 42\media layout,
    but removes worldmap.xml.bin, worldmap-forest.xml.bin, and streets.xml.bin.
    Retains: worldmap.xml, worldmap-forest.xml, worldmap.png (uncompiled XML/PNG).

    Does NOT run PZ. Does NOT upload to Steam Workshop. Does NOT write to Steam/PZ folders.
    Does NOT copy third-party files. Does NOT change binary writer behavior.
    All output under .local/ only.

.PARAMETER Output
    Required. Path under .local/.

.PARAMETER CandidateMapId
    Optional. Defaults to pzmapforge_build42_candidate_v4_001.

.PARAMETER WorkshopId
    Optional. Defaults to 3740642200.
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateMapId = 'pzmapforge_build42_candidate_v4_001',
    [string]$WorkshopId     = '3740642200'
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

Write-Output "MAP-8D: No Invalid Worldmap Bin Stubs Probe Packet"
Write-Output "Output:         $Output"
Write-Output "CandidateMapId: $CandidateMapId"
Write-Output "WorkshopId:     $WorkshopId"
Write-Output ""

$targetCell = '35_27'

# ---------------------------------------------------------------------------
# Step 1: Source coordinate-aligned binaries
# ---------------------------------------------------------------------------

# Try MAP-7Y staged package (has 35_27.* coordinate-aligned binaries)
$map7yStagedMapsDir = Join-Path $repoRoot ".local\map7y-packet\staged-workshop-sidecar-stubs\$CandidateMapId\common\media\maps\$CandidateMapId"
$usedPriorSource   = $false

if ((Test-Path -LiteralPath $map7yStagedMapsDir) -and
    (Test-Path -LiteralPath (Join-Path $map7yStagedMapsDir "$targetCell.lotheader"))) {
    Write-Output "Using MAP-7Y staged coordinate-aligned binaries."
    $binarySourceDir = $map7yStagedMapsDir
    $usedPriorSource = $true
} else {
    Write-Output "MAP-7Y staged package not found. Generating empty_grass_v4 via CLI..."
    $candidateGenOut = Join-Path $Output 'candidate-gen'
    & dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
        --configuration Release --no-build `
        -- map-export-experimental `
        --map-id $CandidateMapId `
        --output $candidateGenOut `
        --build42-candidate-writer `
        --build42-candidate-profile empty_grass_v4
    if ($LASTEXITCODE -ne 0) { Write-Error "CLI candidate generation failed."; exit 1 }
    $candDir = Join-Path $candidateGenOut ($CandidateMapId + '_build42_candidate')
    $binarySourceDir = Join-Path $candDir "42\media\maps\$CandidateMapId"
}

# ---------------------------------------------------------------------------
# Step 2: Build staged package (42\media layout, no .bin sidecar stubs)
# ---------------------------------------------------------------------------

$stagedRoot    = Join-Path $Output "staged-workshop-no-worldmap-bin\$CandidateMapId"
$staged42Dir   = Join-Path $stagedRoot '42'
$stagedMapsDir = Join-Path $staged42Dir "media\maps\$CandidateMapId"

New-Item -ItemType Directory -Force -Path $staged42Dir   | Out-Null
New-Item -ItemType Directory -Force -Path $stagedMapsDir | Out-Null

function Write-PlaceholderPng {
    param([string]$Path, [int]$Width = 64, [int]$Height = 64)
    try {
        Add-Type -AssemblyName System.Drawing -ErrorAction Stop
        $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
        $g   = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.Color]::DarkGray)
        $g.Dispose()
        $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    } catch {
        [System.IO.File]::WriteAllBytes($Path, [byte[]]@())
    }
}

# mod.info at staged root (= Contents\mods\<MapId>\mod.info on Workshop)
$modInfoContent = "id=$CandidateMapId`nname=PZMapForge MAP-8D No Invalid Worldmap Bin Probe`nmodversion=1.0`n"
[System.IO.File]::WriteAllText(
    (Join-Path $stagedRoot 'mod.info'),
    $modInfoContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  mod.info: root"

# mod.info at 42\ (versioned)
[System.IO.File]::WriteAllText(
    (Join-Path $staged42Dir 'mod.info'),
    $modInfoContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  42\mod.info: versioned"

# poster.png placeholder
Write-PlaceholderPng (Join-Path $stagedRoot 'poster.png')
Write-Output "  poster.png: placeholder"

# Binary files (coordinate-aligned, content unchanged)
$binaryFiles   = @("$targetCell.lotheader", "world_$targetCell.lotpack", "chunkdata_$targetCell.bin")
$binaryPresent = $true

foreach ($f in $binaryFiles) {
    $src = Join-Path $binarySourceDir $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $stagedMapsDir $f) -Force
        Write-Output "  Staged binary: $f"
    } else {
        # Try 0_0 variant (CLI generation output uses 0_0 naming)
        $alt = $f -replace '35_27', '0_0'
        $srcAlt = Join-Path $binarySourceDir $alt
        if (Test-Path -LiteralPath $srcAlt) {
            Copy-Item -LiteralPath $srcAlt -Destination (Join-Path $stagedMapsDir $f) -Force
            Write-Output "  Staged binary (renamed): $alt -> $f"
        } else {
            $binaryPresent = $false
            Write-Output "  WARNING: binary file not found: $f"
        }
    }
}

# map.info (coordinate-aligned, lots=NONE)
$mapInfoContent = @"
title=$CandidateMapId
lots=NONE
description=PZMapForge MAP-8D No Invalid Worldmap Bin Probe. Diagnostic only. Not a playable map.
fixed2x=true
zoomX=10505
zoomY=12220
zoomS=14.5
"@
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'map.info'),
    $mapInfoContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  map.info: lots=NONE zoomX=10505 zoomY=12220"

# spawnpoints.lua (function SpawnPoints() style, worldX=35 worldY=27)
$spawnContent = @"
-- PZMapForge MAP-8D: No Invalid Worldmap Bin Probe
-- Target cell: $targetCell (worldX=35, worldY=27)
-- Not a playable map. Diagnostic probe only.
-- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false

function SpawnPoints()
    local spawnpoints = {}
    spawnpoints["Profession_Unemployed"] = {
        { worldX = 35, worldY = 27, posX = 246, posY = 188, posZ = 0 },
    }
    return spawnpoints
end
"@
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'spawnpoints.lua'),
    $spawnContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  spawnpoints.lua: function SpawnPoints() worldX=35 worldY=27"

# objects.lua (comment-only)
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'objects.lua'),
    "-- MAP-8D no invalid worldmap bin probe`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  objects.lua: comment-only"

# Uncompiled worldmap XML (retained, NOT the binary .bin versions)
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'worldmap.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'worldmap-forest.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  worldmap.xml: retained (uncompiled XML)"
Write-Output "  worldmap-forest.xml: retained (uncompiled XML)"

# worldmap.png (placeholder)
Write-PlaceholderPng (Join-Path $stagedMapsDir 'worldmap.png') -Width 256 -Height 256
Write-Output "  worldmap.png: 256x256 placeholder PNG"

# thumb.png
Write-PlaceholderPng (Join-Path $stagedMapsDir 'thumb.png')
Write-Output "  thumb.png: placeholder"

# MAP8D canary file (no .bin stubs)
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'MAP8D_NO_WORLDMAP_BIN_STUBS.txt'),
    "MAP8D_NO_WORLDMAP_BIN_STUBS=true`nMAP8D_INVALID_BIN_STUBS_REMOVED=true`nSOURCE_BASIS=MAP-8B`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  MAP8D_NO_WORLDMAP_BIN_STUBS.txt: canary"

# Explicit confirmation that .bin stubs are absent
$binStubsAbsent = (-not (Test-Path (Join-Path $stagedMapsDir 'worldmap.xml.bin'))) -and
                  (-not (Test-Path (Join-Path $stagedMapsDir 'worldmap-forest.xml.bin'))) -and
                  (-not (Test-Path (Join-Path $stagedMapsDir 'streets.xml.bin')))
Write-Output "  worldmap.xml.bin: ABSENT (removed)"
Write-Output "  worldmap-forest.xml.bin: ABSENT (removed)"
Write-Output "  streets.xml.bin: ABSENT (removed)"
Write-Output "  binStubsAbsent: $binStubsAbsent"

# ---------------------------------------------------------------------------
# Step 3: Verify staged package
# ---------------------------------------------------------------------------

$requiredFiles = @(
    'mod.info',
    '42\mod.info',
    "42\media\maps\$CandidateMapId\map.info",
    "42\media\maps\$CandidateMapId\spawnpoints.lua",
    "42\media\maps\$CandidateMapId\objects.lua",
    "42\media\maps\$CandidateMapId\$targetCell.lotheader",
    "42\media\maps\$CandidateMapId\world_$targetCell.lotpack",
    "42\media\maps\$CandidateMapId\chunkdata_$targetCell.bin",
    "42\media\maps\$CandidateMapId\worldmap.xml",
    "42\media\maps\$CandidateMapId\worldmap-forest.xml",
    "42\media\maps\$CandidateMapId\worldmap.png",
    "42\media\maps\$CandidateMapId\MAP8D_NO_WORLDMAP_BIN_STUBS.txt"
)

$missingFiles  = [System.Collections.Generic.List[string]]::new()
$manifestLines = [System.Collections.Generic.List[string]]::new()

foreach ($rel in $requiredFiles) {
    $abs = Join-Path $stagedRoot $rel
    if (Test-Path -LiteralPath $abs) {
        $size = (Get-Item -LiteralPath $abs).Length
        $manifestLines.Add("  PRESENT  $rel  ($size bytes)")
    } else {
        $missingFiles.Add($rel)
        $manifestLines.Add("  MISSING  $rel")
    }
}
$requiredFilesPresent = ($missingFiles.Count -eq 0)

# ---------------------------------------------------------------------------
# Step 4: Write packet docs
# ---------------------------------------------------------------------------

$manifestPath = Join-Path $Output 'MAP_8D_STAGED_PACKAGE_MANIFEST.md'
Set-Content -Path $manifestPath -Value @"
# MAP-8D: Staged Package Manifest

``````text
MAP8D_NO_INVALID_WORLDMAP_BIN_STUBS_PROBE_STAGED
MAP8D_VERSION_42_MEDIA_PATH_RETAINED
INVALID_WORLDMAP_BIN_STUBS_REMOVED
STREETS_XML_BIN_REMOVED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Package root

$stagedRoot

## Required files

$(($manifestLines | ForEach-Object { $_ }) -join "`n")

## Removed files (invalid generated stubs)

  worldmap.xml.bin        -- REMOVED (invalid magic, MAP-8B error)
  worldmap-forest.xml.bin -- REMOVED (invalid magic, MAP-8B error)
  streets.xml.bin         -- REMOVED (ASCII-marker stub, same category)

## Status

``````text
required_files_present:         $($requiredFilesPresent.ToString().ToLower())
binary_stubs_absent:            $($binStubsAbsent.ToString().ToLower())
binary_writer_gate_closed:      true
public_playable_claim_allowed:  false
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_8D_STAGED_PACKAGE_MANIFEST.md"

$checklistPath = Join-Path $Output 'MAP_8D_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md'
Set-Content -Path $checklistPath -Value @"
# MAP-8D: Human Workshop Update Checklist

``````text
HUMAN_ONLY_WORKSHOP_UPDATE
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_PZ_RUN_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## This script does NOT upload anything.

All Workshop update steps are HUMAN-ONLY.

## Steps

### Step 1: Verify staged package

Staged at: $stagedRoot

Confirm required files present (see MAP_8D_STAGED_PACKAGE_MANIFEST.md).
Confirm worldmap.xml.bin, worldmap-forest.xml.bin, streets.xml.bin are ABSENT.

### Step 2: Update existing Workshop item $WorkshopId

Update the EXISTING private Workshop item $WorkshopId.
DO NOT create a new Workshop item.
DO NOT make it public.
DO NOT use 3355966216 (that is Dru_map's ID).

After uploading, wait for Steam to finish downloading updates.
Verify downloaded payload does NOT contain worldmap.xml.bin or worldmap-forest.xml.bin.

### Step 3: Wire the server

``````ini
Mods=$CandidateMapId
WorkshopItems=$WorkshopId
Map=$CandidateMapId;Muldraugh, KY
Public=false
``````

### Step 4: Capture and analyze logs

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
    -LogPath .\.local\map8d-logs\DebugLog-client.txt ``
    -Output .\.local\map8d-packet\analysis-after-upload ``
    -ExpectedMapId $CandidateMapId ``
    -VariantLabel VariantNoWorldmapBin
``````

### Key signals to watch

- No 'invalid format (magic doesn't match)' in log: stubs successfully removed.
- IsoMetaGrid scan non-empty: map folder mounted. Binary writer gate opens.
- New error referencing missing .bin file: PZ expected the stub. Note the file name.
- Same fallback forest, no new signals: discriminator is elsewhere.
"@ -Encoding ASCII
Write-Output "Wrote: MAP_8D_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md"

$packetPath = Join-Path $Output 'MAP_8D_NO_INVALID_WORLDMAP_BIN_PACKET.md'
Set-Content -Path $packetPath -Value @"
# MAP-8D: No Invalid Worldmap Bin Stubs Probe Packet

``````text
MAP8D_NO_INVALID_WORLDMAP_BIN_STUBS_PROBE_STAGED
MAP8D_VERSION_42_MEDIA_PATH_RETAINED
INVALID_WORLDMAP_BIN_STUBS_REMOVED
STREETS_XML_BIN_REMOVED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````

## Source basis: MAP-8B

MAP-8B proved 42\media path visible to worldmap loader.
worldmap.xml.bin + worldmap-forest.xml.bin had invalid magic.
This probe removes those stubs and streets.xml.bin to test the effect.

## Layout

``````text
42\media\maps\$CandidateMapId\
  map.info              RETAINED
  spawnpoints.lua       RETAINED (function SpawnPoints(), worldX=35 worldY=27)
  objects.lua           RETAINED
  35_27.lotheader       RETAINED
  world_35_27.lotpack   RETAINED
  chunkdata_35_27.bin   RETAINED
  worldmap.xml          RETAINED (uncompiled XML, not binary)
  worldmap-forest.xml   RETAINED (uncompiled XML, not binary)
  worldmap.png          RETAINED (placeholder PNG)
  MAP8D_NO_WORLDMAP_BIN_STUBS.txt  CANARY
  [worldmap.xml.bin]    REMOVED
  [worldmap-forest.xml.bin]  REMOVED
  [streets.xml.bin]     REMOVED
``````

## Server wiring (after human upload)

``````ini
Mods=$CandidateMapId
WorkshopItems=$WorkshopId
Map=$CandidateMapId;Muldraugh, KY
Public=false
``````

## Packet files

``````text
MAP_8D_NO_INVALID_WORLDMAP_BIN_PACKET.md  (this file)
MAP_8D_STAGED_PACKAGE_MANIFEST.md
MAP_8D_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md
map8d-preflight.json
map8d-preflight.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
BINARY_WRITER_GATE_STILL_CLOSED
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_8D_NO_INVALID_WORLDMAP_BIN_PACKET.md"

# ---------------------------------------------------------------------------
# Preflight JSON
# ---------------------------------------------------------------------------

$preflightJsonPath = Join-Path $Output 'map8d-preflight.json'
$preflight = [ordered]@{
    schema                             = 'pzmapforge.map8d-preflight.v0.1'
    source_basis                       = 'MAP-8B'
    candidate_map_id                   = $CandidateMapId
    workshop_id                        = $WorkshopId
    version_scoped_media_path          = $true
    layout_42_media_maps               = $true
    invalid_worldmap_bin_stubs_removed = $true
    worldmap_xml_bin_removed           = $true
    worldmap_forest_xml_bin_removed    = $true
    streets_xml_bin_removed            = $true
    streets_xml_bin_removal_rationale  = 'ascii_marker_stub_no_valid_binary_magic_prefer_clean_probe'
    worldmap_xml_retained              = $true
    worldmap_forest_xml_retained       = $true
    worldmap_png_retained              = $true
    binary_writer_gate_closed          = $true
    no_binary_writer_changes           = $true
    no_third_party_files_copied        = $true
    no_pz_run_by_script                = $true
    no_workshop_upload_by_script       = $true
    playable_claim_allowed             = $false
    required_files_present             = $requiredFilesPresent
    binary_stubs_absent                = $binStubsAbsent
    required_files_missing             = [string[]]@($missingFiles.ToArray())
    next_human_action                  = "upload staged package to item $WorkshopId and run controlled MAP-8D runtime test"
}
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map8d-preflight.json"

$preflightMdPath = Join-Path $Output 'map8d-preflight.md'
Set-Content -Path $preflightMdPath -Value @"
# MAP-8D Preflight

``````text
MAP8D_NO_INVALID_WORLDMAP_BIN_STUBS_PROBE_STAGED
INVALID_WORLDMAP_BIN_STUBS_REMOVED
STREETS_XML_BIN_REMOVED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
source_basis:                         MAP-8B
version_scoped_media_path:            true
invalid_worldmap_bin_stubs_removed:   true
worldmap_xml_bin_removed:             true
worldmap_forest_xml_bin_removed:      true
streets_xml_bin_removed:              true
worldmap_xml_retained:                true
worldmap_forest_xml_retained:         true
worldmap_png_retained:                true
binary_writer_gate_closed:            true
required_files_present:               $($requiredFilesPresent.ToString().ToLower())
binary_stubs_absent:                  $($binStubsAbsent.ToString().ToLower())
playable_claim_allowed:               false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map8d-preflight.md"

Write-Output ""
Write-Output "MAP-8D packet complete."
Write-Output "Staged: $stagedRoot"
Write-Output "invalid_worldmap_bin_stubs_removed: true"
Write-Output "streets_xml_bin_removed: true"
Write-Output "worldmap_xml_retained: true"
Write-Output "binary_writer_gate_closed: true"
Write-Output "required_files_present: $requiredFilesPresent"
Write-Output "binary_stubs_absent: $binStubsAbsent"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
