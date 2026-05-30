#Requires -Version 5.1
<#+
.SYNOPSIS
    Generates a deterministic 300x300 palette image for ImageMapForge local validation.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$outputDir = Join-Path $repoRoot '.local\mapforge'
$outputPath = Join-Path $outputDir 'sample-input.png'
$palettePath = Join-Path $repoRoot 'source\image-palette.json'

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$palette = Get-Content -LiteralPath $palettePath -Raw | ConvertFrom-Json
$colors = @{}
foreach ($kind in $palette.kinds) {
    $hex = [string]$kind.color
    $r = [Convert]::ToInt32($hex.Substring(1, 2), 16)
    $g = [Convert]::ToInt32($hex.Substring(3, 2), 16)
    $b = [Convert]::ToInt32($hex.Substring(5, 2), 16)
    $colors[[string]$kind.kind] = [System.Drawing.Color]::FromArgb(255, $r, $g, $b)
}

function Fill-Rect {
    param(
        [Parameter(Mandatory = $true)][System.Drawing.Graphics]$Graphics,
        [Parameter(Mandatory = $true)]$ColorMap,
        [Parameter(Mandatory = $true)][string]$Kind,
        [Parameter(Mandatory = $true)][int]$X,
        [Parameter(Mandatory = $true)][int]$Y,
        [Parameter(Mandatory = $true)][int]$W,
        [Parameter(Mandatory = $true)][int]$H
    )
    $brush = [System.Drawing.SolidBrush]::new($ColorMap[$Kind])
    $Graphics.FillRectangle($brush, $X, $Y, $W, $H)
    $brush.Dispose()
}

$bmp = [System.Drawing.Bitmap]::new(300, 300)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'grass' -X 0 -Y 0 -W 300 -H 300

# Roads and sidewalks.
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'sidewalk' -X 0 -Y 136 -W 300 -H 4
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'road' -X 0 -Y 140 -W 300 -H 14
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'sidewalk' -X 0 -Y 154 -W 300 -H 4
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'sidewalk' -X 88 -Y 0 -W 2 -H 140
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'road' -X 90 -Y 0 -W 8 -H 140
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'sidewalk' -X 98 -Y 0 -W 2 -H 140
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'road' -X 190 -Y 30 -W 5 -H 110

# Buildings and zones.
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'row_house' -X 10 -Y 40 -W 16 -H 24
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'row_house' -X 28 -Y 40 -W 16 -H 24
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'row_house' -X 46 -Y 40 -W 16 -H 24
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'row_house' -X 64 -Y 40 -W 16 -H 24
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'depanneur' -X 82 -Y 96 -W 20 -H 16
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'garage' -X 110 -Y 60 -W 40 -H 30
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'industrial_yard' -X 165 -Y 20 -W 80 -H 100

# Markers.
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'spawn' -X 84 -Y 128 -W 5 -H 5
Fill-Rect -Graphics $g -ColorMap $colors -Kind 'landmark' -X 109 -Y 89 -W 3 -H 3

$g.Dispose()
$bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Output "Sample image written: $outputPath"
