Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$adapterContractRoot = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-adapter-contract\map_00"
$adapterContract     = Join-Path $adapterContractRoot "map_00.minimal_concrete_geometry_writer_adapter_contract.json"

$outputRoot    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-v0\map_00"
$outputJson    = Join-Path $outputRoot "map_00.minimal_concrete_geometry_sandbox_writer_v0.json"
$outputMd      = Join-Path $outputRoot "map_00.minimal_concrete_geometry_sandbox_writer_v0.md"
$outputCsv     = Join-Path $outputRoot "map_00.minimal_concrete_geometry_sandbox_writer_v0.csv"
$outputSummary = Join-Path $outputRoot "map_00.minimal_concrete_geometry_sandbox_writer_v0.summary.txt"

if (-not (Test-Path $adapterContract)) {
    Write-Error "MAP-26I adapter contract not found: $adapterContract"
    Write-Error "Run run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-adapter-contract.ps1 first."
    exit 1
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0 `
    --adapter-contract $adapterContract `
    --output-root      $outputRoot `
    --output-json      $outputJson `
    --output-md        $outputMd `
    --output-csv       $outputCsv `
    --summary          $outputSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "sandbox-writer-v0 command failed with exit code $LASTEXITCODE"
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
Write-Host "=== Operation files ==="
$opFiles = @(
    "map_00.sandbox_writer_component_operations.json",
    "map_00.sandbox_writer_lot_operations.json",
    "map_00.sandbox_writer_building_slot_operations.json",
    "map_00.sandbox_writer_access_operations.json",
    "map_00.sandbox_writer_forbidden_output_guard.json"
)
foreach ($name in $opFiles) {
    $f = Join-Path $outputRoot $name
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
