# MAP-6I: Build 42 Format Design Matrix

```text
Schema:           pzmapforge.build42-format-design-matrix.v0.1
Claim boundary:   writer_design_only_not_implemented_not_load_tested
PZ build:         Build 42
MAP-6H status:    BUILD42_256_MODEL_STRONGLY_SUPPORTED (Drummondville reference)
MAP-6I scope:     word-level stability matrix for LOTP, LOTH, chunkdata; writer design
BUILD42_FORMAT_DESIGN_MATRIX_CREATED
WRITER_DESIGN_ONLY
WRITER_NOT_IMPLEMENTED
GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6H result summary

MAP-6H inspected the Drummondville Build 42 reference:
- 20 LOTP lotpacks (format confirmed)
- 20 LOTH lotheaders (format confirmed)
- 19 chunkdata files with body=1024 (32×32 candidate)
- Status: `BUILD42_256_MODEL_STRONGLY_SUPPORTED`

---

## 2. Purpose

MAP-6I transforms the MAP-6H inspection output into a word-level stability matrix:
for each of the first 16 U32 LE words in LOTP and LOTH files, it classifies
each word position as `stable_magic`, `stable_version`, `stable_unknown`, or
`variable_unknown` across sampled files. This gives us the grounded byte-level
surface needed to design a candidate writer.

---

## 3. Script

```
scripts/derive-build42-format-design-matrix.ps1
  -InspectionReport  <path under .local to build42-reference-geometry-report.json>
  -Output            <path under .local for matrix output>
  [-MaxRecords 50]
```

Both paths must be under `.local/`. Input must match schema
`pzmapforge.build42-reference-geometry-report.v0.2`. No PZ assets are read.

Outputs:
- `build42-format-design-matrix.json`
- `build42-format-design-matrix.md`

---

## 4. Observed Drummondville stability (from smoke run)

### LOTP lotpack (first 16 words, 20 files sampled)

| Position | Value | Label | Notes |
|---|---|---|---|
| 0 | 1347702604 | stable_magic | 0x50544F4C LE = LOTP bytes 4C 4F 54 50 |
| 1 | 1 | stable_version | version = 1 |
| 2 | 1024 | stable_unknown | chunk count = 32×32 = 1024 |
| 3 | 8204 | stable_unknown | first chunk offset = 12 + 1024×8 |
| 4 | 0 | stable_unknown | high word of chunk offset 1 (U64 layout) |
| 5 | 9228 | stable_unknown | second chunk offset (8204 + 1024) |
| ... | alternating | stable_unknown | offset table entries, each chunk = 1024 bytes |
| 14 | 0 | stable_unknown | high word |
| 15 | 14348 | stable_unknown | 7th chunk offset |

All 16 words stable across Drummondville files (all identical grass cells).
Words 3,5,7,9,11,13,15 = arithmetic series: 8204, 9228, 10252, ...

**Inference:** LOTP format (header 12 bytes + offset table 1024×8 bytes = 8204 bytes) then chunk data. Chunk data appears ~1024 bytes each for simple grass cells.

### LOTH lotheader (first 16 words, 20 files sampled)

| Position | Value | Label | Notes |
|---|---|---|---|
| 0 | 1213484876 | stable_magic | 0x48544F4C LE = LOTH bytes 4C 4F 54 48 |
| 1 | 1 | stable_version | version = 1 |
| 2 | variable | variable_entry_count | entry count — 47 to 85+ depending on cell |
| 3-15 | same bytes | stable_unknown | tileset pack name string bytes |

Interpretation of words 3+: these are the ASCII bytes of "blends_grassoverlays_01_0\n..." read as U32 LE words. They appear stable because many cells share the same prefix tileset entries.

---

## 5. Candidate writer design

### LOTP lotpack candidate

```
bytes 0-3:   4C 4F 54 50       "LOTP" magic
bytes 4-7:   01 00 00 00       version = 1
bytes 8-11:  00 04 00 00       chunk_count = 1024 (U32 LE, 32x32)
bytes 12+:   offset table      1024 x 8-byte chunk offsets
             chunk data        unknown format — requires further research
```

Offset table structure (from stable word observation):
- First chunk at byte 8204 (= 12 + 1024×8)
- Each entry: [offset_u32le, 0] (8 bytes per chunk, little-endian 64-bit)
- For all-zero content: all chunks at the same offset? Or equal-spaced? Unknown.
- For an empty cell, the chunk data format and size are not yet known.

### LOTH lotheader candidate

```
bytes 0-3:   4C 4F 54 48       "LOTH" magic
bytes 4-7:   01 00 00 00       version = 1
bytes 8-11:  NN 00 00 00       entry_count (U32 LE, depends on tileset usage)
bytes 12+:   <entry>\n         newline-delimited ASCII tileset pack names
                               (same format as MAP-4E Build 41 evidence)
```

Minimum entry count for an empty/grass cell: **unknown**. Candidate: write
entries observed for simple RED-Speedway grass cells (31+ entries), starting
with `blends_grassoverlays_01_0`. Exact minimum requires a load test.

### Chunkdata candidate

```
bytes 0-1:   00 01             2-byte header (Build 41 and Build 42 consistent)
bytes 2-1025: 00×1024          32x32 chunk grid, all-zero (hypothesis only)
```

Total size: 1026 bytes.

---

## 6. What we can safely write now (candidate only, not load-tested)

| Component | Bytes we can write | Confidence |
|---|---|---|
| LOTP magic + version + chunk_count | bytes 0-11 | stable_observed |
| LOTP offset table header | bytes 12-8203 | derivable (1024×8) |
| LOTH magic + version | bytes 0-7 | stable_observed |
| LOTH entry_count | bytes 8-11 | variable per cell |
| LOTH tileset entries | bytes 12+ | depends on cell content |
| Chunkdata header | bytes 0-1 | stable_observed |
| Chunkdata body (32×32 zero) | bytes 2-1025 | hypothesis only |

**NOT safe to write yet:**
- LOTP chunk data format (content, compression, encoding)
- LOTP chunk offset table — are offsets for empty cells zero-size chunks?
- LOTH minimum entry set for a cell to load

---

## 7. Status labels

```text
BUILD42_FORMAT_DESIGN_MATRIX_CREATED
BUILD42_LOTP_FORMAT_OBSERVED
BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED
BUILD42_32X32_CHUNK_GRID_OBSERVED
BUILD42_256_MODEL_STRONGLY_SUPPORTED
WRITER_DESIGN_ONLY
WRITER_NOT_IMPLEMENTED
GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 8. Recommended next task: MAP-6J

MAP-6J: Build 42 candidate writer contract

- Define the exact byte layout for a minimal all-empty LOTP lotpack.
  - Key unknown: chunk data format (offset table + content for 1024 all-empty chunks).
- Define the exact byte layout for a minimal LOTH lotheader.
  - Key unknown: minimum required tileset entry set.
- Create a deterministic candidate writer for LOTP, LOTH, and chunkdata.
- Prepare a versioned load-test packet.
- **No load test until the packet is reviewed and approved.**

---

## 9. Non-claims

- No writer has been implemented.
- No load test has been performed.
- No PZ assets were copied into the repo.
- The stability observations are from a single reference mod (Drummondville).
  Other mods or content-heavy cells may show different patterns.
- `BUILD42_256_MODEL_STRONGLY_SUPPORTED` is evidence-based inference, not proven.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
