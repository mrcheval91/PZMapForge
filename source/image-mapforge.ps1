#Requires -Version 5.1
<#+
.SYNOPSIS
    Converts a PNG/BMP blockout image into deterministic PZMapForge planning artifacts.

.DESCRIPTION
    ImageMapForge reads an image, maps pixels to semantic cell kinds using
    source/image-palette.json, and writes local-only planning outputs:
      .local/mapforge/parsed-cell.json
      .local/mapforge/parsed-cell-report.md
      .local/mapforge/parsed-cell-preview.png
      .local/mapforge/parsed-cell-tiles.png
      .local/mapforge/parsed-cell-basic.tmx

    The TMX uses generated colour tiles and is a TileZed-openable planning artifact.
    It is not a Project Zomboid load-tested map export.

    Nearest-colour drift: pixels that do not exactly match a palette entry are
    resolved to the closest palette entry by RGB squared distance. The drift table
    (source colour -> matched kind, distance) is written to the JSON and report
    so colour mismatch is visible without guessing.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ImagePath,

    [ValidateSet('Palette', 'Debug')]
    [string]$Mode = 'Palette',

    [switch]$Resize,

    [string]$OutputDir,

    [switch]$AllowExternalOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName 'System.IO.Compression'

$scriptDir        = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot         = Split-Path -Parent $scriptDir
$palettePath      = Join-Path $scriptDir 'image-palette.json'
$defaultOutputDir = Join-Path $repoRoot '.local\mapforge'
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = $defaultOutputDir
}

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)
    return [System.IO.Path]::GetFullPath($PathValue)
}

function Test-PathInside {
    param(
        [Parameter(Mandatory = $true)][string]$Child,
        [Parameter(Mandatory = $true)][string]$Parent
    )
    $childFull  = Resolve-FullPath $Child
    $parentFull = Resolve-FullPath $Parent
    if ($childFull.Equals($parentFull, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    if (-not $parentFull.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $parentFull = $parentFull + [System.IO.Path]::DirectorySeparatorChar
    }
    return $childFull.StartsWith($parentFull, [System.StringComparison]::OrdinalIgnoreCase)
}

function ConvertFrom-HexColor {
    param([Parameter(Mandatory = $true)][string]$Hex)
    if ($Hex -notmatch '^#[0-9A-Fa-f]{6}$') {
        throw "Invalid palette colour '$Hex'. Expected #RRGGBB."
    }
    $r = [Convert]::ToInt32($Hex.Substring(1, 2), 16)
    $g = [Convert]::ToInt32($Hex.Substring(3, 2), 16)
    $b = [Convert]::ToInt32($Hex.Substring(5, 2), 16)
    return [System.Drawing.Color]::FromArgb(255, $r, $g, $b)
}

function Get-ColorKey {
    param([Parameter(Mandatory = $true)][System.Drawing.Color]$Color)
    return ('#{0:X2}{1:X2}{2:X2}' -f $Color.R, $Color.G, $Color.B)
}

function Get-ColorDistanceSq {
    param(
        [Parameter(Mandatory = $true)][System.Drawing.Color]$A,
        [Parameter(Mandatory = $true)][System.Drawing.Color]$B
    )
    $dr = [int]$A.R - [int]$B.R
    $dg = [int]$A.G - [int]$B.G
    $db = [int]$A.B - [int]$B.B
    return (($dr * $dr) + ($dg * $dg) + ($db * $db))
}

function Get-NearestKind {
    param(
        [Parameter(Mandatory = $true)][System.Drawing.Color]$Color,
        [Parameter(Mandatory = $true)]$PaletteKinds
    )
    $best         = $null
    $bestDistance = [int]::MaxValue
    foreach ($entry in $PaletteKinds) {
        $distance = Get-ColorDistanceSq -A $Color -B $entry.Color
        if ($distance -lt $bestDistance) { $best = $entry; $bestDistance = $distance }
    }
    return $best
}

function Write-TmxFile {
    param(
        [Parameter(Mandatory = $true)][uint32[]]$Gids,
        [Parameter(Mandatory = $true)][int]$Width,
        [Parameter(Mandatory = $true)][int]$Height,
        [Parameter(Mandatory = $true)][int]$TileSize,
        [Parameter(Mandatory = $true)][int]$TilesetWidth,
        [Parameter(Mandatory = $true)][string]$TmxPath
    )
    $rawBytes = New-Object byte[] ($Gids.Length * 4)
    [System.Buffer]::BlockCopy($Gids, 0, $rawBytes, 0, $rawBytes.Length)
    $ms = [System.IO.MemoryStream]::new()
    $gz = [System.IO.Compression.GZipStream]::new($ms, [System.IO.Compression.CompressionMode]::Compress)
    $gz.Write($rawBytes, 0, $rawBytes.Length)
    $gz.Close()
    $compressed = $ms.ToArray()
    $ms.Dispose()
    $b64 = [Convert]::ToBase64String($compressed)
    $tmxContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<map version="1.0" orientation="orthogonal" width="$Width" height="$Height" tilewidth="$TileSize" tileheight="$TileSize">
 <tileset firstgid="1" name="pzmapforge_colour_planning" tilewidth="$TileSize" tileheight="$TileSize">
  <image source="parsed-cell-tiles.png" width="$TilesetWidth" height="$TileSize"/>
 </tileset>
 <layer name="Ground" width="$Width" height="$Height">
  <data encoding="base64" compression="gzip">
   $b64
  </data>
 </layer>
</map>
"@
    Set-Content -Path $TmxPath -Value $tmxContent -Encoding UTF8
}

function Write-TileStrip {
    param(
        [Parameter(Mandatory = $true)]$PaletteKinds,
        [Parameter(Mandatory = $true)][int]$TileSize,
        [Parameter(Mandatory = $true)][string]$TilesPath
    )
    $ordered    = @($PaletteKinds | Sort-Object Gid)
    $stripWidth = $ordered.Count * $TileSize
    $stripBmp   = [System.Drawing.Bitmap]::new($stripWidth, $TileSize)
    $g          = [System.Drawing.Graphics]::FromImage($stripBmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    for ($i = 0; $i -lt $ordered.Count; $i++) {
        $brush = [System.Drawing.SolidBrush]::new($ordered[$i].Color)
        $g.FillRectangle($brush, ($i * $TileSize), 0, $TileSize, $TileSize)
        $brush.Dispose()
        $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 0, 0, 0), 1)
        $g.DrawRectangle($pen, ($i * $TileSize), 0, ($TileSize - 1), ($TileSize - 1))
        $pen.Dispose()
    }
    $g.Dispose()
    $stripBmp.Save($TilesPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $stripBmp.Dispose()
    return $stripWidth
}

function Resize-BitmapNearest {
    param(
        [Parameter(Mandatory = $true)][System.Drawing.Bitmap]$Bitmap,
        [Parameter(Mandatory = $true)][int]$Width,
        [Parameter(Mandatory = $true)][int]$Height
    )
    $resized = [System.Drawing.Bitmap]::new($Width, $Height)
    $g = [System.Drawing.Graphics]::FromImage($resized)
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::None
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.DrawImage($Bitmap, 0, 0, $Width, $Height)
    $g.Dispose()
    return $resized
}

# ---------------------------------------------------------------------------
# Guards
# ---------------------------------------------------------------------------

$imageFull = Resolve-FullPath $ImagePath
if (-not (Test-Path -LiteralPath $imageFull)) {
    throw "Input image not found: $imageFull"
}

$extension = [System.IO.Path]::GetExtension($imageFull).ToLowerInvariant()
if ($extension -ne '.png' -and $extension -ne '.bmp') {
    throw "Unsupported image extension '$extension'. Use PNG or BMP."
}

if (-not (Test-Path -LiteralPath $palettePath)) {
    throw "Palette config not found: $palettePath"
}

$outputFull      = Resolve-FullPath $OutputDir
$defaultFull     = Resolve-FullPath $defaultOutputDir
$mediaMapsFull   = Resolve-FullPath (Join-Path $repoRoot 'media\maps')

if (Test-PathInside -Child $outputFull -Parent $mediaMapsFull) {
    throw "Refusing to write into media/maps: $outputFull"
}

if (-not $AllowExternalOutput) {
    if (-not (Test-PathInside -Child $outputFull -Parent $defaultFull)) {
        throw "Refusing to write outside .local/mapforge without -AllowExternalOutput: $outputFull"
    }
}

New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

# ---------------------------------------------------------------------------
# Load palette
# ---------------------------------------------------------------------------

$palette      = Get-Content -LiteralPath $palettePath -Raw | ConvertFrom-Json
$W            = [int]$palette.cell_width
$H            = [int]$palette.cell_height
$TileSize     = [int]$palette.tile_size

$paletteKinds = @()
foreach ($kind in $palette.kinds) {
    $paletteKinds += [PSCustomObject]@{
        Kind   = [string]$kind.kind
        Gid    = [uint32]$kind.gid
        Symbol = [string]$kind.symbol
        Hex    = [string]$kind.color
        Color  = ConvertFrom-HexColor -Hex ([string]$kind.color)
    }
}

$requiredKinds = @('grass','road','sidewalk','row_house','depanneur',
                   'garage','industrial_yard','landmark','spawn')
foreach ($req in $requiredKinds) {
    if (-not @($paletteKinds | Where-Object { $_.Kind -eq $req })) {
        throw "Palette missing required kind '$req'."
    }
}

$exactByHex = @{}
foreach ($entry in $paletteKinds) {
    $exactByHex[$entry.Hex.ToUpperInvariant()] = $entry
}

# ---------------------------------------------------------------------------
# Load and optionally resize image
# ---------------------------------------------------------------------------

$sourceBitmap = [System.Drawing.Bitmap]::new($imageFull)
$bitmap       = $sourceBitmap
$resizedFrom  = $null
try {
    if ($sourceBitmap.Width -ne $W -or $sourceBitmap.Height -ne $H) {
        if (-not $Resize) {
            throw "Input image is $($sourceBitmap.Width)x$($sourceBitmap.Height). Expected ${W}x${H}. Re-run with -Resize to scale with nearest-neighbour sampling."
        }
        $resizedFrom = "$($sourceBitmap.Width)x$($sourceBitmap.Height)"
        $bitmap      = Resize-BitmapNearest -Bitmap $sourceBitmap -Width $W -Height $H
    }

    # -----------------------------------------------------------------------
    # Pixel scan
    # -----------------------------------------------------------------------

    $gids        = New-Object uint32[] ($W * $H)
    $rows        = New-Object string[] $H
    $counts      = @{}
    $colorCounts = @{}
    $unmapped    = @{}

    # nearestDrift: hex key -> @{source_hex, entry, nearest_kind, nearest_hex, dist, count}
    # Caches nearest-colour lookups so each unique unmapped colour is resolved once.
    $nearestDrift = @{}

    foreach ($entry in $paletteKinds) { $counts[$entry.Kind] = 0 }

    for ($y = 0; $y -lt $H; $y++) {
        $chars = New-Object char[] $W
        for ($x = 0; $x -lt $W; $x++) {
            $color    = $bitmap.GetPixel($x, $y)
            $key      = Get-ColorKey -Color $color
            $upperKey = $key.ToUpperInvariant()

            if (-not $colorCounts.ContainsKey($key)) { $colorCounts[$key] = 0 }
            $colorCounts[$key]++

            $entry = $null
            if ($exactByHex.ContainsKey($upperKey)) {
                $entry = $exactByHex[$upperKey]
            }
            else {
                if (-not $unmapped.ContainsKey($key)) { $unmapped[$key] = 0 }
                $unmapped[$key]++

                if ($nearestDrift.ContainsKey($key)) {
                    $entry = $nearestDrift[$key].entry
                    $nearestDrift[$key].count++
                }
                else {
                    $nearest  = Get-NearestKind -Color $color -PaletteKinds $paletteKinds
                    $sqDist   = Get-ColorDistanceSq -A $color -B $nearest.Color
                    $rgbDist  = [Math]::Round([Math]::Sqrt([double]$sqDist), 2)
                    $nearestDrift[$key] = @{
                        source_hex   = $key
                        entry        = $nearest
                        nearest_kind = $nearest.Kind
                        nearest_hex  = $nearest.Hex
                        dist         = $rgbDist
                        count        = 1
                    }
                    $entry = $nearest
                }
            }

            $index       = ($y * $W) + $x
            $gids[$index] = [uint32]$entry.Gid
            $chars[$x]   = [char]$entry.Symbol
            $counts[$entry.Kind]++
        }
        $rows[$y] = -join $chars
    }

    # -----------------------------------------------------------------------
    # Build drift records (sorted by count desc)
    # -----------------------------------------------------------------------

    $driftRecords = @()
    $nearestDrift.Values |
        Sort-Object -Property @{E='count';D=$true}, @{E='source_hex';D=$false} |
        ForEach-Object {
            $driftRecords += [ordered]@{
                source_hex   = $_.source_hex
                count        = [int]$_.count
                nearest_kind = $_.nearest_kind
                nearest_hex  = $_.nearest_hex
                dist         = $_.dist
            }
        }

    # -----------------------------------------------------------------------
    # Write artifacts
    # -----------------------------------------------------------------------

    $previewScale = 3
    $previewPath  = Join-Path $outputFull 'parsed-cell-preview.png'
    $preview = [System.Drawing.Bitmap]::new(($W * $previewScale), ($H * $previewScale))
    $pg = [System.Drawing.Graphics]::FromImage($preview)
    $pg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    for ($y = 0; $y -lt $H; $y++) {
        for ($x = 0; $x -lt $W; $x++) {
            $gid   = $gids[($y * $W) + $x]
            $entry = @($paletteKinds | Where-Object { $_.Gid -eq $gid })[0]
            $brush = [System.Drawing.SolidBrush]::new($entry.Color)
            $pg.FillRectangle($brush, ($x * $previewScale), ($y * $previewScale), $previewScale, $previewScale)
            $brush.Dispose()
        }
    }
    $pg.Dispose()
    $preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $preview.Dispose()

    $tilesPath   = Join-Path $outputFull 'parsed-cell-tiles.png'
    $tilesetWidth = Write-TileStrip -PaletteKinds $paletteKinds -TileSize $TileSize -TilesPath $tilesPath

    $tmxPath = Join-Path $outputFull 'parsed-cell-basic.tmx'
    Write-TmxFile -Gids $gids -Width $W -Height $H -TileSize $TileSize `
        -TilesetWidth $tilesetWidth -TmxPath $tmxPath

    # -----------------------------------------------------------------------
    # JSON artifact
    # -----------------------------------------------------------------------

    $legend = @{}
    foreach ($entry in ($paletteKinds | Sort-Object Gid)) {
        $legend[$entry.Symbol] = [PSCustomObject]@{
            kind   = $entry.Kind
            gid    = [int]$entry.Gid
            color  = $entry.Hex
        }
    }

    $countObject = [ordered]@{}
    foreach ($entry in ($paletteKinds | Sort-Object Gid)) {
        $countObject[$entry.Kind] = [int]$counts[$entry.Kind]
    }

    $json = [ordered]@{
        schema         = 'pzmapforge.parsed-cell.v0.1'
        generator      = 'ImageMapForge'
        claim_boundary = 'Planning artifact only. Not a Project Zomboid load-tested map export.'
        source_image   = $imageFull
        palette        = (Resolve-FullPath $palettePath)
        width          = $W
        height         = $H
        resized_from   = $resizedFrom
        matching       = [ordered]@{
            unique_colours   = [int]$colorCounts.Count
            unmapped_colours = [int]$unmapped.Count
        }
        legend         = $legend
        counts         = $countObject
        nearest_drift  = $driftRecords
        rows           = $rows
    }

    $jsonPath = Join-Path $outputFull 'parsed-cell.json'
    ($json | ConvertTo-Json -Depth 10) | Set-Content -Path $jsonPath -Encoding UTF8

    # -----------------------------------------------------------------------
    # Markdown report
    # -----------------------------------------------------------------------

    $topKinds = foreach ($entry in ($paletteKinds | Sort-Object Gid)) {
        '| ' + $entry.Kind + ' | ' + $entry.Symbol + ' | ' + $entry.Gid + ' | ' + $entry.Hex + ' | ' + $counts[$entry.Kind] + ' |'
    }

    $unmappedLines = @()
    if ($unmapped.Count -eq 0) {
        $unmappedLines += 'None. All input colours matched the palette exactly.'
    }
    else {
        foreach ($k in ($unmapped.Keys | Sort-Object)) {
            $unmappedLines += "- $k : $($unmapped[$k]) pixels"
        }
    }

    $driftTop   = $driftRecords | Select-Object -First 20
    $driftLines = foreach ($d in $driftTop) {
        "| $($d.source_hex) | $($d.count) | $($d.nearest_kind) | $($d.nearest_hex) | $($d.dist) |"
    }
    if (@($driftLines).Count -eq 0) { $driftLines = @('| (none) | — | — | — | — |') }

    $reportPath = Join-Path $outputFull 'parsed-cell-report.md'
    $report = @"
# ImageMapForge Parsed Cell Report

Generated: deterministic local run (timestamp omitted for reproducibility)

## Claim boundary

Planning artifact only. Not a Project Zomboid load-tested map export.
No lotpack, lotheader, or bin files generated. No PZ game assets copied.

## Input

| Field | Value |
|---|---|
| Image | ``$imageFull`` |
| Palette | ``$(Resolve-FullPath $palettePath)`` |
| Mode | ``$Mode`` |
| Dimensions | ${W}x${H} |
| Resized from | $resizedFrom |

## Matching

| Field | Value |
|---|---:|
| Unique source colours | $($colorCounts.Count) |
| Unmapped exact colours | $($unmapped.Count) |

## Nearest-colour drift (top 20 by pixel count)

Pixels that did not match a palette entry exactly, and the palette entry they
were mapped to. Zero rows means all pixels matched exactly.
High distance values flag palette mismatches.

| Source hex | Count | Nearest kind | Palette hex | RGB dist |
|---|---:|---|---|---:|
$($driftLines -join "`n")

## Semantic counts

| Kind | Symbol | GID | Colour | Pixel count |
|---|---:|---:|---|---:|
$($topKinds -join "`n")

## Outputs

| File | Purpose |
|---|---|
| ``parsed-cell.json`` | Semantic grid, counts, drift records |
| ``parsed-cell-report.md`` | This report |
| ``parsed-cell-preview.png`` | Visual semantic preview |
| ``parsed-cell-tiles.png`` | Generated colour-strip tileset for the TMX |
| ``parsed-cell-basic.tmx`` | TileZed-openable planning TMX |
"@
    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    # -----------------------------------------------------------------------
    # Debug mode extra output
    # -----------------------------------------------------------------------

    if ($Mode -eq 'Debug') {
        Write-Output ''
        Write-Output '--- DEBUG: colour frequencies ---'
        foreach ($k in ($colorCounts.Keys | Sort-Object)) {
            Write-Output ("  {0}  {1}" -f $k, $colorCounts[$k])
        }
        Write-Output ''
        Write-Output '--- DEBUG: unmapped exact colours ---'
        foreach ($line in $unmappedLines) { Write-Output "  $line" }
        Write-Output ''
        Write-Output '--- DEBUG: nearest-colour drift ---'
        foreach ($d in ($driftRecords | Select-Object -First 20)) {
            Write-Output ("  {0} -> {1} ({2}) dist={3}" -f `
                $d.source_hex, $d.nearest_kind, $d.nearest_hex, $d.dist)
        }
    }

    Write-Output "JSON written: $jsonPath"
    Write-Output "Report written: $reportPath"
    Write-Output "Preview written: $previewPath"
    Write-Output "Tiles written: $tilesPath"
    Write-Output "TMX written: $tmxPath"
    Write-Output 'Done.'
}
finally {
    if ($bitmap -ne $sourceBitmap -and $null -ne $bitmap) { $bitmap.Dispose() }
    if ($null -ne $sourceBitmap) { $sourceBitmap.Dispose() }
}
