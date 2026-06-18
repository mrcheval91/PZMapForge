Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$emitterOutputRoot = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00"
$emitterResult     = Join-Path $emitterOutputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emitter.json"

$outputRoot    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt\map_00"
$outputJson    = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.json"
$outputMd      = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.md"
$outputCsv     = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.csv"
$outputSummary = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.summary.txt"

if (-not (Test-Path $emitterResult)) {
    Write-Error "MAP-26G emitter result not found: $emitterResult"
    Write-Error "Run run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter.ps1 first."
    exit 1
}

if (-not (Test-Path $emitterOutputRoot)) {
    Write-Error "MAP-26G emitter output root not found: $emitterOutputRoot"
    exit 1
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt `
    --emitter-result      $emitterResult `
    --emitter-output-root $emitterOutputRoot `
    --output-root         $outputRoot `
    --output-json         $outputJson `
    --output-md           $outputMd `
    --output-csv          $outputCsv `
    --summary             $outputSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "dry-run-emission-audit-receipt command failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "=== Output file existence ==="
$files = @($outputJson, $outputMd, $outputCsv, $outputSummary)
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
