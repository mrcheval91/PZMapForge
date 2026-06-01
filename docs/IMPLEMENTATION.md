# Implementation

Current-state mapping of what PZMapForge actually does vs. what it claims.

---

## Ratified: what is proven and committed

| Capability | Status | Evidence |
|---|---|---|
| RGB palette loading and validation | Ratified | 28-assertion test harness passes |
| .NET ImageMapForgeParser (PNG/BMP -> rows/counts/matching) | Ratified | 10 xUnit tests; exact+nearest match, Resize flag, determinism, palette SHA-256, Windows/GDI+ |
| .NET parser cross-verification vs parsed-cell fixture | Ratified | 6 xUnit tests; rows/counts/matching/resize all match valid.json fixture |
| .NET CLI image-check command | Ratified | Exits 0; prints dims/rows/kinds/exact/nearest/unmapped/sha256/status; --resize flag supported |
| ImageMapForgeArtifactWriter (image -> parsed-cell.json) | Ratified | 9 xUnit tests; schema/claim/dims/rows/counts/resized/determinism/loadable by ParsedCellLoader |
| .NET CLI image-export command | Ratified | Writes parsed-cell.json to .local/; refuses non-.local output; --resize flag supported |
| .NET CLI full-pipeline command | Ratified | Writes parsed-cell.json, regions.json, primitives.json, plan-recommendations.json, plan-report.md; --resize and thresholds supported |
| RegionArtifactWriter (regions.json + regions-report.md) | Ratified | 8 xUnit tests; schema, claim_boundary, region_count, pixel total, md exists, md claim boundary, md summary table |
| PrimitiveArtifactWriter (primitives.json + primitives-report.md) | Ratified | 8 xUnit tests; schema, claim_boundary, primitive_count, pixel total, md exists, md claim boundary, md summary table |
| Process-level CLI integration tests | Ratified | 10 process tests; test 7 verifies 7 artifacts including regions-report.md and primitives-report.md |
| Image pixel scan (exact colour match) | Ratified | Test 9: exact grass pixel matched correctly |
| Nearest-colour fallback with drift cache | Ratified | Test 9: near-grass mapped to grass, dist 1.73 |
| parsed-cell.json (counts, legend, drift) | Ratified | Test 6: all 5 outputs written |
| parsed-cell.json artifact contract | Ratified | test-parsed-cell-contract.ps1: 40 assertions pass |
| TMX structural integrity (base64, gzip, GID range) | Ratified | test-tmx-integrity.ps1: 21 assertions pass |
| Schema file sanity — all 4 schemas | Ratified | test-schema-files.ps1: 104 assertions pass (4 schemas, proof-packet on v0.3) |
| Palette SHA-256 verification (parsed-cell vs source/image-palette.json) | Ratified | test-palette-sha256.ps1: 5 assertions pass; Gap 3 closed |
| ImageMapForge -Resize flag (150x150 scaled to 300x300) | Ratified | test-image-mapforge.ps1 Test 11: 8 assertions; Gap 1 closed |
| Plan-recommendations schema sanity | Ratified | test-schema-files.ps1: 134 total (was 104; +26 plan-recs +4 proof-packet v0.7) |
| Plan-recommendations artifact contract | Ratified | test-plan-recommendations-contract.ps1: 28 assertions pass (+7 thresholds_used checks) |
| Proof packet v0.10 (dotnet_validation_summary added; PS total=381) | Ratified | test-proof-packet.ps1: 69 assertions pass; dotnet lane separate |
| Validation ledger (docs/VALIDATION_LEDGER.md) | Ratified | Both lanes documented; baseline cac517c; commands and expected counts recorded |
| validate.ps1 ledger summary | Ratified | Final output reports PS lane (381 total) and .NET lane (152 total) separately; claim boundary stated |
| Phase 2 decision record (docs/PHASE_2_DECISION.md) | Ratified | Option A (multi-layer image conventions) chosen; Option B (PZ tile IDs) deferred; Slice 2A-1 complete |
| LayerManifestLoader (Slice 2A-1) | Ratified | 12 xUnit tests; valid fixture, missing file, schema/boundary/dims, dup names, precedence errors, unknown kinds, empty kinds/path |
| .NET plan artifact cross-verification | Ratified | PlanningArtifactCrossVerificationTests: 3 [Fact] methods verify header fields, all 13 recommendations, and summary against committed fixture |
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
| .NET planning rule engine (PlanningRuleEngine.Evaluate) | Ratified | 15 xUnit tests cover all 7 primitive types, missing spawn, determinism, counts by type/severity, source id retention |
| PlanningRuleOptions configurable thresholds | Ratified | 8 xUnit tests: default preserves output, zero tiny threshold suppresses warnings, boundary match, high large-ground suppresses note, negatives throw |
| CLI --tiny-threshold / --large-threshold flags | Ratified | plan-check and plan-export accept optional threshold flags; non-integer/negative exit 1; output shows values used; 5 CLI smoke tests |
| .NET CLI plan-check command | Ratified | Exits 0; prints dims/primitives/recommendations/warnings/thresholds/status |
| Planning artifact export (PlanningArtifactWriter) | Ratified | 10 xUnit tests; deterministic JSON + markdown; thresholds_used field recorded |
| .NET CLI plan-export command | Ratified | Writes plan-recommendations.json + plan-report.md to .local/mapforge/ |

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
| Multi-layer image conventions | Phase 2. Slice 2A-1 complete (manifest loader). Slice 2A-2 (layer merger) is next. |
| Build 42 compatibility | Unverified. No load test performed. |
| Steam Workshop packaging | Not planned. |

## Known gaps

1. ~~-Resize flag not test-covered~~ — Closed by test-image-mapforge.ps1 Test 11.

2. ~~TMX structural validation~~ — Closed by Slice 10 (test-tmx-integrity.ps1).

3. ~~palette_sha256 not verified~~ — Closed by test-palette-sha256.ps1.

4. The contract test validates the artifact structure but does not validate
   field types beyond what ConvertFrom-Json infers. A JSON Schema validator
   (against schemas/pzmapforge.parsed-cell.v0.1.schema.json) would close this gap.
