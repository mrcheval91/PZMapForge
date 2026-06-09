# MAP-8M: Worldmap XML.bin Investigation Plan

```text
MAP8M_WORLDMAP_BIN_INVESTIGATION_PLAN_DEFINED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
NO_BINARY_CONTENTS_READ
```

---

## 1. Evidence baseline from MAP-8L

MAP-8L proved: substantial text worldmap.xml does not unlock IsoMetaGrid mount.

MAP-8K confirmed:
- `reference_worldmap_xml_bin_present=true` (Project Russia parent)
- `candidate_worldmap_xml_bin_present=false` (PZMapForge parent)
- `candidate_worldmap_xml: skeletal -> substantial` tested in MAP-8L, failed to mount.
- `streets.xml.bin` absent from BOTH candidate and reference parent.
- `map.info` fields largely match (fixed2x=true, no lots field, no demoVideo).

Leading hypothesis: `worldmap.xml.bin` is required for IsoMetaGrid parent folder mount.

This is a hypothesis. It is not yet proven.

---

## 2. Staged investigation steps

### Step 1: Presence/shape inventory (immediate, allowed now)

Script: `scripts/inspect-build42-worldmap-bin-presence.ps1`

Compares PZMapForge candidate parent vs Project Russia reference parent:

```text
Allowed reads:
  - file presence (exists/not exists)
  - file size in bytes
  - binary file count (lotheader/lotpack/chunkdata) -- count only

Forbidden:
  - binary file contents
  - reading bytes from *.bin, *.lotheader, *.lotpack, *.pack, *.bik
  - copying any third-party files
```

Target fields:
```text
worldmap.xml            - exists + size_bytes
worldmap.xml.bin        - exists + size_bytes  (KEY discriminator)
worldmap-forest.xml     - exists + size_bytes
worldmap-forest.xml.bin - exists + size_bytes
streets.xml.bin         - exists + size_bytes
objects.lua             - exists + size_bytes
spawnpoints.lua         - exists + size_bytes
lotheader_count         - count only
lotpack_count           - count only
chunkdata_count         - count only
```

### Step 2: Format research gate (requires explicit operator approval)

Before any binary content reading, the operator must explicitly authorize:

```text
GATE: Before reading any bytes from worldmap.xml.bin or other binary sidecars:
  1. Operator approves format investigation.
  2. Approved reads: magic bytes / header only (first N bytes).
  3. Do NOT copy third-party binaries into the repo.
  4. Do NOT ship third-party-derived data.
  5. Any format findings are documented as observations only.
```

Approved future investigation may inspect:
- First 4-16 bytes of worldmap.xml.bin (magic/header identification).
- Total file length.
- Presence of known compression signatures (gzip/zlib/zip magic).

Nothing beyond this without additional operator approval.

### Step 3: Binary writer gate

The binary writer gate remains closed.

Gate opens only when:
- IsoMetaGrid logs PZMapForge parent folder in the map folder scan, OR
- Logs show a lotheader parse attempt against PZMapForge cell (35_27.lotheader).

Until then: no binary writer changes. No cell format modifications.

---

## 3. Forbidden actions in this investigation

```text
Do NOT copy Project Russia worldmap.xml.bin.
Do NOT copy Project Russia .lotheader/.lotpack/chunkdata files.
Do NOT rename third-party binaries to 35_27.*.
Do NOT use copied third-party binaries to force a mount.
Do NOT read Project Russia binary contents without Step 2 approval.
Do NOT claim Build 42 requires worldmap.xml.bin without evidence.
```

---

## 4. Claim boundary

```text
MAP8M_WORLDMAP_BIN_INVESTIGATION_PLAN_DEFINED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

Non-claims:
- Plan does not claim worldmap.xml.bin is required. Hypothesis only.
- Binary writer gate remains closed.
- No binary contents read.
- No third-party files copied or used.
