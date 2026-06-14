# generate-readable-terrain-proof-pack.ps1
# Generates a readable terrain proof board under .local/.
#
# Purpose: MAP-21B. MAP-21A used tiny 12x12 swatches that were hard to read in-game
# and isolated water swatches produced a black/unwalkable anomaly. This pack uses
# large terrain patches (at least 24x24) and a shaped water/shore zone instead of
# isolated tiny swatches.
#
# Canvas: 220x170 tiles, origin_x=10580, origin_y=8200
# Spawn:  world (10650, 8250) = pixel (70, 50)
#
# Layout (terrain_biomes.png, priority 10):
#
#   Shaped water/shore zone (west side, full height) -- avoids isolated water anomaly:
#     water:     x=0..44,  y=0..169  (45x170 filled area -- full west column)
#     sand_bank: x=45..54, y=0..169  (10x170 shore strip east of water)
#
#   Biome patches (2 columns x 4 rows, 25x25 each):
#     Col A: x=58..82   Col B: x=84..108
#     Row 1 (y=4..28):   birch_forest, oak_forest
#     Row 2 (y=60..84):  pine_forest, light_birch_forest
#     Row 3 (y=86..110): light_oak_forest, light_pine_forest
#     Row 4 (y=112..136): grass_plain, flower_plain
#     Gap at y=30..59 left clear for the WE road crossing at y=48..55
#
# Layout (terrain_roads.png, priority 50):
#   normal_road_WE_00: x=57..131, y=48..55 (world y=8248..8255, crosses spawn y=8250)
#   highway_NS_00:     x=125..132, y=2..163 (world x=10705..10712, 55 tiles east of spawn)
#
# This pack does NOT need to pass DeadMtlLayerPackValidator.
# It uses compile-worldgen-project directly.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\generate-readable-terrain-proof-pack.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\generate-readable-terrain-proof-pack.ps1 -OutputDir <path>

param(
    [string]$OutputDir = ""
)

Add-Type -AssemblyName System.Drawing

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot ".local\deadmtl-authoring\readable-terrain-proof-pack"
}

$layersDir   = Join-Path $OutputDir "layers"
$palettesDir = Join-Path $OutputDir "palettes"

New-Item -ItemType Directory -Force -Path $layersDir   | Out-Null
New-Item -ItemType Directory -Force -Path $palettesDir | Out-Null

Write-Host "Generating readable terrain proof board at: $OutputDir"
Write-Host "Spawn reference: world tile 10650, 8250 = pixel (70, 50)"
Write-Host "Shaped shoreline: water x=0..44, sand_bank x=45..54 (full height -- no isolated swatches)"
Write-Host ""

$W      = 220
$H      = 170
$MAP_ID = "deadmtl_readable_terrain_proof_v1"

# ---------------------------------------------------------------------------
# Colors (match palette JSON exactly)
# ---------------------------------------------------------------------------
$cWater           = [System.Drawing.ColorTranslator]::FromHtml("#0000FF")
$cSandBank        = [System.Drawing.ColorTranslator]::FromHtml("#D8C080")
$cGrassPlain      = [System.Drawing.ColorTranslator]::FromHtml("#00AA00")
$cFlowerPlain     = [System.Drawing.ColorTranslator]::FromHtml("#55CC55")
$cBirchForest     = [System.Drawing.ColorTranslator]::FromHtml("#207020")
$cOakForest       = [System.Drawing.ColorTranslator]::FromHtml("#145C14")
$cPineForest      = [System.Drawing.ColorTranslator]::FromHtml("#0B4418")
$cLightBirch      = [System.Drawing.ColorTranslator]::FromHtml("#60A060")
$cLightOak        = [System.Drawing.ColorTranslator]::FromHtml("#4F8F4F")
$cLightPine       = [System.Drawing.ColorTranslator]::FromHtml("#3F7F50")
$cRoadWE          = [System.Drawing.ColorTranslator]::FromHtml("#FF6600")
$cHighwayNS       = [System.Drawing.ColorTranslator]::FromHtml("#CC3300")
$transparent      = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)

function New-Transparent([int]$w, [int]$h) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear($transparent)
    $g.Dispose()
    return $bmp
}

function Fill([System.Drawing.Bitmap]$bmp, [System.Drawing.Color]$c, [int]$x, [int]$y, [int]$w, [int]$h) {
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $b = New-Object System.Drawing.SolidBrush($c)
    $g.FillRectangle($b, $x, $y, $w, $h)
    $b.Dispose(); $g.Dispose()
}

function Save-Bmp([System.Drawing.Bitmap]$bmp, [string]$path) {
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  OK  $([System.IO.Path]::GetFileName($path))"
}

# ---------------------------------------------------------------------------
# Project manifest
# ---------------------------------------------------------------------------
Write-Host "Writing project manifest..."
$manifest = @"
{
  "format": "pzmapforge.worldgen.project.v1",
  "map_id": "$MAP_ID",
  "origin_x": 10580,
  "origin_y": 8200,
  "width": $W,
  "height": $H,
  "layers": [
    {
      "id": "terrain_biomes",
      "path": "layers/terrain_biomes.png",
      "palette": "palettes/worldgen-png-palette.json",
      "priority": 10
    },
    {
      "id": "terrain_roads",
      "path": "layers/terrain_roads.png",
      "palette": "palettes/worldgen-png-palette.json",
      "priority": 50
    }
  ]
}
"@
Set-Content -Path (Join-Path $OutputDir "readable_terrain_proof_project.json") -Value $manifest -Encoding UTF8
Write-Host "  OK  readable_terrain_proof_project.json"

# ---------------------------------------------------------------------------
# README
# ---------------------------------------------------------------------------
Set-Content -Path (Join-Path $OutputDir "README.md") -Value @"
# Readable Terrain Proof Board

Map ID: $MAP_ID
Generated by: generate-readable-terrain-proof-pack.ps1

## Purpose

MAP-21A used tiny 12x12 swatches that were hard to read in-game.
Isolated water swatches produced a black/unwalkable anomaly.
This board uses large patches (25x25+) and a shaped shoreline for water/sand.

## Layout (pixel coordinates, origin 10580/8200)

Shaped water/shore zone (west side):
  water:     x=0..44,  y=0..169  (45x170 -- full height, no isolated swatch)
  sand_bank: x=45..54, y=0..169  (10x170 shore strip)

Biome patches (25x25 each):
  Row 1 (y=4..28):    birch_forest (x=58), oak_forest (x=84)
  Row 2 (y=60..84):   pine_forest (x=58), light_birch_forest (x=84)
  Row 3 (y=86..110):  light_oak_forest (x=58), light_pine_forest (x=84)
  Row 4 (y=112..136): grass_plain (x=58), flower_plain (x=84)

Roads (priority 50, override biomes):
  normal_road_WE_00: y=48..55, x=57..131  (world y=8248..8255, crosses spawn)
  highway_NS_00:     x=125..132, y=2..163 (world x=10705..10712)

Spawn reference: world tile (10650, 8250) = pixel (70, 50)

## Claim boundary

Authoring artifact only. No public mod packaging claimed.
Proof status requires human visual confirmation after PZ load.
"@ -Encoding UTF8
Write-Host "  OK  README.md"

# ---------------------------------------------------------------------------
# Palette JSON (full 12-entry)
# ---------------------------------------------------------------------------
Write-Host "Writing palette..."
$paletteJson = @"
{
  "format": "pzmapforge.worldgen.png-palette.v1",
  "entries": [
    { "color": "#0000FF", "type": "biome",  "key": "water" },
    { "color": "#D8C080", "type": "biome",  "key": "sand_bank" },
    { "color": "#00AA00", "type": "biome",  "key": "grass_plain" },
    { "color": "#55CC55", "type": "biome",  "key": "flower_plain" },
    { "color": "#207020", "type": "biome",  "key": "birch_forest" },
    { "color": "#145C14", "type": "biome",  "key": "oak_forest" },
    { "color": "#0B4418", "type": "biome",  "key": "pine_forest" },
    { "color": "#60A060", "type": "biome",  "key": "light_birch_forest" },
    { "color": "#4F8F4F", "type": "biome",  "key": "light_oak_forest" },
    { "color": "#3F7F50", "type": "biome",  "key": "light_pine_forest" },
    { "color": "#FF6600", "type": "prefab", "key": "normal_road_WE_00" },
    { "color": "#CC3300", "type": "prefab", "key": "highway_NS_00" }
  ]
}
"@
Set-Content -Path (Join-Path $palettesDir "worldgen-png-palette.json") -Value $paletteJson -Encoding UTF8
Write-Host "  OK  worldgen-png-palette.json"

# ---------------------------------------------------------------------------
# Generate palette charts into proof pack palettes dir
# ---------------------------------------------------------------------------
Write-Host "Generating palette charts..."
$chartScript = Join-Path $PSScriptRoot "generate-palette-color-charts.ps1"
& powershell -ExecutionPolicy Bypass -File $chartScript -PalettesDir $palettesDir
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Chart generation failed - continuing without charts"
}

# ---------------------------------------------------------------------------
# terrain_biomes.png
#
# Shaped water/shore zone (west, full height -- large area avoids isolated swatch anomaly):
#   water:     x=0..44,  y=0..169  (45 tiles wide)
#   sand_bank: x=45..54, y=0..169  (10-tile shore strip)
#
# Biome patches (2 col x 4 row, 25x25):
#   Row 1 (y=4):  birch_forest (x=58), oak_forest (x=84)
#   Row 2 (y=60): pine_forest (x=58), light_birch_forest (x=84)
#   Row 3 (y=86): light_oak_forest (x=58), light_pine_forest (x=84)
#   Row 4 (y=112): grass_plain (x=58), flower_plain (x=84)
#
# Gap at y=30..59 left clear so WE road at y=48..55 is not overridden by biomes.
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Generating terrain_biomes layer..."

$bmp = New-Transparent $W $H

# Shaped water/shore (west -- uses large continuous area, not isolated swatches)
Fill $bmp $cWater    0  0  45 170   # biome:water     full-height west column
Fill $bmp $cSandBank 45  0  10 170  # biome:sand_bank shore strip

# Row 1 (y=4, 25x25 patches, world y=8204..8228)
Fill $bmp $cBirchForest 58  4  25 25  # biome:birch_forest
Fill $bmp $cOakForest   84  4  25 25  # biome:oak_forest

# Row 2 (y=60, below road gap, world y=8260..8284)
Fill $bmp $cPineForest  58 60  25 25  # biome:pine_forest
Fill $bmp $cLightBirch  84 60  25 25  # biome:light_birch_forest

# Row 3 (y=86, world y=8286..8310)
Fill $bmp $cLightOak    58 86  25 25  # biome:light_oak_forest
Fill $bmp $cLightPine   84 86  25 25  # biome:light_pine_forest

# Row 4 (y=112, world y=8312..8336)
Fill $bmp $cGrassPlain  58 112 25 25  # biome:grass_plain
Fill $bmp $cFlowerPlain 84 112 25 25  # biome:flower_plain

Save-Bmp $bmp (Join-Path $layersDir "terrain_biomes.png")

# ---------------------------------------------------------------------------
# terrain_roads.png
#   normal_road_WE_00: y=48..55, x=57..131  (crosses spawn y=50, world y=8248..8255)
#   highway_NS_00:     x=125..132, y=2..163 (world x=10705..10712, 55 tiles east of spawn)
#
# Paint road WE first; highway painted on top at intersection so highway reads continuous.
# ---------------------------------------------------------------------------
Write-Host "Generating terrain_roads layer..."

$bmp = New-Transparent $W $H

Fill $bmp $cRoadWE    57  48  75   8  # prefab:normal_road_WE_00  x=57..131, world y=8248..8255
Fill $bmp $cHighwayNS 125  2   8 162  # prefab:highway_NS_00      x=125..132, world x=10705..10712

Save-Bmp $bmp (Join-Path $layersDir "terrain_roads.png")

# ---------------------------------------------------------------------------
# layer-color-chart.png: composite reference (all layers over gray background)
# Documentation only -- not included in project manifest.
# ---------------------------------------------------------------------------
Write-Host "Generating layer color chart..."

$bmp = New-Transparent $W $H
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(255, 220, 220, 220))
$g.Dispose()

# Water/shore zone
Fill $bmp $cWater    0  0  45 170
Fill $bmp $cSandBank 45  0  10 170

# Biome patches
Fill $bmp $cBirchForest 58  4  25 25
Fill $bmp $cOakForest   84  4  25 25
Fill $bmp $cPineForest  58 60  25 25
Fill $bmp $cLightBirch  84 60  25 25
Fill $bmp $cLightOak    58 86  25 25
Fill $bmp $cLightPine   84 86  25 25
Fill $bmp $cGrassPlain  58 112 25 25
Fill $bmp $cFlowerPlain 84 112 25 25

# Roads (on top, as in priority order)
Fill $bmp $cRoadWE    57  48  75   8
Fill $bmp $cHighwayNS 125  2   8 162

Save-Bmp $bmp (Join-Path $layersDir "layer-color-chart.png")

Write-Host ""
Write-Host "Pack generated: $OutputDir"
Write-Host ""
Write-Host "Compile with:"
Write-Host "  dotnet run --project src\PZMapForge.Cli --configuration Release --no-build -- ``"
Write-Host "    compile-worldgen-project ``"
Write-Host "    --input `"$(Join-Path $OutputDir "readable_terrain_proof_project.json")`" ``"
Write-Host "    --output .local\deadmtl-authoring\readable-terrain-proof\worldgen_layers.json"
