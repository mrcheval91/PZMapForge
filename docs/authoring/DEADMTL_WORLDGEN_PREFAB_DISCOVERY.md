# DeadMTL WorldGen Prefab Discovery

## Why MAP-22B exists

MAP-22A established that only two WorldGen prefab keys are currently proven:
- `normal_road_WE_00` (WE strip, VISUAL_CONFIRMED)
- `highway_NS_00` (NS strip, VISUAL_CONFIRMED)

These two keys represent only WE and NS infinite strips. They cannot represent turns,
intersections, local streets, alleys, or any road type beyond straight one-directional
strips. Montreal-scale road network authoring requires knowing whether PZ Build 42
exposes additional prefab keys for these cases.

MAP-22B is a runtime discovery probe. It runs a Lua script in PZ Build 42 that iterates
`worldgen.prefabs` and prints every key it finds to the PZ log. The results tell us
what prefab keys actually exist in the engine, beyond what is currently registered.

## This is not a visual proof

MAP-22B is a discovery step only. It does not prove any prefab visually.

The probe:
- Iterates `worldgen.prefabs` in Lua and prints all keys
- Filters keys containing road-like substrings (road, highway, street, alley, lane, path, rail, bridge, we, ns)
- Prints machine-readable markers to the PZ log
- Does NOT assign any terrain modules (worldgen["static_modules"] is empty)
- Does NOT alter any tile or biome in the world

Running this probe and finding a key does NOT mean that key works correctly or looks
as expected. A found key must go through the full proof chain before being marked
VISUAL_CONFIRMED.

## Discovery to proof chain

Finding a key in MAP-22B output is only step 1:

```
MAP-22B: key appears in worldgen.prefabs dump
  -> MAP-22C: add key to WorldGenRegistry (with KNOWN_IN_CODE status)
  -> MAP-22C: build a proof board swatch for the key
  -> MAP-22C: compile and install the proof board
  -> MAP-22C: human spawns in PZ and visually confirms the terrain
  -> MAP-22C: mark key VISUAL_CONFIRMED in registry and docs
```

Do not add any key to `WorldGenRegistry.cs` without step 2.
Do not mark any key VISUAL_CONFIRMED without step 5.

## If no alley or local road keys are found

If MAP-22B reveals no alley, local street, or ruelle-specific prefab keys in
`worldgen.prefabs`, then alleys and local streets remain SYSTEM_2_REQUIRED.

In that case:
- MAP-22D (System 2 static tile overlay) becomes the path forward for alleys
- WorldGen cannot represent local streets or ruelles at any level of detail
- Do not claim WorldGen alley support without discovered and proven keys

## Commands

### Generate and preview the probe Lua

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\generate-worldgen-prefab-dump-lua.ps1
```

Output: `.local/deadmtl-authoring/worldgen-prefab-dump/WorldGenOverride.lua`

### Install the probe to PZ (required for runtime)

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-worldgen-prefab-dump.ps1 -Install
```

This:
1. Generates the probe Lua
2. Backs up the existing WorldGenOverride.lua
3. Installs the probe as WorldGenOverride.lua in the PZ game map folder
4. Clears the save folder so PZ regenerates on next load
5. Prints: `VERDICT: MAP22B_WORLDGEN_PREFAB_DUMP_INSTALLED_RESTART_REQUIRED`

### Harvest results after PZ run

After launching PZ, hosting the proof server, and exiting:

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\harvest-worldgen-prefab-dump-logs.ps1
```

Output: `.local/deadmtl-authoring/worldgen-prefab-dump/prefab-dump-harvest.txt`

## Expected markers in PZ log

After the probe loads in PZ, the following markers should appear in console.txt or
the PZ game log:

```
PZMAPFORGE_PREFAB_DUMP_LOADED
PZMAPFORGE_PREFAB_KEY=highway_NS_00
PZMAPFORGE_PREFAB_KEY=normal_road_WE_00
PZMAPFORGE_PREFAB_KEY=<... any other keys ...>
PZMAPFORGE_ROADLIKE_PREFAB_KEY=highway_NS_00
PZMAPFORGE_ROADLIKE_PREFAB_KEY=normal_road_WE_00
PZMAPFORGE_ROADLIKE_PREFAB_KEY=<... any road-like keys ...>
PZMAPFORGE_PREFAB_COUNT=<n>
PZMAPFORGE_ROADLIKE_PREFAB_COUNT=<n>
```

If `PZMAPFORGE_PREFAB_DUMP_LOADED` does not appear, the Lua did not execute. Check
the PZ log for `Error found in LUA file` or `LuaManager.RunLuaInternal` errors.

## Claim boundary

- No prefab key is runtime-proven by this discovery probe.
- Existence of a key in `worldgen.prefabs` does not guarantee correct visual behavior.
- Any discovered key requires a dedicated proof board (MAP-22C) and human visual
  confirmation before being added to WorldGenRegistry as VISUAL_CONFIRMED.
- If no alley or local road keys are found, alleys remain SYSTEM_2_REQUIRED.
- No public mod packaging is claimed.

---

## MAP-22B runtime result

Runtime harvest performed: 2026-06-14
Result sidecar: `docs/authoring/MAP22B_PREFAB_DUMP_RESULT.txt`

### Harvested markers

```
PZMAPFORGE_PREFAB_DUMP_LOADED
PZMAPFORGE_PREFAB_KEY=highway_NS_00
PZMAPFORGE_PREFAB_KEY=normal_road_WE_00
PZMAPFORGE_ROADLIKE_PREFAB_KEY=highway_NS_00
PZMAPFORGE_ROADLIKE_PREFAB_KEY=normal_road_WE_00
PZMAPFORGE_PREFAB_COUNT=2
PZMAPFORGE_ROADLIKE_PREFAB_COUNT=2
```

### Finding

PZ Build 42 `worldgen.prefabs` contains exactly two keys in this environment:
- `highway_NS_00`
- `normal_road_WE_00`

No additional road, alley, local street, turn, or intersection prefab keys were found.

### Consequence

The condition described above ("If no alley or local road keys are found") applies.

- MAP-22C proof board is not needed: no new keys were discovered to prove.
- WorldGenRegistry remains at two keys: `highway_NS_00` and `normal_road_WE_00`.
- Small roads, alleys, and ruelles remain SYSTEM_2_REQUIRED.
- MAP-22D (System 2 static tile overlay) is the next path for local streets and alleys.
- Do not claim WorldGen alley or local street support.

VERDICT: MAP22C_WORLDGEN_SMALL_ROAD_PATH_CLOSED_ONLY_TWO_PREFABS
