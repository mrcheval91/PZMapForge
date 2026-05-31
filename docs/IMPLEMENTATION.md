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
| Schema file sanity — all 4 schemas | Ratified | test-schema-files.ps1: 104 assertions pass (4 schemas, proof-packet on v0.3) |
| Proof packet v0.3 (ImageMapForge + region + primitive hashes) | Ratified | test-proof-packet.ps1: 46 assertions pass |
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

## Provisional: present but not load-tested

| Capability | Flag | Notes |
|---|---|---|
| TileZed-openable planning TMX | PROVISIONAL | Opened visibly in TileZed. Not a PZ load-tested export. |
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

2. The TMX validity (gzip payload integrity, correct GID count) is not
   programmatically validated. A structural TMX validator is Phase 2.

3. The `palette_sha256` in the JSON artifact is not verified in any test.
   A future test should hash the palette and confirm the JSON field matches.

4. The contract test validates the artifact structure but does not validate
   field types beyond what ConvertFrom-Json infers. A JSON Schema validator
   (against schemas/pzmapforge.parsed-cell.v0.1.schema.json) would close this gap.
