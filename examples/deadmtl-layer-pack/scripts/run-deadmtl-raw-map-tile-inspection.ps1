#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# MAP-23A: DeadMTL Raw 256x256 Map Tile Inspection
# Inspection only. No compilation. No runtime proof. No writer readiness.

$RepoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$InputPng        = "E:\Omni\Zomboid\assets\raw\map_00.png"
$WorldgenPalette = Join-Path $RepoRoot "examples\deadmtl-layer-pack\palettes\worldgen-png-palette.json"
$System2Palette  = Join-Path $RepoRoot "examples\deadmtl-layer-pack\palettes\system2-static-road-intent-palette.json"
$OutDir          = Join-Path $RepoRoot ".local\deadmtl-authoring\raw-map-tile-inspection\map_00"
$OutJson         = Join-Path $OutDir "map_00.raw_tile_inspection.json"
$OutMd           = Join-Path $OutDir "map_00.raw_tile_inspection.md"
$OutCsv          = Join-Path $OutDir "map_00.raw_tile_colors.csv"
$OutSummary      = Join-Path $OutDir "map_00.raw_tile_inspection.summary.txt"

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

dotnet run `
    --project (Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj") `
    --configuration Release `
    --no-build `
    -- `
    deadmtl-inspect-raw-map-tile `
    --input            $InputPng `
    --worldgen-palette $WorldgenPalette `
    --system2-palette  $System2Palette `
    --output-json      $OutJson `
    --output-md        $OutMd `
    --output-csv       $OutCsv `
    --summary          $OutSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-inspect-raw-map-tile failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "VERDICT: MAP23A_RAW_256_MAP_TILE_INSPECTION_COMPLETE"
