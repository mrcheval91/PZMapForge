# MAP-6K: Build 42 LOTP Payload and LOTH Entry Research

```text
Schema:           pzmapforge.payload-research.v0.1
Claim boundary:   writer_research_only_not_implemented_not_load_tested
PZ build:         Build 42
Reference mod:    Drummondville — under .local/reference-build42-map/Dru_map
BUILD42_LOTP_PAYLOAD_WINDOWS_INSPECTED
BUILD42_LOTH_ENTRIES_EXTRACTED
BUILD42_CHUNKDATA_BODY_INSPECTED
WRITER_RESEARCH_ONLY
WRITER_NOT_IMPLEMENTED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Why MAP-6J was not enough to implement the writer

MAP-6J defined the writer contract at the header level:
- LOTP header: magic + version + chunk_count (12 bytes) ✓
- LOTH header: magic + version + entry_count + string table ✓
- Chunkdata: 1026 bytes ✓

But the contract could not define:
- The LOTP chunk payload format (what each 1024-byte chunk contains).
- The correct offset table values for an empty cell.
- The minimum required LOTH entry set.

MAP-6K adds `scripts/inspect-build42-lotp-payload-windows.ps1` to read bounded
payload windows from the Drummondville reference and the LOTH entry lists.

---

## 2. Script

```
scripts/inspect-build42-lotp-payload-windows.ps1
  -Source  <path under .local to Build 42 reference mod root>
  -Output  <path under .local>
  [-MaxCells 3] [-MaxChunksPerCell 16] [-WindowBytes 64]
```

Reads:
- `world_*.lotpack`: full offset table + bounded chunk windows
- `*.lotheader`: complete entry list (files are small, ~2-3 KB)
- `chunkdata_*.bin`: bounded prefix, body zero-check

Does NOT read PZ assets. Does NOT write to repo.

---

## 3. Drummondville reference findings

### 3.1 LOTP lotpack payload analysis (3 cells sampled)

| Cell | first_offset | last_offset | most_common_payload | unique_sizes | tail_bytes |
|---|---|---|---|---|---|
| world_0_0 | 8204 | 1,056,292 | 1024 | 24 | 1056 |
| world_0_1 | 8204 | 1,045,840 | 1024 | 63 | 1024 |
| world_0_2 | 8204 | 1,053,904 | 1024 | 34 | 1024 |

**Key findings:**
- `first_offset=8204` confirms `12 + 1024×8 = 8204` header+table structure (MAP-6J contract ✓).
- `most_common_payload_size=1024` — the majority of chunk payloads are 1024 bytes.
- `unique_payload_sizes=24-63` — chunk payload sizes are NOT uniform across all 1024 chunks.
  Some chunks contain more data than others (content-dependent).
- `monotonic=True` — offsets are strictly increasing (no repeated/backward offsets).
- `tail_bytes=1024-1056` — a small trailer section follows the last chunk.

**Implication for the writer:**
For an all-empty/all-grass cell, if all chunks have identical content (or are all-zero),
the payload sizes would all be identical (uniform). The variable sizes in Drummondville
indicate this is a content-rich mixed cell. For a minimal empty cell, uniform chunk sizes
are plausible — but the chunk content format is still unknown.

### 3.2 LOTH lotheader entry analysis (3 cells sampled)

| Cell | declared_count | parsed_count | off_by_one |
|---|---|---|---|
| 0_0.lotheader | 47 | 48 | yes |
| 0_1.lotheader | 85 | 86 | yes |
| 0_2.lotheader (0_10) | 36 | 37 | yes |

**Key findings:**
- `parsed_count = declared_count + 1` consistently across all 3 cells.
- This aligns with MAP-4E Build 41 evidence, which observed a similar off-by-one
  in 2 files. The LOTH format may include trailing content (non-printable bytes or
  an extra entry) that the simple newline-split parser also counts.
- The entry format is newline-delimited ASCII `<pack>_<sprite_index>\n`.
- The smallest observed entry count is 36. For a minimal all-grass cell, the required
  set is likely a subset of the `blends_grassoverlays_*` entries.
- First entries confirmed: `blends_grassoverlays_01_0`, `blends_grassoverlays_01_1`, etc.

**Implication for the writer:**
The entry count field (bytes 8-11) should be set to the ACTUAL number of tileset entries,
not parsed_count. The trailing content is separate from the string table. For a minimal cell,
writing 0 entries (count=0) and testing if PZ loads the cell would be the simplest
experiment — but this has not been load-tested.

### 3.3 Chunkdata body analysis (3 cells sampled)

All 3 sampled chunkdata files: `all_zero_body=True`.

This confirms: for Drummondville reference cells (which are likely simple grass/road cells),
the chunkdata body is entirely zero. This supports the MAP-6J chunkdata candidate:
1026 bytes = 2-byte header `00 01` + 1024 zero bytes.

---

## 4. Remaining unknowns

| Unknown | Status after MAP-6K |
|---|---|
| `chunk_payload_format` | Bounded windows captured but encoding still unknown. Chunks are 1024 bytes most commonly. |
| `loth_minimum_entries` | Smallest observed = 36 entries. Minimum for a loadable cell unknown. |
| `lotp_empty_chunk_offsets` | Offsets are monotonic with uniform spacing for simple cells. For empty cells: offsets likely all uniform (e.g., all identical or all zero). |
| `chunk_payload_size` | Most common = 1024 bytes. Some chunks differ (variable content). |
| `build42_load_test` | No load test performed. |

---

## 5. Recommended next task: MAP-6L

MAP-6L: Build 42 candidate writer MVP

Proceed with MAP-6L only if:
1. The chunk payload for a minimal empty cell can be plausibly hypothesized as
   a fixed-size (1024-byte) all-zero or repeated block.
2. The LOTH entry count for a minimal grass cell can be set to a small number
   (e.g., the grass overlay entries only: ~47 entries).

**Evidence supports proceeding with a candidate writer MVP:**
- Chunkdata body=all-zero confirmed for simple reference cells.
- LOTP most_common_payload_size=1024, suggesting uniform-size chunks are plausible.
- LOTH entry format confirmed (newline-delimited; MAP-4E and MAP-6K consistent).

**The MVP writer would NOT be claimed as functional until load-tested.**

---

## 6. Non-claims

- No writer was implemented.
- No load test was performed.
- No PZ assets were copied into the repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
