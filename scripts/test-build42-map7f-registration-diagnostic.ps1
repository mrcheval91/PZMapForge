#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7F: timestamped DebugLog analyzer fix and
    prepare-build42-map7f-registration-diagnostic-packet.ps1.

    Uses synthetic log fixtures; does NOT depend on user's Zomboid log files.
    Expected assertion count: 11
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$analyzerScript = Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1'
$packetScript   = Join-Path $repoRoot 'scripts\prepare-build42-map7f-registration-diagnostic-packet.ps1'
$tempRoot       = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7f-registration-diagnostic.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic logs and temp dirs
# ---------------------------------------------------------------------------

$testBase  = Join-Path $tempRoot ('pzmf-t7f-' + [System.IO.Path]::GetRandomFileName())
$logDir    = Join-Path $testBase 'logs'
$badPath   = Join-Path $tempRoot 'pzmf-t7f-bad-no-local'

New-Item -ItemType Directory -Force -Path $logDir  | Out-Null
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# Synthetic: bare format, PARTIAL_PASS with empty map-folder list (existing behavior)
$bareEmptyLog = Join-Path $logDir 'bare-empty.txt'
Set-Content -Path $bareEmptyLog -Encoding ASCII -Value @"
INFO: loading pzmapforge_build42_candidate_v4_001
INFO: Player data received from the server
INFO: game loading took 32 seconds
INFO: STATE: exit zombie.gameStates.GameLoadingState
INFO: STATE: exit zombie.gameStates.IngameState
Looking in these map folders:
<End of map-folders list>
WARN: initSpawnBuildings: no room or building at 150,150,0
"@

# Synthetic: real PZ DebugLog format (f:N st:N>), empty map-folder list
$tsEmptyLog = Join-Path $logDir 'ts-empty.txt'
Set-Content -Path $tsEmptyLog -Encoding ASCII -Value @"
INFO: loading pzmapforge_build42_candidate_v4_001
INFO: Player data received from the server
INFO: game loading took 32 seconds
INFO: STATE: exit zombie.gameStates.GameLoadingState
INFO: STATE: exit zombie.gameStates.IngameState
[06-06-26 10:27:38.628] LOG  : General      f:0 st:0> Looking in these map folders:.
[06-06-26 10:27:38.629] LOG  : General      f:0 st:0> <End of map-folders list>.
[06-06-26 10:27:54.061] WARN : General      f:0 st:0 at SpawnPoints.initSpawnBuildings      > initSpawnBuildings: no room or building at 150,150,0.
"@

# Synthetic: real PZ DebugLog format, with folder entries between markers
$tsWithFoldersLog = Join-Path $logDir 'ts-with-folders.txt'
Set-Content -Path $tsWithFoldersLog -Encoding ASCII -Value @"
INFO: loading pzmapforge_build42_candidate_v4_001
INFO: Player data received from the server
INFO: game loading took 32 seconds
INFO: STATE: exit zombie.gameStates.GameLoadingState
[06-06-26 10:27:38.628] LOG  : General      f:0 st:0> Looking in these map folders:.
[06-06-26 10:27:38.629] LOG  : General      f:0 st:0> pzmapforge_build42_candidate_v4_001.
[06-06-26 10:27:38.630] LOG  : General      f:0 st:0> Muldraugh, KY.
[06-06-26 10:27:38.631] LOG  : General      f:0 st:0> <End of map-folders list>.
"@

# Synthetic: timeout log (bare format - existing behavior preserved)
$timeoutLog = Join-Path $logDir 'timeout.txt'
Set-Content -Path $timeoutLog -Encoding ASCII -Value @"
INFO: loading pzmapforge_build42_candidate_v4_001
ERROR: Timed out waiting for the server to send player data
"@

# Synthetic: LexState log (bare format - existing behavior preserved)
$lexstateLog = Join-Path $logDir 'lexstate.txt'
Set-Content -Path $lexstateLog -Encoding ASCII -Value @"
INFO: loading pzmapforge_build42_candidate_v4_001
SEVERE: LexState.token2str
ERROR: ArrayIndexOutOfBoundsException: Index 65022 out of bounds for length 31
"@

function Invoke-Analyzer {
    param([string]$LogFile, [string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $analyzerScript `
        -LogPath $LogFile -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ---------------------------------------------------------------------------
# Test 1: Analyzer still detects timeout (bare format -- existing behavior)
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Analyzer still detects timeout ---'
$out1 = Join-Path $testBase '.local\t1'
Invoke-Analyzer -LogFile $timeoutLog -OutDir $out1 | Out-Null
$d1 = if (Test-Path (Join-Path $out1 'map7d-load-result.json')) {
    Get-Content (Join-Path $out1 'map7d-load-result.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ([string]$d1.classification -eq 'MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA') `
    "Test1: timeout log -> MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA"

# ---------------------------------------------------------------------------
# Test 2: Analyzer still detects LexState (bare format -- existing behavior)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Analyzer still detects LexState ---'
$out2 = Join-Path $testBase '.local\t2'
Invoke-Analyzer -LogFile $lexstateLog -OutDir $out2 | Out-Null
$d2 = if (Test-Path (Join-Path $out2 'map7d-load-result.json')) {
    Get-Content (Join-Path $out2 'map7d-load-result.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ([string]$d2.classification -eq 'MAP7D_LOAD_TEST_FAIL_LUA_BOM_OR_LEXSTATE') `
    "Test2: LexState log -> MAP7D_LOAD_TEST_FAIL_LUA_BOM_OR_LEXSTATE"

# ---------------------------------------------------------------------------
# Test 3: Bare format empty map-folder list still detected (existing behavior)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 3: Bare format empty map-folder list detected ---'
$out3 = Join-Path $testBase '.local\t3'
Invoke-Analyzer -LogFile $bareEmptyLog -OutDir $out3 | Out-Null
$d3 = if (Test-Path (Join-Path $out3 'map7d-load-result.json')) {
    Get-Content (Join-Path $out3 'map7d-load-result.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ([bool]$d3.map_folders_list_empty -eq $true) `
    "Test3: bare format -> map_folders_list_empty == true"

# ---------------------------------------------------------------------------
# Test 4: Timestamped DebugLog format -> map_folders_list_empty=true (the fix)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 4: Timestamped format empty map-folder list detected ---'
$out4 = Join-Path $testBase '.local\t4'
Invoke-Analyzer -LogFile $tsEmptyLog -OutDir $out4 | Out-Null
$d4 = if (Test-Path (Join-Path $out4 'map7d-load-result.json')) {
    Get-Content (Join-Path $out4 'map7d-load-result.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ([bool]$d4.map_folders_list_empty -eq $true) `
    "Test4: timestamped format -> map_folders_list_empty == true"

# ---------------------------------------------------------------------------
# Test 5: Timestamped format -> timestamped_debuglog_detected=true
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 5: Timestamped format detected ---'
Assert-True ([bool]$d4.timestamped_debuglog_detected -eq $true) `
    "Test5: timestamped format -> timestamped_debuglog_detected == true"

# ---------------------------------------------------------------------------
# Test 6: Timestamped format with folder entries -> count > 0
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 6: Timestamped format with folders -> count > 0 ---'
$out6 = Join-Path $testBase '.local\t6'
Invoke-Analyzer -LogFile $tsWithFoldersLog -OutDir $out6 | Out-Null
$d6 = if (Test-Path (Join-Path $out6 'map7d-load-result.json')) {
    Get-Content (Join-Path $out6 'map7d-load-result.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ([int]$d6.map_folders_list_count -gt 0) `
    "Test6: timestamped format with folders -> map_folders_list_count > 0 (got $([int]$d6.map_folders_list_count))"

# ---------------------------------------------------------------------------
# Test 7: Timestamped empty map-folder -> PARTIAL_PASS classification
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 7: Timestamped format PARTIAL_PASS classification ---'
Assert-True ([string]$d4.classification -eq 'MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD') `
    "Test7: timestamped empty map-folder -> MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD (got '$([string]$d4.classification)')"

# ---------------------------------------------------------------------------
# Test 8: MAP-7F packet refuses output outside .local
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 8: MAP-7F packet refuses output outside .local ---'
$t8Exit = Invoke-Packet -OutDir $badPath
Assert-True ($t8Exit -ne 0) 'Test8: packet output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 9: MAP-7F packet writes all required files
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 9: MAP-7F packet writes all required files ---'
$packetOut = Join-Path $testBase '.local\packet'
$t9Exit = Invoke-Packet -OutDir $packetOut
$reqFiles = @(
    'MAP_7F_REGISTRATION_DIAGNOSTIC_PACKET.md',
    'MAP_7F_MANUAL_RETEST_RECORD.local-template.md',
    'MAP_7F_MAP_LINE_VARIANTS_TO_TEST.md',
    'MAP_7F_LOG_CAPTURE_AND_ANALYSIS_COMMANDS.md',
    'map7f-registration-preflight.json',
    'map7f-registration-preflight.md'
)
$allPresent = $true
foreach ($f in $reqFiles) {
    if (-not (Test-Path (Join-Path $packetOut $f))) { $allPresent = $false }
}
Assert-True ($t9Exit -eq 0 -and $allPresent) 'Test9: packet exits 0 and all required files present'

# ---------------------------------------------------------------------------
# Test 10: Preflight JSON has public_playable_claim_allowed=false
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 10: Preflight public_playable_claim_allowed=false ---'
$pfl = if (Test-Path (Join-Path $packetOut 'map7f-registration-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7f-registration-preflight.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test10: preflight public_playable_claim_allowed == false'

# ---------------------------------------------------------------------------
# Test 11: Packet contains all three Map= variants and HUMAN-ONLY markers
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 11: Packet has all Map= variants and HUMAN-ONLY markers ---'
$variantsContent = if (Test-Path (Join-Path $packetOut 'MAP_7F_MAP_LINE_VARIANTS_TO_TEST.md')) {
    Get-Content (Join-Path $packetOut 'MAP_7F_MAP_LINE_VARIANTS_TO_TEST.md') -Raw
} else { '' }
$hasVariantA = $variantsContent -match 'Muldraugh, KY'
$hasVariantB = $variantsContent -match 'Map=pzmapforge_build42_candidate_v4_001'
$hasHumanOnly = $variantsContent -match 'HUMAN-ONLY'
Assert-True ($hasVariantA -and $hasVariantB -and $hasHumanOnly) `
    "Test11: variants doc has variant A (Muldraugh), variant B (candidate), and HUMAN-ONLY markers"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
