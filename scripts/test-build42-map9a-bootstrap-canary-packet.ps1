#Requires -Version 5.1
<#
.SYNOPSIS
    Tests the MAP-9A Muldraugh bootstrap canary overlay packet.
    Runs prepare-build42-map9a-bootstrap-canary-packet.ps1 against a temp .local/ path
    and asserts 25 contract requirements.
    Exits 0 if all pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir
$prepScript  = Join-Path $repoRoot 'scripts\prepare-build42-map9a-bootstrap-canary-packet.ps1'
$map8zDoc    = Join-Path $repoRoot 'docs\MAP_8Z_RUNTIME_FALLBACK_RESULT.md'
$map9aDoc    = Join-Path $repoRoot 'docs\MAP_9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY.md'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# ---------------------------------------------------------------------------
# Test 1: .local guard - refuses output outside .local/
# ---------------------------------------------------------------------------

Write-Output "--- Test 1: .local guard ---"
$outside = Join-Path $repoRoot 'scripts\map9a-guard-test-output'
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $prepScript -Output $outside 2>$null
$ecGuard = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ecGuard -ne 0) "Prep script refuses output outside .local/"
if (Test-Path $outside) { Remove-Item -Recurse -Force $outside }

# ---------------------------------------------------------------------------
# Tests 2-6: Output files exist after valid run
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 2-6: Output files ---"
$testOutput = Join-Path $repoRoot '.local\map9a-test'
if (Test-Path $testOutput) { Remove-Item -Recurse -Force $testOutput }

& powershell -ExecutionPolicy Bypass -File $prepScript -Output $testOutput
Assert-True ($LASTEXITCODE -eq 0) "Prep script exits 0 on valid .local/ output"

$jsonPath  = Join-Path $testOutput 'map9a-bootstrap-canary-packet.json'
$mdPath    = Join-Path $testOutput 'map9a-bootstrap-canary-packet.md'
$overlayDoc = Join-Path $testOutput 'MAP_9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_PACKET.md'

Assert-True (Test-Path $jsonPath  -PathType Leaf) "map9a-bootstrap-canary-packet.json exists"
Assert-True (Test-Path $mdPath    -PathType Leaf) "map9a-bootstrap-canary-packet.md exists"
Assert-True (Test-Path $overlayDoc -PathType Leaf) "MAP_9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY_PACKET.md exists"

# ---------------------------------------------------------------------------
# Tests 7-8: Repo docs exist
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 7-8: Repo docs ---"
Assert-True (Test-Path $map8zDoc -PathType Leaf) "docs/MAP_8Z_RUNTIME_FALLBACK_RESULT.md exists"
Assert-True (Test-Path $map9aDoc -PathType Leaf) "docs/MAP_9A_MULDRAUGH_BOOTSTRAP_CANARY_OVERLAY.md exists"

# ---------------------------------------------------------------------------
# Tests 9-25: Packet JSON fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 9-25: Packet JSON fields ---"
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

Assert-True ($p.schema -eq 'pzmapforge.map9a-bootstrap-canary-packet.v0.1') `
    "schema == 'pzmapforge.map9a-bootstrap-canary-packet.v0.1'"

Assert-True ($p.map8z_result_recorded -eq $true) `
    "map8z_result_recorded == true"

Assert-True ($p.no_muldraugh_strategy_rejected -eq $true) `
    "no_muldraugh_strategy_rejected == true"

Assert-True ($p.muldraugh_bootstrap_required -eq $true) `
    "muldraugh_bootstrap_required == true"

Assert-True ($p.server_map_line -like '*Muldraugh*') `
    "server_map_line contains Muldraugh"

$mapLineParts = $p.server_map_line -split ';'
Assert-True ($mapLineParts[-1].Trim() -eq 'Muldraugh, KY') `
    "server_map_line ends with 'Muldraugh, KY' as last map"

Assert-True ($p.fresh_world_required -eq $true) `
    "fresh_world_required == true"

Assert-True ($p.canary_required -eq $true) `
    "canary_required == true"

Assert-True ($null -ne $p.PSObject.Properties['canary_writer_available']) `
    "canary_writer_available field is explicit"

Assert-True ($null -ne $p.PSObject.Properties['canary_writer_blocked']) `
    "canary_writer_blocked field is explicit"

Assert-True ($p.playable_claim_allowed -eq $false) `
    "playable_claim_allowed == false"

Assert-True ($p.pz_run_performed -eq $false) `
    "pz_run_performed == false"

Assert-True ($p.workshop_upload_performed -eq $false) `
    "workshop_upload_performed == false"

Assert-True ($p.steam_write_performed -eq $false) `
    "steam_write_performed == false"

Assert-True ($p.third_party_files_copied -eq $false) `
    "third_party_files_copied == false"

Assert-True ($p.staged_output_local_only -eq $true) `
    "staged_output_local_only == true"

Assert-True (-not [string]::IsNullOrWhiteSpace($p.success_signal)) `
    "success_signal is present and non-empty"

Assert-True (-not [string]::IsNullOrWhiteSpace($p.failure_signal)) `
    "failure_signal is present and non-empty"

Assert-True ($p.next_branch -eq 'map9a_human_runtime_test_pending') `
    "next_branch == 'map9a_human_runtime_test_pending'"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
