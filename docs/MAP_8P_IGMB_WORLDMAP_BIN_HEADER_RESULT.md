# MAP-8P IGMB Worldmap Bin Header Result

```text
Status: MAP-8P IGMB header result recorded
Classification: MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED
Binary writer gate: CLOSED
Playable claim: not allowed
```

## Source basis

The operator ran scripts/inspect-build42-worldmap-bin-header.ps1 (MAP-8O) against the
Project Russia reference worldmap.xml.bin. The header reveals a custom binary format.

Reference path:
D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3734334068\mods\Project Russia\common\media\maps\Project Russia\worldmap.xml.bin

## Observed header

reference_first_16_bytes_hex:
49 47 4D 42 02 00 00 00 00 01 00 00 3B 00 00 00

reference_first_64_bytes_hex:
49 47 4D 42 02 00 00 00 00 01 00 00 3B 00 00 00 44 00 00 00 0C 00 00 00 07 00 50 6F 6C 79 67 6F 6E 07 00 68 69 67 68 77 61 79 07 00 70 72 69 6D 61 72 79 05 00 74 72 61 69 6C 07 00 6E 61 74 75

reference_ascii_preview:
IGMB........;...D.........Polygon..highway..primary..trail..natu

## Signature analysis

Magic bytes: 49 47 4D 42 = ASCII "IGMB"
- Not gzip (1F 8B). Not zlib (78 xx). Not zip (50 4B). Not XML (3C).
- Custom Project Zomboid worldmap binary format.

Compression: appears_compressed=false.
Readable strings appear immediately in the first 64 bytes.

Endianness: likely_little_endian_fields=true.
- Version field: bytes 4-7 = 02 00 00 00.
  Read as little-endian U32 = 2. Plausible version.
  Read as big-endian U32 = 33554432. Not plausible.
- String length prefixes: 07 00 before "Polygon" = U16LE 7. Matches string length.
  07 00 before "highway" = U16LE 7. Matches.
  07 00 before "primary" = U16LE 7. Matches.
  05 00 before "trail" = U16LE 5. Matches.

IMPORTANT CORRECTION: The community note claimed Java binary streams use big-endian.
The observed IGMB header contradicts this for the version and string length fields.
Both are consistent with little-endian 16-bit or 32-bit integers.
big_endian_claim_contradicted_by_observed_header=true.

Visible string tokens: Polygon, highway, primary, trail, natu (prefix)
These appear consistent with OSM feature types (roads, natural features).
possible_length_prefixed_strings=true
possible_string_length_prefix_width=16-bit

## What this is NOT

- This is NOT a format specification. Only 64 bytes were read.
- The full structure (header metadata, string pool, cell index, feature payloads) is
  not confirmed from repo evidence or source code.
- Community layout notes are recorded as unverified supporting context only.
- worldmap.xml.bin is not claimed as a proven Build 42 requirement.
  It is the leading discriminator / strongest hypothesis.

## Inspector update (MAP-8P)

scripts/inspect-build42-worldmap-bin-header.ps1 updated to detect IGMB magic:
  bytes 0-3 == 49 47 4D 42 -> detected_signature = 'igmb'

scripts/test-build42-worldmap-bin-header.ps1 updated:
  Test 21: IGMB dummy reference exits 0.
  Test 22: detected_signature == 'igmb' for 49 47 4D 42 prefix.
  Total: 20 -> 22 assertions.

## Status labels

```text
MAP8P_IGMB_WORLDMAP_BIN_HEADER_RESULT_RECORDED
IGMB_MAGIC_DETECTED=true
APPEARS_COMPRESSED=false
APPEARS_CUSTOM_BINARY_WORLDMAP_FORMAT=true
LIKELY_LITTLE_ENDIAN_FIELDS=true
POSSIBLE_VERSION_VALUE=2
POSSIBLE_LENGTH_PREFIXED_STRINGS=true
BIG_ENDIAN_CLAIM_CONTRADICTED_BY_OBSERVED_HEADER=true
COMMUNITY_LAYOUT_NOTES_RECORDED_AS_UNVERIFIED=true
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
NO_BINARY_CONTENTS_FULL_READ
MAX_BYTES_ALLOWED=64
```

## Next branch

next_branch=igmb_structure_research_pending_operator_approval

The IGMB magic is identified. The version, string pool, and field layout are observable
from only 64 bytes. A deeper structure research step requires explicit operator approval
before any further binary reading or encoder implementation.

Binary writer gate remains CLOSED until IsoMetaGrid logs a parse attempt against
PZMapForge lotheader/sidecar.
