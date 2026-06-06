# MAP-7F: Build 42 Map Folder Registration Diagnostic

```text
MAP_FOLDER_SCAN_EMPTY_CONFIRMED
MAP_FOLDER_REGISTRATION_BLOCKER_ACTIVE
ANALYZER_TIMESTAMPED_LOG_BUG_FIXED
MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7E confirmed that the empty_grass_v4 candidate loads and reaches the
multiplayer in-game state in 32 seconds, but the world is empty.

IsoMetaGrid.Create scanned directories and found no map folders. The
candidate map folder was not discovered or registered.

MAP-7F records this confirmed state, fixes an analyzer bug that caused
incorrect map_folders_list_empty reporting on timestamped DebugLog format,
and produces a focused registration diagnostic packet.

---

## 2. Confirmed MAP-7E diagnostics

### 2.1 Cleared blockers

| Blocker | Status |
|---|---|
| objects.lua LexState / BOM | CLEARED: no-BOM rewrite applied in MAP-7D |
| server spawnregions.lua BOM LexState | CLEARED |
| spawn null error | CLEARED |
| player-data timeout | CLEARED |

### 2.2 Load progression

```text
loading pzmapforge_build42_candidate_v4_001
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>
IsoMetaGrid.Create: finished scanning directories in 0.034 seconds
IsoMetaGrid.Create: begin loading
IsoMetaGrid.Create: finished loading in 11.445 seconds
initSpawnBuildings: no room or building at 150,150,0
Player data received from the server
game loading took 32 seconds
Game Mode: Multiplayer
```

### 2.3 Remaining blocker

IsoMetaGrid scanned directories and found NO map folders. The candidate
map folder pzmapforge_build42_candidate_v4_001 did not appear in the
map folder list. The world is empty because no map cells were loaded.

Spawn warning: `initSpawnBuildings: no room or building at 150,150,0`
persists because no rooms or buildings exist without a registered map.

---

## 3. Analyzer bug fixed (MAP-7F)

### 3.1 Bug description

`scripts/inspect-build42-map7d-load-result.ps1` reported
`map_folders_list_empty=False` when processing a timestamped DebugLog format
log, despite the map folder list visibly being empty.

### 3.2 Root cause

The analyzer used a regex:

```text
'Looking in these map folders:\r?\n(.*?)<End of map-folders list>'
```

In timestamped DebugLog format, each line is:

```text
[date] LOG : General     , 1594854399842> Looking in these map folders:.
[date] LOG : General     , 1594854399842> <End of map-folders list>.
```

The trailing period after `Looking in these map folders:` broke the
`\r?\n` match. The log prefix before `<End of map-folders list>` on
the next line broke the end-marker match.

The fallback regex also failed for the same reasons. Result:
`map_folders_list_empty=False` even though the list was empty.

### 3.3 Fix

The analyzer now uses a line-by-line parser that:
1. Strips the timestamped log prefix: `[date] LOG : category , timestamp> `
2. Strips trailing periods from semantic message text
3. Finds the start marker line (by semantic content)
4. Finds the end marker line
5. Counts non-empty semantic lines between them

Both bare console format and timestamped DebugLog format now correctly
produce `map_folders_list_empty=True` when no folder entries appear
between the markers.

### 3.4 New fields

| Field | Description |
|---|---|
| `map_folder_lines` | Array of semantic folder lines extracted |
| `map_folder_parser_strategy` | `bare_console` or `timestamped_debuglog` |
| `timestamped_debuglog_detected` | True when timestamped prefix detected |

---

## 4. Registration diagnostic

### 4.1 Hypotheses for empty map folder list

| Hypothesis | To test |
|---|---|
| Map= server ini does not include candidate ID | Test variants A / B / C |
| spawnregions.lua missing or wrong path | Verify presence and no-BOM |
| server _spawnregions.lua format | Verify no-BOM and content |
| lotheader/lotpack rejection before map registration | Check for binary errors |

### 4.2 Map= line variants

Three variants to test in separate manual runs (HUMAN-ONLY writes, no-BOM):

```text
A: Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
B: Map=pzmapforge_build42_candidate_v4_001
C: Map=Muldraugh, KY;pzmapforge_build42_candidate_v4_001
```

Each variant must be tested with a fresh log capture. Record results in
`.local/map7f-packet/MAP_7F_MANUAL_RETEST_RECORD.local-template.md`.

### 4.3 Log capture targets

For each retest, collect:
- Client DebugLog (IsoMetaGrid map folder scan section)
- Server DebugLog-server (any map registration errors)
- coop-console.txt (startup sequence)

Run `scripts/inspect-build42-map7d-load-result.ps1` on the captured client
DebugLog and record all fields, especially:
- `map_folders_scan_found`
- `map_folders_list_empty`
- `map_folders_list_count`
- `map_folder_parser_strategy`
- `timestamped_debuglog_detected`
- `classification`

---

## 5. Script deliverables

| Script | Purpose |
|---|---|
| `scripts/inspect-build42-map7d-load-result.ps1` | Analyzer (timestamped log fix applied) |
| `scripts/prepare-build42-map7f-registration-diagnostic-packet.ps1` | Generates MAP-7F packet under .local/ |
| `scripts/test-build42-map7f-registration-diagnostic.ps1` | Tests: analyzer fix + packet |

Packet output files (all under .local/):
- `MAP_7F_REGISTRATION_DIAGNOSTIC_PACKET.md`
- `MAP_7F_MANUAL_RETEST_RECORD.local-template.md`
- `MAP_7F_MAP_LINE_VARIANTS_TO_TEST.md`
- `MAP_7F_LOG_CAPTURE_AND_ANALYSIS_COMMANDS.md`
- `map7f-registration-preflight.json`
- `map7f-registration-preflight.md`

---

## 6. Non-claims

- This document is a controlled diagnostic only.
- No load test was performed by any script in this slice.
- The empty world state is NOT a passing load test.
- Binary writer behavior was NOT changed.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
