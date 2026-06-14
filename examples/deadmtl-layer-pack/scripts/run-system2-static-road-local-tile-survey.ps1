param(
    [string]$PzRoot = "D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid"
)

$ErrorActionPreference = "Stop"

$repoRoot       = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$SurveyScript   = Join-Path $PSScriptRoot "run-system2-static-road-tile-family-survey.ps1"
$CliProject     = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$SurveyJson     = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-family-survey\system2_static_road_tile_family_survey.json"
$LocalDir       = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-local-tile-survey"
$LocalJson      = Join-Path $LocalDir "system2_static_road_local_tile_survey.json"
$LocalSummary   = Join-Path $LocalDir "system2_static_road_local_tile_survey.summary.txt"

Write-Host "Running tile-family survey to produce input..."
& powershell -ExecutionPolicy Bypass -File $SurveyScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tile family survey failed with exit code $LASTEXITCODE"
    exit 1
}

if (-not (Test-Path $PzRoot)) {
    Write-Warning "PZ install root not found: $PzRoot"
    Write-Warning "Survey will run with zero candidates."
}

New-Item -ItemType Directory -Force -Path $LocalDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-build-static-road-local-tile-survey..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-local-tile-survey `
    --input   $SurveyJson `
    --pz-root $PzRoot `
    --output  $LocalJson `
    --summary $LocalSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-local-tile-survey failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "Survey:  $LocalJson"
Write-Host "Summary: $LocalSummary"
Write-Host "VERDICT: MAP22J_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_COMPLETE"
