#Requires -Version 5.1
<#
.SYNOPSIS
    Tests the MAP-9B Build 42 canary writer capability inspector.
    Runs inspect-build42-canary-writer-capability.ps1 against a temp .local/ path
    and asserts 22 contract requirements.
    Exits 0 if all pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot      = Split-Path -Parent $scriptDir
$inspectScript = Join-Path $repoRoot 'scripts\inspect-build42-canary-writer-capability.ps1'
$map9bDoc      = Join-Path $repoRoot 'docs\MAP_9B_CANARY_WRITER_UNBLOCK.md'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

# ---------------------------------------------------------------------------
# Test 1: .local guard - refuses output outside .local/
# ---------------------------------------------------------------------------

Write-Output "--- Test 1: .local guard ---"
$outside = Join-Path $repoRoot 'scripts\map9b-canary-guard-test'
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $inspectScript -Output $outside 2>$null
$ecGuard = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ecGuard -ne 0) "Inspector refuses output outside .local/"
if (Test-Path $outside) { Remove-Item -Recurse -Force $outside }

# ---------------------------------------------------------------------------
# Tests 2-4: Inspector script and doc exist, valid run
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 2-4: File existence and valid run ---"
Assert-True (Test-Path $inspectScript -PathType Leaf) "inspect-build42-canary-writer-capability.ps1 exists"
Assert-True (Test-Path $map9bDoc -PathType Leaf) "docs/MAP_9B_CANARY_WRITER_UNBLOCK.md exists"

$testOutput = Join-Path $repoRoot '.local\map9b-capability-test'
if (Test-Path $testOutput) { Remove-Item -Recurse -Force $testOutput }
& powershell -ExecutionPolicy Bypass -File $inspectScript -Output $testOutput
Assert-True ($LASTEXITCODE -eq 0) "Inspector exits 0 on valid .local/ output"

# ---------------------------------------------------------------------------
# Tests 5-6: Output files and schema
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 5-6: Output file and schema ---"
$jsonPath = Join-Path $testOutput 'build42-canary-writer-capability.json'
Assert-True (Test-Path $jsonPath -PathType Leaf) "build42-canary-writer-capability.json exists"

$p = Get-Content $jsonPath -Raw | ConvertFrom-Json
Assert-True ($p.schema -eq 'pzmapforge.build42-canary-writer-capability.v0.1') `
    "schema == 'pzmapforge.build42-canary-writer-capability.v0.1'"

# ---------------------------------------------------------------------------
# Tests 7-10: Safety / inspection constraint fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 7-10: Safety fields ---"
Assert-True ($p.inspected_repo_only   -eq $true)  "inspected_repo_only == true"
Assert-True ($p.pz_assets_read        -eq $false) "pz_assets_read == false"
Assert-True ($p.pz_run_performed      -eq $false) "pz_run_performed == false"
Assert-True ($p.playable_claim_allowed -eq $false) "playable_claim_allowed == false"

# ---------------------------------------------------------------------------
# Tests 11-14: Canary capability verdict (explicit field presence + values)
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 11-14: Canary capability verdict ---"
Assert-True ($null -ne $p.PSObject.Properties['canary_writer_available']) `
    "canary_writer_available field is explicit"
Assert-True ($null -ne $p.PSObject.Properties['canary_writer_blocked']) `
    "canary_writer_blocked field is explicit"
Assert-True ($p.canary_writer_available -eq $false) "canary_writer_available == false"
Assert-True ($p.canary_writer_blocked   -eq $true)  "canary_writer_blocked == true"

# ---------------------------------------------------------------------------
# Tests 15-18: Technical blocker flags
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 15-18: Technical blocker flags ---"
Assert-True ($p.visible_tile_encoding_supported      -eq $false) "visible_tile_encoding_supported == false"
Assert-True ($p.canary_strategy_available            -eq $false) "canary_strategy_available == false"
Assert-True ($p.lotp_chunk_payload_format_understood -eq $false) "lotp_chunk_payload_format_understood == false"
Assert-True ($p.tile_placement_record_model_exists   -eq $false) "tile_placement_record_model_exists == false"

# ---------------------------------------------------------------------------
# Tests 19-22: Outcome, writer confirmation, and next branch
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 19-22: Outcome and next branch ---"
Assert-True ($p.outcome -eq 'B') "outcome == 'B'"
Assert-True (-not [string]::IsNullOrWhiteSpace($p.outcome_label)) "outcome_label is present and non-empty"
Assert-True ($p.writer_command_found -eq $true) "writer_command_found == true"
Assert-True (-not [string]::IsNullOrWhiteSpace($p.next_research_branch)) "next_research_branch is present and non-empty"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
