# MAP-27B: WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Buffer V0

## Purpose

Applies MAP-27A sandbox operation files to an internal 256x256 tile buffer.
Produces deterministic planning artifacts. No PZ runtime files are written.

## Inputs

| Argument | Description |
|---|---|
| `--sandbox-writer-result` | MAP-27A result JSON (`map_00.sandbox_writer_v0.json`) |
| `--component-op` | Component operation file from MAP-27A |
| `--lot-op` | Lot operation file from MAP-27A |
| `--building-slot-op` | Building slot operation file from MAP-27A |
| `--access-op` | Access operation file from MAP-27A |
| `--forbidden-guard` | Forbidden output guard JSON from MAP-27A |
| `--output-root` | Output directory (must contain `.local`) |
| `--output-json` | Main result JSON output path |
| `--output-md` | Markdown report output path |
| `--output-csv` | Check CSV output path |
| `--summary` | Plain-text summary output path |

## Outputs

### Main outputs (4)

- `map_00.sandbox_writer_tile_buffer_v0.json` — full result with all checks
- `map_00.sandbox_writer_tile_buffer_v0.md` — markdown report
- `map_00.sandbox_writer_tile_buffer_v0.csv` — check CSV
- `map_00.sandbox_writer_tile_buffer_v0.summary.txt` — plain-text summary

### Extra outputs (5)

- `map_00.sandbox_writer_tile_buffer_cells.csv` — per-cell touched state (x, y, primary owner, tags, collision count)
- `map_00.sandbox_writer_tile_buffer_ownership.json` — ownership summary by kind
- `map_00.sandbox_writer_tile_buffer_replay_log.json` — ordered replay of all 17 operations
- `map_00.sandbox_writer_tile_buffer_collision_report.json` — all collision records
- `map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json` — inherited forbidden output declarations

## Buffer model

- 256x256 cells, PNG pixel tile space, origin top-left
- Coordinate system: `PNG_PIXEL_TILE_SPACE`
- Ownership priority: `BUILDING_FOOTPRINT(4) > ACCESS_LINK(3) > LOT_BOUNDARY(2) > COMPONENT_ENVELOPE(1)`
- Collision resolution: `OVERRIDE_BY_PRIORITY`, `SAME_OWNER_TAG_MERGE`, `LOWER_PRIORITY_RETAINED`
- Zero-dimension operations (width_px=0 or height_px=0) produce replay entries but apply zero cells

## Verdict

`MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_COMPLETE`

## Security constraints

This writer does NOT emit:
- `.lotpack` or `.lotheader` files
- `WorldGenOverride.lua` or any runtime Lua
- Files into `media/maps`
- Anything into `steamapps/workshop`
- Any claim of `writer_ready`, `runtime_valid`, or `materialized`

## Example usage

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1
```

Or invoke the CLI directly:

```text
dotnet run --project src/PZMapForge.Cli -- \
  deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0 \
  --sandbox-writer-result .local/map27a/map_00.sandbox_writer_v0.json \
  --component-op .local/map27a/map_00.sandbox_writer_component_operations.json \
  --lot-op .local/map27a/map_00.sandbox_writer_lot_operations.json \
  --building-slot-op .local/map27a/map_00.sandbox_writer_building_slot_operations.json \
  --access-op .local/map27a/map_00.sandbox_writer_access_operations.json \
  --forbidden-guard .local/map27a/map_00.sandbox_writer_forbidden_output_guard.json \
  --output-root .local/map27b \
  --output-json .local/map27b/map_00.sandbox_writer_tile_buffer_v0.json \
  --output-md .local/map27b/map_00.sandbox_writer_tile_buffer_v0.md \
  --output-csv .local/map27b/map_00.sandbox_writer_tile_buffer_v0.csv \
  --summary .local/map27b/map_00.sandbox_writer_tile_buffer_v0.summary.txt
```
