# Example 2B: Full 4-Layer Manifest

This is a documentation example showing the recommended 4-layer manifest
structure covering all nine semantic planning kinds.

Claim boundary: planning_artifact_only_not_pz_load_tested

---

## Manifest

layer-manifest.json defines four layers at 300x300 with the standard
precedence and recommended allowed_kinds per layer:

| Layer | Image | Allowed kinds |
|---|---|---|
| terrain | terrain.png | grass, industrial_yard |
| roads | roads.png | road, sidewalk |
| buildings | buildings.png | row_house, depanneur, garage, landmark |
| markers | markers.png | spawn, landmark |

Precedence: markers > buildings > roads > terrain

---

## Expected layer images

The four PNG images are not committed to this directory.
They should be 300x300 pixels painted with palette colours from
source/image-palette.json.

### terrain.png (300x300)

Background land-use layer.

Suggested content:
- Most cells: grass (R:100 G:140 B:70)
- Industrial zone in one quadrant: industrial_yard (R:160 G:130 B:90)
- All other cells: grass background

### roads.png (300x300)

Road network layer. Non-road cells should be the grass background colour.

Suggested content:
- Horizontal strip through the centre: road (R:70 G:70 B:70)
- Sidewalk border alongside road: sidewalk (R:190 G:180 B:160)
- All other cells: grass background

### buildings.png (300x300)

Building footprint layer. Non-building cells should be the grass background colour.

Suggested content:
- Row house cluster in top-left zone: row_house (R:160 G:110 B:80)
- Depanneur footprint near road: depanneur (R:200 G:130 B:60)
- Garage behind row houses: garage (R:80 G:80 B:100)
- A landmark marker: landmark (R:255 G:220 B:0)
- All other cells: grass background

### markers.png (300x300)

Point annotation layer. Only a few cells should be non-grass.

Suggested content:
- One or two spawn points: spawn (R:0 G:220 B:80)
- All other cells: grass background

---

## How to run layer-pipeline once images exist

Place terrain.png, roads.png, buildings.png, and markers.png in this directory
alongside layer-manifest.json. Then run:

    dotnet run --project src/PZMapForge.Cli -- layer-pipeline ^
      --layers tests/fixtures/layers/example-2b/layer-manifest.json ^
      --palette source/image-palette.json ^
      --output .local/mapforge

Output artifacts go to .local/mapforge/ (gitignored).
Inspect layer-merge-report.md for conflict counts and per-layer contribution
stats, then inspect plan-report.md for planning recommendations.

---

## Why PNG images are not committed in this slice

Binary PNG files are not committed here unless generated deterministically by
test code or a committed fixture-generator script.

The reason: hand-drawn PNG files are opaque binary blobs that cannot be
inspected in a diff and are fragile to tool-specific rendering differences.

To generate test images programmatically, see:
- tests/PZMapForge.Core.Tests/Layers/LayerMergerTests.cs (MakeSolid, MakeZone helpers)
- tests/PZMapForge.Cli.Tests/LayerPipelineProcessTests.cs (MakeGrassImage helper)

A future slice may add a fixture-generator script that deterministically
produces the expected images for this example. That script would be committed
instead of the binary output.
