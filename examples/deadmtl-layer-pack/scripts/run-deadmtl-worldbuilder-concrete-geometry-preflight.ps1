#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-25H: DeadMTL WorldBuilder Concrete Geometry Preflight Contract
# No terrain generation. No lot subdivision. No sidewalk geometry. No road geometry.
# No building placement. No fences. No lotpack writing.
# No worldgen override file. No concrete geometry created. No runtime proof.
# Layout not materialized. Contract only.

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$profilePath          = Join-Path $repoRoot "examples\deadmtl-layer-pack\worldbuilder\neighborhoods\deadmtl_baseline_neighborhood_profile.json"
$metadataPath         = Join-Path $repoRoot "examples\deadmtl-layer-pack\worldbuilder\tiles\map_00.zone_metadata.json"
$lotPlanPath          = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-lot-subdivision-plan\map_00\map_00.lot_subdivision_plan.json"
$sidewalkPlanPath     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-sidewalk-generation-plan\map_00\map_00.sidewalk_generation_plan.json"
$buildingSelectPath   = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-building-selection-policy-plan\map_00\map_00.building_selection_policy_plan.json"
$dependencyManifest   = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-generation-dependency-manifest\map_00\map_00.generation_dependency_manifest.json"
$futureLayoutPlan     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-future-world-layout-plan\map_00\map_00.future_world_layout_plan.json"

$outDir  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00"
$outJson = Join-Path $outDir "map_00.concrete_geometry_preflight.json"
$outMd   = Join-Path $outDir "map_00.concrete_geometry_preflight.md"
$outCsv  = Join-Path $outDir "map_00.concrete_geometry_preflight.csv"
$outSum  = Join-Path $outDir "map_00.concrete_geometry_preflight.summary.txt"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet run `
    --project (Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-build-worldbuilder-concrete-geometry-preflight `
    --profile                $profilePath `
    --metadata               $metadataPath `
    --lot-plan               $lotPlanPath `
    --sidewalk-plan          $sidewalkPlanPath `
    --building-selection-plan $buildingSelectPath `
    --dependency-manifest    $dependencyManifest `
    --future-layout-plan     $futureLayoutPlan `
    --output-json            $outJson `
    --output-md              $outMd `
    --output-csv             $outCsv `
    --summary                $outSum

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-concrete-geometry-preflight failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Outputs:"
Write-Host "  $outJson"
Write-Host "  $outMd"
Write-Host "  $outCsv"
Write-Host "  $outSum"
