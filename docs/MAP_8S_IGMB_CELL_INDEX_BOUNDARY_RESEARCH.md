# MAP-8S IGMB Cell Index Boundary Research

```text
Status: MAP-8S IGMB cell index boundary research defined
Classification: MAP8S_IGMB_CELL_BOUNDARY_RESEARCH_DEFINED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Operator approval

The operator approved bounded IGMB cell index boundary research.

Approval scope:
- Read-only, bytes after string_pool_end_offset_candidate=133, within first 4096 bytes.
- No copying of any Project Russia or vanilla files.
- No full binary read.
- No binary writer.
- No PZ run.
- No Workshop upload.

## Source basis

MAP-8R recorded the real IGMB structure result:
- 12 U16LE LP strings detected in first 4096 bytes.
- offset-20 U32LE = 12 = detected LP string count.
- string_pool_end_offset_candidate=133.
- partial_header_model_confidence=medium.
- full_format_understood=false; geometry_payload_understood=false.

This step examines bytes after offset 133 within the already-approved 4096-byte window.

Reference path:
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia\worldmap.xml.bin

## Inspector

scripts/inspect-build42-igmb-cell-boundary.ps1
- Params: -ReferenceWorldmapBinPath, -CandidateWorldmapBinPath (optional), -Output,
  -MaxBytes (default 4096, hard cap 4096), -StringPoolEndOffset (default 133)
- Reads at most min(file_size, MaxBytes, 4096) bytes via FileStream read-only.
- .local/ guard on -Output.
- No files copied.

Output fields:
- schema=pzmapforge.map8s-igmb-cell-boundary-inspection.v0.1
- reference_present, reference_size_bytes, bytes_read_count
- max_bytes_allowed=4096, full_file_read=false
- magic=IGMB, version_le_u32=2
- string_pool_end_offset, post_string_pool_window_start
- post_string_pool_window_bytes_available
- first_128_bytes_after_string_pool_hex
- first_256_bytes_after_string_pool_hex
- u32le_values_after_string_pool_first_128: U32LE from postStart, 4-byte steps
- u32le_aligned_boundary_hypothesis: U32LE from next 4-byte aligned offset after postStart
- u16le_values_after_string_pool_first_128: U16LE from postStart, 2-byte steps
- float32le_values_after_string_pool_first_128: float32LE heuristic, marked as heuristic
- plausible_count_fields_after_string_pool: U32LE values in range 1..65535
- plausible_offset_table_candidates: U32LE values in range 1..(file_size-1)
- plausible_cell_coordinate_candidates: U32LE values in range 1..500 (heuristic)
- zero_run_candidates: runs of >= 4 zero bytes
- repeated_pattern_candidates: consecutive identical U32LE pairs
- section_boundary_hypotheses: list of low-confidence structural hypotheses
- confidence_level=low
- full_format_understood=false
- cell_index_understood=false
- geometry_payload_understood=false
- writer_implementation_allowed=false
- binary_writer_gate_closed=true
- playable_claim_allowed=false
- third_party_files_copied=false
- next_branch=igmb_cell_index_model_research_pending_operator_approval_if_boundary_evidence_sufficient

## What this is NOT

- This is NOT a cell index specification.
- The full IGMB format structure beyond the string pool is not confirmed.
- Community layout notes remain unverified supporting context only.
- worldmap.xml.bin is not claimed as a proven Build 42 requirement.
  It is the leading discriminator / strongest hypothesis.

## Status labels

```text
MAP8S_IGMB_CELL_BOUNDARY_RESEARCH_DEFINED
OPERATOR_APPROVED_CELL_INDEX_BOUNDARY_RESEARCH=true
MAX_BYTES_ALLOWED=4096
BINARY_CONTENTS_FULL_READ=false
THIRD_PARTY_FILES_COPIED=false
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
CONFIDENCE_LEVEL=low
```

## MAP-8T result

The operator ran the MAP-8S inspector. Real result: all bytes from offset 133 through
the 4096-byte window are 0xFF (padding/sentinel). No count field, offset table, or
cell coordinate was observed.

See: docs/MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT.md

Classification: MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED
first_non_ff_offset_known=false
immediate_cell_index_after_string_pool_supported=false
next_branch=igmb_first_non_ff_transition_scan_pending_operator_approval

## Next branch

next_branch=igmb_first_non_ff_transition_scan_pending_operator_approval (updated by MAP-8T)

The real MAP-8S run showed entirely 0xFF bytes after offset 133 within the 4096-byte
window. The first non-FF transition offset is unknown (beyond the observed window).
Locating it requires a separate explicit operator approval.

Binary writer gate remains CLOSED until IsoMetaGrid logs a parse attempt against
PZMapForge lotheader/sidecar.
