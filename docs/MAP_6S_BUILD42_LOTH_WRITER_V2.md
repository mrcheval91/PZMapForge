# MAP-6S: Build 42 LOTH Candidate Writer v2

```text
Schema:           pzmapforge.map6s-loth-writer-v2.v0.1
Claim boundary:   build42_candidate_only_not_load_tested_not_playable
Candidate:        pzmapforge_build42_candidate_v1
Profile:          empty_grass_v1
BUILD42_LOTH_WRITER_V2_IMPLEMENTED
LOTH_ENTRY_COUNT=1024
LOTH_ENTRY_STRATEGY=generated_contiguous_grass_overlay_range
LOTH_KNOWN_RISK=generated_entries_may_not_match_loaded_tile_definitions
LOTP_UNCHANGED
CHUNKDATA_UNCHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6Q failure basis

MAP-6Q confirmed that PZ Build 42 fails on the MAP-6L LOTH lotheader at:

```text
java.io.EOFException at IsoLot.readInt(IsoLot.java:75)
```

The MAP-6Q comparison smoke showed:
- Candidate: 38 bytes
- Smallest reference LOTH (Dru_map): 34920 bytes
- Candidate smaller than all references: true

The MAP-6L LOTH has `entry_count=1` and one ASCII entry. References have
entry_count in the range 920-2007.

---

## 2. MAP-6R structure basis

MAP-6R inspected 20 reference LOTH files (Dru_map, 512-byte prefix) and found:

| Finding | Value |
|---|---|
| magic | LOTH (stable, 20/20) |
| version | 1 (stable, 20/20) |
| binaryGap (bytes 12+ not immediately ASCII) | False (20/20) |
| field8 range | 920-2007 |

Key result: **no binary section between the 12-byte header and the string table.**
Bytes 12+ are immediately newline-delimited ASCII entries. The LOTH format is:
```text
bytes 0-3:   LOTH magic
bytes 4-7:   version=1 (U32 LE)
bytes 8-11:  entry_count (U32 LE)
bytes 12+:   newline-delimited ASCII entries
```

The MAP-6L writer structure is directionally correct. The only gap is the
entry count and table scale.

---

## 3. MAP-6S LOTH v2 emission strategy

Profile: `empty_grass_v1`

### Entry generation strategy

Generate 1024 contiguous entries of the form:
```text
blends_grassoverlays_01_0
blends_grassoverlays_01_1
...
blends_grassoverlays_01_1023
```

The base name `blends_grassoverlays_01_0` is the committed MAP-4E evidence entry
(docs/COMPILED_CELL_FORMAT_EVIDENCE.md section 16). The contiguous range 0-1023
is a candidate hypothesis -- not copied from any reference mod.

### Why not Dru_map entries

Dru_map LOTH entry lists are NOT embedded in PZMapForge source. Reasons:
1. Dru_map is a third-party Workshop mod. Embedding its tile definition list
   would constitute asset reproduction.
2. The entry list is not needed -- the entry NAMES tell PZ which tilesets to
   reference at load time. If the referenced tilesets don't exist or have
   different IDs, PZ may log a warning but the format itself should be valid.
3. The MAP-6S hypothesis is that providing a correctly-structured LOTH with
   enough entries will pass `IsoLot.readInt`. The specific entry content is
   a secondary concern.

### Entry count rationale

- MAP-6K: smallest observed entry_count = 36 (Drummondville).
- MAP-6R: all 20 sampled Dru_map cells have field8 = 920-2007.
- MAP-6S chooses 1024 as a round power-of-two above the MAP-6K minimum (36)
  and within the MAP-6R range.
- 1024 is deterministic and independent of any reference mod's tile set.

### Candidate risk

`loth_known_risk = generated_entries_may_not_match_loaded_tile_definitions`

PZ may accept the LOTH structure (passing IsoLot.readInt) but log warnings
or reject cells at tile-render time if the entry names don't match installed
tilesheet definitions. This is a secondary failure mode; the primary goal is
to pass `IsoLot.readInt`.

---

## 4. LOTH v2 exact format

| Field | Offset | Bytes | Value |
|---|---|---|---|
| Magic | 0 | 4 | 4C 4F 54 48 (LOTH) |
| Version | 4 | 4 | 01 00 00 00 (U32 LE = 1) |
| Entry count | 8 | 4 | 00 04 00 00 (U32 LE = 1024) |
| String table | 12 | variable | 1024 x "blends_grassoverlays_01_N\n" |

Total size: 12 + sum(len("blends_grassoverlays_01_N") + 1) for N in 0..1023
- N=0-9:    10 x 26 = 260
- N=10-99:  90 x 27 = 2430
- N=100-999: 900 x 28 = 25200
- N=1000-1023: 24 x 29 = 696
Total string table: 28586 bytes
Total file: 12 + 28586 = 28598 bytes (candidate target)

Actual size is confirmed by test: must be >= 25000 and > 38.

---

## 5. LOTP and chunkdata unchanged

LOTP and chunkdata are unchanged from MAP-6L `empty_grass_v0`:
- `world_0_0.lotpack`: 1,056,780 bytes (LOTP magic + 1024 x 1024-byte zero chunks)
- `chunkdata_0_0.bin`: 1,026 bytes (00 01 + 1024 zero bytes)

`LOTP_NOT_REACHED` and `CHUNKDATA_NOT_REACHED` from MAP-6Q remain true for
`empty_grass_v0`. With `empty_grass_v1`, if the LOTH passes IsoLot.readInt,
PZ will attempt to read the LOTP next. LOTP acceptance remains unproven.

---

## 6. Command

```
dotnet run --project src/PZMapForge.Cli -- map-export-experimental \
  --map-id <id> \
  --output .local/<dir> \
  --build42-candidate-writer \
  --build42-candidate-profile empty_grass_v1
```

Output under `.local/<dir>/<id>_build42_candidate/42/`.

---

## 7. Remaining unknowns

| Unknown | Status after MAP-6S |
|---|---|
| `loth_generated_entry_acceptance` | Unknown. PZ may accept or reject unrecognized entry names. |
| `lotp_zero_payload_load_acceptance` | Unknown. LOTP not reached in MAP-6Q. |
| `chunkdata_zero_body_acceptance` | Unknown. Chunkdata not reached in MAP-6Q. |
| `build42_load_test` | Not performed. |

---

## 8. Status labels

```text
BUILD42_LOTH_WRITER_V2_IMPLEMENTED
  -- empty_grass_v1 profile added to the CLI candidate writer.

LOTH_ENTRY_COUNT=1024
  -- 1024 generated contiguous grass overlay entries.

LOTH_ENTRY_STRATEGY=generated_contiguous_grass_overlay_range
  -- Entries generated in source; not copied from any reference mod.

LOTH_KNOWN_RISK=generated_entries_may_not_match_loaded_tile_definitions
  -- Entry names may not match installed tilesheet definitions.

LOTP_UNCHANGED
  -- world_0_0.lotpack is identical to MAP-6L empty_grass_v0 output.

CHUNKDATA_UNCHANGED
  -- chunkdata_0_0.bin is identical to MAP-6L empty_grass_v0 output.

LOAD_TEST_NOT_PERFORMED
  -- MAP-6S does not perform a PZ load test.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding until a LOAD_TEST_PASS record is committed.
```

---

## 9. Recommended next task: MAP-6T

MAP-6T: Build 42 LOTH v2 manual load-test packet and controlled retest.

MAP-6T should:
1. Use the MAP-6M/MAP-6O packet workflow to prepare a copy-ready packet for
   the `empty_grass_v1` candidate.
2. Update the server ini and spawnregions.lua as established in MAP-6P/MAP-6O.
3. Record results with the MAP-6N triage tool on a fresh console.txt.
4. If LOTH passes and LOTP fails: next slice is LOTP writer v2.
5. If LOTH passes and load succeeds: record LOAD_TEST_PASS.
6. If LOTH fails again: inspect the exact IsoLot read position and revise v2.

---

## 10. Non-claims

- MAP-6S does not perform a load test.
- No playable Project Zomboid map was produced.
- Entries are generated -- not copied from any Workshop mod or reference LOTH.
- No PZ assets were copied or read by PZMapForge scripts.
- No media/maps writes occurred in this repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
