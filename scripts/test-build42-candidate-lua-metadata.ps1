#Requires -Version 5.1
<#
.SYNOPSIS
    Tests for inspect-build42-candidate-lua-metadata.ps1 (MAP-7B).

    Creates synthetic candidate fixtures under .local/ and validates the inspector output.
    Does NOT copy files to PZ folders. Does NOT read PZ install files.
    Expected assertion count: 15
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Split-Path -Parent $scriptDir
$modScript  = Join-Path $repoRoot 'scripts\inspect-build42-candidate-lua-metadata.ps1'
$tempRoot   = [System.IO.Path]::GetTempPath()

$pass = 0
$fail = 0

function Assert-True {
    param([bool]$Condition, [string]$Label)
    if ($Condition) { Write-Output "  PASS  $Label"; $script:pass++ }
    else            { Write-Output "  FAIL  $Label"; $script:fail++ }
}

function Invoke-Mod {
    param([string]$CandidateRoot, [string]$Output)
    & powershell -ExecutionPolicy Bypass -File $modScript `
        -CandidateRoot $CandidateRoot `
        -Output $Output | Out-Null
    return [int]$LASTEXITCODE
}

Write-Output 'test-build42-candidate-lua-metadata.ps1'
Write-Output ''

# ---------------------------------------------------------------------------
# Synthetic fixture: candidate 42/ directory with all metadata files
# ---------------------------------------------------------------------------

$testBase    = Join-Path $tempRoot ('pzmf-t7b-' + [System.IO.Path]::GetRandomFileName())
$candBase    = Join-Path $testBase '.local\candidate\42'
$mapDataBase = Join-Path $candBase 'media\maps\test_map_7b'
$outDir      = Join-Path $testBase '.local\output'
$badPath     = Join-Path $tempRoot 'pzmf-t7b-bad-no-local'

New-Item -ItemType Directory -Force -Path $mapDataBase | Out-Null
New-Item -ItemType Directory -Force -Path $badPath     | Out-Null

# Write synthetic candidate files (ASCII to avoid UTF-8 BOM from PS Set-Content)
Set-Content -Path (Join-Path $candBase 'mod.info') -Value "name=Test Map`nid=test_map_7b`ncategory=map`n" -Encoding ASCII
Set-Content -Path (Join-Path $mapDataBase 'map.info') -Value "lots=test_map_7b`ntitle=Test`n" -Encoding ASCII
# v3-style spawnpoints.lua: unemployed key with full coordinates
Set-Content -Path (Join-Path $mapDataBase 'spawnpoints.lua') -Value "function SpawnPoints()`nreturn { unemployed = { { worldX = 0, worldY = 0, posX = 150, posY = 150, posZ = 0 } } }`nend`n" -Encoding ASCII
Set-Content -Path (Join-Path $mapDataBase 'objects.lua') -Value "return {}`n" -Encoding ASCII

# ---------------------------------------------------------------------------
# Test 1: CandidateRoot outside .local refused
# ---------------------------------------------------------------------------

Write-Output '--- Test 1: CandidateRoot outside .local refused ---'
$t1Exit = Invoke-Mod -CandidateRoot $badPath -Output $outDir
Assert-True ($t1Exit -ne 0) 'Test1: CandidateRoot outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Test 2: Output outside .local refused
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 2: Output outside .local refused ---'
$t2Exit = Invoke-Mod -CandidateRoot $candBase -Output $badPath
Assert-True ($t2Exit -ne 0) 'Test2: Output outside .local exits nonzero'

# ---------------------------------------------------------------------------
# Tests 3-15: Valid run
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Tests 3-15: valid run ---'
$t3Exit = Invoke-Mod -CandidateRoot $candBase -Output $outDir

$jsonFile = Join-Path $outDir 'build42-candidate-lua-metadata.json'
$mdFile   = Join-Path $outDir 'build42-candidate-lua-metadata.md'

Assert-True ($t3Exit -eq 0)       'Test3: script exits 0'
Assert-True (Test-Path $jsonFile) 'Test4: JSON report exists'
Assert-True (Test-Path $mdFile)   'Test5: MD report exists'

$data      = if (Test-Path $jsonFile) { Get-Content $jsonFile -Raw | ConvertFrom-Json } else { $null }
$mdContent = if (Test-Path $mdFile)   { Get-Content $mdFile   -Raw } else { '' }

# Test 6: mod_info_exists == true
$mie = if ($null -ne $data) { [bool]$data.mod_info_exists } else { $false }
Assert-True ($mie -eq $true) "Test6: mod_info_exists == true (got $mie)"

# Test 7: map_info_exists == true
$maie = if ($null -ne $data) { [bool]$data.map_info_exists } else { $false }
Assert-True ($maie -eq $true) "Test7: map_info_exists == true (got $maie)"

# Test 8: spawnpoints_lua_exists == true
$spe = if ($null -ne $data) { [bool]$data.spawnpoints_lua_exists } else { $false }
Assert-True ($spe -eq $true) "Test8: spawnpoints_lua_exists == true (got $spe)"

# Test 9: objects_lua_exists == true
$ole = if ($null -ne $data) { [bool]$data.objects_lua_exists } else { $false }
Assert-True ($ole -eq $true) "Test9: objects_lua_exists == true (got $ole)"

# Test 10: objects_lua_content_type == 'return_only'
$olct = if ($null -ne $data) { [string]$data.objects_lua_content_type } else { '' }
Assert-True ($olct -eq 'return_only') "Test10: objects_lua_content_type == return_only (got '$olct')"

# Test 11: spawnpoints_lua_compatible_shape == true
$spcs = if ($null -ne $data) { [bool]$data.spawnpoints_lua_compatible_shape } else { $false }
Assert-True ($spcs -eq $true) "Test11: spawnpoints_lua_compatible_shape == true (got $spcs)"

# Test 12: mod_info_id_matches == true
$miim = if ($null -ne $data) { [bool]$data.mod_info_id_matches } else { $false }
Assert-True ($miim -eq $true) "Test12: mod_info_id_matches == true (got $miim)"

# Test 13: map_info_lots_matches == true
$mailm = if ($null -ne $data) { [bool]$data.map_info_lots_matches } else { $false }
Assert-True ($mailm -eq $true) "Test13: map_info_lots_matches == true (got $mailm)"

# Test 14: MD contains OBJECTS_LUA_PRIMARY_BLOCKER
Assert-True ($mdContent -match 'OBJECTS_LUA_PRIMARY_BLOCKER') `
    'Test14: MD contains OBJECTS_LUA_PRIMARY_BLOCKER'

# Test 15: MD contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false
Assert-True ($mdContent -match 'PLAYABLE_EXPORT_CLAIM_ALLOWED=false') `
    'Test15: MD contains PLAYABLE_EXPORT_CLAIM_ALLOWED=false'

# ---------------------------------------------------------------------------
# Tests 16-18: Extended inspector capabilities (MAP-7C additions)
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '--- Test 16: comment_only detection ---'
# Create a second fixture with comment-only objects.lua
$cand2Base    = Join-Path $testBase '.local\cand2\42'
$mapData2     = Join-Path $cand2Base 'media\maps\test_7b_2'
$outDir2      = Join-Path $testBase '.local\output2'
New-Item -ItemType Directory -Force -Path $mapData2 | Out-Null
Set-Content -Path (Join-Path $cand2Base 'mod.info') -Value "id=test_7b_2`n" -Encoding ASCII
Set-Content -Path (Join-Path $mapData2 'map.info') -Value "lots=test_7b_2`n" -Encoding ASCII
Set-Content -Path (Join-Path $mapData2 'spawnpoints.lua') -Value "function SpawnPoints()`nreturn { all = {} }`nend`n" -Encoding ASCII
Set-Content -Path (Join-Path $mapData2 'objects.lua') -Value "-- MAP-7C comment only placeholder`n" -Encoding ASCII

$t16Exit = Invoke-Mod -CandidateRoot $cand2Base -Output $outDir2
$data2 = if (Test-Path (Join-Path $outDir2 'build42-candidate-lua-metadata.json')) {
    Get-Content (Join-Path $outDir2 'build42-candidate-lua-metadata.json') -Raw | ConvertFrom-Json
} else { $null }
$olct2 = if ($null -ne $data2) { [string]$data2.objects_lua_content_type } else { '' }
Assert-True ($olct2 -eq 'comment_only') "Test16: comment-only fixture → content_type == comment_only (got '$olct2')"

Write-Output ''
Write-Output '--- Test 17: return_only recommendation is risky ---'
$olrec = if ($null -ne $data) { [string]$data.objects_lua_recommendation } else { '' }
Assert-True ($olrec -match 'risky') "Test17: return_only → recommendation contains risky (got '$olrec')"

Write-Output ''
Write-Output '--- Test 18: spawnpoints_lua_has_unemployed detected ---'
$spUnemp = if ($null -ne $data) { [bool]$data.spawnpoints_lua_has_unemployed } else { $false }
Assert-True ($spUnemp -eq $true) "Test18: spawnpoints_lua_has_unemployed == true (got $spUnemp)"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ''
Write-Output '----------------------------------------'
Write-Output "Results: $pass passed, $fail failed"
Write-Output '----------------------------------------'

if ($fail -gt 0) { exit 1 }
exit 0
