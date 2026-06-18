# MAP-26G WorldBuilder Minimal Concrete Geometry Writer Dry-Run Emitter

## Purpose

MAP-26G reads the MAP-26F dry-run writer design and the MAP-26A geometry MVP,
then emits deterministic dry-run records under `.local` only. It answers:

```
If the future writer were run as a sandboxed dry-run, what non-runtime records
would it emit from the MAP-26A geometry?
```

MAP-26G does NOT write PZ runtime files, `.lotpack`, `.lotheader`,
`WorldGenOverride.lua`, or call `compile-worldgen`.

## Input files

| File | Source |
|------|--------|
| `map_00.minimal_concrete_geometry_writer_dry_run_design.json` | MAP-26F output |
| `map_00.minimal_concrete_geometry_mvp.json` | MAP-26A output |

## Output directory

```
.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00\
```

## Main output files

| File | Description |
|------|-------------|
| `map_00.minimal_concrete_geometry_writer_dry_run_emitter.json` | Full emitter result with checks, emitted record descriptors, forbidden scan, rollback record, claim boundary |
| `map_00.minimal_concrete_geometry_writer_dry_run_emitter.md` | Human-readable emitter result with writer gate and claim boundary |
| `map_00.minimal_concrete_geometry_writer_dry_run_emitter.csv` | One row per check |
| `map_00.minimal_concrete_geometry_writer_dry_run_emitter.summary.txt` | Key/value summary for quick inspection |

## Dry-run emitted record files (8 total)

| File | Record Kind |
|------|-------------|
| `map_00.component_writer_record.json` | COMPONENT_WRITER_RECORD |
| `map_00.lot_writer_records.json` | LOT_WRITER_RECORDS |
| `map_00.building_slot_writer_records.json` | BUILDING_SLOT_WRITER_RECORDS |
| `map_00.frontage_access_record.json` | FRONTAGE_ACCESS_RECORD |
| `map_00.rear_service_access_record.json` | REAR_SERVICE_ACCESS_RECORD |
| `map_00.forbidden_output_scan.json` | SCAN_RECORD |
| `map_00.rollback_record.json` | ROLLBACK_RECORD |
| `map_00.claim_boundary_record.json` | CLAIM_BOUNDARY_RECORD |

All dry-run records have status `DRY_RUN_RECORD_EMITTED`.

## Writer gate

```
dry_run_only                   : true
approved_for_writer_experiment : false
writer_experiment_gate_status  : LOCKED_PENDING_OPERATOR_APPROVAL
future_writer_name             : MAP-26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER
emitter_status                 : DRY_RUN_RECORDS_EMITTED_TO_DOT_LOCAL_ONLY
```

MAP-26G does not authorize any runtime writer. It emits .json dry-run records to `.local` only.

## Checks (25 total)

| # | Check ID | What it verifies |
|---|----------|-----------------|
| 1 | `MAP26F_DRY_RUN_DESIGN_EXISTS` | MAP-26F dry-run design file exists on disk |
| 2 | `MAP26F_DRY_RUN_DESIGN_HASHED` | MAP-26F dry-run design SHA-256 computed |
| 3 | `MAP26F_VERDICT_COMPLETE` | MAP-26F verdict is complete |
| 4 | `MAP26F_DRY_RUN_ONLY_TRUE` | MAP-26F dry_run_only is true |
| 5 | `MAP26F_FUTURE_WRITER_STATUS_DESIGN_ONLY` | MAP-26F future_writer_status is DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME |
| 6 | `MAP26A_GEOMETRY_MVP_EXISTS` | MAP-26A geometry MVP file exists on disk |
| 7 | `MAP26A_GEOMETRY_MVP_HASHED` | MAP-26A geometry MVP SHA-256 computed |
| 8 | `TARGET_COMPONENT_STABLE` | Target component ID matches expected value |
| 9 | `COMPONENT_BBOX_STABLE` | Component bbox matches expected MAP-26A values |
| 10 | `LOT_COUNT_7` | Lot count is 7 |
| 11 | `BUILDING_SLOT_COUNT_7` | Building slot count is 7 |
| 12 | `FRONTAGE_ACCESS_STABLE` | Frontage access matches expected (NORTH, map_00_component_0023, 89px) |
| 13 | `REAR_SERVICE_ACCESS_STABLE` | Rear service access matches expected (EAST, map_00_component_0030, 60px) |
| 14 | `EMITTED_RECORD_COUNT_8` | Emitted record count is 8 |
| 15 | `ALL_EMITTED_RECORDS_HASHED` | All emitted records have SHA-256 computed |
| 16 | `FORBIDDEN_OUTPUT_SCAN_PASS` | Forbidden output scan passed |
| 17 | `ROLLBACK_RECORD_PASS` | Rollback record: dot_local_outputs_only=true, source_hashes_unchanged=true, runtime_files_created=false |
| 18 | `CLAIM_BOUNDARY_RECORD_PASS` | Claim boundary record: writer_ready=false, runtime_valid=false, materialized=false |
| 19 | `DRY_RUN_ONLY_TRUE` | dry_run_only is true |
| 20 | `WRITER_READY_FALSE` | writer_ready is false |
| 21 | `RUNTIME_VALID_FALSE` | runtime_valid is false |
| 22 | `MATERIALIZED_FALSE` | materialized is false |
| 23 | `APPROVED_FOR_WRITER_EXPERIMENT_FALSE` | approved_for_writer_experiment is false |
| 24 | `GATE_STATUS_LOCKED` | writer_experiment_gate_status is LOCKED_PENDING_OPERATOR_APPROVAL |
| 25 | `NO_FORBIDDEN_OUTPUTS_CREATED` | No forbidden outputs created |

## What this proves

- MAP-26F design exists, is SHA-256 hashed, and has a complete verdict.
- MAP-26F dry_run_only=true and future_writer_status=DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME.
- MAP-26A geometry MVP exists and is SHA-256 hashed.
- Target component, bbox, lot count (7), building slot count (7), frontage, and rear service access are all stable.
- 8 dry-run records emitted to `.local` only, all SHA-256 hashed.
- Forbidden output scan passed: no .lotpack, .lotheader, WorldGenOverride.lua, or PZ install paths.
- Rollback and claim boundary records are correctly set.

## What this does NOT prove

- Runtime validity in Project Zomboid.
- Writer readiness for PZ map compilation.
- Authorization to run any runtime writer.
- Public playable packaging.
- Correct in-game tile placement.

## Claim boundary

```
writer_ready                   : false
runtime_valid                  : false
materialized                   : false
approved_for_writer_experiment : false
writer_experiment_gate_status  : LOCKED_PENDING_OPERATOR_APPROVAL
emitter_status                 : DRY_RUN_RECORDS_EMITTED_TO_DOT_LOCAL_ONLY
```

MAP-26G emits dry-run records only. It does not create PZ runtime geometry, write lotpack,
write lotheader, write WorldGenOverride.lua, call compile-worldgen, or install
anything into Project Zomboid.

## Running the helper script

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter.ps1
```

The script resolves the repo root, uses real MAP-26F and MAP-26A `.local` outputs,
and writes the dry-run emitter outputs (4 main + 8 record files) to:

```
.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00\
```

## Expected verdict

```
MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_COMPLETE
```
