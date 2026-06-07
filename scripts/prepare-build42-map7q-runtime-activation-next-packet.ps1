#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7Q: Generates the runtime activation next-step diagnostic packet.

    Records the Dru_map baseline runtime success and prepares the runtime
    activation investigation framework for the PZMapForge candidate.

    Does NOT run Project Zomboid.
    Does NOT write outside .local/.
    Does NOT read or write Workshop, mods, Server, or PZ install paths.
    No load test is performed by this script.

    Writes under -Output (must be under .local):
      MAP_7Q_RUNTIME_ACTIVATION_PACKET.md
      MAP_7Q_DRUMAP_BASELINE_RESULT_SUMMARY.md
      MAP_7Q_ANALYZER_EVIDENCE_MODEL.md
      MAP_7Q_NEXT_DECISION_TREE.md
      MAP_7Q_WORKSHOP_STYLE_ACTIVATION_HYPOTHESES.md
      map7q-preflight.json
      map7q-preflight.md

.PARAMETER Output
    Path under .local/ for all packet outputs.

.PARAMETER CandidateMapId
    Optional. Defaults to pzmapforge_build42_candidate_v4_001.

.PARAMETER ReferenceMapId
    Optional. Defaults to Dru_map.

.PARAMETER ReferenceWorkshopId
    Optional. Defaults to 3355966216.

.EXAMPLE
    powershell -ExecutionPolicy Bypass `
        -File .\scripts\prepare-build42-map7q-runtime-activation-next-packet.ps1 `
        -Output .\.local\map7q-activation-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateMapId      = 'pzmapforge_build42_candidate_v4_001',
    [string]$ReferenceMapId      = 'Dru_map',
    [string]$ReferenceWorkshopId = '3355966216'
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

Write-Output "MAP-7Q: Runtime Activation Next-Step Packet"
Write-Output "Output: $Output"
Write-Output ""

# ---------------------------------------------------------------------------
# map7q-preflight.json
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema                              = 'pzmapforge.map7q-preflight.v0.1'
    drumap_baseline_runtime_success_model = $true
    empty_client_scan_not_decisive      = $true
    variants_abcdefghi_exhausted        = $true
    candidate_map_id                    = $CandidateMapId
    reference_map_id                    = $ReferenceMapId
    reference_workshop_id               = $ReferenceWorkshopId
    public_playable_claim_allowed       = $false
    load_test_performed_by_script       = $false
    no_automatic_workshop_upload        = $true
    no_pz_run                           = $true
    no_workshop_writes                  = $true
    no_mods_folder_writes               = $true
    no_server_folder_writes             = $true
    binary_writer_changed               = $false
}

$preflightJsonPath = Join-Path $Output 'map7q-preflight.json'
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7q-preflight.json"

# ---------------------------------------------------------------------------
# map7q-preflight.md
# ---------------------------------------------------------------------------

$preflightMdPath = Join-Path $Output 'map7q-preflight.md'
$preflightMdContent = @"
# MAP-7Q Preflight

## State

``````text
MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS
EMPTY_CLIENT_SCAN_NOT_DECISIVE
VARIANTS_ABCDEFGHI_EXHAUSTED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Evidence model

``````text
drumap_baseline_runtime_success_model: true
empty_client_scan_not_decisive:        true
variants_abcdefghi_exhausted:          true
``````

## Safety constraints

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
no_automatic_workshop_upload=true
no_pz_run=true
no_workshop_writes=true
binary_writer_changed=false
``````
"@

Set-Content -Path $preflightMdPath -Value $preflightMdContent -Encoding ASCII
Write-Output "Wrote: map7q-preflight.md"

# ---------------------------------------------------------------------------
# MAP_7Q_DRUMAP_BASELINE_RESULT_SUMMARY.md
# ---------------------------------------------------------------------------

$summaryPath = Join-Path $Output 'MAP_7Q_DRUMAP_BASELINE_RESULT_SUMMARY.md'
$summaryContent = @"
# MAP-7Q: Dru_map Baseline Result Summary

``````text
MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS
EMPTY_CLIENT_SCAN_NOT_DECISIVE
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
``````

## Human result

Server:        PZMF_B42_DRUMAP_BASELINE_001
Mods:          Mods=$ReferenceMapId
WorkshopItems: WorkshopItems=$ReferenceWorkshopId
Map:           Map=$ReferenceMapId;Muldraugh, KY

Human visual:
- $ReferenceMapId worked.
- Player spawned into a real built Drummondville/Dru_map-looking world.
- Roads and houses were visible.
- This was NOT the fallback forest world.
- Game reached multiplayer.

Runtime evidence:
- Workshop ID $ReferenceWorkshopId reached Installed / Ready state.
- $ReferenceMapId loaded.
- Real lotheader/meta files referenced in log (e.g. 43_30.lotheader).
- Player data received from the server.
- Game Mode: Multiplayer.

## Old analyzer result

MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY

The client log printed:
  Looking in these map folders:
  <End of map-folders list>

This classification was INCOMPLETE. The runtime succeeded despite empty scan.

## Corrected classification

MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS

The empty printed client map-folder scan is NOT decisive in Build 42 coop/server.
Runtime success is determined by stronger multi-signal evidence.

## What this means for PZMapForge

The $CandidateMapId candidate has NOT produced:
- Workshop ID Installed / Ready state
- lotheader/meta load evidence
- Player entry into a custom-map built world

The PZMapForge candidate still needs runtime activation.
Next: investigate Workshop-style activation contract.
"@

Set-Content -Path $summaryPath -Value $summaryContent -Encoding ASCII
Write-Output "Wrote: MAP_7Q_DRUMAP_BASELINE_RESULT_SUMMARY.md"

# ---------------------------------------------------------------------------
# MAP_7Q_ANALYZER_EVIDENCE_MODEL.md
# ---------------------------------------------------------------------------

$evidenceModelPath = Join-Path $Output 'MAP_7Q_ANALYZER_EVIDENCE_MODEL.md'
$evidenceModelContent = @"
# MAP-7Q: Analyzer Evidence Model

## Old model (MAP-7P and earlier)

Decision signal: map_folders_list_empty=true/false

Shortcoming: In Build 42 coop/server, Workshop map mods can succeed even
when the printed client map-folder scan list is empty.

## New model (MAP-7Q)

For -VariantLabel DruMapBaseline and -ExpectedMapId ${ReferenceMapId}:

### Runtime success classification: MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS

Fires when ALL of the following are present:
1. (workshop_installed_seen OR workshop_ready_seen)
2. expected_mod_loaded
3. player_data_received AND game_loading_completed
4. (multiplayer_reached OR lotheader_meta_evidence_found)

Even if map_folders_list_empty=true.

### Fallback classifications (backward compatible)

MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND:
  Non-empty scan + expected map ID in folder list + no runtime success evidence.

MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY:
  Empty scan + no runtime success evidence.

### New report fields

| Field | Meaning |
|---|---|
| expected_mod_loaded | "loading $ReferenceMapId" detected |
| workshop_id_3355966216_seen | "$ReferenceWorkshopId" in log |
| workshop_download_seen | Workshop download signal |
| workshop_installed_seen | Workshop Installed signal |
| workshop_ready_seen | Workshop Ready signal |
| multiplayer_reached | Game Mode: Multiplayer detected |
| lotheader_meta_evidence_found | .lotheader in log |
| lotheader_meta_paths_or_names | Unique lotheader file names |
| runtime_success_evidence_found | Multi-signal composite |
| empty_client_map_folder_scan_decisive | false when runtime success overrides |
| visual_confirmation_required | true for DruMapBaseline |

## Evidence gate for PZMapForge binary writer work

Until PZMapForge candidate produces:
  lotheader/meta load evidence (lotheader_meta_evidence_found=true)
  referencing PZMapForge lotheader files

...the binary writer quality (LOTH/LOTP/chunkdata format) is not the
active blocker. Runtime activation / mounting is the priority.

## Claim boundary

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
``````
"@

Set-Content -Path $evidenceModelPath -Value $evidenceModelContent -Encoding ASCII
Write-Output "Wrote: MAP_7Q_ANALYZER_EVIDENCE_MODEL.md"

# ---------------------------------------------------------------------------
# MAP_7Q_WORKSHOP_STYLE_ACTIVATION_HYPOTHESES.md
# ---------------------------------------------------------------------------

$hypothesesPath = Join-Path $Output 'MAP_7Q_WORKSHOP_STYLE_ACTIVATION_HYPOTHESES.md'
$hypothesesContent = @"
# MAP-7Q: Workshop-Style Activation Hypotheses

## Context

Dru_map reached runtime success via the Workshop subscription/download flow
(WorkshopItems=$ReferenceWorkshopId). The $CandidateMapId candidate
uses local loose-mod placement only, without a Workshop ID or Steam download.

The hypothesis is that Build 42 multiplayer server requires Workshop-activated
mods for IsoMetaGrid map discovery.

## Hypotheses

### H1: WorkshopItems= is required for IsoMetaGrid map discovery (LIKELY)

Adding WorkshopItems=<ID> to the server ini causes PZ to mount the mod
through the Workshop runtime path, which registers the map folder with
IsoMetaGrid. Without WorkshopItems=, the mod loads but does not get mounted.

Test: Add a fake WorkshopItems= value to the PZMapForge candidate server wiring
and observe whether IsoMetaGrid behavior changes. This does not require a real
Workshop upload.

### H2: PZ requires Workshop-downloaded files in the Workshop content path (POSSIBLE)

The map mod must be physically located in the Steam Workshop content directory
(steamapps/workshop/content/108600/<ID>/) for IsoMetaGrid to mount it.
Local loose mods in the mods/ folder are not scanned by the server's map
discovery path.

Test: Copy the PZMapForge candidate to a local Workshop content path and
observe behavior. HUMAN-ONLY.

### H3: WorkshopItems= without a real Workshop download triggers a different path (UNCERTAIN)

PZ may attempt to find Workshop content by ID. If no Workshop content exists
for that ID, the mod may silently fail to mount. The candidate would need
either a real Steam Workshop upload or a local Workshop content directory
simulation.

Note: A private/unlisted Workshop upload is a separate human-approved task.
It is NOT automatic and NOT performed by this script.

### H4: Local mods ARE mountable but require a different mod folder layout (LESS LIKELY)

The local mods/ path does support IsoMetaGrid discovery, but the required
layout differs from what PZMapForge has tested. This is less likely given
all nine layout variants A-I were exhausted.

## Evidence gate

If any hypothesis test produces:
  lotheader_meta_evidence_found=true
  referencing $CandidateMapId lotheader files

Then binary writer quality becomes the next investigation focus.
Until then, runtime activation / mounting is the priority.

## No automatic Workshop upload

A Workshop upload is a human decision requiring:
- Steam developer account access
- Explicit operator approval
- A separate MAP task assignment

Claude does NOT perform Workshop uploads automatically.
This script does NOT upload anything.

## Claim boundary

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````
"@

Set-Content -Path $hypothesesPath -Value $hypothesesContent -Encoding ASCII
Write-Output "Wrote: MAP_7Q_WORKSHOP_STYLE_ACTIVATION_HYPOTHESES.md"

# ---------------------------------------------------------------------------
# MAP_7Q_NEXT_DECISION_TREE.md
# ---------------------------------------------------------------------------

$decisionTreePath = Join-Path $Output 'MAP_7Q_NEXT_DECISION_TREE.md'
$decisionTreeContent = @"
# MAP-7Q: Next Decision Tree

## Current state

``````text
MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS
EMPTY_CLIENT_SCAN_NOT_DECISIVE
VARIANTS_ABCDEFGHI_EXHAUSTED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

The discriminator: Workshop-activated Dru_map succeeds. Local-only PZMapForge
candidate does not reach lotheader/meta load evidence.

## Decision tree

### Branch 1: PZMapForge candidate cannot use WorkshopItems

Result: Local mod activation is insufficient for Build 42 map discovery.

Implication: A Workshop upload or local Workshop content path simulation
is required before the candidate can be mounted by IsoMetaGrid.

Next human action:
  Evaluate whether a private/unlisted Workshop upload is acceptable.
  This is a separate human-approved task. NOT automatic.
  If not acceptable, investigate whether a local Workshop content path
  simulation can substitute for real Workshop activation.

### Branch 2: WorkshopItems= wiring change causes lotheader/meta evidence

Evidence: lotheader_meta_evidence_found=true in PZMapForge candidate log.
  AND lotheader/meta paths reference $CandidateMapId lotheader files.

Implication: Runtime activation was the blocker. Now that the map folder
is discovered, binary writer quality becomes the investigation focus.

Next task: Record the lotheader/meta evidence stage. Begin LOTH/LOTP/chunkdata
format validation against the known-working reference.

### Branch 3: WorkshopItems= wiring change causes map folder scan FOUND

Evidence: pzmapforge candidate appears in IsoMetaGrid map-folder scan list.
  map_folders_list_empty=false AND candidate ID in folder list.

Implication: The runtime activation is working. But lotheader files may
still need to be validated. This is progress beyond variants A-I.

Next task: Record MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND equivalent
for the PZMapForge candidate. Proceed to lotheader file validation.

### Branch 4: No change despite WorkshopItems= wiring

Result: The candidate still does not mount even with WorkshopItems= present.

Implication: Either Workshop content must be in the Steam Workshop directory,
or the server-side map discovery path is different from what we tested.

Next task: Investigate server-side logs for map discovery evidence.
Inspect whether the Workshop content path (not just mods/) is scanned.

### Branch 5: PZMapForge candidate reaches lotheader/meta stage but binary fails

Evidence: lotheader_meta_evidence_found=true, but java.io.EOFException or
similar lotheader rejection appears.

Implication: The runtime activation gate is passed. The LOTH/LOTP/chunkdata
binary format is now the active blocker.

Next task: Resume LOTH/LOTP/chunkdata binary format investigation.
This was previously blocked pending runtime activation confirmation.

## Non-claims

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_PZ_LOAD_TEST_PERFORMED_BY_CLAUDE
``````

No playable PZMapForge export is claimed from any branch above.
All PZ operations are HUMAN-ONLY.
"@

Set-Content -Path $decisionTreePath -Value $decisionTreeContent -Encoding ASCII
Write-Output "Wrote: MAP_7Q_NEXT_DECISION_TREE.md"

# ---------------------------------------------------------------------------
# MAP_7Q_RUNTIME_ACTIVATION_PACKET.md (main packet doc)
# ---------------------------------------------------------------------------

$packetPath = Join-Path $Output 'MAP_7Q_RUNTIME_ACTIVATION_PACKET.md'
$packetContent = @"
# MAP-7Q: Runtime Activation Next-Step Packet

``````text
MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS
EMPTY_CLIENT_SCAN_NOT_DECISIVE
DRUMAP_BASELINE_RUNTIME_SUCCESSFUL
VARIANTS_ABCDEFGHI_EXHAUSTED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Purpose

Records the Dru_map baseline runtime success, corrects the evidence model,
and prepares the runtime activation investigation framework.

## Key finding

The MAP-7P analyzer classified the Dru_map baseline as EMPTY (empty client
map-folder scan). The human result was runtime successful: player spawned
into a real built world with roads and houses visible.

Empty client map-folder scan is NOT decisive for Build 42 coop/server
with Workshop-activated mods.

## Dru_map reference

``````text
Reference mod:     $ReferenceMapId
Workshop ID:       $ReferenceWorkshopId
Server wiring:
  Mods=$ReferenceMapId
  WorkshopItems=$ReferenceWorkshopId
  Map=$ReferenceMapId;Muldraugh, KY
``````

## Runtime success evidence signals

``````text
Workshop ID ${ReferenceWorkshopId}: Installed / Ready
$ReferenceMapId loaded
Real lotheader files referenced (43_30.lotheader etc.)
Player data received from the server
Game Mode: Multiplayer
Human visual: built world with roads and houses
``````

## Analyzer update

Classification for DruMapBaseline with runtime evidence:
  MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS

Even if map_folders_list_empty=true.

Backward compatible: MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY still
fires when runtime evidence is absent.

## Next investigation

WorkshopItems=3355966216 flow is likely the discriminator.
$CandidateMapId must reach lotheader/meta load evidence before
binary writer quality becomes the active investigation focus.

No automatic Workshop upload. Any Workshop upload requires explicit
human approval and a separate MAP task.

## Packet files

``````text
MAP_7Q_RUNTIME_ACTIVATION_PACKET.md        (this file)
MAP_7Q_DRUMAP_BASELINE_RESULT_SUMMARY.md
MAP_7Q_ANALYZER_EVIDENCE_MODEL.md
MAP_7Q_NEXT_DECISION_TREE.md
MAP_7Q_WORKSHOP_STYLE_ACTIVATION_HYPOTHESES.md
map7q-preflight.json
map7q-preflight.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
No PZ run performed by script.
No writes to Workshop, mods, Server, or PZ install paths.
``````
"@

Set-Content -Path $packetPath -Value $packetContent -Encoding ASCII
Write-Output "Wrote: MAP_7Q_RUNTIME_ACTIVATION_PACKET.md"

Write-Output ""
Write-Output "MAP-7Q packet complete."
Write-Output "drumap_baseline_runtime_success_model=true"
Write-Output "empty_client_scan_not_decisive=true"
Write-Output "WorkshopItems=$ReferenceWorkshopId"
Write-Output "reference_map_id=$ReferenceMapId"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_SCRIPT"
Write-Output "Done."
