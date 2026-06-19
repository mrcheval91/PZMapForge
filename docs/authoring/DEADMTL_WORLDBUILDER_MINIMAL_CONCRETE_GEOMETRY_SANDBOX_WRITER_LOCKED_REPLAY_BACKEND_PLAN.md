# MAP-27J: DeadMTL WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Replay Backend Plan

## Claim boundary

| Field | Value |
|-------|-------|
| `sandbox_only` | `true` |
| `sandbox_backend_plan_only` | `true` |
| `writer_ready` | `false` |
| `runtime_valid` | `false` |
| `materialized` | `false` |
| `pz_runtime_materialized` | `false` |
| `runtime_proof_claimed` | `false` |
| `public_playable_packaging_claimed` | `false` |

This is a backend-neutral plan object. It is NOT a runtime writer, NOT a lotpack writer, and NOT a Project Zomboid export.

## Input (MAP-27I dry-run root)

| Role | File |
|------|------|
| DRY_RUN_RESULT_JSON | `map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.json` |
| DRY_RUN_SUMMARY_TXT | `map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.summary.txt` |
| LOCKED_REPLAY_MATERIAL_COUNTS_CSV | `map_00.sandbox_writer_locked_replay_material_counts.csv` |
| LOCKED_REPLAY_SOURCE_MANIFEST_JSON | `map_00.sandbox_writer_locked_replay_source_manifest.json` |
| LOCKED_REPLAY_DIGEST_JSON | `map_00.sandbox_writer_locked_replay_digest.json` |
| LOCKED_REPLAY_FORBIDDEN_OUTPUT_GUARD_JSON | `map_00.sandbox_writer_locked_replay_forbidden_output_guard.json` |

## Outputs

| File | Description |
|------|-------------|
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.json` | Full result JSON |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.md` | Markdown summary |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.csv` | Checks CSV |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.summary.txt` | Summary text |
| `map_00.sandbox_writer_locked_replay_backend_operation_plan.json` | Operation plan JSON |
| `map_00.sandbox_writer_locked_replay_backend_operation_plan.csv` | Operation plan CSV |
| `map_00.sandbox_writer_locked_replay_backend_source_manifest.json` | Source manifest JSON |
| `map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json` | Forbidden output guard |

## Backend operation records

5 canonical material bucket records, one per group. None produce runtime files, binary files, Lua files, or install paths. All require the locked replay digest.

| Order | Operation Kind | Bucket | Planned Cells | Target Family |
|-------|---------------|--------|--------------|---------------|
| 1 | WRITE_WALL_CANDIDATE_BUCKET | WALL | 850 | BUILDING_WALL_LAYER_CANDIDATE |
| 2 | WRITE_FLOOR_CANDIDATE_BUCKET | FLOOR | 2444 | BUILDING_FLOOR_LAYER_CANDIDATE |
| 3 | WRITE_ACCESS_EDGE_BUCKET | ACCESS | 148 | ACCESS_EDGE_LAYER_CANDIDATE |
| 4 | WRITE_LOT_SPACE_BUCKET | LOT | 1898 | LOT_SPACE_LAYER_CANDIDATE |
| 5 | WRITE_COMPONENT_RESIDUAL_BUCKET | COMPONENT | 0 | COMPONENT_RESIDUAL_LAYER_CANDIDATE |

## Checks (51)

51 checks run in deterministic order. All must pass for `is_valid=true` and
`backend_plan_status=BACKEND_PLAN_COMPLETE`.

The CLI finalizes the forbidden artifact scan after writing the 8 MAP-27J output files, then rewrites the result files with the finalized scan/verdict.

## Forbidden steps (11)

| Category |
|----------|
| LOT_PACK_RUNTIME_BINARY |
| LOT_HEADER_RUNTIME_BINARY |
| WORLDGEN_OVERRIDE_LUA |
| RUNTIME_LUA |
| PROJECT_ZOMBOID_INSTALL_PATH |
| STEAM_WORKSHOP_OUTPUT |
| COMPILE_WORLDGEN_INVOCATION |
| MAP_00_PNG_MUTATION |
| RUNTIME_PROOF_CLAIM |
| WRITER_READY_CLAIM |
| PUBLIC_PLAYABLE_PACKAGING_CLAIM |

## Next allowed experiment

`MAP-27K_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER` (SANDBOX_ONLY_NOT_RUNTIME)
