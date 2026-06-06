# MAP-7E: Empty World and Map Registration Diagnostics

```text
Schema:           pzmapforge.map7e-empty-world-diagnostics.v0.1
Claim boundary:   controlled_partial_load_proof_not_public_playable_map
MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD
MAP7D_NO_BOM_FIX_EFFECTIVE
OBJECTS_LUA_LEXSTATE_CLEARED
SERVER_SPAWNREGIONS_BOM_CLEARED
SPAWN_REGION_NULL_CLEARED
PLAYER_DATA_TIMEOUT_CLEARED
PLAYER_DATA_RECEIVED=true
GAME_LOADING_COMPLETED=true
ENTERED_INGAME_STATE=true
MAP_FOLDERS_LIST_EMPTY=true
SPAWN_BUILDING_WARNING=true
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED
```

---

## 1. MAP-7D retest result (no-BOM server files)

### 1.1 First run failure (server files had BOM)

The first MAP-7D retest showed a red error panel. Investigation revealed the server
files written manually still had UTF-8 BOM:
- `C:\Users\Palmacede\Zomboid\Server\PZMF_B42_METADATA_V4_TEST_001.ini`
- `C:\Users\Palmacede\Zomboid\Server\PZMF_B42_METADATA_V4_TEST_001_spawnregions.lua`
- `C:\Users\Palmacede\Zomboid\Lua\host.ini`

After rewriting all server files with UTF-8 no-BOM (PowerShell `[System.IO.File]::WriteAllText`
with `[System.Text.UTF8Encoding]::new($false)`), the retest progressed.

### 1.2 Successful no-BOM retest

| Field | Value |
|---|---|
| candidate | pzmapforge_build42_candidate_v4_001 |
| profile | empty_grass_v4 |
| candidate_loaded | **true** |
| loth_error_found | **false** |
| lotp_error_found | **false** |
| chunkdata_error_found | **false** |
| objects_lua_error_found | **false** |
| server_spawnregions_bom_error | **false** |
| spawn_region_null_error | **false** |
| timeout_waiting_player_data | **false** |
| player_data_received | **true** |
| game_loading_completed | **true** — game loading took 32 seconds |
| entered_ingame_state | **true** — exit zombie.gameStates.GameLoadingState |
| map_folders_list_empty | **true** |
| spawn_building_warning | **true** — no room or building at 150,150,0 |
| no_city_choice | **true** |
| user_observed_empty_world | **true** |
| result | **MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD** |

### 1.3 Log evidence (key lines)

```
loading pzmapforge_build42_candidate_v4_001
Player data received from the server
game loading took 32 seconds
STATE: exit zombie.gameStates.GameLoadingState
Game Mode: Multiplayer
STATE: exit zombie.gameStates.IngameState
Looking in these map folders:
<End of map-folders list>
initSpawnBuildings: no room or building at 150,150,0
```

---

## 2. Cleared blockers

The following blockers from prior retests are now cleared:

| Blocker | Status | Map pass |
|---|---|---|
| lotheader EOFException at IsoLot.readInt | **CLEARED** | MAP-6Z: LOTH v3 + trailer |
| objects.lua LexState.token2str (UTF-8 BOM) | **CLEARED** | MAP-7D: no-BOM v4 |
| server spawnregions.lua LexState (server BOM) | **CLEARED** | MAP-7D: no-BOM server files |
| spawn region NullPointerException | **CLEARED** | MAP-7D: unemployed key format |
| player-data timeout | **CLEARED** | MAP-7D: no-BOM fix stack |

**IsoMetaGrid.Create finished loading is confirmed.** This is a meaningful milestone.

---

## 3. Remaining issue: empty world and map registration

### 3.1 Observed

- Game loaded into a world state.
- No city choice appeared.
- World appeared empty (no visible custom content).
- Log shows `map_folders_list_empty=true`: the map folder scan found no registered folders.
- Log shows `no room or building at 150,150,0`: the spawn point exists but has no room/building attached.

### 3.2 Diagnostic branches

**Branch 1: Map folder not registered by server**

The server `Map=` line or mod metadata may not be routing PZ's map scan to the candidate
folder. PZ scans map folders listed by active mods and the server `Map=` field. If the
mod's `media/maps/<map_id>/` is not discovered, the map folder list is empty.

Evidence: `Looking in these map folders: <End of map-folders list>`.

Possible cause: the server ini `Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY`
may not be the correct format to register a new map folder alongside the base world.
Or: the mod.info or map.info may not be exposing the map correctly.

**Branch 2: Map folder registered too late**

IsoMetaGrid scans map folders during init. If the mod's map folder is discovered only
after IsoMetaGrid initialises, the candidate cell may not be included in the grid.

**Branch 3: Cell content too minimal**

The candidate cell (0_0.lotheader + lotpack + chunkdata) may load correctly but
contain no visible content. The world appears empty because the 0_0 cell
does not contain roads, buildings, or terrain features — just grass overlay entries.
The 150,150 spawn point may land in an empty part of the base world rather than in
the candidate cell region.

**Branch 4: Spawn point at 150,150,0 not tied to a room/building**

The `initSpawnBuildings: no room or building at 150,150,0` warning means the spawn
point is not inside a PZ building. This is expected for an empty cell (no buildings
were placed). The warning is non-fatal — the game still loads.

**Branch 5: City choice absent (single region, auto-spawn)**

The server may have auto-selected a spawn because only one spawn region exists and
it has no ambiguity. Or the `Game Mode: Multiplayer` path skips the city choice UI.

---

## 4. MAP-7E: diagnostic packet

MAP-7E prepares a diagnostic packet for the next controlled retest. Goals:
1. Capture whether the map folder appears in the log scan.
2. Capture whether the player is in the candidate cell (world coordinates).
3. Determine whether the map name appears in IsoMetaGrid output.
4. Verify no-BOM is still in effect on all server files.

Script: `scripts/prepare-build42-map7e-diagnostics-packet.ps1`
Analyzer: `scripts/inspect-build42-map7d-load-result.ps1`

---

## 5. Claim boundary

This is a **controlled partial in-game load proof**. It is NOT:
- A playable custom map claim.
- A map replacement claim.
- A content-complete claim.
- A compatibility verification.

The candidate cell may not be visually distinguishable from the base world.
No public announcement or compatibility statement is permitted.

`PUBLIC_PLAYABLE_CLAIM_ALLOWED=false` is binding.

---

## 6. Status labels

```text
MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD
MAP7D_NO_BOM_FIX_EFFECTIVE
OBJECTS_LUA_LEXSTATE_CLEARED
SERVER_SPAWNREGIONS_BOM_CLEARED
SPAWN_REGION_NULL_CLEARED
PLAYER_DATA_TIMEOUT_CLEARED
PLAYER_DATA_RECEIVED=true
GAME_LOADING_COMPLETED=true
ENTERED_INGAME_STATE=true
MAP_FOLDERS_LIST_EMPTY=true
SPAWN_BUILDING_WARNING=true
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED
```
