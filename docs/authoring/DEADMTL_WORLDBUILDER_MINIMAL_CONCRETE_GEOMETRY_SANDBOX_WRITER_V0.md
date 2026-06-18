# MAP-27A WorldBuilder Minimal Concrete Geometry Sandbox Writer V0

## Purpose

MAP-27A is the first writer seam for DeadMTL WorldBuilder minimal concrete geometry.

It reads the canonical MAP-26I/MAP-26J adapter contract and emits concrete writer-operation
artifacts under `.local`.

It writes sandbox writer operation artifacts only.
It does not write PZ runtime files.
It does not prove runtime validity.

## What This Is Not

- Does NOT write `.lotpack`, `.lotheader`, or `WorldGenOverride.lua`.
- Does NOT write runtime Lua.
- Does NOT call `compile-worldgen`.
- Does NOT write to `media/maps`.
- Does NOT install anything into Project Zomboid.
- Does NOT write to `steamapps/workshop`.
- Does NOT claim runtime proof.
- Does NOT claim writer_ready.
- Does NOT claim public playable packaging.
- Does NOT mutate `map_00.png` or any MAP-26A/B/C/D/E/F/G/H/I/J outputs.

## Inputs

| Argument | Description |
|----------|-------------|
| `--adapter-contract` | Path to MAP-26I/MAP-26J adapter contract JSON |
| `--output-root` | `.local` directory for operation files and main outputs |
| `--output-json` | Path for the sandbox writer result JSON |
| `--output-md` | Path for the sandbox writer result Markdown |
| `--output-csv` | Path for the sandbox writer checks CSV |
| `--summary` | Path for the sandbox writer summary text |

All output paths must contain `.local`.

## Main Output Files (4)

```
map_00.minimal_concrete_geometry_sandbox_writer_v0.json
map_00.minimal_concrete_geometry_sandbox_writer_v0.md
map_00.minimal_concrete_geometry_sandbox_writer_v0.csv
map_00.minimal_concrete_geometry_sandbox_writer_v0.summary.txt
```

## Emitted Sandbox Writer Operation Files (5)

All written to the output root under `.local`:

```
map_00.sandbox_writer_component_operations.json
map_00.sandbox_writer_lot_operations.json
map_00.sandbox_writer_building_slot_operations.json
map_00.sandbox_writer_access_operations.json
map_00.sandbox_writer_forbidden_output_guard.json
```

These are sandbox writer artifacts only. They are not PZ runtime files.

## Writer Operations

| Kind | Count | Source |
|------|-------|--------|
| COMPONENT_ENVELOPE_WRITE | 1 | normalized_component |
| LOT_BOUNDARY_WRITE | 7 | normalized_lots[] |
| BUILDING_FOOTPRINT_WRITE | 7 | normalized_building_slots[] |
| ACCESS_LINK_WRITE | 2 | normalized_access_records[] |

Total: 17 operations. All have `runtime_effect: NONE`.

## Forbidden Output Guard

8 forbidden output families verified as NOT_EMITTED:

| Family ID | Blocked Pattern |
|-----------|-----------------|
| `LOT_PACK_RUNTIME_BINARY` | `*.lotpack` |
| `LOT_HEADER_RUNTIME_BINARY` | `*.lotheader` |
| `WORLDGEN_OVERRIDE_LUA` | `WorldGenOverride.lua` |
| `RUNTIME_LUA` | `*.lua` |
| `PROJECT_ZOMBOID_INSTALL_PATH` | `*/Project Zomboid/*` |
| `STEAM_WORKSHOP_OUTPUT` | `*/steamapps/workshop/*` |
| `COMPILE_WORLDGEN_INVOCATION` | `compile-worldgen` |
| `MAP_00_PNG_MUTATION` | `map_00.png` |

## Checks (33)

| # | Check ID | Can Fail? |
|---|----------|-----------|
| 1 | ADAPTER_CONTRACT_EXISTS | No (past guard) |
| 2 | ADAPTER_CONTRACT_HASHED | No (past guard) |
| 3 | ADAPTER_CONTRACT_VERDICT_COMPLETE | Yes |
| 4 | ADAPTER_CONTRACT_IS_VALID_TRUE | Yes |
| 5 | ADAPTER_CONTRACT_STATUS_NORMALIZED_DRY_RUN_RECORDS_ONLY | Yes |
| 6 | ADAPTER_CONTRACT_CANONICAL_COMPONENT_SOURCE_RECORD | Yes |
| 7 | ADAPTER_CONTRACT_CANONICAL_ACCESS_KINDS | Yes |
| 8 | ADAPTER_CONTRACT_FORBIDDEN_FAMILIES_8_FORBIDDEN | Yes |
| 9 | WRITER_STAGE_SANDBOX_WRITER_V0 | No |
| 10 | SANDBOX_ONLY_TRUE | No |
| 11 | COMPONENT_OPERATION_COUNT_1 | Yes |
| 12 | LOT_OPERATION_COUNT_7 | Yes |
| 13 | BUILDING_SLOT_OPERATION_COUNT_7 | Yes |
| 14 | ACCESS_OPERATION_COUNT_2 | Yes |
| 15 | TOTAL_OPERATION_COUNT_17 | Yes |
| 16 | OPERATION_FILE_COUNT_5 | No |
| 17 | ALL_OPERATION_FILES_WRITTEN | No |
| 18 | ALL_OPERATION_FILES_HASHED | No |
| 19 | ALL_OPERATIONS_RUNTIME_EFFECT_NONE | Yes |
| 20 | FORBIDDEN_OUTPUT_GUARD_WRITTEN | No |
| 21 | NO_LOTPACK_WRITTEN | No |
| 22 | NO_LOTHEADER_WRITTEN | No |
| 23 | NO_WORLDGENOVERRIDE_WRITTEN | No |
| 24 | NO_RUNTIME_LUA_WRITTEN | No |
| 25 | NO_COMPILE_WORLDGEN_CALLED | No |
| 26 | NO_PZ_INSTALL_PATH_WRITTEN | No |
| 27 | WRITER_READY_FALSE | Yes |
| 28 | RUNTIME_VALID_FALSE | Yes |
| 29 | MATERIALIZED_FALSE | Yes |
| 30 | APPROVED_FOR_WRITER_EXPERIMENT_FALSE | Yes |
| 31 | GATE_STATUS_LOCKED | Yes |
| 32 | NO_RUNTIME_PROOF_CLAIMED | No |
| 33 | NO_PUBLIC_PLAYABLE_PACKAGING_CLAIMED | No |

## Verdicts

- `MAP27A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_V0_COMPLETE`
- `MAP27A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_V0_INVALID`

## Claim Boundary

```
writer_stage: SANDBOX_WRITER_V0
writer_mode: WRITE_SANDBOX_OPERATION_ARTIFACTS_ONLY
sandbox_only: true
writer_ready: false
runtime_valid: false
materialized: false
approved_for_writer_experiment: false
runtime_proof_claimed: false
public_playable_packaging_claimed: false
gate: LOCKED_PENDING_OPERATOR_APPROVAL
```

## CLI Usage

```powershell
dotnet run --project src/PZMapForge.Cli/PZMapForge.Cli.csproj -- `
    deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0 `
    --adapter-contract .local\...\map_00.minimal_concrete_geometry_writer_adapter_contract.json `
    --output-root      .local\...\sandbox-writer-v0\map_00 `
    --output-json      .local\...\map_00.minimal_concrete_geometry_sandbox_writer_v0.json `
    --output-md        .local\...\map_00.minimal_concrete_geometry_sandbox_writer_v0.md `
    --output-csv       .local\...\map_00.minimal_concrete_geometry_sandbox_writer_v0.csv `
    --summary          .local\...\map_00.minimal_concrete_geometry_sandbox_writer_v0.summary.txt
```

## Helper Script

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0.ps1
```

Requires MAP-26I/MAP-26J (adapter contract) to have been run first.
