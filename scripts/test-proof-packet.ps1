#Requires -Version 5.1
<#
.SYNOPSIS
    Validates .local/mapforge/proof-packet.json against the v0.54 proof-packet contract.

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
# Required top-level fields (same 28 as v0.10/v0.11/v0.16)
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
    'claim_boundary', 'validation_summary', 'dotnet_validation_summary', 'safety'
)
foreach ($field in $requiredFields) {
    Assert-True ($null -ne $p.PSObject.Properties[$field]) "Field '$field' present"
}

# ---------------------------------------------------------------------------
# Sentinels
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Sentinels ---"
Assert-True ($p.schema -eq 'pzmapforge.proof-packet.v0.55') `
    "schema == 'pzmapforge.proof-packet.v0.55' (got '$($p.schema)')"
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
# Validation summary counts (PowerShell lane only)
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Validation summary (PowerShell lane) ---"
Assert-True ([int]$p.validation_summary.schema_file_sanity               -eq 214) "schema_file_sanity == 214"
Assert-True ([int]$p.validation_summary.artifact_contract                -eq 40)  "artifact_contract == 40"
Assert-True ([int]$p.validation_summary.palette_sha256_verification      -eq 5)   "palette_sha256_verification == 5"
Assert-True ([int]$p.validation_summary.tmx_integrity                    -eq 21)  "tmx_integrity == 21"
Assert-True ([int]$p.validation_summary.hardening_harness                -eq 36)  "hardening_harness == 36"
Assert-True ([int]$p.validation_summary.region_extraction                -eq 24)  "region_extraction == 24"
Assert-True ([int]$p.validation_summary.primitive_classification          -eq 22)  "primitive_classification == 22"
Assert-True ([int]$p.validation_summary.plan_recommendations_contract     -eq 28)  "plan_recommendations_contract == 28"
Assert-True ([int]$p.validation_summary.build42_geometry_inspector_tests       -eq 23)  "build42_geometry_inspector_tests == 23"
Assert-True ([int]$p.validation_summary.build42_format_design_matrix_tests    -eq 13)  "build42_format_design_matrix_tests == 13"
Assert-True ([int]$p.validation_summary.build42_writer_contract_tests         -eq 20)  "build42_writer_contract_tests == 20"
Assert-True ([int]$p.validation_summary.build42_lotp_payload_window_tests     -eq 20)  "build42_lotp_payload_window_tests == 20"
Assert-True ([int]$p.validation_summary.build42_candidate_packet_tests        -eq 20)  "build42_candidate_packet_tests == 20"
Assert-True ([int]$p.validation_summary.map6n_log_triage_tests                -eq 12)  "map6n_log_triage_tests == 12"
Assert-True ([int]$p.validation_summary.map6o_retest_checklist_tests          -eq 15)  "map6o_retest_checklist_tests == 15"
Assert-True ([int]$p.validation_summary.map6p_spawn_activation_tests          -eq 12)  "map6p_spawn_activation_tests == 12"
Assert-True ([int]$p.validation_summary.map6q_lotheader_comparison_tests      -eq 13)  "map6q_lotheader_comparison_tests == 13"
Assert-True ([int]$p.validation_summary.map6r_loth_structure_tests            -eq 14)  "map6r_loth_structure_tests == 14"
Assert-True ([int]$p.validation_summary.map6t_load_test_packet_tests          -eq 18)  "map6t_load_test_packet_tests == 18"
Assert-True ([int]$p.validation_summary.map6u_full_body_tests                 -eq 14)  "map6u_full_body_tests == 14"
Assert-True ([int]$p.validation_summary.map6v_trailing_body_decode_tests      -eq 17)  "map6v_trailing_body_decode_tests == 17"
Assert-True ([int]$p.validation_summary.map6w_byte_pattern_tests              -eq 20)  "map6w_byte_pattern_tests == 20"
Assert-True ([int]$p.validation_summary.map6x_per_entry_record_tests          -eq 20)  "map6x_per_entry_record_tests == 20"
Assert-True ([int]$p.validation_summary.map6y_fixed_1048_block_tests          -eq 20)  "map6y_fixed_1048_block_tests == 20"
Assert-True ([int]$p.validation_summary.map7a_load_test_packet_tests          -eq 23)  "map7a_load_test_packet_tests == 23"
Assert-True ([int]$p.validation_summary.map7b_lua_metadata_tests               -eq 21)  "map7b_lua_metadata_tests == 21"
Assert-True ([int]$p.validation_summary.map7c_metadata_v3_packet_tests         -eq 18)  "map7c_metadata_v3_packet_tests == 18"
Assert-True ([int]$p.validation_summary.map7d_metadata_v4_packet_tests         -eq 15)  "map7d_metadata_v4_packet_tests == 15"
Assert-True ([int]$p.validation_summary.map8b_version_media_runtime_result_tests -eq 20) "map8b_version_media_runtime_result_tests == 20"
Assert-True ([int]$p.validation_summary.map7y_sidecar_stub_probe_tests            -eq 24) "map7y_sidecar_stub_probe_tests == 24"
Assert-True ([int]$p.validation_summary.map7x_actual_contract_result_tests        -eq 20) "map7x_actual_contract_result_tests == 20"
Assert-True ([int]$p.validation_summary.map7w_runtime_registration_tests          -eq 20) "map7w_runtime_registration_tests == 20"
Assert-True ([int]$p.validation_summary.map7v_control_results_tests               -eq 20) "map7v_control_results_tests == 20"
Assert-True ([int]$p.validation_summary.map7u_coordinate_aligned_diagnostic_tests -eq 20) "map7u_coordinate_aligned_diagnostic_tests == 20"
Assert-True ([int]$p.validation_summary.map7t_k002_runtime_payload_tests          -eq 20) "map7t_k002_runtime_payload_tests == 20"
Assert-True ([int]$p.validation_summary.map7s_private_workshop_staging_tests      -eq 20) "map7s_private_workshop_staging_tests == 20"
Assert-True ([int]$p.validation_summary.map7r_workshop_trigger_failure_tests      -eq 20) "map7r_workshop_trigger_failure_tests == 20"
Assert-True ([int]$p.validation_summary.map7q_runtime_baseline_success_tests      -eq 20) "map7q_runtime_baseline_success_tests == 20"
Assert-True ([int]$p.validation_summary.map7p_known_working_runtime_baseline_tests -eq 20) "map7p_known_working_runtime_baseline_tests == 20"
Assert-True ([int]$p.validation_summary.map7o_drumap_aligned_experiment_tests  -eq 19)  "map7o_drumap_aligned_experiment_tests == 19"
Assert-True ([int]$p.validation_summary.map7n_reference_map_id_tests           -eq 9)   "map7n_reference_map_id_tests == 9"
Assert-True ([int]$p.validation_summary.map7m_known_working_contract_tests     -eq 12)  "map7m_known_working_contract_tests == 12"
Assert-True ([int]$p.validation_summary.map7l_common_layout_experiment_tests   -eq 15)  "map7l_common_layout_experiment_tests == 15"
Assert-True ([int]$p.validation_summary.map7k_modinfo_map_field_tests          -eq 11)  "map7k_modinfo_map_field_tests == 11"
Assert-True ([int]$p.validation_summary.map7j_metadata_contract_tests         -eq 17)  "map7j_metadata_contract_tests == 17"
Assert-True ([int]$p.validation_summary.map7i_root_modinfo_experiment_tests   -eq 12)  "map7i_root_modinfo_experiment_tests == 12"
Assert-True ([int]$p.validation_summary.map7h_discovery_path_tests            -eq 12)  "map7h_discovery_path_tests == 12"
Assert-True ([int]$p.validation_summary.map7g_variant_a_failure_tests         -eq 8)   "map7g_variant_a_failure_tests == 8"
Assert-True ([int]$p.validation_summary.map7f_registration_diagnostic_tests   -eq 11)  "map7f_registration_diagnostic_tests == 11"
Assert-True ([int]$p.validation_summary.map7e_diagnostics_tests                -eq 11)  "map7e_diagnostics_tests == 11"
Assert-True ([int]$p.validation_summary.total_expected_assertions              -eq 1212) "total_expected_assertions == 1212"

# ---------------------------------------------------------------------------
# dotnet_validation_summary (separate lane)
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- dotnet_validation_summary ---"
$d = $p.dotnet_validation_summary
Assert-True ([int]$d.test_total                                -eq 556)  "dotnet test_total == 556"
Assert-True ([int]$d.core_tests -eq 190)  "dotnet core_tests == 190"
Assert-True ([int]$d.cli_tests                                 -eq 366)  "dotnet cli_tests == 366"
Assert-True ($d.process_cli_tests_present                      -eq $true) "process_cli_tests_present == true"
Assert-True ($d.full_pipeline_contract_tests_present           -eq $true) "full_pipeline_contract_tests_present == true"
Assert-True ([int]$d.full_pipeline_artifact_count              -eq 7)    "dotnet full_pipeline_artifact_count == 7"
$fpArts = @($d.full_pipeline_artifacts)
Assert-True ($fpArts -contains 'parsed-cell.json')          "full_pipeline_artifacts contains 'parsed-cell.json'"
Assert-True ($fpArts -contains 'regions.json')              "full_pipeline_artifacts contains 'regions.json'"
Assert-True ($fpArts -contains 'regions-report.md')         "full_pipeline_artifacts contains 'regions-report.md'"
Assert-True ($fpArts -contains 'primitives.json')           "full_pipeline_artifacts contains 'primitives.json'"
Assert-True ($fpArts -contains 'primitives-report.md')      "full_pipeline_artifacts contains 'primitives-report.md'"
Assert-True ($fpArts -contains 'plan-recommendations.json') "full_pipeline_artifacts contains 'plan-recommendations.json'"
Assert-True ($fpArts -contains 'plan-report.md')            "full_pipeline_artifacts contains 'plan-report.md'"
Assert-True ($d.layer_pipeline_present                         -eq $true) "layer_pipeline_present == true"
Assert-True ([int]$d.layer_pipeline_artifact_count             -eq 8)    "dotnet layer_pipeline_artifact_count == 8"
$lpArts = @($d.layer_pipeline_artifacts)
Assert-True ($lpArts -contains 'parsed-cell.json')          "layer_pipeline_artifacts contains 'parsed-cell.json'"
Assert-True ($lpArts -contains 'layer-merge-report.md')     "layer_pipeline_artifacts contains 'layer-merge-report.md'"
Assert-True ($lpArts -contains 'regions.json')              "layer_pipeline_artifacts contains 'regions.json'"
Assert-True ($lpArts -contains 'regions-report.md')         "layer_pipeline_artifacts contains 'regions-report.md'"
Assert-True ($lpArts -contains 'primitives.json')           "layer_pipeline_artifacts contains 'primitives.json'"
Assert-True ($lpArts -contains 'primitives-report.md')      "layer_pipeline_artifacts contains 'primitives-report.md'"
Assert-True ($lpArts -contains 'plan-recommendations.json') "layer_pipeline_artifacts contains 'plan-recommendations.json'"
Assert-True ($lpArts -contains 'plan-report.md')            "layer_pipeline_artifacts contains 'plan-report.md'"
Assert-True ($d.layer_validate_present                         -eq $true)  "layer_validate_present == true"
Assert-True ($d.layer_validate_writes_artifacts                -eq $false) "layer_validate_writes_artifacts == false"
Assert-True ($d.local_pz_config_loader_present -eq $true) "local_pz_config_loader_present == true"
Assert-True ($d.local_pz_config_loader_requires_real_install -eq $false) "local_pz_config_loader_requires_real_install == false"
Assert-True ($d.local_pz_config_loader_inspects_assets -eq $false) "local_pz_config_loader_inspects_assets == false"
Assert-True ($d.local_pz_config_loader_copies_assets -eq $false) "local_pz_config_loader_copies_assets == false"
Assert-True ($d.local_pz_install_validator_present -eq $true) "local_pz_install_validator_present == true"
Assert-True ($d.local_pz_install_validator_requires_real_install -eq $false) "local_pz_install_validator_requires_real_install == false"
Assert-True ($d.local_pz_install_validator_reads_asset_contents -eq $false) "local_pz_install_validator_reads_asset_contents == false"
Assert-True ($d.local_pz_install_validator_copies_assets -eq $false) "local_pz_install_validator_copies_assets == false"
Assert-True ($d.local_pz_install_validator_touches_media_maps -eq $false) "local_pz_install_validator_touches_media_maps == false"

Assert-True ($d.local_tile_reference_survey_writer_present -eq $true) "local_tile_reference_survey_writer_present == true"
Assert-True ($d.local_tile_reference_survey_writer_requires_real_install -eq $false) "local_tile_reference_survey_writer_requires_real_install == false"
Assert-True ($d.local_tile_reference_survey_writer_reads_asset_contents -eq $false) "local_tile_reference_survey_writer_reads_asset_contents == false"
Assert-True ($d.local_tile_reference_survey_writer_copies_assets -eq $false) "local_tile_reference_survey_writer_copies_assets == false"
Assert-True ($d.local_tile_reference_survey_writer_touches_media_maps -eq $false) "local_tile_reference_survey_writer_touches_media_maps == false"
Assert-True ($d.local_tile_reference_survey_writer_outputs_local_only -eq $true) "local_tile_reference_survey_writer_outputs_local_only == true"
Assert-True ($d.local_tile_survey_cli_present                         -eq $true)  "local_tile_survey_cli_present == true"
Assert-True ($d.local_tile_survey_cli_requires_real_install_for_tests -eq $false) "local_tile_survey_cli_requires_real_install_for_tests == false"
Assert-True ($d.local_tile_survey_cli_outputs_local_only              -eq $true)  "local_tile_survey_cli_outputs_local_only == true"
Assert-True ($d.local_tile_survey_cli_copies_assets                   -eq $false) "local_tile_survey_cli_copies_assets == false"
Assert-True ($d.local_tile_survey_cli_reads_asset_contents            -eq $false) "local_tile_survey_cli_reads_asset_contents == false"
Assert-True ($d.local_tile_survey_cli_touches_media_maps              -eq $false) "local_tile_survey_cli_touches_media_maps == false"

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
