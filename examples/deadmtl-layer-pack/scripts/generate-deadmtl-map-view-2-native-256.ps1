#Requires -Version 5.1
<#
.SYNOPSIS
MAP-VIEW-2-NATIVE-256: Generate native 256x256 DeadMTL map_00 preview images.

Produces 4 PNG previews (all exactly 256x256) + optional HTML viewer + README
under .local\deadmtl-authoring\MAP_VIEW_2_NATIVE_256\

One PNG pixel = one map tile/cell.

This is NOT a playable export. No runtime files are written.
No .lotpack, .lotheader, .lua, or .bin files are produced.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

# --- Paths ---
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$RepoRoot = (Resolve-Path (Join-Path $RepoRoot "..")).Path
$RepoRoot = (Resolve-Path (Join-Path $RepoRoot "..")).Path

$RawPng  = "E:\Omni\Zomboid\assets\raw\map_00.png"
$CsvPath = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\map_00.sandbox_writer_tile_materialized_cells.csv"
$OutDir  = Join-Path $RepoRoot ".local\deadmtl-authoring\MAP_VIEW_2_NATIVE_256"
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# --- Component known values ---
$CompId   = "map_00_component_0001"
$BboxMinX = 124; $BboxMaxX = 212   # inclusive tile coords
$BboxMinY = 10;  $BboxMaxY = 69    # inclusive tile coords
# bbox width/height for DrawRectangle (GDI+ exclusive right/bottom)
$BboxW = $BboxMaxX - $BboxMinX     # 88  (draws pixel cols 124..212)
$BboxH = $BboxMaxY - $BboxMinY     # 59  (draws pixel rows 10..69)

# --- Material colors (readable palette — NOT the pipeline QA palette) ---
$CLR_BG       = [System.Drawing.Color]::FromArgb(18, 18, 24)
$CLR_WALL     = [System.Drawing.Color]::FromArgb(220, 100, 40)    # orange
$CLR_FLOOR    = [System.Drawing.Color]::FromArgb(240, 210, 100)   # yellow
$CLR_ACCESS   = [System.Drawing.Color]::FromArgb(30, 200, 160)    # teal
$CLR_LOT      = [System.Drawing.Color]::FromArgb(100, 210, 60)    # green
$CLR_RESIDUAL = [System.Drawing.Color]::FromArgb(130, 80, 40)     # brown
$CLR_BBOX     = [System.Drawing.Color]::FromArgb(255, 60, 160)    # magenta

function MaterialColor($kind) {
    switch ($kind.ToUpper()) {
        "WALL"     { return $CLR_WALL }
        "FLOOR"    { return $CLR_FLOOR }
        "ACCESS"   { return $CLR_ACCESS }
        "LOT"      { return $CLR_LOT }
        default    { return $CLR_RESIDUAL }
    }
}

# ============================================================
# Load inputs
# ============================================================
Write-Host "Loading raw source PNG: $RawPng"
if (-not (Test-Path $RawPng)) { Write-Error "Raw PNG not found: $RawPng"; exit 1 }
$rawBmp = New-Object System.Drawing.Bitmap($RawPng)
$rawW = $rawBmp.Width; $rawH = $rawBmp.Height
Write-Host "  Source: ${rawW}x${rawH}"

Write-Host "Loading cells CSV: $CsvPath"
if (-not (Test-Path $CsvPath)) { Write-Error "Cells CSV not found: $CsvPath"; exit 1 }
$cells = Import-Csv $CsvPath
Write-Host "  Loaded $($cells.Count) cells"

# ============================================================
# IMAGE 1: Raw source — exact copy, 256x256
# ============================================================
Write-Host "Generating Image 1: raw source native 256..."

$img1 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g1   = [System.Drawing.Graphics]::FromImage($img1)
$g1.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g1.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g1.DrawImage($rawBmp, 0, 0, 256, 256)
$g1.Dispose()

$out1 = Join-Path $OutDir "map_00_raw_source_native_256.png"
$img1.Save($out1, [System.Drawing.Imaging.ImageFormat]::Png)
$img1.Dispose()
Write-Host "  Saved: $out1  (256x256)"

# ============================================================
# IMAGE 2: Component context — raw + bbox outline + cell pixels
# ============================================================
Write-Host "Generating Image 2: component context native 256..."

$img2 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g2   = [System.Drawing.Graphics]::FromImage($img2)
$g2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g2.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g2.DrawImage($rawBmp, 0, 0, 256, 256)
$g2.Dispose()

# Paint materialized cells at 1px/tile over raw background
foreach ($c in $cells) {
    $cx = [int]$c.x; $cy = [int]$c.y
    $img2.SetPixel($cx, $cy, (MaterialColor $c.layer_kind))
}

# Bbox outline at exact native tile coords — use SetPixel for 1px precision
for ($px = $BboxMinX; $px -le $BboxMaxX; $px++) {
    $img2.SetPixel($px, $BboxMinY, $CLR_BBOX)
    $img2.SetPixel($px, $BboxMaxY, $CLR_BBOX)
}
for ($py = $BboxMinY; $py -le $BboxMaxY; $py++) {
    $img2.SetPixel($BboxMinX, $py, $CLR_BBOX)
    $img2.SetPixel($BboxMaxX, $py, $CLR_BBOX)
}

$out2 = Join-Path $OutDir "map_00_component_context_native_256.png"
$img2.Save($out2, [System.Drawing.Imaging.ImageFormat]::Png)
$img2.Dispose()
Write-Host "  Saved: $out2  (256x256)"

# ============================================================
# IMAGE 3: Materialized overlay — dark background + cell pixels
# ============================================================
Write-Host "Generating Image 3: materialized overlay native 256..."

$img3 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g3   = [System.Drawing.Graphics]::FromImage($img3)
$b3   = New-Object System.Drawing.SolidBrush($CLR_BG)
$g3.FillRectangle($b3, 0, 0, 256, 256)
$b3.Dispose()
$g3.Dispose()

# Paint each cell: 1 pixel = 1 tile
foreach ($c in $cells) {
    $cx = [int]$c.x; $cy = [int]$c.y
    $img3.SetPixel($cx, $cy, (MaterialColor $c.layer_kind))
}

# Thin bbox outline
for ($px = $BboxMinX; $px -le $BboxMaxX; $px++) {
    $img3.SetPixel($px, $BboxMinY, $CLR_BBOX)
    $img3.SetPixel($px, $BboxMaxY, $CLR_BBOX)
}
for ($py = $BboxMinY; $py -le $BboxMaxY; $py++) {
    $img3.SetPixel($BboxMinX, $py, $CLR_BBOX)
    $img3.SetPixel($BboxMaxX, $py, $CLR_BBOX)
}

$out3 = Join-Path $OutDir "map_00_materialized_overlay_native_256.png"
$img3.Save($out3, [System.Drawing.Imaging.ImageFormat]::Png)
$img3.Dispose()
Write-Host "  Saved: $out3  (256x256)"

# ============================================================
# IMAGE 4: Debug grid — raw + grid lines every 8px + bbox
# ============================================================
Write-Host "Generating Image 4: debug grid native 256..."

$img4 = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g4   = [System.Drawing.Graphics]::FromImage($img4)
$g4.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g4.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g4.DrawImage($rawBmp, 0, 0, 256, 256)
$g4.Dispose()

# Grid every 8 tiles (8px at native scale) — drawn via SetPixel to stay in 256x256 contract
$CLR_GRID     = [System.Drawing.Color]::FromArgb(90, 255, 255, 255)
$CLR_GRID_MAJ = [System.Drawing.Color]::FromArgb(160, 255, 255, 255)
$GRID_MINOR   = 8    # every 8 tiles
$GRID_MAJOR   = 32   # every 32 tiles

for ($gx = 0; $gx -lt 256; $gx++) {
    for ($gy = 0; $gy -lt 256; $gy++) {
        if ($gx % $GRID_MAJOR -eq 0 -or $gy % $GRID_MAJOR -eq 0) {
            $img4.SetPixel($gx, $gy, $CLR_GRID_MAJ)
        } elseif ($gx % $GRID_MINOR -eq 0 -or $gy % $GRID_MINOR -eq 0) {
            $img4.SetPixel($gx, $gy, $CLR_GRID)
        }
    }
}

# Bbox outline
for ($px = $BboxMinX; $px -le $BboxMaxX; $px++) {
    $img4.SetPixel($px, $BboxMinY, $CLR_BBOX)
    $img4.SetPixel($px, $BboxMaxY, $CLR_BBOX)
}
for ($py = $BboxMinY; $py -le $BboxMaxY; $py++) {
    $img4.SetPixel($BboxMinX, $py, $CLR_BBOX)
    $img4.SetPixel($BboxMaxX, $py, $CLR_BBOX)
}

$out4 = Join-Path $OutDir "map_00_debug_grid_native_256.png"
$img4.Save($out4, [System.Drawing.Imaging.ImageFormat]::Png)
$img4.Dispose()
Write-Host "  Saved: $out4  (256x256)"

# ============================================================
# IMAGE 5 (optional): HTML pixel-art viewer
# ============================================================
Write-Host "Generating optional HTML viewer..."

$htmlContent = @"
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>DeadMTL map_00 — Native 256x256 Preview</title>
<style>
  body { background: #111; color: #ddd; font-family: monospace; padding: 16px; }
  h1   { font-size: 1.1em; color: #f0d264; }
  h2   { font-size: 0.9em; color: #aaa; margin-top: 24px; }
  .row { display: flex; flex-wrap: wrap; gap: 24px; margin-top: 12px; }
  .card { background: #1a1a1a; border: 1px solid #333; padding: 8px; }
  .card img {
    display: block;
    width: 512px;
    height: 512px;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
  }
  .card .label { font-size: 0.75em; color: #888; margin-top: 6px; }
  .legend { font-size: 0.8em; margin-top: 16px; }
  .legend span { display: inline-block; width: 12px; height: 12px; margin-right: 4px; vertical-align: middle; }
  .warn { color: #f08040; font-size: 0.85em; margin-top: 16px; }
</style>
</head>
<body>
<h1>DeadMTL map_00 — Native 256x256 Preview Package</h1>
<p class="warn">
  NOT a playable Project Zomboid export.<br>
  NOT .lotpack / .lotheader / .lua / .bin.<br>
  All source PNGs are exactly 256x256. CSS zoom only.
</p>
<div class="legend">
  <b>Material palette:</b>
  <span style="background:#dc6428;"></span>WALL (850) &nbsp;
  <span style="background:#f0d264;"></span>FLOOR (2444) &nbsp;
  <span style="background:#1ec8a0;"></span>ACCESS (148) &nbsp;
  <span style="background:#64d23c;"></span>LOT (1898) &nbsp;
  <span style="background:#824828;"></span>RESIDUAL (0) &nbsp;
  <span style="background:#ff3ca0;"></span>BBOX
</div>
<div class="row">
  <div class="card">
    <img src="map_00_raw_source_native_256.png" alt="raw source">
    <div class="label">map_00_raw_source_native_256.png — 256x256 source chunk</div>
  </div>
  <div class="card">
    <img src="map_00_component_context_native_256.png" alt="component context">
    <div class="label">map_00_component_context_native_256.png — raw + materialized cells + bbox</div>
  </div>
  <div class="card">
    <img src="map_00_materialized_overlay_native_256.png" alt="materialized overlay">
    <div class="label">map_00_materialized_overlay_native_256.png — dark bg + 1px/cell by material</div>
  </div>
  <div class="card">
    <img src="map_00_debug_grid_native_256.png" alt="debug grid">
    <div class="label">map_00_debug_grid_native_256.png — raw + grid (8 tile / 32 tile) + bbox</div>
  </div>
</div>
<h2>Component: map_00_component_0001 | Bbox X:124-212 Y:10-69 (89x60 tiles) | Total cells: 5340</h2>
</body>
</html>
"@

$outHtml = Join-Path $OutDir "map_00_native_viewer.html"
[System.IO.File]::WriteAllText($outHtml, $htmlContent, [System.Text.Encoding]::UTF8)
Write-Host "  Saved: $outHtml"

# ============================================================
# Cleanup
# ============================================================
$rawBmp.Dispose()

# ============================================================
# Dimension verification
# ============================================================
Write-Host ""
Write-Host "=== Dimension verification ==="
$pngs = @($out1, $out2, $out3, $out4)
$allOk = $true
foreach ($p in $pngs) {
    $b = New-Object System.Drawing.Bitmap($p)
    $w = $b.Width; $h = $b.Height
    $b.Dispose()
    $ok = if ($w -eq 256 -and $h -eq 256) { "OK" } else { "FAIL"; $allOk = $false }
    Write-Host "  [$ok] $(Split-Path $p -Leaf)  ${w}x${h}"
}

# ============================================================
# Forbidden artifact scan
# ============================================================
Write-Host ""
Write-Host "=== Forbidden artifact scan ==="
$Pat1 = ("*." + "lotpack")
$Pat2 = ("*." + "lotheader")
$Pat3 = ("*." + "lua")
$Pat4 = ("*." + "bin")
$forbidden = @()
foreach ($pat in @($Pat1, $Pat2, $Pat3, $Pat4)) {
    $found = Get-ChildItem -Path $OutDir -Filter $pat -Recurse -ErrorAction SilentlyContinue
    if ($found) { $forbidden += $found }
}
if (Test-Path (Join-Path $OutDir "steamapps")) {
    Write-Host "FAIL: FORBIDDEN: steamapps directory found"; $allOk = $false
}
$mapsDir = Join-Path (Join-Path $OutDir "media") "maps"
if (Test-Path $mapsDir) {
    Write-Host "FAIL: FORBIDDEN: media/maps directory found"; $allOk = $false
}
if ($forbidden.Count -gt 0) {
    Write-Host "FAIL: FORBIDDEN artifacts found: $($forbidden.Count)"; $allOk = $false
} else {
    Write-Host "  POST_MAP_VIEW_2_NATIVE_256_FORBIDDEN_SCAN PASS (0 forbidden artifacts)"
}

# ============================================================
# Final report
# ============================================================
Write-Host ""
Write-Host "=== MAP_VIEW_2_NATIVE_256 output ==="
Get-ChildItem $OutDir | Select-Object Name, @{N='SizeKB';E={[math]::Round($_.Length/1KB,1)}}
Write-Host ""
Write-Host "Output folder: $OutDir"
if ($allOk) {
    Write-Host "STATUS: ALL PASS -- 4 PNGs 256x256, 0 forbidden artifacts"
    exit 0
} else {
    Write-Host "STATUS: FAIL -- see above"
    exit 1
}
