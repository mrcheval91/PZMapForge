# MAP-37D Human Runtime Load-Test Packet

MAP37D_HUMAN_RUNTIME_LOAD_TEST_PACKET_DEFINED

> **Superseded by MAP-37E.** Outcome 7 below (TERRAIN_MOUNT_SUCCESS = "player
> spawns at correct coordinate and terrain is visible") is not a sufficient
> criterion: MAP-9N/MAP-9Q showed Build 42 can render generic procedural
> wilderness even when the generated 35_27 cell files are removed entirely.
> The server wiring below also reuses the MAP-9D shape (self-token +
> `;Muldraugh, KY`) that MAP-9D showed produces an empty IsoMetaGrid
> map-folder list, rather than the MAP-9K/9L/9Q shape that actually mounted.
> Do not run this packet as-is to claim terrain mount evidence. Use
> [MAP_37E_DIFFERENTIAL_CONTROL_TEST.md](MAP_37E_DIFFERENTIAL_CONTROL_TEST.md)
> instead. This document is kept unmodified below as the historical record.

## Purpose

Defines the controlled human runtime test for the MAP-37C chunkdata candidate.
The operator manually installs the staged candidate and records terrain mount evidence.
Claude does not run Project Zomboid or write to Steam, Workshop, or live server folders.

## What is tested

Whether Project Zomboid Build 42 accepts the MAP-37B/C chunkdata shape:
- chunkdata_35_27.bin: 1026 bytes, header 0x00 0x01, 128 × 8-byte zero records
- Cell identity: 35_27 (cellX=35, cellY=27)
- Map folder: pzmapforge_map37c (self-referential, lots=pzmapforge_map37c)

## Generated packet

`scripts/prepare-build42-map37d-human-runtime-load-test-packet.ps1` generates under `.local/`:
- `map37d-human-runtime-load-test-packet.json` — preflight JSON (schema v0.1)
- `map37d-human-runtime-load-test-packet.md` — preflight MD
- `MAP_37D_HUMAN_RUNTIME_LOAD_TEST_PACKET.md` — operator overview
- `MAP_37D_HUMAN_INSTALL_STEPS.md` — HUMAN-ONLY install checklist
- `MAP_37D_SERVER_WIRING.md` — Mods= and Map= lines + spawn coordinate
- `MAP_37D_LOG_CAPTURE_COMMANDS.md` — log capture instructions
- `MAP_37D_SUCCESS_FAILURE_CRITERIA.md` — 7 outcome criteria
- `MAP_37D_RUNTIME_RESULT_RECORD.md` — result recording template

The script also invokes `prepare-build42-map37c-chunkdata-staged-packet.ps1` to
regenerate the MAP-37C candidate and captures its SHA-256 as binary evidence.

## Server wiring

```
Mods=pzmapforge_map37c
Map=pzmapforge_map37c;Muldraugh, KY
```

Spawn: worldX=35 worldY=27 posX=150 posY=150 → PZ world (10650, 8250, 0)

## Seven outcome criteria

1. MOD_NOT_LOADED — mod absent from server/client mod list
2. SPAWN_METADATA_FAIL — player spawns at wrong coordinate
3. MAP_FOLDER_REGISTRATION_FAIL — IsoMetaGrid does not list pzmapforge_map37c
4. CHUNKDATA_PARSE_FAIL — log shows error on chunkdata/lotheader/lotpack parse
5. MULDRAUGH_FALLBACK — visible world is vanilla Muldraugh, no pzmapforge_map37c terrain
6. EMPTY_WORLD — player spawns at correct coordinate but terrain is empty/water
7. TERRAIN_MOUNT_SUCCESS — player spawns at correct coordinate and terrain is visible

## Claim boundary

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
HUMAN_ONLY_INSTALL_REQUIRED=true
CLAUDE_RAN_PZ=false
CLAUDE_WROTE_STEAM=false
CLAUDE_WROTE_WORKSHOP=false
staged_output_local_only=true
playable_terrain_mount_proven=false (pending operator runtime result)
