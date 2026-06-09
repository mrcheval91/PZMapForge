# MAP-8U First Non-FF Transition Scan

```text
Status: MAP-8U first non-FF transition scan approved and staged
Classification: MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Operator approval

The operator approved bounded first non-FF transition scan beyond the 4096-byte window.

Approval scope:
- Read-only scan.
- Start scanning at offset 133.
- Maximum bytes to read: 65536 total bytes from worldmap.xml.bin.
- Do not read the full file (file is 283881 bytes).
- No copying of any Project Russia or vanilla files.
- No binary writer.
- No PZ run.
- No Workshop upload.

## Source basis

MAP-8T recorded that bytes after string_pool_end_offset=133 are all 0xFF
within the first 4096-byte read window:
- first_128_bytes_after_string_pool_all_ff=true
- first_256_bytes_after_string_pool_all_ff=true
- first_non_ff_offset_known=false
- immediate_cell_index_after_string_pool_supported=false

The FF region continues beyond the 4096-byte window. The first non-FF byte
offset is unknown. This scan locates it within a bounded 65536-byte window.

Reference path:
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia\worldmap.xml.bin

## Inspector

scripts/inspect-build42-igmb-first-non-ff-transition.ps1
- Params: -ReferenceWorldmapBinPath, -Output, -StringPoolEndOffset (default 133),
  -MaxBytes (default 65536, hard cap 65536), -WindowBytes (default 64, hard cap 256)
- Reads at most min(file_size, MaxBytes, 65536) bytes via FileStream read-only.
- Scans from StringPoolEndOffset for first non-0xFF byte.
- .local/ guard on -Output.
- No files copied.

If first non-FF byte found:
- first_non_ff_found=true
- Records exact absolute offset, relative offset from string pool end.
- Records small bounded hex window around transition.
- Records U32LE/U16LE values around transition.
- Records printable ASCII runs near transition.
- Heuristic-only: plausible count/offset/coordinate candidates.
- Does NOT claim cell index understood.

If first non-FF byte NOT found within bounded scan:
- first_non_ff_found=false
- interpretation=ff_region_continues_beyond_bounded_scan
- next_branch=larger_bounded_transition_scan_pending_operator_approval

## What this is NOT

- This is NOT cell index discovery.
- The full IGMB format structure beyond the string pool is not confirmed.
- Any structural observations are heuristic only, confidence=low.
- worldmap.xml.bin is not claimed as a proven Build 42 requirement.
  It is the leading discriminator / strongest hypothesis.

## Status labels

```text
MAP8U_FIRST_NON_FF_TRANSITION_SCAN_APPROVED
MAX_BYTES_ALLOWED=65536
BINARY_CONTENTS_FULL_READ=false
THIRD_PARTY_FILES_COPIED=false
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
CONFIDENCE_LEVEL=low
```

## Next branch

next_branch=igmb_transition_structure_analysis_pending_operator_approval_if_non_ff_found

If the transition is found, structural analysis of the bytes after the transition
requires explicit operator approval.

If the FF region extends beyond 65536 bytes, a larger bounded scan requires
explicit operator approval.

Binary writer gate remains CLOSED until IsoMetaGrid logs a parse attempt against
PZMapForge lotheader/sidecar.

## MAP-8V reference

MAP-8V recorded the real MAP-8U run result. The operator ran the scanner against
Project Russia worldmap.xml.bin and found the first non-FF byte at offset 6389.
ff_run_length=6256. Offset 6389 is NOT 4-byte or 2-byte aligned.
Exact U32LE at transition (unaligned): 30, 26, 9. Observed-only.
Inspector hardened with exact-offset decoding fields (MAP-8V).
See docs/MAP_8V_REAL_FIRST_NON_FF_TRANSITION_RESULT.md.
