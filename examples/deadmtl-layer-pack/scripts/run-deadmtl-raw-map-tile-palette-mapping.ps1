#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-23B: DeadMTL Raw Tile Palette Mapping Contract
# Mapping contract only. No compilation. No runtime proof. No writer readiness.

$RepoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$InspectionJson  = Join-Path $RepoRoot ".local\deadmtl-authoring\raw-map-tile-inspection\map_00\map_00.raw_tile_inspection.json"
$WorldgenPalette = Join-Path $RepoRoot "examples\deadmtl-layer-pack\palettes\worldgen-png-palette.json"
$System2Palette  = Join-Path $RepoRoot "examples\deadmtl-layer-pack\palettes\system2-static-road-intent-palette.json"
$OutDir          = Join-Path $RepoRoot ".local\deadmtl-authoring\raw-map-tile-palette-mapping\map_00"
$OutJson         = Join-Path $OutDir "map_00.raw_tile_palette_mapping.json"
$OutMd           = Join-Path $OutDir "map_00.raw_tile_palette_mapping.md"
$OutCsv          = Join-Path $OutDir "map_00.raw_tile_palette_mapping.csv"
$OutSummary      = Join-Path $OutDir "map_00.raw_tile_palette_mapping.summary.txt"

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

dotnet run `
    --project (Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-build-raw-map-tile-palette-mapping `
    --inspection       $InspectionJson `
    --worldgen-palette $WorldgenPalette `
    --system2-palette  $System2Palette `
    --output-json      $OutJson `
    --output-md        $OutMd `
    --output-csv       $OutCsv `
    --summary          $OutSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-raw-map-tile-palette-mapping failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "VERDICT: MAP23B_RAW_TILE_PALETTE_MAPPING_CONTRACT_COMPLETE"
