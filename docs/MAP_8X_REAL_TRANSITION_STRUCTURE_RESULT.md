# MAP-8X Real Transition Structure Result

```text
Status: MAP-8X real transition structure result recorded
Classification: MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Source basis

MAP-8W defined the bounded IGMB transition structure inspector.
The operator ran scripts/inspect-build42-igmb-transition-structure.ps1 against the actual
Project Russia reference worldmap.xml.bin (283881 bytes, first 65536 bytes read).

Reference path (operator-local, not copied into repo):
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia\worldmap.xml.bin

## Actual MAP-8W real run result

- schema=pzmapforge.map8w-igmb-transition-structure-inspection.v0.1
- reference_present=true, reference_size_bytes=283881
- bytes_read_count=65536, max_bytes_allowed=65536, full_file_read=false
- magic=IGMB, version_le_u32=2
- transition_offset=6389
- transition_offset_is_4_byte_aligned=false
- transition_offset_is_2_byte_aligned=false
- transition_offset_in_range=true
- transition_window_before_hex: 64 bytes of FF (confirmed)
- transition_window_after_hex starts:
  1e 00 00 00 1a 00 00 00 09 00 00 00 00 00 02 14
  00 ff 00 f4 00 ff 00 27 01 fb 00 2d 01 fb 00 b8
  01 02 01 c6 01 0b 01 ce 01 1f 01 e2 01 33 01 f6
  01 3d 01 01 02 4d 01 09 02 a4 01 09 02 a4 01 13

## Critical findings (observed evidence only)

### Candidate header U32 triplet

| Field | Value | Interpretation |
|-------|-------|----------------|
| first (offset 6389) | 30 | observed_only_unconfirmed |
| second (offset 6393) | 26 | observed_only_unconfirmed |
| third (offset 6397) | 9 | observed_only_unconfirmed |

candidate_header_triplet_confidence=low.
These values may be counts, dimensions, table lengths, or other structure fields.
No hypothesis is confirmed.

### Post-triplet payload pattern

The 12 bytes following the triplet header (bytes 13+ from transition) plausibly
resemble packed U16LE or I16LE coordinate/delta pairs. This is observed evidence only.
No coordinate system is identified. No field mapping is confirmed.

### Structural pattern observations

candidate_small_value_clusters:
- start_offset=6389, entry_count=7

candidate_ff_or_null_sentinels_after_transition:
- offset=6391, relative=2, value=0
- offset=6395, relative=6, value=0
- offset=6399, relative=10, value=0
- offset=6401, relative=12, value=0

candidate_monotonic_sequences:
- start_offset=6401, length=3, direction=increasing
- start_offset=6435, length=3, direction=decreasing
- start_offset=6469, length=3, direction=increasing
- start_offset=6479, length=3, direction=increasing

printable_ascii_runs_near_transition=[]

entropy_estimate_transition_window=4.0713

### Structure hypotheses (observed-only)

All hypotheses are recorded as observed patterns only. None are confirmed.

1. hypothesis_a_count_table_header: first_3_u32le_may_be_counts_or_dimensions_observed_only
2. hypothesis_b_u16le_pairs: bytes_after_first_12_may_be_packed_u16le_coordinate_or_delta_pairs
3. hypothesis_c_variable_length_section: transition_may_mark_start_of_variable_length_geometry_payload
4. hypothesis_d_big_endian_possibility: if_java_big_endian_u16_pairs_after_byte_12_have_different_interpretation
5. hypothesis_e_coordinate_sequence: monotonic_u16le_sequences_suggest_possible_coordinate_run
6. hypothesis_f_run_length_or_repetition: repeated_u16le_values_suggest_rle_or_repeated_structure

strongest_current_hypothesis:
transition_immediately_follows_ff_padding_first_12_bytes_are_3_u32le_fields_bytes_after_resemble_packed_u16le_pairs_no_hypothesis_confirmed

interpretation=transition_structure_window_analyzed_observe_only
confidence_level=low

## What this is NOT

- This does NOT prove the cell index is understood.
- This does NOT prove the geometry payload is understood.
- This does NOT prove the full IGMB format is understood.
- The values 30, 26, 9 are not confirmed as any specific field type.
- The packed U16LE pattern is not confirmed as coordinates or any other field type.
- The monotonic sequences are not confirmed as coordinate runs.
- No encoder or writer may be designed from this evidence.
- No worldmap.xml.bin may be written based on this evidence.

## Safety

```text
MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED
OPERATOR_RAN_MAP8W_INSPECTOR=true
REFERENCE_SIZE_BYTES=283881
BYTES_READ_COUNT=65536
MAX_BYTES_ALLOWED=65536
FULL_FILE_READ=false
TRANSITION_OFFSET=6389
TRANSITION_OFFSET_IN_RANGE=true
TRANSITION_WINDOW_BEFORE_ALL_FF=true
CANDIDATE_HEADER_U32_TRIPLET=30_26_9_OBSERVED_ONLY
CANDIDATE_HEADER_TRIPLET_CONFIDENCE=low
POST_TRIPLET_PAYLOAD_RESEMBLES_PACKED_U16LE_PAIRS=true
PRINTABLE_ASCII_RUNS_NEAR_TRANSITION_COUNT=0
ENTROPY_ESTIMATE_TRANSITION_WINDOW=4.0713
SMALL_VALUE_CLUSTER_OBSERVED=true
FF_OR_NULL_SENTINELS_OBSERVED=true
MONOTONIC_SEQUENCES_OBSERVED=true
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
CONFIDENCE_LEVEL=low
```

## Next branch

next_branch=igmb_transition_model_hypothesis_review_pending_operator_approval

The binary writer gate was opened for an experimental local-only skeleton in MAP-8Y
based on the evidence recorded here.

## MAP-8Y reference

Script: `scripts/write-build42-experimental-igmb-worldmap.ps1`
Doc: `docs/MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON.md`
next_branch=map8z_controlled_install_packet_pending_operator_approval

No playable claim is allowed. Load test has not been performed.
