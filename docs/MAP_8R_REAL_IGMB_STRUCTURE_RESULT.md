# MAP-8R Real IGMB Structure Result

```text
Status: MAP-8R real IGMB structure result recorded
Classification: MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Source basis

MAP-8Q defined the IGMB structure inspector.
The operator ran scripts/inspect-build42-igmb-structure.ps1 against the actual
Project Russia reference worldmap.xml.bin (283881 bytes), reading at most 4096 bytes.

Reference path:
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia\worldmap.xml.bin

Candidate path:
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3740642200\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\PZMapForge\worldmap.xml.bin

Output path: .local\map8q-igmb-structure

## Actual MAP-8Q inspector result

- schema=pzmapforge.map8q-igmb-structure-inspection.v0.1
- reference_present=true, reference_size_bytes=283881
- bytes_read_count=4096, max_bytes_allowed=4096, full_file_read=false
- magic=IGMB, version_le_u32=2

## Observed U32LE header values (first 24 bytes)

| Offset | U32LE Value | Note |
|--------|-------------|------|
| 0 | 1112360777 | ASCII IGMB as U32LE |
| 4 | 2 | version |
| 8 | 256 | unknown_a |
| 12 | 59 | unknown_b |
| 16 | 68 | unknown_c |
| 20 | 12 | probable_string_pool_count |

## Observed U16LE length-prefixed strings

| Offset | Len | Value |
|--------|-----|-------|
| 24 | 7 | Polygon |
| 33 | 7 | highway |
| 42 | 7 | primary |
| 51 | 5 | trail |
| 58 | 7 | natural |
| 67 | 6 | forest |
| 75 | 5 | water |
| 82 | 5 | river |
| 89 | 8 | tertiary |
| 99 | 8 | building |
| 109 | 11 | Residential |
| 122 | 9 | secondary |

Total: 12 LP strings detected. Offset-20 field value = 12. Match confirmed.

## Probable partial IGMB header model

```text
0x00  char[4]  magic = IGMB
0x04  u32le    version = 2
0x08  u32le    unknown_a = 256
0x0C  u32le    unknown_b = 59
0x10  u32le    unknown_c = 68
0x14  u32le    probable_string_pool_count = 12
0x18  string_pool_start (offset 24)
      format: U16LE byte length + ASCII/UTF-8 bytes (no null terminator observed)
```

string_pool_end_offset_candidate = 133 (last string: offset 122 + 2 + 9 = 133)

Confidence: medium. The string count matches the offset-20 field value exactly.
This is the strongest hypothesis derived from the first 4096 bytes alone.

## What is NOT known

- full_format_understood=false
- geometry_payload_understood=false
- cell_index_understood=false
- What follows offset 133 is not confirmed from the first 4096 bytes
- The field roles of unknown_a=256, unknown_b=59, unknown_c=68 are not confirmed
- This is a partial header model, not a complete format specification

## Safety

```text
MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED
OPERATOR_RAN_MAP8Q_INSPECTOR=true
STRING_POOL_COUNT_MATCHES_HEADER_OFFSET_20=true
PARTIAL_HEADER_MODEL_CONFIDENCE=medium
FULL_FORMAT_UNDERSTOOD=false
GEOMETRY_PAYLOAD_UNDERSTOOD=false
WRITER_IMPLEMENTATION_ALLOWED=false
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
BINARY_CONTENTS_READ_SCOPE=first_4096_bytes_only
BINARY_CONTENTS_FULL_READ=false
MAX_BYTES_ALLOWED=4096
```

## Next branch

next_branch=igmb_cell_index_boundary_research_pending_operator_approval

The string pool ends at offset 133 (estimated). What follows is the cell index
or geometry payload — not confirmed from the first 4096 bytes alone. A bounded
cell index boundary research step requires explicit operator approval.

Binary writer gate remains CLOSED until IsoMetaGrid logs a parse attempt
against PZMapForge lotheader/sidecar.
