# MAP-7J: Variant E Metadata Contract Failure and Metadata Contract Investigation

```text
MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY
VARIANTS_ABCDE_EXHAUSTED
ROOT_MOD_INFO_EXPERIMENT_FAILED
METADATA_CONTRACT_FOCUS
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7J records the Variant E manual retest result. Adding a root `mod.info`
alongside `42/mod.info` plus a root `media/maps/` alongside `42/media/maps/`
did NOT register the custom map folder with IsoMetaGrid.

All layout/Map= registration experiments A through E are exhausted. The
registration failure is NOT caused by:
- Map= line order (A/B/C)
- Missing root media/maps (D)
- Missing root mod.info (E)

The next focus shifts to map metadata contract and mod metadata shape.

---

## 2. Variant E tested

Configuration:
- Layout: root `mod.info` + `42/mod.info` + root `media/maps/` + `42/media/maps/`
- Map= line: `Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY`
- Mods= line: `Mods=pzmapforge_build42_candidate_v4_001`
- Server: `PZMF_B42_METADATA_V4_VARIANT_E_001`

```text
classification: MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY
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
IsoMetaGrid.Create: finished scanning directories in 0.034 seconds
IsoMetaGrid.Create: begin loading
IsoMetaGrid.Create: finished loading in 10.874 seconds
ERROR: Mannequin zone missing properties in media/maps/Muldraugh, KY/objects.lua coords: 13583, 1299, 0
initSpawnBuildings: no room or building at 150,150,0
Player data received from the server
game loading took 21 seconds
STATE: exit zombie.gameStates.GameLoadingState
Game Mode: Multiplayer
```

Human observation:
- No city choice. New world loads. No visible red error.
- Forest/fallback world. No blocked square area this time.

The absence of the blocked square area (compared to Variant D) does not indicate
progress or regression — it reflects normal Muldraugh world variability.

---

## 3. Exhausted experiments: A through E

| Variant | Layout | Map= | map_folders_list_empty |
|---|---|---|---|
| A | 42/ only | candidate;Muldraugh, KY | true |
| B | 42/ only | candidate only | true |
| C | 42/ only | Muldraugh, KY;candidate | true |
| D | 42/ + root media/maps/ | candidate;Muldraugh, KY | true |
| E | 42/ + root media/maps/ + root mod.info | candidate;Muldraugh, KY | true |

All five variants: `pzmapforge_build42_candidate_v4_001` does NOT appear in
IsoMetaGrid's map folder scan list.

VARIANTS_ABCDE_EXHAUSTED: confirmed. Map= ordering, root media/maps layout, and
root mod.info are all ruled out as the sole cause.

---

## 4. Diagnostic distinction (MAP-7J addition)

External evidence from Build 42 forum/server troubleshooting shows a DIFFERENT
failure mode:

```text
Looking in these map folders:
  D:\...\media\maps\SomeCustomMap
<End of map-folders list>
ERROR: Failed to find any .lotheader files
```

This is `MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING`:
- IsoMetaGrid DOES find the map folder path.
- But the folder contains no `.lotheader` files.

Our current failure is `MAP_FOLDER_SCAN_EMPTY`:
- IsoMetaGrid finds ZERO map folder paths.
- The folder is never reached; `.lotheader` files are irrelevant at this stage.

The two failure modes are DISTINCT. Fixing `.lotheader` format is premature
until the map folder registration is resolved.

The forum evidence also confirms:
- `Muldraugh, KY` should remain in `Map=` (all variants A/C/D/E included it).
- That advice addresses a different later-stage failure, not our discovery blocker.

---

## 5. Remaining hypotheses (metadata contract)

With A/B/C/D/E layout experiments exhausted, the remaining hypotheses involve
the content/shape of the metadata files:

### H4: map.info field contract (MEDIUM RISK)

The candidate `map.info` may be missing required fields or have fields with
incorrect values. Build 42 may require specific keys (e.g., `lots=`, `title=`,
`fixed=`, `description=`) or specific value formats.

### H5: mod.info id / folder name mismatch (MEDIUM RISK)

The mod.info `id=` field must exactly match the mod folder name. If there is
a mismatch, PZ may load the mod but fail to associate its media path.

### H6: map.info must reference the correct map ID (MEDIUM RISK)

The `id=` field in `map.info` must exactly match the map folder name and the
`Map=` server ini entry. A mismatch breaks the association chain.

### H7: Dedicated server vs client manifest difference (LOW RISK)

Build 42 dedicated server may use a different map registration mechanism
than single-player. Our test uses a dedicated-style server preset. The
map folder discovery may require a manifest file we have not identified.

### H8: Media path mounting requires a specific mod.info field (MEDIUM RISK)

The mod.info may need a `map=<map_id>` or similar field to explicitly register
the map folder path. Without this field, the media/maps path may not be
mounted even when present.

---

## 6. Next investigation: metadata contract inspection

`scripts/inspect-build42-map-metadata-contract.ps1` inspects mod.info and
map.info content from both the versioned (42/) and root layouts:
- Parses key/value fields
- Preserves raw lines
- Checks no-BOM and ASCII status
- Detects byte-identical root/42 copies
- Checks id field match against expected MapId/ModId

The inspection produces `map-metadata-contract-report.json` + `.md` under
`.local/` for human review before any further Variant F test.

---

## 7. Non-claims

- No load test was performed by any script in this slice.
- LOTH/LOTP/chunkdata binary writer was not changed.
- The absence of a blocked-square area in Variant E does not indicate
  that the candidate cells loaded.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
