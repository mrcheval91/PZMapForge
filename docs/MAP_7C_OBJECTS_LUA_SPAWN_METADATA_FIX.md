# MAP-7C: objects.lua and Spawn Metadata Fix

```text
Schema:           pzmapforge.map7c-objects-lua-spawn-fix.v0.1
Claim boundary:   build42_candidate_only_not_load_tested_not_playable
MAP7C_OBJECTS_LUA_METADATA_PACKET_CREATED
OBJECTS_LUA_FIXED_COMMENT_ONLY
SPAWNPOINTS_LUA_UNEMPLOYED_KEY
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-7B basis

MAP-7A manual retest of `empty_grass_v2` produced:
- No lotheader EOF (`LOTH_V3_EOF_NOT_OBSERVED`).
- `IsoMetaGrid.Create finished loading in 11.728 seconds` (`ISO_META_GRID_FINISHED_LOADING`).
- objects.lua failure: `LexState.token2str ArrayIndexOutOfBoundsException index 65022 length 31`.
- Spawn region failure: `NullPointerException in getSpawnRegionsAux`.
- Classification: `LOAD_TEST_FAIL_OBJECTS_LUA`.

The LOTH v3 design from MAP-6Z is preserved. MAP-7C addresses only the Lua metadata gaps.

---

## 2. LOTH progress preserved

The `empty_grass_v3` profile keeps LOTH/LOTP/chunkdata identical to v2:

| Component | Value |
|---|---|
| LOTH total size | 29646 bytes (unchanged) |
| LOTH entry count | 1024 (unchanged) |
| LOTH trailer size | 1048 bytes (unchanged) |
| LOTH trailer SHA-256 | 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7 |
| LOTP size | 1056780 bytes (unchanged) |
| chunkdata size | 1026 bytes (unchanged) |

---

## 3. objects.lua strategy selected: comment_only

### Selected strategy

```lua
-- PZMapForge MAP-7C: no objects or zones for this experimental empty cell.
-- objects.lua is a placeholder. Not load-tested. Not a playable Project Zomboid map.
```

### Rationale

The MAP-7A error `LexState.token2str ArrayIndexOutOfBoundsException index 65022 length 31`
occurred at the Lua lexer level when processing `return {}`. Index 65022 is anomalous
— it overflows the 31-entry Lua 5.1 keyword/token table.

Four possible causes were considered:
1. PZ Lua runtime encodes certain token types with large IDs that overflow the table.
2. The file content is read with unexpected encoding (UTF-8 BOM, encoding mismatch).
3. PZ expects a non-empty structured table (e.g., `version`, zone keys).
4. The Lua file loader doesn't expect any Lua code in objects.lua at all.

A **comment-only file** addresses causes 1, 2, and 4:
- No Lua tokens to evaluate → no token-to-string lookup.
- Pure ASCII → no encoding ambiguity.
- If PZ's objects.lua loader doesn't read for evaluation (just for presence), a comment file is neutral.

### Known risk

`objects_lua_known_risk=build42_may_expect_specific_zone_table_format`

If PZ reads objects.lua and expects specific keys (e.g., a zone definition table), a
comment-only file will produce a nil result. This would likely produce a different error
than the MAP-7A LexState exception — which would itself be progress.

---

## 4. spawnpoints.lua strategy selected: minimal_unemployed_spawnpoint

### Selected strategy

```lua
-- PZMapForge MAP-7C: candidate spawn point for experimental empty cell.
-- Not load-tested. Not a playable Project Zomboid map.
function SpawnPoints()
    return {
        unemployed = {
            { worldX = 0, worldY = 0, posX = 150, posY = 150, posZ = 0 },
        },
    }
end
```

### Rationale

MAP-7A showed `NullPointerException in getSpawnRegionsAux`. The v0-v2 format used
`all = { ... }` as the spawn key. The `unemployed` key is an explicit, documented PZ
profession-based spawn key. Using `unemployed` makes the spawn type explicit and avoids
any ambiguity about whether `all` is recognized.

Additionally, the profession key format (`unemployed`) is commonly seen in workshop maps
and is expected by the spawn selection system.

The `posX = 150, posY = 150` coordinates place the spawn point within the cell bounds
rather than at the edge (posX/Y=128 in v0-v2).

---

## 5. Inspector update (MAP-7C)

`scripts/inspect-build42-candidate-lua-metadata.ps1` now detects:
- `comment_only` objects.lua (all non-empty lines are `--` comments).
- Spawn field presence: worldX, worldY, posX, posY, posZ.
- Spawn key presence: `unemployed`.
- Per-type recommendations for objects.lua and spawnpoints.lua.

---

## 6. Non-claims

- `LOAD_TEST_NOT_PERFORMED`: MAP-7C is implementation and packet preparation only.
- `WRITER_NOT_CHANGED`: only Lua metadata changed; LOTH/LOTP/chunkdata are unchanged.
- No PZ assets copied or read into the repo.
- No repo media/maps writes.
- `PLAYABLE_EXPORT_CLAIM_ALLOWED=false`: binding.

---

## 7. Recommended next human action

Run the MAP-7C controlled retest packet:

```
powershell -ExecutionPolicy Bypass -File .\scripts\prepare-build42-metadata-v3-load-test-packet.ps1 `
    -Output .\.local\map7c-packet
```

Then follow the `MAP_7C_INSTALL_AND_SERVER_WIRING_COMMANDS.md` for human-only copy/wiring.
Record the result in `MAP_7C_LOAD_TEST_RECORD.local-template.md`.

If `objects_lua_error_found=yes` again, MAP-7D should try omitting objects.lua entirely
or using a structured zone table.

If `spawn_region_error_found=no` (resolved), the spawn fix was effective.
If `spawn_region_error_found=yes` still, MAP-7D should investigate the spawnregions
server wiring and the `_spawnregions.lua` format.
