# MAP-6X: LOTH Per-Entry Record Model Research

```text
Schema:           pzmapforge.map6x-loth-per-entry-research.v0.1
Claim boundary:   writer_research_only_not_implemented_not_load_tested
BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED
LOTH_REQUIRES_TRAILING_BINARY_BODY
LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS
HYPOTHESIS_FIXED_HEADER_PLUS_RECORDS
WRITER_NOT_DEFENSIBLE
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. MAP-6W basis

MAP-6W showed avg entropy 2.657 and no clear byte alignment (mod2=10/20,
mod4=3/20). The per-entry record model was the recommended next investigation.

---

## 2. MAP-6X smoke: critical finding (40 Dru_map files sorted smallest-first)

**All 40 smallest files have EXACTLY 1048 trailing bytes.**

| Finding | Value |
|---|---|
| Trailing bytes count for all 40 sampled | 1048 (constant) |
| 1048 % 4 (U32 alignment) | 0 (U32-aligned) |
| 1048 / 4 = U32 word count | 262 |
| Stable prefix byte positions | 32 / 32 |
| Most plausible record sizes | 10, 9, 16 (but all with high overhead) |

---

## 3. The per-entry model is REJECTED for simple cells

For the simplest cells (n=2 or n=5 ASCII entries, trail=1048 bytes):
- bytes_per_entry = 1048/2 = 524 or 1048/5 = 209.6
- This is far too large for a per-entry record structure.
- No candidate record size (4-16 bytes) explains 1048 bytes with low overhead
  for n=2-5 entries.

**Conclusion**: The 1048-byte trailing body is NOT a per-entry variable structure
for simple grass/field cells. It is a **fixed-size block** present regardless of
the number of tile entries.

---

## 4. Fixed-size trailing body model

For simple cells, the trailing body appears to be:
- Fixed at **1048 bytes**
- **U32-aligned** (1048 / 4 = 262 exactly)
- **Stable first 32 bytes** across all 8 focus cells (all bytes identical)

This is consistent with a fixed binary table or a constant-size record structure
that stores grid or tile data independent of the LOTH entry count.

Possible models for the 1048-byte block:
1. **Fixed cell grid table**: 1048 = some cell-specific fixed structure
2. **Uniform record table**: 1048 / N for various N
   - 1048 / 8 = 131 exactly (8-byte records)
   - 1048 / 4 = 262 (U32 entries)
   - 1048 / 16 = 65.5 (not integer, close to 16-byte records but not exact)
3. **Header + variable + footer**: but 1048 is constant across cells with different
   entry counts (2 to 5), so the structure must not depend on entry count.

---

## 5. MAP-6U discrepancy: why Dru_map city cells are not 1048 bytes

MAP-6U showed Dru_map city cells with trailing bytes ranging 7018-33558. These
are different files from the simple cells sampled in MAP-6X. The Dru_map
reference used in MAP-6U (sorted by file size descending) captured complex urban
cells; MAP-6X sorted ascending and found simpler outlying cells.

This suggests:
- Simple/grass cells: **fixed 1048-byte trailing body** (U32-aligned, stable prefix)
- Complex/urban cells: **variable trailing body** (7018-33558 bytes, not U32-aligned)

For the PZMapForge candidate (a grass-only empty cell), the 1048-byte fixed
model is the relevant target.

---

## 6. First-64-byte prefix analysis of the 1048-byte block

Across 8 focus files (different cells), the first 32 bytes are ALL STABLE
(identical). This means the 1048-byte block begins with a constant header
section. The stable prefix must be inspected in the next slice to determine:
- Is the entire 1048 bytes constant (same in every simple cell)?
- Or does it change after the first 32 bytes?

---

## 7. Writer readiness

`WRITER_NOT_DEFENSIBLE` by the per-entry model metric. However, the fixed-size
discovery substantially changes the picture:

If a smoke test confirms that the 1048-byte block is ENTIRELY zero (or entirely
constant) for simple grass cells, then `WRITER_MAYBE_DEFENSIBLE_AFTER_MODEL_CONFIRMATION`
becomes achievable.

**MAP-6Y must answer**:
1. Is the entire 1048-byte block constant across all simple cells?
2. Or does it contain per-cell variable data after the stable prefix?
3. If constant: does writing 1048 zero bytes (or the stable pattern) pass IsoLot.readInt?

---

## 8. Status labels

```text
BUILD42_LOTH_PER_ENTRY_RECORD_MODEL_ANALYSED
  -- Per-entry model tested on 40 smallest Dru_map cells.

LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS
  -- All 40 smallest cells: exactly 1048 trailing bytes.

HYPOTHESIS_FIXED_HEADER_PLUS_RECORDS
  -- 32/32 stable first-bytes across focus cells; fixed structure confirmed.

WRITER_NOT_DEFENSIBLE
  -- Per-entry model doesn't explain simple cells; fixed-block content unknown.
  -- Must confirm whether 1048-byte block is constant before writing.

WRITER_NOT_CHANGED
  -- No changes to the MAP-6S candidate writer.

LOAD_TEST_NOT_PERFORMED
  -- MAP-6X is research only.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding.
```

---

## 9. Recommended next step: MAP-6Y

MAP-6Y: Inspect the content of the 1048-byte trailing block for simple cells.

Goals:
1. Read the full 1048-byte block from 5+ simple cells.
2. Check if all bytes are identical across cells (fully constant block).
3. If fully constant: the block is a fixed header blob and can be embedded.
4. If partially variable: identify which byte positions change per cell.
5. If mostly zero with a small header: attempt a LOTH v3 writer with
   the known-stable prefix + zero-padded remainder.

Only if MAP-6Y shows a fully constant or safely-zero-padded structure should
a LOTH v3 writer be attempted.

---

## 10. Non-claims

- No load test was performed as part of MAP-6X.
- No binary writer was changed.
- No PZ assets were copied or read by PZMapForge scripts.
- No media/maps writes occurred in this repo.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
