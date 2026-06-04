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
does not exist yet. MAP-0 does not implement any step past dry-run planning.

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
| MAP-2 | Dry-run map export plan command | Add a `map-plan` CLI command that reads source format and outputs a dry-run export plan artifact with no file writes |
| MAP-3 | Text-only local mod scaffold writer | Add a command that writes a minimal text-only mod scaffold under `.local/` only; no binary files; no assets |
| MAP-4 | Minimal compiled cell writer proof | Add a command that writes one minimal compiled cell under `.local/`; requires local evidence from section 14 first |
| MAP-5 | Manual Project Zomboid load-test evidence | Operator performs load test; evidence file recorded; no public claim before this milestone |
| MAP-6 | In-game map feature writer research/proof | Research and document in-game map feature file formats; prototype if evidence supports it |

Each slice is blocked by its predecessor. No slice may be started without the
prior slice's evidence being committed. MAP-4 is additionally blocked by
resolving all unknowns in section 14.
