#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for prepare-map6p-spawn-activation-diagnostic.ps1 (MAP-6P).

    Validates diagnostic commands and record template output. Does not copy
    files to PZ folders. Does not require a real PZ install.
    Expected assertion count: 12
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$diagScript = Join-Path $repoRoot 'scripts\prepare-map6p-spawn-activation-diagnostic.ps1'
$tempRoot   = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Diag {
    param([string]$Output)
    & powershell -ExecutionPolicy Bypass -File $diagScript -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-map6p-spawn-activation-diagnostic.ps1'
Write-Output ''

$MapId         = 'pzmapforge_build42_candidate_001'
$ModFolderName = 'pzmapforge_build42_candidate_001_test_clean'
$ServerName    = 'PZMF_B42_CANDIDATE_CLEAN_001'

$testBase  = Join-Path $tempRoot ('pzmf-t6p-' + [System.IO.Path]::GetRandomFileName())
$goodOut   = Join-Path $testBase '.local\map6p-output'
$badPath   = Join-Path $tempRoot 'pzmf-t6p-bad-path-no-local'

New-Item -ItemType Directory -Force -Path $badPath | Out-Null

# ---------------------------------------------------------------------------
# Test 1: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: Output outside .local refused ---'
$t1Exit = Invoke-Diag -Output $badPath
Assert-True ($t1Exit -ne 0) 'Test1: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 2-12: Valid output exits 0, all files exist, correct content
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 2-12: valid output run ---'
$t2Exit = Invoke-Diag -Output $goodOut

$cmdsMd   = Join-Path $goodOut 'MAP_6P_SPAWN_ACTIVATION_DIAGNOSTIC_COMMANDS.md'
$recordMd = Join-Path $goodOut 'MAP_6P_SPAWN_ACTIVATION_RECORD.local-template.md'

Assert-True ($t2Exit -eq 0)      'Test2: script exits 0 on valid output'
Assert-True (Test-Path $cmdsMd)  'Test3: diagnostic commands file exists'
Assert-True (Test-Path $recordMd)'Test4: record template exists'

$cmdsContent   = if (Test-Path $cmdsMd)   { Get-Content $cmdsMd   -Raw } else { '' }
$recordContent = if (Test-Path $recordMd) { Get-Content $recordMd -Raw } else { '' }

Assert-True ($cmdsContent   -match [regex]::Escape($MapId))         'Test5: commands include map id'
Assert-True ($cmdsContent   -match [regex]::Escape($ModFolderName)) 'Test6: commands include mod folder name'
Assert-True ($cmdsContent   -match [regex]::Escape($ServerName))    'Test7: commands include server name'
Assert-True ($cmdsContent   -match 'Mods=')                         'Test8: commands include Mods='
Assert-True ($cmdsContent   -match 'Map=')                          'Test9: commands include Map='
Assert-True ($cmdsContent   -match 'spawnregions.lua')              'Test10: commands include spawnregions.lua'
Assert-True ($recordContent -match 'CANDIDATE_SPAWN_REGION_NOT_VISIBLE') 'Test11: record template contains CANDIDATE_SPAWN_REGION_NOT_VISIBLE'
Assert-True ($recordContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') 'Test12: record template contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
