# DeadMTL System 2 Static Road Intent Extract (MAP-22E)

## Purpose

This document describes the System 2 static road intent extractor introduced in MAP-22E.

The extractor reads the System 2 PNG layers defined in `system2-static-road-overlay-contract.json`,
decodes each opaque pixel against the intent palette in `system2-static-road-intent-palette.json`,
and emits structured JSON describing the road geometry at Montreal ruelle authoring scale (1 px = 1 m).

## Why this exists

MAP-22B runtime harvest found exactly two prefab keys in `worldgen.prefabs`:
- `highway_NS_00`
- `normal_road_WE_00`

MAP-22C confirmed there is no WorldGen path for local streets, alleys (ruelles), service lanes,
parking access roads, pedestrian cuts, intersections, turns, or dead ends.

MAP-22D established the System 2 static overlay contract and intent palette.
MAP-22E implements the extractor that reads those authored layers and produces an inspectable JSON
record of what was authored. This JSON is the evidence layer for downstream static overlay logic
(not yet implemented).

## What the extractor does

For each layer in the contract:

1. Opens the PNG from `<pack-root>/<layer.file>`.
2. Reads every pixel. If `alpha >= 128`, the pixel is opaque.
3. Looks up the pixel RGB in the intent palette.
4. If the layer id is `static_road_nodes`: emits each opaque pixel as an individual node record
   (pixel_x, pixel_y, world_x, world_y, intent, color).
5. Otherwise: run-length encodes horizontal runs of the same intent
   (y, x_start, x_end, world_y, world_x_start, world_x_end, intent, color).
6. Unknown opaque colors cause extraction to fail (by default).

## Output JSON shape

```json
{
  "format": "pzmapforge.deadmtl.system2.static-road-extract.v1",
  "status": "EXTRACT_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_contract": "<path>",
  "origin_x": 10580,
  "origin_y": 8200,
  "width": 220,
  "height": 170,
  "scale": { "pixels_per_meter": 1, "meters_per_pixel": 1, "pz_tiles_per_pixel": 1 },
  "layers": [
    {
      "id": "static_roads_local",
      "file": "layers/static_roads_local.png",
      "class": "local_street",
      "non_empty_pixels": 0,
      "intents": [],
      "runs": [],
      "nodes": []
    }
  ],
  "totals": {
    "layer_count": 6,
    "non_empty_pixels": 0,
    "unknown_opaque_pixels": 0
  },
  "claim_boundary": {
    "writes_lotpack": false,
    "writes_worldgen_lua": false,
    "runtime_proven": false,
    "public_playable_claim": false
  }
}
```

## World coordinates

The contract specifies the DeadMTL map origin: `origin_x = 10580`, `origin_y = 8200`.

World coordinates are computed as:
- `world_x = origin_x + pixel_x`
- `world_y = origin_y + pixel_y`

## CLI command

```
pzmapforge system2-extract-static-roads \
  --input   system2-static-road-overlay-contract.json \
  --palette palettes/system2-static-road-intent-palette.json \
  --root    . \
  --output  .local/deadmtl-authoring/system2-road-extract/system2-road-extract.json \
  --summary .local/deadmtl-authoring/system2-road-extract/system2-road-extract-summary.txt \
  [--origin-x 10580] \
  [--origin-y 8200]
```

Output paths must contain `.local/`.

## Helper script

```powershell
.\scripts\run-system2-static-road-extract.ps1
```

Accepts `-OutputDir`, `-OriginX`, `-OriginY`, `-Contract`, `-Palette`, `-PackRoot` overrides.

## Claim boundary

- Does NOT write `.lotpack`.
- Does NOT write `WorldGenOverride.lua`.
- Does NOT install into a live PZ server.
- Runtime proof is NOT claimed.
- Public playable claim is NOT made.

VERDICT: MAP22E_SYSTEM2_STATIC_ROAD_INTENT_EXTRACT_COMPLETE
