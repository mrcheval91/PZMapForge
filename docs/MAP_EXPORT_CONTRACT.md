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
