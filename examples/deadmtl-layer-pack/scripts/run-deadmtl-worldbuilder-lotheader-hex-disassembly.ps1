#Requires -Version 5.1
<#
.SYNOPSIS
MAP-36C EXP_001: Lotheader hex-disassembly read-only probe.

Compares MAP-33A minimal seed lotheader against MAP-35A visible source lotheader
and produces a deterministic hex-disassembly packet:
  - byte diff runs
  - candidate structural regions (UNVERIFIED)
  - hexdump previews for prefix, diff, expansion, and suffix regions

Claim boundary:
  runtime_binary_written=false
  geometry_injected=false
  playable_export_claimed=false
  Read-only probe only. No binary files written. No geometry injection.
#>

Set-StrictMode -Version Latest

$ScriptDir  = $PSScriptRoot
$RepoRoot   = (Resolve-Path (Join-Path $ScriptDir "..\..\..\")).Path
$CliProject = Join-Path $RepoRoot "src\PZMapForge.Cli\PZMapForge.Cli.csproj"

$MinimalLotheader = Join-Path $RepoRoot ".local\map7y-packet\staged-workshop-sidecar-stubs\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001\35_27.lotheader"
$VisibleLotheader = "C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\pzmapforge_build42_candidate_v4_001\35_27.lotheader"

$OutputRoot     = Join-Path $RepoRoot ".local\deadmtl-authoring\map36c-lotheader-hex-disassembly"
$ResultJson     = Join-Path $OutputRoot "deadmtl-lotheader-hex-disassembly-result.json"
$ChecksCsv      = Join-Path $OutputRoot "deadmtl-lotheader-hex-disassembly-checks.csv"
$SummaryTxt     = Join-Path $OutputRoot "deadmtl-lotheader-hex-disassembly-summary.txt"
$ByteRegionsCsv = Join-Path $OutputRoot "deadmtl-lotheader-hex-disassembly-byte-regions.csv"
$DiffRunsCsv    = Join-Path $OutputRoot "deadmtl-lotheader-hex-disassembly-diff-runs.csv"
$CandidateMd    = Join-Path $OutputRoot "deadmtl-lotheader-hex-disassembly-candidate-structure.md"
$HexdumpMd      = Join-Path $OutputRoot "deadmtl-lotheader-hex-disassembly-prefix-suffix-hexdump.md"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

if (-not (Test-Path $MinimalLotheader)) {
    Write-Error "Minimal lotheader not found: $MinimalLotheader"
    exit 1
}
if (-not (Test-Path $VisibleLotheader)) {
    Write-Error "Visible lotheader not found: $VisibleLotheader"
    exit 1
}

dotnet run --project $CliProject -- deadmtl-build-worldbuilder-lotheader-hex-disassembly `
    --minimal-lotheader $MinimalLotheader `
    --visible-lotheader $VisibleLotheader `
    --output-root       $OutputRoot

if ($LASTEXITCODE -ne 0) {
    Write-Error "deadmtl-build-worldbuilder-lotheader-hex-disassembly failed (exit $LASTEXITCODE)"
    exit 1
}

Write-Host ""
Write-Host "=== MAP-36C Lotheader Hex-Disassembly ==="

if (Test-Path $ResultJson) {
    $result = Get-Content $ResultJson -Raw | ConvertFrom-Json
    Write-Host "Minimal lotheader found  : $($result.minimal_lotheader_found)  size=$($result.minimal_size)"
    Write-Host "Visible lotheader found  : $($result.visible_lotheader_found)  size=$($result.visible_size)"
    Write-Host "Size delta               : $($result.size_delta)"
    Write-Host "SHA256 computed          : $($result.sha256_computed)"
    Write-Host "Minimal SHA256           : $($result.minimal_sha256)"
    Write-Host "Visible SHA256           : $($result.visible_sha256)"
    Write-Host "Common prefix length     : $($result.common_prefix_length)"
    Write-Host "Common suffix length     : $($result.common_suffix_length)"
    Write-Host "Diff runs                : $($result.diff_runs.Count)"
    Write-Host "Candidate regions        : $($result.candidate_regions.Count)"
    Write-Host ""
    Write-Host "=== Candidate Regions ==="
    foreach ($r in $result.candidate_regions) {
        Write-Host ("  {0,-20} 0x{1:X8} - 0x{2:X8}  len={3}" -f $r.label, $r.start_offset, $r.end_offset, $r.length)
    }
    Write-Host ""
    Write-Host "=== Claim Boundary ==="
    Write-Host "runtime_binary_written   : $($result.runtime_binary_written)"
    Write-Host "geometry_injected        : $($result.geometry_injected)"
    Write-Host "playable_export_claimed  : $($result.playable_export_claimed)"
    Write-Host ""
    Write-Host "=== Checks ==="
    Write-Host "Checks  : $($result.check_count) total / $($result.passed_check_count) PASS / $($result.failed_check_count) FAIL"
    Write-Host "Verdict : $($result.verdict)"
    Write-Host ""
    Write-Host "=== Output Artifacts ==="
    Write-Host "Result JSON           : $ResultJson"
    Write-Host "Checks CSV            : $ChecksCsv"
    Write-Host "Summary               : $SummaryTxt"
    Write-Host "Byte regions CSV      : $ByteRegionsCsv"
    Write-Host "Diff runs CSV         : $DiffRunsCsv"
    Write-Host "Candidate structure   : $CandidateMd"
    Write-Host "Hexdump               : $HexdumpMd"
} else {
    Write-Warning "Result JSON not found: $ResultJson"
}
