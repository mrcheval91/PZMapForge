[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'prepare-build42-map8t-real-cell-boundary-result-packet.ps1'
$tmpOut     = Join-Path $PSScriptRoot '.local\map8t-result-test-tmp'

Write-Host "MAP-8T Real Cell Boundary FF Sentinel Result Tests"
Write-Host "==================================================="

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
Write-Host "`n[2] exits 0 with valid .local path"
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -Output '$tmpOut'" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local path"

$jsonPath   = Join-Path $tmpOut 'map8t-real-cell-boundary-result.json'
$mdPath     = Join-Path $tmpOut 'map8t-real-cell-boundary-result.md'
$packetPath = Join-Path $tmpOut 'MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_PACKET.md'

# Tests 3-5: output files exist
Write-Host "`n[3-5] output files exist"
Assert-True (Test-Path $jsonPath)   "map8t-real-cell-boundary-result.json exists"
Assert-True (Test-Path $mdPath)     "map8t-real-cell-boundary-result.md exists"
Assert-True (Test-Path $packetPath) "MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_PACKET.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 6: schema
Write-Host "`n[6] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8t-result.v0.1') "schema == pzmapforge.map8t-result.v0.1"

# Test 7: operator_ran_map8s_inspector == true
Write-Host "`n[7] operator_ran_map8s_inspector == true"
Assert-True ($p.operator_ran_map8s_inspector -eq $true) "operator_ran_map8s_inspector == true"

# Test 8: reference_size_bytes == 283881
Write-Host "`n[8] reference_size_bytes == 283881"
Assert-True ([long]$p.reference_size_bytes -eq 283881) "reference_size_bytes == 283881"

# Test 9: bytes_read_count == 4096
Write-Host "`n[9] bytes_read_count == 4096"
Assert-True ([int]$p.bytes_read_count -eq 4096) "bytes_read_count == 4096"

# Test 10: full_file_read == false
Write-Host "`n[10] full_file_read == false"
Assert-True ($p.full_file_read -eq $false) "full_file_read == false"

# Test 11: first_128_bytes_after_string_pool_all_ff == true
Write-Host "`n[11] first_128_bytes_after_string_pool_all_ff == true"
Assert-True ($p.first_128_bytes_after_string_pool_all_ff -eq $true) `
    "first_128_bytes_after_string_pool_all_ff == true"

# Test 12: first_256_bytes_after_string_pool_all_ff == true
Write-Host "`n[12] first_256_bytes_after_string_pool_all_ff == true"
Assert-True ($p.first_256_bytes_after_string_pool_all_ff -eq $true) `
    "first_256_bytes_after_string_pool_all_ff == true"

# Test 13: observed_u32le_values_after_string_pool_are_minus_one == true
Write-Host "`n[13] observed_u32le_values_after_string_pool_are_minus_one == true"
Assert-True ($p.observed_u32le_values_after_string_pool_are_minus_one -eq $true) `
    "observed_u32le_values_after_string_pool_are_minus_one == true"

# Test 14: immediate_cell_index_after_string_pool_supported == false
Write-Host "`n[14] immediate_cell_index_after_string_pool_supported == false"
Assert-True ($p.immediate_cell_index_after_string_pool_supported -eq $false) `
    "immediate_cell_index_after_string_pool_supported == false"

# Test 15: first_non_ff_offset_known == false
Write-Host "`n[15] first_non_ff_offset_known == false"
Assert-True ($p.first_non_ff_offset_known -eq $false) "first_non_ff_offset_known == false"

# Test 16: full_format_understood == false
Write-Host "`n[16] full_format_understood == false"
Assert-True ($p.full_format_understood -eq $false) "full_format_understood == false"

# Test 17: binary_writer_gate_closed == true
Write-Host "`n[17] binary_writer_gate_closed == true"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Test 18: playable_claim_allowed == false
Write-Host "`n[18] playable_claim_allowed == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 19: third_party_files_copied == false
Write-Host "`n[19] third_party_files_copied == false"
Assert-True ($p.third_party_files_copied -eq $false) "third_party_files_copied == false"

# Test 20: next_branch correct
Write-Host "`n[20] next_branch"
Assert-True ($p.next_branch -eq 'igmb_first_non_ff_transition_scan_pending_operator_approval') `
    "next_branch == igmb_first_non_ff_transition_scan_pending_operator_approval"

# Verify packet doc sentinels
Write-Host "`n[Sentinel checks]"
$packetContent = Get-Content $packetPath -Raw
Assert-True ($packetContent -match 'MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED') `
    "packet doc contains MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED"
Assert-True ($packetContent -match 'BINARY_WRITER_GATE_STILL_CLOSED') `
    "packet doc contains BINARY_WRITER_GATE_STILL_CLOSED"
Assert-True ($packetContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') `
    "packet doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"

# Cleanup
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }

Write-Host "`n==================================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
