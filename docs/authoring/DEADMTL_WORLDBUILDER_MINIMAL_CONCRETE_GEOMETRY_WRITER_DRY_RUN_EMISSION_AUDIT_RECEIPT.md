# MAP-26H WorldBuilder Minimal Concrete Geometry Writer Dry-Run Emission Audit Receipt

## Purpose

MAP-26H independently verifies the 8 dry-run records emitted by MAP-26G after emission.

It answers: Did MAP-26G emit exactly the expected records under `.local`, with stable hashes,
correct claim boundaries, and no forbidden runtime artifacts?

This is NOT a real Project Zomboid writer and NOT runtime proof.

## What This Is Not

- Does NOT write `.lotpack`, `.lotheader`, or `WorldGenOverride.lua`.
- Does NOT call `compile-worldgen`.
- Does NOT install anything into Project Zomboid.
- Does NOT claim runtime proof, writer readiness, or public playable packaging.
- Does NOT mutate MAP-26G outputs.

## Inputs

| Argument | Description |
|----------|-------------|
| `--emitter-result` | Path to `map_00.minimal_concrete_geometry_writer_dry_run_emitter.json` (MAP-26G output) |
| `--emitter-output-root` | Directory containing all MAP-26G outputs (4 main + 8 emitted records) |
| `--output-root` | `.local` directory for MAP-26H outputs |
| `--output-json` | Path for the audit receipt JSON |
| `--output-md` | Path for the audit receipt Markdown |
| `--output-csv` | Path for the audit receipt CSV |
| `--summary` | Path for the audit receipt summary text |

All paths must contain `.local`.

## Outputs (4 files, all under .local)

- `map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.json`
- `map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.md`
- `map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.csv`
- `map_00.minimal_concrete_geometry_writer_dry_run_emission_audit_receipt.summary.txt`

## Audited Files (12 total)

4 main MAP-26G output files:
1. `map_00.minimal_concrete_geometry_writer_dry_run_emitter.json`
2. `map_00.minimal_concrete_geometry_writer_dry_run_emitter.md`
3. `map_00.minimal_concrete_geometry_writer_dry_run_emitter.csv`
4. `map_00.minimal_concrete_geometry_writer_dry_run_emitter.summary.txt`

8 emitted record files:
5. `map_00.component_writer_record.json`
6. `map_00.lot_writer_records.json`
7. `map_00.building_slot_writer_records.json`
8. `map_00.frontage_access_record.json`
9. `map_00.rear_service_access_record.json`
10. `map_00.forbidden_output_scan.json`
11. `map_00.rollback_record.json`
12. `map_00.claim_boundary_record.json`

For the 8 emitted records, the actual on-disk SHA-256 is compared against the SHA-256
recorded in `emitted_records[]` inside the MAP-26G emitter result JSON.

## Checks (29)

| # | Check ID | Can Fail? |
|---|----------|-----------|
| 1 | MAP26G_EMITTER_RESULT_EXISTS | No (past guard) |
| 2 | MAP26G_EMITTER_RESULT_HASHED | No (past guard) |
| 3 | MAP26G_VERDICT_COMPLETE | Yes |
| 4 | MAP26G_IS_VALID_TRUE | Yes |
| 5 | MAP26G_EMITTER_STATUS_DOT_LOCAL_ONLY | Yes |
| 6 | MAP26G_DRY_RUN_ONLY_TRUE | Yes |
| 7 | MAP26G_EMITTED_RECORD_COUNT_8 | Yes |
| 8 | ALL_12_EXPECTED_OUTPUT_FILES_EXIST | Yes |
| 9 | ALL_12_EXPECTED_OUTPUT_FILES_HASHED | Yes |
| 10 | ALL_8_EMITTED_RECORD_HASHES_MATCH | Yes |
| 11 | NO_HASH_MISMATCHES | Yes |
| 12 | COMPONENT_RECORD_TARGET_COMPONENT_STABLE | Yes |
| 13 | COMPONENT_RECORD_INTENT_STABLE | Yes |
| 14 | COMPONENT_RECORD_BBOX_STABLE | Yes |
| 15 | LOT_RECORDS_COUNT_7 | Yes |
| 16 | BUILDING_SLOT_RECORDS_COUNT_7 | Yes |
| 17 | FRONTAGE_RECORD_STABLE | Yes |
| 18 | REAR_SERVICE_RECORD_STABLE | Yes |
| 19 | MAP26G_FORBIDDEN_SCAN_PASS | Yes |
| 20 | POST_EMISSION_FORBIDDEN_SCAN_PASS | Yes |
| 21 | ROLLBACK_RECORD_PASS | Yes |
| 22 | CLAIM_BOUNDARY_RECORD_PASS | Yes |
| 23 | WRITER_READY_FALSE | Yes |
| 24 | RUNTIME_VALID_FALSE | Yes |
| 25 | MATERIALIZED_FALSE | Yes |
| 26 | APPROVED_FOR_WRITER_EXPERIMENT_FALSE | Yes |
| 27 | GATE_STATUS_LOCKED | Yes |
| 28 | NO_RUNTIME_PROOF_CLAIMED | Yes |
| 29 | NO_FORBIDDEN_OUTPUTS_CREATED | No (always pass) |

## Verdicts

- `MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_COMPLETE`
- `MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_INVALID`

## CLI Usage

```powershell
dotnet run --project src/PZMapForge.Cli/PZMapForge.Cli.csproj -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt `
    --emitter-result      .local\...\map_00.minimal_concrete_geometry_writer_dry_run_emitter.json `
    --emitter-output-root .local\...\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00 `
    --output-root         .local\...\worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt\map_00 `
    --output-json         .local\...\receipt.json `
    --output-md           .local\...\receipt.md `
    --output-csv          .local\...\receipt.csv `
    --summary             .local\...\receipt.summary.txt
```

## Helper Script

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt.ps1
```

Requires MAP-26G to have been run first.
