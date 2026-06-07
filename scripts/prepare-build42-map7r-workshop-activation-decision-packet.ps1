#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7R: Generates the Workshop activation decision packet.

    Records Variant J failure and prepares the real candidate Workshop-style
    activation decision framework. Does not upload anything. Does not run PZ.
    Does not write to Workshop, mods, Server, or PZ install paths.

    Writes under -Output (must be under .local):
      MAP_7R_WORKSHOP_ACTIVATION_DECISION_PACKET.md
      MAP_7R_VARIANT_J_RESULT_SUMMARY.md
      MAP_7R_NEXT_DECISION_TREE.md
      MAP_7R_PRIVATE_WORKSHOP_UPLOAD_REQUIREMENTS.md
      MAP_7R_NO_MORE_STATIC_LAYOUT_TESTS.md
      map7r-preflight.json
      map7r-preflight.md

.PARAMETER Output
    Path under .local/ for all packet outputs.

.PARAMETER CandidateMapId
    Optional. Defaults to pzmapforge_build42_candidate_v4_001.

.PARAMETER BorrowedWorkshopId
    Optional. Defaults to 3355966216.

.EXAMPLE
    powershell -ExecutionPolicy Bypass `
        -File .\scripts\prepare-build42-map7r-workshop-activation-decision-packet.ps1 `
        -Output .\.local\map7r-decision-packet
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateMapId      = 'pzmapforge_build42_candidate_v4_001',
    [string]$BorrowedWorkshopId  = '3355966216'
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

Write-Output "MAP-7R: Workshop Activation Decision Packet"
Write-Output "Output: $Output"
Write-Output ""

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

$variantJResult                      = 'MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT'
$borrowedWorkshopItemsTriggerInsufficient = $true
$staticVariantsABCDEFGHIExhausted    = $true
$noMoreStaticLayoutTests             = $true
$publicPlayableClaimAllowed          = $false

# ---------------------------------------------------------------------------
# map7r-preflight.json
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema                               = 'pzmapforge.map7r-preflight.v0.1'
    variant_j_result                     = $variantJResult
    borrowed_workshopitems_trigger_insufficient = $borrowedWorkshopItemsTriggerInsufficient
    static_variants_abcdefghi_exhausted  = $staticVariantsABCDEFGHIExhausted
    no_more_static_layout_tests          = $noMoreStaticLayoutTests
    candidate_map_id                     = $CandidateMapId
    borrowed_workshop_id                 = $BorrowedWorkshopId
    public_playable_claim_allowed        = $publicPlayableClaimAllowed
    load_test_performed_by_script        = $false
    no_automatic_workshop_upload         = $true
    no_binary_writer_changes             = $true
    no_pz_run                            = $true
    no_workshop_writes                   = $true
    no_mods_folder_writes                = $true
}

$preflightJsonPath = Join-Path $Output 'map7r-preflight.json'
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7r-preflight.json"

# ---------------------------------------------------------------------------
# map7r-preflight.md
# ---------------------------------------------------------------------------

$preflightMdPath = Join-Path $Output 'map7r-preflight.md'
$preflightMdContent = @"
# MAP-7R Preflight

## State

``````text
MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED
STATIC_VARIANTS_ABCDEFGHI_EXHAUSTED
NO_MORE_STATIC_LAYOUT_TESTS
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Preflight fields

``````text
variant_j_result:                     $variantJResult
borrowed_workshopitems_trigger_insufficient: true
static_variants_abcdefghi_exhausted:  true
no_more_static_layout_tests:          true
``````

## Safety constraints

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
``````
"@

Set-Content -Path $preflightMdPath -Value $preflightMdContent -Encoding ASCII
Write-Output "Wrote: map7r-preflight.md"

# ---------------------------------------------------------------------------
# MAP_7R_VARIANT_J_RESULT_SUMMARY.md
# ---------------------------------------------------------------------------

$summaryPath = Join-Path $Output 'MAP_7R_VARIANT_J_RESULT_SUMMARY.md'
$summaryContent = @"
# MAP-7R: Variant J Result Summary

``````text
MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
``````

## Wiring

``````text
Server:        PZMF_B42_CANDIDATE_WS_TRIGGER_J_001
Candidate:     $CandidateMapId
Mods:          Mods=$CandidateMapId
WorkshopItems: WorkshopItems=$BorrowedWorkshopId
Map:           Map=$CandidateMapId;Muldraugh, KY
``````

## Human result

Forest/fallback world appeared.
No built PZMapForge world.
Not a successful custom map load.

## Log evidence

- Workshop ID $BorrowedWorkshopId reached Subscribed / Installed / Ready.
- PZMapForge candidate loaded: loading $CandidateMapId
- IsoMetaGrid map folder scan: empty.
- Player data received. Game Mode: Multiplayer.
- Generic CellLoader/lotheader lines appeared (Muldraugh/vanilla context).
- No $CandidateMapId lotheader/meta evidence.
- Muldraugh mannequin-zone warning: fallback noise.

## Correct interpretation

WorkshopItems=$BorrowedWorkshopId is bound to Dru_map's content.
Adding it registered Dru_map's runtime path, not PZMapForge's.
WorkshopItems= alone does not mount arbitrary local loose mods.
The PZMapForge candidate still only loads at the mod registration level.

## What is exhausted

``````text
Static layout variants A through I: EXHAUSTED
Borrowed WorkshopItems trigger J:   EXHAUSTED
``````

## Next step

Real candidate Workshop-style activation.
Requires human-approved private/unlisted Workshop upload.
Not automatic. Requires explicit operator approval and a separate MAP task.
"@

Set-Content -Path $summaryPath -Value $summaryContent -Encoding ASCII
Write-Output "Wrote: MAP_7R_VARIANT_J_RESULT_SUMMARY.md"

# ---------------------------------------------------------------------------
# MAP_7R_NO_MORE_STATIC_LAYOUT_TESTS.md
# ---------------------------------------------------------------------------

$noStaticPath = Join-Path $Output 'MAP_7R_NO_MORE_STATIC_LAYOUT_TESTS.md'
$noStaticContent = @"
# MAP-7R: No More Static Layout Tests

``````text
STATIC_VARIANTS_ABCDEFGHI_EXHAUSTED
BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED
NO_MORE_STATIC_LAYOUT_TESTS
``````

## What has been tested

All nine static layout variants (A through I) covering:
- Map= line ordering variants
- Root vs versioned vs common media/maps/ layouts
- dual mod.info layouts
- mod.info map= field variations
- common/media/maps/ Dru_map-aligned layout

Plus: Variant J (borrowed WorkshopItems= trigger).

All produced MAP_FOLDER_SCAN_EMPTY or the equivalent runtime failure.

## Why further static tests are not productive

The root cause is runtime activation, not static package layout.
PZMapForge must reach Workshop-style runtime activation (Installed/Ready
state, runtime mount path registration) for IsoMetaGrid to discover
the map folder. Static layout changes cannot address this.

## What to do instead

Wait for operator approval of a private/unlisted Workshop upload,
which is the next logical diagnostic step.
If the upload path is not available, pause map-loading investigation
and focus on other PZMapForge product areas.

## Binary writer gate

Do not investigate LOTH/LOTP/chunkdata binary format quality until
the candidate reaches expected-map lotheader/meta evidence.
No binary writer changes from this task.
"@

Set-Content -Path $noStaticPath -Value $noStaticContent -Encoding ASCII
Write-Output "Wrote: MAP_7R_NO_MORE_STATIC_LAYOUT_TESTS.md"

# ---------------------------------------------------------------------------
# MAP_7R_PRIVATE_WORKSHOP_UPLOAD_REQUIREMENTS.md
# ---------------------------------------------------------------------------

$uploadReqPath = Join-Path $Output 'MAP_7R_PRIVATE_WORKSHOP_UPLOAD_REQUIREMENTS.md'
$uploadReqContent = @"
# MAP-7R: Private Workshop Upload Requirements

## Context

Real candidate Workshop-style activation requires the PZMapForge candidate
to have its OWN Steam Workshop ID. Borrowing Dru_map's Workshop ID
(WorkshopItems=$BorrowedWorkshopId) was insufficient because that ID
is bound to Dru_map's Steam Workshop content.

## HUMAN DECISION REQUIRED

A Steam Workshop upload is:
- A human decision requiring explicit operator approval.
- NOT automatic and NOT performed by Claude.
- NOT authorized by this task or any previous MAP task.
- A separate MAP task must be explicitly created for this work.

## Requirements for a future Workshop upload task

If the operator approves, a future task should:

1. Stage the PZMapForge candidate as a Workshop package under .local/.
   Claude may prepare the staged files and checklist.

2. The staged package must use its OWN Workshop ID after upload,
   NOT WorkshopItems=$BorrowedWorkshopId.

3. The future server wiring would be:
   ``````ini
   Mods=$CandidateMapId
   WorkshopItems=<PZMapForgeOwnWorkshopId>
   Map=$CandidateMapId;Muldraugh, KY
   ``````

4. Human manually uploads to Steam Workshop (private/unlisted).
   Claude does not perform this step.

5. After upload, human tests the server wiring and captures the log.

6. Analyzer command:
   ``````powershell
   powershell -ExecutionPolicy Bypass ``
       -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
       -LogPath .\.local\map7r-logs\DebugLog-ws-candidate.txt ``
       -Output .\.local\map7r-analysis-ws-candidate ``
       -ExpectedMapId $CandidateMapId ``
       -VariantLabel VariantWSUpload
   ``````

## Success condition

Expected-map lotheader/meta evidence appears in log:
  lotheader_meta_evidence_found=true
  expected_map_lotheader_meta_evidence_found=true

AND/OR: Custom-map built world is visually confirmed (not fallback forest).

## Failure condition

Candidate still only loads as mod and fallback forest appears.
lotheader_meta_evidence_found=false for candidate.

## If success: binary writer becomes the next blocker

If the candidate reaches lotheader/meta evidence, the binary writer
quality (LOTH/LOTP/chunkdata) becomes the next investigation focus.
Binary writer investigation was deferred pending this activation gate.

## No automatic upload

Claude does NOT upload to Steam Workshop.
Any upload requires explicit human approval before Claude begins staging.
"@

Set-Content -Path $uploadReqPath -Value $uploadReqContent -Encoding ASCII
Write-Output "Wrote: MAP_7R_PRIVATE_WORKSHOP_UPLOAD_REQUIREMENTS.md"

# ---------------------------------------------------------------------------
# MAP_7R_NEXT_DECISION_TREE.md
# ---------------------------------------------------------------------------

$decisionTreePath = Join-Path $Output 'MAP_7R_NEXT_DECISION_TREE.md'
$decisionTreeContent = @"
# MAP-7R: Next Decision Tree

## Current state

``````text
MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
STATIC_VARIANTS_ABCDEFGHI_EXHAUSTED
BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED
NO_MORE_STATIC_LAYOUT_TESTS
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

All static layout variants and the borrowed WorkshopItems trigger are exhausted.
The only remaining productive diagnostic is real candidate Workshop-style activation.

## Decision tree

### Branch 1: Operator approves private/unlisted Workshop upload

Action:
  Create a new MAP task for staging and upload preparation.
  Claude stages files and generates human-only upload checklist under .local/.
  Human performs the upload manually.
  Future server wiring:
    Mods=$CandidateMapId
    WorkshopItems=<PZMapForgeOwnWorkshopId>
    Map=$CandidateMapId;Muldraugh, KY

Success condition:
  expected_map_lotheader_meta_evidence_found=true
  OR: built custom world with custom-map content visually confirmed.

If success:
  Binary writer validation becomes the next blocker.
  Resume LOTH/LOTP/chunkdata format investigation.

If failure (fallback forest + no lotheader evidence):
  Investigate server-side logs. Check if Workshop content path vs mods path
  is the discriminator. Consider whether the candidate's binary files are
  the new blocker after activation.

### Branch 2: Operator does not approve Workshop upload

Action:
  Map-loading investigation pauses at this branch.
  No further PZMapForge mod activation work without Workshop upload.
  Focus may shift to other PZMapForge product areas.

This is a valid operational choice. The candidate has reached the limit
of what local-only testing can resolve.

### Branch 3: Expected-map lotheader/meta evidence appears

Evidence: lotheader_meta_evidence_found=true AND expected map ID in lotheader paths.

Interpretation:
  The activation gate is cleared. Binary writer quality is now the focus.
  Investigate whether LOTH/LOTP/chunkdata files are accepted or rejected.

Next task:
  Record the exact lotheader stage. Compare against Dru_map reference.
  Determine if the binary format is the active blocker.

### Branch 4: Expected-map lotheader stage fails explicitly

Evidence: java.io.EOFException or similar lotheader rejection for the candidate.

Interpretation:
  Runtime activation is working. Binary format is the blocker.
  LOTH/LOTP/chunkdata investigation is now appropriate.

## Binary writer gate

``````text
BINARY_WRITER_GATE_OPEN_WHEN:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit binary format failure (EOFException on candidate lotheader)

BINARY_WRITER_GATE_CLOSED_UNTIL_THEN:
  Do not mutate LOTH/LOTP/chunkdata.
  No binary writer changes from this task.
``````

## Non-claims

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
``````
"@

Set-Content -Path $decisionTreePath -Value $decisionTreeContent -Encoding ASCII
Write-Output "Wrote: MAP_7R_NEXT_DECISION_TREE.md"

# ---------------------------------------------------------------------------
# MAP_7R_WORKSHOP_ACTIVATION_DECISION_PACKET.md (main packet doc)
# ---------------------------------------------------------------------------

$packetPath = Join-Path $Output 'MAP_7R_WORKSHOP_ACTIVATION_DECISION_PACKET.md'
$packetContent = @"
# MAP-7R: Workshop Activation Decision Packet

``````text
MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED
STATIC_VARIANTS_ABCDEFGHI_EXHAUSTED
NO_MORE_STATIC_LAYOUT_TESTS
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Purpose

Records Variant J failure (borrowed WorkshopItems=$BorrowedWorkshopId insufficient)
and prepares the Workshop activation decision framework.

## Key finding

WorkshopItems=$BorrowedWorkshopId is bound to Dru_map's Steam Workshop content.
Adding it to the server ini activated Dru_map's runtime mount path, not
the PZMapForge candidate's path. The PZMapForge candidate still only loads
at the mod registration level.

No further static layout changes or borrowed Workshop IDs should be attempted.
No binary writer changes yet -- binary writer gate is still closed.

## Exhausted paths

``````text
Static layout variants A through I: EXHAUSTED
Borrowed WorkshopItems trigger J:   EXHAUSTED
``````

## Next productive step

Real candidate Workshop-style activation (private/unlisted Workshop upload).

Requires explicit operator approval. NOT automatic.
A separate MAP task must be created for this work.

Future server wiring (after upload):
``````ini
Mods=$CandidateMapId
WorkshopItems=<PZMapForgeOwnWorkshopId>
Map=$CandidateMapId;Muldraugh, KY
``````

Future success condition:
  expected_map_lotheader_meta_evidence_found=true
  OR: built custom world visually confirmed.

## Packet files

``````text
MAP_7R_WORKSHOP_ACTIVATION_DECISION_PACKET.md  (this file)
MAP_7R_VARIANT_J_RESULT_SUMMARY.md
MAP_7R_NEXT_DECISION_TREE.md
MAP_7R_PRIVATE_WORKSHOP_UPLOAD_REQUIREMENTS.md
MAP_7R_NO_MORE_STATIC_LAYOUT_TESTS.md
map7r-preflight.json
map7r-preflight.md
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
No PZ run performed by script.
No writes to Workshop, mods, Server, or PZ install paths.
Future private/unlisted Workshop upload requires human approval.
``````
"@

Set-Content -Path $packetPath -Value $packetContent -Encoding ASCII
Write-Output "Wrote: MAP_7R_WORKSHOP_ACTIVATION_DECISION_PACKET.md"

Write-Output ""
Write-Output "MAP-7R packet complete."
Write-Output "variant_j_result=$variantJResult"
Write-Output "borrowed_workshopitems_trigger_insufficient=$borrowedWorkshopItemsTriggerInsufficient"
Write-Output "WorkshopItems=$BorrowedWorkshopId was insufficient for local loose mod"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_SCRIPT"
Write-Output "NO_AUTOMATIC_WORKSHOP_UPLOAD"
Write-Output "NO_BINARY_WRITER_CHANGES"
Write-Output "Done."
