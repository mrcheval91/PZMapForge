# MAP-8N: Worldmap XML.bin Presence Discriminator Result

```text
MAP8N_WORLDMAP_XML_BIN_PRESENCE_DISCRIMINATOR_CONFIRMED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
NO_BINARY_CONTENTS_READ
```

---

## 1. Context

MAP-8M defined the presence/shape inventory plan. MAP-8N records the operator-run result
of scripts/inspect-build42-worldmap-bin-presence.ps1 comparing PZMapForge candidate parent
against Project Russia reference parent.

Inspector run:
```text
CandidateParentRoot: ...\3740642200\...\common\media\maps\PZMapForge
ReferenceParentRoot: ...\3734334068\...\common\media\maps\Project Russia
Schema: pzmapforge.map8m-worldmap-bin-presence.v0.1
```

---

## 2. Presence result

| File | Candidate | Reference |
|------|-----------|-----------|
| worldmap.xml | present (1915 bytes) | present (888333 bytes) |
| worldmap.xml.bin | **absent** | **present (283881 bytes)** |
| worldmap-forest.xml | absent | absent |
| worldmap-forest.xml.bin | absent | absent |
| streets.xml.bin | absent | absent |
| objects.lua | present (32 bytes) | present (5823 bytes) |
| spawnpoints.lua | present (380 bytes) | present (10323 bytes) |
| lotheader_count | 1 | 359 |
| lotpack_count | 1 | 0 |
| chunkdata_count | 1 | 359 |

---

## 3. Bug fixed: lotpack_count pattern

MAP-8M inspector used `*.pack` to count lotpack files. The correct pattern is `*.lotpack`.

PZMapForge candidate contains `world_35_27.lotpack`. The corrected inspector now correctly
reports `candidate lotpack_count=1`.

Project Russia parent contains no lotpack files (it uses a different cell layout).
Reference `lotpack_count=0` is correct for that parent.

---

## 4. Interpretation

```text
streets_xml_bin_primary_blocker_likely    = false
  (streets.xml.bin absent in BOTH candidate and reference parent)

worldmap_xml_text_primary_blocker_likely  = false
  (MAP-8L proved substantial text worldmap.xml does not unlock IsoMetaGrid mount)

worldmap_xml_bin_primary_discriminator    = true
  (candidate_worldmap_xml_bin_present=false; reference_worldmap_xml_bin_present=true)
  (size: 283881 bytes in reference)
```

`worldmap.xml.bin` is the strongest remaining discriminator between the working Project Russia
parent and the PZMapForge candidate. This is a hypothesis, not a proven requirement.

The claim boundary is:
- "Leading discriminator" or "strongest hypothesis".
- NOT: "Build 42 requires worldmap.xml.bin".

---

## 5. Next branch

```text
next_branch=worldmap_xml_bin_header_format_investigation_pending_operator_approval
```

Binary reading requires explicit operator approval (MAP-8M Step 2 gate).

Until operator approves Step 2:
- Do not read bytes from worldmap.xml.bin.
- Do not implement a binary writer for worldmap.xml.bin.
- Binary writer gate remains closed.

---

## 6. Claim boundary

```text
MAP8N_WORLDMAP_XML_BIN_PRESENCE_DISCRIMINATOR_CONFIRMED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

Non-claims:
- worldmap.xml.bin is the leading discriminator, not a proven requirement.
- Binary writer gate remains closed.
- No binary contents read.
- No third-party files copied or used.
- No playable PZMapForge export claimed.
