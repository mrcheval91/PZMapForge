# MAP-8H: Parent/Child Map Contract Probe

```text
MAP8H_PARENT_CHILD_CONTRACT_PROBE_STAGED
MAP8H_COMMON_MEDIA_MAPS_PARENT_CHILD_LAYOUT
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_THIRD_PARTY_FILES_COPIED
NO_PROJECT_RUSSIA_FILES_COPIED
```

---

## 1. Source basis: Project Russia contract observation

Inspection of Project Russia Workshop item 3734334068 revealed:

```text
common\media\maps\Dmitrov\map.info:
  title=Dmitrov
  lots=Project Russia
  description=Chunk size is 8x8, Cell size is 256x256
  zoomX=9000
  zoomY=9300
  zoomS=14.5
  demoVideo=PR.bik

Dmitrov contains: map.info, objects.lua, spawnpoints.lua,
                  worldmap.xml, worldmap.xml.bin
Dmitrov does NOT contain playable cell binaries.

common\media\maps\Project Russia\:
  map.info: title=- Random city - Project Russia, fixed2x=true
  Contains: *.lotheader, world_*.lotpack, chunkdata_*.bin
            worldmap.xml, worldmap.xml.bin, objects.lua,
            spawnpoints.lua, thumb.png, thumb.bik

Other child cities: Dolgoprudny, Nazarovo, Skhodnya
  All have: lots=Project Russia
```

---

## 2. Contract model derived from Project Russia

```text
Parent folder (e.g. "Project Russia"):
  - Contains actual playable cell binaries (lotheader/lotpack/chunkdata)
  - map.info has NO lots field (or lots=self)
  - fixed2x=true
  - Is the IsoMetaGrid cell-loading target

Child city folders (e.g. "Dmitrov"):
  - map.info has lots=<ParentFolderId>
  - Contains city-selector metadata (zoomX/Y/S) and worldmap files
  - Does NOT contain cell binaries
  - Is the city-selector UI target
```

PZMapForge has been using a single folder with both lots=self and cell binaries.
The Project Russia pattern separates these concerns.

Also confirmed: `common\media\maps` is NOT ignored by Build 42. Project Russia
uses this layout and it works. Previous test variants using common\ were failing
for a different reason (not the path prefix itself).

---

## 3. MAP-8H staged layout

```text
staged-workshop-parent-child/<MapId>/      <- mod root (=Contents\mods\<MapId>\ on Workshop)
  mod.info
  42\
    mod.info
  common\
    media\
      maps\
        PZMapForge\                        <- PARENT (cell-loading target)
          map.info                  title=PZMapForge, fixed2x=true
          35_27.lotheader           generated cell binary (unchanged)
          world_35_27.lotpack       generated cell binary (unchanged)
          chunkdata_35_27.bin       generated cell binary (unchanged)
          spawnpoints.lua           worldX=35, worldY=27
          objects.lua
          worldmap.xml              minimal stub
          thumb.png                 placeholder
          MAP8H_PARENT_CHILD_PROBE.txt  canary

        pzmapforge_build42_candidate_v4_001\  <- CHILD (city-selector UI)
          map.info                  lots=PZMapForge, zoomX/Y/S
          spawnpoints.lua           worldX=35, worldY=27
          objects.lua
          worldmap.xml              minimal stub
```

---

## 4. Parent map.info

```text
title=PZMapForge
fixed2x=true
description=PZMapForge parent playable cell map. Diagnostic only. Not a playable claim.
```

No lots field. No zoomX/Y/S.

---

## 5. Child map.info

```text
title=pzmapforge_build42_candidate_v4_001
lots=PZMapForge
description=PZMapForge child city selector layer. Diagnostic only. Not a playable claim.
zoomX=10505
zoomY=12220
zoomS=14.5
```

---

## 6. Server Map line

```text
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
```

The child map ID comes first (for city selector), parent second (for cell loading),
followed by Muldraugh fallback.

---

## 7. What this probe tests

If the parent/child contract is the missing piece, this test should produce:
1. City selector shows pzmapforge_build42_candidate_v4_001 (child) as selectable.
2. IsoMetaGrid lists PZMapForge folder (parent).
3. Player spawns in the PZMapForge generated cell (35_27 area).

If IsoMetaGrid still ignores the parent folder:
- The map.info parent contract alone is not sufficient.
- Investigate additional required files (worldmap.xml.bin, streets.xml.bin format).

If player spawns but in fallback:
- IsoMetaGrid finds the folder but binary files are still rejected.
- Binary writer investigation resumes.

---

## 8. Binary writer gate

```text
BINARY_WRITER_GATE_STILL_CLOSED

Gate opens when:
  IsoMetaGrid lists PZMapForge (parent) in map folder scan
  OR: explicit lotheader parse attempt logged for parent folder

The parent/child layout change is a metadata contract change.
Binary file contents (35_27.lotheader, etc.) are UNCHANGED from MAP-8F.
```

---

## 9. Claim boundary

```text
MAP8H_PARENT_CHILD_CONTRACT_PROBE_STAGED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PROJECT_RUSSIA_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
```

Non-claims:
- No playable PZMapForge export claimed.
- Binary writer gate is still closed.
- Staged binaries are unchanged PZMapForge-generated files; no Project Russia cells copied.
- The parent/child layout is observed from Project Russia but all files are
  generated from scratch by PZMapForge tooling.
