# MAP-38ZH: field1 Confirmed as a Real 5-Slot Array Index (Java Stack Trace Evidence)

Date: 2026-07-11
Status: CONFIRMED (engine stack trace evidence, human runtime test)

## What was tested

Three `--renderable-layer-field` candidates from MAP-38ZG's hypothesis
(field1 = a layer selector, TBX order 0=Floor..9=WallTrim2), each using the
same safe single-marker-tile mechanism (`walls_exterior_house_01_4`) at cell
34_26:

- `pzmapforge_map38h6` (field1=6, hypothesized "Walls")
- `pzmapforge_map38h7` (field1=7, hypothesized "WallTrim")
- `pzmapforge_map38h0` (field1=0, hypothesized "Floor", control)

## Result

**h6 and h7 both threw repeated exceptions on load** (game did not crash
outright, but the affected chunks failed to load). **h0 loaded cleanly**
but the marker tile was not visually distinguishable from plain grass.

Confirmed directly from `%USERPROFILE%\Zomboid\Logs\2026-07-11_21-48_DebugLog.txt`:

```
ERROR: General f:0> IsoCell.PlaceLot> Exception thrown
java.lang.IndexOutOfBoundsException: Index 6 out of bounds for length 5 at Preconditions.outOfBounds(null:-1).
Stack trace:
    java.base/jdk.internal.util.Preconditions.outOfBounds(Unknown Source)
    java.base/jdk.internal.util.Preconditions.outOfBoundsCheckIndex(Unknown Source)
    java.base/jdk.internal.util.Preconditions.checkIndex(Unknown Source)
    java.base/java.util.Objects.checkIndex(Unknown Source)
    java.base/java.util.ArrayList.get(Unknown Source)
    zombie.iso.IsoCell.PlaceLot(IsoCell.java:2946)
    zombie.iso.CellLoader.LoadCellBinaryChunk(CellLoader.java:348)
    zombie.iso.IsoChunk.LoadBrandNew(IsoChunk.java:2261)
    zombie.iso.IsoChunk.LoadOrCreate(IsoChunk.java:2330)
    zombie.iso.WorldStreamer.DoChunkAlways(WorldStreamer.java:535)
    zombie.iso.WorldStreamer.DoChunk(WorldStreamer.java:519)
    zombie.iso.WorldStreamer.threadLoop(WorldStreamer.java:431)
```

Tally of the exact exception text across the whole log:

```
256 occurrences: "Index 6 out of bounds for length 5"
256 occurrences: "Index 7 out of bounds for length 5"
```

512 total — matches the human-reported "h6 256 errors, h7 = 512 errors"
(cumulative) exactly. Each failed chunk logs "Failed to load chunk, blocking
out area." and continues; the game does not crash, it just blanks the
affected area.

## Interpretation

This is the strongest evidence this repo has ever obtained about the
compiled binary format, because it comes from the game engine's own
decompiled call stack, not from inference over output bytes:

- `IsoCell.PlaceLot` (`IsoCell.java:2946`) calls `ArrayList.get(field1)` on
  some list that is populated with **exactly 5 elements** for this cell.
  `field1` (this repo's Type-B record's first U32, previously hardcoded to
  `2`) is used **directly as that list index**.
- Valid range is provably **0-4**, not the 0-9 range MAP-38ZG's TBX-layer-order
  hypothesis assumed. That hypothesis is now falsified in its literal form —
  there is no 10-slot layer array backing this field.
- `field1=2` (used in every confirmed content-type test this session: floor,
  foliage, grass overlay, forest, atmospheric forest, wall-texture-smear) is
  a valid index into this 5-slot array. `field1=0` also loads cleanly but
  produced no visible marker — a different, not-yet-explained behavior from
  index 2, at a valid index.
- **This does not mean indices 0-4 are "material layers" (Floor/Walls/etc.)
  in the TBX sense.** The MAP-38ZF wall-texture-as-flat-smear result was
  already obtained at index 2 (a known-valid index) and still rendered as
  flat ground content, not a structural wall object — so whatever this
  5-slot array represents, it is not simply "which visual layer category
  this tile belongs to." A more likely reading, given the call path goes
  through `IsoCell.PlaceLot` → `CellLoader.LoadCellBinaryChunk`, is that the
  list is something like a per-lot Z-level/floor list, capped at 5 entries,
  and index 2 happens to be a safe, renderable slot for ground-level content
  — not a proven claim, just the shape the evidence supports.

## What this means for building/room placement

The field1=layer-selector idea, as a way to reach `Walls`/`WallTrim` output,
is **dead in its original form** — there is no slot 6 or 7 to write to. Real
wall/room placement, if pursued via this record type at all, needs slots
0-4 mapped empirically (only 0 and 2 tested so far; 1, 3, 4 remain unknown),
and there is no a priori reason to expect any of those 5 slots to correspond
to WorldEd's Walls/WallTrim/Walls2/WallTrim2 concept — that TBX layer list
describes the *authoring* format, not this compiled 5-slot array.

## Claim boundary

```
field1_valid_range_confirmed=[0,4]
field1_indexoutofbounds_confirmed_via_stack_trace=true
field1_layer_selector_hypothesis_falsified=true (10-slot TBX-order form)
field1_5_slot_array_meaning_understood=false (only indices 0 and 2 empirically observed; 0 differs visually from 2)
coherent_room_structure_confirmed=false
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
```
