# MAP-27I: WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Materialization Replay Dry Run

## Purpose

MAP-27I is a sandbox-only pipeline step that verifies the 8 locked files from MAP-27H remain
intact, loads the materialized cells CSV, computes a deterministic locked replay digest, and
emits 8 output files for downstream audit. It does NOT produce any runtime artifact.

## Claim boundary

| Flag | Value |
|------|-------|
| `sandbox_only` | `true` |
| `sandbox_locked_replay_dry_run` | `true` |
| `writer_ready` | `false` |
| `runtime_valid` | `false` |
| `materialized` | `false` |
| `pz_runtime_materialized` | `false` |
| `runtime_proof_claimed` | `false` |
| `public_playable_packaging_claimed` | `false` |

## Inputs

| Input | Description |
|-------|-------------|
| `--audit-root` | Directory containing MAP-27H audit JSON |
| `--output-root` | Output directory (must contain `.local`) |

MAP-27H audit file: `map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json`

## Outputs (8 files)

| File | Description |
|------|-------------|
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.json` | Full result JSON |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.md` | Markdown report |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.csv` | Checks CSV |
| `map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.summary.txt` | Summary text |
| `map_00.locked_replay_dry_run_material_counts.csv` | Material bucket counts |
| `map_00.locked_replay_dry_run_source_manifest.json` | Locked file re-hash manifest |
| `map_00.locked_replay_dry_run_replay_digest.json` | Locked replay digest JSON |
| `map_00.locked_replay_dry_run_forbidden_output_guard.json` | Forbidden output guard |

## Locked replay digest formula

```
MAP27I_LOCKED_REPLAY_DRY_RUN_V1
|SOURCE_REPLAY_LOCK_ID:<id>
|AUDIT_SHA256:<sha>
|<ROLE>:<rehash>  (x8, in file_order)
|CELLS_CSV_SHA256:<sha>
|CELL_COUNT:<n>
|WALL:<n>|FLOOR:<n>|ACCESS:<n>|LOT:<n>|RESIDUAL:<n>
|MATERIAL_KINDS:<n>|LAYER_KINDS:<n>
```

SHA-256(UTF-8(above string)).ToLower()

## Checks (44)

44 checks run in deterministic order. All must pass for `is_valid=true` and
`dry_run_status=LOCKED_REPLAY_DRY_RUN_COMPLETE`.

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

## CLI command

```
deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-materialization-replay-dry-run
  --audit-root <path>
  --output-root <path>
  --output-json <path>
  --output-md <path>
  --output-csv <path>
  --summary <path>
  --output-material-counts-csv <path>
  --output-source-manifest-json <path>
  --output-replay-digest-json <path>
  --output-forbidden-guard-json <path>
```

## Helper script

```
examples/deadmtl-layer-pack/scripts/run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-materialization-replay-dry-run.ps1
```

## Next allowed experiment

`MAP-27J_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN` (`SANDBOX_ONLY_NOT_RUNTIME`)
