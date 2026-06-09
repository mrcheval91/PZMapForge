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

$scriptsDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$packetScript = Join-Path $scriptsDir 'prepare-build42-map8x-real-transition-structure-result-packet.ps1'
$outDir       = Join-Path $scriptsDir '.local\test-map8x-result-output'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }

# === Assertion 1: .local guard exits nonzero ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $packetScript `
    -Output 'C:\temp\no-local-in-path' 2>&1
$guardExit = $LASTEXITCODE
$ErrorActionPreference = $eap
Assert-True ($guardExit -ne 0) '.local guard exits nonzero'

# === Run packet script ===
$eap = $ErrorActionPreference; $ErrorActionPreference = 'SilentlyContinue'
$null = & powershell.exe -NonInteractive -NoProfile -File $packetScript `
    -Output $outDir 2>&1
$exitCode = $LASTEXITCODE
$ErrorActionPreference = $eap

# === Assertion 2: exits 0 ===
Assert-True ($exitCode -eq 0) 'packet script exits 0'

$jsonPath   = Join-Path $outDir 'map8x-real-transition-structure-result.json'
$mdPath     = Join-Path $outDir 'map8x-real-transition-structure-result.md'
$packetPath = Join-Path $outDir 'MAP_8X_REAL_TRANSITION_STRUCTURE_RESULT_PACKET.md'

# === Assertion 3: JSON exists ===
Assert-True (Test-Path $jsonPath) 'JSON output exists'

# === Assertion 4: MD exists ===
Assert-True (Test-Path $mdPath) 'MD output exists'

# === Assertion 5: packet MD exists ===
Assert-True (Test-Path $packetPath) 'packet MD exists'

$p = $null
if (Test-Path $jsonPath) {
    $p = Get-Content -Raw $jsonPath | ConvertFrom-Json
}

# === Assertion 6: schema ===
Assert-True ($null -ne $p -and $p.schema -eq 'pzmapforge.map8x-result.v0.1') "schema == 'pzmapforge.map8x-result.v0.1'"

# === Assertion 7: operator_ran_map8w_inspector == true ===
Assert-True ($null -ne $p -and $p.operator_ran_map8w_inspector -eq $true) 'operator_ran_map8w_inspector == true'

# === Assertion 8: bytes_read_count == 65536 ===
Assert-True ($null -ne $p -and [int]$p.bytes_read_count -eq 65536) 'bytes_read_count == 65536'

# === Assertion 9: max_bytes_allowed == 65536 ===
Assert-True ($null -ne $p -and [int]$p.max_bytes_allowed -eq 65536) 'max_bytes_allowed == 65536'

# === Assertion 10: full_file_read == false ===
Assert-True ($null -ne $p -and $p.full_file_read -eq $false) 'full_file_read == false'

# === Assertion 11: transition_offset == 6389 ===
Assert-True ($null -ne $p -and [int]$p.transition_offset -eq 6389) 'transition_offset == 6389'

# === Assertion 12: transition_offset_in_range == true ===
Assert-True ($null -ne $p -and $p.transition_offset_in_range -eq $true) 'transition_offset_in_range == true'

# === Assertion 13: transition_window_before_all_ff == true ===
Assert-True ($null -ne $p -and $p.transition_window_before_all_ff -eq $true) 'transition_window_before_all_ff == true'

# === Assertion 14: candidate_header_u32_triplet_first == 30 ===
Assert-True ($null -ne $p -and [int]$p.candidate_header_u32_triplet_first -eq 30) 'candidate_header_u32_triplet_first == 30'

# === Assertion 15: candidate_header_u32_triplet_second == 26 ===
Assert-True ($null -ne $p -and [int]$p.candidate_header_u32_triplet_second -eq 26) 'candidate_header_u32_triplet_second == 26'

# === Assertion 16: candidate_header_u32_triplet_third == 9 ===
Assert-True ($null -ne $p -and [int]$p.candidate_header_u32_triplet_third -eq 9) 'candidate_header_u32_triplet_third == 9'

# === Assertion 17: transition_structure_understood == false ===
Assert-True ($null -ne $p -and $p.transition_structure_understood -eq $false) 'transition_structure_understood == false'

# === Assertion 18: binary_writer_gate_closed == true ===
Assert-True ($null -ne $p -and $p.binary_writer_gate_closed -eq $true) 'binary_writer_gate_closed == true'

# === Assertion 19: playable_claim_allowed == false ===
Assert-True ($null -ne $p -and $p.playable_claim_allowed -eq $false) 'playable_claim_allowed == false'

# === Assertion 20: packet doc contains MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED ===
$packetContent = ''
if (Test-Path $packetPath) { $packetContent = Get-Content -Raw $packetPath }
Assert-True ($packetContent.Contains('MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED')) 'packet doc contains MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED sentinel'

Write-Host ""
Write-Host "Results: $pass passed, $fail failed, $total total"

if ($fail -gt 0) { exit 1 }
exit 0
