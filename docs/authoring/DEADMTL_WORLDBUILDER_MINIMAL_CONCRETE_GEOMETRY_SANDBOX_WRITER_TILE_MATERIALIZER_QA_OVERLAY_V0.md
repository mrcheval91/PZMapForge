# MAP-27D — DeadMTL Worldbuilder Minimal Concrete Geometry
# Sandbox Writer Tile Materializer QA Overlay v0

## Purpose

Renders a deterministic 1024x1024 pixel PNG visual QA overlay from the materialized cells
produced by MAP-27C (Sandbox Writer Tile Materializer v0).

Each tile-space cell (within the 256x256 source grid) is drawn as a 4x4 pixel block in the
output image.  The overlay is a diagnostic artifact — it is not a PZ runtime file, not a
playable map, and does not claim writer-ready or materialized status.

## Claim boundary

    sandbox_only: true
    sandbox_materialized_source: true
    visual_qa_overlay_written: true
    pz_runtime_materialized: false
    writer_ready: false
    runtime_valid: false
    materialized: false
    runtime_proof_claimed: false
    public_playable_packaging_claimed: false

## Security constraints

This step MUST NOT and WILL NOT produce:

- Any `.lotpack` file
- Any `.lotheader` file
- Any `WorldGenOverride.lua` file
- Any runtime Lua file
- Any `media/maps` output
- Any `compile-worldgen` invocation
- Any mutation of `map_00.png`
- Any Project Zomboid installation artifacts
- Any Steam Workshop output
- Any runtime proof claim
- Any writer_ready claim
- Any public playable packaging claim
- Any materialized PZ output claim

## Inputs (7 required files from MAP-27C output)

| Argument                             | Expected file                                                             |
|--------------------------------------|---------------------------------------------------------------------------|
| `--tile-materializer-result`         | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json` |
| `--materialized-cells`               | `map_00.sandbox_writer_tile_materialized_cells.csv`                       |
| `--material-palette`                 | `map_00.sandbox_writer_tile_material_palette.json`                        |
| `--layer-stack`                      | `map_00.sandbox_writer_tile_layer_stack.json`                             |
| `--materialization-replay-log`       | `map_00.sandbox_writer_tile_materialization_replay_log.json`              |
| `--materialization-ownership-summary`| `map_00.sandbox_writer_tile_materialization_ownership_summary.json`       |
| `--materializer-forbidden-output-guard` | `map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json`  |

## Outputs (4 main + 4 extra)

### Main outputs

| Argument        | File                                                                       |
|-----------------|----------------------------------------------------------------------------|
| `--output-json` | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json` |
| `--output-md`   | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md`   |
| `--output-csv`  | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv`  |
| `--summary`     | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt` |

### Extra outputs (written to `--output-root`)

| File                                                              | Kind                  |
|-------------------------------------------------------------------|-----------------------|
| `map_00.sandbox_writer_tile_materializer_qa_overlay.png`         | PNG overlay (1024x1024) |
| `map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json` | Material color legend |
| `map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv`  | Per-material counts   |
| `map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json` | Inherited forbidden guard |

## Material color palette

| Material kind                         | Hex color |
|---------------------------------------|-----------|
| `BUILDING_EXTERIOR_WALL_CANDIDATE`    | `#1F1F1F` |
| `BUILDING_INTERIOR_FLOOR_CANDIDATE`   | `#A8A8A8` |
| `ACCESS_EDGE_CANDIDATE`               | `#2F6FDB` |
| `LOT_YARD_OR_SERVICE_SPACE_CANDIDATE` | `#4F8A3B` |
| `COMPONENT_RESIDUAL_SPACE_CANDIDATE`  | `#7A4E2A` |
| (background)                          | `#101010` |

## Rendering rules

- Source grid: 256x256 tile-space cells
- Scale factor: 4 (each cell = 4x4 pixels)
- Output image: 1024x1024 pixels
- Pixel offset: `(cell.x * 4, cell.y * 4)` with size `(4, 4)`
- Background fill: `#101010`
- Interpolation: nearest-neighbor (pixel-perfect)
- Format: PNG (System.Drawing, Windows-only)

## CLI usage

    dotnet run --project src/PZMapForge.Cli -- \
        deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0 \
        --tile-materializer-result            <MAP-27C json> \
        --materialized-cells                  <csv> \
        --material-palette                    <json> \
        --layer-stack                         <json> \
        --materialization-replay-log          <json> \
        --materialization-ownership-summary   <json> \
        --materializer-forbidden-output-guard <json> \
        --output-root   <path ending in .local> \
        --output-json   <json> \
        --output-md     <md> \
        --output-csv    <csv> \
        --summary       <txt>

## Helper script

    examples/deadmtl-layer-pack/scripts/run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.ps1

Reads MAP-27C outputs from the default authoring path.  Pass `-AuthoringRoot <path>` to override.

## Verdict values

| Verdict                                                                                               | Meaning               |
|-------------------------------------------------------------------------------------------------------|-----------------------|
| `MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_COMPLETE` | All 39 checks passed |
| `MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_INVALID`  | One or more checks failed |

## Exit codes

| Code | Meaning                               |
|------|---------------------------------------|
| `0`  | COMPLETE — all checks passed          |
| `1`  | INVALID — missing inputs, bad args, or check failures |

## Relationship to MAP-27C

MAP-27D reads the `materialized_cells.csv` produced by MAP-27C (which already contains
`material_kind` and `layer_kind` columns).  It does not re-run materialization; it only
renders a visual representation of already-materialized cells.

## Provisional status

This step is PROVISIONAL.  It produces a sandbox-only diagnostic overlay.  No claim
of runtime validity, writer-readiness, or playability is made.
