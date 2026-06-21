#Requires -Version 5.1
<#
.SYNOPSIS
MAP-36A: Audit visible-cell binary anatomy before DeadMTL geometry injection.

Compares MAP-33A minimal seed binary files with MAP-35A visible source binary
files (35_27.lotheader, chunkdata_35_27.bin, world_35_27.lotpack) and records:
  - file sizes and SHA256 for minimal, visible, and installed candidates
  - byte prefix/suffix hex dumps
  - diff analysis (first differing byte, total differing bytes, common prefix/suffix)
  - Shannon entropy per file
  - printable string extraction (max 200 chars)
  - header and size-delta observations
  - chunkdata special analysis (record-count guesses, labeled as GUESS)
  - MAP-31B cross-reference: emits_binary_file=false, sandbox_only=true

Claim boundary:
  runtime_binary_written=false
  geometry_injected=false
  playable_export_claimed=false
  No binary files written. No geometry injection. Read-only audit.
#>

Set-StrictMode -Version Latest

$ScriptDir  = $PSScriptRoot
$RepoRoot   = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$Map33aSeedDir = Join-Path $RepoRoot ".local\map7y-packet\staged-workshop-sidecar-stubs\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001"
$Map35aSourceDir = "C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001"
$Map35aInstalledDir = "C:\Users\Palmacede\Zomboid\mods\deadmtl_map35a_visible_cell_candidate\common\media\maps\DeadMTL_MAP35A"
$Map31bEmitterJson  = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter\map_00\map_00.sandbox_writer_locked_replay_backend_dry_run_operations.json"

$OutputRoot   = Join-Path $RepoRoot ".local\deadmtl-authoring\map36a-visible-cell-binary-anatomy-audit"
$OutputResult = Join-Path $OutputRoot "deadmtl-visible-cell-binary-anatomy-audit-result.json"
$ChecksCsv    = Join-Path $OutputRoot "deadmtl-visible-cell-binary-anatomy-audit-checks.csv"
$Summary      = Join-Path $OutputRoot "deadmtl-visible-cell-binary-anatomy-audit-summary.txt"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$cliArgs = @(
    "--map33a-seed-dir",       $Map33aSeedDir,
    "--map35a-source-dir",     $Map35aSourceDir,
    "--map35a-installed-dir",  $Map35aInstalledDir,
    "--map31b-emitter-json",   $Map31bEmitterJson,
    "--output-root",           $OutputRoot,
    "--output-result",         $OutputResult,
    "--output-checks-csv",     $ChecksCsv,
    "--summary",               $Summary
)

dotnet run --project $CliProject -- deadmtl-build-worldbuilder-visible-cell-binary-anatomy-audit @cliArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-visible-cell-binary-anatomy-audit failed (exit $LASTEXITCODE)"
    exit 1
}

Write-Host ""
Write-Host "=== MAP-36A Visible Cell Binary Anatomy Audit ==="

if (Test-Path $OutputResult) {
    $result = Get-Content $OutputResult -Raw | ConvertFrom-Json
    Write-Host "Cell coord                       : $($result.cell_coord)"
    Write-Host "MAP-33A seed dir found           : $($result.map33a_seed_dir_found)"
    Write-Host "MAP-35A source dir found         : $($result.map35a_source_dir_found)"
    Write-Host "MAP-35A installed dir found      : $($result.map35a_installed_dir_found)"
    Write-Host "MAP-31B emitter JSON found       : $($result.map31b_emitter_json_found)"
    Write-Host ""
    Write-Host "=== Binary File Sizes ==="
    Write-Host "Lotheader  minimal=$($result.lotheader_anatomy.minimal_size)  visible=$($result.lotheader_anatomy.visible_size)  installed=$($result.lotheader_anatomy.installed_size)  delta=$($result.lotheader_anatomy.size_delta)"
    Write-Host "Chunkdata  minimal=$($result.chunkdata_anatomy.minimal_size)  visible=$($result.chunkdata_anatomy.visible_size)  installed=$($result.chunkdata_anatomy.installed_size)  delta=$($result.chunkdata_anatomy.size_delta)"
    Write-Host "Lotpack    minimal=$($result.lotpack_anatomy.minimal_size)  visible=$($result.lotpack_anatomy.visible_size)  installed=$($result.lotpack_anatomy.installed_size)  delta=$($result.lotpack_anatomy.size_delta)"
    Write-Host ""
    Write-Host "=== Chunkdata Special Analysis (GUESS_NOT_VERIFIED) ==="
    Write-Host "Size ratio (visible/minimal)     : $($result.chunkdata_special.size_ratio)"
    Write-Host "Record count guess (32-byte rec) : $($result.chunkdata_special.record_count_guess_if_fixed_32_byte_records)"
    Write-Host "Record count guess (8-byte rec)  : $($result.chunkdata_special.record_count_guess_if_fixed_8_byte_records)"
    Write-Host "Guess label                      : $($result.chunkdata_special.record_count_guess_label)"
    Write-Host ""
    Write-Host "=== MAP-31B Cross-Reference ==="
    Write-Host "Emitter JSON found               : $($result.map31b_cross_ref.emitter_json_found)"
    Write-Host "Emitted operation count          : $($result.map31b_cross_ref.emitted_operation_count)"
    Write-Host "Emitted planned cell count       : $($result.map31b_cross_ref.emitted_total_planned_cell_count)"
    Write-Host "Emits binary file                : $($result.map31b_cross_ref.emits_binary_file)"
    Write-Host "Runtime consumable               : $($result.map31b_cross_ref.runtime_consumable)"
    Write-Host "Sandbox only                     : $($result.map31b_cross_ref.sandbox_only)"
    Write-Host "Geometry-to-binary gap           : $($result.map31b_cross_ref.map31b_geometry_to_binary_gap)"
    Write-Host ""
    Write-Host "=== Checks ==="
    Write-Host "Checks                           : $($result.check_count) total / $($result.passed_check_count) PASS / $($result.failed_check_count) FAIL"
    Write-Host "Verdict                          : $($result.verdict)"
    Write-Host ""
    Write-Host "=== Claim Boundary ==="
    Write-Host "runtime_binary_written           : $($result.runtime_binary_written)"
    Write-Host "geometry_injected                : $($result.geometry_injected)"
    Write-Host "playable_export_claimed          : $($result.playable_export_claimed)"
    Write-Host "workshop_upload_performed        : $($result.workshop_upload_performed)"
    Write-Host "steam_install_write              : $($result.steam_install_write)"
} else {
    Write-Warning "Result JSON not found: $OutputResult"
}
