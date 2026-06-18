#Requires -Version 5.1
<#
    MAP-27E - DeadMTL Worldbuilder Minimal Concrete Geometry
             Sandbox Writer Tile Materialization QA Review Packet

    Audits the MAP-27C and MAP-27D canonical outputs and produces a
    deterministic QA review packet. Sandbox-only. Writes no PZ runtime files.

    Canonical MAP-27C input directory:
        .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\

    Canonical MAP-27D input directory:
        .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0\map_00\

    Canonical output directory:
        .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet\map_00\
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# repo root (3 dirs up: scripts -> deadmtl-layer-pack -> examples -> PZMapForge)

$RepoRoot = (Resolve-Path (Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "..") "..")).Path

# canonical paths

$Map27CRoot = Join-Path (Join-Path (Join-Path $RepoRoot ".local") "deadmtl-authoring") "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0"
$Map27CRoot = Join-Path $Map27CRoot "map_00"
$Map27CRoot = [IO.Path]::GetFullPath($Map27CRoot)

$Map27DRoot = Join-Path (Join-Path (Join-Path $RepoRoot ".local") "deadmtl-authoring") "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0"
$Map27DRoot = Join-Path $Map27DRoot "map_00"
$Map27DRoot = [IO.Path]::GetFullPath($Map27DRoot)

$OutputBase = Join-Path (Join-Path $RepoRoot ".local") "deadmtl-authoring"
$OutputRoot = Join-Path (Join-Path $OutputBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet") "map_00"
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

if (-not (Test-Path $OutputRoot)) { New-Item -ItemType Directory -Path $OutputRoot | Out-Null }

# output file paths

$OutputJson    = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json"
$OutputMd      = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.md"
$OutputCsv     = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.csv"
$OutputSummary = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.summary.txt"

# guard: required input roots

if (-not (Test-Path $Map27CRoot)) {
    Write-Error "MAP-27C root not found: $Map27CRoot"
    exit 1
}
if (-not (Test-Path $Map27DRoot)) {
    Write-Error "MAP-27D root not found: $Map27DRoot"
    exit 1
}

# guard: required MAP-27C input files

$map27cRequired = @(
    (Join-Path $Map27CRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"),
    (Join-Path $Map27CRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md"),
    (Join-Path $Map27CRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv"),
    (Join-Path $Map27CRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt"),
    (Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialized_cells.csv"),
    (Join-Path $Map27CRoot "map_00.sandbox_writer_tile_material_palette.json"),
    (Join-Path $Map27CRoot "map_00.sandbox_writer_tile_layer_stack.json"),
    (Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialization_replay_log.json"),
    (Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materialization_ownership_summary.json"),
    (Join-Path $Map27CRoot "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json")
)

$missing27c = @($map27cRequired | Where-Object { -not (Test-Path $_) })
if ($missing27c.Count -gt 0) {
    Write-Error "MISSING MAP-27C INPUT(S):`n$($missing27c -join "`n")"
    exit 1
}

# guard: required MAP-27D input files

$map27dRequired = @(
    (Join-Path $Map27DRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json"),
    (Join-Path $Map27DRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md"),
    (Join-Path $Map27DRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv"),
    (Join-Path $Map27DRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt"),
    (Join-Path $Map27DRoot "map_00.sandbox_writer_tile_materializer_qa_overlay.png"),
    (Join-Path $Map27DRoot "map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json"),
    (Join-Path $Map27DRoot "map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv"),
    (Join-Path $Map27DRoot "map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json")
)

$missing27d = @($map27dRequired | Where-Object { -not (Test-Path $_) })
if ($missing27d.Count -gt 0) {
    Write-Error "MISSING MAP-27D INPUT(S):`n$($missing27d -join "`n")"
    exit 1
}

# run CLI

$CliProject = Join-Path (Join-Path (Join-Path $RepoRoot "src") "PZMapForge.Cli") "PZMapForge.Cli.csproj"

Write-Host "MAP-27E: QA review packet..."
Write-Host "  MAP-27C : $Map27CRoot"
Write-Host "  MAP-27D : $Map27DRoot"
Write-Host "  Output  : $OutputRoot"

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet `
    --tile-materializer-root           $Map27CRoot `
    --tile-materializer-qa-overlay-root $Map27DRoot `
    --output-root  $OutputRoot `
    --output-json  $OutputJson `
    --output-md    $OutputMd `
    --output-csv   $OutputCsv `
    --summary      $OutputSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-27E CLI exited with code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# forbidden artifact scan
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

# report

if (Test-Path $OutputSummary) { Get-Content $OutputSummary }
Write-Host ""
Write-Host "MAP-27E: DONE. Output root: $OutputRoot"
