# MAP-26F WorldBuilder Minimal Concrete Geometry Writer Dry-Run Design

## Purpose

MAP-26F reads the MAP-26E writer experiment scope record and produces a deterministic
dry-run writer design record. It answers:

```
What would a future writer attempt to emit, where would it be sandboxed,
what schema would it use, and what guards would prevent runtime mutation?
```

MAP-26F does NOT emit any writer outputs. It only designs what MAP-26G would do.
Authorization requires a separate operator decision and a separate task.

## Input files

| File | Source |
|------|--------|
| `map_00.minimal_concrete_geometry_writer_experiment_scope_record.json` | MAP-26E output |
| `map_00.minimal_concrete_geometry_writer_input_manifest.json` | MAP-26D output |
| `map_00.minimal_concrete_geometry_mvp.json` | MAP-26A output |

## Output files

| File | Description |
|------|-------------|
| `map_00.minimal_concrete_geometry_writer_dry_run_design.json` | Full dry-run design with planned records, constraints, guards, rollback checks, SHA-256 of inputs |
| `map_00.minimal_concrete_geometry_writer_dry_run_design.md` | Human-readable design with writer gate and claim boundary |
| `map_00.minimal_concrete_geometry_writer_dry_run_design.csv` | One row per design check |
| `map_00.minimal_concrete_geometry_writer_dry_run_design.summary.txt` | Key/value summary for quick inspection |

## Writer gate

```
dry_run_only                   : true
approved_for_writer_experiment : false
writer_experiment_gate_status  : LOCKED_PENDING_OPERATOR_APPROVAL
future_writer_name             : MAP-26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER
future_writer_status           : DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME
```

MAP-26F does NOT authorize MAP-26G. Unlocking requires a separate operator decision.

## Planned output records

All records have status `DESIGN_ONLY_NOT_EMITTED`:

| # | Record ID | Kind |
|---|-----------|------|
| 1 | `COMPONENT_WRITER_RECORD` | COMPONENT_RECORD |
| 2 | `LOT_WRITER_RECORDS` | LOT_RECORDS |
| 3 | `BUILDING_SLOT_WRITER_RECORDS` | BUILDING_SLOT_RECORDS |
| 4 | `FRONTAGE_ACCESS_RECORD` | ACCESS_RECORD |
| 5 | `REAR_SERVICE_ACCESS_RECORD` | ACCESS_RECORD |
| 6 | `FORBIDDEN_OUTPUT_SCAN_RECORD` | SCAN_RECORD |
| 7 | `ROLLBACK_RECORD` | ROLLBACK_RECORD |
| 8 | `CLAIM_BOUNDARY_RECORD` | CLAIM_BOUNDARY_RECORD |

## Sandbox constraints

| # | Constraint ID | Details |
|---|---------------|---------|
| 1 | `DOT_LOCAL_ONLY` | All outputs to .local only |
| 2 | `NO_PZ_INSTALL_PATHS` | No Project Zomboid install paths |
| 3 | `NO_RUNTIME_EXTENSION_OUTPUTS` | No .lotpack, .lotheader, .lua |
| 4 | `NO_SOURCE_ARTIFACT_MUTATION` | MAP-26A/B/C/D/E outputs are read-only |
| 5 | `NO_COMPILE_COMMANDS` | compile-worldgen must not be called |
| 6 | `NO_WORLDGEN_OVERRIDE` | WorldGenOverride.lua must not be written |

## Forbidden output guards

| # | Guard ID | Blocked Pattern |
|---|----------|-----------------|
| 1 | `BLOCK_LOTPACK` | `*.lotpack` |
| 2 | `BLOCK_LOTHEADER` | `*.lotheader` |
| 3 | `BLOCK_WORLDGENOVERRIDE_LUA` | `WorldGenOverride.lua` |
| 4 | `BLOCK_COMPILE_WORLDGEN` | `compile-worldgen` |
| 5 | `BLOCK_PROJECT_ZOMBOID_INSTALL_PATH` | `ProjectZomboid` |
| 6 | `BLOCK_MAP_00_PNG_MUTATION` | `map_00.png (write)` |
| 7 | `BLOCK_RUNTIME_PROOF_CLAIM` | `runtime_valid=true` |
| 8 | `BLOCK_WRITER_READY_CLAIM` | `writer_ready=true` |

## Rollback checks

| # | Check ID | Details |
|---|----------|---------|
| 1 | `GIT_STATUS_TASK_FILES_ONLY` | git status shows only expected task files |
| 2 | `NO_FORBIDDEN_ARTIFACTS_FOUND` | scan confirms zero .lotpack/.lotheader/WorldGenOverride.lua |
| 3 | `DOT_LOCAL_OUTPUTS_ONLY` | all emitter outputs in .local |
| 4 | `SOURCE_HASHES_UNCHANGED` | SHA-256 of MAP-26A/D/E inputs unchanged |
| 5 | `NO_PZ_INSTALL_MUTATION` | PZ install directory unmodified |

## Design checks (21 total)

| # | Check ID | What it verifies |
|---|----------|-----------------|
| 1 | `MAP26E_SCOPE_RECORD_EXISTS` | MAP-26E scope record file exists on disk |
| 2 | `MAP26E_SCOPE_RECORD_HASHED` | MAP-26E scope record SHA-256 computed |
| 3 | `MAP26E_VERDICT_COMPLETE` | MAP-26E verdict is complete |
| 4 | `MAP26E_IS_VALID_TRUE` | MAP-26E is_valid is true |
| 5 | `MAP26E_FUTURE_STATUS_NOT_AUTHORIZED` | MAP-26E future_experiment_status is NOT_AUTHORIZED |
| 6 | `MAP26E_GATE_LOCKED` | MAP-26E writer_experiment_gate_status is LOCKED_PENDING_OPERATOR_APPROVAL |
| 7 | `MAP26D_MANIFEST_EXISTS` | MAP-26D manifest file exists on disk |
| 8 | `MAP26D_MANIFEST_HASHED` | MAP-26D manifest SHA-256 computed |
| 9 | `MAP26A_GEOMETRY_MVP_EXISTS` | MAP-26A geometry MVP file exists on disk |
| 10 | `MAP26A_GEOMETRY_MVP_HASHED` | MAP-26A geometry MVP SHA-256 computed |
| 11 | `DRY_RUN_ONLY_TRUE` | dry_run_only is true |
| 12 | `WRITER_READY_FALSE` | writer_ready is false |
| 13 | `RUNTIME_VALID_FALSE` | runtime_valid is false |
| 14 | `MATERIALIZED_FALSE` | materialized is false |
| 15 | `APPROVED_FOR_WRITER_EXPERIMENT_FALSE` | approved_for_writer_experiment is false |
| 16 | `SANDBOX_OUTPUT_ROOT_IS_DOT_LOCAL` | sandbox_output_root is under .local |
| 17 | `PLANNED_OUTPUT_RECORDS_LISTED` | planned_output_records list is non-empty |
| 18 | `SANDBOX_CONSTRAINTS_LISTED` | sandbox_constraints list is non-empty |
| 19 | `FORBIDDEN_OUTPUT_GUARDS_LISTED` | forbidden_output_guards list is non-empty |
| 20 | `ROLLBACK_CHECKS_LISTED` | rollback_checks list is non-empty |
| 21 | `NO_FORBIDDEN_OUTPUTS_CREATED` | No forbidden outputs created |

## What this proves

- MAP-26E scope record exists, is SHA-256 hashed, and has a complete verdict.
- MAP-26E is_valid=true, future_experiment_status=NOT_AUTHORIZED, gate=LOCKED_PENDING_OPERATOR_APPROVAL.
- MAP-26D manifest and MAP-26A geometry MVP both exist and are SHA-256 hashed.
- dry_run_only=true, writer_ready=false, runtime_valid=false, materialized=false.
- approved_for_writer_experiment=false.
- Planned output records, sandbox constraints, forbidden output guards, and rollback checks are documented.

## What this does NOT prove

- Runtime validity in Project Zomboid.
- Writer readiness for PZ map compilation.
- Authorization to run MAP-26G or any writer emitter.
- Public playable packaging.
- Correct in-game tile placement.

## Claim boundary

```
writer_ready                   : false
runtime_valid                  : false
materialized                   : false
approved_for_writer_experiment : false
writer_experiment_gate_status  : LOCKED_PENDING_OPERATOR_APPROVAL
future_writer_status           : DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME
```

This dry-run design does not create geometry, write PZ runtime files, write lotpack,
write lotheader, write WorldGenOverride.lua, call compile-worldgen, or install
anything into Project Zomboid.

## Running the helper script

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-design.ps1
```

The script resolves the repo root, uses real MAP-26E, MAP-26D, and MAP-26A .local outputs,
and writes the dry-run design to:

```
.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-design\map_00\
```

## Expected verdict

```
MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE
```
