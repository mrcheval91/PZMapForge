#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7I: Generates the experiment-E local candidate layout (root mod.info +
    root media/maps + 42/ versioned layout) and produces a diagnostic packet.

    All output under .local/ only.
    Does NOT copy files to PZ folders.
    Does NOT write outside .local/.
    Does NOT run PZ.

.PARAMETER Output
    Required. Path under .local/ for packet output.

.PARAMETER MapId
    Map ID. Default: pzmapforge_build42_candidate_v4_001

.PARAMETER ModFolderName
    Mod folder name for reference. Default: pzmapforge_build42_candidate_v4_001_test

.PARAMETER ServerName
    Suggested server preset name. Default: PZMF_B42_METADATA_V4_VARIANT_E_001

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\prepare-build42-map7i-root-modinfo-experiment-packet.ps1 `
        -Output .\.local\map7i-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001_test',
    [string]$ServerName    = 'PZMF_B42_METADATA_V4_VARIANT_E_001'
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

$fence = '```'

# ---------------------------------------------------------------------------
# Step 1: Generate empty_grass_v4 candidate
# ---------------------------------------------------------------------------

$candidateOut = Join-Path $Output 'candidate'
Write-Output "Generating empty_grass_v4 candidate..."

& dotnet run --project (Join-Path $repoRoot 'src\PZMapForge.Cli') `
    --configuration Release --no-build `
    -- map-export-experimental `
    --map-id $MapId `
    --output $candidateOut `
    --build42-candidate-writer `
    --build42-candidate-profile empty_grass_v4

if ($LASTEXITCODE -ne 0) {
    Write-Error "CLI candidate generation failed (exit $LASTEXITCODE)"
    exit 1
}

# Source paths from the generated candidate
$candDir    = Join-Path $candidateOut ($MapId + '_build42_candidate')
$src42Dir   = Join-Path $candDir '42'
$srcMapData = Join-Path $src42Dir "media\maps\$MapId"

# ---------------------------------------------------------------------------
# Step 2: Build experiment-E candidate layout under .local/
# ---------------------------------------------------------------------------

Write-Output "Building experiment-E candidate layout..."

$expEBase     = Join-Path $Output ('experiment-e-candidate\' + $MapId)
$expE42Dir    = Join-Path $expEBase '42'
$expE42Maps   = Join-Path $expE42Dir "media\maps\$MapId"
$expERootMaps = Join-Path $expEBase "media\maps\$MapId"

New-Item -ItemType Directory -Force -Path $expE42Maps   | Out-Null
New-Item -ItemType Directory -Force -Path $expERootMaps | Out-Null

# Copy versioned files from source candidate (byte-exact, preserves no-BOM)
$textFiles = @('mod.info')
foreach ($f in $textFiles) {
    Copy-Item -LiteralPath (Join-Path $src42Dir $f) `
              -Destination (Join-Path $expE42Dir $f) -Force
}
$mapDataFiles = @('map.info', 'spawnpoints.lua', 'objects.lua',
                  '0_0.lotheader', 'world_0_0.lotpack', 'chunkdata_0_0.bin')
foreach ($f in $mapDataFiles) {
    $src = Join-Path $srcMapData $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src `
                  -Destination (Join-Path $expE42Maps $f) -Force
    }
}

# Create root mod.info (copy from 42/mod.info — already no-BOM)
Copy-Item -LiteralPath (Join-Path $expE42Dir 'mod.info') `
          -Destination (Join-Path $expEBase 'mod.info') -Force

# Create root media/maps/<MapId>/ (copy map data files)
foreach ($f in $mapDataFiles) {
    $src = Join-Path $expE42Maps $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src `
                  -Destination (Join-Path $expERootMaps $f) -Force
    }
}

Write-Output "Experiment-E candidate at: $expEBase"

# ---------------------------------------------------------------------------
# Step 3: Verify no-BOM on text files
# ---------------------------------------------------------------------------

function Test-HasBom {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $b = [System.IO.File]::ReadAllBytes($Path)
    return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

$bomChecks = [ordered]@{
    '42/mod.info'                             = -not (Test-HasBom (Join-Path $expE42Dir 'mod.info'))
    'root/mod.info'                           = -not (Test-HasBom (Join-Path $expEBase 'mod.info'))
    '42/media/maps/map.info'                  = -not (Test-HasBom (Join-Path $expE42Maps 'map.info'))
    '42/media/maps/spawnpoints.lua'           = -not (Test-HasBom (Join-Path $expE42Maps 'spawnpoints.lua'))
    '42/media/maps/objects.lua'               = -not (Test-HasBom (Join-Path $expE42Maps 'objects.lua'))
    'root/media/maps/map.info'                = -not (Test-HasBom (Join-Path $expERootMaps 'map.info'))
    'root/media/maps/spawnpoints.lua'         = -not (Test-HasBom (Join-Path $expERootMaps 'spawnpoints.lua'))
    'root/media/maps/objects.lua'             = -not (Test-HasBom (Join-Path $expERootMaps 'objects.lua'))
}

$allNoBom = $true
foreach ($kv in $bomChecks.GetEnumerator()) {
    $status = if ($kv.Value) { 'OK (no-BOM)' } else { 'FAIL (has BOM)' }
    Write-Output "  BOM check $($kv.Key): $status"
    if (-not $kv.Value) { $allNoBom = $false }
}

# ---------------------------------------------------------------------------
# Step 4: Run discovery path inspector on experiment-E candidate
# ---------------------------------------------------------------------------

$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
Write-Output "Running discovery path inspector on experiment-E candidate..."

& powershell -ExecutionPolicy Bypass -File $inspectorScript `
    -CandidateRoot $expEBase `
    -Output $Output `
    -MapId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Discovery path inspection failed (exit $LASTEXITCODE)"
    exit 1
}

$discoveryJson = Join-Path $Output 'map-discovery-path-report.json'
$discovery = if (Test-Path $discoveryJson) {
    Get-Content $discoveryJson -Raw | ConvertFrom-Json
} else { $null }

$hasDual    = if ($null -ne $discovery) { [bool]$discovery.has_dual_mod_info_layout } else { $false }
$riskLabel  = if ($null -ne $discovery) { [string]$discovery.map_folder_discovery_risk } else { 'UNKNOWN' }
$hasRootMod = if ($null -ne $discovery) { [bool]$discovery.has_root_mod_info } else { $false }

# ---------------------------------------------------------------------------
# Packet files
# ---------------------------------------------------------------------------

# MAP_7I_ROOT_MODINFO_EXPERIMENT_PACKET.md
$packetMd = Join-Path $Output 'MAP_7I_ROOT_MODINFO_EXPERIMENT_PACKET.md'
Set-Content -Path $packetMd -Encoding ASCII -Value @"
# MAP-7I: Root mod.info Experiment E Packet

${fence}text
MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY
ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT
EXPERIMENT_E_ROOT_MOD_INFO_RECOMMENDED
has_root_mod_info=$($hasRootMod.ToString().ToLower())
has_dual_mod_info_layout=$($hasDual.ToString().ToLower())
map_folder_discovery_risk=$riskLabel
all_text_files_no_bom=$($allNoBom.ToString().ToLower())
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## Variant D result (recorded)

Variant D tested root media/maps/ duplicate alongside 42/media/maps/.
Result: MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY.
Root media/maps alone is NOT sufficient to register the map folder.
Root mod.info was absent in Variant D.

## Experiment E local candidate

A dual-layout experiment-E candidate has been generated under .local/.

${fence}text
experiment-e-candidate/$MapId/
  mod.info                              (root -- NEW)
  42/mod.info                           (versioned -- kept)
  42/media/maps/$MapId/                 (versioned -- kept)
  media/maps/$MapId/                    (root -- duplicated)
${fence}

has_dual_mod_info_layout: $hasDual
map_folder_discovery_risk: $riskLabel

## No-BOM verification

All text files in the experiment-E candidate have been verified:
$(foreach ($kv in $bomChecks.GetEnumerator()) { "  $($kv.Key): $(if ($kv.Value) { 'no-BOM OK' } else { 'HAS BOM -- FAIL' })`n" })
## Human-only install instructions

See MAP_7I_EXPERIMENT_E_MANUAL_INSTALL_COMMANDS.md for step-by-step
human-only copy instructions. The packet script does NOT copy to PZ folders.

## Non-claims

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@

# MAP_7I_VARIANT_D_RESULT_SUMMARY.md
$variantDMd = Join-Path $Output 'MAP_7I_VARIANT_D_RESULT_SUMMARY.md'
Set-Content -Path $variantDMd -Encoding ASCII -Value @"
# MAP-7I: Variant D Result Summary

MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY
ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT

Variant D:
  Layout: 42/media/maps/$MapId/ + media/maps/$MapId/ (root duplicate)
  Map= line: Map=$MapId;Muldraugh, KY
  Result: IsoMetaGrid map-folder scan still empty
  candidate_loaded: true
  player_data_received: true
  map_folders_list_empty: true
  spawn_building_warning: true
  Visual: forest/fog world, square blocked area, no city choice
  Square blocked area: NOT proof of map registration (scan was empty)
  Mannequin error: Muldraugh objects.lua issue, unrelated to candidate

Conclusion: root media/maps without root mod.info is insufficient.
Next: Experiment E adds root mod.info.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7I_EXPERIMENT_E_MANUAL_INSTALL_COMMANDS.md
$installMd = Join-Path $Output 'MAP_7I_EXPERIMENT_E_MANUAL_INSTALL_COMMANDS.md'
Set-Content -Path $installMd -Encoding ASCII -Value @"
# MAP-7I: Experiment E Manual Install Commands (HUMAN-ONLY)

These commands show how to copy the experiment-E candidate to the PZ mods
folder. The packet script does NOT do this. All steps are HUMAN-ONLY.

## Candidate source location (already built by packet script)

${fence}text
$expEBase
${fence}

Structure inside:
${fence}text
$MapId/
  mod.info                    (root -- no-BOM)
  42/mod.info                 (versioned -- no-BOM)
  42/media/maps/$MapId/       (versioned map files)
  media/maps/$MapId/          (root map files)
${fence}

## Step 1: Clean old install (HUMAN-ONLY)

Delete any existing $ModFolderName folder from your PZ mods directory.

## Step 2: Copy experiment-E candidate (HUMAN-ONLY)

${fence}powershell
Copy-Item -Recurse -Force `
  "$expEBase" `
  "C:\Users\YourUser\Zomboid\mods\$ModFolderName"
${fence}

Replace YourUser with your Windows username.
Verify the destination has both mod.info and 42/mod.info after copying.

## Step 3: Server configuration (HUMAN-ONLY -- no-BOM UTF-8)

In your server preset INI file:
${fence}ini
Map=$MapId;Muldraugh, KY
Mods=$MapId
${fence}

Server name: $ServerName
Save with no-BOM UTF-8 encoding (verify in hex editor).

## Step 4: Launch and capture log

Run PZ server and client. After loading, copy DebugLog to .local:
${fence}text
C:\Users\YourUser\Zomboid\Logs\*_DebugLog.txt -> .local\map7i-logs\DebugLog-variant-E.txt
${fence}

## Step 5: Analyze log

${fence}powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\inspect-build42-map7d-load-result.ps1 `
  -LogPath .\.local\map7i-logs\DebugLog-variant-E.txt `
  -Output .\.local\map7i-analysis\variant-E `
  -ExpectedMapId $MapId `
  -VariantLabel VariantE
${fence}

Target result: map_folders_list_empty=false and $MapId appears in the list.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7I_EXPERIMENT_E_LOG_CAPTURE_COMMANDS.md
$logCaptureMd = Join-Path $Output 'MAP_7I_EXPERIMENT_E_LOG_CAPTURE_COMMANDS.md'
Set-Content -Path $logCaptureMd -Encoding ASCII -Value @"
# MAP-7I: Experiment E Log Capture Commands

After running Experiment E, copy and analyze logs to confirm whether
$MapId appears in the IsoMetaGrid map-folder scan.

## Copy log (HUMAN-ONLY)

${fence}powershell
New-Item -ItemType Directory -Force -Path .\.local\map7i-logs | Out-Null
Copy-Item "C:\Users\YourUser\Zomboid\Logs\*_DebugLog.txt" `
  .\.local\map7i-logs\DebugLog-variant-E.txt
${fence}

## Run analyzer

${fence}powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\inspect-build42-map7d-load-result.ps1 `
  -LogPath .\.local\map7i-logs\DebugLog-variant-E.txt `
  -Output .\.local\map7i-analysis\variant-E `
  -ExpectedMapId $MapId `
  -VariantLabel VariantE
${fence}

## Expected success output

${fence}text
classification: MAP7I_VARIANT_E_MAP_FOLDER_SCAN_FOUND
map_folders_list_empty: false
map_folders_list_count: >= 1
timestamped_debuglog_detected: true
${fence}

## Expected failure output (still blocked)

${fence}text
classification: MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY
map_folders_list_empty: true
${fence}

## Key map-folder scan section to capture

${fence}text
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
  (expect: $MapId appears here)
<End of map-folders list>
${fence}

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
"@

# MAP_7I_EXPERIMENT_E_MANUAL_RESULT.local-template.md
$resultMd = Join-Path $Output 'MAP_7I_EXPERIMENT_E_MANUAL_RESULT.local-template.md'
Set-Content -Path $resultMd -Encoding ASCII -Value @"
# MAP-7I: Experiment E Manual Result (local template)

Fill in after Experiment E manual retest. Do not commit.

## Test metadata

Date:
PZ version:
Server: $ServerName
Map= line: Map=$MapId;Muldraugh, KY
Layout tested: dual mod.info (root + 42/) + dual media/maps (root + 42/)

## Pre-test checks (HUMAN-ONLY)

- [ ] root mod.info present at <mod_folder>/mod.info (no-BOM)
- [ ] 42/mod.info present and unchanged
- [ ] root media/maps/$MapId/ present (no-BOM text files)
- [ ] 42/media/maps/$MapId/ present and unchanged
- [ ] Server preset INI updated (no-BOM)
- [ ] Old log files deleted
- [ ] Only candidate mod enabled

## Map folder scan section (paste from DebugLog)

${fence}text
(paste here)
${fence}

- Did $MapId appear in map folder scan? YES / NO
- map_folders_list_empty:

## Analyzer output

classification:
map_folders_list_empty:
map_folders_list_count:
timestamped_debuglog_detected:

## Result

(PASS -- map folder registered) / (FAIL -- still empty) / (INCONCLUSIVE)

## Visual observation

World type:
Spawn warning present:
Any new errors:

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# ---------------------------------------------------------------------------
# map7i-root-modinfo-preflight.json
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $Output 'map7i-root-modinfo-preflight.json'
$preflight = [ordered]@{
    schema                              = 'pzmapforge.map7i-root-modinfo-preflight.v0.1'
    map_id                              = $MapId
    mod_folder_name                     = $ModFolderName
    server_name                         = $ServerName
    variant_d_result                    = 'MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY'
    root_media_maps_alone_insufficient  = $true
    experiment_e_root_mod_info          = $true
    experiment_e_candidate_generated    = $true
    experiment_e_candidate_path         = $expEBase
    has_root_mod_info                   = $hasRootMod
    has_dual_mod_info_layout            = $hasDual
    map_folder_discovery_risk           = $riskLabel
    all_text_files_no_bom               = $allNoBom
    bom_checks                          = $bomChecks
    suggested_map_line                  = "Map=$MapId;Muldraugh, KY"
    suggested_analyzer_expected_map_id  = $MapId
    suggested_analyzer_variant_label    = 'VariantE'
    all_server_writes_human_only        = $true
    all_pz_folder_writes_human_only     = $true
    load_test_not_performed             = $true
    public_playable_claim_allowed       = $false
    binary_writer_not_changed           = $true
}
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJson -Encoding UTF8
Write-Output "JSON: $preflightJson"

# map7i-root-modinfo-preflight.md
$preflightMd = Join-Path $Output 'map7i-root-modinfo-preflight.md'
Set-Content -Path $preflightMd -Encoding ASCII -Value @"
# MAP-7I Root mod.info Experiment Preflight

${fence}text
map_id=$MapId
variant_d_result=MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY
root_media_maps_alone_insufficient=true
experiment_e_root_mod_info=true
has_root_mod_info=$($hasRootMod.ToString().ToLower())
has_dual_mod_info_layout=$($hasDual.ToString().ToLower())
map_folder_discovery_risk=$riskLabel
all_text_files_no_bom=$($allNoBom.ToString().ToLower())
all_pz_folder_writes_human_only=true
load_test_not_performed=true
public_playable_claim_allowed=false
${fence}

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@
Write-Output "MD: $preflightMd"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Output ""
Write-Output "MAP_7I_ROOT_MODINFO_EXPERIMENT_PACKET.md:          $(Test-Path $packetMd)"
Write-Output "MAP_7I_VARIANT_D_RESULT_SUMMARY.md:                $(Test-Path $variantDMd)"
Write-Output "MAP_7I_EXPERIMENT_E_MANUAL_INSTALL_COMMANDS.md:    $(Test-Path $installMd)"
Write-Output "MAP_7I_EXPERIMENT_E_LOG_CAPTURE_COMMANDS.md:       $(Test-Path $logCaptureMd)"
Write-Output "MAP_7I_EXPERIMENT_E_MANUAL_RESULT.local-template:  $(Test-Path $resultMd)"
Write-Output "map7i-root-modinfo-preflight.json:                 $(Test-Path $preflightJson)"
Write-Output "map7i-root-modinfo-preflight.md:                   $(Test-Path $preflightMd)"
Write-Output "map-discovery-path-report.json:                    $(Test-Path $discoveryJson)"
Write-Output "map-discovery-path-report.md:                      $(Test-Path (Join-Path $Output 'map-discovery-path-report.md'))"
Write-Output ""
Write-Output "MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY"
Write-Output "ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT"
Write-Output "EXPERIMENT_E_ROOT_MOD_INFO_RECOMMENDED"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
