$ErrorActionPreference = "Stop"

$repoRoot       = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$ReviewScript   = Join-Path $PSScriptRoot "run-system2-static-road-tile-candidate-review.ps1"
$CliProject     = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$ReviewJson     = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.json"
$DecisionsCsv   = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-candidate-review\system2_static_road_tile_candidate_review.csv"

$ApplyDir       = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-candidate-review-applied"
$AppliedJson    = Join-Path $ApplyDir "system2_static_road_tile_candidate_review_applied.json"
$AppliedMd      = Join-Path $ApplyDir "system2_static_road_tile_candidate_review_applied.md"
$AppliedCsv     = Join-Path $ApplyDir "system2_static_road_tile_candidate_review_applied.csv"
$AppliedSummary = Join-Path $ApplyDir "system2_static_road_tile_candidate_review_applied.summary.txt"

Write-Host "Running tile candidate review to produce base review packet..."
& powershell -ExecutionPolicy Bypass -File $ReviewScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tile candidate review failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $ApplyDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-apply-static-road-tile-candidate-review..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-apply-static-road-tile-candidate-review `
    --review-json    $ReviewJson `
    --decisions-csv  $DecisionsCsv `
    --output-json    $AppliedJson `
    --output-md      $AppliedMd `
    --output-csv     $AppliedCsv `
    --summary        $AppliedSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-apply-static-road-tile-candidate-review failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "JSON:    $AppliedJson"
Write-Host "MD:      $AppliedMd"
Write-Host "CSV:     $AppliedCsv"
Write-Host "Summary: $AppliedSummary"
Write-Host "VERDICT: MAP22M_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_APPLY_COMPLETE"
