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
$packetScript = Join-Path $scriptsDir 'prepare-build42-map8v-real-first-non-ff-transition-result-packet.ps1'
$outDir       = Join-Path $scriptsDir '.local\test-map8v-result-output'
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

$jsonPath   = Join-Path $outDir 'map8v-real-first-non-ff-transition-result.json'
$mdPath     = Join-Path $outDir 'map8v-real-first-non-ff-transition-result.md'
$packetPath = Join-Path $outDir 'MAP_8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_PACKET.md'

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
Assert-True ($null -ne $p -and $p.schema -eq 'pzmapforge.map8v-result.v0.1') "schema == 'pzmapforge.map8v-result.v0.1'"

# === Assertion 7: operator_ran_map8u_scanner == true ===
Assert-True ($null -ne $p -and $p.operator_ran_map8u_scanner -eq $true) 'operator_ran_map8u_scanner == true'

# === Assertion 8: reference_size_bytes == 283881 ===
Assert-True ($null -ne $p -and [long]$p.reference_size_bytes -eq 283881) 'reference_size_bytes == 283881'

# === Assertion 9: bytes_read_count == 65536 ===
Assert-True ($null -ne $p -and [int]$p.bytes_read_count -eq 65536) 'bytes_read_count == 65536'

# === Assertion 10: first_non_ff_found == true ===
Assert-True ($null -ne $p -and $p.first_non_ff_found -eq $true) 'first_non_ff_found == true'

# === Assertion 11: first_non_ff_offset == 6389 ===
Assert-True ($null -ne $p -and [int]$p.first_non_ff_offset -eq 6389) 'first_non_ff_offset == 6389'

# === Assertion 12: ff_run_length_until_first_non_ff == 6256 ===
Assert-True ($null -ne $p -and [int]$p.ff_run_length_until_first_non_ff -eq 6256) 'ff_run_length_until_first_non_ff == 6256'

# === Assertion 13: transition_offset_is_4_byte_aligned == false ===
Assert-True ($null -ne $p -and $p.transition_offset_is_4_byte_aligned -eq $false) 'transition_offset_is_4_byte_aligned == false'

# === Assertion 14: exact_u32le_at_transition_0 == 30 ===
Assert-True ($null -ne $p -and [int]$p.exact_u32le_at_transition_0 -eq 30) 'exact_u32le_at_transition_0 == 30'

# === Assertion 15: exact_u32le_at_transition_4 == 26 ===
Assert-True ($null -ne $p -and [int]$p.exact_u32le_at_transition_4 -eq 26) 'exact_u32le_at_transition_4 == 26'

# === Assertion 16: exact_u32le_at_transition_8 == 9 ===
Assert-True ($null -ne $p -and [int]$p.exact_u32le_at_transition_8 -eq 9) 'exact_u32le_at_transition_8 == 9'

# === Assertion 17: binary_writer_gate_closed == true ===
Assert-True ($null -ne $p -and $p.binary_writer_gate_closed -eq $true) 'binary_writer_gate_closed == true'

# === Assertion 18: playable_claim_allowed == false ===
Assert-True ($null -ne $p -and $p.playable_claim_allowed -eq $false) 'playable_claim_allowed == false'

# === Assertion 19: third_party_files_copied == false ===
Assert-True ($null -ne $p -and $p.third_party_files_copied -eq $false) 'third_party_files_copied == false'

# === Assertion 20: packet doc contains MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED ===
$packetContent = ''
if (Test-Path $packetPath) { $packetContent = Get-Content -Raw $packetPath }
Assert-True ($packetContent.Contains('MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED')) 'packet doc contains MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED sentinel'

Write-Host ""
Write-Host "Results: $pass passed, $fail failed, $total total"

if ($fail -gt 0) { exit 1 }
exit 0
