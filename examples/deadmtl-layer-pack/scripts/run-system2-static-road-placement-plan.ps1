param(
    [int]$OriginX = 10580,
    [int]$OriginY = 8200
)

$ErrorActionPreference = "Stop"

$repoRoot      = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$SampleExtractScript = Join-Path $PSScriptRoot "run-system2-static-road-sample-extract.ps1"
$CliProject    = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$ExtractJson   = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-sample-extract\system2_static_road_sample_extract.json"
$PlanDir       = Join-Path $repoRoot ".local\deadmtl-authoring\system2-static-road-placement-plan"
$PlanJson      = Join-Path $PlanDir "system2_static_road_placement_plan.json"
$PlanSummary   = Join-Path $PlanDir "system2_static_road_placement_plan.summary.txt"

Write-Host "Running sample extract to produce input..."
& powershell -ExecutionPolicy Bypass -File $SampleExtractScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Sample extract failed with exit code $LASTEXITCODE"
    exit 1
}

New-Item -ItemType Directory -Force -Path $PlanDir | Out-Null

Write-Host "Building CLI..."
& dotnet build $CliProject --configuration Release -q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Running system2-build-static-road-placement-plan..."
& dotnet run --project $CliProject --configuration Release --no-build -- `
    system2-build-static-road-placement-plan `
    --input   $ExtractJson `
    --output  $PlanJson `
    --summary $PlanSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "system2-build-static-road-placement-plan failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "Plan:    $PlanJson"
Write-Host "Summary: $PlanSummary"
Write-Host "VERDICT: MAP22G_SYSTEM2_STATIC_ROAD_PLACEMENT_PLAN_COMPLETE"
