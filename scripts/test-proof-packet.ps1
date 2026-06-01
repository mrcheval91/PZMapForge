#Requires -Version 5.1
<#
.SYNOPSIS
    Validates .local/mapforge/proof-packet.json against the v0.9 proof-packet contract.

    Runs write-proof-packet.ps1 first if proof-packet.json does not exist.
    Exits 0 if all checks pass, exits 1 if any fail.
    Does not commit .local/. Does not touch media/maps.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$outputDir   = Join-Path $repoRoot '.local\mapforge'
$packetJson  = Join-Path $outputDir 'proof-packet.json'
$packetMd    = Join-Path $outputDir 'proof-packet.md'
$writeScript = Join-Path $repoRoot 'scripts\write-proof-packet.ps1'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# ---------------------------------------------------------------------------
# Ensure proof packet exists
# ---------------------------------------------------------------------------

if (-not (Test-Path $packetJson -PathType Leaf)) {
    Write-Output "proof-packet.json not found. Running write-proof-packet.ps1..."
    & powershell -ExecutionPolicy Bypass -File $writeScript
    if ($LASTEXITCODE -ne 0) { Write-Error "write-proof-packet.ps1 failed."; exit 1 }
}

Write-Output "Proof packet validation: $packetJson"
Write-Output ""

# ---------------------------------------------------------------------------
# Output files present
# ---------------------------------------------------------------------------

Write-Output "--- Output files ---"
Assert-True (Test-Path $packetJson -PathType Leaf) "proof-packet.json exists"
Assert-True (Test-Path $packetMd   -PathType Leaf) "proof-packet.md exists"

# ---------------------------------------------------------------------------
# Parse
# ---------------------------------------------------------------------------

$p = Get-Content $packetJson -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Required top-level fields (v0.3 adds primitive artifact fields)
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Required fields ---"
$requiredFields = @(
    'schema', 'generated_at_utc', 'repo_root',
    'git_branch', 'git_commit', 'git_status_short',
    'parsed_cell_path', 'parsed_cell_sha256',
    'report_sha256', 'preview_sha256', 'tiles_sha256', 'tmx_sha256',
    'regions_json_path', 'regions_report_path',
    'regions_json_sha256', 'regions_report_sha256',
    'primitives_json_path', 'primitives_report_path',
    'primitives_json_sha256', 'primitives_report_sha256',
    'plan_recommendations_path', 'plan_report_path',
    'plan_recommendations_sha256', 'plan_report_sha256',
    'claim_boundary', 'validation_summary', 'safety'
)
foreach ($field in $requiredFields) {
    Assert-True ($null -ne $p.PSObject.Properties[$field]) "Field '$field' present"
}

# ---------------------------------------------------------------------------
# Sentinels
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Sentinels ---"
Assert-True ($p.schema -eq 'pzmapforge.proof-packet.v0.9') `
    "schema == 'pzmapforge.proof-packet.v0.9' (got '$($p.schema)')"
Assert-True ($p.claim_boundary -eq 'planning_artifact_only_not_pz_load_tested') `
    "claim_boundary == 'planning_artifact_only_not_pz_load_tested'"

# ---------------------------------------------------------------------------
# SHA-256 format (64 lowercase hex chars)
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- SHA-256 hashes ---"
$shaFields = @(
    'parsed_cell_sha256', 'report_sha256', 'preview_sha256',
    'tiles_sha256', 'tmx_sha256',
    'regions_json_sha256', 'regions_report_sha256',
    'primitives_json_sha256', 'primitives_report_sha256',
    'plan_recommendations_sha256', 'plan_report_sha256'
)
foreach ($field in $shaFields) {
    $val = [string]$p.PSObject.Properties[$field].Value
    Assert-True ($val -match '^[0-9a-f]{64}$') "$field is 64-char lowercase hex"
}

# ---------------------------------------------------------------------------
# Validation summary counts
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Validation summary ---"
Assert-True ([int]$p.validation_summary.schema_file_sanity          -eq 134) "schema_file_sanity == 134"
Assert-True ([int]$p.validation_summary.artifact_contract           -eq 40)  "artifact_contract == 40"
Assert-True ([int]$p.validation_summary.palette_sha256_verification -eq 5)   "palette_sha256_verification == 5"
Assert-True ([int]$p.validation_summary.tmx_integrity               -eq 21)  "tmx_integrity == 21"
Assert-True ([int]$p.validation_summary.hardening_harness           -eq 36)  "hardening_harness == 36"
Assert-True ([int]$p.validation_summary.region_extraction           -eq 24)  "region_extraction == 24"
Assert-True ([int]$p.validation_summary.primitive_classification      -eq 22)  "primitive_classification == 22"
Assert-True ([int]$p.validation_summary.plan_recommendations_contract -eq 28)  "plan_recommendations_contract == 28"
Assert-True ([int]$p.validation_summary.total_expected_assertions     -eq 365) "total_expected_assertions == 365"

# ---------------------------------------------------------------------------
# Safety flags
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Safety flags ---"
Assert-True ($p.safety.local_only_outputs      -eq $true)  "local_only_outputs == true"
Assert-True ($p.safety.media_maps_touched      -eq $false) "media_maps_touched == false"
Assert-True ($p.safety.pz_assets_copied        -eq $false) "pz_assets_copied == false"
Assert-True ($p.safety.playable_export_claimed -eq $false) "playable_export_claimed == false"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
