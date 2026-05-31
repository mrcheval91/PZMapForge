#Requires -Version 5.1
<#
.SYNOPSIS
    Test harness for semantic primitive classification.

    Runs classify-primitives.ps1 if primitives.json is missing, then validates
    the output against the pzmapforge.primitives.v0.1 contract.
    Exits 0 if all checks pass, exits 1 if any fail.
    Does not commit .local/. Does not touch media/maps.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$outputDir      = Join-Path $repoRoot '.local\mapforge'
$primitivesJson = Join-Path $outputDir 'primitives.json'
$primitivesMd   = Join-Path $outputDir 'primitives-report.md'
$classifyScript = Join-Path $repoRoot 'scripts\classify-primitives.ps1'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Classify {
    $ErrorActionPreference = 'SilentlyContinue'
    & powershell -ExecutionPolicy Bypass -File $classifyScript 2>&1 | Out-Null
    return $LASTEXITCODE
}

# ---------------------------------------------------------------------------
# Ensure primitives exist
# ---------------------------------------------------------------------------

if (-not (Test-Path $primitivesJson -PathType Leaf)) {
    Write-Output "primitives.json not found. Running classify-primitives.ps1..."
    $ec = Invoke-Classify
    if ($ec -ne 0) { Write-Error "classify-primitives.ps1 failed."; exit 1 }
}

Write-Output "Primitive classification validation: $primitivesJson"
Write-Output ""

$pr = Get-Content $primitivesJson -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Output files
# ---------------------------------------------------------------------------

Write-Output "--- Output files ---"
Assert-True (Test-Path $primitivesJson -PathType Leaf) "primitives.json exists"
Assert-True (Test-Path $primitivesMd   -PathType Leaf) "primitives-report.md exists"

# ---------------------------------------------------------------------------
# Sentinels
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Sentinels ---"
Assert-True ($pr.schema         -eq 'pzmapforge.primitives.v0.1')               "schema == pzmapforge.primitives.v0.1"
Assert-True ($pr.claim_boundary -eq 'planning_artifact_only_not_pz_load_tested') "claim_boundary correct"

# ---------------------------------------------------------------------------
# Dimensions
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Dimensions ---"
Assert-True ([int]$pr.width  -eq 300) "width == 300"
Assert-True ([int]$pr.height -eq 300) "height == 300"

# ---------------------------------------------------------------------------
# Primitives array structure
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Primitives structure ---"
$prims = @($pr.primitives)
Assert-True ($prims.Count -gt 0) "primitives array non-empty (got $($prims.Count))"

$requiredFields = @('primitive_id','primitive_type','source_region_id','kind','code',
                    'pixel_count','bounds','centroid','planning_role')
$missingField = $false
foreach ($prim in $prims) {
    foreach ($f in $requiredFields) {
        if ($null -eq $prim.PSObject.Properties[$f]) { $missingField = $true; break }
    }
    if ($missingField) { break }
}
Assert-True (-not $missingField) "all primitives have all required fields"

$allPositive = $true
foreach ($prim in $prims) { if ([int]$prim.pixel_count -le 0) { $allPositive = $false; break } }
Assert-True $allPositive "all primitives have positive pixel_count"

$allBoundsOk = $true
foreach ($prim in $prims) {
    $bx = [int]$prim.bounds.x; $by = [int]$prim.bounds.y
    $bw = [int]$prim.bounds.width; $bh = [int]$prim.bounds.height
    if ($bx -lt 0 -or $by -lt 0 -or ($bx + $bw) -gt 300 -or ($by + $bh) -gt 300 -or $bw -le 0 -or $bh -le 0) {
        $allBoundsOk = $false; break
    }
}
Assert-True $allBoundsOk "all primitive bounds inside 300x300"

$allCentroidsOk = $true
foreach ($prim in $prims) {
    $bx = [double]$prim.bounds.x; $bw = [double]$prim.bounds.width
    $by = [double]$prim.bounds.y; $bh = [double]$prim.bounds.height
    $cx = [double]$prim.centroid.x; $cy = [double]$prim.centroid.y
    if ($cx -lt ($bx - 0.5) -or $cx -gt ($bx + $bw - 0.5) -or
        $cy -lt ($by - 0.5) -or $cy -gt ($by + $bh - 0.5)) {
        $allCentroidsOk = $false; break
    }
}
Assert-True $allCentroidsOk "all primitive centroids within bounds"

# ---------------------------------------------------------------------------
# All expected primitive types present
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Primitive types ---"
$presentTypes = @($pr.summary_by_primitive_type | ForEach-Object { [string]$_.primitive_type })
$expectedTypes = @('road_region','sidewalk_region','building_footprint',
                   'yard_region','landmark_marker','spawn_marker','ground_region')
foreach ($t in $expectedTypes) {
    Assert-True ($presentTypes -contains $t) "primitive type '$t' present"
}

# ---------------------------------------------------------------------------
# Pixel count completeness
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Pixel sum ---"
$pxSum = 0
foreach ($prim in $prims) { $pxSum += [int]$prim.pixel_count }
Assert-True ($pxSum -eq 90000) "sum of primitive pixel_count == 90000 (got $pxSum)"

# ---------------------------------------------------------------------------
# Determinism: two runs produce identical JSON
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Determinism ---"
$text1  = Get-Content $primitivesJson -Raw
$exitCode2 = Invoke-Classify
Assert-True ($exitCode2 -eq 0) "second classification run exits 0 (exit $exitCode2)"
$text2  = Get-Content $primitivesJson -Raw
Assert-True ($text1 -eq $text2) "primitives.json text identical across two runs"

# ---------------------------------------------------------------------------
# .local/ gitignore proof
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
