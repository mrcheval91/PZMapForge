# MAP-26D WorldBuilder Minimal Concrete Geometry Writer Input Manifest

## Purpose

MAP-26D reads the MAP-26A, MAP-26B, and MAP-26C output artifacts and packages them into a
locked, hash-verified input bundle. It answers:

```
Do we have a complete, internally consistent, reviewed input bundle for a future writer experiment?
```

MAP-26D does not create geometry. It does not write PZ runtime files. It does not authorize
a writer experiment. It records that such an experiment could be attempted and confirms the
input bundle is internally consistent.

## Input files

| File | Source |
|------|--------|
| `map_00.minimal_concrete_geometry_mvp.json` | MAP-26A output |
| `map_00.minimal_concrete_geometry_qa_overlay.json` | MAP-26B output |
| `map_00.minimal_concrete_geometry_qa_overlay.csv` | MAP-26B output |
| `map_00.minimal_concrete_geometry_qa_overlay.png` | MAP-26B output |
| `map_00.minimal_concrete_geometry_qa_review_packet.json` | MAP-26C output |
| `map_00.minimal_concrete_geometry_qa_review_packet.csv` | MAP-26C output |
| `map_00.minimal_concrete_geometry_qa_review_packet.md` | MAP-26C output |
| `map_00.minimal_concrete_geometry_qa_review_packet.summary.txt` | MAP-26C output |

## Output files

| File | Description |
|------|-------------|
| `map_00.minimal_concrete_geometry_writer_input_manifest.json` | Full manifest with artifact list, checks, SHA-256 hashes |
| `map_00.minimal_concrete_geometry_writer_input_manifest.md` | Human-readable manifest with writer gate and claim boundary |
| `map_00.minimal_concrete_geometry_writer_input_manifest.csv` | One row per manifest check |
| `map_00.minimal_concrete_geometry_writer_input_manifest.summary.txt` | Key/value summary for quick inspection |

## SHA-256 hashing

MAP-26D computes SHA-256 for all 8 input artifacts. This produces a stable, verifiable
fingerprint of the input bundle at the time the manifest was built.

Hashes are stored in `input_artifacts[].sha256` in the JSON output.

If any artifact is missing, the manifest reports `is_valid: false`.

## Manifest checks

MAP-26D performs 17 deterministic checks:

| # | Check ID | What it verifies |
|---|----------|-----------------|
| 1 | ALL_REQUIRED_INPUT_ARTIFACTS_EXIST | All 8 input artifacts exist |
| 2 | ALL_REQUIRED_INPUT_ARTIFACTS_HASHED | All 8 input artifacts are SHA-256 hashed |
| 3 | MAP26A_VERDICT_COMPLETE | MAP-26A verdict is complete |
| 4 | MAP26B_VERDICT_COMPLETE | MAP-26B verdict is complete |
| 5 | MAP26C_VERDICT_COMPLETE | MAP-26C verdict is complete |
| 6 | MAP26C_REVIEW_CHECKS_18_OF_18_PASS | MAP-26C passed review check count is 18 |
| 7 | COMPONENT_ID_STABLE | Target component ID is stable (map_00_component_0001) |
| 8 | COMPONENT_BBOX_STABLE | Component bbox is stable ((124,10)->(212,69) 89x60px) |
| 9 | LOT_COUNT_STABLE_7 | Lot count is stable at 7 |
| 10 | ACCEPTED_BUILDING_SLOT_COUNT_STABLE_7 | Accepted building slot count is stable at 7 |
| 11 | OVERLAY_FEATURE_COUNT_STABLE_15 | Overlay feature count is stable at 15 |
| 12 | SOURCE_DIMENSIONS_STABLE_256X256 | Source dimensions are stable at 256x256 |
| 13 | WRITER_READY_FALSE | writer_ready is false |
| 14 | RUNTIME_VALID_FALSE | runtime_valid is false |
| 15 | MATERIALIZED_FALSE | materialized is false |
| 16 | APPROVED_FOR_WRITER_EXPERIMENT_FALSE | approved_for_writer_experiment is false |
| 17 | NO_FORBIDDEN_OUTPUTS_CREATED | No forbidden outputs created |

## Writer experiment gate

```
approved_for_writer_experiment : false
writer_experiment_gate_status  : LOCKED_PENDING_OPERATOR_APPROVAL
```

`approved_for_writer_experiment` is always `false` in MAP-26D. It is a future gate, not a
claim of writer readiness. The gate exists so that a future writer experiment can declare
its input as a reviewed, hash-verified bundle — but only after explicit operator approval.

MAP-26D does not unlock the gate. MAP-26D does not imply the gate will ever be unlocked.
Unlocking requires a separate operator decision and a separate task.

## Why approved_for_writer_experiment remains false

MAP-26D records the geometry chain is internally consistent and all input artifacts exist
and are hash-verified. That is necessary but not sufficient to authorize a writer experiment.

Authorization also requires:
- A defined writer task scope and boundary
- Explicit operator review of that scope
- A decision record

Until those exist, the gate stays locked.

## What this proves

- All 8 input artifacts from MAP-26A/B/C exist on disk.
- All 8 input artifacts are SHA-256 hashed.
- MAP-26A, MAP-26B, and MAP-26C verdicts are complete.
- MAP-26C passed all 18 review checks.
- Component ID, bbox, lot count, slot count, feature count, source dimensions are stable.
- writer_ready, runtime_valid, and materialized remain false.
- No forbidden outputs were created.

## What this does NOT prove

- Runtime validity in Project Zomboid.
- Writer readiness for PZ map compilation.
- Public playable packaging.
- Correct in-game tile placement.
- Any claim about TileZed or WorldEd compatibility.
- Authorization to run a writer experiment.

## Claim boundary

```
writer_ready                   : false
runtime_valid                  : false
materialized                   : false
approved_for_writer_experiment : false
writer_experiment_gate_status  : LOCKED_PENDING_OPERATOR_APPROVAL
```

This manifest does not create geometry, write PZ runtime files, write lotpack,
write WorldGenOverride.lua, call compile-worldgen, or install anything into Project Zomboid.

## Running the helper script

```powershell
.\examples\deadmtl-layer-pack\scripts\run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-input-manifest.ps1
```

The script resolves the repo root, uses the real MAP-26A/B/C `.local` outputs as inputs,
and writes the manifest to:

```
.local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-input-manifest\map_00\
```

## Expected verdict

```
MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE
```
