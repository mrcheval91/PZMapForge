# MAP-9C IsoMetaGrid Map Folder Registration

```text
Status: MAP-9C research packet defined
Classification: MAP9C_ISOMETAGRID_MAP_FOLDER_REGISTRATION_RESEARCH_PACKET_DEFINED
Purpose: make PZMapForge appear in IsoMetaGrid map folder scan
Success signal: IsoMetaGrid logs PZMapForge or candidate folder in map-folder list
Failure signal: IsoMetaGrid map folder list empty or only vanilla
Public playable claim: not allowed
```

---

## MAP-9B debug blocker (carried forward)

MAP-9B runtime evidence (Build 42.19.0) confirmed:

```text
MAP9B_DEBUG_RUNTIME_EVIDENCE_CONFIRMS_MOD_LOAD_BUT_NO_ISOMETAGRID_MAP_FOLDER
WORKSHOP_RUNTIME_CACHE_CONFIRMED=true
DEBUG_RUNTIME_MOD_LOADED=true (pzmapforge_build42_candidate_v4_001 / Workshop 3740642200)
SPAWN_METADATA_WORKS=true (player position 10746,8288,0)
ISOMETAGRID_MAP_FOLDER_LIST_EMPTY=true
PZMAPFORGE_LOTHEADER_PARSE_EVIDENCE=false
PZMAPFORGE_LOTPACK_PARSE_EVIDENCE=false
PZMAPFORGE_CHUNKDATA_PARSE_EVIDENCE=false
PLAYABLE_TERRAIN_MOUNT_PROVEN=false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

The observed IsoMetaGrid log was:

```text
Looking in these map folders:
<empty / no PZMapForge listed>
```

The engine is not scanning the PZMapForge map folder. Therefore no `.lotheader`,
`.lotpack`, or chunkdata can be considered for mounting.

---

## Research purpose

Determine which folder layout and naming in the Workshop item causes IsoMetaGrid
to include the PZMapForge folder in its map-folder scan.

Do not chase canary terrain output yet.
Do not chase worldmap.xml.bin yet.
Do not claim playable.
First success is map-folder registration only.

---

## Success signal

The debug log contains:

```text
Looking in these map folders:
...PZMapForge...
```

or another candidate folder path that clearly references the PZMapForge map folder.

---

## Failure signal

The debug log contains:

```text
Looking in these map folders:
<empty / no PZMapForge listed>
```

---

## Current Map line (must not change Muldraugh)

```text
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
```

Muldraugh must remain as the last entry (bootstrap/fallback).
NO_MULDRAUGH_STRATEGY_REJECTED=true (MAP-8Z hard-fail test result).

---

## Candidate layout hypotheses

Each hypothesis is a controlled probe. Do not stage all at once.
Test one variant at a time. Human manual only.

```text
Variant A (current known layout):
  common/media/maps/PZMapForge/
  mod.info, map.info, spawnpoints.lua, objects.lua, 0_0.lotheader, etc.

Variant B (child map-id folder name matches Map= token):
  common/media/maps/pzmapforge_build42_candidate_v4_001/
  mod.info, map.info, spawnpoints.lua, objects.lua, 0_0.lotheader, etc.

Variant C (versioned 42 layout — PZMapForge name):
  42/media/maps/PZMapForge/
  mod.info, map.info, spawnpoints.lua, objects.lua, 0_0.lotheader, etc.

Variant D (versioned 42 layout — child map-id name):
  42/media/maps/pzmapforge_build42_candidate_v4_001/
  mod.info, map.info, spawnpoints.lua, objects.lua, 0_0.lotheader, etc.

Variant E (direct legacy layout):
  media/maps/PZMapForge/
  mod.info, map.info, spawnpoints.lua, objects.lua, 0_0.lotheader, etc.
```

Registration_hypotheses (all unverified):

```text
H1: IsoMetaGrid reads map folders from the Workshop item common/media/maps/ subtree.
H2: The Map= token value must match the subfolder name under common/media/maps/.
H3: The map.info id= field must match the subfolder name for registration.
H4: Build 42 uses the 42/ versioned subtree instead of root common/.
H5: The lots= field in map.info must be present for IsoMetaGrid to list the folder.
H6: A missing or malformed spawnregions.lua prevents registration even if folder exists.
H7: IsoMetaGrid only scans folders listed in a parent map index — not by filesystem discovery.
```

---

## Human manual runtime test steps

**Before each test run:**

1. Close Project Zomboid / server completely.
2. Apply only ONE variant manually to the Workshop item at:
   `D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\steamapps\workshop\content\108600\3740642200`
3. Keep the Map line unchanged:
   `Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY`
4. Do not remove Muldraugh from the Map line.
5. Use a fresh/parked server world (delete or park the world folder).
6. Launch PZ with -debug flag.
7. Join a coop session or start the server.

**Evidence to capture:**

Search `console.txt` and `coop-console.txt` for:

```text
Looking in these map folders:
PZMapForge
pzmapforge_build42_candidate_v4_001
lotheader
lotpack
chunkdata
worldmap
```

**Interpreting results:**

- SUCCESS: `PZMapForge` or the candidate folder appears in the IsoMetaGrid map-folder list.
  - Even if registered, do NOT claim playable terrain.
  - Even if registered, do NOT claim lotheader parsed.
  - Next step after success: check for lotheader parse evidence.
- FAILURE: List is empty or contains only vanilla maps.
  - Record the variant ID and try the next variant.

**Never test more than one variant at a time.**
**Never claim playable from registration alone.**

---

## Safety constraints

```text
inspected_repo_only=true (default)
pz_assets_read=false
pz_run_performed=false (human-only)
workshop_upload_performed=false
steam_write_performed=false
third_party_files_copied=false
binary_contents_dumped=false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## Classification labels

```text
MAP9C_ISOMETAGRID_MAP_FOLDER_REGISTRATION_RESEARCH_PACKET_DEFINED
MAP9B_DEBUG_RUNTIME_BLOCKER_CARRIED_FORWARD=true
ISOMETAGRID_MAP_FOLDER_LIST_EMPTY=true (last observed)
MOD_LOAD_CONFIRMED=true
SPAWN_METADATA_WORKS=true
MAP_FOLDER_REGISTRATION_UNPROVEN=true
PLAYABLE_TERRAIN_MOUNT_PROVEN=false
CANARY_WRITER_AVAILABLE=false
CANARY_WRITER_BLOCKED=true
MULDRAUGH_BOOTSTRAP_REQUIRED=true
NO_MULDRAUGH_STRATEGY_REJECTED=true
HUMAN_MANUAL_RUNTIME_TEST_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_UPLOADED_WORKSHOP=false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## Next research branch

`map9c_isometagrid_registration_probe_human_runtime_test_pending`
