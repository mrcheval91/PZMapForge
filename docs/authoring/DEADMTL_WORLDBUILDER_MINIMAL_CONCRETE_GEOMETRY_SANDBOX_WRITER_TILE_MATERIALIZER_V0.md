# MAP-27C: WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materializer V0

## Purpose

Consumes MAP-27B1 tile buffer outputs and materializes the internal 256x256 sandbox buffer
into deterministic sandbox tile/material records.
No PZ runtime files are written. No runtime validity is claimed.

## Claim boundary

```
sandbox_materialized: true   (sandbox material records were written)
pz_runtime_materialized: false
writer_ready: false
runtime_valid: false
materialized: false          (global — no PZ runtime materialization)
runtime_proof_claimed: false
public_playable_packaging_claimed: false
```

## Inputs

| Argument | Description |
|---|---|
| `--tile-buffer-result` | MAP-27B1 main result JSON |
| `--tile-buffer-cells` | MAP-27B1 touched cells CSV |
| `--tile-buffer-ownership` | MAP-27B1 ownership JSON |
| `--tile-buffer-replay-log` | MAP-27B1 replay log JSON |
| `--tile-buffer-collision-report` | MAP-27B1 collision report JSON |
| `--tile-buffer-forbidden-output-guard` | MAP-27B1 forbidden output guard JSON |
| `--output-root` | Output directory (must contain `.local`) |
| `--output-json` | Main result JSON output path |
| `--output-md` | Markdown report output path |
| `--output-csv` | Check CSV output path |
| `--summary` | Plain-text summary output path |

## Default paths

- Input dir: `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\`
- Output dir: `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\`

## Outputs

### Main outputs (4)

- `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json` — full result with all checks
- `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md` — markdown report
- `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv` — check CSV
- `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt` — plain-text summary

### Extra outputs (6)

- `map_00.sandbox_writer_tile_materialized_cells.csv` — per-cell materialized records
- `map_00.sandbox_writer_tile_material_palette.json` — 5 material kinds with cell counts
- `map_00.sandbox_writer_tile_layer_stack.json` — 5 layers in canonical order
- `map_00.sandbox_writer_tile_materialization_replay_log.json` — cells per owner_kind × material_kind
- `map_00.sandbox_writer_tile_materialization_ownership_summary.json` — cells per owner_kind
- `map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json` — inherited guard

## Materialization rules

Every touched cell from the MAP-27B1 buffer is materialized exactly once.

Base mapping from `primary_owner_kind`:

| primary_owner_kind | material_kind | layer_kind |
|---|---|---|
| `BUILDING_FOOTPRINT` (exterior) | `BUILDING_EXTERIOR_WALL_CANDIDATE` | `WALL` |
| `BUILDING_FOOTPRINT` (interior) | `BUILDING_INTERIOR_FLOOR_CANDIDATE` | `FLOOR` |
| `ACCESS_LINK` | `ACCESS_EDGE_CANDIDATE` | `ACCESS` |
| `LOT_BOUNDARY` | `LOT_YARD_OR_SERVICE_SPACE_CANDIDATE` | `LOT` |
| `COMPONENT_ENVELOPE` | `COMPONENT_RESIDUAL_SPACE_CANDIDATE` | `COMPONENT` |

### Wall vs floor classification

For each `BUILDING_FOOTPRINT` cell, compute the bbox of all cells sharing the same `primary_owner_id` (slot id).
A cell is **exterior** (wall candidate) if `x == min_x` or `x == max_x` or `y == min_y` or `y == max_y`.
All other cells are **interior** (floor candidates).

## Material palette

5 records in deterministic order:

| palette_order | material_kind | layer_kind |
|---|---|---|
| 1 | `BUILDING_EXTERIOR_WALL_CANDIDATE` | `WALL` |
| 2 | `BUILDING_INTERIOR_FLOOR_CANDIDATE` | `FLOOR` |
| 3 | `ACCESS_EDGE_CANDIDATE` | `ACCESS` |
| 4 | `LOT_YARD_OR_SERVICE_SPACE_CANDIDATE` | `LOT` |
| 5 | `COMPONENT_RESIDUAL_SPACE_CANDIDATE` | `COMPONENT` |

## Layer stack

5 layers in canonical order (paint order: lowest first):

| layer_order | layer_kind |
|---|---|
| 1 | `COMPONENT` |
| 2 | `LOT` |
| 3 | `ACCESS` |
| 4 | `FLOOR` |
| 5 | `WALL` |

## Materialized cells CSV columns

```
x,y,primary_owner_kind,primary_owner_id,material_kind,layer_kind,source_operation_ids,collision_count
```

## Checks

30 checks. All must PASS.

Key invariant: `materialized_cell_count == input_touched_cell_count` (every touched cell is materialized).

## Verdict

`MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE`

## Example usage

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1
```

Or invoke the CLI directly:

```text
dotnet run --project src/PZMapForge.Cli -- \
  deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0 \
  --tile-buffer-result .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json \
  --tile-buffer-cells  .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\map_00.sandbox_writer_tile_buffer_cells.csv \
  --tile-buffer-ownership .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\map_00.sandbox_writer_tile_buffer_ownership.json \
  --tile-buffer-replay-log .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\map_00.sandbox_writer_tile_buffer_replay_log.json \
  --tile-buffer-collision-report .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\map_00.sandbox_writer_tile_buffer_collision_report.json \
  --tile-buffer-forbidden-output-guard .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0\map_00\map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json \
  --output-root .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00 \
  --output-json .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json \
  --output-md   .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md \
  --output-csv  .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv \
  --summary     .local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt
```
