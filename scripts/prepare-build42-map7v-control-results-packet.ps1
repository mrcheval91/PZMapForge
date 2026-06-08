#Requires -Version 5.1
<#
.SYNOPSIS
    MAP-7V: Records K004 and K006 control test results and prepares the
    binary gate decision and next-branch documentation.

    Does NOT run Project Zomboid.
    Does NOT upload to Steam Workshop.
    Does NOT write outside .local/.

    Writes:
      MAP_7V_K004_K006_CONTROL_RESULTS.md
      MAP_7V_BINARY_GATE_DECISION.md
      MAP_7V_NEXT_RUNTIME_MOUNTING_BRANCH.md
      map7v-preflight.json
      map7v-preflight.md

.PARAMETER Output
    Required. Path under .local/.
#>

param(
    [Parameter(Mandatory=$true)][string]$Output
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

Write-Output "MAP-7V: K004/K006 Control Results Packet"
Write-Output "Output: $Output"
Write-Output ""

$workshopId     = '3740642200'
$modId          = 'pzmapforge_build42_candidate_v4_001'
$nextBranch     = 'runtime_map_registration_and_mounting'

# ---------------------------------------------------------------------------
# MAP_7V_K004_K006_CONTROL_RESULTS.md
# ---------------------------------------------------------------------------

$resultsPath = Join-Path $Output 'MAP_7V_K004_K006_CONTROL_RESULTS.md'
Set-Content -Path $resultsPath -Value @"
# MAP-7V: K004 / K006 Control Results

``````text
MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED
MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED
BINARY_FORMAT_INVESTIGATION_PAUSED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## K004: Coordinate-aligned

Workshop $workshopId | Mod $modId
Payload: 35_27.lotheader, world_35_27.lotpack, chunkdata_35_27.bin
map.info: zoomX=10505, zoomY=12220, zoomS=14.5
spawnpoints.lua: worldX=35, worldY=27, posX=246, posY=188

Result:
  Workshop Ready: YES
  Mod loaded: YES
  Spawnpoint honored: YES (warning at 10746,8288,0)
  Visible result: fallback forest
  Expected candidate lotheader evidence: ABSENT
  Map folder scan: EMPTY

## K006: Zero-binary control

Workshop $workshopId | Mod $modId
Payload: mod.info, 42/mod.info, common/media/maps/<MapId>/, map.info, objects.lua, spawnpoints.lua
lotheader count: 0
lotpack count:   0
chunkdata count: 0

Result:
  Workshop Ready: YES
  Mod loaded: YES
  Map folder scan: EMPTY
  Spawn target honored: YES (no room/building at 10746,8288,0)
  Expected candidate lotheader evidence: ABSENT
  Server: SANITY CHECK FAIL

Note: SANITY CHECK FAIL is NOT a binary parse error.
K006 had ZERO PZMapForge binary files. SANITY CHECK FAIL is standard PZ
behavior when spawn infrastructure is absent. It is not caused by
PZMapForge binary format quality.

## Combined conclusion

Binary presence (K004) vs binary absence (K006) produces the same
fallback-forest outcome. Binary format is not the discriminator.
The active gap is runtime map registration / map folder mounting.

PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7V_K004_K006_CONTROL_RESULTS.md"

# ---------------------------------------------------------------------------
# MAP_7V_BINARY_GATE_DECISION.md
# ---------------------------------------------------------------------------

$gatePath = Join-Path $Output 'MAP_7V_BINARY_GATE_DECISION.md'
Set-Content -Path $gatePath -Value @"
# MAP-7V: Binary Gate Decision

``````text
BINARY_WRITER_GATE_STILL_CLOSED
BINARY_FORMAT_INVESTIGATION_PAUSED
no_explicit_binary_parse_error_observed=true
k006_proves_zero_binary_same_result=true
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## Decision

Binary format investigation is paused.

## Evidence basis

K006 proves: zero PZMapForge binary files (lotheader count=0, lotpack count=0,
chunkdata count=0) produces the same result as full binary presence (K004):
  Workshop Ready + mod loaded + spawn honored + fallback forest + empty map scan.

K004 with 35_27.lotheader/lotpack/chunkdata: same fallback forest result.
K006 with zero binary files:                 same fallback forest result.
  Note: K006 produced SANITY CHECK FAIL -- this is NOT a binary parse error.
  SANITY CHECK FAIL occurred with zero PZMapForge binary files present.
  It cannot be attributed to PZMapForge binary format quality.

The fallback forest result and empty map scan were never binary evidence.
They are evidence of missing map registration/mounting, not binary format quality.

## Gate remains closed

No explicit binary parse error (EOFException, LOAD_TEST_FAIL_LOTH, etc.)
has been observed. Without this evidence, investigating binary format details
is misdirected work.

Binary writer gate opens when:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit EOFException/parse error on candidate lotheader in log

## Non-claims

``````text
No binary format success claimed.
No binary format failure (parse error) observed.
No playable export claimed.
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7V_BINARY_GATE_DECISION.md"

# ---------------------------------------------------------------------------
# MAP_7V_NEXT_RUNTIME_MOUNTING_BRANCH.md
# ---------------------------------------------------------------------------

$nextBranchPath = Join-Path $Output 'MAP_7V_NEXT_RUNTIME_MOUNTING_BRANCH.md'
Set-Content -Path $nextBranchPath -Value @"
# MAP-7V: Next Branch — Runtime Map Registration and Mounting

``````text
RUNTIME_MAP_REGISTRATION_IS_NEXT_BRANCH
BINARY_FORMAT_INVESTIGATION_PAUSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

## The gap

Workshop $workshopId activates. Mod $modId loads.
spawnpoints.lua is honored. But the map folder is not mounted by IsoMetaGrid.

## Investigation targets

1. Server-side IsoMetaGrid scan log
   The client log shows empty map folder scan. The server-side log may show
   the actual IsoMetaGrid mount attempt and any errors.

2. mod.info map= field
   Does PZ require a map= field in mod.info that exactly matches the map folder
   name within the common/media/maps/ path?

3. Workshop vs mods/ path scan
   Does IsoMetaGrid scan the Steam Workshop content path (steamapps/workshop/)
   directly, or only the mods/ folder? Does it scan both?

4. spawnregions.lua at server level
   The server _spawnregions.lua may need to register the mod's spawn region.
   This was the blocker in MAP-6P/6Q. Revisit whether this is still relevant
   for Build 42 Workshop-activated mods.

5. server.ini Map= line syntax
   Is ``Map=$modId;Muldraugh, KY`` the correct syntax for a custom Workshop map
   in Build 42 coop server, or does the Map= value need to match a registry?

## Binary writer gate re-check

If investigation reveals the map folder begins mounting but fails at
lotheader parsing, the binary writer gate would open. At that point
LOTH/LOTP/chunkdata format becomes the active blocker.

## Non-claims

``````text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
NO_BINARY_WRITER_CHANGES
``````
"@ -Encoding ASCII
Write-Output "Wrote: MAP_7V_NEXT_RUNTIME_MOUNTING_BRANCH.md"

# ---------------------------------------------------------------------------
# Preflight JSON
# ---------------------------------------------------------------------------

$preflight = [ordered]@{
    schema                                = 'pzmapforge.map7v-preflight.v0.1'
    workshop_id                           = $workshopId
    mod_id                                = $modId
    k004_workshop_ready                   = $true
    k004_mod_loaded                       = $true
    k004_spawnpoints_active               = $true
    k004_coordinate_aligned_binaries_present = $true
    k004_expected_candidate_lotheader_evidence = $false
    k004_visible_result                   = 'fallback_forest'
    k006_workshop_ready                   = $true
    k006_mod_loaded                       = $true
    k006_spawnpoints_active               = $true
    k006_candidate_lotheader_count        = 0
    k006_candidate_lotpack_count          = 0
    k006_candidate_chunkdata_count        = 0
    k006_spawn_target_honored             = $true
    k006_expected_candidate_lotheader_evidence = $false
    binary_writer_gate_closed             = $true
    binary_format_investigation_paused    = $true
    next_branch                           = $nextBranch
    public_playable_claim_allowed         = $false
    load_test_performed_by_script         = $false
    automatic_workshop_upload_performed   = $false
    binary_writer_changed                 = $false
}

$preflightJsonPath = Join-Path $Output 'map7v-preflight.json'
$preflight | ConvertTo-Json -Depth 3 | Set-Content -Path $preflightJsonPath -Encoding ASCII
Write-Output "Wrote: map7v-preflight.json"

$preflightMdPath = Join-Path $Output 'map7v-preflight.md'
Set-Content -Path $preflightMdPath -Value @"
# MAP-7V Preflight

``````text
MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED
MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED
BINARY_FORMAT_INVESTIGATION_PAUSED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
``````

``````text
k004_workshop_ready:                   true
k004_mod_loaded:                       true
k004_coordinate_aligned_binaries_present: true
k004_expected_candidate_lotheader_evidence: false
k004_visible_result:                   fallback_forest
k006_workshop_ready:                   true
k006_mod_loaded:                       true
k006_candidate_lotheader_count:        0
k006_candidate_lotpack_count:          0
k006_candidate_chunkdata_count:        0
k006_spawn_target_honored:             true
k006_expected_candidate_lotheader_evidence: false
binary_writer_gate_closed:             true
binary_format_investigation_paused:    true
next_branch:                           $nextBranch
public_playable_claim_allowed:         false
``````
"@ -Encoding ASCII
Write-Output "Wrote: map7v-preflight.md"

Write-Output ""
Write-Output "MAP-7V packet complete."
Write-Output "k006_candidate_lotheader_count=0"
Write-Output "binary_writer_gate_closed=true"
Write-Output "binary_format_investigation_paused=true"
Write-Output "next_branch=$nextBranch"
Write-Output "PUBLIC_PLAYABLE_CLAIM_ALLOWED=false"
Write-Output "LOAD_TEST_NOT_PERFORMED_BY_SCRIPT"
Write-Output "NO_AUTOMATIC_WORKSHOP_UPLOAD"
Write-Output "NO_BINARY_WRITER_CHANGES"
Write-Output "Done."
