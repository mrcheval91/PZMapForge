param(
    [int]$OriginX = 10580,
    [int]$OriginY = 8200
)

$ErrorActionPreference = "Stop"

$repoRoot            = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$PlacementPlanScript = Join-Path $PSScriptRoot "run-system2-static-road-placement-plan.ps1"
$CliProject          = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$PlanJson    = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-placement-plan\system2_static_road_placement_plan.json"
$TileDir     = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-tile-family-plan"
$TileJson    = Join-Path $TileDir "system2_static_road_tile_family_plan.json"
$TileSummary = Join-Path $TileDir "system2_static_road_tile_family_plan.summary.txt"

Write-Host "Running placement plan to produce input..."
& powershell -ExecutionPolicy Bypass -File $PlacementPlanScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Placement plan failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $TileDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-build-static-road-tile-family-plan..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-tile-family-plan `
    --input   $PlanJson `
    --output  $TileJson `
    --summary $TileSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-tile-family-plan failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "Plan:    $TileJson"
Write-Host "Summary: $TileSummary"
Write-Host "VERDICT: MAP22H_SYSTEM2_STATIC_ROAD_TILE_FAMILY_PLAN_COMPLETE"
