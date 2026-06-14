$ErrorActionPreference = "Stop"

$repoRoot          = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$ShortlistScript   = Join-Path $PSScriptRoot "run-system2-static-road-tile-candidate-shortlist.ps1"
$CliProject        = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$ShortlistJson = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist\system2_static_road_tile_candidate_shortlist.json"
$ReviewDir     = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-candidate-review"
$ReviewJson    = Join-Path $ReviewDir "system2_static_road_tile_candidate_review.json"
$ReviewMd      = Join-Path $ReviewDir "system2_static_road_tile_candidate_review.md"
$ReviewCsv     = Join-Path $ReviewDir "system2_static_road_tile_candidate_review.csv"
$ReviewSummary = Join-Path $ReviewDir "system2_static_road_tile_candidate_review.summary.txt"

Write-Host "Running tile candidate shortlist to produce input..."
& powershell -ExecutionPolicy Bypass -File $ShortlistScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tile candidate shortlist failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $ReviewDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-build-static-road-tile-candidate-review..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-tile-candidate-review `
    --input       $ShortlistJson `
    --output-json $ReviewJson `
    --output-md   $ReviewMd `
    --output-csv  $ReviewCsv `
    --summary     $ReviewSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-tile-candidate-review failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "JSON:    $ReviewJson"
Write-Host "MD:      $ReviewMd"
Write-Host "CSV:     $ReviewCsv"
Write-Host "Summary: $ReviewSummary"
Write-Host "VERDICT: MAP22L_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_PACKET_COMPLETE"
