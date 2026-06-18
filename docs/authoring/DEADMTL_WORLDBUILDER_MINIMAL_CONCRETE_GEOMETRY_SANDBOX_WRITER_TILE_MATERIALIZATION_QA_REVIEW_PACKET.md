# MAP-27E — DeadMTL Worldbuilder Minimal Concrete Geometry
# Sandbox Writer Tile Materialization QA Review Packet

## Purpose

Produces a deterministic QA review packet that audits the full MAP-27C/MAP-27D
sandbox tile materialization chain.  It reads all 18 canonical output files from
MAP-27C and MAP-27D, hashes them, verifies verdicts, checks counts, inspects the
PNG overlay dimensions, and emits a structured review receipt.

This step does NOT generate a new overlay.  It only audits and summarizes existing
canonical outputs.

## Claim boundary

    sandbox_only: true
    sandbox_materialized_source: true
    visual_qa_overlay_written: true (inherited from MAP-27D; not re-generated here)
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

## Inputs

### MAP-27C inputs (10 required files)

| File |
|------|
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt` |
| `map_00.sandbox_writer_tile_materialized_cells.csv` |
| `map_00.sandbox_writer_tile_material_palette.json` |
| `map_00.sandbox_writer_tile_layer_stack.json` |
| `map_00.sandbox_writer_tile_materialization_replay_log.json` |
| `map_00.sandbox_writer_tile_materialization_ownership_summary.json` |
| `map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json` |

### MAP-27D inputs (8 required files)

| File |
|------|
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt` |
| `map_00.sandbox_writer_tile_materializer_qa_overlay.png` |
| `map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json` |
| `map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv` |
| `map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json` |

## Outputs (4 files)

| Argument        | File |
|-----------------|------|
| `--output-json` | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json` |
| `--output-md`   | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.md` |
| `--output-csv`  | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.csv` |
| `--summary`     | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.summary.txt` |

## Expected counts (real MAP-27C/MAP-27D chain)

| Field | Expected |
|-------|----------|
| `materialized_cell_count` (MAP-27C) | 5340 |
| `rendered_cell_count` (MAP-27D) | 5340 |
| `building_wall_candidate_cell_count` | 850 |
| `building_floor_candidate_cell_count` | 2444 |
| `access_edge_cell_count` | 148 |
| `lot_space_cell_count` | 1898 |
| `component_residual_cell_count` | 0 |
| `material_kind_count` | 5 |
| `layer_kind_count` | 5 |
| `overlay_width` | 1024 |
| `overlay_height` | 1024 |
| `scale` | 4 |

## CLI usage

    dotnet run --project src/PZMapForge.Cli -- \
        deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet \
        --tile-materializer-root           <MAP-27C canonical root> \
        --tile-materializer-qa-overlay-root <MAP-27D canonical root> \
        --output-root   <path containing .local> \
        --output-json   <json> \
        --output-md     <md> \
        --output-csv    <csv> \
        --summary       <txt>

## Helper script

    examples/deadmtl-layer-pack/scripts/run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet.ps1

Uses canonical paths by default:
- MAP-27C input:  `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\`
- MAP-27D input:  `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0\map_00\`
- Output:         `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet\map_00\`

## Check list (40 checks)

| # | Check ID |
|---|----------|
| 1 | MAP27C_ROOT_EXISTS |
| 2 | MAP27D_ROOT_EXISTS |
| 3 | MAP27C_10_EXPECTED_FILES_EXIST |
| 4 | MAP27D_8_EXPECTED_FILES_EXIST |
| 5 | ALL_18_REVIEWED_FILES_HASHED |
| 6 | MAP27C_RESULT_VERDICT_COMPLETE |
| 7 | MAP27C_RESULT_IS_VALID_TRUE |
| 8 | MAP27C_SANDBOX_ONLY_TRUE |
| 9 | MAP27C_SANDBOX_MATERIALIZED_TRUE |
| 10 | MAP27C_PZ_RUNTIME_MATERIALIZED_FALSE |
| 11 | MAP27D_RESULT_VERDICT_COMPLETE |
| 12 | MAP27D_RESULT_IS_VALID_TRUE |
| 13 | MAP27D_SANDBOX_ONLY_TRUE |
| 14 | MAP27D_VISUAL_QA_OVERLAY_WRITTEN_TRUE |
| 15 | MAP27D_PZ_RUNTIME_MATERIALIZED_FALSE |
| 16 | MATERIALIZED_CELL_COUNT_5340 |
| 17 | RENDERED_CELL_COUNT_5340 |
| 18 | MATERIALIZED_RENDERED_COUNTS_MATCH |
| 19 | WALL_COUNT_850 |
| 20 | FLOOR_COUNT_2444 |
| 21 | ACCESS_COUNT_148 |
| 22 | LOT_COUNT_1898 |
| 23 | COMPONENT_RESIDUAL_COUNT_0 |
| 24 | MATERIAL_KIND_COUNT_5 |
| 25 | LAYER_KIND_COUNT_5 |
| 26 | OVERLAY_PNG_EXISTS |
| 27 | OVERLAY_PNG_HASHED |
| 28 | OVERLAY_PNG_WIDTH_1024 |
| 29 | OVERLAY_PNG_HEIGHT_1024 |
| 30 | OVERLAY_SCALE_4 |
| 31 | CANONICAL_MAP27C_ROOT |
| 32 | CANONICAL_MAP27D_ROOT |
| 33 | CANONICAL_MAP27E_OUTPUT_ROOT |
| 34 | POST_REVIEW_FORBIDDEN_SCAN_PASS |
| 35 | WRITER_READY_FALSE |
| 36 | RUNTIME_VALID_FALSE |
| 37 | MATERIALIZED_FALSE |
| 38 | NO_RUNTIME_PROOF_CLAIM |
| 39 | NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM |
| 40 | NO_RUNTIME_OUTPUTS_EMITTED |

## Verdict values

| Verdict | Meaning |
|---------|---------|
| `MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_COMPLETE` | All 40 checks passed |
| `MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_INVALID`  | One or more checks failed |

## Exit codes

| Code | Meaning |
|------|---------|
| `0`  | COMPLETE - all checks passed |
| `1`  | INVALID - missing inputs, bad args, or check failures |

## Relationship to MAP-27C and MAP-27D

MAP-27E reads the canonical outputs of both MAP-27C (tile materializer) and MAP-27D
(tile materializer QA overlay) without re-running either step.  It hashes all 18
source files, parses the verdict JSONs, and verifies the materialization chain is
internally consistent and claim-bounded.

## Provisional status

This step is PROVISIONAL.  It produces a sandbox-only QA receipt.  No claim of
runtime validity, writer-readiness, or playability is made.
