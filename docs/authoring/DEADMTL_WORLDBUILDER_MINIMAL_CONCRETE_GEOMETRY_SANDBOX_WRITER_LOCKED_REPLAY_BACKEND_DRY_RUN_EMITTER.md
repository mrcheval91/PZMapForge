# MAP-27K: DeadMTL WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Replay Backend Dry-Run Emitter

## Claim boundary

| Field | Value |
|-------|-------|
| `sandbox_only` | `true` |
| `sandbox_backend_dry_run_emitter_only` | `true` |
| `writer_ready` | `false` |
| `runtime_valid` | `false` |
| `materialized` | `false` |
| `pz_runtime_materialized` | `false` |
| `runtime_proof_claimed` | `false` |
| `public_playable_packaging_claimed` | `false` |

This is a backend dry-run emitter. It is NOT a runtime writer, NOT a lotpack writer, and NOT a Project Zomboid export. Emitted records are sandbox-only dry-run artifacts; no runtime files, binary files, Lua files, or install paths are produced.

## Input (MAP-27J backend plan root)

| Role | File |
|------|------|
| BACKEND_PLAN_RESULT_JSON | `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.json` |
| BACKEND_OPERATION_PLAN_JSON | `map_00.sandbox_writer_locked_replay_backend_operation_plan.json` |
| BACKEND_OPERATION_PLAN_CSV | `map_00.sandbox_writer_locked_replay_backend_operation_plan.csv` |
| BACKEND_SOURCE_MANIFEST_JSON | `map_00.sandbox_writer_locked_replay_backend_source_manifest.json` |
| BACKEND_FORBIDDEN_OUTPUT_GUARD_JSON | `map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json` |

## Outputs

| File | Description |
|------|-------------|
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_dry_run_emitter.json` | Full result JSON |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_dry_run_emitter.md` | Markdown summary |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_dry_run_emitter.csv` | Checks CSV |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_dry_run_emitter.summary.txt` | Summary text |
| `map_00.sandbox_writer_locked_replay_backend_dry_run_operations.json` | Emitted operations JSON |
| `map_00.sandbox_writer_locked_replay_backend_dry_run_operations.csv` | Emitted operations CSV |
| `map_00.sandbox_writer_locked_replay_backend_dry_run_source_manifest.json` | Source manifest JSON |
| `map_00.sandbox_writer_locked_replay_backend_dry_run_digest.json` | Emission digest JSON |
| `map_00.sandbox_writer_locked_replay_backend_dry_run_forbidden_output_guard.json` | Forbidden output guard |

## Emitted operation records

5 canonical dry-run records, one per material bucket. None produce runtime files, binary files, Lua files, or install paths. All require the locked replay digest. All have `emission_status = DRY_RUN_EMITTED_SANDBOX_RECORD_ONLY`.

| Order | Emit Kind | Bucket | Planned Cells |
|-------|-----------|--------|--------------|
| 1 | EMIT_WALL_BUCKET_DRY_RUN_RECORD | WALL | 850 |
| 2 | EMIT_FLOOR_BUCKET_DRY_RUN_RECORD | FLOOR | 2444 |
| 3 | EMIT_ACCESS_BUCKET_DRY_RUN_RECORD | ACCESS | 148 |
| 4 | EMIT_LOT_BUCKET_DRY_RUN_RECORD | LOT | 1898 |
| 5 | EMIT_COMPONENT_BUCKET_DRY_RUN_RECORD | COMPONENT | 0 |

## Emission digest

A deterministic SHA-256 digest computed over:
- Protocol version tag `MAP27K_BACKEND_DRY_RUN_EMITTER_V1`
- `source_backend_plan_sha256`
- `source_locked_replay_digest`
- 5 emitted operation ids/kinds/buckets/planned counts/statuses
- Claim boundary flags

Stored as `backend_dry_run_emission_digest`.

## Checks (49)

49 checks run in deterministic order. All must pass for `is_valid=true` and
`emitter_status=BACKEND_DRY_RUN_EMITTER_COMPLETE`.

The CLI finalizes the forbidden artifact scan after writing all 9 MAP-27K output files, then rewrites the result files with the finalized scan/verdict.

## Next allowed experiment

`MAP-27L_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_AUDIT` (SANDBOX_ONLY_NOT_RUNTIME)
