#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-8H: Stages a parent/child map contract probe package.

    Parent folder PZMapForge: contains generated cell binaries (35_27.*).
    Child folder pzmapforge_build42_candidate_v4_001: city-selector metadata only.
    Layout mirrors Project Russia parent/child contract (common\media\maps).

    Does NOT copy Project Russia files. Does NOT run PZ. Does NOT upload to Workshop.
    Does NOT write to Steam/PZ folders. Does NOT change binary writer behavior.
    All output under .local/ only.

.PARAMETER Output
    Required. Path under .local/.

.PARAMETER ParentMapId
    Optional. Parent map folder ID. Defaults to PZMapForge.

.PARAMETER ChildMapId
    Optional. Child city map ID. Defaults to pzmapforge_build42_candidate_v4_001.

.PARAMETER WorkshopId
    Optional. Defaults to 3740642200.
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$ParentMapId  = 'PZMapForge',
    [string]$ChildMapId   = 'pzmapforge_build42_candidate_v4_001',
    [string]$WorkshopId   = '3740642200'
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

Write-Output "MAP-8H: Parent/Child Map Contract Probe Packet"
Write-Output "Output:      $Output"
Write-Output "ParentMapId: $ParentMapId"
Write-Output "ChildMapId:  $ChildMapId"
Write-Output "WorkshopId:  $WorkshopId"
Write-Output ""

$targetCell = '35_27'

# ---------------------------------------------------------------------------
# Step 1: Source coordinate-aligned cell binaries
# ---------------------------------------------------------------------------

# Try MAP-7Y staged binaries first (already 35_27.*)
$map7yStagedMapsDir = Join-Path $repoRoot ".local\map7y-packet\staged-workshop-sidecar-stubs\$ChildMapId\common\media\maps\$ChildMapId"
$usedPriorSource    = $false

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
        --map-id $ChildMapId `
        --output $candidateGenOut `
        --build42-candidate-writer `
        --build42-candidate-profile empty_grass_v4
    if ($LASTEXITCODE -ne 0) { Write-Error "CLI candidate generation failed."; exit 1 }
    $candDir = Join-Path $candidateGenOut ($ChildMapId + '_build42_candidate')
    $binarySourceDir = Join-Path $candDir "42\media\maps\$ChildMapId"
}

# ---------------------------------------------------------------------------
# Step 2: Build staged package
# ---------------------------------------------------------------------------

$stagedRoot    = Join-Path $Output "staged-workshop-parent-child\$ChildMapId"
$parentMapsDir = Join-Path $stagedRoot "common\media\maps\$ParentMapId"
$childMapsDir  = Join-Path $stagedRoot "common\media\maps\$ChildMapId"
$staged42Dir   = Join-Path $stagedRoot '42'

New-Item -ItemType Directory -Force -Path $parentMapsDir | Out-Null
New-Item -ItemType Directory -Force -Path $childMapsDir  | Out-Null
New-Item -ItemType Directory -Force -Path $staged42Dir   | Out-Null

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

# ---------------------------------------------------------------------------
# mod.info files
# ---------------------------------------------------------------------------

$modInfoContent = "id=$ChildMapId`nname=PZMapForge MAP-8H Parent/Child Probe`nmodversion=1.0`n"
[System.IO.File]::WriteAllText(
    (Join-Path $stagedRoot 'mod.info'), $modInfoContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  mod.info: root"

[System.IO.File]::WriteAllText(
    (Join-Path $staged42Dir 'mod.info'), $modInfoContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  42\mod.info: versioned"

# poster.png
Write-PlaceholderPng (Join-Path $stagedRoot 'poster.png')
Write-Output "  poster.png: placeholder"

# ---------------------------------------------------------------------------
# PARENT map folder: PZMapForge
# Contains generated cell binaries and is the IsoMetaGrid cell-loading target.
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "Building parent map folder ($ParentMapId)..."

# Parent map.info: title=PZMapForge, fixed2x=true, NO lots field
$parentMapInfo = @"
title=$ParentMapId
fixed2x=true
description=PZMapForge parent playable cell map. Diagnostic only. Not a playable claim.
"@
[System.IO.File]::WriteAllText(
    (Join-Path $parentMapsDir 'map.info'), $parentMapInfo,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  map.info: title=$ParentMapId fixed2x=true (no lots field)"

# Parent spawnpoints.lua (worldX=35, worldY=27, function SpawnPoints() style)
$parentSpawn = @"
-- PZMapForge MAP-8H parent map: $ParentMapId
-- Target cell: $targetCell (worldX=35, worldY=27)
-- Not a playable claim. Diagnostic probe only.
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
    (Join-Path $parentMapsDir 'spawnpoints.lua'), $parentSpawn,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  spawnpoints.lua: worldX=35 worldY=27"

# Parent objects.lua (comment-only)
[System.IO.File]::WriteAllText(
    (Join-Path $parentMapsDir 'objects.lua'),
    "-- MAP-8H parent map $ParentMapId`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  objects.lua: comment-only"

# Parent worldmap.xml (minimal stub)
[System.IO.File]::WriteAllText(
    (Join-Path $parentMapsDir 'worldmap.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  worldmap.xml: minimal stub"

# Parent thumb.png (placeholder)
Write-PlaceholderPng (Join-Path $parentMapsDir 'thumb.png')
Write-Output "  thumb.png: placeholder"

# Parent canary
[System.IO.File]::WriteAllText(
    (Join-Path $parentMapsDir 'MAP8H_PARENT_CHILD_PROBE.txt'),
    "MAP8H_PARENT_FOLDER=$ParentMapId`nMAP8H_PARENT_CHILD_PROBE=true`nSOURCE_BASIS=MAP-8F_and_Project_Russia`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  MAP8H_PARENT_CHILD_PROBE.txt: canary"

# Parent cell binaries (coordinate-aligned, content unchanged)
$binaryFiles   = @("$targetCell.lotheader", "world_$targetCell.lotpack", "chunkdata_$targetCell.bin")
$binaryPresent = $true

foreach ($f in $binaryFiles) {
    $src = Join-Path $binarySourceDir $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $parentMapsDir $f) -Force
        Write-Output "  Staged binary: $f"
    } else {
        $alt = $f -replace '35_27', '0_0'
        $srcAlt = Join-Path $binarySourceDir $alt
        if (Test-Path -LiteralPath $srcAlt) {
            Copy-Item -LiteralPath $srcAlt -Destination (Join-Path $parentMapsDir $f) -Force
            Write-Output "  Staged binary (renamed): $alt -> $f"
        } else {
            $binaryPresent = $false
            Write-Output "  WARNING: binary not found: $f"
        }
    }
}

# ---------------------------------------------------------------------------
# CHILD map folder: pzmapforge_build42_candidate_v4_001
# City-selector UI layer only; no cell binaries.
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "Building child map folder ($ChildMapId)..."

# Child map.info: lots=PZMapForge, zoomX/Y/S
$childMapInfo = @"
title=$ChildMapId
lots=$ParentMapId
description=PZMapForge child city selector layer. Diagnostic only. Not a playable claim.
zoomX=10505
zoomY=12220
zoomS=14.5
"@
[System.IO.File]::WriteAllText(
    (Join-Path $childMapsDir 'map.info'), $childMapInfo,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  map.info: lots=$ParentMapId zoomX=10505 zoomY=12220"

# Child spawnpoints.lua (same coordinates as parent -- city selector reference)
$childSpawn = @"
-- PZMapForge MAP-8H child city selector: $ChildMapId
-- lots=$ParentMapId (parent provides cell binaries)
-- Not a playable claim. Diagnostic probe only.
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
    (Join-Path $childMapsDir 'spawnpoints.lua'), $childSpawn,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  spawnpoints.lua: worldX=35 worldY=27"

# Child objects.lua (comment-only)
[System.IO.File]::WriteAllText(
    (Join-Path $childMapsDir 'objects.lua'),
    "-- MAP-8H child map $ChildMapId`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  objects.lua: comment-only"

# Child worldmap.xml (minimal stub)
[System.IO.File]::WriteAllText(
    (Join-Path $childMapsDir 'worldmap.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  worldmap.xml: minimal stub"

# ---------------------------------------------------------------------------
# Step 3: Verify staged package
# ---------------------------------------------------------------------------

$parentRequired = @(
    "common\media\maps\$ParentMapId\map.info",
    "common\media\maps\$ParentMapId\spawnpoints.lua",
    "common\media\maps\$ParentMapId\objects.lua",
    "common\media\maps\$ParentMapId\worldmap.xml",
    "common\media\maps\$ParentMapId\$targetCell.lotheader",
    "common\media\maps\$ParentMapId\world_$targetCell.lotpack",
    "common\media\maps\$ParentMapId\chunkdata_$targetCell.bin",
    "common\media\maps\$ParentMapId\MAP8H_PARENT_CHILD_PROBE.txt"
)
$childRequired = @(
    "common\media\maps\$ChildMapId\map.info",
    "common\media\maps\$ChildMapId\spawnpoints.lua",
    "common\media\maps\$ChildMapId\objects.lua",
    "common\media\maps\$ChildMapId\worldmap.xml"
)

$missingFiles  = [System.Collections.Generic.List[string]]::new()
$manifestLines = [System.Collections.Generic.List[string]]::new()

foreach ($rel in ($parentRequired + $childRequired)) {
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
# Step 4: Verify parent map.info has no lots field
# ---------------------------------------------------------------------------

$parentMapInfoContent = Get-Content -LiteralPath (Join-Path $parentMapsDir 'map.info') -Raw
$parentHasLotsField   = $parentMapInfoContent -match '(?m)^lots='
$childMapInfoContent  = Get-Content -LiteralPath (Join-Path $childMapsDir 'map.info') -Raw
$childHasLotsParent   = $childMapInfoContent -match "lots=$ParentMapId"

# ---------------------------------------------------------------------------
# Step 5: Write packet docs
# ---------------------------------------------------------------------------

$manifestPath = Join-Path $Output 'MAP_8H_STAGED_PACKAGE_MANIFEST.md'
Set-Content -Path $manifestPath -Value @"
# MAP-8H: Staged Package Manifest

``````text
MAP8H_PARENT_CHILD_CONTRACT_PROBE_STAGED
MAP8H_COMMON_MEDIA_MAPS_PARENT_CHILD_LAYOUT
NO_PROJECT_RUSSIA_FILES_COPIED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Package root

$stagedRoot

## Parent map folder ($ParentMapId)

$(($manifestLines | Where-Object { $_ -match $ParentMapId } | ForEach-Object { $_ }) -join "`n")

## Child map folder ($ChildMapId)

$(($manifestLines | Where-Object { $_ -match $ChildMapId -and $_ -notmatch $ParentMapId } | ForEach-Object { $_ }) -join "`n")

## Status

``````text
required_files_present:         $($requiredFilesPresent.ToString().ToLower())
parent_has_lots_field:          $($parentHasLotsField.ToString().ToLower())
child_has_lots_parent:          $($childHasLotsParent.ToString().ToLower())
binary_writer_gate_closed:      true
public_playable_claim_allowed:  false
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_8H_STAGED_PACKAGE_MANIFEST.md"

$checklistPath = Join-Path $Output 'MAP_8H_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md'
Set-Content -Path $checklistPath -Value @"
# MAP-8H: Human Workshop Update Checklist

``````text
HUMAN_ONLY_WORKSHOP_UPDATE
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_PZ_RUN_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## This script does NOT upload anything.

All Workshop update and server wiring steps are HUMAN-ONLY.

## Steps

### Step 1: Verify staged package

Staged at: $stagedRoot

Confirm:
- common\media\maps\$ParentMapId\ has cell binaries (35_27.*)
- common\media\maps\$ParentMapId\map.info has title=$ParentMapId, fixed2x=true, NO lots field
- common\media\maps\$ChildMapId\map.info has lots=$ParentMapId

### Step 2: Update existing Workshop item $WorkshopId

Update the EXISTING private Workshop item $WorkshopId.
DO NOT create a new Workshop item.
DO NOT make it public.

### Step 3: Wire the server

``````ini
Mods=$ChildMapId
WorkshopItems=$WorkshopId
Map=$ChildMapId;$ParentMapId;Muldraugh, KY
Public=false
``````

Note: child map ID first (city selector), parent second (cell loading), Muldraugh fallback.

### Step 4: Key signals to watch

- IsoMetaGrid logs $ParentMapId in map folder list: parent folder mounted.
- Player spawns near worldX=35, worldY=27: cell binary loaded from parent.
- City selector shows $ChildMapId as selectable: child lots= contract honored.
- IsoMetaGrid still empty: parent/child contract alone not sufficient.

### Step 5: Capture and analyze logs

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
    -LogPath .\.local\map8h-logs\DebugLog-client.txt ``
    -Output .\.local\map8h-packet\analysis-after-upload ``
    -ExpectedMapId $ParentMapId ``
    -VariantLabel VariantParentChild
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_8H_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md"

$packetPath = Join-Path $Output 'MAP_8H_PARENT_CHILD_CONTRACT_PROBE_PACKET.md'
Set-Content -Path $packetPath -Value @"
# MAP-8H: Parent/Child Map Contract Probe Packet

``````text
MAP8H_PARENT_CHILD_CONTRACT_PROBE_STAGED
MAP8H_COMMON_MEDIA_MAPS_PARENT_CHILD_LAYOUT
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PROJECT_RUSSIA_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````

## Source basis

Project Russia parent/child contract: lots=<ParentId> in child, cell binaries in parent.

## Layout

``````text
common\media\maps\$ParentMapId\         <- PARENT (IsoMetaGrid cell target)
  map.info                title=$ParentMapId, fixed2x=true, no lots
  35_27.lotheader         generated cell binary
  world_35_27.lotpack     generated cell binary
  chunkdata_35_27.bin     generated cell binary
  spawnpoints.lua         worldX=35, worldY=27
  objects.lua
  worldmap.xml
  thumb.png
  MAP8H_PARENT_CHILD_PROBE.txt (canary)

common\media\maps\$ChildMapId\          <- CHILD (city-selector UI)
  map.info                lots=$ParentMapId, zoomX=10505/Y=12220/S=14.5
  spawnpoints.lua         worldX=35, worldY=27
  objects.lua
  worldmap.xml
``````

## Server Map line

``````ini
Map=$ChildMapId;$ParentMapId;Muldraugh, KY
``````

## Packet files

``````text
MAP_8H_PARENT_CHILD_CONTRACT_PROBE_PACKET.md  (this file)
MAP_8H_STAGED_PACKAGE_MANIFEST.md
MAP_8H_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md
map8h-preflight.json
map8h-preflight.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
BINARY_WRITER_GATE_STILL_CLOSED
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PROJECT_RUSSIA_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_8H_PARENT_CHILD_CONTRACT_PROBE_PACKET.md"

# ---------------------------------------------------------------------------
# Preflight JSON
# ---------------------------------------------------------------------------

$preflightJsonPath = Join-Path $Output 'map8h-preflight.json'
$preflight = [ordered]@{
    schema                                = 'pzmapforge.map8h-preflight.v0.1'
    source_basis                          = 'MAP-8F_and_Project_Russia_contract_observation'
    reference_contract                    = 'Project Russia child lots=Project Russia, parent contains playable cells'
    parent_map_id                         = $ParentMapId
    child_map_id                          = $ChildMapId
    workshop_id                           = $WorkshopId
    layout                                = 'common_media_maps_parent_child'
    parent_contains_generated_cell_binaries = $binaryPresent
    child_contains_spawn_selector_metadata = $true
    parent_map_info_has_no_lots_field     = (-not $parentHasLotsField)
    child_map_info_lots_parent            = $childHasLotsParent
    server_map_line                       = "$ChildMapId;$ParentMapId;Muldraugh, KY"
    no_third_party_files_copied           = $true
    no_project_russia_files_copied        = $true
    no_pz_run_by_script                   = $true
    no_workshop_upload_by_script          = $true
    no_binary_writer_changes              = $true
    binary_writer_gate_closed             = $true
    playable_claim_allowed                = $false
    required_files_present                = $requiredFilesPresent
    required_files_missing                = [string[]]@($missingFiles.ToArray())
}
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map8h-preflight.json"

$preflightMdPath = Join-Path $Output 'map8h-preflight.md'
Set-Content -Path $preflightMdPath -Value @"
# MAP-8H Preflight

``````text
MAP8H_PARENT_CHILD_CONTRACT_PROBE_STAGED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
source_basis:                    MAP-8F_and_Project_Russia_contract_observation
parent_map_id:                   $ParentMapId
child_map_id:                    $ChildMapId
layout:                          common_media_maps_parent_child
parent_contains_cell_binaries:   $($binaryPresent.ToString().ToLower())
child_has_lots_parent:           $($childHasLotsParent.ToString().ToLower())
parent_has_no_lots_field:        $( (-not $parentHasLotsField).ToString().ToLower())
server_map_line:                 $ChildMapId;$ParentMapId;Muldraugh, KY
no_third_party_files_copied:     true
no_project_russia_files_copied:  true
binary_writer_gate_closed:       true
required_files_present:          $($requiredFilesPresent.ToString().ToLower())
playable_claim_allowed:          false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map8h-preflight.md"

Write-Output ""
Write-Output "MAP-8H packet complete."
Write-Output "Staged: $stagedRoot"
Write-Output "parent_map_id: $ParentMapId"
Write-Output "child_map_id: $ChildMapId"
Write-Output "parent_contains_generated_cell_binaries: $binaryPresent"
Write-Output "child_map_info_lots: $ParentMapId"
Write-Output "required_files_present: $requiredFilesPresent"
Write-Output "no_third_party_files_copied: true"
Write-Output "no_project_russia_files_copied: true"
Write-Output "binary_writer_gate_closed: true"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
