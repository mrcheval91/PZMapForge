Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$scopeRecord = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record\map_00\map_00.minimal_concrete_geometry_writer_experiment_scope_record.json"
$manifest    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00\map_00.minimal_concrete_geometry_writer_input_manifest.json"
$geometryMvp = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.json"

$outDir  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-design\map_00"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$outJson    = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_dry_run_design.json"
$outMd      = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_dry_run_design.md"
$outCsv     = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_dry_run_design.csv"
$outSummary = Join-Path $outDir "map_00.minimal_concrete_geometry_writer_dry_run_design.summary.txt"

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-design `
    --scope-record          $scopeRecord `
    --writer-input-manifest $manifest `
    --geometry-mvp          $geometryMvp `
    --output-json           $outJson `
    --output-md             $outMd `
    --output-csv            $outCsv `
    --summary               $outSummary

Write-Host ""
Write-Host "=== Output files ==="
foreach ($f in @($outJson, $outMd, $outCsv, $outSummary)) {
    if (Test-Path $f) { Write-Host "EXISTS: $f" }
    else              { Write-Host "MISSING: $f"; exit 1 }
}

Write-Host ""
Write-Host "=== Summary ==="
Get-Content $outSummary
