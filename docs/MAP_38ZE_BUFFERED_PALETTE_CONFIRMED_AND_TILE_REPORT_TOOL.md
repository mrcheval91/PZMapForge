# MAP-38ZE: Buffered Palette Confirmed Working + Tile Report Debug Tool Discovered

Date: 2026-07-11
Status: CONFIRMED (human-observed, crash fix verified; major tooling discovery)

## Crash fix confirmed

MAP-38ZD's buffered palette layout (2-chunk Type-A default gap between
every region) was tested by the operator: **no crash.** World loaded
cleanly, all four quadrants rendered correctly (carpet, foliage, herbs,
forest), operator confirmed "all is there" and "yeah its all good."

This confirms MAP-38ZC's root-cause hypothesis: the original crash
(`NullPointerException` in `Blending.changeGround`) was caused by
different explicit tile types sitting directly adjacent to each other.
Separating them with an untouched (Type-A default) buffer avoids it.
`--renderable-palette` is now considered safe to use with this layout.

A water feature was observed in one of the gap columns — confirmed via the
Tile Report tool (see below) to be real vanilla content
(`blends_natural_02_0`), not a bug. Gap zones correctly fall through to
genuine underlying Muldraugh terrain, exactly as designed.

## Major tooling discovery: PZ's Tile Report debug feature

Operator discovered (screenshot) that **right-clicking in-game opens a
context menu with "Tile Report"**, showing the exact tile name and precise
coordinates under the cursor (e.g. "Tile Report: blends_natural_02_0",
"Coordinates Report x: 8833, y: 6799, z: 0"), plus contextual actions
(Natural Water Source, Fishing, Sit on ground, Walk to).

**This is a debug-mode feature** and should be used for all future testing
in this repo instead of relying on visual descriptions. It gives exact,
unambiguous ground truth about what tile is actually rendering at any
position — this entire session's testing (MAP-38K onward) relied on
verbal/visual descriptions ("carpet tile", "long herbs", "dark foggy
forest") which, while informative, are open to interpretation. The Tile
Report tool removes that ambiguity entirely.

**Recommendation for any future PZMapForge runtime test**: enable/use
whatever setting exposes this (appears to already be available without
extra setup, possibly tied to Debug Mode or admin/moderator status) and
have the operator right-click and report the exact `Tile Report:` string
alongside the `Coordinates Report:` line, rather than a verbal description.
This would have resolved several ambiguous moments this session (e.g. the
original "electrical pylon" reports, and confirming exactly which tile was
under the character) far faster.

## Claim boundary

buffered_palette_crash_fix_confirmed=true (no crash, human-observed,
2026-07-11)
tile_report_debug_tool_available=true
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
CLAUDE_RAN_PZ=false
