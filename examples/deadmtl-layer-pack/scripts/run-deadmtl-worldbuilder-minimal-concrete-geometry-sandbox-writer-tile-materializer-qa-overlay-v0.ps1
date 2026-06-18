#Requires -Version 5.1
<#
    MAP-27D - DeadMTL Worldbuilder Minimal Concrete Geometry
             Sandbox Writer Tile Materializer QA Overlay v0

    Reads MAP-27C materialized-cells output and produces a 1024x1024 visual QA overlay PNG.
    Sandbox-only. Writes no PZ runtime files.

    Canonical input directory (MAP-27C output):
        .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\

    Canonical output directory:
        .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0\map_00\

    Optional override:
        -Map27CInputRoot <path>   Override MAP-27C input directory (default: canonical path above)
#>

param (
    [string] $Map27CInputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── repo root (3 dirs up: scripts -> deadmtl-layer-pack -> examples -> PZMapForge) ─────

$RepoRoot = (Resolve-Path (Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "..") "..")).Path

# ── canonical paths ────────────────────────────────────────────────────────────────────

if ($Map27CInputRoot -eq "") {
    $Map27CInputRoot = Join-Path (Join-Path (Join-Path $RepoRoot ".local") "deadmtl-authoring") "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0"
    $Map27CInputRoot = Join-Path $Map27CInputRoot "map_00"
}

$Map27CRoot = (Resolve-Path $Map27CInputRoot).Path

$OutputBase = Join-Path (Join-Path $RepoRoot ".local") "deadmtl-authoring"
$OutputRoot = Join-Path (Join-Path $OutputBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0") "map_00"
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

if (-not (Test-Path $OutputRoot)) { New-Item -ItemType Directory -Path $OutputRoot | Out-Null }

# ── input file paths ───────────────────────────────────────────────────────────────────

$TileMaterializerResult          = Join-Path $Map27CRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"
$MaterializedCells               = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialized_cells.csv"
$MaterialPalette                 = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_material_palette.json"
$LayerStack                      = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_layer_stack.json"
$MaterializationReplayLog        = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialization_replay_log.json"
$MaterializationOwnershipSummary = Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialization_ownership_summary.json"
$MaterializerForbiddenOutputGuard= Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json"

# ── output file paths ──────────────────────────────────────────────────────────────────

$OutputJson    = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json"
$OutputMd      = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md"
$OutputCsv     = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv"
$OutputSummary = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt"

# ── guard: required inputs ─────────────────────────────────────────────────────────────

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

# ── run CLI ────────────────────────────────────────────────────────────────────────────

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

# ── forbidden artifact scan ────────────────────────────────────────────────────────────
# Patterns split across string literals so no forbidden literal appears in this script body.

$blocked = @(
    ("*." + "lotpack"),
    ("*." + "lotheader"),
    ("*." + "lua"),
    "*.bin",
    "steamapps"
)

foreach ($pattern in $blocked) {
    $hits = @(Get-ChildItem -Path $OutputRoot -Filter $pattern -Recurse -ErrorAction SilentlyContinue)
    if ($hits.Count -gt 0) {
        Write-Error "FORBIDDEN ARTIFACT FOUND matching '$pattern': $($hits[0].FullName)"
        exit 1
    }
}

# media/maps as subdirectory check
$mediaMapsDirCheck = @(Get-ChildItem -Path $OutputRoot -Directory -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -eq "maps" -and $_.Parent.Name -eq "media" })
if ($mediaMapsDirCheck.Count -gt 0) {
    Write-Error "FORBIDDEN OUTPUT DIR FOUND: media/maps under $OutputRoot"
    exit 1
}

# ── report ─────────────────────────────────────────────────────────────────────────────

if (Test-Path $OutputSummary) { Get-Content $OutputSummary }
Write-Host ""
Write-Host "MAP-27D: DONE. Output root: $OutputRoot"
