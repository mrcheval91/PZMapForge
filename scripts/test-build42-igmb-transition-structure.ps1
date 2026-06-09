Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pass  = 0
$fail  = 0
$total = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    $script:total++
    if ($Condition) {
        Write-Host "  PASS  $Label"
        $script:pass++
    } else {
        Write-Host "  FAIL  $Label"
        $script:fail++
    }
}

$scriptsDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$inspectScript = Join-Path $scriptsDir 'inspect-build42-igmb-transition-structure.ps1'

# Build synthetic 300-byte file
# bytes 0-3:   IGMB magic (49 47 4D 42)
# bytes 4-7:   version 2 LE (02 00 00 00)
# bytes 8-41:  0x00 padding
# bytes 42-53: triplet 30/26/9 LE (1e 00 00 00 1a 00 00 00 09 00 00 00)
# bytes 54-59: real data pattern from hex window (00 00 02 14 00 ff)
# bytes 60-299: 0x55 filler
$syntheticFile = Join-Path $env:TEMP 'pzmf-map8w-synthetic-300.bin'
$syntheticBytes = [byte[]]::new(300)
# magic
$syntheticBytes[0] = 0x49; $syntheticBytes[1] = 0x47
$syntheticBytes[2] = 0x4D; $syntheticBytes[3] = 0x42
# version 2 LE
$syntheticBytes[4] = 0x02
# bytes 8-41: already 0x00 from new()
# triplet at offset 42
$syntheticBytes[42] = 0x1e  # 30 LE
$syntheticBytes[46] = 0x1a  # 26 LE
$syntheticBytes[50] = 0x09  # 9 LE
# data pattern 54-59
$syntheticBytes[54] = 0x00; $syntheticBytes[55] = 0x00
$syntheticBytes[56] = 0x02; $syntheticBytes[57] = 0x14
$syntheticBytes[58] = 0x00; $syntheticBytes[59] = 0xff
# 0x55 filler 60-299
for ($i = 60; $i -lt 300; $i++) { $syntheticBytes[$i] = 0x55 }

[System.IO.File]::WriteAllBytes($syntheticFile, $syntheticBytes)

$outDir = Join-Path $scriptsDir '.local\test-map8w-inspector-output'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }

# === Assertion 1: .local guard exits nonzero ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $inspectScript `
    -ReferenceWorldmapBinPath $syntheticFile `
    -Output "$env:TEMP\pzmf-map8w-guard-test" `
    -TransitionOffset 42 -MaxBytes 300 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $eap
Assert-True ($guardExit -ne 0) '.local guard exits nonzero'

# === Run inspector ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $inspectScript `
    -ReferenceWorldmapBinPath $syntheticFile `
    -Output $outDir `
    -TransitionOffset 42 -MaxBytes 300 2>&1
$exitCode = $LASTEXITCODE
$ErrorActionPreference = $eap

# === Assertion 2: exits 0 ===
Assert-True ($exitCode -eq 0) 'inspector exits 0'

$jsonPath = Join-Path $outDir 'igmb-transition-structure-inspection.json'
$mdPath   = Join-Path $outDir 'igmb-transition-structure-inspection.md'

# === Assertion 3: JSON exists ===
Assert-True (Test-Path $jsonPath) 'JSON output exists'

# === Assertion 4: MD exists ===
Assert-True (Test-Path $mdPath) 'MD output exists'

$p = $null
if (Test-Path $jsonPath) {
    $p = Get-Content -Raw $jsonPath | ConvertFrom-Json
}

# === Assertion 5: schema ===
Assert-True ($null -ne $p -and $p.schema -eq 'pzmapforge.map8w-igmb-transition-structure-inspection.v0.1') "schema == 'pzmapforge.map8w-igmb-transition-structure-inspection.v0.1'"

# === Assertion 6: reference_present == true ===
Assert-True ($null -ne $p -and $p.reference_present -eq $true) 'reference_present == true'

# === Assertion 7: bytes_read_count == 300 ===
Assert-True ($null -ne $p -and [int]$p.bytes_read_count -eq 300) 'bytes_read_count == 300'

# === Assertion 8: max_bytes_allowed == 65536 ===
Assert-True ($null -ne $p -and [int]$p.max_bytes_allowed -eq 65536) 'max_bytes_allowed == 65536'

# === Assertion 9: full_file_read == true (300 bytes, all read) ===
Assert-True ($null -ne $p -and $p.full_file_read -eq $true) 'full_file_read == true'

# === Assertion 10: transition_offset == 42 ===
Assert-True ($null -ne $p -and [int]$p.transition_offset -eq 42) 'transition_offset == 42'

# === Assertion 11: transition_offset_is_4_byte_aligned == false (42 % 4 == 2) ===
Assert-True ($null -ne $p -and $p.transition_offset_is_4_byte_aligned -eq $false) 'transition_offset_is_4_byte_aligned == false'

# === Assertion 12: transition_offset_is_2_byte_aligned == true (42 % 2 == 0) ===
Assert-True ($null -ne $p -and $p.transition_offset_is_2_byte_aligned -eq $true) 'transition_offset_is_2_byte_aligned == true'

# === Assertion 13: candidate_header_u32_triplet.first == 30 ===
$tripletFirst = $null
if ($null -ne $p -and $null -ne $p.candidate_header_u32_triplet) {
    $tripletFirst = [int]$p.candidate_header_u32_triplet.first
}
Assert-True ($tripletFirst -eq 30) 'candidate_header_u32_triplet.first == 30'

# === Assertion 14: candidate_header_u32_triplet.second == 26 ===
$tripletSecond = $null
if ($null -ne $p -and $null -ne $p.candidate_header_u32_triplet) {
    $tripletSecond = [int]$p.candidate_header_u32_triplet.second
}
Assert-True ($tripletSecond -eq 26) 'candidate_header_u32_triplet.second == 26'

# === Assertion 15: candidate_header_u32_triplet.third == 9 ===
$tripletThird = $null
if ($null -ne $p -and $null -ne $p.candidate_header_u32_triplet) {
    $tripletThird = [int]$p.candidate_header_u32_triplet.third
}
Assert-True ($tripletThird -eq 9) 'candidate_header_u32_triplet.third == 9'

# === Assertion 16: candidate_header_triplet_confidence == 'low' ===
Assert-True ($null -ne $p -and $p.candidate_header_triplet_confidence -eq 'low') "candidate_header_triplet_confidence == 'low'"

# === Assertion 17: transition_structure_understood == false ===
Assert-True ($null -ne $p -and $p.transition_structure_understood -eq $false) 'transition_structure_understood == false'

# === Assertion 18: binary_writer_gate_closed == true ===
Assert-True ($null -ne $p -and $p.binary_writer_gate_closed -eq $true) 'binary_writer_gate_closed == true'

# === Assertion 19: playable_claim_allowed == false ===
Assert-True ($null -ne $p -and $p.playable_claim_allowed -eq $false) 'playable_claim_allowed == false'

# === Assertion 20: third_party_files_copied == false ===
Assert-True ($null -ne $p -and $p.third_party_files_copied -eq $false) 'third_party_files_copied == false'

# === Assertion 21: exact_u32le_values_from_transition_first_128 has entries ===
$u32Count = 0
if ($null -ne $p -and $null -ne $p.exact_u32le_values_from_transition_first_128) {
    $u32Count = ($p.exact_u32le_values_from_transition_first_128 | Measure-Object).Count
}
Assert-True ($u32Count -gt 0) 'exact_u32le_values_from_transition_first_128 has entries'

# === Assertion 22: exact_u16le_values_from_transition_first_128 has entries ===
$u16Count = 0
if ($null -ne $p -and $null -ne $p.exact_u16le_values_from_transition_first_128) {
    $u16Count = ($p.exact_u16le_values_from_transition_first_128 | Measure-Object).Count
}
Assert-True ($u16Count -gt 0) 'exact_u16le_values_from_transition_first_128 has entries'

# === Assertion 23: confidence_level == 'low' ===
Assert-True ($null -ne $p -and $p.confidence_level -eq 'low') "confidence_level == 'low'"

# === Assertion 24: transition_window_after_hex is non-null and non-empty ===
$afterHex = $null
if ($null -ne $p) { $afterHex = $p.transition_window_after_hex }
Assert-True ($null -ne $afterHex -and "$afterHex".Length -gt 0) 'transition_window_after_hex is non-null and non-empty'

# Cleanup synthetic file
if (Test-Path $syntheticFile) { Remove-Item -Force $syntheticFile }

Write-Host ""
Write-Host "Results: $pass passed, $fail failed, $total total"

if ($fail -gt 0) { exit 1 }
exit 0
