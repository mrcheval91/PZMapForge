#Requires -Version 5.1
<#
.SYNOPSIS
    Hardening test harness for source/image-mapforge.ps1.

    Creates synthetic test images in .local/mapforge-test/ (gitignored).
    Does not depend on pre-existing .local/ state.
    Exits 0 if all tests pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir
$imfScript = Join-Path $repoRoot 'source\image-mapforge.ps1'
$testDir   = Join-Path $repoRoot '.local\mapforge-test'
$outputDir = Join-Path $repoRoot '.local\mapforge'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# Sets local ErrorActionPreference so NativeCommandError from 2>&1 in PS5.1
# does not propagate as a terminating error into the test harness.
function Invoke-Imf {
    param([string[]]$ExtraArgs, [switch]$CaptureOutput)
    $ErrorActionPreference = 'SilentlyContinue'
    $allArgs = @('-ExecutionPolicy', 'Bypass', '-File', $imfScript) + $ExtraArgs
    if ($CaptureOutput) {
        $out = & powershell @allArgs 2>&1
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $out }
    }
    else {
        & powershell @allArgs 2>&1 | Out-Null
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $null }
    }
}

# ---------------------------------------------------------------------------
# Synthetic test images
# Grass colour from palette: #648C46 = RGB(100, 140, 70)
# Near-grass (not in palette, forces nearest-colour): #658D47 = RGB(101, 141, 71)
# ---------------------------------------------------------------------------

if (-not (Test-Path $testDir)) { New-Item -ItemType Directory -Force $testDir | Out-Null }
if (Test-Path $outputDir)      { Remove-Item -Recurse -Force $outputDir }

$GRASS_HEX = '#648C46'
$GRASS_C   = [System.Drawing.Color]::FromArgb(255, 100, 140, 70)
$NEAR_HEX  = '#658D47'
$NEAR_C    = [System.Drawing.Color]::FromArgb(255, 101, 141, 71)

$img300 = Join-Path $testDir 'test-300x300.png'
$bmp300 = [System.Drawing.Bitmap]::new(300, 300)
$g300   = [System.Drawing.Graphics]::FromImage($bmp300)
$g300.Clear($GRASS_C); $g300.Dispose()
$bmp300.SetPixel(150, 150, $NEAR_C)
$bmp300.Save($img300, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp300.Dispose()

$img10  = Join-Path $testDir 'test-10x10.png'
$bmp10  = [System.Drawing.Bitmap]::new(10, 10)
$g10    = [System.Drawing.Graphics]::FromImage($bmp10)
$g10.Clear($GRASS_C); $g10.Dispose()
$bmp10.Save($img10, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp10.Dispose()

Write-Output "Test images created. outputDir cleared."
Write-Output ""

# ---------------------------------------------------------------------------
# Test 1: Bad image path exits nonzero
# ---------------------------------------------------------------------------

Write-Output "Test 1: Bad image path"
$r = Invoke-Imf @('-ImagePath', (Join-Path $testDir 'does-not-exist.png'))
Assert-True ($r.ExitCode -ne 0) "Bad image path exits nonzero (exit $($r.ExitCode))"

# ---------------------------------------------------------------------------
# Test 2: Non-300x300 without -Resize exits nonzero
# ---------------------------------------------------------------------------

Write-Output "Test 2: Non-300x300 without -Resize"
$r = Invoke-Imf @('-ImagePath', $img10)
Assert-True ($r.ExitCode -ne 0) "10x10 without -Resize exits nonzero (exit $($r.ExitCode))"

# ---------------------------------------------------------------------------
# Test 3: External OutputDir refused without -AllowExternalOutput
# ---------------------------------------------------------------------------

Write-Output "Test 3: External output refusal"
$r = Invoke-Imf @('-ImagePath', $img300, '-OutputDir', $env:TEMP)
Assert-True ($r.ExitCode -ne 0) "External OutputDir exits nonzero (exit $($r.ExitCode))"

# ---------------------------------------------------------------------------
# Test 4: media/maps output path refused
# ---------------------------------------------------------------------------

Write-Output "Test 4: media/maps output refused"
$mediaPath = Join-Path $repoRoot 'media\maps'
$r = Invoke-Imf @('-ImagePath', $img300, '-OutputDir', $mediaPath)
Assert-True ($r.ExitCode -ne 0) "media/maps OutputDir exits nonzero (exit $($r.ExitCode))"
# Confirm the media/maps directory was not created by the tool
Assert-True (-not (Test-Path $mediaPath)) "media/maps not created after refused run"

# ---------------------------------------------------------------------------
# Test 5: Normal run exits 0 and writes all 5 output files
# ---------------------------------------------------------------------------

Write-Output "Test 5: Normal run"
$r = Invoke-Imf @('-ImagePath', $img300)
Assert-True ($r.ExitCode -eq 0)                                                "Normal run exits 0 (exit $($r.ExitCode))"
Assert-True (Test-Path (Join-Path $outputDir 'parsed-cell.json'))              "parsed-cell.json written"
Assert-True (Test-Path (Join-Path $outputDir 'parsed-cell-report.md'))         "parsed-cell-report.md written"
Assert-True (Test-Path (Join-Path $outputDir 'parsed-cell-preview.png'))       "parsed-cell-preview.png written"
Assert-True (Test-Path (Join-Path $outputDir 'parsed-cell-tiles.png'))         "parsed-cell-tiles.png written"
Assert-True (Test-Path (Join-Path $outputDir 'parsed-cell-basic.tmx'))         "parsed-cell-basic.tmx written"

# ---------------------------------------------------------------------------
# Test 6: Debug mode exits 0 and reports frequencies and drift
# ---------------------------------------------------------------------------

Write-Output "Test 6: Debug mode"
$r = Invoke-Imf @('-ImagePath', $img300, '-Mode', 'Debug') -CaptureOutput
Assert-True ($r.ExitCode -eq 0) "Debug mode exits 0 (exit $($r.ExitCode))"

$dbg = $r.Output -join "`n"
Assert-True ($dbg -match 'DEBUG.*colour frequencies')      "Debug mode outputs colour frequencies header"
Assert-True ($dbg -match $GRASS_HEX)                       "Debug mode lists grass hex in frequencies"
Assert-True ($dbg -match 'DEBUG.*unmapped')                "Debug mode outputs unmapped section header"
Assert-True ($dbg -match $NEAR_HEX)                        "Debug mode lists near-grass hex in unmapped section"
Assert-True ($dbg -match 'DEBUG.*drift')                   "Debug mode outputs drift section header"

# ---------------------------------------------------------------------------
# Test 7: Kind counts sum to width * height
# ---------------------------------------------------------------------------

Write-Output "Test 7: Kind counts completeness"
$json    = Get-Content (Join-Path $outputDir 'parsed-cell.json') -Raw | ConvertFrom-Json
$kindSum = 0
foreach ($prop in $json.counts.PSObject.Properties) { $kindSum += [int]$prop.Value }
$expected = [int]$json.width * [int]$json.height
Assert-True ($kindSum -eq $expected) "Kind counts sum to $expected (got $kindSum)"

# ---------------------------------------------------------------------------
# Test 8: Deterministic kind counts across two runs
# ---------------------------------------------------------------------------

Write-Output "Test 8: Deterministic kind counts"
$counts1 = $json.counts | ConvertTo-Json -Compress -Depth 3

Remove-Item -Recurse -Force $outputDir
$r = Invoke-Imf @('-ImagePath', $img300)
Assert-True ($r.ExitCode -eq 0) "Second run exits 0"
$json2   = Get-Content (Join-Path $outputDir 'parsed-cell.json') -Raw | ConvertFrom-Json
$counts2 = $json2.counts | ConvertTo-Json -Compress -Depth 3
Assert-True ($counts1 -eq $counts2) "Kind counts identical across two runs of the same image"

# ---------------------------------------------------------------------------
# Test 9: Nearest-colour drift report — presence, accuracy, and report section
# ---------------------------------------------------------------------------

Write-Output "Test 9: Drift report"
$drift = $json2.nearest_drift
Assert-True ($null -ne $drift -and @($drift).Count -gt 0) `
    "nearest_drift present and non-empty in JSON"

$driftRow = @($drift) | Where-Object { $_.source_hex -eq $NEAR_HEX }
Assert-True ($null -ne $driftRow)                "Drift record for near-grass ($NEAR_HEX) present"
Assert-True ($driftRow.nearest_kind -eq 'grass') "Near-grass mapped to grass kind"
Assert-True ([double]$driftRow.dist -lt 5.0)     "Drift distance small (got $($driftRow.dist))"
Assert-True ([int]$driftRow.count -eq 1)          "Drift count is 1 (one near-grass pixel)"

$rpt = Get-Content (Join-Path $outputDir 'parsed-cell-report.md') -Raw
Assert-True ($rpt -match 'Nearest-colour drift') "Drift section present in markdown report"
Assert-True ($rpt -match $NEAR_HEX)              "Near-grass hex in drift table in report"

# ---------------------------------------------------------------------------
# Test 10: .local/ not visible in git status
# ---------------------------------------------------------------------------

Write-Output "Test 10: .local/ gitignore proof"
$ErrorActionPreference = 'SilentlyContinue'
$gitOut = git -C $repoRoot status --porcelain 2>&1
$ErrorActionPreference = 'Stop'
$leaked = @($gitOut | Where-Object { [string]$_ -match '\.local[\\/]' })
Assert-True ($leaked.Count -eq 0) ".local/ absent from git status (leaked: $($leaked.Count))"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
