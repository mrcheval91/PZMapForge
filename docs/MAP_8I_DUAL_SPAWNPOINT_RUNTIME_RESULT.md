# MAP-8I: Dual Spawnpoint Keys Runtime Result

```text
MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED
DUAL_SPAWNPOINT_KEYS_PRESENT=true
SPAWNPOINT_PROFESSION_ERROR_REMOVED=true
PLAYER_SPAWN_COORDINATE=10746,8288,0
SPAWN_COORDINATE_MATCHES_35_27=true
ISO_META_GRID_MAP_FOLDER_LIST_EMPTY=true
SPAWNED_IN_FALLBACK_OR_UNCONFIRMED_GENERATED_CONTENT=true
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
```

---

## 1. Context

MAP-8H staged a parent/child Workshop package (Workshop ID 3740642200) and
the operator ran it. The MAP-8H runtime produced the error:

```text
there is no spawn point table for the player's profession
```

MAP-8I manually patched both map folders in the Workshop source to add dual
spawnpoint keys: both `unemployed` and `Profession_Unemployed`.

---

## 2. Patch applied

Both spawnpoints.lua files patched to return both keys:

```lua
unemployed = { { worldX=35, worldY=27, posX=246, posY=188, posZ=0 } }
Profession_Unemployed = { { worldX=35, worldY=27, posX=246, posY=188, posZ=0 } }
```

Files patched:

```text
common\media\maps\PZMapForge\spawnpoints.lua
common\media\maps\pzmapforge_build42_candidate_v4_001\spawnpoints.lua
```

Downloaded Workshop verification was green in both cache roots:

```text
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3740642200
D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\steamapps\workshop\content\108600\3740642200
```

---

## 3. Runtime result

Server INI:

```ini
WorkshopItems=3740642200
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
```

City selector: operator selected long child city name
`pzmapforge_build42_candidate_v4_001`.

```text
Player fully connected at:  10746,8288,0
Player disconnected at:     10773,8288,0
```

---

## 4. Coordinate interpretation

```text
spawn_coordinate = 10746,8288,0

worldX = floor(10746 / 300) = 35
worldY = floor(8288  / 300) = 27
posX   = 10746 - (35 * 300) = 246
posY   = 8288  - (27 * 300) = 188

35 * 300 + 246 = 10746  (confirmed)
27 * 300 + 188 = 8288   (confirmed)
```

Player spawned at exactly the intended 35_27 cell coordinate.
`worldX=35, worldY=27, posX=246, posY=188` was the MAP-8H spawnpoint target.

---

## 5. What is resolved

```text
workshop_ready                          = true
server_map_line_correct                 = true
parent_child_layout_downloaded          = true
dual_spawnpoint_keys_present            = true
spawnpoint_profession_error_removed     = true
city_selector_works                     = true
player_spawn_coordinate                 = 10746,8288,0
spawn_coordinate_matches_35_27          = true
```

The old blocker "there is no spawn point table for the player's profession"
is removed. Mod loading, city selector, and spawnpoint placement are all
working correctly.

---

## 6. Remaining blocker

Visual result showed vanilla/fallback terrain. IsoMetaGrid did not list the
PZMapForge parent map folder. The player spawns at the correct 35_27 coordinate
but generated cell content is not confirmed loaded.

```text
iso_meta_grid_map_folder_list_empty                  = true
spawned_in_fallback_or_unconfirmed_generated_content = true
```

Classification: `MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED`

Remaining candidates for the cell-mount blocker:

1. Generated `35_27.lotheader` / `world_35_27.lotpack` / `chunkdata_35_27.bin`
   are not valid enough for Build 42 cell loading.
2. Parent folder lacks required sidecars accepted by IsoMetaGrid
   (e.g. `worldmap.xml.bin`, `streets.xml.bin` in the correct binary format).
3. Parent `map.info` is too skeletal (missing fields compared to a known-working
   parent folder that contains cell content).

---

## 7. Observed noise (not classified as blockers)

WorldGen/IsoPropertyType missing property errors in the server log:

```text
ladderW, ladderS, ladderE, ladderN
Countertop, CountertopAttach, FitsBeneathCountertop, WindowShape
```

These appeared in prior MAP-8H runs too. Treated as background noise unless
directly linked to IsoMetaGrid/cell-loading failure evidence.

---

## 8. Binary writer gate

```text
BINARY_WRITER_GATE_STILL_CLOSED

Gate opens when:
  IsoMetaGrid lists PZMapForge parent folder in map folder scan
  OR: explicit lotheader parse attempt logged for PZMapForge parent folder
```

---

## 9. Next branch

```text
next_branch=parent_metadata_or_binary_cell_mount_contract
```

Priority order:

1. Add valid worldmap.xml.bin / streets.xml.bin to PZMapForge parent folder.
   Sidecar binary format required; ASCII stubs ruled out (MAP-8B/8D evidence).
2. Compare parent map.info against a known-working parent folder with cell content.
3. If sidecar + map.info alignment still fails: binary writer investigation.

---

## 10. Claim boundary

```text
MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED
DUAL_SPAWNPOINT_KEYS_PRESENT=true
SPAWNPOINT_PROFESSION_ERROR_REMOVED=true
PLAYER_SPAWN_COORDINATE=10746,8288,0
SPAWN_COORDINATE_MATCHES_35_27=true
ISO_META_GRID_MAP_FOLDER_LIST_EMPTY=true
SPAWNED_IN_FALLBACK_OR_UNCONFIRMED_GENERATED_CONTENT=true
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
```

Non-claims:

- No playable PZMapForge export claimed.
- Binary writer gate remains closed.
- Cell content in Workshop package is generated PZMapForge-owned files only.
- Coordinate match to 35_27 is spawnpoint evidence, not cell-load proof.
- Spawned_in_fallback means IsoMetaGrid did not mount the generated cell.
