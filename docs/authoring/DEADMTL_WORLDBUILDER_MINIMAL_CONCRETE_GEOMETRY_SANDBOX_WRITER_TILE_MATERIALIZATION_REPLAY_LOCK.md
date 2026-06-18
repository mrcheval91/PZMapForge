# MAP-27G — DeadMTL Worldbuilder Minimal Concrete Geometry
# Sandbox Writer Tile Materialization Replay Lock

## Purpose

Consumes the MAP-27F acceptance gate result JSON and MAP-27C tile materializer replay source files,
and locks the exact replayable source set for the next sandbox-only experiment.

This step does NOT generate a new overlay.  It does NOT write PZ runtime files.
It does NOT approve writer-ready, runtime, or playable status.
It only locks the deterministic set of inputs so the next experiment can be reproduced
exactly.

## Claim boundary

    sandbox_only: true
    sandbox_materialized_source: true
    visual_qa_overlay_written: true (inherited from MAP-27F)
    pz_runtime_materialized: false
    accepted_for_next_sandbox_experiment: true (inherited from MAP-27F)
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

### MAP-27F acceptance gate root (4 required sanity files; only the JSON is locked)

| File |
|------|
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt` |

### MAP-27C tile materializer root (7 replay source files; all 7 locked)

| File | Role |
|------|------|
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json` | TILE_MATERIALIZER_RESULT_JSON |
| `map_00.sandbox_writer_tile_materialized_cells.csv` | MATERIALIZED_CELLS_CSV |
| `map_00.sandbox_writer_tile_material_palette.json` | MATERIAL_PALETTE_JSON |
| `map_00.sandbox_writer_tile_layer_stack.json` | LAYER_STACK_JSON |
| `map_00.sandbox_writer_tile_materialization_replay_log.json` | MATERIALIZATION_REPLAY_LOG_JSON |
| `map_00.sandbox_writer_tile_materialization_ownership_summary.json` | MATERIALIZATION_OWNERSHIP_SUMMARY_JSON |
| `map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json` | MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON |

## Locked file set

8 locked files total: 1 MAP-27F acceptance gate result JSON + 7 MAP-27C replay source files.

| # | Stage | Role |
|---|-------|------|
| 1 | MAP-27F | ACCEPTANCE_GATE_RESULT_JSON |
| 2 | MAP-27C | TILE_MATERIALIZER_RESULT_JSON |
| 3 | MAP-27C | MATERIALIZED_CELLS_CSV |
| 4 | MAP-27C | MATERIAL_PALETTE_JSON |
| 5 | MAP-27C | LAYER_STACK_JSON |
| 6 | MAP-27C | MATERIALIZATION_REPLAY_LOG_JSON |
| 7 | MAP-27C | MATERIALIZATION_OWNERSHIP_SUMMARY_JSON |
| 8 | MAP-27C | MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON |

## Outputs (4 files)

| Argument        | File |
|-----------------|------|
| `--output-json` | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json` |
| `--output-md`   | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.md` |
| `--output-csv`  | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.csv` |
| `--summary`     | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.summary.txt` |

## Replay lock ID

The replay lock ID is deterministically computed from all 8 file hashes (uppercase role names):

    lock_input = "MAP27G_REPLAY_LOCK_V1"
               + "|ACCEPTANCE_GATE_RESULT_JSON:<sha256>"
               + "|TILE_MATERIALIZER_RESULT_JSON:<sha256>"
               + "|MATERIALIZED_CELLS_CSV:<sha256>"
               + "|MATERIAL_PALETTE_JSON:<sha256>"
               + "|LAYER_STACK_JSON:<sha256>"
               + "|MATERIALIZATION_REPLAY_LOG_JSON:<sha256>"
               + "|MATERIALIZATION_OWNERSHIP_SUMMARY_JSON:<sha256>"
               + "|MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON:<sha256>"
    replay_lock_id = "map_00_replay_lock_" + SHA256(UTF8(lock_input))[..16]

## Locked file properties

All 8 locked files have:

    locked_for_replay: true
    runtime_consumable: false
    writer_consumable: false

## Replay lock status

| Status | Meaning |
|--------|---------|
| `LOCKED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY` | All 8 files hashed and replay_lock_id computed |
| `LOCK_FAILED` | One or more files missing or unhashable |

## CLI usage

    dotnet run --project src/PZMapForge.Cli -- \
        deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-replay-lock \
        --acceptance-gate-root   <MAP-27F canonical root> \
        --tile-materializer-root <MAP-27C canonical root> \
        --output-root   <path containing .local> \
        --output-json   <json> \
        --output-md     <md> \
        --output-csv    <csv> \
        --summary       <txt>

## Helper script

    examples/deadmtl-layer-pack/scripts/run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-replay-lock.ps1

Uses canonical paths by default:
- MAP-27F input:  `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate\map_00\`
- MAP-27C input:  `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0\map_00\`
- Output:         `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-replay-lock\map_00\`

## Check list (43 checks)

| # | Check ID |
|---|----------|
| 1 | MAP27F_ROOT_EXISTS |
| 2 | MAP27F_4_EXPECTED_FILES_EXIST |
| 3 | MAP27F_ACCEPTANCE_GATE_HASHED |
| 4 | MAP27F_VERDICT_COMPLETE |
| 5 | MAP27F_IS_VALID_TRUE |
| 6 | MAP27F_GATE_STATUS_ACCEPTED_FOR_NEXT_SANDBOX_ONLY |
| 7 | MAP27F_ACCEPTED_FOR_NEXT_SANDBOX_TRUE |
| 8 | MAP27F_ACCEPTED_FOR_RUNTIME_WRITER_FALSE |
| 9 | MAP27F_ACCEPTED_FOR_PLAYABLE_EXPORT_FALSE |
| 10 | MAP27C_ROOT_EXISTS |
| 11 | MAP27C_7_REPLAY_SOURCE_FILES_EXIST |
| 12 | ALL_8_REPLAY_LOCK_FILES_HASHED |
| 13 | ALL_8_REPLAY_LOCK_FILES_LOCKED_FOR_REPLAY |
| 14 | ALL_8_REPLAY_LOCK_FILES_RUNTIME_CONSUMABLE_FALSE |
| 15 | ALL_8_REPLAY_LOCK_FILES_WRITER_CONSUMABLE_FALSE |
| 16 | REPLAY_LOCK_ID_PRESENT |
| 17 | REPLAY_LOCK_FILE_COUNT_8 |
| 18 | SANDBOX_ONLY_TRUE |
| 19 | SANDBOX_MATERIALIZED_SOURCE_TRUE |
| 20 | VISUAL_QA_OVERLAY_WRITTEN_TRUE |
| 21 | PZ_RUNTIME_MATERIALIZED_FALSE |
| 22 | MATERIALIZED_CELL_COUNT_5340 |
| 23 | RENDERED_CELL_COUNT_5340 |
| 24 | COUNT_MATCH_SUMMARY_MATCH |
| 25 | WALL_COUNT_850 |
| 26 | FLOOR_COUNT_2444 |
| 27 | ACCESS_COUNT_148 |
| 28 | LOT_COUNT_1898 |
| 29 | COMPONENT_RESIDUAL_COUNT_0 |
| 30 | MATERIAL_KIND_COUNT_5 |
| 31 | LAYER_KIND_COUNT_5 |
| 32 | OVERLAY_PNG_WIDTH_1024 |
| 33 | OVERLAY_PNG_HEIGHT_1024 |
| 34 | NEXT_ALLOWED_EXPERIMENT_SANDBOX_ONLY |
| 35 | FORBIDDEN_STEPS_LISTED |
| 36 | BLOCKING_REASONS_EMPTY |
| 37 | POST_REPLAY_LOCK_FORBIDDEN_SCAN_PASS |
| 38 | WRITER_READY_FALSE |
| 39 | RUNTIME_VALID_FALSE |
| 40 | MATERIALIZED_FALSE |
| 41 | NO_RUNTIME_PROOF_CLAIM |
| 42 | NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM |
| 43 | NO_RUNTIME_OUTPUTS_EMITTED |

## Verdict values

| Verdict | Meaning |
|---------|---------|
| `MAP27G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK_COMPLETE` | All 43 checks passed |
| `MAP27G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK_INVALID`  | One or more checks failed |

## Exit codes

| Code | Meaning |
|------|---------|
| `0`  | COMPLETE - all checks passed |
| `1`  | INVALID - missing inputs, bad args, or check failures |

## Relationship to MAP-27F and MAP-27C

MAP-27G reads the MAP-27F acceptance gate JSON directly for acceptance status and
counts (flat top-level fields).  It reads MAP-27C files only for hashing — it does
not re-run the materializer or re-parse its output for validation.

## Provisional status

This step is PROVISIONAL.  It produces a sandbox-only replay lock receipt.
No claim of runtime validity, writer-readiness, or playability is made.
