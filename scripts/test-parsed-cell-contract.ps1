#Requires -Version 5.1
<#
.SYNOPSIS
    Artifact contract validation for .local/mapforge/parsed-cell.json.

    Checks that the artifact produced by image-mapforge.ps1 satisfies the
    pzmapforge.parsed-cell.v0.1 contract:
      - required top-level fields present
      - schema sentinel correct
      - dimensions match palette config (300x300)
      - rows count == height
      - every row length == width
      - counts sum to width * height
      - all 9 required semantic kinds present in counts
      - outputs section names all 5 expected local artifacts
      - claim_boundary sentinel correct

    Runs ImageMapForge first if parsed-cell.json does not exist.
    Exits 0 if all checks pass, exits 1 if any check fails.
    Does not commit .local/. Does not touch media/maps.
    Does not claim playable Project Zomboid export.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$jsonPath   = Join-Path $repoRoot '.local\mapforge\parsed-cell.json'
$imfScript  = Join-Path $repoRoot 'source\image-mapforge.ps1'
$imgScript  = Join-Path $repoRoot 'scripts\new-test-image.ps1'
$sampleImg  = Join-Path $repoRoot '.local\mapforge\sample-input.png'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# ---------------------------------------------------------------------------
# Ensure artifact exists
# ---------------------------------------------------------------------------

if (-not (Test-Path $jsonPath -PathType Leaf)) {
    Write-Output "parsed-cell.json not found. Running ImageMapForge to generate it..."
    if (-not (Test-Path $sampleImg -PathType Leaf)) {
        & powershell -ExecutionPolicy Bypass -File $imgScript
        if ($LASTEXITCODE -ne 0) { Write-Error "new-test-image.ps1 failed."; exit 1 }
    }
    & powershell -ExecutionPolicy Bypass -File $imfScript -ImagePath $sampleImg
    if ($LASTEXITCODE -ne 0) { Write-Error "image-mapforge.ps1 failed."; exit 1 }
}

if (-not (Test-Path $jsonPath -PathType Leaf)) {
    Write-Error "parsed-cell.json still not found after generation attempt."
    exit 1
}

Write-Output "Contract validation: $jsonPath"
Write-Output ""

# ---------------------------------------------------------------------------
# Load artifact
# ---------------------------------------------------------------------------

$raw = Get-Content $jsonPath -Raw
$a   = $raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Required top-level fields
# ---------------------------------------------------------------------------

Write-Output "--- Required fields ---"
$requiredFields = @(
    'schema', 'tool', 'claim_boundary',
    'source_image', 'palette',
    'width', 'height', 'resized',
    'matching', 'legend', 'counts', 'nearest_drift', 'rows', 'outputs'
)
foreach ($field in $requiredFields) {
    $present = $null -ne $a.PSObject.Properties[$field]
    Assert-True $present "Field '$field' present"
}

# ---------------------------------------------------------------------------
# Schema sentinel
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Schema sentinel ---"
Assert-True ($a.schema -eq 'pzmapforge.parsed-cell.v0.1') `
    "schema == 'pzmapforge.parsed-cell.v0.1' (got '$($a.schema)')"

# ---------------------------------------------------------------------------
# Claim boundary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Claim boundary ---"
Assert-True ($a.claim_boundary -eq 'planning_artifact_only_not_pz_load_tested') `
    "claim_boundary == 'planning_artifact_only_not_pz_load_tested' (got '$($a.claim_boundary)')"

# ---------------------------------------------------------------------------
# Dimensions
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Dimensions ---"
$W = [int]$a.width
$H = [int]$a.height
Assert-True ($W -eq 300) "width == 300 (got $W)"
Assert-True ($H -eq 300) "height == 300 (got $H)"

# ---------------------------------------------------------------------------
# Rows structural integrity
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Rows ---"
$rows      = @($a.rows)
$rowCount  = $rows.Count
Assert-True ($rowCount -eq $H) "rows.Count == height ($H) (got $rowCount)"

$badRows = 0
for ($i = 0; $i -lt $rows.Count; $i++) {
    if ($rows[$i].Length -ne $W) { $badRows++ }
}
Assert-True ($badRows -eq 0) "All row lengths == width ($W) ($badRows bad rows)"

# ---------------------------------------------------------------------------
# Counts integrity
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Counts ---"
$counts  = @($a.counts)
$kindSum = 0
foreach ($c in $counts) { $kindSum += [int]$c.pixels }
$expected = $W * $H
Assert-True ($kindSum -eq $expected) "counts pixel sum == $expected (got $kindSum)"

# ---------------------------------------------------------------------------
# Required semantic kinds
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Required kinds ---"
$requiredKinds = @('grass','road','sidewalk','row_house','depanneur',
                   'garage','industrial_yard','landmark','spawn')
$presentKinds  = @($counts | ForEach-Object { [string]$_.kind })
foreach ($kind in $requiredKinds) {
    Assert-True ($presentKinds -contains $kind) "Kind '$kind' present in counts"
}

# ---------------------------------------------------------------------------
# Outputs section
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Outputs section ---"
$expectedOutputs = @('json','report','preview','generated_tileset','tmx')
foreach ($key in $expectedOutputs) {
    $present = $null -ne $a.outputs.PSObject.Properties[$key]
    Assert-True $present "outputs.$key present"
}

# ---------------------------------------------------------------------------
# Matching section
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Matching section ---"
$matchingFields = @('exact_pixels','nearest_pixels','unique_source_colours','unmapped_exact_colours')
foreach ($field in $matchingFields) {
    $present = $null -ne $a.matching.PSObject.Properties[$field]
    Assert-True $present "matching.$field present"
}
$totalPixels = [int]$a.matching.exact_pixels + [int]$a.matching.nearest_pixels
Assert-True ($totalPixels -eq $expected) `
    "matching exact+nearest == $expected (got $totalPixels)"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
