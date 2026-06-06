# MAP-7D: Timeout and Lua Encoding Fix

```text
Schema:           pzmapforge.map7d-timeout-lua-encoding-fix.v0.1
Claim boundary:   build42_candidate_only_not_load_tested_not_playable
MAP7C_MANUAL_RETEST_RECORDED
LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA
LOTH_V3_EOF_NOT_OBSERVED
ISO_META_GRID_FINISHED_LOADING
OBJECTS_LUA_ERROR_FOUND=true
SPAWN_REGION_ERROR_FOUND=true
TIMEOUT_WAITING_PLAYER_DATA=true
UTF8_BOM_HYPOTHESIS_RAISED
OBJECTS_LUA_NO_BOM_FIX_APPLIED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED_BINARY
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-7C manual retest result

### 1.1 Wiring (correct)

| Setting | Value |
|---|---|
| Candidate | pzmapforge_build42_candidate_v3_001 |
| Profile | empty_grass_v3 |
| host.ini servername | PZMF_B42_METADATA_V3_TEST_001 |
| Mods= | pzmapforge_build42_candidate_v3_001 |
| Map= | pzmapforge_build42_candidate_v3_001;Muldraugh, KY |
| spawnregions | references media/maps/.../spawnpoints.lua |

### 1.2 Classification: LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA

| Field | Value |
|---|---|
| loth_error_found | **false** |
| lotp_error_found | **false** |
| chunkdata_error_found | **false** |
| objects_lua_error_found | **true** |
| spawn_region_error_found | **true** |
| timeout_waiting_player_data | **true** |
| result | **LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA** |

### 1.3 Evidence from log

**Progress confirmed (no lotheader EOF):**
```
loading pzmapforge_build42_candidate_v3_001
IsoMetaGrid.Create finished loading in 11.717 seconds
WorldDictionary.init ended
WorldStreamer.isBusy() loop ended
```

**Final hard failure:**
```
java.lang.RuntimeException: Timed out waiting for the server to send player data
at IsoWorld.init(IsoWorld.java:2505)
```

**objects.lua still failed (same error as MAP-7A):**
```
SEVERE: Error found in LUA file:
C:\...\pzmapforge_build42_candidate_v3_001_test\42\media\maps\...\objects.lua
LuaManager.RunLuaInternal> Exception thrown
java.lang.ArrayIndexOutOfBoundsException: Index 65022 out of bounds for length 31
at LexState.token2str
```

**Spawn region still failed:**
```
getSpawnRegionsAux / getServerSpawnRegions
NullPointerException: Cannot invoke KahluaTable.iterator() because orig is null
```

### 1.4 Progress vs MAP-7A

The LOTH v3 canonical trailer (MAP-6Z) continues to clear the prior lotheader EOF.
IsoMetaGrid, WorldDictionary, and WorldStreamer all progressed further than MAP-7A.

The PZ error panel showed error codes 4, 6, 7.

---

## 2. The no-BOM hypothesis

### 2.1 Why comment-only objects.lua still fails

MAP-7C switched objects.lua from `return {}` to a comment-only file. The same
`LexState.token2str ArrayIndexOutOfBoundsException index 65022 length 31` persisted.

Index 65022 is close to 0xFE1E. In UTF-8, the BOM is 0xEF 0xBB 0xBF. When the Lua
lexer reads the first byte 0xEF (239), it may interpret it as a token ID of 239 or
combine BOM bytes into a multi-byte token index that overflows the 31-entry keyword
table.

MAP-7C confirmed that `File.WriteAllText` with `Encoding.UTF8` in .NET emits a UTF-8
BOM (0xEF 0xBB 0xBF) at the start of text files (verified: first 3 bytes of the
generated objects.lua are `EF BB BF`).

The PZ Lua engine likely does not handle a UTF-8 BOM and misinterprets it as a
malformed token → `LexState.token2str` with index 65022 (a BOM-derived value).

### 2.2 No-BOM hypothesis summary

- **Evidence**: MAP-7B inspector confirmed BOM on v3 files; MAP-7C did not remove it.
- **Hypothesis**: UTF-8 BOM causes PZ Lua lexer to misparse; removing it allows parsing.
- **Risk**: Even without BOM, PZ may expect a specific Lua file format. But BOM removal
  is the minimum necessary step before any format-level investigation.

### 2.3 Timeout: secondary failure

The `Timed out waiting for the server to send player data` is likely downstream of the
objects.lua failure. If objects.lua causes a Lua state corruption, the server initialization
sequence may stall indefinitely.

The spawn region NullPointerException may also be downstream: if the Lua state is
corrupted by the objects.lua error, spawn region lookup returns null tables.

---

## 3. MAP-7D: no-BOM candidate profile empty_grass_v4

### 3.1 Changes from v3

MAP-7D adds `empty_grass_v4`. All LOTH/LOTP/chunkdata are unchanged from v3:

| Component | Value |
|---|---|
| LOTH size | 29646 bytes |
| LOTH entry count | 1024 |
| LOTH trailer size | 1048 bytes |
| LOTH trailer SHA-256 | 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7 |
| LOTP size | 1056780 bytes |
| chunkdata size | 1026 bytes |

Changes:
- **objects.lua**: comment-only, written with `new UTF8Encoding(false)` (no BOM)
- **spawnpoints.lua**: unemployed key format, written with `new UTF8Encoding(false)` (no BOM)
- **mod.info**: written with `new UTF8Encoding(false)` (no BOM)
- **map.info**: written with `new UTF8Encoding(false)` (no BOM)
- **README**: written with `new UTF8Encoding(false)` (no BOM)
- **All binary files**: unchanged (LOTH/LOTP/chunkdata use byte arrays, no encoding)

### 3.2 Inspector update

`scripts/inspect-build42-candidate-lua-metadata.ps1` now reports `has_bom` for each
inspected file. The MAP-7D preflight verifies all game-read text files have no BOM.

---

## 4. Status labels

```text
MAP7C_MANUAL_RETEST_RECORDED
  -- Manual retest of empty_grass_v3 completed and recorded.

LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA
  -- Final failure: Timed out waiting for server to send player data.

LOTH_V3_EOF_NOT_OBSERVED
  -- No lotheader EOF in MAP-7C retest.

ISO_META_GRID_FINISHED_LOADING
  -- IsoMetaGrid.Create finished loading in 11.717 seconds.

OBJECTS_LUA_ERROR_FOUND=true
  -- Same LexState.token2str error persisted. Hypothesis: UTF-8 BOM.

SPAWN_REGION_ERROR_FOUND=true
  -- NullPointerException in getSpawnRegionsAux. Likely downstream of objects.lua.

TIMEOUT_WAITING_PLAYER_DATA=true
  -- RuntimeException at IsoWorld.init: player data timeout.

UTF8_BOM_HYPOTHESIS_RAISED
  -- UTF-8 BOM (EF BB BF) detected in v3 generated files. Likely cause of lexer failure.

OBJECTS_LUA_NO_BOM_FIX_APPLIED
  -- empty_grass_v4 uses UTF8Encoding(false) for all game-read text files.

LOAD_TEST_NOT_PERFORMED
  -- MAP-7D is implementation and packet preparation only.

WRITER_NOT_CHANGED_BINARY
  -- LOTH/LOTP/chunkdata unchanged. Only text file encoding changed for v4.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding.
```

---

## 5. Non-claims

- `LOAD_TEST_NOT_PERFORMED`: MAP-7D is implementation and packet preparation only.
- `WRITER_NOT_CHANGED_BINARY`: no change to LOTH/LOTP/chunkdata.
- No PZ assets copied or read into the repo.
- No repo media/maps writes.
- `PLAYABLE_EXPORT_CLAIM_ALLOWED=false`: binding.
- v0/v1/v2/v3 profiles are unchanged.
