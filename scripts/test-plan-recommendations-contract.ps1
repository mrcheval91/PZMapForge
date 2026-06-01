#Requires -Version 5.1
<#
.SYNOPSIS
    Contract validator for .local/mapforge/plan-recommendations.json.

    If the artifact is missing, generates it via dotnet plan-export (no-build,
    no validate.ps1 recursion).
    Exits 0 if all checks pass, exits 1 if any fail.
    Does not commit .local/. Does not touch media/maps.
    Does not claim playable Project Zomboid export.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$outputDir  = Join-Path $repoRoot '.local\mapforge'
$planJson   = Join-Path $outputDir 'plan-recommendations.json'
$planMd     = Join-Path $outputDir 'plan-report.md'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# ---------------------------------------------------------------------------
# Ensure artifacts exist (direct dotnet call, not validate.ps1)
# ---------------------------------------------------------------------------

if (-not (Test-Path $planJson -PathType Leaf)) {
    Write-Output "plan-recommendations.json not found. Running plan-export..."
    & dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
        --configuration Release --no-build `
        -- plan-export `
        --path (Join-Path $repoRoot '.local\mapforge\parsed-cell.json') `
        --output (Join-Path $repoRoot '.local\mapforge')
    if ($LASTEXITCODE -ne 0) { Write-Error "plan-export failed."; exit 1 }
}

Write-Output "Plan recommendations contract: $planJson"
Write-Output ""

# ---------------------------------------------------------------------------
# Output files
# ---------------------------------------------------------------------------

Write-Output "--- Output files ---"
Assert-True (Test-Path $planJson -PathType Leaf) "plan-recommendations.json exists"
Assert-True (Test-Path $planMd   -PathType Leaf) "plan-report.md exists"

if ($fail -gt 0) {
    Write-Output ""; Write-Output "Results: $pass passed, $fail failed"; exit 1
}

$a = Get-Content $planJson -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Sentinels
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Sentinels ---"
Assert-True ($a.schema         -eq 'pzmapforge.plan-recommendations.v0.1') "schema == pzmapforge.plan-recommendations.v0.1"
Assert-True ($a.claim_boundary -eq 'planning_artifact_only_not_pz_load_tested') "claim_boundary correct"
Assert-True ([int]$a.width  -eq 300) "width == 300"
Assert-True ([int]$a.height -eq 300) "height == 300"

# ---------------------------------------------------------------------------
# Counts
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Counts ---"
Assert-True ([int]$a.primitive_count     -gt 0)                        "primitive_count > 0"
Assert-True ([int]$a.recommendation_count -gt 0)                       "recommendation_count > 0"
Assert-True ([int]$a.warning_count        -ge 0)                       "warning_count >= 0"
Assert-True ([int]$a.warning_count        -le [int]$a.recommendation_count) "warning_count <= recommendation_count"

# ---------------------------------------------------------------------------
# Recommendations array
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Recommendations ---"
Assert-True ($null -ne $a.PSObject.Properties['recommendations']) "recommendations field exists"

$recs = @($a.recommendations)
Assert-True ($recs.Count -eq [int]$a.recommendation_count) `
    "recommendations.Count == recommendation_count (got $($recs.Count))"

$requiredRecFields = @('recommendation_id', 'recommendation_type', 'severity', 'planning_role', 'message')
foreach ($field in $requiredRecFields) {
    $missing = 0
    foreach ($r in $recs) {
        if ($null -eq $r.PSObject.Properties[$field]) { $missing++ }
    }
    Assert-True ($missing -eq 0) "All recommendations have '$field' ($missing missing)"
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Summary ---"
Assert-True ($null -ne $a.PSObject.Properties['summary']) "summary field exists"
Assert-True ([int]$a.summary.recommendation_count -eq [int]$a.recommendation_count) `
    "summary.recommendation_count == top-level recommendation_count"
Assert-True ([int]$a.summary.warning_count -eq [int]$a.warning_count) `
    "summary.warning_count == top-level warning_count"

$sevSum = 0
$a.summary.counts_by_severity.PSObject.Properties | ForEach-Object { $sevSum += [int]$_.Value }
Assert-True ($sevSum -eq [int]$a.recommendation_count) `
    "counts_by_severity values sum == recommendation_count (got $sevSum)"

# ---------------------------------------------------------------------------
# Thresholds used
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Thresholds ---"
Assert-True ($null -ne $a.PSObject.Properties['thresholds_used']) "thresholds_used field exists"

if ($null -ne $a.PSObject.Properties['thresholds_used']) {
    $t = $a.thresholds_used
    Assert-True ($null -ne $t.PSObject.Properties['tiny_building_pixel_threshold']) `
        "thresholds_used.tiny_building_pixel_threshold exists"
    Assert-True ($null -ne $t.PSObject.Properties['large_ground_pixel_threshold']) `
        "thresholds_used.large_ground_pixel_threshold exists"
    Assert-True ([int]$t.tiny_building_pixel_threshold -ge 0) `
        "tiny_building_pixel_threshold >= 0 (got $($t.tiny_building_pixel_threshold))"
    Assert-True ([int]$t.large_ground_pixel_threshold -ge 0) `
        "large_ground_pixel_threshold >= 0 (got $($t.large_ground_pixel_threshold))"
    Assert-True ([int]$t.tiny_building_pixel_threshold -eq 9) `
        "tiny_building_pixel_threshold == 9 (got $($t.tiny_building_pixel_threshold))"
    Assert-True ([int]$t.large_ground_pixel_threshold -eq 50000) `
        "large_ground_pixel_threshold == 50000 (got $($t.large_ground_pixel_threshold))"
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
