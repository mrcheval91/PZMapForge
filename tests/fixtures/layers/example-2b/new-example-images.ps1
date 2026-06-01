#Requires -Version 5.1
<#
.SYNOPSIS
    Generates four deterministic 300x300 PNG layer images for the example-2b manifest.

    Output: tests/fixtures/layers/example-2b/generated/
      terrain.png   -- grass background + industrial_yard zone (bottom-right)
      roads.png     -- road grid + sidewalk bands
      buildings.png -- row_house, depanneur, garage, landmark footprints
      markers.png   -- spawn marker (overlaps row_house; 36 conflict cells; markers wins)

    Uses exact RGB values from source/image-palette.json.
    Does not write to media/maps. Does not claim playable PZ export.
    Claim boundary: planning_artifact_only_not_pz_load_tested

    After running this script, use generated-layer-manifest.json to run
    the full layer-pipeline:

      dotnet run --project src/PZMapForge.Cli -- layer-pipeline `
        --layers tests/fixtures/layers/example-2b/generated-layer-manifest.json `
        --palette source/image-palette.json `
        --output .local/mapforge
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDir  = Join-Path $scriptDir 'generated'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Write-Output "Generating example-2b layer images..."
Write-Output "Output: $outputDir"
Write-Output ""

# ---------------------------------------------------------------------------
# Palette colours (from source/image-palette.json)
# ---------------------------------------------------------------------------

function New-Color([int]$R, [int]$G, [int]$B) {
    [System.Drawing.Color]::FromArgb($R, $G, $B)
}

$Grass          = New-Color 100 140 70    # kind: grass         code: g
$Road           = New-Color  70  70 70    # kind: road          code: r
$Sidewalk       = New-Color 190 180 160   # kind: sidewalk      code: s
$RowHouse       = New-Color 160 110  80   # kind: row_house     code: h
$Depanneur      = New-Color 200 130  60   # kind: depanneur     code: d
$Garage         = New-Color  80  80 100   # kind: garage        code: a
$IndustrialYard = New-Color 160 130  90   # kind: industrial_yard code: i
$Landmark       = New-Color 255 220   0   # kind: landmark      code: l
$Spawn          = New-Color   0 220  80   # kind: spawn         code: p

# ---------------------------------------------------------------------------
# Image helper
# ---------------------------------------------------------------------------

function New-Image {
    param([System.Drawing.Color]$Background)
    $bmp = New-Object System.Drawing.Bitmap(300, 300)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.Clear($Background)
    return $bmp, $gfx
}

function Fill-Rect {
    param(
        [System.Drawing.Graphics]$Gfx,
        [System.Drawing.Color]$Color,
        [int]$X, [int]$Y, [int]$W, [int]$H
    )
    $brush = New-Object System.Drawing.SolidBrush($Color)
    $gfx.FillRectangle($brush, $X, $Y, $W, $H)
    $brush.Dispose()
}

function Save-Image {
    param(
        [System.Drawing.Bitmap]$Bmp,
        [System.Drawing.Graphics]$Gfx,
        [string]$Path
    )
    $Gfx.Dispose()
    $Bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $Bmp.Dispose()
    Write-Output "  OK: $Path"
}

# ---------------------------------------------------------------------------
# terrain.png
# Layer: terrain
# Allowed kinds: grass, industrial_yard
# Layout:
#   - entire image: grass background
#   - industrial_yard block at (210, 210) 80x80
# ---------------------------------------------------------------------------

$bmp, $gfx = New-Image $Grass
Fill-Rect $gfx $IndustrialYard 210 210 80 80
Save-Image $bmp $gfx (Join-Path $outputDir 'terrain.png')

# ---------------------------------------------------------------------------
# roads.png
# Layer: roads
# Allowed kinds: road, sidewalk
# Layout:
#   - entire image: grass background (no contribution = default kind)
#   - horizontal road: y=130, width=300, height=16  (rows 130..145)
#   - vertical road:   x=130, width=16, height=300  (cols 130..145)
#   - sidewalk bands (4px):
#       above horizontal road:  y=126, height=4
#       below horizontal road:  y=146, height=4
#       left of vertical road:  x=126, width=4
#       right of vertical road: x=146, width=4
# ---------------------------------------------------------------------------

$bmp, $gfx = New-Image $Grass
Fill-Rect $gfx $Sidewalk    0 126 300   4   # above horizontal road
Fill-Rect $gfx $Road        0 130 300  16   # horizontal road spine
Fill-Rect $gfx $Sidewalk    0 146 300   4   # below horizontal road
Fill-Rect $gfx $Sidewalk  126   0   4 300   # left of vertical road
Fill-Rect $gfx $Road      130   0  16 300   # vertical road spine
Fill-Rect $gfx $Sidewalk  146   0   4 300   # right of vertical road
Save-Image $bmp $gfx (Join-Path $outputDir 'roads.png')

# ---------------------------------------------------------------------------
# buildings.png
# Layer: buildings
# Allowed kinds: row_house, depanneur, garage, landmark
# Layout (all zones non-overlapping with roads):
#   - row_house block:  (10,  10) 80x70  -- top-left residential cluster
#   - depanneur block:  (160,160) 28x28  -- near intersection (just outside road)
#   - garage block:     (10, 200) 25x25  -- bottom-left service area
#   - landmark block:   (260,  10) 20x20 -- top-right reference point
# ---------------------------------------------------------------------------

$bmp, $gfx = New-Image $Grass
Fill-Rect $gfx $RowHouse  10  10  80  70
Fill-Rect $gfx $Depanneur 160 160  28  28
Fill-Rect $gfx $Garage     10 200  25  25
Fill-Rect $gfx $Landmark  260  10  20  20
Save-Image $bmp $gfx (Join-Path $outputDir 'buildings.png')

# ---------------------------------------------------------------------------
# markers.png
# Layer: markers
# Allowed kinds: spawn, landmark
# Layout:
#   - spawn marker: (70, 70) 6x6 -- in the gap between row houses and roads
# ---------------------------------------------------------------------------

$bmp, $gfx = New-Image $Grass
Fill-Rect $gfx $Spawn 70 70 6 6
Save-Image $bmp $gfx (Join-Path $outputDir 'markers.png')

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "Generated 4 layer images under: $outputDir"
Write-Output ""
Write-Output "Expected pipeline output (36 conflicts: spawn marker overlaps row_house cluster):"
Write-Output "  - terrain:   grass base + industrial_yard block (210,210) 80x80"
Write-Output "  - roads:     horizontal + vertical road grid with sidewalk bands"
Write-Output "  - buildings: row_house (10,10,80x70), depanneur, garage, landmark"
Write-Output "  - markers:   spawn (70,70,6x6) -- inside row_house block"
Write-Output "  Conflict:    markers.spawn wins over buildings.row_house at 36 cells"
Write-Output ""
Write-Output "Claim boundary: planning_artifact_only_not_pz_load_tested"
Write-Output ""
Write-Output "To run layer-pipeline:"
Write-Output "  dotnet run --project src/PZMapForge.Cli -- layer-pipeline \"
Write-Output "    --layers tests/fixtures/layers/example-2b/generated-layer-manifest.json \"
Write-Output "    --palette source/image-palette.json \"
Write-Output "    --output .local/mapforge"
