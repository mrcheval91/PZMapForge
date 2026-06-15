#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-25I: DeadMTL WorldBuilder Geometry Primitive Schema Contract
# No terrain generation. No lot subdivision. No sidewalk geometry. No road geometry.
# No building placement. No fences. No lotpack writing.
# No worldgen override file. No concrete geometry created. No runtime proof.
# Layout not materialized. Schema contract only.

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$profilePath        = Join-Path $repoRoot "examples\deadmtl-layer-pack\worldbuilder\neighborhoods\deadmtl_baseline_neighborhood_profile.json"
$metadataPath       = Join-Path $repoRoot "examples\deadmtl-layer-pack\worldbuilder\tiles\map_00.zone_metadata.json"
$futureLayoutPlan   = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-future-world-layout-plan\map_00\map_00.future_world_layout_plan.json"
$geometryPreflight  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.json"

$outDir  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00"
$outJson = Join-Path $outDir "map_00.geometry_primitive_schema.json"
$outMd   = Join-Path $outDir "map_00.geometry_primitive_schema.md"
$outCsv  = Join-Path $outDir "map_00.geometry_primitive_schema.csv"
$outSum  = Join-Path $outDir "map_00.geometry_primitive_schema.summary.txt"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet run `
    --project (Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-build-worldbuilder-geometry-primitive-schema `
    --profile              $profilePath `
    --metadata             $metadataPath `
    --future-layout-plan   $futureLayoutPlan `
    --geometry-preflight   $geometryPreflight `
    --output-json          $outJson `
    --output-md            $outMd `
    --output-csv           $outCsv `
    --summary              $outSum

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-geometry-primitive-schema failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Outputs:"
Write-Host "  $outJson"
Write-Host "  $outMd"
Write-Host "  $outCsv"
Write-Host "  $outSum"
