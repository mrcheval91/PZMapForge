# MAP-37A: Chunkdata record-cluster audit read-only probe
# Read-only. Does not write any game files or binary cells.

$RepoRoot  = Resolve-Path "$PSScriptRoot\..\..\.."
$CliProject = "$RepoRoot\src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$MinimalChunkdata = "$RepoRoot\.local\map7y-packet\staged-workshop-sidecar-stubs\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001\chunkdata_35_27.bin"
$VisibleChunkdata = "C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001\chunkdata_35_27.bin"
$OutputRoot = "$RepoRoot\.local\deadmtl-authoring\map37a-chunkdata-record-cluster-audit"

if (-not (Test-Path $MinimalChunkdata)) {
    Write-Error "Minimal chunkdata not found: $MinimalChunkdata"
    exit 1
}
if (-not (Test-Path $VisibleChunkdata)) {
    Write-Error "Visible chunkdata not found: $VisibleChunkdata"
    exit 1
}

$minSize = (Get-Item $MinimalChunkdata).Length
$visSize = (Get-Item $VisibleChunkdata).Length

Write-Host "MAP-37A: Chunkdata record-cluster audit"
Write-Host "  minimal  : $MinimalChunkdata"
Write-Host "  minimal_size : $minSize"
Write-Host "  visible  : $VisibleChunkdata"
Write-Host "  visible_size : $visSize"
Write-Host "  output   : $OutputRoot"
Write-Host ""

dotnet run --project "$CliProject" -- `
    deadmtl-build-worldbuilder-chunkdata-record-cluster-audit `
    --minimal-chunkdata $MinimalChunkdata `
    --visible-chunkdata $VisibleChunkdata `
    --output-root $OutputRoot

if ($LASTEXITCODE -ne 0) {
    Write-Error "MAP-37A chunkdata record-cluster audit failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "MAP-37A complete. Artifacts in: $OutputRoot"

$ResultJson = "$OutputRoot\deadmtl-chunkdata-record-cluster-audit-result.json"
if (Test-Path $ResultJson) {
    $r = Get-Content $ResultJson | ConvertFrom-Json
    Write-Host ""
    Write-Host "--- Results ---"
    Write-Host "format                       : $($r.format)"
    Write-Host "minimal_size                 : $($r.minimal_size)"
    Write-Host "visible_size                 : $($r.visible_size)"
    Write-Host "size_delta                   : $($r.size_delta)"
    Write-Host "candidate_header_sizes_count : $($r.candidate_header_sizes.Count)"
    Write-Host "record_width_analyses_count  : $($r.record_width_analyses.Count)"
    Write-Host "column_statistics_count      : $($r.record_column_statistics.Count)"
    Write-Host "changed_record_windows_count : $($r.changed_record_windows.Count)"
    Write-Host "minimal_record_samples_count : $($r.minimal_record_samples.Count)"
    Write-Host "visible_record_samples_count : $($r.visible_record_samples.Count)"
    Write-Host ""
    Write-Host "--- Hypothesis summary ---"
    foreach ($h in $r.hypothesis_summary) {
        Write-Host "  $h"
    }
    Write-Host ""
    Write-Host "--- Changed record windows ---"
    foreach ($w in $r.changed_record_windows) {
        Write-Host ("  type={0,-14} start={1,5} end={2,5} len={3,5} vis_off={4}" -f $w.window_type, $w.start_record_index, $w.end_record_index, $w.length_records, $w.visible_start_offset)
    }
    Write-Host ""
    Write-Host "--- Claim boundary ---"
    Write-Host "  runtime_binary_written   : $($r.runtime_binary_written)"
    Write-Host "  geometry_injected        : $($r.geometry_injected)"
    Write-Host "  playable_export_claimed  : $($r.playable_export_claimed)"
    Write-Host "  verified_chunkdata_format: $($r.verified_chunkdata_format)"
    Write-Host "  read_only_probe          : $($r.read_only_probe)"
    Write-Host ""
    Write-Host ("checks: {0} total / {1} PASS / {2} FAIL" -f $r.check_count, $r.passed_check_count, $r.failed_check_count)
    Write-Host "verdict: $($r.verdict)"
    Write-Host ""
    Write-Host "--- Output artifacts ---"
    foreach ($a in $r.output_artifacts) {
        Write-Host "  $a"
    }
}
