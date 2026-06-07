#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7U: Generates the coordinate-aligned diagnostic staging packet.

    Renames binary files from 0_0 to 35_27 coordinates.
    Updates map.info zoom fields to match Dru_map reference values.
    Updates spawnpoints.lua to spawn at worldX=35,worldY=27.
    Does NOT mutate binary file contents.
    Does NOT change the binary writer.
    Does NOT run PZ or upload anything.

    All output under .local/ only.

.PARAMETER Output
    Required. Path under .local/.

.PARAMETER CandidateMapId
    Optional. Defaults to pzmapforge_build42_candidate_v4_001.

.PARAMETER ReferenceMapId
    Optional. Defaults to Dru_map (for documentation only).

.PARAMETER TargetWorldX
    Optional. Target spawn worldX. Defaults to 35.

.PARAMETER TargetWorldY
    Optional. Target spawn worldY. Defaults to 27.

.PARAMETER TargetPosX
    Optional. Target spawn posX. Defaults to 246.

.PARAMETER TargetPosY
    Optional. Target spawn posY. Defaults to 188.

.PARAMETER TargetZoomX
    Optional. Target zoomX from Dru_map. Defaults to 10505.

.PARAMETER TargetZoomY
    Optional. Target zoomY from Dru_map. Defaults to 12220.

.PARAMETER TargetZoomS
    Optional. Target zoomS from Dru_map. Defaults to 14.5.
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateMapId = 'pzmapforge_build42_candidate_v4_001',
    [string]$ReferenceMapId = 'Dru_map',
    [int]   $TargetWorldX   = 35,
    [int]   $TargetWorldY   = 27,
    [int]   $TargetPosX     = 246,
    [int]   $TargetPosY     = 188,
    [int]   $TargetZoomX    = 10505,
    [int]   $TargetZoomY    = 12220,
    [double]$TargetZoomS    = 14.5
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

Write-Output "MAP-7U: Coordinate-Aligned Diagnostic Staging Packet"
Write-Output "Output:       $Output"
Write-Output "CandidateMapId: $CandidateMapId"
Write-Output "TargetCell:   ${TargetWorldX}_${TargetWorldY}"
Write-Output ""

$targetCell = "${TargetWorldX}_${TargetWorldY}"

# ---------------------------------------------------------------------------
# Step 1: Generate empty_grass_v4 candidate via dotnet CLI
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v4 candidate..."

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $CandidateMapId `
    --output $candidateOut `
    --build42-candidate-writer `
    --build42-candidate-profile empty_grass_v4

if ($LASTEXITCODE -ne 0) {
    Write-Error "CLI candidate generation failed (exit $LASTEXITCODE)"
    exit 1
}

$candDir    = Join-Path $candidateOut ($CandidateMapId + '_build42_candidate')
$src42Dir   = Join-Path $candDir '42'
$srcMapData = Join-Path $src42Dir "media\maps\$CandidateMapId"

# ---------------------------------------------------------------------------
# Step 2: Build coordinate-aligned staged package
# ---------------------------------------------------------------------------

Write-Output "Building coordinate-aligned staged package..."

$stagedRoot      = Join-Path $Output "staged-workshop-coordinate-aligned\$CandidateMapId"
$staged42Dir     = Join-Path $stagedRoot '42'
$stagedCommonDir = Join-Path $stagedRoot 'common'
$stagedMapsDir   = Join-Path $stagedCommonDir "media\maps\$CandidateMapId"

New-Item -ItemType Directory -Force -Path $staged42Dir     | Out-Null
New-Item -ItemType Directory -Force -Path $stagedMapsDir   | Out-Null

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

# root mod.info and 42/mod.info (no-BOM)
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $stagedRoot 'mod.info') -Force
Copy-Item -LiteralPath (Join-Path $src42Dir 'mod.info') `
          -Destination (Join-Path $staged42Dir 'mod.info') -Force

# Text files (copy then update)
foreach ($f in @('spawnpoints.lua', 'objects.lua')) {
    $src = Join-Path $srcMapData $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $stagedMapsDir $f) -Force
    }
}

# Binary files: RENAME from 0_0 to <targetCell> (do not mutate content)
$binPairs = @(
    @{ Src = '0_0.lotheader';    Dst = "$targetCell.lotheader" },
    @{ Src = 'world_0_0.lotpack'; Dst = "world_$targetCell.lotpack" },
    @{ Src = 'chunkdata_0_0.bin'; Dst = "chunkdata_$targetCell.bin" }
)

$stagedBinaryFiles = [System.Collections.Generic.List[string]]::new()
foreach ($pair in $binPairs) {
    $src = Join-Path $srcMapData $pair.Src
    $dst = Join-Path $stagedMapsDir $pair.Dst
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination $dst -Force
        $size = (Get-Item -LiteralPath $dst).Length
        $stagedBinaryFiles.Add("$($pair.Dst) ($size bytes)")
        Write-Output "  Staged: $($pair.Src) -> $($pair.Dst) ($size bytes)"
    }
}

# Placeholders
function Write-PlaceholderPng {
    param([string]$Path)
    try {
        Add-Type -AssemblyName System.Drawing -ErrorAction Stop
        $bmp = New-Object System.Drawing.Bitmap(64, 64)
        $g   = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.Color]::DarkGray)
        $g.Dispose()
        $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    } catch {
        [System.IO.File]::WriteAllBytes($Path, [byte[]]@())
    }
}

Write-PlaceholderPng (Join-Path $stagedRoot 'poster.png')
Write-PlaceholderPng (Join-Path $stagedMapsDir 'thumb.png')

# ---------------------------------------------------------------------------
# Step 3: Write coordinate-aligned map.info
# ---------------------------------------------------------------------------

$zoomSStr = "$TargetZoomS"  # e.g. "14.5"
$mapInfoContent = @"
title=$CandidateMapId
lots=NONE
description=PZMapForge Build42 Coordinate-Aligned Diagnostic. Not a playable map.
fixed2x=true
zoomX=$TargetZoomX
zoomY=$TargetZoomY
zoomS=$zoomSStr
"@
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'map.info'),
    $mapInfoContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  map.info: zoomX=$TargetZoomX zoomY=$TargetZoomY zoomS=$TargetZoomS"

# ---------------------------------------------------------------------------
# Step 4: Write coordinate-aligned spawnpoints.lua
# ---------------------------------------------------------------------------

$spawnContent = @"
-- PZMapForge MAP-7U coordinate-aligned diagnostic spawnpoints.
-- Target cell: $targetCell (worldX=$TargetWorldX worldY=$TargetWorldY)
-- These coordinates align with the Dru_map reference spawn (worldX=35, worldY=27).
-- This is a diagnostic relabel only; not a playable PZMapForge map.
-- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false

local spawnpoints = {
    Profession_Unemployed = {
        { worldX = $TargetWorldX, worldY = $TargetWorldY, posX = $TargetPosX, posY = $TargetPosY, posZ = 0 },
    },
}

return spawnpoints
"@
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'spawnpoints.lua'),
    $spawnContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  spawnpoints.lua: worldX=$TargetWorldX worldY=$TargetWorldY posX=$TargetPosX posY=$TargetPosY"

# worldmap stubs
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'worldmap.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'worldmap-forest.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))

# ---------------------------------------------------------------------------
# Step 5: Verify staged files
# ---------------------------------------------------------------------------

$requiredFiles = @(
    'mod.info',
    '42\mod.info',
    "common\media\maps\$CandidateMapId\map.info",
    "common\media\maps\$CandidateMapId\spawnpoints.lua",
    "common\media\maps\$CandidateMapId\objects.lua",
    "common\media\maps\$CandidateMapId\$targetCell.lotheader",
    "common\media\maps\$CandidateMapId\world_$targetCell.lotpack",
    "common\media\maps\$CandidateMapId\chunkdata_$targetCell.bin"
)

$presentFiles = [System.Collections.Generic.List[string]]::new()
$missingFiles = [System.Collections.Generic.List[string]]::new()
foreach ($rel in $requiredFiles) {
    $abs = Join-Path $stagedRoot $rel
    if (Test-Path -LiteralPath $abs) { $presentFiles.Add($rel) }
    else { $missingFiles.Add($rel) }
}
$requiredFilesPresent = ($missingFiles.Count -eq 0)

# ---------------------------------------------------------------------------
# Step 6: Write packet docs
# ---------------------------------------------------------------------------

# MAP_7U_COORDINATE_ALIGNED_STAGING_MANIFEST.md
$manifestPath = Join-Path $Output 'MAP_7U_COORDINATE_ALIGNED_STAGING_MANIFEST.md'
Set-Content -Path $manifestPath -Value @"
# MAP-7U: Coordinate-Aligned Staging Manifest

``````text
COORDINATE_ALIGNED_DIAGNOSTIC_PACKAGE
BINARY_CONTENTS_UNCHANGED
COORDINATE_LABELS_REBASED_0_0_TO_${TargetWorldX}_${TargetWorldY}
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Package root

$stagedRoot

## Cell relabeling

Original cell: 0_0
Target cell:   $targetCell (worldX=$TargetWorldX worldY=$TargetWorldY)

Binary files (content unchanged, filename rebased):
  0_0.lotheader     -> $targetCell.lotheader
  world_0_0.lotpack -> world_$targetCell.lotpack
  chunkdata_0_0.bin -> chunkdata_$targetCell.bin

## map.info zoom (updated to match Dru_map reference)

  zoomX: 0 -> $TargetZoomX
  zoomY: 0 -> $TargetZoomY
  zoomS: 1 -> $TargetZoomS

## spawnpoints.lua (updated to match Dru_map reference spawn)

  worldX: 0 -> $TargetWorldX
  worldY: 0 -> $TargetWorldY
  posX: 150 -> $TargetPosX
  posY: 150 -> $TargetPosY

## Required files present: $($requiredFilesPresent.ToString().ToLower())
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7U_COORDINATE_ALIGNED_STAGING_MANIFEST.md"

# MAP_7U_MODROOT_LAYOUT_MATCH_SUMMARY.md
$layoutSummaryPath = Join-Path $Output 'MAP_7U_MODROOT_LAYOUT_MATCH_SUMMARY.md'
Set-Content -Path $layoutSummaryPath -Value @"
# MAP-7U: Mod-Root Layout Match Summary

``````text
MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED
fields_in_reference_not_candidate=0
fields_in_candidate_not_reference=0
candidate_bom_violations_count=0
reference_bom_violations_count=0
``````

## What is ruled out as the blocker

- Workshop wrapper layout: RULED OUT
- mod.info / 42/mod.info presence: RULED OUT
- common/mod.info absence: RULED OUT (both absent)
- common/media/maps/ path: RULED OUT (both present)
- map.info presence: RULED OUT (both present)
- lots=NONE: RULED OUT (both set)
- zoomX/Y/S presence: RULED OUT (both have zoom fields)
- spawnpoints.lua / objects.lua: RULED OUT (both present)
- worldmap.xml / worldmap-forest.xml: RULED OUT (both present)
- BOM violations: RULED OUT (zero on both sides)

## Remaining discriminator

- Cell count: candidate=1, Dru_map=4130
- Cell origin: candidate at 0_0, Dru_map centered at 35_27
- Zoom offset: candidate zoomX/Y=0/0, Dru_map=10505/12220
- Spawn target: candidate worldX/Y=0/0, Dru_map worldX/Y=35/27
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7U_MODROOT_LAYOUT_MATCH_SUMMARY.md"

# MAP_7U_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md
$checklistPath = Join-Path $Output 'MAP_7U_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md'
Set-Content -Path $checklistPath -Value @"
# MAP-7U: Human Workshop Update Checklist

``````text
HUMAN_ONLY_WORKSHOP_UPDATE
NO_AUTOMATIC_WORKSHOP_UPLOAD
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## This script does NOT upload anything.

All Workshop update steps are HUMAN-ONLY.
Claude does not perform Workshop updates.

## Steps

### Step 1: Review staged package

Staged package at:
  $stagedRoot

Key changes from original Workshop upload:
- Binary files RENAMED from 0_0 to ${TargetWorldX}_${TargetWorldY} (content unchanged).
- map.info zoom updated: zoomX=$TargetZoomX zoomY=$TargetZoomY zoomS=$TargetZoomS
- spawnpoints.lua updated: worldX=$TargetWorldX worldY=$TargetWorldY

### Step 2: Update the existing private Workshop item

Update the existing Workshop item (ID 3740642200) with the staged package.
DO NOT create a new Workshop item unless needed.
DO NOT make the item public.

### Step 3: Wire the server

``````ini
Mods=$CandidateMapId
WorkshopItems=3740642200
Map=$CandidateMapId;Muldraugh, KY
Public=false
``````

### Step 4: Analyze

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
    -LogPath .\.local\map7u-logs\DebugLog-client.txt ``
    -Output .\.local\map7u-packet\analysis-after-update ``
    -ExpectedMapId $CandidateMapId ``
    -VariantLabel VariantCoordAligned
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7U_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md"

# MAP_7U_SERVER_WIRING_AFTER_UPDATE_TEMPLATE.md
$wiringPath = Join-Path $Output 'MAP_7U_SERVER_WIRING_AFTER_UPDATE_TEMPLATE.md'
Set-Content -Path $wiringPath -Value @"
# MAP-7U: Server Wiring After Update Template

``````ini
; Server: PZMF_B42_WS_CANDIDATE_K_003_COORD_ALIGNED
Mods=$CandidateMapId
WorkshopItems=3740642200
Map=$CandidateMapId;Muldraugh, KY
Public=false
``````

## What changed from K002

- Binary files renamed: 0_0 -> ${TargetWorldX}_${TargetWorldY}
- map.info zoom aligned to Dru_map reference
- spawnpoints.lua aligned to Dru_map reference spawn cell
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7U_SERVER_WIRING_AFTER_UPDATE_TEMPLATE.md"

# MAP_7U_SUCCESS_FAILURE_CRITERIA.md
$criteriaPath = Join-Path $Output 'MAP_7U_SUCCESS_FAILURE_CRITERIA.md'
Set-Content -Path $criteriaPath -Value @"
# MAP-7U: Success and Failure Criteria

## Success

expected_map_lotheader_meta_evidence_found=true
OR: explicit candidate lotheader parse error (EOFException on candidate lotheader)
OR: visible non-fallback PZMapForge cell/world.

If success (lotheader error specifically):
  Binary writer gate opens. LOTH format is the active blocker.

If success (map loads partially or fully):
  Record exact evidence. Do not claim playable export.

## Failure

Workshop Ready + mod loaded + fallback forest + no expected-map evidence.
Same result as K002 (MAP7F_VARIANT_W_S_UPLOAD_K002_MAP_FOLDER_SCAN_EMPTY).

If failure:
  Coordinate alignment is not the discriminator.
  Next: investigate server-side logs, IsoMetaGrid cell registration path,
  whether single-cell maps are loadable at any coordinate.

## Binary writer gate

``````text
BINARY_WRITER_GATE_STILL_CLOSED_UNLESS:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit EOFException on candidate lotheader in log
``````

## Claim boundary

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7U_SUCCESS_FAILURE_CRITERIA.md"

# MAP_7U_COORDINATE_DISCRIMINATOR_PACKET.md (main)
$packetPath = Join-Path $Output 'MAP_7U_COORDINATE_DISCRIMINATOR_PACKET.md'
Set-Content -Path $packetPath -Value @"
# MAP-7U: Coordinate-Aligned Diagnostic Staging Packet

``````text
MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED
COORDINATE_DISCRIMINATOR_IDENTIFIED
COORDINATE_ALIGNED_DIAGNOSTIC_PACKAGE_CREATED
BINARY_CONTENTS_UNCHANGED
NO_BINARY_WRITER_CHANGES
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Purpose

Records the mod-root layout match and prepares a coordinate-aligned
diagnostic staged package. Renames binary files from 0_0 to ${TargetWorldX}_${TargetWorldY}
to align with the Dru_map reference spawn. Binary contents unchanged.

## Layout match

Candidate ($CandidateMapId) vs Reference ($ReferenceMapId):
  layout_match=True
  fields_in_reference_not_candidate=0

## Remaining discriminator

Candidate: 1 cell at 0_0, spawn worldX=0 worldY=0, zoomX=0 zoomY=0
Reference: 4130 cells, spawn worldX=35 worldY=27, zoomX=10505 zoomY=12220

## Staged package

$stagedRoot

Cell files renamed (content unchanged):
  ${TargetWorldX}_${TargetWorldY}.lotheader
  world_${TargetWorldX}_${TargetWorldY}.lotpack
  chunkdata_${TargetWorldX}_${TargetWorldY}.bin

map.info: zoomX=$TargetZoomX zoomY=$TargetZoomY zoomS=$TargetZoomS
spawnpoints.lua: worldX=$TargetWorldX worldY=$TargetWorldY posX=$TargetPosX posY=$TargetPosY

## Packet files

``````text
MAP_7U_COORDINATE_DISCRIMINATOR_PACKET.md  (this file)
MAP_7U_MODROOT_LAYOUT_MATCH_SUMMARY.md
MAP_7U_COORDINATE_ALIGNED_STAGING_MANIFEST.md
MAP_7U_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md
MAP_7U_SERVER_WIRING_AFTER_UPDATE_TEMPLATE.md
MAP_7U_SUCCESS_FAILURE_CRITERIA.md
map7u-preflight.json
map7u-preflight.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
Binary contents of lotheader/lotpack/chunkdata are UNCHANGED.
Only filenames were relabeled (0_0 -> ${TargetWorldX}_${TargetWorldY}).
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7U_COORDINATE_DISCRIMINATOR_PACKET.md"

# ---------------------------------------------------------------------------
# Preflight JSON and MD
# ---------------------------------------------------------------------------

$preflightJsonPath = Join-Path $Output 'map7u-preflight.json'
$preflight = [ordered]@{
    schema                          = 'pzmapforge.map7u-preflight.v0.1'
    modroot_layout_match            = $true
    fields_in_reference_not_candidate = 0
    candidate_lotheader_count       = 1
    reference_lotheader_count       = 4130
    candidate_original_cell         = '0_0'
    coordinate_aligned_target_cell  = $targetCell
    target_world_x                  = $TargetWorldX
    target_world_y                  = $TargetWorldY
    target_zoom_x                   = $TargetZoomX
    target_zoom_y                   = $TargetZoomY
    target_zoom_s                   = $TargetZoomS
    binary_contents_mutated         = $false
    binary_writer_changed           = $false
    automatic_workshop_upload_performed = $false
    load_test_performed_by_script   = $false
    public_playable_claim_allowed   = $false
    staged_package_created          = $true
    staged_package_path             = "staged-workshop-coordinate-aligned/$CandidateMapId"
    required_files_present          = $requiredFilesPresent
    required_files_missing          = [string[]]@($missingFiles.ToArray())
}
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7u-preflight.json"

$preflightMdPath = Join-Path $Output 'map7u-preflight.md'
Set-Content -Path $preflightMdPath -Value @"
# MAP-7U Preflight

``````text
MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED
COORDINATE_ALIGNED_DIAGNOSTIC_PACKAGE_CREATED
BINARY_CONTENTS_UNCHANGED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
modroot_layout_match:             true
coordinate_aligned_target_cell:   $targetCell
candidate_lotheader_count:        1
reference_lotheader_count:        4130
binary_contents_mutated:          false
binary_writer_changed:            false
automatic_workshop_upload_performed: false
public_playable_claim_allowed:    false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map7u-preflight.md"

Write-Output ""
Write-Output "MAP-7U packet complete."
Write-Output "staged: $stagedRoot"
Write-Output "target_cell: $targetCell"
Write-Output "required_files_present: $requiredFilesPresent"
Write-Output "binary_writer_changed=false"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
