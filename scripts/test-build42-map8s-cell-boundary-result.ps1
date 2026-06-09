[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'prepare-build42-map8s-cell-boundary-result-packet.ps1'
$tmpOut     = Join-Path $PSScriptRoot '.local\map8s-result-test-tmp'

Write-Host "MAP-8S Cell Boundary Result Tests"
Write-Host "==================================="

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

$jsonPath   = Join-Path $tmpOut 'map8s-cell-boundary-result.json'
$mdPath     = Join-Path $tmpOut 'map8s-cell-boundary-result.md'
$packetPath = Join-Path $tmpOut 'MAP_8S_CELL_BOUNDARY_RESULT_PACKET.md'

# Tests 3-5: output files exist
Write-Host "`n[3-5] output files exist"
Assert-True (Test-Path $jsonPath)   "map8s-cell-boundary-result.json exists"
Assert-True (Test-Path $mdPath)     "map8s-cell-boundary-result.md exists"
Assert-True (Test-Path $packetPath) "MAP_8S_CELL_BOUNDARY_RESULT_PACKET.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 6: schema
Write-Host "`n[6] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8s-result.v0.1') "schema == pzmapforge.map8s-result.v0.1"

# Test 7: operator_approved_cell_index_boundary_research == true
Write-Host "`n[7] operator_approved_cell_index_boundary_research"
Assert-True ($p.operator_approved_cell_index_boundary_research -eq $true) `
    "operator_approved_cell_index_boundary_research == true"

# Test 8: string_pool_end_offset == 133
Write-Host "`n[8] string_pool_end_offset == 133"
Assert-True ([int]$p.string_pool_end_offset -eq 133) "string_pool_end_offset == 133"

# Test 9: max_bytes_allowed == 4096
Write-Host "`n[9] max_bytes_allowed == 4096"
Assert-True ([int]$p.max_bytes_allowed -eq 4096) "max_bytes_allowed == 4096"

# Test 10: full_file_read == false
Write-Host "`n[10] full_file_read == false"
Assert-True ($p.full_file_read -eq $false) "full_file_read == false"

# Test 11: full_format_understood == false
Write-Host "`n[11] full_format_understood == false"
Assert-True ($p.full_format_understood -eq $false) "full_format_understood == false"

# Test 12: cell_index_understood == false
Write-Host "`n[12] cell_index_understood == false"
Assert-True ($p.cell_index_understood -eq $false) "cell_index_understood == false"

# Test 13: geometry_payload_understood == false
Write-Host "`n[13] geometry_payload_understood == false"
Assert-True ($p.geometry_payload_understood -eq $false) "geometry_payload_understood == false"

# Test 14: binary_writer_gate_closed == true
Write-Host "`n[14] binary_writer_gate_closed == true"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Test 15: playable_claim_allowed == false
Write-Host "`n[15] playable_claim_allowed == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 16: third_party_files_copied == false
Write-Host "`n[16] third_party_files_copied == false"
Assert-True ($p.third_party_files_copied -eq $false) "third_party_files_copied == false"

# Test 17: next_branch correct
Write-Host "`n[17] next_branch"
Assert-True ($p.next_branch -eq 'igmb_cell_index_model_research_pending_operator_approval_if_boundary_evidence_sufficient') `
    "next_branch == igmb_cell_index_model_research_pending_operator_approval_if_boundary_evidence_sufficient"

# Tests 18-20: packet doc sentinels
Write-Host "`n[18-20] packet doc sentinels"
$packetContent = Get-Content $packetPath -Raw
Assert-True ($packetContent -match 'MAP8S_CELL_BOUNDARY_RESEARCH_DEFINED') `
    "packet doc contains MAP8S_CELL_BOUNDARY_RESEARCH_DEFINED"
Assert-True ($packetContent -match 'BINARY_WRITER_GATE_STILL_CLOSED') `
    "packet doc contains BINARY_WRITER_GATE_STILL_CLOSED"
Assert-True ($packetContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') `
    "packet doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"

# Cleanup
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }

Write-Host "`n==================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
