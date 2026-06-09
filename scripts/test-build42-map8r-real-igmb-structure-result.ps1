[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath = Join-Path $PSScriptRoot 'prepare-build42-map8r-real-igmb-structure-result-packet.ps1'
$tmpOut     = Join-Path $PSScriptRoot '.local\map8r-result-test-tmp'

Write-Host "MAP-8R Real IGMB Structure Result Tests"
Write-Host "========================================"

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

$jsonPath   = Join-Path $tmpOut 'map8r-real-igmb-structure-result.json'
$mdPath     = Join-Path $tmpOut 'map8r-real-igmb-structure-result.md'
$packetPath = Join-Path $tmpOut 'MAP_8R_REAL_IGMB_STRUCTURE_RESULT_PACKET.md'

# Tests 3-5: output files exist
Write-Host "`n[3-5] output files exist"
Assert-True (Test-Path $jsonPath)   "map8r-real-igmb-structure-result.json exists"
Assert-True (Test-Path $mdPath)     "map8r-real-igmb-structure-result.md exists"
Assert-True (Test-Path $packetPath) "MAP_8R_REAL_IGMB_STRUCTURE_RESULT_PACKET.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 6: schema
Write-Host "`n[6] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8r-result.v0.1') "schema == pzmapforge.map8r-result.v0.1"

# Test 7: magic
Write-Host "`n[7] magic"
Assert-True ($p.magic -eq 'IGMB') "magic == IGMB"

# Test 8: version_le_u32 == 2
Write-Host "`n[8] version_le_u32 == 2"
Assert-True ([int]$p.version_le_u32 -eq 2) "version_le_u32 == 2"

# Test 9: string_pool_detected_count == 12
Write-Host "`n[9] string_pool_detected_count == 12"
Assert-True ([int]$p.string_pool_detected_count -eq 12) "string_pool_detected_count == 12"

# Test 10: string_pool_count_matches_header_offset_20 == true
Write-Host "`n[10] string_pool_count_matches_header_offset_20 == true"
Assert-True ($p.string_pool_count_matches_header_offset_20 -eq $true) "string_pool_count_matches_header_offset_20 == true"

# Test 11: string_pool_values contains 'Polygon'
Write-Host "`n[11] string_pool_values contains Polygon"
$spArr = @($p.string_pool_values)
Assert-True ($spArr -contains 'Polygon') "string_pool_values contains 'Polygon'"

# Test 12: string_pool_values contains 'secondary'
Write-Host "`n[12] string_pool_values contains secondary"
Assert-True ($spArr -contains 'secondary') "string_pool_values contains 'secondary'"

# Test 13: header_probable_string_pool_count_offset_20_u32le == 12
Write-Host "`n[13] header_probable_string_pool_count_offset_20_u32le == 12"
Assert-True ([int]$p.header_probable_string_pool_count_offset_20_u32le -eq 12) "header_probable_string_pool_count_offset_20_u32le == 12"

# Test 14: string_pool_end_offset_candidate == 133
Write-Host "`n[14] string_pool_end_offset_candidate == 133"
Assert-True ([int]$p.string_pool_end_offset_candidate -eq 133) "string_pool_end_offset_candidate == 133"

# Test 15: partial_header_model_confidence == 'medium'
Write-Host "`n[15] partial_header_model_confidence == medium"
Assert-True ($p.partial_header_model_confidence -eq 'medium') "partial_header_model_confidence == medium"

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
Assert-True ($p.next_branch -eq 'igmb_cell_index_boundary_research_pending_operator_approval') `
    "next_branch == igmb_cell_index_boundary_research_pending_operator_approval"

# Verify packet doc sentinels
Write-Host "`n[Sentinel checks]"
$packetContent = Get-Content $packetPath -Raw
Assert-True ($packetContent -match 'MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED') `
    "packet doc contains MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED"
Assert-True ($packetContent -match 'BINARY_WRITER_GATE_STILL_CLOSED') `
    "packet doc contains BINARY_WRITER_GATE_STILL_CLOSED"
Assert-True ($packetContent -match 'PUBLIC_PLAYABLE_CLAIM_ALLOWED=false') `
    "packet doc contains PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"

# Cleanup
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }

Write-Host "`n========================================"
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
