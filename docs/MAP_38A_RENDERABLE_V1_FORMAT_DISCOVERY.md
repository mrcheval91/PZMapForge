# MAP-38A: renderable_v1 Format Discovery — Session Report

Date: 2026-07-08 → 2026-07-09
Status: PROVISIONAL (one unrepeatable human runtime test, not an automated proof)

This document is the durable record of a live human-in-the-loop debugging session that
found and fixed the reason no PZMapForge Build 42 candidate had ever been observed
rendering in-game. It supersedes nothing; it complements `docs/MAP_37E_DIFFERENTIAL_CONTROL_TEST.md`.

---

## Starting point

MAP-37E's own differential control test packet (Run A: files present, Run B: files
removed) came back `FALLBACK_INDISTINGUISHABLE` — both runs showed generic PZ wilderness at
spawn, indistinguishable from each other. Every prior candidate profile (`empty_grass_v0`
through `v5`) had shipped with `PLAYABLE_EXPORT_CLAIM_ALLOWED=false` and
`LOAD_TESTED=false` for a real reason: nobody had ever confirmed one rendered.

## What was found (in order of discovery)

1. **Wrong folder structure.** Every prior profile wrote cell data
   (`.lotheader`/`.lotpack`/`chunkdata`/`map.info`/etc.) under
   `<mod>/<version>/media/maps/<mapId>/`. Real Build 42 map folders never do this — cell
   data belongs under `<mod>/common/media/maps/<mapId>/`, with the version folder (`42/`)
   holding only `mod.info` + `poster.png`. Confirmed against:
   - `pzwiki.net/wiki/Mod_structure` (via `r.jina.ai` proxy, since direct fetch is
     Cloudflare-blocked): "At least one versioning or a common folder is needed."
   - Real vanilla sub-town map folders (`Rosewood, KY`, `West Point, KY`, `Riverside, KY`
     inside the actual Steam install) — these have no `.lotheader` files of their own at
     all; they're pure spawn-select UI entries pointing back into `Muldraugh, KY`'s own
     folder, which is where all real terrain data physically lives.
   - A confirmed-working community sample mod, `github.com/pzmapping/Sample-Mod-and-Project-`
     ("MyMapMod") — cloned and inspected directly; its real, working folder tree uses
     exactly this `common/` + lean version-folder layout.

2. **Wrong `map.info` `lots=` value.** Every prior profile used a self-referential
   `lots=<mapId>`. Real cells that sit inside the vanilla coordinate space (as all of ours
   do) need `lots=Muldraugh, KY` — confirmed against the same real vanilla sub-town
   `map.info` files (all three read `lots=Muldraugh, KY`) and MyMapMod's own `map.info`.

3. **Cell-choice mistake.** An early fix attempt deliberately targeted a verified-empty
   gap cell (12,12 — confirmed absent from all 4065 populated cells in the real vanilla
   `Muldraugh, KY` folder) to avoid vanilla content shadowing test output. This was
   backwards: `lots=Muldraugh, KY` mods can only **overlay/replace cells the base map
   already has**, not conjure terrain at genuinely empty coordinates. Confirmed two ways:
   MyMapMod's own 4 authored cells (31_45, 31_46, 32_45, 32_46) are all real, populated
   vanilla cells, not gaps; and every format-correct candidate targeting the empty gap
   cell never rendered distinctly across multiple iterations. **Always target a populated
   cell.**

4. **Wrong lotpack payload shape.** Every prior profile wrote 1024 chunks of flat,
   all-zero, fixed 1024-byte payloads. Real lotpack chunks are **not** flat — each chunk
   is an 8×8 tile-slot grid (64 slots), built from a sequence of two record types that
   always sum to exactly 64 slots:
   - `[U32=0xFFFFFFFF][U32=run_length]` (8 bytes) — `run_length` consecutive default/
     unauthored slots. A whole-chunk `[0xFFFFFFFF][64]` (8 bytes total) is the single most
     common real chunk shape (904/1024 chunks in the working sample; 957/1024 chunks in
     real vanilla Muldraugh use the equivalent all-explicit 768-byte shape as their
     baseline).
   - `[U32=2][U32=0xFFFFFFFF][U32=tile_index]` (12 bytes) — one explicit tile, indexing
     into the `.lotheader`'s name table.
   This was reverse-engineered by writing a Python offset-table/chunk-size-distribution
   parser against both real vanilla `world_35_27.lotpack` and MyMapMod's real
   `world_31_45.lotpack`, then hex-dumping representative chunks of each observed size
   (8, 224, 392, 448, 768 bytes) and decoding the word-level grammar by hand.

5. **The content-choice trap (cost the most wasted test cycles).** Several intermediate
   iterations used real vanilla `blends_natural_01/02_*` ground-blend tile names — format-
   correct, and likely rendering, but **visually indistinguishable from PZ's own procedural
   wilderness fallback**, because both are "natural terrain" textures. Every one of these
   tests looked like "still fallback forest" even after every structural bug above was
   fixed, which repeatedly (and reasonably) read as "the format must still be wrong."
   The actual breakthrough came from finding a 5th tile name in MyMapMod's own real
   `lotheader` — `unofficial_fork_map_0`, a non-vanilla placeholder/test tile bundled with
   the Alree/Unjammer unofficial B42 mapping tools (see the archived Discord tutorials,
   `E:\Omni\Zomboid\docs\B42_MAPPING_DISCORD_TUTORIALS.md`, TUT-01). Writing this tile
   uniformly across a cell produced "square, aligned patches of vegetation" in-game — a
   visibly artificial, grid-regular pattern PZ's own procedural generation never produces.
   **Lesson for any future format-verification test: never use natural/blend content as
   the test signal. Use something that cannot be confused with the default renderer's own
   output.**

## Confirmed format specification

```
map_folder structure:
  <mod>/42/mod.info                              (lean version-folder override)
  <mod>/42/poster.png
  <mod>/common/mod.info                          (duplicate, same content)
  <mod>/common/poster.png
  <mod>/common/media/maps/<mapId>/map.info       lots=Muldraugh, KY
  <mod>/common/media/maps/<mapId>/objects.lua    comment-only, no BOM
  <mod>/common/media/maps/<mapId>/spawnpoints.lua  unemployed key, no BOM
  <mod>/common/media/maps/<mapId>/thumb.png
  <mod>/common/media/maps/<mapId>/<cellX>_<cellY>.lotheader
  <mod>/common/media/maps/<mapId>/world_<cellX>_<cellY>.lotpack
  <mod>/common/media/maps/<mapId>/chunkdata_<cellX>_<cellY>.bin

.lotheader:
  "LOTH" (4B) + version=1 (U32LE) + entry_count (U32LE)
  + entry_count newline-separated ASCII tile names
  + 1048-byte trailer: first two U32LE both = 8, rest zero
  (entry count/names vary per cell; 5 and 20 both confirmed working)

world_X_Y.lotpack:
  "LOTP" (4B) + version=1 (U32LE) + chunk_count=1024 (U32LE)
  + 1024 x U64LE cumulative byte offsets (first = 12 + 1024*8 = 8204)
  + chunk bodies, each an 8x8 (64-slot) grid encoded as a mix of:
      Type A (8B):  [U32=0xFFFFFFFF][U32=run_length]   -- N default slots
      Type B (12B): [U32=2][U32=0xFFFFFFFF][U32=tile_index]  -- one explicit tile
    summing to exactly 64 slots per chunk. Uniform 64xType-B (768B/chunk) confirmed
    sufficient; Type A mixing observed in real data but not re-derived/implemented.

chunkdata_X_Y.bin:
  header 00 01 (2B) + 128 x 8B records (existing MAP-37B shape, UNCHANGED).
  Real chunkdata bodies are confirmed NON-zero in every working reference seen
  (vanilla: grouped 0x08 bytes; community sample: regular 0x05 pattern) but exact
  semantics are still undecoded. This profile still emits the old zero-body candidate.
```

## What shipped (commit `edbd364`, pushed to `origin/map10_loader_trace`)

- New `--build42-candidate-profile renderable_v1` on the existing
  `Build42CandidateWriterCommand` (`src/PZMapForge.Cli/Program.cs`) implementing items
  1, 2, and 4 above exactly as confirmed. Item 5 (distinctive tile choice) is baked into
  the profile's default tile-name table.
- `empty_grass_v0`–`v5` are completely unchanged — this is purely additive.
- 21 new CLI process tests
  (`tests/PZMapForge.Cli.Tests/MapExportBuild42CandidateWriterRenderableV1ProcessTests.cs`)
  covering folder structure, `map.info`, lotheader/lotpack byte-shape and content, no-BOM
  encoding, and report claim-boundary fields. All pre-existing 143 Build42CandidateWriter
  tests still pass; full `Core.Tests` (2725/2725) unaffected.
- Marked **PROVISIONAL** (not Ratified) in `docs/IMPLEMENTATION.md`'s "Provisional:
  present but not load-tested" table.
- `CHANGELOG.md` entry under `[Unreleased]`.

## What is NOT done

- **Chunkdata semantics are still unknown.** The profile emits the old all-zero body;
  a real, non-zero example (borrowed byte-for-byte from MyMapMod's `chunkdata_32_46.bin`
  during live testing, not committed to this repo) was present in the one confirmed-
  working in-game test, but all-zero chunkdata was also tested against a correct
  lotpack/lotheader without visibly breaking anything further — so its actual necessity
  and meaning remain open questions.
- **Type A/Type B mixed run-length chunk encoding** was decoded from real data by
  inspection but never independently re-derived by writing a new mixed chunk by hand and
  confirming it renders. The writer only emits the simpler uniform-64×Type-B shape.
- **No object/building/vegetation-as-discrete-entity content has been proven** — only a
  single repeated ground-tile reference was confirmed. The "square patches of aligned
  bushes" result came entirely from the ground-tile layer.
- **No MAP-3x differential-control-test packet exists for `renderable_v1`** the way
  MAP-37E built one for the old format. Promoting this from PROVISIONAL to Ratified
  needs either a repeatable automated proof or several independent human confirmations.
- **Only tested once, at one cell** (35,27 / Riverside Rd, world coords 10746,8288),
  by one human, one time. Treat every claim above as "consistent with the evidence
  gathered," not as settled fact.

## Recommended next step

Before building further on this (chunkdata decoding, object placement, or wiring the
stalled MAP-25x WorldBuilder pipeline into this writer): **run one more independent
confirmation test.** Host with `renderable_v1` again, ideally at a different populated
cell, and confirm the same kind of visibly artificial, non-procedural pattern appears.
This is cheap insurance against the whole finding being an artifact of one specific test
run, before spending more engineering effort on top of it.

After that, the two live forks are:
1. Decode chunkdata + real object/building placement — the piece that would make this
   format actually useful for content, and the natural connection point for MAP-25x
   WorldBuilder's stalled lot/building planning pipeline (it has never had a writer to
   target).
2. Formalize `renderable_v1` into full MAP-3x doctrine with its own differential-control-
   test packet, promoting PROVISIONAL → Ratified with real evidence discipline first.
