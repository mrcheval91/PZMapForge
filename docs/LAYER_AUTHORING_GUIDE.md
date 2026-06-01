# Layer Authoring Guide

## Claim boundary

planning_artifact_only_not_pz_load_tested

All artifacts produced by layer-pipeline are local planning artifacts only.
They are not playable Project Zomboid map exports.
See docs/CLAIM_BOUNDARY.md for the full boundary statement.

---

## What layer-pipeline does

layer-pipeline accepts a layer manifest and a set of per-layer PNG/BMP images.
It merges the layers into one 300x300 semantic grid using a fixed precedence rule,
then runs the full downstream pipeline:

    manifest validation
    -> per-layer image parsing (palette-matched)
    -> allowed-kind validation per layer
    -> deterministic pixel-level merge by precedence
    -> parsed-cell.json  (merged semantic grid)
    -> layer-merge-report.md  (conflict summary, per-layer stats)
    -> regions.json + regions-report.md
    -> primitives.json + primitives-report.md
    -> plan-recommendations.json + plan-report.md

CLI command:

    dotnet run --project src/PZMapForge.Cli -- layer-pipeline \
      --layers <manifest.json> \
      --palette source/image-palette.json \
      --output .local/mapforge \
      [--resize] \
      [--tiny-threshold <int>] \
      [--large-threshold <int>]

Output is always written to a .local/ directory.
Non-.local output paths are refused.

---

## Required manifest shape

    {
      "schema": "pzmapforge.layer-manifest.v0.1",
      "claim_boundary": "planning_artifact_only_not_pz_load_tested",
      "width": 300,
      "height": 300,
      "layers": [
        {
          "name": "<layer-name>",
          "path": "<relative-path-to-image>",
          "allowed_kinds": ["<kind1>", "<kind2>"]
        }
      ],
      "precedence": ["<highest-priority-layer>", ..., "<lowest-priority-layer>"]
    }

Rules:
- schema must be pzmapforge.layer-manifest.v0.1
- claim_boundary must be planning_artifact_only_not_pz_load_tested
- width and height must be 300
- layers must be non-empty; each layer must have a non-empty name, path, and allowed_kinds
- layer names must be unique
- precedence must list every layer name exactly once, no duplicates, no unknown names
- all allowed_kinds must be known semantic kinds (see palette)
- image paths are relative to the manifest file location

---

## Standard layer names and recommended allowed kinds

The following four layers cover all nine semantic kinds.

| Layer | File | Recommended allowed_kinds |
|---|---|---|
| terrain | terrain.png | grass, industrial_yard |
| roads | roads.png | road, sidewalk |
| buildings | buildings.png | row_house, depanneur, garage, landmark |
| markers | markers.png | spawn, landmark |

Notes:
- landmark may appear in both buildings and markers. Use the layer that
  better represents the intent. If both mark the same cell, markers wins.
- grass is the default/background kind and is allowed everywhere.
  It does not need to be in allowed_kinds but may be included.
- If a layer image has a pixel whose kind is not in allowed_kinds
  and is not the default kind (grass), the merge fails with an error.

---

## Precedence policy

Default precedence (highest to lowest):

    markers > buildings > roads > terrain

For each cell in the 300x300 grid:
- If only one layer contributes a non-default kind, that kind wins.
- If two or more layers contribute non-default kinds to the same cell,
  the layer with the highest precedence wins.
- If no layer contributes a non-default kind, the cell defaults to grass.

The precedence list is declared in the manifest and applied exactly.
You can define any precedence order you need.

---

## Default kind

The default kind is grass.

A cell covered only by grass across all layers remains grass.
A cell with at least one non-grass contribution uses the winning layer's kind.

The DefaultKind can be overridden via LayerMergeOptions in code, but the CLI
always uses grass as the default.

---

## Conflict policy

A conflict occurs when two or more non-default layers contribute non-default
kinds to the same cell.

Behavior:
- The winning kind is determined by precedence (the highest-priority layer wins).
- All conflicts are counted and reported in layer-merge-report.md.
- The first 100 conflicts (x, y, chosen layer, losing layers) are sampled.
- Conflicts are not silently ignored. They are evidence, not errors.

High conflict counts indicate overlapping authoring zones. Consider:
- Separating layer coverage into non-overlapping zones where possible.
- Using markers only for single-cell annotation points.
- Reviewing layer-merge-report.md before inspecting plan-report.md.

---

## Authoring workflow

Step 1: Create the terrain base image.
  Paint the 300x300 background with your land use.
  Use grass for open ground and industrial_yard for yards.
  This becomes the fallback layer for all cells.

Step 2: Add roads.
  Paint road and sidewalk areas on a separate image (roads.png).
  Leave non-road areas as the grass background color.

Step 3: Add buildings.
  Paint building footprints on a separate image (buildings.png).
  Use row_house, depanneur, garage, or landmark for each footprint type.
  Leave non-building areas as the grass background color.

Step 4: Add markers.
  Paint spawn points and landmark markers on a separate image (markers.png).
  These typically cover only a few cells.
  Leave unmarked areas as the grass background color.

Step 5: Write or update the manifest.
  List all four layers with their allowed_kinds.
  Set precedence: markers first, then buildings, then roads, then terrain.

Step 6: Run layer-pipeline.

    dotnet run --project src/PZMapForge.Cli -- layer-pipeline \
      --layers layer-manifest.json \
      --palette source/image-palette.json \
      --output .local/mapforge

Step 7: Inspect layer-merge-report.md.
  Check conflict count and per-layer contribution table.
  High conflict counts may indicate unintended overlap.

Step 8: Inspect plan-report.md.
  Review planning recommendations.
  Adjust layer images and re-run if needed.

---

## Naming conventions

| File | Purpose |
|---|---|
| layer-manifest.json | Manifest declaring layers, paths, kinds, precedence |
| terrain.png | Terrain/background layer (grass, industrial_yard) |
| roads.png | Road network layer (road, sidewalk) |
| buildings.png | Building footprints (row_house, depanneur, garage, landmark) |
| markers.png | Point markers (spawn, landmark) |

All paths in the manifest are relative to the manifest file location.
Output artifacts go under .local/mapforge/ and are gitignored.

---

## Error glossary

### Layer image not found

    error: Layer 'terrain' image not found: <path>

Cause: The resolved path for a layer image does not exist.
Fix: Verify the path in the manifest and that the file exists.

### Disallowed kind in layer

    error: Layer 'buildings': kind 'road' (code 'r') is not in allowed_kinds.

Cause: The layer image contains pixels mapped to a kind not in its allowed_kinds,
and that kind is not the default kind (grass).
Fix: Either update allowed_kinds in the manifest to include that kind,
or repaint the offending pixels in the layer image.

### Duplicate layer name

    error: Duplicate layer name: 'terrain'.

Cause: Two layers have the same name field in the manifest.
Fix: Give each layer a unique name.

### Unknown entry in precedence

    error: Precedence entry 'unknown_layer' does not match any layer name.

Cause: The precedence list contains a name not in the layers array.
Fix: Ensure every precedence entry matches a layer name exactly.

### Missing layer in precedence

    error: Layer 'markers' is missing from precedence.

Cause: A layer defined in layers[] is absent from the precedence list.
Fix: Add the missing layer name to precedence in the correct position.

### Non-300x300 image without --resize

    error: Layer 'terrain' parse error: Image is not 300x300 ...

Cause: A layer image is not 300x300 and --resize was not passed.
Fix: Pass --resize to the CLI command, or resize the image to 300x300 first.

### Output outside .local/ refused

    error: layer-pipeline: refusing to write outside a .local/ directory: <path>

Cause: The --output path does not contain a .local/ segment.
Fix: Pass an --output path under .local/, e.g. .local/mapforge.

---

## Non-claims

The following are not claimed and must not be implied without documented evidence:

- Playable Project Zomboid map export
- lotpack, lotheader, or bin file generation
- WorldEd replacement or compatibility
- Official Project Zomboid tool status
- Build 42 tested compatibility
- Steam Workshop readiness
- Copying or redistributing Project Zomboid game assets
- Writing to media/maps

The claim boundary does not advance without a real local load test
and a documented decision record.
