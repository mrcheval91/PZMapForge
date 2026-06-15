$ErrorActionPreference = "Stop"

$repoRoot    = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$CliProject  = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

# MAP-22I survey contract is the input
$SurveyJson  = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.json"

# PZ install root â€” adjust to your local install
$PzRoot = "D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid"

$OutDir      = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-local-tile-survey-filtered"
$OutJson     = Join-Path $OutDir "system2_static_road_local_tile_survey_filtered.json"
$OutMd       = Join-Path $OutDir "system2_static_road_local_tile_survey_filtered.md"
$OutCsv      = Join-Path $OutDir "system2_static_road_local_tile_survey_filtered.csv"
$OutSummary  = Join-Path $OutDir "system2_static_road_local_tile_survey_filtered.summary.txt"

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-build-static-road-local-tile-survey-filtered..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-local-tile-survey-filtered `
    --input       $SurveyJson `
    --pz-root     $PzRoot `
    --output-json $OutJson `
    --output-md   $OutMd `
    --output-csv  $OutCsv `
    --summary     $OutSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-local-tile-survey-filtered failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "JSON:    $OutJson"
Write-Host "MD:      $OutMd"
Write-Host "CSV:     $OutCsv"
Write-Host "Summary: $OutSummary"
Write-Host "VERDICT: MAP22O_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_FILTERED_COMPLETE"

