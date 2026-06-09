[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'prepare-build42-map8p-igmb-header-result-packet.ps1'
$tmpOut     = Join-Path $PSScriptRoot '.local\map8p-igmb-result-test-tmp'

Write-Host "MAP-8P IGMB Header Result Packet Tests"
Write-Host "======================================="

# Test 1: .local guard exits nonzero
Write-Host "`n[1] .local guard on bad path"
$savedPref = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -Output 'C:\tmp\bad-path'" 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref
Assert-True ($guardExit -ne 0) ".local guard exits nonzero for path without .local"

# Test 2: exits 0 with valid .local path
Write-Host "`n[2] exits 0 with valid path"
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -Output '$tmpOut'" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local path"

$jsonPath   = Join-Path $tmpOut 'map8p-igmb-header-result.json'
$mdPath     = Join-Path $tmpOut 'map8p-igmb-header-result.md'
$packetPath = Join-Path $tmpOut 'MAP_8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_PACKET.md'

# Tests 3-5: output files exist
Write-Host "`n[3-5] output files exist"
Assert-True (Test-Path $jsonPath)   "map8p-igmb-header-result.json exists"
Assert-True (Test-Path $mdPath)     "map8p-igmb-header-result.md exists"
Assert-True (Test-Path $packetPath) "MAP_8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_PACKET.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 6: schema
Write-Host "`n[6] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8p-result.v0.1') "schema == pzmapforge.map8p-result.v0.1"

# Test 7: igmb_magic_detected == true
Write-Host "`n[7] igmb_magic_detected == true"
Assert-True ($p.igmb_magic_detected -eq $true) "igmb_magic_detected == true"

# Test 8: reference_detected_signature == 'igmb'
Write-Host "`n[8] reference_detected_signature == igmb"
Assert-True ($p.reference_detected_signature -eq 'igmb') "reference_detected_signature == igmb"

# Test 9: appears_compressed == false
Write-Host "`n[9] appears_compressed == false"
Assert-True ($p.appears_compressed -eq $false) "appears_compressed == false"

# Test 10: appears_custom_binary_worldmap_format == true
Write-Host "`n[10] appears_custom_binary_worldmap_format == true"
Assert-True ($p.appears_custom_binary_worldmap_format -eq $true) "appears_custom_binary_worldmap_format == true"

# Test 11: likely_little_endian_fields == true
Write-Host "`n[11] likely_little_endian_fields == true"
Assert-True ($p.likely_little_endian_fields -eq $true) "likely_little_endian_fields == true"

# Test 12: big_endian_claim_contradicted_by_observed_header == true
Write-Host "`n[12] big_endian_claim_contradicted_by_observed_header == true"
Assert-True ($p.big_endian_claim_contradicted_by_observed_header -eq $true) "big_endian_claim_contradicted_by_observed_header == true"

# Test 13: community_layout_notes_recorded_as_unverified == true
Write-Host "`n[13] community_layout_notes_recorded_as_unverified == true"
Assert-True ($p.community_layout_notes_recorded_as_unverified -eq $true) "community_layout_notes_recorded_as_unverified == true"

# Test 14: max_bytes_allowed == 64
Write-Host "`n[14] max_bytes_allowed == 64"
Assert-True ([int]$p.max_bytes_allowed -eq 64) "max_bytes_allowed == 64"

# Test 15: binary_contents_full_read == false
Write-Host "`n[15] binary_contents_full_read == false"
Assert-True ($p.binary_contents_full_read -eq $false) "binary_contents_full_read == false"

# Test 16: third_party_files_copied == false
Write-Host "`n[16] third_party_files_copied == false"
Assert-True ($p.third_party_files_copied -eq $false) "third_party_files_copied == false"

# Test 17: playable_claim_allowed == false
Write-Host "`n[17] playable_claim_allowed == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 18: binary_writer_gate_closed == true
Write-Host "`n[18] binary_writer_gate_closed == true"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Test 19: next_branch == igmb_structure_research_pending_operator_approval
Write-Host "`n[19] next_branch"
Assert-True ($p.next_branch -eq 'igmb_structure_research_pending_operator_approval') "next_branch == igmb_structure_research_pending_operator_approval"

# Test 20: packet doc contains sentinel MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED
Write-Host "`n[20] packet doc sentinel"
$packetContent = Get-Content $packetPath -Raw
Assert-True ($packetContent -match 'MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED') "packet doc contains MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED"

# Cleanup
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }

Write-Host "`n======================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
