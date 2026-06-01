#Requires -Version 5.1
<#
.SYNOPSIS
    Full local validation for PZMapForge.
    Runs all PowerShell validation sub-scripts and finishes with a ledger
    summary. All sub-scripts must pass; exits nonzero on any failure.

    Final output reports the complete PowerShell validation lane total (381)
    and the .NET lane total (152) as separate evidence lanes.
    Counts are sourced from proof-packet v0.11 / docs/VALIDATION_LEDGER.md.
    Do not edit the constants below without also updating the proof packet
    schema and the validation ledger.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

Write-Output 'PZMapForge validate.ps1'
Write-Output "Root: $repoRoot"
Write-Output ""

# Happy-path smoke: generate sample image and run ImageMapForge against it
Write-Output "--- Smoke: sample image + ImageMapForge ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\new-test-image.ps1')
if ($LASTEXITCODE -ne 0) { throw "new-test-image.ps1 failed." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'source\image-mapforge.ps1') `
    -ImagePath (Join-Path $repoRoot '.local\mapforge\sample-input.png')
if ($LASTEXITCODE -ne 0) { throw "image-mapforge.ps1 failed." }

$required = @(
    '.local\mapforge\parsed-cell.json',
    '.local\mapforge\parsed-cell-report.md',
    '.local\mapforge\parsed-cell-preview.png',
    '.local\mapforge\parsed-cell-tiles.png',
    '.local\mapforge\parsed-cell-basic.tmx'
)

foreach ($relative in $required) {
    $path = Join-Path $repoRoot $relative
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing expected output: $relative"
    }
    Write-Output "OK: $relative"
}

$mediaMaps = Join-Path $repoRoot 'media\maps'
if (Test-Path -LiteralPath $mediaMaps) {
    $items = @(Get-ChildItem -LiteralPath $mediaMaps -Recurse -Force -ErrorAction SilentlyContinue)
    if ($items.Count -gt 0) {
        throw 'media/maps contains files. ImageMapForge must not write into media/maps.'
    }
}

Write-Output ""
Write-Output "--- Schema file sanity ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-schema-files.ps1')
if ($LASTEXITCODE -ne 0) { throw "Schema file sanity failed." }

Write-Output ""
Write-Output "--- Artifact contract validation ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-parsed-cell-contract.ps1')
if ($LASTEXITCODE -ne 0) { throw "Artifact contract validation failed." }

Write-Output ""
Write-Output "--- Palette SHA-256 verification ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-palette-sha256.ps1')
if ($LASTEXITCODE -ne 0) { throw "Palette SHA-256 verification failed." }

Write-Output ""
Write-Output "--- TMX integrity ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-tmx-integrity.ps1')
if ($LASTEXITCODE -ne 0) { throw "TMX integrity validation failed." }

Write-Output ""
Write-Output "--- Hardening test harness ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'tests\test-image-mapforge.ps1')
if ($LASTEXITCODE -ne 0) { throw "Hardening test harness failed." }

Write-Output ""
Write-Output "--- Restore sample artifacts for region extraction ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\new-test-image.ps1')
if ($LASTEXITCODE -ne 0) { throw "new-test-image.ps1 failed (restore)." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'source\image-mapforge.ps1') `
    -ImagePath (Join-Path $repoRoot '.local\mapforge\sample-input.png')
if ($LASTEXITCODE -ne 0) { throw "image-mapforge.ps1 failed (restore)." }

Write-Output ""
Write-Output "--- Region extraction ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\extract-regions.ps1')
if ($LASTEXITCODE -ne 0) { throw "extract-regions.ps1 failed." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-region-extraction.ps1')
if ($LASTEXITCODE -ne 0) { throw "Region extraction tests failed." }

Write-Output ""
Write-Output "--- Primitive classification ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\classify-primitives.ps1')
if ($LASTEXITCODE -ne 0) { throw "classify-primitives.ps1 failed." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-primitive-classification.ps1')
if ($LASTEXITCODE -ne 0) { throw "Primitive classification tests failed." }

Write-Output ""
Write-Output "--- Plan export ---"
& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- plan-export `
    --path (Join-Path $repoRoot '.local\mapforge\parsed-cell.json') `
    --output (Join-Path $repoRoot '.local\mapforge')
if ($LASTEXITCODE -ne 0) { throw "plan-export failed." }

Write-Output ""
Write-Output "--- Plan recommendations contract ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-plan-recommendations-contract.ps1')
if ($LASTEXITCODE -ne 0) { throw "Plan recommendations contract failed." }

Write-Output ""
Write-Output "--- Proof packet ---"
& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\write-proof-packet.ps1')
if ($LASTEXITCODE -ne 0) { throw "write-proof-packet.ps1 failed." }

& powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'scripts\test-proof-packet.ps1')
if ($LASTEXITCODE -ne 0) { throw "Proof packet validation failed." }

Write-Output ""
Write-Output "========================================"
Write-Output "PZMapForge validation summary"
Write-Output "========================================"

# ---------------------------------------------------------------------------
# Ledger constants — sourced from proof-packet v0.11 / docs/VALIDATION_LEDGER.md.
# Update here when counts change; update the proof packet schema and ledger too.
# ---------------------------------------------------------------------------

$psChecks = [ordered]@{
    'Schema file sanity'            = 136
    'Artifact contract'             = 40
    'Palette SHA-256 verification'  = 5
    'TMX integrity'                 = 21
    'Hardening harness'             = 36
    'Region extraction'             = 24
    'Primitive classification'      = 22
    'Plan recommendations contract' = 28
    'Proof packet'                  = 79
}
$psTotal = 391   # = validation_summary.total_expected_assertions in proof-packet v0.11

$dnCoreTests = 154   # PZMapForge.Core.Tests
$dnCliTests  = 30    # PZMapForge.Cli.Tests
$dnTotal     = 184   # = dotnet_validation_summary.test_total in proof-packet v0.11

Write-Output ""
Write-Output "  PowerShell lane  (validation_summary in proof-packet v0.11):"
foreach ($kv in $psChecks.GetEnumerator()) {
    Write-Output ("    {0,-34} {1,4}" -f "$($kv.Key):", $kv.Value)
}
Write-Output "    -------------------------------------- ----"
Write-Output ("    {0,-34} {1,4}" -f "Total:", $psTotal)

Write-Output ""
Write-Output "  .NET lane  (dotnet_validation_summary in proof-packet v0.11 -- tracked separately):"
Write-Output ("    {0,-34} {1,4}" -f "Core tests (PZMapForge.Core.Tests):", $dnCoreTests)
Write-Output ("    {0,-34} {1,4}" -f "CLI tests  (PZMapForge.Cli.Tests):", $dnCliTests)
Write-Output "    -------------------------------------- ----"
Write-Output ("    {0,-34} {1,4}" -f "Total:", $dnTotal)

Write-Output ""
Write-Output ("  PS {0} + .NET {1} = two separate evidence lanes, not summed." -f $psTotal, $dnTotal)
Write-Output "  Claim boundary: planning_artifact_only_not_pz_load_tested"
Write-Output ""
Write-Output "========================================"
Write-Output "Validation passed."
Write-Output "========================================"
