#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for the Build 42 writer contract artifacts (MAP-6J).

    Validates docs/MAP_6J_BUILD42_WRITER_CONTRACT.md,
    schemas/pzmapforge.build42-writer-plan.v0.1.schema.json, and
    examples/build42-writer-plan/minimal-empty-cell-writer-plan.json.

    Does not run any subprocesses or write files.
    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Split-Path -Parent $scriptDir

$contractDoc = Join-Path $repoRoot 'docs\MAP_6J_BUILD42_WRITER_CONTRACT.md'
$schemaFile  = Join-Path $repoRoot 'schemas\pzmapforge.build42-writer-plan.v0.1.schema.json'
$exampleFile = Join-Path $repoRoot 'examples\build42-writer-plan\minimal-empty-cell-writer-plan.json'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-writer-contract.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Tests 1-4: Writer contract doc
# ---------------------------------------------------------------------------

Write-Output '--- Tests 1-4: Writer contract doc ---'

Assert-True (Test-Path $contractDoc) 'MAP_6J_BUILD42_WRITER_CONTRACT.md exists'

$docContent = if (Test-Path $contractDoc) { Get-Content $contractDoc -Raw } else { '' }

Assert-True ($docContent -match 'BUILD42_WRITER_CONTRACT_CREATED') 'doc contains BUILD42_WRITER_CONTRACT_CREATED'
Assert-True ($docContent -match 'WRITER_NOT_IMPLEMENTED')          'doc contains WRITER_NOT_IMPLEMENTED'
Assert-True ($docContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'doc contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'

# ---------------------------------------------------------------------------
# Tests 5-6: Schema and example exist
# ---------------------------------------------------------------------------

Write-Output '--- Tests 5-6: Schema and example files ---'

Assert-True (Test-Path $schemaFile)  'pzmapforge.build42-writer-plan.v0.1.schema.json exists'
Assert-True (Test-Path $exampleFile) 'minimal-empty-cell-writer-plan.json exists'

# ---------------------------------------------------------------------------
# Test 7: Example parses as JSON
# ---------------------------------------------------------------------------

Write-Output '--- Test 7: Example parses ---'
$ex = $null
try {
    $ex = Get-Content $exampleFile -Raw | ConvertFrom-Json
    Assert-True ($null -ne $ex) 'Example JSON parses successfully'
} catch {
    Assert-True $false 'Example JSON parses successfully'
}

if ($null -eq $ex) {
    Write-Output '  (skipping remaining assertions: parse failed)'
    Write-Output ''
    Write-Output '----------------------------------------'
    Write-Output "Results: $pass passed, $fail failed"
    Write-Output '----------------------------------------'
    exit 1
}

# ---------------------------------------------------------------------------
# Tests 8-16: Example field assertions
# ---------------------------------------------------------------------------

Write-Output '--- Tests 8-13: Schema and geometry ---'

Assert-True ($ex.schema -eq 'pzmapforge.build42-writer-plan.v0.1') 'schema == pzmapforge.build42-writer-plan.v0.1'
Assert-True ($ex.safety.writer_implemented      -eq $false) 'safety.writer_implemented == false'
Assert-True ($ex.safety.load_test_performed     -eq $false) 'safety.load_test_performed == false'
Assert-True ($ex.safety.playable_export_claimed -eq $false) 'safety.playable_export_claimed == false'
Assert-True ([int]$ex.geometry_model.chunk_count    -eq 1024) 'geometry_model.chunk_count == 1024'
Assert-True ([int]$ex.geometry_model.cell_size_tiles -eq 256) 'geometry_model.cell_size_tiles == 256'

Write-Output '--- Tests 14-16: Contract magic bytes and size ---'

Assert-True ($ex.lotp_contract.magic_ascii -eq 'LOTP') 'lotp_contract.magic_ascii == LOTP'
Assert-True ($ex.loth_contract.magic_ascii -eq 'LOTH') 'loth_contract.magic_ascii == LOTH'
Assert-True ([int]$ex.chunkdata_contract.size_bytes -eq 1026) 'chunkdata_contract.size_bytes == 1026'

# ---------------------------------------------------------------------------
# Tests 17-19: Unknowns and review status
# ---------------------------------------------------------------------------

Write-Output '--- Tests 17-19: Unknowns and review gate ---'

$unknownIds = @($ex.unknowns | ForEach-Object { $_.id })
Assert-True ($unknownIds -contains 'chunk_payload_format') 'unknowns contains chunk_payload_format'
Assert-True ($unknownIds -contains 'loth_minimum_entries') 'unknowns contains loth_minimum_entries'
Assert-True ($ex.contract_review_required -eq $true) 'contract_review_required == true'

# ---------------------------------------------------------------------------
# Test 20: No playable claim in example JSON
# ---------------------------------------------------------------------------

Write-Output '--- Test 20: No playable claim in example ---'
$exJson = Get-Content $exampleFile -Raw
# Check there's no "playable_export_claimed": true anywhere
Assert-True ($exJson -notmatch '"playable_export_claimed"\s*:\s*true') 'Example does not contain playable_export_claimed: true'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
