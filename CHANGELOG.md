# Changelog

All notable changes to PZMapForge will be documented here.

Format: Keep a Changelog.

---

## [Unreleased]

### Added (MAP-8Y: Experimental IGMB writer skeleton)
- docs/MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON.md: MAP-8Y writer doctrine.
  - Classification: MAP8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_ADDED.
  - Operator approved experimental local-only skeleton from MAP-8Q through MAP-8X evidence.
  - Binary layout: IGMB magic + version 2 + header fields + 12-string pool + FF padding +
    U32LE triplet 30/26/9 + synthetic U16LE payload + FF pad to 65536 bytes.
  - EXPERIMENTAL_WRITER_LOCAL_ONLY; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - next_branch=map8z_controlled_install_packet_pending_operator_approval.
- scripts/write-build42-experimental-igmb-worldmap.ps1:
  - .local/ guard and 7 forbidden path guards.
  - -TotalBytes range 8192-65536 (default 65536).
  - Deterministic binary writer producing 65536-byte IGMB candidate from scratch.
  - Manifest: schema pzmapforge.map8y-experimental-igmb-writer-manifest.v0.1, SHA-256,
    all safety gates.
- scripts/test-build42-experimental-igmb-worldmap-writer.ps1: 30 assertions.
- scripts/prepare-build42-map8y-experimental-igmb-writer-packet.ps1:
  - .local/ guard on -Output.
  - Writes map8y-experimental-igmb-writer-packet.json
    (schema pzmapforge.map8y-experimental-igmb-writer-packet.v0.1),
    map8y-experimental-igmb-writer-packet.md,
    MAP_8Y_EXPERIMENTAL_IGMB_WRITER_SKELETON_PACKET.md.
- scripts/test-build42-map8y-experimental-igmb-writer-packet.ps1: 20 assertions.
- Proof packet schema: v0.73 -> v0.74.
- psTotal: 1721 -> 1773.

### Added (MAP-8X: Record real MAP-8W transition structure inspection result)
- docs/MAP_8X_REAL_TRANSITION_STRUCTURE_RESULT.md: MAP-8X result doctrine.
  - Classification: MAP8X_REAL_TRANSITION_STRUCTURE_RESULT_RECORDED.
  - Operator ran MAP-8W inspector against Project Russia worldmap.xml.bin (283881 bytes,
    first 65536 bytes read).
  - transition_offset=6389, transition_offset_in_range=true, transition_window_before_all_ff=true.
  - Candidate header U32 triplet: first=30, second=26, third=9 (observed_only_unconfirmed).
  - candidate_header_triplet_confidence=low.
  - post_triplet_payload_resembles_packed_u16le_pairs=true (observed only, not confirmed).
  - entropy_estimate_transition_window=4.0713. printable_ascii_runs=0.
  - Small-value clusters, FF/null sentinels, and monotonic sequences observed.
  - transition_structure_understood=false; full_format_understood=false.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - next_branch=igmb_transition_model_hypothesis_review_pending_operator_approval.
- scripts/prepare-build42-map8x-real-transition-structure-result-packet.ps1:
  - .local/ guard on -Output.
  - Hardcoded real run values from MAP-8W inspector run.
  - Writes map8x-real-transition-structure-result.json (schema pzmapforge.map8x-result.v0.1),
    map8x-real-transition-structure-result.md, MAP_8X_REAL_TRANSITION_STRUCTURE_RESULT_PACKET.md.
- scripts/test-build42-map8x-real-transition-structure-result.ps1: 20 assertions.
- Proof packet schema: v0.72 -> v0.73.
- psTotal: 1700 -> 1721.

### Added (MAP-8W: Bounded IGMB transition structure analysis around offset 6389)
- docs/MAP_8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS.md: MAP-8W analysis doctrine.
  - Classification: MAP8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_APPROVED.
  - Operator approved bounded structure analysis at transition_offset=6389, max 65536 bytes.
  - Inspector reads at most min(file_size, MaxBytes, 65536) bytes via FileStream read-only.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - next_branch=igmb_transition_model_record_pending_operator_review.
- scripts/inspect-build42-igmb-transition-structure.ps1: IGMB transition structure inspector.
  - Schema: pzmapforge.map8w-igmb-transition-structure-inspection.v0.1.
  - Params: -ReferenceWorldmapBinPath, -Output (.local/ guard), -TransitionOffset (default 6389),
    -MaxBytes (hard cap 65536), -WindowBeforeBytes (hard cap 256), -WindowAfterBytes (hard cap 2048).
  - Exact U32LE/U16LE/I16LE/byte values from transition (first 128 bytes).
  - candidate_header_u32_triplet: first=30, second=26, third=9 (observed_only_unconfirmed).
  - candidate_header_triplet_confidence=low.
  - Candidate count, offset, coordinate, signed-coordinate, run-length, repeated-pair,
    small-cluster, FF/null sentinel, and monotonic sequence fields.
  - Shannon entropy estimate over first 128 bytes from transition.
  - structure_hypotheses_observed_only (min 4 entries).
  - transition_structure_understood=false; full_format_understood=false.
  - All result arrays use [System.Collections.ArrayList]::new().
- scripts/test-build42-igmb-transition-structure.ps1: 24 assertions.
  - Synthetic 300-byte file with triplet at offset 42.
  - 42 % 4 == 2 → not 4-byte aligned; 42 % 2 == 0 → 2-byte aligned.
  - full_file_read=true (300 bytes, all read within cap).
- scripts/prepare-build42-map8w-transition-structure-result-packet.ps1:
  - .local/ guard on -Output.
  - Writes map8w-transition-structure-result.json (schema pzmapforge.map8w-result.v0.1),
    map8w-transition-structure-result.md, MAP_8W_IGMB_TRANSITION_STRUCTURE_ANALYSIS_PACKET.md.
- scripts/test-build42-map8w-transition-structure-result.ps1: 20 assertions.
- Proof packet schema: v0.71 -> v0.72.
- psTotal: 1654 -> 1700.

### Added (MAP-8V: Record real first non-FF transition result and add exact-offset decoding)
- docs/MAP_8V_REAL_FIRST_NON_FF_TRANSITION_RESULT.md: MAP-8V result doctrine.
  - Classification: MAP8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_RECORDED.
  - Operator ran MAP-8U scanner against Project Russia worldmap.xml.bin (283881 bytes,
    first 65536 bytes read).
  - first_non_ff_offset=6389, ff_run_length=6256. Offset NOT 4-byte or 2-byte aligned.
  - Exact U32LE at transition (unaligned, observed-only): 30, 26, 9.
  - transition_structure_understood=false; full_format_understood=false.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - next_branch=igmb_transition_structure_analysis_pending_operator_approval.
- scripts/inspect-build42-igmb-first-non-ff-transition.ps1: hardened with exact-offset decoding.
  - New fields: exact_offset_u32le_values_from_transition, exact_offset_u16le_values_from_transition,
    exact_offset_hex_first_32_bytes, exact_offset_hex_first_64_bytes,
    exact_offset_small_u32_candidates, transition_exact_offset_decoding_added=true,
    aligned_u32le_values_are_context_only=true, transition_structure_understood=false.
  - Exact reads start at first_non_ff_offset (not aligned to 4-byte boundary).
- scripts/test-build42-igmb-first-non-ff-transition.ps1: 23 -> 27 assertions.
  - Tests 24-27: transition_exact_offset_decoding_added==true,
    aligned_u32le_values_are_context_only==true, transition_structure_understood==false,
    exact_offset_u32le_values_from_transition has entries.
- scripts/prepare-build42-map8v-real-first-non-ff-transition-result-packet.ps1:
  - .local/ guard on -Output.
  - Hardcoded real run values: first_non_ff_offset=6389, exact_u32le_at_transition_0=30,
    exact_u32le_at_transition_4=26, exact_u32le_at_transition_8=9.
  - Writes map8v-real-first-non-ff-transition-result.json (schema pzmapforge.map8v-result.v0.1),
    map8v-real-first-non-ff-transition-result.md, MAP_8V_REAL_FIRST_NON_FF_TRANSITION_RESULT_PACKET.md.
- scripts/test-build42-map8v-real-first-non-ff-transition-result.ps1: 20 assertions.
- Proof packet schema: v0.70 -> v0.71.
- psTotal: 1629 -> 1654.

### Added (MAP-8U: Bounded first non-FF transition scan after IGMB string pool)
- docs/MAP_8U_FIRST_NON_FF_TRANSITION_SCAN.md: MAP-8U first non-FF transition scan doctrine.
  - Classification: MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED.
  - Operator approved bounded first non-FF transition scan from offset 133, max 65536 bytes.
  - Source basis: MAP-8T found all bytes 133-4095 are 0xFF.
  - Scope: read-only, no file copy, no binary writer, no PZ run, no Workshop upload.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - CONFIDENCE_LEVEL=low; full_format_understood=false; cell_index_understood=false.
  - next_branch=igmb_transition_structure_analysis_pending_operator_approval_if_non_ff_found.
- scripts/inspect-build42-igmb-first-non-ff-transition.ps1:
  - .local/ guard on -Output.
  - Params: -ReferenceWorldmapBinPath, -Output, -StringPoolEndOffset (default 133),
    -MaxBytes (hard cap 65536), -WindowBytes (hard cap 256).
  - Reads at most min(file_size, MaxBytes, 65536) bytes via FileStream read-only.
  - Scans from StringPoolEndOffset for first non-0xFF byte.
  - Writes igmb-first-non-ff-transition-inspection.json
    (schema pzmapforge.map8u-igmb-first-non-ff-transition-inspection.v0.1) + .md.
  - If found: first_non_ff_offset, relative_offset, ff_run_length, alignment,
    hex windows, U32LE/U16LE around transition, ASCII runs, heuristic candidates.
  - If not found: first_non_ff_found=false, interpretation=ff_region_continues_beyond_bounded_scan.
  - All result arrays use [System.Collections.ArrayList]::new() (PS5.1 null-array fix).
  - max_bytes_allowed in JSON = 65536 (hard cap, regardless of -MaxBytes).
- scripts/test-build42-igmb-first-non-ff-transition.ps1: 23 assertions (see MAP-8V for update to 27).
  - Synthetic file A (300 bytes): StringPoolEndOffset=42, MaxBytes=150.
    59 FF bytes then 0x42 at offset 101.
    first_non_ff_offset=101, relative=59, ff_run_length=59, not 4-byte aligned.
  - Synthetic file B (200 bytes): StringPoolEndOffset=42, MaxBytes=70.
    All FF after offset 42. first_non_ff_found=false.
- scripts/prepare-build42-map8u-first-non-ff-transition-result-packet.ps1:
  - .local/ guard on -Output.
  - Records approval metadata only.
  - Writes map8u-first-non-ff-transition-result.json (schema pzmapforge.map8u-result.v0.1),
    map8u-first-non-ff-transition-result.md, MAP_8U_FIRST_NON_FF_TRANSITION_RESULT_PACKET.md.
- scripts/test-build42-map8u-first-non-ff-transition-result.ps1: 20 assertions.
- Proof packet schema: v0.69 -> v0.70.
- psTotal: 1584 -> 1629.

### Added (MAP-8T: Record real MAP-8S post-string-pool FF sentinel result)
- docs/MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT.md: MAP-8T result doctrine.
  - Classification: MAP8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_RECORDED.
  - Operator ran MAP-8S inspector against Project Russia worldmap.xml.bin (283881 bytes,
    first 4096 bytes read).
  - All bytes after string_pool_end_offset=133 within the 4096-byte window are 0xFF.
  - first_128_bytes_after_string_pool_all_ff=true; first_256_bytes_after_string_pool_all_ff=true.
  - No count field, offset table, or cell coordinate observed.
  - immediate_cell_index_after_string_pool_supported=false; first_non_ff_offset_known=false.
  - Interpretation: 0xFF padding or sentinel (NOT claiming full file is FF; only
    first 4096 bytes were read; file is 283881 bytes).
  - full_format_understood=false; cell_index_understood=false; geometry_payload_understood=false.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - next_branch=igmb_first_non_ff_transition_scan_pending_operator_approval.
- scripts/prepare-build42-map8t-real-cell-boundary-result-packet.ps1:
  - .local/ guard on -Output.
  - Hardcoded fields from real MAP-8S run: reference_size_bytes=283881,
    bytes_read_count=4096, max_bytes_allowed=4096, full_file_read=false,
    first_128_bytes_after_string_pool_all_ff=true, first_256_bytes_after_string_pool_all_ff=true,
    observed_u32le_values_after_string_pool_are_minus_one=true,
    immediate_cell_index_after_string_pool_supported=false,
    first_non_ff_offset_known=false, first_non_ff_offset=null,
    binary_writer_gate_closed=true, playable_claim_allowed=false.
  - Writes map8t-real-cell-boundary-result.json (schema pzmapforge.map8t-result.v0.1),
    map8t-real-cell-boundary-result.md, MAP_8T_REAL_CELL_BOUNDARY_FF_SENTINEL_RESULT_PACKET.md.
- scripts/test-build42-map8t-real-cell-boundary-result.ps1: 20 assertions.

### Added (MAP-8S: IGMB cell-index boundary research after string pool)
- docs/MAP_8S_IGMB_CELL_INDEX_BOUNDARY_RESEARCH.md: MAP-8S cell boundary research doctrine.
  - Classification: MAP8S_IGMB_CELL_BOUNDARY_RESEARCH_DEFINED.
  - Operator approved bounded inspection of bytes after string_pool_end_offset=133,
    within first 4096 bytes only.
  - Scope: read-only, no file copy, no binary writer, no PZ run, no Workshop upload.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - CONFIDENCE_LEVEL=low; cell_index_understood=false; full_format_understood=false.
  - next_branch=igmb_cell_index_model_research_pending_operator_approval_if_boundary_evidence_sufficient.
- scripts/inspect-build42-igmb-cell-boundary.ps1:
  - .local/ guard on -Output.
  - Params: -ReferenceWorldmapBinPath, -CandidateWorldmapBinPath (optional), -Output,
    -MaxBytes (default 4096, hard cap 4096), -StringPoolEndOffset (default 133).
  - Reads at most min(file_size, MaxBytes, 4096) bytes via FileStream read-only.
  - Writes igmb-cell-boundary-inspection.json
    (schema pzmapforge.map8s-igmb-cell-boundary-inspection.v0.1) + .md.
  - Fields: post-string-pool hex window (128+256 bytes), u32le_values_after_string_pool_first_128,
    u32le_aligned_boundary_hypothesis (from next 4-byte aligned offset after postStart),
    u16le_values_after_string_pool_first_128, float32le_values_after_string_pool_first_128,
    plausible_count_fields_after_string_pool (U32LE 1..65535),
    plausible_offset_table_candidates (U32LE 1..<file_size),
    plausible_cell_coordinate_candidates (U32LE 1..500 heuristic),
    zero_run_candidates (>= 4 consecutive zero bytes),
    repeated_pattern_candidates (consecutive identical U32LE pairs),
    section_boundary_hypotheses, confidence_level=low,
    binary_writer_gate_closed=true, playable_claim_allowed=false,
    third_party_files_copied=false.
  - All result arrays use [System.Collections.ArrayList]::new() (PS5.1 null-array fix).
- scripts/test-build42-igmb-cell-boundary.ps1: 20 assertions.
  - Synthetic 300-byte IGMB file: 2 LP strings (Polygon at 24, highway at 33),
    string pool ends at 42; U32LE=3 at 42, U32LE=200 at 46, 8 zero bytes at 50-57,
    U32LE=5 at 58, fill 0x41 at 62-299.
  - Test 1: .local guard exits nonzero.
  - Test 2: exits 0 with valid .local path.
  - Tests 3-4: 2 output files exist.
  - Test 5: schema correct.
  - Test 6: reference_present == true.
  - Test 7: bytes_read_count > 0.
  - Test 8: full_file_read == false.
  - Test 9: max_bytes_allowed == 4096.
  - Test 10: string_pool_end_offset == 42.
  - Test 11: post_string_pool_window_start == 42.
  - Test 12: post_string_pool_window_bytes_available > 0.
  - Test 13: u32le_values_after_string_pool_first_128.Count > 0.
  - Test 14: u32le_aligned_boundary_hypothesis.Count > 0 (nextAligned=44 != 42).
  - Test 15: u16le_values_after_string_pool_first_128.Count > 0.
  - Test 16: float32le_values_after_string_pool_first_128.Count > 0.
  - Test 17: zero_run_candidates.Count > 0.
  - Test 18: confidence_level == low.
  - Test 19: binary_writer_gate_closed == true.
  - Test 20: playable_claim_allowed == false.
- scripts/prepare-build42-map8s-cell-boundary-result-packet.ps1:
  - .local/ guard on -Output.
  - Records approval metadata only (operator has not yet run inspector against real file).
  - Writes map8s-cell-boundary-result.json (schema pzmapforge.map8s-result.v0.1)
    + .md + MAP_8S_CELL_BOUNDARY_RESULT_PACKET.md.
  - Fields: operator_approved_cell_index_boundary_research=true,
    string_pool_end_offset=133, max_bytes_allowed=4096, full_file_read=false,
    binary_contents_read_scope=first_4096_bytes_only,
    focus_region=after_string_pool_offset_133,
    full_format_understood=false, cell_index_understood=false,
    geometry_payload_understood=false, writer_implementation_allowed=false,
    binary_writer_gate_closed=true, playable_claim_allowed=false,
    third_party_files_copied=false, no_pz_run_by_claude=true,
    no_workshop_upload_by_claude=true,
    next_branch=igmb_cell_index_model_research_pending_operator_approval_if_boundary_evidence_sufficient.
- scripts/test-build42-map8s-cell-boundary-result.ps1: 20 assertions.
  - Tests 1-2: .local guard / exits 0.
  - Tests 3-5: 3 output files exist.
  - Test 6: schema correct.
  - Tests 7-16: approval metadata and safety flags.
  - Test 17: next_branch correct.
  - Tests 18-20: packet doc sentinels (MAP8S_CELL_BOUNDARY_RESEARCH_DEFINED /
    BINARY_WRITER_GATE_STILL_CLOSED / PUBLIC_PLAYABLE_CLAIM_ALLOWED=false).

### Updated (MAP-8S)
- docs/MAP_8R_REAL_IGMB_STRUCTURE_RESULT.md: next_branch note updated to reference MAP-8S.
- docs/IMPLEMENTATION.md: MAP-8S row added.
- docs/MAP_EXPORT_CONTRACT.md: MAP-8S boundary updated.
- scripts/validate.ps1: MAP-8S section added; psTotal 1520 -> 1562.
- scripts/write-proof-packet.ps1: schema v0.67 -> v0.68; MAP-8S entries added;
  proof_packet 123 -> 125; total_expected_assertions 1520 -> 1562.
- scripts/test-proof-packet.ps1: schema check updated; MAP-8S assertions added;
  total_expected_assertions 1520 -> 1562.

### Added (MAP-8R: Record real IGMB structure result and identify probable string pool header)
- docs/MAP_8R_REAL_IGMB_STRUCTURE_RESULT.md: MAP-8R real IGMB structure result doctrine.
  - Classification: MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED.
  - Operator ran MAP-8Q inspector against actual Project Russia worldmap.xml.bin.
  - Observed 12 U16LE LP strings (Polygon/highway/primary/trail/natural/forest/water/river/
    tertiary/building/Residential/secondary).
  - Offset-20 U32LE value = 12 matches detected LP string count exactly.
  - string_pool_count_matches_header_offset_20=true.
  - Probable partial IGMB header: magic(4)+version u32le(4)+unknown_a(4)+unknown_b(4)+
    unknown_c(4)+string_pool_count(4)+string_pool at offset 24.
  - string_pool_end_offset_candidate=133.
  - partial_header_model_confidence=medium.
  - full_format_understood=false; geometry_payload_understood=false.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - next_branch=igmb_cell_index_boundary_research_pending_operator_approval.
- scripts/prepare-build42-map8r-real-igmb-structure-result-packet.ps1:
  - .local/ guard on -Output.
  - Writes map8r-real-igmb-structure-result.json (schema pzmapforge.map8r-result.v0.1)
    + .md + MAP_8R_REAL_IGMB_STRUCTURE_RESULT_PACKET.md.
  - Fields: reference_size_bytes=283881, bytes_read_count=4096, full_file_read=false,
    magic=IGMB, version_le_u32=2, header_unknown_a_offset_8_u32le=256,
    header_unknown_b_offset_12_u32le=59, header_unknown_c_offset_16_u32le=68,
    header_probable_string_pool_count_offset_20_u32le=12,
    string_pool_start_offset_candidate=24, string_pool_detected_count=12,
    string_pool_count_matches_header_offset_20=true,
    string_pool_values=[12 OSM/PZ feature type strings],
    string_pool_end_offset_candidate=133,
    partial_header_model_confidence=medium, full_format_understood=false,
    geometry_payload_understood=false, writer_implementation_allowed=false,
    binary_writer_gate_closed=true, playable_claim_allowed=false,
    third_party_files_copied=false,
    next_branch=igmb_cell_index_boundary_research_pending_operator_approval.
- scripts/test-build42-map8r-real-igmb-structure-result.ps1: 20 assertions.
  - Test 1: .local guard exits nonzero.
  - Test 2: exits 0 with valid .local path.
  - Tests 3-5: 3 output files exist.
  - Test 6: schema correct.
  - Test 7: magic == IGMB.
  - Test 8: version_le_u32 == 2.
  - Test 9: string_pool_detected_count == 12.
  - Test 10: string_pool_count_matches_header_offset_20 == true.
  - Test 11: string_pool_values contains Polygon.
  - Test 12: string_pool_values contains secondary.
  - Test 13: header_probable_string_pool_count_offset_20_u32le == 12.
  - Test 14: string_pool_end_offset_candidate == 133.
  - Test 15: partial_header_model_confidence == medium.
  - Test 16: full_format_understood == false.
  - Tests 17-19: gate flags.
  - Test 20: next_branch correct.
  - Sentinel checks: MAP8R_REAL_IGMB_STRUCTURE_RESULT_RECORDED /
    BINARY_WRITER_GATE_STILL_CLOSED / PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.

### Updated (MAP-8R: inspector improvements and MAP-8Q inspector tests)
- scripts/inspect-build42-igmb-structure.ps1:
  - Added explicit named header fields: header_magic_text=IGMB,
    header_version_le_u32, header_unknown_a_offset_8_u32le,
    header_unknown_b_offset_12_u32le, header_unknown_c_offset_16_u32le,
    header_probable_string_pool_count_offset_20_u32le.
  - Added string_pool_start_offset_candidate=24.
  - Added string_pool_detected_count (count of LP string candidates found).
  - Added string_pool_count_matches_header_offset_20 (true when detected count
    equals offset-20 value).
  - Added string_pool_values (array of LP string values).
  - Added string_pool_end_offset_candidate (last LP string offset + 2 + len).
  - Added probable_partial_header_model (list of annotated header field strings).
  - Preserved all existing output fields unchanged.
- scripts/test-build42-igmb-structure.ps1: 20 -> 24 assertions.
  - Synthetic file byte 20-23 changed from 0x0C (12) to 0x02 (2) so that
    string_pool_count_matches_header_offset_20 is true (2 LP strings, header=2).
  - Test 21: header_magic_text == IGMB.
  - Test 22: header_probable_string_pool_count_offset_20_u32le == 2.
  - Test 23: string_pool_count_matches_header_offset_20 == true.
  - Test 24: string_pool_values not null.
- docs/MAP_8Q_IGMB_STRUCTURE_RESEARCH.md: MAP-8R result reference added.
- psTotal 1494 -> 1520; proof-packet v0.66 -> v0.67.

### Added (MAP-8Q: IGMB structure research with bounded 4096-byte inspection)
- docs/MAP_8Q_IGMB_STRUCTURE_RESEARCH.md: MAP-8Q IGMB structure research doctrine.
  - Classification: MAP8Q_IGMB_STRUCTURE_RESEARCH_DEFINED.
  - Operator approved bounded IGMB structure research: max 4096 bytes, read-only, no copying,
    no full binary read, no binary writer.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - next_branch=igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient.
- scripts/inspect-build42-igmb-structure.ps1:
  - .local/ guard on -Output.
  - Params: -ReferenceWorldmapBinPath, -CandidateWorldmapBinPath (optional), -Output,
    -MaxBytes (default 4096, hard cap 4096).
  - Reads at most min(file_size, MaxBytes, 4096) bytes via FileStream read-only.
  - Missing candidate handled gracefully as present=false.
  - Writes igmb-structure-inspection.json (schema
    pzmapforge.map8q-igmb-structure-inspection.v0.1) + .md.
  - Fields: reference_present, reference_size_bytes, bytes_read_count,
    max_bytes_allowed=4096, full_file_read=false, magic=IGMB, version_le_u32,
    candidate_u32_values_first_64_le, candidate_u16_values_first_64_le,
    printable_ascii_runs_min_length_3, possible_length_prefixed_strings,
    possible_string_pool_offset_candidates, possible_string_pool_count_candidates,
    possible_header_fields_observed_only, unverified_format_hypotheses,
    confidence_level=low_to_medium, binary_writer_gate_closed=true,
    playable_claim_allowed=false, third_party_files_copied=false, next_branch.
  - U32LE and U16LE parsing of first 64 bytes.
  - Printable ASCII run detection (length >= 3) across full read window.
  - U16LE length-prefixed string scan: [len_lo len_hi] + len printable ASCII chars.
- scripts/test-build42-igmb-structure.ps1: 20 assertions.
  - Test 1: .local guard exits nonzero.
  - Test 2: exits 0 with synthetic IGMB file.
  - Tests 3-4: output files exist.
  - Test 5: schema correct.
  - Test 6: reference_present == true.
  - Test 7: bytes_read_count > 0.
  - Test 8: max_bytes_allowed == 4096.
  - Test 9: full_file_read == false.
  - Test 10: version_le_u32 == 2 (from synthetic file).
  - Tests 11-12: u32/u16 value arrays not null.
  - Test 13: possible_string_pool_count_candidates >= 2 (Polygon + highway).
  - Test 14: printable_ascii_runs_min_length_3 not null.
  - Tests 15-17: gate flags.
  - Test 18: confidence_level == low_to_medium.
  - Test 19: next_branch correct.
  - Test 20: LP strings contain entry with value 'Polygon'.
- scripts/prepare-build42-map8q-igmb-structure-result-packet.ps1:
  - .local/ guard on -Output.
  - Writes map8q-igmb-structure-result.json (schema pzmapforge.map8q-result.v0.1)
    + .md + MAP_8Q_IGMB_STRUCTURE_RESEARCH_PACKET.md.
  - Fields: operator_approved_igmb_structure_research=true, max_bytes_allowed=4096,
    binary_contents_read_scope=first_4096_bytes_only, binary_contents_full_read=false,
    third_party_files_copied=false, playable_claim_allowed=false,
    binary_writer_gate_closed=true,
    next_branch=igmb_minimal_encoder_design_pending_operator_approval_if_structure_sufficient.
- scripts/test-build42-map8q-igmb-structure-result.ps1: 20 assertions.
  - Test 1: .local guard exits nonzero.
  - Test 2: exits 0 with valid path.
  - Tests 3-5: output files exist.
  - Test 6: schema == pzmapforge.map8q-result.v0.1.
  - Test 7: operator_approved_igmb_structure_research == true.
  - Test 8: max_bytes_allowed == 4096.
  - Test 9: binary_contents_read_scope == first_4096_bytes_only.
  - Tests 10-13: gate flags.
  - Test 14: next_branch correct.
  - Tests 15-19: packet doc sentinels.
  - Test 20: md contains expected header.
- Updated scripts/validate.ps1: MAP-8Q section before MAP-8P; psTotal 1454->1494;
  proof-packet v0.65->v0.66.
- Updated scripts/write-proof-packet.ps1: map8q fields (x2); total 1454->1494;
  schema v0.65->v0.66.
- Updated scripts/test-proof-packet.ps1: map8q assertions (x2); total 1454->1494;
  schema v0.65->v0.66.

### Added (MAP-8P: Record IGMB worldmap bin header result and update signature detection)
- docs/MAP_8P_IGMB_WORLDMAP_BIN_HEADER_RESULT.md: MAP-8P IGMB header result doctrine.
  - Classification: MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED.
  - Operator ran MAP-8O inspector against Project Russia reference worldmap.xml.bin.
  - Reference detected_signature=igmb (magic bytes 49 47 4D 42 = "IGMB").
  - reference_first_16_bytes_hex: 49 47 4D 42 02 00 00 00 00 01 00 00 3B 00 00 00.
  - reference_first_64_bytes_hex contains visible tokens: Polygon, highway, primary, trail, natu.
  - igmb_magic_detected=true; appears_compressed=false; likely_little_endian_fields=true.
  - possible_version_value=2 (bytes 4-7 = 02 00 00 00 as little-endian U32).
  - possible_string_length_prefix_width=16-bit (07 00 before "Polygon" = U16LE 7).
  - big_endian_claim_contradicted_by_observed_header=true (community note was unverified).
  - community_layout_notes_recorded_as_unverified=true.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - next_branch=igmb_structure_research_pending_operator_approval.
- Updated scripts/inspect-build42-worldmap-bin-header.ps1:
  - Added IGMB signature detection: bytes 0-3 == 49 47 4D 42 -> detected_signature='igmb'.
  - IGMB check inserted before sqlite check in signature detection block.
- Updated scripts/test-build42-worldmap-bin-header.ps1:
  - Tests 21-22 added: IGMB dummy reference exits 0; detected_signature == 'igmb'.
  - Total assertions: 20 -> 22.
- scripts/prepare-build42-map8p-igmb-header-result-packet.ps1:
  - .local/ guard on -Output.
  - Writes map8p-igmb-header-result.json (schema pzmapforge.map8p-result.v0.1) + .md + packet doc.
  - Records all observed fields from MAP-8O inspector run (hardcoded observed values).
  - Fields: candidate_present=false, reference_present=true, reference_size_bytes=283881,
    reference_bytes_read_count=64, reference_first_16_bytes_hex, reference_first_64_bytes_hex,
    reference_ascii_preview, reference_detected_signature=igmb, igmb_magic_detected=true,
    appears_compressed=false, appears_custom_binary_worldmap_format=true,
    likely_little_endian_fields=true, possible_version_value=2,
    possible_length_prefixed_strings=true, possible_string_length_prefix_width=16-bit,
    visible_string_tokens=Polygon/highway/primary/trail/natu_prefix,
    community_layout_notes_recorded_as_unverified=true,
    big_endian_claim_contradicted_by_observed_header=true,
    max_bytes_allowed=64, binary_contents_read_scope=first_64_bytes_only,
    binary_contents_full_read=false, third_party_files_copied=false,
    playable_claim_allowed=false, binary_writer_gate_closed=true,
    next_branch=igmb_structure_research_pending_operator_approval.
- scripts/test-build42-map8p-igmb-header-result.ps1: 20 assertions.
  - Test 1: .local guard exits nonzero.
  - Test 2: exits 0 with valid path.
  - Tests 3-5: output files exist (json + md + packet doc).
  - Test 6: schema == pzmapforge.map8p-result.v0.1.
  - Test 7: igmb_magic_detected == true.
  - Test 8: reference_detected_signature == igmb.
  - Test 9: appears_compressed == false.
  - Test 10: appears_custom_binary_worldmap_format == true.
  - Test 11: likely_little_endian_fields == true.
  - Test 12: big_endian_claim_contradicted_by_observed_header == true.
  - Test 13: community_layout_notes_recorded_as_unverified == true.
  - Test 14: max_bytes_allowed == 64.
  - Test 15: binary_contents_full_read == false.
  - Test 16: third_party_files_copied == false.
  - Test 17: playable_claim_allowed == false.
  - Test 18: binary_writer_gate_closed == true.
  - Test 19: next_branch == igmb_structure_research_pending_operator_approval.
  - Test 20: packet doc contains MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED.
- Updated scripts/validate.ps1: MAP-8P section before MAP-8O; map8o inspector 20->22;
  psTotal 1432->1454; proof-packet v0.64->v0.65.
- Updated scripts/write-proof-packet.ps1: map8p=20; map8o inspector 20->22;
  total 1432->1454; schema v0.64->v0.65.
- Updated scripts/test-proof-packet.ps1: map8p assertion; map8o inspector 20->22;
  total 1432->1454; schema v0.64->v0.65.

### Added (MAP-8O: Header-only worldmap.xml.bin format inspection)
- docs/MAP_8O_WORLDMAP_BIN_HEADER_INSPECTION.md: MAP-8O header inspection doctrine.
  - Classification: MAP8O_WORLDMAP_XML_BIN_HEADER_INSPECTION_DEFINED.
  - Operator approved Step 2 of MAP-8M investigation plan: header-only inspection,
    max 64 bytes per file, read-only, no copying, no binary writer.
  - worldmap.xml.bin remains leading discriminator (hypothesis, not proven fact).
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-worldmap-bin-header.ps1:
  - .local/ guard on -Output.
  - Params: -CandidateWorldmapBinPath, -ReferenceWorldmapBinPath, -Output.
  - Reads at most first 64 bytes from each existing .bin file via FileStream read-only.
  - Missing candidate handled gracefully as present=false.
  - Writes worldmap-bin-header-inspection.json (schema
    pzmapforge.map8o-worldmap-bin-header-inspection.v0.1) + .md.
  - Fields: candidate/reference present, size_bytes, bytes_read_count,
    first_16_bytes_hex, first_64_bytes_hex, ascii_preview, detected_signature,
    max_bytes_allowed=64, binary_contents_read_scope=first_64_bytes_only,
    binary_contents_full_read=false, third_party_files_copied=false,
    playable_claim_allowed=false, binary_writer_gate_closed=true, next_branch.
  - Signature detection: gzip (1F 8B), zlib (78 01/5E/9C/DA), zip (50 4B),
    sqlite (53 51 4C 69), xml_or_text (leading 3C), unknown otherwise.
- scripts/test-build42-worldmap-bin-header.ps1: 20 assertions.
  - Test 1: .local guard exits nonzero.
  - Test 2: exits 0 with valid path, absent candidate.
  - Tests 3-4: output files exist.
  - Test 5: schema correct.
  - Tests 6-7: candidate absent, bytes_read_count=0.
  - Tests 8-9: reference present, size_bytes=128.
  - Test 10: reference_bytes_read_count=64 (not full 128 bytes -- 64-byte cap verified).
  - Tests 11-14: first_16/64_bytes_hex, gzip signature detected, ascii_preview.
  - Tests 15-20: max_bytes_allowed=64, read_scope, full_read=false, gate flags.
- scripts/prepare-build42-map8o-header-result-packet.ps1:
  - .local/ guard on -Output.
  - Writes map8o-header-result.json (schema pzmapforge.map8o-result.v0.1) + MD + packet doc.
  - Records operator_approved_header_only_inspection=true, max_bytes_allowed=64,
    binary_contents_read_scope=first_64_bytes_only, binary_contents_full_read=false,
    no_project_russia_files_copied=true, playable_claim_allowed=false,
    binary_writer_gate_closed=true, worldmap_xml_bin_primary_discriminator=true,
    next_branch=run_header_inspector_then_evaluate_signature.
- scripts/test-build42-map8o-header-result.ps1: 20 assertions.
  - Test 1: .local guard exits nonzero.
  - Tests 3-5: output files exist.
  - Tests 6-16: JSON field values.
  - Tests 17-20: packet doc content (MAP8O label, gate sentinels, operator_approved).
- Updated scripts/validate.ps1: MAP-8O section before MAP-8N; psTotal 1392->1432;
  proof-packet v0.63->v0.64.
- Updated scripts/write-proof-packet.ps1: map8o fields (x2); total 1392->1432;
  schema v0.63->v0.64.
- Updated scripts/test-proof-packet.ps1: map8o assertions (x2); total 1392->1432;
  schema v0.63->v0.64.

### Added (MAP-8N: Record worldmap.xml.bin presence discriminator result and fix lotpack count)
- docs/MAP_8N_WORLDMAP_BIN_PRESENCE_RESULT.md: MAP-8N presence result doctrine.
  - Classification: MAP8N_WORLDMAP_XML_BIN_PRESENCE_DISCRIMINATOR_CONFIRMED.
  - Operator ran scripts/inspect-build42-worldmap-bin-presence.ps1 against:
    - Candidate: PZMapForge parent (Workshop 3740642200).
    - Reference: Project Russia parent (Workshop 3734334068).
  - Key result: candidate_worldmap_xml_bin_present=false; reference=true (283881 bytes).
  - streets.xml.bin absent in BOTH candidate and reference parent (not the blocker).
  - worldmap.xml text already proved not sufficient (MAP-8L).
  - worldmap_xml_bin_primary_discriminator=true (leading hypothesis, not proven fact).
  - Bug found and fixed: inspector used *.pack pattern for lotpack count (should be *.lotpack).
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map8n-presence-result-packet.ps1:
  - .local/ guard on -Output.
  - Writes map8n-result.json (schema pzmapforge.map8n-result.v0.1) + MD + packet doc.
  - Records all result fields including size bytes, blocker flags, discriminator, gate status.
- scripts/test-build42-map8n-presence-result.ps1: 20 assertions.
  - Test 1: .local guard exits nonzero.
  - Tests 3-5: output files exist.
  - Tests 6-20: JSON field values including bin presence, size, blocker flags, discriminator,
    lotpack fix, binary guards, next branch.
- Fixed scripts/inspect-build42-worldmap-bin-presence.ps1:
  - Changed lotpack_count pattern from *.pack to *.lotpack.
  - PZMapForge candidate has world_35_27.lotpack; old pattern missed it.
- Updated scripts/test-build42-worldmap-bin-presence.ps1: 15 -> 16 assertions.
  - Added dummy world_35_27.lotpack to temp candidate dir.
  - Test 16: candidate.lotpack_count == 1 (*.lotpack pattern fix verified).
- Updated scripts/validate.ps1: MAP-8N section before MAP-8M; psTotal 1371->1392;
  proof-packet v0.62->v0.63.
- Updated scripts/write-proof-packet.ps1: map8n field; map8m count 15->16; total 1371->1392;
  schema v0.62->v0.63.
- Updated scripts/test-proof-packet.ps1: map8n assertion; map8m count 15->16; total 1371->1392;
  schema v0.62->v0.63.

### Added (MAP-8M: Record MAP-8L runtime result and prepare worldmap.xml.bin investigation)
- docs/MAP_8L_RUNTIME_RESULT.md: MAP-8L runtime result doctrine.
  - Classification: MAP8L_WORLDMAP_XML_FAILED_TO_MOUNT.
  - MAP-8L deployed substantial PZMapForge-owned worldmap.xml (1915 bytes, 44 lines).
  - Player connected at 10746,8288,0 (matches worldX=35, worldY=27 coordinate proof).
  - IsoMetaGrid map folder list still empty. Parent folder not listed.
  - WorldMapDataAssetManager failed to load both child and parent worldmap.xml.
  - worldmap_xml_bin_present=false; lotheader_parse_attempt_logged=false.
  - next_branch=worldmap_xml_bin_binary_format_investigation.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- docs/MAP_8M_WORLDMAP_BIN_INVESTIGATION_PLAN.md: MAP-8M investigation plan.
  - MAP8M_WORLDMAP_BIN_INVESTIGATION_PLAN_DEFINED.
  - Staged plan: Step 1 presence/shape inventory (allowed now); Step 2 format
    research gate (requires operator approval); Step 3 binary writer gate.
  - Binary writer gate opens only when IsoMetaGrid logs parse attempt against PZMapForge.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map8l-runtime-result-packet.ps1:
  - .local/ guard on -Output.
  - Writes map8l-result.json (schema pzmapforge.map8l-result.v0.1) + MD + packet doc.
  - Records all runtime result fields: spawn coordinate, ISO meta grid, worldmap bin presence,
    lotheader parse attempt, binary writer gate, next branch.
- scripts/test-build42-map8l-runtime-result.ps1: 20 assertions.
  - Test 1: .local guard exits nonzero.
  - Tests 3-5: output files exist.
  - Tests 6-20: JSON field values including spawn coordinate, ISO meta grid state,
    worldmap bin presence, gate status, next branch.
- scripts/inspect-build42-worldmap-bin-presence.ps1:
  - .local/ guard on -Output.
  - Presence/size check for worldmap.xml, worldmap.xml.bin, worldmap-forest.xml,
    worldmap-forest.xml.bin, streets.xml.bin, objects.lua, spawnpoints.lua.
  - Count-only for lotheader/lotpack/chunkdata files (no content reading).
  - Schema pzmapforge.map8m-worldmap-bin-presence.v0.1.
  - binary_contents_read=false; no_project_russia_files_copied=true; playable_claim_allowed=false.
- scripts/test-build42-worldmap-bin-presence.ps1: 15 assertions.
  - Test 1: .local guard exits nonzero.
  - Tests 3-4: output JSON and MD exist.
  - Tests 5-15: schema, map IDs, binary guards, presence flags.
- Updated scripts/validate.ps1: MAP-8M section + MAP-8L runtime result section before MAP-8L
  substantial candidate; psTotal 1336->1371; proof-packet v0.61->v0.62.
- Updated scripts/write-proof-packet.ps1: map8m/map8l runtime result fields; total 1336->1371;
  schema v0.61->v0.62.
- Updated scripts/test-proof-packet.ps1: 2 new assertions; total 1336->1371; schema v0.61->v0.62.

### Added (MAP-8L: Worldmap XML substantial candidate)
- docs/MAP_8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE.md: MAP-8L doctrine.
  - Source basis: MAP-8K found candidate worldmap.xml skeletal (52 bytes, 2 lines).
  - Reference (Project Russia parent) worldmap.xml substantial (888KB, 30959 lines).
  - Reference has worldmap.xml.bin; candidate does not.
  - Hypothesis: substantial worldmap.xml may be required for IsoMetaGrid parent mount.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map8l-worldmap-xml-candidate.ps1:
  - .local/ guard on -Output.
  - Generates substantial PZMapForge-owned worldmap.xml (1915 bytes, 44 lines).
  - Describes 1 cell at worldX=35, worldY=27 with coordinate proof.
  - No Project Russia content used. No binary contents read.
  - Outputs worldmap.xml + map8l-preflight.json + map8l-preflight.md + packet doc.
  - Includes operator deployment command.
- scripts/test-build42-map8l-worldmap-xml-candidate.ps1: 20 assertions.
  - Test1: refuses output outside .local.
  - Tests 3-5: output files exist.
  - Tests 6-12: worldmap.xml content (size, lines, XML declaration, cell coords, lotheader ref).
  - Tests 13-20: preflight JSON fields.
- Updated scripts/validate.ps1: MAP-8L section before MAP-8K; psTotal 1316→1336; proof-packet v0.60→v0.61.
- Updated scripts/write-proof-packet.ps1: map8l field; total 1316→1336; schema v0.60→v0.61.
- Updated scripts/test-proof-packet.ps1: map8l assertion; total 1316→1336; schema v0.60→v0.61.

### Added (MAP-8K: Parent map metadata contract comparator)
- docs/MAP_8K_PARENT_METADATA_CONTRACT_COMPARATOR.md: MAP-8K comparator doctrine.
  - MAP8K_PARENT_METADATA_CONTRACT_COMPARATOR_DEFINED.
  - Defines evidence step for parent_metadata_or_binary_cell_mount_contract branch.
  - Comparator reads map.info fields, binary file counts, text-file summaries (no binary contents).
  - Reference: Project Russia parent folder (read-only, no files copied).
  - Includes operator run commands: copy candidate only, then run comparator.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-parent-map-metadata-contract.ps1:
  - Parameters: -CandidateParentRoot, -ReferenceParentRoot, -Output, -CandidateParentMapId, -ReferenceParentMapId.
  - -Output must be under .local/. Exits nonzero otherwise.
  - Does NOT copy any reference files.
  - Does NOT read binary file contents (*.lotheader, *.lotpack, chunkdata_*.bin, *.bin, *.png, *.bik, *.pack).
  - Reads map.info key/value; counts lotheader/lotpack/chunkdata files.
  - Text-file summaries for worldmap.xml, objects.lua, spawnpoints.lua: size, line count, skeletal/substantial.
  - Outputs build42-parent-map-metadata-contract.json + .md.
- scripts/test-build42-parent-map-metadata-contract.ps1: 20 assertions.
  - Test1: refuses output outside .local.
  - Tests 3-4: output files exist.
  - Tests 5-20: JSON field values including schema, map IDs, binary guards, lots field, fixed2x, demoVideo, worldmap.xml summary, diffs.
- Updated scripts/validate.ps1: MAP-8K section before MAP-8I; psTotal 1296→1316; proof-packet v0.59→v0.60.
- Updated scripts/write-proof-packet.ps1: map8k field; total 1296→1316; schema v0.59→v0.60.
- Updated scripts/test-proof-packet.ps1: map8k assertion; total 1296→1316; schema v0.59→v0.60.

### Added (MAP-8I: Record dual spawnpoint keys runtime result)
- docs/MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT.md: MAP-8I dual spawnpoint runtime result doctrine.
  - Status: MAP8I_SPAWNPOINT_FIXED_BUT_ISOMETAGRID_NOT_MOUNTED.
  - Patch: both spawnpoints.lua files patched — unemployed + Profession_Unemployed at worldX=35 worldY=27.
  - MAP-8H profession-key error removed.
  - Player connected at 10746,8288,0 — matches intended 35_27 coordinate exactly.
  - Coordinate proof: 35*300+246=10746, 27*300+188=8288.
  - IsoMetaGrid map folder list still empty; terrain is vanilla/fallback.
  - next_branch=parent_metadata_or_binary_cell_mount_contract.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map8i-runtime-result-packet.ps1:
  - .local/ guard.
  - Writes map8i-result.json (schema pzmapforge.map8i-result.v0.1).
  - Writes map8i-result.md and MAP_8I_DUAL_SPAWNPOINT_RUNTIME_RESULT_PACKET.md.
  - All key result fields recorded including coordinate proof and gate status.
- scripts/test-build42-map8i-runtime-result.ps1: 20 assertions.
  - Test1: .local guard refuses output outside .local/.
  - Tests 3-5: packet files exist.
  - Tests 6-20: JSON field values including spawn coordinate, worldX/Y, binary gate.
- Updated scripts/validate.ps1: MAP-8I section before MAP-8H; psTotal 1275→1296; proof-packet v0.58→v0.59.
- Updated scripts/write-proof-packet.ps1: map8i_dual_spawnpoint_runtime_result_tests=20 field; total 1275→1296; schema v0.58→v0.59.
- Updated scripts/test-proof-packet.ps1: map8i assertion; total_expected_assertions 1275→1296; schema v0.58→v0.59.

### Added (MAP-8H: Prepare parent/child map contract probe)
- docs/MAP_8H_PARENT_CHILD_CONTRACT_PROBE.md: parent/child probe doctrine.
  - Source basis: MAP-8F result + Project Russia parent/child contract observation.
  - Project Russia: child lots=Project Russia; parent has cell binaries, no lots field.
  - common\media\maps confirmed NOT ignored in Build 42 (Project Russia uses it).
  - MAP-8H layout: PZMapForge/ parent (cell binaries, fixed2x=true) +
    pzmapforge_build42_candidate_v4_001/ child (lots=PZMapForge, zoomX/Y/S).
  - Server Map line: pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY.
  - BINARY_WRITER_GATE_STILL_CLOSED; NO_PROJECT_RUSSIA_FILES_COPIED;
    NO_THIRD_PARTY_FILES_COPIED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map8h-parent-child-contract-packet.ps1:
  - .local/ guard.
  - CLI generation fallback for cell binaries.
  - Stages common\media\maps\PZMapForge\ with 35_27.* cell binaries + spawnpoints.
  - Stages common\media\maps\pzmapforge_build42_candidate_v4_001\ with lots=PZMapForge.
  - 5 packet files + staged package.
  - Preflight JSON: source_basis/parent_map_id=PZMapForge/child_map_id/
    layout=common_media_maps_parent_child/parent_contains_generated_cell_binaries=true/
    no_project_russia_files_copied=true/binary_writer_gate_closed=true/playable_claim_allowed=false.
- scripts/test-build42-map8h-parent-child-contract.ps1: 20 assertions.
- scripts/validate.ps1: MAP-8H section added; psTotal 1254->1275; proof-packet v0.58 reference.
- scripts/write-proof-packet.ps1: v0.57->v0.58; map8h_parent_child_contract_probe_tests=20;
  total_expected_assertions 1254->1275; MD table updated.
- scripts/test-proof-packet.ps1: map8h assertion added; total check 1254->1275; schema v0.58.
- docs/IMPLEMENTATION.md: MAP-8H ratified row added.
- docs/MAP_EXPORT_CONTRACT.md: MAP-8H section added.

### Added (MAP-8G: Record MAP-8F lots=self runtime result + define known-working comparator)
- docs/MAP_8F_LOTS_SELF_RUNTIME_RESULT.md: MAP-8F runtime result record.
  - MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED: city selector shows candidate for first time.
  - lots=NONE -> lots=<MapId> was the only change from MAP-8D.
  - Player selected PZMapForge city; spawned in Muldraugh/fallback (not PZMapForge content).
  - IsoMetaGrid map folder list still empty (server and client).
  - WorldMapDataAssetManager failed to load worldmap.xml/worldmap-forest.xml.
  - Invalid magic error absent (stubs removed in MAP-8D/8F).
  - Player fully connected at 10851,9846,0; disconnected at 10856,9850,0.
  - Conclusion: lots=<MapId> necessary for city selector but not sufficient for IsoMetaGrid.
  - next_branch=known_working_build42_map_contract_comparator.
  - PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map8f-runtime-result-packet.ps1:
  - .local/ guard.
  - Writes map8f-result-packet.json (schema pzmapforge.map8f-result-packet.v0.1) + MD + packet doc.
  - Result fields: workshop_ready/lots_self/city_selector_visible/invalid_bin_stubs_absent/
    worldmap_xml_failed_to_load/player_fully_connected/player_spawn_coordinate=10851,9846,0/
    spawned_in_muldraugh_or_fallback=true/iso_meta_grid_map_folder_list_empty=true/
    playable_claim_allowed=false/binary_writer_gate_closed=true.
- scripts/test-build42-map8f-runtime-result.ps1: 20 assertions.
- docs/MAP_8G_KNOWN_WORKING_CONTRACT_COMPARATOR.md: comparator planning doc.
  - MAP8G_KNOWN_WORKING_CONTRACT_COMPARATOR_DEFINED.
  - Scope: 42\media\maps\<MapId>\ version-scoped layout.
  - Key fields: map.info lots/zoomX/Y/S, worldmap xml vs xml.bin, spawnregions.lua, mod.info.
  - Human-only operator steps: place known-working map text metadata under .local/.
  - No cell binary files copied. No third-party content redistributed.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-known-working-map-contract-v2.ps1:
  - .local/ guard on all three parameters (CandidateRoot/ReferenceRoot/Output).
  - Reads map.info (key=value), file presence for 11 worldmap/metadata files from both roots.
  - Computes map_info_field_differences/file_set_differences/worldmap_bin_differs/spawnregions_lua_differs.
  - Outputs build42-known-working-map-contract-v2.json + .md.
  - No files copied; no binary contents read; no PZ run.
- scripts/validate.ps1: MAP-8G + MAP-8F sections added; psTotal 1233->1254; proof-packet v0.57 reference.
- scripts/write-proof-packet.ps1: v0.56->v0.57; map8f_lots_self_runtime_result_tests=20;
  total_expected_assertions 1233->1254; MD table updated.
- scripts/test-proof-packet.ps1: map8f assertion added; total check 1233->1254; schema v0.57.
- docs/IMPLEMENTATION.md: MAP-8G and MAP-8F ratified rows added.
- docs/MAP_EXPORT_CONTRACT.md: MAP-8G and MAP-8F sections added.

### Added (MAP-8D: Prepare no invalid worldmap bin stubs probe packet)
- docs/MAP_8D_NO_INVALID_WORLDMAP_BIN_STUBS_PACKET.md: probe doctrine.
  - Removes worldmap.xml.bin, worldmap-forest.xml.bin, streets.xml.bin from staged package.
  - Retains worldmap.xml, worldmap-forest.xml, worldmap.png (uncompiled XML/PNG).
  - Retains coordinate-aligned binaries 35_27.* (content unchanged).
  - Decision: streets.xml.bin removed -- ASCII-marker stub, no valid binary magic, prefer clean probe.
  - 42\media\maps\<MapId>\ version-scoped layout (no root media path).
  - Canary: MAP8D_NO_WORLDMAP_BIN_STUBS.txt in map folder.
  - BINARY_WRITER_GATE_STILL_CLOSED; NO_BINARY_WRITER_CHANGES; NO_THIRD_PARTY_FILES_COPIED.
  - PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map8d-no-invalid-worldmap-bin-packet.ps1:
  - .local/ guard; refuses output outside .local.
  - CLI generation fallback for binaries (dotnet map-export-experimental empty_grass_v4).
  - Stages 42\media\maps\<MapId>\ layout with all required files minus .bin stubs.
  - 5 packet files + staged package.
  - Preflight JSON: source_basis=MAP-8B/version_scoped_media_path=true/
    invalid_worldmap_bin_stubs_removed=true/worldmap_xml_retained=true/
    streets_xml_bin_removed=true/binary_writer_gate_closed=true/playable_claim_allowed=false.
  - No PZ run; no Workshop upload; no binary writer changes; no third-party files.
- scripts/test-build42-map8d-no-invalid-worldmap-bin.ps1: 20 assertions.
- scripts/validate.ps1: MAP-8D section added; psTotal 1212->1233; proof-packet v0.56 reference.
- scripts/write-proof-packet.ps1: v0.55->v0.56; map8d_no_invalid_worldmap_bin_probe_tests=20;
  total_expected_assertions 1212->1233; MD table updated.
- scripts/test-proof-packet.ps1: map8d assertion added; total check 1212->1233; schema v0.56.
- docs/IMPLEMENTATION.md: MAP-8D ratified row added.
- docs/MAP_EXPORT_CONTRACT.md: MAP-8D section added.

### Added (MAP-8B: Record version-scoped media path runtime result)
- docs/MAP_8B_VERSION_MEDIA_RUNTIME_RESULT.md: MAP-8B partial registration breakthrough record.
  - MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH: 42\media path visible to worldmap loader.
  - Client attempted reads of worldmap.xml.bin and worldmap-forest.xml.bin from 42\media\maps\<MapId>\.
  - Both failed java.io.IOException: invalid format (magic doesn't match).
  - IsoMetaGrid map folder list still empty (server and client).
  - Visual custom city selector visible; player fully connected at 10878,10028,0.
  - Generated worldmap .bin stubs now actively read and rejected.
  - Binary writer gate still closed; no binary writer changes; no third-party files copied.
  - Next branch candidates: remove invalid .bin stubs / investigate IsoMetaGrid registration contract /
    inspect known-working version-scoped structure without copying third-party files.
  - PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map8b-runtime-result-packet.ps1:
  - .local/ guard; refuses output outside .local.
  - Writes map8b-result-packet.json (schema pzmapforge.map8b-result-packet.v0.1) + MD + packet doc.
  - Result fields: workshop_ready/mod_loaded/visual_custom_city_selector_visible/player_fully_connected/
    player_spawn_coordinate=10878,10028,0/iso_meta_grid_map_folder_list_empty=true/
    version_scoped_media_path_visible_to_worldmap_loader=true/worldmap_bin_invalid_magic=true/
    playable_claim_allowed=false/binary_writer_gate_closed=true.
  - No PZ run; no Workshop upload; no binary writer changes; no third-party files copied.
- scripts/test-build42-map8b-runtime-result.ps1: 20 assertions.
- scripts/validate.ps1: MAP-8B section added; psTotal 1191->1212; proof-packet v0.55 reference.
- scripts/write-proof-packet.ps1: v0.54->v0.55; map8b_version_media_runtime_result_tests=20;
  total_expected_assertions 1191->1212; MD table updated.
- scripts/test-proof-packet.ps1: map8b assertion added; total_expected_assertions check 1191->1212.
- docs/IMPLEMENTATION.md: MAP-8B ratified row added.
- docs/MAP_EXPORT_CONTRACT.md: MAP-8B section added.

### Added (MAP-7Y: Prepare minimal sidecar stub probe)
- docs/MAP_7Y_MINIMAL_SIDECAR_STUB_PROBE.md: sidecar stub probe doctrine.
  - MAP7Y_SIDECAR_STUB_PROBE_STAGED; MAP_BIN_DISCRIMINATOR_FALSE; SIDECAR_STUBS_GENERATED_FROM_SCRATCH; NO_THIRD_PARTY_FILES_COPIED.
  - 12,390 cell file gap is not actionable (Dru_map assets, must not be copied).
  - Generated candidate-owned stubs: streets.xml.bin, worldmap.xml.bin, worldmap-forest.xml.bin, worldmap.png.
  - Retained coordinate-aligned binaries: 35_27.lotheader, world_35_27.lotpack, chunkdata_35_27.bin.
  - Test outcomes: map folder mounts OR sidecar parse error OR fallback forest persists.
  - Binary writer gate: still closed; stubs are sidecar files, NOT lotheader/lotpack/chunkdata changes.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false; NO_BINARY_WRITER_CHANGES.
- scripts/prepare-build42-map7y-sidecar-stub-packet.ps1:
  - .local/ guard; no PZ run; no Workshop upload; no reference file copying.
  - Generates empty_grass_v4 base, renames 0_0->35_27, updates map.info zoom, uses function SpawnPoints() style.
  - Generates minimal deterministic stubs (PZMF_MAP7Y_STUB_* ASCII markers) for streets.xml.bin, worldmap.xml.bin, worldmap-forest.xml.bin.
  - Generates 256x256 placeholder worldmap.png.
  - 7 packet files + staged-workshop-sidecar-stubs package.
  - preflight: source_map7x_commit=8c45c0a, map_bin_discriminator=false, sidecar_probe_created=true, bak_sidecars_created=false, third_party_reference_files_copied=false.
- scripts/test-build42-map7y-sidecar-stub-packet.ps1: 24 assertions.
- psTotal 1166->1191; proof-packet v0.53->v0.54.

### Added (MAP-7X: Record actual runtime registration contract result)
- docs/MAP_7X_ACTUAL_REGISTRATION_CONTRACT_RESULT.md: actual MAP-7W inspector result recorded.
  - MAP7X_ACTUAL_CONTRACT_RESULT_RECORDED; MAP_BIN_DISCRIMINATOR_FALSE; NON_CELL_SIDECAR_GAP_IDENTIFIED.
  - map.bin: ruled out (neither reference nor candidate has it).
  - 12,398 missing files dominated by expected Dru_map cell files (4130 lotheader + 4130 lotpack + 4130 chunkdata = 12390 cells); DO NOT copy Dru_map cells.
  - Non-cell sidecar gap: streets.xml.bin, worldmap.xml.bin, worldmap-forest.xml.bin (and .bak variants), worldmap.png (8 files total).
  - map.info/mod.info differences are expected identity differences, not proven blockers.
  - BINARY_WRITER_GATE_STILL_CLOSED; NO_THIRD_PARTY_FILES_COPIED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
  - Next branch: non-cell sidecar and runtime registration probe.
- scripts/prepare-build42-map7x-actual-contract-result-packet.ps1:
  - .local/ guard; no PZ run; no Workshop upload; no reference file copying.
  - 5 packet files: actual contract result, non-cell sidecar discriminators, next decision tree, preflight JSON+MD.
  - preflight: map_bin_discriminator=false, missing_non_cell_sidecars=[streets.xml.bin etc.], binary_writer_gate_closed=true, third_party_reference_files_copied=false.
- scripts/test-build42-map7x-actual-contract-result.ps1: 20 assertions.
- psTotal 1145->1166; proof-packet v0.52->v0.53.

### Added (MAP-7W: Add runtime map registration contract inspector)
- docs/MAP_7W_RUNTIME_MAP_REGISTRATION_MOUNTING_CONTRACT.md: active branch doctrine.
  - MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED; BINARY_FORMAT_INVESTIGATION_PAUSED.
  - Seven hypotheses: H1=map.bin missing, H2/H3=mod.info/map.info value mismatch, H4=Workshop path differs, H5=server log, H6=Map= syntax, H7=spawnregions.lua.
  - map.bin not declared as cause until inspector proves it from local reference.
  - BINARY_WRITER_GATE_STILL_CLOSED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map-registration-contract.ps1:
  - .local/ output guard. Reads only two explicit mod roots.
  - Inventories all files in common/media/maps/<MapId>/ (non-recursive flat list + has_map_bin, has_worldmap_xml, has_lotheader etc.).
  - Compares mod.info and map.info key/value pairs for differences.
  - Detects spawnpoints.lua style (function SpawnPoints() vs bare return).
  - Parses optional log files for: Workshop Ready, mod loaded, map-folder scan, SANITY CHECK FAIL, IsoMetaGrid, lotheader evidence.
  - Outputs: map-registration-contract.json+.md; schema v0.1.
  - Key fields: exact_file_set_match, reference_has_map_bin, candidate_has_map_bin, map_bin_discriminator, map_info_value_differences_count, runtime_mount_discriminator_found.
- scripts/prepare-build42-map7w-runtime-registration-packet.ps1:
  - .local/ guard. Optionally runs inspector if roots exist. Always writes packet docs.
  - 6 packet files: registration packet, file-set discriminators, log evidence plan, next decision tree, preflight JSON+MD.
  - preflight: binary_format_investigation_paused=true, binary_writer_gate_closed=true, next_branch=runtime_map_registration_and_mounting.
- scripts/test-build42-map7w-runtime-registration.ps1: 20 assertions.
- psTotal 1124->1145; proof-packet v0.51->v0.52.

### Added (MAP-7V: Record K004 K006 map activation controls)
- docs/MAP_7V_K004_K006_CONTROL_RESULTS.md: K004 and K006 results recorded.
  - MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED: coordinate-aligned binaries present, spawnpoint honored (10746,8288,0), fallback forest, no candidate lotheader evidence.
  - MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED: zero PZMapForge binary files, Workshop Ready, mod loaded, spawn honored, SANITY CHECK FAIL. SANITY CHECK FAIL is NOT binary parse evidence.
  - K006 proves: binary presence is not the discriminator; same fallback-forest outcome with or without binaries.
  - All previous fallback-forest results were map-registration evidence, not binary-format evidence.
  - BINARY_FORMAT_INVESTIGATION_PAUSED.
  - Next branch: runtime map registration / map folder mounting.
  - BINARY_WRITER_GATE_STILL_CLOSED; LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map7v-control-results-packet.ps1:
  - .local/ guard; no PZ run; no Workshop upload.
  - 5 packet files: results, binary gate decision, next-branch, preflight JSON+MD.
  - preflight: k006_candidate_lotheader/lotpack/chunkdata_count=0, binary_writer_gate_closed=true, binary_format_investigation_paused=true, next_branch=runtime_map_registration_and_mounting.
- scripts/test-build42-map7v-control-results.ps1: 20 assertions.
- psTotal 1103->1124; proof-packet v0.50->v0.51.

### Added (MAP-7U: Prepare coordinate-aligned Workshop diagnostic packet)
- docs/MAP_7U_MODROOT_LAYOUT_MATCH_AND_COORDINATE_DISCRIMINATOR.md: layout match and coordinate discriminator recorded.
  - MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED: candidate mod-root layout matches Dru_map (all bool fields equal, 0 BOM violations).
  - COORDINATE_DISCRIMINATOR_IDENTIFIED: candidate=1 cell at 0_0, Dru_map=4130 cells centered at 35_27.
  - Zoom discriminator: candidate zoomX/Y=0/0, Dru_map=10505/12220/14.5.
  - Spawn discriminator: candidate worldX/Y=0/0, Dru_map=35/27.
  - BINARY_WRITER_GATE_STILL_CLOSED; LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-workshop-cell-coordinate-contract.ps1:
  - .local/ output guard. Reads only two explicit mod roots.
  - Detects lotheader cell counts, minX/maxX/minY/maxY, first 40 cells.
  - Parses map.info zoomX/Y/S fields.
  - Parses spawnpoints.lua worldX/Y/posX/Y pairs.
  - Checks whether candidate spawn target cell exists as lotheader file.
  - Outputs: workshop-cell-coordinate-contract.json+.md; schema v0.1.
- scripts/prepare-build42-map7u-coordinate-discriminator-packet.ps1:
  - .local/ guard. Generates empty_grass_v4 via dotnet CLI.
  - Renames binary files 0_0 -> 35_27 (content unchanged, coordinate relabel only).
  - Updates map.info: zoomX=10505, zoomY=12220, zoomS=14.5.
  - Updates spawnpoints.lua: worldX=35, worldY=27.
  - 8 packet files + staged-workshop-coordinate-aligned package.
  - preflight: modroot_layout_match=true, coordinate_aligned_target_cell=35_27, binary_contents_mutated=false.
  - Human checklist: update existing Workshop item 3740642200 manually.
- scripts/test-build42-map7u-coordinate-discriminator.ps1: 20 assertions.
- psTotal 1082->1103; proof-packet v0.49->v0.50.

### Added (MAP-7T: Record Workshop K002 runtime payload comparison)
- docs/MAP_7T_WORKSHOP_K002_RUNTIME_PAYLOAD_COMPARISON.md: K002 recorded.
  - Workshop ID 3740642200 downloaded, installed, reached Ready.
  - PZMapForge mod loaded (pzmapforge_build42_candidate_v4_001).
  - No expected-map lotheader/meta evidence. Fallback forest.
  - Binary writer gate still closed.
  - Empty map-folder scan still failure evidence here (no lotheader + no built world).
  - Next: compare actual downloaded payload vs Dru_map payload structure.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-workshop-runtime-payload.ps1:
  - .local/ output guard. Reads only the two explicit roots provided.
  - Detects: root mod.info, 42/mod.info, common/mod.info, mods/<id>, Contents/mods/<id>,
    common/media/maps/<id>, map.info fields (lots=NONE, zoomX/Y), spawnpoints/objects/worldmap,
    lotheader/lotpack/chunkdata file names and sizes, BOM violations.
  - Outputs: workshop-runtime-payload-comparison.json + .md
  - Schema: pzmapforge.workshop-runtime-payload-comparison.v0.1
- scripts/prepare-build42-map7t-k002-record-packet.ps1:
  - .local/ guard. No PZ run. No Workshop writes.
  - Records K002 result: MAP7F_VARIANT_W_S_UPLOAD_K002_MAP_FOLDER_SCAN_EMPTY, Workshop 3740642200 Installed/Ready, mod loaded, no expected-map evidence.
  - Writes: MAP_7T_K002_RESULT_SUMMARY.md, MAP_7T_NEXT_DECISION_TREE.md, preflight JSON+MD.
  - Optionally runs comparison if operator provides both roots.
  - Decision tree: payload comparison, binary gate, server-side logs.
- scripts/test-build42-map7t-k002-runtime-payload.ps1: 20 assertions.
- psTotal 1061->1082; proof-packet v0.48->v0.49.

### Added (MAP-7S: Prepare private Workshop staging packet)
- docs/MAP_7S_PRIVATE_WORKSHOP_STAGING_PACKET.md: Staging packet doctrine.
  - MAP7S_WORKSHOP_STAGING_PACKET_CREATED; NO_AUTOMATIC_WORKSHOP_UPLOAD; STAGED_PACKAGE_LOCAL_ONLY.
  - Staged package uses Dru_map-aligned layout (MAP-7O contract): root mod.info + 42/mod.info + no common/mod.info + common/media/maps/.
  - Human upload checklist: create NEW private/unlisted Workshop item; do NOT use 3355966216 (Dru_map's ID); own Workshop ID required.
  - Server wiring template: Mods=pzmapforge_build42_candidate_v4_001 + WorkshopItems=<PZMapForgeOwnWorkshopId>.
  - Success: expected_map_lotheader_meta_evidence_found=true; binary writer gate opens.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false; NO_BINARY_WRITER_CHANGES.
- scripts/prepare-build42-map7s-private-workshop-staging-packet.ps1:
  - .local/ output guard.
  - Generates empty_grass_v4 candidate via dotnet CLI.
  - Stages Dru_map-aligned Workshop package under .local/staged-workshop/<MapId>/.
  - Writes 8 packet files: staging packet, upload checklist, server wiring template, log capture, success/failure criteria, manifest, preflight JSON+MD.
  - preflight: variant_j_result/borrowed_workshopitems_trigger_insufficient/requires_own_workshop_id/automatic_workshop_upload_performed=false/staged_package_created=true.
  - No PZ run; no Workshop/mods/Server writes; no Steam API calls.
- scripts/test-build42-map7s-private-workshop-staging.ps1: 20 assertions.
- psTotal 1040->1061; proof-packet v0.47->v0.48.

### Added (MAP-7R: Record borrowed WorkshopItems trigger failure)
- docs/MAP_7R_VARIANT_J_WORKSHOP_TRIGGER_FAILURE.md: Variant J recorded.
  - MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT: WorkshopItems=3355966216 (Dru_map's ID) was insufficient for local loose mod.
  - BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED: borrowed Workshop ID ruled out.
  - Workshop ID 3355966216 reached Ready but activated Dru_map's path, not PZMapForge's.
  - PZMapForge candidate still only loads at mod registration level, no expected-map lotheader evidence.
  - STATIC_VARIANTS_ABCDEFGHI_EXHAUSTED + BORROWED_WORKSHOPITEMS_TRIGGER_EXHAUSTED.
  - Next: real candidate Workshop-style activation (human-approved private/unlisted upload).
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map7d-load-result.ps1: MAP-7R fields and VariantJ classification added.
  - New field: expected_map_lotheader_meta_evidence_found (checks expected map ID near .lotheader in log).
  - MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT: fires when VariantJ + Workshop Installed/Ready + candidate loaded + game reached + no expected-map lotheader evidence.
  - Generic lotheader lines (Muldraugh/vanilla) do not satisfy expected_map_lotheader_meta_evidence_found.
  - Backward compatible: all existing classifications preserved.
- scripts/prepare-build42-map7r-workshop-activation-decision-packet.ps1:
  - .local/ output guard; no PZ run; no Workshop/mods/Server writes; no upload.
  - 7 packet files: decision packet, Variant J summary, next decision tree,
    private Workshop upload requirements, no-more-static-layout-tests, preflight JSON+MD.
  - preflight: variant_j_result=MAP7R_VARIANT_J_WORKSHOP_TRIGGER_INSUFFICIENT,
    borrowed_workshopitems_trigger_insufficient=true, static_variants_abcdefghi_exhausted=true,
    no_more_static_layout_tests=true, public_playable_claim_allowed=false.
  - Decision tree: real Workshop upload (human-approved), binary writer gate, lotheader evidence gate.
- scripts/test-build42-map7r-workshop-trigger-failure.ps1: 20 assertions.
- psTotal 1019->1040; proof-packet v0.46->v0.47.

### Added (MAP-7Q: Record Dru_map runtime baseline success)
- docs/MAP_7Q_DRUMAP_RUNTIME_BASELINE_SUCCESS.md: Dru_map baseline corrected.
  - MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS: new classification for runtime success.
  - EMPTY_CLIENT_SCAN_NOT_DECISIVE: empty printed client map-folder scan is not decisive in Build 42 coop/server.
  - Dru_map baseline human result: player spawned into real built world with roads and houses.
  - Old analyzer produced MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY (incorrect).
  - Runtime success signals: Workshop Installed/Ready, mod loaded, lotheader evidence, player data, multiplayer.
  - Next: runtime activation / mounting investigation (WorkshopItems= flow).
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map7d-load-result.ps1: MAP-7Q runtime success model added.
  - New fields: expected_mod_loaded, workshop_id_3355966216_seen, workshop_download_seen,
    workshop_installed_seen, workshop_ready_seen, multiplayer_reached,
    lotheader_meta_evidence_found, lotheader_meta_paths_or_names,
    runtime_success_evidence_found, empty_client_map_folder_scan_decisive,
    visual_confirmation_required.
  - MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS: fires when DruMapBaseline + multi-signal runtime evidence,
    even if map_folders_list_empty=true.
  - Backward compatible: MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND/EMPTY preserved.
  - Schema bumped: v0.3 -> v0.4.
- scripts/prepare-build42-map7q-runtime-activation-next-packet.ps1:
  - .local/ output guard; no PZ run; no Workshop/mods/Server writes.
  - 7 packet files: runtime activation packet, Dru_map result summary, analyzer evidence model,
    next decision tree, Workshop-style activation hypotheses, preflight JSON+MD.
  - preflight: drumap_baseline_runtime_success_model=true, empty_client_scan_not_decisive=true,
    variants_abcdefghi_exhausted=true, public_playable_claim_allowed=false.
  - Decision tree: WorkshopItems= flow, lotheader evidence gate, binary writer gate.
  - No automatic Workshop upload.
- scripts/test-build42-map7q-runtime-baseline-success.ps1: 20 assertions.
- psTotal 998->1019; proof-packet v0.45->v0.46.

### Added (MAP-7P: Record Variant I and add known-working runtime baseline)
- docs/MAP_7P_VARIANT_I_AND_RUNTIME_BASELINE.md: Variant I failure record and runtime baseline plan.
  - Experiment I result: MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY.
  - VARIANTS_ABCDEFGHI_EXHAUSTED: all nine layout variants exhausted.
  - Blocker is runtime activation, not static layout.
  - Decision tree: Dru_map baseline diagnostic to determine runtime contract.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/prepare-build42-map7p-known-working-runtime-baseline-packet.ps1:
  - .local/ output guard.
  - Writes 7 packet files: packet, variant-I summary, server wiring, log capture, decision tree, preflight JSON+MD.
  - preflight records variant_i_result=MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY and variants_abcdefghi_exhausted=true.
  - Human-only instructions for Dru_map baseline: Mods=Dru_map, WorkshopItems=3355966216, Map=Dru_map;Muldraugh, KY.
  - Analyzer commands include -ExpectedMapId Dru_map -VariantLabel DruMapBaseline.
  - Note: if Dru_map only works via Workshop flow, local mods may not mount the same way.
  - No PZ run, no Workshop/mods/Server writes, no Dru_map auto-copy.
- scripts/inspect-build42-map7d-load-result.ps1: DruMapBaseline classifications added.
  - DruMapBaseline + non-empty scan + expected map found -> MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND.
  - DruMapBaseline + empty scan -> MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY.
  - Existing Variant A-I classifications unchanged.
- scripts/test-build42-map7p-known-working-runtime-baseline.ps1: 20 assertions.
- psTotal 977->998; proof-packet v0.44->v0.45.

### Added (MAP-7O: Add Dru_map aligned metadata experiment)
- docs/MAP_7O_DRUMAP_ALIGNED_EXPERIMENT_I.md: Experiment I plan.
  - Dru_map comparison findings: root mod.info + 42/mod.info + common/media/maps/.
  - Untested combination: root mod.info (NOT common/mod.info) + common/media/maps/.
  - map.info fixes: lots=NONE, zoomX/Y/S placeholders (MAP-7N evidence).
  - Success/failure/progress conditions documented.
  - No city choice is not decisive; map_folders_list_empty is decisive.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map-discovery-path.ps1: updated to v0.4.
  - New fields: has_drumap_aligned_layout, common_mod_info_absent.
  - has_drumap_aligned_layout: true when root+42 mod.info, no common/mod.info,
    common/media/maps present.
  - Updated risk logic and markdown output.
- scripts/inspect-build42-map-metadata-contract.ps1: updated to v0.3.
  - Added common/mod.info and common/media/maps/map.info paths.
  - New fields: map_info_lots_is_none, map_info_has_zoomX/Y/S.
  - All three sources (42/, root, common/) checked for zoom/lots fields.
- scripts/prepare-build42-map7o-drumap-aligned-experiment-packet.ps1:
  - .local/ output guard.
  - Generates empty_grass_v4 candidate via CLI.
  - Creates experiment-I candidate (root mod.info + 42/mod.info + NO common/mod.info +
    common/media/maps/).
  - Modifies map.info: lots=NONE, adds zoomX=0, zoomY=0, zoomS=1.
  - Placeholder files: thumb.png, worldmap.xml/forest, maps/biomemap_0_0.png.
  - Binary files byte-exact unchanged from empty_grass_v4.
  - Runs discovery + metadata inspectors.
  - Optionally runs Dru_map comparator if reference present under .local/.
  - Writes 11 packet files.
- scripts/test-build42-map7o-drumap-aligned-experiment.ps1: 19 assertions.
  - Tests 1-2: path guard + all 11 files present.
  - Tests 3-5: root mod.info, 42/mod.info, NO common/mod.info.
  - Test 6: common/media/maps/map.info exists.
  - Tests 7-10: lots=NONE, zoomX, zoomY, zoomS in map.info.
  - Tests 11-13: no-BOM on root/42/common text files.
  - Test 14: binary sizes unchanged (29646/1056780/1026).
  - Tests 15-16: preflight drumap_aligned_layout, public_playable=false.
  - Tests 17-19: HUMAN-ONLY, MAP7F_VARIANT_I_MAP_FOLDER_SCAN_EMPTY,
    MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING in docs.
  - psTotal 958->977.
- scripts/validate.ps1: MAP-7O section; psTotal 958->977; v0.44.
- scripts/write-proof-packet.ps1: v0.44; map7o=19; total 977.
- scripts/test-proof-packet.ps1: assertions updated (19/977, schema v0.44).
- docs/IMPLEMENTATION.md: MAP-7O ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7O section.

No load test. No binary writer change. No PZ assets outside .local. No playable claim.
PS 977 / .NET 556 (unchanged).

### Added (MAP-7N: Add reference map id support to known-working comparator)
- docs/MAP_7N_REFERENCE_MAP_ID_COMPARATOR.md: patch record and Dru_map comparison.
  - Comparator needed -ReferenceMapId when reference map folder name != candidate.
  - Dru_map (workshop mod) copied to .local/ for comparison.
  - Dru_map structure: root mod.info + 42/mod.info + common/media/maps/Dru_map/
    (same layout as our experiment-H but with ROOT mod.info, not common/mod.info).
  - decision_signals: map_info_fields_in_reference_not_candidate (zoomX, zoomY, zoomS).
  - mod.info gap: none (candidate has all reference fields plus extras).
  - Key finding: reference uses root mod.info + 42/mod.info, NOT common/mod.info.
    Candidate experiment-H used common/mod.info -- never tested root mod.info
    combined with common/media/maps layout.
  - lots field bug: candidate has lots=<map_id>, reference has lots=NONE.
  - Recommended Experiment I: root mod.info + 42/mod.info + common/media/maps/ +
    lots=NONE + zoomX/zoomY/zoomS in map.info (exact Dru_map structure).
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-known-working-map-contract.ps1: -ReferenceMapId added.
  - New parameter: -ReferenceMapId (defaults to -MapId when omitted).
  - Candidate scan uses -MapId, reference scan uses -ReferenceMapId.
  - New report fields: candidate_map_id, reference_map_id.
  - Schema bumped from v0.1 to v0.2.
  - Updated markdown header to show both IDs.
  - Updated .EXAMPLE to show Dru_map usage.
- scripts/prepare-build42-map7m-known-working-contract-packet.ps1:
  - Updated command examples in generated instructions to include -ReferenceMapId.
- scripts/test-build42-map7n-reference-map-id.ps1: 9 assertions.
  - Tests 1-2: path guards (CandidateRoot/ReferenceRoot non-.local regression).
  - Test 3: exits 0 with separate MapId and ReferenceMapId.
  - Test 4: reference common/media/maps/<ReferenceMapId> found.
  - Tests 5-6: candidate_map_id and reference_map_id reported correctly.
  - Test 7: candidate layout scanned with candidate MapId.
  - Test 8: packet command examples contain -ReferenceMapId.
  - Test 9: public_playable_claim_allowed=false.
  - psTotal 949->958.
- scripts/validate.ps1: MAP-7N section; psTotal 949->958; v0.43.
- scripts/write-proof-packet.ps1: v0.43; map7n=9; total 958.
- scripts/test-proof-packet.ps1: assertions updated (9/958, schema v0.43).
- docs/IMPLEMENTATION.md: MAP-7N ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7N section.

No load test. No binary writer change. No PZ assets outside .local. No playable claim.
PS 958 / .NET 556 (unchanged).

### Added (MAP-7M: Record Variant H and add known-working map contract comparator)
- docs/MAP_7M_VARIANT_H_AND_WORKING_MAP_CONTRACT.md: Variant H result record.
  - Variant H: common/media/maps layout. MAP7F_VARIANT_H_MAP_FOLDER_SCAN_EMPTY.
  - COMMON_LAYOUT_ALONE_INSUFFICIENT; VARIANTS_ABCDEFGH_EXHAUSTED.
  - Decisive signal clarification: map_folders_list_empty=true is decisive.
    No city choice, forest world, player death/respawn are NOT decisive.
  - KNOWN_WORKING_MAP_COMPARATOR_REQUIRED; MAP_FOLDER_DISCOVERY_CONTRACT_UNKNOWN.
  - Decision tree for comparator results.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-known-working-map-contract.ps1: new comparator.
  - Accepts CandidateRoot and ReferenceRoot -- both MUST be under .local/.
  - Does NOT read PZ install, Workshop, mods, or Server paths.
  - Compares: layout (42/, 42.0/, common/), mod.info/map.info field names and values,
    file naming (world_0_0 vs 0_0 lotpack), worldmap files, biomemap folder,
    no-BOM/ASCII status, SHA-256 hashes, file sizes.
  - Reports: mod/map_info_fields_in_reference_not_candidate, value differences,
    decision_signals, version_folder_differs, common_folder_differs.
  - Writes map-known-working-contract-report.json + .md.
  - No binary contents copied into report (names/sizes/hashes only).
- scripts/prepare-build42-map7m-known-working-contract-packet.ps1:
  - .local/ output guard.
  - Generates experiment-H candidate (common/ layout, same as MAP-7L).
  - Creates reference-known-working-map/ placeholder under .local/.
  - Writes README with instructions for human to place reference mod.
  - Does NOT run comparator automatically (no reference available yet).
  - Writes 6 packet files:
    MAP_7M_KNOWN_WORKING_CONTRACT_PACKET.md, MAP_7M_VARIANT_H_RESULT_SUMMARY.md,
    MAP_7M_REFERENCE_CAPTURE_INSTRUCTIONS.md, MAP_7M_NEXT_DECISION_TREE.md,
    map7m-preflight.json (VARIANTS_ABCDEFGH_EXHAUSTED=true, comparator_run=false),
    map7m-preflight.md.
- scripts/test-build42-map7m-known-working-contract.ps1: 12 assertions.
  - Tests 1-2: comparator refuses non-.local CandidateRoot/ReferenceRoot.
  - Test 3: comparator produces report for two .local fixtures.
  - Tests 4-5: detects differing mod.info/map.info fields.
  - Test 6: detects 42 vs 42.0 folder difference.
  - Test 7: detects common/media/maps presence in reference.
  - Test 8: detects no-BOM/ASCII for text files.
  - Tests 9-12: packet all 6 docs present, VARIANTS_ABCDEFGH_EXHAUSTED,
    public_playable=false, no_automatic_pz_read_or_write=true.
  - psTotal 937->949.
- scripts/validate.ps1: MAP-7M section; psTotal 937->949; v0.42.
- scripts/write-proof-packet.ps1: v0.42; map7m=12; total 949.
- scripts/test-proof-packet.ps1: assertions updated (12/949, schema v0.42).
- docs/IMPLEMENTATION.md: MAP-7M ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7M section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 949 / .NET 556 (unchanged).

### Added (MAP-7L: Record Variant G and add common layout experiment)
- docs/MAP_7L_VARIANT_G_AND_COMMON_LAYOUT_PIVOT.md: Variant G result record.
  - Variant G: mod.info map= field (H8). MAP7F_VARIANT_G_MAP_FOLDER_SCAN_EMPTY.
  - H8_MOD_INFO_MAP_FIELD_RULED_OUT; VARIANTS_ABCDEFG_EXHAUSTED.
  - Operator-provided Build 42 structure evidence: common/media/maps/<MapId>/.
  - COMMON_LAYOUT_PIVOT: map files must be under common/, not 42/ or root.
  - Diagnostic distinction maintained: SCAN_EMPTY vs LOTHEADER_FILES_MISSING.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map-discovery-path.ps1: updated to v0.3.
  - New fields: has_common_mod_info, has_common_media_maps, has_common_map_info,
    has_common_maps_subfolder, has_common_worldmap_xml, has_common_worldmap_forest_xml,
    has_common_thumb_png, has_common_biomemap_folder, has_world_xy_lotpack_pattern,
    has_plain_xy_lotpack_pattern, common_media_maps_recommended, variant_g_result,
    variants_abcdefg_exhausted.
  - New discovery risk: LOWER_COMMON_FULL_LAYOUT (when common/mod.info + common/media/maps present).
  - Schema bumped to v0.3.
- scripts/prepare-build42-map7l-common-layout-experiment-packet.ps1:
  - .local/ output guard.
  - Generates empty_grass_v4 candidate via CLI.
  - Creates experiment-H candidate with documented common/ layout:
    42/mod.info (version layer) + common/mod.info + common/media/maps/<MapId>/.
  - Map data files (binary + text) copied byte-exact to common/.
  - thumb.png placeholder (1x1 via System.Drawing or empty fallback).
  - worldmap.xml and worldmap-forest.xml minimal XML placeholders.
  - maps/biomemap_0_0.png placeholder.
  - SHA-256 verification that binary files are unchanged.
  - Runs discovery path + metadata contract inspectors.
  - Writes 12 packet files: MAP_7L_COMMON_LAYOUT_PACKET.md,
    MAP_7L_VARIANT_G_RESULT_SUMMARY.md, MAP_7L_OPERATOR_STRUCTURE_EVIDENCE.md,
    MAP_7L_EXPERIMENT_H_MANUAL_INSTALL_COMMANDS.md (HUMAN-ONLY),
    MAP_7L_EXPERIMENT_H_LOG_CAPTURE_COMMANDS.md, result template,
    map7l-common-layout-preflight.json, map7l-common-layout-preflight.md,
    discovery + metadata inspector outputs (4 files).
- scripts/test-build42-map7l-common-layout-experiment.ps1: 15 assertions.
  - Test 1: path guard.
  - Test 2: all 12 files present + exit 0.
  - Tests 3-8: common/ layout structure (mod.info, map.info, spawnpoints,
    objects, lotheader, chunkdata).
  - Test 9: LOTH/LOTP/chunkdata sizes unchanged (29646/1056780/1026).
  - Test 10: text files no-BOM.
  - Tests 11-13: preflight variant_g_result, variants_abcdefg_exhausted,
    common_media_maps_layout.
  - Tests 14-15: HUMAN-ONLY marker, public_playable=false.
  - psTotal 922->937.
- scripts/validate.ps1: MAP-7L section; psTotal 922->937; v0.41.
- scripts/write-proof-packet.ps1: v0.41; map7l=15; total 937.
- scripts/test-proof-packet.ps1: assertions updated (15/937, schema v0.41).
- docs/IMPLEMENTATION.md: MAP-7L ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7L section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 937 / .NET 556 (unchanged).

### Added (MAP-7K: Record Variant F folder id discovery failure)
- docs/MAP_7K_VARIANT_F_FOLDER_ID_FAILURE.md: Variant F result record.
  - Variant F: exact folder name == mod.info id (H5 hypothesis).
    MAP7F_VARIANT_F_MAP_FOLDER_SCAN_EMPTY; H5_FOLDER_ID_ALIGNMENT_RULED_OUT.
  - VARIANTS_ABCDEF_EXHAUSTED: six experiments produced empty scan.
  - Next: H8 -- mod.info map= field to register media path.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map-metadata-contract.ps1: updated to v0.2.
  - New fields: mod_info_has_map_field, v42_mod_info_map_value,
    root_mod_info_map_value, mod_info_map_value_matches_expected,
    h5_folder_id_alignment_result, h8_mod_info_map_field_recommended.
  - h8_mod_info_map_field_recommended=true when map= field absent.
- scripts/prepare-build42-map7k-modinfo-map-field-experiment-packet.ps1:
  - .local/ output guard.
  - Generates empty_grass_v4 candidate via CLI.
  - Creates experiment-G dual-layout candidate: root + 42/ mod.info + media/maps/.
  - Adds map=pzmapforge_build42_candidate_v4_001 to both mod.info files.
  - Preserves no-BOM UTF-8 encoding on modified mod.info files.
  - LOTH/LOTP/chunkdata binary files unchanged from empty_grass_v4.
  - Runs discovery path + metadata contract inspectors.
  - Writes 11 packet files: MAP_7K_MODINFO_MAP_FIELD_PACKET.md,
    MAP_7K_VARIANT_F_RESULT_SUMMARY.md, EXPERIMENT_G install commands (HUMAN-ONLY),
    log capture commands, result template, preflight JSON + MD, inspector outputs.
  - Analyzer command uses -ExpectedMapId and -VariantLabel VariantG.
- scripts/test-build42-map7k-modinfo-map-field-experiment.ps1: 11 assertions.
  - Tests 1-2: path guard, all 11 files present + exit 0.
  - Tests 3-4: root/42 mod.info contain map=<MapId>.
  - Test 5: mod.info files remain no-BOM.
  - Test 6: LOTH/LOTP/chunkdata sizes unchanged (29646/1056780/1026).
  - Tests 7-9: preflight variant_f_result, h5_ruled_out, h8_map_field.
  - Tests 10-11: HUMAN-ONLY marker, public_playable=false.
  - psTotal 911->922.
- scripts/validate.ps1: MAP-7K section; psTotal 911->922; v0.40.
- scripts/write-proof-packet.ps1: v0.40; map7k=11; total 922.
- scripts/test-proof-packet.ps1: assertions updated (11/922, schema v0.40).
- docs/IMPLEMENTATION.md: MAP-7K ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7K section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 922 / .NET 556 (unchanged).

### Added (MAP-7J: Record Variant E metadata discovery failure)
- docs/MAP_7J_VARIANT_E_METADATA_CONTRACT_FAILURE.md: Variant E result record.
  - Variant E: root mod.info + root media/maps + 42/ dual layout.
    MAP7F_VARIANT_E_MAP_FOLDER_SCAN_EMPTY; ROOT_MOD_INFO_EXPERIMENT_FAILED.
  - VARIANTS_ABCDE_EXHAUSTED: A/B/C/D/E layout experiments exhausted.
  - METADATA_CONTRACT_FOCUS: investigation shifts to mod/map metadata shape.
  - Diagnostic distinction: MAP_FOLDER_SCAN_EMPTY (our blocker) vs
    MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING (different stage).
  - Forum evidence: seen in other mods; different from our case.
  - Hypotheses H4-H8 recorded (map.info fields, mod.info id, map= field).
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map7d-load-result.ps1: lotheader detection added.
  - New field: lotheader_files_missing (detects 'Failed to find any .lotheader files').
  - New classification: MAP_FOLDER_SCAN_FOUND_BUT_LOTHEADER_FILES_MISSING
    (triggers when scan non-empty AND lotheader files missing).
  - Does not affect existing A-E classifications (scan is empty in all our cases).
- scripts/inspect-build42-map-metadata-contract.ps1: new inspector.
  - Parses mod.info and map.info from 42/ and root layouts.
  - Extracts key/value fields, raw lines, SHA-256, no-BOM, ASCII status.
  - Detects byte-identity between root/42 copies.
  - Records id field match vs expected ModId/MapId.
  - Adds variant_e_result, metadata_contract_focus, map_line_variants_exhausted.
  - .local/ output guard; refuses PZ user paths.
  - Writes map-metadata-contract-report.json + .md.
- scripts/prepare-build42-map7j-metadata-contract-packet.ps1:
  - .local/ output guard.
  - Generates empty_grass_v4 candidate via CLI.
  - Creates experiment-F dual-layout candidate (same as experiment-E).
  - Runs discovery path inspector and metadata contract inspector.
  - Writes 11 packet files:
    MAP_7J_METADATA_CONTRACT_PACKET.md, MAP_7J_VARIANT_E_RESULT_SUMMARY.md,
    MAP_7J_METADATA_CONTRACT_REPORT.md, MAP_7J_NEXT_HYPOTHESES.md,
    MAP_7J_EXPERIMENT_F_MANUAL_RESULT.local-template.md,
    map7j-metadata-contract-preflight.json, map7j-metadata-contract-preflight.md,
    map-discovery-path-report.json, map-discovery-path-report.md,
    map-metadata-contract-report.json, map-metadata-contract-report.md.
  - Variant F requires human decision; packet does not auto-schedule it.
- scripts/test-build42-map7j-metadata-contract.ps1: 17 assertions.
  - Tests 1-10: inspector path guard, mod.info/map.info id parsing, no-BOM,
    byte-identical detection, id match, JSON/MD output, public_playable=false.
  - Tests 11-17: packet path guard, all 11 files present, variant_e_result,
    metadata_contract_focus, VARIANTS_ABCDE_EXHAUSTED, LOAD_TEST_NOT_PERFORMED,
    public_playable=false.
  - psTotal 894->911.
- scripts/validate.ps1: MAP-7J section; psTotal 894->911; v0.39.
- scripts/write-proof-packet.ps1: v0.39; map7j=17; total 911.
- scripts/test-proof-packet.ps1: assertions updated (17/911, schema v0.39).
- docs/IMPLEMENTATION.md: MAP-7J ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7J section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 911 / .NET 556 (unchanged).

### Added (MAP-7I: Record Variant D root media failure)
- docs/MAP_7I_VARIANT_D_ROOT_MEDIA_FAILURE.md: Variant D result record.
  - Variant D: root media/maps duplicate + 42/media/maps existing.
    MAP7F_VARIANT_D_MAP_FOLDER_SCAN_EMPTY; ROOT_MEDIA_MAPS_ALONE_INSUFFICIENT.
  - Square/blocked visual area observed. NOT proof of map registration.
  - Next hypothesis: root mod.info required to mount root media path.
  - Experiment E defined: root mod.info + root media/maps + 42/ preserved.
  - EXPERIMENT_E_ROOT_MOD_INFO_RECOMMENDED.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map-discovery-path.ps1: updated to v0.2.
  - New fields: has_dual_mod_info_layout, has_dual_media_maps_layout,
    root_mod_info_missing, experiment_d_root_media_maps_result,
    experiment_e_root_mod_info_recommended.
  - Updated map_folder_discovery_risk logic to reflect D finding:
    ROOT_MEDIA_MAPS_WITHOUT_ROOT_MOD_INFO_INSUFFICIENT,
    LOWER_DUAL_FULL_LAYOUT, HIGH_42_VERSION_LAYER_MAY_NOT_BE_SCANNED.
  - Updated markdown output with new fields.
- scripts/prepare-build42-map7i-root-modinfo-experiment-packet.ps1:
  - .local/ output guard.
  - Generates empty_grass_v4 candidate via CLI.
  - Creates dual-layout experiment-E candidate under .local/:
    root mod.info + root media/maps/ + 42/mod.info + 42/media/maps/ all present.
  - Verifies no-BOM on all 8 text files (42/mod.info, root/mod.info,
    42/map.info/spawnpoints/objects, root/map.info/spawnpoints/objects).
  - Runs inspector on experiment-E candidate.
  - Writes 9 packet files:
    MAP_7I_ROOT_MODINFO_EXPERIMENT_PACKET.md,
    MAP_7I_VARIANT_D_RESULT_SUMMARY.md,
    MAP_7I_EXPERIMENT_E_MANUAL_INSTALL_COMMANDS.md (HUMAN-ONLY),
    MAP_7I_EXPERIMENT_E_LOG_CAPTURE_COMMANDS.md,
    MAP_7I_EXPERIMENT_E_MANUAL_RESULT.local-template.md,
    map7i-root-modinfo-preflight.json, map7i-root-modinfo-preflight.md,
    map-discovery-path-report.json, map-discovery-path-report.md.
  - Analyzer command in log capture uses -ExpectedMapId and -VariantLabel VariantE.
- scripts/test-build42-map7i-root-modinfo-experiment.ps1: 12 assertions.
  - Test 1: packet path guard.
  - Tests 2-6: packet exits 0, experiment-E root mod.info, root map.info,
    42/mod.info preserved, 42/map.info preserved.
  - Tests 7-8: no-BOM on root mod.info, root map.info via inspector JSON.
  - Tests 9-10: preflight variant_d_result, experiment_e_root_mod_info.
  - Tests 11-12: HUMAN-ONLY marker, public_playable=false.
  - psTotal 882->894.
- scripts/validate.ps1: MAP-7I section; psTotal 882->894; v0.38.
- scripts/write-proof-packet.ps1: v0.38; map7i=12; total 894.
- scripts/test-proof-packet.ps1: assertions updated (12/894, schema v0.38).
- docs/IMPLEMENTATION.md: MAP-7I ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7I section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 894 / .NET 556 (unchanged).

### Added (MAP-7H: Record Variant BC map discovery failure)
- docs/MAP_7H_VARIANT_BC_AND_DISCOVERY_PATH.md: Variant B/C result record.
  - Variant B: Map=candidate. MAP7F_VARIANT_B_MAP_FOLDER_SCAN_EMPTY.
  - Variant C: Map=Muldraugh,KY;candidate. MAP7F_VARIANT_C_MAP_FOLDER_SCAN_EMPTY.
  - A/B/C comparison table: all produce empty map-folder scan.
  - MAP_LINE_VARIANTS_EXHAUSTED: Map= ordering is not the root cause.
  - Discovery path investigation opened: 42/ version layer may not be scanned.
  - Experiments D/E/F defined as human-only next steps.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map-discovery-path.ps1:
  - Inspects CandidateRoot for versioned (42/) and root media/maps layouts.
  - Parses map.info and mod.info key=value fields.
  - Checks no-BOM for all game-read text files.
  - Reports: has_versioned_42_media_maps, has_root_media_maps,
    root_media_maps_missing, has_root_mod_info,
    possible_build42_version_layer_not_scanned_by_isometagrid,
    map_folder_discovery_risk.
  - .local/ output guard; refuses PZ user/install paths.
  - Writes map-discovery-path-report.json + .md.
- scripts/prepare-build42-map7h-discovery-path-packet.ps1:
  - .local/ output guard.
  - Generates empty_grass_v4 candidate via CLI.
  - Runs inspect-build42-map-discovery-path.ps1 on generated candidate.
  - Writes MAP_7H_DISCOVERY_PATH_PACKET.md.
  - Writes MAP_7H_DISCOVERY_PATH_HYPOTHESES.md (hypotheses H1/H2/H3, experiments D/E/F).
  - Writes MAP_7H_VARIANT_RESULTS_SUMMARY.md (A/B/C comparison).
  - Writes MAP_7H_NEXT_MANUAL_EXPERIMENTS.local-template.md (fillable).
  - Writes map7h-discovery-preflight.json (variants_abc_exhausted=true, public_playable=false).
  - Writes map7h-discovery-preflight.md.
  - Includes map-discovery-path-report.json + .md from inspector.
- scripts/test-build42-map7h-discovery-path.ps1: 12 assertions.
  - Tests 1-6: inspector path guard, versioned layout detection, missing root
    media/maps, root media/maps present, no-BOM correct, BOM detected.
  - Tests 7-12: packet path guard, exits 0, all 8 files present,
    preflight variants_abc_exhausted+public_playable=false,
    LOAD_TEST_NOT_PERFORMED sentinel, root media/maps hypothesis present.
  - psTotal 870->882.
- scripts/validate.ps1: MAP-7H section; psTotal 870->882; v0.37.
- scripts/write-proof-packet.ps1: v0.37; map7h=12; total 882.
- scripts/test-proof-packet.ps1: assertions updated (12/882, schema v0.37).
- docs/IMPLEMENTATION.md: MAP-7H ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7H section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 882 / .NET 556 (unchanged).

### Added (MAP-7G: Record Variant A map registration failure)
- docs/MAP_7G_VARIANT_A_REGISTRATION_FAILURE.md: Variant A result record.
  - Variant A: Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY.
  - MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY: pzmapforge absent from IsoMetaGrid folder list.
  - Muldraugh terrain loaded (not black void), confirming Muldraugh, KY resolves from Map=.
  - Custom candidate does not resolve to a scanned map folder path.
  - Spawn warning persists. No city choice. World forest/grass.
  - Analyzer DebugLog parser fix documented.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map7d-load-result.ps1: real DebugLog prefix fix.
  - Strip-DebugLogPrefix regex updated: '^\[.*?\]\s+\w+\s*:\s+\w+\s+f:\d+[^>]*>\s*'
  - Handles real Build 42 format: [date time.ms] LOG/WARN : category      f:N st:N> msg
  - Handles WARN lines: f:N st:N at Class.method          > msg
  - Handles early startup lines: f:N> msg (no st:N)
  - Handles multiplayer lines: f:N st:N,M,P at class> msg
  - New params: -ExpectedMapId (optional), -VariantLabel (optional).
  - Classification: MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY when ExpectedMapId+VariantLabel+empty scan.
  - VariantLabel->VARIANT_KEY: VariantA->VARIANT_A, VariantB->VARIANT_B.
  - New report fields: expected_map_id, variant_label, variant_classification.
  - Schema bumped to v0.3.
- scripts/test-build42-map7f-registration-diagnostic.ps1: fixtures updated.
  - Updated tsEmptyLog and tsWithFoldersLog to use real f:0 st:0> format.
  - WARN line format updated to real f:0 st:0 at Class.method>.
  - All 11 MAP-7F assertions still pass.
- scripts/prepare-build42-map7f-registration-diagnostic-packet.ps1:
  - MAP_7F_LOG_CAPTURE_AND_ANALYSIS_COMMANDS.md now includes -ExpectedMapId and
    -VariantLabel in the analyzer command examples.
- scripts/test-build42-map7g-variant-a-failure.ps1: 8 assertions.
  - Tests 1-3: real f:0 st:0> empty list, timestamped_debuglog_detected, WARN not counted.
  - Test 4: real format with 2 folder entries -> count=2.
  - Test 5: -ExpectedMapId -VariantLabel VariantA + empty scan -> MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY.
  - Tests 6-7: expected_map_id and variant_label fields populated correctly.
  - Test 8: timeout regression test without new params.
  - psTotal 862->870.
- scripts/validate.ps1: MAP-7G section; psTotal 862->870; v0.36.
- scripts/write-proof-packet.ps1: v0.36; map7g=8; total 870.
- scripts/test-proof-packet.ps1: assertions updated (8/870, schema v0.36).
- docs/IMPLEMENTATION.md: MAP-7G ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7G section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 870 / .NET 556 (unchanged).

### Added (MAP-7F: Diagnose Build 42 map folder registration)
- docs/MAP_7F_MAP_FOLDER_REGISTRATION_DIAGNOSTIC.md: registration diagnostic record.
  - MAP-7E confirmed MAP_FOLDER_SCAN_EMPTY_CONFIRMED: IsoMetaGrid found no map folders.
  - Cleared blockers listed: LexState/BOM/spawn-null/player-data-timeout all cleared.
  - Remaining blocker: IsoMetaGrid map folder discovery finds no folders.
  - Analyzer bug documented and fixed (timestamped DebugLog false-negative).
  - Three Map= variants (A/B/C) defined for next manual retest.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map7d-load-result.ps1: fixed timestamped DebugLog parser.
  - Replaced regex-based map-folder extraction with line-by-line parser.
  - Strips log prefix ([date] LOG : category , timestamp> ) before comparing messages.
  - Strips trailing periods (DebugLog appends them to some messages).
  - Correctly sets map_folders_list_empty=true when markers are adjacent (both formats).
  - New fields: map_folder_lines, map_folder_parser_strategy, timestamped_debuglog_detected.
  - Schema bumped to v0.2.
- scripts/prepare-build42-map7f-registration-diagnostic-packet.ps1:
  - .local/ output guard; refuses paths outside .local/.
  - Writes MAP_7F_REGISTRATION_DIAGNOSTIC_PACKET.md.
  - Writes MAP_7F_MANUAL_RETEST_RECORD.local-template.md.
  - Writes MAP_7F_MAP_LINE_VARIANTS_TO_TEST.md (variants A/B/C with HUMAN-ONLY notes).
  - Writes MAP_7F_LOG_CAPTURE_AND_ANALYSIS_COMMANDS.md.
  - Writes map7f-registration-preflight.json (public_playable_claim_allowed=false).
  - Writes map7f-registration-preflight.md.
  - No PZ folder writes. No PZ execution.
- scripts/test-build42-map7f-registration-diagnostic.ps1: 11 assertions.
  - Tests 1-2: analyzer still detects timeout and LexState (bare format).
  - Test 3: bare format empty list still detected.
  - Tests 4-5: timestamped format correctly sets map_folders_list_empty=true,
    timestamped_debuglog_detected=true.
  - Test 6: timestamped format with folder entries correctly counts > 0.
  - Test 7: timestamped empty map-folder -> PARTIAL_PASS classification.
  - Test 8: MAP-7F packet refuses output outside .local.
  - Tests 9-11: packet exits 0, all 6 files present, preflight has
    public_playable_claim_allowed=false, all Map= variants and HUMAN-ONLY markers.
  - psTotal 851->862.
- scripts/validate.ps1: MAP-7F section; psTotal 851->862; v0.35.
- scripts/write-proof-packet.ps1: v0.35; map7f=11; total 862.
- scripts/test-proof-packet.ps1: assertions updated (11/862, schema v0.35).
- docs/IMPLEMENTATION.md: MAP-7F ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7F section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 862 / .NET 556 (unchanged).

### Added (MAP-7E: Record MAP-7D partial load diagnostics)
- docs/MAP_7E_EMPTY_WORLD_MAP_REGISTRATION_DIAGNOSTICS.md: result record.
  - MAP-7D no-BOM server retest: MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD.
  - Cleared blockers: objects.lua LexState (no-BOM), server spawnregions BOM LexState,
    spawn null, player-data timeout.
  - Remaining: map_folders_list_empty, no city choice, spawn at 150,150,0 (no room/building).
  - 5 diagnostic branches for empty-world/map-registration gap documented.
  - Controlled partial in-game load proof. No public playable claim.
  - LOAD_TEST_NOT_PERFORMED; PUBLIC_PLAYABLE_CLAIM_ALLOWED=false.
- scripts/inspect-build42-map7d-load-result.ps1:
  - .local guard on -Output.
  - Reads log, extracts: candidate_loaded, player_data_received, game_loading_completed,
    entered_ingame_state, exited_ingame_state, lexstate_token2str_found, candidate_objects_lua_error,
    server_spawnregions_lua_error, spawn_region_null_error, timeout_waiting_player_data,
    map_folders_list_empty (regex match between log markers), map_folders_list_count,
    spawn_building_warning_found, mannequin_warning_found.
  - Classifies: MAP7D_LOAD_TEST_PARTIAL_PASS_IN_GAME_EMPTY_WORLD /
    MAP7D_LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA / MAP7D_LOAD_TEST_FAIL_LUA_BOM_OR_LEXSTATE /
    MAP7D_LOAD_TEST_INCONCLUSIVE.
  - Writes map7d-load-result.json + .md.
- scripts/prepare-build42-map7e-diagnostics-packet.ps1:
  - .local guard; generates v4 candidate; verifies no-BOM on all text files.
  - Preflight: no-BOM + binary sizes unchanged.
  - Writes MAP_7E_DIAGNOSTIC_PACKET.md + MANUAL_RETEST_RECORD + OBSERVATION_CHECKLIST +
    SERVER_WIRING_NO_BOM_TEMPLATE (all PZ writes HUMAN-ONLY) + preflight JSON/MD.
  - public_playable_claim_allowed=false in preflight JSON.
- scripts/test-build42-map7e-diagnostics.ps1: 11 assertions.
  - Tests 1-6: analyzer path guard, PARTIAL_PASS, TIMEOUT, LEXSTATE classifications,
    map_folders_list_empty, spawn_building_warning.
  - Tests 7-11: packet path guard, exits 0, required files, public_playable=false,
    HUMAN-ONLY markers in wiring template.
  - psTotal 840->851.
- scripts/validate.ps1: MAP-7E section; psTotal 840->851; v0.34.
- scripts/write-proof-packet.ps1: v0.34; map7e=11; total 851.
- scripts/test-proof-packet.ps1: assertions updated (11/851).
- docs/IMPLEMENTATION.md: MAP-7E ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7E section.

No load test. No binary writer change. No PZ assets. No playable claim.
PS 851 / .NET 556 (unchanged).

---

### Added (MAP-7D: Harden Build 42 candidate Lua encoding)
- docs/MAP_7D_TIMEOUT_AND_LUA_ENCODING_FIX.md: MAP-7C result record.
  - MAP-7C retest: LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA.
  - loth_error=false, lotp_error=false, chunkdata_error=false.
  - objects_lua_error=true (same LexState error), spawn_region_error=true.
  - UTF-8 BOM hypothesis: inspector confirmed EF BB BF in v3 files.
  - MAP-7D fix: empty_grass_v4 uses UTF8Encoding(false) for all game-read text files.
  - OBJECTS_LUA_NO_BOM_FIX_APPLIED; LOAD_TEST_NOT_PERFORMED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- src/PZMapForge.Cli/Program.cs:
  - Profile empty_grass_v4 added; validation accepts v0-v4.
  - gameReadEnc = new UTF8Encoding(false) for v4; Encoding.UTF8 for v0-v3 (unchanged).
  - All game-read text file writes (mod.info, map.info, spawnpoints.lua, objects.lua, README)
    use gameReadEnc → no BOM for v4.
  - objects.lua v4: MAP-7D comment (no BOM). v3 content unchanged.
  - spawnpoints.lua v4: MAP-7D comment + unemployed key (no BOM). v3 content unchanged.
  - text_encoding_strategy, spawnregions_packet_strategy added to report.
  - remaining_unknowns, mdRemainingUnknowns: updated for v4.
- tests/PZMapForge.Cli.Tests/MapExportBuild42CandidateWriterV4ProcessTests.cs: 18 tests.
  - LOTH size 29646 and trailer SHA canonical.
  - No BOM: objects.lua, spawnpoints.lua, mod.info, map.info.
  - Objects comment-only (not return_only). Spawnpoints: unemployed/worldX/Y/posX/Y/Z.
  - Report: profile=v4, text_encoding_strategy, load_tested=false, playable=false.
  - dnCliTests 348->366; dnTotal 538->556.
- scripts/inspect-build42-candidate-lua-metadata.ps1: updated.
  - has_bom field added to inspect-file return (all paths, including early-return and notFound).
  - JSON report: objects_lua_has_bom, mod_info_has_bom, map_info_has_bom, spawnpoints_lua_has_bom.
- scripts/test-build42-candidate-lua-metadata.ps1: 18->21 assertions.
  - Test 19: objects_lua_has_bom field exists in JSON.
  - Test 20: ASCII fixture objects_lua_has_bom == false.
  - Test 21: BOM-encoded mod.info detected → mod_info_has_bom == true.
- scripts/prepare-build42-metadata-v4-load-test-packet.ps1:
  - Generates empty_grass_v4, runs inspector, preflight includes BOM checks.
  - Preflight: no_bom_objects_lua, no_bom_spawnpoints_lua, no_bom_mod_info, no_bom_map_info.
  - Writes MAP_7D_* output files.
- scripts/test-build42-metadata-v4-load-test-packet.ps1: 15 assertions.
  - Path guard, exits 0, 5 output files, preflight fields (profile, no_bom, loth_size),
    packet labels, record LOAD_TEST_FAIL_TIMEOUT_PLAYER_DATA and LOAD_TEST_PASS.
- scripts/validate.ps1: MAP-7D section; MAP-7B 18->21; psTotal 822->840; v0.33.
- scripts/write-proof-packet.ps1: v0.33; map7b=21, map7c=18, map7d=15; total 840; cli=366; .NET 556.
- scripts/test-proof-packet.ps1: assertions updated.
- docs/IMPLEMENTATION.md: MAP-7D ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7D section.

No load test. LOTH/LOTP/chunkdata unchanged. No PZ assets into repo. No playable claim.
PS 840 / .NET 556 (core=190, cli=366).

---

### Added (MAP-7C: Fix Build 42 candidate Lua metadata)
- docs/MAP_7C_OBJECTS_LUA_SPAWN_METADATA_FIX.md: fix doc.
  - MAP-7B basis: LOTH v3 passed, objects.lua failed with LexState.token2str.
  - objects.lua strategy: comment-only (avoids return {} Lua lexer issue from MAP-7A).
  - spawnpoints.lua strategy: unemployed key with worldX/worldY/posX/posY/posZ.
  - LOTH/LOTP/chunkdata: unchanged from v2 (MAP-6Z).
  - Known risk: build42_may_expect_specific_zone_table_format.
  - OBJECTS_LUA_FIXED_COMMENT_ONLY; SPAWNPOINTS_LUA_UNEMPLOYED_KEY;
    LOAD_TEST_NOT_PERFORMED; WRITER_NOT_CHANGED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- src/PZMapForge.Cli/Program.cs:
  - Profile empty_grass_v3 added; validation accepts v0/v1/v2/v3.
  - objects.lua for v3: comment-only placeholder.
  - spawnpoints.lua for v3: unemployed key format with explicit coords.
  - New report fields: lua_metadata_strategy, objects_lua_strategy, objects_lua_known_risk,
    spawnpoints_strategy.
  - loth_entries, lothTrailer, remaining_unknowns: updated for v3.
  - LOTH/LOTP/chunkdata: unchanged from v2.
- tests/PZMapForge.Cli.Tests/MapExportBuild42CandidateWriterV3ProcessTests.cs: 20 tests.
  - LOTH: magic/version/count/size unchanged (29646). Trailer SHA canonical.
  - Objects: not return_only, starts with comment (--).
  - Spawnpoints: unemployed key, worldX/worldY, posX/posY/posZ.
  - Report: profile=v3, lua_metadata_strategy, objects_lua_strategy, load_tested=false.
  - LOTP size unchanged (1056780).
  - dnCliTests 328->348; dnTotal 518->538.
- scripts/inspect-build42-candidate-lua-metadata.ps1: updated.
  - objects.lua type: comment_only added (all non-empty lines start with --).
  - Spawn fields: has_worldX/worldY/posX/posY/posZ/unemployed.
  - Recommendations: objects_lua_recommendation, spawnpoints_lua_recommendation.
- scripts/test-build42-candidate-lua-metadata.ps1: 15->18 assertions.
  - Test 16: comment_only fixture → content_type == comment_only.
  - Test 17: return_only → recommendation contains risky.
  - Test 18: spawnpoints_lua_has_unemployed == true (from updated fixture).
- scripts/prepare-build42-metadata-v3-load-test-packet.ps1:
  - Generates empty_grass_v3 candidate, runs inspector, 24-point preflight.
  - Extra checks: objects_lua_not_return_only, objects_lua_is_comment_only_or_safe,
    spawnpoints_compatible_shape, spawnpoints_has_unemployed.
  - Writes MAP_7C_LOAD_TEST_PACKET.md + RECORD template + WIRING_COMMANDS + preflight JSON/MD.
- scripts/test-build42-metadata-v3-load-test-packet.ps1: 18 assertions.
  - Path guard, exits 0, 5 output files, preflight fields, packet labels,
    record variants, HUMAN-ONLY guards.
- scripts/validate.ps1: MAP-7C section; MAP-7B count 15->18; psTotal 801->822; v0.32.
- scripts/write-proof-packet.ps1: v0.32; map7b=18, map7c=18; total 822; cli=348; .NET 538.
- scripts/test-proof-packet.ps1: assertions updated.
- docs/IMPLEMENTATION.md: MAP-7C ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7C section.

No load test. LOTH/LOTP/chunkdata unchanged. No PZ assets into repo. No playable claim.
PS 822 / .NET 538 (core=190, cli=348).

---

### Added (MAP-7B: LOTH v3 retest result and objects.lua failure record)
- docs/MAP_7B_LOTH_V3_RETEST_OBJECTS_LUA_FAILURE.md: retest record and blocker analysis.
  - MAP-7A manual retest: empty_grass_v2, correct wiring, clean log.
  - LOTH_V3_EOF_NOT_OBSERVED: no lotheader EOF in this run. Prior blocker cleared.
  - ISO_META_GRID_FINISHED_LOADING: 11.728 seconds. Map grid loaded.
  - OBJECTS_LUA_PRIMARY_BLOCKER: LexState.token2str ArrayIndexOutOfBoundsException
    (index 65022, length 31). Current format: return {}. Rejected by PZ Lua engine.
  - SPAWN_REGION_SECONDARY_BLOCKER: NullPointerException in getSpawnRegionsAux.
  - LOAD_TEST_FAIL_OBJECTS_LUA.
  - Interpretation: 4 possible causes for objects.lua failure documented.
  - Recommended next: MAP-7C fix objects.lua and spawn metadata.
  - WRITER_NOT_CHANGED; LOAD_TEST_NOT_PERFORMED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/inspect-build42-candidate-lua-metadata.ps1:
  - Guards: -CandidateRoot and -Output under .local.
  - Detects map_id from mod.info id= field.
  - Inspects: mod.info (id match), map.info (lots match), spawnpoints.lua (function/return
    shape for SpawnPoints compatibility), objects.lua (content type: empty / return_only /
    binary_looking / other_lua).
  - Per file: exists, size, first_bytes_hex, first_lines (20 max), ascii_clean.
  - Sentinels: candidate_files_read=true, pz_assets_read=false, pz_install_read=false.
  - Writes build42-candidate-lua-metadata.json (depth 6) + .md.
- scripts/test-build42-candidate-lua-metadata.ps1: 15 assertions.
  - Synthetic fixtures: mod.info with id=test_map_7b, map.info lots=test_map_7b,
    spawnpoints.lua with function SpawnPoints(), objects.lua with return {}.
  - Tests 1-2: path guards. Tests 3-5: exits 0, JSON+MD exist.
  - Tests 6-9: all 4 candidate files detected as existing.
  - Test 10: objects_lua_content_type == return_only.
  - Test 11: spawnpoints_lua_compatible_shape == true.
  - Tests 12-13: id/lots match == true.
  - Tests 14-15: status labels in MD.
  - psTotal 786->801.
- scripts/validate.ps1: MAP-7B section; psTotal 786->801; v0.31 labels.
- scripts/write-proof-packet.ps1: v0.31; map7b=15; total 801.
- scripts/test-proof-packet.ps1: assertions updated (15/801).
- docs/IMPLEMENTATION.md: MAP-7B ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7B section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 801 / .NET 518.

---

### Added (MAP-7A: Build 42 LOTH v3 load test packet)
- docs/MAP_7A_LOTH_V3_LOAD_TEST_PACKET.md: packet doc.
  - MAP-6Z basis: empty_grass_v2 with 29646-byte LOTH and 1048-byte stable trailer.
  - Diagnostic value table: FAIL_LOTH / FAIL_LOTP / FAIL_CHUNKDATA / FAIL_OBJECTS_LUA / PASS / INCONCLUSIVE.
  - No load test. No writer change. No PZ writes. PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
  - MAP7A_LOTH_V3_LOAD_TEST_PACKET_CREATED; HUMAN_ONLY_COPY_REQUIRED.
- scripts/prepare-build42-loth-v3-load-test-packet.ps1:
  - Guard: -Output under .local.
  - Generates empty_grass_v2 candidate via CLI (--no-build).
  - 24-point preflight: 7 file existence, 8 LOTH binary (magic/version/count/first/last/
    trailer_size/trailer_sha256/total_size), 2 sizes (LOTP/chunkdata), 7 report fields.
  - Trailer SHA-256 computed live and compared to canonical MAP-6Y value.
  - Writes MAP_7A_LOAD_TEST_PACKET.md + RECORD.local-template.md +
    INSTALL_AND_SERVER_WIRING_COMMANDS.md + map7a-preflight.json + map7a-preflight.md.
  - All install/wiring instructions marked HUMAN-ONLY; no automatic PZ folder writes.
  - Preflight JSON: schema map7a-preflight.v0.1, loth_trailer_sha256 field.
- scripts/test-build42-loth-v3-load-test-packet.ps1: 23 assertions.
  - Test 1: Output outside .local refused.
  - Test 2: exits 0. Tests 3-6: 5 output files exist.
  - Test 7: preflight candidate_profile == empty_grass_v2.
  - Test 8: preflight entry_count == 1024.
  - Test 9: preflight loth_size == 29646.
  - Test 10: preflight loth_trailer_size == 1048.
  - Test 11: preflight loth_trailer_sha256 == canonical MAP-6Y value.
  - Test 12: preflight lotp_size == 1056780.
  - Test 13: preflight chunkdata_size == 1026.
  - Tests 14-16: packet status labels.
  - Tests 17-21: record template result variants.
  - Test 22: all Copy-Item in packet marked HUMAN-ONLY.
  - Test 23: no automatic Zomboid writes in packet.
  - psTotal 763->786.
- scripts/validate.ps1: MAP-7A section (doc/script/test + sentinel checks); psTotal 763->786.
- scripts/write-proof-packet.ps1: v0.30; map7a=23; total 786.
- scripts/test-proof-packet.ps1: assertions updated (23/786).
- docs/IMPLEMENTATION.md: MAP-7A ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-7A section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 786 / .NET 518.

---

### Added (MAP-6Z: Build 42 LOTH v3 stable literal trailer writer)
- docs/MAP_6Z_LOTH_V3_STABLE_LITERAL_WRITER.md: writer doc.
  - MAP-6Y basis: 80 Dru_map simple cells, all_1048_blocks_identical=true.
  - v3 LOTH structure: 12-byte header + 28586-byte ASCII table + 1048-byte stable trailer.
  - Total LOTH size: 29646 bytes.
  - Canonical trailer: first two U32LE = 8, rest zero. SHA-256: 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7.
  - LOTP and chunkdata: unchanged.
  - objects.lua secondary parse issue: still pending.
  - Recommended next: MAP-7A controlled LOTH v3 load-test packet and retest.
  - BUILD42_LOTH_V3_STABLE_LITERAL_WRITER_IMPLEMENTED; LOAD_TEST_NOT_PERFORMED;
    PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- src/PZMapForge.Cli/Program.cs:
  - Profile empty_grass_v2 added to Build42CandidateWriterCommand.
  - Validation accepts empty_grass_v0, empty_grass_v1, and empty_grass_v2.
  - empty_grass_v2: same 1024 entries as v1 + BuildMap6yCanonicalTrailer() appended.
  - BuildMap6yCanonicalTrailer(): bytes[0]=0x08, bytes[4]=0x08, rest zero. 1048 bytes.
  - Report fields added: loth_trailer_strategy, loth_trailer_size, loth_trailer_status,
    loth_trailer_sha256.
  - loth_known_risk for v2: stable_reference_block_may_not_match_generated_tile_table_or_cell_payload.
  - remaining_unknowns for v2: includes loth_trailer_acceptance_at_eof.
  - loth_entry_strategy, loth_known_risk, loth_entries_source, remaining_unknowns: made
    dynamic via switch expression (all profiles now accurate in JSON and MD reports).
  - mdRemainingUnknowns: dynamic per profile (fixes v0/v1 MD which had hardcoded v0 list).
  - LOTP and chunkdata: unchanged.
- tests/PZMapForge.Cli.Tests/MapExportBuild42CandidateWriterV2ProcessTests.cs: 28 assertions.
  - Test 1: exits 0. Test 2: 42/ exists. Test 3: lotheader exists.
  - Tests 4-6: magic/version/entry_count.
  - Test 7: 1024 ASCII entries parsed before trailer via FindTrailerStart.
  - Tests 8-9: first/last entry names.
  - Test 10: trailer size == 1048.
  - Test 11: trailer starts at 12 + computed ascii_table_bytes.
  - Test 12: trailer SHA-256 == canonical MAP-6Y value (structure-derived + constant).
  - Test 13: total size == trailerStart + 1048.
  - Tests 14-23: report field assertions.
  - Tests 24-27: LOTP and chunkdata unchanged.
  - Test 28: output outside .local refused.
  - dnCliTests 300->328; dnTotal 490->518.
- scripts/validate.ps1: dnCliTests 300->328; dnTotal 490->518; v0.29 labels.
- scripts/write-proof-packet.ps1: v0.29; cli_tests=328; test_total=518.
- scripts/test-proof-packet.ps1: assertions updated (328/518).
- docs/IMPLEMENTATION.md: MAP-6Z ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6Z section.

No load test. LOTP/chunkdata unchanged. No PZ assets into repo. No playable export claim.
PS 763 / .NET 518 (core=190, cli=328).

---

### Added (MAP-6Y: LOTH fixed 1048-byte block research)
- docs/MAP_6Y_LOTH_FIXED_1048_BLOCK_RESEARCH.md: research doc.
  - MAP-6X basis: per-entry model rejected; all 40 smallest cells have 1048 trailing bytes.
  - MAP-6Y compares the full 1048-byte block byte-by-byte across reference cells.
  - Determines: fully constant / stable prefix + variable body / stable header + zero body /
    cell coordinate fields / variable unknown / not enough reference files.
  - Writer readiness verdict: NOT_DEFENSIBLE until real reference smoke confirms stability.
  - BUILD42_LOTH_FIXED_1048_BLOCK_ANALYSED; WRITER_NOT_DEFENSIBLE;
    WRITER_NOT_CHANGED; LOAD_TEST_NOT_PERFORMED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/analyze-build42-loth-fixed-1048-block.ps1:
  - Guards: ReferenceRoot and Output under .local.
  - Collects *.lotheader files (sorted by size, bounded by MaxFiles).
  - Parses: magic, version u32le, field8 u32le, ASCII string table (offset 12).
  - Selects files where trailing_bytes_count == OnlyTrailingSize (default 1048).
  - Per file: sha256_trailer, zero/nonzero_byte_count, u32_word_count,
    first/last_64_trailer_hex, first/last_32_u32le_words, cell_x/cell_y.
  - Cross-file: SHA-256 deduplication, byte-level stability flags (per-position
    HashSet), stable/variable_byte_count, compute-runs for ranges,
    stable_prefix/suffix_length, U32 word stability, coordinate correlation.
  - Hypotheses: FULLY_CONSTANT / STABLE_PREFIX_VARIABLE_BODY / STABLE_HEADER_ZERO_BODY /
    CELL_COORDINATE_FIELDS / VARIABLE_UNKNOWN / NOT_ENOUGH_REFERENCE_FILES.
  - Writer readiness: NOT_DEFENSIBLE / MAYBE_DEFENSIBLE_WITH_ZERO_1048_BLOCK /
    MAYBE_DEFENSIBLE_WITH_STABLE_LITERAL_1048_BLOCK /
    MAYBE_DEFENSIBLE_WITH_STABLE_PREFIX_ZERO_REMAINDER.
  - Writes build42-loth-fixed-1048-block.json (depth 8) + .md.
- scripts/test-build42-loth-fixed-1048-block.ps1: 20 assertions.
  - Synthetic fixtures: ref1 (3 entries, standard trailer), ref2 (4 entries, same trailer),
    ref3 (5 entries, one byte at position 64 changed to 0xFF).
  - Test 1: ReferenceRoot outside .local refused.
  - Test 2: Output outside .local refused.
  - Test 3: exits 0. Tests 4-5: JSON and MD exist.
  - Test 6: selected_file_count == 3.
  - Test 7: unique_trailer_sha256_count == 2.
  - Test 8: all_1048_blocks_identical == false.
  - Test 9: stable_byte_count == 1047.
  - Test 10: variable_byte_count == 1.
  - Test 11: stable_byte_ranges has 2 ranges.
  - Test 12: variable_byte_ranges has 1 range.
  - Test 13: stable_prefix_length == 64.
  - Test 14: stable_suffix_length == 983.
  - Tests 15-16: hypotheses and writer_readiness present.
  - Tests 17-20: status labels in MD.
  - psTotal 743->763.
- scripts/validate.ps1: MAP-6Y section; psTotal 743->763.
- scripts/write-proof-packet.ps1: v0.28; map6y=20; total 763.
- scripts/test-proof-packet.ps1: assertions updated (20/763).
- docs/IMPLEMENTATION.md: MAP-6Y ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6Y section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 763 / .NET 490.

---

### Added (MAP-6X: LOTH per-entry record model research)
- docs/MAP_6X_LOTH_PER_ENTRY_RECORD_MODEL_RESEARCH.md: research doc.
  - Per-entry model REJECTED for simple cells.
  - Critical: all 40 smallest cells have EXACTLY 1048 trailing bytes (constant).
  - 1048 % 4 = 0 (U32-aligned: 262 words). 32/32 stable prefix bytes.
  - LOTH_TRAILING_BODY_FIXED_SIZE_FOR_SIMPLE_CELLS.
  - MAP-6Y: confirm whether full 1048-byte block is constant.
  - WRITER_NOT_DEFENSIBLE until block content confirmed.
- scripts/analyze-build42-loth-per-entry-record-model.ps1:
  - Guards: ReferenceRoot and Output under .local.
  - Record size scoreboard (4,5,6,7,8,9,10,12,16).
  - Bytes-per-entry ratio; best record size by abs remainder and overhead.
  - Per-record sampling (first 8 records for rs=5/6/7/8).
  - Stability analysis across focus cells (32 prefix byte positions).
  - Cross-file scoreboard and overall hypotheses.
- scripts/test-build42-loth-per-entry-record-model.ps1: 20 assertions.
  - Tests 1-2: path guards. Tests 3-5: exits 0, JSON+MD exist.
  - Tests 6-15: all analysis fields present and populated.
  - Tests 16-19: status labels in MD.
  - psTotal 723->743.
- scripts/validate.ps1: MAP-6X section; psTotal 723->743.
- scripts/write-proof-packet.ps1: v0.27; map6x=20; total 743.
- scripts/test-proof-packet.ps1: assertions updated (20/743).
- docs/IMPLEMENTATION.md: MAP-6X ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6X section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 743 / .NET 490.

---

### Added (MAP-6W: LOTH trailing byte pattern research)
- docs/MAP_6W_LOTH_TRAILING_BYTE_PATTERN_RESEARCH.md: byte-level research doc.
  - MAP-6V basis; why U32 model rejected.
  - Smoke: mod2=10/20, mod4=3/20, avg_entropy=2.657, avg_u16_ratio=0.245.
  - No LP-strings, no compression. HYPOTHESIS_TRAILER_UNKNOWN.
  - WRITER_NOT_DEFENSIBLE. MAP-6X must try per-entry record model.
  - Entropy ~2.66 -> packed small integers (tile IDs, sprite indices, etc.)
- scripts/analyze-build42-loth-trailing-byte-patterns.ps1:
  - Guards: ReferenceRoot and Output under .local.
  - Byte histogram (top 16); entropy; zero/printable/high-bit counts.
  - U16 first-64 analysis; U16/U32 top values; string-index ratio.
  - Length-prefixed string scan (U8 and U16).
  - Compression probe (zlib headers, gzip magic).
  - Structural markers (newlines, CRLF, first/last nonzero offset).
  - Focus-file stability analysis.
  - Per-file and overall hypotheses + writer_readiness + next_step.
- scripts/test-build42-loth-trailing-byte-patterns.ps1: 20 assertions.
  - Tests 1-2: path guards. Tests 3-5: exits 0, JSON+MD exist.
  - Tests 6-15: all major analysis fields present and populated.
  - Tests 16-19: status labels in MD.
  - psTotal 703->723.
- scripts/validate.ps1: MAP-6W section; psTotal 703->723.
- scripts/write-proof-packet.ps1: v0.26; map6w=20; total 723.
- scripts/test-proof-packet.ps1: assertions updated (20/723).
- docs/IMPLEMENTATION.md: MAP-6W ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6W section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 723 / .NET 490.

---

### Added (MAP-6V: LOTH trailing body decode research)
- docs/MAP_6V_LOTH_TRAILING_BODY_DECODE_RESEARCH.md: decode research doc.
  - MAP-6U basis; trailing body 7018-33558 bytes, 20/20 files.
  - Smoke: 17/20 NOT u32-aligned; 3/20 u32-aligned; HYPOTHESIS_TRAILER_UNKNOWN.
  - v3 writer NOT defensible; MAP-6W must deepen analysis before writing.
  - BUILD42_LOTH_TRAILING_BODY_DECODED; HYPOTHESIS_TRAILER_UNKNOWN;
    WRITER_NOT_CHANGED; LOAD_TEST_NOT_PERFORMED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/decode-build42-loth-trailing-body.ps1:
  - Guards: ReferenceRoot and Output under .local.
  - Reads full file bytes (bounded by MaxBytesPerFile).
  - Parses ASCII region; records trailing_bytes_count/mod4/u32_count.
  - Analyses: zero/nonzero words, words < field8, words in coordinate ranges.
  - Emits per-file and overall hypotheses.
  - Smoke 20 Dru_map: 3/20 u32-aligned; 6/20 references string table; UNKNOWN overall.
- scripts/test-build42-loth-trailing-body-decode.ps1: 17 assertions.
  - Tests 1-2: path guards. Tests 3-5: exits 0, JSON+MD exist.
  - Tests 6-10: magic/ascii/trailing/mod4/u32 fields.
  - Tests 11-12: HYPOTHESIS_TRAILER_U32_RECORDS + REFERENCES_STRING_TABLE.
  - Tests 13-16: status labels in MD.
  - psTotal 686->703.
- scripts/validate.ps1: label drift fixed (.NET 465->490 in header); MAP-6V section; psTotal 686->703.
- scripts/write-proof-packet.ps1: v0.25; map6v=17; total 703.
- scripts/test-proof-packet.ps1: assertions updated (17/703).
- docs/IMPLEMENTATION.md: MAP-6V ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6V section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 703 / .NET 490.

---

### Added (MAP-6U: LOTH v2 failure record and full LOTH body research)
- docs/MAP_6U_LOTH_V2_FAILURE_AND_FULL_BODY_RESEARCH.md: failure record.
  - MAP-6T v1 retest: LOAD_TEST_FAIL_LOTH; IsoLot.readInt EOF same as MAP-6Q.
  - Scale alone insufficient; hypothesis: LOTH trailing binary body missing.
  - MAP6T_CLEAN_V1_LOAD_TEST_RECORDED; EMPTY_GRASS_V1_LOTHEADER_REJECTED;
    CURRENT_CANDIDATE_LOTHEADER_EOF; LOTP_NOT_REACHED; CHUNKDATA_NOT_REACHED;
    OBJECTS_LUA_SECONDARY_PARSE_ERROR_OBSERVED; LOAD_TEST_FAIL_LOTH;
    WRITER_NOT_CHANGED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/inspect-build42-loth-full-body.ps1:
  - Guards: ReferenceRoot and Output under .local.
  - Reads FULL file bytes per *.lotheader.
  - Parses ASCII region from offset 12; finds where it ends.
  - Records trailing_bytes_count, first_64_trailing_bytes_hex, trailing_u32le_words.
  - Hypothesis: LOTH_REQUIRES_TRAILING_BINARY_BODY if all files have trailing.
  - Smoke 20 Dru_map files: hypothesis confirmed; 20/20 have trailing;
    min=7018 bytes, max=33558 bytes; field8 exactly matches ascii_entry_count.
- scripts/test-build42-loth-full-body.ps1: 14 assertions.
  - Tests 1-2: path guards. Tests 3-5: exits 0, JSON+MD exist.
  - Tests 6-10: magic, field8, ASCII region, trailing bytes detected.
  - Test 11: hypothesis LOTH_REQUIRES_TRAILING_BINARY_BODY confirmed.
  - Tests 12-14: status labels in MD.
  - psTotal 672->686.
- scripts/validate.ps1: MAP-6U section; psTotal 672->686.
- scripts/write-proof-packet.ps1: v0.24; map6u=14; total 686.
- scripts/test-proof-packet.ps1: assertions updated (14/686).
- docs/IMPLEMENTATION.md: MAP-6U ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6U section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 686 / .NET 490.

---

### Added (MAP-6T: Build 42 LOTH v2 load test packet)
- docs/MAP_6T_LOTH_V2_LOAD_TEST_PACKET.md: packet doc.
  - MAP-6S basis; what the packet prepares; diagnostic value table.
  - MAP6T_LOTH_V2_LOAD_TEST_PACKET_CREATED; HUMAN_ONLY_COPY_REQUIRED;
    LOAD_TEST_NOT_PERFORMED; WRITER_NOT_CHANGED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/prepare-build42-loth-v2-load-test-packet.ps1:
  - Guard: -Output under .local.
  - Generates empty_grass_v1 candidate via CLI.
  - 20-point preflight: 7 file existence, 6 LOTH binary, 2 sizes, 5 report flags.
  - Writes MAP_6T_LOAD_TEST_PACKET.md, RECORD.local-template.md,
    INSTALL_AND_SERVER_WIRING_COMMANDS.md, map6t-preflight.json, map6t-preflight.md.
  - All copy/wiring instructions marked HUMAN-ONLY; no PZ writes by script.
- scripts/test-build42-loth-v2-load-test-packet.ps1: 18 assertions.
  - Test 1: Output outside .local refused.
  - Test 2: exits 0; Tests 3-6: all 4 output files + JSON exist.
  - Tests 7-10: preflight JSON entry_count/loth/lotp/chunkdata sizes.
  - Tests 11-13: packet status labels.
  - Tests 14-16: record template result variants.
  - Tests 17-18: no automatic Copy-Item or Set-Content to PZ paths.
  - psTotal 654->672.
- scripts/validate.ps1: MAP-6T section; psTotal 654->672.
- scripts/write-proof-packet.ps1: v0.23; map6t=18; total 672.
- scripts/test-proof-packet.ps1: assertions updated (18/672).
- docs/IMPLEMENTATION.md: MAP-6T ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6T section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 672 / .NET 490.

---

### Added (MAP-6S: Build 42 LOTH candidate writer v2)
- docs/MAP_6S_BUILD42_LOTH_WRITER_V2.md: writer doc.
  - MAP-6Q failure basis; MAP-6R structure basis.
  - empty_grass_v1: 1024 generated contiguous entries (blends_grassoverlays_01_0..._01_1023).
  - Entries generated in source; not copied from any reference mod.
  - Why Dru_map entries not embedded.
  - LOTP/chunkdata unchanged; LOAD_TEST_NOT_PERFORMED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
  - Recommended next: MAP-6T controlled retest.
- src/PZMapForge.Cli/Program.cs: empty_grass_v1 profile added.
  - Profile validation accepts empty_grass_v0 and empty_grass_v1.
  - empty_grass_v1: 1024 entries via Enumerable.Range(0,1024).
  - Report: loth_entry_strategy, loth_known_risk, updated remaining_unknowns.
  - LOTP and chunkdata unchanged.
- tests/PZMapForge.Cli.Tests/MapExportBuild42CandidateWriterV1ProcessTests.cs: 25 tests.
  - Exits 0; 42/ layout; LOTH exists; LOTH magic; LOTH version=1.
  - entry_count=1024; 1024 entries; first entry; last entry; size>38; size>=25000.
  - Report profile/entry_count/loth_status/load_tested/playable/pz_assets/known_risk.
  - LOTP+chunkdata sizes unchanged; output outside .local refused.
  - dnCliTests 275->300; dnTotal 465->490.
- Smoke: v1 candidate 28598 bytes vs smallest Dru_map ref 34920 bytes (21% gap).
  candidate_smaller_than_all_references=true; candidate_magic=LOTH.
- scripts/validate.ps1: .NET count constants updated (300/490); psTotal unchanged.
- scripts/write-proof-packet.ps1: v0.22; dnCliTests=300; dnTotal=490.
- scripts/test-proof-packet.ps1: assertions updated (300/490).
- docs/IMPLEMENTATION.md: MAP-6S ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6S section.

No load test. No PZ assets into repo. No reference entry copying. No playable export claim.
PS 654 / .NET 490.

---

### Added (MAP-6R: Build 42 LOTH structure research)
- docs/MAP_6R_BUILD42_LOTH_STRUCTURE_RESEARCH.md: research doc.
  - Why 38-byte LOTH is insufficient.
  - How MAP-6R results guide MAP-6S.
  - Status labels: BUILD42_LOTH_STRUCTURE_INSPECTED etc.
- scripts/inspect-build42-loth-structure.ps1:
  - Guards: ReferenceRoot and Output under .local.
  - Reads bounded prefix (default 512 bytes) per *.lotheader.
  - Extracts: magic, version, field8, u32le_words_first_128, null/newline count,
    first_printable_offset, ascii_lines_from_offset_12, binary_gap_before_ascii.
  - Smoke (20 Dru_map files): binaryGap=False (bytes 12+ immediately ASCII);
    field8=920-2007; candidate field8=1 grossly insufficient.
  - No binary section between 12-byte header and string table confirmed.
- scripts/test-build42-loth-structure.ps1: 14 assertions.
  - Tests 1-2: path guards.
  - Tests 3-5: exits 0; JSON+MD exist.
  - Tests 6-7: magic LOTH + version 1 detected.
  - Tests 8-10: first_printable_offset, newline_count, u32le_words recorded.
  - Tests 11-14: status labels in MD.
  - psTotal 640->654.
- scripts/validate.ps1: MAP-6R section; psTotal 640->654.
- scripts/write-proof-packet.ps1: v0.21; map6r=14; total 654.
- scripts/test-proof-packet.ps1: assertions updated (14/654).
- docs/IMPLEMENTATION.md: MAP-6R ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6R section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 654 / .NET 465.

---

### Added (MAP-6Q: Spawn activation fixed; candidate lotheader EOF failure)
- docs/MAP_6Q_SPAWN_FIXED_LOTHEADER_EOF_FAILURE_RECORD.md: failure record.
  - Spawn wiring fixed: spawnregions.lua, Mods=, Map=, server _spawnregions.lua.
  - Retest: java.io.EOFException at IsoLot.readInt on 0_0.lotheader.
  - SPAWN_ACTIVATION_WIRING_FIXED; CANDIDATE_MAP_FILES_EXERCISED;
    CURRENT_CANDIDATE_LOTHEADER_EOF; LOTHEADER_STRUCTURE_REJECTED;
    LOTP_NOT_REACHED; CHUNKDATA_NOT_REACHED; LOAD_TEST_FAIL_CURRENT_CANDIDATE;
    WRITER_NOT_CHANGED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/compare-build42-lotheader-candidate.ps1:
  - Guards: CandidateLotheader, ReferenceRoot, Output all under .local.
  - Reads candidate + reference *.lotheader bounded prefixes (128 bytes max).
  - Reports: size, magic, version, field8, u32le_words_first_64.
  - Comparison: candidate_smaller_than_all_references, stable word summary.
  - Status labels: BUILD42_LOTHEADER_CANDIDATE_COMPARISON_CREATED etc.
  - Smoke: candidate 38 bytes, min reference 34920 bytes; candidate_smaller=true.
- scripts/test-build42-lotheader-candidate-comparison.ps1: 13 assertions.
  - Tests 1-3: path guards (candidate/reference/output outside .local refused).
  - Tests 4-6: exits 0; JSON and MD exist.
  - Tests 7-8: candidate+reference magic LOTH detected.
  - Test 9: candidate_smaller_than_all_references=true.
  - Tests 10-13: status labels in MD.
  - psTotal 627->640.
- scripts/validate.ps1: MAP-6Q section; psTotal 627->640.
- scripts/write-proof-packet.ps1: v0.20; map6q=13; total 640.
- scripts/test-proof-packet.ps1: assertions updated (13/640).
- docs/IMPLEMENTATION.md: MAP-6Q ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6Q section.

No load test by Claude. No writer change. No PZ assets into repo. No playable export claim.
PS 640 / .NET 465.

---

### Added (MAP-6P: Clean retest result and spawn activation gap)
- docs/MAP_6P_CLEAN_RETEST_SPAWN_ACTIVATION_RECORD.md: MAP-6O clean retest recorded.
  - BUILD42_CANDIDATE_MOD_LOAD_CONFIRMED: current_candidate_matches=2, no exception.
  - VANILLA_WORLD_ENTRY_WITH_CANDIDATE_ENABLED: vanilla world entered with candidate mod.
  - CANDIDATE_SPAWN_REGION_NOT_VISIBLE: only vanilla cities on spawn screen.
  - CANDIDATE_MAP_CELL_NOT_PROVEN_LOADED: binary files not exercised.
  - LOAD_TEST_INCONCLUSIVE; WRITER_NOT_CHANGED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- docs/MAP_6P_SPAWN_ACTIVATION_DIAGNOSTIC_PROTOCOL.md: human-only diagnostic steps.
  - Verify mod layout, mod.info id, spawnregions.lua, server ini Map= line,
    server _spawnregions.lua reference. All read-only manual commands.
- scripts/prepare-map6p-spawn-activation-diagnostic.ps1:
  - Guards: -Output under .local; forbidden paths refused.
  - Emits MAP_6P_SPAWN_ACTIVATION_DIAGNOSTIC_COMMANDS.md and RECORD template.
  - Does NOT read PZ folders. ASCII-encoded output. No PZ writes.
- scripts/test-map6p-spawn-activation-diagnostic.ps1: 12 assertions.
  - psTotal 612 + 12 = partial; see MAP-6O fix below for full total.
- MAP-6O checklist encoding bugs fixed:
  - scripts/prepare-map6o-clean-retest-checklist.ps1: triple-backtick fences
    now use $fence variable (fixes ```text -> backtick+tab+ext bug). Em-dash
    replaced with ASCII --. Output files use ASCII encoding (no BOM/non-ASCII).
  - scripts/test-map6o-clean-retest-checklist.ps1: 12 -> 15 assertions:
    Test13 (ASCII-clean byte check x2), Test14 (proper fenced text block).
  - psTotal 612 + 3 (MAP-6O) + 12 (MAP-6P) = 627.
- scripts/validate.ps1: MAP-6P section + psTotal 612->627.
- scripts/write-proof-packet.ps1: v0.19; map6p=12, map6o=15; total 627.
- scripts/test-proof-packet.ps1: assertions updated (12/15/627).
- docs/IMPLEMENTATION.md: MAP-6P ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6P section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 627 / .NET 465.

---

### Added (MAP-6O: Clean isolated Build 42 candidate retest protocol)
- docs/MAP_6O_CLEAN_ISOLATED_CANDIDATE_RETEST_PROTOCOL.md: human-only retest protocol.
  - Pre-clean: delete stale maptest_a folders, disable other mods, fresh console.txt.
  - Install: operator manual copy (not automated) from .local to PZ mods.
  - Verify: 7 required files checklist before launch.
  - Test sequence: mod selection, spawn region, world load.
  - Post-test: fresh log capture to .local, triage tool reference.
  - CLEAN_ISOLATED_RETEST_PROTOCOL_CREATED; HUMAN_ONLY_COPY_REQUIRED;
    LOAD_TEST_NOT_PERFORMED; WRITER_NOT_CHANGED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/prepare-map6o-clean-retest-checklist.ps1:
  - Guards: -CandidateSource and -Output under .local; forbidden Zomboid paths refused.
  - Verifies 7 required candidate source files (mod.info, map.info, spawnpoints.lua,
    objects.lua, lotheader, lotpack, chunkdata).
  - Writes MAP_6O_CLEAN_RETEST_CHECKLIST.md, RECORD.local-template.md,
    TRIAGE_COMMANDS.md under Output.
  - No files copied to PZ. LOAD_TEST_NOT_PERFORMED.
- scripts/test-map6o-clean-retest-checklist.ps1: 12 assertions.
  - Test 1: CandidateSource outside .local refused.
  - Test 2: Output outside .local refused.
  - Tests 3-10: valid source exits 0; 3 output files exist; checklist has
    HUMAN_ONLY_COPY_REQUIRED, LOAD_TEST_NOT_PERFORMED, PLAYABLE_EXPORT_CLAIM_ALLOWED=false,
    candidate ID.
  - Test 11: no automatic Copy-Item to Zomboid mods.
  - Test 12: record template contains LOAD_TEST_INCONCLUSIVE.
  - psTotal 600->612.
- scripts/validate.ps1: label drift fixed (v0.16->v0.17 in final summary labels);
  MAP-6O section added; psTotal 600->612.
- scripts/write-proof-packet.ps1: v0.18; map6o_retest_checklist_tests=12; total 612.
- scripts/test-proof-packet.ps1: assertions updated (12/612).
- docs/IMPLEMENTATION.md: MAP-6O ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6O section.

No load test. No writer change. No PZ assets into repo. No playable export claim.
PS 612 / .NET 465.

---

### Added (MAP-6N: Preliminary Build 42 candidate load test record)
- docs/MAP_6N_PRELIMINARY_CANDIDATE_LOAD_TEST_RECORD.md: inconclusive load test record.
  - Candidate: pzmapforge_build42_candidate_001 (MAP-6L/MAP-6M source).
  - BUILD42_CANDIDATE_MOD_LOAD_LOGGED: loading line confirmed in console.txt.
  - MANUAL_TEST_ABORTED_OR_CRASHED_AT_MOD_SELECTION: PZ crashed or returned to menu.
  - CURRENT_CANDIDATE_ERROR_LOG_NOT_FOUND: no current-candidate LOTP/LOTH/IsoLot stack trace.
  - STALE_MAPTEST_A_LOGS_EXCLUDED: maptest_a EOFException traces (MAP-6B path) excluded.
  - LOAD_TEST_INCONCLUSIVE; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/extract-map6n-current-candidate-log-evidence.ps1: .local-only log triage.
  - Guards: -InputLogFolder and -Output both under .local; all other paths refused.
  - Searches current candidate patterns: pzmapforge_build42_candidate_001 + technical terms.
  - Separately counts stale pzmapforge_manual_b42_001_maptest_a matches (excluded).
  - Outputs map6n-log-triage-report.json + .md with:
    current_candidate_matches, stale_maptest_a_matches,
    candidate_specific_exception_found, result_recommendation.
  - result_recommendation=LOAD_TEST_INCONCLUSIVE unless current candidate stack trace found.
  - No load test. No PZ assets. No writer change.
- scripts/test-extract-map6n-log-evidence.ps1: 12 assertions.
  - Test 1: current candidate loading + no exception => LOAD_TEST_INCONCLUSIVE (4 assertions).
  - Test 2: stale maptest_a exception does not flip result (3 assertions).
  - Test 3: current candidate IsoLot exception => candidate_specific_exception_found=true (3 assertions).
  - Test 4: paths outside .local refused (2 assertions).
  - psTotal 588->600.
- scripts/validate.ps1: MAP-6N sentinel (doc + script + test existence + sentinels + test run); psTotal 588->600.
- scripts/write-proof-packet.ps1: map6n_log_triage_tests=12; total 600.
- scripts/test-proof-packet.ps1: assertions updated (12/600).
- docs/IMPLEMENTATION.md: MAP-6N ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6N section.

No load test performed. No writer change. No PZ assets into repo. No playable export claim.
PS 600 / .NET 465.

---

### Added (MAP-6M: Build 42 candidate load test packet)
- docs/MAP_6M_BUILD42_CANDIDATE_LOAD_TEST_PACKET.md: packet doc.
  - 31 preflight checks; smoke: all PASS; 5 output files described.
  - BUILD42_CANDIDATE_LOAD_TEST_PACKET_CREATED; CANDIDATE_PREFLIGHT_VERIFIED;
    MANUAL_LOAD_TEST_REQUIRED; LOAD_TEST_NOT_PERFORMED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/prepare-build42-candidate-load-test-packet.ps1:
  - Guards: Source/Output under .local; forbidden paths refused.
  - Reads MAP-6L report JSON (schema + 11 safety flag checks).
  - Requires 8 files in 42/ versioned layout.
  - LOTH binary: magic=LOTH, version=1, entry_count>=1.
  - LOTP binary: magic=LOTP, version=1, chunk_count=1024, offsets 8204/9228, size=1056780.
  - Chunkdata: size=1026, header=0001, body all-zero.
  - Outputs: BUILD42_CANDIDATE_PREFLIGHT.json, PACKET.md, RECORD.local-template.md,
    pzmapforge_candidate_spawnregions.lua, INSTALL_COPY_COMMANDS_README.txt.
  - No files copied to PZ. No load test.
- scripts/test-build42-candidate-load-test-packet.ps1: 20 assertions.
  - Path guards, 5 output files, 3 preflight JSON safety flags, 3 binary checks,
    copy dest present but not executed, PZMapForge Candidate Cell, 3 result choices,
    no playable claim, PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
  - psTotal 568→588.
- scripts/validate.ps1: MAP-6M sentinel (6 checks + test run); psTotal 568→588.
- scripts/write-proof-packet.ps1: build42_candidate_packet_tests=20; total 588.
- scripts/test-proof-packet.ps1: assertions updated (20/588).
- docs/IMPLEMENTATION.md: MAP-6M ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6M section.

No files copied to PZ. No load test. No PZ assets into repo. No playable export claim.
PS 588 / .NET 465 unchanged.

---

## [Unreleased]

### Added (MAP-6L: Build 42 candidate writer MVP)
- docs/MAP_6L_BUILD42_CANDIDATE_WRITER_MVP.md: candidate writer doc.
  - Exact bytes: LOTH(38b, LOTH magic+1 entry), LOTP(1056780b, LOTP+1024 zero chunks), chunkdata(1026b).
  - Entry source: blends_grassoverlays_01_0 from committed MAP-4E evidence only.
  - Smoke: first_offset=8204/monotonic/unique_sizes=1/all_zero confirmed.
  - Remaining unknowns: lotp_zero_payload/loth_minimum_entries/missing_trailer/build42_load_test.
  - BUILD42_CANDIDATE_WRITER_IMPLEMENTED; LOAD_TEST_NOT_PERFORMED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- src/PZMapForge.Cli/Program.cs:
  - --build42-candidate-writer flag added to map-export-experimental.
  - --build42-candidate-profile flag (default empty_grass_v0).
  - Build42CandidateWriterCommand: versioned 42/ layout; all-zero chunkdata(1026b);
    LOTH lotheader(38b, LOTH+ver+1 entry); LOTP lotpack(1056780b, LOTP+1024 zero chunks).
  - Report JSON: build42_candidate_writer/writer_implemented/load_tested=false/
    playable_export_generated=false/playable_export_claimed=false + full layout fields.
- tests/PZMapForge.Cli.Tests/MapExportBuild42CandidateWriterProcessTests.cs: 25 tests.
  - Tests 1-5: exit 0, 42/ dir, lotheader/lotpack/chunkdata exist.
  - Tests 6-8: LOTH magic(4C4F5448), version=1, entry_count matches entries.
  - Tests 9-11: LOTP magic(4C4F5450), version=1, chunk_count=1024.
  - Tests 12-15: offsets (first=8204, second=9228, last=1055756), file size=1056780.
  - Tests 16-18: chunkdata size=1026, header=0001, body all zero.
  - Tests 19-23: report fields (writer_implemented/load_tested/playable/claimed/status).
  - Test 24: output outside .local refused.
  - Test 25: lotp_file_size_expected=1056780 in report.
  - cli_tests 250→275; dnTotal 440→465.
- scripts/validate.ps1: dnCliTests 250→275; dnTotal 440→465.
- scripts/write-proof-packet.ps1: cli_tests 250→275; test_total 440→465.
- scripts/test-proof-packet.ps1: assertions updated (275/465).
- docs/IMPLEMENTATION.md: MAP-6L ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6L section.

No load test performed. No PZ assets into repo. No playable export claim.
PS 568 unchanged. .NET 465 (cli_tests 275).

---

## [Unreleased]

### Added (MAP-6K: Build 42 LOTP payload and LOTH entry research)
- docs/MAP_6K_LOTP_PAYLOAD_AND_LOTH_ENTRY_RESEARCH.md: research findings.
  - LOTP: first_offset=8204 confirmed; most_common_payload_size=1024; unique_sizes variable
    (24-63 per cell, content-dependent); monotonic offsets; tail_bytes 1024-1056.
  - LOTH: parsed_count=declared+1 consistently (off-by-one trailing content pattern);
    smallest entry count=36 (grass overlay subset); entry format confirmed (MAP-4E compatible).
  - Chunkdata: all_zero_body=True for all 3 sampled cells.
  - Recommendation: proceed to MAP-6L MVP writer.
  - WRITER_RESEARCH_ONLY; WRITER_NOT_IMPLEMENTED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/inspect-build42-lotp-payload-windows.ps1:
  - Guards: Source/Output under .local; forbidden paths refused.
  - LOTP: reads full 8192-byte offset table; computes payload sizes, monotonic check,
    most_common_size, unique_sizes, tail_bytes; reads MaxChunksPerCell chunk windows
    at WindowBytes bytes each; SHA256 per window; windows_identical check.
  - LOTH: reads full file; parses newline-delimited string table; records declared/parsed counts.
  - Chunkdata: body zero-check up to 1024 bytes.
  - Outputs build42-lotp-payload-window-report.json+.md.
  - WRITER_NOT_IMPLEMENTED=true; LOAD_TEST_NOT_PERFORMED=true; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/test-build42-lotp-payload-windows.ps1: 20 assertions with synthetic fixtures.
  - Synthetic LOTP: 1024 chunks, 64-byte sequential payloads (73740 bytes total).
  - Synthetic LOTH: LOTH magic + 2 entries (blends_grassoverlays_01_0, blends_natural_01_0).
  - Synthetic chunkdata: 1026 bytes, all-zero body.
  - Tests: path guards, LOTP magic/chunk_count/first_offset/monotonic/windows, LOTH magic/counts/entry,
    chunkdata size/all_zero, safety flags x3, no playable claim.
  - psTotal 548→568.
- scripts/validate.ps1: MAP-6K sentinel (6 checks + test run); psTotal 548→568.
- scripts/write-proof-packet.ps1: build42_lotp_payload_window_tests=20; total 568.
- scripts/test-proof-packet.ps1: assertions updated (20/568).
- docs/IMPLEMENTATION.md: MAP-6K ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6K section.

No writer implemented. No load test. No PZ assets into repo. No playable export claim.
PS 568 / .NET 440 unchanged.

---

## [Unreleased]

### Added (MAP-6J: Build 42 writer contract)
- docs/MAP_6J_BUILD42_WRITER_CONTRACT.md: exact byte-level writer contracts.
  - LOTP: magic(4C4F5450) + version(1) + chunk_count(1024) + offset table (12+1024x8=8204).
  - LOTH: magic(4C4F5448) + version(1) + entry_count(variable) + newline-delimited names.
  - Chunkdata: 1026 bytes = 00 01 header + 1024-byte body.
  - File set: 7 required files under 42/ versioned layout.
  - Explicit unknowns: chunk_payload_format, loth_minimum_entries, lotp_empty_chunk_offsets, chunk_payload_size.
  - Status: BUILD42_WRITER_CONTRACT_CREATED; WRITER_NOT_IMPLEMENTED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- schemas/pzmapforge.build42-writer-plan.v0.1.schema.json: full schema.
  - Required fields: schema, claim_boundary, created_at, map_id, cell, target_layout, geometry_model,
    lotp_contract, loth_contract, chunkdata_contract, file_set_contract, unknowns, safety.
  - Safety fields all const false (writer_implemented/load_test_performed/playable_export_claimed/pz_assets_copied).
  - lotp_contract.magic_ascii const LOTP; loth_contract.magic_ascii const LOTH.
- examples/build42-writer-plan/minimal-empty-cell-writer-plan.json: schema instance.
  - map_id=pzmapforge_build42_candidate_empty_cell, cell=(0,0).
  - geometry_model: chunk_count=1024, cell_size_tiles=256, strongly_supported_not_load_tested.
  - lotp/loth/chunkdata contracts fully filled; all unknowns listed.
  - contract_review_required=true; candidate_plan_ready_for_writer=false.
- scripts/test-build42-writer-contract.ps1: 20 assertions.
  - Tests 1-4: doc exists + three sentinels.
  - Tests 5-6: schema and example exist.
  - Test 7: example parses.
  - Tests 8-16: field values (schema, safety x3, geometry x2, magic x2, size).
  - Tests 17-19: unknowns (chunk_payload_format, loth_minimum_entries, contract_review_required).
  - Test 20: no playable_export_claimed:true in example.
  - psTotal 528→548.
- scripts/validate.ps1: MAP-6J test run added; psTotal 528→548.
- scripts/write-proof-packet.ps1: build42_writer_contract_tests=20; total 548.
- scripts/test-proof-packet.ps1: assertions updated (20/548).
- docs/IMPLEMENTATION.md: MAP-6J ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6J section.

No writer implemented. No load test. No PZ assets into repo. No playable export claim.
PS 548 / .NET 440 unchanged.

---

## [Unreleased]

### Added (MAP-6I: Build 42 format design matrix)
- docs/MAP_6I_BUILD42_FORMAT_DESIGN_MATRIX.md: design matrix doc.
  - MAP-6H result summary; stability observations; candidate writer layouts.
  - LOTP: bytes 0-11 stable (LOTP magic + version=1 + chunk_count=1024).
  - LOTP offset table: 12 + 1024×8 = 8204 bytes before chunk data.
  - LOTH: bytes 0-7 stable (LOTH magic + version=1); word[2]=entry_count variable.
  - LOTH string table: same structure as MAP-4E Build 41 evidence.
  - Chunkdata: 1026 bytes, 2-byte header + 1024-byte body (32x32 zero hypothesis).
  - Recommended next: MAP-6J writer contract.
  - BUILD42_FORMAT_DESIGN_MATRIX_CREATED; WRITER_NOT_IMPLEMENTED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/derive-build42-format-design-matrix.ps1:
  - Reads MAP-6H JSON (schema guard: v0.2 required).
  - Guards: InspectionReport and Output under .local only.
  - LOTP/LOTH word stability: List[object] per position (stable/variable/missing).
  - Word classification: stable_magic (pos 0), stable_version (pos 1), stable/variable_unknown.
  - LOTH word[2] labelled variable_entry_count when variable.
  - Chunkdata body size analysis: body_900/body_1024/other counts; dominant_model.
  - Outputs build42-format-design-matrix.json (depth 8) + .md.
  - WRITER_NOT_IMPLEMENTED=true; PLAYABLE_EXPORT_CLAIM_ALLOWED=false in JSON.
- scripts/test-build42-format-design-matrix.ps1: 13 assertions.
  - Tests 1-2: path guards. Test 3: valid exit 0. Tests 4-5: files written.
  - Tests 6-7: LOTP word[0]=stable_magic, word[1]=stable_version.
  - Tests 8-9: LOTH word[0]=stable_magic, word[1]=stable_version.
  - Test 10: LOTH word[2] variable. Test 11: chunkdata dominant=32x32_1024.
  - Tests 12-13: WRITER_NOT_IMPLEMENTED, PLAYABLE_EXPORT_CLAIM_ALLOWED.
  - psTotal 515→528.
- scripts/validate.ps1: MAP-6I sentinel (6 checks + test run); psTotal 515→528.
- scripts/write-proof-packet.ps1: build42_format_design_matrix_tests=13; total 528.
- scripts/test-proof-packet.ps1: assertions updated (13/528).
- docs/IMPLEMENTATION.md: MAP-6I ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6I section.

No writer implemented. No load test. No PZ assets into repo. No playable export claim.
PS 528 / .NET 440 unchanged.

---

## [Unreleased]

### Added (MAP-6H: Build 42 LOTP LOTH deep reference inspection)
- docs/MAP_6H_BUILD42_LOTP_LOTH_DEEP_INSPECTION.md: deep inspection evidence.
  - LOTP lotpack: magic bytes, version field, deep prefix fields documented.
  - LOTH lotheader: magic 4C 54 5A 48 = "LOTH"; bytes 4-7 = version=1 (Drummondville).
  - Chunkdata body=1024 supports 32x32 chunk grid and 256x256 cell model.
  - BUILD42_256_MODEL_STRONGLY_SUPPORTED when LOTP+LOTH+1024 all present.
  - Why MAP-6G insufficient for writer; what remains unknown.
  - PLAYABLE_EXPORT_CLAIM_ALLOWED=false; GEOMETRY_MODEL_STILL_NOT_LOAD_TESTED.
- scripts/inspect-build42-reference-geometry.ps1 extended (MAP-6H):
  - LOTH magic detection: if bytes 0-3 = 4C 54 5A 48 → lotheader_format=LOTH.
  - LOTH branch records: lotheader_magic/version_field/first_16/32/64_bytes_hex/u32le_words_first_64.
  - Legacy lotheader: adds lotheader_format=legacy, first_64_bytes_hex.
  - LOTP branch: adds first_16/32/64_bytes_hex, u32le_words_first_64.
  - Chunkdata: extends to 32-byte prefix; adds first_32_bytes_hex, u32le_words_first_32.
  - New counters: lotheader_ltz_count.
  - geometry_statuses array: BUILD42_LOTP/LOTH/32X32/256_STRONGLY_SUPPORTED/BUILD41/NOT_LOAD_TESTED.
  - Primary geometry_status: BUILD42_256_MODEL_STRONGLY_SUPPORTED when all three present.
  - Schema bumped to v0.2; ConvertTo-Json -Depth 8.
  - Slice-ToHex and Get-U32Words helpers added.
- scripts/test-build42-reference-geometry-inspector.ps1: Tests 16-23 added:
  - T16-17: LOTP deep fields (first_16/32_bytes_hex) in LOTP record.
  - T18: LOTH/LOTP/chunkdata1024 source exits 0.
  - T19-20: lotheader_format=LOTH, lotheader_magic=LOTH.
  - T21: lotheader_ltz_count >= 1.
  - T22: BUILD42_LOTH_LOTHEADER_FORMAT_OBSERVED in geometry_statuses.
  - T23: BUILD42_32X32_CHUNK_GRID_OBSERVED in geometry_statuses.
  - psTotal 507→515; build42_geometry_inspector_tests 15→23.
- scripts/validate.ps1: MAP-6H sentinel (4 checks); psTotal 507→515.
- scripts/write-proof-packet.ps1: counts updated (23/515).
- scripts/test-proof-packet.ps1: assertions updated (23/515).
- docs/IMPLEMENTATION.md: MAP-6H ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6H section.

No writer implemented. No load test. No PZ assets into repo. No playable export claim.
PS 515 / .NET 440 unchanged.

---

## [Unreleased]

### Added (MAP-6G: Build 42 LOTP lotpack evidence and inspector hardening)
- docs/MAP_6G_BUILD42_LOTP_LOTPACK_EVIDENCE.md: Drummondville reference evidence.
  - world_0_0.lotpack size=1057348; first 4 bytes = 4C 4F 54 50 = LOTP magic.
  - Build 42 confirmed to use new LOTP lotpack format (not old hdrA=900/hdrB=7204).
  - Current experimental writer incompatible with Build 42 LOTP format.
  - BUILD42_LOTP_FORMAT_OBSERVED, LEGACY_900_LOTPACK_HEADER_NOT_APPLICABLE_TO_REFERENCE,
    GEOMETRY_MODEL_STILL_UNVERIFIED, PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/inspect-build42-reference-geometry.ps1 hardened (MAP-6G):
  - LOTP magic detection: if bytes 0-3 = 4C 4F 54 50, set lotpack_format=LOTP.
  - LOTP branch records: lotpack_magic=LOTP, version_field (bytes 4-7), note.
  - No table size computation for LOTP records (prevents Int32 overflow).
  - Legacy branch: [int64] for inferred_table_bytes/inferred_table_end_byte.
  - MD template uses $r['key'] indexer (hashtable-safe, not PSObject.Properties).
  - New counters: lotpack_lotp_count, lotpack_legacy_900_count.
  - Geometry status: BUILD42_LOTP_FORMAT_OBSERVED when LOTP records found.
- scripts/test-build42-reference-geometry-inspector.ps1: Tests 11-15 added:
  - LOTP source exits 0; lotpack_format=LOTP; lotpack_magic=LOTP;
    lotpack_lotp_count>=1; geometry_status=BUILD42_LOTP_FORMAT_OBSERVED.
  - psTotal 502→507; build42_geometry_inspector_tests 10→15.
- scripts/validate.ps1: MAP-6G sentinel section (4 checks); psTotal 502→507.
- scripts/write-proof-packet.ps1: counts updated (15/507).
- scripts/test-proof-packet.ps1: assertions updated (15/507).
- docs/IMPLEMENTATION.md: MAP-6G ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6G section.

No load test performed. No PZ assets copied into repo. No playable export claim.
PS 507 / .NET 440 unchanged.

---

## [Unreleased]

### Added (MAP-6F: Build 42 reference geometry inspector packet)
- docs/MAP_6F_BUILD42_REFERENCE_GEOMETRY_PACKET.md: operator guide; inspector purpose;
  required operator action (manual copy to .local); what inspector does; geometry statuses;
  safety record; REFERENCE_GEOMETRY_OBSERVED; PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- scripts/inspect-build42-reference-geometry.ps1:
  - Args: -Source <.local path>, -Output <.local path>, -MaxFiles (default 20).
  - Guards: Source/Output under .local; refuses Zomboid/Workshop/Server/PZ-install.
  - Scans *.lotheader, world_*.lotpack, chunkdata_*.bin, map.info, mod.info, spawnpoints.lua.
  - For .lotpack: reads 64-byte prefix; parses hdrA/hdrB (U32 LE); derives table entries,
    table bytes, table end; notes model match.
  - For chunkdata: reads 16-byte prefix; body_bytes = size-2; infers chunk_grid_candidate
    (30x30_900 / 32x32_1024 / 16x16_256 / unknown).
  - For .lotheader: reads 32-byte prefix.
  - Geometry status: BUILD42_300_MODEL_SUPPORTED / BUILD42_256_MODEL_SUPPORTED /
    BUILD42_300_MODEL_PARTIALLY_SUPPORTED / BUILD42_GEOMETRY_STILL_UNKNOWN.
  - Writes build42-reference-geometry-report.json + .md.
  - Safety flags: reference_files_copied=false, pz_assets_copied=false,
    playable_export_claimed=false, compiled_writer_implemented=false, load_test_performed=false.
- scripts/test-build42-reference-geometry-inspector.ps1: 10 assertions with synthetic fixtures:
  source-outside-local refused; output-outside-local refused; valid run exits 0;
  JSON written; MD written; hdrA=900 parsed; chunkdata 902b body=900; chunkdata 1026b body=1024;
  reference_files_copied=false; playable_export_claimed=false.
- scripts/validate.ps1: MAP-6F sentinel section (9 checks + test run); psTotal 492→502;
  Build42 geometry inspector tests = 10 added to psChecks.
- scripts/write-proof-packet.ps1: build42_geometry_inspector_tests=10 added; total 492→502.
- scripts/test-proof-packet.ps1: build42_geometry_inspector_tests assertion added; total 502.
- docs/IMPLEMENTATION.md: MAP-6F ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6F section.

No load test performed. No PZ assets. No playable export claim.
PS 502 / .NET 440 unchanged.

---

## [Unreleased]

### Added (MAP-6E: Build 42 geometry model audit)
- docs/MAP_6E_BUILD42_GEOMETRY_MODEL_AUDIT.md: full geometry audit.
  - 300x300 found in: PaletteLoader (RequiredCellWidth/Height=300, RequiredTileSize=32),
    ImageMapForgeParser (RequiredWidth/Height=300), SemanticGrid (comment),
    layer-manifest schema (const: 300), minimal-cell.json (cell_size:300),
    examples/README.md, test code, binary writer values (hdrA=900, chunkdata=902).
  - 256 found in: placeholder PNG bitmap (256x64, not geometry), MaxBytes limit (not geometry),
    chunkdata header as U16 LE = 256 (role unconfirmed, no geometry meaning assigned).
  - Operator observation: Build 42 may use 256x256 (unverified).
  - Binary writer hardcoded values (hdrA=900, 7208, 902) derive from 30x30 chunk
    model (300/10=30), which may be Build 41 convention only.
  - Status labels: GEOMETRY_MODEL_UNVERIFIED, LEGACY_300_ASSUMPTION_AUDITED,
    BUILD42_256_MODEL_OPERATOR_REPORTED, LOAD_TEST_BLOCKED_PENDING_GEOMETRY_DECISION,
    PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
- src/PZMapForge.Cli/Program.cs:
  - geometry_model_status = mismatch_suspected_not_verified added to both report paths.
  - geometry_model_basis = 30x30_chunk_grid_from_300x300_cell_build41_workshop_evidence.
  - target_build42_cell_size = operator_reported_256_unverified.
- tests/PZMapForge.Cli.Tests/MapExportExperimentalProcessTests.cs:
  - Tests 34-35: geometry_model_status, target_build42_cell_size.
  - cli_tests 248→250.
- scripts/validate.ps1: MAP-6E sentinel section (5 checks); dnCliTests 248→250; dnTotal 438→440.
- scripts/write-proof-packet.ps1: counts updated.
- scripts/test-proof-packet.ps1: assertions updated.
- docs/IMPLEMENTATION.md: MAP-6E ratified row.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6E section.

No load test performed. No PZ assets. No playable export claim.
PS 492 unchanged. .NET 440 (cli_tests 250).

---

## [Unreleased]

### Added (MAP-6D: Non-empty lotheader candidate from committed evidence)
- docs/MAP_6D_NONEMPTY_LOTHEADER_CANDIDATE.md: candidate record.
  - Candidate: newline_tileset_table_minimal.
  - Entry source: blends_grassoverlays_01_0 from committed MAP-4E evidence only.
  - Byte layout: version(U32=0) + count(U32LE=1) + "blends_grassoverlays_01_0\n" = 34 bytes.
  - LOTHEADER_CANDIDATE_V2=newline_tileset_table_minimal
  - PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  - generated_not_load_tested
- src/PZMapForge.Cli/Program.cs:
  - BuildLotheaderForCandidate extended: newline_tileset_table_minimal case added.
  - Returns (Bytes, CandidateStatus, EntryCount, Entries) — signature extended.
  - lotheader_first_bytes widened from Take(8) to Take(32) in both paths.
  - Report JSON: lotheader_entry_count, lotheader_entries added (both flat + build42).
  - binary_runtime_status predicate covers newline_tileset_table_minimal.
- tests/PZMapForge.Cli.Tests/MapExportExperimentalProcessTests.cs:
  - Tests 27-33: exit 0, lotheader_candidate, candidate_status, entry_count=1,
    entries includes grass entry, lotheader is 34 bytes, first 8 bytes verified.
- tests/PZMapForge.Cli.Tests/MapExportExperimentalBuild42ProcessTests.cs:
  - Test 13: build42 path with minimal candidate produces 34-byte lotheader.
  - cli_tests 240→248.
- docs/MAP_6C_LOTHEADER_FORMAT_RESEARCH_PACKET.md: v2 candidate added.
- scripts/validate.ps1: MAP-6D sentinel section (4 checks); dnCliTests 240→248; dnTotal 430→438.
- scripts/write-proof-packet.ps1: cli_tests 240→248, test_total 430→438.
- scripts/test-proof-packet.ps1: assertions updated.
- docs/IMPLEMENTATION.md: MAP-6D ratified row added.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6D section added.

No load test performed. No PZ assets. No playable export claim.
PS 492 unchanged. .NET 438 (cli_tests 248).

---

## [Unreleased]

### Added (MAP-6C: Lotheader format research packet and candidate writer gate)
- docs/MAP_6C_LOTHEADER_FORMAT_RESEARCH_PACKET.md:
  - MAP-4E evidence summary for lotheader structure.
  - IsoLot.readInt EOF failure analysis (why 8-byte placeholder fails).
  - Candidate matrix: v0=current_failed (known_failing), v1=newline_tileset_table
    (generated_not_load_tested), v2=not_implemented.
  - objects.lua syntax fix documented (return {} not load-tested).
  - LOTHEADER_CANDIDATE_V0=current_failed, LOTHEADER_CANDIDATE_V1=newline_tileset_table
  - PLAYABLE_EXPORT_CLAIM_ALLOWED=false
- src/PZMapForge.Cli/Program.cs:
  - --lotheader-candidate flag added to map-export-experimental (default=current_failed).
  - BuildLotheaderForCandidate helper method (MAP-4E format model).
  - Report JSON new fields: lotheader_candidate, lotheader_candidate_status,
    lotheader_sha256, lotheader_first_bytes, lotheader_byte_count.
  - binary_runtime_status now candidate-aware: candidate_generated_not_load_tested
    for newline_tileset_table, failing_placeholder_format for current_failed.
  - objects.lua fixed from comment-only to return {} (syntactically valid Lua).
  - objects_lua_runtime_status changed to syntax_candidate_not_load_tested.
  - Applied to both MAP-5A flat layout and MAP-5D build42_workshop paths.
- tests/PZMapForge.Cli.Tests/MapExportExperimentalProcessTests.cs:
  - Tests 21-26: lotheader_candidate (default), lotheader_candidate_status (default),
    lotheader_sha256 (64-char hex), newline_tileset_table binary_runtime_status,
    newline_tileset_table candidate_status, objects.lua contains return {}.
  - cli_tests 234→240.
- scripts/validate.ps1: MAP-6C sentinel section (4 checks); dnCliTests 234→240; dnTotal 424→430.
- scripts/write-proof-packet.ps1: cli_tests 234→240, test_total 424→430.
- scripts/test-proof-packet.ps1: assertions updated.
- docs/IMPLEMENTATION.md: MAP-6C ratified row added.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6C section added.

No load test performed. No PZ assets. No playable export claim.
PS 492 unchanged. .NET 430 (cli_tests 240).

---

## [Unreleased]

### Added (MAP-6B: Build 42 binary format failure record)
- docs/MAP_6B_BINARY_FORMAT_FAILURE_RECORD.md: runtime failure evidence record.
  - Status labels: DISCOVERY_PASS_VERSIONED_LAYOUT, MAP_FILES_DISCOVERED_BY_PZ,
    BINARY_FAILURE_CONFIRMED, OBJECTS_LUA_FAILURE_CONFIRMED,
    PLAYABLE_EXPORT_CLAIM_ALLOWED=false.
  - 0_0.lotheader failed: java.io.EOFException at IsoLot.readInt. Placeholder
    format definitively invalid. Real format research required.
  - CellLoader / IsoCell.PlaceLot: repeated failures downstream of lotheader.
  - objects.lua failed: LuaManager.RunLuaInternal exception. Comment-only invalid.
  - Binary hypothesis table supersedes MAP-6A section 5 PLAUSIBLE entries.
- src/PZMapForge.Cli/Program.cs: runtime status fields added to
  experimental-map-export-report.json (both MAP-5A flat and MAP-5D build42_workshop):
  - binary_runtime_status: failing_placeholder_format
  - lotheader_runtime_status: eof_exception_observed
  - lotpack_runtime_status: unproven_after_lotheader_failure
  - chunkdata_runtime_status: unproven_after_lotheader_failure
  - objects_lua_runtime_status: invalid_or_not_accepted
- tests/PZMapForge.Cli.Tests/MapExportExperimentalProcessTests.cs:
  - Tests 17-20: verify binary_runtime_status, lotheader_runtime_status,
    lotpack_runtime_status, chunkdata_runtime_status fields in report JSON.
  - cli_tests 230→234.
- docs/MAP_6A_BUILD42_VERSIONED_DISCOVERY_PROOF.md: section 5 binary hypothesis
  table superseded by MAP-6B evidence. PLAUSIBLE entries replaced.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6A section updated; MAP-6B section added.
- docs/IMPLEMENTATION.md: MAP-6B ratified row added.
- scripts/validate.ps1: MAP-6B contract section added (4 checks); dnCliTests
  230→234; dnTotal 420→424.
- scripts/write-proof-packet.ps1: cli_tests 230→234, test_total 420→424.
- scripts/test-proof-packet.ps1: assertions updated for new counts.

Previous MAP-6A belief (binary hypotheses PLAUSIBLE) was superseded by MAP-6B
runtime evidence. Placeholder binary format failed. Real format required.
No load test performed. No PZ assets. No playable export claim.
PS 492 unchanged. .NET 424 (cli_tests 234).

---

## [Unreleased]

### Added (MAP-6A: Build 42 versioned discovery proof + spawn-region test packet)
- docs/MAP_6A_BUILD42_VERSIONED_DISCOVERY_PROOF.md: evidence record.
  - Confirmed routes: versioned loose-mod (<mods>/<folder>/42/mod.info) works.
  - Non-working routes: flat, Workshop, unversioned loose-mod.
  - PZ log proof: "loading <mod_id>" confirmed.
  - Spawn-selection reached; only vanilla locations visible.
  - Custom spawn NOT visible (cell registration gap, not binary failure).
  - Binary hypotheses plausible (no crash) but UNTESTED (no spawn confirmed).
  - Required proof list before any playable export claim.
- scripts/prepare-spawn-region-test-packet.ps1:
  - Args: -Source <.local MAP-5D package> -Output <.local dir>.
  - Both -Source and -Output guarded as .local/ only.
  - Generates versioned-mod/<map_id>/42/ layout (confirmed Build 42 path).
  - Generates 3 spawn-coord variant patches:
    - Variant A: cell (0,0) + explicit lots= field
    - Variant B: cell (1,1) matching ModTemplate
    - Variant C: cell (25,15) matching RED-Speedway coordinate range
  - Writes SPAWN_REGION_TEST_PACKET.md (background, versioned copy dest,
    variant instructions, regeneration commands for binary variants).
  - Writes SPAWN_REGION_TEST_RECORD.local-template.md.
  - Does NOT copy to PZ mods folder.
- docs/MAP_5C_BUILD42_MOD_PACKAGING_DISCOVERY.md: header note updated.
- docs/MAP_EXPORT_CONTRACT.md: MAP-6A section added.
- docs/IMPLEMENTATION.md: MAP-6A ratified row added.

MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.
Custom spawn location not yet visible. No playable export claim.
No files copied to PZ folders. PS 492 / .NET 420 unchanged.

---

## [Unreleased]

### Added (MAP-5F: inspector path hardening + Build 42 load-test packet)
- src/PZMapForge.Cli/Program.cs: --package .local/ guard added to
  inspect-build42-experimental-package. The command now refuses --package
  paths that are not under .local/, matching the existing --output guard.
  Existing test for non-existent package updated to use .local/ path.
- tests/PZMapForge.Cli.Tests/InspectBuild42ExperimentalPackageProcessTests.cs:
  Test 11 added: --package outside .local exits nonzero.
- scripts/prepare-build42-load-test-packet.ps1:
  - Args: -Source <.local MAP-5D package dir> -Output <.local output dir>.
  - Both -Source and -Output must be under .local/.
  - Reads embedded experimental-map-export-report.json (auto-discovers map_id).
  - Validates package_layout=build42_workshop, experimental_writer=true.
  - Verifies 12 expected package files are present.
  - Writes BUILD42_LOAD_TEST_PACKET.md:
    - inspect command to verify 21/21 checks PASS
    - step-by-step manual copy instructions (Workshop folder + loose mods fallback)
    - what to observe during the test
  - Writes BUILD42_LOAD_TEST_RECORD.local-template.md:
    - pre-filled with map_id, cell, package_layout from source report
    - observation checklist, result field (PASS/FAIL/INCONCLUSIVE), non-claims
  - Does NOT copy files to PZ folders.
  - Does NOT touch PZ install.
  - Does NOT claim playable export.
- scripts/validate.ps1: cli_tests 229→230, total 419→420.
- scripts/write-proof-packet.ps1 + test-proof-packet.ps1: counts updated.
- docs/MAP_EXPORT_CONTRACT.md: MAP-5F section added.
- docs/IMPLEMENTATION.md: MAP-5F ratified row added.

MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.
No files copied to PZ folders. No PZ assets. No playable export claim.

---

## [Unreleased]

### Added (MAP-5E: Build 42 experimental package self-inspection)
- src/PZMapForge.Cli/Program.cs: inspect-build42-experimental-package command.
  - Args: --package <dir> --output <.local dir>.
  - Reads embedded experimental-map-export-report.json to discover map_id,
    cell_x, cell_y automatically.
  - 21 checks: workshop.txt, preview.png, Contents/mods/ directory,
    report JSON flags (package_layout=build42_workshop, playable_export_generated=false,
    load_tested=false, experimental_writer=true),
    mod.info/category=map/poster.png, map.info/spawnpoints.lua/objects.lua/
    thumb.png/README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt,
    lotheader 8 bytes, lotpack 7208 bytes + header 84030000241c0000,
    chunkdata 902 bytes + header 0001, total file count = 14.
  - Exits 0 if all 21 checks pass; exits 1 if any fail.
  - Writes build42-experimental-package-inspection.json + .md.
  - Safety flags: playable_export_claimed=false, load_tested=false,
    no_files_copied=true, no_pz_assets_read=true.
- tests/PZMapForge.Cli.Tests/InspectBuild42ExperimentalPackageProcessTests.cs:
  10 new tests (valid PASS, all-checks-pass, safety flags, stdout content,
  missing args, bad output, missing package, incomplete package FAIL).
- scripts/validate.ps1: cli_tests 219→229, total 409→419.
- scripts/write-proof-packet.ps1 + test-proof-packet.ps1: counts updated.
- docs/MAP_EXPORT_CONTRACT.md: MAP-5E section added.
- docs/IMPLEMENTATION.md: MAP-5E ratified row added.

MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.
No files copied. No PZ assets. No playable export claim.

---

## [Unreleased]

### Added (MAP-5D: Build 42 experimental package writer)
- src/PZMapForge.Cli/Program.cs: --build42-package flag for map-export-experimental.
  - When set: generates Contents/mods/<id>/ nested layout under
    <output>/<id>_build42_workshop/ matching Build 42 ModTemplate structure.
  - workshop.txt with visibility=private.
  - preview.png and poster.png as 256x64 placeholder PNGs via WritePlaceholderPng
    (System.Drawing, no PZ assets read or copied).
  - Contents/mods/<id>/mod.info with Build 42 fields:
    category=map, modversion=1.0, pzversion=42.0, versionMin=42.0, poster=poster.png.
  - map.info: title+description only (no lots field, matching ModTemplate).
  - spawnpoints.lua, objects.lua, README, thumb.png (64x64 placeholder PNG).
  - Binary files (lotheader/lotpack/chunkdata) unchanged from MAP-5A.
  - Report JSON: package_layout=build42_workshop, playable_export_generated=false,
    load_tested=false, experimental_writer=true.
  - 14 files total in package.
  - WritePlaceholderPng static helper added.
  - When --build42-package not set: original flat layout (MAP-5A) unchanged.
- tests/PZMapForge.Cli.Tests/MapExportExperimentalBuild42ProcessTests.cs: 12 tests:
  - exits 0 and creates package root
  - workshop.txt at root, preview.png at root
  - nested mod.info, mod.info has category=map
  - lotheader 8b, lotpack 7208b, chunkdata 902b at nested paths
  - lotpack first 8 bytes match known header
  - chunkdata first 2 bytes match 0001
  - exactly 14 files under package root
  - report JSON: package_layout=build42_workshop, safety flags
- scripts/validate.ps1: cli_tests 207→219, total 397→409.
- scripts/write-proof-packet.ps1 + test-proof-packet.ps1: counts updated.
- docs/MAP_EXPORT_CONTRACT.md: MAP-5D section added.
- docs/IMPLEMENTATION.md: MAP-5D ratified row added.

MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.
No PZ assets read or copied. No .local files committed. No playable export claim.

---

## [Unreleased]

### Added (MAP-5C: Build 42 mod packaging discovery record)
- docs/MAP_5C_BUILD42_MOD_PACKAGING_DISCOVERY.md: packaging discovery record.
  - MAP-5B result: LOAD_TEST_INCONCLUSIVE (Build 42 packaging/discovery blocker).
  - Attempted paths table with observed behavior.
  - Working reference: `avisibleprobe` flat mod appears in Build 42 Mods screen.
  - ModTemplate structure documented (Contents/mods nested layout).
  - Inspect diagnostic results: PZMapForgeEmptyCellTest has correct structure
    (all 5 expected paths present, correct mod.info fields including category,
    modversion, pzversion, versionMin); mod still did not appear in Mods screen.
  - Blocker identified as Discovery mechanism, not package structure.
  - Binary hypotheses (lotheader/lotpack/chunkdata) remain UNTESTED.
  - Non-claims: MAP-5B is INCONCLUSIVE, not FAIL.
- scripts/inspect-build42-mod-package.ps1: Build 42 package inspector.
  - Args: -PackageRoot <path> -TemplateRoot <path> -Output <.local dir>.
  - Refuses non-.local Output.
  - Reads only: workshop.txt, mod.info, map.info (small text files, max 8KB).
  - Does NOT read .lotheader/.lotpack/.bin.
  - Enumerates file names and sizes (no binary reads).
  - Compares 5 expected Build 42 paths against ModTemplate.
  - Compares mod.info fields between package and template.
  - Writes build42-mod-package-inspection.json + .md.
  - copied_files=false, binary_files_read=false, pz_assets_copied=false.
- docs/MAP_5B_MANUAL_LOAD_TEST_PROTOCOL.md: INCONCLUSIVE result note added.
- docs/MAP_EXPORT_CONTRACT.md: MAP-5C section added.
- docs/IMPLEMENTATION.md: MAP-5C ratified row added.

MAP-5B binary hypotheses remain UNTESTED. No playable export claim.
No PZ install modified. No .local files committed. PS 492 / .NET 397 unchanged.

---

## [Unreleased]

### Added (MAP-5B: experimental map load test record protocol)
- docs/MAP_5B_MANUAL_LOAD_TEST_PROTOCOL.md: manual load test protocol.
  - What is tested (lotheader/lotpack/chunkdata hypotheses).
  - Step-by-step: run preparation script, copy mod folder, launch PZ,
    observe, record result, clean up.
  - Success and failure diagnostic tables.
  - Non-claims: no playable export claim until PASS reviewed.
- docs/examples/manual-load-test/MAP_5B_LOAD_TEST_RECORD_TEMPLATE.md:
  fillable record template.
  - metadata, safety confirmation, observation checklist
  - result: LOAD_TEST_PASS / LOAD_TEST_FAIL / LOAD_TEST_INCONCLUSIVE
  - non-claims section
- scripts/prepare-map-export-experimental-load-test.ps1:
  - Args: -Source <.local MAP-5A output> -Output <.local load-tests dir>.
  - Refuses non-.local Output.
  - Validates MAP-5A source (8 expected files + 3 report flags).
  - Writes MAP_5B_LOAD_TEST_PACKET.md (copy instructions, hypothesis table,
    non-claims, mods destination path).
  - Writes MAP_5B_LOAD_TEST_RECORD.local-template.md (pre-filled with
    map_id, cell, generated_at from source report).
  - Does NOT copy files to PZ mods folder.
  - Does NOT touch PZ install.
  - Does NOT touch repo media/maps.
  - Does NOT claim playable export.
- docs/MAP_EXPORT_CONTRACT.md: MAP-5B section added.
- docs/IMPLEMENTATION.md: MAP-5B ratified row added.

No binary writer changes. No .lotheader/.lotpack/.bin in diff.
No PZ assets. No playable export claim. PS 492 / .NET 397 unchanged.
No proof-packet sync.

---

## [Unreleased]

### Added (MAP-5A: experimental local compiled empty cell writer)
- src/PZMapForge.Cli/Program.cs: map-export-experimental command.
  - Args: --map-id <id> --output <.local dir> [--cell-x <int>] [--cell-y <int>].
  - Authorized by MAP-4H: MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY.
  - Refuses non-.local output, PZ install paths, repo media/maps.
  - Writes exactly 10 files (7 text, 3 binary):
    - mod.info, map.info, spawnpoints.lua, objects.lua (text)
    - README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt (mandatory boundary)
    - <cx>_<cy>.lotheader: 8 bytes (zero header + 0-entry count hypothesis)
    - world_<cx>_<cy>.lotpack: 7208 bytes (hdrA=900, hdrB=7204, all-zero table)
    - chunkdata_<cx>_<cy>.bin: 902 bytes (0x0001 + 900 zero bytes)
    - experimental-map-export-report.json + .md
  - Report flags: playable_export_generated=false, load_tested=false,
    experimental_writer=true, pz_assets_copied=false, manual_load_test_required=true.
  - Boundary README mandatory; all assumptions logged in report.
- tests/PZMapForge.Cli.Tests/MapExportExperimentalProcessTests.cs: 16 new tests:
  - exits 0 and writes expected files
  - lotheader exactly 8 bytes
  - lotpack exactly 7208 bytes
  - chunkdata exactly 902 bytes
  - lotpack first 8 bytes = 84030000241c0000
  - chunkdata first 2 bytes = 0001
  - boundary README contains EXPERIMENTAL OUTPUT phrase
  - boundary README contains Not a playable Project Zomboid map phrase
  - report JSON: playable_export_generated false
  - report JSON: load_tested false
  - report JSON: experimental_writer true
  - exactly 10 files written
  - missing --map-id exits nonzero
  - output outside .local exits nonzero
  - output in media/maps exits nonzero
  - ProjectZomboid install path exits nonzero
- scripts/validate.ps1: $dnCliTests 191→207, $dnTotal 381→397.
- scripts/write-proof-packet.ps1: cli_tests and test_total updated.
- scripts/test-proof-packet.ps1: assertions updated to 207/397.
- docs/MAP_EXPORT_CONTRACT.md: MAP-5A section added.
- docs/IMPLEMENTATION.md: MAP-5A ratified row added.

No load test performed. No playable export claim.
No PZ assets read or copied. No repo media/maps writes.
Boundary README in all generated output sets.
All assumptions logged in report JSON.

---

## [Unreleased]

### Added (MAP-4H: compiled writer decision gate report)
- docs/MAP_4H_COMPILED_WRITER_DECISION_GATE.md: formal decision gate.
  - Evidence summary by artifact type (lotheader, lotpack, chunkdata,
    text metadata).
  - 9 known safe facts (no assumption required).
  - 10 remaining unknowns with severity (HIGH/MEDIUM/LOW).
  - Risk table with 7 items and mitigations.
  - Smallest experimental writer hypothesis:
    - lotheader: 8-byte header + 0-entry string table (blank cell variant).
    - lotpack: 7208-byte file; header + 900-entry zero-offset table;
      assumes offset=0 means "no chunk data."
    - chunkdata_0_0.bin: 902-byte file; 2-byte header + 900 zero bytes;
      matches observed simple grass cell pattern.
  - DECISION: MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY
  - Required safeguards (all mandatory for MAP-5A):
    - CLI command name must include "experimental."
    - Output under .local/ only; refuse PZ install paths and repo media/maps.
    - Boundary README in every generated file set.
    - All writer assumptions logged in output report.
    - No playable export claim until manual load test performed and documented.
    - No PZ assets read or copied.
  - MAP-5A scope boundary (one map, one cell at 0_0).
  - Success/failure diagnostic table.
- docs/MAP_EXPORT_CONTRACT.md: MAP-4H section added with decision summary.
- docs/IMPLEMENTATION.md: MAP-4H ratified row added.

No compiled writer implemented. No binary files written.
No .lotheader/.lotpack/.bin in repo. No playable export claim.
PS 492 / .NET 381 unchanged. No proof-packet sync.

---

## [Unreleased]

### Added (MAP-4G: chunkdata binary pattern evidence probe)
- scripts/inspect-chunkdata-binary-patterns.ps1: chunkdata_*.bin pattern reader.
  - Args: -Path <dir> -Output <.local dir> -MaxFiles (default 10, max 50)
    -MaxBytesPerFile (default 65536, max 1048576).
  - Refuses output outside .local/.
  - Reads ONLY chunkdata_*.bin files. NOT .lotheader. NOT .lotpack.
  - Does NOT write any binary files.
  - Records per-file: first 2/4/8 bytes hex, U16/U32 LE values, zero/nonzero
    byte counts, distinct byte count, top-16 most common bytes, first-32
    nonzero offsets, 16-byte prefix group.
  - Writes chunkdata-binary-pattern-evidence.json + .md with aggregate,
    interpretation notes, and open questions.
  - only_chunkdata_bin_files_read=true, lotheader_files_read=false,
    lotpack_files_read=false, bin_files_written=false.
- scripts/validate.ps1: MAP-4G inline contract section (7 checks).
  PS lane stays 492. No proof-packet sync.
- docs/COMPILED_CELL_FORMAT_EVIDENCE.md:
  - Header updated (MAP-4G, chunkdata patterns).
  - Section 5: chunkdata_*.bin gap row added (OPEN → PARTIAL):
    first2=0001 consistent 16/16; minimum size 902 = 2+900 (exact);
    chunk grid bytes partially observed; extended section unknown.
  - Section 13: extended section and grid semantics unknowns updated.
  - Section 18 added: MAP-4G chunkdata binary pattern evidence.
    - `00 01` first-2-bytes consistent across all 16 files.
    - 902 bytes = 2 (header) + 900 (30×30 chunk grid) — EXACT match.
    - Empty grass cells: all-zero chunk grid, 902 bytes total.
    - Complex cells: nonzero per-chunk flags (0x02/0x03/0x08) + extended data.
    - RED-Speedway: minimum 10202 bytes (no empty cells observed).
- docs/MAP_EXPORT_CONTRACT.md: MAP-4G section added.
- docs/IMPLEMENTATION.md: MAP-4G ratified row added.

Gap advance (PARTIAL only — not CLOSED):
- chunkdata_*.bin format: OPEN → PARTIAL.

Chunk grid byte semantics, extended section format, and PZ load validity
for minimal 902-byte file remain unknown. Writing not permitted.
No files copied. No binary files written. Only chunkdata_*.bin read.
No .lotheader/.lotpack read. No compiled writer. No playable export claim.
PS 492 / .NET 381 unchanged. No proof-packet sync.

---

## [Unreleased]

### Added (MAP-4F: lotpack offset table evidence probe)
- scripts/inspect-lotpack-offset-table.ps1: .lotpack bounded prefix analyser.
  - Args: -Path <dir> -Output <.local dir> -MaxFiles (default 10, max 50)
    -MaxBytesPerFile (default 65536, max 1048576).
  - Refuses output outside .local/.
  - Reads ONLY .lotpack bounded prefixes. NOT .lotheader. NOT .bin.
  - Does NOT read full .lotpack files.
  - Interprets bytes 0-3 and 4-7 as U32 LE header fields.
  - Parses bytes 8+ as candidate U32 and U64 LE table values.
  - Detects alternating-zero U32 pattern and monotonically increasing offsets.
  - Writes lotpack-offset-table-evidence.json + .md with aggregate.
  - only_lotpack_files_read=true, lotheader_files_read=false,
    bin_files_read=false, full_lotpack_files_read=false.
- scripts/validate.ps1: MAP-4F inline contract section (7 checks):
  - script exists, .local refusal, only_lotpack_files_read,
    lotheader_files_read, bin_files_read, full_lotpack_files_read,
    compiled_writer_implemented sentinels.
  PS lane stays 492. No proof-packet sync.
- docs/COMPILED_CELL_FORMAT_EVIDENCE.md:
  - Header updated (MAP-4F, lotpack offsets).
  - Section 5: .lotpack gap refined (900-entry offset table confirmed).
  - Section 13: gap section and chunk data unknowns updated.
  - Section 17 added: lotpack offset table evidence (MAP-4F).
    - hdrA=900 constant (16/16 files, both mods) = 30×30 chunks/cell.
    - hdrB=7204 constant (16/16) = formula exact: 4 + hdrA×8.
    - Bytes 8-7207: 900-entry × 8-byte offset table.
    - Each entry: {0x00000000, absolute_chunk_file_offset_U32}.
    - Offsets monotonically increasing; variable (city) or constant (uniform).
    - Gap section (bytes 7208 to first-chunk-offset): 1204-1432 bytes, unknown.
    - Chunk data format entirely unknown.
- docs/MAP_EXPORT_CONTRACT.md: MAP-4F section added.
- docs/IMPLEMENTATION.md: MAP-4F ratified row added.

Gap advances (PARTIAL only — not CLOSED):
- .lotpack binary format: header+table structure well-supported (PARTIAL).

Chunk data encoding and gap section remain unknown. Writing not permitted.
No files copied. Only .lotpack bounded prefixes read. No .lotheader/.bin.
No full .lotpack files read. No compiled writer. No playable export claim.
PS 492 / .NET 381 unchanged. No proof-packet sync.

---

## [Unreleased]

### Added (MAP-4E: lotheader string table evidence probe)
- scripts/inspect-lotheader-string-table.ps1: .lotheader string table reader.
  - Args: -Path <dir> -Output <.local dir> -MaxFiles (default 10, max 50)
    -MaxBytesPerFile (default 131072, max 1048576).
  - Refuses output outside .local/.
  - Reads ONLY .lotheader files. NOT .lotpack. NOT .bin.
  - Extracts: bytes 0-3 as header_zero_hex, bytes 4-7 as candidate_entry_count
    (32-bit LE), bytes 8+ as newline-delimited ASCII tileset pack name strings.
  - Counts: extracted_entry_count, count_matches_extracted_entries,
    duplicate_entry_count, unique_entry_count, non_printable_byte_count_after_header.
  - Writes lotheader-string-table-evidence.json + .md with aggregate.
  - only_lotheader_files_read=true, lotpack_files_read=false, bin_files_read=false.
- scripts/validate.ps1: MAP-4E inline contract section (6 checks):
  - script exists, .local refusal, only_lotheader_files_read, lotpack_files_read,
    bin_files_read, compiled_writer_implemented sentinels.
  PS lane stays 492. No proof-packet sync.
- docs/COMPILED_CELL_FORMAT_EVIDENCE.md:
  - Header updated.
  - Section 5: .lotheader gap refined (14/16 exact count matches across 16 files).
  - Section 13: mismatch details added.
  - Section 16 added: lotheader string table evidence (MAP-4E).
    - 16/16 header-zero consistent (both mods).
    - 14/16 exact entry count matches.
    - Entry range: 31 to 2450 per cell.
    - Entries are tileset pack+sprite names (e.g., blends_grassoverlays_01_0).
    - 2 mismatches in complex cells: off by 2-3; embedded non-printable bytes.
    - Tileset name pattern documented.
    - Implications for minimal MAP-4 cell noted.
- docs/MAP_EXPORT_CONTRACT.md: MAP-4E section added.
- docs/IMPLEMENTATION.md: MAP-4E ratified row added.

.lotheader structure evidence significantly advanced.
Writing not yet permitted: 2 mismatches unexplained; non-printable byte
role in complex cells unknown; full format not decoded.
No files copied. Only .lotheader read. No .lotpack/.bin read.
No compiled writer. No playable export claim.
PS 492 / .NET 381 unchanged. No proof-packet sync.

---

## [Unreleased]

### Added (MAP-4D: compiled binary header evidence probe)
- scripts/inspect-compiled-binary-headers.ps1: bounded prefix reader.
  - Args: -Path <dir> -Output <.local dir> -MaxBytes (default 64, max 256)
    -MaxFilesPerExtension (default 5, max 20).
  - Refuses output outside .local/.
  - MaxBytes clamped to [1, 256] before any I/O.
  - Scans for .lotheader, .lotpack, .bin files.
  - Reads ONLY first MaxBytes bytes per sampled file via stream.
  - Does NOT read full binary file contents.
  - Does NOT copy files.
  - Writes per-file: prefix_hex, prefix_ascii_preview, first_4/8_bytes_hex,
    all_zero_prefix, prefix_group_key (first 16 bytes).
  - Writes compiled-binary-header-evidence.json + .md.
  - Safety flags: full_binary_files_read=false,
    compiled_writer_implemented=false, binary_prefixes_read=true.
- scripts/validate.ps1: MAP-4D inline contract section (5 checks):
  - script exists, .local refusal, full_binary_files_read sentinel,
    compiled_writer_implemented sentinel, MaxBytes 256 guard.
  PS lane stays 492. No proof-packet sync.
- docs/COMPILED_CELL_FORMAT_EVIDENCE.md:
  - Header updated (MAP-4D status, binary prefixes read).
  - Section 5: .lotheader OPEN→PARTIAL, .lotpack OPEN→PARTIAL.
  - Section 13: still-blocked list updated.
  - Section 15 added (MAP-4D binary header evidence):
    - lotheader: bytes 0-3 always 0x00000000; bytes 4-7 = 32-bit LE variable
      integer (appears to be tileset entry count, 31–903 across samples);
      bytes 8+ = newline-separated ASCII tileset pack names.
    - lotpack: first 8 bytes = 84030000241c0000 IDENTICAL across all 10
      sampled files from both mods; bytes 8+ = apparent offset/size table.
    - chunkdata bin: bytes 0-1 = 0001 consistent.
    - Safety record.
- docs/MAP_EXPORT_CONTRACT.md: MAP-4D section added.
- docs/IMPLEMENTATION.md: MAP-4D ratified row added.

Gap advances (PARTIAL only — not CLOSED):
- .lotheader binary format: OPEN → PARTIAL.
- .lotpack binary format: OPEN → PARTIAL.

No files copied. No full binary reads. No compiled writer.
No .lotpack/.lotheader/.bin in repo. No playable export claim.
PS 492 / .NET 381 unchanged. No proof-packet sync.

---

## [Unreleased]

### Added (MAP-4C: map text metadata evidence reader)
- scripts/inspect-map-text-metadata.ps1: local-only text metadata reader.
  - Args: -Path <local mod/map root> -Output <.local dir>.
  - Refuses output outside .local/.
  - Reads ONLY: mod.info, map.info, spawnpoints.lua, objects.lua.
  - Does NOT read: .lotheader, .lotpack, .bin, .png, any binary.
  - Detects map folders under media/maps/.
  - Parses key=value from .info files.
  - Writes map-text-metadata-evidence.json:
    - schema: pzmapforge.map-text-metadata-evidence.v0.1
    - map_info_key_values, spawnpoints_summary, objects_summary
    - binary_files_read: false, compiled_writer_implemented: false
  - Writes map-text-metadata-evidence.md with non-claims section.
  - [string] cast fix: explicit ForEach { [string]$_ } before Select-Object
    to prevent PS 5.1 extended-string serialization in first_non_empty_lines.
- scripts/validate.ps1: MAP-4C inline contract section added (4 checks):
  - script exists
  - contains .local refusal
  - contains binary_files_read sentinel
  - contains compiled_writer_implemented sentinel
  PS lane stays 492. No proof-packet sync.
- docs/COMPILED_CELL_FORMAT_EVIDENCE.md:
  - Header updated (MAP-4C status, text metadata read).
  - Section 5 gap updates:
    - Spawn file format: PARTIAL (format pattern confirmed).
    - Spawn coordinate system: PARTIAL (worldX/worldY/posX/posY/posZ observed).
    - map.info required fields: PARTIAL (title/lots/description/fixed2x observed).
  - Section 13 still-blocked updated.
  - Section 14 added: MAP-4C text metadata observations (mod.info fields,
    map.info fields, spawnpoints.lua format pattern, coordinate field names,
    profession-key structure, per-mod summaries).
- docs/MAP_EXPORT_CONTRACT.md: MAP-4C section added.
- docs/IMPLEMENTATION.md: MAP-4C ratified row added.

Gap advances (PARTIAL only, not CLOSED):
- Spawn file format: worldX/worldY/posX/posY/posZ field names confirmed.
- Spawn coordinate system: cell-grid coordinates confirmed.
- map.info fields: title/lots/description/fixed2x observed.

No binary files read. No compiled writer. No PZ assets.
No .lotpack/.lotheader/.bin in repo. No playable export claim.
PS 492 / .NET 381 unchanged. No proof-packet sync.

---

## [Unreleased]

### Added (MAP-4B: compiled cell evidence summaries)
- docs/COMPILED_CELL_FORMAT_EVIDENCE.md: updated with two Workshop mod
  inventory observations and derived hypotheses.
  - Header: status updated to MAP-4B; observation count added.
  - Section 3: file type table updated with confirmed naming patterns:
    - lotpack is world_<cx>_<cy>.lotpack (not <cx>_<cy>.lotpack).
    - chunk bin is chunkdata_<cx>_<cy>.bin (not map_<cx>_<cy>.bin).
    - objects.lua and worldmap.xml.bin added (observed but not in original hypothesis).
  - Section 4: directory layout updated with observed flat layout under
    media/maps/<map_id>/.
  - Section 5: gap statuses updated:
    - Cell coordinate naming: PARTIAL.
    - Exact directory layout: PARTIAL.
    - Spawn file format (spawnpoints.lua): PARTIAL (presence only).
    - map.info required fields: PARTIAL (presence only).
    - All binary format gaps: OPEN.
    - All load-test gaps: OPEN.
  - Section 11: evidence observation comparison table (new).
  - Section 12: evidence-derived hypotheses (new).
  - Section 13: still-blocked summary (new).
- scripts/inspect-compiled-cell-evidence.ps1: PowerShell -replace syntax fix
  (two expressions wrapped in parentheses; no behavior change). Commit ed9e44c.
- docs/MAP_EXPORT_CONTRACT.md: MAP-4B section added.
- docs/IMPLEMENTATION.md: MAP-4B ratified row added.

No files copied into repo. No binary content parsed.
No compiled writer implemented. No PZ assets. No playable export claim.
PS 492 / .NET 381 unchanged. No proof-packet sync.

---

## [Unreleased]

### Added (MAP-4A: compiled cell format evidence inventory)
- docs/COMPILED_CELL_FORMAT_EVIDENCE.md: evidence requirements and gap table.
  - 10 evidence gaps defined (lotheader/lotpack binary formats, cell coordinate
    naming, directory layout, minimum viable cell count, single-cell load test,
    spawn file format, spawn coordinate system, map.info fields,
    Build 41 vs Build 42 format differences).
  - Decision gate in section 8 — lists all conditions that must be true
    before MAP-4 implementation is permitted.
  - How-to guide for collecting evidence with the inspector script.
  - Forbidden actions during evidence collection documented.
- docs/examples/compiled-cell-evidence/COMPILED_CELL_EVIDENCE_TEMPLATE.md:
  fillable observation template for the operator.
  - Covers: date/source/PZ version, directory tree, file inventory,
    cell coordinate naming, map.info, spawnpoints.lua, .lotheader, .lotpack,
    minimum viable cell, unknowns, risks, gap closure status.
  - Operator fills and saves to .local/ (not committed).
- scripts/inspect-compiled-cell-evidence.ps1: local-only evidence enumerator.
  - Args: -Path <local dir> -Output <.local dir>.
  - Refuses output outside .local/.
  - Enumerates file names, relative paths, extensions, sizes, SHA-256 hashes.
  - Writes compiled-cell-evidence.json and compiled-cell-evidence.md to .local/.
  - Safety flags (all false): copied_input_files, pz_assets_copied,
    media_maps_touched, playable_export_claimed, compiled_writer_implemented.
  - Does not copy files. Does not parse content beyond hashing.
- scripts/validate.ps1: MAP-4A inline contract section added (5 checks):
  - docs/COMPILED_CELL_FORMAT_EVIDENCE.md exists.
  - scripts/inspect-compiled-cell-evidence.ps1 exists.
  - docs/examples/compiled-cell-evidence/COMPILED_CELL_EVIDENCE_TEMPLATE.md exists.
  - Script contains .local refusal language.
  - Script contains copied_input_files sentinel.
  - Uses inline throw (not Assert-True). PS lane stays 492. No proof-packet sync.
- docs/MAP_EXPORT_CONTRACT.md: MAP-4A section added.
- docs/IMPLEMENTATION.md: MAP-4A ratified row added.

No compiled writer implemented. No .lotpack/.lotheader/.bin written.
No PZ assets read or copied. No media/maps writes in repo.
No playable export claim. PS 492 / .NET 381 unchanged.

---

## [Unreleased]

### Added (MAP-3C: map scaffold smoke script)
- scripts/smoke-map-scaffold-minimal.ps1: local-only smoke helper for map-scaffold.
  - Runs map-scaffold against examples/map-source/minimal-cell.json.
  - Output to .local\map-scaffold\minimal-cell-smoke (gitignored).
  - 16 assertions:
    - source file present
    - map-scaffold exits 0
    - mod.info, media/maps/<map_id>/map.info, spawnpoints.lua,
      README_PZMAPFORGE_BOUNDARY.txt each exist by path
    - exactly 4 files total under output
    - no compiled output extensions (.lotpack/.lotheader/.bin/.tmx/.pzw)
    - boundary language in files:
      Text-only scaffold, Not playable, No PZ assets, Not load-tested
    - stdout safety lines:
      text_only_scaffold_written: true
      compiled_outputs_written: false
      playable_export_generated: false
      pz_assets_read_or_copied: false
  - Standalone helper; not wired into validate.ps1.
  - PS 492 / .NET 381 unchanged.
- docs/MAP_EXPORT_CONTRACT.md: MAP-3C section added.
- docs/IMPLEMENTATION.md: MAP-3C ratified row added.

No compiled outputs. No PZ assets read or copied. No playable export claim.
media/maps writes: inside .local smoke output only. No proof packet sync.

---

## [Unreleased]

### Added (MAP-3B: text-only local mod scaffold writer)
- src/PZMapForge.Cli/Program.cs: map-scaffold command.
  - Args: --source <path> --output <dir>.
  - Reads pzmapforge.map-source.v0.1 JSON source file.
  - Validates schema, claim_boundary, and cells array.
  - Refuses output paths containing media/maps.
  - Refuses output paths outside a .local/ directory.
  - Writes exactly four text-only scaffold files under --output:
    - mod.info (mod metadata; boundary language; not playable)
    - media/maps/<map_id>/map.info (minimal text boundary; not a compiled map)
    - media/maps/<map_id>/spawnpoints.lua (placeholder; no coordinate math)
    - media/maps/<map_id>/README_PZMAPFORGE_BOUNDARY.txt (full boundary note)
  - Every generated file contains boundary language:
    - Text-only scaffold. Not playable. No compiled map files.
    - No PZ assets included. Not load-tested.
  - Stdout reports: text_only_scaffold_written: true,
    compiled_outputs_written: false, playable_export_generated: false,
    media_maps_scope: .local_only, pz_assets_read_or_copied: false.
- tests/PZMapForge.Cli.Tests/MapScaffoldProcessTests.cs: 15 new CLI tests:
  - Positive: exits 0 + writes 4 files; mod.info exists; map.info exists;
    spawnpoints.lua exists; README_PZMAPFORGE_BOUNDARY.txt exists;
    boundary language present in all files; stdout safety lines correct;
    no compiled output files (.lotpack/.lotheader/.bin/.tmx/.pzw) under output.
  - Negative: missing source; source not found; invalid JSON; wrong schema;
    wrong claim_boundary; output outside .local; media/maps output path.
- scripts/validate.ps1: $dnCliTests 176->191, $dnTotal 366->381.
- scripts/write-proof-packet.ps1: cli_tests 176->191, test_total 366->381,
  markdown table updated.
- scripts/test-proof-packet.ps1: assertions updated to 191/381.
- docs/MAP_EXPORT_CONTRACT.md: MAP-3B section added; slice row updated;
  MAP-4 remains blocked on compiled cell format evidence.
- docs/IMPLEMENTATION.md: MAP-3B ratified row added.
- README.md: map-scaffold command example added.

No compiled outputs written. No PZ assets read or copied.
No app-export behavior changed. No SVG geometry converted.
No coordinate math performed. No playable Project Zomboid export claim.
media/maps writes: inside .local output root only, never in repo media/maps.

---

## [Unreleased]

### Added (MAP-3A: text-only mod scaffold contract)
- src/PZMapForge.Cli/Program.cs: map-plan JSON output extended with
  scaffold contract fields:
  - scaffold_contract_version: "0.1"
  - text_only_scaffold_supported_now: false
  - text_only_scaffold_written: false
  - scaffold_execute_supported: false
  - future_scaffold_files: array of 4 planned file objects, each with
    written_now=false and reason="MAP-3A contract only":
    - future mod.info
    - future media/maps/<map_id>/map.info
    - future media/maps/<map_id>/spawnpoints.lua
    - future media/maps/<map_id>/README_PZMAPFORGE_BOUNDARY.txt
- src/PZMapForge.Cli/Program.cs: map-plan Markdown extended with
  "Future text-only scaffold contract" section listing planned files
  and explicitly stating none are written.
- tests/PZMapForge.Cli.Tests/MapPlanProcessTests.cs: 4 new CLI tests:
  - JSON contains scaffold contract fields with all written_now=false.
  - Markdown contains scaffold contract section with key phrases.
  - Output directory contains only the 2 expected files.
  - No media/maps subdirectory exists under output.
- scripts/validate.ps1: $dnCliTests 172->176, $dnTotal 362->366.
- scripts/write-proof-packet.ps1: cli_tests 172->176, test_total 362->366,
  markdown table updated.
- scripts/test-proof-packet.ps1: assertions updated to 176/366.
- docs/MAP_EXPORT_CONTRACT.md: MAP-3A subsection added; MAP-3A slice row
  added; MAP-3B noted as not implemented, no approval given.
- docs/IMPLEMENTATION.md, README.md, CHANGELOG.md: updated.

No scaffold files written. No mod.info. No map.info. No spawnpoints.lua.
No README_PZMAPFORGE_BOUNDARY.txt. No media/maps writes. No PZ assets
read or copied. No execute flag. No compiled output. No playable export claim.

---

## [Unreleased]

### Added (MAP-2: dry-run map export plan command)
- src/PZMapForge.Cli/Program.cs: map-plan command.
  - Args: --source <path> --output <dir>.
  - Reads pzmapforge.map-source.v0.1 JSON source file.
  - Validates schema, claim_boundary, and cells array.
  - Writes two inert artifacts to --output (must be under .local/):
    - map-export-plan.json (schema pzmapforge.map-export-plan.v0.1)
    - map-export-plan.md
  - JSON includes: dry_run=true, execute_supported=false,
    playable_export_generated=false, compiled_outputs_written=false,
    local_mod_scaffold_written=false, media_maps_touched=false,
    pz_assets_read_or_copied=false, would_write (future items only).
  - Refuses output paths containing media/maps.
  - Refuses output paths outside .local/.
  - Exits 1 on missing --source, missing --output, missing file,
    invalid JSON, wrong schema, wrong claim_boundary.
- tests/PZMapForge.Cli.Tests/MapPlanProcessTests.cs: 9 CLI process tests.
  - Positive: exits 0, writes both artifacts, JSON fields, markdown text.
  - Negative: missing source, file not found, wrong schema,
    wrong claim_boundary, media/maps output, invalid JSON.
- scripts/validate.ps1: $dnCliTests 163->172, $dnTotal 353->362.
- scripts/write-proof-packet.ps1: cli_tests 163->172, test_total 353->362,
  markdown table updated.
- scripts/test-proof-packet.ps1: assertions updated to 172/362.
- docs/MAP_EXPORT_CONTRACT.md: map-plan subsection added; MAP-2 slice updated.
- docs/IMPLEMENTATION.md, README.md, CHANGELOG.md: updated.

No playable map export. No execute flag. No .local mod scaffold beyond the
map-plan output. No media/maps writes. No PZ assets read or copied.
No app-export behavior changed. No SVG geometry conversion. No coordinate math.
No playable Project Zomboid export claim.

---

## [Unreleased]

### Added (MAP-1: map source schema)
- schemas/pzmapforge.map-source.v0.1.schema.json: v0.1 map source schema.
  - Required fields: schema, format_version, claim_boundary, map_id,
    cell_size, cells.
  - claim_boundary: map_source_only_not_exported_not_pz_load_tested.
  - Cell fields: cell_id, x, y, terrain, spawn_points, zones, notes.
  - Terrain enum (v0.1): grass, asphalt, water, unknown.
  - Spawn point fields: id, x, y, label (source metadata only).
  - Zone fields: id, kind, label.
  - Source format only. Not an export format. Not compiled. Not playable.
- examples/map-source/minimal-cell.json: valid minimal example conforming
  to the v0.1 schema (one cell, one spawn point as source metadata, grass
  terrain). Notes in example state it is source-only and not exported.
- scripts/test-schema-files.ps1: Test-Schema call added for map-source
  schema (18 assertions). Schema file sanity total: 196 -> 214.
- scripts/validate.ps1: Schema file sanity count 196 -> 214; psTotal 474 -> 492.
- scripts/write-proof-packet.ps1: schema_file_sanity 196 -> 214;
  total_expected_assertions 474 -> 492; markdown table updated.
- scripts/test-proof-packet.ps1: assertions updated to 214 / 492.
- docs/MAP_EXPORT_CONTRACT.md: Section 6 updated with v0.1 source format
  details; MAP-1 slice row updated.
- docs/IMPLEMENTATION.md, README.md, CHANGELOG.md: updated.

No map export command added. No .local writer added. No app-export behavior
changed. No SVG geometry conversion. No coordinate math. No PZ assets.
No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (MAP-0: map export contract discovery)
- docs/MAP_EXPORT_CONTRACT.md: contract discovery document defining all areas
  PZMapForge must satisfy before claiming playable Project Zomboid map export.
  - Status block: planning_artifact_only_not_pz_load_tested.
  - Sections: purpose, current boundary, why replace TileZed/WorldEd,
    what TileZed/WorldEd provide, target pipeline, source/editing format,
    compiled game-load format, mod packaging, spawn/player entry,
    in-game map features, asset boundary, local output boundary,
    first playable proof target (MAP-4), unknowns and required local evidence,
    forbidden claims, proposed next slices MAP-1 through MAP-6.
  - Defines dry-run-by-default requirement for future compiler commands.
  - Defines explicit --execute flag requirement for any playable-style output.
  - Documents MAP-4 target: minimal local playable cell proof.
  - Lists all unknowns requiring local evidence before compiler work can begin.
- docs/IMPLEMENTATION.md, README.md, CHANGELOG.md: updated.

No app-export behavior changed. No SVG geometry conversion. No coordinate math.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (APP-9A: verify artifact index in Montreal SVG smoke)
- scripts/smoke-montreal-svg-planning-manifest.ps1:
  - 19 new APP-9 artifact index checks (source run: 13 checks, review run: 6 checks).
  - Source run checks: Evidence Artifacts, Clean analysis image, Parsed preview,
    Parsed cell JSON, Regions JSON, Primitives JSON, Plan recommendations JSON,
    Annotation image, SVG structure report, SVG layer candidates,
    SVG layer selection template, planning artifact only,
    not a playable Project Zomboid export.
  - Review run checks: Evidence Artifacts, SVG selection review,
    SVG planning manifest JSON, SVG planning manifest Markdown,
    planning artifact only, not a playable Project Zomboid export.
  - Total checks in smoke script: 44 (up from 25). All PASS on real Montreal SVG run.
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No app-export behavior changed. No SVG geometry conversion. No coordinate math.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Fixed (PROOF-1: sync proof packet .NET test evidence)
- scripts/write-proof-packet.ps1:
  - test_total: 230 -> 353
  - cli_tests: 40 -> 163
  - core_tests: 190 (unchanged)
  - Markdown table updated to match corrected values.
- scripts/test-proof-packet.ps1:
  - dotnet test_total assertion: -eq 230 -> -eq 353
  - dotnet cli_tests assertion: -eq 40 -> -eq 163
- scripts/validate.ps1:
  - $dnCliTests: 40 -> 163
  - $dnTotal: 230 -> 353
- docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

Evidence correction only. No app-export behavior changed.
No SVG geometry conversion. No coordinate math. No path d= extraction.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (APP-9: app export artifact index panel)
- src/PZMapForge.Cli/Program.cs:
  - BuildArtifactIndexHtml: compact .artifacts-idx panel inserted between
    the Run Summary cockpit and the workbench in generated index.html.
  - Always shows: Clean analysis image, Parsed preview, Parsed cell JSON,
    Regions JSON, Primitives JSON, Plan recommendations JSON.
  - Conditionally shows (when present): Annotation image, SVG structure report,
    SVG layer candidates, SVG layer selection template, SVG selection review,
    SVG planning manifest JSON, SVG planning manifest Markdown.
  - Absent artifacts are omitted. All links are relative. No absolute machine
    paths exposed. Panel note: planning artifact only -- not a playable
    Project Zomboid export.
  - artifactIndexHtml parameter threaded through BuildAppHtml.
  - CSS: .artifacts-idx, .artifacts-idx-hdr, .artifacts-idx-note.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportContentTests (APP-9 block): 4 new tests: ArtifactIndexContainsEvidenceArtifacts,
    ArtifactIndexContainsCleanAnalysisImage, ArtifactIndexContainsParsedCellJson,
    ArtifactIndexContainsPlanningArtifactOnly.
  - AppExportAnnotationTests (APP-9 block): 1 new test: Annotation_ArtifactIndexContainsAnnotationImage.
  - AppExportSvgAnnotationTests (APP-9 block): 2 new tests: ArtifactIndexContainsSvgStructureReport,
    ArtifactIndexContainsSvgLayerCandidates.
  - AppExportSelectionTests (APP-9 block): 3 new tests: ArtifactIndexContainsSvgSelectionReview,
    ArtifactIndexContainsSvgPlanningManifestJson, ArtifactIndexContainsSvgPlanningManifestMarkdown.
  - Total: 353 tests (190 Core + 163 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No SVG geometry conversion. No coordinate math. No path d= extraction.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (APP-8A: cockpit verification in Montreal SVG smoke)
- scripts/smoke-montreal-svg-planning-manifest.ps1:
  - 15 new APP-8 cockpit checks after existing manifest verification.
  - Source run HTML (SVG annotation path): Run Summary, SVG annotation: present,
    SVG parse: parsed, SVG candidates: present, playable export generated: false,
    PZ assets copied/read: false, media/maps touched: false, claim_boundary: intact.
  - Review run HTML (manifest path): Run Summary, SVG review: present,
    Planning manifest: present, playable export generated: false,
    PZ assets copied/read: false, media/maps touched: false, claim_boundary: intact.
  - Total checks in smoke script: 25. All PASS on real Montreal SVG run.
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No path d= extraction.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (APP-8: app export run summary cockpit)
- src/PZMapForge.Cli/Program.cs:
  - svgParseStatus and svgManifestPresent variables initialized before the
    annotation and selection blocks; populated as the command executes.
  - paletteClean, svgAnnotationPresent, svgCandidatesPresent, svgReviewPresent
    derived from in-memory state before BuildAppHtml.
  - BuildRunSummaryHtml(paletteClean, svgAnnotation, svgParseStatus,
    svgCandidates, svgReview, svgManifest): builds a .cockpit div with two rows:
    Run Summary (palette/SVG annotation/SVG parse/SVG candidates/SVG review/
    planning manifest) and Safety (playable export generated: false, PZ assets
    copied/read: false, media/maps touched: false, claim_boundary: intact).
    Items use .ck-ok/.ck-warn/.ck-absent/.ck-safe CSS classes.
  - runSummarySectionHtml threaded through BuildAppHtml as new parameter.
  - {{runSummarySectionHtml}} inserted between .boundary div and .workbench div.
  - CSS: .cockpit, .cockpit-row, .ck-lbl, .ck-ok, .ck-warn, .ck-absent, .ck-safe.
  - All flags derived from generated artifacts and in-memory state. Static HTML only.
    No JS, no server.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportContentTests (APP-8 block): 5 new tests: CockpitContainsRunSummary,
    CockpitContainsPlayableExportFalse, CockpitContainsPzAssetsFalse,
    CockpitContainsMediaMapsFalse, CockpitContainsClaimBoundaryIntact.
  - AppExportSvgAnnotationTests (APP-8 block): 2 new tests:
    CockpitContainsSvgAnnotationPresent, CockpitContainsSvgParseParsed.
  - AppExportSelectionTests (APP-8 block): 1 new test:
    CockpitContainsPlanningManifestPresent.
  - Total: 343 tests (190 Core + 153 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No path d= extraction.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (SVG-11: Montreal SVG planning manifest smoke script)
- scripts/smoke-montreal-svg-planning-manifest.ps1: local-only, reproducible
  two-pass smoke for the real Montreal SVG planning manifest chain.
  - Pass 1 (source run): app-export with SVG annotation
    (arrondissements-quartiers-montreal-200802.svg); generates SVG structure,
    candidate inventory, and selection template under
    .local/app/mtl-svg-selection-source-smoke.
  - Writes 9-item operator selection JSON into source run artifacts:
    Eaux (water_reference), Outline_MTL (city_boundary_reference),
    SudOuest / VilleMarie / Plateau / NDG_CDN (borough_boundary),
    ANGRIGNON (transit_landmark), Pte-Angus / Cap-Saint-Jacques (park_reference).
  - Pass 2 (review run): app-export --svg-selection; generates selection review
    and planning manifest under .local/app/mtl-svg-planning-manifest-smoke.
  - 10 verification checks: file existence (index.html, selection-review.json,
    manifest.json, manifest.md), selected_count == 9, planning_status ==
    operator_selected_metadata_only, exported_to_project_zomboid == false,
    converted_to_map_geometry == false, markdown contains "No SVG geometry
    converted", HTML contains "SVG Planning Manifest".
  - Exits 1 with clear message if required machine-local files are absent
    (SVG and analysis PNG not committed to repo).
  - All output under .local/ (gitignored). No SVG geometry converted.
    No coordinates extracted. No PZ assets. No media/maps writes.
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No path d= extraction.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (SVG-10: planning manifest visible summary)
- src/PZMapForge.Cli/Program.cs:
  - BuildSvgPlanningManifestHtml now accepts List<SelectedLayerItem> items and
    renders manifest contents directly in the HTML section:
    - Metadata table: selected_count and planning_status
      (operator_selected_metadata_only).
    - Intended Uses chip list (distinct intended_use values, sorted).
    - Selected items grouped by bucket: value chip, intended_use label,
      operator_note (italic). Layout matches SVG Selection Review.
    - Non-claims list: "No SVG geometry converted", "No SVG coordinates
      extracted", "No Project Zomboid export generated", "No media/maps writes",
      "No PZ assets copied or read".
    - Artifact links to svg-planning-manifest.json and svg-planning-manifest.md
      retained.
  - Zero-selected path: unchanged. HTML still says "No selected SVG metadata
    was available for a planning manifest." No table or items rendered.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportSelectionTests (SVG-10 block): 7 new tests:
    IndexHtmlManifestContainsSelectedCount,
    IndexHtmlManifestContainsPlanningStatus,
    IndexHtmlManifestContainsSelectedValueEaux,
    IndexHtmlManifestContainsIntendedUseWaterBody,
    IndexHtmlManifestContainsNoSvgGeometryConverted,
    IndexHtmlManifestContainsNoSvgCoordinatesExtracted,
    IndexHtmlManifestContainsNoProjectZomboidExportGenerated.
  - AppExportZeroSelectionTests: IndexHtmlContainsNoMetadataAvailable (1 new
    test: zero-selected HTML contains "no selected SVG metadata was available").
  - Total: 335 tests (190 Core + 145 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No path d= extraction.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (SVG-9: selected SVG planning manifest)
- src/PZMapForge.Cli/Program.cs:
  - WriteSvgPlanningManifest: writes svg-planning-manifest.json (schema
    pzmapforge.svg-planning-manifest.v0.1) when selected_count > 0.
    Fields: schema, claim_boundary, source_selection_file_name,
    generated_from, selected_count, selected_by_bucket (bucket/count/items
    with value/intended_use/operator_note), intended_uses (distinct sorted),
    operator_notes (max 50, value+note), planning_status
    "operator_selected_metadata_only", exported_to_project_zomboid false,
    and all safety flags false.
  - BuildSvgPlanningManifestMarkdown: writes svg-planning-manifest.md. ASCII.
    Title "SVG Planning Manifest", claim boundary, source, selected count,
    planning status, items by bucket, intended uses list, non-claims:
    "No SVG geometry converted", "No SVG coordinates extracted",
    "No Project Zomboid export generated", "No media/maps writes",
    "No PZ assets copied or read".
  - BuildSvgPlanningManifestHtml: h2 "SVG Planning Manifest" section; note
    "This is an inert planning manifest. It records selected SVG metadata
    only. It does not convert or export SVG geometry."; artifact links for
    svg-planning-manifest.json and svg-planning-manifest.md.
  - Zero-selected path: does not fail; review artifact still written;
    manifest not written; HTML says no selected SVG metadata available.
  - svgManifestSectionHtml slot added to BuildAppHtml.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportSelectionFixture: ManifestJsonExists, ManifestMdExists,
    ManifestJson, ManifestMd computed properties.
  - AppExportSelectionTests (SVG-9 block): 8 new tests: WritesManifestJson,
    WritesManifestMd, ManifestJsonContainsPlanningStatus,
    ManifestJsonContainsExportedFalse, ManifestMdContainsTitle,
    ManifestMdContainsNonClaim, IndexHtmlContainsSvgPlanningManifest,
    IndexHtmlContainsInertManifestNote.
  - AppExportZeroSelectionTests: ExitsZeroAndNoManifest (zero-selected
    does not fail and does not write manifest).
  - Total: 327 tests (190 Core + 137 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No path d= extraction.
No PZ assets. No media/maps writes. No playable export claim.

---

## [Unreleased]

### Added (SVG-8: SVG layer selection review import)
- src/PZMapForge.Cli/Program.cs:
  - --svg-selection <json> (-s): optional argument; exits 1 if file missing or JSON invalid.
  - ReadSvgLayerSelection: tolerant JsonDocument parser; iterates known bucket keys;
    collects items where selected==true into List<SelectedLayerItem>.
  - WriteSvgLayerSelectionReview: writes svg-layer-selection-review.json (schema v0.1),
    selection_status "reviewed", selected_count, selected_items, all safety flags false.
  - Input file copied to artifacts/svg-layer-selection.input.json.
  - BuildSvgLayerSelectionReviewHtml: "SVG Selection Review" h2 panel with grouped
    chips, intended_use/operator_note spans, "This review is metadata only" note,
    "not exported to Project Zomboid" note, artifact links.
  - SelectedLayerItem sealed record.
  - .review-item/.review-use/.review-note CSS added.
  - svgReviewSectionHtml slot added to BuildAppHtml.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportAnnotationTests: MissingSvgSelectionFile_ExitsOne process test.
  - AppExportSelectionFixture: runs once with minimal 1-item selection JSON.
  - AppExportSelectionTests: 9 tests (exit 0, input copy, review exists, selected_count,
    Eaux value, converted false, SVG Selection Review, metadata-only, not-exported note).
  - Total: 318 tests (190 Core + 128 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No PZ assets. No media/maps writes.

---

## [Unreleased]

### Added (SVG-7: SVG layer selection template)
- src/PZMapForge.Cli/Program.cs:
  - WriteSvgLayerSelectionTemplate: writes svg-layer-selection.template.json. Each
    bucket's sample items become objects with value/selected/intended_use/operator_note.
    selection_status: "operator_review_required". All safety flags false. Generated
    from candidate sample lists (bounded, not full metadata lists).
  - BuildSvgLayerSelectionHtml: "SVG Layer Selection Template" h2 section with notes:
    "Operator review required", "Selecting a candidate does not convert SVG geometry",
    "This template is for future planning decisions only." Artifact link included.
  - svgSelectionSectionHtml slot added to BuildAppHtml (new parameter).
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportSvgFixture: SvgLayerSelectionExists + SvgLayerSelectionJson properties.
  - AppExportSvgAnnotationTests: 8 new assertions (file exists, operator_review_required,
    selected false, intended_use, converted false, SVG Layer Selection Template in HTML,
    Operator review required in HTML, does-not-convert note in HTML).
  - Total: 308 tests (190 Core + 118 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No PZ assets. No media/maps writes.

---

## [Unreleased]

### Added (SVG-6: classify full SVG metadata candidate inventory)
- src/PZMapForge.Cli/Program.cs:
  - SvgStructureResult: AllIds, AllClasses, AllTextLabels properties (bounded at 500,
    deduplicated, in-memory only — not written to svg-reference-structure.json).
  - WriteSvgStructure: computes allIds/allClasses/allTextLabels alongside sample lists.
  - WriteSvgLayerCandidates: uses AllIds/AllClasses/AllTextLabels for classification
    instead of bounded sample lists.
  - svg-layer-candidates.json: adds total_id_values_inspected,
    total_class_values_inspected, total_text_labels_inspected,
    total_metadata_values_inspected. borough_or_district_candidates.count now
    reflects full classified set; samples still capped at 30.
  - SvgLayerCandidatesResult: BoroughOrDistrictFullCount, TotalIds/Classes/Labels
    Inspected properties.
  - BuildSvgLayerCandidatesHtml: shows "Metadata Values Inspected" totals line.
  - AppendCandidateBucket: optional fullCount parameter; shows truncation note when
    count > samples.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - 5 new assertions in AppExportSvgAnnotationTests (total fields x4, HTML total).
  - AppExportSvgFullInventoryFixture: creates SVG with 35 unique borough IDs to
    exercise sample truncation.
  - AppExportSvgFullInventoryTests: 2 tests (exit 0, count=35 proves count > 30 limit).
  - Total: 300 tests (190 Core + 110 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No PZ assets. No media/maps writes.

---

## [Unreleased]

### Changed (SVG-5: tune Montreal SVG layer candidate classification)
- src/PZMapForge.Cli/Program.cs:
  - ContainsWord: word-boundary match helper (fixes "Plateau" false-positive in water
    bucket caused by substring "eau").
  - IsAllCapsAlpha: detects all-caps alphabetic text labels (transit heuristic).
  - WriteSvgLayerCandidates: 3 new classification buckets:
    technical_layer_candidates (fond/base/layer/background keywords),
    transit_or_station_candidates (all-caps labels >= 4 chars, or station/metro/gare),
    park_or_green_space_candidates (pte/parc/anse/trail/bois/nature/forest keywords).
    Priority order: water > outline > technical > street > borough for IDs/classes.
    Text labels: transit > park > label (remaining).
  - candidate_generation_notes: string array documenting each pattern rule.
  - inspected_metadata_sources: ["ids", "classes", "text_labels"].
  - SvgLayerCandidatesResult: 3 new properties + GenerationNotes.
  - BuildSvgLayerCandidatesHtml: shows new buckets via AppendCandidateBucket.
  - Test fixture SVG updated: fond_arrond (technical), ANGRIGNON (transit),
    Pte-Angus (park), background class.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - 8 new assertions (transit key, technical key, generation_notes, metadata_sources,
    Transit/Station in HTML, Technical Layers in HTML, still metadata-only, still
    no-geometry note). Total: 293 tests (190 Core + 103 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

Real Montreal SVG result (SVG-5):
  water: Eaux (1)  |  technical: Fond_arrond (1)  |  outline: Outline_MTL (1)
  transit: ANGRIGNON (1)  |  borough: 17  |  park: 13 (Pte-Angus, Cap-Saint-Jacques...)
  "Plateau" correctly in borough (no longer false-positive water match)

No coordinate extraction. No geometry conversion. No PZ assets. No media/maps writes.

---

## [Unreleased]

### Added (SVG-4: SVG layer candidate inventory)
- src/PZMapForge.Cli/Program.cs:
  - SvgStructureResult: SampleClasses property added; WriteSvgStructure
    populates it from sampleClasses.
  - SvgLayerCandidatesResult sealed class: WaterCandidates, OutlineCandidates,
    BoroughOrDistrictCandidates, StreetOrRouteCandidates, LabelCandidates,
    UnknownCandidates (all IReadOnlyList<string>).
  - MatchesAny: case-insensitive keyword helper.
  - WriteSvgLayerCandidates: classifies IDs+classes against water/outline/street
    keyword patterns; unmatched → borough_or_district; text labels → label bucket;
    writes svg-layer-candidates.json with schema v0.1, candidate_generation_method
    "metadata_name_pattern_only", counts+samples per bucket, all safety flags false.
  - BuildSvgLayerCandidatesHtml + AppendCandidateBucket: "SVG Layer Candidates"
    h2 panel with bucket chip lists and "metadata candidates only" / "No SVG
    geometry is converted" notes.
  - AppExportCommand: svgCandidatesSectionHtml slot; WriteSvgLayerCandidates called
    after WriteSvgStructure; BuildAppHtml receives new parameter.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportSvgFixture: SvgLayerCandidatesExists + SvgLayerCandidatesJson added.
  - AppExportSvgAnnotationTests: 8 new assertions (file exists, method string,
    converted_to_map_geometry false, borough key, label key, SVG Layer Candidates
    in HTML, metadata-candidates language, no-geometry-conversion note).
  - Total: 285 tests (190 Core + 95 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No coordinate extraction. No geometry conversion. No PZ assets. No media/maps writes.

---

## [Unreleased]

### Added (SVG-3: SVG structure viewer panel)
- src/PZMapForge.Cli/Program.cs:
  - SvgStructureResult sealed class: in-memory result from WriteSvgStructure.
    Carries parse_status, parse_error, source_file_name, file_size_bytes,
    root_element, width, height, viewBox, element counts (g/path/polyline/
    polygon/line/rect/text), sample_ids, sample_text_labels.
  - WriteSvgStructure now returns SvgStructureResult (changed from void).
    JSON artifact still written identically.
  - BuildSvgStructureHtml: builds "SVG Structure Summary" HTML panel from
    the in-memory result (no JSON re-parse). Includes: parse_status badge
    (clean/dirty), parse_error note when non-empty, metadata table, "Element
    Counts" sub-table, "Sample IDs" chip list, "Sample Text Labels" chip list,
    non-conversion note ("not converted to map geometry"), artifact links.
  - CSS: .svg-sub, .svg-chips, .svg-chip added.
  - svgStructureSectionHtml now populated by BuildSvgStructureHtml.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - 6 new assertions in AppExportSvgAnnotationTests: SVG Structure Summary,
    parse_status in HTML, Element Counts, Sample IDs, Sample Text Labels,
    not-converted language. Total: 277 tests (190 Core + 87 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No SVG geometry conversion. No PZ assets. No media/maps writes.

---

## [Unreleased]

### Fixed (SVG-2A: large SVG structure inspection)
- src/PZMapForge.Cli/Program.cs: WriteSvgStructure
  - DtdProcessing changed from Prohibit to Ignore so SVGs with DOCTYPE
    declarations (e.g., real Montreal arrondissements SVG) parse correctly.
    XmlResolver=null still blocks all external entity resolution.
  - MaxCharactersInDocument raised from 10_000_000 to 50_000_000 to handle
    real-world SVG files (e.g., 12.3 MB Montreal arrondissements SVG).
  - catch block now captures Exception.Message instead of discarding it.
  - Three new fields added to svg-reference-structure.json:
    parse_status ("parsed" or "failed"), parse_error (error message or ""),
    max_characters_in_document (50_000_000).
  - Parse failures are recorded honestly; structure JSON is always written.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - 4 new assertions in AppExportSvgAnnotationTests: parse_status present,
    parse_status is "parsed", parse_error is empty, max_characters_in_document
    present. Total: 271 tests (190 Core + 81 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

SVG paths are counted but not converted. No PZ assets. No media/maps writes.

---

## [Unreleased]

### Added (SVG-2: SVG reference structure inspector)
- src/PZMapForge.Cli/Program.cs:
  - using System.Xml, System.Xml.Linq added.
  - WriteSvgStructure: safely parses SVG XML (DtdProcessing.Prohibit, XmlResolver=null,
    10MB limit). Counts 12 element types (svg/g/path/polyline/polygon/line/rect/circle/
    ellipse/text/image/use). Collects sample_ids, sample_classes, sample_text_labels
    (max 20 each). Writes svg-reference-structure.json to artifacts/ with schema
    pzmapforge.svg-reference-structure.v0.1 and all safety flags
    (parsed_as_geometry: false, converted_to_map_geometry: false).
  - BuildAppHtml: svgStructureSectionHtml parameter added; {{svgStructureSectionHtml}}
    slot inserted before Non-claims in right panel.
  - AppExportCommand: WriteSvgStructure called when annotation is SVG; svgStructure-
    SectionHtml computed and passed to BuildAppHtml.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportSvgFixture SVG updated to include g/path/polygon/text with id/class.
  - SvgStructureExists + SvgStructureJson properties added to fixture.
  - AppExportSvgAnnotationTests: 7 new tests (structure file exists, parsed_as_geometry
    false, converted_to_map_geometry false, path element count present, districts id
    present, District A text label present, SVG Structure in HTML).
  - Total: 267 tests (190 Core + 77 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

SVG paths are counted but not extracted or interpreted as geometry.
No SVG-to-PZ conversion. No PZ assets. No media/maps writes.

---

## [Unreleased]

### Added (APP-7: SVG annotation reference support)
- src/PZMapForge.Cli/Program.cs:
  - using System.Text.Json added.
  - Step 8c: SVG extension detection (annotExt == ".svg").
  - When SVG: annotPanelLabel = "SVG Vector Reference"; annotGuidanceHtml set with
    "SVG is not parsed into map geometry" guidance note; svg-reference-summary.json
    written to artifacts/ with schema v0.1 and all safety flags (parsed_as_geometry:
    false, pz_assets_copied: false, media_maps_touched: false, playable_export_claimed:
    false).
  - BuildAppHtml: annotPanelLabel + annotGuidanceHtml parameters added; annotColHtml
    uses annotPanelLabel; {{annotGuidanceHtml}} slot inserted after preview row;
    .svg-note CSS class added.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - AppExportSvgFixture: shared fixture runs once with minimal SVG annotation.
  - AppExportSvgAnnotationTests: 6 tests (exit 0, SVG file written, summary JSON
    written, SVG Vector Reference in HTML, not-parsed-geometry in HTML,
    parsed_as_geometry:false in JSON).
  - Total: 260 tests (190 Core + 70 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No proof packet update. No SVG parsing. No PZ assets read/copied. No media/maps writes.

---

## [Unreleased]

### Added (APP-6: annotation-safe app export workflow)
- src/PZMapForge.Cli/Program.cs:
  - --annotation <image>: optional argument parsed with -a shortform.
  - If provided and file does not exist, exits 1 with clear error.
  - Annotation image copied to images/annotation-image.<ext> (not parsed).
  - BuildAppHtml: Annotation Reference panel added conditionally when annotation
    is provided (annotColHtml injected into preview-row flex container).
  - "Original Input" label renamed to "Analysis Input" throughout.
  - preview-pair grid replaced with preview-row flex (handles 2 or 3 images).
  - Palette Health guidance updated to: "Use --path for a clean palette-only
    analysis image. Text labels and antialiasing should not be part of the
    analysis image -- they produce a not palette-clean result."
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - ContainsOriginalInputLabel renamed to ContainsAnalysisInputLabel.
  - ContainsTextLabelsGuidance assertion updated to new guidance text.
  - ContainsCleanPaletteOnlyGuidance added (+1 content test).
  - AppExportAnnotationTests class: WithAnnotation_WritesFileAndUpdatesHtml,
    MissingAnnotationFile_ExitsOne (+2 process tests).
  - Total: 254 tests (190 Core + 64 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No proof packet update. No PZ assets read/copied. No media/maps writes.

---

## [Unreleased]

### Added (APP-5: palette health and parsed preview)
- src/PZMapForge.Cli/Program.cs:
  - WriteParsedPreview: generates images/parsed-preview.png by rendering each pixel
    in its snapped palette color from the SemanticGrid + Legend. Uses System.Drawing.
    Fallback color #282828 for unrecognized codes. Written under images/ alongside
    the copied input image.
  - BuildAppHtml: Map Preview section now shows "Original Input" and "Parsed Preview"
    side by side (preview-pair grid). Added Palette Health section to right panel:
    health badge (Palette clean / Not palette-clean / Unknown), conditional guidance
    text, and always-visible note "A blockout is not palette-clean when... Text labels
    and antialiasing can affect parsing."
  - healthLabel/healthClass/healthGuidance computed from doc.Matching.UnmappedExactColours.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - Test 1 now also asserts images/parsed-preview.png exists.
  - AppExportContentFixture: ParsedPreviewExists property added.
  - AppExportContentTests: 6 new assertions (WritesParsedPreview, OriginalInputLabel,
    ParsedPreviewLabel, PaletteHealthSection, NotPaletteCleanText, TextLabelsGuidance).
  - Total: 251 tests (190 Core + 61 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No proof packet update. No PZ assets read/copied. No media/maps writes.

---

## [Unreleased]

### Changed (APP-4: improve image-to-map app workbench layout)
- src/PZMapForge.Cli/Program.cs: BuildAppHtml redesigned as two-column workbench.
  Left panel: Map Preview (image up to 600px, pixelated) + Visual Legend + drift.
  Right panel: Summary cards + metadata table + Artifact Files + Non-claims.
  Responsive media query stacks to single column below 860px. Section headers
  renamed: Map Preview, Summary, Visual Legend, Artifact Files, Non-claims.
  Artifact Files section combines all 7 links (JSON + MD). @media closing braces
  split to avoid raw string literal parse conflict.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs: ContainsJsonArtifactsSection
  and ContainsMarkdownReportsSection replaced by ContainsArtifactFilesSection;
  added ContainsMapPreviewSection, ContainsSummarySection, ContainsWorkbenchClass.
  Total: 245 tests (190 Core + 55 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No proof packet update. No PZ assets read/copied. No media/maps writes.

---

## [Unreleased]

### Changed (APP-3: improve image-to-map app blockout UX)
- src/PZMapForge.Cli/Program.cs:
  - --run-name <name>: output goes to <output>/<sanitized-name>/; unsafe chars
    (spaces, punctuation) sanitized to hyphens; empty result after sanitization
    exits 1.
  - Visual Legend section: color swatches from palette legend, pixel counts from
    parsed-cell counts, color match summary bar (exact/nearest/unmapped/unique).
  - Nearest Color Drift section: table of nearest-snapped pixels; omitted if all
    exact matches.
  - Palette kinds card added to Pipeline Summary row.
  - Palette file name + kind count shown in Input metadata table.
  - Artifacts split into JSON Artifacts + Markdown Reports subsections.
  - BuildLegendHtml, BuildDriftHtml, SanitizeRunName helpers added.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs:
  - Tests 6-7: run-name subdirectory and unsafe-name sanitization process tests.
  - AppExportContentFixture + AppExportContentTests: shared fixture (one pipeline
    run) with 6 contract assertions: exit 0, Visual Legend, swatch class, Palette
    name, JSON Artifacts section, Markdown Reports section.
  - Total: 243 tests (190 Core + 53 CLI).
- docs/IMAGE_TO_MAP_APP.md, docs/IMPLEMENTATION.md, CHANGELOG.md: updated.

No proof packet update. No PZ assets read/copied. No media/maps writes.

---

## [Unreleased]

### Changed (APP-2: improve local image-to-map app viewer)
- src/PZMapForge.Cli/Program.cs: BuildAppHtml redesigned. Added summary cards
  (dimensions, regions, primitives, recommendations, warnings). Input section
  now shows input image (copied to images/input-image.<ext>) alongside metadata
  table. Artifact section uses card-style links. Dark-themed CSS. No JS framework.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs: tests 1 and 3 updated to
  assert input image file is copied and index.html contains img element and card
  markup. All 235 tests pass (190 Core + 45 CLI).
- docs/IMAGE_TO_MAP_APP.md: viewer description updated.
- docs/IMPLEMENTATION.md: APP-2 row added.
- CHANGELOG.md: this entry.

No proof packet update. No PZ assets read/copied. No media/maps writes.

---

## [Unreleased]

### Added (Slice 3A-6 app: local image-to-map app export)
- src/PZMapForge.Cli/Program.cs: app-export command. Accepts --path, --palette,
  --output, --resize, --tiny-threshold, --large-threshold. Runs full pipeline;
  writes artifacts/ (parsed-cell.json, regions.json, regions-report.md,
  primitives.json, primitives-report.md, plan-recommendations.json,
  plan-report.md) and index.html to --output. Output guard: must contain .local
  path segment. Static HTML, no JS framework, no server.
- tests/PZMapForge.Cli.Tests/AppExportProcessTests.cs: 5 process tests.
  Valid image writes index.html (exit 0); index.html contains claim boundary;
  index.html links artifact file names; non-.local output rejected (exit 1);
  missing --path exits 1. All tests use 300x300 programmatic PNG, no real PZ install.
- docs/IMAGE_TO_MAP_APP.md: operator guide covering command, inputs, outputs,
  pipeline stages, HTML viewer contents, safety boundaries, non-claims, next candidates.

### Changed
- docs/IMPLEMENTATION.md: Slice 3A-6 app row added.
- CHANGELOG.md: this entry.

Tests: 235/235 (190 Core + 45 CLI).
No PZ assets copied or read. No media/maps writes.

---

## [Unreleased]

### Added (Slice 3A-6-pre: tilesheet format investigation decision record)
- docs/TILESHEET_FORMAT_INVESTIGATION_DECISION.md: governance gate for
  Slice 3A-6. Documents why 3A-6 is blocked, what format knowledge is
  required, allowed investigation sources, forbidden actions, 4 decision
  options (A-D), recommended decision (Option B + D), evidence checklist,
  and operator checklist. No code changes.

### Changed
- docs/IMPLEMENTATION.md: Slice 3A-6-pre row added.
- CHANGELOG.md: this entry.

No code changes. No new tests. No proof packet bump.
No PZ assets copied or read. No media/maps writes.

---

## [Unreleased]

### Added (Slice 3A-5: local tile survey CLI hardening and docs)
- docs/LOCAL_TILE_SURVEY_CLI.md: operator-facing CLI reference for the
  local-tile-survey command. Covers purpose, command syntax, fake-path
  example, output files, validation behavior, safety guarantees, non-claims,
  and troubleshooting table.

### Changed
- docs/IMPLEMENTATION.md: Slice 3A-5 row added.
- CHANGELOG.md: this entry.

No code changes. No new tests. No proof packet bump.
No PZ assets copied or read. No media/maps writes.

---

## [Unreleased - v0.16 proof packet sync]

### Added
- schemas/pzmapforge.proof-packet.v0.16.schema.json: bump from v0.15.
  test_total 225->230, cli_tests 35->40. Adds local_tile_survey_cli_*
  evidence flags (present, no real install for tests, local-only output,
  no assets copied/read, no media/maps). proof_packet 96->102,
  total_expected_assertions 468->474.

### Changed
- scripts/write-proof-packet.ps1, test-proof-packet.ps1, test-schema-files.ps1,
  validate.ps1: updated to v0.16 with new counts and CLI evidence flags.
- docs/VALIDATION_LEDGER.md, docs/IMPLEMENTATION.md: updated counts.
- CHANGELOG.md: this entry.

---
## [Unreleased]

### Added (Slice 3A-4: local tile reference survey CLI command)
- local-tile-survey CLI command: --config <path> [--output <dir>].
  Loads local PZ install config via LocalPzInstallConfigLoader, validates
  the install filesystem via LocalPzInstallValidator, writes survey artifacts
  via LocalTileReferenceSurveyWriter. --output must end with a .local
  directory segment (refused otherwise). Defaults to <cwd>/.local. Prints
  schema, claim_boundary, install/tiles presence, artifact paths, and all
  safety flag values. No assets read or copied. No media/maps writes.
- tests/PZMapForge.Cli.Tests/LocalTileSurveyProcessTests.cs: 5 process tests
  using temp fake install directories (no real PZ install required). Tests:
  valid config writes artifacts, non-.local output refused, missing --config
  exits 1, JSON contains claim boundary fields, markdown contains non-claims.
  All tests use --configuration Release --no-build.

### Changed
- src/PZMapForge.Cli/Program.cs: local-tile-survey command added; using added;
  help text and UnknownCommand updated.
- docs/IMPLEMENTATION.md: Slice 3A-4 row added.
- CHANGELOG.md: this entry.

No PZ assets copied or read. No media/maps writes.
claim_boundary = planning_artifact_only_not_pz_load_tested preserved.

### Added

- schemas/pzmapforge.proof-packet.v0.15.schema.json: proof packet schema synchronized after Slice 3A-3.
- scripts/write-proof-packet.ps1: proof packet writer updated to v0.15 with PS 468 and .NET 225 evidence counts.
- scripts/test-proof-packet.ps1: proof packet contract updated to v0.15 with LocalTileReferenceSurveyWriter evidence flags.
- scripts/test-schema-files.ps1 and scripts/validate.ps1: validation counts updated for proof packet v0.15.
- docs/VALIDATION_LEDGER.md and docs/IMPLEMENTATION.md: validation ledger and implementation status updated for v0.15.

- schemas/pzmapforge.local-tile-reference-survey.v0.1.schema.json: local-only survey artifact schema for Slice 3A-3.
- src/PZMapForge.Core/LocalPz/LocalTileReferenceSurvey.cs: survey artifact model.
- src/PZMapForge.Core/LocalPz/LocalTileReferenceSurveyWriter.cs: writer for .local/local-tile-reference-survey.json and .local/local-tile-reference-survey.md.
- tests/PZMapForge.Core.Tests/LocalPz/LocalTileReferenceSurveyWriterTests.cs: writer tests using validation summary data only.
- docs/LOCAL_TILE_REFERENCE_SURVEY.md: Slice 3A-3 artifact contract.

- src/PZMapForge.Core/LocalPz/LocalPzInstallValidator.cs: read-only local install validator for Slice 3A-2.
- src/PZMapForge.Core/LocalPz/LocalPzInstallValidationResult.cs: validator result model.
- src/PZMapForge.Core/LocalPz/LocalPzInstallValidationSummary.cs: extension count and safety summary model.
- tests/PZMapForge.Core.Tests/LocalPz/LocalPzInstallValidatorTests.cs: validator tests using temporary fake installs only.

### Changed

- tests/PZMapForge.Cli.Tests/FullPipelineContractTests.cs: process CLI invocation now
  passes --configuration Release --no-build to avoid triggering nested Debug builds
  (VBCSCompiler file lock on PZMapForge.Core.dll during parallel test execution).
- tests/PZMapForge.Cli.Tests/LayerPipelineProcessTests.cs: same fix applied.
- docs/PHASE_3A_DECISION.md: Slice 3A-2 status added.
- docs/PHASE_3_LOCAL_PZ_CONFIG_SPEC.md: local install validator status added.
- docs/IMPLEMENTATION.md: LocalPz validator row added; CLI process test stabilization noted.

No real PZ install is required for tests.
No PZ assets are copied.
No PZ asset contents are read.
No media/maps writes.
No tile catalog, semantic mapping, lotpack, lotheader, bin, or playable export claim.
### Added
- docs/PHASE_3A_DECISION.md: Phase 3A decision record. Media layout survey
  2026-06-02 confirmed: local install present, media/ layout known (33 subdirs),
  .pack (~20) and .tiles (~7) formats confirmed as likely tile containers,
  media/tiles/ absent (tiles not in classic path). Decision: Phase 3A-1 (local
  config schema + loader) is unblocked. Tile catalog, semantic kind mapping, and
  PZ export remain blocked. Slice 3A-1 definition included. Safety rules listed.
  No real paths, file names, or asset data committed.
- schemas/pzmapforge.local-pz-install-config.v0.1.schema.json: local-only Phase 3A config schema.
- src/PZMapForge.Core/LocalPz/: typed loader for local PZ install config documents.
- tests/PZMapForge.Core.Tests/LocalPz/LocalPzInstallConfigLoaderTests.cs: config loader tests.
- tests/fixtures/local-pz/valid-local-pz-install-config.json: placeholder-only valid config fixture.

### Changed
- docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md: status updated (COMPLETE); media
  layout survey result section added; first survey archived.
- README.md: link to PHASE_3A_DECISION.md added.
- docs/IMPLEMENTATION.md: Phase 3A decision record row added.
- CHANGELOG.md: this entry.
- docs/PHASE_3A_DECISION.md: Slice 3A-1 marked implemented.
- docs/PHASE_3_LOCAL_PZ_CONFIG_SPEC.md: local config schema/loader status updated.
- docs/IMPLEMENTATION.md: local PZ config loader row added.

No real PZ install is required.
No PZ assets inspected or copied.
No .local config committed.
No media/maps writes.
No tile catalog, semantic mapping, lotpack, lotheader, bin, or playable export claim.
No code changes. No validation count changes.
Phase 3A-1: UNBLOCKED -- config schema + loader may now be implemented.

### Changed
- docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md: status updated; "Latest manual-path
  survey status" section added (redacted result: install found, 33 media/
  subdirs, media/tiles/ absent, tile files not found, Phase 3 BLOCKED).
  Interpretation added (tile directory not at expected path; next step needed).
  "Next manual survey step: locate tile-bearing directories" section added
  with safe PS commands and redacted placeholder table. No real paths or
  asset data committed.
- docs/IMPLEMENTATION.md: Phase 3A survey status row updated.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.
Phase 3 implementation status: BLOCKED.

### Changed
- docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md: status updated to "automated
  discovery found no install"; "Latest automated survey status" section added
  (run date, 4 paths checked, result, Phase 3 implementation status NOT STARTED,
  6-step operator action sequence). No real paths or asset data included.
- docs/IMPLEMENTATION.md: Phase 3A survey status row added.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.
Phase 3 implementation status: NOT STARTED.

### Added
- scripts/Run-Phase3ALocalPzSurvey.ps1: read-only Phase 3A survey helper.
  Accepts optional -PzRoot parameter. Searches 4 common Steam paths for PZ.
  If found: inventories media/ and media/tiles/ (file count/extensions
  bucketed), searches tilesheet names for semantic-kind keywords, probes
  build version. Writes full local report (with paths) and redacted report
  (yes/no + buckets, no exact paths/names) to .local/pzmapforge/surveys/.
  Exits 0 even when PZ install is not found. Never copies assets.

### Changed
- docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md: "Claude-assisted survey helper"
  section added (how to run, what is automated, what the operator must verify).
- README.md: survey helper command added under Phase 3A link.
- docs/IMPLEMENTATION.md: survey helper row added.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.
PZ install not found at default paths; operator must pass -PzRoot if PZ
is installed at a non-standard location.

### Added
- docs/PHASE_3A_LOCAL_INSTALL_SURVEY.md: operator survey guide for local PZ
  install inventory. Provides 8-step PowerShell survey (locate install, check
  build version, list media/, survey tiles/, sample tilesheet names, count
  asset types, search naming conventions, capture output to .local/).
  Specifies placeholder table for committed PHASE_3A_DECISION.md, what not to
  copy/commit, and the next decision gate before any code is written.

### Changed
- README.md: link to PHASE_3A_LOCAL_INSTALL_SURVEY.md added.
- docs/IMPLEMENTATION.md: Phase 3A survey row added.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.

### Added
- docs/PHASE_3_LOCAL_PZ_CONFIG_SPEC.md: precondition documentation for Phase 3
  local PZ install / tile ID mapping. Defines what Phase 3 may and may not do,
  proposed local config path (.local/pzmapforge/pz-install-config.json), proposed
  config schema shape, local install path handling, tilesheet discovery rules,
  tile reference rules, no-copy/no-write/no-playable-export rules, 8 mandatory
  safety checks, evidence required before implementation, and 5 proposed slices
  (3A-1: config loader, 3A-2: install validator, 3A-3: tile catalog, 3A-4: kind
  mapping, 3A-5: local-tile TMX export). Status: precondition doc only.

### Changed
- README.md: link to PHASE_3_LOCAL_PZ_CONFIG_SPEC.md added.
- docs/IMPLEMENTATION.md: Phase 3 precondition row added.
- docs/PHASE_2B_OR_PHASE_3_DECISION.md: Phase 3 precondition marked started;
  conditions to begin 3A-1 listed.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.

### Added
- schemas/pzmapforge.proof-packet.v0.12.schema.json: bump from v0.11. Adds
  layer_validate_present (true) and layer_validate_writes_artifacts (false) to
  dotnet_validation_summary. Updates test_total 184->197, core_tests 154->162,
  cli_tests 30->35. Updates proof_packet 79->81, total_expected_assertions 391->393.

### Changed
- scripts/write-proof-packet.ps1: schema v0.12; dotnet counts updated; two new
  fields added; markdown report updated.
- scripts/test-proof-packet.ps1: schema sentinel v0.12; dotnet assertions updated;
  2 new assertions (81 total, was 79).
- scripts/test-schema-files.ps1: proof-packet check updated from v0.11 to v0.12
  (same 17 top-level CheckRequired; schema sanity stays at 136).
- scripts/validate.ps1: .NET lane constants updated (Core 162, CLI 35, total 197);
  PS total updated to 393; version references to v0.12.
- docs/VALIDATION_LEDGER.md: baseline commit updated; .NET counts updated; test
  breakdowns updated; proof packet section updated to 81 assertions.
- docs/IMPLEMENTATION.md: proof packet v0.12 row added.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  197/197
test-proof-packet.ps1: 81/81
test-schema-files.ps1: 136/136
validate.ps1: PS 393 + .NET 197, Validation passed

### Added
- src/PZMapForge.Core/Layers/LayerValidationLayerResult.cs: per-layer result
  (IsValid, Errors, NonDefaultPixels, InvalidPixels, Width, Height).
- src/PZMapForge.Core/Layers/LayerValidationResult.cs: overall result
  (IsValid, Errors, LayerResults, Precedence, ClaimBoundary).
- src/PZMapForge.Core/Layers/LayerValidator.cs: Validate(manifestPath,
  palettePath, options) loads manifest, checks image existence, parses each
  image via ImageMapForgeParser, enforces allowed_kinds, reports invalid pixels.
  Windows-only (GDI+). No artifact output.
- tests/PZMapForge.Core.Tests/Layers/LayerValidatorTests.cs: 8 tests
  (valid layer, missing image, disallowed kind, non-square without/with resize,
  invalid manifest, determinism, claim boundary).
- tests/PZMapForge.Cli.Tests/LayerValidateProcessTests.cs: 5 process tests
  (valid exits 0, missing image exits 1, disallowed kind exits 1,
  non-square without/with --resize).

### Changed
- src/PZMapForge.Cli/Program.cs: layer-validate command added; help text and
  UnknownCommand updated.
- README.md: layer-validate usage added.
- docs/LAYER_AUTHORING_GUIDE.md: workflow updated to include layer-validate step.
- docs/IMPLEMENTATION.md: LayerValidator row added.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  197/197 (162 Core + 35 Cli)
PS lane:      391 assertions unchanged
validate.ps1: Validation passed
layer-validate against example-2b: 4 layers, all OK

### Added (Slice 2B-2: example layer image generator)
- tests/fixtures/layers/example-2b/new-example-images.ps1: generates 4
  deterministic 300x300 PNGs using System.Drawing with exact palette RGB
  values. terrain (grass + industrial_yard), roads (road grid + sidewalk
  bands), buildings (row_house, depanneur, garage, landmark), markers (spawn).
  Spawn at (70,70,6x6) intentionally overlaps row_house: 36 conflict cells,
  markers wins. Script prints expected output and pipeline command.
- tests/fixtures/layers/example-2b/generated-layer-manifest.json: manifest
  referencing generated/terrain.png, generated/roads.png, generated/buildings.png,
  generated/markers.png. Run after new-example-images.ps1 to exercise the full
  layer-pipeline end-to-end.

### Changed
- .gitignore: tests/fixtures/layers/example-2b/generated/ pattern added.
- tests/fixtures/layers/example-2b/README.md: generator instructions, layout
  table with pixel coordinates, known conflict documented, runnable steps.
- docs/IMPLEMENTATION.md: Slice 2B-2 row added.
- docs/PHASE_2B_OR_PHASE_3_DECISION.md: Slice 2B-2 marked complete; next steps.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.
Pipeline verified: 4 layers merged, 36 conflicts, 15 regions, Status OK.

### Added (Slice 2B-1: layer authoring guide and fixture examples)
- docs/LAYER_AUTHORING_GUIDE.md: claim boundary, what layer-pipeline does,
  required manifest shape, standard layer names, kind-by-layer table,
  precedence policy, default kind, conflict policy, authoring workflow (8 steps),
  naming conventions, error glossary (7 errors), non-claims.
- tests/fixtures/layers/README.md: fixture folder purpose, binary-image policy,
  contents table, guidance for adding new fixtures.
- tests/fixtures/layers/example-2b/layer-manifest.json: v0.1 manifest covering
  all 9 kinds across 4 layers with standard precedence.
- tests/fixtures/layers/example-2b/README.md: expected image descriptions per
  layer (palette RGB values included), how to run layer-pipeline, why PNGs
  are not committed.

### Changed
- README.md: link to LAYER_AUTHORING_GUIDE.md added.
- docs/IMPLEMENTATION.md: Slice 2B-1 rows added.
- docs/PHASE_2B_OR_PHASE_3_DECISION.md: Slice 2B-1 marked complete;
  Slice 2B-2 candidate noted.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.

### Added
- docs/PHASE_2B_OR_PHASE_3_DECISION.md: Phase 2B (layer authoring conventions)
  chosen over Phase 3 (local PZ install / tile ID mapping). Documents Option A
  vs B comparison, rationale, non-claims, and Slice 2B-1 definition
  (layer authoring guide + fixture examples). Phase 3 deferred pending
  Phase 2B stability and a documented local load test mechanism.

### Changed
- README.md: link to PHASE_2B_OR_PHASE_3_DECISION.md added.
- docs/IMPLEMENTATION.md: Phase 2B/3 decision row added.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.

### Added
- schemas/pzmapforge.proof-packet.v0.11.schema.json: bump from v0.10. Adds
  layer_pipeline_present, layer_pipeline_artifact_count, layer_pipeline_artifacts
  (8 items) to dotnet_validation_summary. Updates test_total 152->184,
  core_tests 123->154, cli_tests 29->30. Updates proof_packet 69->79,
  total_expected_assertions 381->391.

### Changed
- scripts/write-proof-packet.ps1: schema v0.11; dotnet counts updated; layer
  pipeline fields added; markdown report updated.
- scripts/test-proof-packet.ps1: schema sentinel v0.11; dotnet assertions
  updated; 10 new assertions for layer_pipeline fields (79 total, was 69).
- scripts/test-schema-files.ps1: proof-packet check updated from v0.10 to v0.11
  (same 17 top-level CheckRequired; schema sanity stays at 136).
- scripts/validate.ps1: .NET lane constants updated (Core 154, CLI 30, total 184);
  all version references updated to v0.11; PS total updated to 391.
- docs/VALIDATION_LEDGER.md: baseline commit updated; .NET counts updated;
  test breakdowns updated; layer-pipeline artifact surface section added;
  proof packet section updated to 79 assertions.
- docs/IMPLEMENTATION.md: proof packet v0.11 row added.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  184/184
test-proof-packet.ps1: 79/79
test-schema-files.ps1: 136/136
validate.ps1: PS 391 + .NET 184, Validation passed

### Added (Slice 2A-3: layer pipeline CLI command and artifact writer)
- src/PZMapForge.Core/Layers/LayerMergeArtifactWriter.cs: Write(outputDir,
  manifestPath, palettePath, palette, mergeResult, options) writes
  parsed-cell.json (compatible with ParsedCellLoader, all 9 kinds in counts,
  schema/boundary/dims validated) and layer-merge-report.md (claim boundary,
  manifest path, dimensions, default kind, total conflict count, per-layer
  contribution table, conflict sample table when conflicts exist).
- tests/PZMapForge.Core.Tests/Layers/LayerMergeArtifactWriterTests.cs: 7 tests
  (file creation, report claim boundary, contribution table, conflict count,
  parsed-cell loadable by ParsedCellLoader, determinism).
- tests/PZMapForge.Cli.Tests/LayerPipelineProcessTests.cs: 1 process test;
  creates layer images + manifest in temp dir, runs layer-pipeline, asserts
  8 artifacts present and claim boundary in merge report + plan report.

### Changed
- src/PZMapForge.Cli/Program.cs: layer-pipeline command added; duplicate using
  directive removed; help text and UnknownCommand updated.
- README.md: layer-pipeline command example added.
- docs/PHASE_2_DECISION.md: Slice 2A-3 marked complete; next steps noted.
- docs/IMPLEMENTATION.md: writer + CLI rows added; multi-layer row updated.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  184/184 (154 Core + 30 Cli)
PS lane:      381 assertions unchanged
validate.ps1: Validation passed

### Added (Slice 2A-2: layer merger)
- src/PZMapForge.Core/Layers/LayerMergeOptions.cs: Resize, DefaultKind.
- src/PZMapForge.Core/Layers/LayerMergeContribution.cs: per-layer pixel
  contribution stats (contributed, ignoredDefault, invalid, chosen, overridden).
- src/PZMapForge.Core/Layers/LayerMergeConflict.cs: conflict record (x, y,
  chosenLayer, chosenKind, losingLayers, losingKinds).
- src/PZMapForge.Core/Layers/LayerMergeResult.cs: IsValid, Errors, Width,
  Height, Rows, Grid, Contributions, TotalConflictCount, ConflictSample,
  ClaimBoundary.
- src/PZMapForge.Core/Layers/LayerMerger.cs: Merge(manifestPath, palettePath,
  options) - validates manifest, loads palette, resolves and parses all layer
  images via ImageMapForgeParser, validates allowed_kinds, merges into one
  SemanticGrid using precedence (highest-first), tracks per-layer contributions
  and conflicts (sample capped at 100).
- tests/PZMapForge.Core.Tests/Layers/LayerMergerTests.cs: 12 tests.

### Changed
- docs/PHASE_2_DECISION.md: Slice 2A-2 marked complete; Slice 2A-3 defined.
- docs/IMPLEMENTATION.md: merger row added; multi-layer row updated.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  176/176 (147 Core + 29 Cli)
PS lane:      381 assertions unchanged
validate.ps1: Validation passed

### Added (Slice 2A-1: layer manifest schema and loader foundation)
- schemas/pzmapforge.layer-manifest.v0.1.schema.json: JSON Schema for the
  layer manifest format (schema, claim_boundary, width, height, layers array,
  precedence array).
- src/PZMapForge.Core/Layers/LayerManifest.cs: root manifest POCO.
- src/PZMapForge.Core/Layers/LayerManifestLayer.cs: per-layer entry (name,
  path, allowed_kinds).
- src/PZMapForge.Core/Layers/LayerManifestLoadResult.cs: result type matching
  existing loader pattern (IsValid, Errors, Document).
- src/PZMapForge.Core/Layers/LayerManifestLoader.cs: Load(path) validates
  schema sentinel, claim_boundary, 300x300 dimensions, layer non-emptiness,
  unique layer names, non-empty paths, non-empty allowed_kinds, all kinds
  known via PrimitiveClassifier.IsKnownKind, precedence completeness,
  no unknown/duplicate precedence entries.
- tests/PZMapForge.Core.Tests/Layers/LayerManifestLoaderTests.cs: 12 tests.
- tests/fixtures/layers/valid-layer-manifest.json: 4-layer fixture.

### Changed
- docs/PHASE_2_DECISION.md: Slice 2A-1 marked complete; Slice 2A-2 defined.
- docs/IMPLEMENTATION.md: loader row added; multi-layer row updated.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  164/164 (135 Core + 29 Cli)
PS lane:      381 assertions unchanged
validate.ps1: Validation passed

### Added
- docs/PHASE_2_DECISION.md: Phase 2 decision record. Option A (multi-layer
  image conventions) chosen over Option B (PZ tile ID mapping). Documents
  current verified capability, option comparison, risks, non-claims,
  recommendation rationale, and first implementation slice (2A-1: layer
  manifest schema + loader foundation). Option B deferred pending Phase 2
  stability and documented local load test mechanism.

### Changed
- README.md: link to PHASE_2_DECISION.md added.
- docs/IMPLEMENTATION.md: Phase 2 decision row added; multi-layer row updated.
- CHANGELOG.md: this entry.

No code changes. No validation count changes.

---

## Previous

### Changed
- scripts/validate.ps1: final output now prints the full validation ledger
  summary. PowerShell lane (381 total, 9 checks) and .NET lane (152 total,
  Core 123 + CLI 29) are shown as separate tables. Both are stated not to be
  summed. Claim boundary printed. "Validation passed." still the final line.
  Constants sourced from proof-packet v0.10 / VALIDATION_LEDGER.md; comment
  in script directs maintainer to update proof packet schema and ledger too.
- docs/IMPLEMENTATION.md: validate.ps1 ledger summary row added.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  152/152
test-proof-packet.ps1: 69/69 (unchanged)
validate.ps1: Validation passed. PS 381 + .NET 152.

---

## Previous

### Added
- docs/VALIDATION_LEDGER.md: operator-readable ledger for both validation lanes.
  Documents PowerShell lane (381 assertions, 9 scripts), .NET lane (152 tests,
  breakdown by project), proof packet lane (69 assertions), full-pipeline artifact
  surface (7 artifacts with content contracts), full validation command sequence,
  and explicit non-claims.

### Changed
- README.md: link to VALIDATION_LEDGER.md added; Quickstart assertion count
  updated from 285 to 381; full-pipeline artifact list updated from 5 to 7.
- docs/IMPLEMENTATION.md: validation ledger row added.
- CHANGELOG.md: this entry.

dotnet build: 0 errors
dotnet test:  152/152
validate.ps1: Validation passed (no count changes)

---

## Previous

### Added
- schemas/pzmapforge.proof-packet.v0.10.schema.json: proof packet schema bumped to v0.10.
  Adds dotnet_validation_summary section (test_total, core_tests, cli_tests, process/contract
  booleans, artifact_count, artifact list, note). PS validation_summary counts updated:
  schema_file_sanity=136, proof_packet=69, total_expected_assertions=381.

### Changed
- scripts/write-proof-packet.ps1: schema Ã¢â€ â€™ v0.10; dotnet_validation_summary block added;
  validation_summary counts updated (136/69/381); markdown report updated.
- scripts/test-proof-packet.ps1: schema sentinel Ã¢â€ â€™ v0.10; dotnet_validation_summary field
  added to required check; 13 new dotnet section assertions (69 total, was 55).
- scripts/test-schema-files.ps1: proof-packet check updated from v0.9 to v0.10;
  dotnet_validation_summary added to CheckRequired (136 total, was 134).
- docs/IMPLEMENTATION.md: proof packet row updated to v0.10.

dotnet build: 0 errors
dotnet test:  152/152 (123 Core + 29 Cli)
scripts/test-schema-files.ps1: 136 assertions pass
scripts/test-proof-packet.ps1: 69 assertions pass (6 new dotnet lane checks)
scripts/validate.ps1: 69 assertions pass, Validation passed

---

## Previous

### Added
- tests/PZMapForge.Cli.Tests/FullPipelineContractTests.cs: FullPipelineContractFixture
  (IClassFixture) runs full-pipeline once against a temp 300x300 grass image.
  6 contract tests: exit code, regions-report.md claim boundary + summary-by-kind,
  primitives-report.md claim boundary + summary-by-primitive-type,
  plan-report.md claim boundary.

### Changed
- CHANGELOG.md: contract test entry added.

dotnet build: 0 errors, 2 pre-existing warnings
dotnet test:  152/152 (123 Core + 29 Cli)
scripts/validate.ps1: 55 PS assertions pass.

---

## Previous

### Added
- src/PZMapForge.Core/Regions/RegionArtifactWriter.cs: Write() now returns
  (string JsonPath, string MdPath) and also writes regions-report.md with
  claim boundary, summary-by-kind table, top-20-regions table.
- src/PZMapForge.Core/Primitives/PrimitiveArtifactWriter.cs: Write() now returns
  (string JsonPath, string MdPath) and also writes primitives-report.md with
  claim boundary, summary-by-primitive-type table, top-20-primitives table.
- tests/PZMapForge.Core.Tests/Regions/RegionArtifactWriterTests.cs: 8 tests
  (5 JSON + 3 markdown: file exists, claim boundary, summary-by-kind).
- tests/PZMapForge.Core.Tests/Primitives/PrimitiveArtifactWriterTests.cs: 8 tests
  (5 JSON + 3 markdown: file exists, claim boundary, summary-by-primitive-type).

### Changed
- src/PZMapForge.Cli/Program.cs: full-pipeline now prints regions-report.md and
  primitives-report.md paths. 7 artifacts emitted total.
- tests/PZMapForge.Cli.Tests/CliProcessTests.cs: Test 7 now verifies 7 artifacts
  including regions-report.md and primitives-report.md.
- docs/IMPLEMENTATION.md: artifact writer rows updated to 8 tests each.
- docs/IMAGE_MAPFORGE.md: full-pipeline artifact list updated.

dotnet build: 0 errors, 0 warnings
dotnet test:  146/146 (123 Core + 23 Cli)
scripts/validate.ps1: 55 PS assertions pass.

---

## [Unreleased - prev32]

### Added
- tests/PZMapForge.Cli.Tests/CliProcessTests.cs: 10 process-level integration
  tests invoking the CLI via dotnet run and verifying exit codes and artifacts.
  1. image-check 300x300 -> exit 0, Status OK
  2. image-check 150x150 without --resize -> exit 1
  3. image-check 150x150 with --resize -> exit 0
  4. image-export writes parsed-cell.json
  5. plan-check on valid fixture -> exit 0
  6. plan-export writes plan-recommendations.json + plan-report.md
  7. full-pipeline writes all 3 artifacts
  8. full-pipeline refuses non-.local output -> exit 1
  9. plan-check --tiny-threshold abc -> exit 1
  10. plan-check --tiny-threshold -1 -> exit 1

### Changed
- tests/PZMapForge.Cli.Tests/PZMapForge.Cli.Tests.csproj: System.Drawing.Common
  10.0.8 added for programmatic test image creation.

dotnet build: 0 errors, 0 warnings
dotnet test:  130/130 (107 Core + 23 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev31]

### Added
- src/PZMapForge.Cli/Program.cs: full-pipeline --path --palette [--output]
  [--resize] [--tiny-threshold] [--large-threshold]. Chains:
  ImageMapForgeParser -> ImageMapForgeArtifactWriter (parsed-cell.json) ->
  ParsedCellLoader -> RegionExtractor -> PrimitiveClassifier ->
  PlanningRuleEngine -> PlanningArtifactWriter (plan-recommendations.json,
  plan-report.md). Default output .local/mapforge. Refuses non-.local output.
  Prints: parsed-cell path, plan paths, dims, resized, regions, primitives,
  recommendations, warnings, thresholds, status.
- tests/PZMapForge.Cli.Tests/CliSmokeTests.cs: 3 new full-pipeline coverage
  tests (ArtifactWriter accessible, PlanningArtifactWriter accessible, default
  output path under .local). 13 total CLI tests (was 10).

dotnet build: 0 errors, 0 warnings
dotnet test:  120/120 (107 Core + 13 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev30]

### Added
- src/PZMapForge.Core/ImageParsing/ImageMapForgeArtifactWriter.cs: writes
  parsed-cell.json from ImageMapForgeResult + PaletteDocument. Builds a full
  ParsedCellDocument (schema, tool, claim_boundary, source/palette paths and
  SHA-256, width/height/resized, matching, legend, counts, nearest_drift, rows,
  outputs). Output is loadable by ParsedCellLoader and compatible with the full
  .NET downstream pipeline.
- src/PZMapForge.Cli/Program.cs: image-export --path --palette [--output]
  [--resize] command. Defaults to .local/mapforge. Refuses output outside
  .local/. Calls ImageMapForgeArtifactWriter.Write().
- tests/PZMapForge.Core.Tests/ImageParsing/ImageMapForgeArtifactWriterTests.cs:
  9 xUnit tests (file created, schema, claim_boundary, dims, rows, counts,
  resized flag, determinism, loadable by ParsedCellLoader).

dotnet build: 0 errors, 0 warnings
dotnet test:  117/117 (107 Core + 10 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev29]

### Added
- src/PZMapForge.Cli/Program.cs: image-check --path --palette [--resize] command.
  Calls ImageMapForgeParser.Parse(). Prints image/palette paths, dimensions,
  resized flag, row count, kind count, exact/nearest/unmapped pixels, palette
  SHA-256, status. Exits 0 on success, 1 on error. Does not write artifacts.
- tests/PZMapForge.Cli.Tests/CliSmokeTests.cs: 3 new ImageMapForge smoke tests
  (DefaultResize=false, ResizeTrue construction, Parser accessible). 10 total
  CLI tests (was 7).

### Changed
- src/PZMapForge.Cli/PZMapForge.Cli.csproj: <NoWarn>CA1416</NoWarn> added
  (Windows-only CLI; System.Drawing.Common calls are intentional).

dotnet build: 0 errors, 0 warnings
dotnet test:  108/108 (98 Core + 10 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev28]

### Added
- tests/PZMapForge.Core.Tests/ImageParsing/ImageMapForgeParserCrossVerificationTests.cs:
  6 cross-verification tests comparing ImageMapForgeParser output against
  tests/fixtures/parsed-cell/valid.json:
  1. HeaderFields: width==300, height==300, resized==false, claim_boundary correct
  2. PaletteSha256: parser result matches actual source/image-palette.json hash
     (fixture has placeholder zeros; test documents this explicitly)
  3. AllRows: all 300 rows identical to fixture
  4. Counts: all 9 kinds with correct pixel counts match fixture
  5. MatchingStats: exact==90000, nearest==0, unique==9, unmapped==0
  6. Resize: 150x150 all-grass -> 300x300 with all 90000 grass pixels

dotnet test: 105/105 (98 Core + 7 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev27]

### Added
- src/PZMapForge.Core/ImageParsing/ImageMapForgeOptions.cs: Resize flag
  (default false).
- src/PZMapForge.Core/ImageParsing/ImageMapForgeResult.cs: Width, Height,
  Resized, PaletteSha256, Rows, Counts, Matching, NearestDrift, BuildGrid().
- src/PZMapForge.Core/ImageParsing/ImageMapForgeParser.cs: [SupportedOSPlatform
  windows] static Parse(imagePath, palettePath, options). Loads palette via
  PaletteLoader, computes palette SHA-256 from file bytes, loads image with
  System.Drawing.Common (GDI+), resizes with NearestNeighbor if Resize=true,
  scans pixels with exact-then-nearest-colour matching (drift cache per unique
  unmapped colour), returns ImageMapForgeResult.
- tests/PZMapForge.Core.Tests/ImageParsing/ImageMapForgeParserTests.cs:
  10 xUnit tests using programmatically created temp images. Covers 300x300,
  150x150 without/with Resize, dimension/row/count validation, resized flag,
  palette SHA-256, invalid path, unsupported extension, determinism.
- System.Drawing.Common 10.0.8 added to PZMapForge.Core and PZMapForge.Core.Tests.

dotnet build: 0 errors, 0 warnings
dotnet test:  99/99 pass (92 Core + 7 Cli)
scripts/validate.ps1: 365 PS assertions unchanged.

---

## [Unreleased - prev26]

### Added
- schemas/pzmapforge.proof-packet.v0.9.schema.json: plan_recommendations_contract
  const=28, proof_packet const=55, total_expected_assertions const=365.

### Changed
- scripts/test-plan-recommendations-contract.ps1: thresholds_used section
  added (+7 assertions): field exists, tiny/large sub-fields exist,
  both >= 0, tiny==9, large==50000. 28 total (was 21).
- scripts/write-proof-packet.ps1: bumped to v0.9; plan_recommendations_contract=28,
  total=365.
- scripts/test-proof-packet.ps1: v0.9; plan_recommendations_contract==28;
  total==365. 55 assertions (unchanged count).
- scripts/test-schema-files.ps1: proof-packet -> v0.9; total 134 unchanged.
- docs/IMPLEMENTATION.md: contract and proof packet rows updated.

Full PowerShell pipeline: 134+40+5+21+36+24+22+28+55 = 365 assertions. All pass.
dotnet test: 89/89 unchanged.

---

## [Unreleased - prev25]

### Added
- plan-recommendations.json now includes thresholds_used object
  (tiny_building_pixel_threshold, large_ground_pixel_threshold) recording
  the PlanningRuleOptions values active during export.

### Changed
- PlanningArtifactWriter.Write: added PlanningRuleOptions? options = null
  parameter (before overrideGeneratedAt). Defaults to PlanningRuleOptions.Default
  when null. Backward-compatible via default and named arguments.
- PlanExportCommand: passes parsed opts to PlanningArtifactWriter.Write.
- schemas/pzmapforge.plan-recommendations.v0.1.schema.json: thresholds_used
  added to required and properties.
- tests/fixtures/plan-recommendations/valid.json: regenerated with
  thresholds_used: {tiny_building_pixel_threshold: 9, large_ground_pixel_threshold: 50000}.
- PlanningArtifactWriterTests: 2 new tests (default/custom thresholds recorded).
  10 total (was 8).
- PlanningArtifactCrossVerificationTests: thresholds_used fields added to
  CrossVerify_HeaderFieldsMatch.

dotnet test: 89/89 (82 Core + 7 Cli)
scripts/validate.ps1: 358 PS assertions unchanged.

---

## [Unreleased - prev24]

### Added
- src/PZMapForge.Cli/Program.cs: --tiny-threshold and --large-threshold optional
  flags for plan-check and plan-export. ParsePlanningOptions() helper parses
  both flags, returns (PlanningRuleOptions?, errorCode). Non-integer value or
  negative value prints a clear error and exits 1. Both commands print
  "Tiny threshold: N" and "Large threshold: N" in their output.
- tests/PZMapForge.Cli.Tests/CliSmokeTests.cs: 5 new threshold tests (default
  values, custom values accepted, zero tiny valid, negative tiny throws,
  negative large throws). 7 total Cli tests.

dotnet build: 0 errors
dotnet test:  87/87 pass (80 Core + 7 Cli)

---

## [Unreleased - prev23]

### Added
- src/PZMapForge.Core/Planning/PlanningRuleOptions.cs: configurable thresholds.
  TinyBuildingPixelThreshold (default 9) and LargeGroundPixelThreshold
  (default 50000). Both must be >= 0; ArgumentOutOfRangeException otherwise.
  PlanningRuleOptions.Default provides the original hardcoded values.
- PlanningRuleEngine.Evaluate(primitives, options) overload; existing no-options
  overload delegates to it via PlanningRuleOptions.Default.
- tests/PZMapForge.Core.Tests/Planning/PlanningRuleEngineOptionsTests.cs:
  8 xUnit tests: default preserves output, zero-threshold suppresses tiny
  warnings (1px buildings not <= 0), lower threshold = fewer warnings,
  threshold equals pixel_count triggers, higher large-ground suppresses note,
  lower large-ground triggers, negative tiny throws, negative large throws.

### Changed
- src/PZMapForge.Core/Planning/PlanningRuleEngine.cs: removed private const
  thresholds; now reads from PlanningRuleOptions.
- docs/PLANNING_RULES.md: threshold table updated to reflect PlanningRuleOptions
  field names; configurable usage example added.

dotnet build: 0 errors
dotnet test:  82/82 pass (80 Core + 2 Cli)
scripts/validate.ps1: 358 PS assertions unchanged.

---

## [Unreleased - prev22]

### Added
- tests/fixtures/plan-recommendations/valid.json: committed reference fixture
  generated from tests/fixtures/parsed-cell/valid.json via PlanningArtifactWriter.
  9 primitives, 13 recommendations (3 warnings/tiny_building + 10 info),
  warning_count=3, total_pixels=90000.
- tests/PZMapForge.Core.Tests/Planning/PlanningArtifactCrossVerificationTests.cs:
  3 [Fact] cross-verification tests comparing .NET pipeline output against the
  committed fixture (header fields, all 13 recommendation details incl. bounds,
  summary counts_by_type/counts_by_severity). EnsureFixture() generates the
  fixture if missing (developer commits on first run).

dotnet test: 74/74 (72 Core + 2 Cli)
scripts/validate.ps1: 358 PS assertions unchanged.

---

## [Unreleased - prev21]

### Added
- scripts/test-plan-recommendations-contract.ps1: 21-assertion contract
  validator for .local/mapforge/plan-recommendations.json. Checks: 2 output
  files, schema/claim_boundary/width/height sentinels, 4 count integrity
  checks, recommendations array exists/count-matches/5-field presence,
  summary exists/matches/counts_by_severity sum. Generates artifact via
  dotnet plan-export if missing (no validate.ps1 recursion).
- schemas/pzmapforge.proof-packet.v0.8.schema.json: adds
  plan_recommendations_contract (const: 21) to validation_summary; updates
  proof_packet (55) and total_expected_assertions (358).

### Changed
- scripts/validate.ps1: plan-recommendations contract step added between
  plan-export and proof packet.
- scripts/write-proof-packet.ps1: bumped to v0.8; adds
  plan_recommendations_contract=21, proof_packet=55, total=358.
- scripts/test-proof-packet.ps1: v0.8; adds plan_recommendations_contract==21;
  total==358. 55 assertions (was 54).
- scripts/test-schema-files.ps1: proof-packet -> v0.8; total unchanged at 134.
- docs/IMPLEMENTATION.md: plan contract ratified; proof packet v0.8.

Full PowerShell pipeline: 134+40+5+21+36+24+22+21+55 = 358 assertions. All pass.

---

## [Unreleased - prev20]

### Added
- schemas/pzmapforge.proof-packet.v0.7.schema.json: adds plan_recommendations_
  sha256/plan_report_sha256 to required; updates validation_summary consts
  (schema_file_sanity=134, proof_packet=54, total=336).

### Changed
- scripts/test-schema-files.ps1: proof-packet -> v0.7 (16 CheckRequired, +2);
  plan-recommendations schema section added (10 CheckRequired, 26 assertions).
  134 total assertions (was 104).
- scripts/write-proof-packet.ps1: bumped to v0.7; adds plan_recommendations_
  path/report_path/sha256 fields; runs plan-export if artifacts missing;
  schema_file_sanity=134, proof_packet=54, total=336.
- scripts/test-proof-packet.ps1: v0.7; +4 required field checks, +2 SHA-256
  checks; schema_file_sanity==134; total==336. 54 assertions (was 48).
- scripts/validate.ps1: plan-export step (dotnet run --no-build) inserted
  before proof packet step.
- docs/IMPLEMENTATION.md: plan-recs schema sanity ratified; proof packet v0.7.

Full PowerShell pipeline: 134+40+5+21+36+24+22+54 = 336 assertions. All pass.

---

## [Unreleased - prev19]

### Added
- src/PZMapForge.Core/Planning/PlanningArtifactWriter.cs: writes
  plan-recommendations.json (schema v0.1) and plan-report.md from a
  PlanningRuleResult. Accepts DateTimeOffset? overrideGeneratedAt for
  deterministic testing. File stream disposed with using block.
- schemas/pzmapforge.plan-recommendations.v0.1.schema.json: JSON Schema.
- docs/PLAN_EXPORT.md: artifact structure, determinism, output path safety.
- src/PZMapForge.Cli/Program.cs: plan-export --path --output command.
  Refuses output outside .local/. Prints JSON path, markdown path,
  primitive count, recommendation count, warning count, status.
- tests/PZMapForge.Core.Tests/Planning/PlanningArtifactWriterTests.cs:
  8 xUnit tests: JSON/md files created, schema sentinel, claim boundary,
  recommendation_count, warning_count, markdown contains claim boundary,
  determinism with fixed timestamp.

dotnet build: 0 errors
dotnet test:  71/71 pass (69 Core + 2 Cli)
plan-export: plan-recommendations.json + plan-report.md written, Status OK

---

## [Unreleased - prev18]

### Added
- src/PZMapForge.Core/Planning/: planning rule engine.
  - PlanningSeverity enum (Warning, Info).
  - PlanningRecommendationType enum (10 values) + ToTypeString() extension.
  - PlanningRecommendation: source primitive id, type, severity, role, pixel
    count, bounds; id=0 for global recommendations.
  - PlanningSummary: total pixels, primitive count, recommendation count,
    warning count, counts by type and severity.
  - PlanningRuleResult: ClaimBoundary, Recommendations, Summary.
  - PlanningRuleEngine.Evaluate(PrimitiveClassificationResult):
    per-primitive rules for all 7 types; tiny_building_candidate warning
    (pixel_count <= 9); large_open_ground_area info (pixel_count > 50000);
    missing_spawn_marker global warning; deterministic sort (severity ASC,
    type ASC, pixel_count DESC, y ASC, x ASC, primitive_id ASC).
- tests/PZMapForge.Core.Tests/Planning/PlanningRuleEngineTests.cs:
  15 xUnit tests covering non-empty output, claim boundary, count consistency,
  all 7 primitive-type mappings ([Theory]), missing spawn warning, determinism,
  counts_by_type stability, counts_by_severity stability, source primitive id.
- docs/PLANNING_RULES.md: rules table, thresholds, sort order, output model.
- src/PZMapForge.Cli/Program.cs: plan-check --path command.

dotnet build: 0 errors
dotnet test:  63/63 pass (61 Core + 2 Cli)
plan-check output: 300x300, 20 primitives, 20 recommendations, 0 warnings, OK

---

## [Unreleased - prev17]

### Added
- tests/test-image-mapforge.ps1 Test 11: -Resize coverage (8 assertions).
  Creates a 150x150 all-grass image, runs image-mapforge.ps1 with -Resize,
  asserts exit 0, parsed-cell.json written, width==300, height==300,
  rows.Count==300, all row lengths==300, counts sum==90000, resized==true.
  Closes IMPLEMENTATION.md gap 1. Hardening harness total: 36 (was 28).
- schemas/pzmapforge.proof-packet.v0.6.schema.json: hardening_harness const=36,
  total_expected_assertions const=300.

### Changed
- scripts/write-proof-packet.ps1: bumped to v0.6; hardening_harness=36, total=300.
- scripts/test-proof-packet.ps1: expects v0.6; hardening_harness==36; total==300.
  48 assertions unchanged.
- scripts/test-schema-files.ps1: proof-packet section updated to v0.6.
  Total schema assertions unchanged at 104.
- docs/IMPLEMENTATION.md: -Resize ratified; gap 1 closed; proof packet v0.6.

### Note
Full PowerShell pipeline: 104+40+5+21+36+24+22+48 = 300 assertions. All pass.
All three original known gaps now closed:
  Gap 1: -Resize flag (closed this slice)
  Gap 2: TMX structural validation (closed by test-tmx-integrity.ps1)
  Gap 3: palette_sha256 verification (closed by test-palette-sha256.ps1)

---

## [Unreleased - prev16]

### Added
- scripts/test-palette-sha256.ps1: 5-assertion palette hash verifier. Checks
  parsed-cell.json exists, source/image-palette.json exists, palette_sha256
  field is present and 64-char hex, and matches the computed SHA-256 of
  source/image-palette.json. Closes IMPLEMENTATION.md gap 3.
- schemas/pzmapforge.proof-packet.v0.5.schema.json: adds
  palette_sha256_verification (const: 5) to validation_summary; updates
  proof_packet (48) and total_expected_assertions (292).

### Changed
- scripts/validate.ps1: palette SHA-256 verification step inserted after
  artifact contract and before TMX integrity.
- scripts/write-proof-packet.ps1: bumped to v0.5; adds
  palette_sha256_verification=5, proof_packet=48, total=292.
- scripts/test-proof-packet.ps1: expects v0.5; adds
  palette_sha256_verification==5; total==292. 48 assertions (was 47).
- scripts/test-schema-files.ps1: proof-packet section updated to v0.5.
  Total schema assertions unchanged at 104.
- docs/IMPLEMENTATION.md: palette SHA-256 ratified; gap 3 closed.

### Note
Full PowerShell pipeline: 104+40+5+21+28+24+22+48 = 292 assertions, all pass.

---

## [Unreleased - prev15]

### Added
- schemas/pzmapforge.proof-packet.v0.4.schema.json: proof packet schema v0.4.
  Adds tmx_integrity (const: 21) to validation_summary; updates proof_packet
  (47) and total_expected_assertions (286).

### Changed
- scripts/write-proof-packet.ps1: bumped to v0.4; added tmx_integrity=21,
  updated proof_packet=47, total=286.
- scripts/test-proof-packet.ps1: expects v0.4; adds tmx_integrity==21 check;
  total_expected_assertions==286. 47 assertions (was 46).
- scripts/test-schema-files.ps1: proof-packet section now validates v0.4.
  Total schema assertions unchanged at 104.
- docs/IMPLEMENTATION.md: proof packet row updated to v0.4/47 assertions.

### Note
Full PowerShell pipeline: schema (104) + contract (40) + TMX integrity (21) +
hardening (28) + regions (24) + primitives (22) + proof packet (47) = 286
assertions. All pass.

---

## [Unreleased - prev14]

### Added
- src/PZMapForge.Cli/Program.cs: primitive-check --path <path> command.
  Loads parsed-cell, extracts regions, classifies primitives, prints
  dimensions/regions/primitives/primitive-types/pixels/status. Exits 0
  on success; exits 1 if parsed-cell is invalid or classification fails
  (e.g. unmapped kind).
- tests/PZMapForge.Cli.Tests/CliSmokeTests.cs: PrimitiveClassifier_IsAccessible
  test confirming PrimitiveClassifier.IsKnownKind and all 7 PlanningPrimitiveType
  enum values are accessible from the CLI test project.

---

## [Unreleased - prev13]

### Added
- src/PZMapForge.Core/Primitives/: typed primitive classifier.
  - PlanningPrimitiveType enum (7 values mirroring PS kindMap output).
  - PlanningPrimitive, PrimitiveKindSummary, PrimitiveClassificationResult.
  - PrimitiveClassifier.Classify(RegionExtractionResult): ports PS classify-
    primitives.ps1 exactly -- same 9->7 kind mapping, same sort order
    (primitive_type ASC, pixel_count DESC, y ASC, x ASC, source_region_id ASC),
    same sequential primitive_id, same summary_by_primitive_type aggregation.
    Throws ArgumentException for unmapped kinds.
- tests/PZMapForge.Core.Tests/Primitives/PrimitiveClassifierTests.cs:
  16 xUnit tests: 8 [Theory] kind-mapping assertions + Classify_ValidFixture
  (count, coverage), Classify_IsDeterministic, Classify_NoUnclassifiedRegions,
  Classify_UnknownKind_Throws, Classify_BuildingFootprintAggregates3Kinds,
  Classify_MatchesPsReferenceFixture (full cross-verification).
- tests/fixtures/primitives/valid.json: PS-generated reference fixture (9
  primitives, 90000 px, 7 primitive types; generated by running classify-
  primitives.ps1 against tests/fixtures/regions/valid.json).
- src/PZMapForge.Core/Regions/: public CreateForTesting factory methods added
  to SemanticRegion, RegionKindSummary, and RegionExtractionResult to support
  isolated unit tests without going through ParsedCellLoader/RegionExtractor.

---

## [Unreleased - prev12]

### Added
- tests/PZMapForge.Core.Tests/Regions/RegionCrossVerificationTests.cs:
  3 cross-verification [Fact] tests asserting the .NET RegionExtractor
  matches the PowerShell extract-regions.ps1 reference for the same input:
  1. TotalsMatch: total region count and total pixel count
  2. SummaryByKindMatches: region_count, total_pixels, largest_region_pixels
     per kind; no extra kinds in either direction
  3. RegionDetailsMatch: kind, code, pixel_count, bounds (x,y,w,h), centroid
     (x,y to 2 decimal places) for all 9 regions (< 20 cap)
- tests/fixtures/regions/valid.json: PS-generated reference fixture from
  tests/fixtures/parsed-cell/valid.json; 9 regions, 90000 pixels,
  grass centroid (149.49, 149.51).

---

## [Unreleased - prev11]

### Added
- src/PZMapForge.Core/Regions/: typed .NET region extractor.
  - RegionBounds, RegionCentroid, SemanticRegion, RegionKindSummary,
    RegionExtractionResult: typed region models.
  - RegionExtractor.Extract(SemanticGrid, IReadOnlyDictionary<char,string>):
    BFS flood-fill with 4-neighbor connectivity, deterministic sort
    (kind ASC, pixel_count DESC, y ASC, x ASC, discovery_id ASC),
    sequential region_id, summary_by_kind. Uses integer modulo decomposition
    (cx = idx % w; cy = idx / w) to avoid floating-point rounding issues.
- SemanticGrid.CreateForTesting(w, h, rows): public factory for test fixtures
  that bypass ParsedCellLoader's 300x300 constraint.
- src/PZMapForge.Cli/Program.cs: region-check --path <path> command.
  Loads parsed-cell, extracts regions, prints dims/regions/kinds/pixels/status.
- tests/PZMapForge.Core.Tests/Regions/RegionExtractorTests.cs:
  8 xUnit tests: all-grass=1 region, valid fixture has 9 kinds, pixel sum 90000,
  all regions positive, bounds in grid, centroid in bounds, deterministic,
  diagonal cells are separate regions (4-neighbor proof).

---

## [Unreleased - prev10]

### Added
- src/PZMapForge.Core/ParsedCell/: typed parsed-cell artifact reader.
  - ParsedCellDocument, ParsedCellMatching, ParsedCellLegendEntry,
    ParsedCellCount, ParsedCellDrift, ParsedCellOutputs: JSON-mapped models.
  - SemanticGrid: InBounds(x,y), GetCode(x,y) throws on OOB, CountCode(code).
  - ParsedCellLoadResult: IsValid, Document, Grid, Errors.
  - ParsedCellLoader.Load(path): validates schema, claim_boundary, width==300,
    height==300, rows count/length, counts sum==90000, all 9 required kinds.
- src/PZMapForge.Cli/Program.cs: parsed-cell-check --path <path> command.
- tests/PZMapForge.Core.Tests/ParsedCell/ParsedCellLoaderTests.cs:
  11 xUnit tests (valid fixture, missing file, wrong schema, wrong claim
  boundary, wrong width, bad row count, bad row length, counts sum mismatch,
  missing required kind, GetCode works, GetCode out-of-bounds throws).
- tests/fixtures/parsed-cell/valid.json: checked-in minimal 300x300 fixture
  (292 grass + 1 each of all 8 non-grass kinds per row 0; rows 1-299 all grass;
  counts sum == 90000; all 9 required kinds present).
- docs/IMPLEMENTATION.md: parsed-cell reader and CLI command ratified.

---

## [Unreleased - prev9]

### Added
- PZMapForge.slnx: .NET 10 solution (.slnx format, dotnet 10 SDK default).
- src/PZMapForge.Core: class library with typed palette models
  (PaletteDocument, PaletteKind, PaletteValidationResult) and PaletteLoader
  which reads source/image-palette.json, validates schema/dims/kinds/GIDs/
  codes/RGB, and returns structured errors.
- src/PZMapForge.Cli: console app with palette-check command. Exits 0 on
  valid palette; prints schema, dimensions, kind count, GID range, status.
  Usage: dotnet run --project src/PZMapForge.Cli -- palette-check --palette
  source/image-palette.json
- tests/PZMapForge.Core.Tests: 8 xUnit tests covering valid canonical palette,
  missing file, duplicate GID, missing required kind, invalid RGB, duplicate
  code, wrong schema, wrong cell_width.
- tests/PZMapForge.Cli.Tests: 1 smoke test confirming PZMapForge.Core loads.
- .gitignore: bin/, obj/, .vs/ added for .NET build artifacts.

### Notes
PowerShell scripts are unchanged. The .NET engine is additive only.
All existing PowerShell validation (285 assertions) continues to pass.

---

## [Unreleased - prev8]

### Added
- scripts/test-tmx-integrity.ps1: 21-assertion TMX structural validator.
  Checks map/tileset/layer XML attributes, decodes base64+gzip payload,
  verifies decompressed length == 360000, GID count == 90000, all GIDs
  in range 1..9. Closes IMPLEMENTATION.md gap 2.
- docs/TMX_INTEGRITY.md: validator design, payload encoding, assertions table,
  claim boundary.
- scripts/validate.ps1: TMX integrity step added after artifact contract
  and before hardening harness.
- docs/IMPLEMENTATION.md: TMX structural integrity ratified; gap 2 closed;
  TileZed provisional note updated.

### Note
Proof packet stays at v0.3. The running pipeline total is now 285 assertions
(104+40+21+28+24+22+46). Proof packet v0.4 will correct the stale counts
(schema_file_sanity and total_expected_assertions).

---

## [Unreleased - prev7]

### Added
- schemas/pzmapforge.proof-packet.v0.3.schema.json: proof packet schema v0.3
  covering all three artifact groups (ImageMapForge, region, primitive) and
  updated validation_summary consts (schema_file_sanity=104, primitive_
  classification=22, proof_packet=46, total=264).

### Changed
- scripts/write-proof-packet.ps1: bumped to v0.3. Hashes primitives.json and
  primitives-report.md; runs classify-primitives.ps1 if missing; updated
  all validation_summary counts.
- scripts/test-proof-packet.ps1: 46 assertions (was 39). 4 new required
  fields, 2 new SHA-256 checks, primitive_classification=22, total=264.
- scripts/test-schema-files.ps1: proof-packet section updated to v0.3
  (14 checked fields, was 12) -- 104 total schema assertions (was 100).
- docs/IMPLEMENTATION.md: proof packet and schema sanity rows updated.

---

## [Unreleased - prev6]

### Added
- scripts/classify-primitives.ps1: maps 9 semantic kinds to 7 planning
  primitive types (road_region, sidewalk_region, building_footprint,
  yard_region, landmark_marker, spawn_marker, ground_region). Reads
  regions.json, sorts primitives deterministically (primitive_type ASC,
  pixel_count DESC, y ASC, x ASC, source_region_id ASC), writes
  primitives.json and primitives-report.md.
- scripts/test-primitive-classification.ps1: 22-assertion harness (output
  files, sentinels, dimensions, structure, bounds, centroids, all 7 types
  present, pixel sum 90000, determinism, gitignore proof).
- schemas/pzmapforge.primitives.v0.1.schema.json: JSON Schema for primitives.
- docs/PRIMITIVE_CLASSIFICATION.md: kind-to-primitive mapping table, output
  fields, deterministic sort order, claim boundary.
- scripts/test-schema-files.ps1: primitives schema added -- 100 total
  assertions (was 78 for 3 schemas).
- scripts/validate.ps1: primitive classification step added before proof packet.
- docs/IMPLEMENTATION.md: primitive classification ratified.

### Note
Proof packet remains at v0.2 (schema_file_sanity hardcoded at 78, total at 209).
These counts are now stale. Proof packet v0.3 will cover primitive artifacts
and correct the counts.

---

## [Unreleased - prev5]

### Added
- schemas/pzmapforge.proof-packet.v0.2.schema.json: proof packet schema v0.2
  covering region artifact fields (regions_json_path, regions_report_path,
  regions_json_sha256, regions_report_sha256) and updated validation_summary
  (schema_file_sanity=78, region_extraction=24, proof_packet=39, total=209).

### Changed
- scripts/write-proof-packet.ps1: bumped to v0.2. Now hashes regions.json and
  regions-report.md; runs extract-regions.ps1 if regions.json is missing;
  updated validation_summary counts.
- scripts/test-proof-packet.ps1: updated for v0.2 contract Ã¢â‚¬â€ 39 assertions
  (was 32): 4 new required fields, 2 new SHA-256 checks, region_extraction=24,
  total=209.
- scripts/test-schema-files.ps1: proof-packet section now validates v0.2
  (12 checked fields, was 10) Ã¢â‚¬â€ 78 total schema assertions (unchanged count
  since proof-packet gained 4 but the total was already recomputed correctly).
- docs/IMPLEMENTATION.md: proof packet row updated to v0.2.

---

## [Unreleased - prev4]

### Added
- scripts/extract-regions.ps1: BFS flood-fill region extraction from
  parsed-cell.json rows using 4-neighbor connectivity. Outputs
  regions.json (schema, claim, regions[], summary_by_kind[]) and
  regions-report.md. Deterministic sort: kind ASC, pixel_count DESC,
  y ASC, x ASC. Fixed PS5.1 [int] rounding bug: uses $cur % $W for
  flat-index decomposition instead of [int]($cur / $W). Used @() array
  with PSCustomObject elements to avoid ConvertTo-Json wrapping $finalRegions
  as {"value":[...]} instead of a JSON array.
- scripts/test-region-extraction.ps1: 24-assertion harness (output files,
  schema/claim sentinels, dimensions, regions structure, bounds validity,
  centroid within bounds, summary 9 kinds, pixel sum 90000, determinism,
  gitignore proof).
- schemas/pzmapforge.regions.v0.1.schema.json: JSON Schema for regions.json.
- docs/REGION_EXTRACTION.md: 4-neighbor BFS docs, sort order, output fields,
  PS5.1 [int] rounding bug note.
- scripts/test-schema-files.ps1: extended to validate all 3 schemas
  (parsed-cell, proof-packet, regions) Ã¢â‚¬â€ 74 total assertions (was 28).
- scripts/validate.ps1: region extraction step (extract + test) added before
  proof packet.
- docs/IMPLEMENTATION.md: region extraction and updated schema sanity row.

---

## [Unreleased - prev3]

### Added
- scripts/write-proof-packet.ps1: generates .local/mapforge/proof-packet.json
  and proof-packet.md with schema sentinel, UTC timestamp, git state,
  SHA-256 hashes of all 5 artifacts, expected validation counts, and safety
  flags. Runs validate.ps1 first if parsed-cell.json is missing.
- scripts/test-proof-packet.ps1: 32-assertion proof packet validator (output
  files exist, 15 required fields, schema/claim sentinels, 5 SHA-256 formats,
  validation summary counts, 4 safety flags).
- schemas/pzmapforge.proof-packet.v0.1.schema.json: JSON Schema for proof packet.
- scripts/validate.ps1: proof packet write + test steps added after hardening.
- docs/IMPLEMENTATION.md: proof packet row added to ratified table.

---

## [Unreleased - prev2]

### Added
- scripts/test-schema-files.ps1: schema file sanity validator (28 assertions:
  $schema, $id sentinel, title, required list for 11 fields, properties keys
  for those same fields). No external dependencies.
- scripts/validate.ps1: schema sanity step added before artifact contract step.
- docs/IMPLEMENTATION.md: schema sanity row added to ratified table.
- Fixed: CHANGELOG and IMPLEMENTATION.md had stale "33 assertions" for the
  parsed-cell contract; corrected to 40.

---

## [Unreleased - prev]

### Added
- scripts/test-parsed-cell-contract.ps1: deterministic artifact contract check
  (40 assertions: required fields, schema sentinel, claim_boundary, dimensions
  300x300, rows count and length, counts pixel sum, all 9 required kinds,
  outputs keys, matching fields and pixel sum).
- scripts/validate.ps1: contract step added before hardening test harness.
- docs/IMPLEMENTATION.md: contract validation row added to ratified table;
  gap 4 added for JSON Schema type validation.

---

## [Unreleased - prev]

### Added
- Import hardened ImageMapForge MVP from pz-sud-ouest-montreal@5944173.
  - source/image-mapforge.ps1: RGB palette format, Fail+exit pattern, Debug
    exits early (no artifacts), nearest-colour drift cache and drift records
    in JSON and report.
  - source/image-palette.json: RGB array format (9 kinds, contiguous GIDs).
  - tests/test-image-mapforge.ps1: 28-assertion hardening harness.
- docs/GENESIS.md: why PZMapForge exists, scope, identity.
- docs/CONSTITUTION.md: non-negotiable behavioral rules.
- docs/IMPLEMENTATION.md: ratified, provisional, and absent capabilities.
- docs/TOOL_USAGE.md: parameters, examples, palette format, colour matching.
- docs/decisions/0001-independent-mapmaker-layer.md: decision record.
- docs/decisions/0002-planning-artifacts-before-playable-export.md: decision record.
- schemas/pzmapforge.parsed-cell.v0.1.schema.json: JSON Schema for artifact.
- examples/README.md: how to create and use a custom blockout image.
- LICENSE: MIT.
- scripts/validate.ps1: updated to run tests/test-image-mapforge.ps1.
- scripts/new-test-image.ps1: updated to read RGB array palette format.

### Removed
- scripts/test-image-mapforge.ps1: moved to tests/.

---

## [0.0.1] - 2026-05-30

### Added
- Initial independent PZMapForge repository scaffold.
- ImageMapForge MVP script (hex palette format, symbol field).
- Palette configuration (hex colour format).
- Local sample image generator and validation wrapper.
- Claim boundary, ImageMapForge, and roadmap documentation.
- Drift tracking port from pz-sud-ouest-montreal (ff0a21f).
