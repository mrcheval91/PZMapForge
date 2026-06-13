# DeadMTL WorldGen Palette Proof

This document explains how WorldGen palette colors are proved and what each
proof status means. Read this before marking any color VISUAL_CONFIRMED.

---

## Proof chain

A hex color in a palette JSON is not proved just because it exists in the file.
The full proof chain is:

```
hex color
  -> palette JSON       (color must be listed with a valid type and key)
  -> WorldGenRegistry   (key must be in the registered biome or prefab set)
  -> compiler           (compile-worldgen-project must accept without errors)
  -> Lua output         (WorldGenOverride.lua must contain the module)
  -> PZ load            (Lua must load without error on PZ startup)
  -> human visual       (human must see the correct terrain patch in-game)
```

Each step is necessary. No step can be assumed from the previous one.

---

## Proof statuses

| Status | Meaning |
|---|---|
| `VISUAL_CONFIRMED` | All steps complete. Human saw the patch in-game. Documented MAP. |
| `KNOWN_IN_CODE` | Compiler accepts. No human visual proof yet. |
| `UNPROVEN_VISUAL` | No compiler or visual proof. |

Statuses are recorded in `palettes/worldgen-png-palette.swatches.txt`.
Do not promote a color to `VISUAL_CONFIRMED` without a human screenshot or
explicit confirmation from a documented in-game test.

Compiler proof and human visual proof must remain separate records.
A passing test suite does not constitute visual confirmation.

---

## Current palette colors

| Hex | Type | Key | Status |
|---|---|---|---|
| `#0000FF` | biome | water | VISUAL_CONFIRMED (MAP-20A) |
| `#D8C080` | biome | sand_bank | VISUAL_CONFIRMED (MAP-20A) |
| `#00AA00` | biome | grass_plain | KNOWN_IN_CODE |
| `#55CC55` | biome | flower_plain | KNOWN_IN_CODE |
| `#207020` | biome | birch_forest | VISUAL_CONFIRMED (MAP-20A) |
| `#145C14` | biome | oak_forest | KNOWN_IN_CODE |
| `#0B4418` | biome | pine_forest | KNOWN_IN_CODE |
| `#60A060` | biome | light_birch_forest | KNOWN_IN_CODE |
| `#4F8F4F` | biome | light_oak_forest | KNOWN_IN_CODE |
| `#3F7F50` | biome | light_pine_forest | KNOWN_IN_CODE |
| `#FF6600` | prefab | normal_road_WE_00 | VISUAL_CONFIRMED (MAP-20A) |
| `#CC3300` | prefab | highway_NS_00 | VISUAL_CONFIRMED (MAP-13C/15C/16B) |

All keys are registered in `WorldGenRegistry.cs`.

---

## Layer PNG authoring rules

- **One pixel = one PZ world tile.** The canvas is in tile coordinates.
- **Transparent pixels (alpha < 128) are skipped.** They produce no worldgen output
  and never override lower-priority layers.
- **Unknown colors cause a compilation error.** Every opaque pixel must map to
  a palette entry.
- **Priority composition:** lower priority number is painted first. Higher priority
  overrides lower where both layers have opaque pixels at the same coordinate.
- **Palette files are compiler input.** Chart files (`*.chart.png`, `*.swatches.txt`,
  `*.layer-guide.txt`) are documentation only — they are never read by the compiler.

---

## Palette proof workflow

### 1. Generate and inspect charts

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\generate-palette-color-charts.ps1
```

Writes:
- `examples/deadmtl-layer-pack/palettes/worldgen-png-palette.chart.png`
- `examples/deadmtl-layer-pack/palettes/worldgen-png-palette.swatches.txt`
- `examples/deadmtl-layer-pack/palettes/worldgen-png-palette.layer-guide.txt`

### 2. Run palette proof build (no install)

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-worldgen-palette-proof-build.ps1
```

Generates the palette proof pack, compiles the project and Lua, and prints expected
runtime markers. Does NOT write to any PZ game folder.

### 3. Run palette proof build with install

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-worldgen-palette-proof-build.ps1 -Install
```

Same as step 2, then installs the Lua to the PZ game map folder and clears the
save folder. Prints:

```
VERDICT: MAP21A_WORLDGEN_PALETTE_PROOF_INSTALLED_RESTART_REQUIRED
```

### 4. Human visual confirmation

After PZ loads, spawn at world tile (10650, 8250). Confirm:
- Biome swatches visible at pixel rows y=5..30 (world y=8205..8230)
- Road (WE) crossing at pixel y=48..55 (world y=8248..8255)
- Highway (NS) at pixel x=130..137 (world x=10710..10717)

For each color confirmed in-game, update the status in:
`palettes/worldgen-png-palette.swatches.txt`

Do not update `swatches.txt` automatically. Update only after explicit human
confirmation. Record the MAP reference (e.g., MAP-21A) alongside the date.

---

## What is NOT claimed

- No public mod packaging is claimed.
- No playable map is claimed.
- No Project Zomboid load test result is implied by automated test output alone.
- A compiler-accepted key does not guarantee in-game visual appearance.
- Human visual confirmation is required for VISUAL_CONFIRMED status.
