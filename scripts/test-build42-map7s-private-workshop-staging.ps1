#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-7S: prepare-build42-map7s-private-workshop-staging-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map7s-private-workshop-staging-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()
$candidateMapId = 'pzmapforge_build42_candidate_v4_001'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map7s-private-workshop-staging.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7s-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t7s-bad-no-local'
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
Write-Output '--- Running packet (Tests 2-20) ---'
$packetOut = Join-Path $testBase '.local\packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Packet exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Test 3: Staged package folder exists
Write-Output ''
Write-Output '--- Test 3: Staged package folder exists ---'
$stagedRoot = Join-Path $packetOut "staged-workshop\$candidateMapId"
Assert-True (Test-Path $stagedRoot) 'Test3: staged-workshop/<MapId> folder exists'

# Tests 4-11: Required packet docs
Write-Output ''
Write-Output '--- Tests 4-11: Required packet docs ---'
$reqFiles = @(
    'MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md',
    'MAP_7S_HUMAN_UPLOAD_CHECKLIST.md',
    'MAP_7S_SERVER_WIRING_AFTER_UPLOAD_TEMPLATE.md',
    'MAP_7S_LOG_CAPTURE_AFTER_UPLOAD.md',
    'MAP_7S_SUCCESS_FAILURE_CRITERIA.md',
    'MAP_7S_STAGED_PACKAGE_MANIFEST.md',
    'map7s-preflight.json',
    'map7s-preflight.md'
)
foreach ($f in $reqFiles) {
    Assert-True (Test-Path (Join-Path $packetOut $f)) "Test: $f exists"
}

# Tests 12-14: Preflight JSON fields
Write-Output ''
Write-Output '--- Tests 12-14: Preflight JSON fields ---'
$preflightPath = Join-Path $packetOut 'map7s-preflight.json'
$pfl = if (Test-Path $preflightPath) { Get-Content $preflightPath -Raw | ConvertFrom-Json } else { $null }

Assert-True ($null -ne $pfl -and [bool]$pfl.public_playable_claim_allowed -eq $false) `
    'Test12: preflight public_playable_claim_allowed=false'

Assert-True ($null -ne $pfl -and [bool]$pfl.automatic_workshop_upload_performed -eq $false) `
    'Test13: preflight automatic_workshop_upload_performed=false'

Assert-True ($null -ne $pfl -and [bool]$pfl.staged_package_created -eq $true) `
    'Test14: preflight staged_package_created=true'

# Tests 15-18: Checklist doc content
Write-Output ''
Write-Output '--- Tests 15-18: Checklist doc content ---'
$checklistPath = Join-Path $packetOut 'MAP_7S_HUMAN_UPLOAD_CHECKLIST.md'
$checklistContent = if (Test-Path $checklistPath) { Get-Content $checklistPath -Raw } else { '' }

Assert-True ($checklistContent -match 'HUMAN.ONLY|human.only|Human.Only') `
    'Test15: checklist mentions human-only upload'

Assert-True ($checklistContent -match '3355966216') `
    'Test16: checklist mentions do not use 3355966216'

Assert-True ($checklistContent -match 'PZMapForgeOwnWorkshopId|own.*Workshop.*[Ii][Dd]|Workshop.*[Ii][Dd].*own') `
    'Test17: checklist mentions own (not borrowed) Workshop ID placeholder'

Assert-True ($checklistContent -match 'expected_map_lotheader_meta_evidence_found') `
    'Test18: checklist references expected_map_lotheader_meta_evidence_found'

# Test 19: Success criteria mentions binary writer gate
Write-Output ''
Write-Output '--- Test 19: Success criteria binary writer gate ---'
$criteriaPath = Join-Path $packetOut 'MAP_7S_SUCCESS_FAILURE_CRITERIA.md'
$criteriaContent = if (Test-Path $criteriaPath) { Get-Content $criteriaPath -Raw } else { '' }
Assert-True ($criteriaContent -match 'BINARY_WRITER_GATE|binary writer gate|binary.*writer.*gate') `
    'Test19: success criteria mentions binary writer gate'

# Test 20: Staged package contains 0_0.lotheader
Write-Output ''
Write-Output '--- Test 20: Staged package contains 0_0.lotheader ---'
$lotheaderPath = Join-Path $stagedRoot "common\media\maps\$candidateMapId\0_0.lotheader"
Assert-True (Test-Path $lotheaderPath) `
    'Test20: staged package contains common/media/maps/<MapId>/0_0.lotheader'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
