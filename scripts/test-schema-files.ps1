#Requires -Version 5.1
<#
.SYNOPSIS
    Schema file sanity validator for PZMapForge.

    Parses schemas/pzmapforge.parsed-cell.v0.1.schema.json and verifies its
    structure: meta-schema reference, id, title, required list, and properties.

    No external dependencies. PowerShell 5.1 compatible.
    Exits 0 if all checks pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$schemaPath = Join-Path $repoRoot 'schemas\pzmapforge.parsed-cell.v0.1.schema.json'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output "Schema sanity: $schemaPath"
Write-Output ""

# ---------------------------------------------------------------------------
# Parse
# ---------------------------------------------------------------------------

if (-not (Test-Path $schemaPath -PathType Leaf)) {
    Write-Error "Schema file not found: $schemaPath"
    exit 1
}

$s = Get-Content $schemaPath -Raw | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Top-level meta fields
# ---------------------------------------------------------------------------

Write-Output "--- Meta fields ---"
Assert-True ($null -ne $s.PSObject.Properties['$schema'])  '$schema field present'
Assert-True ($null -ne $s.PSObject.Properties['$id'])      '$id field present'
Assert-True ($null -ne $s.PSObject.Properties['title'])    'title field present'
Assert-True ($null -ne $s.PSObject.Properties['required']) 'required field present'
Assert-True ($null -ne $s.PSObject.Properties['properties']) 'properties field present'

# ---------------------------------------------------------------------------
# $id value
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Id sentinel ---"
$idValue = $s.'$id'
Assert-True ($idValue -eq 'pzmapforge.parsed-cell.v0.1') `
    "`$id == 'pzmapforge.parsed-cell.v0.1' (got '$idValue')"

# ---------------------------------------------------------------------------
# required list
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Required list ---"
$requiredInSchema = @($s.required)

$checkRequired = @(
    'schema', 'tool', 'claim_boundary',
    'width', 'height',
    'matching', 'legend', 'counts', 'nearest_drift', 'rows', 'outputs'
)

foreach ($field in $checkRequired) {
    Assert-True ($requiredInSchema -contains $field) "required contains '$field'"
}

# ---------------------------------------------------------------------------
# properties keys
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Properties keys ---"
$props = $s.properties

foreach ($field in $checkRequired) {
    Assert-True ($null -ne $props.PSObject.Properties[$field]) "properties.$field defined"
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
