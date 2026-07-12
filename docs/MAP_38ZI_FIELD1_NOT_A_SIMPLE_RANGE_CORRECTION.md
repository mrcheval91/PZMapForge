# MAP-38ZI: field1 Is Not a Simple 0-4 Range — MAP-38ZH's Range Claim Corrected

Date: 2026-07-12
Status: CONFIRMED (engine stack trace evidence, human runtime test) — corrects MAP-38ZH

## What was tested

The three remaining untested field1 values from MAP-38ZG/ZH's diagnostic set
— 1, 3, 4 — generated as `pzmapforge_map38h1`/`h3`/`h4`, same mechanism
(`--renderable-layer-field <n> --renderable-marker-tile
walls_exterior_house_01_4`), cell 34_26.

## Result

Bounded each candidate's load window in
`%USERPROFILE%\Zomboid\Logs\2026-07-12_15-33_DebugLog.txt` by its "loading
pzmapforge_map38hN" marker line and counted "Failed to load chunk" events
inside that window:

| field1 | Load window | Chunk failures | Exception |
|---|---|---|---|
| 0 (re-check) | 15:33:50-15:34:48 | 0 | none |
| 1 | 15:34:48-15:41:27 (loaded twice, both clean) | 0 | none |
| 3 | 15:41:27-15:47:05 (loaded twice, both clean) | 0 | none |
| 4 | 15:47:05-end | 256 | `IndexOutOfBoundsException: Index -1 out of bounds for length 5` |

**field1=4 crashes.** This directly falsifies MAP-38ZH's claim that the
valid range is 0-4 — 4 is inside that range and still throws. Worse: the
exception's reported index is **-1**, not 4. MAP-38ZH's working assumption
(the exception's reported "Index N" equals field1 literally, since that
held for field1=6→"Index 6" and field1=7→"Index 7") does **not** hold here.
There is no constant-offset transform that reconciles all observed pairs
either: 6→6 (offset 0), 7→7 (offset 0), 4→-1 (offset -5). Three different
field1 values, three different relationships between input and reported
index. Whatever `IsoCell.PlaceLot` is actually doing with field1, it is not
a direct array-index lookup with a fixed transform — something
context/state-dependent (chunk position, prior records processed, an
internal running counter) is involved, and it is not recoverable from
black-box crash testing alone.

Visual result initially reported for the h4 candidate ("vegetation at
intervals") is **not attributable to field1=4's content** — the crash log
confirms the marker chunk block failed to load and was blanked
("Failed to load chunk, blocking out area"); the observed vegetation is
ordinary real Muldraugh terrain adjacent to the blocked-out region, the
same pattern seen in every prior `FALLBACK_INDISTINGUISHABLE`-adjacent
observation this session. Retract that interpretation.

field1=3's "walls on/off" (zigzag wall segments with grass gaps, black
occluded regions) visually matches MAP-38ZF's original field1=2 result
description closely enough that it is likely the same rendering mechanism,
not a new behavior — not confirmed identical, just not distinguishable
from the existing description.

field1=1 loaded cleanly twice but no visual result was reported for it
specifically (the "empty grass" report was attributed to a re-check of
field1=0, matching its already-known blank behavior).

## Confirmed-safe / confirmed-crash tally after this round

```
Safe (loads, no exception):  0, 1, 2, 3
Crashes:                     4 (Index -1/len 5), 6 (Index 6/len 5), 7 (Index 7/len 5)
```

Three crash-causing values, three different exception shapes. No pattern
fits all three.

## Recommendation

Stop black-box-guessing individual field1 values. Three crashes with three
unrelated index/length relationships is strong evidence this is not a
simple bounds-checked selector reachable by trial and error — it is state
computed elsewhere in `IsoCell.PlaceLot` (or a caller) that black-box
testing cannot resolve further. Continuing to test field1=5 or re-testing
already-crashed values will not produce a formula; it will only produce
more unexplained data points. If this line of investigation is to continue
productively, it needs actual decompilation of `IsoCell.PlaceLot`
(`IsoCell.java:2946` per the obfuscated/decompiled stack trace) and its
callers, not further runtime probing.

## Claim boundary

```
field1_valid_range_0_to_4_CORRECTED=false (superseded MAP-38ZH claim)
field1_confirmed_safe_values=[0,1,2,3]
field1_confirmed_crash_values=[4,6,7]
field1_index_transform_formula_known=false
field1_5_slot_array_meaning_understood=false
coherent_room_structure_confirmed=false
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
```
