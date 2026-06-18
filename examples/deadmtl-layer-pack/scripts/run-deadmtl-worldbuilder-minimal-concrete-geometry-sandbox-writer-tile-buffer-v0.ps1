#Requires -Version 5.1
<#
    MAP-27B: WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Buffer V0
    Applies MAP-27A operation files to an internal 256x256 tile buffer.
    Produces: touched-cell CSV, ownership JSON, replay log, collision report,
              forbidden output guard JSON, plus 4 main outputs.

    SANDBOX ONLY. No PZ runtime files are written.
#>

param(
    [string]$SandboxWriterResult = "",
    [string]$ComponentOp         = "",
    [string]$LotOp               = "",
    [string]$BuildingSlotOp      = "",
    [string]$AccessOp            = "",
    [string]$ForbiddenGuard      = "",
    [string]$OutputRoot          = "",
    [string]$OutputJson          = "",
    [string]$OutputMd            = "",
    [string]$OutputCsv           = "",
    [string]$Summary             = ""
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

$RepoRoot    = Resolve-Path (Join-Path $PSScriptRoot "..\..\..") | Select-Object -ExpandProperty Path
$ExamplesDir = Join-Path $RepoRoot "examples\deadmtl-layer-pack"
$LocalDir    = Join-Path $ExamplesDir ".local\map27b"

if ([string]::IsNullOrEmpty($SandboxWriterResult)) {
    $SandboxWriterResult = Join-Path $ExamplesDir ".local\map27a\map_00.sandbox_writer_v0.json"
}
if ([string]::IsNullOrEmpty($ComponentOp)) {
    $ComponentOp = Join-Path $ExamplesDir ".local\map27a\map_00.sandbox_writer_component_operations.json"
}
if ([string]::IsNullOrEmpty($LotOp)) {
    $LotOp = Join-Path $ExamplesDir ".local\map27a\map_00.sandbox_writer_lot_operations.json"
}
if ([string]::IsNullOrEmpty($BuildingSlotOp)) {
    $BuildingSlotOp = Join-Path $ExamplesDir ".local\map27a\map_00.sandbox_writer_building_slot_operations.json"
}
if ([string]::IsNullOrEmpty($AccessOp)) {
    $AccessOp = Join-Path $ExamplesDir ".local\map27a\map_00.sandbox_writer_access_operations.json"
}
if ([string]::IsNullOrEmpty($ForbiddenGuard)) {
    $ForbiddenGuard = Join-Path $ExamplesDir ".local\map27a\map_00.sandbox_writer_forbidden_output_guard.json"
}
if ([string]::IsNullOrEmpty($OutputRoot)) {
    $OutputRoot = $LocalDir
}
if ([string]::IsNullOrEmpty($OutputJson)) {
    $OutputJson = Join-Path $OutputRoot "map_00.sandbox_writer_tile_buffer_v0.json"
}
if ([string]::IsNullOrEmpty($OutputMd)) {
    $OutputMd = Join-Path $OutputRoot "map_00.sandbox_writer_tile_buffer_v0.md"
}
if ([string]::IsNullOrEmpty($OutputCsv)) {
    $OutputCsv = Join-Path $OutputRoot "map_00.sandbox_writer_tile_buffer_v0.csv"
}
if ([string]::IsNullOrEmpty($Summary)) {
    $Summary = Join-Path $OutputRoot "map_00.sandbox_writer_tile_buffer_v0.summary.txt"
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$ArgList = @(
    "run", "--project", $CliProject, "--",
    "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0",
    "--sandbox-writer-result", $SandboxWriterResult,
    "--component-op",          $ComponentOp,
    "--lot-op",                $LotOp,
    "--building-slot-op",      $BuildingSlotOp,
    "--access-op",             $AccessOp,
    "--forbidden-guard",       $ForbiddenGuard,
    "--output-root",           $OutputRoot,
    "--output-json",           $OutputJson,
    "--output-md",             $OutputMd,
    "--output-csv",            $OutputCsv,
    "--summary",               $Summary
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
