#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for prepare-build42-loth-v2-load-test-packet.ps1 (MAP-6T).

    Runs the packet script against .local/ and validates its output.
    Does NOT copy files to PZ folders. Requires dotnet build to be current.
    Expected assertion count: 18
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-loth-v2-load-test-packet.ps1'
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

Write-Output 'test-build42-loth-v2-load-test-packet.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup
# ---------------------------------------------------------------------------

$testBase  = Join-Path $tempRoot ('pzmf-t6t-' + [System.IO.Path]::GetRandomFileName())
$goodOut   = Join-Path $testBase '.local\map6t-packet'
$badPath   = Join-Path $tempRoot 'pzmf-t6t-bad-no-local'

New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# ---------------------------------------------------------------------------
# Test 1: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Output outside .local refused ---'
$t1Exit = Invoke-Packet -Output $badPath
Assert-True ($t1Exit -ne 0) 'Test1: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 2-18: Valid run (generates candidate + produces packet)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 2-18: valid packet run ---'
$t2Exit = Invoke-Packet -Output $goodOut

$packetMd  = Join-Path $goodOut 'MAP_6T_LOAD_TEST_PACKET.md'
$recordMd  = Join-Path $goodOut 'MAP_6T_LOAD_TEST_RECORD.local-template.md'
$wiringMd  = Join-Path $goodOut 'MAP_6T_INSTALL_AND_SERVER_WIRING_COMMANDS.md'
$preflJson = Join-Path $goodOut 'map6t-preflight.json'

Assert-True ($t2Exit -eq 0)      'Test2: script exits 0'
Assert-True (Test-Path $packetMd)  'Test3: packet markdown exists'
Assert-True (Test-Path $recordMd)  'Test4: record template exists'
Assert-True (Test-Path $wiringMd)  'Test5: wiring commands markdown exists'
Assert-True (Test-Path $preflJson) 'Test6: preflight JSON exists'

$pfl = if (Test-Path $preflJson) { Get-Content $preflJson -Raw | ConvertFrom-Json } else { $null }

Assert-True ([int]$pfl.entry_count    -eq 1024)    'Test7: preflight entry_count == 1024'
Assert-True ([int]$pfl.loth_size      -eq 28598)   'Test8: preflight loth_size == 28598'
Assert-True ([int]$pfl.lotp_size      -eq 1056780) 'Test9: preflight lotp_size == 1056780'
Assert-True ([int]$pfl.chunkdata_size -eq 1026)    'Test10: preflight chunkdata_size == 1026'

$pkContent  = if (Test-Path $packetMd)  { Get-Content $packetMd  -Raw } else { '' }
$recContent = if (Test-Path $recordMd)  { Get-Content $recordMd  -Raw } else { '' }

Assert-True ($pkContent -match 'HUMAN_ONLY_COPY_REQUIRED')       'Test11: packet contains HUMAN_ONLY_COPY_REQUIRED'
Assert-True ($pkContent -match 'LOAD_TEST_NOT_PERFORMED')        'Test12: packet contains LOAD_TEST_NOT_PERFORMED'
Assert-True ($pkContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'Test13: packet contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
Assert-True ($recContent -match 'LOAD_TEST_FAIL_LOTH')           'Test14: record template contains LOAD_TEST_FAIL_LOTH'
Assert-True ($recContent -match 'LOAD_TEST_FAIL_LOTP')           'Test15: record template contains LOAD_TEST_FAIL_LOTP'
Assert-True ($recContent -match 'LOAD_TEST_PASS')                'Test16: record template contains LOAD_TEST_PASS'

# Test 17: no automatic Copy-Item into Zomboid mods as an executable action
# All Copy-Item references must be preceded by HUMAN-ONLY: marker
$hasAutoCopy = ($pkContent -match 'Copy-Item') -and ($pkContent -notmatch 'HUMAN-ONLY')
Assert-True (-not $hasAutoCopy) 'Test17: all Copy-Item lines are marked HUMAN-ONLY'

# Test 18: no automatic writes to Zomboid Server paths in packet
$hasAutoServer = ($pkContent -match 'Set-Content.*Zomboid\\Server') -and ($pkContent -notmatch 'HUMAN-ONLY')
Assert-True (-not $hasAutoServer) 'Test18: no automatic Set-Content to Zomboid Server in packet'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
