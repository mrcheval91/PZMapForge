#Requires -Version 5.1
<#
.SYNOPSIS
    Tests the MAP-9C IsoMetaGrid registration packet.
    Runs prepare-build42-map9c-isometagrid-registration-packet.ps1 against a temp .local/ path
    and asserts 30 contract requirements.
    Exits 0 if all pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$prepScript = Join-Path $repoRoot 'scripts\prepare-build42-map9c-isometagrid-registration-packet.ps1'
$map9cDoc   = Join-Path $repoRoot 'docs\MAP_9C_ISOMETAGRID_MAP_FOLDER_REGISTRATION.md'

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
$outside = Join-Path $repoRoot 'scripts\map9c-pkt-guard-test'
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $prepScript -Output $outside 2>$null
$ecGuard = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ecGuard -ne 0) "Prep script refuses output outside .local/"
if (Test-Path $outside) { Remove-Item -Recurse -Force $outside }

# ---------------------------------------------------------------------------
# Tests 2-3: Scripts and doc exist
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 2-3: Scripts and doc exist ---"
Assert-True (Test-Path $prepScript -PathType Leaf) "prepare-build42-map9c-isometagrid-registration-packet.ps1 exists"
Assert-True (Test-Path $map9cDoc   -PathType Leaf) "docs/MAP_9C_ISOMETAGRID_MAP_FOLDER_REGISTRATION.md exists"

# ---------------------------------------------------------------------------
# Tests 4-8: Output files
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 4-8: Output files ---"
$testOutput = Join-Path $repoRoot '.local\map9c-pkt-test'
if (Test-Path $testOutput) { Remove-Item -Recurse -Force $testOutput }
& powershell -ExecutionPolicy Bypass -File $prepScript -Output $testOutput
Assert-True ($LASTEXITCODE -eq 0) "Prep script exits 0 on valid .local/ output"

$jsonPath   = Join-Path $testOutput 'map9c-isometagrid-registration-packet.json'
$mdPath     = Join-Path $testOutput 'map9c-isometagrid-registration-packet.md'
$overlayDoc = Join-Path $testOutput 'MAP_9C_ISOMETAGRID_REGISTRATION_PACKET.md'
$humanSteps = Join-Path $testOutput 'MAP_9C_HUMAN_RUNTIME_STEPS.md'
$variantsDir = Join-Path $testOutput 'variants'
Assert-True (Test-Path $jsonPath    -PathType Leaf) "map9c-isometagrid-registration-packet.json exists"
Assert-True (Test-Path $mdPath      -PathType Leaf) "map9c-isometagrid-registration-packet.md exists"
Assert-True (Test-Path $overlayDoc  -PathType Leaf) "MAP_9C_ISOMETAGRID_REGISTRATION_PACKET.md exists"
Assert-True (Test-Path $humanSteps  -PathType Leaf) "MAP_9C_HUMAN_RUNTIME_STEPS.md exists"

# ---------------------------------------------------------------------------
# Tests 9-10: Variant directories
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 9-10: Variant directories ---"
Assert-True (Test-Path $variantsDir -PathType Container) "variants/ directory exists"
$variantManifests = Get-ChildItem -Path $variantsDir -Recurse -Filter 'variant-manifest.json' -ErrorAction SilentlyContinue
Assert-True ($variantManifests.Count -ge 5) "At least 5 variant manifests produced"

# ---------------------------------------------------------------------------
# Tests 11-18: Packet JSON safety and evidence fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 11-18: Packet JSON safety and evidence fields ---"
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

Assert-True ($p.schema -eq 'pzmapforge.map9c-isometagrid-registration-packet.v0.1') `
    "schema == 'pzmapforge.map9c-isometagrid-registration-packet.v0.1'"
Assert-True ($p.map9b_debug_runtime_evidence_recorded -eq $true) "map9b_debug_runtime_evidence_recorded == true"
Assert-True ($p.mod_load_confirmed -eq $true) "mod_load_confirmed == true"
Assert-True ($p.workshop_runtime_cache_confirmed -eq $true) "workshop_runtime_cache_confirmed == true"
Assert-True ($p.spawn_metadata_works -eq $true) "spawn_metadata_works == true"
Assert-True ($p.isometagrid_map_folder_list_empty -eq $true) "isometagrid_map_folder_list_empty == true"
Assert-True ($p.map_folder_registration_unproven -eq $true) "map_folder_registration_unproven == true"
Assert-True ($p.playable_terrain_mount_proven -eq $false) "playable_terrain_mount_proven == false"

# ---------------------------------------------------------------------------
# Tests 19-22: Canary and Muldraugh state
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 19-22: Canary and Muldraugh state ---"
Assert-True ($p.canary_writer_available -eq $false) "canary_writer_available == false"
Assert-True ($p.canary_writer_blocked -eq $true) "canary_writer_blocked == true"
Assert-True ($p.muldraugh_bootstrap_required -eq $true) "muldraugh_bootstrap_required == true"
Assert-True ($p.no_muldraugh_strategy_rejected -eq $true) "no_muldraugh_strategy_rejected == true"

# ---------------------------------------------------------------------------
# Tests 23-26: Probe goal and signals
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 23-26: Probe goal and signals ---"
Assert-True ($p.registration_probe_goal -match 'IsoMetaGrid') "registration_probe_goal contains 'IsoMetaGrid'"
Assert-True ($p.success_signal -match 'IsoMetaGrid') "success_signal contains 'IsoMetaGrid'"
Assert-True (-not [string]::IsNullOrWhiteSpace($p.failure_signal)) "failure_signal is non-empty"
Assert-True ($p.test_one_variant_at_a_time -eq $true) "test_one_variant_at_a_time == true"

# ---------------------------------------------------------------------------
# Tests 27-30: Hard safety gates
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 27-30: Hard safety gates ---"
Assert-True ($p.claude_ran_pz -eq $false) "claude_ran_pz == false"
Assert-True ($p.claude_wrote_steam -eq $false) "claude_wrote_steam == false"
Assert-True ($p.claude_uploaded_workshop -eq $false) "claude_uploaded_workshop == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
