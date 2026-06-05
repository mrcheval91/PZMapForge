# MAP-6J: Build 42 Writer Contract

```text
Schema:           pzmapforge.build42-writer-plan.v0.1
Claim boundary:   writer_plan_only_not_implemented_not_load_tested
PZ build:         Build 42
Evidence basis:   MAP-6G/MAP-6H/MAP-6I (Drummondville reference; format design matrix)
BUILD42_WRITER_CONTRACT_CREATED
WRITER_CONTRACT_ONLY
WRITER_NOT_IMPLEMENTED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Purpose

MAP-6J codifies the Build 42 binary format contracts derived from reference
inspection (MAP-6G/H/I) into an exact byte-level definition. This document and
the accompanying schema/example define what a candidate writer MUST produce,
what remains unknown, and what preconditions must hold before any load test.

No writer is implemented here. No load test is performed.

---

## 2. LOTP lotpack writer contract

Evidence basis: MAP-6G (LOTP magic confirmed), MAP-6I (offset table structure
inferred from Drummondville word stability analysis).

### Byte layout

```
offset  size    value / description
------  ------  -----------------------------------------------
0       4       4C 4F 54 50   "LOTP" magic (stable, 20/20 observed)
4       4       01 00 00 00   version = 1 U32 LE (stable, 20/20 observed)
8       4       00 04 00 00   chunk_count = 1024 U32 LE (stable, 20/20 observed; 32x32)
12      8192    offset_table  1024 x 8-byte chunk offsets (U64 LE candidates)
8204    ?       chunk_data    UNKNOWN FORMAT — see section 2.1
```

Offset table entry format (inferred from Drummondville word analysis):
- Each entry is 8 bytes (U64 LE candidate)
- First chunk at byte 8204 (= 12 + 1024×8)
- Subsequent entries appear to be consecutive with ~1024 bytes per chunk for
  identical empty cells (arithmetic series observed in prefix words)
- Format interpretation: `[offset_u32le, 0]` = low 32 bits of U64 + high 32 bits = 0

### 2.1 Unknown: chunk payload format

The content and encoding of each chunk's data block within the LOTP lotpack
is **not yet known**. The reference inspection captured only the first 64 bytes
of each file (the header + beginning of the offset table). The actual chunk data
was not read.

Required before writer: inspect LOTP chunk data by reading more bytes from a
reference file, or implement a candidate all-zero strategy and attempt a load test.

Candidate strategy (hypothesis only, not proven):
- For an all-empty cell, use a repeated placeholder block per chunk.
- Size per chunk: the offset table suggests each chunk occupies ~1024 bytes in
  the reference; but for an empty cell the size may differ.

### 2.2 Derivable from contract

The following can be written deterministically from the contract:
- Bytes 0-3: magic `4C 4F 54 50`
- Bytes 4-7: version `01 00 00 00`
- Bytes 8-11: chunk_count `00 04 00 00` (1024, little-endian)
- Bytes 12-8203: offset table (values depend on chunk payload size — unknown)

---

## 3. LOTH lotheader writer contract

Evidence basis: MAP-6H (LOTH magic confirmed), MAP-6I (word stability: magic
and version stable, entry_count variable, bytes 12+ = tileset pack names).
The string table format is consistent with MAP-4E Build 41 evidence.

### Byte layout

```
offset  size    value / description
------  ------  -----------------------------------------------
0       4       4C 4F 54 48   "LOTH" magic (stable, 20/20 observed)
4       4       01 00 00 00   version = 1 U32 LE (stable, 20/20 observed)
8       4       NN 00 00 00   entry_count U32 LE (variable per cell; min unknown)
12      N×?     string_table  newline-delimited ASCII tileset pack names
```

String table format: each entry is `<pack_name>_<sprite_index>\n` where `\n`
is byte `0x0A`. This is consistent with the MAP-4E Build 41 evidence.

### 3.1 Unknown: minimum required entry set

The minimum required number and set of tileset entries for a LOTH lotheader to
be accepted by Build 42 is **not yet known**. For simple grass cells in the
Drummondville reference, entry counts ranged from 47 to 85.

Candidate minimal set (hypothesis only):
- Start with `blends_grassoverlays_01_0` (observed in MAP-4E as first entry
  for grass cells; also seen in the Drummondville reference prefix bytes).
- The minimum count is unknown — 0 entries may or may not be accepted.

For an all-empty/grass cell, the candidate minimal entry set must be researched
before any load test claim can be made.

---

## 4. Chunkdata writer contract

Evidence basis: MAP-6H (1026-byte files, body=1024 observed in 19/20 Drummondville
files), MAP-6G (chunkdata body analysis confirmed 32x32 candidate grid).

### Byte layout

```
offset  size    value / description
------  ------  -----------------------------------------------
0       2       00 01   2-byte header (consistent Build 41 + Build 42)
2       1024    body    32x32 chunk grid, 1 byte per chunk
```

Total size: 1026 bytes.

Body candidate: all-zero (hypothesis only — not proven by load test).
Body format: 1 byte per chunk for 32×32 = 1024 bytes. The meaning of each byte
(flags, type codes, etc.) is not yet confirmed.

---

## 5. File set contract

### Required files per cell

| File name | Role | Format |
|---|---|---|
| `{x}_{y}.lotheader` | Lotheader | LOTH candidate |
| `world_{x}_{y}.lotpack` | Lotpack | LOTP candidate |
| `chunkdata_{x}_{y}.bin` | Chunk data | chunkdata candidate |
| `map.info` | Map metadata | Text key=value |
| `spawnpoints.lua` | Spawn points | Lua |
| `objects.lua` | Objects | Lua (return {}) |
| `mod.info` | Mod metadata | Text key=value |

### Directory layout

Build 42 versioned loose-mod layout (confirmed by MAP-6A):
```
<mods>/<mod_folder>/42/
  mod.info
  media/
    maps/
      <map_id>/
        {x}_{y}.lotheader
        world_{x}_{y}.lotpack
        chunkdata_{x}_{y}.bin
        map.info
        spawnpoints.lua
        objects.lua
```

---

## 6. Cell geometry model

| Property | Value | Status |
|---|---|---|
| Chunk grid | 32×32 | Strongly supported (Drummondville reference) |
| Chunk count | 1024 | Stable in LOTP word[2] (20/20 observed) |
| Cell tile model | 256×256 | Strongly supported (32×8=256, BUILD42_256_MODEL_STRONGLY_SUPPORTED) |
| Tiles per chunk | 8×8 | Hypothesized (256/32=8; not confirmed by load test) |
| Source cell_size | 300 (planning pipeline) | Legacy; requires update for Build 42 |

---

## 7. Explicit unknowns

| Unknown ID | Description |
|---|---|
| `chunk_payload_format` | LOTP chunk data encoding, compression, and content format |
| `loth_minimum_entries` | Minimum required tileset entry set for a loadable LOTH lotheader |
| `lotp_empty_chunk_offsets` | How to encode offsets for all-empty chunks (zero? sequential?) |
| `chunk_payload_size` | Actual size of each chunk's data block in the LOTP file |
| `build42_load_test` | No load test has been performed; all formats are candidate-only |

---

## 8. Preconditions for any load test

Before any manual load test of a Build 42 candidate output:
1. All three binary files must be generated from this contract.
2. The candidate output must pass a pre-flight inspection (comparable to MAP-5B/MAP-6F).
3. The unknowns for chunk payload format must be addressed (even a placeholder strategy).
4. A versioned loose-mod packet must be prepared under `.local/` only.
5. A fillable test record template must be prepared.
6. No load test is performed until the operator reviews the packet.

---

## 9. Status labels

```text
BUILD42_WRITER_CONTRACT_CREATED
WRITER_CONTRACT_ONLY
WRITER_NOT_IMPLEMENTED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 10. Non-claims

- No writer has been implemented.
- No load test has been performed.
- No PZ assets were copied into the repo.
- All byte layouts are candidate-only. None have been confirmed by a successful load test.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
