#Requires -Version 5.1
<#
    MAP-27B: WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Buffer V0
    Applies MAP-27A operation files to an internal 256x256 tile buffer.
    Produces: touched-cell CSV, ownership JSON, replay log, collision report,
              forbidden output guard JSON, plus 4 main outputs.

    SANDBOX ONLY. No PZ runtime files are written.

    Access edge derivation: zero-dimension ACCESS_LINK_WRITE ops derive their
    edge cells from the component envelope bbox using access_kind/side fields.
    FRONTAGE_ACCESS/NORTH -> north edge (y=min_y, x=min_x..max_x).
    REAR_SERVICE_ACCESS/EAST -> east edge (x=max_x, y=min_y..max_y).
#>

param(
    [string]$SandboxWriterResult = "",
    [string]$ComponentOperations  = "",
    [string]$LotOperations        = "",
    [string]$BuildingSlotOperations = "",
    [string]$AccessOperations     = "",
    [string]$ForbiddenOutputGuard = "",
    [string]$OutputRoot           = "",
    [string]$OutputJson           = "",
    [string]$OutputMd             = "",
    [string]$OutputCsv            = "",
    [string]$Summary              = ""
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..") | Select-Object -ExpandProperty Path

$InputDir  = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-v0\map_00"
$OutputDir = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00"

if ([string]::IsNullOrEmpty($SandboxWriterResult)) {
    $SandboxWriterResult = Join-Path $InputDir "map_00.minimal_concrete_geometry_sandbox_writer_v0.json"
}
if ([string]::IsNullOrEmpty($ComponentOperations)) {
    $ComponentOperations = Join-Path $InputDir "map_00.sandbox_writer_component_operations.json"
}
if ([string]::IsNullOrEmpty($LotOperations)) {
    $LotOperations = Join-Path $InputDir "map_00.sandbox_writer_lot_operations.json"
}
if ([string]::IsNullOrEmpty($BuildingSlotOperations)) {
    $BuildingSlotOperations = Join-Path $InputDir "map_00.sandbox_writer_building_slot_operations.json"
}
if ([string]::IsNullOrEmpty($AccessOperations)) {
    $AccessOperations = Join-Path $InputDir "map_00.sandbox_writer_access_operations.json"
}
if ([string]::IsNullOrEmpty($ForbiddenOutputGuard)) {
    $ForbiddenOutputGuard = Join-Path $InputDir "map_00.sandbox_writer_forbidden_output_guard.json"
}
if ([string]::IsNullOrEmpty($OutputRoot)) {
    $OutputRoot = $OutputDir
}
if ([string]::IsNullOrEmpty($OutputJson)) {
    $OutputJson = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json"
}
if ([string]::IsNullOrEmpty($OutputMd)) {
    $OutputMd = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.md"
}
if ([string]::IsNullOrEmpty($OutputCsv)) {
    $OutputCsv = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.csv"
}
if ([string]::IsNullOrEmpty($Summary)) {
    $Summary = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.summary.txt"
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$ArgList = @(
    "run", "--project", $CliProject, "--",
    "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0",
    "--sandbox-writer-result",    $SandboxWriterResult,
    "--component-operations",     $ComponentOperations,
    "--lot-operations",           $LotOperations,
    "--building-slot-operations", $BuildingSlotOperations,
    "--access-operations",        $AccessOperations,
    "--forbidden-output-guard",   $ForbiddenOutputGuard,
    "--output-root",              $OutputRoot,
    "--output-json",              $OutputJson,
    "--output-md",                $OutputMd,
    "--output-csv",               $OutputCsv,
    "--summary",                  $Summary
)

Write-Host "Running MAP-27B tile buffer writer..."
& dotnet @ArgList
$ExitCode = $LASTEXITCODE

if ($ExitCode -eq 0) {
    Write-Host ""
    Write-Host "MAP-27B complete. Outputs written to: $OutputRoot"
    Write-Host "  Main JSON   : $OutputJson"
    Write-Host "  Markdown    : $OutputMd"
    Write-Host "  CSV         : $OutputCsv"
    Write-Host "  Summary     : $Summary"
} else {
    Write-Error "MAP-27B tile buffer writer exited with code $ExitCode"
}

exit $ExitCode
