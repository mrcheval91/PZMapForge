#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7P: prepare-build42-map7p-known-working-runtime-baseline-packet.ps1
    and DruMapBaseline classifier in inspect-build42-map7d-load-result.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map7p-known-working-runtime-baseline-packet.ps1'
$analyzerScript = Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7p-known-working-runtime-baseline.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7p-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7p-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 1: Packet refuses output outside .local
Write-Output '--- Test 1: Packet refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: packet output outside .local exits nonzero'

# Run packet
Write-Output ''
Write-Output '--- Running packet (Tests 2-18) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Packet exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Tests 3-9: Required files present
$reqFiles = @(
    'MAP_7P_KNOWN_WORKING_RUNTIME_BASELINE_PACKET.md',
    'MAP_7P_VARIANT_I_RESULT_SUMMARY.md',
    'MAP_7P_DRUMAP_BASELINE_MANUAL_SERVER_WIRING.md',
    'MAP_7P_DRUMAP_BASELINE_LOG_CAPTURE_COMMANDS.md',
    'MAP_7P_NEXT_DECISION_TREE.md',
    'map7p-preflight.json',
    'map7p-preflight.md'
)
Write-Output ''
Write-Output '--- Tests 3-9: Required files present ---'
foreach ($f in $reqFiles) {
    $exists = Test-Path (Join-Path $packetOut $f)
    Assert-True $exists "Test: $f exists"
}

# Tests 10-11: Preflight JSON fields
Write-Output ''
Write-Output '--- Tests 10-11: Preflight JSON fields ---'
$preflightPath = Join-Path $packetOut 'map7p-preflight.json'
$pfl = if (Test-Path $preflightPath) {
    Get-Content $preflightPath -Raw | ConvertFrom-Json
} else { $null }

Assert-True ($null -ne $pfl -and [string]$pfl.variant_i_result -eq 'MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY') `
    'Test10: preflight variant_i_result=MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY'

Assert-True ($null -ne $pfl -and [bool]$pfl.variants_abcdefghi_exhausted -eq $true) `
    'Test11: preflight variants_abcdefghi_exhausted=true'

# Tests 12-18: Packet doc content checks
Write-Output ''
Write-Output '--- Tests 12-18: Packet doc content ---'
$packetDocPath = Join-Path $packetOut 'MAP_7P_KNOWN_WORKING_RUNTIME_BASELINE_PACKET.md'
$packetDocContent = if (Test-Path $packetDocPath) { Get-Content $packetDocPath -Raw } else { '' }

Assert-True ($packetDocContent -match 'WorkshopItems=3355966216') `
    'Test12: packet contains WorkshopItems=3355966216'

Assert-True ($packetDocContent -match 'Mods=Dru_map') `
    'Test13: packet contains Mods=Dru_map'

Assert-True ($packetDocContent -match 'Map=Dru_map;Muldraugh, KY') `
    'Test14: packet contains Map=Dru_map;Muldraugh, KY'

Assert-True ($packetDocContent -match '-ExpectedMapId Dru_map') `
    'Test15: packet contains -ExpectedMapId Dru_map'

Assert-True ($packetDocContent -match '-VariantLabel DruMapBaseline') `
    'Test16: packet contains -VariantLabel DruMapBaseline'

Assert-True ($packetDocContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') `
    'Test17: packet contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false'

Assert-True ($packetDocContent -match 'LOAD_TEST_NOT_PERFORMED_BY_SCRIPT') `
    'Test18: packet contains LOAD_TEST_NOT_PERFORMED_BY_SCRIPT'

# Tests 19-20: Analyzer DruMapBaseline classifications
Write-Output ''
Write-Output '--- Tests 19-20: Analyzer DruMapBaseline classifications ---'

$logBase = Join-Path $testBase '.local\logs'
New-Item -ItemType Directory -Force -Path $logBase | Out-Null

# Synthetic log: DruMapBaseline FOUND (Dru_map appears in folder list)
$logFoundPath = Join-Path $logBase 'drumap-baseline-found.txt'
$logFoundContent = @'
[2024-01-01 00:00:00.000] LOG  : General         f:0 st:0> loading Dru_map
[2024-01-01 00:00:01.000] LOG  : General         f:0 st:0> IsoMetaGrid.Create
[2024-01-01 00:00:02.000] LOG  : General         f:0 st:0> Looking in these map folders:
[2024-01-01 00:00:02.001] LOG  : General         f:0 st:0> Dru_map
[2024-01-01 00:00:02.002] LOG  : General         f:0 st:0> Muldraugh, KY
[2024-01-01 00:00:02.003] LOG  : General         f:0 st:0> <End of map-folders list>
[2024-01-01 00:00:03.000] LOG  : General         f:0 st:0> IsoMetaGrid finished loading.
[2024-01-01 00:00:04.000] LOG  : General         f:0 st:0> Player data received from the server.
[2024-01-01 00:00:05.000] LOG  : General         f:0 st:0> game loading took 5 seconds.
'@
Set-Content -Path $logFoundPath -Value $logFoundContent -Encoding ASCII

$analyzerFoundOutput = Join-Path $testBase '.local\analysis-drumap-found'
& powershell -ExecutionPolicy Bypass -File $analyzerScript `
    -LogPath $logFoundPath `
    -Output $analyzerFoundOutput `
    -ExpectedMapId 'Dru_map' `
    -VariantLabel 'DruMapBaseline' | Out-Null

$analyzerFoundJsonPath = Join-Path $analyzerFoundOutput 'map7d-load-result.json'
$analyzerFoundResult = if (Test-Path $analyzerFoundJsonPath) {
    (Get-Content $analyzerFoundJsonPath -Raw | ConvertFrom-Json).classification
} else { '' }

Assert-True ($analyzerFoundResult -eq 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND') `
    "Test19: analyzer DruMapBaseline found -> MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND (got '$analyzerFoundResult')"

# Synthetic log: DruMapBaseline EMPTY (empty map folder list)
$logEmptyPath = Join-Path $logBase 'drumap-baseline-empty.txt'
$logEmptyContent = @'
[2024-01-01 00:00:00.000] LOG  : General         f:0 st:0> loading Dru_map
[2024-01-01 00:00:01.000] LOG  : General         f:0 st:0> IsoMetaGrid.Create
[2024-01-01 00:00:02.000] LOG  : General         f:0 st:0> Looking in these map folders:
[2024-01-01 00:00:02.001] LOG  : General         f:0 st:0> <End of map-folders list>
[2024-01-01 00:00:03.000] LOG  : General         f:0 st:0> IsoMetaGrid finished loading.
[2024-01-01 00:00:04.000] LOG  : General         f:0 st:0> Player data received from the server.
[2024-01-01 00:00:05.000] LOG  : General         f:0 st:0> game loading took 5 seconds.
'@
Set-Content -Path $logEmptyPath -Value $logEmptyContent -Encoding ASCII

$analyzerEmptyOutput = Join-Path $testBase '.local\analysis-drumap-empty'
& powershell -ExecutionPolicy Bypass -File $analyzerScript `
    -LogPath $logEmptyPath `
    -Output $analyzerEmptyOutput `
    -ExpectedMapId 'Dru_map' `
    -VariantLabel 'DruMapBaseline' | Out-Null

$analyzerEmptyJsonPath = Join-Path $analyzerEmptyOutput 'map7d-load-result.json'
$analyzerEmptyResult = if (Test-Path $analyzerEmptyJsonPath) {
    (Get-Content $analyzerEmptyJsonPath -Raw | ConvertFrom-Json).classification
} else { '' }

Assert-True ($analyzerEmptyResult -eq 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY') `
    "Test20: analyzer DruMapBaseline empty -> MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY (got '$analyzerEmptyResult')"

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
