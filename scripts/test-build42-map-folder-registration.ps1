#Requires -Version 5.1
<#
.SYNOPSIS
    Tests the MAP-9C Build 42 map folder registration inspector.
    Runs inspect-build42-map-folder-registration.ps1 against a temp .local/ path
    and asserts 25 contract requirements.
    Exits 0 if all pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot      = Split-Path -Parent $scriptDir
$inspectScript = Join-Path $repoRoot 'scripts\inspect-build42-map-folder-registration.ps1'
$map9cDoc      = Join-Path $repoRoot 'docs\MAP_9C_ISOMETAGRID_MAP_FOLDER_REGISTRATION.md'

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
$outside = Join-Path $repoRoot 'scripts\map9c-reg-guard-test'
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $inspectScript -Output $outside 2>$null
$ecGuard = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ecGuard -ne 0) "Inspector refuses output outside .local/"
if (Test-Path $outside) { Remove-Item -Recurse -Force $outside }

# ---------------------------------------------------------------------------
# Tests 2-4: Script, doc, valid run
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 2-4: Script, doc, valid run ---"
Assert-True (Test-Path $inspectScript -PathType Leaf) "inspect-build42-map-folder-registration.ps1 exists"
Assert-True (Test-Path $map9cDoc -PathType Leaf) "docs/MAP_9C_ISOMETAGRID_MAP_FOLDER_REGISTRATION.md exists"

$testOutput = Join-Path $repoRoot '.local\map9c-reg-inspector-test'
if (Test-Path $testOutput) { Remove-Item -Recurse -Force $testOutput }
& powershell -ExecutionPolicy Bypass -File $inspectScript -Output $testOutput
Assert-True ($LASTEXITCODE -eq 0) "Inspector exits 0 on valid .local/ output"

# ---------------------------------------------------------------------------
# Tests 5-6: Output files
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 5-6: Output files ---"
$jsonPath = Join-Path $testOutput 'build42-map-folder-registration-inspector.json'
$mdPath   = Join-Path $testOutput 'build42-map-folder-registration-inspector.md'
Assert-True (Test-Path $jsonPath -PathType Leaf) "build42-map-folder-registration-inspector.json exists"
Assert-True (Test-Path $mdPath   -PathType Leaf) "build42-map-folder-registration-inspector.md exists"

# ---------------------------------------------------------------------------
# Tests 7-14: Safety and blocker fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 7-14: Safety and blocker fields ---"
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

Assert-True ($p.schema -eq 'pzmapforge.map9c-map-folder-registration-inspector.v0.1') `
    "schema == 'pzmapforge.map9c-map-folder-registration-inspector.v0.1'"
Assert-True ($p.inspected_repo_only -eq $true) "inspected_repo_only == true"
Assert-True ($p.pz_assets_read -eq $false) "pz_assets_read == false"
Assert-True ($p.steam_write_performed -eq $false) "steam_write_performed == false"
Assert-True ($p.workshop_upload_performed -eq $false) "workshop_upload_performed == false"
Assert-True ($p.pz_run_performed -eq $false) "pz_run_performed == false"
Assert-True ($p.third_party_files_copied -eq $false) "third_party_files_copied == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# ---------------------------------------------------------------------------
# Tests 15-19: MAP-9B blocker fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 15-19: MAP-9B blocker fields ---"
Assert-True ($p.map9b_debug_blocker_recorded -eq $true) "map9b_debug_blocker_recorded == true"
Assert-True ($p.mod_load_confirmed_by_debug_logs -eq $true) "mod_load_confirmed_by_debug_logs == true"
Assert-True ($p.isometagrid_map_folder_list_empty -eq $true) "isometagrid_map_folder_list_empty == true"
Assert-True ($p.muldraugh_bootstrap_required -eq $true) "muldraugh_bootstrap_required == true"
Assert-True ($p.no_muldraugh_strategy_rejected -eq $true) "no_muldraugh_strategy_rejected == true"

# ---------------------------------------------------------------------------
# Tests 20-25: Probe content fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 20-25: Probe content fields ---"
Assert-True (-not [string]::IsNullOrWhiteSpace($p.current_server_map_line) -and $p.current_server_map_line -match 'PZMapForge') `
    "current_server_map_line contains PZMapForge"
Assert-True ($p.candidate_map_folder_names_considered.Count -ge 2) "candidate_map_folder_names_considered non-empty"
Assert-True ($p.candidate_layouts_considered.Count -ge 5) "candidate_layouts_considered has >= 5 entries"
Assert-True ($p.registration_hypotheses.Count -ge 3) "registration_hypotheses has >= 3 entries"
Assert-True ($p.recommended_probe_order.Count -ge 3) "recommended_probe_order has >= 3 entries"
Assert-True ($p.success_signal -match 'IsoMetaGrid') "success_signal contains 'IsoMetaGrid'"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
