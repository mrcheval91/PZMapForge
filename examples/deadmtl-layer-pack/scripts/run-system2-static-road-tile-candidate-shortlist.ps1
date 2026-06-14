param(
    [int]$Top = 25
)

$ErrorActionPreference = "Stop"

$repoRoot         = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$LocalSurveyScript = Join-Path $PSScriptRoot "run-system2-static-road-local-tile-survey.ps1"
$CliProject        = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$LocalSurveyJson = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-local-tile-survey\system2_static_road_local_tile_survey.json"
$ShortlistDir    = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-candidate-shortlist"
$ShortlistJson   = Join-Path $ShortlistDir "system2_static_road_tile_candidate_shortlist.json"
$ShortlistSummary = Join-Path $ShortlistDir "system2_static_road_tile_candidate_shortlist.summary.txt"

Write-Host "Running local tile survey to produce input..."
& powershell -ExecutionPolicy Bypass -File $LocalSurveyScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Local tile survey failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $ShortlistDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-build-static-road-tile-candidate-shortlist..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-tile-candidate-shortlist `
    --input   $LocalSurveyJson `
    --output  $ShortlistJson `
    --summary $ShortlistSummary `
    --top     $Top

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-tile-candidate-shortlist failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "Shortlist: $ShortlistJson"
Write-Host "Summary:   $ShortlistSummary"
Write-Host "VERDICT: MAP22K_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_SHORTLIST_COMPLETE"
