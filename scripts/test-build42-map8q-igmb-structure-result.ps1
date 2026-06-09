[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'prepare-build42-map8q-igmb-structure-result-packet.ps1'
$tmpOut     = Join-Path $PSScriptRoot '.local\map8q-result-test-tmp'

Write-Host "MAP-8Q IGMB Structure Research Result Packet Tests"
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
Write-Host "`n[2] exits 0 with valid path"
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -Output '$tmpOut'" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local path"

$jsonPath   = Join-Path $tmpOut 'map8q-igmb-structure-result.json'
$mdPath     = Join-Path $tmpOut 'map8q-igmb-structure-result.md'
$packetPath = Join-Path $tmpOut 'MAP_8Q_IGMB_STRUCTURE_RESEARCH_PACKET.md'

# Tests 3-5: output files exist
Write-Host "`n[3-5] output files exist"
Assert-True (Test-Path $jsonPath)   "map8q-igmb-structure-result.json exists"
Assert-True (Test-Path $mdPath)     "map8q-igmb-structure-result.md exists"
Assert-True (Test-Path $packetPath) "MAP_8Q_IGMB_STRUCTURE_RESEARCH_PACKET.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 6: schema
Write-Host "`n[6] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8q-result.v0.1') "schema == pzmapforge.map8q-result.v0.1"

# Test 7: operator_approved_igmb_structure_research == true
Write-Host "`n[7] operator_approved_igmb_structure_research == true"
Assert-True ($p.operator_approved_igmb_structure_research -eq $true) "operator_approved_igmb_structure_research == true"

# Test 8: max_bytes_allowed == 4096
Write-Host "`n[8] max_bytes_allowed == 4096"
Assert-True ([int]$p.max_bytes_allowed -eq 4096) "max_bytes_allowed == 4096"

# Test 9: binary_contents_read_scope == 'first_4096_bytes_only'
Write-Host "`n[9] binary_contents_read_scope"
Assert-True ($p.binary_contents_read_scope -eq 'first_4096_bytes_only') "binary_contents_read_scope == first_4096_bytes_only"

# Test 10: binary_contents_full_read == false
Write-Host "`n[10] binary_contents_full_read == false"
Assert-True ($p.binary_contents_full_read -eq $false) "binary_contents_full_read == false"

# Test 11: third_party_files_copied == false
Write-Host "`n[11] third_party_files_copied == false"
Assert-True ($p.third_party_files_copied -eq $false) "third_party_files_copied == false"

# Test 12: playable_claim_allowed == false
Write-Host "`n[12] playable_claim_allowed == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 13: binary_writer_gate_closed == true
Write-Host "`n[13] binary_writer_gate_closed == true"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Test 14: next_branch correct
Write-Host "`n[14] next_branch"
Assert-True ($p.next_branch -eq 'igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient') `
    "next_branch == igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient"

$packetContent = Get-Content $packetPath -Raw

# Test 15: packet doc contains MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED
Write-Host "`n[15] packet doc sentinel MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED"
Assert-True ($packetContent -match 'MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED') "packet doc contains MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED"

# Test 16: packet doc contains BINARY_WRITER_GATE_STILL_CLOSED
Write-Host "`n[16] packet doc sentinel BINARY_WRITER_GATE_STILL_CLOSED"
Assert-True ($packetContent -match 'BINARY_WRITER_GATE_STILL_CLOSED') "packet doc contains BINARY_WRITER_GATE_STILL_CLOSED"

# Test 17: packet doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
Write-Host "`n[17] packet doc sentinel PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Assert-True ($packetContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') "packet doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"

# Test 18: packet doc contains OPERATOR_APPROVED_IGMB_STRUCTURE_RESEARCH=true
Write-Host "`n[18] packet doc operator approval sentinel"
Assert-True ($packetContent -match 'OPERATOR_APPROVED_IGMB_STRUCTURE_RESEARCH=true') "packet doc contains OPERATOR_APPROVED_IGMB_STRUCTURE_RESEARCH=true"

# Test 19: packet doc contains MAX_BYTES_ALLOWED=4096
Write-Host "`n[19] packet doc max bytes sentinel"
Assert-True ($packetContent -match 'MAX_BYTES_ALLOWED=4096') "packet doc contains MAX_BYTES_ALLOWED=4096"

# Test 20: md contains expected header
Write-Host "`n[20] md contains expected header"
$mdContent = Get-Content $mdPath -Raw
Assert-True ($mdContent -match 'MAP-8Q IGMB Structure Research Result Packet') "md contains 'MAP-8Q IGMB Structure Research Result Packet'"

# Cleanup
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }

Write-Host "`n==================================================="
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
