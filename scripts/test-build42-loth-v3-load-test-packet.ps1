#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for prepare-build42-loth-v3-load-test-packet.ps1 (MAP-7A).

    Runs the packet script against .local/ and validates its output.
    Does NOT copy files to PZ folders. Requires dotnet build to be current.
    Expected assertion count: 23
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-loth-v3-load-test-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Packet {
    param([string]$Output)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-loth-v3-load-test-packet.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup
# ---------------------------------------------------------------------------

$testBase = Join-Path $tempRoot ('pzmf-t7a-' + [System.IO.Path]::GetRandomFileName())
$goodOut  = Join-Path $testBase '.local\map7a-packet'
$badPath  = Join-Path $tempRoot 'pzmf-t7a-bad-no-local'

New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# ---------------------------------------------------------------------------
# Test 1: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Output outside .local refused ---'
$t1Exit = Invoke-Packet -Output $badPath
Assert-True ($t1Exit -ne 0) 'Test1: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 2-23: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 2-23: valid packet run ---'
$t2Exit = Invoke-Packet -Output $goodOut

$packetMd  = Join-Path $goodOut 'MAP_7A_LOAD_TEST_PACKET.md'
$recordMd  = Join-Path $goodOut 'MAP_7A_LOAD_TEST_RECORD.local-template.md'
$wiringMd  = Join-Path $goodOut 'MAP_7A_INSTALL_AND_SERVER_WIRING_COMMANDS.md'
$preflJson = Join-Path $goodOut 'map7a-preflight.json'

Assert-True ($t2Exit -eq 0)       'Test2: script exits 0'
Assert-True (Test-Path $packetMd)  'Test3: packet markdown exists'
Assert-True (Test-Path $recordMd)  'Test4: record template exists'
Assert-True (Test-Path $wiringMd)  'Test5: wiring commands markdown exists'
Assert-True (Test-Path $preflJson) 'Test6: preflight JSON exists'

$pfl = if (Test-Path $preflJson) { Get-Content $preflJson -Raw | ConvertFrom-Json } else { $null }

# Test 7: preflight profile
$prof = if ($null -ne $pfl) { [string]$pfl.candidate_profile } else { '' }
Assert-True ($prof -eq 'empty_grass_v2') "Test7: preflight candidate_profile == empty_grass_v2 (got '$prof')"

# Test 8: entry count
$ec = if ($null -ne $pfl) { [int]$pfl.entry_count } else { -1 }
Assert-True ($ec -eq 1024) "Test8: preflight entry_count == 1024 (got $ec)"

# Test 9: loth size
$ls = if ($null -ne $pfl) { [int]$pfl.loth_size } else { -1 }
Assert-True ($ls -eq 29646) "Test9: preflight loth_size == 29646 (got $ls)"

# Test 10: trailer size
$ts = if ($null -ne $pfl) { [int]$pfl.loth_trailer_size } else { -1 }
Assert-True ($ts -eq 1048) "Test10: preflight loth_trailer_size == 1048 (got $ts)"

# Test 11: trailer SHA-256 canonical
$canonSha = '93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7'
$sha = if ($null -ne $pfl) { [string]$pfl.loth_trailer_sha256 } else { '' }
Assert-True ($sha -eq $canonSha) "Test11: preflight loth_trailer_sha256 == canonical (got '$sha')"

# Test 12: LOTP size
$lotp = if ($null -ne $pfl) { [int]$pfl.lotp_size } else { -1 }
Assert-True ($lotp -eq 1056780) "Test12: preflight lotp_size == 1056780 (got $lotp)"

# Test 13: chunkdata size
$cds = if ($null -ne $pfl) { [int]$pfl.chunkdata_size } else { -1 }
Assert-True ($cds -eq 1026) "Test13: preflight chunkdata_size == 1026 (got $cds)"

$pkContent  = if (Test-Path $packetMd)  { Get-Content $packetMd  -Raw } else { '' }
$recContent = if (Test-Path $recordMd)  { Get-Content $recordMd  -Raw } else { '' }

Assert-True ($pkContent -match 'HUMAN_ONLY_COPY_REQUIRED')           'Test14: packet contains HUMAN_ONLY_COPY_REQUIRED'
Assert-True ($pkContent -match 'LOAD_TEST_NOT_PERFORMED')            'Test15: packet contains LOAD_TEST_NOT_PERFORMED'
Assert-True ($pkContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'Test16: packet contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
Assert-True ($recContent -match 'LOAD_TEST_FAIL_LOTH')               'Test17: record template contains LOAD_TEST_FAIL_LOTH'
Assert-True ($recContent -match 'LOAD_TEST_FAIL_LOTP')               'Test18: record template contains LOAD_TEST_FAIL_LOTP'
Assert-True ($recContent -match 'LOAD_TEST_FAIL_CHUNKDATA')          'Test19: record template contains LOAD_TEST_FAIL_CHUNKDATA'
Assert-True ($recContent -match 'LOAD_TEST_FAIL_OBJECTS_LUA')        'Test20: record template contains LOAD_TEST_FAIL_OBJECTS_LUA'
Assert-True ($recContent -match 'LOAD_TEST_PASS')                    'Test21: record template contains LOAD_TEST_PASS'

# Test 22: all Copy-Item references in packet are marked HUMAN-ONLY
$hasAutoCopy = ($pkContent -match 'Copy-Item') -and ($pkContent -notmatch 'HUMAN-ONLY')
Assert-True (-not $hasAutoCopy) 'Test22: all Copy-Item lines in packet are marked HUMAN-ONLY'

# Test 23: no automatic Set-Content/writes to Zomboid Server in packet
$hasAutoServer = ($pkContent -match 'Set-Content.*Zomboid') -and ($pkContent -notmatch 'HUMAN-ONLY')
Assert-True (-not $hasAutoServer) 'Test23: no automatic Zomboid writes in packet'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
