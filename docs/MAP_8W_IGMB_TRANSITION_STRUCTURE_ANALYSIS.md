# MAP-8W IGMB Transition Structure Analysis

```text
Status: MAP-8W IGMB transition structure analysis approved and staged
Classification: MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Operator approval

The operator approved bounded IGMB transition structure analysis around offset 6389.

Approval scope:
- Read-only structure analysis.
- Start at transition_offset=6389.
- Maximum bytes to read: 65536 total bytes from worldmap.xml.bin.
- Do not read beyond the first 65536 bytes.
- Do not read the full Project Russia file (283881 bytes).
- No copying of any Project Russia or vanilla files.
- No binary writer.
- No playable claim.
- No Project Zomboid run.
- No Workshop upload.
- No SteamCMD.

Source basis: MAP-8V recorded the real first non-FF transition result.
- first_non_ff_offset=6389
- ff_run_length=6256 (FF region from string_pool_end_offset=133 to offset 6388)
- transition_offset NOT 4-byte or 2-byte aligned
- Exact U32LE triplet at transition (unaligned, observed-only): 30, 26, 9

## Inspector

scripts/inspect-build42-igmb-transition-structure.ps1

Parameters:
- -ReferenceWorldmapBinPath (Mandatory)
- -Output (Mandatory, must contain .local/)
- -TransitionOffset (default 6389)
- -MaxBytes (default 65536, hard cap 65536)
- -WindowBeforeBytes (default 64, hard cap 256)
- -WindowAfterBytes (default 512, hard cap 2048)

Reads at most min(file_size, MaxBytes, 65536) bytes via FileStream read-only.
If TransitionOffset is outside bytes_read_count, reports safely without crash.

Schema: pzmapforge.map8w-igmb-transition-structure-inspection.v0.1

Key output fields:
- Exact U32LE, U16LE, I16LE, byte values from transition (first 128 bytes)
- candidate_header_u32_triplet (30, 26, 9 as observed-only)
- candidate_count_fields, candidate_offset_fields
- candidate_coordinate_fields, candidate_signed_coordinate_fields
- candidate_run_length_patterns, candidate_repeated_pairs
- candidate_small_value_clusters, candidate_ff_or_null_sentinels_after_transition
- candidate_monotonic_sequences
- entropy_estimate_transition_window
- structure_hypotheses_observed_only
- strongest_current_hypothesis

## Analysis constraints

- The values 30, 26, 9 are treated as observed-only candidate fields.
- Patterns are recorded. Conclusions are not drawn.
- No claim is made about the cell index being understood.
- No claim is made about the geometry payload being understood.
- No encoder is designed.
- No worldmap.xml.bin is written.

## Safety

```text
MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED
TRANSITION_OFFSET=6389
FF_RUN_LENGTH=6256
TRANSITION_OFFSET_IS_4_BYTE_ALIGNED=false
CANDIDATE_TRIPLET=30_26_9_OBSERVED_ONLY
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
CONFIDENCE_LEVEL=low
```

## Next branch

next_branch=igmb_transition_model_record_pending_operator_review

The operator runs scripts/inspect-build42-igmb-transition-structure.ps1 against Project Russia
worldmap.xml.bin with default parameters (TransitionOffset=6389, MaxBytes=65536).
Results feed MAP-8W structural model record.
Binary writer gate remains CLOSED.

## MAP-8X reference

MAP-8X (real_transition_structure_result) recorded the actual run result.
Doc: docs/MAP_8X_REAL_TRANSITION_STRUCTURE_RESULT.md
Classification: MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED
next_branch=igmb_transition_model_hypothesis_review_pending_operator_approval
