#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7W: Generates the runtime map-registration diagnostic packet.

    Optionally runs the registration contract inspector if mod roots exist.
    Always writes packet docs regardless of whether roots are available.

    Does NOT run Project Zomboid.
    Does NOT upload to Steam Workshop.
    Does NOT write outside .local/.

.PARAMETER Output
    Required. Path under .local/.

.PARAMETER CandidateModRoot
    Optional. If provided and exists, runs inspector.

.PARAMETER ReferenceModRoot
    Optional. If provided and exists, used as reference in inspector.

.PARAMETER CandidateLogsRoot
    Optional. Passed to inspector for log parsing.

.PARAMETER ReferenceLogsRoot
    Optional. Passed to inspector for log parsing.

.PARAMETER CandidateMapId
    Optional. Defaults to pzmapforge_build42_candidate_v4_001.

.PARAMETER ReferenceMapId
    Optional. Defaults to Dru_map.
#>

param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$CandidateModRoot  = '',
    [string]$ReferenceModRoot  = '',
    [string]$CandidateLogsRoot = '',
    [string]$ReferenceLogsRoot = '',
    [string]$CandidateMapId    = 'pzmapforge_build42_candidate_v4_001',
    [string]$ReferenceMapId    = 'Dru_map'
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

Write-Output "MAP-7W: Runtime Registration Diagnostic Packet"
Write-Output "Output: $Output"
Write-Output ""

$candidateWorkshopId = '3740642200'
$referenceWorkshopId = '3355966216'

# ---------------------------------------------------------------------------
# Try to run inspector
# ---------------------------------------------------------------------------

$inspectorRan               = $false
$runtimeMountDiscriminator  = $null
$mapBinDiscriminator        = $null
$refFilesMissingInCand      = $null

$inspScript = Join-Path $repoRoot 'scripts\inspect-build42-map-registration-contract.ps1'
$inspOut    = Join-Path $Output 'inspector-output'

if ($CandidateModRoot -ne '' -and $ReferenceModRoot -ne '' -and
    (Test-Path -LiteralPath $CandidateModRoot) -and
    (Test-Path -LiteralPath $ReferenceModRoot)) {

    Write-Output "Running registration contract inspector..."
    New-Item -ItemType Directory -Force -Path $inspOut | Out-Null

    & powershell -ExecutionPolicy Bypass -File $inspScript `
        -CandidateModRoot $CandidateModRoot `
        -ReferenceModRoot $ReferenceModRoot `
        -CandidateMapId $CandidateMapId `
        -ReferenceMapId $ReferenceMapId `
        -CandidateLogsRoot $CandidateLogsRoot `
        -ReferenceLogsRoot $ReferenceLogsRoot `
        -Output $inspOut

    if ($LASTEXITCODE -eq 0) {
        $inspectorRan = $true
        $inspJson     = Get-Content (Join-Path $inspOut 'map-registration-contract.json') -Raw | ConvertFrom-Json
        $runtimeMountDiscriminator = [bool]$inspJson.runtime_mount_discriminator_found
        $mapBinDiscriminator       = [bool]$inspJson.map_bin_discriminator
        $refFilesMissingInCand     = [int]$inspJson.reference_files_missing_in_candidate_count
    }
} else {
    Write-Output "Inspector skipped: roots not supplied or not found."
    Write-Output "  CandidateModRoot: $CandidateModRoot"
    Write-Output "  ReferenceModRoot: $ReferenceModRoot"
    Write-Output "Run manually with -CandidateModRoot and -ReferenceModRoot to compare."
}

# ---------------------------------------------------------------------------
# MAP_7W_RUNTIME_REGISTRATION_PACKET.md (main)
# ---------------------------------------------------------------------------

$packetPath = Join-Path $Output 'MAP_7W_RUNTIME_REGISTRATION_PACKET.md'
Set-Content -Path $packetPath -Value @"
# MAP-7W: Runtime Map Registration / Map Folder Mounting Packet

``````text
MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED
BINARY_FORMAT_INVESTIGATION_PAUSED
RUNTIME_MAP_REGISTRATION_IS_ACTIVE_BRANCH
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
``````

## Purpose

Map registration contract inspector added to compare the exact file set,
mod.info/map.info key/value content, and spawnpoints contract between
the PZMapForge candidate and the Dru_map known-working reference.

## Source

MAP-7V (commit 083277d) established:
- Binary presence (K004) and binary absence (K006) both produce fallback forest.
- Binary format investigation is paused.
- Active branch: runtime map registration / map folder mounting.

## Inspector

``````powershell
powershell -ExecutionPolicy Bypass ``
    -File .\scripts\inspect-build42-map-registration-contract.ps1 ``
    -CandidateModRoot "<candidate mod root>" ``
    -ReferenceModRoot "<reference mod root>" ``
    -CandidateMapId $CandidateMapId ``
    -ReferenceMapId $ReferenceMapId ``
    -Output .\.local\map7w-packet\inspector-output
``````

## Safety

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
No PZ run performed by script.
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7W_RUNTIME_REGISTRATION_PACKET.md"

# ---------------------------------------------------------------------------
# MAP_7W_FILE_SET_DISCRIMINATORS.md
# ---------------------------------------------------------------------------

$fileSetPath = Join-Path $Output 'MAP_7W_FILE_SET_DISCRIMINATORS.md'
Set-Content -Path $fileSetPath -Value @"
# MAP-7W: File Set Discriminators

``````text
RUNTIME_MAP_REGISTRATION_IS_ACTIVE_BRANCH
BINARY_FORMAT_INVESTIGATION_PAUSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Hypotheses

H1 (map.bin): map folders in known-working mods may include map.bin or
a similar registration/index file that PZMapForge has never generated.
This file may be required for IsoMetaGrid to recognize the map folder.
Status: Hypothesis only. Inspector must compare file sets from local reference.

H2 (mod.info values): field-presence comparison matched, but exact values
may differ. The id= or other field value might require exact content.

H3 (map.info values): zoom and other fields were aligned manually in K004
but may still differ from the exact reference values.

H4 (Workshop path structure): actual Workshop-downloaded payload path may
differ from the local reference copy used in comparison.

H5 (server-side logs): server logs may show more IsoMetaGrid evidence.

H6 (Map= server.ini): may require registered map folder value.

H7 (spawnregions.lua): server-side spawn region registration may be needed.

## Inspector output

$(if ($inspectorRan) {
"Inspector ran. See inspector-output/ for full results.
runtime_mount_discriminator_found: $runtimeMountDiscriminator
map_bin_discriminator:             $mapBinDiscriminator
reference_files_missing_in_candidate: $refFilesMissingInCand"
} else {
"Inspector did not run (roots not provided or missing).
Provide -CandidateModRoot and -ReferenceModRoot to generate comparison."
})

## Non-claims

``````text
map.bin is NOT declared as the cause until the inspector proves it.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7W_FILE_SET_DISCRIMINATORS.md"

# ---------------------------------------------------------------------------
# MAP_7W_LOG_EVIDENCE_PLAN.md
# ---------------------------------------------------------------------------

$logEvidencePath = Join-Path $Output 'MAP_7W_LOG_EVIDENCE_PLAN.md'
Set-Content -Path $logEvidencePath -Value @"
# MAP-7W: Log Evidence Plan

## Purpose

Client logs have shown empty map-folder scan. Server-side logs may reveal
more IsoMetaGrid evidence about why the PZMapForge map folder is not mounted.

## Evidence to capture

1. Server log immediately after K004/K006 tests (not client log).
2. IsoMetaGrid mount attempt -- does the server try to scan the candidate folder?
3. Any error after the IsoMetaGrid scan for the candidate mod.
4. map.bin or registration file missing error, if any.
5. Difference between how Dru_map and PZMapForge appear in server IsoMetaGrid scan.

## Log capture instructions (HUMAN-ONLY)

Server log location (typically):
  C:\Users\Palmacede\Zomboid\Logs\server\<ServerName>_DebugLog.txt
or
  C:\Users\Palmacede\Zomboid\server\<ServerName>\DebugLog.txt

Copy to .local before analyzing:
  New-Item -ItemType Directory -Force -Path ".\.local\map7w-logs"
  Copy-Item "<server log path>" ".\.local\map7w-logs\server-debug.txt"

Analyzer command:
  ``````powershell
  powershell -ExecutionPolicy Bypass ``
      -File .\scripts\inspect-build42-map7d-load-result.ps1 ``
      -LogPath .\.local\map7w-logs\server-debug.txt ``
      -Output .\.local\map7w-analysis ``
      -ExpectedMapId $CandidateMapId ``
      -VariantLabel VariantServerLog
  ``````

## Non-claims

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_SCRIPT
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7W_LOG_EVIDENCE_PLAN.md"

# ---------------------------------------------------------------------------
# MAP_7W_NEXT_DECISION_TREE.md
# ---------------------------------------------------------------------------

$nextTreePath = Join-Path $Output 'MAP_7W_NEXT_DECISION_TREE.md'
Set-Content -Path $nextTreePath -Value @"
# MAP-7W: Next Decision Tree

## Current state

``````text
MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED
BINARY_FORMAT_INVESTIGATION_PAUSED
RUNTIME_MAP_REGISTRATION_IS_ACTIVE_BRANCH
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Decision tree

### Branch 1: Inspector finds reference_has_map_bin=true, candidate_has_map_bin=false

Generate map.bin for PZMapForge candidate (format TBD from reference inspection).
Update Workshop item. Test.

### Branch 2: Inspector finds map.info or mod.info value differences

Correct the differing values in the PZMapForge candidate.
Update Workshop item. Test.

### Branch 3: Inspector shows exact_file_set_match=true, no discriminator

File sets identical, values identical -- still no map mount.
Next: capture server-side IsoMetaGrid log (not client log).
Inspect server log for the actual mount attempt.

### Branch 4: Server log shows IsoMetaGrid attempting to load candidate

Evidence: candidate map folder name appears in server-side IsoMetaGrid scan.
This changes the picture -- the mount attempt is happening but something else fails.

If lotheader error appears:
  Binary writer gate opens. Resume LOTH/LOTP/chunkdata investigation.

If no lotheader error:
  Continue investigating registration contract (spawnregions.lua, Map= syntax, etc.)

### Branch 5: Server log shows no IsoMetaGrid attempt for candidate

The server is not even trying to mount the candidate map folder.
Investigate: server Map= line, spawnregions.lua registration, Workshop mod discovery.

## Next recommended manual experiment

1. Run: ``scripts\inspect-build42-map-registration-contract.ps1`` with actual mod roots.
2. Capture server-side log from K004 or K006.
3. Check if server log shows IsoMetaGrid attempting to scan candidate folder.

## Binary writer gate

``````text
BINARY_WRITER_GATE_STILL_CLOSED
BINARY_FORMAT_INVESTIGATION_PAUSED

Opens when:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit EOFException on candidate lotheader in log
``````

## Non-claims

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
runtime_map_registration_and_mounting is the active branch.
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7W_NEXT_DECISION_TREE.md"

# ---------------------------------------------------------------------------
# Preflight JSON
# ---------------------------------------------------------------------------

$preflightJsonPath = Join-Path $Output 'map7w-preflight.json'
$preflight = [ordered]@{
    schema                               = 'pzmapforge.map7w-preflight.v0.1'
    source_map7v_commit                  = '083277d'
    binary_format_investigation_paused   = $true
    binary_writer_gate_closed            = $true
    next_branch                          = 'runtime_map_registration_and_mounting'
    candidate_mod_id                     = $CandidateMapId
    reference_mod_id                     = $ReferenceMapId
    candidate_workshop_id                = $candidateWorkshopId
    reference_workshop_id                = $referenceWorkshopId
    inspector_ran                        = $inspectorRan
    runtime_mount_discriminator_found    = $runtimeMountDiscriminator
    map_bin_discriminator                = $mapBinDiscriminator
    reference_files_missing_in_candidate_count = $refFilesMissingInCand
    public_playable_claim_allowed        = $false
    load_test_performed_by_script        = $false
    automatic_workshop_upload_performed  = $false
    binary_writer_changed                = $false
}
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7w-preflight.json"

$preflightMdPath = Join-Path $Output 'map7w-preflight.md'
Set-Content -Path $preflightMdPath -Value @"
# MAP-7W Preflight

``````text
MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED
BINARY_FORMAT_INVESTIGATION_PAUSED
BINARY_WRITER_GATE_STILL_CLOSED
runtime_map_registration_and_mounting is the active branch.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
binary_format_investigation_paused:  true
binary_writer_gate_closed:           true
next_branch:                         runtime_map_registration_and_mounting
candidate_workshop_id:               $candidateWorkshopId
reference_workshop_id:               $referenceWorkshopId
inspector_ran:                       $($inspectorRan.ToString().ToLower())
public_playable_claim_allowed:       false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map7w-preflight.md"

Write-Output ""
Write-Output "MAP-7W packet complete."
Write-Output "inspector_ran=$($inspectorRan.ToString().ToLower())"
Write-Output "next_branch=runtime_map_registration_and_mounting"
Write-Output "binary_writer_gate_closed=true"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "Done."
