#Requires -Version 5.1
<#
.SYNOPSIS
    Tests the MAP-9B canary writer unblock packet.
    Runs prepare-build42-map9b-canary-writer-unblock-packet.ps1 against a temp .local/ path
    and asserts 37 contract requirements.
    Exits 0 if all pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$prepScript = Join-Path $repoRoot 'scripts\prepare-build42-map9b-canary-writer-unblock-packet.ps1'
$map9bDoc   = Join-Path $repoRoot 'docs\MAP_9B_CANARY_WRITER_UNBLOCK.md'

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
$outside = Join-Path $repoRoot 'scripts\map9b-packet-guard-test'
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $prepScript -Output $outside 2>$null
$ecGuard = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ecGuard -ne 0) "Prep script refuses output outside .local/"
if (Test-Path $outside) { Remove-Item -Recurse -Force $outside }

# ---------------------------------------------------------------------------
# Tests 2-5: Output files exist after valid run
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 2-5: Output files ---"
$testOutput = Join-Path $repoRoot '.local\map9b-packet-test'
if (Test-Path $testOutput) { Remove-Item -Recurse -Force $testOutput }
& powershell -ExecutionPolicy Bypass -File $prepScript -Output $testOutput
Assert-True ($LASTEXITCODE -eq 0) "Prep script exits 0 on valid .local/ output"

$jsonPath   = Join-Path $testOutput 'map9b-canary-writer-unblock-packet.json'
$mdPath     = Join-Path $testOutput 'map9b-canary-writer-unblock-packet.md'
$overlayDoc = Join-Path $testOutput 'MAP_9B_CANARY_WRITER_UNBLOCK_PACKET.md'
Assert-True (Test-Path $jsonPath   -PathType Leaf) "map9b-canary-writer-unblock-packet.json exists"
Assert-True (Test-Path $mdPath     -PathType Leaf) "map9b-canary-writer-unblock-packet.md exists"
Assert-True (Test-Path $overlayDoc -PathType Leaf) "MAP_9B_CANARY_WRITER_UNBLOCK_PACKET.md exists"

# ---------------------------------------------------------------------------
# Test 6: MAP-9B repo doc exists
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Test 6: Repo doc ---"
Assert-True (Test-Path $map9bDoc -PathType Leaf) "docs/MAP_9B_CANARY_WRITER_UNBLOCK.md exists"

# ---------------------------------------------------------------------------
# Tests 7-22: Packet JSON fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 7-22: Packet JSON fields ---"
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

Assert-True ($p.schema -eq 'pzmapforge.map9b-canary-writer-unblock-packet.v0.1') `
    "schema == 'pzmapforge.map9b-canary-writer-unblock-packet.v0.1'"

Assert-True ($p.outcome -eq 'B') `
    "outcome == 'B'"

Assert-True ($null -ne $p.PSObject.Properties['canary_writer_available']) `
    "canary_writer_available field is explicit"

Assert-True ($null -ne $p.PSObject.Properties['canary_writer_blocked']) `
    "canary_writer_blocked field is explicit"

Assert-True ($p.canary_writer_available -eq $false) `
    "canary_writer_available == false"

Assert-True ($p.canary_writer_blocked -eq $true) `
    "canary_writer_blocked == true"

Assert-True ($p.visible_tile_encoding_supported -eq $false) `
    "visible_tile_encoding_supported == false"

Assert-True ($p.canary_strategy_available -eq $false) `
    "canary_strategy_available == false"

Assert-True ($p.inspected_repo_only -eq $true) `
    "inspected_repo_only == true"

Assert-True ($p.pz_assets_read -eq $false) `
    "pz_assets_read == false"

Assert-True ($p.lotp_chunk_payload_format_understood -eq $false) `
    "lotp_chunk_payload_format_understood == false"

Assert-True ($p.playable_claim_allowed -eq $false) `
    "playable_claim_allowed == false"

Assert-True ($p.pz_run_performed -eq $false) `
    "pz_run_performed == false"

Assert-True ($p.workshop_upload_performed -eq $false) `
    "workshop_upload_performed == false"

Assert-True ($p.staged_output_local_only -eq $true) `
    "staged_output_local_only == true"

Assert-True (-not [string]::IsNullOrWhiteSpace($p.next_research_branch)) `
    "next_research_branch is present and non-empty"

# ---------------------------------------------------------------------------
# Tests 23-36: Community claims triage and debug runtime evidence
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 23-37: Community claims triage and debug runtime evidence ---"
Assert-True ($p.community_claims_integrated_as_unverified_research_leads -eq $true) `
    "community_claims_integrated_as_unverified_research_leads == true"
Assert-True ($p.community_claims_not_adopted_as_doctrine -eq $true) `
    "community_claims_not_adopted_as_doctrine == true"
Assert-True ($p.measured_igmb_header_takes_precedence -eq $true) `
    "measured_igmb_header_takes_precedence == true"
Assert-True ($null -ne $p.PSObject.Properties['community_claim_wmxm_magic_status']) `
    "community_claim_wmxm_magic_status field is explicit"
Assert-True ($p.community_claim_wmxm_magic_status -eq 'contradicted_by_measured_b42_igmb_sample') `
    "community_claim_wmxm_magic_status == contradicted_by_measured_b42_igmb_sample"
Assert-True ($p.measured_igmb_magic_status -eq 'measured_in_project_russia_b42_sample') `
    "measured_igmb_magic_status == measured_in_project_russia_b42_sample"
Assert-True ($p.worldmap_bin_playable_terrain_canary_supported -eq $false) `
    "worldmap_bin_playable_terrain_canary_supported == false"
Assert-True ($p.playable_world_canary_separate_from_map_ui_canary -eq $true) `
    "playable_world_canary_separate_from_map_ui_canary == true"
Assert-True ($p.debug_runtime_logs_reviewed -eq $true) `
    "debug_runtime_logs_reviewed == true"
Assert-True ($p.debug_runtime_workshop_runtime_cache_confirmed -eq $true) `
    "debug_runtime_workshop_runtime_cache_confirmed == true"
Assert-True ($p.debug_runtime_mod_loaded -eq $true) `
    "debug_runtime_mod_loaded == true"
Assert-True ($p.debug_runtime_isometagrid_map_folder_list_empty -eq $true) `
    "debug_runtime_isometagrid_map_folder_list_empty == true"
Assert-True ($p.debug_runtime_spawn_metadata_works -eq $true) `
    "debug_runtime_spawn_metadata_works == true"
Assert-True ($p.debug_runtime_pzmapforge_lotheader_parse_evidence -eq $false) `
    "debug_runtime_pzmapforge_lotheader_parse_evidence == false"
Assert-True ($p.debug_runtime_server_console_ignored_stale_b41 -eq $true) `
    "debug_runtime_server_console_ignored_stale_b41 == true"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
