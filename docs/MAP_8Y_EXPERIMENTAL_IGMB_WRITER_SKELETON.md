# MAP-8Y Experimental IGMB Writer Skeleton

```text
Status: MAP-8Y experimental IGMB writer skeleton added
Classification: MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED
Binary writer gate: OPEN (experimental local-only skeleton, operator-approved)
Playable claim: not allowed
```

## Source basis

MAP-8X recorded the real MAP-8W transition structure inspection result.
The operator explicitly approved an experimental, local-only IGMB writer skeleton
that generates a PZMapForge-owned `worldmap.xml.bin` candidate from scratch using
the observed IGMB structure from MAP-8Q through MAP-8X.

No third-party bytes are copied. All content is generated or reproduced from
PZMapForge-observed structure constants.

## Writer

Script: `scripts/write-build42-experimental-igmb-worldmap.ps1`

Parameters:
- `-Output` — required; must contain `.local`; forbidden paths refused
- `-MapId` — manifest identifier (default: `pzmapforge_build42_candidate_v4_001`)
- `-ParentMapFolder` — manifest parent folder name (default: `PZMapForge`)
- `-TotalBytes` — total output file size, range 8192-65536 (default: 65536)
- `-TransitionOffset` — offset where triplet begins after FF padding (default: 6389)
- `-StringPoolEndOffset` — offset where string pool ends (default: 133)

Forbidden output paths: `media\maps`, `Steam`, `workshop`, `ProjectZomboid`,
`C:\Program Files`, `D:\Program Files`.

## Binary layout (65536 bytes, default params)

| Offset | Content | Source |
|--------|---------|--------|
| 0-3 | IGMB magic (49 47 4D 42) | MAP-8P evidence |
| 4-7 | U32LE 2 (version) | MAP-8P/8Q evidence |
| 8-11 | U32LE 256 (unknown_a) | MAP-8Q/8R evidence |
| 12-15 | U32LE 59 (unknown_b) | MAP-8Q/8R evidence |
| 16-19 | U32LE 68 (unknown_c) | MAP-8Q/8R evidence |
| 20-23 | U32LE 12 (string_pool_count) | MAP-8R evidence |
| 24-132 | U16LE LP string pool (12 strings) | MAP-8R evidence |
| 133-6388 | 0xFF padding (6256 bytes) | MAP-8T/8U/8V evidence |
| 6389-6400 | U32LE triplet: 30, 26, 9 | MAP-8V/8W/8X evidence |
| 6401-6432 | Synthetic U16LE pairs (PZMapForge-owned) | Generated from scratch |
| 6433-65535 | 0xFF padding | Generated from scratch |

String pool (12 strings, offsets 24-132):
Polygon, highway, primary, trail, natural, forest, water, river, tertiary, building, Residential, secondary.

## What this is NOT

- This does NOT produce a playable Project Zomboid map.
- This does NOT copy any bytes from Project Russia or any other reference file.
- This does NOT claim that the binary format is understood.
- This does NOT claim that the triplet values 30/26/9 are confirmed field types.
- This does NOT claim that the cell index or geometry payload are understood.
- The output is local-only and must not be submitted to Steam Workshop.
- No load test has been performed.

## Safety

```text
MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED
EXPERIMENTAL_WRITER_LOCAL_ONLY
BINARY_WRITER_GATE_OPEN_FOR_EXPERIMENTAL_LOCAL_SKELETON_ONLY
WRITES_WORLDMAP_XML_BIN=true
OUTPUT_SCOPE=.local_only
WRITER_STATUS=experimental_skeleton_not_load_proven
THIRD_PARTY_BYTES_COPIED=false
PROJECT_RUSSIA_FILE_READ=false
PZ_RUN_PERFORMED=false
WORKSHOP_UPLOAD_PERFORMED=false
PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_PERFORMED=false
FULL_FORMAT_UNDERSTOOD=false
CELL_INDEX_UNDERSTOOD=false
GEOMETRY_PAYLOAD_UNDERSTOOD=false
TRIPLET_FIELDS_PROVEN=false
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
```

## Next branch

next_branch=map8z_controlled_install_packet_pending_operator_approval

Operator reviews the generated worldmap.xml.bin candidate and decides whether to
approve a controlled install packet (MAP-8Z) for local game-load testing.
Playable claim remains not allowed until a real load test is performed.

## MAP-8Z reference

MAP-8Z controlled install packet created (operator-approved, sha256 confirmed):
- Doc: `docs/MAP_8Z_CONTROLLED_IGMB_INSTALL_PACKET.md`
- Script: `scripts/prepare-build42-map8z-controlled-igmb-install-packet.ps1`
- Tests: `scripts/test-build42-map8z-controlled-igmb-install-packet.ps1` (24 assertions)
- next_branch=human_runtime_test_pending
