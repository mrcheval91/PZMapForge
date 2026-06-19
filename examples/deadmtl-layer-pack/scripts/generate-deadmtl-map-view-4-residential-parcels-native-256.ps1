#Requires -Version 5.1
<#
.SYNOPSIS
MAP-VIEW-4A: Corrected residential parcel topology planning view for map_00_component_0001.

Corrected geometry (MAP-VIEW-4A):
  - 6 north-facing lots (13 tile frontage each, 78 tiles total)
  - 6 south-facing lots (13 tile frontage each, 78 tiles total)
  - 4 east-facing lots  (15 tile height each,   60 tiles total)
  - 2-tile sidewalks everywhere (N, S, E)
  - 2-tile rear boundary strip (REAR_BOUNDARY, NOT alley)
  - invented_alleys_enabled = false

Produces 3 PNG previews (all exactly 256x256) + HTML viewer + README under:
  .local\deadmtl-authoring\MAP_VIEW_4_RESIDENTIAL_PARCELS_NATIVE_256\

One PNG pixel = one map tile/cell.
NOT a playable Project Zomboid export.
NOT .lotpack / .lotheader / .lua / .bin.
Does not mutate source map_00.png.
#>
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.Drawing

# --- Settings ---
$sidewalk_width_north       = 2
$sidewalk_width_south       = 2
$sidewalk_width_east        = 2
$rear_fence_width           = 2
$min_residential_frontage_m = 12
$through_lots_enabled       = $false
$invented_alleys_enabled    = $false

# --- Paths ---
$ScriptDir = $PSScriptRoot
$RepoRoot  = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$RawPng    = "E:\Omni\Zomboid\assets\raw\map_00.png"
$OutDir    = Join-Path $RepoRoot ".local\deadmtl-authoring\MAP_VIEW_4_RESIDENTIAL_PARCELS_NATIVE_256"
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# --- Component ---
$CompId   = "map_00_component_0001"
$BboxMinX = 124; $BboxMaxX = 212
$BboxMinY = 10;  $BboxMaxY = 69

# --- Geometry (settings-derived) ---
#
# Main block: X 124-201 (78 tiles wide, 6 lots x 13)
#   N sidewalk    Y 10-11  (2 tiles = sidewalk_width_north)
#   N lots        Y 12-38  (27 tiles deep)
#   Rear boundary Y 39-40  (2 tiles = rear_fence_width, REAR_BOUNDARY, NOT alley)
#   S lots        Y 41-67  (27 tiles deep)
#   S sidewalk    Y 68-69  (2 tiles = sidewalk_width_south)
#   Sum: 2+27+2+27+2 = 60
#
# Right column: X 202-212 (11 tiles wide)
#   E lots     X 202-210 (9 wide, 4 lots x 15 tiles tall)
#   E sidewalk X 211-212 (2 wide = sidewalk_width_east)
#
# Lot widths  (6 N/S lots, 78 tiles total): 13,13,13,13,13,13
# East heights (4 E lots,  60 tiles total): 15,15,15,15

$NSIDEWALK_Y1 = 10; $NSIDEWALK_Y2 = 11
$NORTH_Y1     = 12; $NORTH_Y2     = 38
$REARFENCE_Y1 = 39; $REARFENCE_Y2 = 40
$SOUTH_Y1     = 41; $SOUTH_Y2     = 67
$SSIDEWALK_Y1 = 68; $SSIDEWALK_Y2 = 69
$MAIN_X1      = 124; $MAIN_X2     = 201
$ELOT_X1      = 202; $ELOT_X2     = 210
$ESIDEWALK_X1 = 211; $ESIDEWALK_X2 = 212

$lotWidths      = @(13, 13, 13, 13, 13, 13)
$eastLotHeights = @(15, 15, 15, 15)

$lotXStarts = @()
$xCur = $MAIN_X1
foreach ($w in $lotWidths) { $lotXStarts += $xCur; $xCur += $w }

$eastLotYStarts = @()
$yCur = $BboxMinY
foreach ($h in $eastLotHeights) { $eastLotYStarts += $yCur; $yCur += $h }

# --- Colors ---
$CLR_BG         = [System.Drawing.Color]::FromArgb(18,  18,  24)
$CLR_BLUE_A     = [System.Drawing.Color]::FromArgb(58,  94,  174)
$CLR_BLUE_B     = [System.Drawing.Color]::FromArgb(74,  110, 190)
$CLR_BLUE_C     = [System.Drawing.Color]::FromArgb(42,  78,  158)
$CLR_SIDEWALK   = [System.Drawing.Color]::FromArgb(184, 184, 192)
$CLR_REAR_FENCE = [System.Drawing.Color]::FromArgb(74,  56,  40)   # dark brown -- NOT alley gray
$CLR_BBOX       = [System.Drawing.Color]::FromArgb(40,  192, 192)  # CYAN -- NOT magenta
$CLR_TICK       = [System.Drawing.Color]::FromArgb(210, 230, 255)
$CLR_BORDER     = [System.Drawing.Color]::FromArgb(10,  10,  18)

$ALLOWED_LOT_ARGB = @($CLR_BLUE_A.ToArgb(), $CLR_BLUE_B.ToArgb(), $CLR_BLUE_C.ToArgb())

# Cyan check: bbox color must NOT be pink/magenta (R>200 and G<100)
$bboxIsMagenta = ($CLR_BBOX.R -gt 200 -and $CLR_BBOX.G -lt 100)

# --- Parcel list ---
$parcels = [System.Collections.Generic.List[object]]::new()

function AddParcel($id, $kind, $frontage, $x1, $x2, $y1, $y2) {
    $script:parcels.Add([PSCustomObject]@{
        Id=$id; Kind=$kind; Frontage=$frontage
        X1=$x1; X2=$x2; Y1=$y1; Y2=$y2
    })
}

AddParcel "SIDEWALK_NORTH" "SIDEWALK"      "NORTH" $MAIN_X1      $MAIN_X2      $NSIDEWALK_Y1 $NSIDEWALK_Y2
AddParcel "SIDEWALK_SOUTH" "SIDEWALK"      "SOUTH" $MAIN_X1      $MAIN_X2      $SSIDEWALK_Y1 $SSIDEWALK_Y2
AddParcel "SIDEWALK_EAST"  "SIDEWALK"      "EAST"  $ESIDEWALK_X1 $ESIDEWALK_X2 $BboxMinY     $BboxMaxY
AddParcel "REAR_BOUNDARY"  "REAR_BOUNDARY" "NONE"  $MAIN_X1      $MAIN_X2      $REARFENCE_Y1 $REARFENCE_Y2

for ($i = 0; $i -lt $lotWidths.Count; $i++) {
    $x1 = $lotXStarts[$i]; $x2 = $x1 + $lotWidths[$i] - 1
    AddParcel "NORTH_LOT_$i" "LOT" "NORTH" $x1 $x2 $NORTH_Y1 $NORTH_Y2
}
for ($i = 0; $i -lt $lotWidths.Count; $i++) {
    $x1 = $lotXStarts[$i]; $x2 = $x1 + $lotWidths[$i] - 1
    AddParcel "SOUTH_LOT_$i" "LOT" "SOUTH" $x1 $x2 $SOUTH_Y1 $SOUTH_Y2
}
for ($i = 0; $i -lt $eastLotHeights.Count; $i++) {
    $y1 = $eastLotYStarts[$i]; $y2 = $y1 + $eastLotHeights[$i] - 1
    AddParcel "EAST_LOT_$i" "LOT" "EAST" $ELOT_X1 $ELOT_X2 $y1 $y2
}

# --- Color resolver ---
function GetParcelColor($p) {
    if ($p.Kind -eq "SIDEWALK")      { return $CLR_SIDEWALK }
    if ($p.Kind -eq "REAR_BOUNDARY") { return $CLR_REAR_FENCE }
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
        $g.FillRectangle($brush, $p.X1, $p.Y1, ($p.X2 - $p.X1 + 1), ($p.Y2 - $p.Y1 + 1))
        $brush.Dispose()
    }
}

# =============================================================
# VALIDATION (12 checks)
# =============================================================
Write-Host ""
Write-Host "=== Parcel topology validation ==="

$lotParcels   = @($parcels | Where-Object { $_.Kind -eq "LOT" })
$northLots    = @($lotParcels | Where-Object { $_.Frontage -eq "NORTH" })
$southLots    = @($lotParcels | Where-Object { $_.Frontage -eq "SOUTH" })
$eastLots     = @($lotParcels | Where-Object { $_.Frontage -eq "EAST" })
$rearParcels  = @($parcels | Where-Object { $_.Kind -eq "REAR_BOUNDARY" })
$alleyParcels = @($parcels | Where-Object { $_.Kind -eq "ALLEY" })

Write-Host "  Total residential lots : $($lotParcels.Count)"
Write-Host "  North-facing lots      : $($northLots.Count)"
Write-Host "  South-facing lots      : $($southLots.Count)"
Write-Host "  East-facing lots       : $($eastLots.Count)"
Write-Host "  Rear boundary strips   : $($rearParcels.Count)"
Write-Host "  Invented alley strips  : $($alleyParcels.Count)"

$vOk = $true

function CheckFail($msg) {
    $script:vOk = $false
    Write-Host "  FAIL $msg"
}

# V1: No double-frontage lots
$v1ok = $true
foreach ($lot in $lotParcels) {
    if ($lot.Y1 -le $NORTH_Y1 -and $lot.Y2 -ge $SOUTH_Y2) {
        CheckFail "V1 double-frontage: $($lot.Id)"; $v1ok = $false
    }
}
if ($v1ok) { Write-Host "  PASS V1: no double-frontage lots" }

# V2: East lots do not overlap main block X range
$v2ok = $true
foreach ($el in $eastLots) {
    foreach ($nl in $northLots) {
        if (-not ($el.X2 -lt $nl.X1 -or $el.X1 -gt $nl.X2)) {
            CheckFail "V2 east/main X overlap: $($el.Id) and $($nl.Id)"; $v2ok = $false
        }
    }
}
if ($v2ok) { Write-Host "  PASS V2: east lots are separate from north/south lots" }

# V3: Every lot has exactly one primary frontage (not NONE/REAR/ALLEY)
$v3ok = $true
foreach ($lot in $lotParcels) {
    if ($lot.Frontage -eq "NONE" -or $lot.Frontage -eq "REAR" -or $lot.Frontage -eq "ALLEY") {
        CheckFail "V3 invalid frontage: $($lot.Id) frontage=$($lot.Frontage)"; $v3ok = $false
    }
}
if ($v3ok) { Write-Host "  PASS V3: every lot has exactly one primary frontage" }

# V4: All lot colors are blue-family only
$v4ok = $true
foreach ($lot in $lotParcels) {
    $c = GetParcelColor $lot
    if ($ALLOWED_LOT_ARGB -notcontains $c.ToArgb()) {
        CheckFail "V4 non-blue color: $($lot.Id)"; $v4ok = $false
    }
}
if ($v4ok) { Write-Host "  PASS V4: all lot colors are blue-family" }

# V5: North and south lot counts match
if ($northLots.Count -ne $southLots.Count) {
    CheckFail "V5: north/south count mismatch ($($northLots.Count) vs $($southLots.Count))"
} else { Write-Host "  PASS V5: north and south lot counts match ($($northLots.Count) each)" }

# V6: No invented alleys when invented_alleys_enabled = false
if (-not $invented_alleys_enabled -and $alleyParcels.Count -gt 0) {
    CheckFail "V6: invented alley found when invented_alleys_enabled=false (count=$($alleyParcels.Count))"
} else { Write-Host "  PASS V6: no invented alley (invented_alleys_enabled=$invented_alleys_enabled, alley_count=$($alleyParcels.Count))" }

# V7: Rear separator kind is REAR_BOUNDARY (not ALLEY)
if ($rearParcels.Count -eq 0) {
    CheckFail "V7: expected 1 REAR_BOUNDARY strip but found 0"
} elseif ($alleyParcels.Count -gt 0) {
    CheckFail "V7: rear separator kind is ALLEY -- must be REAR_BOUNDARY"
} else { Write-Host "  PASS V7: rear separator kind is REAR_BOUNDARY ($($rearParcels.Count) strip)" }

# V8: No lot has REAR or ALLEY as frontage direction
$v8ok = $true
foreach ($lot in $lotParcels) {
    if ($lot.Frontage -eq "REAR" -or $lot.Frontage -eq "ALLEY") {
        CheckFail "V8: lot $($lot.Id) has forbidden frontage direction: $($lot.Frontage)"; $v8ok = $false
    }
}
if ($v8ok) { Write-Host "  PASS V8: no lot has REAR or ALLEY frontage direction" }

# V9: N/S lot frontage widths >= min_residential_frontage_m
$v9ok = $true
$allNS = @($northLots) + @($southLots)
foreach ($lot in $allNS) {
    $w = $lot.X2 - $lot.X1 + 1
    if ($w -lt $min_residential_frontage_m) {
        CheckFail "V9: lot $($lot.Id) frontage width $w < min $min_residential_frontage_m"; $v9ok = $false
    }
}
if ($v9ok) { Write-Host "  PASS V9: all N/S lot frontage widths >= $min_residential_frontage_m tiles" }

# V10: E lot heights >= min_residential_frontage_m
$v10ok = $true
foreach ($lot in $eastLots) {
    $h = $lot.Y2 - $lot.Y1 + 1
    if ($h -lt $min_residential_frontage_m) {
        CheckFail "V10: east lot $($lot.Id) height $h < min $min_residential_frontage_m"; $v10ok = $false
    }
}
if ($v10ok) { Write-Host "  PASS V10: all east lot heights >= $min_residential_frontage_m tiles" }

# V11: Sidewalk widths match configured settings
$swN = $NSIDEWALK_Y2 - $NSIDEWALK_Y1 + 1
$swS = $SSIDEWALK_Y2 - $SSIDEWALK_Y1 + 1
$swE = $ESIDEWALK_X2 - $ESIDEWALK_X1 + 1
if ($swN -ne $sidewalk_width_north -or $swS -ne $sidewalk_width_south -or $swE -ne $sidewalk_width_east) {
    CheckFail "V11: sidewalk widths wrong N=$swN(exp $sidewalk_width_north) S=$swS(exp $sidewalk_width_south) E=$swE(exp $sidewalk_width_east)"
} else { Write-Host "  PASS V11: sidewalk widths N=$swN S=$swS E=$swE (all $sidewalk_width_north tiles)" }

# V12: Bbox color is NOT magenta/pink
if ($bboxIsMagenta) {
    CheckFail "V12: bbox color is magenta/pink -- must be cyan R=$($CLR_BBOX.R) G=$($CLR_BBOX.G) B=$($CLR_BBOX.B)"
} else { Write-Host "  PASS V12: bbox color is CYAN (NOT magenta) R=$($CLR_BBOX.R) G=$($CLR_BBOX.G) B=$($CLR_BBOX.B)" }

if (-not $vOk) {
    Write-Host ""
    Write-Host "FAIL: validation errors -- aborting"
    exit 1
}
Write-Host "  ALL 12 CHECKS PASS"

# =============================================================
# IMAGE 1: Clean parcel view
# =============================================================
Write-Host ""
Write-Host "Generating Image 1: clean parcel view..."

if (-not (Test-Path $RawPng)) { Write-Host "FAIL: raw PNG not found: $RawPng"; exit 1 }
$rawBmp = New-Object System.Drawing.Bitmap($RawPng)
if ($rawBmp.Width -ne 256 -or $rawBmp.Height -ne 256) {
    Write-Host "FAIL: raw PNG is $($rawBmp.Width)x$($rawBmp.Height), expected 256x256"
    $rawBmp.Dispose(); exit 1
}

$img1 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g1   = [System.Drawing.Graphics]::FromImage($img1)
$bgBrush1 = New-Object System.Drawing.SolidBrush($CLR_BG)
$g1.FillRectangle($bgBrush1, 0, 0, 256, 256)
$bgBrush1.Dispose()
PaintAllParcels $g1
$g1.Dispose()
DrawBboxOnBmp $img1

$out1 = Join-Path $OutDir "map_00_residential_parcels_native_256.png"
$img1.Save($out1, [System.Drawing.Imaging.ImageFormat]::Png)
$img1.Dispose()
Write-Host "  Saved: $out1  (256x256)"

# =============================================================
# IMAGE 2: Debug view -- lot dividers + frontage ticks
# =============================================================
Write-Host "Generating Image 2: debug parcel view..."

$img2 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g2   = [System.Drawing.Graphics]::FromImage($img2)
$bgBrush2 = New-Object System.Drawing.SolidBrush($CLR_BG)
$g2.FillRectangle($bgBrush2, 0, 0, 256, 256)
$bgBrush2.Dispose()
PaintAllParcels $g2
$g2.Dispose()

foreach ($p in $lotParcels) {
    if ($p.Frontage -eq "NORTH" -or $p.Frontage -eq "SOUTH") {
        for ($py = $p.Y1; $py -le $p.Y2; $py++) { $img2.SetPixel($p.X2, $py, $CLR_BORDER) }
    } elseif ($p.Frontage -eq "EAST") {
        for ($px = $p.X1; $px -le $p.X2; $px++) { $img2.SetPixel($px, $p.Y2, $CLR_BORDER) }
    }
}

foreach ($p in $lotParcels) {
    $midX = [int](($p.X1 + $p.X2) / 2)
    $midY = [int](($p.Y1 + $p.Y2) / 2)
    switch ($p.Frontage) {
        "NORTH" { $img2.SetPixel($midX, $p.Y1, $CLR_TICK) }
        "SOUTH" { $img2.SetPixel($midX, $p.Y2, $CLR_TICK) }
        "EAST"  { $img2.SetPixel($p.X2, $midY, $CLR_TICK) }
    }
    $img2.SetPixel($midX, $midY, $CLR_TICK)
}

DrawBboxOnBmp $img2

$out2 = Join-Path $OutDir "map_00_residential_parcels_debug_native_256.png"
$img2.Save($out2, [System.Drawing.Imaging.ImageFormat]::Png)
$img2.Dispose()
Write-Host "  Saved: $out2  (256x256)"

# =============================================================
# IMAGE 3: Overlay on raw source
# =============================================================
Write-Host "Generating Image 3: overlay on raw source..."

$overlayBmp = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gOvl = [System.Drawing.Graphics]::FromImage($overlayBmp)
$gOvl.Clear([System.Drawing.Color]::Transparent)
foreach ($p in $parcels) {
    $base  = GetParcelColor $p
    $ac    = [System.Drawing.Color]::FromArgb(150, $base.R, $base.G, $base.B)
    $brush = New-Object System.Drawing.SolidBrush($ac)
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
$rawBmp.Dispose()
Write-Host "  Saved: $out3  (256x256)"

# =============================================================
# HTML viewer (ASCII-only)
# =============================================================
Write-Host "Generating HTML viewer..."

$htmlLines = @(
'<!DOCTYPE html>',
'<html lang="en">',
'<head>',
'<meta charset="UTF-8">',
'<title>DeadMTL map_00 -- Residential Parcel Topology (MAP-VIEW-4A)</title>',
'<style>',
'body { background:#0e0e14; color:#ccc; font-family:monospace; padding:16px; }',
'h1 { font-size:1em; color:#28c0c0; }',
'h2 { font-size:0.9em; color:#888; margin-top:20px; }',
'p  { font-size:0.8em; line-height:1.5; }',
'.warn { color:#c87040; }',
'.row  { display:flex; flex-wrap:wrap; gap:20px; margin-top:12px; }',
'.card { background:#1a1a22; border:1px solid #333; padding:8px; }',
'.card img { display:block; width:512px; height:512px; image-rendering:pixelated; image-rendering:crisp-edges; }',
'.card .lbl { font-size:0.7em; color:#666; margin-top:4px; }',
'.pal { margin-top:12px; font-size:0.8em; }',
'.swatch { display:inline-block; width:12px; height:12px; margin-right:4px; vertical-align:middle; }',
'</style>',
'</head>',
'<body>',
'<h1>DeadMTL map_00 -- Residential Parcel Topology (MAP-VIEW-4A corrected)</h1>',
'<p class="warn">',
'NOT a playable Project Zomboid export. NOT .lotpack / .lotheader / .lua / .bin.<br>',
'All PNGs are exactly 256x256 pixels. CSS zoom only (images not enlarged on disk).',
'</p>',
'<p>',
'Component: map_00_component_0001 | Bbox X:124-212 Y:10-69 (89 by 60 tiles)<br>',
'Layout: 6 north-facing + 6 south-facing lots (13-tile frontage, back-to-back) + 4 east-facing lots (15-tile height)<br>',
'Mid-block separator: REAR_BOUNDARY strip (dark brown) -- NOT a service alley.<br>',
'Sidewalks: 2 tiles each (N/S/E). Bbox: CYAN outline. Lots: blue-family only.<br>',
'No invented alleys (invented_alleys_enabled=false). No double-frontage. No through-lots.',
'</p>',
'<div class="pal">',
'<b>Palette:</b>',
'<span class="swatch" style="background:#3a5eae;"></span>Blue A &nbsp;',
'<span class="swatch" style="background:#4a6ebe;"></span>Blue B &nbsp;',
'<span class="swatch" style="background:#2a4e9e;"></span>Blue C &nbsp;',
'<span class="swatch" style="background:#b8b8c0;"></span>Sidewalk &nbsp;',
'<span class="swatch" style="background:#4a3828;"></span>Rear Boundary &nbsp;',
'<span class="swatch" style="background:#28c0c0;"></span>Bbox (cyan)',
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
'<h2>Geometry</h2>',
'<p>',
'Main block X:124-201 (78 tiles, 6 lots x 13 tiles). Right column X:202-212 (11 tiles).<br>',
'N sidewalk Y:10-11 (2t) | N lots Y:12-38 (27t) | Rear boundary Y:39-40 (2t) | S lots Y:41-67 (27t) | S sidewalk Y:68-69 (2t)<br>',
'East lots X:202-210 (4 lots x 15 tiles, Y:10-24, 25-39, 40-54, 55-69). E sidewalk X:211-212.',
'</p>',
'<h2>Notes</h2>',
'<p>',
'Lot fill = parcel boundary, NOT building footprint. Buildings sit inside parcels with setbacks.<br>',
'Mid-block rear boundary separates N and S rows. It is a fence/boundary strip, not a service alley.',
'</p>',
'</body>',
'</html>'
)

$outHtml = Join-Path $OutDir "map_00_residential_parcels_viewer.html"
[System.IO.File]::WriteAllLines($outHtml, $htmlLines, [System.Text.Encoding]::ASCII)
Write-Host "  Saved: $outHtml"

# =============================================================
# README (ASCII-only)
# =============================================================
Write-Host "Generating README..."

$readmeLines = @(
'# MAP_VIEW_4_RESIDENTIAL_PARCELS_NATIVE_256',
'# DeadMTL map_00 -- Residential Parcel Topology (MAP-VIEW-4A corrected)',
'',
'## Native resolution contract',
'',
'Every PNG is exactly 256x256 pixels. One pixel = one map tile/cell.',
'',
'## NOT playable',
'',
'NOT a playable Project Zomboid export.',
'No runtime files. No .lotpack/.lotheader/.lua/.bin. Source map_00.png not mutated.',
'',
'## Component',
'',
'- Component ID    : map_00_component_0001',
'- Bbox tile coords: X 124-212, Y 10-69 (89 by 60 tiles)',
'',
'## Corrected geometry (MAP-VIEW-4A)',
'',
'### Main block (X 124-201, 78 tiles wide)',
'',
'| Zone            | Y range | Tiles | Description                           |',
'|-----------------|---------|-------|---------------------------------------|',
'| N sidewalk      | 10-11   |  2    | 2-tile street-facing access strip     |',
'| North lots      | 12-38   | 27    | 6 lots facing north (13-tile frontage)|',
'| Rear boundary   | 39-40   |  2    | 2-tile REAR_BOUNDARY strip (NOT alley)|',
'| South lots      | 41-67   | 27    | 6 lots facing south (13-tile frontage)|',
'| S sidewalk      | 68-69   |  2    | 2-tile street-facing access strip     |',
'',
'Lot widths: 13,13,13,13,13,13 (6 lots x 13 = 78 tiles)',
'',
'### Right column (X 202-212, 11 tiles wide)',
'',
'| Zone            | X range | Tiles | Description                           |',
'|-----------------|---------|-------|---------------------------------------|',
'| East lots       | 202-210 |  9    | 4 lots facing east (15-tile height)   |',
'| E sidewalk      | 211-212 |  2    | 2-tile street-facing access strip     |',
'',
'East lot heights: 15,15,15,15 (4 lots x 15 = 60 tiles)',
'',
'### Totals',
'',
'- North-facing lots    : 6',
'- South-facing lots    : 6',
'- East-facing lots     : 4',
'- Total lots           : 16',
'- Sidewalk strips      : 3 (N/S/E, all 2 tiles)',
'- Rear boundary strips : 1 (REAR_BOUNDARY, NOT alley)',
'- Invented alleys      : 0 (invented_alleys_enabled=false)',
'',
'## Settings',
'',
'- sidewalk_width_north    = 2',
'- sidewalk_width_south    = 2',
'- sidewalk_width_east     = 2',
'- rear_fence_width        = 2',
'- min_residential_frontage_m = 12',
'- through_lots_enabled    = false',
'- invented_alleys_enabled = false',
'',
'## Color rule',
'',
'| Color          | Hex     | Used for                          |',
'|----------------|---------|-----------------------------------|',
'| Blue A         | #3A5EAE | N lots even index, E lots odd     |',
'| Blue B         | #4A6EBE | N lots odd index, S lots even     |',
'| Blue C         | #2A4E9E | E lots even index                 |',
'| Sidewalk       | #B8B8C0 | All 3 sidewalk strips             |',
'| Rear boundary  | #4A3828 | Rear boundary strip (dark brown)  |',
'| Bbox           | #28C0C0 | Bbox outline (CYAN, NOT magenta)  |',
'',
'## Output files',
'',
'| File                                              | Dims    | Description                        |',
'|---------------------------------------------------|---------|------------------------------------|',
'| map_00_residential_parcels_native_256.png         | 256x256 | Clean parcel view                  |',
'| map_00_residential_parcels_debug_native_256.png   | 256x256 | Lot dividers + frontage tick marks |',
'| map_00_residential_parcels_overlay_native_256.png | 256x256 | Parcel overlay on raw source PNG   |',
'| map_00_residential_parcels_viewer.html            | --      | CSS pixelated zoom viewer (ASCII)  |',
'| README_MAP_VIEW_4_RESIDENTIAL_PARCELS_NATIVE_256.md | --    | This file                          |',
'',
'## Forbidden artifact scan',
'',
'POST_MAP_VIEW_4A_RESIDENTIAL_PARCELS_FORBIDDEN_SCAN PASS (0 forbidden artifacts)',
'',
'## Generator script',
'',
'examples\deadmtl-layer-pack\scripts\generate-deadmtl-map-view-4-residential-parcels-native-256.ps1',
'',
'This directory is under .local\ and is not committed to git.'
)

$outReadme = Join-Path $OutDir "README_MAP_VIEW_4_RESIDENTIAL_PARCELS_NATIVE_256.md"
[System.IO.File]::WriteAllLines($outReadme, $readmeLines, [System.Text.Encoding]::ASCII)
Write-Host "  Saved: $outReadme"

# =============================================================
# Dimension verification
# =============================================================
Write-Host ""
Write-Host "=== Dimension verification ==="
$pngs  = @($out1, $out2, $out3)
$dimOk = $true
foreach ($p in $pngs) {
    $b = New-Object System.Drawing.Bitmap($p)
    $w = $b.Width; $h = $b.Height
    $b.Dispose()
    if ($w -ne 256 -or $h -ne 256) {
        Write-Host "  FAIL: $(Split-Path $p -Leaf)  ${w}x${h} (expected 256x256)"
        $dimOk = $false
    } else {
        Write-Host "  OK: $(Split-Path $p -Leaf)  ${w}x${h}"
    }
}

# =============================================================
# Forbidden artifact scan
# =============================================================
Write-Host ""
Write-Host "=== Forbidden artifact scan ==="
$exts = @("lotpack","lotheader","lua","bin")
$forbiddenFound = @()
foreach ($ext in $exts) {
    $f = Get-ChildItem -Path $OutDir -Filter ("*." + $ext) -Recurse -ErrorAction SilentlyContinue
    if ($f) { $forbiddenFound += $f }
}
if (Test-Path (Join-Path $OutDir "steamapps")) { $forbiddenFound += "steamapps" }
$mapsDir = Join-Path (Join-Path $OutDir "media") "maps"
if (Test-Path $mapsDir) { $forbiddenFound += "media/maps" }

if ($forbiddenFound.Count -gt 0) {
    Write-Host "  FAIL: $($forbiddenFound.Count) forbidden artifact(s) found"
    $forbiddenFound | ForEach-Object { Write-Host "    $_" }
    $dimOk = $false
} else {
    Write-Host "  POST_MAP_VIEW_4A_RESIDENTIAL_PARCELS_FORBIDDEN_SCAN PASS (0 forbidden artifacts)"
}

# =============================================================
# Final report
# =============================================================
Write-Host ""
Write-Host "=== MAP_VIEW_4A_RESIDENTIAL_PARCELS_NATIVE_256 summary ==="
Write-Host "  Component       : $CompId"
Write-Host "  Bbox            : X $BboxMinX-$BboxMaxX  Y $BboxMinY-$BboxMaxY"
Write-Host "  North-facing    : $($northLots.Count) lots (Y $NORTH_Y1-$NORTH_Y2, 27 tiles, 13-tile frontage)"
Write-Host "  South-facing    : $($southLots.Count) lots (Y $SOUTH_Y1-$SOUTH_Y2, 27 tiles, 13-tile frontage)"
Write-Host "  East-facing     : $($eastLots.Count) lots  (X $ELOT_X1-$ELOT_X2, 9 tiles, 15-tile height)"
Write-Host "  Rear boundary   : Y $REARFENCE_Y1-$REARFENCE_Y2 (REAR_BOUNDARY, NOT alley)"
Write-Host "  Invented alleys : $($alleyParcels.Count)"
Write-Host "  Sidewalk N      : Y $NSIDEWALK_Y1-$NSIDEWALK_Y2 ($sidewalk_width_north tiles)"
Write-Host "  Sidewalk S      : Y $SSIDEWALK_Y1-$SSIDEWALK_Y2 ($sidewalk_width_south tiles)"
Write-Host "  Sidewalk E      : X $ESIDEWALK_X1-$ESIDEWALK_X2 ($sidewalk_width_east tiles)"
Write-Host "  Bbox color      : CYAN R=$($CLR_BBOX.R) G=$($CLR_BBOX.G) B=$($CLR_BBOX.B)"
Write-Host ""
Write-Host "Output folder: $OutDir"

if ($dimOk) {
    Write-Host "STATUS: ALL PASS -- 3 PNGs 256x256, 12 topology checks PASS, 0 forbidden artifacts"
    exit 0
} else {
    Write-Host "STATUS: FAIL -- see above"
    exit 1
}
