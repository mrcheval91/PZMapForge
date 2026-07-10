# MAP-38I: Positive Control Failure — Root Cause Is Upstream, Not This Writer

Date: 2026-07-10
Status: DECISIVE NEGATIVE RESULT (human-observed, coordinate-confirmed)
PZ Build tested: 42.19.0 (revision 1aa820d7bb66c4e55513cae04022bdacdac5b34e)

This document corrects the direction of the entire MAP-38 sub-lineage (MAP-38A
through MAP-38H) and should be read before any further work on
`Build42CandidateWriterCommand` or the `renderable_v1` profile's binary format.

## What was tested

`MyMapMod` — the community sample this repo has cited since MAP-38A as
"confirmed-working" — was tested **completely unmodified except for its own
spawn point**, which was redirected (only `spawnpoints.lua`/`objects.lua`,
not any terrain file) to land the player on one of its own four **real,
authored** cells (31_45), instead of its original spawn at cell 27_39 (which
is not one of MyMapMod's authored cells — the previous test at that spawn,
recorded as "flat grass, no trees, deco rocks," was uninterpretable: it never
proved MyMapMod's own terrain renders, since 27_39 isn't terrain MyMapMod
provides).

## Result

Operator confirmed the on-screen debug coordinate was **(9450, 13650)** —
exactly the intended point inside cell 31_45's world range. The terrain
observed was **dense pine forest, procedural fallback** — not flat,
authored, non-procedural ground.

This is the decisive signal. Build 42's procedural fallback generator
produces scattered trees/shrubs; genuinely-read authored ground (even boring
natural-blend tiles) does not get procedural vegetation scattered onto it.
Seeing forest at the exact coordinate of a real authored cell means **the
game did not read MyMapMod's own `31_45.lotheader` / `world_31_45.lotpack` /
`chunkdata_31_45.bin`.**

## What this changes

Every fix applied in this session (MAP-38D phantom-tile correction, MAP-38G
real spawn-point object, MAP-38H real mixed chunk encoding) was a genuine,
verified byte-level bug relative to real vanilla/community files. **None of
them could have mattered**, because the file MyMapMod ships — which this
repo has treated as ground truth for "what a working file looks like" since
MAP-38A — does not itself render on this installation, on this Build 42
version, via this loading path (`mods/<id>/common/media/maps/<mapId>/` +
`lots=Muldraugh, KY` + Solo "Choose Starting Location" selection).

This means the entire MAP-6A → MAP-38H lineage's foundational premise — that
dropping correctly-shaped lotpack/lotheader/chunkdata files into a mod's map
folder is *sufficient* to have Build 42 read and render them — has never
actually been confirmed on this install. The MAP-38A "confirmed" human
runtime test (2026-07-08, hand-crafted Python scaffolding, not committed to
this repo) is the only positive signal this entire lineage has ever
produced, and it cannot currently be reproduced or independently verified —
not with the phantom tile, not with a real tile, not with a real spawn
object, not with real chunk encoding, and not even with the unmodified
reference sample itself.

## Leading hypotheses for the actual gap (untested, in rough order of plausibility)

1. **PZ version drift.** This repo's binary-format research spans many
   sessions; MAP-38A's original success was 2026-07-08 on whatever build was
   installed then. The current install is Build 42.19.0. If the lot-loading
   contract changed between those points (or between whatever version the
   Alree/Unjammer unofficial tooling and MyMapMod's own files were built
   against, and 42.19.0), files that were structurally valid then may simply
   not be read now.
2. **Missing registration step.** Dropping files under `common/media/maps/`
   may be necessary but not sufficient — there may be a required entry in a
   global world/map registry (an `IsoMetaGrid`-adjacent index, a
   `worldmap.bin`-style file, or something the *official* Build 42 mapping
   tools (`docs/B42_MAPPING_DISCORD_TUTORIALS.md`, TUT-26) generate that the
   unofficial Alree/Unjammer pipeline and this repo's own writer both skip.
3. **Official vs. unofficial tooling divergence.** TUT-26 documents official
   B42 tools superseding the unofficial ones MyMapMod and this repo's
   research were built against (TUT-13). If the game's loader now expects
   the official tools' output shape, both this repo's writer AND MyMapMod
   would fail identically — which is exactly what was observed.
4. **Solo-mode-specific gap.** All positive-signal testing in this session
   was Solo/Host mode. It is untested whether a true dedicated server
   (`servertest.ini`-driven, matching the original MAP-9K/9L/9Q wiring more
   closely) behaves differently. Low priority: MAP-37H's own dedicated-server
   packet already existed and there is no recorded evidence it ever
   succeeded either.

## Recommended next step

**Do not make further changes to `Build42CandidateWriterCommand` or the
`renderable_v1` binary format.** Every further byte-level fix is guessing
in the dark until reading itself is confirmed. Before any more writer work:

- Check whether Build 42.19.0's own patch notes/changelog document a lot
  or chunk loading change (external research, not available from local
  files on this machine).
- Investigate the *official* B42 mapping tools (TUT-26) as a structurally
  different reference, rather than continuing to lean on the unofficial
  Alree/Unjammer-era `MyMapMod` sample now that it too has failed to render.
- Consider that this may need direct input from the PZ modding community
  (the Discord this repo already archives tutorials from) rather than
  further local reverse-engineering, since the positive control — the one
  thing this repo could point to as proof the format is even readable in
  principle — no longer holds on the currently-installed build.

## Claim boundary

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
RENDERABLE_MOUNT_SUCCESS=false
positive_control_confirmed=false
myMapMod_renders_on_this_install=false (contradicts MAP-38A's founding assumption)
root_cause_isolated_to_writer=false
root_cause_location=upstream_of_writer_unconfirmed_mechanism
CLAUDE_RAN_PZ=false (operator installed, launched, and observed; coordinate independently confirmed by operator)
