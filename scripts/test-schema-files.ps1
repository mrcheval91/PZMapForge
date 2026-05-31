#Requires -Version 5.1
<#
.SYNOPSIS
    Schema file sanity validator for all PZMapForge schemas.

    Validates:
      schemas/pzmapforge.parsed-cell.v0.1.schema.json
      schemas/pzmapforge.proof-packet.v0.1.schema.json
      schemas/pzmapforge.regions.v0.1.schema.json

    Checks: meta fields, $id sentinel, required list, properties keys.
    No external dependencies. PowerShell 5.1 compatible.
    Exits 0 if all checks pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir
$schemaDir = Join-Path $repoRoot 'schemas'

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Test-Schema {
    param([string]$FileName, [string]$ExpectedId, [string[]]$CheckRequired)

    $path = Join-Path $script:schemaDir $FileName
    Write-Output "Schema: $FileName"

    if (-not (Test-Path $path -PathType Leaf)) {
        Write-Error "Schema file not found: $path"
        exit 1
    }
    $s = Get-Content $path -Raw | ConvertFrom-Json

    Write-Output "--- Meta fields ---"
    Assert-True ($null -ne $s.PSObject.Properties['$schema'])   '$schema present'
    Assert-True ($null -ne $s.PSObject.Properties['$id'])       '$id present'
    Assert-True ($null -ne $s.PSObject.Properties['title'])     'title present'
    Assert-True ($null -ne $s.PSObject.Properties['required'])  'required present'
    Assert-True ($null -ne $s.PSObject.Properties['properties']) 'properties present'

    Write-Output "--- Id sentinel ---"
    $idValue = $s.'$id'
    Assert-True ($idValue -eq $ExpectedId) "`$id == '$ExpectedId' (got '$idValue')"

    Write-Output "--- Required list ---"
    $reqInSchema = @($s.required)
    foreach ($field in $CheckRequired) {
        Assert-True ($reqInSchema -contains $field) "required contains '$field'"
    }

    Write-Output "--- Properties keys ---"
    $props = $s.properties
    foreach ($field in $CheckRequired) {
        Assert-True ($null -ne $props.PSObject.Properties[$field]) "properties.$field defined"
    }

    Write-Output ""
}

# ---------------------------------------------------------------------------
# parsed-cell schema
# ---------------------------------------------------------------------------

Test-Schema `
    -FileName    'pzmapforge.parsed-cell.v0.1.schema.json' `
    -ExpectedId  'pzmapforge.parsed-cell.v0.1' `
    -CheckRequired @(
        'schema', 'tool', 'claim_boundary',
        'width', 'height',
        'matching', 'legend', 'counts', 'nearest_drift', 'rows', 'outputs'
    )

# ---------------------------------------------------------------------------
# proof-packet schema
# ---------------------------------------------------------------------------

Test-Schema `
    -FileName    'pzmapforge.proof-packet.v0.2.schema.json' `
    -ExpectedId  'pzmapforge.proof-packet.v0.2' `
    -CheckRequired @(
        'schema', 'generated_at_utc', 'repo_root',
        'git_branch', 'git_commit',
        'claim_boundary', 'validation_summary', 'safety',
        'parsed_cell_sha256', 'tmx_sha256',
        'regions_json_sha256', 'regions_report_sha256'
    )

# ---------------------------------------------------------------------------
# regions schema
# ---------------------------------------------------------------------------

Test-Schema `
    -FileName    'pzmapforge.regions.v0.1.schema.json' `
    -ExpectedId  'pzmapforge.regions.v0.1' `
    -CheckRequired @(
        'schema', 'claim_boundary',
        'width', 'height', 'total_regions',
        'regions', 'summary_by_kind'
    )

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
