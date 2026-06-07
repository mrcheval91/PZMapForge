# MAP-7U: Mod-Root Layout Match and Coordinate Discriminator

```text
MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED
COORDINATE_DISCRIMINATOR_IDENTIFIED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7U records the corrected mod-root comparison result from MAP-7T and
identifies the remaining structural discriminator between the PZMapForge
candidate and the known-working Dru_map mod.

The mod-root layout now matches. The remaining discriminator is cell coordinate
alignment and spawn coordinate alignment.

---

## 2. Corrected mod-root comparison

### 2.1 Comparison roots

```text
Candidate mod root:
  D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\steamapps\workshop\content\108600\3740642200\mods\pzmapforge_build42_candidate_v4_001

Reference mod root:
  .\.local\map7m-packet\reference-known-working-map\Dru_map
```

Note: MAP-7T compared the wrong roots (Workshop package root vs mod root).
This task records the corrected mod-root to mod-root comparison.

### 2.2 Layout comparison result

```text
layout_match=True
fields_in_reference_not_candidate=0
fields_in_candidate_not_reference=0
candidate_bom_violations_count=0
reference_bom_violations_count=0
```

| Field | Candidate | Reference (Dru_map) |
|---|---|---|
| root_mod_info | YES | YES |
| 42/mod.info | YES | YES |
| common/mod.info | no | no |
| common/media/maps | YES | YES |
| map.info | YES | YES |
| lots=NONE | YES | YES |
| zoomX/Y fields | YES | YES |
| spawnpoints.lua | YES | YES |
| objects.lua | YES | YES |
| worldmap.xml | YES | YES |
| worldmap-forest.xml | YES | YES |

---

## 3. What is now ruled out as the blocker

```text
Workshop wrapper layout:         RULED OUT (layout matches)
mod.info / 42/mod.info presence: RULED OUT (both present)
common/mod.info absence:         RULED OUT (both absent)
common/media/maps/:              RULED OUT (both present)
map.info presence:               RULED OUT (both present)
lots=NONE:                       RULED OUT (both set)
zoomX/Y/S presence:              RULED OUT (both have zoom fields)
spawnpoints.lua / objects.lua:   RULED OUT (both present)
worldmap.xml / worldmap-forest.xml: RULED OUT (both present)
BOM violations:                  RULED OUT (zero on both sides)
```

---

## 4. Remaining discriminator: cell coordinates and spawn alignment

### 4.1 Cell count

| | Candidate | Dru_map |
|---|---|---|
| lotheader count | 1 | 4130 |
| lotheader file | 0_0.lotheader | 35_27.lotheader etc. |

### 4.2 map.info zoom

| Field | Candidate | Dru_map |
|---|---|---|
| zoomX | 0 | 10505 |
| zoomY | 0 | 12220 |
| zoomS | 1 | 14.5 |

The candidate sets zoomX/Y to zero-origin (0,0) placeholders.
Dru_map sets zoomX/Y to the world-map pixel offset corresponding to its
geographic center (cell 35,27 at Drummondville).

### 4.3 Spawn coordinates

Candidate spawnpoints.lua:
```lua
unemployed = { worldX=0, worldY=0, posX=150, posY=150, posZ=0 }
```

Dru_map spawnpoints.lua:
```lua
-- majority of professions spawn at: worldX=35, worldY=27, posX=240, posY=183
unemployed includes: worldX=35, worldY=27, posX=246, posY=188
                     worldX=35, worldY=27, posX=240, posY=183
```

The candidate spawns at cell (0,0) which does not contain built Dru_map content.
Dru_map spawns at cell (35,27) which is the center of the Drummondville map.

### 4.4 Binary files

| | Candidate | Dru_map |
|---|---|---|
| lotheader | 0_0.lotheader | 35_27.lotheader (and 4129 others) |
| lotpack | world_0_0.lotpack | world_35_27.lotpack (etc.) |
| chunkdata | chunkdata_0_0.bin | chunkdata_35_27.bin (etc.) |

---

## 5. Hypothesis

The PZMapForge candidate's single cell exists at grid origin (0,0).
The spawn target (worldX=0, worldY=0) also points to cell (0,0).
This may be a valid coordinate technically, but:

1. The candidate may be spawning into an empty/unregistered area.
2. The zoom offset (zoomX=0, zoomY=0) may not correspond to any registered
   in-game world-map area.
3. PZ may require the cell to exist in a populated region of the world
   grid, or the spawn point must reference a valid lotheader cell.

The next diagnostic is to rebase the candidate to cell (35,27) coordinates,
matching the Dru_map spawn and zoom reference exactly, and test whether
this allows the candidate to be discovered and mounted by IsoMetaGrid.

---

## 6. Next diagnostic: coordinate-aligned staged Workshop package

The coordinate-aligned diagnostic package:
- Keeps same mod ID: `pzmapforge_build42_candidate_v4_001`
- Renames binary files: `0_0` → `35_27`
  - `0_0.lotheader` → `35_27.lotheader`
  - `world_0_0.lotpack` → `world_35_27.lotpack`
  - `chunkdata_0_0.bin` → `chunkdata_35_27.bin`
- Updates map.info zoom: zoomX=10505, zoomY=12220, zoomS=14.5
- Updates spawnpoints.lua: worldX=35, worldY=27, posX=246, posY=188
- Does NOT mutate binary file contents
- Does NOT change the binary writer

This is a diagnostic coordinate-relabel only. The binary content remains
the empty-grass candidate from MAP-6L/6Z. If the runtime discovery path
requires cell coordinate alignment, this test will reveal that.

---

## 7. Binary writer gate

```text
BINARY_WRITER_GATE_STILL_CLOSED

Gate opens when:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit java.io.EOFException on candidate lotheader in log

Do not mutate LOTH/LOTP/chunkdata binary contents.
Only coordinate labels (filenames) change in MAP-7U staged package.
```

---

## 8. Claim boundary

```text
MAP7U_MODROOT_LAYOUT_MATCH_CONFIRMED
COORDINATE_DISCRIMINATOR_IDENTIFIED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
```

No playable PZMapForge export is claimed from this task.
No Steam Workshop upload is performed by Claude.
No binary writer behavior is changed.
