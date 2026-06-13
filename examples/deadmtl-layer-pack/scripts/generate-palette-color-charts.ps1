# generate-palette-color-charts.ps1
# Reads worldgen-png-palette.json and writes chart sidecars:
#   worldgen-png-palette.chart.png    - visual swatches (System.Drawing)
#   worldgen-png-palette.swatches.txt - hex / type / key / proof status
#   worldgen-png-palette.layer-guide.txt - authoring guide
#
# Charts are documentation artifacts only. They are not compiler input.
# Proof status is locked by MAP history — update only after human visual confirmation.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\generate-palette-color-charts.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\generate-palette-color-charts.ps1 -PalettesDir <path>

param(
    [string]$PalettesDir = ""
)

Add-Type -AssemblyName System.Drawing

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

if ([string]::IsNullOrWhiteSpace($PalettesDir)) {
    $PalettesDir = Join-Path $repoRoot "examples\deadmtl-layer-pack\palettes"
}

$palettePath = Join-Path $PalettesDir "worldgen-png-palette.json"

if (-not (Test-Path $palettePath)) {
    Write-Error "Palette not found: $palettePath"
    exit 1
}

Write-Host "Reading palette: $palettePath"

$palette = Get-Content $palettePath -Raw | ConvertFrom-Json
$entries  = $palette.entries

# ---------------------------------------------------------------------------
# Proof status registry
# Locked by MAP history. Do not update without human visual confirmation.
# ---------------------------------------------------------------------------
$proofStatus = @{
    "#0000FF" = "VISUAL_CONFIRMED"   # water — MAP-20A
    "#D8C080" = "VISUAL_CONFIRMED"   # sand_bank — MAP-20A
    "#207020" = "VISUAL_CONFIRMED"   # birch_forest — MAP-20A
    "#FF6600" = "VISUAL_CONFIRMED"   # normal_road_WE_00 — MAP-20A
    "#CC3300" = "VISUAL_CONFIRMED"   # highway_NS_00 — MAP-13C/MAP-15C/MAP-16B
    "#00AA00" = "KNOWN_IN_CODE"
    "#55CC55" = "KNOWN_IN_CODE"
    "#145C14" = "KNOWN_IN_CODE"
    "#0B4418" = "KNOWN_IN_CODE"
    "#60A060" = "KNOWN_IN_CODE"
    "#4F8F4F" = "KNOWN_IN_CODE"
    "#3F7F50" = "KNOWN_IN_CODE"
}

# ---------------------------------------------------------------------------
# Chart PNG
# ---------------------------------------------------------------------------
$chartPath = Join-Path $PalettesDir "worldgen-png-palette.chart.png"

$swatchW   = 44
$swatchH   = 40
$rowH      = 52
$margin    = 12
$textX     = $margin + $swatchW + 8
$imgW      = 520
$imgH      = $margin + $entries.Count * $rowH + $margin

$bmp = New-Object System.Drawing.Bitmap $imgW, $imgH
$g   = [System.Drawing.Graphics]::FromImage($bmp)

$bgColor = [System.Drawing.Color]::FromArgb(255, 245, 245, 245)
$g.Clear($bgColor)

$fontMono  = New-Object System.Drawing.Font("Courier New", 8, [System.Drawing.FontStyle]::Regular)
$fontBold  = New-Object System.Drawing.Font("Courier New", 8, [System.Drawing.FontStyle]::Bold)
$blackBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Black)
$grayBrush  = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 100, 100, 100))
$greenBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,  30, 140,  30))
$borderPen  = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 180, 180, 180), 1)

for ($i = 0; $i -lt $entries.Count; $i++) {
    $entry  = $entries[$i]
    $hex    = $entry.color.ToUpper()
    $type   = $entry.type
    $key    = $entry.key
    $status = if ($proofStatus.ContainsKey($hex)) { $proofStatus[$hex] } else { "UNPROVEN_VISUAL" }

    $swatchColor = [System.Drawing.ColorTranslator]::FromHtml($hex)
    $swatchBrush = New-Object System.Drawing.SolidBrush($swatchColor)

    $y = $margin + $i * $rowH

    $g.FillRectangle($swatchBrush, $margin, ($y + 4), $swatchW, $swatchH)
    $g.DrawRectangle($borderPen,   $margin, ($y + 4), $swatchW, $swatchH)
    $swatchBrush.Dispose()

    $g.DrawString($hex, $fontBold, $blackBrush, $textX, ($y + 2))
    $g.DrawString("$type : $key", $fontMono, $blackBrush, $textX, ($y + 16))

    $statusBrush = if ($status -eq "VISUAL_CONFIRMED") { $greenBrush } else { $grayBrush }
    $g.DrawString($status, $fontMono, $statusBrush, $textX, ($y + 30))
}

$fontMono.Dispose(); $fontBold.Dispose()
$blackBrush.Dispose(); $grayBrush.Dispose(); $greenBrush.Dispose()
$borderPen.Dispose()
$g.Dispose()
$bmp.Save($chartPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Host "  OK  worldgen-png-palette.chart.png ($imgW x $imgH)"

# ---------------------------------------------------------------------------
# Swatches TXT
# ---------------------------------------------------------------------------
$swatchesPath = Join-Path $PalettesDir "worldgen-png-palette.swatches.txt"

$lines = @()
$lines += "# worldgen-png-palette swatches"
$lines += "# Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$lines += "# Proof chain: hex -> palette -> WorldGen key -> compiler -> Lua loads -> human visual"
$lines += "# Statuses:"
$lines += "#   VISUAL_CONFIRMED  - human confirmed in-game (documented MAP)"
$lines += "#   KNOWN_IN_CODE     - compiler accepts; no human visual proof yet"
$lines += "#   UNPROVEN_VISUAL   - no compiler or visual proof"
$lines += ""
$lines += ("{0,-10} {1,-8} {2,-24} {3}" -f "HEX", "TYPE", "KEY", "PROOF_STATUS")
$lines += ("{0,-10} {1,-8} {2,-24} {3}" -f "----------", "--------", "------------------------", "-------------------")

foreach ($entry in $entries) {
    $hex    = $entry.color.ToUpper()
    $type   = $entry.type
    $key    = $entry.key
    $status = if ($proofStatus.ContainsKey($hex)) { $proofStatus[$hex] } else { "UNPROVEN_VISUAL" }
    $lines += ("{0,-10} {1,-8} {2,-24} {3}" -f $hex, $type, $key, $status)
}

Set-Content -Path $swatchesPath -Value ($lines -join "`n") -Encoding UTF8
Write-Host "  OK  worldgen-png-palette.swatches.txt ($($entries.Count) entries)"

# ---------------------------------------------------------------------------
# Layer guide TXT
# ---------------------------------------------------------------------------
$guidePath = Join-Path $PalettesDir "worldgen-png-palette.layer-guide.txt"

$guide = @"
# worldgen-png-palette layer authoring guide
# Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

== Overview ==

One pixel in a layer PNG = one world tile in Project Zomboid Build 42.
Pixels are mapped to WorldGen keys via this palette file.
Transparent pixels (alpha < 128) are skipped — they produce no worldgen output.
Opaque pixels with an unknown color cause a compilation error.
Charts in this directory are documentation; they are not compiler input.

== Proof chain ==

A color is not proven just because it exists in the palette.
Full proof chain:

  hex color
    -> palette JSON (color must exist with valid type/key)
    -> WorldGen key (must be in WorldGenRegistry)
    -> compiler (must emit without errors)
    -> Lua loads in PZ (no Lua errors on map load)
    -> human sees patch in-game (visual confirmation)

Proof statuses:
  VISUAL_CONFIRMED  = all steps proven, human saw it in-game
  KNOWN_IN_CODE     = compiler accepts; no human visual proof yet
  UNPROVEN_VISUAL   = no compiler or visual proof

== Priority composition ==

Lower priority number = painted first (base layer).
Higher priority overrides lower where both have opaque pixels.
Transparent pixels are always skip — they never override.

== Usage ==

1. Paint pixels with hex colors from this palette.
2. Run compile-worldgen-project to extract WorldGen modules.
3. Run compile-worldgen to emit WorldGenOverride.lua.
4. Use the proof swatch pack (generate-worldgen-palette-proof-pack.ps1)
   to test new colors in-game before marking VISUAL_CONFIRMED.

== Current palette ==

"@

foreach ($entry in $entries) {
    $hex    = $entry.color.ToUpper()
    $type   = $entry.type
    $key    = $entry.key
    $status = if ($proofStatus.ContainsKey($hex)) { $proofStatus[$hex] } else { "UNPROVEN_VISUAL" }
    $guide += "  $hex  $type : $key  [$status]`n"
}

Set-Content -Path $guidePath -Value $guide -Encoding UTF8
Write-Host "  OK  worldgen-png-palette.layer-guide.txt"

Write-Host ""
Write-Host "Charts written to: $PalettesDir"
