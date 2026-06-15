#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# No worldgen override file. No lotpack writing. No sidewalk generation. Contract only.

$RepoRoot   = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$TileId     = "map_00"
$OutputDir  = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-sidewalk-generation-plan\$TileId"
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$ProfilePath  = Join-Path $RepoRoot "examples\deadmtl-layer-pack\worldbuilder\neighborhoods\deadmtl_baseline_neighborhood_profile.json"
$MetadataPath = Join-Path $RepoRoot "examples\deadmtl-layer-pack\worldbuilder\tiles\$TileId.zone_metadata.json"
$LotPlanPath  = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-lot-subdivision-plan\$TileId\$TileId.lot_subdivision_plan.json"

$OutputJson = Join-Path $OutputDir "$TileId.sidewalk_generation_plan.json"
$OutputMd   = Join-Path $OutputDir "$TileId.sidewalk_generation_plan.md"
$OutputCsv  = Join-Path $OutputDir "$TileId.sidewalk_generation_plan.csv"
$OutputSum  = Join-Path $OutputDir "$TileId.sidewalk_generation_plan.summary.txt"

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "MAP-25D: DeadMTL WorldBuilder Sidewalk Generation Plan Contract"
Write-Host "tile_id:   $TileId"
Write-Host "profile:   $ProfilePath"
Write-Host "metadata:  $MetadataPath"
Write-Host "lot-plan:  $LotPlanPath"

if (-not (Test-Path $LotPlanPath)) {
    Write-Error "Lot subdivision plan not found: $LotPlanPath"
    Write-Error "Run run-deadmtl-worldbuilder-lot-subdivision-plan.ps1 first."
    exit 1
}

& dotnet run `
    --project $CliProject `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-build-worldbuilder-sidewalk-generation-plan `
    --profile    $ProfilePath `
    --metadata   $MetadataPath `
    --lot-plan   $LotPlanPath `
    --output-json $OutputJson `
    --output-md   $OutputMd `
    --output-csv  $OutputCsv `
    --summary     $OutputSum

$ExitCode = $LASTEXITCODE

Write-Host ""
Write-Host "output-json: $OutputJson"
Write-Host "output-md:   $OutputMd"
Write-Host "output-csv:  $OutputCsv"
Write-Host "summary:     $OutputSum"

if ($ExitCode -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-sidewalk-generation-plan failed with exit code $ExitCode"
    exit $ExitCode
}

# MAP25D_WORLDBUILDER_SIDEWALK_GENERATION_PLAN_CONTRACT_COMPLETE
Write-Host "VERDICT: MAP25D_WORLDBUILDER_SIDEWALK_GENERATION_PLAN_CONTRACT_COMPLETE"
exit 0
