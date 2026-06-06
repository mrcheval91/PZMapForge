# MAP-7H: Variant B/C Registration Failures and Map Discovery Path

```text
MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY
MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY
MAP_LINE_VARIANTS_EXHAUSTED
DISCOVERY_PATH_INVESTIGATION_ACTIVE
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7H records the Variant B and Variant C manual retest results and
shifts focus from Map= ordering to custom map discovery path investigation.

All three Map= line variants (A, B, C) produce an empty IsoMetaGrid map
folder scan. The Map= line ordering is NOT the root cause.

The candidate mod loads. The game loads. Multiplayer is reached. But the
candidate map folder is not visible to IsoMetaGrid. The root cause is in
the mod structure, map discovery path, or mod media path registration.

---

## 2. A/B/C variant comparison

| Variant | Map= line | map_folders_list_empty | candidate_not_in_list |
|---|---|---|---|
| A | Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY | true | true |
| B | Map=pzmapforge_build42_candidate_v4_001 | true | true |
| C | Map=Muldraugh, KY;pzmapforge_build42_candidate_v4_001 | true | true |

All three share:
- `candidate_loaded=true` (mod.info found and loaded)
- `map_folders_list_empty=true` (IsoMetaGrid scan empty)
- `spawn_building_warning=true` (no room/building at 150,150,0)
- `public_playable_claim_allowed=false`

Variant A: Muldraugh terrain loaded (forest/grass world). Player spawned.
Variant B: Empty/old-save-like world. Sparse forest. player_data_received=false.
           IsoChunk.LoadOrCreate sanity check errors for chunks 3,-2 and 11,-9
           observed in server log — likely fallback/old-save contamination,
           not primary blocker.
Variant C: Forest/grass/wilderness world. Coordinates visible around X 150,
           Y 150, Z 0. player_data_received=true.

---

## 3. Variant B result

```text
classification: MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY
candidate_loaded: true
player_data_received: false
game_loading_completed: true
map_folders_list_empty: true
spawn_building_warning: true
```

Key client log lines:
```text
loading pzmapforge_build42_candidate_v4_001
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>
IsoMetaGrid.Create: X: [ -250 250 ], Y: [ -250 250 ]
IsoMetaGrid.Create: finished scanning directories in 0.004 seconds
IsoMetaGrid.Create: begin loading
IsoMetaGrid.Create: finished loading in 10.626 seconds
initSpawnBuildings: no room or building at 150,150,0
game loading took 20 seconds
STATE: exit zombie.gameStates.GameLoadingState
Game Mode: Multiplayer
```

---

## 4. Variant C result

```text
classification: MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY
candidate_loaded: true
player_data_received: true
game_loading_completed: true
map_folders_list_empty: true
spawn_building_warning: true
```

Key client log lines:
```text
loading pzmapforge_build42_candidate_v4_001
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>
IsoMetaGrid.Create: X: [ -250 250 ], Y: [ -250 250 ]
IsoMetaGrid.Create: finished scanning directories in 0.045 seconds
IsoMetaGrid.Create: begin loading
IsoMetaGrid.Create: finished loading in 11.403 seconds
initSpawnBuildings: no room or building at 150,150,0
Player data received from the server
game loading took 24 seconds
STATE: exit zombie.gameStates.GameLoadingState
Game Mode: Multiplayer
```

---

## 5. MAP= line variants exhausted

The three Map= ordering variants are exhausted. The registration failure
is independent of Map= line ordering.

```text
A: Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY  -> EMPTY
B: Map=pzmapforge_build42_candidate_v4_001               -> EMPTY
C: Map=Muldraugh, KY;pzmapforge_build42_candidate_v4_001  -> EMPTY
```

LOTH/LOTP/chunkdata are NOT the current primary blocker. The game does not
reach the point where the binary files are loaded, because the map folder
is never registered with IsoMetaGrid.

---

## 6. Discovery path investigation

### 6.1 Current candidate structure

The empty_grass_v4 candidate generates a versioned 42/ layout:

```text
<mod_folder>/
  42/
    mod.info
    media/maps/pzmapforge_build42_candidate_v4_001/
      map.info
      spawnpoints.lua
      objects.lua
      0_0.lotheader
      world_0_0.lotpack
      chunkdata_0_0.bin
```

### 6.2 Key observation

Variant A: Muldraugh, KY IS loaded and provides terrain, confirming:
- Built-in map IDs resolve to a real filesystem path IsoMetaGrid scans.
- Custom mod map IDs do NOT resolve the same way.

The IsoMetaGrid map-folder scan list is empty even for Muldraugh when
listed in Map=. This means Muldraugh is NOT discovered via the Map= line
— PZ discovers Muldraugh's path from its install directory directly.

For custom mods, IsoMetaGrid must scan the mod's media/maps path. This
requires the mod's media directory to be registered in the scan list.

### 6.3 Hypotheses for empty scan

| Hypothesis | Risk level | To test |
|---|---|---|
| 42/ version layer not scanned by IsoMetaGrid | HIGH | Add root media/maps/ duplicate |
| map.info missing required fields | MEDIUM | Inspect map.info shape vs reference |
| mod.info not registering media path | MEDIUM | Inspect mod.info vs reference |
| map folder path not mounted by mod loader | HIGH | Experiment D |
| Mod media registered but wrong path used | MEDIUM | Experiment E/F |

### 6.4 Proposed next experiments (HUMAN-ONLY, not yet performed)

D. Duplicate the candidate map folder under a root `media/maps/<map_id>/`
   path alongside the `42/media/maps/<map_id>/` path. This tests whether
   IsoMetaGrid discovers the non-versioned layout.

E. Place a root `mod.info` at `<mod_folder>/mod.info` alongside the
   `42/mod.info`. Some mods use a dual mod.info layout.

F. Compare the candidate `map.info` shape against a known-working
   Build 42 reference map's `map.info`.

These experiments have not been performed. They are recorded for the next
manual retest session.

---

## 7. Scripts

| Script | Purpose |
|---|---|
| `scripts/inspect-build42-map-discovery-path.ps1` | Inspect mod layout; detect versioned vs root media/maps |
| `scripts/prepare-build42-map7h-discovery-path-packet.ps1` | Generate discovery diagnostic packet under .local/ |
| `scripts/test-build42-map7h-discovery-path.ps1` | 12 test assertions |

---

## 8. Non-claims

- No load test was performed by any script in this slice.
- LOTH/LOTP/chunkdata binary writer was not changed.
- No PZ assets were read or copied outside .local.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
