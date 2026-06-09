# MAP-8Q IGMB Structure Research

```text
Status: MAP-8Q IGMB structure research defined
Classification: MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Operator approval

The operator approved bounded IGMB structure research.

Approval scope:
- Read-only, at most first 4096 bytes from reference worldmap.xml.bin.
- No copying of any Project Russia or vanilla files.
- No full binary read.
- No binary writer.
- No PZ run.
- No Workshop upload.

## Source basis

MAP-8P confirmed IGMB magic (49 47 4D 42) in reference worldmap.xml.bin.
reference_size_bytes=283881. Binary writer gate remained closed.
This step extends the read window from 64 to 4096 bytes for structural observation only.

Reference path:
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia\worldmap.xml.bin

## Inspector

scripts/inspect-build42-igmb-structure.ps1
- Params: -ReferenceWorldmapBinPath, -CandidateWorldmapBinPath (optional), -Output, -MaxBytes (default 4096, hard cap 4096)
- Reads at most min(file_size, MaxBytes, 4096) bytes via FileStream read-only
- .local/ guard on -Output
- No files copied

Output fields:
- schema=pzmapforge.map8q-igmb-structure-inspection.v0.1
- reference_present, reference_size_bytes, bytes_read_count
- max_bytes_allowed=4096
- full_file_read=false
- magic=IGMB
- version_le_u32: bytes 4-7 as U32LE
- candidate_u32_values_first_64_le: array of U32LE values from first 64 bytes
- candidate_u16_values_first_64_le: array of U16LE values from first 64 bytes
- printable_ascii_runs_min_length_3: runs of printable ASCII >= 3 chars
- possible_length_prefixed_strings: U16LE len + len printable ASCII candidates
- possible_string_pool_offset_candidates: start offsets of LP string hits
- possible_string_pool_count_candidates: count of LP string candidates found
- possible_header_fields_observed_only: U32LE values in bytes 4-23 annotated
- unverified_format_hypotheses: list of low-confidence format notes
- confidence_level=low_to_medium
- binary_writer_gate_closed=true
- playable_claim_allowed=false
- third_party_files_copied=false
- next_branch=igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient

## What this is NOT

- This is NOT a format specification.
- The full IGMB format structure (header metadata, string pool layout, cell index,
  feature geometry payloads) is not confirmed from first 4096 bytes alone.
- Community layout notes remain unverified supporting context only.
- worldmap.xml.bin is not claimed as a proven Build 42 requirement.
  It is the leading discriminator / strongest hypothesis.

## Status labels

```text
MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED
OPERATOR_APPROVED_IGMB_STRUCTURE_RESEARCH=true
MAX_BYTES_ALLOWED=4096
BINARY_CONTENTS_FULL_READ=false
THIRD_PARTY_FILES_COPIED=false
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
CONFIDENCE_LEVEL=low_to_medium
```

## MAP-8R follow-up

The operator ran this inspector against the actual Project Russia worldmap.xml.bin.
The real result has been recorded in docs/MAP_8R_REAL_IGMB_STRUCTURE_RESULT.md.

Key findings (MAP-8R):
- 12 U16LE LP strings detected.
- offset-20 U32LE value = 12 matches detected count exactly.
- string_pool_count_matches_header_offset_20=true.
- string_pool_end_offset_candidate=133.
- partial_header_model_confidence=medium.
- full_format_understood=false.
- Classification: MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED.
- next_branch=igmb_cell_index_boundary_research_pending_operator_approval.

## Next branch

next_branch=igmb_cell_index_boundary_research_pending_operator_approval (MAP-8R follow-up)

The string pool ends at estimated offset 133. What follows (cell index or geometry
payload) requires explicit operator approval before investigation.

Binary writer gate remains CLOSED until IsoMetaGrid logs a parse attempt against
PZMapForge lotheader/sidecar.
