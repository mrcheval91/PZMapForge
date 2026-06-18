Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$designInput = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-design\map_00\map_00.minimal_concrete_geometry_writer_dry_run_design.json"
$mvpInput    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.json"

$outputRoot  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00"
$outputJson  = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emitter.json"
$outputMd    = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emitter.md"
$outputCsv   = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emitter.csv"
$outputSummary = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emitter.summary.txt"

if (-not (Test-Path $designInput)) {
    Write-Error "MAP-26F dry-run design not found: $designInput"
    exit 1
}

if (-not (Test-Path $mvpInput)) {
    Write-Error "MAP-26A geometry MVP not found: $mvpInput"
    exit 1
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter `
    --dry-run-design   $designInput `
    --geometry-mvp     $mvpInput `
    --output-root      $outputRoot `
    --output-json      $outputJson `
    --output-md        $outputMd `
    --output-csv       $outputCsv `
    --summary          $outputSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "dry-run-emitter command failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "=== Output file existence ==="
$files = @(
    $outputJson,
    $outputMd,
    $outputCsv,
    $outputSummary,
    (Join-Path $outputRoot "map_00.component_writer_record.json"),
    (Join-Path $outputRoot "map_00.lot_writer_records.json"),
    (Join-Path $outputRoot "map_00.building_slot_writer_records.json"),
    (Join-Path $outputRoot "map_00.frontage_access_record.json"),
    (Join-Path $outputRoot "map_00.rear_service_access_record.json"),
    (Join-Path $outputRoot "map_00.forbidden_output_scan.json"),
    (Join-Path $outputRoot "map_00.rollback_record.json"),
    (Join-Path $outputRoot "map_00.claim_boundary_record.json")
)
foreach ($f in $files) {
    if (Test-Path $f) {
        Write-Host "EXISTS : $f"
    } else {
        Write-Error "MISSING: $f"
        exit 1
    }
}

Write-Host ""
Write-Host "=== Summary ==="
Get-Content $outputSummary
