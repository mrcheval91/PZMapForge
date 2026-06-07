# MAP-7K: Variant F Folder/ID Alignment Failure and mod.info map= Field Experiment

```text
MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY
H5_FOLDER_ID_ALIGNMENT_RULED_OUT
H8_MOD_INFO_MAP_FIELD_RECOMMENDED
VARIANTS_ABCDEF_EXHAUSTED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7K records the Variant F manual retest result. Ensuring that the installed
mod folder name exactly matches the mod.info id= field did NOT register the
custom map folder with IsoMetaGrid.

H5 (folder/id alignment) is ruled out as the sole cause. All layout and
alignment experiments A through F are exhausted. The next focused hypothesis
is H8: mod.info may need an explicit `map=<MapId>` field to register the
media/maps path with the mod loader.

---

## 2. Variant F tested

Hypothesis: H5 -- installed mod folder name must exactly match mod.info id.

Configuration:
- Installed folder name: pzmapforge_build42_candidate_v4_001
- mod.info id: pzmapforge_build42_candidate_v4_001
- Layout: same as experiment-E (dual root/42)
- Map= line: Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
- Server: PZMF_B42_METADATA_V4_VARIANT_F_001

```text
classification: MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY
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
IsoMetaGrid.Create: finished scanning directories in 0.055 seconds
IsoMetaGrid.Create: begin loading
IsoMetaGrid.Create: finished loading in 11.255 seconds
ERROR: Mannequin zone missing properties in media/maps/Muldraugh, KY/objects.lua
initSpawnBuildings: no room or building at 150,150,0
Player data received from the server
game loading took 21 seconds
STATE: exit zombie.gameStates.GameLoadingState
Game Mode: Multiplayer
```

Human observation: no city choice, forest/fallback world, no visible red error.

Conclusion: H5 (folder/id alignment) does NOT fix the registration failure.

---

## 3. All experiments A through F exhausted

| Variant | Change tested | Result |
|---|---|---|
| A | Map=candidate;Muldraugh -- ordering | SCAN_EMPTY |
| B | Map=candidate only | SCAN_EMPTY |
| C | Map=Muldraugh;candidate -- ordering | SCAN_EMPTY |
| D | + root media/maps/ | SCAN_EMPTY |
| E | + root mod.info | SCAN_EMPTY |
| F | folder name == mod.info id | SCAN_EMPTY |

H5_FOLDER_ID_ALIGNMENT_RULED_OUT: confirmed by Variant F.
VARIANTS_ABCDEF_EXHAUSTED: all six experiments produced empty scan.

---

## 4. Metadata contract findings (MAP-7J inspection)

The metadata contract inspector (MAP-7J) confirmed the following about the
current candidate mod.info:

Fields present: name, id, description, category, modversion, pzversion,
versionMin, poster, icon.

Notable absence: no `map=` field in mod.info.

In PZ mod architecture, some map mods include a `map=<MapId>` field in
mod.info to explicitly declare which map folder the mod provides. Without
this field, the mod loader may not register the mod's media/maps path with
the engine's map discovery system.

The map.info file has: title, lots, description, fixed2x.
The map.info id= field is EMPTY in the current candidate.
This is a potential additional issue -- map.info id= may need to equal the
map folder name.

---

## 5. Next hypothesis: H8 mod.info map= field

H8: Adding `map=pzmapforge_build42_candidate_v4_001` to mod.info may cause
the mod loader to register this mod's media/maps path with IsoMetaGrid's
scan list.

Experiment G:
- Use the dual-layout candidate (root + 42/ mod.info + media/maps/).
- Add `map=pzmapforge_build42_candidate_v4_001` to BOTH mod.info files.
- Keep no-BOM UTF-8 encoding on all text files.
- Keep LOTH/LOTP/chunkdata binary files unchanged.
- Test with Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY.
- Success condition: pzmapforge_build42_candidate_v4_001 appears in
  IsoMetaGrid map folder scan list.

---

## 6. Scripts

| Script | Purpose |
|---|---|
| `scripts/inspect-build42-map-metadata-contract.ps1` | Updated v0.2: mod_info_has_map_field, h5/h8 fields |
| `scripts/prepare-build42-map7k-modinfo-map-field-experiment-packet.ps1` | Generates experiment-G candidate with map= field |
| `scripts/test-build42-map7k-modinfo-map-field-experiment.ps1` | 11 test assertions |

---

## 7. Non-claims

- No load test was performed by any script in this slice.
- LOTH/LOTP/chunkdata binary writer was not changed.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
