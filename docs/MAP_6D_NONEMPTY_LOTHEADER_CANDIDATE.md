# MAP-6D: Non-Empty Lotheader Candidate from Committed Evidence

```text
Schema:           pzmapforge.candidate-record.v0.1
Claim boundary:   evidence_record_only_not_load_tested_not_playable
PZ build:         Build 42
MAP-6B status:    BINARY_FAILURE_CONFIRMED
MAP-6C status:    zero-entry candidates; same bytes as known-failing placeholder
MAP-6D scope:     first non-empty lotheader candidate from committed MAP-4E evidence
Playable claim:   PLAYABLE_EXPORT_CLAIM_ALLOWED=false
LOTHEADER_CANDIDATE_V2=newline_tileset_table_minimal
```

---

## 1. Context and motivation

MAP-6B confirmed that the 8-byte all-zero placeholder lotheader fails at runtime:
`java.io.EOFException at IsoLot.readInt`.

MAP-6C added two candidates:
- `current_failed`: 8-byte all-zero placeholder, known failing.
- `newline_tileset_table`: MAP-4E format model, 0 entries — **same bytes as
  `current_failed`**. Not a new test surface.

MAP-6D adds the first candidate that produces different, non-empty bytes:
`newline_tileset_table_minimal` — one grass tileset entry from committed MAP-4E
evidence. This creates a new testable hypothesis rather than repeating the known
zero-entry failure.

---

## 2. Entry source: committed MAP-4E evidence only

The tileset entry name `blends_grassoverlays_01_0` is documented in:
- `docs/COMPILED_CELL_FORMAT_EVIDENCE.md` section 16 (MAP-4E lotheader string
  table evidence), item 3: "newline-delimited ASCII string table — each line is
  a tileset pack name such as: `blends_grassoverlays_01_0`"

No PZ assets were read or copied to derive this name. It was observed from
Workshop mod inspection (MAP-4B) and documented in the committed evidence file.

The MAP-4E evidence also notes:
> "A minimal all-grass cell would reference `blends_grassoverlays_*` and possibly
> `blends_natural_*` entries."

This is the basis for choosing this entry for the minimal candidate.

---

## 3. Exact byte layout

Candidate name: `newline_tileset_table_minimal`
Entry: `blends_grassoverlays_01_0` (25 ASCII bytes + `\n` newline = 26 bytes)
Total file size: **34 bytes**

```
offset  size  value         description
------  ----  -----------   ------------------------------------------
0       4     00 00 00 00   version / reserved (U32 = 0, consistent in 16/16 observed files)
4       4     01 00 00 00   entry count (U32 LE = 1)
8       25    62 6c 65 6e   "blends_grassoverlays_01_0" (ASCII)
              64 73 5f 67
              72 61 73 73
              6f 76 65 72
              6c 61 79 73
              5f 30 31 5f
              30
33      1     0a            newline terminator (0x0A)
```

First 34 bytes in a single hex string:
`0000000001000000626c656e64735f6772617373
6f7665726c6179735f30315f300a`

---

## 4. What this candidate tests

Compared to `current_failed` (8 bytes, known failing), `newline_tileset_table_minimal`
(34 bytes) creates a genuinely different load surface:

1. `IsoLot.readInt` will find more bytes available after byte 8 (the entry data).
   If the EOF was caused by the parser reading past the count field into entry data,
   a non-empty entry list may allow the parser to complete.

2. If the parser requires at least one entry to proceed past the header, the
   zero-entry candidates would fail regardless of count-field format. The minimal
   candidate tests whether 1 entry is sufficient.

3. If the parser reads entry strings using a different method (e.g., Java
   `readUTF()` with 2-byte length prefix rather than newline-terminated), the
   entry bytes will be misread. This would produce a different error than the
   EOFException, giving diagnostic information.

4. If the entry name `blends_grassoverlays_01_0` is not recognized as a valid
   tileset pack in Build 42, PZ may log a different warning/error after
   successfully reading the lotheader.

---

## 5. What this candidate does NOT claim

- It does not claim the lotheader format is correct.
- It does not claim `blends_grassoverlays_01_0` is a required or sufficient entry.
- It does not claim 1 entry is the minimum required count.
- It does not claim PZ will accept the newline-terminated format.
- It has not been load-tested.
- No playable export claim.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.

---

## 6. Report fields

When `--lotheader-candidate newline_tileset_table_minimal` is passed:

| Field | Value |
|---|---|
| `lotheader_candidate` | `"newline_tileset_table_minimal"` |
| `lotheader_candidate_status` | `"generated_not_load_tested"` |
| `lotheader_entry_count` | `1` |
| `lotheader_entries` | `["blends_grassoverlays_01_0"]` |
| `lotheader_byte_count` | `34` |
| `lotheader_sha256` | 64-char lowercase hex of the 34 bytes |
| `lotheader_first_bytes` | hex of first 32 bytes |
| `binary_runtime_status` | `"candidate_generated_not_load_tested"` |
| `playable_export_generated` | `false` |
| `load_tested` | `false` |

---

## 7. Next steps

Before any load test:
1. Prepare a versioned loose-mod packet using the new candidate (same process as
   MAP-6A/maptest_a).
2. Record the test in a `.local/` record template.
3. Operator performs the manual load test.
4. Record the result: what error (if any) does PZ produce?

A new load test using `newline_tileset_table_minimal` must not be performed
without a prepared test packet and record template.

MAP-5B remains LOAD_TEST_INCONCLUSIVE.

---

## 8. Non-claims

- No PZ assets were read or copied.
- No load test was performed.
- No playable Project Zomboid export claim.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
