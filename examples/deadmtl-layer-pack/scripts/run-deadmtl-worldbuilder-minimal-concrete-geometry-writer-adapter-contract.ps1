Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$cliProject = Join-Path $repoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$emitterOutputRoot = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00"
$auditReceiptRoot  = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt\map_00"

$auditReceipt     = Join-Path $auditReceiptRoot "map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.json"
$componentRecord  = Join-Path $emitterOutputRoot "map_00.component_writer_record.json"
$lotRecords       = Join-Path $emitterOutputRoot "map_00.lot_writer_records.json"
$slotRecords      = Join-Path $emitterOutputRoot "map_00.building_slot_writer_records.json"
$frontageAccess   = Join-Path $emitterOutputRoot "map_00.frontage_access_record.json"
$rearAccess       = Join-Path $emitterOutputRoot "map_00.rear_service_access_record.json"
$claimBoundary    = Join-Path $emitterOutputRoot "map_00.claim_boundary_record.json"

$outputRoot    = Join-Path $repoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-adapter-contract\map_00"
$outputJson    = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_adapter_contract.json"
$outputMd      = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_adapter_contract.md"
$outputCsv     = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_adapter_contract.csv"
$outputSummary = Join-Path $outputRoot "map_00.minimal_concrete_geometry_writer_adapter_contract.summary.txt"

if (-not (Test-Path $auditReceipt)) {
    Write-Error "MAP-26H audit receipt not found: $auditReceipt"
    Write-Error "Run run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt.ps1 first."
    exit 1
}

foreach ($f in @($componentRecord, $lotRecords, $slotRecords, $frontageAccess, $rearAccess, $claimBoundary)) {
    if (-not (Test-Path $f)) {
        Write-Error "Required input file not found: $f"
        Write-Error "Run run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter.ps1 first."
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

dotnet run --project $cliProject -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-adapter-contract `
    --audit-receipt                $auditReceipt `
    --component-record             $componentRecord `
    --lot-records                  $lotRecords `
    --building-slot-records        $slotRecords `
    --frontage-access-record       $frontageAccess `
    --rear-service-access-record   $rearAccess `
    --claim-boundary-record        $claimBoundary `
    --output-root                  $outputRoot `
    --output-json                  $outputJson `
    --output-md                    $outputMd `
    --output-csv                   $outputCsv `
    --summary                      $outputSummary

if ($LASTEXITCODE -ne 0) {
    Write-Error "adapter-contract command failed with exit code $LASTEXITCODE"
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
