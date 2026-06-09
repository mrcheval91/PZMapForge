#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-8K: inspect-build42-parent-map-metadata-contract.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot       = Split-Path -Parent $scriptDir
$comparatorScript = Join-Path $repoRoot 'scripts\inspect-build42-parent-map-metadata-contract.ps1'
$tempRoot       = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-parent-map-metadata-contract.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t8k-' + [System.IO.Path]::GetRandomFileName())
$badOut   = Join-Path $tempRoot 'pzmf-t8k-bad-no-local'
New-Item -ItemType Directory -Force -Path $badOut | Out-Null

# Build minimal candidate parent dir
$candDir = Join-Path $testBase 'candidate-parent'
New-Item -ItemType Directory -Force -Path $candDir | Out-Null
Set-Content -Path (Join-Path $candDir 'map.info') -Value @"
title=TestCandidateParent
fixed2x=true
"@ -Encoding ASCII
Set-Content -Path (Join-Path $candDir 'worldmap.xml') -Value '<worldMap/>' -Encoding ASCII

# Build minimal reference parent dir (different fields)
$refDir = Join-Path $testBase 'reference-parent'
New-Item -ItemType Directory -Force -Path $refDir | Out-Null
Set-Content -Path (Join-Path $refDir 'map.info') -Value @"
title=TestReferenceParent
fixed2x=true
demoVideo=PR.bik
"@ -Encoding ASCII

function Invoke-Comparator {
    param([string]$Cand, [string]$Ref, [string]$Out)
    & powershell -ExecutionPolicy Bypass -File $comparatorScript `
        -CandidateParentRoot $Cand `
        -ReferenceParentRoot $Ref `
        -Output $Out `
        -CandidateParentMapId 'TestCandidateParent' `
        -ReferenceParentMapId 'TestReferenceParent' | Out-Null
    return [int]$LASTEXITCODE
}

# Test 1: Refuses output outside .local
Write-Output '--- Test 1: Refuses output outside .local ---'
$t1exit = Invoke-Comparator -Cand $candDir -Ref $refDir -Out $badOut
Assert-True ($t1exit -ne 0) 'Test1: output outside .local exits nonzero'

# Run comparator with valid .local output
Write-Output ''
Write-Output '--- Running comparator (Tests 2-20) ---'
$outDir = Join-Path $testBase '.local\map8k-output'
$t2exit = Invoke-Comparator -Cand $candDir -Ref $refDir -Out $outDir

# Test 2: Exits 0
Assert-True ($t2exit -eq 0) 'Test2: comparator exits 0 with valid output path'

# Tests 3-4: Output files exist
Write-Output ''
Write-Output '--- Tests 3-4: Output files ---'
Assert-True (Test-Path (Join-Path $outDir 'build42-parent-map-metadata-contract.json')) `
    'Test3: build42-parent-map-metadata-contract.json exists'
Assert-True (Test-Path (Join-Path $outDir 'build42-parent-map-metadata-contract.md')) `
    'Test4: build42-parent-map-metadata-contract.md exists'

# Parse output JSON
$rep = if (Test-Path (Join-Path $outDir 'build42-parent-map-metadata-contract.json')) {
    Get-Content (Join-Path $outDir 'build42-parent-map-metadata-contract.json') -Raw | ConvertFrom-Json
} else { $null }

Write-Output ''
Write-Output '--- Tests 5-20: JSON fields ---'

# Test 5: Schema
Assert-True ($null -ne $rep -and [string]$rep.schema -eq 'pzmapforge.map8k-parent-metadata-contract.v0.1') `
    "Test5: schema correct (got '$($rep.schema)')"

# Test 6: candidate_parent_map_id
Assert-True ($null -ne $rep -and [string]$rep.candidate_parent_map_id -eq 'TestCandidateParent') `
    "Test6: candidate_parent_map_id=TestCandidateParent (got '$($rep.candidate_parent_map_id)')"

# Test 7: reference_parent_map_id
Assert-True ($null -ne $rep -and [string]$rep.reference_parent_map_id -eq 'TestReferenceParent') `
    "Test7: reference_parent_map_id=TestReferenceParent (got '$($rep.reference_parent_map_id)')"

# Test 8: binary_contents_read = false
Assert-True ($null -ne $rep -and [bool]$rep.binary_contents_read -eq $false) `
    'Test8: binary_contents_read=false'

# Test 9: third_party_files_copied = false
Assert-True ($null -ne $rep -and [bool]$rep.third_party_files_copied -eq $false) `
    'Test9: third_party_files_copied=false'

# Test 10: no_project_russia_files_copied = true
Assert-True ($null -ne $rep -and [bool]$rep.no_project_russia_files_copied -eq $true) `
    'Test10: no_project_russia_files_copied=true'

# Test 11: binary_writer_gate_closed = true
Assert-True ($null -ne $rep -and [bool]$rep.binary_writer_gate_closed -eq $true) `
    'Test11: binary_writer_gate_closed=true'

# Test 12: playable_claim_allowed = false
Assert-True ($null -ne $rep -and [bool]$rep.playable_claim_allowed -eq $false) `
    'Test12: playable_claim_allowed=false'

# Test 13: candidate_has_lots_field = false (no lots in test candidate map.info)
Assert-True ($null -ne $rep -and [bool]$rep.candidate_has_lots_field -eq $false) `
    'Test13: candidate_has_lots_field=false'

# Test 14: reference_has_lots_field = false (no lots in test reference map.info)
Assert-True ($null -ne $rep -and [bool]$rep.reference_has_lots_field -eq $false) `
    'Test14: reference_has_lots_field=false'

# Test 15: candidate_fixed2x = 'true' (set in test candidate map.info)
Assert-True ($null -ne $rep -and [string]$rep.candidate_fixed2x -eq 'true') `
    "Test15: candidate_fixed2x=true (got '$($rep.candidate_fixed2x)')"

# Test 16: candidate_demoVideo_present = false (not in candidate map.info)
Assert-True ($null -ne $rep -and [bool]$rep.candidate_demoVideo_present -eq $false) `
    'Test16: candidate_demoVideo_present=false'

# Test 17: reference_demoVideo_present = true (demoVideo=PR.bik in reference map.info)
Assert-True ($null -ne $rep -and [bool]$rep.reference_demoVideo_present -eq $true) `
    'Test17: reference_demoVideo_present=true'

# Test 18: candidate_worldmap_xml.exists = true (we created worldmap.xml in candidate)
$cwx = if ($null -ne $rep) { $rep.candidate_worldmap_xml } else { $null }
Assert-True ($null -ne $cwx -and [bool]$cwx.exists -eq $true) `
    'Test18: candidate_worldmap_xml.exists=true'

# Test 19: candidate_worldmap_xml.skeletal_or_substantial = 'skeletal' (tiny file)
Assert-True ($null -ne $cwx -and [string]$cwx.skeletal_or_substantial -eq 'skeletal') `
    "Test19: candidate_worldmap_xml.skeletal_or_substantial=skeletal (got '$($cwx.skeletal_or_substantial)')"

# Test 20: map_info_field_differences has at least 1 entry (title differs + demoVideo absent)
$diffs = if ($null -ne $rep) { @($rep.map_info_field_differences) } else { @() }
Assert-True ($diffs.Count -ge 1) `
    "Test20: map_info_field_differences count >= 1 (got $($diffs.Count))"

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
