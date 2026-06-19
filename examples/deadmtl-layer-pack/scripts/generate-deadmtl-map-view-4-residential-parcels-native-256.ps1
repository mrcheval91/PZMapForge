#Requires -Version 5.1
<#
.SYNOPSIS
MAP-VIEW-4: Residential parcel topology planning view for map_00_component_0001.

Produces 3 PNG previews (all exactly 256x256) + HTML viewer + README under:
  .local\deadmtl-authoring\MAP_VIEW_4_RESIDENTIAL_PARCELS_NATIVE_256\

One PNG pixel = one map tile/cell.

NOT a playable Project Zomboid export.
NOT .lotpack / .lotheader / .lua / .bin.
Does not mutate source map_00.png.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

# --- Paths ---
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$RepoRoot = (Resolve-Path (Join-Path $RepoRoot "..")).Path
$RepoRoot = (Resolve-Path (Join-Path $RepoRoot "..")).Path

$RawPng = "E:\Omni\Zomboid\assets\raw\map_00.png"
$OutDir  = Join-Path $RepoRoot ".local\deadmtl-authoring\MAP_VIEW_4_RESIDENTIAL_PARCELS_NATIVE_256"
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# --- Component ---
$CompId   = "map_00_component_0001"
$BboxMinX = 124; $BboxMaxX = 212
$BboxMinY = 10;  $BboxMaxY = 69

# --- Colors ---
# Residential lots use blue-family only (3 tones).
# Sidewalks: light gray. Alley: mid gray. Background: near-black.
$CLR_BG       = [System.Drawing.Color]::FromArgb(18, 18, 24)
$CLR_BLUE_A   = [System.Drawing.Color]::FromArgb(58,  94,  174)   # residential blue A
$CLR_BLUE_B   = [System.Drawing.Color]::FromArgb(74,  110, 190)   # residential blue B
$CLR_BLUE_C   = [System.Drawing.Color]::FromArgb(42,  78,  158)   # residential blue C (east col)
$CLR_SIDEWALK = [System.Drawing.Color]::FromArgb(184, 184, 192)   # light gray
$CLR_ALLEY    = [System.Drawing.Color]::FromArgb(116, 116, 132)   # mid gray
$CLR_BBOX     = [System.Drawing.Color]::FromArgb(255, 60,  160)   # magenta
$CLR_TICK     = [System.Drawing.Color]::FromArgb(210, 230, 255)   # frontage tick (debug)
$CLR_BORDER   = [System.Drawing.Color]::FromArgb(10,  10,  18)    # lot divider (debug)

$ALLOWED_LOT_ARGB = @($CLR_BLUE_A.ToArgb(), $CLR_BLUE_B.ToArgb(), $CLR_BLUE_C.ToArgb())

# --- Parcel geometry ---
#
# Geometry invariants:
#   Main block  : X 124-201 (78 wide)
#   Right col   : X 202-212 (11 wide)
#   Both rows   : Y 10-69   (60 tall)
#
# Main block (Y axis):
#   North sidewalk : Y 10-12   (3 tiles)
#   North lots     : Y 13-37   (25 tiles deep)
#   Alley          : Y 38-39   (2 tiles)
#   South lots     : Y 40-64   (25 tiles deep)
#   South sidewalk : Y 65-69   (5 tiles)
#   Sum            : 3+25+2+25+5 = 60
#
# Right col (X axis):
#   East lots      : X 202-210 (9 wide)
#   East sidewalk  : X 211-212 (2 wide)
#   Sum            : 9+2 = 11
#
# Lot widths (10 north/south lots, 78 tiles total):
#   7,8,8,8,8,8,8,8,8,7  -> sum = 78
# East lot heights (8 lots, 60 tiles total):
#   8,7,8,7,8,7,8,7      -> sum = 60

$NSIDEWALK_Y1 = 10; $NSIDEWALK_Y2 = 12
$NORTH_Y1     = 13; $NORTH_Y2     = 37
$ALLEY_Y1     = 38; $ALLEY_Y2     = 39
$SOUTH_Y1     = 40; $SOUTH_Y2     = 64
$SSIDEWALK_Y1 = 65; $SSIDEWALK_Y2 = 69
$MAIN_X1      = 124; $MAIN_X2     = 201
$ELOT_X1      = 202; $ELOT_X2     = 210
$ESIDEWALK_X1 = 211; $ESIDEWALK_X2 = 212

$lotWidths      = @(7, 8, 8, 8, 8, 8, 8, 8, 8, 7)
$eastLotHeights = @(8, 7, 8, 7, 8, 7, 8, 7)

$lotXStarts = @()
$xCur = $MAIN_X1
foreach ($w in $lotWidths) { $lotXStarts += $xCur; $xCur += $w }

$eastLotYStarts = @()
$yCur = $BboxMinY
foreach ($h in $eastLotHeights) { $eastLotYStarts += $yCur; $yCur += $h }

# Build parcel list
$parcels = [System.Collections.Generic.List[object]]::new()

function AddParcel($id, $kind, $frontage, $x1, $x2, $y1, $y2) {
    $script:parcels.Add([PSCustomObject]@{
        Id=$id; Kind=$kind; Frontage=$frontage
        X1=$x1; X2=$x2; Y1=$y1; Y2=$y2
    })
}

AddParcel "SIDEWALK_NORTH" "SIDEWALK" "NORTH" $MAIN_X1     $MAIN_X2      $NSIDEWALK_Y1 $NSIDEWALK_Y2
AddParcel "SIDEWALK_SOUTH" "SIDEWALK" "SOUTH" $MAIN_X1     $MAIN_X2      $SSIDEWALK_Y1 $SSIDEWALK_Y2
AddParcel "SIDEWALK_EAST"  "SIDEWALK" "EAST"  $ESIDEWALK_X1 $ESIDEWALK_X2 $BboxMinY    $BboxMaxY
AddParcel "ALLEY"          "ALLEY"    "NONE"  $MAIN_X1     $MAIN_X2      $ALLEY_Y1    $ALLEY_Y2

for ($i = 0; $i -lt $lotWidths.Count; $i++) {
    $x1 = $lotXStarts[$i]; $x2 = $x1 + $lotWidths[$i] - 1
    AddParcel "NORTH_LOT_$i" "LOT" "NORTH" $x1 $x2 $NORTH_Y1 $NORTH_Y2
    AddParcel "SOUTH_LOT_$i" "LOT" "SOUTH" $x1 $x2 $SOUTH_Y1 $SOUTH_Y2
}

for ($i = 0; $i -lt $eastLotHeights.Count; $i++) {
    $y1 = $eastLotYStarts[$i]; $y2 = $y1 + $eastLotHeights[$i] - 1
    AddParcel "EAST_LOT_$i" "LOT" "EAST" $ELOT_X1 $ELOT_X2 $y1 $y2
}

# --- Color resolver ---
function GetParcelColor($p) {
    if ($p.Kind -eq "SIDEWALK") { return $CLR_SIDEWALK }
    if ($p.Kind -eq "ALLEY")    { return $CLR_ALLEY }
    $idx = [int]($p.Id -replace '^[A-Z_]+', '')
    switch ($p.Frontage) {
        "NORTH" { if ($idx % 2 -eq 0) { return $CLR_BLUE_A } else { return $CLR_BLUE_B } }
        "SOUTH" { if ($idx % 2 -eq 0) { return $CLR_BLUE_B } else { return $CLR_BLUE_A } }
        "EAST"  { if ($idx % 2 -eq 0) { return $CLR_BLUE_C } else { return $CLR_BLUE_A } }
        default { return $CLR_BG }
    }
}

# --- Helpers ---
function DrawBboxOnBmp($bmp) {
    for ($px = $BboxMinX; $px -le $BboxMaxX; $px++) {
        $bmp.SetPixel($px, $BboxMinY, $CLR_BBOX)
        $bmp.SetPixel($px, $BboxMaxY, $CLR_BBOX)
    }
    for ($py = ($BboxMinY + 1); $py -lt $BboxMaxY; $py++) {
        $bmp.SetPixel($BboxMinX, $py, $CLR_BBOX)
        $bmp.SetPixel($BboxMaxX, $py, $CLR_BBOX)
    }
}

function PaintAllParcels($g) {
    foreach ($p in $parcels) {
        $color = GetParcelColor $p
        $brush = New-Object System.Drawing.SolidBrush($color)
        $w = $p.X2 - $p.X1 + 1
        $h = $p.Y2 - $p.Y1 + 1
        $g.FillRectangle($brush, $p.X1, $p.Y1, $w, $h)
        $brush.Dispose()
    }
}

# ============================================================
# VALIDATION
# ============================================================
Write-Host ""
Write-Host "=== Parcel topology validation ==="

$lotParcels = @($parcels | Where-Object { $_.Kind -eq "LOT" })
$northLots  = @($lotParcels | Where-Object { $_.Frontage -eq "NORTH" })
$southLots  = @($lotParcels | Where-Object { $_.Frontage -eq "SOUTH" })
$eastLots   = @($lotParcels | Where-Object { $_.Frontage -eq "EAST" })

Write-Host "  Total residential lots : $($lotParcels.Count)"
Write-Host "  North-facing lots      : $($northLots.Count)"
Write-Host "  South-facing lots      : $($southLots.Count)"
Write-Host "  East-facing lots       : $($eastLots.Count)"

$vOk = $true

# V1: No lot spans both north-lot and south-lot Y bands (double frontage)
foreach ($lot in $lotParcels) {
    if ($lot.Y1 -le $NORTH_Y1 -and $lot.Y2 -ge $SOUTH_Y2) {
        Write-Host "  FAIL V1 double-frontage: $($lot.Id)"
        $vOk = $false
    }
}
if ($vOk) { Write-Host "  PASS V1: no double-frontage lots" }

# V2: East lots do not overlap X range of north/south lots
$vOkV2 = $true
foreach ($el in $eastLots) {
    foreach ($nl in $northLots) {
        if (-not ($el.X2 -lt $nl.X1 -or $el.X1 -gt $nl.X2)) {
            Write-Host "  FAIL V2 east/main overlap: $($el.Id) and $($nl.Id)"
            $vOkV2 = $false; $vOk = $false
        }
    }
}
if ($vOkV2) { Write-Host "  PASS V2: east lots are separate from north/south lots" }

# V3: Every lot has exactly one primary frontage (not NONE)
$vOkV3 = $true
foreach ($lot in $lotParcels) {
    if ($lot.Frontage -eq "NONE") {
        Write-Host "  FAIL V3 no frontage: $($lot.Id)"
        $vOkV3 = $false; $vOk = $false
    }
}
if ($vOkV3) { Write-Host "  PASS V3: every lot has exactly one primary frontage" }

# V4: All lot colors are blue-family
$vOkV4 = $true
foreach ($lot in $lotParcels) {
    $c = GetParcelColor $lot
    if ($ALLOWED_LOT_ARGB -notcontains $c.ToArgb()) {
        Write-Host "  FAIL V4 non-blue color: $($lot.Id)"
        $vOkV4 = $false; $vOk = $false
    }
}
if ($vOkV4) { Write-Host "  PASS V4: all lot colors are blue-family" }

# V5: Lot count sanity
if ($northLots.Count -ne $southLots.Count) {
    Write-Host "  FAIL V5: north/south lot count mismatch ($($northLots.Count) vs $($southLots.Count))"
    $vOk = $false
} else { Write-Host "  PASS V5: north and south lot counts match ($($northLots.Count) each)" }

if (-not $vOk) {
    Write-Host "FAIL: validation errors -- aborting"
    exit 1
}

# ============================================================
# IMAGE 1: Clean parcel view
# ============================================================
Write-Host ""
Write-Host "Generating Image 1: clean parcel view..."
if (-not (Test-Path $RawPng)) { Write-Host "FAIL: raw PNG not found: $RawPng"; exit 1 }
$rawBmp = New-Object System.Drawing.Bitmap($RawPng)
if ($rawBmp.Width -ne 256 -or $rawBmp.Height -ne 256) {
    Write-Host "FAIL: raw PNG is $($rawBmp.Width)x$($rawBmp.Height), expected 256x256"
    exit 1
}

$img1 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g1   = [System.Drawing.Graphics]::FromImage($img1)
$bgBrush = New-Object System.Drawing.SolidBrush($CLR_BG)
$g1.FillRectangle($bgBrush, 0, 0, 256, 256)
$bgBrush.Dispose()
PaintAllParcels $g1
$g1.Dispose()
DrawBboxOnBmp $img1

$out1 = Join-Path $OutDir "map_00_residential_parcels_native_256.png"
$img1.Save($out1, [System.Drawing.Imaging.ImageFormat]::Png)
$img1.Dispose()
Write-Host "  Saved: $out1  (256x256)"

# ============================================================
# IMAGE 2: Debug view -- frontage ticks + lot dividers
# ============================================================
Write-Host "Generating Image 2: debug parcel view..."

$img2 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g2   = [System.Drawing.Graphics]::FromImage($img2)
$bgBrush2 = New-Object System.Drawing.SolidBrush($CLR_BG)
$g2.FillRectangle($bgBrush2, 0, 0, 256, 256)
$bgBrush2.Dispose()
PaintAllParcels $g2
$g2.Dispose()

# Lot boundary dividers (1px dark line at right/bottom edge of each lot)
$lotParcels | ForEach-Object {
    $p = $_
    if ($p.Frontage -eq "NORTH" -or $p.Frontage -eq "SOUTH") {
        # vertical divider at X2 within lot Y range
        for ($py = $p.Y1; $py -le $p.Y2; $py++) {
            $img2.SetPixel($p.X2, $py, $CLR_BORDER)
        }
    } elseif ($p.Frontage -eq "EAST") {
        # horizontal divider at Y2 within lot X range
        for ($px = $p.X1; $px -le $p.X2; $px++) {
            $img2.SetPixel($px, $p.Y2, $CLR_BORDER)
        }
    }
}

# Frontage tick marks: bright pixel at center of each lot's street-facing edge
foreach ($p in $lotParcels) {
    $midX = [int](($p.X1 + $p.X2) / 2)
    $midY = [int](($p.Y1 + $p.Y2) / 2)
    switch ($p.Frontage) {
        "NORTH" { $img2.SetPixel($midX, $p.Y1, $CLR_TICK) }  # top edge
        "SOUTH" { $img2.SetPixel($midX, $p.Y2, $CLR_TICK) }  # bottom edge
        "EAST"  { $img2.SetPixel($p.X2, $midY, $CLR_TICK) }  # right edge
    }
    # Center dot to distinguish lot
    $img2.SetPixel($midX, $midY, $CLR_TICK)
}

DrawBboxOnBmp $img2

$out2 = Join-Path $OutDir "map_00_residential_parcels_debug_native_256.png"
$img2.Save($out2, [System.Drawing.Imaging.ImageFormat]::Png)
$img2.Dispose()
Write-Host "  Saved: $out2  (256x256)"

# ============================================================
# IMAGE 3: Overlay on raw source
# ============================================================
Write-Host "Generating Image 3: overlay on raw source..."

# Build semi-transparent parcel overlay (alpha 150 out of 255)
$overlayBmp = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gOvl = [System.Drawing.Graphics]::FromImage($overlayBmp)
$gOvl.Clear([System.Drawing.Color]::Transparent)
foreach ($p in $parcels) {
    $base = GetParcelColor $p
    $c    = [System.Drawing.Color]::FromArgb(150, $base.R, $base.G, $base.B)
    $brush = New-Object System.Drawing.SolidBrush($c)
    $gOvl.FillRectangle($brush, $p.X1, $p.Y1, ($p.X2 - $p.X1 + 1), ($p.Y2 - $p.Y1 + 1))
    $brush.Dispose()
}
$gOvl.Dispose()

$img3 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g3   = [System.Drawing.Graphics]::FromImage($img3)
$g3.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g3.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g3.DrawImage($rawBmp, 0, 0, 256, 256)
$g3.DrawImage($overlayBmp, 0, 0)
$g3.Dispose()
$overlayBmp.Dispose()
DrawBboxOnBmp $img3

$out3 = Join-Path $OutDir "map_00_residential_parcels_overlay_native_256.png"
$img3.Save($out3, [System.Drawing.Imaging.ImageFormat]::Png)
$img3.Dispose()
Write-Host "  Saved: $out3  (256x256)"

$rawBmp.Dispose()

# ============================================================
# HTML viewer (ASCII-only)
# ============================================================
Write-Host "Generating HTML viewer..."

$htmlLines = @(
'<!DOCTYPE html>',
'<html lang="en">',
'<head>',
'<meta charset="UTF-8">',
'<title>DeadMTL map_00 -- Residential Parcel Topology</title>',
'<style>',
'body { background:#0e0e14; color:#ccc; font-family:monospace; padding:16px; }',
'h1 { font-size:1em; color:#5a7ec8; }',
'h2 { font-size:0.9em; color:#888; margin-top:20px; }',
'p  { font-size:0.8em; line-height:1.5; }',
'.warn { color:#c87040; }',
'.row { display:flex; flex-wrap:wrap; gap:20px; margin-top:12px; }',
'.card { background:#1a1a22; border:1px solid #333; padding:8px; }',
'.card img { display:block; width:512px; height:512px; image-rendering:pixelated; image-rendering:crisp-edges; }',
'.card .lbl { font-size:0.7em; color:#666; margin-top:4px; }',
'.pal { margin-top:12px; font-size:0.8em; }',
'.swatch { display:inline-block; width:12px; height:12px; margin-right:4px; vertical-align:middle; }',
'</style>',
'</head>',
'<body>',
'<h1>DeadMTL map_00 -- Residential Parcel Topology Planning View</h1>',
'<p class="warn">',
'This is a residential parcel topology planning/debug view, NOT a playable Project Zomboid export.<br>',
'NOT .lotpack / .lotheader / .lua / .bin.<br>',
'All source PNGs are exactly 256x256 pixels. CSS zoom only (images are not enlarged on disk).',
'</p>',
'<p>',
'Component: map_00_component_0001 | Bbox X:124-212 Y:10-69 (89 by 60 tiles)<br>',
'Layout: 10 north-facing lots + 10 south-facing lots (back-to-back) + 8 east-facing lots (right column)<br>',
'Lot color family: blue only (3 close tones, alternating). Sidewalk: light gray. Alley: mid gray.<br>',
'Frontage direction: north lots front the north street (top), south lots front the south street (bottom),<br>',
'east lots front the right-side street. No lot spans both opposing street frontages.',
'</p>',
'<div class="pal">',
'<b>Palette:</b>',
'<span class="swatch" style="background:#3a5eae;"></span>Blue A (north/east lots even) &nbsp;',
'<span class="swatch" style="background:#4a6ebe;"></span>Blue B (north/south lots odd) &nbsp;',
'<span class="swatch" style="background:#2a4e9e;"></span>Blue C (east lots even) &nbsp;',
'<span class="swatch" style="background:#b8b8c0;"></span>Sidewalk &nbsp;',
'<span class="swatch" style="background:#747484;"></span>Alley &nbsp;',
'<span class="swatch" style="background:#ff3ca0;"></span>Bbox',
'</div>',
'<div class="row">',
'  <div class="card">',
'    <img src="map_00_residential_parcels_native_256.png" alt="clean parcel view">',
'    <div class="lbl">map_00_residential_parcels_native_256.png -- clean parcel view (256x256)</div>',
'  </div>',
'  <div class="card">',
'    <img src="map_00_residential_parcels_debug_native_256.png" alt="debug view">',
'    <div class="lbl">map_00_residential_parcels_debug_native_256.png -- lot dividers + frontage ticks (256x256)</div>',
'  </div>',
'  <div class="card">',
'    <img src="map_00_residential_parcels_overlay_native_256.png" alt="overlay">',
'    <div class="lbl">map_00_residential_parcels_overlay_native_256.png -- parcel overlay on raw source (256x256)</div>',
'  </div>',
'</div>',
'<h2>Notes</h2>',
'<p>',
'Lot parcel fill is NOT the same as building footprint. Building footprints would sit inside parcels with setbacks.<br>',
'This planning view shows parcel boundaries only, not buildings.<br>',
'The debug image shows: lot boundary dividers (1px dark line), frontage tick (bright dot on street-facing edge),',
'and center dot for lot identification.',
'</p>',
'</body>',
'</html>'
)

$outHtml = Join-Path $OutDir "map_00_residential_parcels_viewer.html"
[System.IO.File]::WriteAllLines($outHtml, $htmlLines, [System.Text.Encoding]::ASCII)
Write-Host "  Saved: $outHtml"

# ============================================================
# Dimension verification
# ============================================================
Write-Host ""
Write-Host "=== Dimension verification ==="
$pngs  = @($out1, $out2, $out3)
$dimOk = $true
foreach ($p in $pngs) {
    $b = New-Object System.Drawing.Bitmap($p)
    $w = $b.Width; $h = $b.Height
    $b.Dispose()
    $ok = if ($w -eq 256 -and $h -eq 256) { "OK" } else { "FAIL"; $dimOk = $false }
    Write-Host "  [$ok] $(Split-Path $p -Leaf)  ${w}x${h}"
}

# ============================================================
# Forbidden artifact scan
# ============================================================
Write-Host ""
Write-Host "=== Forbidden artifact scan ==="
$exts = @("lotpack","lotheader","lua","bin")
$forbiddenFound = @()
foreach ($ext in $exts) {
    $f = Get-ChildItem -Path $OutDir -Filter ("*." + $ext) -Recurse -ErrorAction SilentlyContinue
    if ($f) { $forbiddenFound += $f }
}
if (Test-Path (Join-Path $OutDir "steamapps")) { $forbiddenFound += "steamapps" }
$mapsPath = Join-Path (Join-Path $OutDir "media") "maps"
if (Test-Path $mapsPath) { $forbiddenFound += "media/maps" }

if ($forbiddenFound.Count -gt 0) {
    Write-Host "  FAIL: $($forbiddenFound.Count) forbidden artifact(s) found"
    $forbiddenFound | ForEach-Object { Write-Host "    $_" }
    $dimOk = $false
} else {
    Write-Host "  POST_MAP_VIEW_4_RESIDENTIAL_PARCELS_FORBIDDEN_SCAN PASS (0 forbidden artifacts)"
}

# ============================================================
# Final report
# ============================================================
Write-Host ""
Write-Host "=== MAP_VIEW_4_RESIDENTIAL_PARCELS_NATIVE_256 summary ==="
Write-Host "  Component       : $CompId"
Write-Host "  Bbox            : X $BboxMinX-$BboxMaxX  Y $BboxMinY-$BboxMaxY"
Write-Host "  North-facing    : $($northLots.Count) lots  (Y $NORTH_Y1-$NORTH_Y2, 25 tiles deep)"
Write-Host "  South-facing    : $($southLots.Count) lots  (Y $SOUTH_Y1-$SOUTH_Y2, 25 tiles deep)"
Write-Host "  East-facing     : $($eastLots.Count) lots   (X $ELOT_X1-$ELOT_X2, 9 tiles wide)"
Write-Host "  Sidewalk strips : NORTH (Y $NSIDEWALK_Y1-$NSIDEWALK_Y2) + SOUTH (Y $SSIDEWALK_Y1-$SSIDEWALK_Y2) + EAST (X $ESIDEWALK_X1-$ESIDEWALK_X2)"
Write-Host "  Alley           : Y $ALLEY_Y1-$ALLEY_Y2"
Write-Host ""
Write-Host "Output folder: $OutDir"
if ($dimOk) {
    Write-Host "STATUS: ALL PASS -- 3 PNGs 256x256, 0 forbidden artifacts"
    exit 0
} else {
    Write-Host "STATUS: FAIL -- see above"
    exit 1
}
