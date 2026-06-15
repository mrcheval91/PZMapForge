#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# No worldgen override file. No lotpack writing. No runtime execution. Contract only.

$RepoRoot   = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$TileId     = "map_00"
$OutputDir  = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-lot-subdivision-plan\$TileId"
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$MetadataPath   = Join-Path $RepoRoot "examples\deadmtl-layer-pack\worldbuilder\tiles\$TileId.zone_metadata.json"
$ProfilePath    = Join-Path $RepoRoot "examples\deadmtl-layer-pack\worldbuilder\neighborhoods\deadmtl_baseline_neighborhood_profile.json"
$InspectionPath = Join-Path $RepoRoot ".local\deadmtl-authoring\raw-map-tile-inspection\$TileId\$TileId.raw_tile_inspection.json"

$OutputJson = Join-Path $OutputDir "$TileId.lot_subdivision_plan.json"
$OutputMd   = Join-Path $OutputDir "$TileId.lot_subdivision_plan.md"
$OutputCsv  = Join-Path $OutputDir "$TileId.lot_subdivision_plan.csv"
$OutputSum  = Join-Path $OutputDir "$TileId.lot_subdivision_plan.summary.txt"

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "MAP-25C: DeadMTL WorldBuilder Lot Subdivision Plan Contract"
Write-Host "tile_id:   $TileId"
Write-Host "metadata:  $MetadataPath"
Write-Host "profile:   $ProfilePath"

if (Test-Path $InspectionPath) {
    Write-Host "inspection: $InspectionPath"
} else {
    Write-Host "inspection: (not found - will be skipped)"
    $InspectionPath = ""
}

$dotnetArgs = @(
    "run",
    "--project", $CliProject,
    "--configuration", "Release",
    "--no-build",
    "--",
    "deadmtl-build-worldbuilder-lot-subdivision-plan",
    "--metadata",    $MetadataPath,
    "--profile",     $ProfilePath,
    "--output-json", $OutputJson,
    "--output-md",   $OutputMd,
    "--output-csv",  $OutputCsv,
    "--summary",     $OutputSum
)

if ($InspectionPath -ne "") {
    $dotnetArgs += "--inspection"
    $dotnetArgs += $InspectionPath
}

& dotnet @dotnetArgs
$ExitCode = $LASTEXITCODE

Write-Host ""
Write-Host "output-json: $OutputJson"
Write-Host "output-md:   $OutputMd"
Write-Host "output-csv:  $OutputCsv"
Write-Host "summary:     $OutputSum"

if ($ExitCode -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-lot-subdivision-plan failed with exit code $ExitCode"
    exit $ExitCode
}

# MAP25C_WORLDBUILDER_LOT_SUBDIVISION_PLAN_CONTRACT_COMPLETE
Write-Host "VERDICT: MAP25C_WORLDBUILDER_LOT_SUBDIVISION_PLAN_CONTRACT_COMPLETE"
exit 0
