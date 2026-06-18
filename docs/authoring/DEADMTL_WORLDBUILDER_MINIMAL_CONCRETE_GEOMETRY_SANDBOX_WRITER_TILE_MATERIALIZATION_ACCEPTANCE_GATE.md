# MAP-27F — DeadMTL Worldbuilder Minimal Concrete Geometry
# Sandbox Writer Tile Materialization Acceptance Gate

## Purpose

Consumes the MAP-27E QA review packet and decides whether the sandbox tile
materialization chain is acceptable as a future writer experiment input.

This step does NOT generate a new overlay.  It does NOT write PZ runtime files.
It does NOT approve writer-ready, runtime, or playable status.
It only evaluates whether the sandbox chain is internally consistent enough to
become the next controlled experiment input.

## Claim boundary

    sandbox_only: true
    sandbox_materialized_source: true
    visual_qa_overlay_written: true (inherited from MAP-27D via MAP-27E)
    pz_runtime_materialized: false
    accepted_for_next_sandbox_experiment: true (when all criteria pass)
    accepted_for_runtime_writer: false
    accepted_for_playable_export: false
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

### MAP-27E inputs (4 required files)

| File |
|------|
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.md` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.csv` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.summary.txt` |

## Outputs (4 files)

| Argument        | File |
|-----------------|------|
| `--output-json` | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json` |
| `--output-md`   | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md` |
| `--output-csv`  | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv` |
| `--summary`     | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt` |

## Expected values from real MAP-27E chain

| Field | Expected |
|-------|----------|
| `source_qa_review_packet_verdict` | `MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_COMPLETE` |
| `reviewed_file_count` | 18 |
| `review_check_count` | 40 |
| `review_passed_check_count` | 40 |
| `review_failed_check_count` | 0 |
| `materialized_cell_count` | 5340 |
| `rendered_cell_count` | 5340 |
| `building_wall_candidate_cell_count` | 850 |
| `building_floor_candidate_cell_count` | 2444 |
| `access_edge_cell_count` | 148 |
| `lot_space_cell_count` | 1898 |
| `component_residual_cell_count` | 0 |
| `material_kind_count` | 5 |
| `layer_kind_count` | 5 |
| `overlay_png_width` | 1024 |
| `overlay_png_height` | 1024 |

## Acceptance decision

| Field | Value |
|-------|-------|
| `acceptance_gate_status` | `ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY` |
| `accepted_for_next_sandbox_experiment` | `true` |
| `accepted_for_runtime_writer` | `false` |
| `accepted_for_playable_export` | `false` |
| `next_allowed_experiment_name` | `MAP-27G_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK` |
| `next_allowed_experiment_status` | `SANDBOX_ONLY_NOT_RUNTIME` |

## CLI usage

    dotnet run --project src/PZMapForge.Cli -- \
        deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate \
        --qa-review-packet-root  <MAP-27E canonical root> \
        --output-root   <path containing .local> \
        --output-json   <json> \
        --output-md     <md> \
        --output-csv    <csv> \
        --summary       <txt>

## Helper script

    examples/deadmtl-layer-pack/scripts/run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate.ps1

Uses canonical paths by default:
- MAP-27E input:  `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet\map_00\`
- Output:         `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate\map_00\`

## Check list (38 checks)

| # | Check ID |
|---|----------|
| 1 | MAP27E_ROOT_EXISTS |
| 2 | MAP27E_4_EXPECTED_FILES_EXIST |
| 3 | MAP27E_QA_REVIEW_PACKET_HASHED |
| 4 | MAP27E_VERDICT_COMPLETE |
| 5 | MAP27E_IS_VALID_TRUE |
| 6 | MAP27E_REVIEWED_FILE_COUNT_18 |
| 7 | MAP27E_CHECK_COUNT_40 |
| 8 | MAP27E_PASSED_CHECK_COUNT_40 |
| 9 | MAP27E_FAILED_CHECK_COUNT_0 |
| 10 | MAP27E_SANDBOX_ONLY_TRUE |
| 11 | MAP27E_SANDBOX_MATERIALIZED_SOURCE_TRUE |
| 12 | MAP27E_VISUAL_QA_OVERLAY_WRITTEN_TRUE |
| 13 | MAP27E_PZ_RUNTIME_MATERIALIZED_FALSE |
| 14 | MATERIALIZED_CELL_COUNT_5340 |
| 15 | RENDERED_CELL_COUNT_5340 |
| 16 | COUNT_MATCH_SUMMARY_MATCH |
| 17 | WALL_COUNT_850 |
| 18 | FLOOR_COUNT_2444 |
| 19 | ACCESS_COUNT_148 |
| 20 | LOT_COUNT_1898 |
| 21 | COMPONENT_RESIDUAL_COUNT_0 |
| 22 | MATERIAL_KIND_COUNT_5 |
| 23 | LAYER_KIND_COUNT_5 |
| 24 | OVERLAY_PNG_WIDTH_1024 |
| 25 | OVERLAY_PNG_HEIGHT_1024 |
| 26 | ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_TRUE |
| 27 | ACCEPTED_FOR_RUNTIME_WRITER_FALSE |
| 28 | ACCEPTED_FOR_PLAYABLE_EXPORT_FALSE |
| 29 | NEXT_ALLOWED_EXPERIMENT_SANDBOX_ONLY |
| 30 | FORBIDDEN_STEPS_LISTED |
| 31 | BLOCKING_REASONS_EMPTY |
| 32 | POST_ACCEPTANCE_FORBIDDEN_SCAN_PASS |
| 33 | WRITER_READY_FALSE |
| 34 | RUNTIME_VALID_FALSE |
| 35 | MATERIALIZED_FALSE |
| 36 | NO_RUNTIME_PROOF_CLAIM |
| 37 | NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM |
| 38 | NO_RUNTIME_OUTPUTS_EMITTED |

## Verdict values

| Verdict | Meaning |
|---------|---------|
| `MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_COMPLETE` | All 38 checks passed |
| `MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_INVALID`  | One or more checks failed |

## Exit codes

| Code | Meaning |
|------|---------|
| `0`  | COMPLETE - all checks passed |
| `1`  | INVALID - missing inputs, bad args, or check failures |

## Relationship to MAP-27E

MAP-27F reads only the canonical JSON output of MAP-27E.  It does not re-run
MAP-27C, MAP-27D, or MAP-27E.  It does not read the PNG overlay directly.
All counts and dimensions are sourced from the MAP-27E result JSON.

## Provisional status

This step is PROVISIONAL.  It produces a sandbox-only acceptance gate receipt.
No claim of runtime validity, writer-readiness, or playability is made.
