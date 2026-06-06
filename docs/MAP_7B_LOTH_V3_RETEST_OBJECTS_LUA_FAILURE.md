# MAP-7B: LOTH v3 Retest Result and objects.lua Failure Record

```text
Schema:           pzmapforge.map7b-retest-record.v0.1
Claim boundary:   build42_candidate_only_not_load_tested_not_playable
MAP7A_CLEAN_RETEST_RECORDED
LOTH_V3_EOF_NOT_OBSERVED
ISO_META_GRID_FINISHED_LOADING
OBJECTS_LUA_PRIMARY_BLOCKER
SPAWN_REGION_SECONDARY_BLOCKER
LOAD_TEST_FAIL_OBJECTS_LUA
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-7A clean retest basis

MAP-7A produced a human-only load-test packet for `empty_grass_v2`.
The test was run manually with the following confirmed wiring:

| Setting | Value |
|---|---|
| Candidate | pzmapforge_build42_candidate_v2_001 |
| Profile | empty_grass_v2 |
| host.ini servername | PZMF_B42_LOTH_V3_TEST_001 |
| Mods= | pzmapforge_build42_candidate_v2_001 |
| Map= | pzmapforge_build42_candidate_v2_001;Muldraugh, KY |
| spawnregions | references media/maps/pzmapforge_build42_candidate_v2_001/spawnpoints.lua |

---

## 2. Result: LOAD_TEST_FAIL_OBJECTS_LUA

| Field | Value |
|---|---|
| mod_selection_crash | no |
| candidate_loaded | yes |
| old_candidate_contamination | no |
| loth_error_found | **false** |
| lotp_error_found | **false** |
| chunkdata_error_found | **false** |
| iso_meta_grid_finished | **true** |
| objects_lua_error_found | **true** |
| spawn_region_error_found | true (secondary) |
| result | **LOAD_TEST_FAIL_OBJECTS_LUA** |

---

## 3. Evidence from console log

### 3.1 Candidate loaded (no contamination)

```
loading pzmapforge_build42_candidate_v2_001
```
No old candidate log lines observed. Clean run.

### 3.2 No lotheader EOF (progress confirmed)

**No `ERROR loading 0_0.lotheader` appeared.**
**No `IsoLot.readInt EOFException` appeared.**

This confirms that the MAP-6Y/MAP-6Z canonical 1048-byte trailer resolved the prior
lotheader EOF blocker.

### 3.3 IsoMetaGrid finished loading

```
IsoMetaGrid.Create finished loading in 11.728 seconds
```
The map grid loaded. This is further evidence that the binary files (LOTH, LOTP,
chunkdata) did not produce an explicit logged failure in this run.

### 3.4 objects.lua failure (primary blocker)

```
SEVERE: Error found in LUA file:
C:\Users\Palmacede\Zomboid\mods\pzmapforge_build42_candidate_v2_001_test\42\media\maps\
pzmapforge_build42_candidate_v2_001\objects.lua
LuaManager.RunLuaInternal> Exception thrown
java.lang.ArrayIndexOutOfBoundsException: Index 65022 out of bounds for length 31
at LexState.token2str
```

The PZ Lua engine threw an `ArrayIndexOutOfBoundsException` when processing
`objects.lua`. The error originates in `LexState.token2str`, which is the Lua
lexer's token-to-string conversion. Index 65022 is suspiciously large — this suggests
the Lua engine may be treating part of the file as a token ID that overflows the
token name table (length 31).

The current `objects.lua` content is `return {}`. This is valid Lua 5.1 syntax,
but the PZ Lua runtime appears to reject it via this path. The exact cause is not
yet decoded — it may be a PZ Lua version incompatibility, a misread of the file
encoding, or an expected format that differs from bare `return {}`.

### 3.5 Spawn region null / secondary failure (secondary blocker)

```
getSpawnRegionsAux exception
NullPointerException: KahluaTable.iterator because orig is null
no spawn region was chosen
```

The spawn region system also failed. This may be a consequence of the objects.lua
failure (spawn region code may depend on a successfully loaded Lua environment)
or may be a separate gap in the candidate spawnpoints.lua/spawnregions format.

---

## 4. Interpretation

### 4.1 Progress confirmed

The MAP-6T v1 candidate failed immediately at `IsoLot.readInt` (lotheader EOF).
The MAP-7A v3 candidate survived past the LOTH and LOTP loading stages.
`IsoMetaGrid.Create finished loading` is a meaningful step forward.

**The LOTH v3 design (MAP-6Z canonical trailer) is provisionally effective.**

### 4.2 Primary blocker: objects.lua

The `LexState.token2str ArrayIndexOutOfBoundsException` at index 65022 suggests
PZ's Lua engine treats `return {}` differently from the expected file format.

Possible explanations:
1. **Empty table token overflow**: The Lua lexer in this PZ version uses a token index
   that overflows for certain token types in `{}`.
2. **UTF-8 BOM or encoding issue**: The file may be misread if the runtime expects
   a different encoding header.
3. **Expected non-empty table**: PZ may require objects.lua to return a non-empty
   structured table (e.g., with specific keys like `version`).
4. **File not expected at all**: Some PZ Lua file loaders may not expect a standalone
   `return {}` and fail at the lexer before evaluating it.

Resolution for MAP-7C: try alternative objects.lua formats (empty comment, different
return structure, or no file at all with a placeholder).

### 4.3 Secondary blocker: spawn regions

The `NullPointerException` in `getSpawnRegionsAux` may be:
1. A downstream consequence of the objects.lua failure.
2. A separate gap in the spawnpoints.lua / server _spawnregions.lua wiring.

This will be re-evaluated after objects.lua is fixed.

---

## 5. MAP-7B: diagnostics added

`scripts/inspect-build42-candidate-lua-metadata.ps1` inspects a generated candidate
under .local/ without reading PZ install files or copying anything.

It reports:
- mod.info: exists, size, first bytes, ASCII sanity, id field, id matches.
- map.info: exists, size, lots field, lots matches.
- spawnpoints.lua: exists, function/return shape (SpawnPoints-compatible).
- objects.lua: exists, size, content type (empty / return_only / binary_looking / other_lua).

This gives deterministic local evidence about the Lua metadata state of any
generated candidate without requiring a PZ session.

---

## 6. Status labels

```text
MAP7A_CLEAN_RETEST_RECORDED
  -- Manual retest of empty_grass_v2 completed and recorded.

LOTH_V3_EOF_NOT_OBSERVED
  -- No lotheader EOF exception in this run. Prior blocker appears cleared.

ISO_META_GRID_FINISHED_LOADING
  -- IsoMetaGrid.Create finished loading in 11.728 seconds.

OBJECTS_LUA_PRIMARY_BLOCKER
  -- LexState.token2str ArrayIndexOutOfBoundsException in objects.lua.
  -- Current format: return {}. PZ Lua runtime rejected it.

SPAWN_REGION_SECONDARY_BLOCKER
  -- NullPointerException in getSpawnRegionsAux; no spawn region chosen.
  -- May be downstream of objects.lua failure.

LOAD_TEST_FAIL_OBJECTS_LUA
  -- Classification of MAP-7A retest result.

WRITER_NOT_CHANGED
  -- No writer change in MAP-7B.

LOAD_TEST_NOT_PERFORMED
  -- MAP-7B is research and record only.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding.
```

---

## 7. Non-claims

- `LOAD_TEST_NOT_PERFORMED`: MAP-7B records a prior result and adds diagnostics.
- No binary files read from PZ install.
- No writer change.
- No repo media/maps writes.
- No PZ assets copied.
- `PLAYABLE_EXPORT_CLAIM_ALLOWED=false`: binding.

---

## 8. Recommended next task: MAP-7C

MAP-7C: Fix objects.lua and spawn metadata; prepare retest packet.

Goals:
1. Investigate alternative objects.lua formats (comment-only, empty, structured table).
2. Fix spawnpoints.lua / _spawnregions.lua if the spawn null-pointer has a separate cause.
3. Prepare a new load-test packet with the fixed metadata.
4. Target result: LOAD_TEST_PASS or LOAD_TEST_INCONCLUSIVE (no LOTH/LOTP/chunkdata errors).
