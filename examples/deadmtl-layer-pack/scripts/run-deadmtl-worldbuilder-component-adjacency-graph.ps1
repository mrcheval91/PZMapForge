#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-25M: DeadMTL WorldBuilder Component Adjacency Graph Contract
# Detects 4-way pixel adjacency between the 45 classified components.
# No terrain generation. No lot subdivision. No sidewalk geometry. No road geometry.
# No building placement. No fences. No concrete geometry created. No runtime proof.
# Layout not materialized. Adjacency graph only.

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$sourcePng           = "E:\Omni\Zomboid\assets\raw\map_00.png"
$connectedComponents = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-connected-component-extraction\map_00\map_00.connected_component_extraction.json"
$componentIntents    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-component-intent-classification\map_00\map_00.component_intent_classification.json"
$geometryPrimSchema  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00\map_00.geometry_primitive_schema.json"
$geometryPreflight   = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.json"

$outDir  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-component-adjacency-graph\map_00"
$outJson = Join-Path $outDir "map_00.component_adjacency_graph.json"
$outMd   = Join-Path $outDir "map_00.component_adjacency_graph.md"
$outCsv  = Join-Path $outDir "map_00.component_adjacency_graph.csv"
$outSum  = Join-Path $outDir "map_00.component_adjacency_graph.summary.txt"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet run `
    --project (Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-build-worldbuilder-component-adjacency-graph `
    --source-png               $sourcePng `
    --connected-components     $connectedComponents `
    --component-intents        $componentIntents `
    --geometry-primitive-schema $geometryPrimSchema `
    --geometry-preflight       $geometryPreflight `
    --output-json              $outJson `
    --output-md                $outMd `
    --output-csv               $outCsv `
    --summary                  $outSum

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-component-adjacency-graph failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Outputs:"
Write-Host "  $outJson"
Write-Host "  $outMd"
Write-Host "  $outCsv"
Write-Host "  $outSum"
