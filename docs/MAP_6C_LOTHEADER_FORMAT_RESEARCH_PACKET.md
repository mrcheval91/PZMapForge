# MAP-6C: Lotheader Format Research Packet and Candidate Writer Gate

```text
Schema:           pzmapforge.research-packet.v0.1
Claim boundary:   evidence_record_only_not_load_tested_not_playable
PZ build:         Build 42
MAP-6B status:    BINARY_FAILURE_CONFIRMED
MAP-6C scope:     lotheader format research + candidate writer gate + objects.lua fix
Playable claim:   PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Context

MAP-6B confirmed that the 8-byte all-zero lotheader placeholder fails at runtime:

```
ERROR loading ...\0_0.lotheader
java.io.EOFException at IsoLot.readInt
IsoMetaGrid$MetaGridLoaderThread.loadCell
IsoLot.load EOFException
```

`IsoLot.readInt` is a Java `DataInputStream.readInt()` equivalent — reads 4 bytes
as a big-endian signed int. The exception fires when the stream has fewer than 4
bytes available. The 8-byte placeholder allows exactly 2 readInt calls (bytes 0-3
and bytes 4-7). Any attempt to read a 3rd int would fail at byte 8+.

This document records:
1. What the MAP-4 evidence tells us about the lotheader format.
2. Why the EOF occurred for the current 8-byte placeholder.
3. The candidate format matrix.
4. What the `--lotheader-candidate` flag gates.
5. The objects.lua syntax fix.

No PZ assets were read or copied. No load test was performed. No playable export
claim is made.

---

## 2. MAP-4E lotheader evidence summary

From `scripts/inspect-lotheader-string-table.ps1` run against 2 Workshop mods
(16 `.lotheader` files, 10 Laval-Montreal + 6 RED-Speedway). Evidence is in
`docs/COMPILED_CELL_FORMAT_EVIDENCE.md` section 16.

| Field | Observed value | Confidence |
|---|---|---|
| Bytes 0-3 | `00 00 00 00` | 16/16 files (100%) |
| Bytes 4-7 | U32 LE = entry count | 14/16 files (87.5%) |
| Bytes 8+ | newline-delimited ASCII tileset pack names | observed in all non-empty files |
| Entry range | 31 to 2450 entries per cell | observed across both mods |
| Entry format | `<pack_name>_<sprite_index>` e.g. `blends_grassoverlays_01_0` | observed |
| After string table | non-printable bytes in 2 complex cells | unexplained |

Interpretation of bytes 0-3 always being 0: likely a version or reserved field.
Interpretation of bytes 4-7 as entry count: matches in 14/16; 2 mismatches
(off by 2-3) may indicate a secondary data section in complex cells.

**Key question raised by MAP-6B failure:** If the format is
`version(4B) + count(4B) + entries(variable)`, then a count of 0 with no
entries should produce a valid 8-byte file. But `IsoLot.readInt` failed on
exactly this 8-byte file. This means either:

(a) The count field is NOT at bytes 4-7 — something else is read first.
(b) A count of 0 is invalid — PZ requires at least one entry.
(c) There is additional required structure after the entry loop (a footer int,
    a checksum, or similar) that the 8-byte file does not provide.
(d) The bytes are correct but `IsoLot.readInt` is reading from a different
    position — e.g., PZ has already read 4+ bytes as a length prefix before
    the structure the probe identified.

None of these can be resolved without decompiling PZ or obtaining a successful
load. The current evidence supports the format hypothesis but does not explain
the EOF failure.

---

## 3. Candidate format matrix

```text
LOTHEADER_CANDIDATE_V0=current_failed
LOTHEADER_CANDIDATE_V0_STATUS=known_failing

LOTHEADER_CANDIDATE_V1=newline_tileset_table
LOTHEADER_CANDIDATE_V1_STATUS=generated_not_load_tested
```

### candidate_v0_current_failed

| Property | Value |
|---|---|
| Bytes | `00 00 00 00 00 00 00 00` (8 bytes) |
| Format model | zero header (4B) + zero count (4B) = 0-entry format |
| Runtime status | FAILING — EOFException at IsoLot.readInt (MAP-6B confirmed) |
| Byte count | 8 |
| Notes | Default; backward compatible with MAP-5A/MAP-5D behavior |

Candidate v0 is the current placeholder. It is known to produce an EOFException.
No new load test is needed; it is definitively failing.

### candidate_v1_newline_tileset_table

| Property | Value |
|---|---|
| Bytes | `00 00 00 00 00 00 00 00` (8 bytes) |
| Format model | version(U32=0) + count(U32LE=0) + empty newline-delimited string table |
| Evidence basis | MAP-4E (section 16 of docs/COMPILED_CELL_FORMAT_EVIDENCE.md) |
| Runtime status | generated_not_load_tested |
| Byte count | 8 |
| Notes | Same bytes as v0 for 0-entry case; distinguished by format model documentation |

Candidate v1 implements the MAP-4E format model explicitly. For 0 tileset entries,
the resulting bytes are identical to candidate v0. The research value is in the
documented format model, not in the bytes themselves.

**Why the bytes are the same:** The MAP-4E model for a 0-entry lotheader is:
```
bytes 0-3:  00 00 00 00   (version/reserved = 0, consistent in 16/16 observed files)
bytes 4-7:  00 00 00 00   (entry count = 0, LE encoding)
bytes 8+:   (empty — no entries)
```

This is the same as candidate v0. The distinction is that v1 is generated from
an explicit implementation of the MAP-4E format model, while v0 was generated as
an unstructured placeholder. If the format model turns out to require entries,
v1 can be extended to write actual tileset names without changing the flag name.

**Note:** Since v0 and v1 produce the same bytes, and v0 is known to fail, v1
will also fail if loaded as-is. The independent value of v1 is:
1. It documents the format model explicitly.
2. When PZ tileset pack names are researched, v1 can be extended to include them.
3. The report marks it `generated_not_load_tested` rather than `known_failing` so
   it is not conflated with the unstructured v0 placeholder.

### candidate_v2_newline_tileset_table_minimal (IMPLEMENTED — MAP-6D)

| Property | Value |
|---|---|
| Bytes | 34 bytes — version(4B=0) + count(4B=1) + `blends_grassoverlays_01_0\n` |
| Format model | MAP-4E format with 1 grass tileset entry from committed evidence |
| Evidence basis | MAP-4E (section 16): "blends_grassoverlays_01_0" documented explicitly |
| Runtime status | generated_not_load_tested |
| Byte count | 34 |
| Notes | See `docs/MAP_6D_NONEMPTY_LOTHEADER_CANDIDATE.md` for byte layout |

```text
LOTHEADER_CANDIDATE_V2=newline_tileset_table_minimal
LOTHEADER_CANDIDATE_V2_STATUS=generated_not_load_tested
```

### candidate_v4_length_prefixed_or_int_table (NOT IMPLEMENTED)

| Property | Value |
|---|---|
| Format model | Alternative: strings are length-prefixed rather than newline-delimited |
| Basis | Speculative — Java `DataInputStream.readUTF()` uses 2-byte length prefix |
| Status | NOT_IMPLEMENTED — insufficient evidence; would require decompiling PZ |
| Byte count | Unknown |

If PZ uses Java's `readUTF()` (2-byte big-endian length + UTF-8 bytes) rather
than a newline-terminated format, then the string table format differs from what
MAP-4E evidence suggested (MAP-4E saw newline bytes but could not distinguish
delimiter vs. terminator). This candidate is documented for research awareness
but not implemented.

---

## 4. objects.lua syntax fix

The MAP-6B failure recorded `LuaManager.RunLuaInternal` exception on the
comment-only `objects.lua`. The file had no return value, which is invalid.

MAP-6C fixes `objects.lua` to:
```lua
return {}
```

This is syntactically valid Lua that returns an empty table. It has not been
load tested. It does not fix the lotheader failure.

Report field: `objects_lua_runtime_status = "syntax_candidate_not_load_tested"`

The previous value `"invalid_or_not_accepted"` applied to the comment-only file.
The new value reflects that the syntax is now valid but the fix has not been
verified by a load test.

---

## 5. `--lotheader-candidate` flag gate

Added to `map-export-experimental` CLI command (MAP-5A flat and MAP-5D build42).

```
--lotheader-candidate current_failed                  (default, backward compatible)
--lotheader-candidate newline_tileset_table            (MAP-4E format model, 0 entries)
--lotheader-candidate newline_tileset_table_minimal    (MAP-6D: 1 grass entry, 34 bytes)
```

Report fields added:
| Field | current_failed | newline_tileset_table | newline_tileset_table_minimal |
|---|---|---|---|
| `lotheader_candidate` | `"current_failed"` | `"newline_tileset_table"` | `"newline_tileset_table_minimal"` |
| `lotheader_candidate_status` | `"known_failing"` | `"generated_not_load_tested"` | `"generated_not_load_tested"` |
| `lotheader_entry_count` | `0` | `0` | `1` |
| `lotheader_entries` | `[]` | `[]` | `["blends_grassoverlays_01_0"]` |
| `lotheader_sha256` | 64-char hex | 64-char hex | 64-char hex (different) |
| `lotheader_first_bytes` | `"0000000000000000"` | `"0000000000000000"` | `"0000000001000000..."` |
| `lotheader_byte_count` | `8` | `8` | `34` |
| `binary_runtime_status` | `"failing_placeholder_format"` | `"candidate_generated_not_load_tested"` | `"candidate_generated_not_load_tested"` |
| `objects_lua_runtime_status` | `"syntax_candidate_not_load_tested"` | `"syntax_candidate_not_load_tested"` | `"syntax_candidate_not_load_tested"` |

---

## 6. Non-claims

- No PZ assets were read or copied.
- No load test was performed.
- No playable Project Zomboid export claim.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
- MAP-5B remains LOAD_TEST_INCONCLUSIVE.
- The newline_tileset_table candidate is NOT expected to succeed without
  further lotheader format research.
- The objects.lua fix (return {}) does not fix the lotheader failure.
