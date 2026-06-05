# MAP-6H: Build 42 LOTP / LOTH Deep Reference Inspection

```text
Schema:           pzmapforge.deep-inspection-record.v0.1
Claim boundary:   evidence_record_only_not_load_tested_not_playable
PZ build:         Build 42
Reference mod:    Dru_map (Drummondville) — manually copied to .local
BUILD42_LOTP_FORMAT_OBSERVED
BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED
BUILD42_32X32_CHUNK_GRID_OBSERVED
BUILD42_256_MODEL_STRONGLY_SUPPORTED
GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Why MAP-6G was not enough to implement a writer

MAP-6G confirmed:
- Build 42 lotpacks use `LOTP` magic (bytes 0-3 = `4C 4F 54 50`).
- The legacy `hdrA=900 / hdrB=7204` chunk-offset-table format does not apply.
- Chunkdata files are 1026 bytes (body=1024, candidate 32×32 grid).

What MAP-6G did NOT tell us:
- The internal structure of the LOTP lotpack after the 8-byte header.
- The number of fields, their sizes, and their roles.
- Whether the LOTP format still contains a chunk-offset table (just reformatted).
- The internal structure of the lotheader beyond the magic + version bytes.
- Whether the `00 01` chunkdata header changed between Build 41 and Build 42.

Until the LOTP and LOTH formats are understood at a field level, no writer can
be safely implemented. MAP-6H extends the inspector to capture bounded word-level
prefixes of all three file types.

---

## 2. LOTP lotpack evidence

### Magic bytes

```
bytes 0-3:  4C 4F 54 50  =  "LOTP" (ASCII)
bytes 4-7:  01 00 00 00  =  U32 LE = 1  (version or first header field)
```

### What this means

- Build 42 lotpacks use a named format with a 4-byte ASCII magic marker.
- The value `1` at bytes 4-7 is likely a version number.
- The bytes after offset 8 are unknown without further inspection.
- The legacy chunk-offset table (hdrA=900 entries × 8 bytes) does NOT apply.
- The current PZMapForge experimental writer (`hdrA=900, hdrB=7204, 7208-byte file`)
  produces files that are not valid Build 42 lotpacks.

### Deep prefix fields (MAP-6H inspector)

The MAP-6H inspector now captures:
- `first_16_bytes_hex`
- `first_32_bytes_hex`
- `first_64_bytes_hex`
- `u32le_words_first_64` — 16 U32 LE words from the first 64 bytes

These fields allow direct inspection of LOTP prefix data without implementing
a full parser.

---

## 3. LOTH lotheader evidence

### Magic bytes

```
bytes 0-3:  4C 4F 54 48  =  "LOTH" (ASCII)
bytes 4-7:  01 00 00 00  =  U32 LE = 1  (version or first header field)
```

From the Drummondville reference run, all 20 lotheader files had:
- `first_u32le = 1213484876` (0x485A544C in big-endian notation)
- In file byte order (LE): `4C 54 5A 48` = "LOTH"
- `second_u32le = 1` (bytes 4-7)

### Status: BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED

The lotheader format appears to have changed in Build 42, analogous to the LOTP
change for lotpacks. Build 42 lotheaders begin with "LOTH" magic.

The MAP-4E lotheader evidence (newline-delimited tileset string table, bytes 0-3 = 0)
was from Workshop mods not explicitly identified as Build 41 or Build 42. That
evidence likely describes Build 41 lotheaders. Build 42 uses LOTH format instead.

### Deep prefix fields (MAP-6H inspector)

- `first_16_bytes_hex`
- `first_32_bytes_hex`
- `first_64_bytes_hex`
- `u32le_words_first_64`

---

## 4. Chunkdata evidence

### File structure (Drummondville observation)

| Property | Observed value |
|---|---|
| File size | 1026 bytes (19/20 sampled) |
| Header bytes 0-1 | `00 01` (consistent with Build 41 observation) |
| Body bytes | 1024 = size - 2 |
| Chunk grid candidate | 32×32 = 1024 chunks |

### Status: BUILD42_32X32_CHUNK_GRID_OBSERVED

If Build 42 cells use 8 tiles per chunk: 32 × 8 = 256 tiles per side → 256×256 cell.
This is consistent with the operator-reported BUILD42_256_MODEL_OPERATOR_REPORTED.

The chunkdata `00 01` header is the same as observed in Build 41 mods. Whether the
meaning changed is not yet known.

### Deep prefix fields (MAP-6H inspector)

- `first_32_bytes_hex`
- `u32le_words_first_32` — 8 U32 LE words

---

## 5. Combined status

When LOTP + LOTH + chunkdata 1024 are all observed together:

```text
BUILD42_256_MODEL_STRONGLY_SUPPORTED
```

This is the combined status when:
- `lotpack_lotp_count > 0` (LOTP format confirmed)
- `lotheader_ltz_count > 0` (LOTH format confirmed)
- `chunkdata_body_1024_count > 0` (32×32 chunk grid observed)

"Strongly supported" does not mean "confirmed by load test." It means the three
independent observations are consistent with the 256×256 tile model. A load test
with a correctly formatted output is required to confirm.

---

## 6. Full geometry status array (MAP-6H)

The inspector now reports `geometry_statuses` as an array:

| Status | Condition |
|---|---|
| `BUILD42_LOTP_FORMAT_OBSERVED` | Any LOTP lotpack found |
| `BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED` | Any LOTH lotheader found |
| `BUILD42_32X32_CHUNK_GRID_OBSERVED` | Any chunkdata body=1024 found |
| `BUILD42_256_MODEL_STRONGLY_SUPPORTED` | All three above present |
| `BUILD41_30X30_CHUNK_GRID_OBSERVED` | Any chunkdata body=900 found |
| `GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED` | Always present |
| `PLAYABLE_EXPORT_CLAIM_ALLOWED=false` | Always present |

---

## 7. What remains unknown

1. LOTP lotpack internal structure after bytes 0-7.
2. Whether LOTP still contains a chunk data offset table.
3. LOTH lotheader internal structure after bytes 0-7.
4. Whether LOTH still contains a tileset string table.
5. Whether the chunkdata `00 01` header changed meaning in Build 42.
6. Whether 8 tiles per chunk is the actual Build 42 chunk size.
7. Whether a single-cell 256×256 map loads without additional files.

---

## 8. Non-claims

- No writer has been implemented.
- No load test has been performed.
- No PZ assets were copied into the repo.
- The MAP-4E tileset string table evidence applies to Build 41 mods, not confirmed for Build 42.
- `BUILD42_256_MODEL_STRONGLY_SUPPORTED` is evidence-based inference, not load-test proof.
- No playable export claim.
- GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
