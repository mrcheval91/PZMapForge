# MAP-23A: DeadMTL Raw 256x256 Map Tile Inspection

> **WARNING:** This is inspection only.
> No compilation, no runtime proof, no writer readiness, no playable export.

## Purpose

MAP-23A defines a safe inspection and intake layer for raw 256x256 authoring map tiles
before any compilation or runtime use is attempted.

The inspector reads a PNG file, computes pixel statistics, compares colors against known
palettes, and produces local planning artifacts only.

## What this does

- Loads the PNG via System.Drawing.Bitmap (Windows only)
- Computes SHA256 of the raw file bytes
- Counts opaque and transparent pixels (alpha=0 → transparent; nonzero alpha → opaque)
- Groups opaque pixels by RGB color
- Validates exact size (default 256x256)
- Produces top N colors by count (default 32)
- Compares opaque RGB colors against worldgen palette (field: `"color"`) and
  System 2 static road intent palette (field: `"hex"`)
- Reports unknown colors (not in either palette, capped at 50)

## What this does NOT do

- Does NOT compile
- Does NOT write lotpack
- Does NOT write WorldGenOverride.lua
- Does NOT install into Project Zomboid
- Runtime proof is NOT claimed
- Writer readiness is NOT claimed
- Playable export is NOT claimed

## Claim boundary

```
writes_lotpack:         false
writes_worldgen_lua:    false
runtime_proven:         false
public_playable_claim:  false
writer_ready_claim:     false
```

## CLI usage

```
deadmtl-inspect-raw-map-tile
  --input <png>
  --output-json <out.json>
  --output-md   <out.md>
  --output-csv  <out.csv>
  --summary     <summary.txt>
  [--worldgen-palette  <path>]
  [--system2-palette   <path>]
  [--expected-width  <int>]    default: 256
  [--expected-height <int>]    default: 256
  [--top-colors <int>]         default: 32
```

All output paths must be under `.local/`.

## Helper script

```
examples/deadmtl-layer-pack/scripts/run-deadmtl-raw-map-tile-inspection.ps1
```

Default input: `E:\Omni\Zomboid\assets\raw\map_00.png`

Output dir: `.local\deadmtl-authoring\raw-map-tile-inspection\map_00\`

Output files:
- `map_00.raw_tile_inspection.json`
- `map_00.raw_tile_inspection.md`
- `map_00.raw_tile_colors.csv`
- `map_00.raw_tile_inspection.summary.txt`

## VERDICT

MAP23A_RAW_256_MAP_TILE_INSPECTION_COMPLETE
