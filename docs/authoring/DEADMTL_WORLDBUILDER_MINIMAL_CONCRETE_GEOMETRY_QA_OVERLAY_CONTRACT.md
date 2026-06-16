# DeadMTL WorldBuilder Minimal Concrete Geometry QA Overlay

CONTRACT ONLY — QA visualization. No lotpack writes. No WorldGenOverride.lua.
No compile-worldgen. No runtime claim. No materialization claim.

## Purpose

MAP-26B reads the source `map_00.png` and the MAP-26A minimal concrete geometry MVP
JSON and renders a debug overlay PNG that proves the MAP-26A integer rectangle
geometry sits where expected on the source map, alongside JSON/MD/CSV/summary
records of every drawn feature.

This is a QA/debug step. It does not create new geometry. It only visualizes
the geometry MAP-26A already produced.

## Input files

- `E:\Omni\Zomboid\assets\raw\map_00.png` — raw 256x256 source authoring map
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-mvp\map_00\map_00.minimal_concrete_geometry_mvp.json`
  — MAP-26A component bbox, lot rectangles, and accepted building slot rectangles

## What this task produces

- An enlarged (4x scale, 1024x1024 base + legend panel) overlay PNG with:
  - the component bbox drawn as a thick outline
  - all lot rectangles drawn as medium outlines
  - all accepted building slot rectangles drawn as thin outlines
  - per-feature labels
  - a legend panel with map id, component id, counts, scale, and verdict
- A JSON record of every overlay feature (source rect, scaled overlay rect, draw order)
- A Markdown report with a feature table
- A CSV with one row per overlay feature (1 component bbox + 7 lots + 7 slots = 15 rows)
- A summary text file

## What this task does NOT produce

- No PZ tile data
- No lotpack files
- No WorldGenOverride.lua
- No call to compile-worldgen
- No installation into Project Zomboid
- Not writer-ready (`writer_ready: false`)
- Not runtime-validated (`runtime_valid: false`)
- Not materialized (`materialized: false`)

## Why this is QA-only

The overlay draws exactly the rectangles already present in the MAP-26A output.
It does not derive new geometry, does not write any PZ-consumable format, and
does not touch the source PNG. It exists purely so a human (or future automated
check) can visually confirm the MAP-26A rectangles land in the correct place on
the source map.

## What this proves

- The MAP-26A component bbox, lot rectangles, and accepted building slot
  rectangles are internally consistent with the source PNG dimensions.
- All drawn rectangles fall within the 256x256 source bounds.
- The geometry-to-pixel mapping used by MAP-26A is visually inspectable.

## What this does NOT prove

- It does not prove the geometry is writer-ready.
- It does not prove the geometry is valid inside the PZ runtime.
- It does not prove any part of this pipeline is materialized into a playable map.

## Claim boundary

| Field          | Value |
|----------------|-------|
| writer_ready   | false |
| runtime_valid  | false |
| materialized   | false |

## Running the helper script

```powershell
powershell -ExecutionPolicy Bypass -File examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-qa-overlay.ps1
```

Output (under `.local/`):

- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.png`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.json`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.md`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.csv`
- `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-overlay\map_00\map_00.minimal_concrete_geometry_qa_overlay.summary.txt`

## Verdict

`MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE`
