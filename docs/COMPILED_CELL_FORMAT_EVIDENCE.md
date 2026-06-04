# Compiled Cell Format Evidence

```text
Status:           MAP-4D binary header evidence recorded
Claim boundary:   evidence_inventory_only_not_compiled_not_pz_load_tested
Compiler status:  not implemented
PZ assets:        not copied into repo
media/maps:       forbidden in repo
Observations:     2 (Laval-Montreal workshop, RED-Speedway workshop)
Text metadata:    map.info, mod.info, spawnpoints.lua, objects.lua read (2 mods)
Binary prefixes:  first 64 bytes sampled (5 files per extension per mod)
Binary formats:   PARTIAL — prefix patterns observed; full format not decoded
```

---

## 1. Purpose

This document defines what compiled cell format evidence is required before
PZMapForge can implement a MAP-4 compiled cell writer. No writer is implemented
here. This is the evidence specification and gap record only.

MAP-4 is blocked until the evidence gaps listed in section 5 are closed by
direct local observation. No gap may be closed by inference, LLM output, or
documentation alone.

---

## 2. Why MAP-4 is blocked

MAP-3B (map-scaffold) produces a text-only mod skeleton under `.local/`. It
does not write compiled cell files. Project Zomboid requires compiled binary
files (`.lotpack`, `.lotheader`) for map cells to load. The exact binary format
of these files is unknown without direct local inspection of a known-good
WorldEd export.

The following are unknown and must not be assumed:

- Binary layout of `.lotheader`
- Binary layout of `.lotpack`
- Cell coordinate naming convention
- Minimum viable cell set for a map to load
- Whether a single cell is sufficient without a world grid
- Spawn file format and coordinate system
- Build 41 vs Build 42 format differences

---

## 3. Suspected file types and roles

Updated from MAP-4B observations. File names and presence are locally observed
from two Workshop mod inventories. Binary content is not parsed.

| File type | Suspected role | Observed naming | Verified locally |
|---|---|---|---|
| `<cx>_<cy>.lotheader` | Cell geometry/header for game engine | `<cx>_<cy>.lotheader` (confirmed) | Yes — file presence and naming |
| `world_<cx>_<cy>.lotpack` | Packed cell tile data | `world_<cx>_<cy>.lotpack` (differs from initial hypothesis) | Yes — file presence and naming |
| `chunkdata_<cx>_<cy>.bin` | Auxiliary chunk data | `chunkdata_<cx>_<cy>.bin` (differs from initial hypothesis) | Yes — file presence and naming |
| `media/maps/<name>/map.info` | Map metadata | present in both observations | Yes — file presence only |
| `media/maps/<name>/spawnpoints.lua` | Player spawn definitions | present in both observations | Yes — file presence only |
| `media/maps/<name>/objects.lua` | Object placement definitions | present in both observations | Yes — file presence only |
| `media/maps/<name>/worldmap.xml.bin` | In-game world map data | present in Laval-Montreal; absent in RED-Speedway | Partial — may be optional |

**Naming corrections from initial hypothesis:**
- Lotpack naming is `world_<cx>_<cy>.lotpack`, not `<cx>_<cy>.lotpack`.
- Chunk bin naming is `chunkdata_<cx>_<cy>.bin`, not `map_<cx>_<cy>.bin`.
- `objects.lua` is present and was not in the original hypothesis.
- `worldmap.xml.bin` presence may depend on map size or WorldEd settings.

Binary content of all file types remains unknown. File presence and naming only.

---

## 4. Directory layout — observed (MAP-4B)

Updated from MAP-4B observations. File names confirmed by inventory.
Binary content not parsed.

```text
<mod root>/
  mod.info
  media/
    maps/
      <map_name>/
        map.info
        spawnpoints.lua
        objects.lua
        worldmap.xml.bin         (present in Laval-Montreal; may be optional)
        <cx>_<cy>.lotheader      (one per cell; e.g. 0_0.lotheader, 25_15.lotheader)
        world_<cx>_<cy>.lotpack  (one per cell; e.g. world_0_0.lotpack)
        chunkdata_<cx>_<cy>.bin  (one per cell; e.g. chunkdata_0_0.bin)
```

All cell files appear to live directly in `media/maps/<map_name>/`.
No subdirectory for cell files was observed in either inventory.
Coordinate naming convention is `<cx>_<cy>` where cx and cy are integers
(zero-based in Laval-Montreal; offset in RED-Speedway at 25_15 to 26_17).

---

## 5. Evidence gaps — must be closed before MAP-4

| Gap | Investigation method | Status |
|---|---|---|
| `.lotheader` binary format | Inspect byte-level content locally (hex editor or parser) | **PARTIAL** — prefix sampled (64 bytes, 10 files, 2 mods); bytes 0-3 = `00000000` (consistent); bytes 4-7 = 32-bit LE variable integer (likely entry count); bytes 8+ = newline-separated ASCII tileset names. Byte-level semantics not confirmed; full decoding required before writing |
| `.lotpack` binary format | Inspect byte-level content locally (hex editor or parser) | **PARTIAL** — prefix sampled; first 8 bytes = `84030000241c0000` IDENTICAL across all 10 sampled files from both mods; bytes 8+ appear to be an offset/size table with increasing 4-byte or 8-byte LE values. Full format not decoded; writing not permitted |
| Cell coordinate naming convention | Observe file names in a Workshop/WorldEd export | **PARTIAL** — `<cx>_<cy>` pattern confirmed in 2 observations; offset coords confirmed (25_15); zero-origin also observed |
| Exact directory layout for cell files | Observe directory tree in a Workshop/WorldEd export | **PARTIAL** — flat layout under `media/maps/<map_id>/` confirmed in 2 observations |
| Minimum viable cell count for load | Local load test with smallest possible cell | **OPEN** — smallest observed is 6 cells (2x3); single-cell not tested |
| Whether single cell loads without world grid | Local load test | **OPEN** — not tested |
| Spawn file format (`spawnpoints.lua`) | Inspect file content locally | **PARTIAL** — format pattern observed: profession-keyed Lua table; fields: `worldX`, `worldY`, `posX`, `posY`, `posZ`; `function SpawnPoints()` / `return {}` structure confirmed |
| Spawn coordinate system (cell/chunk) | Inspect spawn file content and PZ source references | **PARTIAL** — `worldX`/`worldY` appear to be cell-grid coordinates; `posX`/`posY`/`posZ` appear to be in-cell position; exact origin and scale not confirmed |
| `map.info` required fields | Inspect file content locally | **PARTIAL** — fields observed: `title`, `lots`, `description`, `fixed2x`; which are required vs optional is not confirmed |
| Build 41 vs Build 42 format differences | Local comparison; no assumption made | **OPEN** — not investigated |

All gaps must be marked CLOSED with a filled evidence template before MAP-4
implementation begins. No gap may be closed by assumption.
PARTIAL status means file presence or naming is observed; byte-level content
or functional semantics are not confirmed.

---

## 6. How to collect evidence

### Step 1: obtain a known-good WorldEd export

Export a minimal test map using WorldEd to a local directory outside this
repo. Do not copy any output into this repo.

### Step 2: run the evidence inspector

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\inspect-compiled-cell-evidence.ps1" `
    -Path  "C:\path\to\your\worlded-export" `
    -Output ".local\evidence\worlded-export-01"
```

The script enumerates file names, extensions, sizes, and SHA-256 hashes.
It does not copy files into the repo. Output stays under `.local/`.

### Step 3: fill the evidence template

Copy `docs/examples/compiled-cell-evidence/COMPILED_CELL_EVIDENCE_TEMPLATE.md`
to `.local/evidence/` and fill it with your observations.

The template is not evidence itself. The filled copy in `.local/` is local
evidence. Do not commit filled evidence templates to the repo unless they
are sanitized and contain no PZ binary content.

### Step 4: close gaps

For each gap in section 5 above:
- Record the local finding in the filled template.
- Update this document to mark the gap CLOSED.
- Note the PZ version, build, and mod source.

---

## 7. Forbidden actions during evidence collection

- Do not copy `.lotpack`, `.lotheader`, `.bin`, or any compiled cell files
  into this repo.
- Do not copy PZ game assets (tiles, textures, sprites) into this repo.
- Do not commit the filled evidence template if it contains binary excerpts.
- Do not write to `media/maps/` inside this repo.
- Do not begin a MAP-4 writer implementation until all gaps in section 5
  are closed.
- Do not claim playable export at any stage of evidence collection.
- Do not infer binary formats from documentation or LLM output.

---

## 8. Decision gate for MAP-4

MAP-4 implementation is permitted only when all of the following are true:

- [ ] All gaps in section 5 are marked CLOSED.
- [ ] At least one filled evidence template exists in `.local/`.
- [ ] The cell coordinate naming convention is confirmed.
- [ ] The directory layout is confirmed.
- [ ] `map.info` required fields are confirmed.
- [ ] `spawnpoints.lua` format is confirmed.
- [ ] Binary formats of `.lotheader` and `.lotpack` are described at byte level
      (field offsets, types, and sizes), not just from file presence.
- [ ] A minimum viable cell count is estimated.
- [ ] No PZ assets were copied into this repo during evidence collection.
- [ ] This document has been updated with CLOSED status for all gaps.
- [ ] A decision record (`docs/decisions/`) is committed before any writer is
      implemented.

---

## 9. Tools

| Tool | Purpose |
|---|---|
| `scripts/inspect-compiled-cell-evidence.ps1` | Enumerates files, sizes, SHA-256 — local only, no copying |
| `docs/examples/compiled-cell-evidence/COMPILED_CELL_EVIDENCE_TEMPLATE.md` | Fillable observation template |

---

## 10. Claim boundary

This document and all associated scripts are evidence inventory only:

```text
claim_boundary: evidence_inventory_only_not_compiled_not_pz_load_tested
```

No compiled writer is implemented. No PZ assets are read or copied.
No playable export is claimed. No `media/maps` writes occur in the repo.

---

## 11. Evidence observations (MAP-4B)

Two local Workshop mod inventories were collected using
`scripts/inspect-compiled-cell-evidence.ps1`. No files were copied into this
repo. All evidence output is under `.local/` only. Binary content was not read.

### Observation comparison

| Field | Laval-Montreal workshop | RED-Speedway workshop |
|---|---|---|
| Local output | `.local\evidence\laval-montreal-workshop-01` | `.local\evidence\red-speedway-workshop-01` |
| Map folder | `Laval-Montreal` | `redboidraceway` |
| Grid dimensions | 5 x 5 | 2 x 3 |
| Coordinate range | 0_0 to 4_4 | 25_15 to 26_17 |
| `.lotheader` count | 25 | 6 |
| `.lotpack` count | 25 | 6 |
| `chunkdata*.bin` count | 25 | 6 |
| Extra `.bin` | `worldmap.xml.bin` (1) | none observed |
| `mod.info` | Yes | Yes |
| `map.info` | Yes | Yes |
| `spawnpoints.lua` | Yes | Yes |
| `objects.lua` | Yes | Yes |
| `.png` files | 2 (poster/preview) | 3 |
| `.bak` files | 2 | 0 |
| Total file count | 85 | 25 |

### Safety record

| Property | Value |
|---|---|
| Files copied into repo | false |
| PZ assets copied | false |
| media/maps touched in repo | false |
| Playable export claimed | false |
| Binary content parsed | false |

---

## 12. Evidence-derived hypotheses (MAP-4B)

These are working hypotheses derived from file-name and presence observation
only. They are NOT confirmed at the byte level. They do NOT permit MAP-4
implementation to begin.

- **lotheader naming:** `<cx>_<cy>.lotheader` — one file per cell.
- **lotpack naming:** `world_<cx>_<cy>.lotpack` — one file per cell.
  (Differs from initial hypothesis of `<cx>_<cy>.lotpack`.)
- **chunk data naming:** `chunkdata_<cx>_<cy>.bin` — one file per cell.
  (Differs from initial hypothesis of `map_<cx>_<cy>.bin`.)
- **cell file location:** all cell files appear directly under `media/maps/<map_id>/`.
  No cell subdirectory observed.
- **coordinate system:** `<cx>_<cy>` integers; zero-origin and offset both observed.
  The coordinate origin convention (world offset vs local 0,0) requires further
  investigation before any writer can use it.
- **map.info:** present in both observations. Content not read. Required fields unknown.
- **spawnpoints.lua:** present in both observations. Lua text assumed but not confirmed.
  Coordinate system for spawn points not known.
- **objects.lua:** present in both observations. Not in original hypothesis. Role unknown.
- **worldmap.xml.bin:** present in Laval-Montreal (5x5 map); absent in RED-Speedway (2x3 map).
  May be required only for larger maps or a specific WorldEd setting.

All hypotheses require byte-level verification before use in a writer.

---

## 13. Still blocked (MAP-4B)

MAP-4 writer implementation remains blocked. The following remain unknown:

- Byte-level semantics of `.lotheader` field at bytes 4-7 (count assumed but not confirmed).
- Exact format of `.lotheader` tileset-name list (encoding, termination, any padding).
- Full binary format of `.lotpack` beyond the consistent 8-byte header.
- Full binary format of `chunkdata_*.bin` beyond the `0001` prefix.
- Whether a single cell (1x1 grid) is sufficient for a map to load.
- Exact spawn coordinate origin and scale (worldX/worldY cell-grid unit confirmed; absolute origin not confirmed).
- `objects.lua` required fields (if any are mandatory).
- Role and requirement of `worldmap.xml.bin`.
- Whether `lots` in map.info is a directory prefix, a game-world region name, or both.
- Whether `fixed2x = true` in map.info is required or optional.
- Build 41 vs Build 42 format differences.

No load test has been performed. No playable export claim is made.
No writer implementation is permitted until the decision gate in section 8
is fully satisfied.

---

## 14. Text metadata observations (MAP-4C)

`scripts/inspect-map-text-metadata.ps1` was run against both Workshop mods.
No binary files were read. No files were copied. All output is under `.local/`.

### mod.info key fields observed

| Field | Laval-Montreal | RED-Speedway |
|---|---|---|
| `name` | `Laval-Montreal` | `Redboid Speedway` |
| `id` | `Laval-Montreal` | `RED-Speedway` |
| `description` | present (long text) | present |
| `poster` | `poster.png` | `poster.png` |
| `icon` | (not observed) | `mini-poster.png` |
| `authors` | (not observed) | `Bambino` |

### media/maps/<map_id>/map.info key fields observed

| Field | Laval-Montreal | RED-Speedway |
|---|---|---|
| `title` | `Laval-Montreal` | `Redboid Speedway` |
| `lots` | `lavalmontreal` | `Muldraugh, KY` |
| `description` | present | present |
| `fixed2x` | `true` | `true` |

Notes:
- `lots` values differ: Laval-Montreal uses a custom identifier; RED-Speedway
  references `Muldraugh, KY` (a base game location). The `lots` field role
  requires further investigation — it may specify the world-map region where
  the map is placed.
- `fixed2x = true` is present in both. Role is not confirmed; may relate to
  zoom level or tile scale.
- `title` appears to be the display name; `lots` appears to be a location
  identifier. Neither is confirmed as required vs optional.

### spawnpoints.lua format observed (RED-Speedway example)

```lua
function SpawnPoints()
return {
  constructionworker = {
    { worldX = 26, worldY = 16, posX = 200, posY = 196, posZ = 0 }
  },
  fireofficer = {
    { worldX = 26, worldY = 16, posX = 200, posY = 196, posZ = 0 }
  },
  ...
}
```

- The outermost table is keyed by **profession name** (e.g. `constructionworker`).
- Each profession maps to an array of spawn point entries.
- Each spawn point entry has: `worldX`, `worldY` (cell-grid coordinates) and
  `posX`, `posY`, `posZ` (position within the cell).
- The function is named `SpawnPoints` and returns the table.
- Note: `contains_profession_name_tokens` detection in the script searches for
  the literal string `profession` and returned false for RED-Speedway because
  the format uses the profession **names** (constructionworker, fireofficer) as
  keys, not the word `profession` itself.

### spawnpoints.lua summary from text metadata reader

| Field | Laval-Montreal | RED-Speedway |
|---|---|---|
| `file_present` | true | true |
| `line_count` | 3049 | 22 |
| `contains_return_statement` | true | true |
| `contains_profession_name_tokens` | false | false |
| `coordinate_number_count` | 14935 | 30 |

Notes on `coordinate_number_count`: Laval-Montreal's 14935 reflects 3049 lines
of spawn data for a 5x5 cell map; RED-Speedway's 30 reflects a compact 22-line
file. The count is a rough heuristic; it counts all numeric tokens.

### Safety record

| Property | Value |
|---|---|
| Binary files read | false |
| Files copied into repo | false |
| PZ assets copied | false |
| media/maps touched in repo | false |
| Playable export claimed | false |

---

## 15. Binary header evidence (MAP-4D)

`scripts/inspect-compiled-binary-headers.ps1` was run against both Workshop mods
with `-MaxBytes 64 -MaxFilesPerExtension 5`. No files were copied. All output
is under `.local/` only. Only first-64-byte prefixes were read as hex strings.

### .lotheader prefix evidence

| File | Bytes | Bytes 0-3 | Bytes 4-7 (LE int) | Bytes 8-15 (ASCII) |
|---|---:|---|---|---|
| Laval `0_0.lotheader` | 2155 | `00000000` | `33000000` = 51 | `blends_gr...` |
| Laval `0_1.lotheader` | 1970 | `00000000` | `2a000000` = 42 | `blends_gr...` |
| Laval `0_2.lotheader` | 2617 | `00000000` | `4b000000` = 75 | `blends_gr...` |
| Laval `0_3.lotheader` | 5020 | `00000000` | `b9000000` = 185 | `blends_gr...` |
| Laval `0_4.lotheader` | 3370 | `00000000` | `69000000` = 105 | `blends_gr...` |
| Speed `25_15.lotheader` | 1628 | `00000000` | `23000000` = 35 | `blends_na...` |
| Speed `25_16.lotheader` | 10308 | `00000000` | `46010000` = 326 | `BMyers_1\n...` |
| Speed `25_17.lotheader` | 1560 | `00000000` | `1f000000` = 31 | `blends_na...` |
| Speed `26_15.lotheader` | 7076 | `00000000` | `d1000000` = 209 | `appliances...` |
| Speed `26_16.lotheader` | 28435 | `00000000` | `87030000` = 903 | `DylansRan...` |

**Observations:**
- Bytes 0-3: `00 00 00 00` — CONSISTENT across all 10 sampled files from both mods.
- Bytes 4-7: 32-bit LE variable integer — varies per cell. Appears to correlate with
  the number of tileset name entries (more complex/content-rich cells have higher values).
- Bytes 8+: newline-separated (`0x0A`) ASCII tileset pack names, e.g.:
  `blends_grassoverlays_01_`, `blends_natural_01_16`, `BMyers_1`,
  `BZM_Industry_extras_01_3`, `appliances_cooking_01_38`, `DylansRandomFurniture01_`.
  These are Project Zomboid tileset pack identifiers.
- **Hypothesis (not confirmed):** bytes 4-7 = count of tileset name entries;
  bytes 8+ = newline-terminated string list.
- Byte-level semantics require further investigation before writing.

### .lotpack prefix evidence

| File | Bytes | Bytes 0-7 (hex) |
|---|---:|---|
| Laval `world_0_0.lotpack` | 1,258,804 | `84030000241c0000` |
| Laval `world_0_1.lotpack` | 1,228,324 | `84030000241c0000` |
| Laval `world_0_2.lotpack` | 1,267,212 | `84030000241c0000` |
| Speed `world_25_15.lotpack` | 1,104,076 | `84030000241c0000` |
| Speed `world_25_16.lotpack` | 1,233,352 | `84030000241c0000` |

**Observations:**
- Bytes 0-7: `84 03 00 00 24 1c 00 00` — IDENTICAL across ALL 10 sampled files
  from both mods. This is very strong evidence of a fixed-format header.
- `84 03 00 00` as 32-bit LE = 0x0384 = 900. Role unknown (version? magic?).
- `24 1c 00 00` as 32-bit LE = 0x1C24 = 7204. Role unknown (entry count? offset?).
- Bytes 8+: sequence of increasing 4-byte or 8-byte LE values — likely an offset
  or size table. Differences between values appear to be of similar magnitude,
  suggesting fixed-size entries.
- **Full format not decoded.** Writing is not permitted.

### chunkdata .bin prefix evidence

| File | Bytes | Bytes 0-7 (hex) |
|---|---:|---|
| Laval `chunkdata_0_0.bin` | 902 | `0001000000000000` |
| Laval `chunkdata_0_3.bin` | 14,302 | `0001000000000000` |
| Laval `chunkdata_0_4.bin` | 15,702 | `0001030303020808` |
| Speed `chunkdata_25_15.bin` | 10,202 | `0001000000000000` |
| Speed `chunkdata_25_16.bin` | 10,802 | `0001000000000200` |

**Observations:**
- Bytes 0-1: `00 01` — CONSISTENT across all 10 sampled files. Likely a format
  version marker (LE 16-bit value = 1).
- Bytes 2+: variable — mostly zeros for low-content cells; non-zero for cells
  with more data. Full format not decoded.

### Safety record

| Property | Value |
|---|---|
| Binary files copied into repo | false |
| Full binary files read | false |
| Only prefix bytes read | true (max 64 bytes per file) |
| PZ assets copied | false |
| media/maps touched in repo | false |
| Playable export claimed | false |
| Compiled writer implemented | false |
