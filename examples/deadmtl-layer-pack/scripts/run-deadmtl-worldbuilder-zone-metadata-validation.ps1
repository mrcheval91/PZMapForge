#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-25B: DeadMTL WorldBuilder Raw Tile Zone Metadata Contract Validation
# Metadata only. No terrain generation. No sidewalk generation.
# No lot subdivision. No building placement. No fence placement.
# No lotpack writing. No worldgen override file. No runtime proof.

$RepoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$MetadataPath  = Join-Path $RepoRoot "examples\deadmtl-layer-pack\worldbuilder\tiles\map_00.zone_metadata.json"
$InspectionPath = Join-Path $RepoRoot ".local\deadmtl-authoring\raw-map-tile-inspection\map_00\map_00.raw_tile_inspection.json"
$ProfilePath   = Join-Path $RepoRoot "examples\deadmtl-layer-pack\worldbuilder\neighborhoods\deadmtl_baseline_neighborhood_profile.json"

$OutDir     = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-zone-metadata\map_00"
$OutJson    = Join-Path $OutDir "map_00.zone_metadata.validation.json"
$OutMd      = Join-Path $OutDir "map_00.zone_metadata.validation.md"
$OutCsv     = Join-Path $OutDir "map_00.zone_metadata.validation.csv"
$OutSummary = Join-Path $OutDir "map_00.zone_metadata.validation.summary.txt"

$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

if (-not (Test-Path $CliProject)) {
    Write-Error "CLI project not found: $CliProject"
    exit 1
}

if (-not (Test-Path $InspectionPath)) {
    Write-Error "Inspection JSON not found: $InspectionPath"
    Write-Error "Run MAP-23A first: examples\deadmtl-layer-pack\scripts\run-deadmtl-raw-map-tile-inspection.ps1"
    exit 1
}

Write-Host "MAP-25B: Validating WorldBuilder zone metadata..."
Write-Host "  metadata:    $MetadataPath"
Write-Host "  inspection:  $InspectionPath"
Write-Host "  profile:     $ProfilePath"
Write-Host "  output-json: $OutJson"

dotnet run --project $CliProject --configuration Release --no-build -- `
    deadmtl-validate-worldbuilder-zone-metadata `
    --metadata     $MetadataPath `
    --inspection   $InspectionPath `
    --profile      $ProfilePath `
    --output-json  $OutJson `
    --output-md    $OutMd `
    --output-csv   $OutCsv `
    --summary      $OutSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-validate-worldbuilder-zone-metadata failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Output files:"
Write-Host "  JSON:    $OutJson"
Write-Host "  MD:      $OutMd"
Write-Host "  CSV:     $OutCsv"
Write-Host "  Summary: $OutSummary"
Write-Host ""

if (Test-Path $OutSummary) {
    Get-Content $OutSummary | Write-Host
}

# VERDICT: MAP25B_WORLDBUILDER_RAW_TILE_ZONE_METADATA_CONTRACT_COMPLETE
