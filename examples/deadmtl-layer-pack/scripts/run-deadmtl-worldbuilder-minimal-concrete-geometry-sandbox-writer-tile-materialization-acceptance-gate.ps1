#Requires -Version 5.1
<#
    MAP-27F - DeadMTL Worldbuilder Minimal Concrete Geometry
             Sandbox Writer Tile Materialization Acceptance Gate

    Consumes the MAP-27E QA review packet and decides whether the sandbox
    tile materialization chain is acceptable as a future writer experiment input.
    Sandbox-only. Writes no PZ runtime files.

    Canonical MAP-27E input directory:
        .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet\map_00\

    Canonical output directory:
        .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate\map_00\
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# repo root (3 dirs up: scripts -> deadmtl-layer-pack -> examples -> PZMapForge)

$RepoRoot = (Resolve-Path (Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "..") "..")).Path

# canonical paths

$Map27ERoot = Join-Path (Join-Path (Join-Path $RepoRoot ".local") "deadmtl-authoring") "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet"
$Map27ERoot = Join-Path $Map27ERoot "map_00"
$Map27ERoot = [IO.Path]::GetFullPath($Map27ERoot)

$OutputBase = Join-Path (Join-Path $RepoRoot ".local") "deadmtl-authoring"
$OutputRoot = Join-Path (Join-Path $OutputBase "worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate") "map_00"
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

if (-not (Test-Path $OutputRoot)) { New-Item -ItemType Directory -Path $OutputRoot | Out-Null }

# output file paths

$OutputJson    = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"
$OutputMd      = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md"
$OutputCsv     = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv"
$OutputSummary = Join-Path $OutputRoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt"

# guard: required MAP-27E input root

if (-not (Test-Path $Map27ERoot)) {
    Write-Error "MAP-27E root not found: $Map27ERoot"
    exit 1
}

# guard: required MAP-27E input files

$map27eRequired = @(
    (Join-Path $Map27ERoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json"),
    (Join-Path $Map27ERoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.md"),
    (Join-Path $Map27ERoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.csv"),
    (Join-Path $Map27ERoot "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.summary.txt")
)

$missing27e = @($map27eRequired | Where-Object { -not (Test-Path $_) })
if ($missing27e.Count -gt 0) {
    Write-Error "MISSING MAP-27E INPUT(S):`n$($missing27e -join "`n")"
    exit 1
}

# run CLI

$CliProject = Join-Path (Join-Path (Join-Path $RepoRoot "src") "PZMapForge.Cli") "PZMapForge.Cli.csproj"

Write-Host "MAP-27F: Acceptance gate..."
Write-Host "  MAP-27E : $Map27ERoot"
Write-Host "  Output  : $OutputRoot"

dotnet run --project $CliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate `
    --qa-review-packet-root $Map27ERoot `
    --output-root  $OutputRoot `
    --output-json  $OutputJson `
    --output-md    $OutputMd `
    --output-csv   $OutputCsv `
    --summary      $OutputSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-27F CLI exited with code $LASTEXITCODE"
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
Write-Host "MAP-27F: DONE. Output root: $OutputRoot"
