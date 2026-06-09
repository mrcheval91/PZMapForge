#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for MAP-8L: prepare-build42-map8l-worldmap-xml-candidate.ps1.

    Expected assertion count: 20
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot      = Split-Path -Parent $scriptDir
$prepareScript = Join-Path $repoRoot 'scripts\prepare-build42-map8l-worldmap-xml-candidate.ps1'
$tempRoot      = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-build42-map8l-worldmap-xml-candidate.ps1'
Write-Output ''

$testBase = Join-Path $tempRoot ('pzmf-t8l-' + [System.IO.Path]::GetRandomFileName())
$badPath  = Join-Path $tempRoot 'pzmf-t8l-bad-no-local'
New-Item -ItemType Directory -Force -Path $badPath | Out-Null

function Invoke-Prepare {
    param([string]$OutDir)
    & powershell -ExecutionPolicy Bypass -File $prepareScript -Output $OutDir | Out-Null
    return [int]$LASTEXITCODE
}

# Test 1: Refuses output outside .local
Write-Output '--- Test 1: Refuses output outside .local ---'
$t1exit = Invoke-Prepare -OutDir $badPath
Assert-True ($t1exit -ne 0) 'Test1: output outside .local exits nonzero'

# Run prepare
Write-Output ''
Write-Output '--- Running prepare (Tests 2-20) ---'
$outDir = Join-Path $testBase '.local\map8l-output'
$t2exit = Invoke-Prepare -OutDir $outDir

# Test 2: Exits 0
Assert-True ($t2exit -eq 0) 'Test2: prepare exits 0 with valid output path'

# Tests 3-5: Required output files
Write-Output ''
Write-Output '--- Tests 3-5: Required output files ---'
Assert-True (Test-Path (Join-Path $outDir 'worldmap.xml')) `
    'Test3: worldmap.xml exists'
Assert-True (Test-Path (Join-Path $outDir 'map8l-preflight.json')) `
    'Test4: map8l-preflight.json exists'
Assert-True (Test-Path (Join-Path $outDir 'MAP_8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE_PACKET.md')) `
    'Test5: MAP_8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE_PACKET.md exists'

# worldmap.xml content checks
$xmlPath = Join-Path $outDir 'worldmap.xml'
$xmlSize = if (Test-Path $xmlPath) { (Get-Item -LiteralPath $xmlPath).Length } else { 0 }
$xmlContent = if (Test-Path $xmlPath) { Get-Content -LiteralPath $xmlPath -Raw } else { '' }
$xmlLines = if (Test-Path $xmlPath) { @(Get-Content -LiteralPath $xmlPath).Count } else { 0 }

Write-Output ''
Write-Output '--- Tests 6-12: worldmap.xml content ---'

# Test 6: worldmap.xml is substantial (> 500 bytes)
Assert-True ($xmlSize -gt 500) `
    "Test6: worldmap.xml size > 500 bytes (got $xmlSize)"

# Test 7: worldmap.xml line count > 5
Assert-True ($xmlLines -gt 5) `
    "Test7: worldmap.xml line count > 5 (got $xmlLines)"

# Test 8: worldmap.xml has XML declaration
Assert-True ($xmlContent -match '<\?xml') `
    'Test8: worldmap.xml contains XML declaration'

# Test 9: worldmap.xml has worldmap root element
Assert-True ($xmlContent -match '<worldmap') `
    'Test9: worldmap.xml contains <worldmap root element'

# Test 10: worldmap.xml references cell worldX=35
Assert-True ($xmlContent -match 'worldX="35"') `
    'Test10: worldmap.xml contains worldX="35"'

# Test 11: worldmap.xml references cell worldY=27
Assert-True ($xmlContent -match 'worldY="27"') `
    'Test11: worldmap.xml contains worldY="27"'

# Test 12: worldmap.xml references 35_27.lotheader
Assert-True ($xmlContent -match '35_27\.lotheader') `
    'Test12: worldmap.xml references 35_27.lotheader'

# Parse preflight JSON
$pfl = if (Test-Path (Join-Path $outDir 'map8l-preflight.json')) {
    Get-Content (Join-Path $outDir 'map8l-preflight.json') -Raw | ConvertFrom-Json
} else { $null }

Write-Output ''
Write-Output '--- Tests 13-20: Preflight JSON fields ---'

# Test 13: Schema correct
Assert-True ($null -ne $pfl -and [string]$pfl.schema -eq 'pzmapforge.map8l-preflight.v0.1') `
    "Test13: schema correct (got '$($pfl.schema)')"

# Test 14: parent_map_id = PZMapForge
Assert-True ($null -ne $pfl -and [string]$pfl.parent_map_id -eq 'PZMapForge') `
    "Test14: parent_map_id=PZMapForge (got '$($pfl.parent_map_id)')"

# Test 15: worldmap_xml_substantial = true
Assert-True ($null -ne $pfl -and [bool]$pfl.worldmap_xml_substantial -eq $true) `
    'Test15: worldmap_xml_substantial=true'

# Test 16: worldmap_xml_size_bytes > 500
Assert-True ($null -ne $pfl -and [int]$pfl.worldmap_xml_size_bytes -gt 500) `
    "Test16: worldmap_xml_size_bytes > 500 (got $($pfl.worldmap_xml_size_bytes))"

# Test 17: binary_contents_read = false
Assert-True ($null -ne $pfl -and [bool]$pfl.binary_contents_read -eq $false) `
    'Test17: binary_contents_read=false'

# Test 18: no_project_russia_content_used = true
Assert-True ($null -ne $pfl -and [bool]$pfl.no_project_russia_content_used -eq $true) `
    'Test18: no_project_russia_content_used=true'

# Test 19: playable_claim_allowed = false
Assert-True ($null -ne $pfl -and [bool]$pfl.playable_claim_allowed -eq $false) `
    'Test19: playable_claim_allowed=false'

# Test 20: binary_writer_gate_closed = true
Assert-True ($null -ne $pfl -and [bool]$pfl.binary_writer_gate_closed -eq $true) `
    'Test20: binary_writer_gate_closed=true'

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
