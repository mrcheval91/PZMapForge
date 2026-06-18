# MAP-26I WorldBuilder Minimal Concrete Geometry Writer Adapter Contract

## Purpose

MAP-26I normalizes the 8 dry-run records emitted by MAP-26G (and verified by MAP-26H)
into a single adapter contract document.

It answers: Can these dry-run planning records be formally presented as a stable, bounded
adapter contract with explicit claim boundaries, forbidden output families, and full
normalization of all component/lot/slot/access records?

This is NOT a real Project Zomboid writer and NOT runtime proof.

## What This Is Not

- Does NOT write `.lotpack`, `.lotheader`, or `WorldGenOverride.lua`.
- Does NOT call `compile-worldgen`.
- Does NOT install anything into Project Zomboid.
- Does NOT claim runtime proof, writer readiness, or public playable packaging.
- Does NOT mutate MAP-26A/B/C/D/E/F/G/H outputs.

## Inputs

| Argument | Description |
|----------|-------------|
| `--audit-receipt` | Path to MAP-26H audit receipt JSON |
| `--component-record` | Path to `map_00.component_writer_record.json` (MAP-26G output) |
| `--lot-records` | Path to `map_00.lot_writer_records.json` (MAP-26G output) |
| `--building-slot-records` | Path to `map_00.building_slot_writer_records.json` (MAP-26G output) |
| `--frontage-access` | Path to `map_00.frontage_access_record.json` (MAP-26G output) |
| `--rear-service-access` | Path to `map_00.rear_service_access_record.json` (MAP-26G output) |
| `--claim-boundary` | Path to `map_00.claim_boundary_record.json` (MAP-26G output) |
| `--output-root` | `.local` directory for MAP-26I outputs |
| `--output-json` | Path for the adapter contract JSON |
| `--output-md` | Path for the adapter contract Markdown |
| `--output-csv` | Path for the adapter contract CSV |
| `--summary` | Path for the adapter contract summary text |

All output paths must contain `.local`.

## Outputs (4 files, all under .local)

- `map_00.minimal_concrete_geometry_writer_adapter_contract.json`
- `map_00.minimal_concrete_geometry_writer_adapter_contract.md`
- `map_00.minimal_concrete_geometry_writer_adapter_contract.csv`
- `map_00.minimal_concrete_geometry_writer_adapter_contract.summary.txt`

## Normalized Records

All normalized records have `writer_consumable: false` and `runtime_consumable: false`.

- `normalized_component` — 1 component (map_00_component_0001)
- `normalized_lots[]` — 7 lots
- `normalized_building_slots[]` — 7 building slots
- `normalized_access_records[]` — 2 records (FRONTAGE + REAR_SERVICE)

## Forbidden Output Families (8)

| # | Family ID | Blocked Pattern |
|---|-----------|-----------------|
| 1 | DOTLOTPACK_FILES | `*.lotpack` |
| 2 | DOTLOTHEADER_FILES | `*.lotheader` |
| 3 | WORLDGENOVERRIDE_LUA | `WorldGenOverride.lua` |
| 4 | RUNTIME_LUA_SCRIPTS | `*.lua` |
| 5 | COMPILE_WORLDGEN | `compile-worldgen` |
| 6 | PZ_INSTALLATION_PATHS | `*/Project Zomboid/*` |
| 7 | MEDIA_MAPS_DIRECTORY | `*/media/maps/*` |
| 8 | LOT_BIN_EXPORT | `*.bin` |

## Checks (28)

| # | Check ID | Can Fail? |
|---|----------|-----------|
| 1 | MAP26H_AUDIT_RECEIPT_EXISTS | No (past guard) |
| 2 | MAP26H_AUDIT_RECEIPT_HASHED | No (past guard) |
| 3 | MAP26H_VERDICT_COMPLETE | Yes |
| 4 | MAP26H_IS_VALID_TRUE | Yes |
| 5 | MAP26H_AUDITED_FILE_COUNT_12 | Yes |
| 6 | MAP26H_HASH_MISMATCH_COUNT_0 | Yes |
| 7 | MAP26H_PASSED_AUDIT_CHECKS_29 | Yes |
| 8 | COMPONENT_RECORD_EXISTS | No (past guard) |
| 9 | LOT_RECORDS_EXISTS | No (past guard) |
| 10 | BUILDING_SLOT_RECORDS_EXISTS | No (past guard) |
| 11 | FRONTAGE_ACCESS_RECORD_EXISTS | No (past guard) |
| 12 | REAR_SERVICE_ACCESS_RECORD_EXISTS | No (past guard) |
| 13 | CLAIM_BOUNDARY_RECORD_EXISTS | No (past guard) |
| 14 | NORMALIZED_COMPONENT_STABLE | Yes |
| 15 | NORMALIZED_LOT_COUNT_7 | Yes |
| 16 | NORMALIZED_BUILDING_SLOT_COUNT_7 | Yes |
| 17 | NORMALIZED_ACCESS_RECORD_COUNT_2 | Yes |
| 18 | FRONTAGE_ACCESS_STABLE | Yes |
| 19 | REAR_SERVICE_ACCESS_STABLE | Yes |
| 20 | FORBIDDEN_OUTPUT_FAMILIES_LISTED | Yes |
| 21 | WRITER_READY_FALSE | Yes |
| 22 | RUNTIME_VALID_FALSE | Yes |
| 23 | MATERIALIZED_FALSE | Yes |
| 24 | APPROVED_FOR_WRITER_EXPERIMENT_FALSE | Yes |
| 25 | GATE_STATUS_LOCKED | Yes |
| 26 | NO_WRITER_CONSUMABLE_RECORDS | Yes |
| 27 | NO_RUNTIME_CONSUMABLE_RECORDS | Yes |
| 28 | NO_FORBIDDEN_OUTPUTS_CREATED | No (always pass) |

## Verdicts

- `MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_COMPLETE`
- `MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID`

## CLI Usage

```powershell
dotnet run --project src/PZMapForge.Cli/PZMapForge.Cli.csproj -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-adapter-contract `
    --audit-receipt         .local\...\audit_receipt.json `
    --component-record      .local\...\map_00.component_writer_record.json `
    --lot-records           .local\...\map_00.lot_writer_records.json `
    --building-slot-records .local\...\map_00.building_slot_writer_records.json `
    --frontage-access       .local\...\map_00.frontage_access_record.json `
    --rear-service-access   .local\...\map_00.rear_service_access_record.json `
    --claim-boundary        .local\...\map_00.claim_boundary_record.json `
    --output-root           .local\...\adapter-contract\map_00 `
    --output-json           .local\...\adapter_contract.json `
    --output-md             .local\...\adapter_contract.md `
    --output-csv            .local\...\adapter_contract.csv `
    --summary               .local\...\adapter_contract.summary.txt
```

## Helper Script

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-adapter-contract.ps1
```

Requires MAP-26G (emitter) and MAP-26H (audit receipt) to have been run first.
