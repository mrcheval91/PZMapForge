# generate-worldgen-palette-proof-pack.ps1
# Generates a WorldGen palette proof pack under .local/.
# Paints one swatch per known palette color near spawn tile (10650, 8250)
# so each biome/prefab is visible immediately on first spawn.
#
# Palette: 10 biomes + 2 prefabs = 12 colors
# Canvas:  190x140 tiles, origin_x=10580, origin_y=8200
# Spawn:   world (10650, 8250) = pixel (70, 50)
#
# Biome swatch grid (worldgen_probe_biomes.png):
#   Row 0 (pixel y=5..16): water, sand_bank, grass_plain, flower_plain, birch_forest
#   Row 1 (pixel y=19..30): oak_forest, pine_forest, light_birch_forest, light_oak_forest, light_pine_forest
#   Columns: x=10,25,40,55,70  (step=15: swatch 12px + gap 3px)
#
# Prefab swatches (worldgen_probe_prefabs.png):
#   normal_road_WE_00: horizontal strip y=48..55, x=5..120  (crosses spawn y=50)
#   highway_NS_00:     vertical strip   x=130..137, y=5..120
#
# This pack uses compile-worldgen-project directly.
# It does NOT need to pass DeadMtlLayerPackValidator.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\generate-worldgen-palette-proof-pack.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\generate-worldgen-palette-proof-pack.ps1 -OutputDir <path>

param(
    [string]$OutputDir = ""
)

Add-Type -AssemblyName System.Drawing

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot ".local\deadmtl-authoring\worldgen-palette-proof-pack"
}

$layersDir   = Join-Path $OutputDir "layers"
$palettesDir = Join-Path $OutputDir "palettes"

New-Item -ItemType Directory -Force -Path $layersDir   | Out-Null
New-Item -ItemType Directory -Force -Path $palettesDir | Out-Null

Write-Host "Generating WorldGen palette proof pack at: $OutputDir"
Write-Host "Spawn reference: world tile 10650, 8250 = pixel (70, 50)"
Write-Host ""

$W      = 190
$H      = 140
$MAP_ID = "deadmtl_worldgen_palette_probe_v1"

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
      "id": "worldgen_probe_biomes",
      "path": "layers/worldgen_probe_biomes.png",
      "palette": "palettes/worldgen-png-palette.json",
      "priority": 10
    },
    {
      "id": "worldgen_probe_prefabs",
      "path": "layers/worldgen_probe_prefabs.png",
      "palette": "palettes/worldgen-png-palette.json",
      "priority": 50
    }
  ]
}
"@
Set-Content -Path (Join-Path $OutputDir "worldgen_palette_probe_project.json") -Value $manifest -Encoding UTF8
Write-Host "  OK  worldgen_palette_probe_project.json"

# ---------------------------------------------------------------------------
# README
# ---------------------------------------------------------------------------
Set-Content -Path (Join-Path $OutputDir "README.md") -Value @"
# WorldGen Palette Proof Pack

Map ID: $MAP_ID
Generated by: generate-worldgen-palette-proof-pack.ps1

## Purpose

Paints one swatch per palette color within 60 tiles of spawn (10650, 8250).
Each color is immediately visible at first spawn without walking.

## Swatch layout (pixel coordinates)

Biomes:
  Row 0 (y=5..16): water / sand_bank / grass_plain / flower_plain / birch_forest
  Row 1 (y=19..30): oak_forest / pine_forest / light_birch_forest / light_oak_forest / light_pine_forest
  Columns: x=10,25,40,55,70

Prefabs:
  normal_road_WE_00: y=48..55, x=5..120  (horizontal, crosses spawn y=50)
  highway_NS_00:     x=130..137, y=5..120 (vertical)

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
# worldgen_probe_biomes.png
# Biome swatches: 5 per row, 2 rows; swatch 12x12, step 15
# Row 0 (y=5): water, sand_bank, grass_plain, flower_plain, birch_forest
# Row 1 (y=19): oak_forest, pine_forest, light_birch_forest, light_oak_forest, light_pine_forest
# Columns: x=10,25,40,55,70
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Generating biome layer..."

$bmp = New-Transparent $W $H

Fill $bmp $cWater       10  5  12 12   # biome:water
Fill $bmp $cSandBank    25  5  12 12   # biome:sand_bank
Fill $bmp $cGrassPlain  40  5  12 12   # biome:grass_plain
Fill $bmp $cFlowerPlain 55  5  12 12   # biome:flower_plain
Fill $bmp $cBirchForest 70  5  12 12   # biome:birch_forest

Fill $bmp $cOakForest   10 19  12 12   # biome:oak_forest
Fill $bmp $cPineForest  25 19  12 12   # biome:pine_forest
Fill $bmp $cLightBirch  40 19  12 12   # biome:light_birch_forest
Fill $bmp $cLightOak    55 19  12 12   # biome:light_oak_forest
Fill $bmp $cLightPine   70 19  12 12   # biome:light_pine_forest

Save-Bmp $bmp (Join-Path $layersDir "worldgen_probe_biomes.png")

# ---------------------------------------------------------------------------
# worldgen_probe_prefabs.png
# normal_road_WE_00: horizontal strip y=48..55 (h=8), x=5..120 (w=116)  - world y=8248..8255, crosses spawn y=50
# highway_NS_00:     vertical strip   x=130..137 (w=8), y=5..120 (h=116) - world x=10710..10717
# ---------------------------------------------------------------------------
Write-Host "Generating prefab layer..."

$bmp = New-Transparent $W $H

Fill $bmp $cRoadWE    5  48 116  8   # prefab:normal_road_WE_00  world x=10585..10700, y=8248..8255
Fill $bmp $cHighwayNS 130  5   8 116 # prefab:highway_NS_00      world x=10710..10717, y=8205..8320

Save-Bmp $bmp (Join-Path $layersDir "worldgen_probe_prefabs.png")

# ---------------------------------------------------------------------------
# layer-color-chart.png: composite reference (biomes + prefabs over white)
# Documentation only — not included in project manifest.
# ---------------------------------------------------------------------------
Write-Host "Generating layer color chart..."

$bmp = New-Transparent $W $H
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(255, 230, 230, 230))
$g.Dispose()

# biomes
Fill $bmp $cWater       10  5  12 12
Fill $bmp $cSandBank    25  5  12 12
Fill $bmp $cGrassPlain  40  5  12 12
Fill $bmp $cFlowerPlain 55  5  12 12
Fill $bmp $cBirchForest 70  5  12 12
Fill $bmp $cOakForest   10 19  12 12
Fill $bmp $cPineForest  25 19  12 12
Fill $bmp $cLightBirch  40 19  12 12
Fill $bmp $cLightOak    55 19  12 12
Fill $bmp $cLightPine   70 19  12 12
# prefabs
Fill $bmp $cRoadWE    5  48 116  8
Fill $bmp $cHighwayNS 130  5   8 116

Save-Bmp $bmp (Join-Path $layersDir "layer-color-chart.png")

Write-Host ""
Write-Host "Pack generated: $OutputDir"
Write-Host ""
Write-Host "Compile with:"
Write-Host "  dotnet run --project src\PZMapForge.Cli --configuration Release --no-build -- ``"
Write-Host "    compile-worldgen-project ``"
Write-Host "    --input `"$(Join-Path $OutputDir "worldgen_palette_probe_project.json")`" ``"
Write-Host "    --output .local\deadmtl-authoring\worldgen-palette-proof\worldgen_layers.json"
