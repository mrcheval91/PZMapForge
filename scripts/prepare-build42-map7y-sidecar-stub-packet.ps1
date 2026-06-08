#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7Y: Generates the minimal non-cell sidecar stub diagnostic package.

    Builds a staged Workshop package with generated candidate-owned sidecar stubs.
    Does NOT copy Dru_map sidecar files. Does NOT run PZ. Does NOT upload anything.
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

Write-Output "MAP-7Y: Minimal Sidecar Stub Diagnostic Packet"
Write-Output "Output:       $Output"
Write-Output "CandidateMapId: $CandidateMapId"
Write-Output "WorkshopId:   $WorkshopId"
Write-Output ""

$targetCell = '35_27'

# ---------------------------------------------------------------------------
# Step 1: Source coordinate-aligned binaries
# ---------------------------------------------------------------------------

# Try MAP-7U staged package first
$map7uStagedRoot = Join-Path $repoRoot ".local\map7u-packet\staged-workshop-coordinate-aligned\$CandidateMapId"
$map7uMapsDir    = Join-Path $map7uStagedRoot "common\media\maps\$CandidateMapId"
$usedMap7uSource = $false

if ((Test-Path -LiteralPath $map7uStagedRoot) -and
    (Test-Path -LiteralPath (Join-Path $map7uMapsDir "$targetCell.lotheader"))) {
    Write-Output "Using MAP-7U coordinate-aligned package."
    $binarySourceDir = $map7uMapsDir
    $usedMap7uSource = $true
} else {
    Write-Output "MAP-7U package not found. Generating empty_grass_v4 candidate..."
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
# Step 2: Build staged package
# ---------------------------------------------------------------------------

$stagedRoot    = Join-Path $Output "staged-workshop-sidecar-stubs\$CandidateMapId"
$staged42Dir   = Join-Path $stagedRoot '42'
$stagedMapsDir = Join-Path $stagedRoot "common\media\maps\$CandidateMapId"

New-Item -ItemType Directory -Force -Path $staged42Dir   | Out-Null
New-Item -ItemType Directory -Force -Path $stagedMapsDir | Out-Null

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

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

# mod.info files
if ($usedMap7uSource) {
    $srcModInfo = Join-Path $map7uStagedRoot 'mod.info'
    if (Test-Path -LiteralPath $srcModInfo) {
        Copy-Item -LiteralPath $srcModInfo -Destination (Join-Path $stagedRoot 'mod.info') -Force
        Copy-Item -LiteralPath $srcModInfo -Destination (Join-Path $staged42Dir 'mod.info') -Force
    }
} else {
    $src42ModInfo = Join-Path (Split-Path -Parent $binarySourceDir) '..\..\mod.info'
    $src42ModInfoAlt = Join-Path $repoRoot ".local\candidate-gen\$CandidateMapId`_build42_candidate\42\mod.info"
    $modInfoSrc = if (Test-Path -LiteralPath $src42ModInfoAlt) { $src42ModInfoAlt } else { $null }
    if ($null -ne $modInfoSrc) {
        Copy-Item -LiteralPath $modInfoSrc -Destination (Join-Path $stagedRoot 'mod.info') -Force
        Copy-Item -LiteralPath $modInfoSrc -Destination (Join-Path $staged42Dir 'mod.info') -Force
    } else {
        [System.IO.File]::WriteAllText((Join-Path $stagedRoot 'mod.info'),
            "id=$CandidateMapId`nname=PZMapForge Sidecar Stub Probe`nmodversion=1.0`n",
            [System.Text.UTF8Encoding]::new($false))
        Copy-Item -LiteralPath (Join-Path $stagedRoot 'mod.info') `
                  -Destination (Join-Path $staged42Dir 'mod.info') -Force
    }
}

# poster.png
Write-PlaceholderPng (Join-Path $stagedRoot 'poster.png')

# Binary files (coordinate-aligned)
$binaryFiles = @("$targetCell.lotheader", "world_$targetCell.lotpack", "chunkdata_$targetCell.bin")
$binaryPresent = $true
foreach ($f in $binaryFiles) {
    $src = Join-Path $binarySourceDir $f
    if (-not (Test-Path -LiteralPath $src)) {
        # Try 0_0 files renamed to 35_27 if source is from CLI generation (0_0 layout)
        $alt0 = $f -replace '^35_27', '0_0' -replace '^world_35_27', 'world_0_0' `
                   -replace '^chunkdata_35_27', 'chunkdata_0_0'
        $srcAlt = Join-Path $binarySourceDir $alt0
        if (Test-Path -LiteralPath $srcAlt) {
            Copy-Item -LiteralPath $srcAlt -Destination (Join-Path $stagedMapsDir $f) -Force
            Write-Output "  Staged (renamed): $alt0 -> $f"
        } else {
            $binaryPresent = $false
            Write-Output "  WARNING: binary file not found: $f"
        }
    } else {
        Copy-Item -LiteralPath $src -Destination (Join-Path $stagedMapsDir $f) -Force
        Write-Output "  Staged: $f"
    }
}

# ---------------------------------------------------------------------------
# Step 3: Write coordinate-aligned map.info
# ---------------------------------------------------------------------------

$mapInfoContent = @"
title=$CandidateMapId
lots=NONE
description=PZMapForge MAP-7Y Sidecar Stub Probe. Diagnostic only. Not a playable map.
fixed2x=true
zoomX=10505
zoomY=12220
zoomS=14.5
"@
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'map.info'),
    $mapInfoContent,
    [System.Text.UTF8Encoding]::new($false))
Write-Output "  map.info: lots=NONE zoomX=10505 zoomY=12220 zoomS=14.5"

# ---------------------------------------------------------------------------
# Step 4: Write function SpawnPoints() style spawnpoints.lua
# ---------------------------------------------------------------------------

$spawnContent = @"
-- PZMapForge MAP-7Y Sidecar Stub Probe: function SpawnPoints() style.
-- Target cell: $targetCell (worldX=35, worldY=27)
-- Not a playable PZMapForge map. Diagnostic probe only.
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

# objects.lua
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'objects.lua'),
    "-- MAP-7Y sidecar stub probe`n",
    [System.Text.UTF8Encoding]::new($false))

# worldmap stubs (uncompiled XML)
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'worldmap.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText(
    (Join-Path $stagedMapsDir 'worldmap-forest.xml'),
    "<?xml version=""1.0"" encoding=""UTF-8""?>`n<worldmap />`n",
    [System.Text.UTF8Encoding]::new($false))

# thumb.png
Write-PlaceholderPng (Join-Path $stagedMapsDir 'thumb.png')

# ---------------------------------------------------------------------------
# Step 5: Generate minimal sidecar stubs (from scratch, NOT copied from Dru_map)
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "Generating sidecar stubs from scratch..."

function Write-SidecarStub {
    param([string]$Path, [string]$Marker)
    $bytes = [System.Text.Encoding]::ASCII.GetBytes($Marker + "`n")
    [System.IO.File]::WriteAllBytes($Path, $bytes)
    Write-Output "  Stub: $(Split-Path -Leaf $Path) ($($bytes.Length) bytes)"
}

Write-SidecarStub (Join-Path $stagedMapsDir 'streets.xml.bin') `
    'PZMF_MAP7Y_STUB_streets_xml_bin'

Write-SidecarStub (Join-Path $stagedMapsDir 'worldmap.xml.bin') `
    'PZMF_MAP7Y_STUB_worldmap_xml_bin'

Write-SidecarStub (Join-Path $stagedMapsDir 'worldmap-forest.xml.bin') `
    'PZMF_MAP7Y_STUB_worldmap_forest_xml_bin'

# worldmap.png - generated 256x256 placeholder
Write-PlaceholderPng (Join-Path $stagedMapsDir 'worldmap.png') -Width 256 -Height 256
Write-Output "  worldmap.png (256x256 placeholder)"

$generatedSidecars = [string[]]@('streets.xml.bin','worldmap.xml.bin','worldmap-forest.xml.bin','worldmap.png')

# ---------------------------------------------------------------------------
# Step 6: Verify staged package
# ---------------------------------------------------------------------------

$requiredFiles = @(
    'mod.info', '42\mod.info',
    "common\media\maps\$CandidateMapId\map.info",
    "common\media\maps\$CandidateMapId\spawnpoints.lua",
    "common\media\maps\$CandidateMapId\objects.lua",
    "common\media\maps\$CandidateMapId\$targetCell.lotheader",
    "common\media\maps\$CandidateMapId\world_$targetCell.lotpack",
    "common\media\maps\$CandidateMapId\chunkdata_$targetCell.bin",
    "common\media\maps\$CandidateMapId\streets.xml.bin",
    "common\media\maps\$CandidateMapId\worldmap.xml.bin",
    "common\media\maps\$CandidateMapId\worldmap-forest.xml.bin",
    "common\media\maps\$CandidateMapId\worldmap.png"
)

$missingFiles  = [System.Collections.Generic.List[string]]::new()
$presentFiles  = [System.Collections.Generic.List[string]]::new()
$manifestLines = [System.Collections.Generic.List[string]]::new()

foreach ($rel in $requiredFiles) {
    $abs = Join-Path $stagedRoot $rel
    if (Test-Path -LiteralPath $abs) {
        $size = (Get-Item -LiteralPath $abs).Length
        $presentFiles.Add($rel)
        $manifestLines.Add("  PRESENT  $rel  ($size bytes)")
    } else {
        $missingFiles.Add($rel)
        $manifestLines.Add("  MISSING  $rel")
    }
}

$requiredFilesPresent = ($missingFiles.Count -eq 0)

# ---------------------------------------------------------------------------
# Step 7: Write packet docs
# ---------------------------------------------------------------------------

# MAP_7Y_STAGED_PACKAGE_MANIFEST.md
$manifestPath = Join-Path $Output 'MAP_7Y_STAGED_PACKAGE_MANIFEST.md'
Set-Content -Path $manifestPath -Value @"
# MAP-7Y: Staged Package Manifest

``````text
MAP7Y_SIDECAR_STUB_PROBE_STAGED
SIDECAR_STUBS_GENERATED_FROM_SCRATCH
NO_THIRD_PARTY_FILES_COPIED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Package root

$stagedRoot

## Required files

$(($manifestLines | ForEach-Object { $_ }) -join "`n")

## Sidecar stubs (generated from scratch)

  streets.xml.bin        -- contains ASCII marker PZMF_MAP7Y_STUB_streets_xml_bin
  worldmap.xml.bin       -- contains ASCII marker PZMF_MAP7Y_STUB_worldmap_xml_bin
  worldmap-forest.xml.bin -- contains ASCII marker PZMF_MAP7Y_STUB_worldmap_forest_xml_bin
  worldmap.png           -- 256x256 placeholder PNG

## Status

``````text
required_files_present: $($requiredFilesPresent.ToString().ToLower())
third_party_reference_files_copied: false
bak_sidecars_created: false
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7Y_STAGED_PACKAGE_MANIFEST.md"

# MAP_7Y_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md
$checklistPath = Join-Path $Output 'MAP_7Y_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md'
Set-Content -Path $checklistPath -Value @"
# MAP-7Y: Human Workshop Update Checklist

``````text
HUMAN_ONLY_WORKSHOP_UPDATE
NO_AUTOMATIC_WORKSHOP_UPLOAD
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## This script does NOT upload anything.

All Workshop update steps are HUMAN-ONLY.
Claude does not upload to Steam Workshop.

## Steps

### Step 1: Review staged package

Staged at:
  $stagedRoot

Verify required files are present (see MAP_7Y_STAGED_PACKAGE_MANIFEST.md).
Confirm sidecar stubs contain PZMF_MAP7Y_STUB markers.

### Step 2: Update existing Workshop item $WorkshopId

Update the EXISTING private Workshop item $WorkshopId.
DO NOT create a new Workshop item.
DO NOT make it public.
DO NOT use 3355966216 (that is Dru_map's ID).

After uploading, wait for Steam to finish downloading updates.
Verify the local downloaded payload contains the generated sidecar stubs.

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
    -LogPath .\.local\map7y-logs\DebugLog-client.txt ``
    -Output .\.local\map7y-packet\analysis-after-upload ``
    -ExpectedMapId $CandidateMapId ``
    -VariantLabel VariantSidecarStub
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7Y_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md"

# MAP_7Y_LOG_CAPTURE_AFTER_UPLOAD.md
$logCapturePath = Join-Path $Output 'MAP_7Y_LOG_CAPTURE_AFTER_UPLOAD.md'
Set-Content -Path $logCapturePath -Value @"
# MAP-7Y: Log Capture After Upload

## HUMAN-ONLY: Copy logs to .local before analyzing.

``````powershell
New-Item -ItemType Directory -Force -Path ".\.local\map7y-logs"
Copy-Item "C:\Users\Palmacede\Zomboid\Logs\DebugLog-client.txt" ``
    ".\.local\map7y-logs\DebugLog-sidecar-stub-test.txt"
``````

## Analyzer command

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
    -LogPath .\.local\map7y-logs\DebugLog-sidecar-stub-test.txt ``
    -Output .\.local\map7y-packet\analysis-after-upload ``
    -ExpectedMapId $CandidateMapId ``
    -VariantLabel VariantSidecarStub
``````

## What to look for

Expected_map_lotheader_meta_evidence_found=true:
  Map folder mounting began. Binary writer gate opens.

Explicit sidecar parse/read error (file-read error for streets.xml.bin etc.):
  PZ attempted to read the sidecar. Format is wrong. Next: investigate format.

Fallback forest + empty scan + no sidecar read error:
  Sidecar presence alone was not the discriminator. Continue investigation.

Also check the SERVER-SIDE log:
  Server log may show IsoMetaGrid scan attempt in more detail than client log.

## Claim boundary

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7Y_LOG_CAPTURE_AFTER_UPLOAD.md"

# MAP_7Y_SUCCESS_FAILURE_CRITERIA.md
$criteriaPath = Join-Path $Output 'MAP_7Y_SUCCESS_FAILURE_CRITERIA.md'
Set-Content -Path $criteriaPath -Value @"
# MAP-7Y: Success and Failure Criteria

## Success conditions

1. Candidate map folder appears in IsoMetaGrid scan (map-folder scan non-empty for candidate).
2. expected_map_lotheader_meta_evidence_found=true (candidate lotheader referenced in log).
3. Explicit candidate lotheader parse error (EOFException on candidate lotheader).
4. Explicit sidecar parse/read error referencing streets.xml.bin, worldmap.xml.bin etc.

Conditions 1-3: binary writer gate opens. Resume LOTH/LOTP/chunkdata investigation.
Condition 4: sidecar format investigation required.

## Failure condition

Workshop Ready + mod loaded + spawn honored + fallback forest + empty map scan +
no sidecar read evidence + no lotheader parse error.

Same result as K004/K006. Sidecar presence alone was not the discriminator.

## Binary writer gate

``````text
BINARY_WRITER_GATE_STILL_CLOSED

Opens when:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit EOFException on candidate lotheader

Sidecar .bin stubs are NOT lotheader/lotpack/chunkdata format changes.
Binary writer remains unchanged.
``````

## Non-claims

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7Y_SUCCESS_FAILURE_CRITERIA.md"

# MAP_7Y_MINIMAL_SIDECAR_STUB_PACKET.md (main)
$packetPath = Join-Path $Output 'MAP_7Y_MINIMAL_SIDECAR_STUB_PACKET.md'
Set-Content -Path $packetPath -Value @"
# MAP-7Y: Minimal Non-Cell Sidecar Stub Probe Packet

``````text
MAP7Y_SIDECAR_STUB_PROBE_STAGED
MAP_BIN_DISCRIMINATOR_FALSE
SIDECAR_STUBS_GENERATED_FROM_SCRATCH
NO_THIRD_PARTY_FILES_COPIED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````

## Purpose

Stages a diagnostic Workshop package with generated minimal sidecar stubs to
test whether streets.xml.bin, worldmap.xml.bin, or worldmap-forest.xml.bin
presence is required for IsoMetaGrid map-folder mounting.

## Source

MAP-7X (commit 8c45c0a): map.bin ruled out; non-cell sidecar gap identified.

## Staged sidecars (generated from scratch)

``````text
streets.xml.bin        -- PZMF_MAP7Y_STUB_streets_xml_bin
worldmap.xml.bin       -- PZMF_MAP7Y_STUB_worldmap_xml_bin
worldmap-forest.xml.bin -- PZMF_MAP7Y_STUB_worldmap_forest_xml_bin
worldmap.png           -- 256x256 placeholder PNG
``````

NOT COPIED FROM DRU_MAP. Generated by PZMapForge tooling.

## Coordinate-aligned binaries retained

``````text
$targetCell.lotheader
world_$targetCell.lotpack
chunkdata_$targetCell.bin
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
MAP_7Y_MINIMAL_SIDECAR_STUB_PACKET.md  (this file)
MAP_7Y_STAGED_PACKAGE_MANIFEST.md
MAP_7Y_HUMAN_WORKSHOP_UPDATE_CHECKLIST.md
MAP_7Y_LOG_CAPTURE_AFTER_UPLOAD.md
MAP_7Y_SUCCESS_FAILURE_CRITERIA.md
map7y-preflight.json
map7y-preflight.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
No PZ run performed by script.
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7Y_MINIMAL_SIDECAR_STUB_PACKET.md"

# ---------------------------------------------------------------------------
# Preflight JSON
# ---------------------------------------------------------------------------

$preflightJsonPath = Join-Path $Output 'map7y-preflight.json'
$preflight = [ordered]@{
    schema                           = 'pzmapforge.map7y-preflight.v0.1'
    source_map7x_commit              = '8c45c0a'
    candidate_map_id                 = $CandidateMapId
    workshop_id                      = $WorkshopId
    map_bin_discriminator            = $false
    sidecar_probe_created            = $true
    generated_sidecars               = $generatedSidecars
    bak_sidecars_created             = $false
    third_party_reference_files_copied = $false
    coordinate_aligned_binaries_present = $binaryPresent
    candidate_lotheader              = "$targetCell.lotheader"
    candidate_lotpack                = "world_$targetCell.lotpack"
    candidate_chunkdata              = "chunkdata_$targetCell.bin"
    spawnpoints_function_style       = $true
    binary_writer_gate_closed        = $true
    binary_format_investigation_paused = $true
    public_playable_claim_allowed    = $false
    load_test_performed_by_script    = $false
    automatic_workshop_upload_performed = $false
    binary_writer_changed            = $false
    required_files_present           = $requiredFilesPresent
    required_files_missing           = [string[]]@($missingFiles.ToArray())
}
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7y-preflight.json"

$preflightMdPath = Join-Path $Output 'map7y-preflight.md'
Set-Content -Path $preflightMdPath -Value @"
# MAP-7Y Preflight

``````text
MAP7Y_SIDECAR_STUB_PROBE_STAGED
MAP_BIN_DISCRIMINATOR_FALSE
SIDECAR_STUBS_GENERATED_FROM_SCRATCH
NO_THIRD_PARTY_FILES_COPIED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
map_bin_discriminator:               false
sidecar_probe_created:               true
bak_sidecars_created:                false
third_party_reference_files_copied:  false
coordinate_aligned_binaries_present: $($binaryPresent.ToString().ToLower())
spawnpoints_function_style:          true
binary_writer_gate_closed:           true
required_files_present:              $($requiredFilesPresent.ToString().ToLower())
public_playable_claim_allowed:       false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map7y-preflight.md"

Write-Output ""
Write-Output "MAP-7Y packet complete."
Write-Output "Staged: $stagedRoot"
Write-Output "Generated sidecars: $($generatedSidecars -join ', ')"
Write-Output "required_files_present: $requiredFilesPresent"
Write-Output "bak_sidecars_created=false"
Write-Output "third_party_reference_files_copied=false"
Write-Output "binary_writer_gate_closed=true"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
