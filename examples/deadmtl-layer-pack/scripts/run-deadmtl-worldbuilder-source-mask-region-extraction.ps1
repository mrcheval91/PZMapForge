#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-25J: DeadMTL WorldBuilder Source Mask Region Extraction Contract
# Reads source PNG and emits mask region records bound to zone metadata.
# No terrain generation. No lot subdivision. No sidewalk geometry. No road geometry.
# No building placement. No fences. No lotpack writing.
# No worldgen override file. No concrete geometry created. No runtime proof.
# Layout not materialized. Source mask extraction only.

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$sourcePng              = "E:\Omni\Zomboid\assets\raw\map_00.png"
$metadataPath           = Join-Path $repoRoot "examples\deadmtl-layer-pack\worldbuilder\tiles\map_00.zone_metadata.json"
$geometryPrimSchema     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-geometry-primitive-schema\map_00\map_00.geometry_primitive_schema.json"
$geometryPreflight      = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-concrete-geometry-preflight\map_00\map_00.concrete_geometry_preflight.json"

$outDir  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-source-mask-region-extraction\map_00"
$outJson = Join-Path $outDir "map_00.source_mask_region_extraction.json"
$outMd   = Join-Path $outDir "map_00.source_mask_region_extraction.md"
$outCsv  = Join-Path $outDir "map_00.source_mask_region_extraction.csv"
$outSum  = Join-Path $outDir "map_00.source_mask_region_extraction.summary.txt"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet run `
    --project (Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-build-worldbuilder-source-mask-region-extraction `
    --source-png                $sourcePng `
    --metadata                  $metadataPath `
    --geometry-primitive-schema $geometryPrimSchema `
    --geometry-preflight        $geometryPreflight `
    --output-json               $outJson `
    --output-md                 $outMd `
    --output-csv                $outCsv `
    --summary                   $outSum

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-source-mask-region-extraction failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Outputs:"
Write-Host "  $outJson"
Write-Host "  $outMd"
Write-Host "  $outCsv"
Write-Host "  $outSum"
