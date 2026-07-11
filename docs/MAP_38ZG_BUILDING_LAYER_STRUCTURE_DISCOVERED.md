# MAP-38ZG: Real Building Layer Structure Discovered (via Third-Party Decoder Output)

Date: 2026-07-11
Status: RESEARCH FINDING (external reference, not yet confirmed against compiled bytes)

## What was found

No published format specification exists for how Build 42's compiled
`.lotpack`/`.lotheader` data encodes buildings and rooms — confirmed by
checking `github.com/Unjammer/PZ_Vanilla_map_b42`, a community project that
has fully decoded 8,724 real buildings from the vanilla map but explicitly
does not publish its decoding methodology ("preserving The Indie Stone's
proprietary map format details").

However, that project's **output** (the reconstructed, human-editable TBX
building files) is published, and studying that output reveals the real
authoring-side structure buildings are made of, even without the compiled
binary decoder itself.

Downloaded `Tutorial.zip` (234KB, a small non-Muldraugh sample map using
the same reconstruction format) from
`github.com/Unjammer/PZ_Vanilla_map_b42/releases/download/B42RC01/Tutorial.zip`
and inspected a real building TBX file
(`tbx_buildings/0_0/0_0_b0000_hall_102_118.tbx`) directly. Not committed to
this repo (third-party reverse-engineered output, kept local/scratch only,
consistent with this repo's "no copied PZ assets" rule — this is analysis
material, not a dependency).

## What the TBX format reveals

Each building has one or more Z-levels (`<floor>` elements), and each
Z-level has **ten independently-gridded named layers**:

```
Floor, FloorOverlay, FloorGrime, FloorGrime2, FloorFurniture,
Vegetation, Walls, WallTrim, Walls2, WallTrim2
```

Each layer is a CSV-style grid of indices into a `<user_tiles>` palette
(0 = empty, N = the Nth declared tile name). Critically:

- **Walls occupy sparse positions** (a thin outline matching the room
  perimeter), not a filled area — e.g. one wall row was
  `0,68,69,69,69,69,70,0,0` — a clear end-cap/middle/end-cap pattern, not
  one uniform tile repeated.
- **Vegetation is its own dedicated layer**, separate from Floor/Walls —
  this directly confirms why this repo's `Vegitation`-type `objects.lua`
  zone experiments (MAP-38O/P) had no effect: real vegetation placement in
  an authored building isn't a zone object at all, it's a layer, just like
  Floor.
- Wall tile indices vary by position (68, 69, 70 in the example) —
  confirming MAP-38ZA's tile-index-matters pattern extends to walls:
  different indices are structural segment types (end caps vs. middles),
  not decorative variants.

## Hypothesis this suggests for the compiled binary format

This repo's lotpack Type-B record format is
`[U32 field1][U32=0xFFFFFFFF][U32 tile_index]`, and every real file this
repo has ever inspected uses `field1=2`. **Hypothesis: field1 is a layer
selector**, and if the compiled format's layer numbering follows the same
order as the TBX layer list above (0=Floor, 1=FloorOverlay, 2=FloorGrime,
3=FloorGrime2, 4=FloorFurniture, 5=Vegetation, 6=Walls, 7=WallTrim,
8=Walls2, 9=WallTrim2), then `field1=2` (used throughout every confirmed
content-type test this session) corresponds to **FloorGrime**, not a
generic "ground" layer — which would still visually read as ground content
when filled uniformly with any tile, consistent with every result seen so
far (floor, foliage, grass overlay, forest, atmospheric forest all
rendered as ground-level content).

This would also explain MAP-38ZF's diagonal wall smear: a wall sprite
placed on the FloorGrime layer renders as a flat texture tile (like every
other successful test), not as a properly positioned/oriented wall object,
because it's on the wrong layer entirely.

## What was built to test this

`--renderable-layer-field <int>` CLI flag on `Build42CandidateWriterCommand`
— overrides the previously-hardcoded `field1=2` constant. Default behavior
unchanged (still 2), 24 existing tests pass unaffected.

Three candidates generated with `walls_exterior_house_01_4` at different
layer field guesses: `pzmapforge_map38h6` (6, "Walls" per the hypothesized
mapping), `pzmapforge_map38h0` (0, "Floor", a control), `pzmapforge_map38h7`
(7, "WallTrim").

**Caution**: field1 values other than 2 have never been observed in any
real file this repo has inspected. This is genuinely untested territory —
unlike every prior tile-name experiment (which only ever changed a value
already proven safe to vary), this changes a field whose safe range is
unknown. There is some chance of a crash or unexpected behavior; same
recovery procedure as MAP-38ZC applies if so (force-close via Task Manager,
report the debug log).

## Claim boundary

layer_field_hypothesis_tested=false (candidates generated, not yet
human-tested)
building_layer_structure_source=third_party_decoder_output_not_official
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
