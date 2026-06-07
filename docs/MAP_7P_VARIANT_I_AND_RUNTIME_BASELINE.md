# MAP-7P: Variant I Failure Record and Known-Working Runtime Baseline

```text
MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY
VARIANTS_ABCDEFGHI_EXHAUSTED
DRUMAP_BASELINE_DIAGNOSTIC_REQUIRED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7P records the Experiment I failure result (Variant I) and pivots
diagnostic focus from static layout changes to runtime activation.

All nine layout variants A through I have now been tested.
All produced MAP_FOLDER_SCAN_EMPTY.

The current blocker is not the static mod package layout.
The current blocker is the runtime activation contract: something in
the IsoMetaGrid discovery path distinguishes a local loose mod from
a Workshop-activated mod that PZMapForge has not yet matched.

The next diagnostic is a known-working runtime baseline using Dru_map,
a Build 42 Workshop map mod confirmed to load in multiplayer.

---

## 2. Experiment I result

### 2.1 Install facts

```text
Candidate:  pzmapforge_build42_candidate_v4_001
Install path:
  C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v4_001
Layout:
  mod.info (root)
  42/mod.info
  common/mod.info ABSENT
  common/media/maps/pzmapforge_build42_candidate_v4_001/
map.info:
  title=PZMapForge Build42 Candidate - pzmapforge_build42_candidate_v4_001
  lots=NONE
  description=BUILD42 CANDIDATE -- NOT VALIDATED. Not a playable map.
  fixed2x=true
  zoomX=0
  zoomY=0
  zoomS=1
Encoding: no-BOM
Server:
  PZMF_B42_METADATA_V4_VARIANT_I_001
  Mods=pzmapforge_build42_candidate_v4_001
  Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
```

### 2.2 Analyzer result

```text
classification:           MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY
candidate_loaded:         true
player_data_received:     true
game_loading_completed:   true
map_folders_list_empty:   true
spawn_building_warning:   true
public_playable_claim_allowed=false
```

### 2.3 Key log evidence

- Server and client both loaded `pzmapforge_build42_candidate_v4_001`.
- IsoMetaGrid.Create began scanning directories.
- `Looking in these map folders:` appeared in log.
- `<End of map-folders list>` appeared immediately after with zero entries.
- IsoMetaGrid finished loading.
- Muldraugh mannequin-zone warning: vanilla fallback noise, not diagnostic.
- `initSpawnBuildings: no room or building at 150,150,0` appeared.
- Player data received from the server.
- Game loading took 40 seconds.
- Game Mode: Multiplayer.

### 2.4 Correct interpretation

Variant I failed. Dru_map-aligned static structure (root mod.info,
no common/mod.info, common/media/maps, lots=NONE, zoomX/Y/S) was not
sufficient for IsoMetaGrid to scan and register the candidate map folder.

This is MAP_FOLDER_SCAN_EMPTY, not MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING.

Lotheader file discovery has not been reached. Do not investigate LOTH/LOTP/chunkdata
writer quality until the map folder appears in the IsoMetaGrid scan.

---

## 3. Variants exhausted

```text
A: Map= line ordering (Map=candidate;Muldraugh, KY)
B: Map= line ordering (Map=candidate)
C: Map= line ordering (Map=Muldraugh, KY;candidate)
D: root media/maps/ duplicate alongside 42/
E: root mod.info + root media/maps/ + 42/
F: exact folder/id alignment (H5 hypothesis)
G: mod.info map= field (H8 hypothesis)
H: common/media/maps/ layout alone
I: root mod.info + NO common/mod.info + common/media/maps/ (Dru_map-aligned)
All nine: MAP_FOLDER_SCAN_EMPTY confirmed.
```

`VARIANTS_ABCDEFGHI_EXHAUSTED` is now binding.

---

## 4. What the failure does NOT mean

- No city choice is not decisive. Variant I produced a forest/fallback world.
  This is vanilla behavior when IsoMetaGrid finds no custom map folders.
  It is not proof that the mod was partially loaded or partially registered.
- Forest/fallback world load is not proof of custom map registration.
- Player data received and game loading completed are partial passes only.
  They confirm mod loading at a shallow level, not map folder registration.
- The spawn building warning at 150,150,0 is expected when no custom map registered.

---

## 5. Success condition

Success for any map remains:

```text
map folder appears between:
  "Looking in these map folders:"
and:
  "<End of map-folders list>"
```

Until this occurs, the candidate map folder is not registered
with IsoMetaGrid and no custom map cell can be loaded.

---

## 6. Next diagnostic: known-working runtime baseline

The new unknown is runtime activation and mounting: local mod activation vs
Workshop activation, server WorkshopItems line, or another runtime registry path.

The baseline test uses Dru_map -- a known-working Build 42 Workshop map mod
(Workshop ID: 3355966216, map folder: Dru_map) -- to establish a reference
for how a working map mod appears in the IsoMetaGrid scan.

If Dru_map appears in the map folder scan, that confirms the runtime pipeline
can discover Workshop map mods. The PZMapForge candidate then lacks a runtime
activation or mount condition that Dru_map satisfies.

Baseline server wiring:
```text
Mods=Dru_map
WorkshopItems=3355966216
Map=Dru_map;Muldraugh, KY
Public=false
```

Analyzer command for baseline log:
```text
powershell -ExecutionPolicy Bypass -File .\scripts\inspect-build42-map7d-load-result.ps1 `
    -LogPath .\.local\<log> `
    -Output .\.local\map7p-analysis `
    -ExpectedMapId Dru_map `
    -VariantLabel DruMapBaseline
```

Expected baseline success classification:
```text
MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND
```

Expected baseline failure classification:
```text
MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY
```

---

## 7. Decision tree

If `Dru_map` appears in IsoMetaGrid scan:
- The runtime pipeline can discover Workshop map mods.
- The PZMapForge candidate local mod is missing a runtime activation condition.
- Next task: compare WorkshopItems/Mods wiring and investigate local-vs-Workshop
  registration contract.

If `Dru_map` does not appear but game still works:
- The client log scan may not be the right evidence point for Build 42
  coop server. Inspect server-side map load evidence instead.

If `Dru_map` baseline fails entirely:
- Baseline server wiring is invalid. Fix reference wiring before
  touching PZMapForge candidate layout or activation.

If `Dru_map` scan reaches lotheader stage:
- Record the exact stage transition and compare against PZMapForge candidate.
- The candidate may need to reach that same stage before lotheader quality matters.

---

## 8. Claim boundary

```text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED
NO_BINARY_WRITER_CHANGE
NO_PZ_ASSETS_OUTSIDE_LOCAL
NO_FORBIDDEN_PATH_WRITES
```

No playable PZMapForge export is claimed from this task.
The Dru_map baseline test is a human-only diagnostic step.
Claude does not run Project Zomboid. Claude does not write to user PZ folders.
