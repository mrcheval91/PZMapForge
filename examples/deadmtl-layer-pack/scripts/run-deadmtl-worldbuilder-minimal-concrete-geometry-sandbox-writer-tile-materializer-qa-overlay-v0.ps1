#Requires -Version 5.1
<#
    MAP-27D - DeadMTL Worldbuilder Minimal Concrete Geometry
             Sandbox Writer Tile Materializer QA Overlay v0

    Reads MAP-27C materialized-cells output and produces a 1024x1024 visual QA overlay PNG.
    Sandbox-only. Writes no PZ runtime files.

    Inputs expected in the MAP-27C authoring output directory:
        map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json
        map_00.sandbox_writer_tile_materialized_cells.csv
        map_00.sandbox_writer_tile_material_palette.json
        map_00.sandbox_writer_tile_layer_stack.json
        map_00.sandbox_writer_tile_materialization_replay_log.json
        map_00.sandbox_writer_tile_materialization_ownership_summary.json
        map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json

    Outputs written to:
        $AuthoringRoot\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.local\
#>

param (
    [string] $AuthoringRoot = (Join-Path $PSScriptRoot `
        "..\authoring\deadmtl\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.local")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── paths ──────────────────────────────────────────────────────────────────────

$Map27CRoot = (Resolve-Path $AuthoringRoot).Path

$TileMaterializerResult          = Join-Path $Map27CRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"
$MaterializedCells               = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialized_cells.csv"
$MaterialPalette                 = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_material_palette.json"
$LayerStack                      = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_layer_stack.json"
$MaterializationReplayLog        = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialization_replay_log.json"
$MaterializationOwnershipSummary = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialization_ownership_summary.json"
$MaterializerForbiddenOutputGuard= Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json"

$OutputRoot   = Join-Path (Join-Path (Join-Path $Map27CRoot "..") "..") "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.local"
$OutputRoot   = [IO.Path]::GetFullPath($OutputRoot)

$OutputJson   = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json"
$OutputMd     = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md"
$OutputCsv    = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv"
$OutputSummary= Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt"

if (-not (Test-Path $OutputRoot)) { New-Item -ItemType Directory -Path $OutputRoot | Out-Null }

# ── guard: required inputs ─────────────────────────────────────────────────────

$required = @(
    $TileMaterializerResult,
    $MaterializedCells,
    $MaterialPalette,
    $LayerStack,
    $MaterializationReplayLog,
    $MaterializationOwnershipSummary,
    $MaterializerForbiddenOutputGuard
)

$missing = @($required | Where-Object { -not (Test-Path $_) })
if ($missing.Count -gt 0) {
    Write-Error "MISSING INPUT(S):`n$($missing -join "`n")"
    exit 1
}

# ── guard: output root must end with .local ────────────────────────────────────

if (-not ($OutputRoot.TrimEnd('\', '/').EndsWith('.local'))) {
    Write-Error "Output root must end with .local -- got: $OutputRoot"
    exit 1
}

# ── run CLI ────────────────────────────────────────────────────────────────────

$RepoRoot   = (Resolve-Path (Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "..") "..")).Path
$CliProject = Join-Path (Join-Path (Join-Path $RepoRoot "src") "PZMapForge.Cli") "PZMapForge.Cli.csproj"

Write-Host "MAP-27D: QA overlay rendering..."
Write-Host "  Source : $Map27CRoot"
Write-Host "  Output : $OutputRoot"

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0 `
    --tile-materializer-result            $TileMaterializerResult `
    --materialized-cells                  $MaterializedCells `
    --material-palette                    $MaterialPalette `
    --layer-stack                         $LayerStack `
    --materialization-replay-log          $MaterializationReplayLog `
    --materialization-ownership-summary   $MaterializationOwnershipSummary `
    --materializer-forbidden-output-guard $MaterializerForbiddenOutputGuard `
    --output-root                         $OutputRoot `
    --output-json                         $OutputJson `
    --output-md                           $OutputMd `
    --output-csv                          $OutputCsv `
    --summary                             $OutputSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-27D CLI exited with code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# ── forbidden artifact scan ────────────────────────────────────────────────────

$forbidden = @("*.lua", "media/maps")
foreach ($pattern in $forbidden) {
    $hits = @(Get-ChildItem -Path $OutputRoot -Filter $pattern -Recurse -ErrorAction SilentlyContinue)
    if ($hits.Count -gt 0) {
        Write-Error "FORBIDDEN ARTIFACT FOUND matching '$pattern': $($hits[0].FullName)"
        exit 1
    }
}

# ── report ─────────────────────────────────────────────────────────────────────

if (Test-Path $OutputSummary) { Get-Content $OutputSummary }
Write-Host ""
Write-Host "MAP-27D: DONE. Output root: $OutputRoot"
