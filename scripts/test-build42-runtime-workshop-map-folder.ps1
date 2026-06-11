#Requires -Version 5.1
<#
.SYNOPSIS
    Tests the MAP-9C Build 42 runtime Workshop map folder inventory inspector.
    Verifies .local guard, 3740642200 path guard, read-only behaviour, and output
    schema against a synthetic test fixture.
    Asserts 25 contract requirements.
    Exits 0 if all pass, exits 1 if any fail.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot      = Split-Path -Parent $scriptDir
$inspectScript = Join-Path $repoRoot 'scripts\inspect-build42-runtime-workshop-map-folder.ps1'

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
$outside = Join-Path $repoRoot 'scripts\map9c-wsinv-guard-test'
$fakeWS  = Join-Path $repoRoot '.local\map9c-ws-fake-3740642200'
New-Item -ItemType Directory -Force -Path $fakeWS | Out-Null
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $inspectScript `
    -WorkshopItemPath $fakeWS -Output $outside 2>$null
$ecGuard = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ecGuard -ne 0) "Inspector refuses output outside .local/"

# ---------------------------------------------------------------------------
# Test 2: refuses WorkshopItemPath not containing 3740642200
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Test 2: Refuses path not containing 3740642200 ---"
$wrongWS = Join-Path $repoRoot '.local\map9c-ws-wrong-id-99999'
New-Item -ItemType Directory -Force -Path $wrongWS | Out-Null
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& powershell -ExecutionPolicy Bypass -File $inspectScript `
    -WorkshopItemPath $wrongWS -Output (Join-Path $repoRoot '.local\map9c-ws-out-test') 2>$null
$ecWrong = $LASTEXITCODE
$ErrorActionPreference = $savedEAP
Assert-True ($ecWrong -ne 0) "Inspector refuses WorkshopItemPath not containing 3740642200"
if (Test-Path $wrongWS) { Remove-Item -Recurse -Force $wrongWS }

# ---------------------------------------------------------------------------
# Test 3: Script exists
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Test 3: Script exists ---"
Assert-True (Test-Path $inspectScript -PathType Leaf) "inspect-build42-runtime-workshop-map-folder.ps1 exists"

# ---------------------------------------------------------------------------
# Tests 4-6: Valid run with synthetic fixture
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 4-6: Valid run with synthetic fixture ---"

# Build a minimal synthetic Workshop fixture
$fixtureWS  = Join-Path $repoRoot '.local\map9c-ws-fixture-3740642200'
if (Test-Path $fixtureWS) { Remove-Item -Recurse -Force $fixtureWS }
$mapDir = Join-Path $fixtureWS 'common\media\maps\PZMapForge'
New-Item -ItemType Directory -Force -Path $mapDir | Out-Null
"ModID=PZMapForge`nName=PZMapForge Test`nDescription=test" | Set-Content (Join-Path $fixtureWS 'common\mod.info')
"id=PZMapForge`nlots=PZMapForge" | Set-Content (Join-Path $mapDir 'map.info')
"-- spawnpoints" | Set-Content (Join-Path $mapDir 'spawnpoints.lua')
"-- spawnregions" | Set-Content (Join-Path $mapDir 'spawnregions.lua')
"-- objects" | Set-Content (Join-Path $mapDir 'objects.lua')
[System.IO.File]::WriteAllBytes((Join-Path $mapDir '35_27.lotheader'), (New-Object byte[] 16))
[System.IO.File]::WriteAllBytes((Join-Path $mapDir 'chunkdata_35_27.bin'), (New-Object byte[] 16))

$testOutput = Join-Path $repoRoot '.local\map9c-ws-inv-test'
if (Test-Path $testOutput) { Remove-Item -Recurse -Force $testOutput }
& powershell -ExecutionPolicy Bypass -File $inspectScript `
    -WorkshopItemPath $fixtureWS -Output $testOutput
Assert-True ($LASTEXITCODE -eq 0) "Inspector exits 0 with valid fixture"

$jsonPath = Join-Path $testOutput 'runtime-workshop-map-folder-inventory.json'
$mdPath   = Join-Path $testOutput 'runtime-workshop-map-folder-inventory.md'
Assert-True (Test-Path $jsonPath -PathType Leaf) "runtime-workshop-map-folder-inventory.json exists"
Assert-True (Test-Path $mdPath   -PathType Leaf) "runtime-workshop-map-folder-inventory.md exists"

# ---------------------------------------------------------------------------
# Tests 7-14: Safety fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 7-14: Safety fields ---"
$p = Get-Content $jsonPath -Raw | ConvertFrom-Json

Assert-True ($p.schema -eq 'pzmapforge.map9c-runtime-workshop-map-folder-inventory.v0.1') `
    "schema == 'pzmapforge.map9c-runtime-workshop-map-folder-inventory.v0.1'"
Assert-True ($p.workshop_item_id -eq '3740642200') "workshop_item_id == '3740642200'"
Assert-True ($p.read_only -eq $true) "read_only == true"
Assert-True ($p.copied_files -eq $false) "copied_files == false"
Assert-True ($p.binary_contents_dumped -eq $false) "binary_contents_dumped == false"
Assert-True ($p.steam_write_performed -eq $false) "steam_write_performed == false"
Assert-True ($p.pz_run_performed -eq $false) "pz_run_performed == false"
Assert-True ($p.workshop_upload_performed -eq $false) "workshop_upload_performed == false"

# ---------------------------------------------------------------------------
# Tests 15-20: Inventory content fields
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 15-20: Inventory content fields ---"
Assert-True ($null -ne $p.PSObject.Properties['workshop_item_path']) "workshop_item_path field present"
Assert-True ($null -ne $p.PSObject.Properties['candidate_mod_folder_found']) "candidate_mod_folder_found field present"
Assert-True ($null -ne $p.PSObject.Properties['candidate_common_media_maps_found']) "candidate_common_media_maps_found field present"
Assert-True ($null -ne $p.PSObject.Properties['candidate_lotheader_files']) "candidate_lotheader_files field present"
Assert-True ($null -ne $p.PSObject.Properties['candidate_chunkdata_files']) "candidate_chunkdata_files field present"
Assert-True ($null -ne $p.PSObject.Properties['candidate_worldmap_files']) "candidate_worldmap_files field present"

# ---------------------------------------------------------------------------
# Tests 21-25: Fixture-specific values and summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "--- Tests 21-25: Fixture-specific values ---"
Assert-True ($p.candidate_common_media_maps_found -eq $true) "candidate_common_media_maps_found == true (fixture)"
Assert-True ($p.candidate_mod_folder_found -eq $true) "candidate_mod_folder_found == true (fixture)"
Assert-True ($null -ne $p.PSObject.Properties['map_folder_inventory_summary']) "map_folder_inventory_summary field present"
Assert-True ($null -ne $p.PSObject.Properties['registration_risk_summary']) "registration_risk_summary field present"
Assert-True ($p.candidate_lotheader_files.Count -ge 1) "lotheader file inventoried in fixture"

# Cleanup
if (Test-Path $fixtureWS) { Remove-Item -Recurse -Force $fixtureWS }
if (Test-Path $fakeWS)    { Remove-Item -Recurse -Force $fakeWS }

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "----------------------------------------"
Write-Output "Results: $pass passed, $fail failed"
Write-Output "----------------------------------------"

if ($fail -gt 0) { exit 1 }
exit 0
