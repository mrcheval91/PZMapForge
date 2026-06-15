# DEADMTL_RAW_TILE_PALETTE_MAPPING_CONTRACT

## Purpose

MAP-23B produces a palette mapping contract for raw 256×256 authoring map tiles.

Given the inspection result from MAP-23A, it classifies each source color as either:
- Exactly matched to an existing palette entry (worldgen or System 2 static road)
- Unmapped and requiring a human decision

This is a **mapping contract only**. It does not compile the tile. It does not produce
a WorldGenOverride.lua. It is not runtime-proven. It is not writer-ready.

## Claim boundary

- `writes_lotpack: false`
- `writes_worldgen_lua: false`
- `runtime_proven: false` — Runtime proof is NOT claimed.
- `public_playable_claim: false`
- `writer_ready_claim: false` — Writer readiness is NOT claimed.

## Exact match behavior

A color is AUTO_MATCHED when it appears verbatim (case-insensitive hex) in:
- `worldgen-png-palette.json` — entries with `"color"` field
- `system2-static-road-intent-palette.json` — entries with `"hex"` field

Exact match → `confidence: EXACT_PALETTE_MATCH_ONLY`

No normalization. No fuzzy match. Exact only.

## Near-match suggestions

For unmapped colors, the builder computes RGB Euclidean distance to every known palette
color and includes suggestions where distance ≤ 32. Suggestions are informational only.
They do **not** resolve the mapping. A human decision is required.

`#00AA10` will suggest `#00AA00 grass_plain` (worldgen, d=16). It remains UNMAPPED.

## Command

```
pzmapforge deadmtl-build-raw-map-tile-palette-mapping \
  --inspection <inspection.json> \
  --worldgen-palette <worldgen-png-palette.json> \
  --system2-palette <system2-static-road-intent-palette.json> \
  --output-json <mapping.json> \
  --output-md <mapping.md> \
  --output-csv <mapping.csv> \
  --summary <summary.txt>
```

All output paths must be under `.local/`.

## Output files

- `map_00.raw_tile_palette_mapping.json` — machine-readable mapping document
- `map_00.raw_tile_palette_mapping.md` — human-readable report
- `map_00.raw_tile_palette_mapping.csv` — spreadsheet-friendly color table
- `map_00.raw_tile_palette_mapping.summary.txt` — console summary

## JSON format

```
pzmapforge.deadmtl.raw-map-tile-palette-mapping.v1
status: RAW_TILE_PALETTE_MAPPING_CONTRACT_ONLY
runtime_status: NOT_RUNTIME_PROVEN
writer_status: NOT_IMPLEMENTED
```

## Helper script

```
examples/deadmtl-layer-pack/scripts/run-deadmtl-raw-map-tile-palette-mapping.ps1
```

Requires the MAP-23A inspection JSON to be present at:
```
.local/deadmtl-authoring/raw-map-tile-inspection/map_00/map_00.raw_tile_inspection.json
```

## VERDICT

MAP23B_RAW_TILE_PALETTE_MAPPING_CONTRACT_COMPLETE
