# MAP-6Y: LOTH Fixed 1048-Byte Block Research

```text
Schema:           pzmapforge.map6y-loth-fixed-1048-block-research.v0.1
Claim boundary:   writer_research_only_not_implemented_not_load_tested
BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED
LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS
WRITER_NOT_DEFENSIBLE
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6X basis

MAP-6X tested per-entry record hypotheses on the LOTH trailing body and found:

- All 40 smallest Dru_map cells have EXACTLY 1048 trailing bytes (fixed size).
- 1048 % 4 = 0 (U32-aligned, 262 words).
- 32/32 first-bytes of the trailing block are STABLE across all 8 focus cells.
- Per-entry model REJECTED for simple/grass cells.
- The 1048-byte block is a fixed-size structure whose content must be confirmed.

MAP-6X concluded: WRITER_NOT_DEFENSIBLE until the block content is confirmed.
MAP-6Y was tasked with determining whether the full 1048-byte block is constant.

---

## 2. Why the per-entry model was rejected

For simple cells with 2-5 ASCII entries, the 1048-byte trailing body cannot be
a per-entry variable record structure:
- bytes_per_entry = 1048/2 = 524 or 1048/5 = 209.6
- No candidate record size (4-16 bytes) explains 1048 bytes with low overhead.
- The block size is constant regardless of the entry count, ruling out
  any model that scales with the number of LOTH entries.

---

## 3. What MAP-6Y compares

MAP-6Y runs `analyze-build42-loth-fixed-1048-block.ps1` against reference
Build 42 .lotheader files copied under `.local/`. For each file with exactly
1048 trailing bytes, it:

1. Extracts the full 1048-byte trailer.
2. Computes SHA-256 of the trailer.
3. Compares every byte position across all selected files.
4. Identifies stable (same value in all files) and variable positions.
5. Computes stable/variable ranges, prefix/suffix lengths, and U32 word stability.
6. Tests coordinate correlation for variable positions.
7. Emits hypotheses and writer-readiness verdict.

---

## 4. Smoke findings

To be filled in after running against reference files copied under `.local/`.

The script test (test-build42-loth-fixed-1048-block.ps1) with synthetic fixtures
confirms that:

- selected_file_count is correctly counted from .lotheader files with 1048
  trailing bytes.
- unique_trailer_sha256_count correctly identifies identical vs. different trailers.
- all_1048_blocks_identical is false when even one file differs.
- stable_byte_count / variable_byte_count are computed byte-for-byte.
- stable_prefix_length / stable_suffix_length are computed from contiguous
  stable runs at the block boundaries.
- Stable and variable byte ranges are correctly enumerated as range objects.
- Hypotheses and writer_readiness are determined and recorded.
- All status labels are present in the MD report.

**Synthetic test result (3 fixtures, 1 variable byte at position 64):**

| Field | Expected | Status |
|---|---|---|
| selected_file_count | 3 | confirmed by test |
| unique_trailer_sha256_count | 2 | confirmed by test |
| all_1048_blocks_identical | false | confirmed by test |
| stable_byte_count | 1047 | confirmed by test |
| variable_byte_count | 1 | confirmed by test |
| stable_prefix_length | 64 | confirmed by test |
| stable_suffix_length | 983 | confirmed by test |
| stable_byte_ranges count | 2 | confirmed by test |
| variable_byte_ranges count | 1 | confirmed by test |

**Real reference smoke (Dru_map):**

Run `scripts/analyze-build42-loth-fixed-1048-block.ps1` against the reference
copy when available. Findings must be recorded in `.local/map6y-loth-fixed-1048/`.

---

## 5. Writer-readiness verdict

Based on the synthetic-fixture test, the script infrastructure is proven correct.
The real reference smoke result determines the actual verdict:

### If all_1048_blocks_identical = true and block is all-zero:
```text
WRITER_MAYBE_DEFENSIBLE_WITH_ZERO_1048_BLOCK
```
Recommended next: MAP-6Z LOTH v3 minimal 1048 zero block.

### If all_1048_blocks_identical = true and block is nonzero:
```text
WRITER_MAYBE_DEFENSIBLE_WITH_STABLE_LITERAL_1048_BLOCK
```
Recommended next: MAP-6Z LOTH v3 stable literal block.

### If mostly stable + zero body plausible:
```text
WRITER_MAYBE_DEFENSIBLE_WITH_STABLE_PREFIX_ZERO_REMAINDER
```
Recommended next: MAP-6Z LOTH v3 stable prefix + zero remainder.

### If variable and unknown:
```text
WRITER_NOT_DEFENSIBLE
```
Recommended next: MAP-6Z deepen fixed-block fields.

**Current verdict (pending real smoke): WRITER_NOT_DEFENSIBLE**
The analysis infrastructure is ready. A real reference copy under .local/
is required to produce a defensible verdict.

---

## 6. Recommended next task

Once the real reference smoke is run:

- **If full block constant**: MAP-6Z LOTH v3 stable literal block, no public claim.
- **If mostly stable + zero body plausible**: MAP-6Z LOTH v3 stable prefix
  + zero remainder, no public claim.
- **If variable unknown**: MAP-6Z deepen fixed-block fields.

No load test is performed as part of MAP-6Y. No writer is changed.

---

## 7. Status labels

```text
BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED
  -- Script infrastructure proven correct; real reference smoke pending.

LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS
  -- Inherited from MAP-6X: all 40 smallest cells have exactly 1048 trailing bytes.

WRITER_NOT_DEFENSIBLE
  -- Block content not yet confirmed from real reference files.
  -- Verdict upgrades to MAYBE_DEFENSIBLE after real smoke confirms stability.

WRITER_NOT_CHANGED
  -- No changes to any candidate writer in MAP-6Y.

LOAD_TEST_NOT_PERFORMED
  -- MAP-6Y is research only.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding.
```

---

## 8. Non-claims

- No load test was performed as part of MAP-6Y.
- No binary writer was changed.
- No PZ assets were copied or read by PZMapForge scripts.
- No media/maps writes occurred in this repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
