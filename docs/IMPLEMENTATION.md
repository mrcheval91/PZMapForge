# Implementation

Current-state mapping of what PZMapForge actually does vs. what it claims.

---

## Ratified: what is proven and committed

| Capability | Status | Evidence |
|---|---|---|
| RGB palette loading and validation | Ratified | 28-assertion test harness passes |
| Image pixel scan (exact colour match) | Ratified | Test 9: exact grass pixel matched correctly |
| Nearest-colour fallback with drift cache | Ratified | Test 9: near-grass mapped to grass, dist 1.73 |
| parsed-cell.json (counts, legend, drift) | Ratified | Test 6: all 5 outputs written |
| parsed-cell.json artifact contract | Ratified | test-parsed-cell-contract.ps1: 40 assertions pass |
| TMX structural integrity (base64, gzip, GID range) | Ratified | test-tmx-integrity.ps1: 21 assertions pass |
| Schema file sanity — all 4 schemas | Ratified | test-schema-files.ps1: 104 assertions pass (4 schemas, proof-packet on v0.3) |
| Palette SHA-256 verification (parsed-cell vs source/image-palette.json) | Ratified | test-palette-sha256.ps1: 5 assertions pass; Gap 3 closed |
| Proof packet v0.5 (+ palette_sha256_verification=5; total=292) | Ratified | test-proof-packet.ps1: 48 assertions pass |
| Semantic region extraction (4-neighbor BFS) | Ratified | test-region-extraction.ps1: 24 assertions pass |
| Primitive classification (9 kinds to 7 types) | Ratified | test-primitive-classification.ps1: 22 assertions pass |
| parsed-cell-report.md (drift table, legend) | Ratified | Test 9: drift section present in report |
| Deterministic output (same input = same counts) | Ratified | Test 8: two runs identical |
| Kind-count completeness (sum = W x H) | Ratified | Test 7: 90000 = 300 x 300 |
| Bad image path exits nonzero | Ratified | Test 1 |
| Non-300x300 without -Resize exits nonzero | Ratified | Test 2 |
| External OutputDir refused | Ratified | Test 3 |
| media/maps refused | Ratified | Test 4 |
| Debug mode exits 0, no artifacts | Ratified | Test 5 |
| .local/ gitignored | Ratified | Test 10 |

| .NET palette loader (PaletteLoader.Load) | Ratified | 8 xUnit tests pass (valid, missing, dup GID, missing kind, invalid RGB, dup code, wrong schema, wrong width) |
| .NET CLI palette-check command | Ratified | Exits 0 on valid palette; prints schema/dims/kinds/GID range/status |
| .NET parsed-cell reader (ParsedCellLoader.Load) | Ratified | 11 xUnit tests pass; SemanticGrid GetCode/InBounds/CountCode verified |
| .NET CLI parsed-cell-check command | Ratified | Exits 0 on valid artifact; prints schema/dims/rows/kinds/status |
| .NET region extractor (RegionExtractor.Extract) | Ratified | 8 xUnit tests; 4-neighbor BFS, deterministic sort, 90000 pixel coverage |
| .NET CLI region-check command | Ratified | Exits 0; prints dims/regions/kinds/pixels/status |
| .NET cross-verification vs PS regions.json | Ratified | 3 cross-verification xUnit tests; totals, summary-by-kind, per-region details match PS reference for all 9 regions |
| .NET primitive classifier (PrimitiveClassifier.Classify) | Ratified | 16 xUnit tests (8 behavioural + 8 [Theory] kind mappings); PS cross-verification fixture proves .NET output matches PS reference for all 9 primitives |
| .NET CLI primitive-check command | Ratified | Exits 0 on valid parsed-cell; prints dimensions/regions/primitives/types/pixels/status |

## Provisional: present but not load-tested

| Capability | Flag | Notes |
|---|---|---|
| TileZed-openable planning TMX | PROVISIONAL | Opened visibly in TileZed. Structurally validated (gap 2 closed). Not a PZ load-tested export. |
| -Resize flag (nearest-neighbour) | PROVISIONAL | Logic present, not explicitly tested in harness. |

## Not present: out of scope for current phase

| Capability | Notes |
|---|---|
| lotpack / lotheader / bin generation | Phase 4. Requires WorldEd format research. |
| Semantic kind -> PZ tile ID mapping | Phase 3. Requires local PZ install config. |
| Multi-layer image conventions | Phase 2. Not designed yet. |
| Build 42 compatibility | Unverified. No load test performed. |
| Steam Workshop packaging | Not planned. |

## Known gaps

1. The `-Resize` flag is not covered by a dedicated test assertion. A test image
   at a non-300x300 size run with `-Resize` should produce correct kind counts.
   Tracked as a future test addition.

2. ~~TMX structural validation~~ — Closed by Slice 10 (test-tmx-integrity.ps1).

3. ~~palette_sha256 not verified~~ — Closed by test-palette-sha256.ps1.

4. The contract test validates the artifact structure but does not validate
   field types beyond what ConvertFrom-Json infers. A JSON Schema validator
   (against schemas/pzmapforge.parsed-cell.v0.1.schema.json) would close this gap.
