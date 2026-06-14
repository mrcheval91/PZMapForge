# DeadMTL Readable Terrain Proof Board

## Why this exists (MAP-21B)

MAP-21A proved all 12 WorldGen palette entries using a small swatch grid (12x12 tiles per
biome). The MAP-21A board had two problems:

1. **Tiny swatches were hard to read in-game.** At 12x12 tiles, each biome patch was
   difficult to identify visually. Patches near the edge of the swatch grid were easy to
   confuse with each other.

2. **Isolated water swatches produced a display anomaly.** A small isolated water rectangle
   rendered black and unwalkable in MAP-21A. The anomaly affected both water and sand_bank
   when painted as isolated tiny swatches in a non-water context.

MAP-21B fixes both problems:

- **Large patches (25x25 tiles minimum)** for all biome types.
- **Shaped shoreline** for water and sand_bank: a large continuous water body on the west
  side of the canvas with a sand_bank shore strip alongside it. No isolated water swatch.

This is still a proof harness, not a playable map.

---

## Proof chain

This board sits inside the same proof chain as MAP-21A:

```
hex color
  -> palette JSON       (color must be listed with a valid type and key)
  -> WorldGenRegistry   (key must be in the registered biome or prefab set)
  -> compiler           (compile-worldgen-project must accept without errors)
  -> Lua output         (WorldGenOverride.lua must contain the module)
  -> PZ load            (Lua must load without error on PZ startup)
  -> human visual       (human must see the correct terrain patch in-game)
```

Compiler proof and human visual proof remain separate. A passing test suite does not
constitute visual confirmation.

---

## Isolated water anomaly

Do not use tiny isolated water or sand_bank swatches as proof. A small isolated water
rectangle rendered black and unwalkable in MAP-21A. This was an anomaly caused by placing
a small water rectangle in a non-water context rather than using water as a canvas base or
a large continuous body.

The shaped shoreline approach used in MAP-21B avoids this anomaly:

- water fills the full-height west column of the canvas (45 tiles wide, 170 tiles tall)
- sand_bank fills a 10-tile-wide shore strip immediately east of the water

This gives both colors enough area that the engine can render them correctly.

---

## Terrain board layout

Canvas: 220x170 tiles. Origin: (10580, 8200). Spawn: world (10650, 8250) = pixel (70, 50).

### terrain_biomes.png (priority 10)

| Area | Hex | Key | Pixel rect |
|---|---|---|---|
| Water body | `#0000FF` | water | (0, 0, 45, 170) |
| Shore strip | `#D8C080` | sand_bank | (45, 0, 10, 170) |
| Row 1, col A | `#207020` | birch_forest | (58, 4, 25, 25) |
| Row 1, col B | `#145C14` | oak_forest | (84, 4, 25, 25) |
| Row 2, col A | `#0B4418` | pine_forest | (58, 60, 25, 25) |
| Row 2, col B | `#60A060` | light_birch_forest | (84, 60, 25, 25) |
| Row 3, col A | `#4F8F4F` | light_oak_forest | (58, 86, 25, 25) |
| Row 3, col B | `#3F7F50` | light_pine_forest | (84, 86, 25, 25) |
| Row 4, col A | `#00AA00` | grass_plain | (58, 112, 25, 25) |
| Row 4, col B | `#55CC55` | flower_plain | (84, 112, 25, 25) |

The biome rows leave a gap at y=30..59 so the WE road crossing at y=48..55 is visible
without biome patches underneath.

### terrain_roads.png (priority 50)

| Area | Hex | Key | Pixel rect |
|---|---|---|---|
| WE road | `#FF6600` | normal_road_WE_00 | (57, 48, 75, 8) |
| NS highway | `#CC3300` | highway_NS_00 | (125, 2, 8, 162) |

The WE road crosses spawn y=50 (world y=8250), making the road visible immediately
on first spawn without movement. The NS highway is at pixel x=125..132 (world x=10705..10712),
55 tiles east of spawn.

---

## Commands

### Generate proof pack (no install)

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-readable-terrain-proof-build.ps1
```

Steps:
1. Generates the readable terrain proof pack to `.local/deadmtl-authoring/readable-terrain-proof-pack/`
2. Generates palette chart sidecars
3. Runs `compile-worldgen-project` to extract WorldGen modules
4. Runs `compile-worldgen` to emit `WorldGenOverride.lua`
5. Prints Lua preview and expected runtime markers

Does NOT write to any PZ game folder.

### Generate proof pack and install

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-readable-terrain-proof-build.ps1 -Install
```

Same as above, then:
- Backs up existing `WorldGenOverride.lua`
- Installs generated `WorldGenOverride.lua` to the PZ game map folder
- Clears save folder so PZ regenerates the world
- Byte-checks installed file (BOM=false, non-ASCII=false)
- Prints VERDICT

### Generate proof pack only

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\generate-readable-terrain-proof-pack.ps1
```

### Expected runtime markers after install

```
PZMAPFORGE_WORLDGENOVERRIDE_MAP_ID=deadmtl_readable_terrain_proof_v1
PZMAPFORGE_WORLDGENOVERRIDE_LOADED
PZMAPFORGE_WORLDGENOVERRIDE_MODULE_COUNT=<actual count>
```

After PZ loads, spawn at world tile (10650, 8250) and confirm:

- Water body visible west of spawn (world x=10580..10624)
- Sand bank shore strip visible (world x=10625..10634)
- Road crossing visible at world y=8248..8255
- Highway visible at world x=10705..10712
- Forest patches visible above and below road (biome rows)

---

## What is NOT claimed

- No public mod packaging is claimed.
- No playable map is claimed.
- No Project Zomboid load test result is implied by automated test output alone.
- A compiler-accepted key does not guarantee in-game visual appearance.
- Human visual confirmation is required for proof status updates.
