#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7T: Records K002 Workshop activation result and optionally runs
    the runtime payload comparison.

    K002 proves: Workshop item 3740642200 installs/reaches Ready, mod loads,
    but expected-map lotheader/meta evidence is still absent.

    Does NOT run Project Zomboid.
    Does NOT upload to Steam Workshop.
    Does NOT write outside .local/.

    Writes:
      MAP_7T_K002_RESULT_SUMMARY.md
      MAP_7T_NEXT_DECISION_TREE.md
      map7t-preflight.json
      map7t-preflight.md
      (optionally) workshop-runtime-payload-comparison.json / .md

.PARAMETER Output
    Required. Path under .local/.

.PARAMETER CandidateWorkshopRoot
    Optional. If provided and exists, runs runtime payload comparison.

.PARAMETER ReferenceWorkshopRoot
    Optional. If provided and exists, used as reference in comparison.

.PARAMETER CandidateMapId
    Optional. Defaults to pzmapforge_build42_candidate_v4_001.

.PARAMETER ReferenceMapId
    Optional. Defaults to Dru_map.
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateWorkshopRoot = '',
    [string]$ReferenceWorkshopRoot = '',
    [string]$CandidateMapId        = 'pzmapforge_build42_candidate_v4_001',
    [string]$ReferenceMapId        = 'Dru_map'
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

Write-Output "MAP-7T: K002 Record Packet"
Write-Output "Output: $Output"
Write-Output ""

$workshopId        = '3740642200'
$variantResult     = 'MAP7F_VARIANT_W_S_UPLOAD_K002_MAP_FOLDER_SCAN_EMPTY'
$binaryWriterGate  = $true   # gate still closed
$publicPlayable    = $false

# ---------------------------------------------------------------------------
# MAP_7T_K002_RESULT_SUMMARY.md
# ---------------------------------------------------------------------------

$summaryPath = Join-Path $Output 'MAP_7T_K002_RESULT_SUMMARY.md'
$summaryContent = @"
# MAP-7T: K002 Workshop Activation Result Summary

``````text
MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED
K002_WORKSHOP_ITEM_INSTALLED_READY
K002_MOD_LOADED_NO_EXPECTED_MAP_EVIDENCE
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## K002 wiring

``````text
Workshop ID:   $workshopId
Server:        PZMF_B42_WS_CANDIDATE_K_002
Mods:          Mods=$CandidateMapId
WorkshopItems: WorkshopItems=$workshopId
Map:           Map=$CandidateMapId;Muldraugh, KY
``````

## Analyzer result

``````text
classification: $variantResult
candidate_loaded: True
player_data_received: True
game_loading_completed: True
map_folders_list_empty: True
spawn_building_warning: True
public_playable_claim_allowed=false
``````

## Runtime signals confirmed

- Workshop $workshopId downloaded.
- Workshop item state: Installed.
- Workshop item state: Ready.
- Server loaded $CandidateMapId.
- Client Workshop $workshopId Subscribed|Installed.
- Client item state: Ready.
- Client loaded $CandidateMapId.
- Player data received from the server.
- game loading took 43 seconds.
- exited GameLoadingState.
- Game Mode: Multiplayer.

## Runtime signals ABSENT

- IsoMetaGrid map folder scan: EMPTY.
- Expected-map lotheader/meta evidence: ABSENT.
- Visible custom PZMapForge world: ABSENT (fallback forest).

## Interpretation

K002 confirms Workshop activation reaches Installed/Ready and the mod loads.
The candidate does NOT reach expected-map lotheader/meta processing.
Binary writer gate remains closed.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.

## Workshop install path observed

``````text
D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\steamapps\workshop\content\108600\$workshopId
``````

## Next step

Compare the actual downloaded Workshop payload structure against Dru_map's
to identify the structural discriminator.
Use: scripts\inspect-build42-workshop-runtime-payload.ps1
"@

Set-Content -Path $summaryPath -Value $summaryContent -Encoding ASCII
Write-Output "Wrote: MAP_7T_K002_RESULT_SUMMARY.md"

# ---------------------------------------------------------------------------
# MAP_7T_NEXT_DECISION_TREE.md
# ---------------------------------------------------------------------------

$decisionTreePath = Join-Path $Output 'MAP_7T_NEXT_DECISION_TREE.md'
$decisionTreeContent = @"
# MAP-7T: Next Decision Tree

## Current state

``````text
MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED
K002_WORKSHOP_ITEM_INSTALLED_READY
K002_MOD_LOADED_NO_EXPECTED_MAP_EVIDENCE
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

The PZMapForge candidate now activates via Workshop and reaches mod loading.
The gap: it does not reach expected-map lotheader/meta processing.

## Decision tree

### Branch 1: Payload comparison reveals structural discriminator

Run:
``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-workshop-runtime-payload.ps1 ``
    -CandidateWorkshopRoot "D:\...\workshop\content\108600\$workshopId" ``
    -ReferenceWorkshopRoot ".\.local\map7m-packet\reference-known-working-map\Dru_map" ``
    -CandidateMapId $CandidateMapId ``
    -ReferenceMapId $ReferenceMapId ``
    -Output .\.local\map7t-comparison
``````

Key fields to compare:
  fields_in_reference_not_candidate  -- structural gaps
  lotheader count difference         -- binary file presence gap
  common/mod.info presence gap       -- mod discovery path difference

If comparison reveals gap:
  Address the specific structural discriminator.
  Upload corrected candidate.
  Retest.

### Branch 2: Payload comparison shows layouts match

If both payloads have identical layout but candidate still fails:
  The discriminator may be in binary file format, not structure.
  At this point, binary writer gate condition is partially met.
  Investigate if PZ is attempting to read candidate lotheader and failing.
  Look for java.io.EOFException or similar in server-side logs.

### Branch 3: Expected-map lotheader/meta evidence appears in next test

Evidence: expected_map_lotheader_meta_evidence_found=true in analyzer output.

Interpretation: Binary writer gate opens. LOTH/LOTP/chunkdata quality is now the blocker.
Next: resume binary format investigation.

### Branch 4: Server-side logs contain more information

Client log may not capture all failure signals. Check server-side logs for:
  IsoMetaGrid map folder scan (server side)
  CellLoader errors for candidate
  lotheader parse errors for candidate

## Binary writer gate

``````text
BINARY_WRITER_GATE_STILL_CLOSED

Opens when:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit java.io.EOFException on candidate lotheader in server log

Do not mutate LOTH/LOTP/chunkdata until gate opens.
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
Write-Output "Wrote: MAP_7T_NEXT_DECISION_TREE.md"

# ---------------------------------------------------------------------------
# Optionally run comparison if both roots exist
# ---------------------------------------------------------------------------

$comparisonRan = $false
if ($CandidateWorkshopRoot -ne '' -and $ReferenceWorkshopRoot -ne '' -and
    (Test-Path -LiteralPath $CandidateWorkshopRoot) -and
    (Test-Path -LiteralPath $ReferenceWorkshopRoot)) {

    Write-Output "Running runtime payload comparison..."
    $compScript = Join-Path $repoRoot 'scripts\inspect-build42-workshop-runtime-payload.ps1'
    & powershell -ExecutionPolicy Bypass -File $compScript `
        -CandidateWorkshopRoot $CandidateWorkshopRoot `
        -ReferenceWorkshopRoot $ReferenceWorkshopRoot `
        -CandidateMapId $CandidateMapId `
        -ReferenceMapId $ReferenceMapId `
        -Output $Output

    if ($LASTEXITCODE -eq 0) { $comparisonRan = $true }
} else {
    Write-Output "Comparison skipped: roots not provided or not found."
    Write-Output "  CandidateWorkshopRoot: $CandidateWorkshopRoot"
    Write-Output "  ReferenceWorkshopRoot: $ReferenceWorkshopRoot"
    Write-Output "  Run manually with -CandidateWorkshopRoot and -ReferenceWorkshopRoot to compare."
}

# ---------------------------------------------------------------------------
# Preflight JSON
# ---------------------------------------------------------------------------

$preflightJsonPath = Join-Path $Output 'map7t-preflight.json'
$preflight = [ordered]@{
    schema                           = 'pzmapforge.map7t-preflight.v0.1'
    k002_workshop_id                 = $workshopId
    k002_result                      = $variantResult
    k002_workshop_installed_ready    = $true
    k002_mod_loaded                  = $true
    k002_expected_map_lotheader_evidence = $false
    binary_writer_gate_closed        = $binaryWriterGate
    public_playable_claim_allowed    = $publicPlayable
    load_test_performed_by_script    = $false
    no_binary_writer_changes         = $true
    comparison_ran                   = $comparisonRan
}
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7t-preflight.json"

$preflightMdPath = Join-Path $Output 'map7t-preflight.md'
$preflightMdContent = @"
# MAP-7T Preflight

``````text
MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED
K002_WORKSHOP_ITEM_INSTALLED_READY
K002_MOD_LOADED_NO_EXPECTED_MAP_EVIDENCE
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
k002_workshop_id:                 $workshopId
k002_result:                      $variantResult
k002_workshop_installed_ready:    true
k002_mod_loaded:                  true
k002_expected_map_lotheader_evidence: false
binary_writer_gate_closed:        true
public_playable_claim_allowed:    false
``````
"@
Set-Content -Path $preflightMdPath -Value $preflightMdContent -Encoding ASCII
Write-Output "Wrote: map7t-preflight.md"

Write-Output ""
Write-Output "MAP-7T packet complete."
Write-Output "k002_workshop_id=$workshopId"
Write-Output "k002_result=$variantResult"
Write-Output "binary_writer_gate_closed=true"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
