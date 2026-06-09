# MAP-8O Worldmap Bin Header Inspection

```text
Status: MAP-8O header inspection defined
Classification: MAP8O_WORLDMAP_XML_BIN_HEADER_INSPECTION_DEFINED
Operator approval: header-only, max 64 bytes per file, read-only
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Source basis

MAP-8N confirmed worldmap.xml.bin as the leading discriminator:
- candidate_worldmap_xml_bin_present=false
- reference_worldmap_xml_bin_present=true (283,881 bytes, Project Russia parent)
- streets.xml.bin absent in both candidate and reference -- not the blocker
- text worldmap.xml proven insufficient (MAP-8L failed to mount)

The operator approved Step 2 of the MAP-8M investigation plan:
header-only inspection of worldmap.xml.bin, maximum 64 bytes per file.

## Approval scope

- Header-only inspection of worldmap.xml.bin.
- Maximum read size: first 64 bytes per inspected file.
- Read-only. No copying. No full binary parsing.
- No binary writer implementation.

## Inspector

Script: scripts/inspect-build42-worldmap-bin-header.ps1

Parameters:
- -CandidateWorldmapBinPath  path to candidate worldmap.xml.bin (may not exist)
- -ReferenceWorldmapBinPath  path to reference worldmap.xml.bin
- -Output                    must be under .local/

Writes:
- worldmap-bin-header-inspection.json (schema pzmapforge.map8o-worldmap-bin-header-inspection.v0.1)
- worldmap-bin-header-inspection.md

Output fields:
- candidate_present, candidate_size_bytes, candidate_bytes_read_count
- candidate_first_16_bytes_hex, candidate_first_64_bytes_hex, candidate_ascii_preview
- candidate_detected_signature
- reference_present, reference_size_bytes, reference_bytes_read_count
- reference_first_16_bytes_hex, reference_first_64_bytes_hex, reference_ascii_preview
- reference_detected_signature
- max_bytes_allowed=64
- binary_contents_read_scope=first_64_bytes_only
- binary_contents_full_read=false
- third_party_files_copied=false
- playable_claim_allowed=false
- binary_writer_gate_closed=true
- next_branch=worldmap_xml_bin_minimal_pzmapforge_owned_encoder_research_pending_evidence

Signature detection (first 2-4 bytes only):

| Signature    | Hex                                    |
|--------------|----------------------------------------|
| gzip         | 1F 8B                                  |
| zlib         | 78 01 / 78 5E / 78 9C / 78 DA          |
| zip          | 50 4B                                  |
| sqlite       | 53 51 4C 69                            |
| xml_or_text  | 3C (leading <)                         |
| unknown      | anything else                          |

## What is NOT done

- No full binary read.
- No binary writer implementation.
- No Project Russia files copied.
- No vanilla files copied.
- No .bin/.lotheader/.lotpack/chunkdata copied into repo.
- No Project Zomboid run.
- No Workshop upload.
- worldmap.xml.bin is not claimed as a proven Build 42 requirement.
  It is the leading discriminator / strongest hypothesis.

## Status labels

```text
MAP8O_WORLDMAP_XML_BIN_HEADER_INSPECTION_DEFINED
OPERATOR_APPROVED_HEADER_ONLY_INSPECTION=true
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
NO_BINARY_CONTENTS_FULL_READ
MAX_BYTES_ALLOWED=64
```

## Next branch

After the operator runs the inspector:
- If reference signature reveals a known format (gzip, zlib, etc.): research a minimal
  PZMapForge-owned encoder for that format without copying any Project Russia content.
- If signature is unknown: escalate for further research approval.
- Binary writer gate remains CLOSED until IsoMetaGrid logs a parse attempt against
  PZMapForge lotheader/sidecar.
