# MAP-38O: Vegitation Zone — No Observed Effect

Date: 2026-07-10
Status: NEGATIVE RESULT (human-observed)

## What was tested

MAP-38N added a real `Vegitation` zone object to `renderable_v1`'s
`objects.lua`, covering the same tile range (cellX*256+64 .. +191) as the
lotpack's central marker-tile block, based on real vanilla Muldraugh's own
map-wide `objects.lua` using `Vegitation` zone rectangles.

Candidate `pzmapforge_map38n` (cell 34_26) installed and tested at the same
confirmed coordinate (8832, 6784).

## Result

Operator observed the same "endless repeating carpet tile" pattern as
MAP-38K/MAP-38L's `pzmapforge_map38j` — no visible vegetation, no
difference from the zone-less candidate.

## Interpretation

The `Vegitation` zone object had no observed effect. Leading hypotheses,
untested:

1. **Zones seed density where the game generates vegetation, not force
   placement over explicitly-authored ground.** Every tile in the marked
   area is already an explicit Type-B lotpack record (the marker tile) —
   there may be no "unauthored" slot left for a vegetation zone to have
   anything to seed.
2. **Zone format incomplete.** The single real example this was based on
   (`type = "Vegitation", x, y, z, width, height`) may need additional
   fields (density, seed, biome reference) not present in the one instance
   inspected.
3. **Per-cell objects.lua may not carry the same authority as Muldraugh's
   own map-wide objects.lua for zone-type objects**, even though the
   `SpawnPoint` object type is confirmed working from the same per-cell
   file (MAP-38G/MAP-38K). Different object types may be processed by
   different subsystems with different scope rules.

## Follow-up (MAP-38P): second zone over untouched ground — also no effect

Added a second `Vegitation` zone over tiles 0..63 (Type-A default-shorthand,
outside the marker block) to distinguish hypothesis 1 from 2/3. Operator
confirmed arrival at that location (visible at the edge of the marker-tile
block, coordinate ~8730,6680) and observed **plain empty grass — no
vegetation, no shrub scatter, same as everywhere else.**

**This rules out hypothesis 1.** Zones do not seed vegetation over
otherwise-unauthored ground either. Both placements (over explicit marker
tiles, and over Type-A default ground) show zero effect. Leading
hypotheses now: the zone object format is incomplete (missing a required
field like density/seed), or per-cell `objects.lua` zones of type
`Vegitation` simply don't carry the same authority as Muldraugh's own
map-wide `objects.lua` — unlike `SpawnPoint`, which is confirmed working
from the same per-cell file (MAP-38G/MAP-38K).

**Conclusion: `Vegitation` zone objects placed via this repo's per-cell
`objects.lua` have no observed effect, tested twice, over two different
ground types.** Not pursued further this session — diminishing returns
without a real reference implementation showing a working per-cell zone
(vanilla's own zones are all in Muldraugh's single map-wide file, which is
not the same code path as a mod's per-cell overlay).

## Claim boundary

vegitation_zone_effect_confirmed=false (tested twice, both negative)
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
