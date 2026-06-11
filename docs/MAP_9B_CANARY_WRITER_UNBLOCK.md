# MAP-9B Canary Writer Unblock Research

```text
Status: MAP-9B research complete
Classification: MAP9B_CANARY_WRITER_UNBLOCK_OUTCOME_B
Outcome: B -- canary impossible with current writer
Inspected: repo-only (inspected_repo_only=true, pz_assets_read=false)
Canary writer available: false
Canary writer blocked: true
Visible tile encoding supported: false
Next research branch: map9b_lotp_chunk_payload_format_research
Public playable claim: not allowed
```

---

## Inspection scope

Repo-only inspection of `src/PZMapForge.Cli/Program.cs`
(`Build42CandidateWriterCommand`, lines 1625-2041) and
`tests/PZMapForge.Cli.Tests/MapExportBuild42CandidateWriterV2ProcessTests.cs`.

No Project Zomboid assets were read. No PZ game files were opened.
No PZ run was performed. No Workshop upload was performed.

```text
inspected_repo_only=true
pz_assets_read=false
pz_run_performed=false
workshop_upload_performed=false
steam_write_performed=false
third_party_files_copied=false
```

---

## What the writer controls

The `Build42CandidateWriterCommand` function (five profiles: empty_grass_v0 through empty_grass_v4)
produces the following file types:

| File | Format | Content |
|---|---|---|
| `{x}_{y}.lotheader` | LOTH magic + version 1 + N entries + optional 1048-byte trailer | Tile names: `blends_grassoverlays_01_0..1023` only |
| `world_{x}_{y}.lotpack` | LOTP magic + version 1 + sequential offset table + 1024 chunks | 1024 x 1024 all-zero payload bytes |
| `chunkdata_{x}_{y}.bin` | `0x0001` header + body | 1024 zero bytes |
| `objects.lua` | Lua file | `return {}` or comment-only placeholder |
| `spawnpoints.lua` | Lua file | `all` or `unemployed` spawn point |
| `mod.info`, `map.info` | Text metadata | Candidate boundary text |

---

## Exact blockers

### Blocker 1: lotp_chunk_payload_format_not_understood

The 1024-byte per-chunk payload within the lotpack is all-zero for every chunk across all
profiles. The binary format needed to encode tile types, tile positions, or terrain indices
within a chunk has not been reverse-engineered. Writing non-zero bytes without knowing this
format would produce unknown results (crash, corrupt load, or silent mismatch).

### Blocker 2: lotheader_tile_table_visual_mapping_not_understood

The lotheader tile name table (`blends_grassoverlays_01_0..1023`) is a string lookup table.
The mapping between lotheader entry index and in-game visual tile placement is not understood.
Substituting different tile names (e.g., asphalt, water) in the lotheader is not sufficient:
the LOTP payload must correctly reference those entries with non-zero tile record data at
defined positions.

### Blocker 3: chunkdata_format_not_understood

The chunkdata binary (`0x0001` header + 1024 zero bytes) structure beyond the two-byte header
has not been decoded. Whether chunkdata controls visual appearance or only load/registration
state is not known.

### Blocker 4: no_tile_placement_record_model

The writer has no concept of tile ID, tile position, or object coordinates at the LOTP payload
level. There are no records of the form `(x, y, tileId)` or similar. Without this model, it is
not possible to encode a visually distinctive cell.

---

## Profile comparison

All five profiles (empty_grass_v0 through empty_grass_v4) produce the same binary outcome
for tile data: LOTP all-zero payload, chunkdata all-zero body. Profile differences are limited
to metadata (objects.lua encoding, spawnpoints key format, LOTH trailer presence). No profile
encodes any tile placement record.

---

## Outcome

**Outcome B -- canary impossible with current writer.**

```text
canary_writer_available=false
canary_writer_blocked=true
visible_tile_encoding_supported=false
canary_strategy_available=false
```

Do not fake a canary. No visual-success claim is allowed until the LOTP chunk payload format
is understood and a writer capable of encoding tile positions is implemented.

---

## Next research branch

next_research_branch=map9b_lotp_chunk_payload_format_research

To unblock the canary writer, the following research is required:
1. Reverse-engineer the LOTP chunk payload format from a reference PZ map mod.
2. Identify how tile IDs are encoded within a 1024-byte chunk (or the actual chunk size).
3. Confirm the lotheader tile name to LOTP tile index mapping.
4. Implement a new profile that writes a non-zero LOTP payload with a known tile pattern.
5. Local-only test first; no PZ run until output format is confirmed.

---

## Classification labels

```text
MAP9B_CANARY_WRITER_UNBLOCK_OUTCOME_B
OUTCOME=B
CANARY_WRITER_AVAILABLE=false
CANARY_WRITER_BLOCKED=true
VISIBLE_TILE_ENCODING_SUPPORTED=false
CANARY_STRATEGY_AVAILABLE=false
INSPECTED_REPO_ONLY=true
PZ_ASSETS_READ=false
PZ_RUN_PERFORMED=false
WORKSHOP_UPLOAD_PERFORMED=false
STEAM_WRITE_PERFORMED=false
THIRD_PARTY_FILES_COPIED=false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NEXT_RESEARCH_BRANCH=map9b_lotp_chunk_payload_format_research
```
