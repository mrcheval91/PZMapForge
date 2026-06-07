# MAP-7L: Variant G Failure and Build 42 common/media/maps Layout Pivot

```text
MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY
H8_MOD_INFO_MAP_FIELD_RULED_OUT
VARIANTS_ABCDEFG_EXHAUSTED
COMMON_LAYOUT_PIVOT
BUILD42_COMMON_MEDIA_MAPS_HYPOTHESIS
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7L records the Variant G manual retest result. Adding `map=<MapId>` to
mod.info did NOT register the custom map folder with IsoMetaGrid.

H8 (mod.info map= field) is ruled out as the sole cause.

All seven layout, ordering, alignment, and metadata experiments A through G
are exhausted. The next pivot is structural: based on operator-provided Build
42 mod structure documentation, the map files must be placed under
`common/media/maps/<MapId>/` -- not under `42/media/maps/` or root
`media/maps/`.

---

## 2. Variant G tested

Hypothesis: H8 -- mod.info map= field registers the media path.

Configuration:
- root mod.info with: map=pzmapforge_build42_candidate_v4_001
- 42/mod.info with: map=pzmapforge_build42_candidate_v4_001
- Dual layout (root + 42/ mod.info + media/maps/)
- Map= line: Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
- Server: PZMF_B42_METADATA_V4_VARIANT_G_001

```text
classification: MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY
candidate_loaded: true
player_data_received: true
game_loading_completed: true
map_folders_list_empty: true
spawn_building_warning: true
```

Key evidence:
```text
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>
```

H8_MOD_INFO_MAP_FIELD_RULED_OUT: confirmed. The map= field alone does not
register the map folder. Note: Musical Menu Framework errors appeared in the
client log. These are client-side noise/mod contamination, not the map
discovery blocker.

---

## 3. All experiments A through G exhausted

| Variant | Change tested | Result |
|---|---|---|
| A | Map= candidate;Muldraugh -- ordering | SCAN_EMPTY |
| B | Map= candidate only | SCAN_EMPTY |
| C | Map= Muldraugh;candidate -- ordering | SCAN_EMPTY |
| D | + root media/maps/ | SCAN_EMPTY |
| E | + root mod.info | SCAN_EMPTY |
| F | folder name == mod.info id | SCAN_EMPTY |
| G | mod.info map= field | SCAN_EMPTY |

VARIANTS_ABCDEFG_EXHAUSTED. The mod loads. The game loads. Map folder
discovery does not find the candidate. No layout, Map= ordering, file
alignment, or mod.info field tested so far has fixed the registration.

---

## 4. Diagnostic distinction (maintained from MAP-7J)

```text
MAP_FOLDER_SCAN_EMPTY = IsoMetaGrid found zero map folder paths.
  Current blocker for all experiments A through G.

MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING = IsoMetaGrid found
  a map folder path but found no .lotheader files inside it.
  A later-stage failure. NOT our current blocker.
```

We are still in the MAP_FOLDER_SCAN_EMPTY stage. The issue is map folder
discovery/registration, not binary file format.

---

## 5. Operator-provided Build 42 structure evidence

The operator provided screenshots/documentation showing the correct Build 42
mod folder structure:

```text
MyMapMod/
  42.0/                (or 42/)
    mod.info
    poster.png
  common/
    mod.info
    media/
      maps/
        MyMap/
          0_0.lotheader
          chunkdata_0_0.bin
          map.info
          objects.lua
          spawnpoints.lua
          thumb.png
          world_0_0.lotpack
          worldmap.xml
          worldmap-forest.xml
          maps/
            biomemap_0_0.png
```

Key findings from this structure:
- Map files belong under common/media/maps/<MapId>/, not 42/media/maps/.
- The 42/ folder contains only mod.info and poster.png (mod versioning).
- common/ is the shared/default content folder for all PZ versions.
- Build 42 cells are exported as 256x256 (though tools may work with 300x300).
- The lotpack naming convention is world_X_Y.lotpack (our writer already uses this).
- Additional files expected: thumb.png, worldmap.xml, worldmap-forest.xml,
  maps/biomemap_X_Y.png. Our writer does not currently produce these.

---

## 6. New hypothesis: Build 42 common/media/maps layout

COMMON_LAYOUT_PIVOT: all previous experiments used wrong map file locations.

The hypothesis: IsoMetaGrid's map folder scan reads mod media paths registered
under `common/`, not `42/` or root. When PZ mounts a mod's `common/` subtree,
its `media/maps/` directory entries become visible to IsoMetaGrid.

Our previous experiments placed map files under:
- 42/media/maps/<MapId>/ -- versioned, not visible to IsoMetaGrid
- media/maps/<MapId>/ (root) -- not the documented location
- Both root + 42/ -- still not the documented location

Experiment H tests the documented common/media/maps/<MapId>/ layout.

---

## 7. Scripts

| Script | Purpose |
|---|---|
| `scripts/inspect-build42-map-discovery-path.ps1` | Updated v0.3: common/ layout detection |
| `scripts/prepare-build42-map7l-common-layout-experiment-packet.ps1` | Generates experiment-H candidate |
| `scripts/test-build42-map7l-common-layout-experiment.ps1` | 15 test assertions |

---

## 8. Non-claims

- No load test was performed by any script in this slice.
- LOTH/LOTP/chunkdata binary content was not changed.
- The documented 256x256 cell size is informational only.
  Our binary writer still generates 300x300-based files.
  The cell size difference is a separate investigation item.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
