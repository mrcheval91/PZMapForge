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

## worldmap.xml.bin community claims triage

Community and public sources contain claims about the worldmap.xml.bin format and role.
These are recorded as unverified research leads only. They are not adopted as doctrine.
Measured evidence from our own Build 42 Project Russia sample takes precedence.

### Measured evidence (Build 42 Project Russia sample)

```text
measured_igmb_magic=IGMB (49 47 4D 42)
measured_igmb_magic_status=measured_in_project_russia_b42_sample
measured_header_little_endian=true
measured_string_pool_length_prefix=U16LE
measured_string_pool_contents=Polygon,highway,primary,trail,natural,forest,water,river,tertiary,building,Residential,secondary
```

### Community claim classifications

```text
community_claim_wmxm_magic_status=contradicted_by_our_measured_B42_Project_Russia_IGMB_sample
community_claim_big_endian_status=contradicted_or_unproven_by_measured_little_endian_header_and_u16le_string_pool
community_claim_string_property_table_status=partially_supported_by_measured_string_pool
worldmap_bin_creates_playable_terrain=false_or_irrelevant_to_playable_terrain_canary
both_xml_and_bin_required_for_map_ui=unverified_research_lead
missing_bin_causes_silent_map_ui_failure=unverified_research_lead
b42_cell_size_256=unverified_research_lead_unless_repo_evidence_proves_it
worlded_write_features_xml_generates_both=unverified_research_lead_unless_source_evidence_added
polygonalmap2_worldmap_xml_reader=unverified_research_lead_unless_source_evidence_added
```

### worldmap.xml.bin role

worldmap.xml.bin is classified as map UI / vector metadata unless a terrain role is explicitly proven.
It is not the playable-terrain canary gate.
The playable-world canary (IsoMetaGrid mounting PZMapForge cells) is separate from any map UI canary.

```text
worldmap_bin_role=map_ui_vector_metadata_unless_terrain_role_proven
worldmap_bin_playable_terrain_canary_supported=false
playable_world_canary_separate_from_map_ui_canary=true
community_claims_integrated_as_unverified_research_leads=true
community_claims_not_adopted_as_doctrine=true
measured_igmb_header_takes_precedence=true
```

---

## Debug runtime evidence (Build 42.19.0 operator run)

Operator collected debug runtime logs by launching PZ with -debug.
Server-console.txt from the dedicated server folder was identified as stale (Build 41.78.19 / servertest)
and ignored for this runtime analysis.

```text
debug_runtime_logs_reviewed=true
debug_runtime_build=42.19.0
debug_runtime_workshop_runtime_cache_confirmed=true
debug_runtime_mod_loaded=true
debug_runtime_mod_id=pzmapforge_build42_candidate_v4_001
debug_runtime_workshop_item=3740642200
debug_runtime_workshop_path=D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\steamapps\workshop\content\108600\3740642200
```

IsoMetaGrid result: map folder list was empty. PZMapForge was NOT listed in the IsoMetaGrid
map folder scan. This is the primary blocker for playable terrain mount.

```text
debug_runtime_isometagrid_map_folder_list_empty=true
```

Spawn target metadata was accepted. Player position packet was delivered.

```text
debug_runtime_spawn_position=10746,8288,0
debug_runtime_spawn_metadata_works=true
```

No lotheader, lotpack, or chunkdata parse evidence appeared in the debug logs.
PZMapForge cells were not parsed or loaded.

```text
debug_runtime_pzmapforge_lotheader_parse_evidence=false
debug_runtime_pzmapforge_lotpack_parse_evidence=false
debug_runtime_pzmapforge_chunkdata_parse_evidence=false
debug_runtime_playable_terrain_mount_proven=false
```

Fresh world signal was observed (expected for new world; not a failure).

```text
debug_runtime_server_console_ignored_stale_b41=true
```

### Debug runtime classification

```text
MAP9B_DEBUG_RUNTIME_EVIDENCE_CONFIRMS_MOD_LOAD_BUT_NO_ISOMETAGRID_MAP_FOLDER
SPAWN_METADATA_WORKS=true
WORKSHOP_RUNTIME_CACHE_CONFIRMED=true
ISOMETAGRID_MAP_FOLDER_LIST_EMPTY=true
PZMAPFORGE_LOTHEADER_PARSE_EVIDENCE=false
PZMAPFORGE_LOTPACK_PARSE_EVIDENCE=false
PZMAPFORGE_CHUNKDATA_PARSE_EVIDENCE=false
PLAYABLE_TERRAIN_MOUNT_PROVEN=false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

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
COMMUNITY_CLAIMS_INTEGRATED_AS_UNVERIFIED_RESEARCH_LEADS=true
COMMUNITY_CLAIMS_NOT_ADOPTED_AS_DOCTRINE=true
MEASURED_IGMB_HEADER_TAKES_PRECEDENCE=true
COMMUNITY_CLAIM_WMXM_MAGIC_STATUS=contradicted_by_measured_b42_igmb_sample
MEASURED_IGMB_MAGIC_STATUS=measured_in_project_russia_b42_sample
WORLDMAP_BIN_PLAYABLE_TERRAIN_CANARY_SUPPORTED=false
PLAYABLE_WORLD_CANARY_SEPARATE_FROM_MAP_UI_CANARY=true
DEBUG_RUNTIME_LOGS_REVIEWED=true
DEBUG_RUNTIME_MOD_LOADED=true
DEBUG_RUNTIME_ISOMETAGRID_MAP_FOLDER_LIST_EMPTY=true
DEBUG_RUNTIME_SPAWN_METADATA_WORKS=true
DEBUG_RUNTIME_SERVER_CONSOLE_IGNORED_STALE_B41=true
```
