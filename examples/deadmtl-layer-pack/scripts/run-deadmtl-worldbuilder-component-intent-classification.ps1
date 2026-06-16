#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-25L: DeadMTL WorldBuilder Component Intent Classification Contract
# Classifies 45 connected components into future geometry intent buckets.
# No terrain generation. No lot subdivision. No sidewalk geometry. No road geometry.
# No building placement. No fences. No concrete geometry created. No runtime proof.
# Layout not materialized. Intent classification only.

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$metadataPath         = Join-Path $repoRoot "examples\deadmtl-layer-pack\worldbuilder\tiles\map_00.zone_metadata.json"
$sourceMaskRegions    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-source-mask-region-extraction\map_00\map_00.source_mask_region_extraction.json"
$connectedComponents  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-connected-component-extraction\map_00\map_00.connected_component_extraction.json"
$geometryPrimSchema   = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00\map_00.geometry_primitive_schema.json"
$geometryPreflight    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.json"

$outDir  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-component-intent-classification\map_00"
$outJson = Join-Path $outDir "map_00.component_intent_classification.json"
$outMd   = Join-Path $outDir "map_00.component_intent_classification.md"
$outCsv  = Join-Path $outDir "map_00.component_intent_classification.csv"
$outSum  = Join-Path $outDir "map_00.component_intent_classification.summary.txt"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet run `
    --project (Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-build-worldbuilder-component-intent-classification `
    --metadata                  $metadataPath `
    --source-mask-regions       $sourceMaskRegions `
    --connected-components      $connectedComponents `
    --geometry-primitive-schema $geometryPrimSchema `
    --geometry-preflight        $geometryPreflight `
    --output-json               $outJson `
    --output-md                 $outMd `
    --output-csv                $outCsv `
    --summary                   $outSum

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-component-intent-classification failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Outputs:"
Write-Host "  $outJson"
Write-Host "  $outMd"
Write-Host "  $outCsv"
Write-Host "  $outSum"
