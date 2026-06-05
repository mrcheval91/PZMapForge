# MAP-6L: Build 42 Candidate Writer MVP

```text
Schema:           pzmapforge.build42-candidate-report.v0.1
Claim boundary:   build42_candidate_only_not_load_tested_not_playable
PZ build:         Build 42
Evidence basis:   MAP-6J writer contract, MAP-6K payload research, MAP-4E committed evidence
Profile:          empty_grass_v0
BUILD42_CANDIDATE_WRITER_IMPLEMENTED
WRITER_SCOPE=candidate_only_not_load_tested
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6K evidence basis

MAP-6K confirmed:
- LOTP first_offset = 8204 (contract: 12 + 1024×8).
- Most common LOTP chunk payload = 1024 bytes.
- Chunkdata body all-zero for simple reference cells.
- LOTH entry format: newline-delimited ASCII, same as MAP-4E Build 41 evidence.
- Smallest observed declared entry count = 36.

MAP-6L implements a minimal candidate writer based on these findings.

---

## 2. Command

```
map-export-experimental
  --map-id <id>
  --output .local/<dir>
  --build42-candidate-writer
  --build42-candidate-profile empty_grass_v0
```

Output goes to:
```
.local/<dir>/<map_id>_build42_candidate/
  42/
    mod.info
    poster.png
    experimental-map-export-report.json
    experimental-map-export-report.md
    media/maps/<map_id>/
      map.info
      spawnpoints.lua
      objects.lua
      thumb.png
      README_PZMAPFORGE_BOUNDARY_BUILD42_CANDIDATE.txt
      0_0.lotheader
      world_0_0.lotpack
      chunkdata_0_0.bin
```

The `42/` layout is the Build 42 versioned loose-mod layout confirmed by MAP-6A.

---

## 3. Exact binary bytes emitted

### 3.1 chunkdata_0_0.bin

```
offset  size  value             description
------  ----  ----------------  -----------
0       2     00 01             header (consistent Build 41 + Build 42)
2       1024  00 × 1024         all-zero body (MAP-6H evidence: simple cells are all-zero)
total:  1026 bytes
```

Status: `chunkdata_candidate = zero_body_1024`, `chunkdata_status = generated_not_load_tested`

### 3.2 0_0.lotheader

```
offset  size  value                      description
------  ----  -------------------------  -----------
0       4     4C 4F 54 48                LOTH magic (MAP-6H confirmed)
4       4     01 00 00 00                version = 1 (MAP-6H confirmed)
8       4     01 00 00 00                entry_count = 1 (U32 LE)
12      26    blends_grassoverlays_01_0  MAP-4E committed evidence entry
+1      1     0A                         newline terminator
total:  38 bytes
```

Entry source: `blends_grassoverlays_01_0` — explicitly documented in
`docs/COMPILED_CELL_FORMAT_EVIDENCE.md` section 16 (MAP-4E). No PZ assets used.

Status: `loth_entries_source = committed_evidence_only_map4e`, `loth_status = generated_not_load_tested`

### 3.3 world_0_0.lotpack

```
offset   size     value             description
-------  -------  ----------------  -----------
0        4        4C 4F 54 50       LOTP magic (MAP-6G confirmed)
4        4        01 00 00 00       version = 1 (MAP-6H confirmed)
8        4        00 04 00 00       chunk_count = 1024 U32 LE (MAP-6I confirmed)
12       8192     offset table      1024 × 8-byte U64 LE chunk offsets
8204     1048576  payload           1024 chunks × 1024 zero bytes each
total:   1056780 bytes
```

Offset table: `offset[i] = 8204 + i × 1024` (sequential, all high U32 = 0).
Payload: all-zero (candidate hypothesis — uniform empty chunks).

Status: `lotp_payload_strategy = uniform_zero_1024_per_chunk`, `lotp_status = generated_not_load_tested`

### 3.4 Smoke inspection results

After running `inspect-build42-lotp-payload-windows.ps1` against the generated output:
- `first_offset = 8204` ✓
- `last_offset = 1055756` (= 8204 + 1023 × 1024) ✓
- `monotonic_offsets = True` ✓
- `most_common_payload_size = 1024` ✓
- `unique_sizes = 1` (all chunks identical) ✓
- `tail_bytes = 1024` (last chunk, no extra trailer) ✓
- `all_zero_body = True` ✓

---

## 4. File size expectations

| File | Expected size |
|---|---|
| `0_0.lotheader` | 38 bytes |
| `world_0_0.lotpack` | 1,056,780 bytes |
| `chunkdata_0_0.bin` | 1,026 bytes |

---

## 5. Remaining unknowns

| Unknown | Notes |
|---|---|
| `lotp_zero_payload_load_acceptance` | PZ may require non-zero chunk content for a valid LOTP lotpack |
| `loth_minimum_entries_acceptance` | 1 entry may not be enough; minimum required set unknown |
| `missing_trailer_acceptance` | Reference Drummondville files had tail_bytes=1024-1056; candidate has no extra trailer |
| `build42_load_test` | No load test has been performed |

---

## 6. Report fields

```json
{
  "build42_candidate_writer": true,
  "build42_candidate_profile": "empty_grass_v0",
  "writer_implemented": true,
  "writer_scope": "candidate_only_not_load_tested",
  "load_tested": false,
  "playable_export_generated": false,
  "playable_export_claimed": false,
  "chunkdata_status": "generated_not_load_tested",
  "loth_status": "generated_not_load_tested",
  "lotp_status": "generated_not_load_tested"
}
```

---

## 7. Recommended next task: MAP-6M

MAP-6M: Build 42 candidate writer inspection packet and manual load-test record.

Proceed with MAP-6M to:
1. Prepare a versioned loose-mod packet from the MAP-6L candidate output.
2. Write a fillable load-test record template.
3. Operator manually copies to `.local/` and performs the load test.
4. Record the result.

No load test is performed until the packet is reviewed and the operator explicitly triggers it.

---

## 8. Non-claims

- No load test has been performed.
- No PZ assets were copied into the repo.
- `empty_grass_v0` profile is hypothesis-only.
- `PLAYABLE_EXPORT_CLAIM_ALLOWED=false` is binding.
