# MAP-6W: LOTH Trailing Byte Pattern Research

```text
Schema:           pzmapforge.map6w-loth-byte-pattern-research.v0.1
Claim boundary:   writer_research_only_not_implemented_not_load_tested
BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED
LOTH_REQUIRES_TRAILING_BINARY_BODY
HYPOTHESIS_TRAILER_UNKNOWN
WRITER_NOT_DEFENSIBLE
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6V basis

MAP-6V showed the LOTH trailing section is NOT u32-aligned in 17/20 reference
files, leading to HYPOTHESIS_TRAILER_UNKNOWN. MAP-6W deepens the analysis to
the byte and U16 level.

---

## 2. MAP-6W smoke results (20 Dru_map reference files)

| Metric | Value |
|---|---|
| Files with trailing body | 20/20 |
| Trailing bytes range | 7018-33558 |
| Mod2-aligned (U16-aligned) | 10/20 |
| Mod4-aligned (U32-aligned) | 3/20 |
| Avg entropy | 2.657 bits |
| Avg U16 string-index ratio | 0.245 |
| Files with LP-U16 segments | 0/20 |
| Files with compression candidates | 0/20 |
| Overall hypothesis | HYPOTHESIS_TRAILER_UNKNOWN |
| Writer readiness | WRITER_NOT_DEFENSIBLE |
| Recommended next step | MAP-6X_DEEPEN_ANALYSIS |

---

## 3. Why U32 model is rejected

MAP-6V established that 17/20 reference files have trailing bytes not divisible
by 4. MAP-6W confirms this and adds:
- Even mod2 (U16) alignment covers only 10/20 files (50%).
- Mod8 alignment is even rarer.
- No single byte-level alignment covers a majority of reference files.

This rules out a simple homogeneous array of same-sized records at the top level.

---

## 4. Entropy analysis

Average entropy: **2.657 bits** across the first-256 trailing bytes.

Interpretation:
- Pure ASCII text: ~4.5 bits (after letter distribution)
- Random noise: ~8.0 bits
- Packed small integers: ~1-3 bits
- Structured binary tables: ~2-4 bits

An entropy of 2.657 is consistent with a **packed structure of small
non-negative integers** -- for example, tile IDs, sprite indices, or grid
coordinates. It is NOT consistent with compressed data (which would show
entropy near 8.0) or plain text (which would show higher entropy with more
distinct byte values).

---

## 5. U16 string-index analysis

Average U16 string-index ratio: **0.245** (24.5% of first-64 U16 words
are in the range (0, field8)).

This is slightly below the threshold (0.3) used to trigger
HYPOTHESIS_TRAILER_STRING_TABLE_REFERENCES. The ratio is plausible (a genuine
reference structure might have about 25% of words pointing into the string
table), but it is not strong enough to conclude.

Possible interpretations:
1. The U16 words near the start of the trailer are a mix of header/count fields
   (small integers not referencing the string table) and actual indices.
2. The string-table references are deeper in the trailing section than the
   first-64 words.
3. The indices reference a different lookup table, not the ASCII entry list.

---

## 6. No length-prefixed strings or compression

Zero files had detectable length-prefixed U16 ASCII segments. Zero files had
zlib or gzip headers. The trailing section is neither a string catalog nor a
compressed block at the top level.

---

## 7. What is likely

The combination of:
- Non-uniform byte alignment (not mod2 or mod4 consistently)
- Entropy ~2.66 (structured small-integer data)
- Some U16 words in plausible string-index range
- No compressed or length-prefixed structure

...suggests the trailing section is a **packed record structure** whose internal
alignment is record-count-dependent, not a fixed-field structure. The record
format and per-record byte size are unknown and require a different investigation
approach.

---

## 8. What MAP-6X must do

`WRITER_NOT_DEFENSIBLE`. The trailing body model is not yet understood.

Recommended investigation for MAP-6X:

1. **Focus on a known-small cell and read adjacent cells**: For 43_43 (7018
   trailing bytes, 1137 entries), compute 7018/1137 ≈ 6.17 bytes per entry.
   If the structure is per-entry records, each entry's record might be ~6 bytes
   with some padding to a non-4-aligned total.

2. **Try per-entry record parsing at 4, 6, 8 bytes per record**:
   - 1137 × 4 = 4548 (too small for 7018)
   - 1137 × 6 = 6822 (close to 7018; 7018 - 6822 = 196 bytes overhead)
   - 1137 × 8 = 9096 (too large for 7018)
   - Hypothesis: ~6-byte per-entry records + fixed header/footer

3. **Inspect the first few bytes of the trailing section carefully**:
   What is at trailing offset 0? Is there a count field? A magic word? A
   version? The first 4-8 bytes of the trailing section may be a header.

4. **Compare trailing first-bytes across cells of the same map**:
   If cells 43_42, 43_43, 43_44 are all present, their trailing sections
   may share stable header bytes.

Only after MAP-6X produces a defensible per-entry record model should a
LOTH v3 writer be attempted.

---

## 9. Status labels

```text
BUILD42_LOTH_TRAILING_BYTE_PATTERNS_ANALYSED
  -- Byte-level analysis of 20 reference files completed.

LOTH_REQUIRES_TRAILING_BINARY_BODY
  -- Confirmed: all 20 files have trailing bytes.

HYPOTHESIS_TRAILER_UNKNOWN
  -- No clear alignment, no LP-strings, no compression.
  -- Structure likely per-entry records but format unknown.

WRITER_NOT_DEFENSIBLE
  -- No v3 writer should be attempted until the record format is understood.

WRITER_NOT_CHANGED
  -- No changes to the MAP-6S candidate writer.

LOAD_TEST_NOT_PERFORMED
  -- MAP-6W is research only.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding.
```

---

## 10. Non-claims

- No load test was performed as part of MAP-6W.
- No binary writer was changed.
- No PZ assets were copied or read by PZMapForge scripts.
- No media/maps writes occurred in this repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
