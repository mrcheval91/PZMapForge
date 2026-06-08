# MAP-8G: Known-Working Build 42 Map Contract Comparator

```text
MAP8G_KNOWN_WORKING_CONTRACT_COMPARATOR_DEFINED
NEXT_BRANCH=known_working_build42_map_contract_comparator
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Why this comparator is needed

MAP-8F established:

- lots=<MapId> makes the city selector list the candidate (new).
- IsoMetaGrid still does not mount the candidate map folder.
- WorldMapDataAssetManager fails to load worldmap.xml / worldmap-forest.xml.
- Player spawns in Muldraugh/fallback, not PZMapForge content.

Two subsystems now distinguish the candidate from a working map:

1. IsoMetaGrid does not discover/mount the map folder.
2. WorldMapDataAssetManager cannot load the worldmap XML files.

The comparator must identify what a known-working Build 42 map provides in the
version-scoped `42\media\maps\<MapId>\` layout that the PZMapForge candidate
does not.

---

## 2. Comparator scope

The comparator focuses on the `42\media\maps\<MapId>\` folder. It does NOT
compare common/ layout (covered by MAP-7M) and does NOT copy third-party files.

The operator must place a known-working Build 42 map under `.local/` for
comparison. The comparator reads both roots from `.local/` only.

---

## 3. Key comparison fields

### 3.1 map.info fields

| Field | PZMapForge candidate | Expected in working map |
|---|---|---|
| lots | lots=<MapId> | unknown -- compare |
| title | <MapId> | unknown |
| description | present | unknown |
| fixed2x | true | unknown |
| zoomX/Y/S | 10505/12220/14.5 | compare |

Key question: does a working map use `lots=<MapId>`, `lots=NONE`, or something else?

### 3.2 Worldmap files

| File | PZMapForge candidate | Expected in working map |
|---|---|---|
| worldmap.xml | present (minimal `<worldmap />`) | unknown -- compare |
| worldmap-forest.xml | present (minimal `<worldmap />`) | unknown |
| worldmap.xml.bin | ABSENT (removed in MAP-8D) | unknown |
| worldmap-forest.xml.bin | ABSENT | unknown |
| worldmap.png | present (placeholder) | unknown |
| streets.xml.bin | ABSENT | unknown |

Key question: does a working map provide compiled .xml.bin files? If so, what
is the binary format and magic? (This would reopen the worldmap binary writer
investigation.)

### 3.3 Additional files

| File | PZMapForge candidate | Expected in working map |
|---|---|---|
| spawnregions.lua | ABSENT | unknown -- compare |
| thumb.png | present | unknown |
| objects.lua | present (comment-only) | unknown |

Key question: is spawnregions.lua required in the map folder (not just the mod root)?

### 3.4 mod.info fields

| Field | PZMapForge candidate | Notes |
|---|---|---|
| modversion | 1.0 | compare |
| pzversion | (not set in 42\mod.info) | compare |
| versionMin | (not set) | compare |
| id | pzmapforge_build42_candidate_v4_001 | compare |

---

## 4. Comparator tool

Script: `scripts/inspect-build42-known-working-map-contract-v2.ps1`

Parameters:
- `-CandidateRoot` — path under `.local/` to the candidate map folder
  (contains map.info, spawnpoints.lua, lotheader, etc.)
- `-ReferenceRoot` — path under `.local/` to the reference map folder
  (known-working Build 42 map, placed by operator)
- `-Output` — path under `.local/` for output JSON + MD

Safety: all paths must be under `.local/`. Script refuses to read from
Workshop, PZ install, or any path outside `.local/`.

Output: `build42-known-working-map-contract-v2.json` + `.md`

Key output fields:
- `candidate_map_info_lots` / `reference_map_info_lots`
- `candidate_has_worldmap_xml_bin` / `reference_has_worldmap_xml_bin`
- `candidate_has_worldmap_xml` / `reference_has_worldmap_xml`
- `candidate_has_streets_xml_bin` / `reference_has_streets_xml_bin`
- `candidate_has_spawnregions_lua` / `reference_has_spawnregions_lua`
- `map_info_field_differences` — array of differing key/value pairs
- `file_set_differences` — files present in reference but absent from candidate
- `contract_recommendation` — derived from diffs

---

## 5. Human-only operator steps

1. Subscribe to a known-working Build 42 custom map on Steam Workshop.
2. Let it download. Locate the workshop content folder.
3. Find the 42\media\maps\<MapId>\ folder inside it.
4. Copy ONLY the 42\media\maps\<MapId>\ folder (no cell binaries) to:
   `.local\map8g-reference\<MapId>\`
5. Run the comparator:
   ```powershell
   powershell -ExecutionPolicy Bypass `
       -File .\scripts\inspect-build42-known-working-map-contract-v2.ps1 `
       -CandidateRoot .local\<candidate-maps-dir> `
       -ReferenceRoot .local\map8g-reference\<ReferenceMapId> `
       -Output .local\map8g-comparator
   ```
6. Review output JSON/MD for structural differences.

DO NOT copy the reference map's binary cell files (lotheader/lotpack/chunkdata)
into the PZMapForge repo. Text metadata files only.

---

## 6. Claim boundary

```text
MAP8G_KNOWN_WORKING_CONTRACT_COMPARATOR_DEFINED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
```

Non-claims:
- No playable PZMapForge export claimed.
- Binary writer gate is still closed.
- Copying a reference map's map.info for comparison is not redistribution of
  the map's content (text metadata only).
- The comparator does not produce a playable map.
