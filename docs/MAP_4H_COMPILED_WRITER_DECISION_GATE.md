# MAP-4H: Compiled Writer Decision Gate Report

```text
Schema:           pzmapforge.decision-gate.v0.1
Date:             2026-06-04
Claim boundary:   evidence_inventory_only_not_compiled_not_pz_load_tested
Compiler status:  not implemented
Decision:         MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY
```

---

## 1. Purpose

This document is the formal decision gate for MAP-5A. It answers whether
PZMapForge has collected sufficient evidence to begin an experimental minimal
compiled-cell writer slice, strictly under `.local/` only, with no playable
export claim.

This document does not implement a writer. It does not produce compiled output.
It does not claim that the experiment will succeed.

---

## 2. Evidence summary by artifact type

Seven evidence probes (MAP-4A through MAP-4G) were run against two Workshop mods:
Laval-Montreal (5×5 grid) and RED-Speedway (2×3 grid). The table below
summarizes what is known and what remains unknown for each file type.

### 2.1 Text metadata (map-scaffold — already implemented in MAP-3B)

| File | Status | Key facts |
|---|---|---|
| `mod.info` | Known structure | key=value; id, name, description, poster |
| `media/maps/<id>/map.info` | Known structure | title, lots, description, fixed2x; which fields required is unclear |
| `media/maps/<id>/spawnpoints.lua` | Known structure | `function SpawnPoints() return { profession = { {worldX, worldY, posX, posY, posZ} } }` |
| `media/maps/<id>/objects.lua` | Presence confirmed | Content format not inspected |

MAP-3B (`map-scaffold`) already writes these four text files. No additional
writer work is needed for this layer.

### 2.2 .lotheader

| Field | Status | Evidence |
|---|---|---|
| Bytes 0-3 | Known | `00 00 00 00` — consistent 16/16 files |
| Bytes 4-7 | Strong candidate | U32 LE = entry count; exact match 14/16 (88%) |
| Bytes 8+ | Known structure | Newline-delimited (`0x0A`) ASCII tileset pack names |
| Entry count range | Observed | 31 to 2450 per cell |
| 2 count mismatches | Risk | Complex cells off by 2-3; embedded non-printable bytes in those files |
| Minimal cell tileset list | Unknown | What tileset names does an empty/blank cell require? |
| Non-printable bytes in complex cells | Unknown | Role of bytes after string table in large lotheaders |

**Experimental hypothesis (minimal empty cell):** A `.lotheader` with
`00000000` header + entry_count=0 as U32 LE + empty string table (no tileset
references). Assumes PZ accepts a cell that references no tilesets.

**Risk:** If PZ requires at least one tileset reference (e.g., a base ground tile),
a 0-entry lotheader would fail to load. The cell would appear blank/corrupted
in-game rather than causing a crash.

### 2.3 .lotpack

| Field | Status | Evidence |
|---|---|---|
| Bytes 0-3 (hdrA) | Known | U32 LE = 900 — constant 16/16; matches 30×30 chunks/cell |
| Bytes 4-7 (hdrB) | Known | U32 LE = 7204; formula: 4 + 900×8 = 7204 — exact 16/16 |
| Bytes 8-7207 | Known structure | 900-entry × 8-byte table: {0x00000000, chunk_file_offset_U32} |
| Chunk offsets | Known pattern | Monotonically increasing; variable (city) or constant (uniform terrain) |
| Gap section (bytes 7208→first_chunk) | **UNKNOWN** | 1204–1432 bytes; content unread |
| Chunk data blocks | **UNKNOWN** | Internal format completely unknown |

**Experimental hypothesis (minimal empty cell):** A `.lotpack` with:
- Bytes 0-7: `84 03 00 00 24 1c 00 00` (hdrA=900, hdrB=7204)
- Bytes 8-7207: 900 × 8 zero bytes (all chunk offsets = 0)
- No additional bytes (hypothesis: offset 0 means "no chunk data")

**Assumptions stated explicitly:**
- That a chunk offset of `0x00000000` is interpreted by PZ as "this chunk has no data."
- That no gap section is needed when all chunk offsets are zero.
- That PZ does not validate that chunk offset 0 ≠ header region.

**Risk:** If PZ interprets offset 0 as "data starts at byte 0 of the file" rather
than "no data present", it will read the header as chunk data and likely crash or
produce garbage. This is the primary risk for MAP-5A.

**Mitigation:** The experiment is local-only. A crash in PZ while loading a local
test map does not affect the game, save files, or repo.

### 2.4 chunkdata_*.bin

| Field | Status | Evidence |
|---|---|---|
| Bytes 0-1 | Known | `00 01` — consistent 16/16 files |
| Base file size | Known | 902 bytes = 2 (header) + 900 (30×30 grid); confirmed in 2 simple cells |
| Chunk grid (bytes 2-901) | Strong candidate | One byte per chunk; all-zero for empty cells |
| Extended section (bytes 902+) | **UNKNOWN** | Variable; present only in non-empty cells |
| Grid byte semantics (0x02, 0x03, 0x08) | **UNKNOWN** | Flags or type codes; meanings not confirmed |

**Experimental hypothesis (minimal empty cell):**
- Bytes 0-1: `00 01`
- Bytes 2-901: 900 × `0x00`
- No extended section
- Total: 902 bytes exactly

**Risk:** The lowest-risk hypothesis of the three. Two simple all-grass cells in
the Laval-Montreal mod are exactly 902 bytes with one nonzero byte (the `0x01`
header byte). This pattern is internally consistent.

**Residual risk:** The 902-byte all-zero file may be valid only for a cell that
existed in a specific game state. PZ may require at least one non-zero chunk entry
even for a "blank" cell.

---

## 3. Known safe facts (no assumption required)

The following facts are directly observed and require no inference:

1. **Directory layout:** `media/maps/<map_id>/` flat layout confirmed in 2 mods.
2. **Cell file naming:** `<cx>_<cy>.lotheader`, `world_<cx>_<cy>.lotpack`,
   `chunkdata_<cx>_<cy>.bin` per cell — confirmed naming pattern.
3. **lotheader header:** Bytes 0-3 = `00000000`, consistent 16/16.
4. **lotpack header:** hdrA=900, hdrB=7204, consistent 16/16.
5. **chunkdata header:** Bytes 0-1 = `00 01`, consistent 16/16.
6. **chunkdata minimum size:** 902 bytes for empty cells — formula 2+900 exact.
7. **lotheader entry count:** 14/16 files have byte-accurate entry count at bytes 4-7.
8. **lotpack offset table:** 900-entry × 8-byte structure with {0, offset} pairs confirmed.
9. **Text metadata:** mod.info, map.info, spawnpoints.lua format confirmed.
   map-scaffold (MAP-3B) already writes these.

---

## 4. Remaining unknowns

| Unknown | Impact on MAP-5A | Severity |
|---|---|---|
| lotpack gap section content (bytes 7208 to first_chunk) | Experimental write may produce invalid file | HIGH |
| Whether lotpack offset=0 means "no chunk" | Experimental write may crash PZ | HIGH |
| lotheader: minimal tileset list for blank cell | Cell may render as error/purple tiles instead of blank | MEDIUM |
| chunkdata grid byte semantics (0x02, 0x03, 0x08) | Empty cell experiment uses all-zero; may be acceptable | LOW |
| chunkdata extended section format | Not needed for empty cell experiment | LOW |
| Whether 1 cell loads without a world grid | Load test required to determine | HIGH |
| map.info `lots` field semantics | May affect whether mod appears in PZ | MEDIUM |
| Whether `fixed2x = true` is required | Cell may not render at correct scale | LOW |
| objects.lua content format | Placeholder file likely acceptable | LOW |
| Build 41 vs Build 42 format differences | Unknown if experiment targets correct build | MEDIUM |

---

## 5. Risk table

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| PZ crashes on zero chunk offsets | MEDIUM | Game crash (local only) | Acceptable for local experiment; restart PZ |
| lotheader 0-entry causes load failure | MEDIUM | Cell not visible in game | Start with a few known grass tile names instead |
| lotpack gap section zeros cause format error | MEDIUM-HIGH | Map fails to load | Document failure; probe gap section in MAP-4I if needed |
| Single cell does not load (world grid required) | UNKNOWN | Map not accessible | Document failure; investigate world grid requirement |
| Wrong PZ version targeted | LOW | Files incompatible | Note PZ version in boundary README |
| Accidental write outside .local | NONE | n/a | CLI guard enforced; refuse non-.local output |
| PZ assets copied or modified | NONE | n/a | Not required for this experiment |

---

## 6. Smallest experimental writer hypothesis

For a single experimental cell at coordinates `(0, 0)` with map_id `pzmapforge_test`:

### 6.1 File list (under `.local/` only)

```text
.local/map-export/pzmapforge_test/
  mod.info                                        (already handled by map-scaffold)
  media/maps/pzmapforge_test/
    map.info                                      (already handled by map-scaffold)
    spawnpoints.lua                               (already handled by map-scaffold)
    objects.lua                                   (placeholder text file)
    README_PZMAPFORGE_BOUNDARY.txt                (boundary document)
    0_0.lotheader                                 (EXPERIMENTAL — new in MAP-5A)
    world_0_0.lotpack                             (EXPERIMENTAL — new in MAP-5A)
    chunkdata_0_0.bin                             (EXPERIMENTAL — new in MAP-5A)
```

### 6.2 `.lotheader` hypothesis (8 bytes — zero-entry variant)

```
Bytes 0-3:  00 00 00 00   (reserved/version, consistent 16/16)
Bytes 4-7:  00 00 00 00   (U32 LE entry count = 0, experimental)
             (no string table bytes follow — empty file after header)
```

Total: 8 bytes.

**Fallback variant (1 grass tile entry):** If 0-entry fails, retry with one
known grass tileset name:
```
Bytes 4-7:  01 00 00 00   (1 entry)
Bytes 8+:   blends_grassoverlays_01_0\n   (24 bytes including newline)
```

### 6.3 `.lotpack` hypothesis (7208 bytes — zero-offset variant)

```
Bytes 0-3:   84 03 00 00   (hdrA = 900, consistent 16/16)
Bytes 4-7:   24 1c 00 00   (hdrB = 7204, consistent 16/16)
Bytes 8-7207: 00...00      (7200 zero bytes — 900 × {0x00000000, 0x00000000})
              (all chunk offsets = 0 — experimental; assumes 0 means "no chunk")
```

Total: 7208 bytes. No gap section. No chunk data blocks.

**Assumption:** PZ interprets chunk offset `0` as "this chunk is absent/empty"
and does not attempt to read chunk data for it.

### 6.4 `chunkdata_0_0.bin` hypothesis (902 bytes — lowest risk)

```
Bytes 0-1:   00 01         (consistent header, 16/16)
Bytes 2-901: 00...00       (900 zero bytes — empty chunk grid)
```

Total: 902 bytes. Directly matches observed pattern for simple grass cells.

### 6.5 Required boundary language in all generated files

Every generated file must contain `README_PZMAPFORGE_BOUNDARY.txt` in the same
directory. The lotheader, lotpack, and chunkdata files are binary and cannot
contain embedded text. The README compensates.

---

## 7. Decision

```text
DECISION: MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY
```

**Rationale:**

The structural evidence collected across MAP-4A through MAP-4G is sufficient to
form credible, internally consistent hypotheses for all three binary file types.
The remaining unknowns (lotpack gap section, zero-offset semantics) cannot be
resolved by further evidence probes alone — they require attempting a write and
performing a manual load test.

The experiment is safe to attempt because:
1. Output is under `.local/` only. No game installation is modified.
2. No PZ assets are read or copied.
3. No claim of playable export is made before a successful manual load test.
4. A failed experiment (PZ does not load the cell) produces diagnostic information
   that would close OPEN gaps more effectively than another evidence probe.
5. The chunkdata hypothesis is backed by direct observation of 902-byte all-zero
   cells in two Workshop mods — this is the strongest single candidate.

The two HIGH-severity unknowns (lotpack zero-offset semantics, single-cell load
viability) can only be resolved by the experiment itself. Waiting for more evidence
cannot answer these questions without writing and loading.

**This decision does NOT claim the experiment will succeed.** It authorizes a
strictly bounded attempt. If the experiment fails, the failure mode is documented
and MAP-5A produces evidence for the next iteration.

---

## 8. Required safeguards for MAP-5A

All of the following are mandatory. No MAP-5A slice may proceed without them.

### 8.1 Command and output

- The CLI command must include "experimental" in its name:
  `map-export-experimental` or equivalent.
- Output must be under `.local/` only. Refuse any other path.
- Explicitly refuse any path containing the PZ install directory.
- Explicitly refuse any path containing `media/maps` outside `.local/`.
- Generated output must be distinct from map-scaffold output (separate output root).

### 8.2 Boundary language

- Every generated file set must include `README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt`.
- The README must state:
  - "EXPERIMENTAL OUTPUT — NOT VALIDATED"
  - "Not a playable Project Zomboid map."
  - "Not load-tested."
  - "Generated binary files are hypothesis-only."
  - "Do not redistribute."
  - "Do not claim Project Zomboid compatibility."
- The generated report (JSON + MD) must include `playable_export_generated: false`
  and `load_tested: false`.
- Generated stdout must print all boundary flags before any file paths.

### 8.3 Evidence tracking

- The writer must log every assumption it makes to its output report:
  - "lotheader: assuming 0-entry tileset list"
  - "lotpack: assuming chunk offset 0 = no chunk data"
  - "chunkdata: assuming 902-byte all-zero structure"
- The writer must log the PZ build version if known.
- On completion, the operator must run the manual load test and record results.

### 8.4 Manual load test gate

- No claim of playable export may be made until the operator:
  1. Loads the generated mod in Project Zomboid.
  2. Observes the cell is accessible and renders without error.
  3. Records the result in a local evidence file.
- The load test result must be committed to the repo as a new evidence doc
  before any public or promotional claim is made.

### 8.5 No PZ assets

- The writer must not read from the PZ install directory.
- The writer must not copy any PZ game asset into `.local/` or the repo.
- All generated content must be PZMapForge-authored data only.

### 8.6 No proof packet sync

- MAP-5A does not require proof packet sync unless a new validate.ps1 section
  is added that changes the PS lane count.

---

## 9. MAP-5A scope boundary

MAP-5A is permitted to write **only the following**, under `.local/` only:

| File | Permitted | Notes |
|---|---|---|
| `<cx>_<cy>.lotheader` | YES | Experimental; 0-entry or minimal-entry hypothesis |
| `world_<cx>_<cy>.lotpack` | YES | Experimental; 7208-byte zero-offset hypothesis |
| `chunkdata_<cx>_<cy>.bin` | YES | Experimental; 902-byte all-zero hypothesis |
| `mod.info` | YES | Already ratified in MAP-3B (map-scaffold) |
| `media/maps/<id>/map.info` | YES | Already ratified in MAP-3B |
| `media/maps/<id>/spawnpoints.lua` | YES | Already ratified in MAP-3B |
| `media/maps/<id>/objects.lua` | YES | Placeholder text file (new) |
| `README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt` | YES | Required |

MAP-5A is **forbidden** from writing:

- `worldmap.xml.bin` — unknown format; omit entirely.
- `.tmx` or `.pzw` files — not part of the minimal cell experiment.
- Any file outside the minimal list above.
- Any file outside `.local/`.
- Any file inside a PZ install directory.

MAP-5A target: **one map_id, one cell, coordinates (0, 0)**.

---

## 10. What success and failure tell us

### If MAP-5A load test PASSES (cell appears in PZ)

- Zero-offset lotpack is valid for empty chunks. ✓
- 902-byte all-zero chunkdata is valid for empty cells. ✓
- 0-entry (or minimal) lotheader is valid for a blank cell. ✓
- Single-cell map loads without a world grid. ✓
- Minimum viable cell count = 1. ✓
- MAP-5B (tile-referenced cell) is unblocked.

### If MAP-5A load test FAILS (cell does not appear or PZ crashes)

Diagnostic information collected:
- Error message (if PZ provides one) → narrows which file format is wrong.
- Whether PZ crashes vs. shows an empty cell vs. shows an error screen.
- Failure mode determines which gap to probe next (likely lotpack gap section).

MAP-4I (lotpack gap section probe) would be the next evidence slice if the
lotpack is identified as the failure cause.

---

## 11. Decision summary

| Item | Value |
|---|---|
| Decision | `MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY` |
| Decision date | 2026-06-04 |
| Evidence basis | MAP-4A through MAP-4G (7 evidence slices) |
| Target scope | 1 map, 1 cell (0,0), experimental local-only |
| Blocking risks | lotpack zero-offset assumption; single-cell load not tested |
| Required safeguards | Sections 8.1-8.6 above |
| Playable claim permitted | NO — not until manual load test passes and is documented |
| Proof packet sync required | NO |
| Next step | MAP-5A — experimental local-only compiled cell writer |
