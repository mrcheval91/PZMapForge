#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7E: inspect-build42-map7d-load-result.ps1 and
    prepare-build42-map7e-diagnostics-packet.ps1.

    Uses synthetic log fixtures; does NOT depend on user's Zomboid log files.
    Expected assertion count: 11
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot      = Split-Path -Parent $scriptDir
$analyzerScript = Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1'
$packetScript  = Join-Path $repoRoot 'scripts\prepare-build42-map7e-diagnostics-packet.ps1'
$tempRoot      = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7e-diagnostics.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: synthetic logs and temp dirs
# ---------------------------------------------------------------------------

$testBase   = Join-Path $tempRoot ('pzmf-t7e-' + [System.IO.Path]::GetRandomFileName())
$logDir     = Join-Path $testBase 'logs'
$goodOut    = Join-Path $testBase '.local\analysis'
$packetOut  = Join-Path $testBase '.local\packet'
$badPath    = Join-Path $tempRoot 'pzmf-t7e-bad-no-local'

New-Item -ItemType Directory -Force -Path $logDir  | Out-Null
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# Synthetic: PARTIAL_PASS log (MAP-7D successful result)
$partialPassLog = Join-Path $logDir 'partial-pass.txt'
Set-Content -Path $partialPassLog -Encoding ASCII -Value @"
INFO: loading pzmapforge_build42_candidate_v4_001
INFO: Player data received from the server
INFO: game loading took 32 seconds
INFO: STATE: exit zombie.gameStates.GameLoadingState
INFO: STATE: exit zombie.gameStates.IngameState
Looking in these map folders:
<End of map-folders list>
WARN: initSpawnBuildings: no room or building at 150,150,0
"@

# Synthetic: TIMEOUT log
$timeoutLog = Join-Path $logDir 'timeout.txt'
Set-Content -Path $timeoutLog -Encoding ASCII -Value @"
INFO: loading pzmapforge_build42_candidate_v4_001
ERROR: Timed out waiting for the server to send player data
"@

# Synthetic: LEXSTATE log
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
# Test 1: Analyzer refuses output outside .local
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Analyzer output outside .local refused ---'
$t1Exit = Invoke-Analyzer -LogFile $partialPassLog -OutDir $badPath
Assert-True ($t1Exit -ne 0) 'Test1: analyzer output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: PARTIAL_PASS classification
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: PARTIAL_PASS classification ---'
$out2 = Join-Path $testBase '.local\analysis-partial'
$t2Exit = Invoke-Analyzer -LogFile $partialPassLog -OutDir $out2
$data2 = if (Test-Path (Join-Path $out2 'map7d-load-result.json')) {
    Get-Content (Join-Path $out2 'map7d-load-result.json') -Raw | ConvertFrom-Json
} else { $null }
$class2 = if ($null -ne $data2) { [string]$data2.classification } else { '' }
Assert-True ($class2 -eq 'MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD') `
    "Test2: PARTIAL_PASS log → MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD (got '$class2')"

# ---------------------------------------------------------------------------
# Test 3: TIMEOUT classification
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 3: TIMEOUT classification ---'
$out3 = Join-Path $testBase '.local\analysis-timeout'
$t3Exit = Invoke-Analyzer -LogFile $timeoutLog -OutDir $out3
$data3 = if (Test-Path (Join-Path $out3 'map7d-load-result.json')) {
    Get-Content (Join-Path $out3 'map7d-load-result.json') -Raw | ConvertFrom-Json
} else { $null }
$class3 = if ($null -ne $data3) { [string]$data3.classification } else { '' }
Assert-True ($class3 -eq 'MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA') `
    "Test3: timeout log → MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA (got '$class3')"

# ---------------------------------------------------------------------------
# Test 4: LEXSTATE classification
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 4: LEXSTATE classification ---'
$out4 = Join-Path $testBase '.local\analysis-lexstate'
$t4Exit = Invoke-Analyzer -LogFile $lexstateLog -OutDir $out4
$data4 = if (Test-Path (Join-Path $out4 'map7d-load-result.json')) {
    Get-Content (Join-Path $out4 'map7d-load-result.json') -Raw | ConvertFrom-Json
} else { $null }
$class4 = if ($null -ne $data4) { [string]$data4.classification } else { '' }
Assert-True ($class4 -eq 'MAP7D_LOAD_TEST_FAIL_LUA_BOM_OR_LEXSTATE') `
    "Test4: LexState log → MAP7D_LOAD_TEST_FAIL_LUA_BOM_OR_LEXSTATE (got '$class4')"

# ---------------------------------------------------------------------------
# Test 5: map_folders_list_empty detected from PARTIAL_PASS log
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 5: map_folders_list_empty detected ---'
$mapEmpty = if ($null -ne $data2) { [bool]$data2.map_folders_list_empty } else { $false }
Assert-True ($mapEmpty -eq $true) "Test5: map_folders_list_empty == true (got $mapEmpty)"

# ---------------------------------------------------------------------------
# Test 6: spawn_building_warning detected from PARTIAL_PASS log
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 6: spawn_building_warning detected ---'
$spawnWarn = if ($null -ne $data2) { [bool]$data2.spawn_building_warning_found } else { $false }
Assert-True ($spawnWarn -eq $true) "Test6: spawn_building_warning_found == true (got $spawnWarn)"

# ---------------------------------------------------------------------------
# Test 7: Packet refuses output outside .local
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 7: Packet output outside .local refused ---'
$t7Exit = Invoke-Packet -OutDir $badPath
Assert-True ($t7Exit -ne 0) 'Test7: packet output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 8-11: Valid packet run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 8-11: valid packet run ---'
$t8Exit = Invoke-Packet -OutDir $packetOut

$packetMd    = Join-Path $packetOut 'MAP_7E_DIAGNOSTIC_PACKET.md'
$preflJson   = Join-Path $packetOut 'map7e-diagnostic-preflight.json'
$wiringMd    = Join-Path $packetOut 'MAP_7E_SERVER_WIRING_NO_BOM_TEMPLATE.md'

Assert-True ($t8Exit -eq 0) 'Test8: packet script exits 0'

# Test 9: required files exist
$filesExist = (Test-Path $packetMd) -and (Test-Path $preflJson)
Assert-True ($filesExist) 'Test9: DIAGNOSTIC_PACKET.md and preflight JSON exist'

# Test 10: preflight public_playable_claim_allowed=false
$pfl = if (Test-Path $preflJson) { Get-Content $preflJson -Raw | ConvertFrom-Json } else { $null }
$noPlayable = if ($null -ne $pfl) { [bool]$pfl.public_playable_claim_allowed -eq $false } else { $false }
Assert-True ($noPlayable) 'Test10: preflight public_playable_claim_allowed == false'

# Test 11: server wiring file has HUMAN-ONLY markers (no auto PZ writes)
$wiringContent = if (Test-Path $wiringMd) { Get-Content $wiringMd -Raw } else { '' }
Assert-True ($wiringContent -match 'HUMAN-ONLY') 'Test11: wiring template contains HUMAN-ONLY markers'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
