$ErrorActionPreference = "Stop"

$repoRoot      = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$ApplyScript   = Join-Path $PSScriptRoot "run-system2-static-road-tile-candidate-review-apply.ps1"
$CliProject    = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$AppliedJson   = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied\system2_static_road_tile_candidate_review_applied.json"

$ManifestDir     = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-human-approved-tile-candidates"
$ManifestJson    = Join-Path $ManifestDir "system2_static_road_human_approved_tile_candidates.json"
$ManifestMd      = Join-Path $ManifestDir "system2_static_road_human_approved_tile_candidates.md"
$ManifestCsv     = Join-Path $ManifestDir "system2_static_road_human_approved_tile_candidates.csv"
$ManifestSummary = Join-Path $ManifestDir "system2_static_road_human_approved_tile_candidates.summary.txt"

Write-Host "Running tile candidate review apply to produce applied review JSON..."
& powershell -ExecutionPolicy Bypass -File $ApplyScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tile candidate review apply failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $ManifestDir | Out-Null

Write-Host "Building CLI..."
$buildAttempt = 0
$buildOk = $false
while (-not $buildOk -and $buildAttempt -lt 3) {
    $buildAttempt++
    & dotnet build $CliProject --configuration Release -q /nodeReuse:false
    if ($LASTEXITCODE -eq 0) {
        $buildOk = $true
    } elseif ($buildAttempt -lt 3) {
        # MSB3492 AssemblyInfoInputs.cache read race: a prior `dotnet build`
        # invocation's background compiler-server process can still hold the
        # file handle for a brief window after that process returns. This is
        # a known transient MSBuild race, not a real compile failure - retry.
        Start-Sleep -Milliseconds 500
    }
}
if (-not $buildOk) {
    Write-Error "Build failed after $buildAttempt attempts."
    exit 1
}

Write-Host "Running system2-build-static-road-human-approved-tile-candidates..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-human-approved-tile-candidates `
    --input       $AppliedJson `
    --output-json $ManifestJson `
    --output-md   $ManifestMd `
    --output-csv  $ManifestCsv `
    --summary     $ManifestSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-human-approved-tile-candidates failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "JSON:    $ManifestJson"
Write-Host "MD:      $ManifestMd"
Write-Host "CSV:     $ManifestCsv"
Write-Host "Summary: $ManifestSummary"
Write-Host "VERDICT: MAP22N_SYSTEM2_STATIC_ROAD_HUMAN_APPROVED_TILE_CANDIDATES_COMPLETE"
