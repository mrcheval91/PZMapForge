#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7G: real PZ DebugLog prefix fix, -ExpectedMapId/-VariantLabel
    parameters, and MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY classification.

    Uses synthetic log fixtures with real PZ DebugLog format (f:N st:N>).
    Does NOT depend on user's Zomboid log files.
    Expected assertion count: 8
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$analyzerScript = Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1'
$tempRoot       = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7g-variant-a-failure.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic fixtures using real PZ DebugLog format
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t7g-' + [System.IO.Path]::GetRandomFileName())
$logDir   = Join-Path $testBase 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

# Real PZ format (f:0 st:0>) — empty map folder list + WARN lines present
$realEmptyLog = Join-Path $logDir 'real-empty.txt'
Set-Content -Path $realEmptyLog -Encoding ASCII -Value @"
[06-06-26 10:24:53.018] LOG  : Mod          f:0> loading pzmapforge_build42_candidate_v4_001.
[06-06-26 10:27:38.628] LOG  : General      f:0 st:0> IsoMetaGrid.Create: begin scanning directories.
[06-06-26 10:27:38.628] LOG  : General      f:0 st:0> Looking in these map folders:.
[06-06-26 10:27:38.629] LOG  : General      f:0 st:0> <End of map-folders list>.
[06-06-26 10:27:38.672] LOG  : General      f:0 st:0> IsoMetaGrid.Create: finished scanning directories in 0.044 seconds.
[06-06-26 10:27:38.734] LOG  : General      f:0 st:0> IsoMetaGrid.Create: begin loading.
[06-06-26 10:27:49.736] LOG  : General      f:0 st:0> IsoMetaGrid.Create: finished loading in 11.002 seconds.
[06-06-26 10:27:54.061] WARN : General      f:0 st:0 at SpawnPoints.initSpawnBuildings      > initSpawnBuildings: no room or building at 150,150,0.
[06-06-26 10:28:12.571] LOG  : General      f:0 st:0> game loading took 35 seconds.
[06-06-26 10:28:17.538] LOG  : General      f:0 st:0> STATE: exit zombie.gameStates.GameLoadingState.
[06-06-26 10:28:17.990] LOG  : General      f:0 st:0> Game Mode: Multiplayer.
"@

# Real PZ format — two entries in map folder list
$realWithFoldersLog = Join-Path $logDir 'real-with-folders.txt'
Set-Content -Path $realWithFoldersLog -Encoding ASCII -Value @"
[06-06-26 10:24:53.018] LOG  : Mod          f:0> loading pzmapforge_build42_candidate_v4_001.
[06-06-26 10:27:38.628] LOG  : General      f:0 st:0> Looking in these map folders:.
[06-06-26 10:27:38.629] LOG  : General      f:0 st:0> pzmapforge_build42_candidate_v4_001.
[06-06-26 10:27:38.630] LOG  : General      f:0 st:0> Muldraugh, KY.
[06-06-26 10:27:38.631] LOG  : General      f:0 st:0> <End of map-folders list>.
[06-06-26 10:28:17.538] LOG  : General      f:0 st:0> STATE: exit zombie.gameStates.GameLoadingState.
INFO: Player data received from the server
INFO: game loading took 32 seconds
"@

# Bare timeout fixture (regression: still classified correctly without new params)
$timeoutLog = Join-Path $logDir 'timeout.txt'
Set-Content -Path $timeoutLog -Encoding ASCII -Value @"
INFO: loading pzmapforge_build42_candidate_v4_001
ERROR: Timed out waiting for the server to send player data
"@

function Invoke-Analyzer {
    param([string]$LogFile, [string]$OutDir, [string]$MapId = '', [string]$Variant = '')
    $args2 = @('-ExecutionPolicy', 'Bypass', '-File', $analyzerScript,
                '-LogPath', $LogFile, '-Output', $OutDir)
    if ($MapId   -ne '') { $args2 += @('-ExpectedMapId', $MapId) }
    if ($Variant -ne '') { $args2 += @('-VariantLabel',  $Variant) }
    & powershell @args2 | Out-Null
    return [int]$LASTEXITCODE
}

function Read-Result {
    param([string]$Dir)
    $f = Join-Path $Dir 'map7d-load-result.json'
    if (Test-Path $f) { return Get-Content $f -Raw | ConvertFrom-Json }
    return $null
}

# ---------------------------------------------------------------------------
# Test 1: Real PZ format (f:0 st:0>) -> map_folders_list_empty=true
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Real PZ format empty map-folder list ---'
$out1 = Join-Path $testBase '.local\t1'
Invoke-Analyzer -LogFile $realEmptyLog -OutDir $out1 | Out-Null
$d1 = Read-Result $out1
Assert-True ([bool]$d1.map_folders_list_empty -eq $true) `
    "Test1: real f:0 st:0> format -> map_folders_list_empty == true"

# ---------------------------------------------------------------------------
# Test 2: Real PZ format -> timestamped_debuglog_detected=true
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Real PZ format -> timestamped_debuglog_detected ---'
Assert-True ([bool]$d1.timestamped_debuglog_detected -eq $true) `
    "Test2: real format -> timestamped_debuglog_detected == true"

# ---------------------------------------------------------------------------
# Test 3: Real PZ WARN line does not pollute map folder list
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 3: WARN line does not appear as folder entry ---'
Assert-True ([int]$d1.map_folders_list_count -eq 0) `
    "Test3: WARN SpawnPoints line not counted as folder entry (count=0, got $([int]$d1.map_folders_list_count))"

# ---------------------------------------------------------------------------
# Test 4: Real PZ format with two folder entries -> count=2
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 4: Real PZ format with folders -> count=2 ---'
$out4 = Join-Path $testBase '.local\t4'
Invoke-Analyzer -LogFile $realWithFoldersLog -OutDir $out4 | Out-Null
$d4 = Read-Result $out4
Assert-True ([int]$d4.map_folders_list_count -eq 2) `
    "Test4: real format with 2 folder entries -> map_folders_list_count == 2 (got $([int]$d4.map_folders_list_count))"

# ---------------------------------------------------------------------------
# Test 5: -ExpectedMapId -VariantLabel VariantA + empty scan -> MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 5: VariantA + empty scan -> MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY ---'
$out5 = Join-Path $testBase '.local\t5'
Invoke-Analyzer -LogFile $realEmptyLog -OutDir $out5 `
    -MapId 'pzmapforge_build42_candidate_v4_001' -Variant 'VariantA' | Out-Null
$d5 = Read-Result $out5
Assert-True ([string]$d5.classification -eq 'MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY') `
    "Test5: VariantA empty scan -> MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY (got '$([string]$d5.classification)')"

# ---------------------------------------------------------------------------
# Test 6: -ExpectedMapId populated in report
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 6: expected_map_id field populated ---'
Assert-True ([string]$d5.expected_map_id -eq 'pzmapforge_build42_candidate_v4_001') `
    "Test6: expected_map_id == 'pzmapforge_build42_candidate_v4_001' (got '$([string]$d5.expected_map_id)')"

# ---------------------------------------------------------------------------
# Test 7: -VariantLabel populated in report
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 7: variant_label field populated ---'
Assert-True ([string]$d5.variant_label -eq 'VariantA') `
    "Test7: variant_label == 'VariantA' (got '$([string]$d5.variant_label)')"

# ---------------------------------------------------------------------------
# Test 8: Without new params -> timeout still classified correctly (regression)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 8: Timeout still classified without new params ---'
$out8 = Join-Path $testBase '.local\t8'
Invoke-Analyzer -LogFile $timeoutLog -OutDir $out8 | Out-Null
$d8 = Read-Result $out8
Assert-True ([string]$d8.classification -eq 'MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA') `
    "Test8: timeout without params -> MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA (got '$([string]$d8.classification)')"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
