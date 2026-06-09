# MAP-8L: Worldmap XML Substantial Candidate

```text
MAP8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE_STAGED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
NO_BINARY_CONTENTS_READ
```

---

## 1. Source basis

MAP-8K comparator result:

```text
candidate_worldmap_xml_bin_present: false
reference_worldmap_xml_bin_present: true
candidate_worldmap_xml:  skeletal  (52 bytes, 2 lines)
reference_worldmap_xml:  substantial  (888333 bytes, 30959 lines)
map_info_field_differences: 2 (title, description - not blocking)
candidate_cell_binary_counts: 1/1/1 (lotheader/lotpack/chunkdata)
reference_cell_binary_counts: 359/359/359
```

Leading discriminators:
1. worldmap.xml.bin present in reference (Project Russia) parent, absent in candidate.
2. worldmap.xml skeletal in candidate vs substantial in reference.

---

## 2. Hypothesis

IsoMetaGrid or WorldMapDataAssetManager may require a substantial worldmap.xml
(or worldmap.xml.bin) to mount the parent map folder. The current 52-byte stub
likely fails the validity check that the reference satisfies.

MAP-8L tests hypothesis: does replacing the skeletal worldmap.xml with a
substantial PZMapForge-owned one unblock IsoMetaGrid mount?

---

## 3. What MAP-8L does NOT do

- Does not generate worldmap.xml.bin (requires binary format investigation).
- Does not copy any Project Russia content.
- Does not claim the generated worldmap.xml matches the official PZ format.
- Does not claim IsoMetaGrid mount will succeed.
- Does not open the binary writer gate.
- Does not claim playable export.

---

## 4. Generated artifact

Script: `scripts\prepare-build42-map8l-worldmap-xml-candidate.ps1`

Generates a substantial PZMapForge-owned `worldmap.xml` describing the single
cell at worldX=35, worldY=27. Content is derived only from known cell coordinates
and PZMapForge metadata. No third-party data is used.

Output under `-Output` (must be under `.local/`):
- `worldmap.xml` -- substantial replacement worldmap for PZMapForge parent folder
- `map8l-preflight.json`
- `map8l-preflight.md`
- `MAP_8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE_PACKET.md`

---

## 5. Operator deployment steps

### Step 1: Run generator

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\prepare-build42-map8l-worldmap-xml-candidate.ps1 `
  -Output .\.local\map8l-output
```

### Step 2: Copy worldmap.xml to Workshop source (PZMapForge parent folder only)

```powershell
$src = ".\.local\map8l-output\worldmap.xml"
$dst = "D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3740642200\mods\pzmapforge_build42_candidate_v4_001\common\media\maps\PZMapForge\worldmap.xml"
Copy-Item -LiteralPath $src -Destination $dst -Force
Write-Output "Replaced worldmap.xml in Workshop source."
```

### Step 3: Re-upload to Workshop and test

Server INI:
```ini
WorkshopItems=3740642200
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
```

---

## 6. Success criteria

```text
SUCCESS: IsoMetaGrid lists PZMapForge parent folder in map folder scan.
PARTIAL: WorldMapDataAssetManager error changes (different parse error).
FAILURE: IsoMetaGrid still empty -> worldmap.xml not the discriminator.
```

If failure: next branch is worldmap.xml.bin binary format investigation.

---

## 7. Claim boundary

```text
MAP8L_WORLDMAP_XML_SUBSTANTIAL_CANDIDATE_STAGED
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
NO_BINARY_CONTENTS_READ
```

Non-claims:
- Generated worldmap.xml is not the official PZ worldmap format.
- MAP-8L does not claim IsoMetaGrid will mount after deployment.
- Binary writer gate remains closed.
- No playable export claimed.
