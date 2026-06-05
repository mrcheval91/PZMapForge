#Requires -Version 5.1
<#
.SYNOPSIS
    Prepares a local-only Build 42 load test packet from a MAP-5D experimental package.

    Validates the MAP-5D source package, reads the embedded report JSON, verifies
    the package is complete, and writes a load-test instruction packet and fillable
    record template under .local only.

    Does NOT copy mod files to any PZ mods or Workshop folder.
    Does NOT touch the PZ install directory.
    Does NOT touch repo media/maps.
    Does NOT copy PZ assets.
    Does NOT claim playable export.
    Both -Source and -Output must be under .local only.

Usage:
    .\scripts\prepare-build42-load-test-packet.ps1 `
        -Source <.local MAP-5D build42_workshop package dir> `
        -Output <.local load-test packet output dir>

Example:
    .\scripts\prepare-build42-load-test-packet.ps1 `
        -Source ".local\map-export-experimental\pzmapforge_test_build42_workshop" `
        -Output ".local\load-tests\b42-test-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Source,

    [Parameter(Mandatory=$true)]
    [string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Output 'prepare-build42-load-test-packet.ps1'
Write-Output "Source: $Source"
Write-Output "Output: $Output"
Write-Output ''

# ---------------------------------------------------------------------------
# Guard: both Source and Output must be under .local
# ---------------------------------------------------------------------------

$sep         = [System.IO.Path]::DirectorySeparatorChar
$sourceFull  = [System.IO.Path]::GetFullPath($Source)
$outputFull  = [System.IO.Path]::GetFullPath($Output)
$localMarker = $sep + '.local' + $sep

function Test-IsUnderLocal {
    param([string]$Path)
    return ($Path.Contains($localMarker) -or $Path.EndsWith($sep + '.local'))
}

if (-not (Test-IsUnderLocal $sourceFull)) {
    Write-Error "prepare-build42-load-test-packet: -Source must be under a .local/ directory: $sourceFull"
    exit 1
}
if (-not (Test-IsUnderLocal $outputFull)) {
    Write-Error "prepare-build42-load-test-packet: -Output must be under a .local/ directory: $outputFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Guard: Source must exist
# ---------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $sourceFull)) {
    Write-Error "Source package not found: $sourceFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Discover report JSON
# ---------------------------------------------------------------------------

Write-Output '--- Reading MAP-5D report ---'

$contentsModsPath = Join-Path $sourceFull 'Contents\mods'
if (-not (Test-Path -LiteralPath $contentsModsPath)) {
    Write-Error "Source does not appear to be a MAP-5D Build 42 package (missing Contents\mods): $sourceFull"
    exit 1
}

$reportJson = $null
$mapId      = $null
$cellX      = 0
$cellY      = 0
$modRoot    = $null

$modDirs = @(Get-ChildItem -LiteralPath $contentsModsPath -Directory -ErrorAction SilentlyContinue)
foreach ($modDir in $modDirs) {
    $candidate = Join-Path $modDir.FullName 'experimental-map-export-report.json'
    if (-not (Test-Path -LiteralPath $candidate)) { continue }
    $reportJson = Get-Content -LiteralPath $candidate -Raw | ConvertFrom-Json
    $mapId      = $reportJson.map_id
    $cellX      = [int]$reportJson.cell_x
    $cellY      = [int]$reportJson.cell_y
    $modRoot    = $modDir.FullName
    break
}

if ($null -eq $reportJson) {
    Write-Error "Could not find experimental-map-export-report.json under Contents\mods\ in: $sourceFull"
    exit 1
}

# ---------------------------------------------------------------------------
# Validate key report flags
# ---------------------------------------------------------------------------

if ($reportJson.playable_export_generated -ne $false) {
    Write-Error "Source report has playable_export_generated=true. Not a valid MAP-5D output."
    exit 1
}
if ($reportJson.experimental_writer -ne $true) {
    Write-Error "Source report does not have experimental_writer=true."
    exit 1
}
if ($reportJson.package_layout -ne 'build42_workshop') {
    Write-Error "Source report package_layout is not build42_workshop (got: $($reportJson.package_layout))."
    exit 1
}

Write-Output "  map_id:         $mapId"
Write-Output "  cell:           ($cellX, $cellY)"
Write-Output "  package_layout: $($reportJson.package_layout)"
Write-Output "  load_tested:    $($reportJson.load_tested)"
Write-Output ''

# ---------------------------------------------------------------------------
# Verify required files exist in source
# ---------------------------------------------------------------------------

Write-Output '--- Verifying source package files ---'

$cellCoord   = "${cellX}_${cellY}"
$mapDataDir  = Join-Path $modRoot "media\maps\$mapId"

$expectedFiles = @(
    (Join-Path $sourceFull 'workshop.txt')
    (Join-Path $sourceFull 'preview.png')
    (Join-Path $modRoot 'mod.info')
    (Join-Path $modRoot 'poster.png')
    (Join-Path $mapDataDir 'map.info')
    (Join-Path $mapDataDir 'spawnpoints.lua')
    (Join-Path $mapDataDir 'objects.lua')
    (Join-Path $mapDataDir 'thumb.png')
    (Join-Path $mapDataDir 'README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt')
    (Join-Path $mapDataDir "${cellCoord}.lotheader")
    (Join-Path $mapDataDir "world_${cellCoord}.lotpack")
    (Join-Path $mapDataDir "chunkdata_${cellCoord}.bin")
)

$allPresent = $true
foreach ($fp in $expectedFiles) {
    if (Test-Path -LiteralPath $fp) {
        $size = (Get-Item -LiteralPath $fp).Length
        Write-Output ("  OK  {0,-70} {1,8}b" -f ($fp.Replace($sourceFull, '').TrimStart($sep) -replace '\\', '/'), $size)
    } else {
        Write-Output "  MISSING  $($fp.Replace($sourceFull, '').TrimStart($sep) -replace '\\', '/')"
        $allPresent = $false
    }
}

if (-not $allPresent) {
    Write-Error "Source package is missing required files. Run map-export-experimental --build42-package first."
    exit 1
}

Write-Output ''

# ---------------------------------------------------------------------------
# Write load test packet
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$generatedAt   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$workshopDest  = "C:\Users\Palmacede\Zomboid\Workshop\$mapId"
$modsDest      = "C:\Users\Palmacede\Zomboid\mods\$mapId"
$packageName   = [System.IO.Path]::GetFileName($sourceFull)

$packetContent = @"
# Build 42 Experimental Package Load Test Packet

Generated:      $generatedAt
Source:         $($sourceFull -replace '\\', '/')
Map ID:         $mapId
Cell:           ($cellX, $cellY)
Package layout: build42_workshop

## Claim boundary

experimental_local_only_not_playable_not_load_tested

This output is experimental. Binary files are hypothesis-only.
Not a playable Project Zomboid map. Not load-tested.
MAP-5B result: LOAD_TEST_INCONCLUSIVE (packaging blocker, binary files untested).

## What is being tested

Three hypothesis-only binary files for one empty compiled cell:
- ${cellCoord}.lotheader (8 bytes): zero header + 0-entry tileset list
- world_${cellCoord}.lotpack (7208 bytes): hdrA=900/hdrB=7204/all-zero offsets
- chunkdata_${cellCoord}.bin (902 bytes): 0x0001 header + 900 zero bytes

## Step 1: verify the package is complete

Run the MAP-5E self-inspection command from the repo root:

    dotnet run --project src/PZMapForge.Cli -- `
      inspect-build42-experimental-package `
      --package "$($sourceFull -replace '\\', '/')" `
      --output ".local/inspections/$mapId"

All 21 checks should show PASS before proceeding.

## Step 2: locate the package source folder

Source folder (already verified complete):
$($sourceFull -replace '\\', '/')

This folder IS the Build 42 Workshop package. It has the correct layout:
  workshop.txt
  preview.png
  Contents/mods/$mapId/mod.info  (category=map, pzversion=42.0)
  Contents/mods/$mapId/media/maps/$mapId/<cell files>

## Step 3: copy the package to your PZ Workshop folder

Copy the ENTIRE source folder to your local PZ Workshop directory.
The destination folder name must match the package folder name.

Destination (Workshop item folder):
$workshopDest

Example (PowerShell -- run MANUALLY, NOT automatic):

    Copy-Item -Recurse -Force ``
        "$sourceFull" ``
        "$workshopDest"

Do NOT copy to the PZ install directory (steamapps).
Use ONLY the user Zomboid folder.

NOTE: Build 42 may require Steam Workshop subscription for mods to appear.
If the Workshop folder approach fails, try placing the Contents/mods/<map_id>/
subfolder directly under the loose mods path:
  $modsDest

## Step 4: launch Project Zomboid (Build 42)

1. Launch Project Zomboid Build 42.
2. Go to Mods menu and check whether "$mapId" appears.
3. If visible: enable the mod and start a new Sandbox game.
4. Observe whether the experimental cell is accessible.
5. Check the PZ log: C:\Users\Palmacede\Zomboid\Logs\

## Step 5: fill in the record template

Open:
$($outputFull -replace '\\', '/')\BUILD42_LOAD_TEST_RECORD.local-template.md

Record your result:
  LOAD_TEST_PASS         -- mod loads, cell accessible, no crash
  LOAD_TEST_FAIL         -- mod fails to load, cell missing, or crash
  LOAD_TEST_INCONCLUSIVE -- partial results

## Step 6: clean up

After the test, remove the experimental mod from the mods/Workshop folder.
Do not leave experimental mods installed permanently.

## Non-claims

- This packet does not perform the load test.
- No files were copied to PZ folders by generating this packet.
- No PZ install was modified.
- No PZ assets were copied.
- No playable export claim.
"@

$packetPath = Join-Path $outputFull 'BUILD42_LOAD_TEST_PACKET.md'
Set-Content -Path $packetPath -Value $packetContent -Encoding UTF8
Write-Output "Load test packet: $packetPath"

# ---------------------------------------------------------------------------
# Write fillable record template
# ---------------------------------------------------------------------------

$templateContent = @"
# Build 42 Experimental Package Load Test Record

Schema:           pzmapforge.load-test-record.v0.1
Claim boundary:   experimental_local_only_not_playable_not_load_tested
Source:           $($sourceFull -replace '\\', '/')
Map ID:           $mapId
Cell:             ($cellX, $cellY)
Package layout:   build42_workshop
Generated:        $generatedAt

---

## Test metadata

| Field | Value |
|---|---|
| Date / time | <!-- YYYY-MM-DD HH:MM --> |
| Tester | <!-- operator name --> |
| PZ version | <!-- Build 42.x --> |
| PZ install path | <!-- local path --> |
| Destination used | <!-- Workshop folder or mods folder --> |
| map_id | $mapId |
| Cell | ($cellX, $cellY) |

---

## Safety confirmation

| Property | Value |
|---|---|
| Package copied from .local only | <!-- yes --> |
| PZ install directory modified | <!-- no (must be no) --> |
| Repo media/maps touched | <!-- no (must be no) --> |
| PZ assets copied into repo | <!-- no (must be no) --> |

---

## Observation checklist

| Observation | Result | Notes |
|---|---|---|
| Mod "$mapId" appears in PZ Mods screen | <!-- yes / no --> | |
| Map location appears in spawn/map selection | <!-- yes / no / unknown --> | |
| Game starts without crashing | <!-- yes / no --> | |
| PZ crashed during load | <!-- yes / no --> | |
| Experimental cell visible on map | <!-- yes / no / unknown --> | |
| Player can access cell | <!-- yes / no / unknown --> | |
| Error messages observed | <!-- yes / no --> | |

---

## Error messages and log excerpts

``````
<!-- Paste relevant PZ log lines here. -->
``````

---

## Observed behavior

<!-- Describe what happened. -->

---

## Result

``````
RESULT: <!-- LOAD_TEST_PASS / LOAD_TEST_FAIL / LOAD_TEST_INCONCLUSIVE -->
``````

| Property | Value |
|---|---|
| playable_claim_allowed | false -- not until result reviewed |
| load_tested | true |
| load_test_result | <!-- PASS / FAIL / INCONCLUSIVE --> |
| pz_version_tested | <!-- fill in --> |

---

## Non-claims

- LOAD_TEST_PASS does not constitute a public playable export claim without review.
- MAP-5A binary hypotheses remain UNTESTED until PASS is confirmed and documented.
"@

$templatePath = Join-Path $outputFull 'BUILD42_LOAD_TEST_RECORD.local-template.md'
Set-Content -Path $templatePath -Value $templateContent -Encoding UTF8
Write-Output "Record template:  $templatePath"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output "========================================"
Write-Output "Build 42 load test packet ready"
Write-Output "========================================"
Write-Output "  Map ID:        $mapId"
Write-Output "  Cell:          ($cellX, $cellY)"
Write-Output "  Packet:        $packetPath"
Write-Output "  Template:      $templatePath"
Write-Output "  Workshop dest: $workshopDest"
Write-Output ''
Write-Output "  Next steps:"
Write-Output "  1. Run inspect-build42-experimental-package to verify 21/21 checks PASS."
Write-Output "  2. Read BUILD42_LOAD_TEST_PACKET.md for copy instructions."
Write-Output "  3. Manually copy package to PZ Workshop folder."
Write-Output "  4. Perform the load test."
Write-Output "  5. Fill in BUILD42_LOAD_TEST_RECORD.local-template.md."
Write-Output ''
Write-Output "  playable_export_claimed:     false"
Write-Output "  pz_assets_copied:            false"
Write-Output "  media_maps_touched_in_repo:  false"
Write-Output "  Status:                      OK"
