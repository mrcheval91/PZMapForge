# MAP-26C WorldBuilder Minimal Concrete Geometry QA Review Packet

## Purpose

MAP-26C reads the MAP-26A geometry MVP JSON and the MAP-26B QA overlay outputs and produces a
structured review packet that lets a human verify the minimal concrete geometry chain is
internally consistent and within its stated claim boundary.

MAP-26C does not create geometry. It only reads and audits existing outputs.

## Input files

| File | Source |
|------|--------|
| `map_00.minimal_concrete_geometry_mvp.json` | MAP-26A output |
| `map_00.minimal_concrete_geometry_qa_overlay.json` | MAP-26B output |
| `map_00.minimal_concrete_geometry_qa_overlay.csv` | MAP-26B output |
| `map_00.minimal_concrete_geometry_qa_overlay.png` | MAP-26B output |

## Output files

| File | Description |
|------|-------------|
| `map_00.minimal_concrete_geometry_qa_review_packet.json` | Full structured review packet with all check results |
| `map_00.minimal_concrete_geometry_qa_review_packet.md` | Human-readable review report with check table and claim boundary |
| `map_00.minimal_concrete_geometry_qa_review_packet.csv` | One row per review check |
| `map_00.minimal_concrete_geometry_qa_review_packet.summary.txt` | Key/value summary for quick inspection |

No PNG is produced by MAP-26C. MAP-26C references the MAP-26B overlay PNG path.

## Review checks

MAP-26C performs 18 deterministic checks:

| # | Check ID | What it verifies |
|---|----------|-----------------|
| 1 | GEOMETRY_MVP_EXISTS | MAP-26A geometry MVP JSON file exists |
| 2 | QA_OVERLAY_JSON_EXISTS | MAP-26B overlay JSON file exists |
| 3 | QA_OVERLAY_CSV_EXISTS | MAP-26B overlay CSV file exists |
| 4 | QA_OVERLAY_PNG_EXISTS | MAP-26B overlay PNG file exists |
| 5 | MAP_ID_MATCHES | MAP-26A and MAP-26B map IDs match |
| 6 | COMPONENT_ID_MATCHES | MAP-26A and MAP-26B component IDs match |
| 7 | COMPONENT_BBOX_MATCHES | MAP-26A and MAP-26B component bboxes match |
| 8 | LOT_COUNT_IS_7 | Lot count is 7 |
| 9 | ACCEPTED_BUILDING_SLOT_COUNT_IS_7 | Accepted building slot count is 7 |
| 10 | OVERLAY_FEATURE_COUNT_IS_15 | Overlay feature count is 15 |
| 11 | CSV_FEATURE_COUNT_IS_15 | CSV feature row count is 15 |
| 12 | CSV_FEATURE_BREAKDOWN_IS_1_7_7 | CSV breakdown is 1 COMPONENT_BBOX / 7 LOT_RECTANGLE / 7 BUILDING_SLOT_RECTANGLE |
| 13 | INCLUSIVE_RIGHT_BOTTOM_SEMANTICS | All features use right = x+width-1 and bottom = y+height-1 |
| 14 | RECTANGLES_INSIDE_256_BOUNDS | All rectangles are inside 256x256 source bounds |
| 15 | WRITER_READY_FALSE | writer_ready is false |
| 16 | RUNTIME_VALID_FALSE | runtime_valid is false |
| 17 | MATERIALIZED_FALSE | materialized is false |
| 18 | NO_FORBIDDEN_WRITER_RUNTIME_MATERIALIZATION_CLAIMS | Overlay verdict contains no forbidden writer/runtime/materialization claims |

Each check produces a PASS or FAIL status with expected/actual/details fields.

If any check fails, `is_valid` is false and verdict is `MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_INVALID`.

## Why this is QA-only

MAP-26C is a review/readiness packet. It reads and cross-checks existing artifacts.
It does not create, modify, or install anything. Its only outputs are text/JSON review files.

## What this proves

- MAP-26A geometry exists and is parseable.
- MAP-26B overlay exists and is parseable.
- Component bbox is consistent between MAP-26A and MAP-26B.
- Lot/slot counts are as expected (7/7).
- Feature counts are as expected (15).
- CSV breakdown is 1/7/7.
- All rectangles use inclusive right/bottom semantics.
- All rectangles are inside 256x256 source bounds.
- writer_ready, runtime_valid, and materialized remain false.
- No forbidden claims appear in the overlay verdict.

## What this does NOT prove

- Runtime validity in Project Zomboid.
- Writer readiness for PZ map compilation.
- Public playable packaging.
- Correct in-game tile placement.
- Any claim about TileZed or WorldEd compatibility.

## Claim boundary

```
writer_ready   : false
runtime_valid  : false
materialized   : false
```

This review packet does not create geometry, write PZ runtime files, write lotpack,
write WorldGenOverride.lua, call compile-worldgen, or install anything into Project Zomboid.

## Running the helper script

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-qa-review-packet.ps1
```

The script resolves the repo root, uses the MAP-26A and MAP-26B `.local` outputs as inputs,
and writes the review packet to:

```
.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-qa-review-packet\map_00\
```

## Expected verdict

```
MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE
```
