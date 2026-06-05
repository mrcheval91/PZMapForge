#Requires -Version 5.1
<#
.SYNOPSIS
    Prepares a local-only spawn-region test packet from a MAP-5D experimental package.

    Reads the MAP-5D source package, generates:
    1. A versioned loose-mod layout copy (the confirmed Build 42 path format).
    2. Three spawn-coordinate variant text patches for testing spawn visibility.
    3. SPAWN_REGION_TEST_PACKET.md with step-by-step test instructions.
    4. SPAWN_REGION_TEST_RECORD.local-template.md for recording results.

    All output under .local only.

    Does NOT copy anything to PZ mods folder.
    Does NOT write to PZ install directory.
    Does NOT touch repo media/maps.
    Does NOT copy PZ assets.
    Does NOT claim playable export.
    Both -Source and -Output must be under .local only.

Usage:
    .\scripts\prepare-spawn-region-test-packet.ps1 `
        -Source <.local MAP-5D build42_workshop package dir> `
        -Output <.local output dir>

Example:
    .\scripts\prepare-spawn-region-test-packet.ps1 `
        -Source ".local\map-export-experimental\pzmapforge_test_build42_workshop" `
        -Output ".local\spawn-region-tests\test-01"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Source,

    [Parameter(Mandatory=$true)]
    [string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Output 'prepare-spawn-region-test-packet.ps1'
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

function Test-IsUnderLocal { param([string]$p); return ($p.Contains($localMarker) -or $p.EndsWith($sep + '.local')) }

if (-not (Test-IsUnderLocal $sourceFull)) {
    Write-Error "prepare-spawn-region-test-packet: -Source must be under .local/: $sourceFull"; exit 1
}
if (-not (Test-IsUnderLocal $outputFull)) {
    Write-Error "prepare-spawn-region-test-packet: -Output must be under .local/: $outputFull"; exit 1
}
if (-not (Test-Path -LiteralPath $sourceFull)) {
    Write-Error "Source package not found: $sourceFull"; exit 1
}

# ---------------------------------------------------------------------------
# Discover report JSON
# ---------------------------------------------------------------------------

Write-Output '--- Reading MAP-5D package ---'

$contentsModsPath = Join-Path $sourceFull 'Contents\mods'
if (-not (Test-Path -LiteralPath $contentsModsPath)) {
    Write-Error "Not a MAP-5D build42_workshop package (missing Contents\mods): $sourceFull"; exit 1
}

$reportJson = $null; $mapId = $null; $cellX = 0; $cellY = 0; $modRoot = $null
foreach ($modDir in @(Get-ChildItem -LiteralPath $contentsModsPath -Directory -ErrorAction SilentlyContinue)) {
    $candidate = Join-Path $modDir.FullName 'experimental-map-export-report.json'
    if (-not (Test-Path -LiteralPath $candidate)) { continue }
    $reportJson = Get-Content -LiteralPath $candidate -Raw | ConvertFrom-Json
    $mapId   = $reportJson.map_id
    $cellX   = [int]$reportJson.cell_x
    $cellY   = [int]$reportJson.cell_y
    $modRoot = $modDir.FullName
    break
}

if ($null -eq $reportJson) {
    Write-Error "Could not find experimental-map-export-report.json under Contents\mods\ in: $sourceFull"
    exit 1
}
if ($reportJson.package_layout -ne 'build42_workshop') {
    Write-Error "package_layout is not build42_workshop (got: $($reportJson.package_layout))."
    exit 1
}

Write-Output "  map_id:   $mapId"
Write-Output "  cell:     ($cellX, $cellY)"
Write-Output ''

# ---------------------------------------------------------------------------
# Generate the versioned loose-mod layout under Output\versioned-mod\
# ---------------------------------------------------------------------------

Write-Output '--- Generating versioned loose-mod layout ---'

$verModDir      = Join-Path $outputFull "versioned-mod\${mapId}\42"
$mapDataSrc     = Join-Path $modRoot "media\maps\$mapId"
$cellCoord      = "${cellX}_${cellY}"

New-Item -ItemType Directory -Force -Path $verModDir | Out-Null
$verMediaDir = Join-Path $verModDir "media\maps\$mapId"
New-Item -ItemType Directory -Force -Path $verMediaDir | Out-Null

# Copy mod-level text files
foreach ($f in @('mod.info', 'poster.png')) {
    $src = Join-Path $modRoot $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $verModDir $f) -Force
        Write-Output ("  copied: $f  ($((Get-Item $src).Length)b)")
    }
}

# Copy map data files
$mapFiles = @('map.info', 'spawnpoints.lua', 'objects.lua', 'thumb.png',
              'README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt',
              "${cellCoord}.lotheader", "world_${cellCoord}.lotpack",
              "chunkdata_${cellCoord}.bin")
foreach ($f in $mapFiles) {
    $src = Join-Path $mapDataSrc $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $verMediaDir $f) -Force
        Write-Output ("  copied: media\maps\$mapId\$f  ($((Get-Item $src).Length)b)")
    }
}

Write-Output "  Versioned layout: $verModDir"
Write-Output ''

# ---------------------------------------------------------------------------
# Generate spawn-coordinate variant text patches
# ---------------------------------------------------------------------------
# Variant A: cell (0,0) + explicit lots=<map_id>  — test if lots field fixes visibility
# Variant B: cell (1,1) + lots=<map_id>           — ModTemplate coordinate style
# Variant C: cell (25,15) + lots=<map_id>         — RED-Speedway coordinate range

Write-Output '--- Generating spawn-coord variant patches ---'

$generatedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$variants = @(
    [ordered]@{ Name = 'variant_a'; WX = 0;  WY = 0;  Desc = 'Cell (0,0) + explicit lots= field' },
    [ordered]@{ Name = 'variant_b'; WX = 1;  WY = 1;  Desc = 'Cell (1,1) matching ModTemplate example' },
    [ordered]@{ Name = 'variant_c'; WX = 25; WY = 15; Desc = 'Cell (25,15) matching RED-Speedway range' }
)

foreach ($v in $variants) {
    $vDir = Join-Path $outputFull "spawn-variants\$($v.Name)"
    New-Item -ItemType Directory -Force -Path $vDir | Out-Null

    $vMapInfo = @"
title=PZMapForge Experimental - $mapId
description=EXPERIMENTAL OUTPUT -- NOT VALIDATED. Spawn-region test variant $($v.Name).
lots=$mapId
"@
    Set-Content -Path (Join-Path $vDir 'map.info') -Value $vMapInfo -Encoding UTF8

    $vCC = "$($v.WX)_$($v.WY)"
    $vSpawn = @"
-- PZMapForge MAP-6A spawn-region test variant $($v.Name).
-- worldX=$($v.WX) worldY=$($v.WY)  ($($v.Desc))
-- Not load-tested. Hypothesis-only.
function SpawnPoints()
return {
  all = {
    { worldX = $($v.WX), worldY = $($v.WY), posX = 150, posY = 150, posZ = 0 }
  },
}
end
"@
    Set-Content -Path (Join-Path $vDir 'spawnpoints.lua') -Value $vSpawn -Encoding UTF8

    Write-Output "  $($v.Name): cell ($($v.WX),$($v.WY)) -- $($v.Desc)"
}

Write-Output ''

# ---------------------------------------------------------------------------
# Write test packet
# ---------------------------------------------------------------------------

$modsDir    = "C:\Users\Palmacede\Zomboid\mods"
$modDest42  = "$modsDir\${mapId}_42\42"

$packetContent = @"
# Build 42 Spawn-Region Test Packet

Generated:      $generatedAt
Source:         $($sourceFull -replace '\\', '/')
Map ID:         $mapId
Current cell:   ($cellX, $cellY)

## Claim boundary

evidence_record_only_not_load_tested_not_playable

## Background: what MAP-6A found

Manual test with versioned loose-mod layout confirmed:
- Mod appeared in Build 42 Mods screen when placed at:
    <mods>\<folder_name>\42\mod.info
- PZ log: "loading <mod_id>" confirmed.
- Spawn-selection screen appeared with only vanilla locations.
- Custom PZMapForge spawn location was NOT visible.

Gap to close: custom spawn location must appear in spawn-selection screen.

## Versioned layout generated under .local

This packet contains a versioned loose-mod layout ready for manual copy:

    Source (this packet):
    $($outputFull -replace '\\', '/')/versioned-mod/$mapId/

    Destination (manually, NOT done by this script):
    $modDest42

The versioned layout at that destination must be:
    $modDest42\mod.info
    $modDest42\media\maps\$mapId\map.info
    $modDest42\media\maps\$mapId\spawnpoints.lua
    (etc.)

Copy command (run MANUALLY):
    Copy-Item -Recurse -Force ``
        "$($outputFull -replace '\\', '/')/versioned-mod/$mapId/42" ``
        "$modDest42"

Then re-enable mod and test.

## Spawn-region test variants

Three spawn-coordinate variants are in spawn-variants\. For each:
1. Replace map.info and spawnpoints.lua in the versioned layout.
2. Re-copy to destination.
3. Launch PZ and check spawn-selection screen.

### Variant A — cell (0,0) + explicit lots field

Files: $($outputFull -replace '\\', '/')/spawn-variants/variant_a/

Hypothesis: adding lots=$mapId to map.info may register the custom location.
Cell coordinates unchanged at (0,0).

### Variant B — cell (1,1) matching ModTemplate

Files: $($outputFull -replace '\\', '/')/spawn-variants/variant_b/

Hypothesis: cell (0,0) may be outside valid world-grid range.
ModTemplate example uses worldX=1/worldY=1. Try this coordinate.

### Variant C — cell (25,15) matching RED-Speedway

Files: $($outputFull -replace '\\', '/')/spawn-variants/variant_c/

Hypothesis: RED-Speedway uses coordinates 25_15 to 26_17 and appears in PZ.
Testing within a confirmed working coordinate range.

Note: for Variant C, you need to regenerate the binary files with matching coordinates:
    dotnet run --project src/PZMapForge.Cli -- map-export-experimental ``
        --map-id $mapId --cell-x 25 --cell-y 15 ``
        --output ".local/map-export-experimental" --build42-package

Then prepare-spawn-region-test-packet.ps1 -Source <new package> -Output <new output>.

## Non-claims

- This packet does not copy files to PZ folders.
- No PZ install was modified.
- No load test performed.
- Binary hypotheses remain UNTESTED.
- No playable export claim.
- MAP-5B remains LOAD_TEST_INCONCLUSIVE.
"@

$packetPath = Join-Path $outputFull 'SPAWN_REGION_TEST_PACKET.md'
Set-Content -Path $packetPath -Value $packetContent -Encoding UTF8
Write-Output "Packet:   $packetPath"

# ---------------------------------------------------------------------------
# Write record template
# ---------------------------------------------------------------------------

$recordContent = @"
# Build 42 Spawn-Region Test Record

Claim boundary:   evidence_record_only_not_load_tested_not_playable
Source:           $($sourceFull -replace '\\', '/')
Map ID:           $mapId
Generated:        $generatedAt

---

## Variant tested

| Field | Value |
|---|---|
| Variant | <!-- A / B / C --> |
| Cell X | <!-- 0 / 1 / 25 --> |
| Cell Y | <!-- 0 / 1 / 15 --> |
| lots field value | <!-- $mapId or blank --> |
| Date / time | <!-- YYYY-MM-DD HH:MM --> |
| PZ version | <!-- Build 42.x --> |

---

## Observation checklist

| Observation | Result |
|---|---|
| Mod appears in Mods screen | <!-- yes / no --> |
| Custom spawn location visible in spawn-selection | <!-- yes / no --> |
| Spawn location label | <!-- e.g. "$mapId" or not visible --> |
| Player can select the spawn location | <!-- yes / no / not reached --> |
| Player spawns into the world | <!-- yes / no / not reached --> |
| PZ log shows loading the mod | <!-- yes / no --> |
| Any errors in PZ log | <!-- yes / no --> |

---

## Log excerpts

``````
<!-- Paste relevant PZ log lines. -->
``````

---

## Result

``````
RESULT: <!-- SPAWN_VISIBLE / SPAWN_NOT_VISIBLE / INCONCLUSIVE -->
``````

---

## Next step based on result

<!-- What to investigate next? -->
"@

$recordPath = Join-Path $outputFull 'SPAWN_REGION_TEST_RECORD.local-template.md'
Set-Content -Path $recordPath -Value $recordContent -Encoding UTF8
Write-Output "Template: $recordPath"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output "========================================"
Write-Output "Spawn-region test packet ready"
Write-Output "========================================"
Write-Output "  Map ID:     $mapId"
Write-Output "  Variants:   A (0,0), B (1,1), C (25,15)"
Write-Output "  Packet:     $packetPath"
Write-Output "  Template:   $recordPath"
Write-Output "  Versioned:  $(Join-Path $outputFull "versioned-mod\${mapId}\42")"
Write-Output ''
Write-Output "  Next steps:"
Write-Output "  1. Copy versioned-mod/$mapId/42 to:"
Write-Output "       $modDest42"
Write-Output "  2. Enable mod, launch PZ Build 42, check spawn-selection."
Write-Output "  3. If spawn not visible, try Variant A, B, or C patches."
Write-Output "  4. Fill in SPAWN_REGION_TEST_RECORD.local-template.md."
Write-Output ''
Write-Output "  pz_assets_copied:            false"
Write-Output "  media_maps_touched_in_repo:  false"
Write-Output "  playable_export_claimed:     false"
Write-Output "  Status:                      OK"
