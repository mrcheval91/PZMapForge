# MAP-6V: LOTH Trailing Body Decode Research

```text
Schema:           pzmapforge.map6v-loth-trailing-decode.v0.1
Claim boundary:   writer_research_only_not_implemented_not_load_tested
BUILD42_LOTH_TRAILING_BODY_DECODED
LOTH_REQUIRES_TRAILING_BINARY_BODY
HYPOTHESIS_TRAILER_UNKNOWN
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6U basis

MAP-6U confirmed that all 20 sampled Dru_map LOTH files have a trailing binary
section after the ASCII string table:
- Trailing bytes range: 7018-33558 per file.
- `field8` exactly matches `ascii_entry_count` (the off-by-one from MAP-6K was a
  parsing artifact -- the trailing binary content stopped being parsed as ASCII).
- MAP-6S/MAP-6T candidate has ZERO trailing bytes -- this is the structural gap.

MAP-6T v1 failure (LOAD_TEST_FAIL_LOTH): `IsoLot.readInt` EOF persists even
after increasing LOTH to 28598 bytes with 1024 ASCII entries. The trailing
body is the missing piece.

---

## 2. MAP-6V decode findings (20 Dru_map reference files)

| Metric | Value |
|---|---|
| Files with trailing body | 20/20 |
| Trailing byte range | 7018-33558 bytes |
| Files where trailing bytes % 4 == 0 (u32-aligned) | 3/20 |
| Files where trailing bytes % 4 != 0 (non-u32-aligned) | 17/20 |
| Files where >50% of first-16 trailer words are < field8 | 6/20 |
| Overall hypothesis | HYPOTHESIS_TRAILER_UNKNOWN |

---

## 3. Key finding: trailing section is NOT u32-aligned

Only 3 of 20 reference LOTH files have a trailing section whose byte count is
divisible by 4. This means the trailing body is NOT a simple array of U32
integers. Possible structures:

1. **Variable-length records with byte-level data**: The trailing section could
   contain per-tile records that include byte or short fields, making the total
   size non-u32-aligned.
2. **UTF-8 or ASCII strings with binary length prefixes**: A second string table
   with different encoding (e.g., length-prefixed rather than newline-delimited).
3. **Interleaved binary and text sections**: Multiple sub-sections with different
   alignments.
4. **Compression or encoding**: The trailing section might be compressed (e.g.,
   zlib) which would produce arbitrary-length output.

The `wordsLtF8` analysis (6/20 files where majority of first-16 trailer words
are < field8) provides weak evidence that some words reference the string table,
but this is not conclusive across all 20 files.

---

## 4. Stable prefix word analysis

The `stable_prefix_word_summary` from the decode report shows which of the first
16 u32 words of the trailing section are stable (same value) across all reference
files. Stability in the first few words would indicate a fixed-size header block
before variable content.

Run `scripts/decode-build42-loth-trailing-body.ps1` and check the
`stable_prefix_word_summary` in the JSON report for the actual stability values.

---

## 5. Is MAP-6W v3 writer defensible?

**No. MAP-6W should NOT attempt a LOTH v3 writer yet.**

Reasons:
1. The trailing section is non-u32-aligned in 17/20 files -- we cannot simply
   append a u32 array.
2. The structure of the trailing section is not yet understood at the byte level.
3. We do not know whether the trailing section is:
   - A fixed-size block
   - Per-entry variable records
   - A compressed section
   - An index/lookup table
4. Without knowing the minimum valid trailing section for an empty grass cell,
   any implementation would be speculative.

---

## 6. What MAP-6W must do instead

MAP-6W must deepen the trailing body analysis before writing:

1. **Inspect trailing section at smaller granularity**: Look at specific byte
   patterns (not just u32 words). Check for 2-byte (U16) alignment, 1-byte
   alignment, known magic bytes.
2. **Cross-reference a known-small reference LOTH**: Use the smallest reference
   (43_43.lotheader at 34920 bytes, 1137 entries, 7018 trailing bytes) to minimize
   variables.
3. **Attempt to parse the trailing section as a known format**: Try U16 records,
   length-prefixed strings, byte arrays with count prefixes.
4. **Compare two adjacent cells**: If cells 43_43 and 43_44 exist, comparing their
   trailing sections may reveal which bytes vary by cell and which are structural.

Only after MAP-6W identifies a defensible minimum trailing structure should
MAP-6X attempt a v3 writer.

---

## 7. Objects.lua secondary issue

The MAP-6T test also logged an `ArrayIndexOutOfBoundsException` on objects.lua.
This needs to be addressed for full load success, but it remains secondary to
the lotheader issue. After the lotheader is resolved, objects.lua must be
validated independently.

---

## 8. Status labels

```text
BUILD42_LOTH_TRAILING_BODY_DECODED
  -- Trailing body was decoded from 20 reference files.

LOTH_REQUIRES_TRAILING_BINARY_BODY
  -- Confirmed from MAP-6U (20/20 files have trailing bytes).

HYPOTHESIS_TRAILER_UNKNOWN
  -- Trailing section is NOT u32-aligned in 17/20 files.
  -- Structure is not yet understood.

WRITER_NOT_CHANGED
  -- No changes to the MAP-6S candidate writer.

LOAD_TEST_NOT_PERFORMED
  -- MAP-6V is research only.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding.
```

---

## 9. Non-claims

- No load test was performed as part of MAP-6V.
- No binary writer was changed.
- No PZ assets were copied or read by PZMapForge scripts.
- No media/maps writes occurred in this repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
