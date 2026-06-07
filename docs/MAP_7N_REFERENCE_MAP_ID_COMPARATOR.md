# MAP-7N: Reference Map ID Support and Dru_map Comparison

```text
REFERENCE_MAP_ID_SUPPORT_ADDED
DRU_MAP_COMPARISON_EXECUTED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7N adds `-ReferenceMapId` support to the known-working map contract
comparator, enabling comparison against reference mods whose map folder
name differs from the candidate's. A known-working Build 42 map mod
(Dru_map) was copied to `.local/` and compared against the candidate.

---

## 2. Comparator patch

### 2.1 Problem

The MAP-7M comparator accepted only one `-MapId` and used it for BOTH
the candidate and reference map folder scan. When the reference mod has
a different map folder name (e.g. `Dru_map` vs `pzmapforge_build42_candidate_v4_001`),
the reference layout was scanned under the wrong folder name and
`has_common_media_maps` would be reported as false.

### 2.2 Fix

Added `-ReferenceMapId` parameter (defaults to `-MapId` if omitted).
The candidate scan uses `-MapId`. The reference scan uses `-ReferenceMapId`.
Both `candidate_map_id` and `reference_map_id` are reported in the output.

### 2.3 Usage (separate map IDs)

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\inspect-build42-known-working-map-contract.ps1 `
  -CandidateRoot ".\.local\map7m-packet\experiment-h-candidate\pzmapforge_build42_candidate_v4_001" `
  -ReferenceRoot ".\.local\map7m-packet\reference-known-working-map\Dru_map" `
  -Output ".\.local\map7m-packet\comparison-dru-map" `
  -MapId "pzmapforge_build42_candidate_v4_001" `
  -ReferenceMapId "Dru_map"
```

---

## 3. Reference mod: Dru_map

The operator copied a known-working Build 42 map mod (Dru_map) from the
PZ Workshop into `.local/`. The comparator reads only from `.local/`.

Reference root: `.local\map7m-packet\reference-known-working-map\Dru_map`

Observed reference structure:
```text
Dru_map/
  mod.info              (root mod.info present)
  poster.png
  42/
    mod.info            (versioned mod.info)
  common/
    media/
      maps/
        Dru_map/        (map folder named Dru_map)
          map.info
          ... (map data files)
```

Key observations:
- Has BOTH root `mod.info` AND `42/mod.info` (same as our experiments E-H).
- Uses `common/media/maps/Dru_map/` (same layout as our experiment-H).
- Uses `42/` (not `42.0/`).
- Does NOT use `42.0/`.

---

## 5. Dru_map comparison findings

Comparator run:
```text
CandidateRoot: .local\map7m-packet\experiment-h-candidate\pzmapforge_build42_candidate_v4_001
ReferenceRoot: .local\map7m-packet\reference-known-working-map\Dru_map
MapId:         pzmapforge_build42_candidate_v4_001
ReferenceMapId: Dru_map
```

### 5.1 Structural comparison

| Field | Candidate | Reference (Dru_map) |
|---|---|---|
| has_42_folder | true | true |
| has_42_0_folder | false | false |
| has_common_folder | true | true |
| has_root_mod_info | false | **true** |
| has_common_mod_info | **true** | false |
| has_common_media_maps | true | true |
| uses_world_xy_lotpack | true | true |
| version_folder_differs | -- | false |
| common_folder_differs | -- | false |

Key structural difference: the reference (Dru_map) has root `mod.info` + `42/mod.info`
but NO `common/mod.info`. Our candidate (experiment-H) has `common/mod.info` +
`42/mod.info` but NO root `mod.info`.

We have not yet tested the combination of:
- root `mod.info` (like experiment-E) + `42/mod.info`
- with map files at `common/media/maps/<MapId>/` (like experiment-H)

This is the exact layout Dru_map uses.

### 5.2 mod.info field differences

Reference has: `name, id, description, poster` (only 4 fields -- very minimal).
Candidate has all of these plus: `category, modversion, pzversion, versionMin, icon`.
`mod_info_fields_in_reference_not_candidate`: empty (no gaps in candidate).
Extra candidate fields do not appear to be the blocker.

### 5.3 map.info field differences

Reference has 3 fields candidate lacks: `zoomX, zoomY, zoomS` (viewport/zoom settings).
These are display parameters for the in-game map. Adding them is low risk.

Significant value difference in the `lots` field:
- Candidate: `lots=pzmapforge_build42_candidate_v4_001` (WRONG -- using the map ID)
- Reference: `lots=NONE`

The `lots` field likely lists tileset lot names or is `NONE` for custom simple maps.
Using the map ID as the lots value is a likely bug in the candidate map.info writer.

### 5.4 Map data files compared

| File | Candidate | Reference |
|---|---|---|
| 0_0.lotheader | 29646 bytes | 2102 bytes |
| world_0_0.lotpack | 1056780 bytes | 1057348 bytes |
| chunkdata_0_0.bin | 1026 bytes | 1026 bytes |
| worldmap.xml | 52 bytes (stub) | 28,137,652 bytes |
| worldmap-forest.xml | 52 bytes (stub) | 9,003,851 bytes |
| maps_subfolder | exists | exists |

The reference lotheader is much smaller (2102 vs 29646). The lotpack sizes are close.
The worldmap files in the reference are full production files; ours are stubs.

---

## 6. Recommended next experiment: Experiment I

Based on the Dru_map comparison, Experiment I should test the untested combination:

1. `mod.info` at root (mirroring Dru_map -- NOT at common/)
2. `42/mod.info` (kept)
3. NO `common/mod.info`
4. Map files at `common/media/maps/<MapId>/` (same as experiment-H)
5. Fix `lots` field in map.info to `NONE` instead of map ID
6. Add `zoomX, zoomY, zoomS` fields to map.info (low-risk, mirrors reference)

This is the first experiment that exactly mirrors the Dru_map mod structure.
Previous experiments never combined root mod.info with common/media/maps/.

---

## 8. Non-claims

- No load test was performed by any script in this slice.
- The reference mod was copied to `.local/` only; no Workshop paths were
  read by the comparator.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
