# MAP-7O: Dru_map-Aligned Metadata and Layout Experiment I

```text
DRUMAP_ALIGNED_EXPERIMENT_I_PREPARED
EXPERIMENT_I_USES_ROOT_MOD_INFO_NO_COMMON_MOD_INFO
MAP_INFO_LOTS_NONE
MAP_INFO_ZOOM_FIELDS_ADDED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7O records the MAP-7N Dru_map comparison findings and prepares
Experiment I -- the first experiment that exactly mirrors the Dru_map
(known-working Build 42 map mod) metadata and layout contract.

---

## 2. MAP-7N comparison findings

The MAP-7N comparator compared PZMapForge experiment-H against Dru_map.

### 2.1 Structural comparison

| Field | Candidate (experiment-H) | Reference (Dru_map) |
|---|---|---|
| has_42_folder | true | true |
| has_42_0_folder | false | false |
| has_common_folder | true | true |
| has_root_mod_info | **false** | **true** |
| has_common_mod_info | **true** | **false** |
| has_common_media_maps | true | true |
| uses_world_xy_lotpack | true | true |

Key finding: Dru_map has root `mod.info` but NOT `common/mod.info`.
All previous experiments either used `common/mod.info` without root, or
root `mod.info` without `common/media/maps/`. The combination of
root `mod.info` + `common/media/maps/` was never tested.

### 2.2 map.info differences

```text
Reference (Dru_map) has, candidate lacks:
  zoomX, zoomY, zoomS  (viewport display settings)

Reference lots=NONE
Candidate lots=pzmapforge_build42_candidate_v4_001  (WRONG)
```

The candidate map.info uses the map ID as the lots value, which is incorrect.
Dru_map uses `lots=NONE`.

### 2.3 mod.info differences

No fields in Dru_map mod.info that the candidate lacks. The candidate has
all Dru_map mod.info fields plus extras (category, modversion, etc.).

---

## 3. Experiment I contract

The exact untested combination, mirroring Dru_map:

```text
<MapId>/
  mod.info              (root -- no common/mod.info)
  42/
    mod.info
  common/
    media/
      maps/
        <MapId>/
          map.info      (lots=NONE, +zoomX, +zoomY, +zoomS)
          objects.lua
          spawnpoints.lua
          0_0.lotheader (binary, unchanged)
          world_0_0.lotpack (binary, unchanged)
          chunkdata_0_0.bin (binary, unchanged)
          thumb.png
          worldmap.xml
          worldmap-forest.xml
          maps/
            biomemap_0_0.png
```

Key changes from experiment-H:
1. `mod.info` at ROOT (not at common/)
2. NO `common/mod.info`
3. map.info `lots=NONE` (was incorrectly set to map ID)
4. map.info `zoomX=0, zoomY=0, zoomS=1` added (viewport placeholders)

---

## 4. Why this is not a binary writer task

The binary files (0_0.lotheader, world_0_0.lotpack, chunkdata_0_0.bin) are
unchanged from empty_grass_v4. The IsoMetaGrid scan blocker is map-folder
DISCOVERY, not binary file format. The binary files are never reached because
IsoMetaGrid does not find the map folder.

Do not change LOTH/LOTP/chunkdata behavior until the map folder registers.

---

## 5. Why no city choice is not decisive

"No city choice" in a PZ server test is NOT a failure signal. PZ often
auto-spawns without a city selection screen in server mode. The decisive
signal is `map_folders_list_empty=true` in the IsoMetaGrid scan log.

Forest/fallback world = game loaded. Not custom map registered.

---

## 6. Success condition

```text
Looking in these map folders:
  <path containing pzmapforge_build42_candidate_v4_001>
<End of map-folders list>
```

`map_folders_list_empty=false` in the analyzer output.

---

## 7. Failure condition

```text
MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY
```

IsoMetaGrid scan is still empty. The Dru_map-aligned layout did not fix
the registration.

---

## 8. Later-stage progress condition

```text
MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING
```

IsoMetaGrid found the map folder path but no `.lotheader` files in it.
This is PROGRESS (map registered, binary format next), not a discovery failure.

---

## 9. Non-claims

- Experiment I was not performed. This is a preparation packet only.
- No load test was performed by any script in this slice.
- LOTH/LOTP/chunkdata binary writer was not changed.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
