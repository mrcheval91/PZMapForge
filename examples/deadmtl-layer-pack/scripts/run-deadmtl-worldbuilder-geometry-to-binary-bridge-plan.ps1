#Requires -Version 5.1
<#
.SYNOPSIS
MAP-36B: Bridge plan from MAP-31B geometry buckets to Build 42 binary cell files.

Reads MAP-36A1 audit JSON and MAP-31B emitter JSON (both optional) and writes a
deterministic bridge-plan packet that explains what is missing between the current
DeadMTL geometry buckets and actual Project Zomboid Build 42 runtime binary cell files.

Outputs:
  deadmtl-geometry-to-binary-bridge-plan-result.json
  deadmtl-geometry-to-binary-bridge-plan-checks.csv
  deadmtl-geometry-to-binary-bridge-plan-summary.txt
  deadmtl-geometry-to-binary-bridge-plan.md
  deadmtl-geometry-to-binary-bridge-plan-required-unknowns.csv
  deadmtl-geometry-to-binary-bridge-plan-candidate-next-experiments.csv

Claim boundary:
  runtime_binary_written=false
  geometry_injected=false
  playable_export_claimed=false
  No binary files written. No geometry injection. Read-only bridge plan artifact.
#>

Set-StrictMode -Version Latest

$ScriptDir  = $PSScriptRoot
$RepoRoot   = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$Map36a1AuditJson = Join-Path $RepoRoot ".local\deadmtl-authoring\map36a-visible-cell-binary-anatomy-audit\deadmtl-visible-cell-binary-anatomy-audit-result.json"
$Map31bEmitterJson = Join-Path $RepoRoot ".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter\map_00\map_00.sandbox_writer_locked_replay_backend_dry_run_operations.json"

$OutputRoot  = Join-Path $RepoRoot ".local\deadmtl-authoring\map36b-geometry-to-binary-bridge-plan"
$ResultJson  = Join-Path $OutputRoot "deadmtl-geometry-to-binary-bridge-plan-result.json"
$ChecksCsv   = Join-Path $OutputRoot "deadmtl-geometry-to-binary-bridge-plan-checks.csv"
$SummaryTxt  = Join-Path $OutputRoot "deadmtl-geometry-to-binary-bridge-plan-summary.txt"
$BridgePlanMd   = Join-Path $OutputRoot "deadmtl-geometry-to-binary-bridge-plan.md"
$UnknownsCsv    = Join-Path $OutputRoot "deadmtl-geometry-to-binary-bridge-plan-required-unknowns.csv"
$ExperimentsCsv = Join-Path $OutputRoot "deadmtl-geometry-to-binary-bridge-plan-candidate-next-experiments.csv"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$cliArgs = @(
    "--output-root", $OutputRoot
)

if (Test-Path $Map36a1AuditJson) {
    $cliArgs += "--map36a1-audit-json", $Map36a1AuditJson
}

if (Test-Path $Map31bEmitterJson) {
    $cliArgs += "--map31b-emitter-json", $Map31bEmitterJson
}

dotnet run --project $CliProject -- deadmtl-build-worldbuilder-geometry-to-binary-bridge-plan @cliArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-geometry-to-binary-bridge-plan failed (exit $LASTEXITCODE)"
    exit 1
}

Write-Host ""
Write-Host "=== MAP-36B Geometry-to-Binary Bridge Plan ==="

if (Test-Path $ResultJson) {
    $result = Get-Content $ResultJson -Raw | ConvertFrom-Json
    Write-Host "Format                       : $($result.format)"
    Write-Host "Audit output found           : $($result.audit_output_found)"
    Write-Host "Emitter output found         : $($result.emitter_output_found)"
    Write-Host "Emits binary file            : $($result.emits_binary_file)"
    Write-Host "Sandbox only                 : $($result.sandbox_only)"
    Write-Host "Required unknown count       : $($result.required_unknown_count)"
    Write-Host "Candidate experiment count   : $($result.candidate_experiment_count)"
    Write-Host ""
    Write-Host "=== Claim Boundary ==="
    Write-Host "runtime_binary_written       : $($result.runtime_binary_written)"
    Write-Host "geometry_injected            : $($result.geometry_injected)"
    Write-Host "playable_export_claimed      : $($result.playable_export_claimed)"
    Write-Host "workshop_upload_performed    : $($result.workshop_upload_performed)"
    Write-Host "steam_install_write          : $($result.steam_install_write)"
    Write-Host ""
    Write-Host "=== Checks ==="
    Write-Host "Checks                       : $($result.check_count) total / $($result.passed_check_count) PASS / $($result.failed_check_count) FAIL"
    Write-Host "Verdict                      : $($result.verdict)"
    Write-Host ""
    Write-Host "=== Output Artifacts ==="
    Write-Host "Result JSON                  : $ResultJson"
    Write-Host "Checks CSV                   : $ChecksCsv"
    Write-Host "Summary                      : $SummaryTxt"
    Write-Host "Bridge plan markdown         : $BridgePlanMd"
    Write-Host "Required unknowns CSV        : $UnknownsCsv"
    Write-Host "Candidate experiments CSV    : $ExperimentsCsv"
} else {
    Write-Warning "Result JSON not found: $ResultJson"
}
