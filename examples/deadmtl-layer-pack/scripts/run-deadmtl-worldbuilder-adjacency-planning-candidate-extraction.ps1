#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-25N: DeadMTL WorldBuilder Adjacency Planning Candidate Extraction Contract
# Converts 82 adjacency edges from MAP-25M into 82 planning candidate records.
# Contract only. No terrain generation. No lot subdivision. No sidewalk geometry.
# No road geometry. No frontage geometry. No rear access geometry. No building slots.
# No fences. No concrete building IDs. No lotpack. No worldgen override file.
# No concrete geometry created. No runtime proof. Not writer-ready. Layout not materialized.

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$adjacencyGraph      = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-component-adjacency-graph\map_00\map_00.component_adjacency_graph.json"
$connectedComponents = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-connected-component-extraction\map_00\map_00.connected_component_extraction.json"
$componentIntents    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-component-intent-classification\map_00\map_00.component_intent_classification.json"
$geometryPrimSchema  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00\map_00.geometry_primitive_schema.json"
$geometryPreflight   = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.json"

$outDir  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-adjacency-planning-candidate-extraction\map_00"
$outJson = Join-Path $outDir "map_00.adjacency_planning_candidate_extraction.json"
$outMd   = Join-Path $outDir "map_00.adjacency_planning_candidate_extraction.md"
$outCsv  = Join-Path $outDir "map_00.adjacency_planning_candidate_extraction.csv"
$outSum  = Join-Path $outDir "map_00.adjacency_planning_candidate_extraction.summary.txt"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet run `
    --project (Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-build-worldbuilder-adjacency-planning-candidate-extraction `
    --adjacency-graph          $adjacencyGraph `
    --connected-components     $connectedComponents `
    --component-intents        $componentIntents `
    --geometry-primitive-schema $geometryPrimSchema `
    --geometry-preflight       $geometryPreflight `
    --output-json              $outJson `
    --output-md                $outMd `
    --output-csv               $outCsv `
    --summary                  $outSum

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-adjacency-planning-candidate-extraction failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Outputs:"
Write-Host "  $outJson"
Write-Host "  $outMd"
Write-Host "  $outCsv"
Write-Host "  $outSum"
