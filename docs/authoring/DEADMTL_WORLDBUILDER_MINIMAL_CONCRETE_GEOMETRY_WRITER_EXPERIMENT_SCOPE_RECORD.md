# MAP-26E WorldBuilder Minimal Concrete Geometry Writer Experiment Scope Record

## Purpose

MAP-26E reads the MAP-26D writer input manifest and produces a deterministic writer experiment
scope / approval record. It answers:

```
What exactly would a future writer experiment be allowed to attempt,
and what is still forbidden?
```

MAP-26E does NOT authorize the future writer experiment. It only documents what that experiment
would need to be. Authorization requires a separate operator decision and a separate task.

## Input files

| File | Source |
|------|--------|
| `map_00.minimal_concrete_geometry_writer_input_manifest.json` | MAP-26D output |
| `map_00.minimal_concrete_geometry_writer_input_manifest.summary.txt` | MAP-26D output |

## Output files

| File | Description |
|------|-------------|
| `map_00.minimal_concrete_geometry_writer_experiment_scope_record.json` | Full scope record with allowed/forbidden actions, preconditions, rollback, risk register, SHA-256 of MAP-26D manifest, checks |
| `map_00.minimal_concrete_geometry_writer_experiment_scope_record.md` | Human-readable scope record with writer gate and claim boundary |
| `map_00.minimal_concrete_geometry_writer_experiment_scope_record.csv` | One row per scope check |
| `map_00.minimal_concrete_geometry_writer_experiment_scope_record.summary.txt` | Key/value summary for quick inspection |

## Future writer experiment gate

```
approved_for_writer_experiment : false
writer_experiment_gate_status  : LOCKED_PENDING_OPERATOR_APPROVAL
future_experiment_name         : MAP-26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN
future_experiment_status       : NOT_AUTHORIZED
```

MAP-26E does NOT unlock the gate. MAP-26E does NOT authorize MAP-26F.
Unlocking requires a separate operator decision and a separate task.

## Allowed future actions

All actions are `FUTURE_ALLOWED_ONLY_AFTER_OPERATOR_APPROVAL`:

| Action ID | Description |
|-----------|-------------|
| `READ_MAP26D_MANIFEST` | Read the locked, hash-verified MAP-26D input bundle manifest |
| `READ_HASHED_INPUT_ARTIFACTS` | Read the 8 SHA-256 verified input artifacts referenced in MAP-26D |
| `DESIGN_WRITER_DRY_RUN_SCHEMA` | Design the output schema for a future writer dry-run experiment |
| `DESIGN_OUTPUT_SANDBOX_CONSTRAINTS` | Define which output paths are allowed (.local only) and which are forbidden |
| `DESIGN_ROLLBACK_CHECKS` | Design automated checks that verify rollback state after a writer experiment |
| `DESIGN_NO_INSTALL_GUARDS` | Design forbidden-output guards ensuring no PZ install mutation occurs |

## Forbidden actions

| Action ID | Reason |
|-----------|--------|
| `COMPILE_MAP_00_PNG` | Compiling map_00.png is a runtime operation not authorized at this stage |
| `WRITE_LOTPACK` | Writing lotpack files requires runtime proof that does not exist |
| `WRITE_WORLDGENOVERRIDE_LUA` | Writing WorldGenOverride.lua would install into the PZ runtime |
| `CALL_COMPILE_WORLDGEN` | compile-worldgen is a runtime command; not authorized |
| `INSTALL_INTO_PROJECT_ZOMBOID` | Installing files into PZ game directories is not authorized |
| `CLAIM_RUNTIME_PROOF` | No runtime proof exists |
| `CLAIM_WRITER_READY` | writer_ready remains false |
| `CLAIM_PUBLIC_PLAYABLE_PACKAGING` | materialized=false |
| `MUTATE_SOURCE_INPUTS` | MAP-26A/B/C/D outputs are frozen |
| `UNLOCK_WRITER_EXPERIMENT_GATE` | MAP-26E does not authorize unlocking |

## Required preconditions

| Precondition ID | Status |
|-----------------|--------|
| `MAP26D_MANIFEST_VALID` | REQUIRED |
| `MAP26D_INPUT_ARTIFACTS_HASHED` | REQUIRED |
| `OPERATOR_APPROVAL_REQUIRED` | REQUIRED |
| `FUTURE_TASK_MUST_BE_DRY_RUN_FIRST` | REQUIRED |
| `FUTURE_TASK_MUST_OUTPUT_TO_DOT_LOCAL_ONLY` | REQUIRED |
| `FUTURE_TASK_MUST_HAVE_FORBIDDEN_OUTPUT_GUARDS` | REQUIRED |
| `FUTURE_TASK_MUST_HAVE_ROLLBACK_PLAN` | REQUIRED |

## Rollback requirements

| Requirement ID | Details |
|----------------|---------|
| `NO_TRACKED_RUNTIME_FILES` | No PZ runtime files may be committed as a result of a writer experiment |
| `NO_PZ_INSTALL_MUTATION` | The PZ installation directory must not be modified |
| `DOT_LOCAL_OUTPUTS_ONLY` | All writer experiment outputs must be in .local directories |
| `GIT_STATUS_MUST_IDENTIFY_ONLY_TASK_FILES` | git status must show only expected task source files |
| `FORBIDDEN_ARTIFACT_SCAN_REQUIRED` | An automated scan must confirm no .lotpack or WorldGenOverride.lua outside .local |

## Risk register

| Risk ID | Level | Mitigation |
|---------|-------|------------|
| `WRITER_OUTPUT_CONFUSED_WITH_RUNTIME_PROOF` | HIGH | Always display claim boundary fields in all outputs |
| `ACCIDENTAL_LOTPACK_CREATION` | HIGH | Forbidden output guard must scan for .lotpack and .lotheader |
| `ACCIDENTAL_WORLDGENOVERRIDE_CREATION` | HIGH | Forbidden output guard must scan for WorldGenOverride.lua |
| `COMPILE_WORLDGEN_CALLED_BY_HELPER_SCRIPT` | MEDIUM | All future writer helper scripts must be statically verified |
| `SOURCE_ARTIFACT_MUTATION` | MEDIUM | SHA-256 hashes must be re-verified before any writer experiment |
| `APPROVAL_GATE_MISINTERPRETED` | MEDIUM | approved_for_writer_experiment=false must be stated explicitly in all outputs |

## Scope checks

MAP-26E performs 17 deterministic checks:

| # | Check ID | What it verifies |
|---|----------|-----------------|
| 1 | `MAP26D_MANIFEST_EXISTS` | MAP-26D manifest file exists on disk |
| 2 | `MAP26D_MANIFEST_HASHED` | MAP-26D manifest SHA-256 computed |
| 3 | `MAP26D_VERDICT_COMPLETE` | MAP-26D verdict is complete |
| 4 | `MAP26D_IS_VALID_TRUE` | MAP-26D is_valid is true |
| 5 | `MAP26D_ARTIFACTS_8_OF_8` | MAP-26D input artifact count is 8 |
| 6 | `MAP26D_HASHED_ARTIFACTS_8_OF_8` | MAP-26D hashed input artifact count is 8 |
| 7 | `MAP26D_CHECKS_17_OF_17_PASS` | MAP-26D passed manifest check count is 17 |
| 8 | `WRITER_READY_FALSE` | writer_ready is false |
| 9 | `RUNTIME_VALID_FALSE` | runtime_valid is false |
| 10 | `MATERIALIZED_FALSE` | materialized is false |
| 11 | `APPROVED_FOR_WRITER_EXPERIMENT_FALSE` | approved_for_writer_experiment is false |
| 12 | `GATE_STATUS_LOCKED` | writer_experiment_gate_status is LOCKED_PENDING_OPERATOR_APPROVAL |
| 13 | `FUTURE_EXPERIMENT_NOT_AUTHORIZED` | future_experiment_status is NOT_AUTHORIZED |
| 14 | `FORBIDDEN_ACTIONS_LISTED` | forbidden_actions list is non-empty |
| 15 | `ROLLBACK_REQUIREMENTS_LISTED` | rollback_requirements list is non-empty |
| 16 | `RISK_REGISTER_LISTED` | risk_register list is non-empty |
| 17 | `MAP26D_MANIFEST_SUMMARY_EXISTS` | MAP-26D manifest summary file exists on disk |

## What this proves

- MAP-26D manifest exists, is SHA-256 hashed, and has a complete verdict.
- MAP-26D is_valid=true, all 8 input artifacts exist and are hashed.
- MAP-26D passed all 17 manifest checks.
- writer_ready, runtime_valid, materialized remain false.
- approved_for_writer_experiment remains false.
- The writer experiment gate is LOCKED_PENDING_OPERATOR_APPROVAL.
- Forbidden actions, required preconditions, rollback requirements, and risk register are documented.

## What this does NOT prove

- Runtime validity in Project Zomboid.
- Writer readiness for PZ map compilation.
- Authorization to run MAP-26F or any writer experiment.
- Public playable packaging.
- Correct in-game tile placement.

## Claim boundary

```
writer_ready                   : false
runtime_valid                  : false
materialized                   : false
approved_for_writer_experiment : false
writer_experiment_gate_status  : LOCKED_PENDING_OPERATOR_APPROVAL
future_experiment_status       : NOT_AUTHORIZED
```

This scope record does not create geometry, write PZ runtime files, write lotpack,
write WorldGenOverride.lua, call compile-worldgen, or install anything into Project Zomboid.

## Running the helper script

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record.ps1
```

The script resolves the repo root, uses the real MAP-26D manifest as input, and writes
the scope record to:

```
.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record\map_00\
```

## Expected verdict

```
MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE
```
