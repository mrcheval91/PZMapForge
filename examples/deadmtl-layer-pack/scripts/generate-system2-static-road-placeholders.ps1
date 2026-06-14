# generate-system2-static-road-placeholders.ps1
# Generates System 2 static road overlay placeholder layers for DeadMTL.
#
# Status: CONTRACT_ONLY / NOT_RUNTIME_PROVEN
# This script does not perform a compile step, does not install any game files,
# and does not write any Lua override.
#
# Output layers (all 220x170 transparent):
#   layers/static_roads_local.png
#   layers/static_roads_alleys.png
#   layers/static_roads_service.png
#   layers/static_roads_parking_access.png
#   layers/static_pedestrian_cuts.png
#   layers/static_road_nodes.png
#   layers/layer-color-chart.png  (intent color reference, dark background)
#
# Also copies:
#   system2-static-road-overlay-contract.json
#   palettes/system2-static-road-intent-palette.json
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\generate-system2-static-road-placeholders.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\generate-system2-static-road-placeholders.ps1 -OutputDir <path>

param(
    [string]$OutputDir = ""
)

Add-Type -AssemblyName System.Drawing

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

if ([string]::IsNullOrEmpty($OutputDir)) {
    $OutputDir = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-placeholders"
}

$layersDir  = Join-Path $OutputDir "layers"
$paletteDir = Join-Path $OutputDir "palettes"

New-Item -ItemType Directory -Force -Path $OutputDir  | Out-Null
New-Item -ItemType Directory -Force -Path $layersDir  | Out-Null
New-Item -ItemType Directory -Force -Path $paletteDir | Out-Null

Write-Host "MAP-22D System 2 Static Road Overlay Placeholder Generator"
Write-Host "==========================================================="
Write-Host "Status:         CONTRACT_ONLY"
Write-Host "Runtime status: NOT_RUNTIME_PROVEN"
Write-Host "Writer status:  NOT_IMPLEMENTED"
Write-Host "Output:         $OutputDir"
Write-Host ""

$width  = 220
$height = 170

# Transparent placeholder layers
$placeholderLayers = @(
    "static_roads_local.png",
    "static_roads_alleys.png",
    "static_roads_service.png",
    "static_roads_parking_access.png",
    "static_pedestrian_cuts.png",
    "static_road_nodes.png"
)

foreach ($layerName in $placeholderLayers) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.Dispose()
    $layerPath = Join-Path $layersDir $layerName
    $bmp.Save($layerPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  [PLACEHOLDER] $layerName (220x170 transparent)"
}

# Intent color definitions (A, R, G, B components)
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

# Layer color chart: dark background, colored horizontal strips per intent
$chartBmp = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$chartG   = [System.Drawing.Graphics]::FromImage($chartBmp)
$chartG.Clear([System.Drawing.Color]::FromArgb(255, 20, 20, 20))

$stripH = [int]($height / $intentEntries.Count)

for ($i = 0; $i -lt $intentEntries.Count; $i++) {
    $e     = $intentEntries[$i]
    $color = [System.Drawing.Color]::FromArgb($e.A, $e.R, $e.G, $e.B)
    $yTop  = $i * $stripH + 2
    $brush = New-Object System.Drawing.SolidBrush $color
    $chartG.FillRectangle($brush, 8, $yTop, ($width - 16), ($stripH - 4))
    $brush.Dispose()
}

$chartG.Dispose()
$chartPath = Join-Path $layersDir "layer-color-chart.png"
$chartBmp.Save($chartPath, [System.Drawing.Imaging.ImageFormat]::Png)
$chartBmp.Dispose()
Write-Host "  [CHART] layer-color-chart.png (intent color reference)"

# Copy contract JSON from repo
$srcContract = Join-Path $repoRoot "examples\deadmtl-layer-pack\system2-static-road-overlay-contract.json"
if (Test-Path $srcContract) {
    Copy-Item $srcContract (Join-Path $OutputDir "system2-static-road-overlay-contract.json")
    Write-Host "  [COPY]  system2-static-road-overlay-contract.json"
} else {
    Write-Warning "Contract JSON not found: $srcContract"
}

# Copy intent palette from repo
$srcPalette = Join-Path $repoRoot "examples\deadmtl-layer-pack\palettes\system2-static-road-intent-palette.json"
if (Test-Path $srcPalette) {
    Copy-Item $srcPalette (Join-Path $paletteDir "system2-static-road-intent-palette.json")
    Write-Host "  [COPY]  palettes/system2-static-road-intent-palette.json"
} else {
    Write-Warning "Intent palette not found: $srcPalette"
}

# Write README
$readmeContent = @'
# System 2 Static Road Overlay - Placeholder Pack

Status: CONTRACT_ONLY / NOT_RUNTIME_PROVEN

Generated by: examples\deadmtl-layer-pack\scripts\generate-system2-static-road-placeholders.ps1

These are authoring placeholder layers for the DeadMTL System 2 static road overlay.
All placeholder PNGs are 220x170 fully transparent.
No Lua override is generated. No compile step is performed. No .lotpack output is produced.

This pack is a planning artifact only.

Layers:
  layers/static_roads_local.png          - local_street (5-7 px)
  layers/static_roads_alleys.png         - alley_ruelle (3-4 px)
  layers/static_roads_service.png        - service_lane (2-3 px)
  layers/static_roads_parking_access.png - parking_access (3-5 px)
  layers/static_pedestrian_cuts.png      - pedestrian_cut (1-2 px)
  layers/static_road_nodes.png           - intersection_turn_deadend_nodes
  layers/layer-color-chart.png           - intent color reference chart

Intent palette:
  palettes/system2-static-road-intent-palette.json

Contract:
  system2-static-road-overlay-contract.json

See: docs/authoring/DEADMTL_SYSTEM2_STATIC_ROADS_ALLEYS_OVERLAY.md
'@

[System.IO.File]::WriteAllText((Join-Path $OutputDir "README.md"), $readmeContent, [System.Text.Encoding]::ASCII)
Write-Host "  [WRITE] README.md"

Write-Host ""
Write-Host "STATUS:         CONTRACT_ONLY"
Write-Host "RUNTIME_STATUS: NOT_RUNTIME_PROVEN"
Write-Host "WRITER_STATUS:  NOT_IMPLEMENTED"
Write-Host ""
Write-Host "VERDICT: MAP22D_SYSTEM2_STATIC_ROADS_ALLEYS_OVERLAY_CONTRACT_COMPLETE"

exit 0
