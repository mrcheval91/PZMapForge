# MAP-7M: Variant H Failure and Known-Working Map Contract Investigation

```text
MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY
COMMON_LAYOUT_ALONE_INSUFFICIENT
VARIANTS_ABCDEFGH_EXHAUSTED
MAP_FOLDER_DISCOVERY_CONTRACT_UNKNOWN
KNOWN_WORKING_MAP_COMPARATOR_REQUIRED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7M records the Variant H manual retest result. Using the documented
Build 42 `common/media/maps/<MapId>/` layout did NOT register the custom
map folder with IsoMetaGrid.

All eight layout and structural experiments A through H are exhausted.
The map folder discovery contract for custom Build 42 mods remains unknown
from inspection alone. The only path forward is comparison against a
known-working Build 42 map mod structure.

---

## 2. Variant H tested

Configuration:
- Layout: `common/media/maps/<MapId>/` (documented Build 42 structure)
- Includes: thumb.png, worldmap.xml, worldmap-forest.xml, maps/biomemap_0_0.png
- Map= line: Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
- Server: PZMF_B42_METADATA_V4_VARIANT_H_001

```text
classification: MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY
candidate_loaded: true
player_data_received: true
game_loading_completed: true
map_folders_list_empty: true
spawn_building_warning: true
```

Key log evidence:
```text
pzmapforge_build42_candidate_v4_001 loaded
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>
IsoMetaGrid.Create: finished loading
Muldraugh mannequin-zone warning (vanilla/noise)
initSpawnBuildings: no room or building at 150,150,0
Player data received from the server
game loading took 44 seconds
Game Mode: Multiplayer
```

Human observation:
- No city choice.
- Player spawned into fallback/forest-looking world.
- No red error panel. No void.
- Player died; server offered respawn/quit.

IMPORTANT: No city choice is not a decisive failure signal for server retests.
DECISIVE signal: `map_folders_list_empty=true`.
Forest/fallback world means the game loaded, NOT that the custom map registered.
Player death and respawn offer confirm the world runs; they do not confirm
custom map registration.

---

## 3. All experiments A through H exhausted

| Variant | Change | Result |
|---|---|---|
| A | Map= candidate;Muldraugh | SCAN_EMPTY |
| B | Map= candidate only | SCAN_EMPTY |
| C | Map= Muldraugh;candidate | SCAN_EMPTY |
| D | + root media/maps/ | SCAN_EMPTY |
| E | + root mod.info | SCAN_EMPTY |
| F | folder name == mod.info id | SCAN_EMPTY |
| G | mod.info map= field | SCAN_EMPTY |
| H | common/media/maps layout | SCAN_EMPTY |

VARIANTS_ABCDEFGH_EXHAUSTED. Every structural hypothesis so far has failed
to resolve the map folder registration.

---

## 4. Diagnostic signals (maintained)

```text
MAP_FOLDER_SCAN_EMPTY = IsoMetaGrid found zero map folder paths.
  Current blocker for all experiments A through H.
  map_folders_list_empty=true is the decisive signal.

MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING = later-stage failure.
  IsoMetaGrid found a path but no .lotheader files.
  This is PROGRESS, not the same failure.
```

Do NOT investigate LOTH/LOTP/chunkdata until the map folder is registered.
The binary files are never reached while the scan is empty.

---

## 5. Next required step: known-working map comparison

MAP_FOLDER_DISCOVERY_CONTRACT_UNKNOWN: all experiments have been derived
from documentation screenshots and structural hypotheses without direct
comparison to a known-working Build 42 mod.

KNOWN_WORKING_MAP_COMPARATOR_REQUIRED: the only reliable way to identify
what the candidate is missing is to compare it field-by-field against a
mod that is confirmed to appear in IsoMetaGrid's map folder scan list.

The comparator `scripts/inspect-build42-known-working-map-contract.ps1`
accepts both candidate and reference roots under `.local/` only. The human
must first obtain a known-working Build 42 map mod and place it under
`.local/` before running the comparator.

---

## 6. Decision tree for comparator results

| Finding | Next task |
|---|---|
| Reference has mod.info fields candidate lacks | Metadata contract alignment |
| Reference has map.info fields candidate lacks | map.info contract alignment |
| Reference uses 42.0/ instead of 42/ | Exact version-folder experiment |
| Reference has Workshop-only metadata | Local-mod vs Workshop registration investigation |
| Candidate lacks files reference has | Add missing non-binary placeholders |
| Contract matches but scan still empty | Isolated clean-client/server contamination test |

---

## 7. Scripts

| Script | Purpose |
|---|---|
| `scripts/inspect-build42-known-working-map-contract.ps1` | Field-by-field comparator (both roots under .local only) |
| `scripts/prepare-build42-map7m-known-working-contract-packet.ps1` | Generates candidate + reference placeholder + instructions |
| `scripts/test-build42-map7m-known-working-contract.ps1` | 12 test assertions |

---

## 8. Non-claims

- No load test was performed by any script in this slice.
- Forest/fallback world does NOT confirm custom map loaded.
- Player death/respawn does NOT confirm custom map registered.
- LOTH/LOTP/chunkdata binary files are NOT the current blocker.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
