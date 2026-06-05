#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for extract-map6n-current-candidate-log-evidence.ps1 (MAP-6N).

    Creates synthetic log fixtures under temp .local dirs and validates triage
    output. Does not use real PZ files. Does not read outside .local.
    Expected assertion count: 12
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot     = Split-Path -Parent $scriptDir
$triageScript = Join-Path $repoRoot 'scripts\extract-map6n-current-candidate-log-evidence.ps1'
$tempRoot     = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

Write-Output 'test-extract-map6n-log-evidence.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Helper: run triage script, return exit code (stdout suppressed)
# ---------------------------------------------------------------------------

function Invoke-Triage {
    param([string]$InputLogFolder, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $triageScript `
        -InputLogFolder $InputLogFolder `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

# ---------------------------------------------------------------------------
# Test 1: Current candidate loading line, no exception => LOAD_TEST_INCONCLUSIVE
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: current candidate loading, no exception ---'

$t1Base   = Join-Path $tempRoot ('pzmf-t1-' + [System.IO.Path]::GetRandomFileName())
$t1Input  = Join-Path $t1Base '.local\map6n-input'
$t1Output = Join-Path $t1Base '.local\map6n-output'
New-Item -ItemType Directory -Force -Path $t1Input  | Out-Null
New-Item -ItemType Directory -Force -Path $t1Output | Out-Null

Set-Content -Path (Join-Path $t1Input 'console.txt') -Value @'
LOG : General > GameVersion: Build 42.0.4
LOG : Mod > loading pzmapforge_build42_candidate_001
LOG : Mod > Mod loaded successfully
'@ -Encoding UTF8

$t1Exit = Invoke-Triage -InputLogFolder $t1Input -Output $t1Output
$t1Json = Join-Path $t1Output 'map6n-log-triage-report.json'
$t1Data = if (Test-Path $t1Json) { Get-Content $t1Json -Raw | ConvertFrom-Json } else { $null }

Assert-True ($t1Exit -eq 0) 'Test1: triage exits 0'
Assert-True (Test-Path $t1Json) 'Test1: triage JSON exists'
Assert-True ([string]$t1Data.result_recommendation -eq 'LOAD_TEST_INCONCLUSIVE') 'Test1: result_recommendation == LOAD_TEST_INCONCLUSIVE'
Assert-True ($t1Data.candidate_specific_exception_found -eq $false) 'Test1: candidate_specific_exception_found == false'

# ---------------------------------------------------------------------------
# Test 2: Stale maptest_a exception does not propagate to current result
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: stale maptest_a exception excluded ---'

$t2Base   = Join-Path $tempRoot ('pzmf-t2-' + [System.IO.Path]::GetRandomFileName())
$t2Input  = Join-Path $t2Base '.local\map6n-input'
$t2Output = Join-Path $t2Base '.local\map6n-output'
New-Item -ItemType Directory -Force -Path $t2Input  | Out-Null
New-Item -ItemType Directory -Force -Path $t2Output | Out-Null

Set-Content -Path (Join-Path $t2Input 'console.txt') -Value @'
LOG : General > GameVersion: Build 42.0.4
ERROR : IsoLot > java.io.EOFException at IsoLot.readInt in pzmapforge_manual_b42_001_maptest_a
ERROR : CellLoader > Exception loading pzmapforge_manual_b42_001_maptest_a cell 0_0
'@ -Encoding UTF8

$t2Exit = Invoke-Triage -InputLogFolder $t2Input -Output $t2Output
$t2Json = Join-Path $t2Output 'map6n-log-triage-report.json'
$t2Data = if (Test-Path $t2Json) { Get-Content $t2Json -Raw | ConvertFrom-Json } else { $null }

Assert-True ($t2Exit -eq 0) 'Test2: triage exits 0'
Assert-True ([int]$t2Data.stale_maptest_a_matches -ge 1) 'Test2: stale_maptest_a_matches >= 1'
Assert-True ([string]$t2Data.result_recommendation -eq 'LOAD_TEST_INCONCLUSIVE') 'Test2: stale traces do not flip result to failure'

# ---------------------------------------------------------------------------
# Test 3: Current candidate IsoLot exception => exception detected
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 3: current candidate IsoLot exception detected ---'

$t3Base   = Join-Path $tempRoot ('pzmf-t3-' + [System.IO.Path]::GetRandomFileName())
$t3Input  = Join-Path $t3Base '.local\map6n-input'
$t3Output = Join-Path $t3Base '.local\map6n-output'
New-Item -ItemType Directory -Force -Path $t3Input  | Out-Null
New-Item -ItemType Directory -Force -Path $t3Output | Out-Null

Set-Content -Path (Join-Path $t3Input 'console.txt') -Value @'
LOG : General > GameVersion: Build 42.0.4
LOG : Mod > loading pzmapforge_build42_candidate_001
ERROR : IsoLot > java.io.Exception at IsoLot.readInt pzmapforge_build42_candidate_001
'@ -Encoding UTF8

$t3Exit = Invoke-Triage -InputLogFolder $t3Input -Output $t3Output
$t3Json = Join-Path $t3Output 'map6n-log-triage-report.json'
$t3Data = if (Test-Path $t3Json) { Get-Content $t3Json -Raw | ConvertFrom-Json } else { $null }

Assert-True ($t3Exit -eq 0) 'Test3: triage exits 0'
Assert-True ($t3Data.candidate_specific_exception_found -eq $true) 'Test3: candidate_specific_exception_found == true'
Assert-True ([string]$t3Data.result_recommendation -ne 'LOAD_TEST_INCONCLUSIVE') 'Test3: result_recommendation != LOAD_TEST_INCONCLUSIVE when exception found'

# ---------------------------------------------------------------------------
# Test 4: Paths outside .local refused
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 4: paths outside .local refused ---'

$badInput  = Join-Path $tempRoot 'pzmf-bad-input-no-local'
$badOutput = Join-Path $tempRoot 'pzmf-bad-output-no-local'
New-Item -ItemType Directory -Force -Path $badInput | Out-Null

$t4ExitBadInput  = Invoke-Triage -InputLogFolder $badInput -Output (Join-Path $tempRoot 'pzmf-t4-ok\.local\out')
$t4ExitBadOutput = Invoke-Triage -InputLogFolder $t1Input  -Output $badOutput

Assert-True ($t4ExitBadInput  -ne 0) 'Test4: -InputLogFolder outside .local exits nonzero'
Assert-True ($t4ExitBadOutput -ne 0) 'Test4: -Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
