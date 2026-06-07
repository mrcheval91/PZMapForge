#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7Q: DruMapBaseline runtime success classifier and
    prepare-build42-map7q-runtime-activation-next-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$analyzerScript = Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1'
$packetScript   = Join-Path $repoRoot 'scripts\prepare-build42-map7q-runtime-activation-next-packet.ps1'
$tempRoot       = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7q-runtime-baseline-success.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7q-' + [System.IO.Path]::GetRandomFileName())
$logBase  = Join-Path $testBase '.local\logs'
New-Item -ItemType Directory -Force -Path $logBase | Out-Null

# ---------------------------------------------------------------------------
# Helper: run analyzer on a log file
# ---------------------------------------------------------------------------
function Invoke-Analyzer {
    param([string]$LogPath, [string]$OutDir, [string]$ExpectedMapId, [string]$VariantLabel)
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    & powershell -ExecutionPolicy Bypass -File $analyzerScript `
        -LogPath $LogPath `
        -Output $OutDir `
        -ExpectedMapId $ExpectedMapId `
        -VariantLabel $VariantLabel | Out-Null
    $jsonPath = Join-Path $OutDir 'map7d-load-result.json'
    if (Test-Path $jsonPath) {
        return (Get-Content $jsonPath -Raw | ConvertFrom-Json)
    }
    return $null
}

# ---------------------------------------------------------------------------
# Test 1: Analyzer classifies MAP7Q runtime success with workshop+mod+lotheader
#         even when map folder scan is empty
# ---------------------------------------------------------------------------
Write-Output '--- Test 1: MAP7Q runtime success (empty scan + runtime evidence) ---'

$logRuntimeSuccess = Join-Path $logBase 'drumap-runtime-success.txt'
Set-Content -Path $logRuntimeSuccess -Value @'
[2024-01-01 00:00:00.000] LOG  : General         f:0 st:0> Workshop: Installed 3355966216
[2024-01-01 00:00:00.001] LOG  : General         f:0 st:0> Workshop: Ready 3355966216
[2024-01-01 00:00:01.000] LOG  : General         f:0 st:0> loading Dru_map mod
[2024-01-01 00:00:02.000] LOG  : General         f:0 st:0> IsoMetaGrid.Create
[2024-01-01 00:00:03.000] LOG  : General         f:0 st:0> Looking in these map folders:
[2024-01-01 00:00:03.001] LOG  : General         f:0 st:0> <End of map-folders list>
[2024-01-01 00:00:04.000] LOG  : General         f:0 st:0> IsoMetaGrid: loadCell 43_30.lotheader
[2024-01-01 00:00:04.001] LOG  : General         f:0 st:0> IsoMetaGrid: loadCell 42_31.lotheader
[2024-01-01 00:00:05.000] LOG  : General         f:0 st:0> Player data received from the server.
[2024-01-01 00:00:06.000] LOG  : General         f:0 st:0> game loading took 45 seconds.
[2024-01-01 00:00:07.000] LOG  : General         f:0 st:0> Game Mode: Multiplayer
'@ -Encoding ASCII

$r1 = Invoke-Analyzer -LogPath $logRuntimeSuccess `
    -OutDir (Join-Path $testBase '.local\analysis-t1') `
    -ExpectedMapId 'Dru_map' -VariantLabel 'DruMapBaseline'

Assert-True ($null -ne $r1 -and [string]$r1.classification -eq 'MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS') `
    "Test1: MAP7Q runtime success (got '$($r1.classification)')"

# ---------------------------------------------------------------------------
# Test 2: Analyzer keeps MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY when
#         no runtime success evidence (empty scan only)
# ---------------------------------------------------------------------------
Write-Output ''
Write-Output '--- Test 2: MAP7P empty when no runtime evidence ---'

$logEmptyNoEvidence = Join-Path $logBase 'drumap-empty-no-evidence.txt'
Set-Content -Path $logEmptyNoEvidence -Value @'
[2024-01-01 00:00:00.000] LOG  : General         f:0 st:0> loading Dru_map mod
[2024-01-01 00:00:01.000] LOG  : General         f:0 st:0> Looking in these map folders:
[2024-01-01 00:00:01.001] LOG  : General         f:0 st:0> <End of map-folders list>
[2024-01-01 00:00:02.000] LOG  : General         f:0 st:0> IsoMetaGrid finished loading.
[2024-01-01 00:00:03.000] LOG  : General         f:0 st:0> Player data received from the server.
[2024-01-01 00:00:04.000] LOG  : General         f:0 st:0> game loading took 5 seconds.
'@ -Encoding ASCII

$r2 = Invoke-Analyzer -LogPath $logEmptyNoEvidence `
    -OutDir (Join-Path $testBase '.local\analysis-t2') `
    -ExpectedMapId 'Dru_map' -VariantLabel 'DruMapBaseline'

Assert-True ($null -ne $r2 -and [string]$r2.classification -eq 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY') `
    "Test2: MAP7P empty (no runtime evidence) preserved (got '$($r2.classification)')"

# ---------------------------------------------------------------------------
# Test 3: Analyzer preserves MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY for VariantI
# ---------------------------------------------------------------------------
Write-Output ''
Write-Output '--- Test 3: MAP7F VariantI classification preserved ---'

$logVariantI = Join-Path $logBase 'variant-i-empty.txt'
Set-Content -Path $logVariantI -Value @'
[2024-01-01 00:00:00.000] LOG  : General         f:0 st:0> loading pzmapforge_build42_candidate_v4_001
[2024-01-01 00:00:01.000] LOG  : General         f:0 st:0> Looking in these map folders:
[2024-01-01 00:00:01.001] LOG  : General         f:0 st:0> <End of map-folders list>
[2024-01-01 00:00:02.000] LOG  : General         f:0 st:0> Player data received from the server.
[2024-01-01 00:00:03.000] LOG  : General         f:0 st:0> game loading took 40 seconds.
'@ -Encoding ASCII

$r3 = Invoke-Analyzer -LogPath $logVariantI `
    -OutDir (Join-Path $testBase '.local\analysis-t3') `
    -ExpectedMapId 'pzmapforge_build42_candidate_v4_001' -VariantLabel 'VariantI'

Assert-True ($null -ne $r3 -and [string]$r3.classification -eq 'MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY') `
    "Test3: MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY preserved (got '$($r3.classification)')"

# ---------------------------------------------------------------------------
# Packet tests
# ---------------------------------------------------------------------------

$badPath  = Join-Path $tempRoot 'pzmf-t7q-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 4: Packet refuses output outside .local
Write-Output ''
Write-Output '--- Test 4: Packet refuses output outside .local ---'
$t4exit = Invoke-Packet -OutDir $badPath
Assert-True ($t4exit -ne 0) 'Test4: packet outside .local exits nonzero'

# Test 5: Packet exits 0 with valid path
Write-Output ''
Write-Output '--- Test 5: Packet exits 0 ---'
$packetOut = Join-Path $testBase '.local\packet'
$t5exit    = Invoke-Packet -OutDir $packetOut
Assert-True ($t5exit -eq 0) 'Test5: packet exits 0 with valid output path'

# Tests 6-12: Required files present
Write-Output ''
Write-Output '--- Tests 6-12: Required files present ---'
$reqFiles = @(
    'MAP_7Q_RUNTIME_ACTIVATION_PACKET.md',
    'MAP_7Q_DRUMAP_BASELINE_RESULT_SUMMARY.md',
    'MAP_7Q_ANALYZER_EVIDENCE_MODEL.md',
    'MAP_7Q_NEXT_DECISION_TREE.md',
    'MAP_7Q_WORKSHOP_STYLE_ACTIVATION_HYPOTHESES.md',
    'map7q-preflight.json',
    'map7q-preflight.md'
)
foreach ($f in $reqFiles) {
    Assert-True (Test-Path (Join-Path $packetOut $f)) "Test: $f exists"
}

# Tests 13-16: Preflight JSON fields
Write-Output ''
Write-Output '--- Tests 13-16: Preflight JSON fields ---'
$preflightPath = Join-Path $packetOut 'map7q-preflight.json'
$pfl = if (Test-Path $preflightPath) { Get-Content $preflightPath -Raw | ConvertFrom-Json } else { $null }

Assert-True ($null -ne $pfl -and [bool]$pfl.drumap_baseline_runtime_success_model -eq $true) `
    'Test13: preflight drumap_baseline_runtime_success_model=true'

Assert-True ($null -ne $pfl -and [bool]$pfl.empty_client_scan_not_decisive -eq $true) `
    'Test14: preflight empty_client_scan_not_decisive=true'

Assert-True ($null -ne $pfl -and [bool]$pfl.variants_abcdefghi_exhausted -eq $true) `
    'Test15: preflight variants_abcdefghi_exhausted=true'

Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test16: preflight public_playable_claim_allowed=false'

# Tests 17-20: Packet doc content
Write-Output ''
Write-Output '--- Tests 17-20: Packet doc content ---'
$packetDocPath = Join-Path $packetOut 'MAP_7Q_RUNTIME_ACTIVATION_PACKET.md'
$packetDocContent = if (Test-Path $packetDocPath) { Get-Content $packetDocPath -Raw } else { '' }

Assert-True ($packetDocContent -match 'WorkshopItems=3355966216') `
    'Test17: packet contains WorkshopItems=3355966216'

Assert-True ($packetDocContent -match 'Dru_map') `
    'Test18: packet contains Dru_map'

$hypothesesPath = Join-Path $packetOut 'MAP_7Q_WORKSHOP_STYLE_ACTIVATION_HYPOTHESES.md'
$hypothesesContent = if (Test-Path $hypothesesPath) { Get-Content $hypothesesPath -Raw } else { '' }
Assert-True ($hypothesesContent -match 'NOT automatic|not automatic|NO_AUTOMATIC_WORKSHOP_UPLOAD') `
    'Test19: hypotheses doc mentions no automatic Workshop upload'

Assert-True ($packetDocContent -match 'LOAD_TEST_NOT_PERFORMED_BY_SCRIPT') `
    'Test20: packet contains LOAD_TEST_NOT_PERFORMED_BY_SCRIPT'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
