#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-8I: prepare-build42-map8i-runtime-result-packet.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$packetScript = Join-Path $repoRoot 'scripts\prepare-build42-map8i-runtime-result-packet.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map8i-runtime-result.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t8i-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t8i-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Packet {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $packetScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 1: Script refuses output outside .local
Write-Output '--- Test 1: Refuses output outside .local ---'
$t1exit = Invoke-Packet -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: output outside .local exits nonzero'

# Run packet
Write-Output ''
Write-Output '--- Running packet (Tests 2-20) ---'
$packetOut = Join-Path $testBase '.local\map8i-packet'
$t2exit    = Invoke-Packet -OutDir $packetOut

# Test 2: Exits 0
Assert-True ($t2exit -eq 0) 'Test2: packet exits 0 with valid output path'

# Tests 3-5: Required packet files
Write-Output ''
Write-Output '--- Tests 3-5: Required packet files ---'
Assert-True (Test-Path (Join-Path $packetOut 'MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT_PACKET.md')) `
    'Test3: MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT_PACKET.md exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8i-result.json')) `
    'Test4: map8i-result.json exists'

Assert-True (Test-Path (Join-Path $packetOut 'map8i-result.md')) `
    'Test5: map8i-result.md exists'

# Parse result JSON
$res = if (Test-Path (Join-Path $packetOut 'map8i-result.json')) {
    Get-Content (Join-Path $packetOut 'map8i-result.json') -Raw | ConvertFrom-Json
} else { $null }

Write-Output ''
Write-Output '--- Tests 6-20: Result JSON fields ---'

# Test 6: Schema correct
Assert-True ($null -ne $res -and [string]$res.schema -eq 'pzmapforge.map8i-result.v0.1') `
    "Test6: schema correct (got '$($res.schema)')"

# Test 7: workshop_ready = true
Assert-True ($null -ne $res -and [bool]$res.workshop_ready -eq $true) `
    'Test7: workshop_ready=true'

# Test 8: server_map_line_correct = true
Assert-True ($null -ne $res -and [bool]$res.server_map_line_correct -eq $true) `
    'Test8: server_map_line_correct=true'

# Test 9: dual_spawnpoint_keys_present = true
Assert-True ($null -ne $res -and [bool]$res.dual_spawnpoint_keys_present -eq $true) `
    'Test9: dual_spawnpoint_keys_present=true'

# Test 10: spawnpoint_profession_error_removed = true
Assert-True ($null -ne $res -and [bool]$res.spawnpoint_profession_error_removed -eq $true) `
    'Test10: spawnpoint_profession_error_removed=true'

# Test 11: player_spawn_coordinate = 10746,8288,0
Assert-True ($null -ne $res -and [string]$res.player_spawn_coordinate -eq '10746,8288,0') `
    "Test11: player_spawn_coordinate=10746,8288,0 (got '$($res.player_spawn_coordinate)')"

# Test 12: player_disconnect_coordinate = 10773,8288,0
Assert-True ($null -ne $res -and [string]$res.player_disconnect_coordinate -eq '10773,8288,0') `
    "Test12: player_disconnect_coordinate=10773,8288,0 (got '$($res.player_disconnect_coordinate)')"

# Test 13: spawn_coordinate_matches_35_27 = true
Assert-True ($null -ne $res -and [bool]$res.spawn_coordinate_matches_35_27 -eq $true) `
    'Test13: spawn_coordinate_matches_35_27=true'

# Test 14: spawn_worldX = 35
Assert-True ($null -ne $res -and [int]$res.spawn_worldX -eq 35) `
    "Test14: spawn_worldX=35 (got '$($res.spawn_worldX)')"

# Test 15: spawn_worldY = 27
Assert-True ($null -ne $res -and [int]$res.spawn_worldY -eq 27) `
    "Test15: spawn_worldY=27 (got '$($res.spawn_worldY)')"

# Test 16: iso_meta_grid_map_folder_list_empty = true
Assert-True ($null -ne $res -and [bool]$res.iso_meta_grid_map_folder_list_empty -eq $true) `
    'Test16: iso_meta_grid_map_folder_list_empty=true'

# Test 17: spawned_in_fallback_or_unconfirmed_generated_content = true
Assert-True ($null -ne $res -and [bool]$res.spawned_in_fallback_or_unconfirmed_generated_content -eq $true) `
    'Test17: spawned_in_fallback_or_unconfirmed_generated_content=true'

# Test 18: playable_claim_allowed = false
Assert-True ($null -ne $res -and [bool]$res.playable_claim_allowed -eq $false) `
    'Test18: playable_claim_allowed=false'

# Test 19: binary_writer_gate_closed = true
Assert-True ($null -ne $res -and [bool]$res.binary_writer_gate_closed -eq $true) `
    'Test19: binary_writer_gate_closed=true'

# Test 20: next_branch contains parent_metadata_or_binary_cell_mount_contract
$nb = if ($null -ne $res) { [string]$res.next_branch } else { '' }
Assert-True ($nb -match 'parent_metadata_or_binary_cell_mount_contract') `
    "Test20: next_branch contains parent_metadata_or_binary_cell_mount_contract (got '$nb')"

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
