#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for prepare-build42-candidate-load-test-packet.ps1 (MAP-6M).

    Creates a synthetic MAP-6L-like fixture under temp .local and validates
    the load-test packet output. Does not use real PZ files.
    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-candidate-load-test-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-candidate-load-test-packet.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Setup: temp dirs
# ---------------------------------------------------------------------------

$testBase   = Join-Path $tempRoot ('pzmapforge-packet-test-' + [System.IO.Path]::GetRandomFileName())
$badPath    = Join-Path $tempRoot 'not-local-packet'
$testMapId  = 'pzmapforge_b42_cand_test'
$candRoot   = Join-Path $testBase ('.local\candidate\' + $testMapId + '_build42_candidate')
$v42Dir     = Join-Path $candRoot '42'
$mapDataDir = Join-Path $v42Dir ('media\maps\' + $testMapId)

New-Item -ItemType Directory -Force -Path $mapDataDir | Out-Null
New-Item -ItemType Directory -Force -Path $badPath    | Out-Null

# ---- Synthetic MAP-6L report JSON ----
$reportJson = [ordered]@{
    schema                       = 'pzmapforge.build42-candidate-report.v0.1'
    map_id                       = $testMapId
    build42_candidate_profile    = 'empty_grass_v0'
    build42_candidate_writer     = $true
    writer_implemented           = $true
    writer_scope                 = 'candidate_only_not_load_tested'
    load_tested                  = $false
    playable_export_generated    = $false
    playable_export_claimed      = $false
    pz_assets_copied             = $false
    pz_assets_read               = $false
    media_maps_touched_in_repo   = $false
    lotp_file_size_expected      = 1056780
    lotp_status                  = 'generated_not_load_tested'
    loth_status                  = 'generated_not_load_tested'
    chunkdata_status             = 'generated_not_load_tested'
}
$reportJson | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $v42Dir 'experimental-map-export-report.json') -Encoding UTF8

# ---- Synthetic text files ----
Set-Content -Path (Join-Path $v42Dir 'mod.info')                  -Value "id=$testMapId`nname=Test"       -Encoding UTF8
Set-Content -Path (Join-Path $mapDataDir 'map.info')              -Value "title=Test"                      -Encoding UTF8
Set-Content -Path (Join-Path $mapDataDir 'spawnpoints.lua')       -Value "function SpawnPoints() return {} end" -Encoding UTF8
Set-Content -Path (Join-Path $mapDataDir 'objects.lua')           -Value 'return {}'                        -Encoding UTF8

# ---- Synthetic LOTH lotheader: 38 bytes ----
$entry       = [System.Text.Encoding]::ASCII.GetBytes("blends_grassoverlays_01_0`n")
$lothBytes   = [byte[]]::new(12 + $entry.Length)
$lothBytes[0]=0x4C; $lothBytes[1]=0x4F; $lothBytes[2]=0x54; $lothBytes[3]=0x48  # LOTH
$lothBytes[4]=0x01                                                                 # version=1
$lothBytes[8]=0x01                                                                 # entry_count=1
$entry.CopyTo($lothBytes, 12)
[System.IO.File]::WriteAllBytes((Join-Path $mapDataDir '0_0.lotheader'), $lothBytes)

# ---- Synthetic LOTP lotpack: 1,056,780 bytes ----
# Full size required because preflight checks file size = 1056780
$chunkCount   = 1024
$chunkPayload = 1024
$headerSize   = 12
$tableSize    = $chunkCount * 8
$firstOff     = $headerSize + $tableSize   # 8204
$expectedSize = $firstOff + $chunkCount * $chunkPayload  # 1056780

$lotpBytes    = [byte[]]::new($expectedSize)
$lotpBytes[0]=0x4C; $lotpBytes[1]=0x4F; $lotpBytes[2]=0x54; $lotpBytes[3]=0x50  # LOTP
$lotpBytes[4]=0x01                          # version=1
$lotpBytes[8]=0x00; $lotpBytes[9]=0x04       # chunk_count=1024 LE

for ($i = 0; $i -lt $chunkCount; $i++) {
    $entryPos = $headerSize + $i * 8
    $offset   = $firstOff + $i * $chunkPayload
    $ob = [System.BitConverter]::GetBytes([long]$offset)
    [System.Array]::Copy($ob, 0, $lotpBytes, $entryPos, 8)
}
[System.IO.File]::WriteAllBytes((Join-Path $mapDataDir 'world_0_0.lotpack'), $lotpBytes)

# ---- Synthetic chunkdata: 1026 bytes ----
$cdataBytes = [byte[]]::new(1026)
$cdataBytes[0]=0x00; $cdataBytes[1]=0x01
[System.IO.File]::WriteAllBytes((Join-Path $mapDataDir 'chunkdata_0_0.bin'), $cdataBytes)

# ---------------------------------------------------------------------------
# Helper
# ---------------------------------------------------------------------------

function Invoke-Packet {
    param([string[]]$ArgList)
    $saved = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $null = & powershell -ExecutionPolicy Bypass -File $packetScript @ArgList
    $ec = $LASTEXITCODE; $ErrorActionPreference = $saved; return $ec
}

# ---------------------------------------------------------------------------
# Test 1: Source outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Source outside .local refused ---'
$ec1 = Invoke-Packet @('-Source', $badPath, '-Output', (Join-Path $testBase '.local\out'))
Assert-True ($ec1 -ne 0) 'Source outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 2: Output outside .local refused ---'
$ec2 = Invoke-Packet @('-Source', $candRoot, '-Output', $badPath)
Assert-True ($ec2 -ne 0) 'Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 3: Valid source exits 0
# ---------------------------------------------------------------------------

Write-Output '--- Test 3: Valid run exits 0 ---'
$packetOut = Join-Path $testBase '.local\packet-out'
$ec3 = Invoke-Packet @('-Source', $candRoot, '-Output', $packetOut, '-ServerName', 'TEST_SERVER_001', '-ModFolderName', 'test_mod_folder')
Assert-True ($ec3 -eq 0) 'Valid run exits 0'

# Load output files
$preflightFile = Join-Path $packetOut 'BUILD42_CANDIDATE_PREFLIGHT.json'
$packetFile    = Join-Path $packetOut 'BUILD42_CANDIDATE_LOAD_TEST_PACKET.md'
$recordFile    = Join-Path $packetOut 'BUILD42_CANDIDATE_LOAD_TEST_RECORD.local-template.md'
$spawnFile     = Join-Path $packetOut 'pzmapforge_candidate_spawnregions.lua'

$preflight     = Get-Content $preflightFile -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Tests 4-7: Output files exist
# ---------------------------------------------------------------------------

Write-Output '--- Test 4: Packet MD exists ---'
Assert-True (Test-Path $packetFile) 'BUILD42_CANDIDATE_LOAD_TEST_PACKET.md exists'

Write-Output '--- Test 5: Record template exists ---'
Assert-True (Test-Path $recordFile) 'BUILD42_CANDIDATE_LOAD_TEST_RECORD.local-template.md exists'

Write-Output '--- Test 6: Preflight JSON exists ---'
Assert-True (Test-Path $preflightFile) 'BUILD42_CANDIDATE_PREFLIGHT.json exists'

Write-Output '--- Test 7: Spawnregions template exists ---'
Assert-True (Test-Path $spawnFile) 'pzmapforge_candidate_spawnregions.lua exists'

# ---------------------------------------------------------------------------
# Tests 8-10: Preflight JSON safety flags
# ---------------------------------------------------------------------------

Write-Output '--- Test 8: Preflight build42_candidate_writer true ---'
Assert-True ($preflight.build42_candidate_writer -eq $true) 'preflight.build42_candidate_writer == true'

Write-Output '--- Test 9: Preflight load_tested false ---'
Assert-True ($preflight.load_tested -eq $false) 'preflight.load_tested == false'

Write-Output '--- Test 10: Preflight playable_export_claimed false ---'
Assert-True ($preflight.playable_export_claimed -eq $false) 'preflight.playable_export_claimed == false'

# ---------------------------------------------------------------------------
# Tests 11-13: Preflight binary signature checks
# ---------------------------------------------------------------------------

Write-Output '--- Test 11: Preflight LOTH magic check passed ---'
$lothMagicCheck = @($preflight.checks) | Where-Object { $_.id -eq 'loth_magic_correct' } | Select-Object -First 1
Assert-True ($null -ne $lothMagicCheck -and $lothMagicCheck.pass -eq $true) 'loth_magic_correct check passed'

Write-Output '--- Test 12: Preflight LOTP magic check passed ---'
$lotpMagicCheck = @($preflight.checks) | Where-Object { $_.id -eq 'lotp_magic_correct' } | Select-Object -First 1
Assert-True ($null -ne $lotpMagicCheck -and $lotpMagicCheck.pass -eq $true) 'lotp_magic_correct check passed'

Write-Output '--- Test 13: Preflight chunkdata header check passed ---'
$cdHdrCheck = @($preflight.checks) | Where-Object { $_.id -eq 'chunkdata_header_0001' } | Select-Object -First 1
Assert-True ($null -ne $cdHdrCheck -and $cdHdrCheck.pass -eq $true) 'chunkdata_header_0001 check passed'

# ---------------------------------------------------------------------------
# Tests 14-15: Packet content
# ---------------------------------------------------------------------------

Write-Output '--- Test 14: Packet contains expected destination but did not copy ---'
$packetContent = Get-Content $packetFile -Raw
Assert-True ($packetContent -match 'Zomboid.mods' -and -not (Test-Path "C:\Users\Palmacede\Zomboid\mods\test_mod_folder")) 'Packet has dest path; script did NOT copy'

Write-Output '--- Test 15: Packet contains PZMapForge Candidate Cell ---'
Assert-True ($packetContent -match 'PZMapForge Candidate Cell') 'Packet contains PZMapForge Candidate Cell'

# ---------------------------------------------------------------------------
# Tests 16-18: Record template choices
# ---------------------------------------------------------------------------

Write-Output '--- Test 16: Record template contains LOAD_TEST_PASS ---'
$recordContent = Get-Content $recordFile -Raw
Assert-True ($recordContent -match 'LOAD_TEST_PASS') 'Record template contains LOAD_TEST_PASS'

Write-Output '--- Test 17: Record template contains LOAD_TEST_FAIL ---'
Assert-True ($recordContent -match 'LOAD_TEST_FAIL') 'Record template contains LOAD_TEST_FAIL'

Write-Output '--- Test 18: Record template contains LOAD_TEST_INCONCLUSIVE ---'
Assert-True ($recordContent -match 'LOAD_TEST_INCONCLUSIVE') 'Record template contains LOAD_TEST_INCONCLUSIVE'

# ---------------------------------------------------------------------------
# Tests 19-20: No playable claim
# ---------------------------------------------------------------------------

Write-Output '--- Test 19: No playable_export_claimed true in preflight ---'
$pfJson = Get-Content $preflightFile -Raw
Assert-True ($pfJson -notmatch '"playable_export_claimed"\s*:\s*true') 'No playable_export_claimed:true in preflight'

Write-Output '--- Test 20: PLAYABLE_EXPORT_CLAIM_ALLOWED=false in packet ---'
Assert-True ($packetContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'Packet contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'

# ---------------------------------------------------------------------------
# Cleanup
# ---------------------------------------------------------------------------

try { Remove-Item -LiteralPath $testBase -Recurse -Force -ErrorAction SilentlyContinue } catch {}
try { Remove-Item -LiteralPath $badPath  -Recurse -Force -ErrorAction SilentlyContinue } catch {}

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'
if ($fail -gt 0) { exit 1 }
exit 0
