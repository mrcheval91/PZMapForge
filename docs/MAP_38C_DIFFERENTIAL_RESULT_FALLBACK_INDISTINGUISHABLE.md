# MAP-38C: MAP-38B Differential Result — FALLBACK_INDISTINGUISHABLE

Date: 2026-07-09
Status: RESULT RECORD (human-observed, both runs)

This is the durable record of the MAP-38B differential control test's actual
outcome. It supersedes nothing in MAP-38A's format specification, but it
directly contradicts MAP-38A's claim that `renderable_v1` produces
observably distinct rendering versus Build 42's procedural fallback.

## What was run

Operator generated a `renderable_v1` candidate for `pzmapforge_map38b` at
cell 35_27 via `scripts/prepare-build42-map38b-differential-control-test-packet.ps1`,
installed it as a Solo game mod (`%USERPROFILE%\Zomboid\mods\pzmapforge_map38b\`),
and selected it as the starting location from the in-game "Choose Starting
Location" screen (confirming `MAP_FOLDER_REGISTRATION` succeeded — the mod
appeared as a selectable option, not just the four vanilla towns).

## Run A — files-present

- Spawn observed at approximately world (10660, 8259) — inside cell 35_27's
  world range (10500-10799, 8100-8399).
- Terrain: small dirt road to the south, forest around spawn.
- Vegetation pattern: **natural/organic**, not grid-aligned or repeating.
  Operator explicitly confirmed "natural" when asked to distinguish
  uniform/tiled-looking placement from scattered/organic placement.
- No chunkdata/lotheader/lotpack parse errors in
  `Logs/2026-07-09_21-10_DebugLog.txt`; `IsoMetaGrid.Create` completed
  normally; mod load line present (`loading pzmapforge_map38b.`).

## Run B — files-removed

- Live mod folder swapped to the packet's staged
  `run-b-files-removed\pzmapforge_map38b_build42_candidate\` (no
  `35_27.lotheader` / `world_35_27.lotpack` / `chunkdata_35_27.bin` present;
  `map.info`/`objects.lua`/`spawnpoints.lua`/`thumb.png` unchanged).
- Fresh Solo game, same starting-location selection.
- Operator's report: same result as Run A ("same fukin thing") — same
  general terrain character, no distinguishing feature.

## Outcome

**FALLBACK_INDISTINGUISHABLE.** Per `MAP_38B_SUCCESS_FAILURE_CRITERIA.md`
Outcome 8: Run A and Run B render similarly, and neither shows the specific
grid-aligned `unofficial_fork_map_0` marker-tile pattern that MAP-38A's
original session recorded ("square patches of aligned bushes"). The visible
terrain in Run A cannot be attributed to the renderable_v1-generated binary
files.

`RENDERABLE_MOUNT_SUCCESS` is NOT recorded. `PLAYABLE_EXPORT_CLAIM_ALLOWED`
remains false.

## This does not reproduce MAP-38A's finding — leading hypothesis

MAP-38A's original human runtime test used **hand-crafted Python scaffolding**,
not this repo's C# writer (the C# `renderable_v1` profile was built
*afterward*, as a port of the confirmed format). Re-reading MAP-38A's own
recorded gaps (`docs/MAP_38A_RENDERABLE_V1_FORMAT_DISCOVERY.md`, "What is NOT
done"):

> Chunkdata semantics are still unknown. The profile emits the old all-zero
> body; a real, non-zero example (borrowed byte-for-byte from MyMapMod's
> `chunkdata_32_46.bin` during live testing, **not committed to this repo**)
> was present in the one confirmed-working in-game test, but all-zero
> chunkdata was also tested against a correct lotpack/lotheader without
> visibly breaking anything further — so its actual necessity and meaning
> remain open questions.

The C# `Build42CandidateWriterCommand`'s `renderable_v1` profile still emits
the old **all-zero** `chunkdata_35_27.bin` (MAP-37B shape: 1026 bytes total,
`00 01` header + 128 x 8-byte zero records — confirmed by file size in this
session's generated packet; content not independently re-diffed byte-for-byte
against a prior run in this session). This is the same all-zero shape
`docs/MAP_38A_RENDERABLE_V1_FORMAT_DISCOVERY.md` itself says the profile
still emits, unchanged from MAP-37B/C/D/E. The ORIGINAL confirmed-working
test used a
**real, non-zero chunkdata body**. MAP-38A's own record already flagged this
as an untested assumption ("all-zero chunkdata... tested... without visibly
breaking anything further" — itself based on the same single human's
say-so, not independently verified).

**Leading hypothesis: chunkdata's non-zero body is not cosmetic — it may be
required for the game to actually load/render the authored lotpack/lotheader
content instead of silently falling back to procedural generation.** This
would explain why:
- The map folder registers correctly (folder structure / map.info fixes
  from MAP-38A are confirmed still valid — the mod appears as a selectable
  location).
- No parse errors are logged (an all-zero chunkdata may be a structurally
  "valid" but semantically empty/no-op payload the game silently accepts).
- The rendered terrain is indistinguishable from fallback (if chunkdata
  gates whether the lotpack's tile data is actually consulted at all).

## Recommended next step

Before any further work on chunkdata semantics decode or object placement
(MAP-38A's "fork 1"), or before re-attempting MAP-38A's own recommended
"second confirmation test" as a pure repeat: **test whether a real, non-zero
chunkdata body (e.g. borrowed byte-for-byte from a real vanilla or
community-sample chunkdata file, the same approach MAP-38A's original human
test used) changes this outcome.** If a non-zero chunkdata body reproduces
the grid-aligned pattern where an all-zero body does not, that is strong,
falsifiable evidence chunkdata is load-gating, not decorative — and turns
"chunkdata semantics are unknown" from a documentation gap into a concrete,
testable next slice.

## Claim boundary

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
RENDERABLE_MOUNT_SUCCESS=false
FALLBACK_INDISTINGUISHABLE=true
renderable_v1_finding_reproduced=false
CLAUDE_RAN_PZ=false (operator installed, launched, and observed both runs)
