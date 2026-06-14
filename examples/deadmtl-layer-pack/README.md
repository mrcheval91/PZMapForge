# DeadMTL Layer Pack

Authoring skeleton for the DeadMTL/Montreal map.

This is a planning artifact. Not a playable Project Zomboid mod package.
No PZ load test has been performed. No Steam Workshop readiness is claimed.

---

## What this is

A structured layer-painting workspace for the DeadMTL worldgen map.
Each layer is a PNG painted with a palette-mapped color per tile.
The project manifest composes layers in priority order and emits a
`WorldGenOverride.lua` that controls PZ Build 42 world generation.

---

## System boundaries

### System 1 — WorldGen (SUPPORTED by compile-worldgen-project)

Controls biome and major prefab placement at the world level.
These layers are included in `deadmtl_worldgen_project.json`.

| Layer        | Palette key       | Notes                              |
|--------------|-------------------|------------------------------------|
| water        | biome: water      | Open water bodies                  |
| shore        | biome: sand_bank  | Shoreline / riverbanks             |
| parks_forest | biome: birch_forest | Parks, forests, green space      |
| roads_major  | prefab: normal_road_WE_00 | Major arterials and highways |

### System 2 — Binary Overlay (FUTURE — not yet implemented)

Fine-grained building placement, local roads, and props.
These layers exist as transparent placeholders.

| Layer           | Notes                              |
|-----------------|------------------------------------|
| roads_local     | Local streets and alleys           |
| placed_buildings| Individual building footprints     |
| props           | Vehicles, containers, set-dressing |

### System 3 — Zoning Generator (FUTURE — not yet implemented)

Procedural fill for residential, commercial, and industrial zones.
Palette: `palettes/zoning-palette.json` (format not ratified).

| Layer              | Notes                              |
|--------------------|------------------------------------|
| zones_residential  | Low/medium/high density housing    |
| zones_commercial   | Strip malls, dense commercial      |
| zones_industrial   | Light/heavy industry               |

### System 4 — Runtime Society Layer (FUTURE — not yet implemented)

NPC zones, faction territories, ownership, and rent districts.
Palette: `palettes/metadata-palette.json` (format not ratified).

| Layer      | Notes                              |
|------------|------------------------------------|
| npc_zones  | NPC spawn and patrol zones         |
| ownership  | Property ownership boundaries      |

---

## Project coordinates

- origin_x: 10580 (world tile X)
- origin_y: 8200  (world tile Y)
- width: 190 tiles
- height: 140 tiles

---

## Usage

### 1. Generate empty layers (one-time setup)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-empty-layer-pack.ps1
```

This creates transparent PNG stubs for all layers plus sample paint on
the four supported System 1 layers.

### 2. Compile the worldgen project

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  compile-worldgen-project `
  --input examples\deadmtl-layer-pack\deadmtl_worldgen_project.json `
  --output .local\worldgen\deadmtl\worldgen_layers.json
```

### 3. Compile the Lua override

```powershell
dotnet run --project src\PZMapForge.Cli --configuration Release -- `
  compile-worldgen `
  --input .local\worldgen\deadmtl\worldgen_layers.json `
  --output .local\worldgen\deadmtl\WorldGenOverride.lua
```

### 4. Validate the layer pack

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-layer-pack.ps1
```

---

## Palette charts and color proof

Generate visual chart sidecars for all palette colors, with proof status
(VISUAL_CONFIRMED / KNOWN_IN_CODE):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-palette-color-charts.ps1
```

Writes to `palettes/`:
- `worldgen-png-palette.chart.png` — visual swatches with hex / type / key / proof status
- `worldgen-png-palette.swatches.txt` — text table with proof status column
- `worldgen-png-palette.layer-guide.txt` — authoring rules and pixel conventions

Run the WorldGen palette proof build (no install):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-palette-proof-build.ps1
```

With install (-Install installs Lua and clears save folder):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-worldgen-palette-proof-build.ps1 -Install
```

See `docs/authoring/DEADMTL_WORLDGEN_PALETTE_PROOF.md` for the full proof chain
and rules for updating proof statuses.

---

## Spawn-centered runtime proof

A pre-built workflow that places all painted features within 50 tiles of the
spawn tile (10650, 8250) so they are visible immediately on first spawn
without walking or searching.

### Features painted (world tile coordinates)

| Layer        | Key                 | World X          | World Y          |
|--------------|---------------------|------------------|------------------|
| roads_major  | normal_road_WE_00   | 10618..10682     | 8248..8252       |
| shore        | sand_bank           | 10632..10668     | 8238..8265       |
| parks_forest | birch_forest        | 10608..10630     | 8238..8272       |
| water        | (base fill)         | entire canvas    | entire canvas    |

Road strip y=8248..8252 passes immediately under the player at spawn tile
10650, 8250, making the WorldGen road visible without any movement.

### Run proof build (no install)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-spawn-centered-proof-build.ps1
```

Generates the pack to `.local/deadmtl-authoring/spawn-centered-proof-pack/`,
runs `deadmtl-authoring-build`, and prints the Lua preview and expected
runtime markers. Does NOT write to any PZ game folder.

### Run proof build with install

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-spawn-centered-proof-build.ps1 -Install
```

Same as above, then:
- Backs up existing `WorldGenOverride.lua`
- Installs new `WorldGenOverride.lua` to the PZ game map folder
- Clears the save folder so PZ regenerates the world
- Byte-checks the installed file (BOM=false, non-ASCII=false)
- Prints VERDICT

### Expected runtime markers

```
PZMAPFORGE_WORLDGENOVERRIDE_MAP_ID=deadmtl_spawn_centered_proof_v1
PZMAPFORGE_WORLDGENOVERRIDE_LOADED
PZMAPFORGE_WORLDGENOVERRIDE_MODULE_COUNT=<N>
```

### Claim boundary

The spawn-centered proof pack is an authoring artifact only.
No public mod packaging is claimed.
No Project Zomboid load test has been performed by any automated tool.
Output must not be treated as a playable map without explicit runtime
verification documented as a human proof step.

---

## Readable terrain proof board

MAP-21B improves on the MAP-21A tiny swatch grid with large terrain patches (25x25+)
and a shaped shoreline for water/sand proof. Avoids isolated water swatches, which
produced a black/unwalkable display anomaly in MAP-21A.

Run readable terrain proof build (no install):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-readable-terrain-proof-build.ps1
```

With install (-Install installs Lua and clears save folder):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-readable-terrain-proof-build.ps1 -Install
```

See `docs/authoring/DEADMTL_READABLE_TERRAIN_PROOF.md` for the full layout, the
isolated-water anomaly note, and commands.

---

## Claim boundary

This skeleton does not constitute a playable Project Zomboid map.
No PZ load test has been performed.
No public mod packaging is claimed.
Lua output is a planning artifact only until a real local load test is documented.
