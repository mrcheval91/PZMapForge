# MAP-27G — DeadMTL Worldbuilder Minimal Concrete Geometry
# Sandbox Writer Tile Materialization Replay Lock

## Purpose

Consumes the MAP-27F acceptance gate and MAP-27C tile materializer source files,
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

### MAP-27F acceptance gate root (4 required files)

| File |
|------|
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv` |
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt` |

### MAP-27C tile materializer root (4 locked files)

| File | Role |
|------|------|
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json` | tile_materializer_result_json |
| `map_00.sandbox_writer_tile_materialized_cells.csv` | materialized_cells_csv |
| `map_00.sandbox_writer_tile_material_palette.json` | material_palette_json |
| `map_00.sandbox_writer_tile_layer_stack.json` | layer_stack_json |

## Outputs (4 files)

| Argument        | File |
|-----------------|------|
| `--output-json` | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json` |
| `--output-md`   | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.md` |
| `--output-csv`  | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.csv` |
| `--summary`     | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.summary.txt` |

## Replay lock ID

The replay lock ID is deterministically computed from all 8 file hashes:

    lock_input = "MAP27G_REPLAY_LOCK_V1"
               + "|acceptance_gate_json:<sha256>"
               + "|acceptance_gate_md:<sha256>"
               + "|acceptance_gate_csv:<sha256>"
               + "|acceptance_gate_summary:<sha256>"
               + "|tile_materializer_result_json:<sha256>"
               + "|materialized_cells_csv:<sha256>"
               + "|material_palette_json:<sha256>"
               + "|layer_stack_json:<sha256>"
    replay_lock_id = "map_00_replay_lock_" + SHA256(UTF8(lock_input))[..16]

## Locked file properties

All 8 locked files have:

    locked_for_replay: true
    runtime_consumable: false
    writer_consumable: false

## Replay lock status

| Status | Meaning |
|--------|---------|
| `LOCKED` | All 8 files hashed and replay_lock_id computed |
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
| 1 | ACCEPTANCE_GATE_ROOT_EXISTS |
| 2 | ACCEPTANCE_GATE_4_FILES_EXIST |
| 3 | ACCEPTANCE_GATE_JSON_HASHED |
| 4 | ACCEPTANCE_GATE_VERDICT_COMPLETE |
| 5 | ACCEPTANCE_GATE_IS_VALID_TRUE |
| 6 | ACCEPTANCE_GATE_STATUS_ACCEPTED |
| 7 | SOURCE_ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_TRUE |
| 8 | SOURCE_ACCEPTED_FOR_RUNTIME_WRITER_FALSE |
| 9 | SOURCE_ACCEPTED_FOR_PLAYABLE_EXPORT_FALSE |
| 10 | TILE_MATERIALIZER_ROOT_EXISTS |
| 11 | TILE_MATERIALIZER_4_LOCK_FILES_EXIST |
| 12 | TILE_MATERIALIZER_RESULT_JSON_HASHED |
| 13 | LOCK_FILE_1_ACCEPTANCE_GATE_JSON_EXISTS |
| 14 | LOCK_FILE_2_ACCEPTANCE_GATE_MD_EXISTS |
| 15 | LOCK_FILE_3_ACCEPTANCE_GATE_CSV_EXISTS |
| 16 | LOCK_FILE_4_ACCEPTANCE_GATE_SUMMARY_EXISTS |
| 17 | LOCK_FILE_5_TILE_MATERIALIZER_RESULT_JSON_EXISTS |
| 18 | LOCK_FILE_6_MATERIALIZED_CELLS_CSV_EXISTS |
| 19 | LOCK_FILE_7_MATERIAL_PALETTE_JSON_EXISTS |
| 20 | LOCK_FILE_8_LAYER_STACK_JSON_EXISTS |
| 21 | LOCK_FILE_1_SHA256_COMPUTED |
| 22 | LOCK_FILE_2_SHA256_COMPUTED |
| 23 | LOCK_FILE_3_SHA256_COMPUTED |
| 24 | LOCK_FILE_4_SHA256_COMPUTED |
| 25 | LOCK_FILE_5_SHA256_COMPUTED |
| 26 | LOCK_FILE_6_SHA256_COMPUTED |
| 27 | LOCK_FILE_7_SHA256_COMPUTED |
| 28 | LOCK_FILE_8_SHA256_COMPUTED |
| 29 | ALL_LOCK_FILES_LOCKED_FOR_REPLAY_TRUE |
| 30 | ALL_LOCK_FILES_RUNTIME_CONSUMABLE_FALSE |
| 31 | ALL_LOCK_FILES_WRITER_CONSUMABLE_FALSE |
| 32 | LOCK_FILE_COUNT_8 |
| 33 | REPLAY_LOCK_ID_GENERATED |
| 34 | REPLAY_LOCK_ID_PREFIX_CORRECT |
| 35 | REPLAY_LOCK_STATUS_LOCKED |
| 36 | SANDBOX_ONLY_TRUE |
| 37 | PZ_RUNTIME_MATERIALIZED_FALSE |
| 38 | ACCEPTED_FOR_RUNTIME_WRITER_FALSE |
| 39 | WRITER_READY_FALSE |
| 40 | NO_RUNTIME_PROOF_CLAIM |
| 41 | NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM |
| 42 | BLOCKING_REASONS_EMPTY |
| 43 | POST_REPLAY_LOCK_FORBIDDEN_SCAN_PASS |

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
