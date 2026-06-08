# Map Export Contract

```text
Status: MAP-0 contract discovery
Claim boundary: planning_artifact_only_not_pz_load_tested
Playable export status: not implemented
PZ assets: not read or copied
media/maps writes: forbidden in repo
Output target for future experiments: .local only
```

---

## 1. Purpose

This document defines the contract areas PZMapForge must satisfy before it can
truthfully claim playable Project Zomboid map export. It separates known facts
from unknowns, identifies the smallest useful playable proof target, and
preserves the current safety boundary.

MAP-0 is contract discovery. It does not implement export.

---

## 2. Current boundary

PZMapForge currently produces planning artifacts only:

```text
PNG or BMP blockout
  -> semantic 300x300 grid
  -> parsed-cell.json (planning evidence)
  -> parsed-cell-report.md
  -> parsed-cell-preview.png
  -> parsed-cell-tiles.png
  -> parsed-cell-basic.tmx (TileZed-openable planning artifact)
```

No playable Project Zomboid map is produced. No PZ game files are read or
copied. No writes occur to any media/maps directory. No lotpack, lotheader, or
bin files are generated.

See [CLAIM_BOUNDARY.md](CLAIM_BOUNDARY.md) for the full current boundary.

---

## 3. Why PZMapForge should replace TileZed + WorldEd

TileZed and WorldEd are GUI tools that require manual round-trips, produce
opaque binary outputs, and give no deterministic compile step. The operator
cannot verify what was generated or replay the process.

PZMapForge targets the same outcome through a different contract:

- Deterministic: same input always produces the same output.
- Inspectable: every artifact is a text or JSON file with a schema.
- Evidence-bearing: each run records provenance and safety flags.
- Model-replaceable: no AI or model is required; the pipeline is code.
- Dry-run by default: no write occurs without an explicit execute flag.

This is a future target. MAP-0 does not replace TileZed or WorldEd.

---

## 4. What TileZed / WorldEd currently provide

Documented from public knowledge and local survey. Evidence required for
complete accuracy.

| Capability | Tool | Status |
|---|---|---|
| TMX layer editor (visual) | TileZed | In use as planning viewer |
| World-level cell grid | WorldEd | Not used; future research target |
| lotpack / lotheader / bin export | WorldEd | Not implemented in PZMapForge |
| Spawn point editor | TileZed/WorldEd | Not implemented in PZMapForge |
| Tileset assignment | TileZed | PROVISIONAL in PZMapForge (planning tiles only) |
| In-game map feature files | WorldEd | Not implemented in PZMapForge |
| Chunk/cell coordinate system | TileZed/WorldEd | Requires proof; not implemented |

---

## 5. PZMapForge target pipeline

```text
PZMapForge source
  -> deterministic validation
  -> dry-run export plan
  -> explicit local execute
  -> generated local experimental map mod
  -> manual Project Zomboid load test
  -> evidence record
```

Every future compiler command must be dry-run by default. Any write to
playable-style output must require an explicit `--execute` flag. This flag
does not exist yet.

**`map-plan` command (MAP-2):**
The `map-plan` CLI command is the first step in the future pipeline. It reads a
`pzmapforge.map-source.v0.1` JSON file and produces a deterministic dry-run
export plan artifact. It is dry-run only.

Command:
```
dotnet run --project src/PZMapForge.Cli -- map-plan \
  --source examples/map-source/minimal-cell.json \
  --output .local/map-plan/minimal-cell
```

Written files (only these two):
- `map-export-plan.json` — machine-readable plan artifact
- `map-export-plan.md` — human-readable plan report

Not written:
- No compiled cell files.
- No mod.info.
- No spawn definition.
- No media/maps directory.
- No playable Project Zomboid output.
- No PZ assets read or copied.
- No execute flag exists. No local mod scaffold is created.

**MAP-3A — scaffold contract added to map-plan output:**
MAP-3A extends the map-plan JSON and Markdown with a future text-only scaffold
contract. The contract lists the files a future MAP-3B writer would create, but
does not write them. MAP-3A is contract evidence only.

The scaffold contract section in map-export-plan.json includes:
- `scaffold_contract_version: "0.1"`
- `text_only_scaffold_supported_now: false`
- `text_only_scaffold_written: false`
- `scaffold_execute_supported: false`
- `future_scaffold_files` array — each item has `written_now: false`

**MAP-3B — text-only local mod scaffold writer (implemented):**
MAP-3B adds the `map-scaffold` CLI command. It reads a
`pzmapforge.map-source.v0.1` JSON source file and writes exactly four
text-only scaffold files under a caller-provided `.local` output directory.

Command:
```
dotnet run --project src/PZMapForge.Cli -- map-scaffold \
  --source examples/map-source/minimal-cell.json \
  --output .local/map-scaffold/minimal-cell
```

Written files (exactly these four):
- `<output>/mod.info`
- `<output>/media/maps/<map_id>/map.info`
- `<output>/media/maps/<map_id>/spawnpoints.lua`
- `<output>/media/maps/<map_id>/README_PZMAPFORGE_BOUNDARY.txt`

Safety guarantees:
- Output must be under a `.local/` directory. All other paths are refused.
- Output path itself must not contain `media/maps`. The command writes
  `media/maps/<map_id>/` only inside the provided `.local` root.
- No compiled cell files written (no .lotpack, .lotheader, .bin, .tmx, .pzw).
- No worldmap files written.
- No PZ assets read or copied.
- No coordinate math performed.
- No SVG geometry converted.
- Not load-tested. Not a playable Project Zomboid map.
- Every generated file contains explicit boundary language.

**MAP-3C — map-scaffold smoke script:**
MAP-3C adds `scripts/smoke-map-scaffold-minimal.ps1`, a local-only smoke helper
that proves `map-scaffold` writes the expected four text scaffold files and
preserves the non-playable boundary. It runs 16 assertions against the real
command output. All output stays under `.local/`. The script is a standalone
helper; it is not wired into `validate.ps1`.

Command:
```
powershell -ExecutionPolicy Bypass -File "scripts\smoke-map-scaffold-minimal.ps1"
```

Assertions (16):
- source file present
- map-scaffold exits 0
- mod.info, map.info, spawnpoints.lua, README_PZMAPFORGE_BOUNDARY.txt each exist
- exactly 4 files written
- no compiled output extensions (.lotpack/.lotheader/.bin/.tmx/.pzw)
- boundary language in files: Text-only scaffold, Not playable, No PZ assets, Not load-tested
- stdout: text_only_scaffold_written/compiled_outputs_written/playable_export_generated/pz_assets_read_or_copied

**MAP-4A — compiled cell format evidence inventory (implemented):**
MAP-4A adds the compiled cell format evidence inventory process. It does not
implement any compiled writer. It provides the docs, template, and inspector
script that the operator uses to close the evidence gaps listed in
docs/COMPILED_CELL_FORMAT_EVIDENCE.md section 5 before MAP-4 begins.

Artifacts:
- `docs/COMPILED_CELL_FORMAT_EVIDENCE.md` — evidence gap specification; lists
  all 10 gaps that must be CLOSED before MAP-4 implementation is permitted.
- `docs/examples/compiled-cell-evidence/COMPILED_CELL_EVIDENCE_TEMPLATE.md` —
  fillable observation template; operator saves filled copy to `.local/`.
- `scripts/inspect-compiled-cell-evidence.ps1` — local-only enumerator;
  accepts `-Path <local dir> -Output <.local dir>`; enumerates file names,
  extensions, sizes, SHA-256 hashes; writes `compiled-cell-evidence.json` and
  `compiled-cell-evidence.md` under `.local/`; refuses non-.local output;
  does not copy files; safety flags: `copied_input_files: false`,
  `pz_assets_copied: false`, `media_maps_touched: false`,
  `playable_export_claimed: false`, `compiled_writer_implemented: false`.
- `scripts/validate.ps1` — MAP-4A inline contract section added (5 checks:
  doc/script/template exist, script contains .local refusal and
  copied_input_files sentinel); no Assert-True used, PS lane stays 492.

**MAP-4B — compiled cell evidence summaries recorded:**
MAP-4B records two local Workshop mod inventories in
`docs/COMPILED_CELL_FORMAT_EVIDENCE.md`. No files were copied into the repo.
No binary content was parsed. No compiled writer was implemented.

Observations:
- Laval-Montreal workshop: 5x5 grid, coords 0_0 to 4_4, 25 cells each of
  .lotheader / world_*.lotpack / chunkdata_*.bin, plus map.info, spawnpoints.lua,
  objects.lua, worldmap.xml.bin.
- RED-Speedway workshop: 2x3 grid, coords 25_15 to 26_17, 6 cells each of
  .lotheader / world_*.lotpack / chunkdata_*.bin, plus map.info, spawnpoints.lua,
  objects.lua.

Gap status updates (PARTIAL only — not CLOSED):
- Cell coordinate naming: PARTIAL (two observations confirm `<cx>_<cy>` pattern).
- Directory layout: PARTIAL (flat layout under `media/maps/<map_id>/` confirmed).
- map.info presence: PARTIAL (file present; content not read).
- spawnpoints.lua presence: PARTIAL (file present; content not read).

Still OPEN: .lotheader binary format, .lotpack binary format, minimum viable
cell count, single-cell load test, spawn coordinate system, Build 41/42 differences.

**MAP-4C — map text metadata evidence reader:**
MAP-4C adds `scripts/inspect-map-text-metadata.ps1`, which reads safe text
files (mod.info, map.info, spawnpoints.lua, objects.lua) from an
operator-provided local path and writes evidence JSON and Markdown under
`.local/` only. No binary files are read. No files are copied.

Script: `scripts/inspect-map-text-metadata.ps1 -Path <dir> -Output <.local dir>`

Outputs: `map-text-metadata-evidence.json`, `map-text-metadata-evidence.md`

Gap status updates from running against both Workshop mods (PARTIAL only):
- Spawn file format: PARTIAL — format pattern confirmed: profession-keyed Lua
  table with `worldX`/`worldY`/`posX`/`posY`/`posZ` fields.
- Spawn coordinate system: PARTIAL — `worldX`/`worldY` appear to be cell-grid
  coordinates; `posX`/`posY`/`posZ` appear to be in-cell position.
- `map.info` fields: PARTIAL — title/lots/description/fixed2x observed; required
  vs optional not confirmed.

Still OPEN: .lotheader binary format, .lotpack binary format, minimum viable
cell count, single-cell load test, Build 41/42 differences.

**MAP-4D — compiled binary header evidence probe:**
MAP-4D adds `scripts/inspect-compiled-binary-headers.ps1`, which reads only
bounded byte prefixes (default 64 bytes, max 256) from compiled map files
(.lotheader, .lotpack, .bin). No files are copied. Output is hex strings only
under `.local/`. No full binary content is read.

Script: `scripts/inspect-compiled-binary-headers.ps1 -Path <dir> -Output <.local dir> -MaxBytes 64`

Outputs: `compiled-binary-header-evidence.json`, `compiled-binary-header-evidence.md`

Binary prefix observations (10 files per type, 2 mods, 64-byte prefixes):
- `.lotheader`: bytes 0-3 = `00000000` (consistent); bytes 4-7 = 32-bit LE variable
  integer (appears to be tileset entry count); bytes 8+ = newline-separated ASCII
  tileset pack names. Hypothesis only — writing not permitted.
- `.lotpack`: first 8 bytes = `84030000241c0000` IDENTICAL across all 10 sampled
  files from both mods; bytes 8+ = apparent offset/size table. Full format not decoded.
- `chunkdata .bin`: bytes 0-1 = `0001` (consistent); bytes 2+ variable.

Gap advances (PARTIAL only — not CLOSED):
- `.lotheader` binary format: OPEN → PARTIAL.
- `.lotpack` binary format: OPEN → PARTIAL.

Still OPEN: full .lotheader semantics, full .lotpack format, minimum viable
cell count, single-cell load test, Build 41/42 differences.

**MAP-4E — lotheader string table evidence probe:**
MAP-4E adds `scripts/inspect-lotheader-string-table.ps1`, which reads `.lotheader`
files (not `.lotpack` or `.bin`) and extracts the candidate tileset string table
for evidence. No files are copied. Output is under `.local/` only.

Script: `scripts/inspect-lotheader-string-table.ps1 -Path <dir> -Output <.local dir>`

Evidence from 16 files across 2 mods:
- Bytes 0-3: `00000000` — consistent in 16/16 files.
- Bytes 4-7: 32-bit LE integer = entry count — exact match in 14/16 (87.5%).
- Bytes 8+: newline-delimited ASCII tileset pack+sprite name entries (31–2450 per cell).
- 2 count mismatches in complex Laval cells have embedded non-printable bytes,
  suggesting a secondary data section in some lotheaders.

Writing is not permitted. The 2 mismatches and the role of embedded non-printable
bytes in complex cells remain unexplained.

**MAP-4F — lotpack offset table evidence probe:**
MAP-4F adds `scripts/inspect-lotpack-offset-table.ps1`, which reads bounded
`.lotpack` prefixes (not `.lotheader` or `.bin`) and analyses the apparent
chunk offset table. No files are copied. Output is under `.local/` only.

Script: `scripts/inspect-lotpack-offset-table.ps1 -Path <dir> -Output <.local dir>`

Evidence from 16 files across 2 mods (10 Laval + 6 RED-Speedway):
- hdrA (bytes 0-3) = 900 — CONSTANT, 16/16 files. Matches 30×30 = 900 chunks/cell.
- hdrB (bytes 4-7) = 7204 — CONSTANT, 16/16 files. Formula exact: 4 + 900×8 = 7204.
- Bytes 8–7207: 900-entry offset table; each 8-byte entry = {0x00000000, chunk_offset_U32}.
- Chunk offsets monotonically increasing; variable per cell (city) or constant (uniform).
- Gap section between table end (byte 7208) and first chunk data is 1204–1432 bytes
  per observed file. Role unknown.

Gap advances (PARTIAL only — not CLOSED):
- `.lotpack` binary format: PARTIAL (header+table structure well-supported).

Still OPEN: gap section content, chunk data encoding, how to write valid chunk data.

MAP-4 remains blocked. The decision gate in section 8 of
`docs/COMPILED_CELL_FORMAT_EVIDENCE.md` is not satisfied.

**MAP-4G — chunkdata binary pattern evidence probe:**
MAP-4G adds `scripts/inspect-chunkdata-binary-patterns.ps1`, which reads bounded
`chunkdata_*.bin` prefixes (not `.lotheader` or `.lotpack`) and records byte-
pattern evidence. No files are copied. No binary files are written.

Script: `scripts/inspect-chunkdata-binary-patterns.ps1 -Path <dir> -Output <.local dir>`

Evidence from 16 chunkdata_*.bin files across 2 mods:
- First 2 bytes = `00 01` — CONSTANT, 16/16 files, both mods.
- Minimum file size = 902 bytes = 2-byte header + 900-byte (30×30 chunk grid).
  This is an exact match to the PZ cell architecture (300/10 × 300/10 = 900 chunks/cell).
- Simple grass cells (902 bytes): chunk grid (bytes 2-901) all zero.
- Complex cells: chunk grid has nonzero per-chunk flags (0x02, 0x03, 0x08 observed)
  plus variable additional data beyond byte 901.

Gap advance (PARTIAL only — not CLOSED):
- `chunkdata_*.bin` format: OPEN → PARTIAL.

Still OPEN: chunk grid byte semantics, extended section format, whether minimal
902-byte file is valid for PZ load.

**MAP-4H — compiled writer decision gate report:**
MAP-4H adds `docs/MAP_4H_COMPILED_WRITER_DECISION_GATE.md`, a formal decision
gate report assessing whether structural evidence collected in MAP-4A through
MAP-4G is sufficient to begin an experimental compiled cell writer slice.

```text
DECISION: MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY
```

The decision authorizes a strictly bounded experimental slice (MAP-5A) to write
one minimal all-empty cell under `.local/` only. Rationale:

- lotheader structure is well-supported (0-entry hypothesis for blank cell).
- lotpack header+table structure is confirmed (zero-offset assumption for empty chunks).
- chunkdata 902-byte all-zero pattern matches observed simple grass cells.
- Remaining unknowns (lotpack gap section, single-cell load viability) can only
  be resolved by attempting a write and performing a manual load test.
- Local-only output makes the experiment safe.

Primary risks accepted: lotpack zero-offset assumption may be wrong; single cell
may not load without a world grid. Both risks produce diagnostic information.

Required safeguards (all mandatory for MAP-5A):
- CLI command name must include "experimental".
- Output under `.local/` only; refuse PZ install paths and repo media/maps.
- Boundary README in every generated file set.
- All writer assumptions logged in output report.
- No playable export claim until manual load test is performed and documented.
- No PZ assets read or copied.

**No playable export claim.** MAP-5A is hypothesis-testing only.

**MAP-5A — experimental local compiled empty cell writer (implemented):**
MAP-5A adds the `map-export-experimental` CLI command. It writes one minimal
compiled empty cell under `.local/` only, per MAP-4H authorization.

Command:
```
dotnet run --project src/PZMapForge.Cli -- map-export-experimental \
  --map-id <id> --output .local/map-export-experimental/<name>
```

Writes exactly 10 files (7 text, 3 binary):
- `mod.info`, `media/maps/<id>/map.info`, `spawnpoints.lua`, `objects.lua`
- `README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt` (mandatory boundary)
- `<cx>_<cy>.lotheader` — 8 bytes: zero header + 0-entry count hypothesis
- `world_<cx>_<cy>.lotpack` — 7208 bytes: hdrA=900, hdrB=7204, all-zero offset table
- `chunkdata_<cx>_<cy>.bin` — 902 bytes: 0x0001 header + 900 zero bytes
- `experimental-map-export-report.json`, `experimental-map-export-report.md`

Report JSON flags: `playable_export_generated: false`, `load_tested: false`,
`experimental_writer: true`, `pz_assets_copied: false`, `manual_load_test_required: true`.

Safeguards enforced: refuses non-.local output, refuses PZ install paths,
refuses repo media/maps, boundary README in every output set, all assumptions
logged in report.

**No load test has been performed. No playable export claim.**
Manual load test required before any claim changes.

**MAP-5B — experimental map load test record protocol:**
MAP-5B adds the protocol and tooling for the manual MAP-5A load test.

Artifacts:
- `docs/MAP_5B_MANUAL_LOAD_TEST_PROTOCOL.md` — load test protocol and step-by-step guide.
- `docs/examples/manual-load-test/MAP_5B_LOAD_TEST_RECORD_TEMPLATE.md` — master fillable record template.
- `scripts/prepare-map-export-experimental-load-test.ps1` — validates MAP-5A output and writes a load-test packet (instructions + per-run record template) under `.local/` only. Does not copy files to PZ. Does not claim playable export.

The packet script (`-Source <.local map-export-experimental dir> -Output <.local load-tests dir>`) verifies all 8 expected source files, validates report safety flags, and writes `MAP_5B_LOAD_TEST_PACKET.md` and `MAP_5B_LOAD_TEST_RECORD.local-template.md`.

**No playable export claim.** Load test must be performed manually by the operator.
Results are recorded in the local template and reviewed before any claim changes.

**MAP-5C — Build 42 mod packaging discovery record:**
MAP-5C records the MAP-5B LOAD_TEST_INCONCLUSIVE result and adds tooling to
diagnose Build 42 mod package structure.

MAP-5B result: **LOAD_TEST_INCONCLUSIVE** — packaging/discovery blocker on Build 42.
The generated binary map files (`.lotheader`, `.lotpack`, `chunkdata_*.bin`) were
**not tested**. The blocker is mod discovery, not binary format.

Key findings from inspection:
- `PZMapForgeEmptyCellTest` Workshop package has correct Build 42 structure
  (all 5 expected paths present, correct `mod.info` fields).
- Mod still did not appear in Build 42 Mods screen.
- Possible cause: Build 42 Workshop mods may require Steam subscription/download
  rather than manual folder placement.
- Binary hypotheses from MAP-5A remain untested.

Artifacts:
- `docs/MAP_5C_BUILD42_MOD_PACKAGING_DISCOVERY.md` — full inconclusive record,
  packaging structure analysis, diagnostic findings.
- `scripts/inspect-build42-mod-package.ps1` — compares a package against the
  ModTemplate; reads workshop.txt/mod.info/map.info; outputs inspection JSON + MD
  under `.local/` only; does not copy or modify files.

**MAP-5D — Build 42 experimental package writer:**
MAP-5D adds `--build42-package` to `map-export-experimental`. When set, the
command generates a correct Build 42 Workshop-style nested package under
`.local/<output>/<map_id>_build42_workshop/` instead of the MAP-5A flat layout.

Package layout matches the PZ ModTemplate (`Contents/mods/<id>/` nested structure):
`workshop.txt`, `preview.png`, `Contents/mods/<id>/mod.info` (with `category=map`,
`modversion=1.0`, `pzversion=42.0`, `versionMin=42.0`), `poster.png`, `thumb.png`,
and all compiled binary files at `Contents/mods/<id>/media/maps/<id>/`.

Report includes `package_layout: "build42_workshop"`.
Placeholder PNGs are generated via System.Drawing (no PZ assets read or copied).
MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.

**MAP-5E — Build 42 experimental package self-inspection command:**
MAP-5E adds `inspect-build42-experimental-package --package <dir> --output <.local dir>`.

The command self-inspects a MAP-5D generated package: reads the embedded
`experimental-map-export-report.json` to discover `map_id`/`cell_x`/`cell_y`,
then runs 21 checks (structure, mod.info fields, binary sizes and headers,
report flags, file count). Exits 0 if all checks pass, 1 if any fail.

Writes `build42-experimental-package-inspection.json` and `.md` under `--output`.
No files are copied. No PZ assets read. Output is under `.local/` only.
MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.

**MAP-5F — Inspector path hardening + Build 42 manual load-test packet:**
MAP-5F adds a `.local/` guard to `--package` in `inspect-build42-experimental-package`
(previously only `--output` was guarded). Also adds
`scripts/prepare-build42-load-test-packet.ps1` which validates a MAP-5D package,
verifies all required files are present, and writes `BUILD42_LOAD_TEST_PACKET.md`
(step-by-step copy/test instructions) and `BUILD42_LOAD_TEST_RECORD.local-template.md`
(fillable result template) under `.local/` only. Does NOT copy files to PZ folders.
MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.

**MAP-6A — Build 42 versioned discovery proof + spawn-region test packet:**
MAP-6A records manual evidence that the Build 42 versioned loose-mod layout
(`<mods>/<folder>/42/mod.info`) successfully loads a PZMapForge-generated mod.
The spawn-region test packet (maptest_a variant) confirmed the custom spawn
location became visible. Binary hypothesis status from MAP-6A was PLAUSIBLE
(no crash at registration); this was superseded by MAP-6B runtime evidence.

Artifacts:
- `docs/MAP_6A_BUILD42_VERSIONED_DISCOVERY_PROOF.md` — full evidence record,
  confirmed routes, spawn-selection observation, binary hypothesis status
  (superseded by MAP-6B), gap analysis, required proof for playable export claim.
- `scripts/prepare-spawn-region-test-packet.ps1` — generates versioned layout
  copy + three spawn-coord variants (cell 0,0 / 1,1 / 25,15) + instruction packet
  + record template, all under `.local/` only. Does NOT copy to PZ folders.

**MAP-6B — Build 42 binary format failure record:**
MAP-6B records the runtime failure evidence when PZ attempted to load the
placeholder binary cell files generated by MAP-5A.

Runtime failures confirmed:
- `0_0.lotheader`: `java.io.EOFException` at `IsoLot.readInt`. The 8-byte
  placeholder (zero header + U32 LE 0 entry count) is definitively invalid.
- `CellLoader` / `IsoCell.PlaceLot`: repeated failures, downstream of lotheader.
- `objects.lua`: `LuaManager.RunLuaInternal` exception. Comment-only file rejected.

Status labels:
```text
DISCOVERY_PASS_VERSIONED_LAYOUT     — from MAP-6A, confirmed
MAP_FILES_DISCOVERED_BY_PZ          — from MAP-6A, confirmed
BINARY_FAILURE_CONFIRMED            — MAP-6B
OBJECTS_LUA_FAILURE_CONFIRMED       — MAP-6B
PLAYABLE_EXPORT_CLAIM_ALLOWED=false — binding
```

Report JSON runtime status fields added to experimental-map-export-report.json:
- `binary_runtime_status = failing_placeholder_format`
- `lotheader_runtime_status = eof_exception_observed`
- `lotpack_runtime_status = unproven_after_lotheader_failure`
- `chunkdata_runtime_status = unproven_after_lotheader_failure`
- `objects_lua_runtime_status = invalid_or_not_accepted`

Artifacts:
- `docs/MAP_6B_BINARY_FORMAT_FAILURE_RECORD.md` — full failure record.

No playable export claim. BINARY_FAILURE_CONFIRMED. Real binary format
implementation is the next required step before any further load test.

**MAP-6C — Lotheader format research packet and candidate writer gate:**
MAP-6C adds a research packet documenting the lotheader format based on MAP-4E
evidence, adds a `--lotheader-candidate` flag to `map-export-experimental`, and
fixes `objects.lua` from comment-only to a syntactically valid empty Lua file.

Candidate matrix:
- `current_failed` (default): 8-byte all-zero placeholder, known failing (MAP-6B).
- `newline_tileset_table`: MAP-4E format model (version+count+entries), 0 entries
  for empty cell. Bytes are identical to `current_failed` for the 0-entry case.
  Status: `generated_not_load_tested`.
- `v2_length_prefixed_or_int_table`: NOT IMPLEMENTED — insufficient evidence.

Report JSON fields added:
- `lotheader_candidate`, `lotheader_candidate_status`
- `lotheader_sha256`, `lotheader_first_bytes`, `lotheader_byte_count`
- `binary_runtime_status` = `"candidate_generated_not_load_tested"` for newline_tileset_table
- `objects_lua_runtime_status` = `"syntax_candidate_not_load_tested"` (was invalid_or_not_accepted)

Artifacts:
- `docs/MAP_6C_LOTHEADER_FORMAT_RESEARCH_PACKET.md` — research packet.

No playable export claim. No load test. No PZ assets.

**MAP-6D — Non-empty lotheader candidate from committed evidence:**
MAP-6D adds `--lotheader-candidate newline_tileset_table_minimal`, the first
lotheader candidate that produces genuinely different (non-empty) bytes.

Entry source: `blends_grassoverlays_01_0` — documented in `docs/COMPILED_CELL_FORMAT_EVIDENCE.md`
section 16 (MAP-4E). No PZ assets read or copied.

Byte layout: `00 00 00 00 01 00 00 00` + ASCII entry + `\n` = 34 bytes total.

New report fields: `lotheader_entry_count`, `lotheader_entries` (both paths).
`lotheader_first_bytes` widened to 32 bytes for all candidates.

Artifacts:
- `docs/MAP_6D_NONEMPTY_LOTHEADER_CANDIDATE.md` — candidate record.

No playable export claim. No load test. No PZ assets.

**MAP-7V — K004/K006 control results and binary gate decision:**
MAP-7V records two human-only control tests that close the current binary-format
investigation branch.

K004 (coordinate-aligned): binaries present at `35_27.*`, spawnpoint honored
(warning at 10746,8288,0), but fallback forest and empty map scan persisted.

K006 (zero-binary control): zero PZMapForge lotheader/lotpack/chunkdata files,
Workshop Ready, mod loaded, spawn honored — same fallback forest result.
SANITY CHECK FAIL occurred but is NOT a binary parse error (no binaries present).

Key conclusions:
- Binary presence vs binary absence produces the same fallback-forest outcome.
- All previous fallback-forest results were map-registration evidence, not
  binary-format evidence. The fallback forest was never binary proof.
- `BINARY_FORMAT_INVESTIGATION_PAUSED`
- Next branch: runtime map registration / map folder mounting.

Status labels:
```text
MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED
MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED
BINARY_FORMAT_INVESTIGATION_PAUSED
RUNTIME_MAP_REGISTRATION_IS_NEXT_BRANCH
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No Steam Workshop upload.
No PZ assets outside allowed roots. No forbidden writes.

**MAP-7U — Mod-root layout match and coordinate-aligned diagnostic:**
MAP-7U records that the PZMapForge candidate mod-root layout now exactly
matches the Dru_map reference (`layout_match=True`, zero BOM violations,
zero field gaps). The remaining discriminator is cell coordinates and spawn
alignment: candidate uses a single cell at origin `0_0` with zoom offset `(0,0)`,
while Dru_map uses 4130 cells centered at `(35,27)` with zoom offset `(10505,12220)`.

New tool: `scripts/inspect-build42-workshop-cell-coordinate-contract.ps1` compares
lotheader cell counts/ranges, map.info zoom fields, and spawnpoints coordinates
between two mod roots. Output is `.local/` only.

New staged package: `scripts/prepare-build42-map7u-coordinate-discriminator-packet.ps1`
renames binary files `0_0` → `35_27` (content unchanged), updates map.info zoom
to match Dru_map reference (`zoomX=10505, zoomY=12220, zoomS=14.5`), and updates
spawnpoints to `worldX=35, worldY=27`. Binary writer behavior is unchanged.

Binary writer gate remains closed:
`BINARY_WRITER_GATE_STILL_CLOSED` — do not mutate LOTH/LOTP/chunkdata until
`expected_map_lotheader_meta_evidence_found=true`.

Status labels:
```text
MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED
COORDINATE_DISCRIMINATOR_IDENTIFIED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
```

No load test. No binary writer change. No Steam Workshop upload.
No PZ assets outside allowed explicit roots. No forbidden writes.

**MAP-7T — Workshop K002 runtime payload comparison:**
MAP-7T records K002 (Workshop ID 3740642200): the PZMapForge Workshop item now
downloads, reaches Installed/Ready, and loads the mod — but still does not
produce expected-map lotheader/meta evidence or a visible custom map world.

New tool: `scripts/inspect-build42-workshop-runtime-payload.ps1` compares two
explicit operator-provided Workshop payload roots (candidate and reference).
Detects: mod.info locations (root/42/common/mods/Contents), common/media/maps
presence, map.info fields (lots=NONE, zoomX/Y), binary file names and sizes,
BOM violations. Output is `.local/` only; reads only the provided paths.

Binary writer gate remains closed:
`BINARY_WRITER_GATE_STILL_CLOSED` — do not mutate LOTH/LOTP/chunkdata until
`expected_map_lotheader_meta_evidence_found=true`.

Status labels:
```text
MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED
K002_WORKSHOP_ITEM_INSTALLED_READY
K002_MOD_LOADED_NO_EXPECTED_MAP_EVIDENCE
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No Steam Workshop upload.
No PZ assets outside allowed explicit roots. No forbidden writes.

**MAP-7S — Private Workshop upload staging packet:**
MAP-7S prepares the human-only private/unlisted Workshop upload staging packet
for the PZMapForge candidate.

The staging script (`scripts/prepare-build42-map7s-private-workshop-staging-packet.ps1`)
generates the `empty_grass_v4` candidate via the dotnet CLI and stages it under
`.local/staged-workshop/<MapId>/` using the Dru_map-aligned layout (MAP-7O contract):
root `mod.info` + `42/mod.info` + NO `common/mod.info` + `common/media/maps/<MapId>/`.

Nothing is uploaded to Steam Workshop. No PZ load test is performed.
No binary writer behavior is changed.

Key checklist requirements (all HUMAN-ONLY):
- Create a NEW private/unlisted Workshop item — do NOT use `3355966216` (Dru_map's ID).
- Upload staged package and record the new Workshop ID.
- Wire server: `Mods=pzmapforge_build42_candidate_v4_001 + WorkshopItems=<PZMapForgeOwnWorkshopId>`.
- Analyze with `-ExpectedMapId pzmapforge_build42_candidate_v4_001 -VariantLabel VariantWSUpload`.

Success condition: `expected_map_lotheader_meta_evidence_found=true`
OR visible custom PZMapForge built world (not fallback forest).

If success: binary writer gate opens — LOTH/LOTP/chunkdata validation becomes the focus.

Status labels:
```text
MAP7S_WORKSHOP_STAGING_PACKET_CREATED
NO_AUTOMATIC_WORKSHOP_UPLOAD
STAGED_PACKAGE_LOCAL_ONLY
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No Steam Workshop upload.
No PZ assets outside .local. No forbidden writes.

**MAP-7R — Variant J borrowed WorkshopItems trigger failure:**
MAP-7R records Variant J: adding `WorkshopItems=3355966216` (Dru_map's Workshop ID)
while keeping PZMapForge as a local loose mod did not mount the PZMapForge
candidate as a custom map. The Workshop ID activated Dru_map's runtime path,
not the PZMapForge candidate's path.

Analyzer updated: new field `expected_map_lotheader_meta_evidence_found` detects
whether the expected map ID appears near a `.lotheader` reference in the log.
Generic lotheader lines (Muldraugh/vanilla) do not satisfy this condition.

New classification: `MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT` fires when
`VariantLabel=VariantJ` AND Workshop Installed/Ready present AND candidate mod
loaded AND game reached AND no expected-map lotheader evidence.

Exhausted paths:
- Static layout variants A through I
- Borrowed WorkshopItems trigger J

Next: real candidate Workshop-style activation (private/unlisted Workshop upload).
This requires explicit operator approval and a separate MAP task. No automatic upload.

Binary writer gate: do not investigate LOTH/LOTP/chunkdata until PZMapForge
candidate reaches expected-map lotheader/meta evidence.

Status labels:
```text
MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT
BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED
STATIC_VARIANTS_ABCDEFGHI_EXHAUSTED
NO_MORE_STATIC_LAYOUT_TESTS
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No Steam Workshop upload.
No PZ assets outside .local. No forbidden writes.

**MAP-7Q — Dru_map runtime baseline success and corrected evidence model:**
MAP-7Q records the Dru_map baseline as runtime successful and corrects the
Build 42 analyzer evidence model.

The MAP-7P analyzer produced `MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY`.
The human result was unambiguously successful: player spawned into a real built
Drummondville/Dru_map world with roads and houses visible.

Key finding: empty printed client map-folder scan is NOT decisive for Build 42
coop/server with Workshop-activated mods. (`EMPTY_CLIENT_SCAN_NOT_DECISIVE`)

New classification: `MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS` fires when
`VariantLabel=DruMapBaseline` AND all runtime success signals are present
(Workshop Installed/Ready + expected mod loaded + lotheader evidence +
player data + multiplayer), even if `map_folders_list_empty=true`.

New report fields: `expected_mod_loaded`, `workshop_installed_seen`,
`workshop_ready_seen`, `multiplayer_reached`, `lotheader_meta_evidence_found`,
`lotheader_meta_paths_or_names`, `runtime_success_evidence_found`,
`empty_client_map_folder_scan_decisive`, `visual_confirmation_required`.

Analyzer schema: v0.3 -> v0.4.

Next investigation target: runtime activation / mounting. The discriminator
between Dru_map (works) and PZMapForge candidate (does not) is the Workshop
subscription/download/Installed/Ready flow. PZMapForge candidate must reach
lotheader/meta load evidence before binary writer quality becomes relevant.

Status labels:
```text
MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS
EMPTY_CLIENT_SCAN_NOT_DECISIVE
DRUMAP_BASELINE_RUNTIME_SUCCESSFUL
VARIANTS_ABCDEFGHI_EXHAUSTED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No automatic Workshop upload.
No PZ assets outside .local. No forbidden writes.

**MAP-7P — Variant I failure record and known-working runtime baseline:**
MAP-7P records the Experiment I (Variant I) failure result and pivots diagnostic
focus from static layout to runtime activation.

Experiment I result: `MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY`. All nine layout
variants A through I are exhausted (`VARIANTS_ABCDEFGHI_EXHAUSTED`). The
Dru_map-aligned static structure (root mod.info, no common/mod.info,
common/media/maps/, lots=NONE, zoomX/Y/S) was not sufficient for IsoMetaGrid
to scan and register the candidate map folder.

The new diagnostic is a known-working runtime baseline using Dru_map (Workshop
ID: 3355966216). If Dru_map appears in the IsoMetaGrid map folder scan, the
runtime pipeline can discover Workshop mods. The PZMapForge candidate then lacks
a runtime activation condition that Dru_map satisfies.

Baseline server wiring:
```text
Mods=Dru_map
WorkshopItems=3355966216
Map=Dru_map;Muldraugh, KY
Public=false
```

Analyzer updated: DruMapBaseline-specific classifications added to
`inspect-build42-map7d-load-result.ps1`:
- `MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND`: non-empty scan with expected map.
- `MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY`: empty scan.

Packet script: `scripts/prepare-build42-map7p-known-working-runtime-baseline-packet.ps1`
Writes 7 packet files under `.local/` only. Does not run PZ. Does not write to
Workshop, mods, Server, or PZ install paths. Does not copy Dru_map automatically.

Status labels:
```text
MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY
VARIANTS_ABCDEFGHI_EXHAUSTED
DRUMAP_BASELINE_DIAGNOSTIC_REQUIRED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets outside .local.
If Dru_map scan found but PZMapForge scan empty: next task is runtime
activation contract alignment, not binary writing.

**MAP-7O — Dru_map-aligned Experiment I preparation:**
Experiment I contract: root `mod.info` + `42/mod.info` + NO `common/mod.info` +
`common/media/maps/<MapId>/` + `lots=NONE` + `zoomX/zoomY/zoomS` in map.info.
Exact Dru_map layout mirrored for first time. Discovery inspector updated v0.4
with `has_drumap_aligned_layout` and `common_mod_info_absent`.

Status labels:
```text
DRUMAP_ALIGNED_EXPERIMENT_I_PREPARED
EXPERIMENT_I_USES_ROOT_MOD_INFO_NO_COMMON_MOD_INFO
MAP_INFO_LOTS_NONE
MAP_INFO_ZOOM_FIELDS_ADDED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets outside .local.
Next human action: install experiment-I candidate; run Experiment I retest.

**MAP-7N — Reference map ID support and Dru_map comparison:**
Comparator patched: `-ReferenceMapId` parameter added (defaults to `-MapId`).
Candidate uses `-MapId`, reference uses `-ReferenceMapId`.
Dru_map (known-working Build 42 mod) copied to `.local/` and compared.
Dru_map layout: root mod.info + 42/mod.info + common/media/maps/Dru_map/ (same layout as experiment-H).

```text
REFERENCE_MAP_ID_SUPPORT_ADDED
DRU_MAP_COMPARISON_EXECUTED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets outside .local.

**MAP-7M — Variant H failure and known-working map contract comparator:**
Variant H: common/media/maps layout -- still empty scan. `VARIANTS_ABCDEFGH_EXHAUSTED`.
`COMMON_LAYOUT_ALONE_INSUFFICIENT`. `MAP_FOLDER_DISCOVERY_CONTRACT_UNKNOWN`.
Key clarification: no city choice, forest world, and player death are NOT decisive signals.
`map_folders_list_empty=true` is the only decisive signal.
New comparator: `inspect-build42-known-working-map-contract.ps1` -- reads both roots
from `.local/` only; compares layout, mod.info/map.info fields, naming, no-BOM.

Status labels:
```text
MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY
COMMON_LAYOUT_ALONE_INSUFFICIENT
VARIANTS_ABCDEFGH_EXHAUSTED
MAP_FOLDER_DISCOVERY_CONTRACT_UNKNOWN
KNOWN_WORKING_MAP_COMPARATOR_REQUIRED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets.
Next human action: provide known-working Build 42 map mod under .local/ for comparison.

**MAP-7L — Variant G failure and Build 42 common/media/maps layout pivot:**
Variant G: mod.info map= field (H8) -- still empty scan. `VARIANTS_ABCDEFG_EXHAUSTED`.
Operator evidence: documented Build 42 layout uses `common/media/maps/<MapId>/`.
`COMMON_LAYOUT_PIVOT`. Discovery inspector updated v0.3 with common/ detection.
Experiment H: generates candidate with `common/media/maps/` structure.

Status labels:
```text
MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY
H8_MOD_INFO_MAP_FIELD_RULED_OUT
VARIANTS_ABCDEFG_EXHAUSTED
COMMON_LAYOUT_PIVOT
BUILD42_COMMON_MEDIA_MAPS_HYPOTHESIS
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets.
Next: Experiment H human-only retest with common/media/maps/<MapId>/ layout.
If scan becomes non-empty: progress to MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING stage.

**MAP-7K — Variant F folder/id alignment failure and Experiment G preparation:**
Variant F: exact folder/id alignment (H5) -- still empty scan.
`H5_FOLDER_ID_ALIGNMENT_RULED_OUT`. `VARIANTS_ABCDEF_EXHAUSTED`.
Inspector updated: `mod_info_has_map_field`, `h5_folder_id_alignment_result`, `h8_mod_info_map_field_recommended`.
Experiment G: adds `map=<MapId>` to mod.info to test H8.

Status labels:
```text
MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY
H5_FOLDER_ID_ALIGNMENT_RULED_OUT
H8_MOD_INFO_MAP_FIELD_RECOMMENDED
VARIANTS_ABCDEF_EXHAUSTED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets.
Next: Experiment G human-only retest with map= field in mod.info.

**MAP-7J — Variant E metadata contract failure:**
Variant E: root mod.info + root media/maps + 42/ — still empty scan.
`VARIANTS_ABCDE_EXHAUSTED`. Layout experiments exhausted. `METADATA_CONTRACT_FOCUS`.
Diagnostic distinction: `MAP_FOLDER_SCAN_EMPTY` (discovery blocker, our case A-E) vs
`MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING` (later-stage, seen in forum evidence).
Analyzer updated with new lotheader classification. Hypotheses H4-H8 recorded.
Variant F requires human decision after metadata field comparison.

Status labels:
```text
MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY
VARIANTS_ABCDE_EXHAUSTED
METADATA_CONTRACT_FOCUS
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets.
Next: operator compares candidate metadata against a reference mod before Variant F.

**MAP-7I — Variant D root media failure and Experiment E preparation:**
Variant D: root `media/maps/` duplicate + `42/media/maps/` — still empty scan.
`ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT` confirmed. Root `mod.info` absent in Variant D.
Square/blocked visual area observed — NOT proof of map registration (scan empty).
Experiment E: root `mod.info` + root `media/maps/` + `42/` preserved.
Inspector updated: `has_dual_mod_info_layout`, `experiment_e_root_mod_info_recommended`.

Status labels:
```text
MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY
ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT
EXPERIMENT_E_ROOT_MOD_INFO_RECOMMENDED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets.
Next: Experiment E human-only retest with dual-layout candidate.

**MAP-7H — Variant B/C registration failures and discovery path investigation:**
Variants B (`Map=candidate`) and C (`Map=Muldraugh;candidate`) both confirm empty
IsoMetaGrid map folder scan. Map= ordering variants A/B/C exhausted.
Root cause: mod discovery path, not Map= format. Custom mod map folder not
visible to IsoMetaGrid. Versioned 42/ layout may not be scanned by IsoMetaGrid.

Status labels:
```text
MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY
MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY
MAP_LINE_VARIANTS_EXHAUSTED
DISCOVERY_PATH_INVESTIGATION_ACTIVE
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets.
Next: Experiment D (root media/maps/ duplicate), E (root mod.info), F (map.info comparison).

**MAP-7G — Variant A registration failure and real DebugLog parser fix:**
Variant A tested: `Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY`.
Result: MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY. IsoMetaGrid map folder list still empty.
Muldraugh terrain loaded (confirming built-in maps resolve from Map= line).
Custom candidate does not appear in folder scan.
Analyzer fix: real PZ DebugLog format uses `f:N st:N>` not `, timestamp>`.
New params: `-ExpectedMapId`, `-VariantLabel`.

Status labels:
```text
MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY
VARIANT_A_TESTED_MAP_LINE
CANDIDATE_NOT_IN_MAP_FOLDER_LIST
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets.
Recommended next: test Variants B and C; investigate IsoMetaGrid discovery path.

**MAP-7F — Map folder registration diagnostic and analyzer fix:**
MAP-7E confirmed MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD. IsoMetaGrid
found no map folders. Candidate map folder was not registered or discovered.
Analyzer bug fixed: timestamped DebugLog format caused map_folders_list_empty=False
despite visible empty list. Fix: line-by-line prefix-stripping parser.

Status labels:
```text
MAP_FOLDER_SCAN_EMPTY_CONFIRMED
MAP_FOLDER_REGISTRATION_BLOCKER_ACTIVE
ANALYZER_TIMESTAMPED_LOG_BUG_FIXED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No load test. No binary writer change. No PZ assets.
Registration diagnostic packet prepared. Three Map= variants to test manually.
Recommended next: human-only retest with Map= variants A/B/C; capture DebugLog.

**MAP-7E — Empty world and map registration diagnostics:**
MAP-7D no-BOM retest: MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD (in-game in 32s).
Cleared: LexState, BOM, spawn null, player-data timeout.
Remaining: map folders list empty, no city choice, spawn building warning.

Status labels:
```text
MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

This is a controlled partial in-game load proof. No public playable claim.
Recommended next: MAP-7E diagnostic retest to capture map folder registration evidence.

**MAP-7D — Timeout and Lua encoding fix:**
MAP-7C retest: LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA. Inspector confirmed UTF-8 BOM on v3 files.
MAP-7D adds `empty_grass_v4` with `UTF8Encoding(false)` for all game-read text files.

Status labels:
```text
MAP7C_MANUAL_RETEST_RECORDED
LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA
OBJECTS_LUA_NO_BOM_FIX_APPLIED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test. LOTH/LOTP/chunkdata unchanged. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
Recommended next: MAP-7D controlled retest with `empty_grass_v4` (no-BOM).

**MAP-7C — objects.lua and spawn metadata fix:**
MAP-7C adds `empty_grass_v3` profile with fixed Lua metadata. LOTH/LOTP/chunkdata unchanged.

Key changes from v2:
- objects.lua: `return {}` → comment-only (avoids MAP-7A LexState.token2str exception)
- spawnpoints.lua: `all` key → `unemployed` key with explicit worldX/worldY/posX/posY/posZ
- Inspector: detects `comment_only`, spawn fields, recommendations

Status labels:
```text
MAP7C_OBJECTS_LUA_METADATA_PACKET_CREATED
OBJECTS_LUA_FIXED_COMMENT_ONLY
SPAWNPOINTS_LUA_UNEMPLOYED_KEY
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test. LOTH/LOTP/chunkdata writer unchanged. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
Recommended next: human-only MAP-7C retest with `empty_grass_v3` candidate.

**MAP-7B — LOTH v3 retest result and objects.lua failure record:**
MAP-7B records the MAP-7A manual retest outcome and adds a local diagnostic inspector.

Key findings:
- LOTH v3 (empty_grass_v2): no lotheader EOF observed. IsoMetaGrid loaded in 11.728 s.
- objects.lua LexState.token2str ArrayIndexOutOfBoundsException (index 65022, length 31).
- Spawn region NullPointerException in getSpawnRegionsAux (secondary blocker).
- Classification: LOAD_TEST_FAIL_OBJECTS_LUA.

Inspector: `scripts/inspect-build42-candidate-lua-metadata.ps1`
- Reads mod.info, map.info, spawnpoints.lua, objects.lua from generated .local candidate.
- Reports: exists, size, ASCII sanity, id/lots match, spawnpoints compatibility, objects content type.

Status labels:
```text
MAP7A_CLEAN_RETEST_RECORDED
LOTH_V3_EOF_NOT_OBSERVED
ISO_META_GRID_FINISHED_LOADING
OBJECTS_LUA_PRIMARY_BLOCKER
SPAWN_REGION_SECONDARY_BLOCKER
LOAD_TEST_FAIL_OBJECTS_LUA
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test in MAP-7B. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
Recommended next: MAP-7C objects.lua/spawn metadata fix and controlled retest.

**MAP-7A — Build 42 LOTH v3 controlled load-test packet:**
MAP-7A prepares the human-only load-test packet for the `empty_grass_v2` candidate.

Key script: `scripts/prepare-build42-loth-v3-load-test-packet.ps1`
Key test: `scripts/test-build42-loth-v3-load-test-packet.ps1` (23 assertions)
Doc: `docs/MAP_7A_LOTH_V3_LOAD_TEST_PACKET.md`

Preflight verifies 24 checks including:
- LOTH trailer_size=1048 and trailer_sha256=93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7
- LOTH total_size=29646 = 12 + 28586 + 1048
- LOTP size=1056780 and chunkdata size=1026 (both unchanged)
- Report: profile=empty_grass_v2, trailer_strategy=map6y_stable_literal_1048_block

Packet outputs: PACKET.md, RECORD.local-template.md, WIRING_COMMANDS.md, preflight.json, preflight.md.
All PZ folder operations are HUMAN-ONLY. Script writes only under .local.

Status labels:
```text
MAP7A_LOTH_V3_LOAD_TEST_PACKET_CREATED
EMPTY_GRASS_V2_CANDIDATE_GENERATED
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
Recommended next: human-only retest; result classified as PASS/FAIL_LOTH/FAIL_LOTP/FAIL_CHUNKDATA/FAIL_OBJECTS_LUA/INCONCLUSIVE.

**MAP-6Z — Build 42 LOTH v3 stable literal trailer writer:**
MAP-6Z adds `empty_grass_v2` profile to the candidate writer. LOTH v3 uses the same
1024 generated entries as v1 plus the canonical 1048-byte stable trailer from MAP-6Y.

Key facts:
- Total LOTH size: 29646 bytes (12 + 28586 ASCII + 1048 trailer).
- Trailer: first two U32LE = 8, remaining 1040 bytes = zero.
- Trailer SHA-256: 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7
- LOTP and chunkdata: unchanged.
- objects.lua secondary parse issue: still pending.
- 28 process tests added to CLI test suite.

Status labels:
```text
BUILD42_LOTH_V3_STABLE_LITERAL_WRITER_IMPLEMENTED
LOTH_TRAILER_STRATEGY=map6y_stable_literal_1048_block
LOTP_UNCHANGED
CHUNKDATA_UNCHANGED
OBJECTS_LUA_SECONDARY_PARSE_ISSUE_PENDING
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test. No writer change to LOTP/chunkdata. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
Recommended next: MAP-7A controlled LOTH v3 load-test packet and retest.

**MAP-6Y — LOTH fixed 1048-byte block research:**
MAP-6Y inspects whether the 1048-byte simple-cell LOTH trailer is constant,
partially stable, or variable across reference Build 42 cells.

Key script: `scripts/analyze-build42-loth-fixed-1048-block.ps1`
Key test:   `scripts/test-build42-loth-fixed-1048-block.ps1` (20 assertions)
Doc:        `docs/MAP_6Y_LOTH_FIXED_1048_BLOCK_RESEARCH.md`

Analysis fields:
- Per file: sha256_trailer, zero/nonzero_byte_count, u32_word_count, first/last_64_hex.
- Cross-file: selected_file_count, unique_trailer_sha256_count, all_1048_blocks_identical.
- Stability: stable/variable_byte_count, byte_ranges, prefix/suffix_length, U32 word stability.
- Coordinate correlation: tests if variable bytes match cell_x/cell_y.
- Hypotheses: FULLY_CONSTANT / STABLE_PREFIX_VARIABLE_BODY / STABLE_HEADER_ZERO_BODY /
  CELL_COORDINATE_FIELDS / VARIABLE_UNKNOWN / NOT_ENOUGH_REFERENCE_FILES.
- Writer readiness: NOT_DEFENSIBLE / MAYBE_DEFENSIBLE_WITH_ZERO_1048_BLOCK /
  MAYBE_DEFENSIBLE_WITH_STABLE_LITERAL / MAYBE_DEFENSIBLE_WITH_STABLE_PREFIX_ZERO_REMAINDER.

Status labels:
```text
BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED
LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS
WRITER_NOT_DEFENSIBLE
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6X — LOTH per-entry record model research:**
MAP-6X tests per-entry record hypotheses for the LOTH trailing body.

Critical finding: all 40 smallest Dru_map cells have EXACTLY 1048 trailing bytes.
- 1048 is U32-aligned (1048 % 4 = 0 = 262 U32 words).
- All 32 first-bytes of the trailing block are STABLE across all focus cells.
- Per-entry record model REJECTED for simple/grass cells.
- For simple cells: trailing body is a FIXED-SIZE 1048-byte block.
- Complex/urban cells (Dru_map sorted descending) had variable 7018-33558 bytes.
- `LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS` confirmed.
- MAP-6Y must confirm whether the 1048-byte block is entirely constant.

Status labels:
```text
BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED
LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS
HYPOTHESIS_FIXED_HEADER_PLUS_RECORDS
WRITER_NOT_DEFENSIBLE
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6W — LOTH trailing byte pattern research:**
MAP-6W deepens LOTH trailing body analysis to byte/U16 level.

Key smoke findings (20 Dru_map files):
- Mod2-aligned: 10/20; Mod4-aligned: 3/20. No dominant alignment.
- Avg entropy: 2.657 bits (consistent with packed small-integer structure).
- Avg U16 string-index ratio: 0.245 (slightly below 0.3 threshold).
- No length-prefixed strings (lp_u16=0 all files).
- No compression candidates.
- `HYPOTHESIS_TRAILER_UNKNOWN` persists.
- `WRITER_NOT_DEFENSIBLE`.
- Recommended: MAP-6X try per-entry record model (~6 bytes/entry based on 43_43 math).

Status labels:
```text
BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED
LOTH_REQUIRES_TRAILING_BINARY_BODY
HYPOTHESIS_TRAILER_UNKNOWN
WRITER_NOT_DEFENSIBLE
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6V — LOTH trailing body decode research:**
MAP-6V decodes the trailing binary section confirmed in MAP-6U and determines
whether a v3 writer is defensible.

Key smoke findings (20 Dru_map reference files):
- Trailing section is NOT u32-aligned in 17/20 files (mod4 != 0).
- Only 3/20 files have u32-aligned trailing bytes.
- Only 6/20 files have majority of first-16 trailer words < field8.
- `HYPOTHESIS_TRAILER_UNKNOWN` overall.

Conclusion: the trailing body structure is not yet understood at byte level.
A v3 writer is NOT defensible. MAP-6W must deepen trailing section analysis
before any writing attempt.

Status labels:
```text
BUILD42_LOTH_TRAILING_BODY_DECODED
LOTH_REQUIRES_TRAILING_BINARY_BODY
HYPOTHESIS_TRAILER_UNKNOWN
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6U — LOTH v2 failure record and full LOTH body research:**
MAP-6U records the MAP-6T v1 retest result (LOAD_TEST_FAIL_LOTH) and confirms
through full-body inspection that Build 42 LOTH files require a substantial
trailing binary section after the ASCII string table.

MAP-6T clean retest result:
- IsoLot.readInt EOF at the same location as MAP-6Q (v0).
- Increasing LOTH from 38 bytes (1 entry) to 28598 bytes (1024 entries) did not resolve the EOF.
- Scale alone is insufficient; the LOTH format is structurally incomplete.

MAP-6U full-body smoke (20 Dru_map files):
- ALL 20 reference LOTH files have trailing binary bytes after the ASCII string table.
- `LOTH_REQUIRES_TRAILING_BINARY_BODY` confirmed (20/20 files).
- Trailing range: 7018–33558 bytes per file.
- The MAP-6S/MAP-6T candidate has ZERO trailing bytes.

Status labels:
```text
MAP6T_CLEAN_V1_LOAD_TEST_RECORDED
EMPTY_GRASS_V1_LOTHEADER_REJECTED
CURRENT_CANDIDATE_LOTHEADER_EOF
LOTP_NOT_REACHED
CHUNKDATA_NOT_REACHED
OBJECTS_LUA_SECONDARY_PARSE_ERROR_OBSERVED
LOAD_TEST_FAIL_LOTH
LOTH_REQUIRES_TRAILING_BINARY_BODY
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

Next: MAP-6V must decode the trailing binary section and implement a writer for it.

No load test. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6T — Build 42 LOTH v2 load test packet:**
MAP-6T prepares the controlled retest packet for `empty_grass_v1`.

The packet script generates the v1 candidate, runs a 20-point preflight,
and writes human-only instructions for install, server wiring, and log capture.

Key preflight summary:
- LOTH size: 28598 bytes; entry_count: 1024; magic: LOTH; version: 1
- LOTP size: 1056780 bytes; chunkdata: 1026 bytes; all safety flags: false

Diagnostic classification:
- IsoLot.readInt fails again → LOAD_TEST_FAIL_LOTH (v2 structure still wrong)
- LOTP fails → LOAD_TEST_FAIL_LOTP (LOTH accepted; next: LOTP v2)
- Chunkdata fails → LOAD_TEST_FAIL_CHUNKDATA
- World loads → LOAD_TEST_PASS (record carefully; no public claim until reviewed)

Status labels:
```text
MAP6T_LOTH_V2_LOAD_TEST_PACKET_CREATED
EMPTY_GRASS_V1_CANDIDATE_GENERATED
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

No load test performed. No PZ writes. No writer change. No playable claim.

**MAP-6S — Build 42 LOTH candidate writer v2:**
MAP-6S adds the `empty_grass_v1` profile to the CLI candidate writer. This is
the first LOTH at reference scale.

Profile `empty_grass_v1` generates 1024 contiguous entries:
- Entry range: `blends_grassoverlays_01_0` ... `blends_grassoverlays_01_1023`
- Entries are generated in source; not copied from any Workshop mod or reference LOTH.
- `loth_known_risk = generated_entries_may_not_match_loaded_tile_definitions`

LOTH v2 size: 28598 bytes (vs v0: 38 bytes; smallest Dru_map reference: 34920 bytes).
The v1 candidate is now within 21% of the smallest reference (was 918x too small).
`candidate_smaller_than_all_references = true` (within reference range, not exceeding).

LOTP (1,056,780 bytes) and chunkdata (1,026 bytes) are unchanged from MAP-6L.

Status labels:
```text
BUILD42_LOTH_WRITER_V2_IMPLEMENTED
LOTH_ENTRY_COUNT=1024
LOTH_ENTRY_STRATEGY=generated_contiguous_grass_overlay_range
LOTH_KNOWN_RISK=generated_entries_may_not_match_loaded_tile_definitions
LOTP_UNCHANGED
CHUNKDATA_UNCHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

Artifacts:
- `docs/MAP_6S_BUILD42_LOTH_WRITER_V2.md` — writer doc.
- `src/PZMapForge.Cli/Program.cs`: `empty_grass_v1` profile in `Build42CandidateWriterCommand`.
- `tests/PZMapForge.Cli.Tests/MapExportBuild42CandidateWriterV1ProcessTests.cs`: 25 tests.

No load test. No PZ assets. No reference entry copying. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6R — Build 42 LOTH structure research:**
MAP-6R deepens the LOTH lotheader inspection to explain the MAP-6Q EOF failure
and produce writer-ready structural evidence.

Key smoke findings (20 Dru_map reference files, 512-byte prefix):
- `binaryGap=False` on ALL 20 files: bytes 12+ are immediately ASCII entries.
  No hidden binary section between the 12-byte header and the string table.
- `field8` = entry count (920-2007 per cell). Candidate has `field8=1`. Minimum
  for an empty grass cell is unknown but far above 1.
- `magic=LOTH`, `version=1` stable across all references.

Conclusion: the MAP-6L writer is structurally correct (LOTH magic + version +
field8 + ASCII entries) but the entry count is orders of magnitude too small.
MAP-6S must increase the entry set to the real tile requirements.

Status labels:
```text
BUILD42_LOTH_STRUCTURE_INSPECTED
LOTH_REFERENCE_PREFIX_ANALYSED
CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED
WRITER_RESEARCH_ONLY
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

Artifacts:
- `docs/MAP_6R_BUILD42_LOTH_STRUCTURE_RESEARCH.md` — research doc.
- `scripts/inspect-build42-loth-structure.ps1` — .local-only bounded prefix
  inspector for LOTH files.

No load test. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6Q — Spawn activation fixed; candidate lotheader EOF failure:**
MAP-6Q records that spawn activation wiring was fixed and the candidate map
files were exercised for the first time, confirming a LOTH lotheader rejection.

Wiring fixed: spawnregions.lua placed, server ini Map= and Mods= updated,
server _spawnregions.lua references candidate. Retest produced:
- Error 3 crash before city choice.
- `java.io.EOFException` at `IsoLot.readInt(IsoLot.java:75)` on 0_0.lotheader.
- `IsoMetaGrid$MetaGridLoaderThread.loadCell` in stack.

The MAP-6L LOTH lotheader (38 bytes) is too small — the smallest reference
Build 42 lotheader is 34920 bytes. The LOTH structure needs more entries.
LOTP and chunkdata remain unproven (load stopped at lotheader).

Status labels:
```text
SPAWN_ACTIVATION_WIRING_FIXED
CANDIDATE_MAP_FILES_EXERCISED
CURRENT_CANDIDATE_LOTHEADER_EOF
LOTHEADER_STRUCTURE_REJECTED
LOTP_NOT_REACHED
CHUNKDATA_NOT_REACHED
LOAD_TEST_FAIL_CURRENT_CANDIDATE
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

Artifacts:
- `docs/MAP_6Q_SPAWN_FIXED_LOTHEADER_EOF_FAILURE_RECORD.md` — full failure record.
- `scripts/compare-build42-lotheader-candidate.ps1` — .local-only lotheader
  comparison: reads candidate + reference LOTH files; reports size, magic,
  version, field8, stable word summary; smoke: candidate 38b vs min ref 34920b.

No load test by Claude. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6P — Clean retest result and spawn activation gap:**
MAP-6P records the MAP-6O clean retest outcome and defines the spawn activation
diagnostic protocol.

MAP-6O clean retest result:
- `BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED`: PZ loaded `pzmapforge_build42_candidate_001`
  without a candidate-specific crash. `current_candidate_matches=2` in fresh triage.
- `VANILLA_WORLD_ENTRY_WITH_CANDIDATE_ENABLED`: vanilla world entered successfully
  with the candidate mod active.
- `CANDIDATE_SPAWN_REGION_NOT_VISIBLE`: spawn selection screen showed only vanilla
  cities. Candidate map/spawn region was not activated.
- `CANDIDATE_MAP_CELL_NOT_PROVEN_LOADED`: binary files (LOTH/LOTP/chunkdata) were
  not exercised. Their acceptance remains unproven.
- `LOAD_TEST_INCONCLUSIVE`: mod loads without crashing but candidate map not activated.

Root cause candidates for spawn not visible (in priority order):
1. `Map=` line in server preset does not include candidate map ID.
2. `spawnregions.lua` missing from mod or incorrect format.
3. Server `_spawnregions.lua` does not reference the candidate.

Also in MAP-6P: MAP-6O checklist generator encoding bugs fixed:
- Triple-backtick fences now use a `$fence` variable (was: `` `t ``+ext mojibake).
- Em-dash `—` replaced with ASCII `--` in record template.
- Output files changed to ASCII encoding (no BOM, no non-ASCII bytes).

Status labels:
```text
BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED
VANILLA_WORLD_ENTRY_WITH_CANDIDATE_ENABLED
CANDIDATE_SPAWN_REGION_NOT_VISIBLE
CANDIDATE_MAP_CELL_NOT_PROVEN_LOADED
LOAD_TEST_INCONCLUSIVE
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

Artifacts:
- `docs/MAP_6P_CLEAN_RETEST_SPAWN_ACTIVATION_RECORD.md` — full retest record.
- `docs/MAP_6P_SPAWN_ACTIVATION_DIAGNOSTIC_PROTOCOL.md` — human-only diagnostic.
- `scripts/prepare-map6p-spawn-activation-diagnostic.ps1` — generates diagnostic
  commands and record template under `.local/`; refuses paths outside `.local/`;
  does NOT read PZ folders automatically.

**MAP-6O — Clean isolated Build 42 candidate retest protocol:**
MAP-6O defines the procedure for a clean, isolated retest of the MAP-6L/MAP-6M
candidate to resolve the MAP-6N INCONCLUSIVE status.

Protocol steps:
1. Pre-clean: delete old `pzmapforge_manual_b42_001_maptest_a` folders, disable
   unrelated mods, delete stale console.txt, create fresh server preset.
2. Install: operator manually copies `.local/` candidate to PZ mods (human-only).
3. Verify: 7 required files present at destination before launch.
4. Test: launch PZ, enable only candidate mod, record crash/pass at mod selection,
   spawn region visibility, world load start.
5. Log capture: copy fresh console.txt to `.local/` immediately after test.
6. Triage: run `scripts/extract-map6n-current-candidate-log-evidence.ps1` on
   captured log.

Status labels:
```text
CLEAN_ISOLATED_RETEST_PROTOCOL_CREATED
HUMAN_ONLY_COPY_REQUIRED
LOAD_TEST_NOT_PERFORMED
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

Artifacts:
- `docs/MAP_6O_CLEAN_ISOLATED_CANDIDATE_RETEST_PROTOCOL.md` — full protocol.
- `scripts/prepare-map6o-clean-retest-checklist.ps1` — generates operator
  checklist, record template, and triage commands under `.local/` only; refuses
  paths outside `.local/`; verifies 7 candidate source files; does NOT copy to PZ.

No load test performed. No writer change. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6N — Preliminary Build 42 candidate load test record (INCONCLUSIVE):**
MAP-6N records the first manual load test attempt for the MAP-6L/MAP-6M candidate
(`pzmapforge_build42_candidate_001`). The result is LOAD_TEST_INCONCLUSIVE.

Operator observation:
- Mod was manually copied to the PZ mods folder.
- PZ crashed or returned to menu when enabling/choosing the candidate mod.
- Extracted log evidence: `loading pzmapforge_build42_candidate_001` line found.
- No current-candidate LOTP/LOTH/IsoLot/CellLoader stack trace found in extracted lines.
- No spawn screen reached. No world loading confirmed.

Stale evidence exclusion: IsoLot/EOFException traces in the raw log belong to
`pzmapforge_manual_b42_001_maptest_a` (MAP-6B path). These are excluded and must
not be attributed to the current candidate.

Status labels:
```text
BUILD42_CANDIDATE_MOD_LOAD_LOGGED
MANUAL_TEST_ABORTED_OR_CRASHED_AT_MOD_SELECTION
CURRENT_CANDIDATE_ERROR_LOG_NOT_FOUND
STALE_MAPTEST_A_LOGS_EXCLUDED
LOAD_TEST_INCONCLUSIVE
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

Artifacts:
- `docs/MAP_6N_PRELIMINARY_CANDIDATE_LOAD_TEST_RECORD.md` — full inconclusive record.
- `scripts/extract-map6n-current-candidate-log-evidence.ps1` — .local-only log triage;
  separates current candidate evidence from stale maptest_a evidence; outputs
  `map6n-log-triage-report.json` and `.md` with `candidate_specific_exception_found` flag
  and `result_recommendation`; refuses paths outside `.local/`.

No writer change. No playable export claim. No PZ assets. LOAD_TEST_INCONCLUSIVE.

**MAP-6M — Build 42 candidate load test packet:**
MAP-6M adds `scripts/prepare-build42-candidate-load-test-packet.ps1` which
validates a MAP-6L candidate and produces an operator-ready load-test packet.

Preflight checks (31 total): report schema/safety flags, 8 required files,
LOTH magic/version/entry_count, LOTP magic/version/chunk_count/offsets/size,
chunkdata size/header/body. All 31 PASS for the `empty_grass_v0` candidate.

Output files: preflight JSON, operator packet MD, fillable record template,
spawnregions.lua template, install reference.

Does NOT copy files to PZ. Does NOT run PZ. LOAD_TEST_NOT_PERFORMED.
PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6L — Build 42 candidate writer MVP:**
MAP-6L implements the first deterministic Build 42 binary writer behind
`--build42-candidate-writer --build42-candidate-profile empty_grass_v0`.

Generated output (under `.local/` only, versioned `42/` layout):
- `0_0.lotheader`: 38 bytes — LOTH magic + version=1 + 1 grass entry (MAP-4E committed evidence).
- `world_0_0.lotpack`: 1,056,780 bytes — LOTP magic + version=1 + 1024 zero-payload chunks.
- `chunkdata_0_0.bin`: 1,026 bytes — `00 01` header + 1024 zero bytes.

Smoke inspection confirmed: first_offset=8204, monotonic=True, unique_sizes=1, all_zero_body=True.

Status: `BUILD42_CANDIDATE_WRITER_IMPLEMENTED`. `LOAD_TEST_NOT_PERFORMED`.
`writer_scope = candidate_only_not_load_tested`. Remaining unknowns: lotp_zero_payload_load_acceptance,
loth_minimum_entries_acceptance, missing_trailer_acceptance.

No load test. No PZ assets into repo. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6K — Build 42 LOTP payload and LOTH entry research:**
MAP-6K adds `scripts/inspect-build42-lotp-payload-windows.ps1` to read LOTP
chunk payload windows and LOTH entry lists from the Drummondville reference.

Key Drummondville findings:
- LOTP: first_offset=8204 ✓; most_common_payload_size=1024; unique_sizes 24-63 per cell;
  all offsets monotonic; tail_bytes ~1024-1056.
- LOTH: parsed_count=declared_count+1 consistently (trailing content pattern);
  smallest observed entry count=36; entry format newline-delimited ASCII confirmed.
- Chunkdata: all_zero_body=True for 3/3 sampled cells.

Status: `BUILD42_LOTP_PAYLOAD_WINDOWS_INSPECTED`, `BUILD42_LOTH_ENTRIES_EXTRACTED`,
`BUILD42_CHUNKDATA_BODY_INSPECTED`. `WRITER_NOT_IMPLEMENTED`. Next: MAP-6L writer MVP.

No writer. No load test. No PZ assets into repo. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6J — Build 42 writer contract:**
MAP-6J codifies the Build 42 binary format contracts into exact byte-level
definitions and adds a writer-plan schema and example.

Key contracts:
- LOTP lotpack: magic `4C 4F 54 50`, version=1, chunk_count=1024, offset table
  starts at byte 12 (12 + 1024×8 = 8204 first payload offset). **Chunk payload
  format unknown** — major remaining gap.
- LOTH lotheader: magic `4C 4F 54 48`, version=1, entry_count U32 LE, then
  newline-delimited tileset names. **Minimum entry set unknown.**
- Chunkdata: 1026 bytes = 2 header + 1024 body (all-zero hypothesis).
- File set: 7 required files under `42/` versioned layout.

Schema: `schemas/pzmapforge.build42-writer-plan.v0.1.schema.json`
Example: `examples/build42-writer-plan/minimal-empty-cell-writer-plan.json`

Status: `BUILD42_WRITER_CONTRACT_CREATED`. `WRITER_NOT_IMPLEMENTED`.
`contract_review_required = true`. `candidate_plan_ready_for_writer = false`.

No writer implemented. No load test. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6I — Build 42 format design matrix:**
MAP-6I adds `scripts/derive-build42-format-design-matrix.ps1` which reads a MAP-6H
reference geometry report and produces a word-level stability matrix for LOTP
lotpacks, LOTH lotheaders, and chunkdata records.

Key findings from Drummondville reference:
- LOTP words 0-1: stable (magic + version=1). Word 2: chunk count=1024. Word 3: first chunk offset=8204 (=12+1024×8). This confirms the offset table structure.
- LOTH words 0-1: stable (magic + version=1). Word 2: variable (entry count, depends on cell). Words 3+: tileset pack name bytes (ASCII, same structure as MAP-4E).
- Chunkdata dominant model: 32×32 (body=1024).

Candidate writer design documented. Key unknowns remaining: LOTP chunk data format; LOTH minimum entry set. `BUILD42_FORMAT_DESIGN_MATRIX_CREATED`. `WRITER_NOT_IMPLEMENTED`.

Artifacts: `docs/MAP_6I_BUILD42_FORMAT_DESIGN_MATRIX.md`.

No writer implemented. No load test. No PZ assets into repo. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6F — Build 42 reference geometry inspector packet:**
MAP-6F adds `scripts/inspect-build42-reference-geometry.ps1` to resolve the
MAP-6E geometry uncertainty. The operator manually copies a known-good Build 42
map mod under `.local/reference-build42-map/` and runs the inspector.

The inspector reads bounded byte prefixes of `.lotheader`, `world_*.lotpack`,
`chunkdata_*.bin` and maps observed values to geometry statuses:
- `BUILD42_300_MODEL_SUPPORTED`: hdrA=900, body_bytes=900 across samples.
- `BUILD42_256_MODEL_SUPPORTED`: body_bytes=1024 or 256 observed.
- `BUILD42_GEOMETRY_STILL_UNKNOWN`: inconclusive.

A test script (`scripts/test-build42-reference-geometry-inspector.ps1`) runs
10 assertions against synthetic fixture data. psTotal: 492→502.

Artifacts: `docs/MAP_6F_BUILD42_REFERENCE_GEOMETRY_PACKET.md`.

No playable export claim. No load test. No PZ assets. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6G — Build 42 LOTP lotpack evidence and inspector hardening:**
MAP-6G records the Drummondville Build 42 reference run evidence and hardens
the MAP-6F inspector to detect the Build 42 `LOTP` lotpack magic header.

Key findings:
- Build 42 `world_*.lotpack` files begin with `4C 4F 54 50` = `LOTP` magic.
- The legacy MAP-4F `hdrA=900 / hdrB=7204` offset-table format does NOT apply
  to Build 42 reference lotpacks.
- The current PZMapForge experimental writer produces files incompatible with
  Build 42's LOTP format.
- Status: `BUILD42_LOTP_FORMAT_OBSERVED`.

Inspector updates: LOTP magic detection, `lotpack_format` field, `lotpack_lotp_count`
summary, `[int64]` for legacy table computations (overflow fix). Test script
extended to 15 assertions. psTotal: 502→507.

Artifacts: `docs/MAP_6G_BUILD42_LOTP_LOTPACK_EVIDENCE.md`.

No playable export claim. No load test. No PZ assets into repo.

**MAP-6H — Build 42 LOTP LOTH deep reference inspection:**
MAP-6H extends the inspector to capture bounded word-level prefixes and
detects the Build 42 `LOTH` lotheader magic. Combined with LOTP and chunkdata
body=1024, the evidence now strongly supports the 256×256 model.

Key findings:
- Build 42 lotheaders begin with `4C 54 5A 48` = `LOTH` magic (20/20 Drummondville).
- All three indicators present together → `BUILD42_256_MODEL_STRONGLY_SUPPORTED`.
- Inspector now records `geometry_statuses` array; `first_16/32/64_bytes_hex`;
  `u32le_words_first_64`; `lotheader_ltz_count`.
- Schema bumped to `v0.2`.

Status labels: BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED, BUILD42_32X32_CHUNK_GRID_OBSERVED,
BUILD42_256_MODEL_STRONGLY_SUPPORTED, GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED.

Artifacts: `docs/MAP_6H_BUILD42_LOTP_LOTH_DEEP_INSPECTION.md`.

No writer implemented. No load test. No PZ assets into repo. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.

**MAP-6E — Build 42 geometry model audit:**
MAP-6E audits all 300×300 geometry assumptions in the repo and records the
operator observation that Build 42 may use a 256×256 cell model.

Key findings:
- `PaletteLoader`, `ImageMapForgeParser`: hardcoded 300×300 in C# constants.
- `pzmapforge.layer-manifest.v0.1.schema.json`: `width`/`height` const 300.
- Binary writer: hdrA=900, chunkdata=902 derived from 30×30 chunk grid
  (300/10=30), which may be Build 41 convention only.
- The Workshop mods observed in MAP-4B through MAP-4G are not explicitly
  identified as Build 41 or Build 42 in committed evidence.
- No committed evidence assigns geometric meaning to 256 in the codebase.

Status labels: GEOMETRY_MODEL_UNVERIFIED, LEGACY_300_ASSUMPTION_AUDITED,
BUILD42_256_MODEL_OPERATOR_REPORTED, LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION.

Experimental report new fields: `geometry_model_status`,
`geometry_model_basis`, `target_build42_cell_size`.

Artifacts:
- `docs/MAP_6E_BUILD42_GEOMETRY_MODEL_AUDIT.md` — full audit.

No playable export claim. No load test. No PZ assets.

---

## 6. Source / editing format contract

**PZMapForge source format (v0.1 defined — MAP-1):**
PZMapForge-owned declarative data, schema-backed as of MAP-1.
Schema: `schemas/pzmapforge.map-source.v0.1.schema.json`
Example: `examples/map-source/minimal-cell.json`

The v0.1 source format is a JSON document describing a map in PZMapForge terms.
It is not an export format. It is not a Project Zomboid compiled file. No output
is written to `.local/` from the schema alone. No compiler step exists yet.

Required top-level fields (v0.1):
- `schema`: const `pzmapforge.map-source.v0.1`
- `format_version`: const `0.1`
- `claim_boundary`: const `map_source_only_not_exported_not_pz_load_tested`
- `map_id`: lowercase slug string
- `cell_size`: integer (300 for current planning convention)
- `cells`: array of cell objects (minItems 1)

Each cell (v0.1): `cell_id`, `x`, `y`, `terrain`, `spawn_points`, `zones`, `notes`.
Terrain values (v0.1): `grass`, `asphalt`, `water`, `unknown`.
Spawn points in v0.1 are source metadata only. No compiled spawn output exists.

**Editable / project format:**
Optional future compatibility layer (e.g. a human-editable YAML or JSON map
description). Not canonical. Not implemented.

The source format must be:
- ASCII or UTF-8 text only.
- Schema-validated before any downstream step.
- Not derived from Project Zomboid binary files.

---

## 7. Compiled game-load format contract

The compiled game-load format is the output that Project Zomboid can load as a
map mod. This format is a future compiler target. It is not implemented in
MAP-0.

Known required files (requires local evidence to confirm):

| File type | Purpose | Status |
|---|---|---|
| `.lotheader` | Cell header for the game engine | Not implemented |
| `.lotpack` | Packed cell content | Not implemented |
| `.bin` map data | Auxiliary binary map data | Not implemented |
| TMX (production) | Tile-accurate cell layout for WorldEd | Not implemented |

All compiled output must be generated under `.local/` only. No compiled output
may be committed to the repo. No compiled output may be claimed playable until
a manual load test produces a documented evidence record.

---

## 8. Mod packaging contract

A Project Zomboid map mod requires a directory structure under the game's mod
folder. This is a future generated local scaffold. It is not implemented in
MAP-0.

Known required components (requires local evidence to confirm):

| Component | Notes |
|---|---|
| `mod.info` | Mod metadata file |
| `media/maps/<map-name>/` | Map cell directory |
| `media/textures/` | Tileset references (local assets only) |
| Spawn definition | Required for player entry (see section 9) |

The PZMapForge compiler must never write into any `media/maps` directory inside
the repo. All future mod scaffold output goes to `.local/` only.

---

## 9. Spawn / player entry contract

A playable map requires at least one valid spawn point. Without a spawn point
the player cannot enter the map.

Spawn requirements are partially understood and require local proof:

| Requirement | Status |
|---|---|
| Spawn definition file format | Unknown; requires local evidence |
| Spawn coordinates (cell/chunk system) | Unknown; requires proof |
| Minimum cell requirement for spawn | Unknown; requires proof |
| Whether a single cell is sufficient | Unknown; requires proof |

MAP-0 does not implement spawn generation. The planning rule engine records a
warning when a spawn marker is absent from the blockout input. This is an
advisory only.

---

## 10. In-game map feature contract

The in-game map (the UI map the player opens) requires separate feature files.
These are distinct from the playable cell files.

| Feature | Status |
|---|---|
| World map overlay | Not implemented |
| Road / street lines | Not implemented |
| Zone labels | Not implemented |
| Landmark markers | Not implemented |

All in-game map feature file formats require research and local proof. MAP-0
does not implement any in-game map feature writer.

---

## 11. Asset boundary

PZMapForge must never copy, redistribute, or commit Project Zomboid game
assets, tilesheets, sprites, or proprietary content.

Future compiler steps may reference a locally installed Project Zomboid copy
for local generation only. Any such reference:

- Must read only from the locally installed game directory.
- Must not copy assets into the PZMapForge repository.
- Must not copy assets into `.local/` in a form that risks accidental
  redistribution.
- Must be documented in a local-config schema (see Phase 3 decision records).

The PZMapForge planning TMX uses generated colour tiles only. It is not an
asset reference.

---

## 12. Local output boundary

All experimental and generated output from any future compiler command must
land under `.local/` only. The `.local/` directory is gitignored.

Rules:
- No media/maps writes in the repo ever.
- No compiled cell output committed to the repo.
- No PZ assets committed to the repo.
- No `.local/` output committed to the repo.
- No playable claim until a manual load test produces documented evidence.

---

## 13. First playable proof target

**PZMapForge MAP-4 target: minimal local playable cell proof**

This target is a future milestone. It is not implemented in MAP-0.

Requirements:
- Generated entirely under `.local/`.
- Smallest loadable test map unit (one cell or equivalent minimum).
- One spawn point defined.
- No PZ assets copied or committed.
- No repo media/maps writes.
- Manual load test performed by the operator.
- Evidence file written to `.local/` recording the load outcome.
- No public playable claim until the evidence file exists and confirms success.

The evidence file format is not yet defined. It must include at minimum:
- date of load test
- PZ version tested against
- pass/fail result
- operator notes

---

## 14. Unknowns and required local evidence

The following are unknown and require direct local investigation before any
compiler slice can be ratified:

| Unknown | Investigation method |
|---|---|
| Exact `.lotheader` binary format | Inspect a known-good WorldEd export locally |
| Exact `.lotpack` binary format | Inspect a known-good WorldEd export locally |
| Minimum cell size for PZ to load a custom map | Local load test with smallest possible cell |
| Spawn definition file format and schema | Inspect existing mod spawn files locally |
| Whether a single cell is loadable without a world grid | Local load test |
| Cell coordinate origin convention (0,0 vs offset) | Inspect WorldEd export or PZ mod docs |
| In-game map feature file format | Inspect existing map mods locally |
| Build 42 vs Build 41 format differences | Local comparison; no assumption made |

None of these unknowns may be resolved by inference or LLM output alone.
Local inspection of real files is required evidence.

---

## 15. Forbidden claims

While the current boundary is `planning_artifact_only_not_pz_load_tested`:

- Do not claim PZMapForge exports playable Project Zomboid maps.
- Do not claim PZMapForge replaces TileZed or WorldEd.
- Do not claim generated TMX files are production PZ exports.
- Do not claim generated planning artifacts will load in Project Zomboid.
- Do not claim any map geometry has been converted or extracted.
- Do not claim any SVG coordinates have been converted to PZ coordinates.
- Do not claim Build 42 compatibility.
- Do not claim Workshop readiness.
- Do not claim any compiled output exists until MAP-4 evidence is recorded.

These claims become available only after the corresponding milestone evidence
file is committed and reviewed.

---

## 16. Proposed next slices

| Slice | Title | Scope |
|---|---|---|
| MAP-1 | PZMapForge map source schema | Schema defined: schemas/pzmapforge.map-source.v0.1.schema.json; example: examples/map-source/minimal-cell.json; source format only, not exported, not compiled |
| MAP-2 | Dry-run map export plan command | map-plan CLI command implemented; reads pzmapforge.map-source.v0.1 JSON; writes map-export-plan.json + map-export-plan.md to .local/; dry_run=true, execute_supported=false, playable_export_generated=false; no compiled outputs; no media/maps writes; no PZ assets |
| MAP-3A | Text-only mod scaffold contract | map-plan JSON extended with scaffold contract fields (scaffold_contract_version, text_only_scaffold_supported_now=false, text_only_scaffold_written=false, scaffold_execute_supported=false, future_scaffold_files with all written_now=false); Markdown extended with Future text-only scaffold contract section; no scaffold files written; no media/maps writes; no PZ assets; contract-only evidence |
| MAP-3B | Text-only local mod scaffold writer | map-scaffold CLI command implemented; reads pzmapforge.map-source.v0.1 JSON; validates schema/claim_boundary/cells; writes exactly four text files under .local/ (mod.info, media/maps/<map_id>/map.info, media/maps/<map_id>/spawnpoints.lua, media/maps/<map_id>/README_PZMAPFORGE_BOUNDARY.txt); refuses non-.local and media/maps output paths; no compiled outputs; no PZ assets; no playable export; every file contains boundary language; 15 CLI tests |
| MAP-4 | Minimal compiled cell writer proof | Add a command that writes one minimal compiled cell under `.local/`; requires local evidence from section 14 first |
| MAP-5 | Manual Project Zomboid load-test evidence | Operator performs load test; evidence file recorded; no public claim before this milestone |
| MAP-6 | In-game map feature writer research/proof | Research and document in-game map feature file formats; prototype if evidence supports it |

Each slice is blocked by its predecessor. No slice may be started without the
prior slice's evidence being committed. MAP-4 is additionally blocked by
resolving all unknowns in section 14.
