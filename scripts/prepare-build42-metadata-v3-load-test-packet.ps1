#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7C: Generates the empty_grass_v3 candidate and produces a human-ready
    load-test packet with fixed Lua metadata.

    -Output must be under .local/.
    Does NOT copy any files to PZ folders.
    Does NOT write outside .local/.

    Steps:
    1. Generates empty_grass_v3 candidate under Output/candidate/.
    2. Runs inspector on generated 42/ directory.
    3. Runs preflight verifying LOTH v3 + Lua metadata properties.
    4. Writes packet docs, record template, wiring commands, preflight JSON/MD.

.PARAMETER Output
    Path under .local/ for packet output.

.PARAMETER MapId
    Default: pzmapforge_build42_candidate_v3_001

.PARAMETER ModFolderName
    Default: pzmapforge_build42_candidate_v3_001_test

.PARAMETER ServerName
    Default: PZMF_B42_METADATA_V3_TEST_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\prepare-build42-metadata-v3-load-test-packet.ps1 `
        -Output .local\map7c-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v3_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v3_001_test',
    [string]$ServerName    = 'PZMF_B42_METADATA_V3_TEST_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

function Assert-LocalPath {
    param([string]$Path, [string]$Label)
    $norm = $Path.Replace('/', '\')
    if ($norm -notmatch '\\\.local(\\|$)') {
        Write-Error "$Label must be under .local/. Got: $Path"
        exit 1
    }
}

Assert-LocalPath $Output '-Output'

New-Item -ItemType Directory -Force -Path $Output | Out-Null

# ---------------------------------------------------------------------------
# Step 1: Generate empty_grass_v3 candidate
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v3 candidate..."

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $MapId `
    --output $candidateOut `
    --build42-candidate-writer `
    --build42-candidate-profile empty_grass_v3

if ($LASTEXITCODE -ne 0) {
    Write-Error "CLI candidate generation failed (exit $LASTEXITCODE)"
    exit 1
}

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

$candDir    = Join-Path $candidateOut ($MapId + '_build42_candidate')
$v42Dir     = Join-Path $candDir '42'
$mapDataDir = Join-Path $v42Dir "media\maps\$MapId"
$reportJson = Join-Path $v42Dir 'experimental-map-export-report.json'

$modInfoPath   = Join-Path $v42Dir     'mod.info'
$mapInfoPath   = Join-Path $mapDataDir 'map.info'
$spawnPtsPath  = Join-Path $mapDataDir 'spawnpoints.lua'
$objectsPath   = Join-Path $mapDataDir 'objects.lua'
$lotheaderPath = Join-Path $mapDataDir '0_0.lotheader'
$lotpackPath   = Join-Path $mapDataDir 'world_0_0.lotpack'
$chunkdataPath = Join-Path $mapDataDir 'chunkdata_0_0.bin'

# ---------------------------------------------------------------------------
# Step 2: Run Lua metadata inspector
# ---------------------------------------------------------------------------

$luaMetaOutDir = Join-Path $Output 'lua-metadata'
$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-candidate-lua-metadata.ps1'

Write-Output "Running Lua metadata inspector..."
& powershell -ExecutionPolicy Bypass -File $inspectorScript `
    -CandidateRoot $v42Dir `
    -Output $luaMetaOutDir | Out-Null

$luaMetaJson = Join-Path $luaMetaOutDir 'build42-candidate-lua-metadata.json'
$luaMeta = $null
if (Test-Path $luaMetaJson) {
    $luaMeta = Get-Content $luaMetaJson -Raw | ConvertFrom-Json
}

# ---------------------------------------------------------------------------
# Step 3: Preflight
# ---------------------------------------------------------------------------

Write-Output "Running preflight checks..."

$checks  = [System.Collections.Generic.List[object]]::new()
$allPass = $true

function Add-Check {
    param([string]$Name, [bool]$Pass, [string]$Detail)
    $r = if ($Pass) { 'PASS' } else { 'FAIL' }
    $checks.Add([ordered]@{ name = $Name; result = $r; detail = $Detail }) | Out-Null
    if (-not $Pass) { $script:allPass = $false }
    Write-Output "  [$r] $Name -- $Detail"
}

function Find-TrailerStart ([byte[]]$bytes) {
    $i = 12
    while ($i -lt $bytes.Length) {
        $b = $bytes[$i]
        if ($b -eq 0x0A -or ($b -ge 0x20 -and $b -le 0x7E)) { $i++ }
        else { break }
    }
    return $i
}

# File existence
Add-Check 'mod_info_exists'        (Test-Path $modInfoPath)    $modInfoPath
Add-Check 'map_info_exists'        (Test-Path $mapInfoPath)    $mapInfoPath
Add-Check 'spawnpoints_lua_exists' (Test-Path $spawnPtsPath)   $spawnPtsPath
Add-Check 'objects_lua_exists'     (Test-Path $objectsPath)    $objectsPath
Add-Check 'lotheader_exists'       (Test-Path $lotheaderPath)  $lotheaderPath
Add-Check 'lotpack_exists'         (Test-Path $lotpackPath)    $lotpackPath
Add-Check 'chunkdata_exists'       (Test-Path $chunkdataPath)  $chunkdataPath

# LOTH binary checks (same as MAP-7A)
$trailerSha256 = ''
if (Test-Path $lotheaderPath) {
    $loth = [System.IO.File]::ReadAllBytes($lotheaderPath)

    $magic = if ($loth.Length -ge 4) { [System.Text.Encoding]::ASCII.GetString($loth[0..3]) } else { '' }
    $ver   = if ($loth.Length -ge 8) { [BitConverter]::ToUInt32($loth, 4) } else { 0 }
    $cnt   = if ($loth.Length -ge 12) { [BitConverter]::ToUInt32($loth, 8) } else { 0 }

    Add-Check 'loth_magic_is_LOTH'    ($magic -eq 'LOTH')  "magic=$magic"
    Add-Check 'loth_version_is_1'     ($ver   -eq 1)       "version=$ver"
    Add-Check 'loth_entry_count_1024' ($cnt   -eq 1024)    "entry_count=$cnt"

    $trailerStart = Find-TrailerStart $loth
    $trailerSize  = $loth.Length - $trailerStart
    Add-Check 'loth_trailer_size_1048' ($trailerSize -eq 1048) "trailer_size=$trailerSize"

    $canonSha = '93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7'
    if ($trailerSize -ge 1048) {
        $sha256obj    = [System.Security.Cryptography.SHA256]::Create()
        $trailerBytes = [byte[]]($loth[$trailerStart..($trailerStart + 1047)])
        $hashBytes    = $sha256obj.ComputeHash($trailerBytes)
        $sha256obj.Dispose()
        $trailerSha256 = ($hashBytes | ForEach-Object { $_.ToString('x2') }) -join ''
        Add-Check 'loth_trailer_sha256_canonical' ($trailerSha256 -eq $canonSha) "sha256=$trailerSha256"
    } else {
        Add-Check 'loth_trailer_sha256_canonical' $false "trailer too small"
    }
    Add-Check 'loth_total_size_29646' ($loth.Length -eq 29646) "size=$($loth.Length)"
} else {
    foreach ($n in @('loth_magic_is_LOTH','loth_version_is_1','loth_entry_count_1024',
                     'loth_trailer_size_1048','loth_trailer_sha256_canonical','loth_total_size_29646')) {
        Add-Check $n $false 'lotheader missing'
    }
}

# LOTP size
if (Test-Path $lotpackPath) {
    $lotpSize = (Get-Item $lotpackPath).Length
    Add-Check 'lotp_size_1056780' ($lotpSize -eq 1056780) "size=$lotpSize"
} else { Add-Check 'lotp_size_1056780' $false 'lotpack missing' }

# chunkdata size
if (Test-Path $chunkdataPath) {
    $cdSize = (Get-Item $chunkdataPath).Length
    Add-Check 'chunkdata_size_1026' ($cdSize -eq 1026) "size=$cdSize"
} else { Add-Check 'chunkdata_size_1026' $false 'chunkdata missing' }

# Lua metadata checks from inspector
$objType   = if ($null -ne $luaMeta) { [string]$luaMeta.objects_lua_content_type } else { 'unknown' }
$spawnComp = if ($null -ne $luaMeta) { [bool]$luaMeta.spawnpoints_lua_compatible_shape } else { $false }
$hasUnemp  = if ($null -ne $luaMeta) { [bool]$luaMeta.spawnpoints_lua_has_unemployed } else { $false }

Add-Check 'objects_lua_not_return_only' ($objType -ne 'return_only') "objects_type=$objType"
Add-Check 'objects_lua_is_comment_only_or_safe' ($objType -eq 'comment_only' -or $objType -eq 'empty' -or $objType -eq 'missing') "objects_type=$objType"
Add-Check 'spawnpoints_compatible_shape' ($spawnComp -eq $true) "compatible=$spawnComp"
Add-Check 'spawnpoints_has_unemployed'   ($hasUnemp  -eq $true) "has_unemployed=$hasUnemp"

# Report safety flags
if (Test-Path $reportJson) {
    $rep = Get-Content $reportJson -Raw | ConvertFrom-Json
    Add-Check 'report_profile_empty_grass_v3'          ($rep.build42_candidate_profile -eq 'empty_grass_v3') 'profile=empty_grass_v3'
    Add-Check 'report_load_tested_false'               ($rep.load_tested -eq $false)                          'load_tested=false'
    Add-Check 'report_playable_export_generated_false' ($rep.playable_export_generated -eq $false)             'playable_export_generated=false'
    Add-Check 'report_playable_export_claimed_false'   ($rep.playable_export_claimed -eq $false)               'playable_export_claimed=false'
    Add-Check 'report_pz_assets_copied_false'          ($rep.pz_assets_copied -eq $false)                     'pz_assets_copied=false'
    Add-Check 'report_pz_assets_read_false'            ($rep.pz_assets_read -eq $false)                       'pz_assets_read=false'
} else {
    foreach ($n in @('report_profile_empty_grass_v3','report_load_tested_false',
                     'report_playable_export_generated_false','report_playable_export_claimed_false',
                     'report_pz_assets_copied_false','report_pz_assets_read_false')) {
        Add-Check $n $false 'report.json missing'
    }
}

$passCount = (@($checks | Where-Object { $_.result -eq 'PASS' })).Count
$failCount = (@($checks | Where-Object { $_.result -eq 'FAIL' })).Count
Write-Output ""
Write-Output "Preflight: $passCount PASS / $failCount FAIL"

if (-not $allPass) {
    Write-Error "Preflight failed ($failCount checks). Packet not written."
    exit 1
}

# ---------------------------------------------------------------------------
# Write preflight JSON
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema                      = 'pzmapforge.map7c-preflight.v0.1'
    map_id                      = $MapId
    mod_folder_name             = $ModFolderName
    server_name                 = $ServerName
    candidate_profile           = 'empty_grass_v3'
    all_pass                    = $allPass
    entry_count                 = 1024
    loth_size                   = 29646
    loth_trailer_size           = 1048
    loth_trailer_sha256         = $trailerSha256
    lotp_size                   = 1056780
    chunkdata_size              = 1026
    objects_lua_content_type    = $objType
    objects_lua_not_return_only = ($objType -ne 'return_only')
    spawnpoints_has_unemployed  = $hasUnemp
    checks                      = [object[]]$checks.ToArray()
    load_test_not_performed     = $true
    pz_assets_copied            = $false
    playable_export_claimed     = $false
    writer_not_changed          = $false
}

$fence = '```'
$preflightJsonPath = Join-Path $Output 'map7c-preflight.json'
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJsonPath -Encoding UTF8
Write-Output "Preflight JSON: $preflightJsonPath"

# ---------------------------------------------------------------------------
# Write preflight MD
# ---------------------------------------------------------------------------

$preflightMdPath = Join-Path $Output 'map7c-preflight.md'
$checkTable = ($checks | ForEach-Object { "| $($_.name) | $($_.result) | $($_.detail) |" }) -join "`n"
$preflightMd = @"
# MAP-7C Preflight Report

${fence}text
candidate_profile: empty_grass_v3
map_id: $MapId
objects_lua_content_type: $objType
objects_lua_not_return_only: $($objType -ne 'return_only')
spawnpoints_has_unemployed: $hasUnemp
loth_size: 29646
lotp_size: 1056780
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Checks ($passCount PASS / $failCount FAIL)

| Check | Result | Detail |
|---|---|---|
$checkTable
"@
Set-Content -Path $preflightMdPath -Value $preflightMd -Encoding ASCII

# Destination paths (for docs only)
$destBase   = "C:\Users\Palmacede\Zomboid\mods\$ModFolderName\42"
$serverDir  = 'C:\Users\Palmacede\Zomboid\Server'
$iniPath    = "$serverDir\$ServerName.ini"
$srPath     = "$serverDir\${ServerName}_spawnregions.lua"
$hostIni    = 'C:\Users\Palmacede\Zomboid\Lua\host.ini'
$consoleTxt = 'C:\Users\Palmacede\Zomboid\console.txt'

# ---------------------------------------------------------------------------
# Write MAP_7C_LOAD_TEST_PACKET.md
# ---------------------------------------------------------------------------

$packetMdPath = Join-Path $Output 'MAP_7C_LOAD_TEST_PACKET.md'
$packetMd = @"
# MAP-7C Build 42 Candidate Lua Metadata v3 Load Test Packet

${fence}text
Profile:       empty_grass_v3
MapId:         $MapId
ModFolder:     $ModFolderName
ServerName:    $ServerName
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
OBJECTS_LUA_FIXED_COMMENT_ONLY
SPAWNPOINTS_LUA_UNEMPLOYED_KEY
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## Preflight status

All $passCount preflight checks PASS.

- Profile:                empty_grass_v3 (MAP-7C)
- LOTH size:              29646 bytes (unchanged from v2/MAP-6Z)
- LOTH trailer size:      1048 bytes (MAP-6Y canonical stable block)
- LOTH trailer SHA256:    $trailerSha256
- objects.lua type:       $objType (not return_only)
- spawnpoints unemployed: $hasUnemp
- LOTP size:              1056780 bytes (unchanged)
- chunkdata size:         1026 bytes (unchanged)

Changes from v2 (MAP-7B failure analysis):
- objects.lua: was return {} (MAP-7A: ArrayIndexOutOfBoundsException) -> now comment-only
- spawnpoints.lua: was all key -> now unemployed key (explicit profession)
- LOTH/LOTP/chunkdata: unchanged from MAP-6Z

---

## Diagnostic value table

| Scenario | Classification | Next task |
|---|---|---|
| objects.lua error again | LOAD_TEST_FAIL_OBJECTS_LUA | MAP-7D: try omit/different Lua format |
| spawn region null again | LOAD_TEST_FAIL_SPAWN_REGION | MAP-7D: investigate spawnregions wiring |
| World loads | LOAD_TEST_PASS | Record carefully; no public claim until reviewed |
| Timeout / player data | LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA | MAP-7D: investigate world loading sequence |
| Inconclusive | LOAD_TEST_INCONCLUSIVE | Repeat with clean environment |

---

## Step 1: Remove previous test mods (HUMAN ONLY)

${fence}
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v2_001_test' -ErrorAction SilentlyContinue
HUMAN-ONLY: Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\$ModFolderName' -ErrorAction SilentlyContinue
${fence}

---

## Step 2: Copy v3 candidate to PZ mods (HUMAN ONLY)

SOURCE: $v42Dir
DESTINATION (parent): $($destBase | Split-Path -Parent)

${fence}
HUMAN-ONLY: Copy-Item -Recurse -Force '$v42Dir' '$($destBase | Split-Path -Parent)'
${fence}

---

## Step 3: Server preset (HUMAN ONLY)

See MAP_7C_INSTALL_AND_SERVER_WIRING_COMMANDS.md.

Required in ${iniPath}:
${fence}
Mods=$MapId
Map=$MapId;Muldraugh, KY
WorkshopItems=
${fence}

Required ${srPath}:
${fence}lua
function SpawnRegions()
    return {
        {name="PZMapForge v3 Candidate Cell", file="media/maps/$MapId/spawnpoints.lua"},
    }
end
${fence}

---

## Step 4: Patch host.ini and delete stale log (HUMAN ONLY)

${fence}
HUMAN-ONLY: Set in ${hostIni}: servername=$ServerName
HUMAN-ONLY: Remove-Item -Force '$consoleTxt' -ErrorAction SilentlyContinue
${fence}

---

## Step 5: Run test (HUMAN ONLY) and capture fresh log (HUMAN ONLY)

1. Launch Build 42, enable only $MapId.
2. Host > select $ServerName.
3. Record in MAP_7C_LOAD_TEST_RECORD.local-template.md.

${fence}
HUMAN-ONLY: Copy-Item '$consoleTxt' '$Output\logs\console-map7c-TIMESTAMP.txt'
${fence}

---

## Safety

- HUMAN_ONLY_COPY_REQUIRED
- LOAD_TEST_NOT_PERFORMED
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false
"@
Set-Content -Path $packetMdPath -Value $packetMd -Encoding ASCII
Write-Output "Packet MD: $packetMdPath"

# ---------------------------------------------------------------------------
# Write MAP_7C_LOAD_TEST_RECORD.local-template.md
# ---------------------------------------------------------------------------

$recordMdPath = Join-Path $Output 'MAP_7C_LOAD_TEST_RECORD.local-template.md'
$recordMd = @"
# MAP-7C Load Test Record

${fence}text
Profile:       empty_grass_v3
MapId:         $MapId
tested_at:     FILL_IN
${fence}

## Observations

| Item | Value |
|---|---|
| mod_selection_crash | yes/no |
| spawn_screen_reached | yes/no |
| candidate_spawn_region_visible | yes/no |
| world_load_started | yes/no |
| entered_world | yes/no |
| returned_to_menu | yes/no |
| crash_to_desktop | yes/no |
| loth_error_found | yes/no |
| lotp_error_found | yes/no |
| chunkdata_error_found | yes/no |
| objects_lua_error_found | yes/no |
| spawn_region_error_found | yes/no |
| timeout_waiting_player_data | yes/no |

## First error message

${fence}
FILL_IN
${fence}

## Result

- LOAD_TEST_PASS
- LOAD_TEST_FAIL_LOTH
- LOAD_TEST_FAIL_LOTP
- LOAD_TEST_FAIL_CHUNKDATA
- LOAD_TEST_FAIL_OBJECTS_LUA
- LOAD_TEST_FAIL_SPAWN_REGION
- LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA
- LOAD_TEST_INCONCLUSIVE

## Non-claims

- PLAYABLE_EXPORT_CLAIM_ALLOWED=false: binding.
"@
Set-Content -Path $recordMdPath -Value $recordMd -Encoding ASCII
Write-Output "Record template: $recordMdPath"

# ---------------------------------------------------------------------------
# Write MAP_7C_INSTALL_AND_SERVER_WIRING_COMMANDS.md
# ---------------------------------------------------------------------------

$wiringMdPath = Join-Path $Output 'MAP_7C_INSTALL_AND_SERVER_WIRING_COMMANDS.md'
$wiringMd = @"
# MAP-7C Install and Server Wiring Commands

All commands below are HUMAN-ONLY.

${fence}text
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
${fence}

## 1. Remove old test mods (HUMAN ONLY)

${fence}powershell
# HUMAN-ONLY
Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v2_001_test' -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force 'C:\Users\Palmacede\Zomboid\mods\$ModFolderName' -ErrorAction SilentlyContinue
${fence}

## 2. Copy v3 candidate (HUMAN ONLY)

${fence}powershell
# HUMAN-ONLY
New-Item -ItemType Directory -Force -Path '$($destBase | Split-Path -Parent)'
Copy-Item -Recurse -Force '$v42Dir' '$($destBase | Split-Path -Parent)'
${fence}

## 3. Server INI (HUMAN ONLY)

File: $iniPath
${fence}
Mods=$MapId
Map=$MapId;Muldraugh, KY
WorkshopItems=
${fence}

## 4. _spawnregions.lua (HUMAN ONLY)

File: $srPath
${fence}lua
function SpawnRegions()
    return {
        {name="PZMapForge v3 Candidate Cell", file="media/maps/$MapId/spawnpoints.lua"},
    }
end
${fence}

## 5. host.ini and log cleanup (HUMAN ONLY)

${fence}powershell
# HUMAN-ONLY
# Set in ${hostIni}: servername=$ServerName
Remove-Item -Force '$consoleTxt' -ErrorAction SilentlyContinue
${fence}

## 6. Capture log (HUMAN ONLY)

${fence}powershell
# HUMAN-ONLY
New-Item -ItemType Directory -Force -Path '$Output\logs'
Copy-Item '$consoleTxt' '$Output\logs\console-map7c-TIMESTAMP.txt'
${fence}
"@
Set-Content -Path $wiringMdPath -Value $wiringMd -Encoding ASCII
Write-Output "Wiring MD: $wiringMdPath"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "MAP7C_OBJECTS_LUA_METADATA_PACKET_CREATED"
Write-Output "EMPTY_GRASS_V3_CANDIDATE_GENERATED"
Write-Output "OBJECTS_LUA_FIXED_COMMENT_ONLY"
Write-Output "SPAWNPOINTS_LUA_UNEMPLOYED_KEY"
Write-Output "HUMAN_ONLY_COPY_REQUIRED"
Write-Output "LOAD_TEST_NOT_PERFORMED"
Write-Output "WRITER_NOT_CHANGED"
Write-Output "PLAYABLE_EXPORT_CLAIM_ALLOWED=false"
Write-Output ""
Write-Output "Outputs:"
Write-Output "  $packetMdPath"
Write-Output "  $recordMdPath"
Write-Output "  $wiringMdPath"
Write-Output "  $preflightJsonPath"
Write-Output "  $preflightMdPath"
Write-Output "Done."
