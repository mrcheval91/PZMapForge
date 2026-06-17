Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent

$manifestJson    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00\map_00.minimal_concrete_geometry_writer_input_manifest.json"
$manifestSummary = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00\map_00.minimal_concrete_geometry_writer_input_manifest.summary.txt"

$outDir     = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record\map_00"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$outJson    = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_experiment_scope_record.json"
$outMd      = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_experiment_scope_record.md"
$outCsv     = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_experiment_scope_record.csv"
$outSummary = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_experiment_scope_record.summary.txt"

$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record `
    --writer-input-manifest         $manifestJson `
    --writer-input-manifest-summary $manifestSummary `
    --output-json                   $outJson `
    --output-md                     $outMd `
    --output-csv                    $outCsv `
    --summary                       $outSummary

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
