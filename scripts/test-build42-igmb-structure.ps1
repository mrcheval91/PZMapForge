[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass = 0; $fail = 0
function Assert-True([bool]$cond, [string]$label) {
    if ($cond) { Write-Host "  PASS: $label"; $script:pass++ }
    else        { Write-Host "  FAIL: $label"; $script:fail++ }
}

$scriptPath  = Join-Path $PSScriptRoot 'inspect-build42-igmb-structure.ps1'
$tmpLocalDir = Join-Path $PSScriptRoot '.local'
$tmpRef      = Join-Path $tmpLocalDir 'map8q-dummy-igmb-ref.bin'
$tmpOut      = Join-Path $PSScriptRoot '.local\map8q-structure-test-tmp'

Write-Host "MAP-8Q IGMB Structure Inspector Tests"
Write-Host "======================================"

# Test 1: .local guard exits nonzero
Write-Host "`n[1] .local guard on bad path"
$savedPref = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -ReferenceWorldmapBinPath 'C:\nonexistent.bin' -Output 'C:\tmp\bad-path'" 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref
Assert-True ($guardExit -ne 0) ".local guard exits nonzero for path without .local"

# Build 256-byte synthetic IGMB file with U16LE LP strings
if (-not (Test-Path $tmpLocalDir)) { New-Item -ItemType Directory -Force -Path $tmpLocalDir | Out-Null }
$igmbBytes = [byte[]]::new(256)
# Magic: IGMB
$igmbBytes[0] = 0x49; $igmbBytes[1] = 0x47; $igmbBytes[2] = 0x4D; $igmbBytes[3] = 0x42
# Version: 2 (U32LE)
$igmbBytes[4] = 0x02; $igmbBytes[5] = 0x00; $igmbBytes[6] = 0x00; $igmbBytes[7] = 0x00
# Header fields
$igmbBytes[8] = 0x00; $igmbBytes[9] = 0x01; $igmbBytes[10] = 0x00; $igmbBytes[11] = 0x00
$igmbBytes[12] = 0x3B; $igmbBytes[13] = 0x00; $igmbBytes[14] = 0x00; $igmbBytes[15] = 0x00
$igmbBytes[16] = 0x44; $igmbBytes[17] = 0x00; $igmbBytes[18] = 0x00; $igmbBytes[19] = 0x00
$igmbBytes[20] = 0x0C; $igmbBytes[21] = 0x00; $igmbBytes[22] = 0x00; $igmbBytes[23] = 0x00
# U16LE len=7 + "Polygon"
$igmbBytes[24] = 0x07; $igmbBytes[25] = 0x00
$igmbBytes[26] = 0x50; $igmbBytes[27] = 0x6F; $igmbBytes[28] = 0x6C; $igmbBytes[29] = 0x79
$igmbBytes[30] = 0x67; $igmbBytes[31] = 0x6F; $igmbBytes[32] = 0x6E
# U16LE len=7 + "highway"
$igmbBytes[33] = 0x07; $igmbBytes[34] = 0x00
$igmbBytes[35] = 0x68; $igmbBytes[36] = 0x69; $igmbBytes[37] = 0x67; $igmbBytes[38] = 0x68
$igmbBytes[39] = 0x77; $igmbBytes[40] = 0x61; $igmbBytes[41] = 0x79
# Rest: zeros (already zero from new)
[System.IO.File]::WriteAllBytes($tmpRef, $igmbBytes)

# Test 2: exits 0 with valid path and synthetic reference
Write-Host "`n[2] exits 0 with valid path"
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
$savedPref2 = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & powershell -ExecutionPolicy Bypass -NonInteractive -Command `
    "& '$scriptPath' -ReferenceWorldmapBinPath '$tmpRef' -Output '$tmpOut'" 2>&1
$validExit = $LASTEXITCODE
$ErrorActionPreference = $savedPref2
Assert-True ($validExit -eq 0) "exits 0 with valid .local path and present reference"

$jsonPath = Join-Path $tmpOut 'igmb-structure-inspection.json'
$mdPath   = Join-Path $tmpOut 'igmb-structure-inspection.md'

# Tests 3-4: output files exist
Write-Host "`n[3-4] output files exist"
Assert-True (Test-Path $jsonPath) "igmb-structure-inspection.json exists"
Assert-True (Test-Path $mdPath)   "igmb-structure-inspection.md exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Test 5: schema
Write-Host "`n[5] schema"
Assert-True ($p.schema -eq 'pzmapforge.map8q-igmb-structure-inspection.v0.1') "schema == pzmapforge.map8q-igmb-structure-inspection.v0.1"

# Test 6: reference_present == true
Write-Host "`n[6] reference_present == true"
Assert-True ($p.reference_present -eq $true) "reference_present == true"

# Test 7: bytes_read_count > 0
Write-Host "`n[7] bytes_read_count > 0"
Assert-True ([int]$p.bytes_read_count -gt 0) "bytes_read_count > 0"

# Test 8: max_bytes_allowed == 4096
Write-Host "`n[8] max_bytes_allowed == 4096"
Assert-True ([int]$p.max_bytes_allowed -eq 4096) "max_bytes_allowed == 4096"

# Test 9: full_file_read == false
Write-Host "`n[9] full_file_read == false"
Assert-True ($p.full_file_read -eq $false) "full_file_read == false"

# Test 10: version_le_u32 == 2
Write-Host "`n[10] version_le_u32 == 2"
Assert-True ([int]$p.version_le_u32 -eq 2) "version_le_u32 == 2 (synthetic file byte 4-7 = 02 00 00 00)"

# Test 11: candidate_u32_values_first_64_le not null
Write-Host "`n[11] candidate_u32_values_first_64_le not null"
Assert-True ($null -ne $p.candidate_u32_values_first_64_le) "candidate_u32_values_first_64_le not null"

# Test 12: candidate_u16_values_first_64_le not null
Write-Host "`n[12] candidate_u16_values_first_64_le not null"
Assert-True ($null -ne $p.candidate_u16_values_first_64_le) "candidate_u16_values_first_64_le not null"

# Test 13: possible_string_pool_count_candidates >= 2 (Polygon + highway)
Write-Host "`n[13] possible_string_pool_count_candidates >= 2"
Assert-True ([int]$p.possible_string_pool_count_candidates -ge 2) "possible_string_pool_count_candidates >= 2 (at least Polygon and highway)"

# Test 14: printable_ascii_runs_min_length_3 not null
Write-Host "`n[14] printable_ascii_runs_min_length_3 not null"
Assert-True ($null -ne $p.printable_ascii_runs_min_length_3) "printable_ascii_runs_min_length_3 not null"

# Test 15: binary_writer_gate_closed == true
Write-Host "`n[15] binary_writer_gate_closed == true"
Assert-True ($p.binary_writer_gate_closed -eq $true) "binary_writer_gate_closed == true"

# Test 16: playable_claim_allowed == false
Write-Host "`n[16] playable_claim_allowed == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# Test 17: third_party_files_copied == false
Write-Host "`n[17] third_party_files_copied == false"
Assert-True ($p.third_party_files_copied -eq $false) "third_party_files_copied == false"

# Test 18: confidence_level == 'low_to_medium'
Write-Host "`n[18] confidence_level == low_to_medium"
Assert-True ($p.confidence_level -eq 'low_to_medium') "confidence_level == low_to_medium"

# Test 19: next_branch correct
Write-Host "`n[19] next_branch"
Assert-True ($p.next_branch -eq 'igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient') `
    "next_branch == igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient"

# Test 20: possible_length_prefixed_strings contains entry with value 'Polygon'
Write-Host "`n[20] LP strings contain Polygon"
$lpArr = @($p.possible_length_prefixed_strings)
$hasPolygon = $false
foreach ($item in $lpArr) {
    if ($null -ne $item -and $item.value -eq 'Polygon') { $hasPolygon = $true; break }
}
Assert-True $hasPolygon "possible_length_prefixed_strings contains entry with value 'Polygon'"

# Cleanup
if (Test-Path $tmpOut) { Remove-Item -Recurse -Force $tmpOut }
if (Test-Path $tmpRef) { Remove-Item -Force $tmpRef }

Write-Host "`n======================================"
Write-Host "PASS: $pass   FAIL: $fail   TOTAL: $($pass + $fail)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
