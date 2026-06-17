Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$geometryMvp    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.json"
$overlayJson    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.json"
$overlayCsv     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.csv"
$overlayPng     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.png"
$reviewJson     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\map_00.minimal_concrete_geometry_qa_review_packet.json"
$reviewCsv      = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\map_00.minimal_concrete_geometry_qa_review_packet.csv"
$reviewMd       = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\map_00.minimal_concrete_geometry_qa_review_packet.md"
$reviewSummary  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\map_00.minimal_concrete_geometry_qa_review_packet.summary.txt"

$outDir     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$outJson    = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_input_manifest.json"
$outMd      = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_input_manifest.md"
$outCsv     = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_input_manifest.csv"
$outSummary = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_input_manifest.summary.txt"

$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-input-manifest `
    --geometry-mvp      $geometryMvp `
    --qa-overlay-json   $overlayJson `
    --qa-overlay-csv    $overlayCsv `
    --qa-overlay-png    $overlayPng `
    --qa-review-json    $reviewJson `
    --qa-review-csv     $reviewCsv `
    --qa-review-md      $reviewMd `
    --qa-review-summary $reviewSummary `
    --output-json       $outJson `
    --output-md         $outMd `
    --output-csv        $outCsv `
    --summary           $outSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "CLI exited $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Output files ==="
foreach ($f in @($outJson, $outMd, $outCsv, $outSummary)) {
    if (Test-Path $f) { Write-Host "EXISTS: $f" } else { Write-Error "MISSING: $f" }
}

Write-Host ""
Write-Host "=== Summary ==="
Get-Content $outSummary
