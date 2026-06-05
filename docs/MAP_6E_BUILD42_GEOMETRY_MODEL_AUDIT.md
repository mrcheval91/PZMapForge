# MAP-6E: Build 42 Geometry Model Audit

```text
Schema:           pzmapforge.geometry-audit.v0.1
Claim boundary:   evidence_record_only_not_load_tested_not_playable
PZ build:         Build 42
GEOMETRY_MODEL_UNVERIFIED
LEGACY_300_ASSUMPTION_AUDITED
BUILD42_256_MODEL_OPERATOR_REPORTED
LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Summary

An operator observation raised the possibility that Build 42 uses a 256×256 tile
model rather than the 300×300 model that PZMapForge currently assumes. This document
audits every geometry assumption in the repo and records the conflict.

No load test was performed. No PZ assets were read or copied. No claim is made
about the actual Build 42 cell size until verified evidence is committed.

---

## 2. Where 300×300 appears in the repo

### 2.1 Source code

| File | Symbol / Line | Value | Role |
|---|---|---|---|
| `src/PZMapForge.Core/Palette/PaletteLoader.cs` | `RequiredCellWidth`, `RequiredCellHeight` | 300, 300 | Palette JSON must declare 300×300 cell |
| `src/PZMapForge.Core/Palette/PaletteLoader.cs` | `RequiredTileSize` | 32 | Each tile is 32×32 pixels in palette |
| `src/PZMapForge.Core/ImageParsing/ImageMapForgeParser.cs` | `RequiredWidth`, `RequiredHeight` | 300, 300 | Input blockout image must be 300×300 |
| `src/PZMapForge.Core/ParsedCell/SemanticGrid.cs` | code comment | "300×300 validation constraint" | Documents the validated size |

### 2.2 Schema

| File | Field | Value | Role |
|---|---|---|---|
| `schemas/pzmapforge.map-source.v0.1.schema.json` | `cell_size` | integer, minimum 1 | Accepts any positive integer; no 300 const |
| `schemas/pzmapforge.layer-manifest.v0.1.schema.json` | `width`, `height` | `const: 300` | Layer manifest is locked to 300×300 |

Note: `pzmapforge.map-source.v0.1.schema.json` allows any `cell_size` integer ≥ 1.
The example sets `300` but the schema does not require it.

### 2.3 Examples and docs

| File | Reference | Role |
|---|---|---|
| `examples/map-source/minimal-cell.json` | `"cell_size": 300` | Default example value |
| `examples/README.md` | "a 300×300 image", "paint a 300×300 PNG" | User documentation |
| `docs/COMPILED_CELL_FORMAT_EVIDENCE.md` §18 | "300×300 tiles / 10×10 per chunk = 30×30 = 900 chunks/cell" | Inference from Workshop mod observation |

### 2.4 Binary writer assumptions

The MAP-5A experimental binary writer encodes the following values:

| Binary artifact | Hardcoded value | Geometry implication |
|---|---|---|
| `*.lotpack` header hdrA | 900 | Assumes 30×30 = 900 chunks per cell |
| `*.lotpack` header hdrB | 7204 | Derived: 4 + 900×8 = 7204 (offset table) |
| `*.lotpack` total size | 7208 | 8 header + 7200 offset table bytes |
| `chunkdata_*.bin` body | 900 bytes (bytes 2-901) | 30×30 chunk grid |
| `chunkdata_*.bin` total | 902 bytes | 2 header + 900 grid |

These values derive from the inference:
```
30 × 30 = 900 chunks/cell
derived from: 300 tiles / 10 tiles-per-chunk = 30 chunks/side
```

This inference is based on Workshop mod observation, NOT on verified Build 42
documentation. The observed mods are not explicitly identified as Build 41 or
Build 42 in the committed evidence (gap: OPEN — see
`docs/COMPILED_CELL_FORMAT_EVIDENCE.md` section 5, "Build 41 vs Build 42
format differences: OPEN").

### 2.5 Tests

| Test location | What is tested |
|---|---|
| `tests/PZMapForge.Cli.Tests/CliProcessTests.cs` | 300×300 images required/accepted |
| `tests/PZMapForge.Core.Tests/Palette/PaletteLoaderTests.cs` | PaletteLoader enforces 300×300 |
| MAP-5A binary writer tests | lotpack = 7208 bytes, chunkdata = 902 bytes (both imply 300 model) |

---

## 3. Where 256 appears in the repo

| File | Reference | Role |
|---|---|---|
| `src/PZMapForge.Cli/Program.cs` (WritePlaceholderPng) | `new Bitmap(256, 64)` | Placeholder PNG dimensions — **not** cell geometry |
| `scripts/inspect-compiled-binary-headers.ps1` | `-MaxBytes 256` | Byte-read limit — **not** cell geometry |
| `docs/COMPILED_CELL_FORMAT_EVIDENCE.md` §18 (chunkdata first-bytes table) | "`00 01` as U16 LE = 256" | A mathematical interpretation of the 2-byte header; the document does not assign geometry meaning to this value |

**No committed evidence or code assigns geometric meaning to 256 in the context
of cell or tile dimensions.** The chunkdata `00 01` header is documented as a
consistent pattern but its role is marked unknown.

---

## 4. Operator observation

The operator has reported that Build 42 may use a **256×256 tile model** rather
than 300×300. This has not been verified by committed evidence.

If Build 42 is 256×256:
- The chunk-per-cell count would change. Under the current 10-tiles-per-chunk
  model: 256 / 10 = 25.6 (not integer — chunk size may differ in Build 42).
- An alternative: Build 42 may use 8-tiles-per-chunk: 256/8 = 32 chunks per
  side → 32×32 = 1024 chunks/cell.
- Or: Build 42 may use a 16-tiles-per-chunk model: 256/16 = 16 chunks per side
  → 16×16 = 256 chunks/cell. (256 = 0x100 = the U16 LE value of `00 01` in the
  chunkdata header — speculative; role of `00 01` is unconfirmed.)
- All of these are speculative. No single model can be selected without evidence.

Under any 256-based geometry, **all** current binary hardcoded values (hdrA=900,
hdrB=7204, 7208-byte lotpack, 902-byte chunkdata) would be wrong.

---

## 5. Status

```text
GEOMETRY_MODEL_UNVERIFIED
LEGACY_300_ASSUMPTION_AUDITED
BUILD42_256_MODEL_OPERATOR_REPORTED
LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

- `GEOMETRY_MODEL_UNVERIFIED`: Build 42 cell geometry has not been confirmed
  by direct evidence (binary inspection of a verified Build 42 mod output).
- `LEGACY_300_ASSUMPTION_AUDITED`: All places where 300×300 is assumed have been
  catalogued in section 2 of this document.
- `BUILD42_256_MODEL_OPERATOR_REPORTED`: The operator believes Build 42 uses
  256×256. This is recorded as an operator report, not confirmed fact.
- `LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION`: No further manual load testing
  should proceed until the geometry model is clarified. Loading the current
  binary candidate against a 256-model engine would produce a different error
  than the MAP-6B EOFException, but without knowing the correct model the
  error is not actionable.

---

## 6. What must be resolved before the next load test

Before preparing any new load test packet:

1. **Confirm the Build 42 cell geometry.** Required evidence:
   - A known-good Build 42 map mod (Steam Workshop or local WorldEd export
     targeting Build 42).
   - Binary inspection of its chunkdata file: what size? Is the chunk grid
     still 30×30 (900 bytes) or different?
   - Binary inspection of its lotpack header: is hdrA still 900?

2. **Update binary writers if geometry changed.** If Build 42 uses 256×256:
   - Recalculate chunk count and grid size.
   - Update hdrA, hdrB, lotpack size, chunkdata body.
   - Update the layer-manifest schema if cell dimensions change.

3. **Inspect the image parsing pipeline.** If cell size changes:
   - `PaletteLoader.RequiredCellWidth/Height = 300` may need updating.
   - `ImageMapForgeParser.RequiredWidth/Height = 300` may need updating.
   - Existing parsed-cell artifacts (300×300 semantic grids) may not apply.

---

## 7. Experimental report fields (MAP-6E additions)

The experimental binary report now includes:

| Field | Value |
|---|---|
| `geometry_model_status` | `"mismatch_suspected_not_verified"` |
| `geometry_model_basis` | `"30x30_chunk_grid_from_300x300_cell_build41_workshop_evidence"` |
| `target_build42_cell_size` | `"operator_reported_256_unverified"` |

These fields make the geometry uncertainty explicit in every generated report.

---

## 8. Non-claims

- This document does not claim Build 42 is 256×256.
- This document does not claim the 300×300 model is wrong.
- No load test was performed.
- No PZ assets were read or copied.
- No playable export claim.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
