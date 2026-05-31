#Requires -Version 5.1
<#
.SYNOPSIS
    Generates a deterministic 300x300 sample input image for local validation.
    Reads colours from source/image-palette.json (rgb array format).
    Output: .local/mapforge/sample-input.png
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$outputDir   = Join-Path $repoRoot '.local\mapforge'
$outputPath  = Join-Path $outputDir 'sample-input.png'
$palettePath = Join-Path $repoRoot 'source\image-palette.json'

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$palette = Get-Content -LiteralPath $palettePath -Raw | ConvertFrom-Json
$colors  = @{}
foreach ($kind in $palette.kinds) {
    $rgb = $kind.rgb
    $colors[[string]$kind.kind] = [System.Drawing.Color]::FromArgb(255, [int]$rgb[0], [int]$rgb[1], [int]$rgb[2])
}

function Fill-Rect {
    param(
        [System.Drawing.Graphics]$Graphics,
        [string]$Kind,
        [int]$X, [int]$Y, [int]$W, [int]$H
    )
    $brush = [System.Drawing.SolidBrush]::new($colors[$Kind])
    $Graphics.FillRectangle($brush, $X, $Y, $W, $H)
    $brush.Dispose()
}

$bmp = [System.Drawing.Bitmap]::new(300, 300)
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
Fill-Rect $g 'grass' 0 0 300 300

# Roads and sidewalks
Fill-Rect $g 'sidewalk'  0 136 300   4
Fill-Rect $g 'road'      0 140 300  14
Fill-Rect $g 'sidewalk'  0 154 300   4
Fill-Rect $g 'sidewalk' 88   0   2 140
Fill-Rect $g 'road'     90   0   8 140
Fill-Rect $g 'sidewalk' 98   0   2 140
Fill-Rect $g 'road'    190  30   5 110

# Buildings and zones
Fill-Rect $g 'row_house'       10  40  16  24
Fill-Rect $g 'row_house'       28  40  16  24
Fill-Rect $g 'row_house'       46  40  16  24
Fill-Rect $g 'row_house'       64  40  16  24
Fill-Rect $g 'depanneur'       82  96  20  16
Fill-Rect $g 'garage'         110  60  40  30
Fill-Rect $g 'industrial_yard' 165  20  80 100

# Markers
Fill-Rect $g 'spawn'    84 128   5   5
Fill-Rect $g 'landmark' 109  89  3   3

$g.Dispose()
$bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Output "Sample image written: $outputPath"
