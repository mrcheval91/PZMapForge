# MAP-8F: lots=self Runtime Result

```text
MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED
MAP8F_CITY_SELECTOR_VISIBLE
MAP8F_ISO_META_GRID_MAP_FOLDER_LIST_EMPTY
MAP8F_WORLDMAP_XML_FAILED_TO_LOAD
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. MAP-8F change

MAP-8F changed only one field in map.info from the MAP-8D staged package:

```text
Before (MAP-8D): lots=NONE
After  (MAP-8F): lots=pzmapforge_build42_candidate_v4_001
```

All other files were identical to MAP-8D: 42\media layout, no .bin stubs,
coordinate-aligned binaries 35_27.*, worldmap.xml/worldmap-forest.xml/worldmap.png.

Workshop item: 3740642200
Mod ID: pzmapforge_build42_candidate_v4_001

---

## 2. Downloaded Workshop verification

```text
path: mods\pzmapforge_build42_candidate_v4_001\42\media\maps\pzmapforge_build42_candidate_v4_001
common: ABSENT
root media: ABSENT
worldmap.xml.bin: ABSENT
worldmap-forest.xml.bin: ABSENT
streets.xml.bin: ABSENT
map.info lots: pzmapforge_build42_candidate_v4_001
35_27.lotheader: PRESENT
world_35_27.lotpack: PRESENT
chunkdata_35_27.bin: PRESENT
```

---

## 3. Runtime visual result

- City list showed BOTH Muldraugh AND pzmapforge_build42_candidate_v4_001.
- User selected the PZMapForge city.
- Player spawned in Muldraugh / vanilla / fallback neighborhood.
  NOT in the generated mini-map content.

The city selector now lists the candidate. This is new compared with lots=NONE
(MAP-8D/MAP-8B), where the candidate did not appear in the city list at all.
However, selecting it still produces a Muldraugh / fallback spawn.

---

## 4. Runtime log facts

```text
Server preset: PZMF_B42_MAP8F_LOTS_SELF_001
Server loaded: pzmapforge_build42_candidate_v4_001
Client loaded: pzmapforge_build42_candidate_v4_001

Player MistaSchnazz fully connected at: 10851,9846,0
Player disconnected at: 10856,9850,0
```

IsoMetaGrid (server and client):

```text
Looking in these map folders:
<End of map-folders list>
```

WorldMapDataAssetManager warning (client):

```text
Failed to load worldmap-forest.xml from:
  C:\Users\Palmacede\Zomboid\Workshop\PZMapForge_B42_Private_Test\Contents\mods\
  pzmapforge_build42_candidate_v4_001\42\media\maps\pzmapforge_build42_candidate_v4_001\

Failed to load worldmap.xml from:
  C:\Users\Palmacede\Zomboid\Workshop\PZMapForge_B42_Private_Test\Contents\mods\
  pzmapforge_build42_candidate_v4_001\42\media\maps\pzmapforge_build42_candidate_v4_001\
```

Note: The invalid magic error from MAP-8B (worldmap.xml.bin/worldmap-forest.xml.bin)
is absent because those stubs were removed in MAP-8D/MAP-8F.
The new warning is a failed XML load, not a binary format error.

---

## 5. Classification

```text
MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED
```

---

## 6. Findings

### 6.1 lots=<MapId> makes the city selector list the candidate

The candidate map now appears in the city/spawn selection UI. This is the first
time the PZMapForge candidate appears as a selectable city. This confirms that
`lots=<MapId>` (or `lots=self`) in map.info is required for the city selector
to recognise the map.

### 6.2 City selector visibility is not proof of playable cell mount

Selecting the PZMapForge city results in a Muldraugh / fallback spawn, not the
generated 35_27 cell. IsoMetaGrid still does not list the candidate map folder.
The candidate is registered in the city-selector subsystem but not in the
IsoMetaGrid cell-loading subsystem.

### 6.3 IsoMetaGrid still does not mount the map folder

Both server and client IsoMetaGrid logs show:

```text
Looking in these map folders:
<End of map-folders list>
```

Cell loading has not occurred. The binary files 35_27.lotheader, world_35_27.lotpack,
chunkdata_35_27.bin have not been read.

### 6.4 WorldMapDataAssetManager fails to load worldmap XML

With the .bin stubs removed, the worldmap asset loader attempted to read the
uncompiled XML files (worldmap.xml, worldmap-forest.xml) and failed.
The uncompiled minimal XML stubs (`<worldmap />`) are not valid or not found
in a form the loader accepts.

This is a separate subsystem from IsoMetaGrid. The city selector and worldmap
loader can see the candidate; the cell-grid loader cannot.

### 6.5 Binary writer gate status

Binary writer gate remains closed. IsoMetaGrid did not list the map folder.
No lotheader/lotpack/chunkdata read has been logged.

---

## 7. Known discriminators identified so far

| Discriminator | Status |
|---|---|
| Workshop download/install | PASS (since MAP-7T) |
| Mod loading (server + client) | PASS (since MAP-6A) |
| lots=<MapId> city selector | PASS (MAP-8F new) |
| worldmap loader path discovery | PARTIAL (reads path, XML load fails) |
| IsoMetaGrid map folder mount | BLOCKED (still empty) |
| Binary file reads | NOT REACHED |

---

## 8. Next branch

```text
next_branch=known_working_build42_map_contract_comparator
```

Compare PZMapForge candidate version-scoped layout against a known-working
Build 42 map in the same 42\media\maps\<MapId>\ layout.

Key questions for the comparator:
1. What does the known-working map's map.info contain that PZMapForge does not?
2. Does the known-working map use worldmap.xml.bin (compiled) or worldmap.xml (plain XML)?
3. Does the known-working map have a spawnregions.lua or other required file?
4. What mod.info fields differ?
5. Does the known-working map appear in IsoMetaGrid scan? If yes, what is the structural difference?

The comparator is defined in docs/MAP_8G_KNOWN_WORKING_CONTRACT_COMPARATOR.md.

---

## 9. Claim boundary

```text
MAP8F_LOTS_SELF_VISIBLE_BUT_NOT_MOUNTED
MAP8F_CITY_SELECTOR_VISIBLE
MAP8F_ISO_META_GRID_MAP_FOLDER_LIST_EMPTY
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
```

Non-claims:
- No playable PZMapForge export claimed.
- City selector visibility is not a playable claim.
- Binary writer gate is still closed.
- IsoMetaGrid map-folder registration has not occurred.
- Player spawn location is Muldraugh / fallback, not PZMapForge content.
