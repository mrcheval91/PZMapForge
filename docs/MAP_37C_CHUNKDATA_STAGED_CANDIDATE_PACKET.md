# MAP-37C Chunkdata Staged Candidate Packet

MAP37C_STAGED
CHUNKDATA_MAP37B_WRITER_APPLIED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
verified_chunkdata_format=false

## Summary

Generated a fresh Build 42 candidate packet using the MAP-37B-fixed chunkdata writer.
Proved deterministically that `chunkdata_35_27.bin` contains the corrected binary structure:
2-byte header (0x00 0x01) + 128 × 8-byte zero records = 1026 bytes total.

Source: DeadMtlChunkdataRecordClusterWriterSpec.BuildMinimalChunkdata()
Cell coordinates: --cell-x 35 --cell-y 27
Profile: empty_grass_v5

## Binary shape proof

| File | Size | Structure | Status |
|---|---|---|---|
| chunkdata_35_27.bin | 1026 | 00 01 header + 128 × 8-byte zero records | generated_not_load_tested |

## Determinism evidence

CLI invoked twice with identical arguments. SHA-256 of chunkdata_35_27.bin is identical
across both runs. BuildMinimalChunkdata() is pure (no timestamp in binary output).
chunkdata_deterministic=true recorded in preflight JSON.

## Claim boundary

- PLAYABLE_EXPORT_CLAIM_ALLOWED=false
- verified_chunkdata_format=false (structure confirmed by MAP-37A ExactFitScore=3; not load-tested)
- LOAD_TEST_NOT_PERFORMED=true
- staged_output_local_only=true
- CLAUDE_RAN_PZ=false
- CLAUDE_WROTE_STEAM=false
- chunkdata_map37b_writer_applied=true
- chunkdata_zero_body_acceptance_unknown=true (body content not runtime-verified)

## Remaining unknowns

- chunkdata header field semantics (meaning of 0x00 0x01)
- chunkdata record field meanings (8 bytes per record)
- zero-record cluster runtime acceptance
- build42_load_test not performed
