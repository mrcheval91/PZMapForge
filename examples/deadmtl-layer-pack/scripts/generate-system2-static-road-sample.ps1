# generate-system2-static-road-sample.ps1
# Generates non-empty System 2 road intent sample layers for DeadMTL (MAP-22F).
#
# Status: EXTRACT_ONLY / NOT_RUNTIME_PROVEN
# This script does not perform a compile step, does not install any game files,
# does not write any Lua override, and does not produce any lotpack output.
#
# Layer content (220x170, transparent background):
#   static_roads_local.png:          local_street_asphalt #404040  x=20..120  y=50..55
#   static_roads_alleys.png:         alley_ruelle_asphalt #303030  x=25..115  y=70..72
#   static_roads_service.png:        service_lane         #505050  x=130..170 y=40..42
#   static_roads_parking_access.png: parking_access       #606060  x=140..160 y=80..84
#   static_pedestrian_cuts.png:      sidewalk_or_pedestrian_cut #B0B0B0 x=60..90 y=90
#   static_road_nodes.png:           intersection_node #FF00FF (70,52)
#                                    road_turn_node    #00FFFF (120,55)
#                                    dead_end_node     #FF9900 (115,72)
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\generate-system2-static-road-sample.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\generate-system2-static-road-sample.ps1 -OutputDir <path>

param(
    [string]$OutputDir = ""
)

Add-Type -AssemblyName System.Drawing

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

if ([string]::IsNullOrEmpty($OutputDir)) {
    $OutputDir = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-sample"
}

$layersDir  = Join-Path $OutputDir "layers"
$paletteDir = Join-Path $OutputDir "palettes"

New-Item -ItemType Directory -Force -Path $OutputDir  | Out-Null
New-Item -ItemType Directory -Force -Path $layersDir  | Out-Null
New-Item -ItemType Directory -Force -Path $paletteDir | Out-Null

Write-Host "MAP-22F System 2 Static Road Sample Generator"
Write-Host "=============================================="
Write-Host "Status:         EXTRACT_ONLY"
Write-Host "Runtime status: NOT_RUNTIME_PROVEN"
Write-Host "Writer status:  NOT_IMPLEMENTED"
Write-Host "Output:         $OutputDir"
Write-Host ""

$width  = 220
$height = 170

function New-TransparentBitmap {
    $bmp = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.Dispose()
    return $bmp
}

function Save-Png($bmp, $path) {
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# static_roads_local.png: local_street_asphalt #404040, x=20..120, y=50..55
$bmp   = New-TransparentBitmap
$g     = [System.Drawing.Graphics]::FromImage($bmp)
$color = [System.Drawing.Color]::FromArgb(255, 0x40, 0x40, 0x40)
$brush = New-Object System.Drawing.SolidBrush $color
$g.FillRectangle($brush, 20, 50, 101, 6)
$brush.Dispose(); $g.Dispose()
Save-Png $bmp (Join-Path $layersDir "static_roads_local.png")
Write-Host "  [SAMPLE] static_roads_local.png (local_street_asphalt x=20..120 y=50..55)"

# static_roads_alleys.png: alley_ruelle_asphalt #303030, x=25..115, y=70..72
$bmp   = New-TransparentBitmap
$g     = [System.Drawing.Graphics]::FromImage($bmp)
$color = [System.Drawing.Color]::FromArgb(255, 0x30, 0x30, 0x30)
$brush = New-Object System.Drawing.SolidBrush $color
$g.FillRectangle($brush, 25, 70, 91, 3)
$brush.Dispose(); $g.Dispose()
Save-Png $bmp (Join-Path $layersDir "static_roads_alleys.png")
Write-Host "  [SAMPLE] static_roads_alleys.png (alley_ruelle_asphalt x=25..115 y=70..72)"

# static_roads_service.png: service_lane #505050, x=130..170, y=40..42
$bmp   = New-TransparentBitmap
$g     = [System.Drawing.Graphics]::FromImage($bmp)
$color = [System.Drawing.Color]::FromArgb(255, 0x50, 0x50, 0x50)
$brush = New-Object System.Drawing.SolidBrush $color
$g.FillRectangle($brush, 130, 40, 41, 3)
$brush.Dispose(); $g.Dispose()
Save-Png $bmp (Join-Path $layersDir "static_roads_service.png")
Write-Host "  [SAMPLE] static_roads_service.png (service_lane x=130..170 y=40..42)"

# static_roads_parking_access.png: parking_access #606060, x=140..160, y=80..84
$bmp   = New-TransparentBitmap
$g     = [System.Drawing.Graphics]::FromImage($bmp)
$color = [System.Drawing.Color]::FromArgb(255, 0x60, 0x60, 0x60)
$brush = New-Object System.Drawing.SolidBrush $color
$g.FillRectangle($brush, 140, 80, 21, 5)
$brush.Dispose(); $g.Dispose()
Save-Png $bmp (Join-Path $layersDir "static_roads_parking_access.png")
Write-Host "  [SAMPLE] static_roads_parking_access.png (parking_access x=140..160 y=80..84)"

# static_pedestrian_cuts.png: sidewalk_or_pedestrian_cut #B0B0B0, x=60..90, y=90
$bmp   = New-TransparentBitmap
$g     = [System.Drawing.Graphics]::FromImage($bmp)
$color = [System.Drawing.Color]::FromArgb(255, 0xB0, 0xB0, 0xB0)
$brush = New-Object System.Drawing.SolidBrush $color
$g.FillRectangle($brush, 60, 90, 31, 1)
$brush.Dispose(); $g.Dispose()
Save-Png $bmp (Join-Path $layersDir "static_pedestrian_cuts.png")
Write-Host "  [SAMPLE] static_pedestrian_cuts.png (sidewalk_or_pedestrian_cut x=60..90 y=90)"

# static_road_nodes.png: three node pixels
$bmp = New-TransparentBitmap
$bmp.SetPixel(70,  52, [System.Drawing.Color]::FromArgb(255, 0xFF, 0x00, 0xFF))
$bmp.SetPixel(120, 55, [System.Drawing.Color]::FromArgb(255, 0x00, 0xFF, 0xFF))
$bmp.SetPixel(115, 72, [System.Drawing.Color]::FromArgb(255, 0xFF, 0x99, 0x00))
Save-Png $bmp (Join-Path $layersDir "static_road_nodes.png")
Write-Host "  [SAMPLE] static_road_nodes.png (intersection_node(70,52) road_turn_node(120,55) dead_end_node(115,72))"

# Intent color definitions for chart and copies
$intentEntries = @(
    @{ A = 255; R = 0x40; G = 0x40; B = 0x40; Label = "local_street_asphalt"       },
    @{ A = 255; R = 0x30; G = 0x30; B = 0x30; Label = "alley_ruelle_asphalt"        },
    @{ A = 255; R = 0x50; G = 0x50; B = 0x50; Label = "service_lane"                },
    @{ A = 255; R = 0x60; G = 0x60; B = 0x60; Label = "parking_access"              },
    @{ A = 255; R = 0xB0; G = 0xB0; B = 0xB0; Label = "sidewalk_or_pedestrian_cut" },
    @{ A = 255; R = 0xFF; G = 0xFF; B = 0x00; Label = "centerline_or_lane_marking"  },
    @{ A = 255; R = 0xFF; G = 0xFF; B = 0xFF; Label = "crosswalk_or_stop_marking"   },
    @{ A = 255; R = 0xFF; G = 0x00; B = 0xFF; Label = "intersection_node"           },
    @{ A = 255; R = 0x00; G = 0xFF; B = 0xFF; Label = "road_turn_node"              },
    @{ A = 255; R = 0xFF; G = 0x99; B = 0x00; Label = "dead_end_node"              }
)

$chartBmp = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$chartG   = [System.Drawing.Graphics]::FromImage($chartBmp)
$chartG.Clear([System.Drawing.Color]::FromArgb(255, 20, 20, 20))
$stripH   = [int]($height / $intentEntries.Count)

for ($i = 0; $i -lt $intentEntries.Count; $i++) {
    $e     = $intentEntries[$i]
    $color = [System.Drawing.Color]::FromArgb($e.A, $e.R, $e.G, $e.B)
    $yTop  = $i * $stripH + 2
    $brush = New-Object System.Drawing.SolidBrush $color
    $chartG.FillRectangle($brush, 8, $yTop, ($width - 16), ($stripH - 4))
    $brush.Dispose()
}
$chartG.Dispose()
$chartBmp.Save((Join-Path $layersDir "layer-color-chart.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$chartBmp.Dispose()
Write-Host "  [CHART] layer-color-chart.png"

# Copy contract JSON and palette JSON from repo
$srcContract = Join-Path $repoRoot "examples\deadmtl-layer-pack\system2-static-road-overlay-contract.json"
if (Test-Path $srcContract) {
    Copy-Item $srcContract (Join-Path $OutputDir "system2-static-road-overlay-contract.json")
    Write-Host "  [COPY]  system2-static-road-overlay-contract.json"
} else {
    Write-Warning "Contract JSON not found: $srcContract"
}

$srcPalette = Join-Path $repoRoot "examples\deadmtl-layer-pack\palettes\system2-static-road-intent-palette.json"
if (Test-Path $srcPalette) {
    Copy-Item $srcPalette (Join-Path $paletteDir "system2-static-road-intent-palette.json")
    Write-Host "  [COPY]  palettes/system2-static-road-intent-palette.json"
} else {
    Write-Warning "Intent palette not found: $srcPalette"
}

$readmeContent = @'
# System 2 Static Road Sample Pack (MAP-22F)

Status: EXTRACT_ONLY / NOT_RUNTIME_PROVEN

Generated by: examples\deadmtl-layer-pack\scripts\generate-system2-static-road-sample.ps1

This is a non-empty authoring sample for the DeadMTL System 2 static road overlay.
Layers contain painted pixels at Montreal ruelle authoring scale (1 px = 1 m).
No Lua override is generated. No compile step is performed. No lotpack output is produced.

This pack is a planning artifact only.

Layer content (220x170):
  static_roads_local.png:          local_street_asphalt #404040  x=20..120  y=50..55
  static_roads_alleys.png:         alley_ruelle_asphalt #303030  x=25..115  y=70..72
  static_roads_service.png:        service_lane         #505050  x=130..170 y=40..42
  static_roads_parking_access.png: parking_access       #606060  x=140..160 y=80..84
  static_pedestrian_cuts.png:      sidewalk_or_ped_cut  #B0B0B0  x=60..90   y=90
  static_road_nodes.png:           intersection_node    #FF00FF  (70,52)
                                   road_turn_node       #00FFFF  (120,55)
                                   dead_end_node        #FF9900  (115,72)
  layer-color-chart.png:           intent color reference chart

See: docs/authoring/DEADMTL_SYSTEM2_STATIC_ROAD_SAMPLE_EXTRACT.md
'@

[System.IO.File]::WriteAllText((Join-Path $OutputDir "README.md"), $readmeContent, [System.Text.Encoding]::ASCII)
Write-Host "  [WRITE] README.md"

Write-Host ""
Write-Host "STATUS:         EXTRACT_ONLY"
Write-Host "RUNTIME_STATUS: NOT_RUNTIME_PROVEN"
Write-Host "WRITER_STATUS:  NOT_IMPLEMENTED"
Write-Host ""
Write-Host "VERDICT: MAP22F_SYSTEM2_STATIC_ROAD_SAMPLE_GENERATED"

exit 0
