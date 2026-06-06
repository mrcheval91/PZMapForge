# MAP-6U: LOTH v2 Failure and Full LOTH Body Research

```text
Schema:           pzmapforge.map6u-loth-v2-failure-record.v0.1
Claim boundary:   writer_research_only_not_implemented_not_load_tested
Candidate:        pzmapforge_build42_candidate_v1_001
Profile:          empty_grass_v1
MAP6T_CLEAN_V1_LOAD_TEST_RECORDED
EMPTY_GRASS_V1_LOTHEADER_REJECTED
CURRENT_CANDIDATE_LOTHEADER_EOF
LOTP_NOT_REACHED
CHUNKDATA_NOT_REACHED
OBJECTS_LUA_SECONDARY_PARSE_ERROR_OBSERVED
LOAD_TEST_FAIL_LOTH
WRITER_NOT_CHANGED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6T clean retest result

MAP-6T executed a controlled retest with the `empty_grass_v1` candidate after
fixing all MAP-6P spawn activation wiring gaps:
- host.ini updated to `PZMF_B42_LOTH_V2_TEST_001`
- `Mods=pzmapforge_build42_candidate_v1_001`
- `Map=pzmapforge_build42_candidate_v1_001;Muldraugh, KY`
- spawnregions references `media/maps/pzmapforge_build42_candidate_v1_001/spawnpoints.lua`

Triage on fresh console.txt:
| Field | Value |
|---|---|
| current_candidate_matches | 3 |
| stale_maptest_a_matches | 0 |
| candidate_specific_exception_found | true |
| result_recommendation | CURRENT_CANDIDATE_EXCEPTION_FOUND |

---

## 2. Exact failure

```text
ERROR loading:
  ...\pzmapforge_build42_candidate_v1_001\0_0.lotheader

Exception:
  java.io.EOFException at IsoLot.readInt(IsoLot.java:75)

Stack:
  zombie.iso.IsoLot.readInt(IsoLot.java:75)
  zombie.iso.IsoMetaGrid$MetaGridLoaderThread.loadCell(IsoMetaGrid.java:...)
```

This is the same exception class and method as MAP-6Q. Increasing the string
table from 1 entry (38 bytes) to 1024 entries (28598 bytes) did not resolve
the EOF at `IsoLot.readInt`.

---

## 3. What the MAP-6S scale increase achieved and did not achieve

| Aspect | MAP-6Q (v0, 38 bytes) | MAP-6T (v1, 28598 bytes) |
|---|---|---|
| LOTH magic | LOTH | LOTH |
| Version | 1 | 1 |
| Entry count | 1 | 1024 |
| String table size | 26 bytes | 28586 bytes |
| Error | IsoLot.readInt EOF | IsoLot.readInt EOF |
| Same exception line | yes | yes |

The error is identical despite a ~750x increase in LOTH file size. This
strongly suggests the EOF is NOT caused by the string table being too short.

---

## 4. Revised hypothesis

MAP-6K noted a consistent `parsed_count = declared_count + 1` off-by-one pattern
across all 3 sampled Drummondville cells. MAP-6R found `binaryGap=False` using
a 512-byte prefix, but 512 bytes only covers the very start of a file that can
be 34-76 KB. The trailing content was never inspected.

Current hypothesis: **Build 42 LOTH files contain a binary trailing section after
the newline-delimited ASCII string table.** `IsoLot.readInt` at line 75 attempts
to read one or more integer fields from this binary section. The MAP-6S candidate
has no trailing section -- the file ends immediately after the last ASCII entry.

This would explain why:
- `parsed_count = declared_count + 1` (the off-by-one is the trailing binary section
  being partially mis-parsed as an extra ASCII entry).
- Increasing the string table did not fix the EOF (PZ reads past the string table
  and expects binary data that is not present).

MAP-6U inspects the FULL file body of reference LOTH files to determine:
1. Whether there are trailing bytes after the string table.
2. How many trailing bytes and what their structure is.
3. Whether the trailing section is consistent across cells.

---

## 5. Secondary issue: objects.lua parse exception

A Lua parse exception / `ArrayIndexOutOfBoundsException` was observed in the
same test session, triggered by the `objects.lua` file.

Status: `OBJECTS_LUA_SECONDARY_PARSE_ERROR_OBSERVED`

This is noted but is NOT the primary blocker. The `objects.lua` issue occurred
after the lotheader failure and may be a downstream effect. The `objects.lua`
candidate was previously fixed (MAP-6C) to return `{}`. The Lua error may
require further investigation after the lotheader is resolved.

---

## 6. LOTP and chunkdata remain unproven

```text
LOTP_NOT_REACHED: PZ stopped at lotheader. LOTP not tested.
CHUNKDATA_NOT_REACHED: PZ stopped at lotheader. Chunkdata not tested.
```

---

## 7. Status labels

```text
MAP6T_CLEAN_V1_LOAD_TEST_RECORDED
  -- Clean retest of empty_grass_v1 recorded. No stale contamination.

EMPTY_GRASS_V1_LOTHEADER_REJECTED
  -- PZ Build 42 rejected the MAP-6S LOTH even at 28598 bytes / 1024 entries.

CURRENT_CANDIDATE_LOTHEADER_EOF
  -- java.io.EOFException at IsoLot.readInt(IsoLot.java:75) confirmed.

LOTP_NOT_REACHED
  -- world_0_0.lotpack not exercised. Acceptance unknown.

CHUNKDATA_NOT_REACHED
  -- chunkdata_0_0.bin not exercised. Acceptance unknown.

OBJECTS_LUA_SECONDARY_PARSE_ERROR_OBSERVED
  -- Lua parse / ArrayIndexOutOfBoundsException observed on objects.lua.
  -- Secondary issue; lotheader remains primary blocker.

LOAD_TEST_FAIL_LOTH
  -- Result for MAP-6T v1 retest.

WRITER_NOT_CHANGED
  -- The MAP-6S candidate writer was not changed in MAP-6U.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding.
```

---

## 8. Next required investigation

Run `scripts/inspect-build42-loth-full-body.ps1` against reference files to determine:
- Whether trailing bytes exist after the ASCII string table.
- How many trailing bytes per file.
- Whether trailing bytes form a stable binary structure.

If `LOTH_REQUIRES_TRAILING_BINARY_BODY` is confirmed, MAP-6V must implement
a v3 LOTH writer that appends a correctly-structured binary trailer.

---

## 9. Non-claims

- No load test was performed as part of MAP-6U.
- No binary writer was changed.
- No PZ assets were copied or read by PZMapForge scripts.
- No media/maps writes occurred in this repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
