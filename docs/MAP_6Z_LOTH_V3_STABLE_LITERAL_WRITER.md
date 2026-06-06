# MAP-6Z: Build 42 LOTH v3 Stable Literal Trailer Writer

```text
Schema:           pzmapforge.map6z-loth-v3-stable-literal-writer.v0.1
Claim boundary:   build42_candidate_only_not_load_tested_not_playable
BUILD42_LOTH_V3_STABLE_LITERAL_WRITER_IMPLEMENTED
LOTH_TRAILER_STRATEGY=map6y_stable_literal_1048_block
WRITER_SCOPE=candidate_only_not_load_tested
LOTP_UNCHANGED
CHUNKDATA_UNCHANGED
OBJECTS_LUA_SECONDARY_PARSE_ISSUE_PENDING
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6Y basis

MAP-6Y analysed 80 Dru_map simple-cell .lotheader files:

| Finding | Value |
|---|---|
| selected_file_count | 80 |
| unique_trailer_sha256_count | 1 |
| all_1048_blocks_identical | true |
| stable_byte_count | 1048 / 1048 |
| variable_byte_count | 0 |
| hypothesis | HYPOTHESIS_1048_BLOCK_FULLY_CONSTANT |
| writer_readiness | WRITER_MAYBE_DEFENSIBLE_WITH_STABLE_LITERAL_1048_BLOCK |

All 80 sampled simple cells have an IDENTICAL 1048-byte trailing block.

**Canonical trailer:**

| Bytes | Value |
|---|---|
| 0-3 | 0x08 0x00 0x00 0x00 (U32LE = 8) |
| 4-7 | 0x08 0x00 0x00 0x00 (U32LE = 8) |
| 8-1047 | 0x00 ... 0x00 (1040 zero bytes) |
| SHA-256 | 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7 |

---

## 2. Why v3 writer is now maybe defensible

The MAP-6X/MAP-6Y discovery chain established:
- MAP-6X: all 40 smallest cells have EXACTLY 1048 trailing bytes (U32-aligned).
- MAP-6Y: all 80 sampled cells have IDENTICAL trailing blocks.

MAP-6T confirmed that the LOTH v1 candidate (28598 bytes, no trailer) fails with
`IsoLot.readInt EOFException`. The trailer is the unconfirmed element.

Since MAP-6Y shows the trailer is a fixed, fully-stable 1048-byte block, embedding
the canonical literal is the minimum viable attempt to unblock the LOTH EOF.

---

## 3. Exact v3 LOTH structure

```text
Offset  Bytes  Field
0       4      LOTH magic (0x4C 0x4F 0x54 0x48)
4       4      version = 1 (U32LE)
8       4      entry_count = 1024 (U32LE)
12      28586  ASCII string table:
               blends_grassoverlays_01_0\n
               blends_grassoverlays_01_1\n
               ...
               blends_grassoverlays_01_1023\n
28598   1048   canonical stable trailer from MAP-6Y
               [0x08 0x00 0x00 0x00 0x08 0x00 0x00 0x00 0x00 ... (1040 zero bytes)]
-------
TOTAL:  29646  bytes
```

ASCII table size: 28586 bytes.
- Entries 0-9: 26 bytes each (24 prefix + 1-digit suffix + newline) x 10 = 260
- Entries 10-99: 27 bytes each x 90 = 2430
- Entries 100-999: 28 bytes each x 900 = 25200
- Entries 1000-1023: 29 bytes each x 24 = 696
- Total: 28586 ✓

Trailer start offset: 12 + 28586 = 28598.
Total file size: 28598 + 1048 = 29646 bytes.

---

## 4. Canonical 1048-byte trailer: source and attribution

- **Source:** MAP-6Y reference analysis under `.local/map6y-loth-fixed-1048/`.
- **Reference:** 80 Dru_map (Drummondville) .lotheader files, all_1048_blocks_identical=true.
- **Not copied from PZ game assets.** Derived from analysis of reference .local data.
- **Embedded as a literal in the writer** with full attribution (MAP-6Z).
- **Applies only to profile empty_grass_v2.** Not used in v0 or v1.
- **loth_known_risk:** `stable_reference_block_may_not_match_generated_tile_table_or_cell_payload`

The block is structurally simple: first two U32LE words are 8, remaining 1040 bytes
are zero. It likely represents a minimal cell grid or metadata block. Its exact
semantic meaning is not decoded; we embed it as-is from reference evidence.

---

## 5. LOTP unchanged

The LOTP (lotpack) format is unchanged from MAP-6L/MAP-6S:
- LOTP magic + version=1 + 1024 chunks x 1024 zero bytes.
- File size: 1056780 bytes.
- Status: generated_not_load_tested.
- LOTP behavior at load time remains unproven.

---

## 6. chunkdata unchanged

The chunkdata format is unchanged from MAP-6L:
- 1026 bytes: 00 01 header + 1024 zero-byte body.
- Status: generated_not_load_tested.
- chunkdata behavior at load time remains unproven.

---

## 7. objects.lua secondary parse issue

The objects.lua secondary parse error (`LuaManager.RunLuaInternal`) observed in MAP-6B
remains pending. It is not addressed in MAP-6Z. `return {}` is the current candidate
(MAP-6C). The LOTH EOF is the primary blocker for the current load test. If LOTH v3
passes, objects.lua is the next blocker.

---

## 8. Profile: empty_grass_v2

CLI usage:

```
dotnet run --project src/PZMapForge.Cli -- map-export-experimental \
  --map-id <id> --output .local/<dir> \
  --build42-candidate-writer \
  --build42-candidate-profile empty_grass_v2
```

Report fields added:
- `loth_trailer_strategy`: map6y_stable_literal_1048_block
- `loth_trailer_size`: 1048
- `loth_trailer_status`: generated_not_load_tested
- `loth_trailer_sha256`: SHA-256 of the embedded trailer
- `loth_known_risk`: stable_reference_block_may_not_match_generated_tile_table_or_cell_payload
- `remaining_unknowns`: includes loth_trailer_acceptance_at_eof

---

## 9. Tests

28 process-level xUnit assertions in `MapExportBuild42CandidateWriterV2ProcessTests`:
1. Exits 0.
2. 42/ directory exists.
3. 0_0.lotheader exists.
4. LOTH magic = 4C 4F 54 48.
5. LOTH version = 1.
6. LOTH entry_count = 1024.
7. 1024 ASCII entries before trailer.
8. First entry = blends_grassoverlays_01_0.
9. Last entry = blends_grassoverlays_01_1023.
10. Trailer size = 1048 bytes.
11. Trailer starts at expected offset (12 + ascii_table_bytes).
12. Trailer SHA-256 matches MAP-6Y canonical + structure-derived value.
13. Total size = 12 + ascii_table_bytes + 1048.
14-23. Report field assertions.
24-27. LOTP and chunkdata unchanged.
28. Output outside .local refused.

---

## 10. Non-claims

- `LOAD_TEST_NOT_PERFORMED`: MAP-6Z is implementation only. No PZ session.
- `PLAYABLE_EXPORT_CLAIM_ALLOWED=false`: binding. No playable claim.
- `LOTP_UNCHANGED`: LOTP behavior at load time remains unknown.
- `CHUNKDATA_UNCHANGED`: chunkdata behavior at load time remains unknown.
- `OBJECTS_LUA_SECONDARY_PARSE_ISSUE_PENDING`: not addressed here.
- No PZ assets copied or read into the repo.
- No repo media/maps writes.
- Candidate only. No PZ compatibility claim.

---

## 11. Recommended next task: MAP-7A

MAP-7A: Controlled Build 42 LOTH v3 load-test packet and retest.

Goals for MAP-7A:
1. Prepare a v2 candidate packet for human-only copy to PZ mods.
2. Perform a controlled manual load test with the v2 candidate.
3. Record the result: LOAD_TEST_PASS / LOAD_TEST_FAIL / LOAD_TEST_INCONCLUSIVE.
4. If FAIL: record which file failed and what the next blocker is.
5. If PASS: record the exact conditions and update CLAIM_BOUNDARY.

No load test is claimed by MAP-6Z. The v2 candidate must be manually verified
before any compatibility claim can be made.
