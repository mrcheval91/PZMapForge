#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7H: Generates the empty_grass_v4 candidate and produces a local
    map discovery path diagnostic packet.

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

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\prepare-build42-map7h-discovery-path-packet.ps1 `
        -Output .\.local\map7h-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$MapId         = 'pzmapforge_build42_candidate_v4_001',
    [string]$ModFolderName = 'pzmapforge_build42_candidate_v4_001_test'
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

$candDir    = Join-Path $candidateOut ($MapId + '_build42_candidate')
Write-Output "Candidate root: $candDir"

# ---------------------------------------------------------------------------
# Step 2: Run discovery path inspector
# ---------------------------------------------------------------------------

$inspectorScript = Join-Path $repoRoot 'scripts\inspect-build42-map-discovery-path.ps1'
Write-Output "Running discovery path inspector..."

& powershell -ExecutionPolicy Bypass -File $inspectorScript `
    -CandidateRoot $candDir `
    -Output $Output `
    -MapId $MapId

if ($LASTEXITCODE -ne 0) {
    Write-Error "Discovery path inspection failed (exit $LASTEXITCODE)"
    exit 1
}

# Load inspector results
$discoveryJson = Join-Path $Output 'map-discovery-path-report.json'
$discovery = if (Test-Path $discoveryJson) {
    Get-Content $discoveryJson -Raw | ConvertFrom-Json
} else { $null }

$hasVersioned = if ($null -ne $discovery) { [bool]$discovery.has_versioned_42_media_maps } else { $false }
$hasRoot      = if ($null -ne $discovery) { [bool]$discovery.has_root_media_maps } else { $false }
$riskLabel    = if ($null -ne $discovery) { [string]$discovery.map_folder_discovery_risk } else { 'UNKNOWN' }

# ---------------------------------------------------------------------------
# Packet files
# ---------------------------------------------------------------------------

# MAP_7H_DISCOVERY_PATH_PACKET.md
$packetMd = Join-Path $Output 'MAP_7H_DISCOVERY_PATH_PACKET.md'
Set-Content -Path $packetMd -Encoding ASCII -Value @"
# MAP-7H: Build 42 Map Discovery Path Packet

${fence}text
MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY
MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY
MAP_LINE_VARIANTS_EXHAUSTED
DISCOVERY_PATH_INVESTIGATION_ACTIVE
has_versioned_42_media_maps=$($hasVersioned.ToString().ToLower())
has_root_media_maps=$($hasRoot.ToString().ToLower())
map_folder_discovery_risk=$riskLabel
LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
${fence}

## A/B/C Map= variants exhausted

All three Map= line ordering variants produced empty IsoMetaGrid map folder scan.
Map= line ordering is NOT the root cause.

| Variant | Map= line | Result |
|---|---|---|
| A | Map=$MapId;Muldraugh, KY | MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY |
| B | Map=$MapId | MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY |
| C | Map=Muldraugh, KY;$MapId | MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY |

## Current candidate structure

The empty_grass_v4 candidate uses a versioned 42/ layout:

${fence}text
<mod_folder>/
  42/
    mod.info
    media/maps/$MapId/
      map.info
      spawnpoints.lua
      objects.lua
      0_0.lotheader
      world_0_0.lotpack
      chunkdata_0_0.bin
${fence}

has_versioned_42_media_maps: $hasVersioned
has_root_media_maps:         $hasRoot
map_folder_discovery_risk:   $riskLabel

## Discovery risk

IsoMetaGrid may scan only non-versioned media/maps/ paths. The 42/ version
layer routes mod file loading but may not be visible to IsoMetaGrid's
directory scan. A root-level media/maps/<map_id>/ directory alongside the
42/ layer may be required.

See MAP_7H_DISCOVERY_PATH_HYPOTHESES.md for proposed experiments.
See map-discovery-path-report.json for full inspection output.

## Non-claims

LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
"@

# MAP_7H_DISCOVERY_PATH_HYPOTHESES.md
$hypothesesMd = Join-Path $Output 'MAP_7H_DISCOVERY_PATH_HYPOTHESES.md'
Set-Content -Path $hypothesesMd -Encoding ASCII -Value @"
# MAP-7H: Discovery Path Hypotheses

## Current candidate layout issue

The candidate uses a versioned 42/ layout. IsoMetaGrid scans for map
folders but finds none. The 42/ version layer may be transparent to
IsoMetaGrid's scan.

## Hypotheses

### H1: 42/ version layer not scanned (HIGH RISK)

IsoMetaGrid may only scan paths registered as active mod media directories.
The 42/ prefix is the mod loader's version routing mechanism. IsoMetaGrid
may receive media paths WITHOUT the 42/ prefix, or the path registration
may fail for custom mods.

Fix candidate: add a root media/maps/$MapId/ directory alongside 42/.

### H2: map.info fields incomplete (MEDIUM RISK)

IsoMetaGrid may only list map folders whose map.info contains specific
required fields (e.g., lots=, id=, title=). The candidate map.info may be
missing fields that trigger map folder registration.

Fix candidate: compare candidate map.info against a known-working reference.

### H3: mod.info lacks media path registration (MEDIUM RISK)

The mod.info may need additional fields to register the media/maps/ path
with the mod loader. Some PZ mods use a 'map=' or similar field.

Fix candidate: inspect reference mod.info for map-registration fields.

## Proposed experiments (HUMAN-ONLY, not yet performed)

### Experiment D: Root media/maps/ duplicate

Add a root-level media/maps/$MapId/ alongside the versioned 42/ path.
Keep all existing binary files. Add map.info, spawnpoints.lua, objects.lua.

${fence}text
<mod_folder>/
  42/mod.info
  42/media/maps/$MapId/ (keep existing)
  media/maps/$MapId/ (NEW -- duplicate map files here)
    map.info
    spawnpoints.lua
    objects.lua
    0_0.lotheader (optional)
${fence}

Prediction: if H1 is correct, this allows IsoMetaGrid to find the map folder.
Evidence: does pzmapforge_build42_candidate_v4_001 appear in the map folder list?

### Experiment E: Root mod.info alongside 42/

Add a root-level mod.info at <mod_folder>/mod.info alongside 42/mod.info.
The root mod.info may be what activates media path registration for IsoMetaGrid.

### Experiment F: map.info field comparison

Obtain a known-working Build 42 custom map mod's map.info and compare its
fields line-by-line against the candidate map.info.

All experiments are HUMAN-ONLY. Do not automate writes to PZ folders.
Mark all results in MAP_7H_NEXT_MANUAL_EXPERIMENTS.local-template.md.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7H_VARIANT_RESULTS_SUMMARY.md
$variantsMd = Join-Path $Output 'MAP_7H_VARIANT_RESULTS_SUMMARY.md'
Set-Content -Path $variantsMd -Encoding ASCII -Value @"
# MAP-7H: Variant A/B/C Results Summary

MAP_LINE_VARIANTS_EXHAUSTED

## Variant A

Map= line: Map=$MapId;Muldraugh, KY
classification: MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY
candidate_loaded: true
player_data_received: true
game_loading_completed: true
map_folders_list_empty: true
spawn_building_warning: true
Observation: Muldraugh terrain loaded. Forest/grass world. No city choice.

## Variant B

Map= line: Map=$MapId
classification: MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY
candidate_loaded: true
player_data_received: false
game_loading_completed: true
map_folders_list_empty: true
spawn_building_warning: true
Observation: Empty/old world. Sparse forest. IsoChunk sanity errors in server
log (chunks 3,-2 and 11,-9) -- likely old-save contamination, not primary blocker.

## Variant C

Map= line: Map=Muldraugh, KY;$MapId
classification: MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY
candidate_loaded: true
player_data_received: true
game_loading_completed: true
map_folders_list_empty: true
spawn_building_warning: true
Observation: Forest/grass world. Coordinates around X 150, Y 150, Z 0. No city choice.

## Conclusion

All three variants: pzmapforge_build42_candidate_v4_001 does NOT appear in
IsoMetaGrid map-folder scan list. Map= ordering has no effect.

Next investigation focus: map folder discovery path and mod structure.
See MAP_7H_DISCOVERY_PATH_HYPOTHESES.md.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@

# MAP_7H_NEXT_MANUAL_EXPERIMENTS.local-template.md
$expMd = Join-Path $Output 'MAP_7H_NEXT_MANUAL_EXPERIMENTS.local-template.md'
Set-Content -Path $expMd -Encoding ASCII -Value @"
# MAP-7H: Next Manual Experiments Record (local template)

Fill in after each manual retest. Do not commit.

## Experiment D: Root media/maps/ duplicate

Status: NOT YET PERFORMED
Date:
PZ version:

Pre-conditions:
- [ ] <mod_folder>/42/ remains intact
- [ ] <mod_folder>/media/maps/$MapId/ created (root layout)
- [ ] map.info, spawnpoints.lua, objects.lua copied (no-BOM, HUMAN-ONLY)
- [ ] 0_0.lotheader and world_0_0.lotpack also placed at root level (optional)
- [ ] Server preset uses Map=$MapId or Map=$MapId;Muldraugh, KY
- [ ] Fresh log file (old logs deleted before launch)

Observation:
- Did pzmapforge_build42_candidate_v4_001 appear in the map-folder scan? YES / NO
- Paste map-folder scan section:

${fence}text
(paste here)
${fence}

Result: PASS (appeared in list) / FAIL (still empty) / INCONCLUSIVE

## Experiment E: Root mod.info alongside 42/

Status: NOT YET PERFORMED
Date:
PZ version:

Pre-conditions:
- [ ] Root mod.info placed at <mod_folder>/mod.info (same content as 42/mod.info)
- [ ] no-BOM encoding on root mod.info (HUMAN-ONLY)

Observation:
- Any change in map-folder scan? YES / NO

Result:

## Experiment F: map.info field comparison

Status: NOT YET PERFORMED
Date:

Reference mod:
Candidate map.info:
Reference map.info:

Field differences noted:

Result:

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: do not claim playable export.
LOAD_TEST_NOT_PERFORMED: this template is for future human tests.
"@

# ---------------------------------------------------------------------------
# map7h-discovery-preflight.json
# ---------------------------------------------------------------------------

$preflightJson = Join-Path $Output 'map7h-discovery-preflight.json'
$preflight = [ordered]@{
    schema                              = 'pzmapforge.map7h-discovery-preflight.v0.1'
    map_id                              = $MapId
    mod_folder_name                     = $ModFolderName
    variants_abc_exhausted              = $true
    variant_a_result                    = 'MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY'
    variant_b_result                    = 'MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY'
    variant_c_result                    = 'MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY'
    map_line_ordering_ruled_out         = $true
    has_versioned_42_media_maps         = $hasVersioned
    has_root_media_maps                 = $hasRoot
    root_media_maps_missing             = (-not $hasRoot)
    map_folder_discovery_risk           = $riskLabel
    possible_build42_version_layer_not_scanned_by_isometagrid = ($hasVersioned -and -not $hasRoot)
    next_experiment_d_root_media_maps   = 'NOT_YET_PERFORMED'
    next_experiment_e_root_mod_info     = 'NOT_YET_PERFORMED'
    next_experiment_f_map_info_fields   = 'NOT_YET_PERFORMED'
    load_test_not_performed             = $true
    public_playable_claim_allowed       = $false
    binary_writer_not_changed           = $true
    pz_assets_read                      = $false
}
$preflight | ConvertTo-Json -Depth 4 | Set-Content -Path $preflightJson -Encoding UTF8
Write-Output "JSON: $preflightJson"

# map7h-discovery-preflight.md
$preflightMd = Join-Path $Output 'map7h-discovery-preflight.md'
Set-Content -Path $preflightMd -Encoding ASCII -Value @"
# MAP-7H Discovery Preflight

${fence}text
map_id=$MapId
variants_abc_exhausted=true
map_line_ordering_ruled_out=true
has_versioned_42_media_maps=$($hasVersioned.ToString().ToLower())
has_root_media_maps=$($hasRoot.ToString().ToLower())
map_folder_discovery_risk=$riskLabel
next_experiment_d=NOT_YET_PERFORMED
next_experiment_e=NOT_YET_PERFORMED
next_experiment_f=NOT_YET_PERFORMED
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
Write-Output "MAP_7H_DISCOVERY_PATH_PACKET.md:               $(Test-Path $packetMd)"
Write-Output "MAP_7H_DISCOVERY_PATH_HYPOTHESES.md:           $(Test-Path $hypothesesMd)"
Write-Output "MAP_7H_VARIANT_RESULTS_SUMMARY.md:             $(Test-Path $variantsMd)"
Write-Output "MAP_7H_NEXT_MANUAL_EXPERIMENTS.local-template: $(Test-Path $expMd)"
Write-Output "map7h-discovery-preflight.json:                $(Test-Path $preflightJson)"
Write-Output "map7h-discovery-preflight.md:                  $(Test-Path $preflightMd)"
Write-Output "map-discovery-path-report.json:                $(Test-Path $discoveryJson)"
Write-Output "map-discovery-path-report.md:                  $(Test-Path (Join-Path $Output 'map-discovery-path-report.md'))"
Write-Output ""
Write-Output "MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY"
Write-Output "MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY"
Write-Output "MAP_LINE_VARIANTS_EXHAUSTED"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_THIS_SCRIPT"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
