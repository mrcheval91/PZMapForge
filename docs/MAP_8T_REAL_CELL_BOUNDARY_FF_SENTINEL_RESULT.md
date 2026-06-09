# MAP-8T Real Cell Boundary FF Sentinel Result

```text
Status: MAP-8T real cell boundary FF sentinel result recorded
Classification: MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Source basis

MAP-8S defined the IGMB cell boundary inspector.
The operator ran scripts/inspect-build42-igmb-cell-boundary.ps1 against the actual
Project Russia reference worldmap.xml.bin (283881 bytes), reading at most 4096 bytes.

Reference path:
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia\worldmap.xml.bin

Output path: .local\ (operator-specified)

## Actual MAP-8S inspector result

- schema=pzmapforge.map8s-igmb-cell-boundary-inspection.v0.1
- reference_present=true, reference_size_bytes=283881
- bytes_read_count=4096, max_bytes_allowed=4096, full_file_read=false
- magic=IGMB, version_le_u32=2
- string_pool_end_offset=133
- post_string_pool_window_start=133
- post_string_pool_window_bytes_available=3963
- next_aligned_offset_after_string_pool=136

## Critical observed result: FF region after string pool

| Field | Value |
|-------|-------|
| first_128_bytes_after_string_pool_all_ff | true |
| first_256_bytes_after_string_pool_all_ff | true |
| U32LE values from offset 133 | -1 (0xFFFFFFFF) |
| U32LE aligned-boundary values from offset 136 | -1 (0xFFFFFFFF) |
| U16LE values | 65535 (0xFFFF) |
| float32LE values | NaN |
| plausible_count_fields_after_string_pool | [] |
| plausible_offset_table_candidates | [] |
| plausible_cell_coordinate_candidates | [] |
| zero_run_candidates | [] |
| repeated_pattern_candidates | consecutive -1 U32LE pairs through observed window |

## Interpretation

The immediate bytes after string_pool_end_offset=133 do not look like a cell index
table, offset table, count field, or coordinate table within the first 4096-byte window.

The observed post-string-pool region appears to be 0xFF padding, sentinel, or empty
space (all bytes are 0xFF in the observed window).

What this is NOT:

- This does NOT prove the whole file is FF after offset 133.
  Only the first 4096 bytes were read. The file is 283881 bytes.
  The cell index or geometry section likely exists beyond the observed window.
- This does NOT prove the cell index does not exist.
- This does NOT prove the full IGMB format is understood.
- The section_boundary_hypotheses remain unproven.

## What is NOT known

- full_format_understood=false
- cell_index_understood=false
- geometry_payload_understood=false
- first_non_ff_offset_known=false
  The first non-FF byte offset is beyond the current 4096-byte read window.
- The purpose of the FF padding region is not known.
  It may be a fixed-size reserved section, a sentinel-terminated region, or
  simply uninitialized memory in the reference file.

## Safety

```text
MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED
OPERATOR_RAN_MAP8S_INSPECTOR=true
FIRST_128_BYTES_AFTER_STRING_POOL_ALL_FF=true
FIRST_256_BYTES_AFTER_STRING_POOL_ALL_FF=true
IMMEDIATE_CELL_INDEX_AFTER_STRING_POOL_SUPPORTED=false
FIRST_NON_FF_OFFSET_KNOWN=false
FULL_FORMAT_UNDERSTOOD=false
CELL_INDEX_UNDERSTOOD=false
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

next_branch=igmb_first_non_ff_transition_scan_pending_operator_approval

The next research step is to locate the first non-FF transition offset beyond the
current 4096-byte read window. This requires a separate explicit operator approval
because it may require reading beyond the current 4096-byte cap.

Binary writer gate remains CLOSED until IsoMetaGrid logs a parse attempt against
PZMapForge lotheader/sidecar.

## Next step: MAP-8U

MAP-8U: Bounded first non-FF transition scan after IGMB string pool.
Operator approved reading at most 65536 bytes to locate the first non-FF byte.
See docs/MAP_8U_FIRST_NON_FF_TRANSITION_SCAN.md.
