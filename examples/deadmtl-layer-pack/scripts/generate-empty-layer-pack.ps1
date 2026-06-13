# generate-empty-layer-pack.ps1
# Creates transparent PNG stubs for all DeadMTL layers plus sample paint on
# the four System 1 supported layers.
#
# Run from the repo root or from the deadmtl-layer-pack directory.
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\generate-empty-layer-pack.ps1

Add-Type -AssemblyName System.Drawing

$root = $PSScriptRoot | Split-Path -Parent
$dir  = Join-Path $root "layers"
New-Item -ItemType Directory -Force -Path $dir | Out-Null

$W = 190
$H = 140

function New-Transparent([int]$w, [int]$h) {
    $bmp  = New-Object System.Drawing.Bitmap $w, $h
    $g    = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    $g.Dispose()
    return $bmp
}

function Save-Bmp([System.Drawing.Bitmap]$bmp, [string]$path) {
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $path"
}

# Colors matching palettes/worldgen-png-palette.json
$colorWater    = [System.Drawing.Color]::FromArgb(255,   0,   0, 255)  # #0000FF biome:water
$colorShore    = [System.Drawing.Color]::FromArgb(255, 216, 192, 128)  # #D8C080 biome:sand_bank
$colorForest   = [System.Drawing.Color]::FromArgb(255,  32, 112,  32)  # #207020 biome:birch_forest
$colorRoadWE   = [System.Drawing.Color]::FromArgb(255, 255, 102,   0)  # #FF6600 prefab:normal_road_WE_00
$transparent   = [System.Drawing.Color]::FromArgb(0,    0,   0,   0)

Write-Host "Generating System 1 layers (compile-supported)..."

# --- water.png: fills the canvas ---
$bmp = New-Transparent $W $H
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear($colorWater)
$g.Dispose()
Save-Bmp $bmp (Join-Path $dir "water.png")

# --- shore.png: top 10 rows ---
$bmp  = New-Transparent $W $H
$g    = [System.Drawing.Graphics]::FromImage($bmp)
$b    = New-Object System.Drawing.SolidBrush($colorShore)
$g.FillRectangle($b, 0, 0, $W, 10)
$b.Dispose(); $g.Dispose()
Save-Bmp $bmp (Join-Path $dir "shore.png")

# --- parks_forest.png: upper-left quadrant (x=0..79, y=20..79) ---
$bmp  = New-Transparent $W $H
$g    = [System.Drawing.Graphics]::FromImage($bmp)
$b    = New-Object System.Drawing.SolidBrush($colorForest)
$g.FillRectangle($b, 0, 20, 80, 60)
$b.Dispose(); $g.Dispose()
Save-Bmp $bmp (Join-Path $dir "parks_forest.png")

# --- roads_major.png: horizontal strip at y=100..103 ---
$bmp  = New-Transparent $W $H
$g    = [System.Drawing.Graphics]::FromImage($bmp)
$b    = New-Object System.Drawing.SolidBrush($colorRoadWE)
$g.FillRectangle($b, 0, 100, $W, 4)
$b.Dispose(); $g.Dispose()
Save-Bmp $bmp (Join-Path $dir "roads_major.png")

Write-Host ""
Write-Host "Generating System 2-4 placeholder layers (transparent)..."

$placeholders = @(
    "roads_local.png",
    "zones_residential.png",
    "zones_commercial.png",
    "zones_industrial.png",
    "placed_buildings.png",
    "props.png",
    "npc_zones.png",
    "ownership.png"
)

foreach ($name in $placeholders) {
    $bmp = New-Transparent $W $H
    Save-Bmp $bmp (Join-Path $dir $name)
}

Write-Host ""
Write-Host "Done. Layer pack is at: $dir"
Write-Host ""
Write-Host "Compile System 1 worldgen:"
Write-Host "  dotnet run --project src\PZMapForge.Cli --configuration Release -- ``"
Write-Host "    compile-worldgen-project ``"
Write-Host "    --input examples\deadmtl-layer-pack\deadmtl_worldgen_project.json ``"
Write-Host "    --output .local\worldgen\deadmtl\worldgen_layers.json"
