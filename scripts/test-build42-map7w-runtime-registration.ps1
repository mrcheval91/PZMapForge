#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7W: inspect-build42-map-registration-contract.ps1 and
    prepare-build42-map7w-runtime-registration-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir       = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot        = Split-Path -Parent $scriptDir
$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-map-registration-contract.ps1'
$packetScript    = Join-Path $repoRoot 'scripts\prepare-build42-map7w-runtime-registration-packet.ps1'
$tempRoot        = [System.IO.Path]::GetTempPath()
$candidateMapId  = 'pzmapforge_build42_candidate_v4_001'
$referenceMapId  = 'Dru_map'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7w-runtime-registration.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7w-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7w-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# ---------------------------------------------------------------------------
# Build synthetic mod root fixtures
# ---------------------------------------------------------------------------

# Reference root: has map.bin, SpawnPoints() style spawnpoints
$refRoot     = Join-Path $testBase 'reference-modroot'
$refMapsDir  = Join-Path $refRoot "common\media\maps\$referenceMapId"
New-Item -ItemType Directory -Force -Path (Join-Path $refRoot '42') | Out-Null
New-Item -ItemType Directory -Force -Path $refMapsDir | Out-Null
Set-Content -Path (Join-Path $refRoot 'mod.info')    -Value "id=$referenceMapId`nname=DruMap`nmodversion=2.0`n" -Encoding ASCII
Set-Content -Path (Join-Path $refRoot '42\mod.info') -Value "id=$referenceMapId`nname=DruMap`nmodversion=2.0`n" -Encoding ASCII
Set-Content -Path (Join-Path $refMapsDir 'map.info') `
    -Value "title=Dru_map`nlots=NONE`nzoomX=10505`nzoomY=12220`nzoomS=14.5`nfixed2x=true`n" -Encoding ASCII
Set-Content -Path (Join-Path $refMapsDir 'map.bin')  -Value 'dummy' -Encoding ASCII
Set-Content -Path (Join-Path $refMapsDir 'worldmap.xml') -Value '<worldmap/>' -Encoding ASCII
Set-Content -Path (Join-Path $refMapsDir 'spawnpoints.lua') `
    -Value "function SpawnPoints()`n  return { Unemployed = { { worldX = 35, worldY = 27, posX = 240, posY = 183, posZ = 0 } } }`nend`n" -Encoding ASCII
Set-Content -Path (Join-Path $refMapsDir 'objects.lua')  -Value '-- objects' -Encoding ASCII
[System.IO.File]::WriteAllBytes((Join-Path $refMapsDir '35_27.lotheader'), [System.Byte[]]::new(64))

# Candidate root: NO map.bin, bare return style spawnpoints, different zoom
$candRoot    = Join-Path $testBase 'candidate-modroot'
$candMapsDir = Join-Path $candRoot "common\media\maps\$candidateMapId"
New-Item -ItemType Directory -Force -Path (Join-Path $candRoot '42') | Out-Null
New-Item -ItemType Directory -Force -Path $candMapsDir | Out-Null
Set-Content -Path (Join-Path $candRoot 'mod.info')    -Value "id=$candidateMapId`nname=PZMapForge`nmodversion=1.0`n" -Encoding ASCII
Set-Content -Path (Join-Path $candRoot '42\mod.info') -Value "id=$candidateMapId`nname=PZMapForge`nmodversion=1.0`n" -Encoding ASCII
Set-Content -Path (Join-Path $candMapsDir 'map.info') `
    -Value "title=Dru_map`nlots=NONE`nzoomX=10505`nzoomY=12220`nzoomS=14.5`nfixed2x=true`n" -Encoding ASCII
# NO map.bin
Set-Content -Path (Join-Path $candMapsDir 'worldmap.xml') -Value '<worldmap/>' -Encoding ASCII
Set-Content -Path (Join-Path $candMapsDir 'spawnpoints.lua') `
    -Value "local sp = { Profession_Unemployed = { { worldX = 35, worldY = 27, posX = 246, posY = 188, posZ = 0 } } }`nreturn sp`n" -Encoding ASCII
Set-Content -Path (Join-Path $candMapsDir 'objects.lua')  -Value '-- objects' -Encoding ASCII
[System.IO.File]::WriteAllBytes((Join-Path $candMapsDir '35_27.lotheader'), [System.Byte[]]::new(64))

# Identical candidate root (for exact_file_set_match=true test)
$identRoot    = Join-Path $testBase 'identical-modroot'
$identMapsDir = Join-Path $identRoot "common\media\maps\$candidateMapId"
New-Item -ItemType Directory -Force -Path (Join-Path $identRoot '42') | Out-Null
New-Item -ItemType Directory -Force -Path $identMapsDir | Out-Null
Set-Content -Path (Join-Path $identRoot 'mod.info')    -Value "id=$candidateMapId`n" -Encoding ASCII
Set-Content -Path (Join-Path $identRoot '42\mod.info') -Value "id=$candidateMapId`n" -Encoding ASCII
Set-Content -Path (Join-Path $identMapsDir 'map.info') `
    -Value "title=Dru_map`nlots=NONE`nzoomX=10505`n" -Encoding ASCII
Set-Content -Path (Join-Path $identMapsDir 'map.bin')   -Value 'dummy' -Encoding ASCII
Set-Content -Path (Join-Path $identMapsDir 'worldmap.xml') -Value '<worldmap/>' -Encoding ASCII
Set-Content -Path (Join-Path $identMapsDir 'spawnpoints.lua') -Value "return {}`n" -Encoding ASCII
Set-Content -Path (Join-Path $identMapsDir 'objects.lua')  -Value '-- objects' -Encoding ASCII

# Synthetic log file for log parsing tests
$logRoot = Join-Path $testBase '.local\logs'
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
Set-Content -Path (Join-Path $logRoot 'test.txt') -Value @"
[2024-01-01 00:00:00.000] LOG  : General         f:0 st:0> Workshop: Ready 3740642200
[2024-01-01 00:00:01.000] LOG  : General         f:0 st:0> loading pzmapforge_build42_candidate_v4_001
[2024-01-01 00:00:02.000] LOG  : General         f:0 st:0> Looking in these map folders:
[2024-01-01 00:00:02.001] LOG  : General         f:0 st:0> <End of map-folders list>
[2024-01-01 00:00:03.000] LOG  : General         f:0 st:0> IsoMetaGrid.Create begin
[2024-01-01 00:00:04.000] LOG  : General         f:0 st:0> SANITY CHECK FAIL at some location
"@ -Encoding ASCII

function Invoke-Inspector {
    param([string]$CandRoot, [string]$RefRoot, [string]$CandMapId, [string]$RefMapId,
          [string]$OutDir, [string]$CandLogs = '', [string]$RefLogs = '')
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    $argList = @(
        '-CandidateModRoot', $CandRoot,
        '-ReferenceModRoot', $RefRoot,
        '-CandidateMapId',   $CandMapId,
        '-ReferenceMapId',   $RefMapId,
        '-Output',           $OutDir
    )
    if ($CandLogs -ne '') { $argList += @('-CandidateLogsRoot', $CandLogs) }
    if ($RefLogs  -ne '') { $argList += @('-ReferenceLogsRoot',  $RefLogs) }
    & powershell -ExecutionPolicy Bypass -File $inspectorScript @argList | Out-Null
    return [int]$LASTEXITCODE
}

function Invoke-Packet { param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# ---------------------------------------------------------------------------
# Test 1: Inspector refuses output outside .local
# ---------------------------------------------------------------------------
Write-Output '--- Test 1: Inspector refuses output outside .local ---'
$t1exit = Invoke-Inspector -CandRoot $candRoot -RefRoot $refRoot `
    -CandMapId $candidateMapId -RefMapId $referenceMapId -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: inspector outside .local exits nonzero'

# Run inspector: candidate (no map.bin) vs reference (has map.bin)
Write-Output ''
Write-Output '--- Running inspector: cand-vs-ref (Tests 2-10) ---'
$inspOut1 = Join-Path $testBase '.local\insp1'
$t2exit   = Invoke-Inspector -CandRoot $candRoot -RefRoot $refRoot `
    -CandMapId $candidateMapId -RefMapId $referenceMapId `
    -OutDir $inspOut1 -CandLogs $logRoot

# Test 2: Inspector exits 0 + JSON exists
Assert-True ($t2exit -eq 0 -and (Test-Path (Join-Path $inspOut1 'map-registration-contract.json'))) `
    'Test2: inspector exits 0 and JSON exists'

# Test 3: Inspector writes MD
Assert-True (Test-Path (Join-Path $inspOut1 'map-registration-contract.md')) `
    'Test3: inspector writes map-registration-contract.md'

$j = if (Test-Path (Join-Path $inspOut1 'map-registration-contract.json')) {
    Get-Content (Join-Path $inspOut1 'map-registration-contract.json') -Raw | ConvertFrom-Json
} else { $null }

# Test 4: map.bin discriminator: reference has map.bin, candidate does not
Assert-True ($null -ne $j -and [bool]$j.reference_has_map_bin -eq $true -and
             [bool]$j.candidate_has_map_bin -eq $false) `
    'Test4: reference_has_map_bin=true, candidate_has_map_bin=false'

# Test 5: map_bin_discriminator=true
Assert-True ($null -ne $j -and [bool]$j.map_bin_discriminator -eq $true) `
    'Test5: map_bin_discriminator=true'

# Test 6: reference_files_missing_in_candidate_count > 0 (map.bin missing)
Assert-True ($null -ne $j -and [int]$j.reference_files_missing_in_candidate_count -gt 0) `
    "Test6: reference_files_missing_in_candidate_count>0 (got $($j.reference_files_missing_in_candidate_count))"

# Test 7: mod.info id value extracted (via mod_info_value_differences or key presence)
Assert-True ($null -ne $j -and $null -ne $j.candidate_map_info_keys) `
    'Test7: candidate_map_info_keys field present in JSON'

# Test 8: map.info has no value differences when values match (both have zoomX=10505)
Assert-True ($null -ne $j -and [int]$j.map_info_value_differences_count -eq 0) `
    'Test8: map_info_value_differences_count=0 (zoom values match)'

# Test 9: Spawnpoints style detection (candidate uses bare return style)
Assert-True ($null -ne $j -and
             [bool]$j.candidate_spawnpoints.is_return_style -eq $true -and
             [bool]$j.candidate_spawnpoints.is_function_style -eq $false) `
    'Test9: candidate spawnpoints is_return_style=true (bare return)'

# Test 10: Reference spawnpoints uses function SpawnPoints() style
Assert-True ($null -ne $j -and [bool]$j.reference_spawnpoints.is_function_style -eq $true) `
    'Test10: reference spawnpoints is_function_style=true'

# Test 11: Log parser detects empty map-folder scan
Assert-True ($null -ne $j -and $j.candidate_log.map_folder_scan_empty -eq $true) `
    'Test11: candidate_log map_folder_scan_empty=true from synthetic log'

# Test 12: Log parser detects Workshop Ready
Assert-True ($null -ne $j -and [bool]$j.candidate_log.workshop_ready_seen -eq $true) `
    'Test12: candidate_log workshop_ready_seen=true'

# Run inspector: identical file sets (both use identRoot mapped to candidateMapId)
Write-Output ''
Write-Output '--- Running inspector: identical sets (Test 13) ---'
$inspOut2 = Join-Path $testBase '.local\insp2'
$t13exit  = Invoke-Inspector -CandRoot $identRoot -RefRoot $identRoot `
    -CandMapId $candidateMapId -RefMapId $candidateMapId -OutDir $inspOut2
$j2 = if (Test-Path (Join-Path $inspOut2 'map-registration-contract.json')) {
    Get-Content (Join-Path $inspOut2 'map-registration-contract.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ($null -ne $j2 -and [bool]$j2.exact_file_set_match -eq $true) `
    'Test13: identical file sets yields exact_file_set_match=true'

# Tests 14-15: Safety flags in JSON
Write-Output ''
Write-Output '--- Tests 14-15: Safety flags ---'
Assert-True ($null -ne $j -and [bool]$j.binary_writer_gate_closed -eq $true) `
    'Test14: binary_writer_gate_closed=true'
Assert-True ($null -ne $j -and [bool]$j.public_playable_claim_allowed -eq $false) `
    'Test15: public_playable_claim_allowed=false'

# Tests 16-17: Packet tests
Write-Output ''
Write-Output '--- Tests 16-17: Packet ---'
$t16exit = Invoke-Packet -OutDir $badPath
Assert-True ($t16exit -ne 0) 'Test16: packet outside .local exits nonzero'

$packetOut = Join-Path $testBase '.local\packet'
$t17exit   = Invoke-Packet -OutDir $packetOut
$reqFiles = @(
    'MAP_7W_RUNTIME_REGISTRATION_PACKET.md',
    'MAP_7W_FILE_SET_DISCRIMINATORS.md',
    'MAP_7W_LOG_EVIDENCE_PLAN.md',
    'MAP_7W_NEXT_DECISION_TREE.md',
    'map7w-preflight.json',
    'map7w-preflight.md'
)
$allPresent = $true
foreach ($f in $reqFiles) { if (-not (Test-Path (Join-Path $packetOut $f))) { $allPresent = $false } }
Assert-True ($t17exit -eq 0 -and $allPresent) `
    'Test17: packet exits 0 and all 6 required docs exist'

# Test 18: Preflight next_branch
$pfl = if (Test-Path (Join-Path $packetOut 'map7w-preflight.json')) {
    Get-Content (Join-Path $packetOut 'map7w-preflight.json') -Raw | ConvertFrom-Json
} else { $null }
Assert-True ($null -ne $pfl -and [string]$pfl.next_branch -eq 'runtime_map_registration_and_mounting') `
    "Test18: preflight next_branch=runtime_map_registration_and_mounting"

# Tests 19-20: Doc content
Write-Output ''
Write-Output '--- Tests 19-20: Doc content ---'
$packetContent = Get-Content (Join-Path $packetOut 'MAP_7W_RUNTIME_REGISTRATION_PACKET.md') -Raw
Assert-True ($packetContent -match 'NO_PZ_RUN_BY_SCRIPT|no PZ run|does NOT run|does not run') `
    'Test19: main packet doc mentions no PZ run'

$treeContent = Get-Content (Join-Path $packetOut 'MAP_7W_NEXT_DECISION_TREE.md') -Raw
Assert-True ($treeContent -match 'runtime_map_registration_and_mounting') `
    'Test20: decision tree doc mentions runtime_map_registration_and_mounting'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
