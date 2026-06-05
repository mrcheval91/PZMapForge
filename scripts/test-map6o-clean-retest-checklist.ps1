#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for prepare-map6o-clean-retest-checklist.ps1 (MAP-6O).

    Creates a synthetic MAP-6L-like candidate fixture under temp .local dirs
    and validates checklist output. Does not copy files to PZ folders.
    Expected assertion count: 15
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir       = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot        = Split-Path -Parent $scriptDir
$checklistScript = Join-Path $repoRoot 'scripts\prepare-map6o-clean-retest-checklist.ps1'
$tempRoot        = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Checklist {
    param([string]$CandidateSource, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $checklistScript `
        -CandidateSource $CandidateSource `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-map6o-clean-retest-checklist.ps1'
Write-Output ''

$CandidateId = 'pzmapforge_build42_candidate_001'

# ---------------------------------------------------------------------------
# Setup: synthetic candidate fixture under a .local dir in temp
# ---------------------------------------------------------------------------

$testBase    = Join-Path $tempRoot ('pzmf-t6o-' + [System.IO.Path]::GetRandomFileName())
$goodSrc     = Join-Path $testBase ".local\candidate\${CandidateId}_build42_candidate"
$goodOut     = Join-Path $testBase '.local\map6o-output'
$badPath     = Join-Path $tempRoot 'pzmf-t6o-bad-path-no-local'
$v42Dir      = Join-Path $goodSrc '42'
$mapDataDir  = Join-Path $v42Dir "media\maps\$CandidateId"

New-Item -ItemType Directory -Force -Path $mapDataDir | Out-Null
New-Item -ItemType Directory -Force -Path $badPath    | Out-Null

# Required candidate files
Set-Content -Path (Join-Path $v42Dir     'mod.info')           -Value "id=$CandidateId`nname=Test" -Encoding UTF8
Set-Content -Path (Join-Path $mapDataDir 'map.info')           -Value 'title=Test'                  -Encoding UTF8
Set-Content -Path (Join-Path $mapDataDir 'spawnpoints.lua')    -Value 'function SpawnPoints() return {} end' -Encoding UTF8
Set-Content -Path (Join-Path $mapDataDir 'objects.lua')        -Value 'return {}'                   -Encoding UTF8
[System.IO.File]::WriteAllBytes((Join-Path $mapDataDir '0_0.lotheader'),        [byte[]]::new(38))
[System.IO.File]::WriteAllBytes((Join-Path $mapDataDir 'world_0_0.lotpack'),    [byte[]]::new(1056780))
[System.IO.File]::WriteAllBytes((Join-Path $mapDataDir 'chunkdata_0_0.bin'),    [byte[]]::new(1026))

# ---------------------------------------------------------------------------
# Test 1: CandidateSource outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: CandidateSource outside .local refused ---'
$t1Exit = Invoke-Checklist -CandidateSource $badPath -Output $goodOut
Assert-True ($t1Exit -ne 0) 'Test1: CandidateSource outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Output outside .local refused ---'
$t2Exit = Invoke-Checklist -CandidateSource $goodSrc -Output $badPath
Assert-True ($t2Exit -ne 0) 'Test2: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 3-10: Valid source produces all output files with correct content
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 3-10: Valid source run ---'
$t3Exit = Invoke-Checklist -CandidateSource $goodSrc -Output $goodOut

$checklistMd   = Join-Path $goodOut 'MAP_6O_CLEAN_RETEST_CHECKLIST.md'
$recordMd      = Join-Path $goodOut 'MAP_6O_CLEAN_RETEST_RECORD.local-template.md'
$triageCmdsMd  = Join-Path $goodOut 'MAP_6O_CLEAN_RETEST_TRIAGE_COMMANDS.md'

Assert-True ($t3Exit -eq 0)                       'Test3: script exits 0 on valid source'
Assert-True (Test-Path $checklistMd)              'Test4: checklist MD exists'
Assert-True (Test-Path $recordMd)                 'Test5: record template exists'
Assert-True (Test-Path $triageCmdsMd)             'Test6: triage commands MD exists'

$clContent  = if (Test-Path $checklistMd)  { Get-Content $checklistMd  -Raw } else { '' }
$recContent = if (Test-Path $recordMd)     { Get-Content $recordMd     -Raw } else { '' }

Assert-True ($clContent  -match 'HUMAN_ONLY_COPY_REQUIRED')       'Test7: checklist contains HUMAN_ONLY_COPY_REQUIRED'
Assert-True ($clContent  -match 'LOAD_TEST_NOT_PERFORMED')        'Test8: checklist contains LOAD_TEST_NOT_PERFORMED'
Assert-True ($clContent  -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'Test9: checklist contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'
Assert-True ($clContent  -match [regex]::Escape($CandidateId))    'Test10: checklist contains candidate ID'

# ---------------------------------------------------------------------------
# Test 11: Checklist does not contain an executable automatic Copy-Item into Zomboid mods
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 11: no automatic copy to Zomboid mods ---'
$hasAutoCopy = ($clContent -match 'Copy-Item') -and ($clContent -notmatch 'HUMAN-ONLY')
Assert-True (-not $hasAutoCopy) 'Test11: no automatic Copy-Item to Zomboid mods (all copy lines are HUMAN-ONLY)'

# ---------------------------------------------------------------------------
# Test 12: Record template contains LOAD_TEST_INCONCLUSIVE
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 12: record template contains LOAD_TEST_INCONCLUSIVE ---'
Assert-True ($recContent -match 'LOAD_TEST_INCONCLUSIVE') 'Test12: record template contains LOAD_TEST_INCONCLUSIVE'

# ---------------------------------------------------------------------------
# Test 13: No non-ASCII bytes in checklist or record output
# (Mojibake from UTF-8 em-dash would produce bytes > 127 in output)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 13: output files are ASCII-clean ---'
$clHasNonAscii  = (@([System.IO.File]::ReadAllBytes($checklistMd)  | Where-Object { $_ -gt 127 })).Count -gt 0
$recHasNonAscii = (@([System.IO.File]::ReadAllBytes($recordMd)     | Where-Object { $_ -gt 127 })).Count -gt 0
Assert-True (-not $clHasNonAscii)  'Test13: checklist output has no non-ASCII bytes (no mojibake)'
Assert-True (-not $recHasNonAscii) 'Test13: record template has no non-ASCII bytes (no mojibake)'

# ---------------------------------------------------------------------------
# Test 14: Checklist contains a properly-formed fenced text block (` ``` `text)
# Single-quoted regex: backtick is NOT an escape char in PS single-quoted strings.
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 14: checklist has proper fenced text block ---'
$hasFenceText = $clContent -match '(?m)^```text'
Assert-True $hasFenceText 'Test14: checklist contains properly-formed fenced text block'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
