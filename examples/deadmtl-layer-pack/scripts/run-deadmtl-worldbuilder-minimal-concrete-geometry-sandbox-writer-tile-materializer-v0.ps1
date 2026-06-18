#Requires -Version 5.1
<#
    MAP-27C: WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materializer V0
    Consumes MAP-27B1 tile buffer outputs and materializes the 256x256 sandbox buffer
    into deterministic sandbox tile/material records.

    SANDBOX ONLY. No PZ runtime files are written.

    Materialization rules:
    BUILDING_FOOTPRINT -> BUILDING_EXTERIOR_WALL_CANDIDATE (WALL) or BUILDING_INTERIOR_FLOOR_CANDIDATE (FLOOR)
    ACCESS_LINK        -> ACCESS_EDGE_CANDIDATE (ACCESS)
    LOT_BOUNDARY       -> LOT_YARD_OR_SERVICE_SPACE_CANDIDATE (LOT)
    COMPONENT_ENVELOPE -> COMPONENT_RESIDUAL_SPACE_CANDIDATE (COMPONENT)

    Layer stack order: 1=COMPONENT, 2=LOT, 3=ACCESS, 4=FLOOR, 5=WALL
#>

param(
    [string]$TileBufferResult           = "",
    [string]$TileBufferCells            = "",
    [string]$TileBufferOwnership        = "",
    [string]$TileBufferReplayLog        = "",
    [string]$TileBufferCollisionReport  = "",
    [string]$TileBufferForbiddenOutputGuard = "",
    [string]$OutputRoot                 = "",
    [string]$OutputJson                 = "",
    [string]$OutputMd                   = "",
    [string]$OutputCsv                  = "",
    [string]$Summary                    = ""
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..") | Select-Object -ExpandProperty Path

$InputDir  = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00"
$OutputDir = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00"

if ([string]::IsNullOrEmpty($TileBufferResult)) {
    $TileBufferResult = Join-Path $InputDir "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json"
}
if ([string]::IsNullOrEmpty($TileBufferCells)) {
    $TileBufferCells = Join-Path $InputDir "map_00.sandbox_writer_tile_buffer_cells.csv"
}
if ([string]::IsNullOrEmpty($TileBufferOwnership)) {
    $TileBufferOwnership = Join-Path $InputDir "map_00.sandbox_writer_tile_buffer_ownership.json"
}
if ([string]::IsNullOrEmpty($TileBufferReplayLog)) {
    $TileBufferReplayLog = Join-Path $InputDir "map_00.sandbox_writer_tile_buffer_replay_log.json"
}
if ([string]::IsNullOrEmpty($TileBufferCollisionReport)) {
    $TileBufferCollisionReport = Join-Path $InputDir "map_00.sandbox_writer_tile_buffer_collision_report.json"
}
if ([string]::IsNullOrEmpty($TileBufferForbiddenOutputGuard)) {
    $TileBufferForbiddenOutputGuard = Join-Path $InputDir "map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json"
}
if ([string]::IsNullOrEmpty($OutputRoot)) {
    $OutputRoot = $OutputDir
}
if ([string]::IsNullOrEmpty($OutputJson)) {
    $OutputJson = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"
}
if ([string]::IsNullOrEmpty($OutputMd)) {
    $OutputMd = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md"
}
if ([string]::IsNullOrEmpty($OutputCsv)) {
    $OutputCsv = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv"
}
if ([string]::IsNullOrEmpty($Summary)) {
    $Summary = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt"
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$ArgList = @(
    "run", "--project", $CliProject, "--",
    "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0",
    "--tile-buffer-result",              $TileBufferResult,
    "--tile-buffer-cells",               $TileBufferCells,
    "--tile-buffer-ownership",           $TileBufferOwnership,
    "--tile-buffer-replay-log",          $TileBufferReplayLog,
    "--tile-buffer-collision-report",    $TileBufferCollisionReport,
    "--tile-buffer-forbidden-output-guard", $TileBufferForbiddenOutputGuard,
    "--output-root",  $OutputRoot,
    "--output-json",  $OutputJson,
    "--output-md",    $OutputMd,
    "--output-csv",   $OutputCsv,
    "--summary",      $Summary
)

Write-Host "Running MAP-27C tile materializer..."
& dotnet @ArgList
$ExitCode = $LASTEXITCODE

if ($ExitCode -eq 0) {
    Write-Host ""
    Write-Host "MAP-27C complete. Outputs written to: $OutputRoot"
    Write-Host "  Main JSON   : $OutputJson"
    Write-Host "  Markdown    : $OutputMd"
    Write-Host "  CSV         : $OutputCsv"
    Write-Host "  Summary     : $Summary"
} else {
    Write-Error "MAP-27C tile materializer exited with code $ExitCode"
}

exit $ExitCode
