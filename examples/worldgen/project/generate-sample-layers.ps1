# generate-sample-layers.ps1
# Generates sample layer PNGs for worldgen_project_sample.json.
# Run once before using compile-worldgen-project against the sample.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\generate-sample-layers.ps1

Add-Type -AssemblyName System.Drawing

$dir = Join-Path $PSScriptRoot "layers"
New-Item -ItemType Directory -Force -Path $dir | Out-Null

function Save-Png([string]$path, [int]$width, [int]$height, [System.Drawing.Color]$fill) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear($fill)
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Created: $path"
}

$w = 190
$h = 140

$transparent = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
$water       = [System.Drawing.Color]::FromArgb(255,   0,   0, 255)  # #0000FF
$sandBank    = [System.Drawing.Color]::FromArgb(255, 216, 192, 128)  # #D8C080
$road        = [System.Drawing.Color]::FromArgb(255, 255,   0,   0)  # #FF0000

# Layer 1: water fills the canvas
Save-Png (Join-Path $dir "water.png") $w $h $water

# Layer 2: sand strip along the top edge (y=0..9)
$sandBmp   = New-Object System.Drawing.Bitmap $w, $h
$sandG     = [System.Drawing.Graphics]::FromImage($sandBmp)
$sandBrush = New-Object System.Drawing.SolidBrush($sandBank)
$sandG.Clear($transparent)
$sandG.FillRectangle($sandBrush, 0, 0, $w, 10)
$sandBrush.Dispose()
$sandG.Dispose()
$sandBmp.Save((Join-Path $dir "sand.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$sandBmp.Dispose()
Write-Host "Created: $(Join-Path $dir 'sand.png')"

# Layer 3: road strip across y=70..73 (horizontal road)
$roadBmp   = New-Object System.Drawing.Bitmap $w, $h
$roadG     = [System.Drawing.Graphics]::FromImage($roadBmp)
$roadBrush = New-Object System.Drawing.SolidBrush($road)
$roadG.Clear($transparent)
$roadG.FillRectangle($roadBrush, 0, 70, $w, 4)
$roadBrush.Dispose()
$roadG.Dispose()
$roadBmp.Save((Join-Path $dir "roads.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$roadBmp.Dispose()
Write-Host "Created: $(Join-Path $dir 'roads.png')"

Write-Host ""
Write-Host "Done. Run the compiler with:"
Write-Host "  dotnet run --project src\PZMapForge.Cli --configuration Release --no-build -- ``"
Write-Host "    compile-worldgen-project ``"
Write-Host "    --input examples\worldgen\project\worldgen_project_sample.json ``"
Write-Host "    --output .local\worldgen\map16a\worldgen_layers.json"
