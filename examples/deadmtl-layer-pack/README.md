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

## Claim boundary

This skeleton does not constitute a playable Project Zomboid map.
No PZ load test has been performed.
No public mod packaging is claimed.
Lua output is a planning artifact only until a real local load test is documented.
