#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7R: MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT classifier and
    prepare-build42-map7r-workshop-activation-decision-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$analyzerScript = Join-Path $repoRoot 'scripts\inspect-build42-map7d-load-result.ps1'
$packetScript   = Join-Path $repoRoot 'scripts\prepare-build42-map7r-workshop-activation-decision-packet.ps1'
$tempRoot       = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7r-workshop-trigger-failure.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7r-' + [System.IO.Path]::GetRandomFileName())
$logBase  = Join-Path $testBase '.local\logs'
New-Item -ItemType Directory -Force -Path $logBase | Out-Null

function Invoke-Analyzer {
    param([string]$LogPath, [string]$OutDir, [string]$ExpectedMapId, [string]$VariantLabel)
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    & powershell -ExecutionPolicy Bypass -File $analyzerScript `
        -LogPath $LogPath `
        -Output $OutDir `
        -ExpectedMapId $ExpectedMapId `
        -VariantLabel $VariantLabel | Out-Null
    $jsonPath = Join-Path $OutDir 'map7d-load-result.json'
    if (Test-Path $jsonPath) { return (Get-Content $jsonPath -Raw | ConvertFrom-Json) }
    return $null
}

# ---------------------------------------------------------------------------
# Test 1: MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
#         Workshop Ready + candidate loaded + empty scan + no candidate lotheader
# ---------------------------------------------------------------------------
Write-Output '--- Test 1: MAP7R VariantJ insufficient ---'

$candidateMapId = 'pzmapforge_build42_candidate_v4_001'
$logVariantJ = Join-Path $logBase 'variant-j-workshop-trigger.txt'
Set-Content -Path $logVariantJ -Value @'
[2024-01-01 00:00:00.000] LOG  : General         f:0 st:0> Workshop: Installed 3355966216
[2024-01-01 00:00:00.001] LOG  : General         f:0 st:0> Workshop: Ready 3355966216
[2024-01-01 00:00:01.000] LOG  : General         f:0 st:0> loading pzmapforge_build42_candidate_v4_001
[2024-01-01 00:00:02.000] LOG  : General         f:0 st:0> IsoMetaGrid.Create
[2024-01-01 00:00:03.000] LOG  : General         f:0 st:0> Looking in these map folders:
[2024-01-01 00:00:03.001] LOG  : General         f:0 st:0> <End of map-folders list>
[2024-01-01 00:00:04.000] LOG  : General         f:0 st:0> IsoMetaGrid finished loading.
[2024-01-01 00:00:05.000] LOG  : General         f:0 st:0> Player data received from the server.
[2024-01-01 00:00:06.000] LOG  : General         f:0 st:0> game loading took 40 seconds.
[2024-01-01 00:00:07.000] LOG  : General         f:0 st:0> Game Mode: Multiplayer
[2024-01-01 00:00:08.000] LOG  : General         f:0 st:0> mannequin zone warning Muldraugh
'@ -Encoding ASCII

$r1 = Invoke-Analyzer -LogPath $logVariantJ `
    -OutDir (Join-Path $testBase '.local\analysis-t1') `
    -ExpectedMapId $candidateMapId -VariantLabel 'VariantJ'

Assert-True ($null -ne $r1 -and [string]$r1.classification -eq 'MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT') `
    "Test1: MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT (got '$($r1.classification)')"

# ---------------------------------------------------------------------------
# Test 2: Generic lotheader lines alone do NOT change VariantJ classification
#         Non-candidate lotheader lines are irrelevant
# ---------------------------------------------------------------------------
Write-Output ''
Write-Output '--- Test 2: Generic lotheader lines do not affect VariantJ result ---'

$logVariantJWithLotheader = Join-Path $logBase 'variant-j-generic-lotheader.txt'
Set-Content -Path $logVariantJWithLotheader -Value @'
[2024-01-01 00:00:00.000] LOG  : General         f:0 st:0> Workshop: Installed 3355966216
[2024-01-01 00:00:00.001] LOG  : General         f:0 st:0> Workshop: Ready 3355966216
[2024-01-01 00:00:01.000] LOG  : General         f:0 st:0> loading pzmapforge_build42_candidate_v4_001
[2024-01-01 00:00:02.000] LOG  : General         f:0 st:0> Looking in these map folders:
[2024-01-01 00:00:02.001] LOG  : General         f:0 st:0> <End of map-folders list>
[2024-01-01 00:00:03.000] LOG  : General         f:0 st:0> CellLoader: loading 43_30.lotheader
[2024-01-01 00:00:03.001] LOG  : General         f:0 st:0> CellLoader: loading 42_31.lotheader
[2024-01-01 00:00:04.000] LOG  : General         f:0 st:0> Player data received from the server.
[2024-01-01 00:00:05.000] LOG  : General         f:0 st:0> game loading took 40 seconds.
[2024-01-01 00:00:06.000] LOG  : General         f:0 st:0> Game Mode: Multiplayer
'@ -Encoding ASCII

$r2 = Invoke-Analyzer -LogPath $logVariantJWithLotheader `
    -OutDir (Join-Path $testBase '.local\analysis-t2') `
    -ExpectedMapId $candidateMapId -VariantLabel 'VariantJ'

Assert-True ($null -ne $r2 -and [string]$r2.classification -eq 'MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT') `
    "Test2: generic lotheader lines still MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT (got '$($r2.classification)')"

# ---------------------------------------------------------------------------
# Packet tests
# ---------------------------------------------------------------------------

$badPath  = Join-Path $tempRoot 'pzmf-t7r-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 3: Packet refuses output outside .local
Write-Output ''
Write-Output '--- Test 3: Packet refuses output outside .local ---'
$t3exit = Invoke-Packet -OutDir $badPath
Assert-True ($t3exit -ne 0) 'Test3: packet outside .local exits nonzero'

# Test 4: Packet exits 0 with valid path
Write-Output ''
Write-Output '--- Test 4: Packet exits 0 ---'
$packetOut = Join-Path $testBase '.local\packet'
$t4exit    = Invoke-Packet -OutDir $packetOut
Assert-True ($t4exit -eq 0) 'Test4: packet exits 0 with valid output path'

# Tests 5-11: Required files present
Write-Output ''
Write-Output '--- Tests 5-11: Required files present ---'
$reqFiles = @(
    'MAP_7R_WORKSHOP_ACTIVATION_DECISION_PACKET.md',
    'MAP_7R_VARIANT_J_RESULT_SUMMARY.md',
    'MAP_7R_NEXT_DECISION_TREE.md',
    'MAP_7R_PRIVATE_WORKSHOP_UPLOAD_REQUIREMENTS.md',
    'MAP_7R_NO_MORE_STATIC_LAYOUT_TESTS.md',
    'map7r-preflight.json',
    'map7r-preflight.md'
)
foreach ($f in $reqFiles) {
    Assert-True (Test-Path (Join-Path $packetOut $f)) "Test: $f exists"
}

# Tests 12-16: Preflight JSON fields
Write-Output ''
Write-Output '--- Tests 12-16: Preflight JSON fields ---'
$preflightPath = Join-Path $packetOut 'map7r-preflight.json'
$pfl = if (Test-Path $preflightPath) { Get-Content $preflightPath -Raw | ConvertFrom-Json } else { $null }

Assert-True ($null -ne $pfl -and [string]$pfl.variant_j_result -eq 'MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT') `
    'Test12: preflight variant_j_result=MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT'

Assert-True ($null -ne $pfl -and [bool]$pfl.borrowed_workshopitems_trigger_insufficient -eq $true) `
    'Test13: preflight borrowed_workshopitems_trigger_insufficient=true'

Assert-True ($null -ne $pfl -and [bool]$pfl.static_variants_abcdefghi_exhausted -eq $true) `
    'Test14: preflight static_variants_abcdefghi_exhausted=true'

Assert-True ($null -ne $pfl -and [bool]$pfl.no_more_static_layout_tests -eq $true) `
    'Test15: preflight no_more_static_layout_tests=true'

Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test16: preflight public_playable_claim_allowed=false'

# Tests 17-20: Packet doc content
Write-Output ''
Write-Output '--- Tests 17-20: Packet doc content ---'
$packetDocPath = Join-Path $packetOut 'MAP_7R_WORKSHOP_ACTIVATION_DECISION_PACKET.md'
$packetDocContent = if (Test-Path $packetDocPath) { Get-Content $packetDocPath -Raw } else { '' }

Assert-True ($packetDocContent -match 'WorkshopItems=3355966216') `
    'Test17: packet contains WorkshopItems=3355966216'

$uploadReqPath = Join-Path $packetOut 'MAP_7R_PRIVATE_WORKSHOP_UPLOAD_REQUIREMENTS.md'
$uploadReqContent = if (Test-Path $uploadReqPath) { Get-Content $uploadReqPath -Raw } else { '' }
Assert-True ($uploadReqContent -match 'NOT automatic|no_automatic_workshop_upload|NOT.*automatic') `
    'Test18: upload requirements doc mentions no automatic Workshop upload'

Assert-True ($uploadReqContent -match 'human approval|human.*approv|operator.*approv|explicit.*approv') `
    'Test19: upload requirements doc mentions human approval requirement'

$decisionTreePath = Join-Path $packetOut 'MAP_7R_NEXT_DECISION_TREE.md'
$decisionTreeContent = if (Test-Path $decisionTreePath) { Get-Content $decisionTreePath -Raw } else { '' }
Assert-True ($decisionTreeContent -match 'NO_BINARY_WRITER_CHANGES|no binary writer|binary writer.*not|binary_writer_changed=false') `
    'Test20: decision tree mentions no binary writer changes yet'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
