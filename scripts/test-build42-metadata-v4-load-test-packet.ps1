#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for prepare-build42-metadata-v4-load-test-packet.ps1 (MAP-7D).
    Expected assertion count: 15
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-metadata-v4-load-test-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Packet { param([string]$Output)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-metadata-v4-load-test-packet.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t7d-' + [System.IO.Path]::GetRandomFileName())
$goodOut  = Join-Path $testBase '.local\map7d-packet'
$badPath  = Join-Path $tempRoot 'pzmf-t7d-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# ---------------------------------------------------------------------------
# Test 1: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Output outside .local refused ---'
$t1Exit = Invoke-Packet -Output $badPath
Assert-True ($t1Exit -ne 0) 'Test1: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 2-15: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 2-15: valid packet run ---'
$t2Exit = Invoke-Packet -Output $goodOut

$packetMd  = Join-Path $goodOut 'MAP_7D_LOAD_TEST_PACKET.md'
$recordMd  = Join-Path $goodOut 'MAP_7D_LOAD_TEST_RECORD.local-template.md'
$wiringMd  = Join-Path $goodOut 'MAP_7D_INSTALL_AND_SERVER_WIRING_COMMANDS.md'
$preflJson = Join-Path $goodOut 'map7d-preflight.json'

Assert-True ($t2Exit -eq 0)       'Test2: script exits 0'
Assert-True (Test-Path $packetMd)  'Test3: packet markdown exists'
Assert-True (Test-Path $recordMd)  'Test4: record template exists'
Assert-True (Test-Path $wiringMd)  'Test5: wiring commands markdown exists'
Assert-True (Test-Path $preflJson) 'Test6: preflight JSON exists'

$pfl = if (Test-Path $preflJson) { Get-Content $preflJson -Raw | ConvertFrom-Json } else { $null }

# Test 7: profile == empty_grass_v4
$prof = if ($null -ne $pfl) { [string]$pfl.candidate_profile } else { '' }
Assert-True ($prof -eq 'empty_grass_v4') "Test7: preflight candidate_profile == empty_grass_v4 (got '$prof')"

# Test 8: no_bom_objects_lua == true
$noBomObj = if ($null -ne $pfl) { [bool]$pfl.no_bom_objects_lua } else { $false }
Assert-True ($noBomObj -eq $true) "Test8: preflight no_bom_objects_lua == true (got $noBomObj)"

# Test 9: no_bom_spawnpoints_lua == true
$noBomSp = if ($null -ne $pfl) { [bool]$pfl.no_bom_spawnpoints_lua } else { $false }
Assert-True ($noBomSp -eq $true) "Test9: preflight no_bom_spawnpoints_lua == true (got $noBomSp)"

# Test 10: loth_size == 29646
$ls = if ($null -ne $pfl) { [int]$pfl.loth_size } else { -1 }
Assert-True ($ls -eq 29646) "Test10: preflight loth_size == 29646 (got $ls)"

$pkContent  = if (Test-Path $packetMd) { Get-Content $packetMd  -Raw } else { '' }
$recContent = if (Test-Path $recordMd) { Get-Content $recordMd  -Raw } else { '' }

Assert-True ($pkContent -match 'HUMAN_ONLY_COPY_REQUIRED')            'Test11: packet contains HUMAN_ONLY_COPY_REQUIRED'
Assert-True ($pkContent -match 'LOAD_TEST_NOT_PERFORMED')             'Test12: packet contains LOAD_TEST_NOT_PERFORMED'
Assert-True ($pkContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'Test13: packet contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
Assert-True ($recContent -match 'LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA') 'Test14: record contains LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA'
Assert-True ($recContent -match 'LOAD_TEST_PASS')                     'Test15: record contains LOAD_TEST_PASS'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
