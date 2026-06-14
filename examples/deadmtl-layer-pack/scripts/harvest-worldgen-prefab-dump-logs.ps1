# harvest-worldgen-prefab-dump-logs.ps1
# Searches PZ log files for MAP-22B prefab dump markers.
#
# Searches:
#   C:\Users\Palmacede\Zomboid\console.txt
#   C:\Users\Palmacede\Zomboid\coop-console.txt
#   Recent files under C:\Users\Palmacede\Zomboid\Logs
#
# Markers searched:
#   PZMAPFORGE_PREFAB_DUMP_LOADED
#   PZMAPFORGE_PREFAB_KEY=
#   PZMAPFORGE_ROADLIKE_PREFAB_KEY=
#   PZMAPFORGE_PREFAB_COUNT=
#   PZMAPFORGE_ROADLIKE_PREFAB_COUNT=
#   Error found in LUA file
#   LuaManager.RunLuaInternal
#
# Output:
#   .local/deadmtl-authoring/worldgen-prefab-dump/prefab-dump-harvest.txt
#
# Exit 0 always. If markers are not found, the probe may not have run yet.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\harvest-worldgen-prefab-dump-logs.ps1

$repoRoot  = $PSScriptRoot | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent
$outputDir = Join-Path $repoRoot ".local\deadmtl-authoring\worldgen-prefab-dump"
$harvestPath = Join-Path $outputDir "prefab-dump-harvest.txt"

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$pzUserDir = "C:\Users\Palmacede\Zomboid"
$logsDir   = Join-Path $pzUserDir "Logs"

$markers = @(
    "PZMAPFORGE_PREFAB_DUMP_LOADED",
    "PZMAPFORGE_PREFAB_KEY=",
    "PZMAPFORGE_ROADLIKE_PREFAB_KEY=",
    "PZMAPFORGE_PREFAB_COUNT=",
    "PZMAPFORGE_ROADLIKE_PREFAB_COUNT=",
    "Error found in LUA file",
    "LuaManager.RunLuaInternal"
)

$candidateFiles = @()

# Primary log files
$candidateFiles += Join-Path $pzUserDir "console.txt"
$candidateFiles += Join-Path $pzUserDir "coop-console.txt"

# Recent files under Logs (sorted newest first)
if (Test-Path $logsDir) {
    $logFiles = Get-ChildItem -Path $logsDir -Recurse -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 10 |
        ForEach-Object { $_.FullName }
    $candidateFiles += $logFiles
}

Write-Host "MAP-22B WorldGen Prefab Dump Harvest"
Write-Host "====================================="
Write-Host ""
Write-Host "Harvest output: $harvestPath"
Write-Host "Searching $(($candidateFiles | Where-Object { Test-Path $_ }).Count) log file(s)..."
Write-Host ""

$harvestLines = @()
$harvestLines += "MAP-22B WorldGen Prefab Dump Harvest"
$harvestLines += "====================================="
$harvestLines += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$harvestLines += ""

$totalMatchLines = 0

foreach ($logFile in $candidateFiles) {
    if (-not (Test-Path $logFile)) {
        continue
    }

    $matchedLines = @()

    try {
        $content = Get-Content $logFile -Raw -ErrorAction Stop
        if ($null -eq $content) { continue }

        foreach ($marker in $markers) {
            $found = [regex]::Matches($content, [regex]::Escape($marker) + "[^\r\n]*")
            foreach ($m in $found) {
                $matchedLines += $m.Value.Trim()
            }
        }
    } catch {
        $matchedLines += "ERROR reading file: $($_.Exception.Message)"
    }

    if ($matchedLines.Count -gt 0) {
        $relPath = $logFile.Replace($pzUserDir, "~\Zomboid")
        $harvestLines += "--- $relPath ---"
        foreach ($line in $matchedLines) {
            $harvestLines += $line
            $totalMatchLines++
        }
        $harvestLines += ""
        Write-Host "  $relPath : $($matchedLines.Count) match(es)"
    }
}

$harvestLines += ""
$harvestLines += "SUMMARY"
$harvestLines += "======="
$harvestLines += "Total matched lines: $totalMatchLines"

if ($totalMatchLines -eq 0) {
    $harvestLines += "No MAP-22B markers found."
    $harvestLines += "The probe may not have run yet, or PZ did not load the WorldGen Lua."
    $harvestLines += "Steps to run:"
    $harvestLines += "  1. Run: powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-prefab-dump.ps1 -Install"
    $harvestLines += "  2. Launch PZ, host PZMF_B42_MAP9F_NO_MULDRAUGH_001"
    $harvestLines += "  3. Exit PZ"
    $harvestLines += "  4. Re-run this script"
    Write-Host ""
    Write-Host "No markers found. Probe may not have run yet."
} else {
    Write-Host ""
    Write-Host "Found $totalMatchLines matching line(s)."
}

Set-Content -Path $harvestPath -Value ($harvestLines -join "`n") -Encoding UTF8
Write-Host ""
Write-Host "Harvest written to: $harvestPath"

exit 0
