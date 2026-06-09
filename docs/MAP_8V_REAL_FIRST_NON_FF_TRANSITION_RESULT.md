# MAP-8V Real First Non-FF Transition Result

```text
Status: MAP-8V real first non-FF transition result recorded
Classification: MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Source basis

MAP-8U defined the bounded first non-FF transition scanner.
The operator ran scripts/inspect-build42-igmb-first-non-ff-transition.ps1 against the actual
Project Russia reference worldmap.xml.bin (283881 bytes, first 65536 bytes read).

Reference path (operator-local, not copied into repo):
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia\worldmap.xml.bin

## Actual MAP-8U real run result

- schema=pzmapforge.map8u-igmb-first-non-ff-transition-inspection.v0.1
- reference_present=true, reference_size_bytes=283881
- bytes_read_count=65536, max_bytes_allowed=65536, full_file_read=false
- magic=IGMB, version_le_u32=2
- string_pool_end_offset=133
- scan_start_offset=133, scan_end_offset_exclusive=65536

## Critical finding: FF region length and first non-FF offset

| Field | Value |
|-------|-------|
| first_non_ff_found | true |
| first_non_ff_offset | 6389 |
| first_non_ff_relative_offset_after_string_pool | 6256 |
| ff_run_start_offset | 133 |
| ff_run_length_until_first_non_ff | 6256 |
| transition_offset_is_4_byte_aligned | false |
| transition_offset_is_2_byte_aligned | false |

The FF region extends 6256 bytes from string_pool_end_offset=133 to offset 6388 inclusive.
The first non-FF byte is at offset 6389. This offset is NOT 2-byte or 4-byte aligned.

## Exact-offset decoding (observed-only, unconfirmed)

Hex window after transition (starts at absolute offset 6389):
```text
1e 00 00 00 1a 00 00 00 09 00 00 00 00 00 02 14
00 ff 00 f4 00 ff 00 27 01 fb 00 2d 01 fb 00 b8
01 02 01 c6 01 0b 01 ce 01 1f 01 e2 01 33 01 f6
01 3d 01 01 02 4d 01 09 02 a4 01 09 02 a4 01 13
```

Exact U32LE values from absolute offset 6389 (unaligned read, little-endian):
| Index | Absolute Offset | Value (U32LE) |
|-------|-----------------|---------------|
| 0 | 6389 | 30 |
| 4 | 6393 | 26 |
| 8 | 6397 | 9 |

These values (30, 26, 9) may be counts, table lengths, section dimensions, or other structure
fields. This is observed evidence only. No hypothesis is confirmed.

aligned_u32le_values_are_context_only=true — aligned reads from a non-aligned start are
context evidence, not structure proof.

## What this is NOT

- This does NOT prove the cell index is understood.
- This does NOT prove the geometry payload is understood.
- This does NOT prove the full IGMB format is understood.
- The values 30, 26, 9 are not confirmed as any specific field type.
- The structure after the FF region requires separate dedicated analysis.
- The 6256-byte FF region purpose is unknown (padding, sentinel, reserved space, uninitialized).

## Inspector hardening added (MAP-8V)

scripts/inspect-build42-igmb-first-non-ff-transition.ps1 updated with exact-offset decoding:
- exact_offset_u32le_values_from_transition: U32LE read at steps of 4 from first_non_ff_offset
- exact_offset_u16le_values_from_transition: U16LE read at steps of 2 from first_non_ff_offset
- exact_offset_hex_first_32_bytes: hex string of first 32 bytes from transition
- exact_offset_hex_first_64_bytes: hex string of first 64 bytes from transition
- exact_offset_small_u32_candidates: exact U32LE values in range 1..65535
- transition_exact_offset_decoding_added=true
- aligned_u32le_values_are_context_only=true
- transition_structure_understood=false

## Safety

```text
MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED
OPERATOR_RAN_MAP8U_SCANNER=true
FIRST_NON_FF_OFFSET=6389
FF_RUN_LENGTH=6256
TRANSITION_OFFSET_IS_4_BYTE_ALIGNED=false
TRANSITION_OFFSET_IS_2_BYTE_ALIGNED=false
EXACT_OFFSET_DECODING_ADDED=true
ALIGNED_U32LE_VALUES_ARE_CONTEXT_ONLY=true
TRANSITION_STRUCTURE_UNDERSTOOD=false
FULL_FORMAT_UNDERSTOOD=false
CELL_INDEX_UNDERSTOOD=false
GEOMETRY_PAYLOAD_UNDERSTOOD=false
WRITER_IMPLEMENTATION_ALLOWED=false
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
BINARY_CONTENTS_READ_SCOPE=first_65536_bytes_only
BINARY_CONTENTS_FULL_READ=false
MAX_BYTES_ALLOWED=65536
```

## Next branch

next_branch=igmb_transition_structure_analysis_pending_operator_approval

Structural analysis of the bytes after the transition requires explicit operator approval.
Binary writer gate remains CLOSED until IsoMetaGrid logs a parse attempt against
PZMapForge lotheader/sidecar.

## MAP-8W reference

MAP-8W (igmb_transition_structure_analysis) was approved and implemented.
Inspector: scripts/inspect-build42-igmb-transition-structure.ps1
Doc: docs/MAP_8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS.md
Classification: MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED
