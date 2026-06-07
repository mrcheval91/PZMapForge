# MAP-7I: Variant D Root Media/Maps Failure and Experiment E Preparation

```text
MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY
ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT
EXPERIMENT_E_ROOT_MOD_INFO_RECOMMENDED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7I records the Variant D manual retest result. Adding a root
`media/maps/<map_id>/` duplicate alongside the existing `42/media/maps/`
layout did NOT register the custom map folder with IsoMetaGrid.

The root media/maps path alone is insufficient. The next hypothesis is
that a root `mod.info` (at `<mod_folder>/mod.info`, alongside `42/mod.info`)
is required for PZ to mount the root media path and make it visible to
IsoMetaGrid's scan.

---

## 2. Variant D tested

Configuration:
- Layout: `42/media/maps/pzmapforge_build42_candidate_v4_001/` (versioned)
          PLUS `media/maps/pzmapforge_build42_candidate_v4_001/` (root duplicate)
- Map= line: `Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY`
- Server: `PZMF_B42_METADATA_V4_VARIANT_D_001`

```text
classification: MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY
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
IsoMetaGrid.Create: finished scanning directories in 0.053 seconds
IsoMetaGrid.Create: begin loading
IsoMetaGrid.Create: finished loading in 11.07 seconds
ERROR: Mannequin zone missing properties in media/maps/Muldraugh, KY/objects.lua coords: 13583, 1299, 0
initSpawnBuildings: no room or building at 150,150,0
Player data received from the server
game loading took 34 seconds
STATE: exit zombie.gameStates.GameLoadingState
Game Mode: Multiplayer
```

Human observation:
- World loaded. No city choice.
- Forest/fog world visible.
- Coordinates around X 150, Y 150, Z 0.
- Large square/blocked tile area visible. Player cannot walk into it.
- No visible red error panel.

The square/blocked area is NOT proof of candidate map registration because
the map-folder scan is empty. It is treated as a visual artifact of the
Muldraugh world or old-save data until the map-folder scan confirms the
candidate. The Mannequin error is a Muldraugh objects.lua issue, unrelated
to the candidate.

---

## 3. Root cause analysis

ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT confirmed by Variant D.

In PZ Build 42, placing a mod's map files at `media/maps/<id>/` is not
sufficient for IsoMetaGrid to discover the folder. Something must register
this path with the mod media system first.

Hypothesis: the root `mod.info` file is the trigger that causes PZ's mod
loader to mount the mod's root `media/` path and make it visible to
IsoMetaGrid's scan. Without a root `mod.info`:
- `42/mod.info` is found and the mod loads.
- The `42/` versioned media path is mounted for versioned access.
- The root `media/` path is not mounted because there is no root `mod.info`.
- IsoMetaGrid therefore sees no map folders for this mod.

---

## 4. Experiment E: root mod.info + root media/maps

Experiment E adds a root `mod.info` at `<mod_folder>/mod.info` alongside
the existing `42/mod.info`, combined with the root `media/maps/` layout
from Variant D.

Expected mod folder structure for Experiment E:
```text
<mod_folder>/
  mod.info                              (NEW -- root mod.info)
  42/
    mod.info                            (keep)
    media/maps/<map_id>/                (keep)
      map.info
      spawnpoints.lua
      objects.lua
      0_0.lotheader
      world_0_0.lotpack
      chunkdata_0_0.bin
  media/                               (NEW -- root media layout)
    maps/
      <map_id>/
        map.info
        spawnpoints.lua
        objects.lua
        0_0.lotheader
        world_0_0.lotpack
        chunkdata_0_0.bin
```

Requirements for Experiment E (HUMAN-ONLY):
- All text files (mod.info, map.info, spawnpoints.lua, objects.lua) saved
  with no-BOM UTF-8 encoding.
- Server preset:
  Name: PZMF_B42_METADATA_V4_VARIANT_E_001
  Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
- Capture client DebugLog after test.
- Run analyzer with:
  `-ExpectedMapId pzmapforge_build42_candidate_v4_001 -VariantLabel VariantE`
- Target evidence: `pzmapforge_build42_candidate_v4_001` appears in the
  IsoMetaGrid map-folder scan list.

---

## 5. Inspector update (MAP-7I)

`scripts/inspect-build42-map-discovery-path.ps1` updated with new fields:
- `has_dual_mod_info_layout`
- `has_dual_media_maps_layout`
- `root_mod_info_missing`
- `experiment_d_root_media_maps_result`
- `experiment_e_root_mod_info_recommended`

---

## 6. Non-claims

- No load test was performed by any script in this slice.
- LOTH/LOTP/chunkdata binary writer was not changed.
- The square/blocked visual area is NOT proof of candidate map loading.
- No candidate map cells were confirmed loaded.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
