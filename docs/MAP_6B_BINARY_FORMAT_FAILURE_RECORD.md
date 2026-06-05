# MAP-6B: Build 42 Binary Format Failure Record

```text
Schema:           pzmapforge.failure-record.v0.1
Claim boundary:   evidence_record_only_not_load_tested_not_playable
PZ build:         Build 42
Session:          manual-b42-test-001 (continued from MAP-6A)
MAP-6A status:    DISCOVERY_PASS_VERSIONED_LAYOUT
Binary status:    BINARY_FAILURE_CONFIRMED
objects.lua:      OBJECTS_LUA_FAILURE_CONFIRMED
Playable claim:   PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-6B records the runtime failure evidence from the MAP-6A manual load test.

MAP-6A established that the versioned loose-mod layout works and that PZ loads
the mod and discovers the map files. MAP-6B records what happened when PZ
attempted to load the cell data: the placeholder binary files failed.

This document supersedes the binary hypothesis status in MAP-6A section 5.
Those hypotheses were marked PLAUSIBLE based on the absence of a crash during
mod registration. Runtime cell loading produced definitive failures:

- `0_0.lotheader` failed with `java.io.EOFException` at `IsoLot.readInt`.
- `CellLoader` and `IsoCell.PlaceLot` logged repeated failures.
- `objects.lua` failed with `Error found in LUA file`.

No playable export claim is permitted. Real PZ binary format implementation
is required before any further load-test attempt.

---

## 2. Status labels

```text
DISCOVERY_PASS_VERSIONED_LAYOUT     — confirmed in MAP-6A; still holds
MAP_FILES_DISCOVERED_BY_PZ          — confirmed in MAP-6A; still holds
BINARY_FAILURE_CONFIRMED            — new in MAP-6B
OBJECTS_LUA_FAILURE_CONFIRMED       — new in MAP-6B
PLAYABLE_EXPORT_CLAIM_ALLOWED=false — binding; not changed by any MAP-6B evidence
```

---

## 3. What MAP-6A confirmed (still valid)

| Finding | Status |
|---|---|
| Versioned loose-mod layout (`<mods>/<folder>/42/mod.info`) works | CONFIRMED |
| PZ log shows "loading pzmapforge_manual_b42_001_42" | CONFIRMED |
| PZ sees the map directory and files | CONFIRMED |
| Game reached loading phase (beyond mod registration and spawn selection) | CONFIRMED |
| Custom spawn location visible in spawn list (maptest_a variant) | CONFIRMED |

MAP_FILES_DISCOVERED_BY_PZ is confirmed: PZ found and attempted to load:
- `0_0.lotheader`
- `chunkdata_0_0.bin`
- `map.info`
- `objects.lua`
- `spawnpoints.lua`
- `world_0_0.lotpack`

---

## 4. Runtime failure log evidence

The game reached loading then showed "Sorry, an unexpected error occurred."

### 4.1 lotheader failure

```
ERROR loading C:\Users\Palmacede\Zomboid\mods\pzmapforge_manual_b42_001_maptest_a\42\media\maps\pzmapforge_manual_b42_001\0_0.lotheader
java.io.EOFException at IsoLot.readInt
IsoMetaGrid$MetaGridLoaderThread.loadCell
IsoLot.load EOFException
```

The 8-byte placeholder lotheader was rejected. `IsoLot.readInt` hit EOF while
trying to parse tileset entry data. The 0-entry hypothesis (zero header +
U32 LE 0 for entry count) does not produce a valid lotheader for Build 42.

### 4.2 CellLoader / IsoCell failures

```
Failed to load chunk, blocking out area
IsoCell.PlaceLot IndexOutOfBoundsException
```

These failures are downstream of the lotheader failure. They cannot be
evaluated independently until the lotheader is fixed.

### 4.3 objects.lua failure

```
Error found in LUA file: ...\objects.lua
LuaManager.RunLuaInternal exception
```

The placeholder objects.lua (comment-only, no return value) was not accepted.
PZ expects a Lua file that evaluates successfully, likely returning a table or
a no-op that the engine can process. Comment-only files are invalid.

Note: fixing objects.lua alone does not fix the lotheader failure. Binary
format research is the blocking dependency.

---

## 5. Binary hypothesis status after MAP-6B evidence

The following table supersedes MAP-6A section 5.

| File | Prior status (MAP-6A) | Current status (MAP-6B) | Basis |
|---|---|---|---|
| `.lotheader` 0-entry placeholder | PLAUSIBLE (no crash at registration) | FAILING_PLACEHOLDER_FORMAT | EOFException at IsoLot.readInt confirmed |
| `.lotpack` zero-offset table | PLAUSIBLE (no crash at registration) | UNPROVEN_AFTER_LOTHEADER_FAILURE | Not reached due to lotheader failure |
| `chunkdata_*.bin` 902-byte all-zero | PLAUSIBLE (no crash at registration) | UNPROVEN_AFTER_LOTHEADER_FAILURE | Not reached due to lotheader failure |
| `objects.lua` comment-only | not separately hypothesized | INVALID_OR_NOT_ACCEPTED | LuaManager.RunLuaInternal exception confirmed |

Runtime status fields (matches experimental report JSON):
```text
binary_runtime_status      = failing_placeholder_format
lotheader_runtime_status   = eof_exception_observed
lotpack_runtime_status     = unproven_after_lotheader_failure
chunkdata_runtime_status   = unproven_after_lotheader_failure
objects_lua_runtime_status = invalid_or_not_accepted
```

---

## 6. What this means for the next slice

The placeholder binary format hypothesis is falsified. The next required work is:

1. **Research the real `.lotheader` binary format** — the 8-byte zero hypothesis
   is wrong. The file must contain a valid tileset entry table that `IsoLot.readInt`
   can parse without hitting EOF.

2. **Implement a real `.lotheader` writer** — based on the MAP-4E string-table
   evidence (bytes 4-7 = entry count U32 LE; bytes 8+ = newline-delimited tileset
   names). The zero-entry hypothesis is the most likely starting point if PZ
   accepts an empty list; but the format of the count field and delimiter must be
   verified.

3. **Fix objects.lua** — the file must return a valid Lua expression. The minimal
   candidate is `return {}`. This does not unblock the lotheader failure.

4. **Re-test only after binary format is fixed** — no new load test is permitted
   against the current placeholder binary output.

MAP-4 remains blocked on real binary format implementation.
MAP-5B remains LOAD_TEST_INCONCLUSIVE.

---

## 7. Non-claims

- This document does not claim the binary format is now understood.
- This document does not claim the mod will load after any partial fix.
- MAP-5B remains LOAD_TEST_INCONCLUSIVE.
- MAP-6A discovery findings are unaffected.
- No playable Project Zomboid export claim.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
