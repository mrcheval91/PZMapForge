# MAP-27H — DeadMTL Worldbuilder Minimal Concrete Geometry
# Sandbox Writer Tile Materialization Locked Replay Audit

## Purpose

Audits the MAP-27G1 replay lock by re-reading the replay lock result JSON,
re-hashing every locked file at its stored absolute path, recomputing the
replay_lock_id from the exact MAP-27G1 formula, and confirming the locked
source set is deterministic and reproducible.

This step does NOT generate new geometry. It does NOT write PZ runtime files.
It does NOT approve writer-ready, runtime, or playable status.
It only proves the MAP-27G1 replay lock is hash-stable and the lock ID is
reproducible from the current on-disk state.

## Claim boundary

    sandbox_only: true
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

## Input

### MAP-27G1 replay lock root (1 required file)

| File |
|------|
| `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json` |

The replay lock JSON contains the `locked_files` array with stored `file_path` and
`sha256` for all 8 locked files.

## Audit procedure

1. Read `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json`
2. Parse `locked_files` array (8 entries with stored `file_path` and `sha256`)
3. For each locked file: check existence at stored `file_path`, recompute SHA-256
4. Recompute `replay_lock_id` using MAP-27G1 formula with RECOMPUTED hashes:

       lock_input = "MAP27G_REPLAY_LOCK_V1"
                  + "|ACCEPTANCE_GATE_RESULT_JSON:<recomputed_sha256>"
                  + "|TILE_MATERIALIZER_RESULT_JSON:<recomputed_sha256>"
                  + "|MATERIALIZED_CELLS_CSV:<recomputed_sha256>"
                  + "|MATERIAL_PALETTE_JSON:<recomputed_sha256>"
                  + "|LAYER_STACK_JSON:<recomputed_sha256>"
                  + "|MATERIALIZATION_REPLAY_LOG_JSON:<recomputed_sha256>"
                  + "|MATERIALIZATION_OWNERSHIP_SUMMARY_JSON:<recomputed_sha256>"
                  + "|MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON:<recomputed_sha256>"
       recomputed_replay_lock_id = "map_00_replay_lock_" + SHA256(UTF8(lock_input))[..16]

5. Compare `recomputed_replay_lock_id` to stored `replay_lock_id`

## Outputs (4 files)

| Argument        | File |
|-----------------|------|
| `--output-json` | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json` |
| `--output-md`   | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.md` |
| `--output-csv`  | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.csv` |
| `--summary`     | `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.summary.txt` |

## Audit status values

| Status | Meaning |
|--------|---------|
| `VERIFIED_LOCKED_REPLAY_SOURCE_SET` | All 8 files re-hashed, all hashes match, replay_lock_id recomputed and confirmed |
| `AUDIT_FAILED` | One or more hash mismatches, missing files, or lock ID mismatch |

## CLI usage

    dotnet run --project src/PZMapForge.Cli -- \
        deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit \
        --replay-lock-root <MAP-27G canonical root> \
        --output-root   <path containing .local> \
        --output-json   <json> \
        --output-md     <md> \
        --output-csv    <csv> \
        --summary       <txt>

## Helper script

    examples/deadmtl-layer-pack/scripts/run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit.ps1

Uses canonical paths by default:
- MAP-27G1 input: `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-replay-lock\map_00\`
- Output:         `.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit\map_00\`

## Check list (45 checks)

| # | Check ID |
|---|----------|
| 1 | SOURCE_REPLAY_LOCK_ROOT_EXISTS |
| 2 | SOURCE_REPLAY_LOCK_FILE_EXISTS |
| 3 | SOURCE_REPLAY_LOCK_FILE_HASHED |
| 4 | SOURCE_REPLAY_LOCK_FILE_PARSEABLE |
| 5 | SOURCE_REPLAY_LOCK_STATUS_LOCKED_FOR_NEXT_SANDBOX |
| 6 | SOURCE_REPLAY_LOCK_ID_PRESENT |
| 7 | SOURCE_REPLAY_LOCK_FILE_COUNT_8 |
| 8 | SOURCE_REPLAY_LOCK_VERDICT_COMPLETE |
| 9 | SOURCE_REPLAY_LOCK_IS_VALID_TRUE |
| 10 | SOURCE_REPLAY_LOCK_SANDBOX_ONLY_TRUE |
| 11 | LOCKED_FILE_1_EXISTS |
| 12 | LOCKED_FILE_2_EXISTS |
| 13 | LOCKED_FILE_3_EXISTS |
| 14 | LOCKED_FILE_4_EXISTS |
| 15 | LOCKED_FILE_5_EXISTS |
| 16 | LOCKED_FILE_6_EXISTS |
| 17 | LOCKED_FILE_7_EXISTS |
| 18 | LOCKED_FILE_8_EXISTS |
| 19 | LOCKED_FILE_1_HASH_MATCHES |
| 20 | LOCKED_FILE_2_HASH_MATCHES |
| 21 | LOCKED_FILE_3_HASH_MATCHES |
| 22 | LOCKED_FILE_4_HASH_MATCHES |
| 23 | LOCKED_FILE_5_HASH_MATCHES |
| 24 | LOCKED_FILE_6_HASH_MATCHES |
| 25 | LOCKED_FILE_7_HASH_MATCHES |
| 26 | LOCKED_FILE_8_HASH_MATCHES |
| 27 | ALL_8_LOCKED_FILE_HASHES_MATCH |
| 28 | LOCKED_FILE_HASH_MISMATCH_COUNT_0 |
| 29 | LOCKED_FILE_HASH_MISSING_COUNT_0 |
| 30 | RECOMPUTED_REPLAY_LOCK_ID_PRESENT |
| 31 | RECOMPUTED_REPLAY_LOCK_ID_MATCHES_STORED |
| 32 | REPLAY_LOCK_ID_DETERMINISTIC |
| 33 | AUDIT_LOCKED_FILE_COUNT_8 |
| 34 | AUDIT_SANDBOX_ONLY_TRUE |
| 35 | AUDIT_PZ_RUNTIME_MATERIALIZED_FALSE |
| 36 | AUDIT_WRITER_READY_FALSE |
| 37 | AUDIT_RUNTIME_VALID_FALSE |
| 38 | AUDIT_MATERIALIZED_FALSE |
| 39 | AUDIT_NO_RUNTIME_PROOF_CLAIM |
| 40 | AUDIT_STATUS_VERIFIED |
| 41 | NEXT_ALLOWED_EXPERIMENT_SANDBOX_ONLY |
| 42 | NO_OLD_MAP27F_MD_IN_LOCK_FILES |
| 43 | AUDIT_MODE_CORRECT |
| 44 | AUDIT_REPLAY_LOCK_SOURCE_NEGATIVE |
| 45 | AUDIT_COMPLETE_NO_RUNTIME_OUTPUTS |

## Verdict values

| Verdict | Meaning |
|---------|---------|
| `MAP27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT_COMPLETE` | All 45 checks passed |
| `MAP27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT_INVALID`  | One or more checks failed |

## Exit codes

| Code | Meaning |
|------|---------|
| `0`  | COMPLETE - all checks passed |
| `1`  | INVALID - missing inputs, bad args, or check failures |

## Relationship to MAP-27G1

MAP-27H reads the MAP-27G1 replay lock JSON directly. It does not re-run MAP-27G.
It only re-hashes the locked files at their stored absolute paths and recomputes
the lock ID using the same MAP-27G1 formula.

A successful MAP-27H audit proves:
- All 8 locked files still exist at their stored paths
- None of the locked files have been modified since MAP-27G1 ran
- The replay_lock_id is stable and reproducible

## Provisional status

This step is PROVISIONAL. It produces a sandbox-only audit receipt.
No claim of runtime validity, writer-readiness, or playability is made.
