# Implementation

Current-state mapping of what PZMapForge actually does vs. what it claims.

---

## Ratified: what is proven and committed

| Artifact index panel (APP-9) | Ratified | BuildArtifactIndexHtml(relativeImgSrc, relativeAnnotSrc, svgAnnotationPresent, svgCandidatesPresent, svgReviewPresent, svgManifestPresent): compact full-width .artifacts-idx div inserted between .cockpit and .workbench; always links Clean analysis image / Parsed preview / Parsed cell JSON / Regions JSON / Primitives JSON / Plan recommendations JSON; conditionally adds Annotation image (annotation present), SVG structure report (SVG annotation parsed), SVG layer candidates + SVG layer selection template (candidates present), SVG selection review (review present), SVG planning manifest JSON + Markdown (manifest present); absent items omitted; all links relative; no absolute paths; panel note "planning artifact only -- not a playable Project Zomboid export"; CSS .artifacts-idx/.artifacts-idx-hdr/.artifacts-idx-note; artifactIndexHtml parameter threaded through BuildAppHtml; 10 new tests; total 353 tests (190 Core + 163 CLI); no media/maps, no PZ assets, no SVG geometry conversion, no coordinate math, no playable export claim |
| Run summary cockpit header (APP-8) | Ratified | BuildRunSummaryHtml(paletteClean, svgAnnotation, svgParseStatus, svgCandidates, svgReview, svgManifest): two-row .cockpit div (Run Summary + Safety); flags derived from in-memory state; Safety row hardcodes playable export generated false, PZ assets copied/read false, media/maps touched false, claim_boundary intact; inserted between .boundary and .workbench in HTML; CSS .cockpit/.cockpit-row/.ck-ok/.ck-warn/.ck-absent/.ck-safe; 8 new tests; total 343 tests (190 Core + 153 CLI) |
| Montreal SVG cockpit smoke verification (APP-8A) | Ratified | smoke-montreal-svg-planning-manifest.ps1: 15 new APP-8 cockpit checks added (source run HTML: Run Summary, SVG annotation present, SVG parse parsed, SVG candidates present, 4 safety claims; review run HTML: Run Summary, SVG review present, Planning manifest present, 4 safety claims); 25 total checks; all PASS |
| Montreal SVG planning manifest smoke (SVG-11) | Ratified | scripts/smoke-montreal-svg-planning-manifest.ps1: two-pass local smoke; source run (app-export + SVG annotation -> candidates + selection template); writes 9-item selection JSON (Eaux/water, Outline_MTL/outline, SudOuest+VilleMarie+Plateau+NDG_CDN/borough, ANGRIGNON/transit, Pte-Angus+Cap-Saint-Jacques/park); review run (app-export --svg-selection -> manifest); 10 verification checks (file existence, selected_count 9, planning_status, exported_to_project_zomboid false, converted_to_map_geometry false, markdown non-claim, HTML heading); requires machine-local SVG and analysis PNG (not committed); all output under .local/ |
| SVG planning manifest visible summary (SVG-10) | Ratified | BuildSvgPlanningManifestHtml(items): meta-tbl (selected_count, planning_status operator_selected_metadata_only); Intended Uses chips; items grouped by bucket (svg-chip + review-use + review-note); nc-list (No SVG geometry converted, No SVG coordinates extracted, No Project Zomboid export generated, No media/maps writes, No PZ assets copied or read); artifact links retained; zero-selected: unchanged (no table rendered); 8 new tests in AppExportSelectionTests + AppExportZeroSelectionTests; total 335 tests (190 Core + 145 CLI) |
| SVG planning manifest (SVG-9) | Ratified | WriteSvgPlanningManifest: svg-planning-manifest.json (schema v0.1, planning_status "operator_selected_metadata_only", selected_by_bucket, intended_uses, operator_notes max 50, exported_to_project_zomboid false, all safety flags false); BuildSvgPlanningManifestMarkdown: svg-planning-manifest.md (ASCII, title, claim boundary, items by bucket, intended uses, non-claims); BuildSvgPlanningManifestHtml: h2 "SVG Planning Manifest", inert-manifest note, artifact links; zero-selected does not fail, no manifest written; AppExportSelectionFixture (ManifestJson/Md props); AppExportZeroSelectionTests (ExitsZeroAndNoManifest); 9 new tests; total 327 tests (190 Core + 137 CLI) |
| SVG layer selection review import (SVG-8) | Ratified | --svg-selection <json>; ReadSvgLayerSelection (JsonDocument, tolerant); WriteSvgLayerSelectionReview (schema v0.1, selected_count, selected_items); BuildSvgLayerSelectionReviewHtml (h2 "SVG Selection Review", metadata-only notes); AppExportSelectionFixture + AppExportSelectionTests (9 tests incl. missing-file exit); total 318 tests |
| SVG layer selection template (SVG-7) | Ratified | WriteSvgLayerSelectionTemplate: writes svg-layer-selection.template.json with schema v0.1, selection_status "operator_review_required", per-bucket item objects (value/selected/intended_use/operator_note); BuildSvgLayerSelectionHtml: h2 "SVG Layer Selection Template" with review/no-geometry notes; 8 new tests; total 308 tests |
| Full SVG metadata candidate inventory (SVG-6) | Ratified | AllIds/AllClasses/AllTextLabels (bounded at 500) added to SvgStructureResult; WriteSvgLayerCandidates classifies full lists; JSON adds total_id/class/text/metadata_values_inspected; BoroughOrDistrictFullCount separates true count from sample; HTML shows Metadata Values Inspected totals; AppExportSvgFullInventoryFixture (35-ID SVG) proves count > samples; 7 new tests; total 300 tests |
| Montreal SVG classification tuning (SVG-5) | Ratified | ContainsWord (word-boundary fix for "eau"/"Plateau"); 3 new buckets: technical_layer, transit_or_station, park_or_green_space; all-caps heuristic for transit; candidate_generation_notes + inspected_metadata_sources fields; 8 new tests; total 293 tests |
| SVG layer candidate inventory (SVG-4) | Ratified | WriteSvgLayerCandidates: pattern-match IDs/classes/text against water/outline/street/borough/label buckets; svg-layer-candidates.json with schema v0.1, candidate_generation_method, all safety flags; BuildSvgLayerCandidatesHtml: "SVG Layer Candidates" panel with chip buckets; 8 new tests; total 285 tests |
| SVG structure viewer panel (SVG-3) | Ratified | BuildSvgStructureHtml: SvgStructureResult record; h2 "SVG Structure Summary", parse_status badge, metadata table, Element Counts table, Sample IDs chips, Sample Text Labels chips, not-converted-to-geometry note, artifact links; 6 new tests; total 277 tests |
| SVG large-file parse fix (SVG-2A) | Ratified | DtdProcessing Prohibit→Ignore (SVGs with DOCTYPE now parse; XmlResolver=null still blocks external entities); MaxCharactersInDocument raised 10M→50M; parse_status/parse_error/max_characters_in_document added; 4 new tests; total 271 tests |
| SVG reference structure inspector (SVG-2) | Ratified | WriteSvgStructure: safe XML parse (DTD prohibited, no network); element counts (12 types), id/class/text samples (max 20 each), likely flags; svg-reference-structure.json in artifacts/; SVG Structure section in HTML right panel; 7 new tests; total 267 tests |
| SVG annotation reference support (APP-7) | Ratified | extension-based SVG detection; panel label -> "SVG Vector Reference"; annotGuidanceHtml slot in preview row; svg-reference-summary.json artifact (schema v0.1, parsed_as_geometry=false, all safety flags); AppExportSvgFixture + AppExportSvgAnnotationTests (6 tests); total 260 tests |
| app-export annotation workflow (APP-6) | Ratified | --annotation <image>: copied to images/annotation-image.<ext>; shown as Annotation Reference panel; not parsed; exits 1 if file missing; Analysis Input label replaces Original Input; guidance text updated with clean palette-only language; 3 new tests (annotation file+html, missing file exits 1, clean palette guidance); total 254 tests |
| app-export palette health and parsed preview (APP-5) | Ratified | WriteParsedPreview: bitmap rendered from snapped palette colors, written to images/parsed-preview.png; Original Input + Parsed Preview side-by-side; Palette Health section (health badge clean/dirty/unknown, guidance, not-palette-clean text, text-labels guidance); 6 new tests (file exists, Original Input, Parsed Preview, Palette Health, not-palette-clean, text-labels); total 251 tests |
| app-export workbench layout (APP-4) | Ratified | two-column workbench grid (left: Map Preview 600px + Visual Legend; right: Summary + Artifact Files + Non-claims); responsive at 860px; section headers: Map Preview/Summary/Visual Legend/Artifact Files/Non-claims; 4 new content tests (workbench class, Map Preview, Summary, Artifact Files) |
| app-export blockout UX improvement (APP-3) | Ratified | --run-name <name> subdirectory support; visual legend with color swatches + pixel counts; color match summary bar; nearest drift table; split JSON/MD artifact sections; palette kinds card; 8 new tests (2 run-name process + 6 content contract) |
| app-export HTML viewer improvement (APP-2) | Ratified | index.html: summary cards (dimensions/regions/primitives/recs/warnings), input image display (copied to images/), dark themed static CSS; tests assert image copy + card markup present |
| app-export CLI command and HTML viewer (Slice 3A-6 app) | Ratified | app-export runs full pipeline, writes artifacts/ + index.html to .local/; 5 process tests; static HTML, no server, no JS framework; no PZ assets read/copied; claim boundary enforced |
| Tilesheet format investigation decision record (Slice 3A-6-pre) | Ratified | docs/TILESHEET_FORMAT_INVESTIGATION_DECISION.md: governance gate; documents why 3A-6 is blocked, format knowledge required, allowed/forbidden investigation actions, 4 decision options, recommended Option B+D, evidence checklist, operator checklist; no code changes |
| local-tile-survey CLI hardening and docs (Slice 3A-5) | Ratified | docs/LOCAL_TILE_SURVEY_CLI.md added; operator guide with syntax, fake-path example, output files, validation behavior, safety guarantees, non-claims, troubleshooting; no code changes; no new tests required |
| Proof packet v0.16 (Slice 3A-4 CLI; .NET 230; PS total=474) | Ratified | test-proof-packet.ps1: 102 assertions pass; local_tile_survey_cli evidence flags added |
| local-tile-survey CLI command (Slice 3A-4) | Ratified | Loads config, validates install, writes local-tile-reference-survey.json + .md to --output (.local guard enforced); 5 process tests; no real PZ install required; no assets read/copied |
| Local PZ install config loader (Slice 3A-1) | Ratified | Schema + typed loader; validates document and safety flags only; does not require real PZ install; no asset inspection/copying |

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
| Schema file sanity Ã¢â‚¬â€ all 4 schemas | Ratified | test-schema-files.ps1: 104 assertions pass (4 schemas, proof-packet on v0.3) |
| Palette SHA-256 verification (parsed-cell vs source/image-palette.json) | Ratified | test-palette-sha256.ps1: 5 assertions pass; Gap 3 closed |
| ImageMapForge -Resize flag (150x150 scaled to 300x300) | Ratified | test-image-mapforge.ps1 Test 11: 8 assertions; Gap 1 closed |
| Plan-recommendations schema sanity | Ratified | test-schema-files.ps1: 134 total (was 104; +26 plan-recs +4 proof-packet v0.7) |
| Plan-recommendations artifact contract | Ratified | test-plan-recommendations-contract.ps1: 28 assertions pass (+7 thresholds_used checks) |
| Proof packet v0.10 (dotnet_validation_summary added; PS total=381) | Ratified | test-proof-packet.ps1: 69 assertions pass; dotnet lane separate |
| Proof packet v0.11 (Phase 2A; .NET 184; PS total=391) | Ratified | test-proof-packet.ps1: 79 assertions pass; layer_pipeline fields added |
| Phase 2B/3 decision record (docs/PHASE_2B_OR_PHASE_3_DECISION.md) | Ratified | Phase 2B (layer authoring conventions) chosen; Phase 3 (PZ tile IDs) deferred; Slice 2B-1 defined |
| Layer authoring guide (Slice 2B-1) | Ratified | docs/LAYER_AUTHORING_GUIDE.md: claim boundary, workflow, kind-by-layer table, precedence, conflict policy, error glossary, non-claims |
| Layer fixture examples (Slice 2B-1) | Ratified | tests/fixtures/layers/README.md + example-2b/ (manifest + README; no binary images) |
| Layer fixture generator (Slice 2B-2) | Ratified | new-example-images.ps1 generates 4 deterministic PNGs; generated-layer-manifest.json; generated/ gitignored; pipeline verified (36 conflicts, Status OK) |
| LayerValidator (layer-validate CLI) | Ratified | 8 Core xUnit tests + 5 process tests; validates manifest + images without writing artifacts; exits 0/1; --resize supported |
| Proof packet v0.12 (layer-validate; .NET 197; PS total=393) | Ratified | test-proof-packet.ps1: 81 assertions pass; layer_validate_present + layer_validate_writes_artifacts added |
| Phase 3 local PZ config spec (docs/PHASE_3_LOCAL_PZ_CONFIG_SPEC.md) | Ratified | Precondition documentation; local config contract, 8 safety checks, 5 proposed slices (3A-1 through 3A-5); not implemented |
| Phase 3A local install survey (docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md) | Ratified | Operator survey guide; 8-step PowerShell inventory; placeholder table; survey output to .local/ (gitignored); decision gate defined |
| Phase 3A survey helper (scripts/Run-Phase3ALocalPzSurvey.ps1) | Ratified | Read-only automated survey; writes pz-install-survey-latest.txt and pz-install-survey-redacted-latest.md to .local/ (gitignored); exits 0 even if PZ not found |
| Phase 3A survey status | Recorded | Manual-path run 2026-06-01: install found, media/ present (33 subdirs), media/tiles/ absent. Tile directory not yet located. Phase 3 BLOCKED. |
| Phase 3A decision record (docs/PHASE_3A_DECISION.md) | Ratified | Media layout survey 2026-06-02: .pack (~20) and .tiles (~7) formats confirmed. Phase 3A-1 (config schema + loader) unblocked. Tile catalog and kind mapping remain blocked. |
| Validation ledger (docs/VALIDATION_LEDGER.md) | Ratified | Both lanes documented; baseline cac517c; commands and expected counts recorded |
| validate.ps1 ledger summary | Ratified | Final output reports PS lane (381 total) and .NET lane (152 total) separately; claim boundary stated |
| Phase 2 decision record (docs/PHASE_2_DECISION.md) | Ratified | Option A (multi-layer image conventions) chosen; Option B (PZ tile IDs) deferred; Slice 2A-1 complete |
| LayerManifestLoader (Slice 2A-1) | Ratified | 12 xUnit tests; valid fixture, missing file, schema/boundary/dims, dup names, precedence errors, unknown kinds, empty kinds/path |
| LayerMerger (Slice 2A-2) | Ratified | 12 xUnit tests; single layer, two-layer precedence, 4-layer non-overlapping, missing image, disallowed kind, conflict count/sample cap, resize true/false, determinism, RegionExtractor passthrough, claim boundary |
| LayerMergeArtifactWriter (Slice 2A-3) | Ratified | 7 xUnit tests; creates parsed-cell.json (ParsedCellLoader-loadable), layer-merge-report.md (claim boundary, contribution table, conflict count), determinism |
| layer-pipeline CLI command (Slice 2A-3) | Ratified | 1 process test; writes 8 artifacts; refuses non-.local output; --resize and threshold flags supported |
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
| Multi-layer image conventions | Phase 2A complete: manifest loader, layer merger, CLI command + artifact writer. |
| Build 42 compatibility | Unverified. No load test performed. |
| Steam Workshop packaging | Not planned. |

## Known gaps

1. ~~-Resize flag not test-covered~~ Ã¢â‚¬â€ Closed by test-image-mapforge.ps1 Test 11.

2. ~~TMX structural validation~~ Ã¢â‚¬â€ Closed by Slice 10 (test-tmx-integrity.ps1).

3. ~~palette_sha256 not verified~~ Ã¢â‚¬â€ Closed by test-palette-sha256.ps1.

4. The contract test validates the artifact structure but does not validate
   field types beyond what ConvertFrom-Json infers. A JSON Schema validator
   (against schemas/pzmapforge.parsed-cell.v0.1.schema.json) would close this gap.

| Local PZ install validator (Slice 3A-2) | Ratified | Read-only filesystem validation; checks install root, tiles root, extension counts, and safety flags; no asset content reading/copying; no media/maps writes |
## Slice 3A-3 local tile reference survey artifact

| Capability | Status | Evidence |
|---|---|---|
| Local tile reference survey artifact writer (Slice 3A-3) | Ratified | Writes .local/local-tile-reference-survey.json and .local/local-tile-reference-survey.md from LocalPzInstallValidationResult summary data only; no CLI command; no tile catalog; no asset content reading/copying; no media/maps writes; no playable export claim |
| Proof packet v0.15 (Slice 3A-3 sync) | Ratified | Schema file sanity 196; proof packet 96; PS total 468; .NET total 225; LocalTileReferenceSurveyWriter evidence flags added |
