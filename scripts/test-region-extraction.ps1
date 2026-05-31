#Requires -Version 5.1
<#
.SYNOPSIS
    Test harness for semantic region extraction.

    Runs extract-regions.ps1 if regions.json is missing, then validates the
    output against the pzmapforge.regions.v0.1 contract.
    Exits 0 if all checks pass, exits 1 if any fail.
    Does not commit .local/. Does not touch media/maps.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$outputDir   = Join-Path $repoRoot '.local\mapforge'
$regionsJson = Join-Path $outputDir 'regions.json'
$regionsMd   = Join-Path $outputDir 'regions-report.md'
$extractScript = Join-Path $repoRoot 'scripts\extract-regions.ps1'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# ---------------------------------------------------------------------------
# Ensure regions exist
# ---------------------------------------------------------------------------

if (-not (Test-Path $regionsJson -PathType Leaf)) {
    Write-Output "regions.json not found. Running extract-regions.ps1..."
    & powershell -ExecutionPolicy Bypass -File $extractScript
    if ($LASTEXITCODE -ne 0) { Write-Error "extract-regions.ps1 failed."; exit 1 }
}

Write-Output "Region extraction validation: $regionsJson"
Write-Output ""

$r = Get-Content $regionsJson -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Output files present
# ---------------------------------------------------------------------------

Write-Output "--- Output files ---"
Assert-True (Test-Path $regionsJson -PathType Leaf) "regions.json exists"
Assert-True (Test-Path $regionsMd   -PathType Leaf) "regions-report.md exists"

# ---------------------------------------------------------------------------
# Schema and claim sentinels
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Sentinels ---"
Assert-True ($r.schema         -eq 'pzmapforge.regions.v0.1')               "schema == pzmapforge.regions.v0.1"
Assert-True ($r.claim_boundary -eq 'planning_artifact_only_not_pz_load_tested') "claim_boundary correct"

# ---------------------------------------------------------------------------
# Dimensions
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Dimensions ---"
Assert-True ([int]$r.width  -eq 300) "width == 300"
Assert-True ([int]$r.height -eq 300) "height == 300"

# ---------------------------------------------------------------------------
# Regions structure
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Regions structure ---"
$regions = @($r.regions)
Assert-True ($regions.Count -gt 0) "at least 1 region present (got $($regions.Count))"

$allPositive = $true
foreach ($reg in $regions) {
    if ([int]$reg.pixel_count -le 0) { $allPositive = $false; break }
}
Assert-True $allPositive "all regions have positive pixel_count"

$allBoundsOk = $true
foreach ($reg in $regions) {
    $bx = [int]$reg.bounds.x; $by = [int]$reg.bounds.y
    $bw = [int]$reg.bounds.width; $bh = [int]$reg.bounds.height
    if ($bx -lt 0 -or $by -lt 0 -or ($bx + $bw) -gt 300 -or ($by + $bh) -gt 300 -or $bw -le 0 -or $bh -le 0) {
        $allBoundsOk = $false; break
    }
}
Assert-True $allBoundsOk "all region bounds inside 300x300"

$allCentroidsOk = $true
foreach ($reg in $regions) {
    $bx = [double]$reg.bounds.x; $by = [double]$reg.bounds.y
    $bw = [double]$reg.bounds.width; $bh = [double]$reg.bounds.height
    $cx = [double]$reg.centroid.x; $cy = [double]$reg.centroid.y
    if ($cx -lt ($bx - 0.5) -or $cx -gt ($bx + $bw - 0.5) -or
        $cy -lt ($by - 0.5) -or $cy -gt ($by + $bh - 0.5)) {
        $allCentroidsOk = $false; break
    }
}
Assert-True $allCentroidsOk "all centroids within their region bounds"

# ---------------------------------------------------------------------------
# Summary by kind
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Summary by kind ---"
$summary = @($r.summary_by_kind)
Assert-True ($summary.Count -ge 9) "at least 9 kinds in summary (got $($summary.Count))"

$pixelSum = 0
foreach ($s in $summary) { $pixelSum += [int]$s.total_pixels }
Assert-True ($pixelSum -eq 90000) "summary pixel sum == 90000 (got $pixelSum)"

$requiredKinds = @('grass','road','sidewalk','row_house','depanneur',
                   'garage','industrial_yard','landmark','spawn')
$presentKinds  = @($summary | ForEach-Object { [string]$_.kind })
foreach ($k in $requiredKinds) {
    Assert-True ($presentKinds -contains $k) "kind '$k' in summary"
}

# ---------------------------------------------------------------------------
# Determinism: two runs produce identical JSON
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Determinism ---"
$text1 = Get-Content $regionsJson -Raw

& powershell -ExecutionPolicy Bypass -File $extractScript 2>&1 | Out-Null
$exitCode2 = $LASTEXITCODE
Assert-True ($exitCode2 -eq 0) "second extraction run exits 0 (exit $exitCode2)"

$text2 = Get-Content $regionsJson -Raw
Assert-True ($text1 -eq $text2) "regions.json text identical across two runs"

# ---------------------------------------------------------------------------
# .local/ not visible in git status
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Gitignore proof ---"
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
