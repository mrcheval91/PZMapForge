#Requires -Version 5.1
<#
.SYNOPSIS
    Verifies that parsed-cell.json's palette_sha256 field matches the SHA-256
    of the actual source/image-palette.json file. Closes gap 3.

    If parsed-cell.json is missing, generates it via new-test-image.ps1 and
    image-mapforge.ps1 directly (not validate.ps1) to avoid a recursion loop.
    Exits 0 if all checks pass, exits 1 if any fail.
    Does not commit .local/. Does not touch media/maps.
    Does not claim playable Project Zomboid export.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$outputDir      = Join-Path $repoRoot '.local\mapforge'
$parsedCellPath = Join-Path $outputDir 'parsed-cell.json'
$palettePath    = Join-Path $repoRoot 'source\image-palette.json'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# ---------------------------------------------------------------------------
# Ensure parsed-cell.json exists (direct generation, not validate.ps1)
# ---------------------------------------------------------------------------

if (-not (Test-Path $parsedCellPath -PathType Leaf)) {
    Write-Output "parsed-cell.json not found. Generating via image-mapforge.ps1..."
    $sampleImg = Join-Path $outputDir 'sample-input.png'
    if (-not (Test-Path $sampleImg -PathType Leaf)) {
        & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\new-test-image.ps1')
        if ($LASTEXITCODE -ne 0) { Write-Error "new-test-image.ps1 failed."; exit 1 }
    }
    & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'source\image-mapforge.ps1') `
        -ImagePath $sampleImg
    if ($LASTEXITCODE -ne 0) { Write-Error "image-mapforge.ps1 failed."; exit 1 }
}

Write-Output "Palette SHA-256 verification"
Write-Output ""

# ---------------------------------------------------------------------------
# File existence
# ---------------------------------------------------------------------------

Write-Output "--- Files ---"
Assert-True (Test-Path $parsedCellPath -PathType Leaf) "parsed-cell.json exists"
Assert-True (Test-Path $palettePath    -PathType Leaf) "source/image-palette.json exists"

if ($fail -gt 0) {
    Write-Output ""
    Write-Output "Results: $pass passed, $fail failed"
    exit 1
}

# ---------------------------------------------------------------------------
# palette_sha256 field
# ---------------------------------------------------------------------------

$a = Get-Content $parsedCellPath -Raw | ConvertFrom-Json

Write-Output ""
Write-Output "--- palette_sha256 field ---"
Assert-True ($null -ne $a.PSObject.Properties['palette_sha256']) "palette_sha256 field present in parsed-cell.json"

if ($fail -gt 0) {
    Write-Output ""
    Write-Output "Results: $pass passed, $fail failed"
    exit 1
}

$stored = [string]$a.palette_sha256
Assert-True ($stored -match '^[0-9a-f]{64}$') "palette_sha256 is 64-char lowercase hex"

# ---------------------------------------------------------------------------
# Compute SHA-256 of source/image-palette.json and compare
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- SHA-256 match ---"

$sha    = [System.Security.Cryptography.SHA256]::Create()
$stream = [System.IO.File]::OpenRead($palettePath)
try {
    $computed = ($sha.ComputeHash($stream) | ForEach-Object { $_.ToString('x2') }) -join ''
}
finally { $stream.Dispose(); $sha.Dispose() }

Assert-True ($stored -eq $computed) `
    "palette_sha256 matches source/image-palette.json (stored $($stored.Substring(0,12))... computed $($computed.Substring(0,12))...)"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
