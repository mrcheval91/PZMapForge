Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$geometryMvp    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.json"
$overlayJson    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.json"
$overlayCsv     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.csv"
$overlayPng     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.png"

$outDir = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$outJson    = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_review_packet.json"
$outMd      = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_review_packet.md"
$outCsv     = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_review_packet.csv"
$outSummary = Join-Path $outDir "map_00.minimal_concrete_geometry_qa_review_packet.summary.txt"

$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet `
    --geometry-mvp    $geometryMvp `
    --qa-overlay-json $overlayJson `
    --qa-overlay-csv  $overlayCsv `
    --qa-overlay-png  $overlayPng `
    --output-json     $outJson `
    --output-md       $outMd `
    --output-csv      $outCsv `
    --summary         $outSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet exited $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Output written to: $outDir"

foreach ($f in @($outJson, $outMd, $outCsv, $outSummary)) {
    if (Test-Path $f) {
        Write-Host "EXISTS: $f"
    } else {
        Write-Error "MISSING: $f"
        exit 1
    }
}

Get-Content $outSummary
