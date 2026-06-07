#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7P: Generates the known-working runtime baseline diagnostic packet.

    Records the Experiment I (Variant I) failure and prepares human-only
    instructions for the operator to run a known-working Dru_map baseline
    using the Workshop-activated Build 42 mod.

    Does NOT run Project Zomboid.
    Does NOT write outside .local/.
    Does NOT read or write Workshop, mods, Server, or PZ install paths.
    Does NOT copy Dru_map automatically.
    No load test is performed by this script.

    Writes under -Output (must be under .local):
      MAP_7P_KNOWN_WORKING_RUNTIME_BASELINE_PACKET.md
      MAP_7P_VARIANT_I_RESULT_SUMMARY.md
      MAP_7P_DRUMAP_BASELINE_MANUAL_SERVER_WIRING.md
      MAP_7P_DRUMAP_BASELINE_LOG_CAPTURE_COMMANDS.md
      MAP_7P_NEXT_DECISION_TREE.md
      map7p-preflight.json
      map7p-preflight.md

.PARAMETER Output
    Path under .local/ for all packet outputs.

.PARAMETER ReferenceModId
    Optional. Defaults to Dru_map.

.PARAMETER ReferenceMapId
    Optional. Defaults to Dru_map.

.PARAMETER ReferenceWorkshopId
    Optional. Defaults to 3355966216.

.PARAMETER ReferenceServerName
    Optional. Defaults to PZMF_B42_DRUMAP_BASELINE_001.

.EXAMPLE
    powershell -ExecutionPolicy Bypass `
        -File .\scripts\prepare-build42-map7p-known-working-runtime-baseline-packet.ps1 `
        -Output .\.local\map7p-baseline-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$ReferenceModId      = 'Dru_map',
    [string]$ReferenceMapId      = 'Dru_map',
    [string]$ReferenceWorkshopId = '3355966216',
    [string]$ReferenceServerName = 'PZMF_B42_DRUMAP_BASELINE_001'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

Write-Output "MAP-7P: Known-Working Runtime Baseline Packet"
Write-Output "Output: $Output"
Write-Output ""

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

$variantIResult            = 'MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY'
$variantsExhausted         = $true
$baselineExpectedMapId     = $ReferenceMapId
$baselineVariantLabel      = 'DruMapBaseline'
$baselineFoundClass        = 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND'
$baselineEmptyClass        = 'MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY'
$publicPlayableClaimAllowed = $false

# ---------------------------------------------------------------------------
# map7p-preflight.json
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema                     = 'pzmapforge.map7p-preflight.v0.1'
    variant_i_result           = $variantIResult
    variants_abcdefghi_exhausted = $variantsExhausted
    reference_mod_id           = $ReferenceModId
    reference_map_id           = $ReferenceMapId
    reference_workshop_id      = $ReferenceWorkshopId
    reference_server_name      = $ReferenceServerName
    baseline_expected_map_id   = $baselineExpectedMapId
    baseline_variant_label     = $baselineVariantLabel
    baseline_found_class       = $baselineFoundClass
    baseline_empty_class       = $baselineEmptyClass
    public_playable_claim_allowed = $publicPlayableClaimAllowed
    load_test_performed_by_script = $false
    no_pz_run                  = $true
    no_workshop_writes         = $true
    no_mods_folder_writes      = $true
    no_server_folder_writes    = $true
    no_pz_install_writes       = $true
    drumap_copy_automatic      = $false
}

$preflightJsonPath = Join-Path $Output 'map7p-preflight.json'
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7p-preflight.json"

# ---------------------------------------------------------------------------
# map7p-preflight.md
# ---------------------------------------------------------------------------

$preflightMdPath = Join-Path $Output 'map7p-preflight.md'
$preflightMdContent = @"
# MAP-7P Preflight

## Variant I result

``````text
variant_i_result: $variantIResult
variants_abcdefghi_exhausted: $($variantsExhausted.ToString().ToLower())
``````

## Baseline reference

``````text
mod_id:       $ReferenceModId
map_id:       $ReferenceMapId
workshop_id:  $ReferenceWorkshopId
server_name:  $ReferenceServerName
``````

## Safety constraints

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
no_pz_run=true
no_workshop_writes=true
no_mods_folder_writes=true
no_server_folder_writes=true
no_pz_install_writes=true
drumap_copy_automatic=false
``````
"@

Set-Content -Path $preflightMdPath -Value $preflightMdContent -Encoding ASCII
Write-Output "Wrote: map7p-preflight.md"

# ---------------------------------------------------------------------------
# MAP_7P_VARIANT_I_RESULT_SUMMARY.md
# ---------------------------------------------------------------------------

$variantISummaryPath = Join-Path $Output 'MAP_7P_VARIANT_I_RESULT_SUMMARY.md'
$variantISummaryContent = @"
# MAP-7P: Variant I Result Summary

``````text
$variantIResult
VARIANTS_ABCDEFGHI_EXHAUSTED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Result

Experiment I installed at:
  C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001

Layout (Dru_map-aligned):
  root mod.info: EXISTS
  42/mod.info: EXISTS
  common/mod.info: ABSENT (intentional)
  common/media/maps/pzmapforge_build42_candidate_v4_001/: EXISTS
  map.info lots=NONE: YES
  zoomX/Y/S: YES
  Encoding: no-BOM

Analyzer result:
  classification:         $variantIResult
  candidate_loaded:       true
  player_data_received:   true
  game_loading_completed: true
  map_folders_list_empty: true
  spawn_building_warning: true

Key evidence:
- IsoMetaGrid.Create scanned directories.
- Looking in these map folders: appeared.
- <End of map-folders list> appeared immediately with zero entries.
- No custom map folder was registered.
- Forest/fallback world loaded. Mannequin-zone warning is vanilla noise.
- No city choice. initSpawnBuildings: no room or building at 150,150,0.
- Game loading took 40 seconds. Multiplayer.

## Correct interpretation

$variantIResult is the binding result.
The static Dru_map-aligned layout was not sufficient.
All layout variants A through I are exhausted.
The blocker is runtime activation, not static layout.

## What this is NOT

- Forest/fallback world is not proof of partial map registration.
- No city choice is not a diagnostic signal.
- Player data received is a partial pass only; does not mean map registered.
- Spawn building warning is expected when no custom map is registered.

## Success condition

The map folder must appear between:
  "Looking in these map folders:"
and:
  "<End of map-folders list>"

Until that occurs, no custom map cell can be loaded.
"@

Set-Content -Path $variantISummaryPath -Value $variantISummaryContent -Encoding ASCII
Write-Output "Wrote: MAP_7P_VARIANT_I_RESULT_SUMMARY.md"

# ---------------------------------------------------------------------------
# MAP_7P_DRUMAP_BASELINE_MANUAL_SERVER_WIRING.md
# ---------------------------------------------------------------------------

$wiringPath = Join-Path $Output 'MAP_7P_DRUMAP_BASELINE_MANUAL_SERVER_WIRING.md'
$wiringContent = @"
# MAP-7P: Dru_map Baseline Manual Server Wiring

## Purpose

Establish a known-working reference point. Dru_map (Workshop ID: $ReferenceWorkshopId)
is a confirmed Build 42 multiplayer map mod. Running it through the same
server wiring used for PZMapForge tests confirms whether Workshop-activated
mods appear in the IsoMetaGrid map folder scan.

If $ReferenceModId only works through the Workshop subscription/download flow,
this may reveal that PZMapForge local-only mods are not being mounted into
IsoMetaGrid the same way as Workshop-downloaded mods.

## HUMAN-ONLY: All steps below are operator actions. Claude does not perform them.

## Step 1: Subscribe to Dru_map on Steam

Workshop URL: https://steamcommunity.com/sharedfiles/filedetails/?id=$ReferenceWorkshopId

Subscribe and allow Steam to download the mod before launching PZ.

Expected install location (after Steam download):
  D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\$ReferenceWorkshopId

## Step 2: Create a fresh dedicated server preset

Server name: $ReferenceServerName
Public: false

## Step 3: Server ini wiring

In the server ini file for ${ReferenceServerName}:

``````ini
Mods=$ReferenceModId
WorkshopItems=$ReferenceWorkshopId
Map=$ReferenceMapId;Muldraugh, KY
``````

## Step 4: Launch server and client

Launch the dedicated server with the $ReferenceServerName preset.
Connect a local client.

## Step 5: Capture the client log

After game loading completes (or fails), capture the client log:
  C:\Users\Palmacede\Zomboid\Logs\DebugLog-client.txt
  (or equivalent for your PZ version)

Copy to .local:
  Copy-Item "C:\Users\Palmacede\Zomboid\Logs\DebugLog-client.txt" `
      ".\.local\map7p-logs\DebugLog-drumap-baseline.txt"

## What to look for

In the client log, locate:

  Looking in these map folders:
  <map folders listed here, if any>
  <End of map-folders list>

If $ReferenceMapId appears between those two lines:
  -> Baseline is MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND
  -> Workshop activation is sufficient for IsoMetaGrid discovery.
  -> The PZMapForge local mod lacks a runtime activation condition.

If the list is empty:
  -> Baseline is MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY
  -> Verify the server wiring and Workshop download before concluding.

## Comparison target

If $ReferenceMapId appears in the map-folder scan but
pzmapforge_build42_candidate_v4_001 does not:
  -> Next task is runtime activation contract alignment, not binary writing.
  -> Investigate WorkshopItems vs local-only Mods= activation.
  -> Do not proceed to LOTH/LOTP/chunkdata investigation yet.
"@

Set-Content -Path $wiringPath -Value $wiringContent -Encoding ASCII
Write-Output "Wrote: MAP_7P_DRUMAP_BASELINE_MANUAL_SERVER_WIRING.md"

# ---------------------------------------------------------------------------
# MAP_7P_DRUMAP_BASELINE_LOG_CAPTURE_COMMANDS.md
# ---------------------------------------------------------------------------

$logCapturePath = Join-Path $Output 'MAP_7P_DRUMAP_BASELINE_LOG_CAPTURE_COMMANDS.md'
$logCaptureContent = @"
# MAP-7P: Dru_map Baseline Log Capture and Analyzer Commands

## HUMAN-ONLY: All copy steps are operator actions.

## Step 1: Copy the client log

``````powershell
New-Item -ItemType Directory -Force -Path ".\.local\map7p-logs"
Copy-Item "C:\Users\Palmacede\Zomboid\Logs\DebugLog-client.txt" `
    ".\.local\map7p-logs\DebugLog-drumap-baseline.txt"
``````

## Step 2: Run the analyzer

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
    -LogPath .\.local\map7p-logs\DebugLog-drumap-baseline.txt ``
    -Output .\.local\map7p-analysis-drumap-baseline ``
    -ExpectedMapId $baselineExpectedMapId ``
    -VariantLabel $baselineVariantLabel
``````

Expected success classification:
  $baselineFoundClass

Expected failure classification:
  $baselineEmptyClass

## Step 3: Interpret the result

If classification is ${baselineFoundClass}:
  - $ReferenceMapId appeared in the IsoMetaGrid map-folder scan.
  - Workshop-activated mods ARE discovered by IsoMetaGrid.
  - PZMapForge local mod is missing a runtime activation condition.
  - Next: investigate WorkshopItems vs Mods= activation contract.
  - Record: docs/MAP_7P_VARIANT_I_AND_RUNTIME_BASELINE.md

If classification is ${baselineEmptyClass}:
  - $ReferenceMapId did NOT appear in the IsoMetaGrid map-folder scan.
  - Verify server wiring: WorkshopItems=$ReferenceWorkshopId present?
  - Verify Dru_map is downloaded and not just subscribed.
  - Fix the baseline before interpreting PZMapForge results.

If game fails to load entirely:
  - Check if Dru_map requires a specific Build 42 version.
  - Check if the server preset is correctly configured.
  - Do not interpret a load failure as evidence about map folder scanning.

## Important: Claim boundary

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
``````

This script generates instructions only.
It does not run PZ. It does not write to PZ folders.
All PZ operations are HUMAN-ONLY.
"@

Set-Content -Path $logCapturePath -Value $logCaptureContent -Encoding ASCII
Write-Output "Wrote: MAP_7P_DRUMAP_BASELINE_LOG_CAPTURE_COMMANDS.md"

# ---------------------------------------------------------------------------
# MAP_7P_NEXT_DECISION_TREE.md
# ---------------------------------------------------------------------------

$decisionTreePath = Join-Path $Output 'MAP_7P_NEXT_DECISION_TREE.md'
$decisionTreeContent = @"
# MAP-7P: Next Decision Tree

## Current state

``````text
$variantIResult
VARIANTS_ABCDEFGHI_EXHAUSTED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

All nine static layout variants A through I have produced MAP_FOLDER_SCAN_EMPTY.
The next diagnostic target is runtime activation.

## Decision tree

### Branch 1: Dru_map appears in IsoMetaGrid scan

Classification: $baselineFoundClass

Conclusion:
  The runtime pipeline CAN discover Workshop map mods.
  PZMapForge local-only mod is missing a runtime activation condition.

Next task:
  Compare WorkshopItems/Mods= activation paths.
  Investigate whether local-only (non-Workshop) mods are mounted differently
  by IsoMetaGrid in Build 42 multiplayer server mode.
  Test adding WorkshopItems= pointing to the local folder (if PZ supports it).
  Record findings as MAP-7Q or the next applicable MAP task.

### Branch 2: Dru_map does not appear but game loads

Classification: $baselineEmptyClass (with successful game load)

Conclusion:
  The client log scan may not be the right evidence point.
  Dru_map works in game but did not appear in the map-folder scan.
  Inspect server-side logs for map load evidence instead.
  Verify whether the city-choice screen showed Dru_map as an option.

Next task:
  Capture server-side log and look for the map-folder scan there.
  Record as MAP-7Q.

### Branch 3: Dru_map baseline fails entirely

Classification: $baselineEmptyClass (with game load failure)

Conclusion:
  The baseline server wiring is invalid.
  Do not interpret PZMapForge results against an invalid baseline.

Next task:
  Fix the Dru_map server wiring (verify WorkshopItems, Mods, Map lines).
  Re-run the baseline before drawing conclusions about PZMapForge.

### Branch 4: Dru_map scan reaches lotheader stage

Evidence: MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING in log

Conclusion:
  IsoMetaGrid found the Dru_map folder and began loading lotheader files.
  This is the next stage beyond MAP_FOLDER_SCAN_EMPTY.
  Compare the stage transition evidence against PZMapForge candidate.

Next task:
  Record the exact stage transition.
  Compare: if Dru_map passes lotheader stage but PZMapForge does not,
  binary writer quality becomes the next investigation focus.
  Record as MAP-7Q.

## Non-claims

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_PZMAPFORGE_PLAYABLE_EXPORT_CLAIM
``````

This document is a diagnostic decision guide.
No playable PZMapForge export is claimed from any branch above.
"@

Set-Content -Path $decisionTreePath -Value $decisionTreeContent -Encoding ASCII
Write-Output "Wrote: MAP_7P_NEXT_DECISION_TREE.md"

# ---------------------------------------------------------------------------
# MAP_7P_KNOWN_WORKING_RUNTIME_BASELINE_PACKET.md (main packet doc)
# ---------------------------------------------------------------------------

$packetPath = Join-Path $Output 'MAP_7P_KNOWN_WORKING_RUNTIME_BASELINE_PACKET.md'
$packetContent = @"
# MAP-7P: Known-Working Runtime Baseline Packet

``````text
$variantIResult
VARIANTS_ABCDEFGHI_EXHAUSTED
DRUMAP_BASELINE_DIAGNOSTIC_REQUIRED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Purpose

This packet records the Variant I failure and defines the Dru_map
known-working runtime baseline diagnostic.

All nine layout variants (A through I) have produced MAP_FOLDER_SCAN_EMPTY.
The static layout is not the discriminator.

The hypothesis is that PZMapForge local mods and Workshop-downloaded mods
are treated differently by IsoMetaGrid in Build 42 multiplayer server mode.

The Dru_map baseline (Workshop ID: $ReferenceWorkshopId) tests this hypothesis
by confirming whether a known-working Workshop mod appears in the IsoMetaGrid
map folder scan under the same log-evidence criteria used for PZMapForge.

## Key reference

``````text
Reference mod:        $ReferenceModId
Reference map folder: $ReferenceMapId
Workshop ID:          $ReferenceWorkshopId
Server wiring:
  Mods=$ReferenceModId
  WorkshopItems=$ReferenceWorkshopId
  Map=$ReferenceMapId;Muldraugh, KY
  Public=false
``````

## Analyzer command

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
    -LogPath .\.local\map7p-logs\DebugLog-drumap-baseline.txt ``
    -Output .\.local\map7p-analysis-drumap-baseline ``
    -ExpectedMapId $baselineExpectedMapId ``
    -VariantLabel $baselineVariantLabel
``````

Expected classifications:
  Found:  $baselineFoundClass
  Empty:  $baselineEmptyClass

## Comparison target

If $ReferenceMapId appears in the map-folder scan but
pzmapforge_build42_candidate_v4_001 does not:
  -> Next task = runtime activation contract alignment.
  -> Do not investigate binary writer quality until map folder scan passes.

## Packet files

``````text
MAP_7P_KNOWN_WORKING_RUNTIME_BASELINE_PACKET.md  (this file)
MAP_7P_VARIANT_I_RESULT_SUMMARY.md
MAP_7P_DRUMAP_BASELINE_MANUAL_SERVER_WIRING.md
MAP_7P_DRUMAP_BASELINE_LOG_CAPTURE_COMMANDS.md
MAP_7P_NEXT_DECISION_TREE.md
map7p-preflight.json
map7p-preflight.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
No PZ run performed by script.
No writes to Workshop, mods, Server, or PZ install paths.
Dru_map not copied automatically.
All PZ operations are HUMAN-ONLY.
``````
"@

Set-Content -Path $packetPath -Value $packetContent -Encoding ASCII
Write-Output "Wrote: MAP_7P_KNOWN_WORKING_RUNTIME_BASELINE_PACKET.md"

Write-Output ""
Write-Output "MAP-7P packet complete."
Write-Output "variant_i_result: $variantIResult"
Write-Output "variants_abcdefghi_exhausted: $($variantsExhausted.ToString().ToLower())"
Write-Output "reference_mod_id: $ReferenceModId"
Write-Output "WorkshopItems=$ReferenceWorkshopId"
Write-Output "Mods=$ReferenceModId"
Write-Output "Map=$ReferenceMapId;Muldraugh, KY"
Write-Output "-ExpectedMapId $baselineExpectedMapId"
Write-Output "-VariantLabel $baselineVariantLabel"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_SCRIPT"
Write-Output "Done."
