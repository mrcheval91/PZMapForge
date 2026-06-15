$ErrorActionPreference = "Stop"

$repoRoot        = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$FilteredScript  = Join-Path $PSScriptRoot "run-system2-static-road-local-tile-survey-filtered.ps1"
$CliProject      = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$FilteredJson = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-local-tile-survey-filtered\system2_static_road_local_tile_survey_filtered.json"

$OutDir      = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-filtered-tile-candidate-shortlist"
$OutJson     = Join-Path $OutDir "system2_static_road_filtered_tile_candidate_shortlist.json"
$OutMd       = Join-Path $OutDir "system2_static_road_filtered_tile_candidate_shortlist.md"
$OutCsv      = Join-Path $OutDir "system2_static_road_filtered_tile_candidate_shortlist.csv"
$OutSummary  = Join-Path $OutDir "system2_static_road_filtered_tile_candidate_shortlist.summary.txt"

Write-Host "Running filtered local tile survey..."
& powershell -ExecutionPolicy Bypass -File $FilteredScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Filtered local tile survey failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-build-static-road-filtered-tile-candidate-shortlist..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-filtered-tile-candidate-shortlist `
    --input       $FilteredJson `
    --output-json $OutJson `
    --output-md   $OutMd `
    --output-csv  $OutCsv `
    --summary     $OutSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-filtered-tile-candidate-shortlist failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "JSON:    $OutJson"
Write-Host "MD:      $OutMd"
Write-Host "CSV:     $OutCsv"
Write-Host "Summary: $OutSummary"
Write-Host "VERDICT: MAP22P_SYSTEM2_STATIC_ROAD_FILTERED_TILE_CANDIDATE_SHORTLIST_COMPLETE"
