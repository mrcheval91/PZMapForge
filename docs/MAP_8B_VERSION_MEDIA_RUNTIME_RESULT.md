# MAP-8B: Version-Scoped Media Path Runtime Result

```text
MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH
MAP8B_VERSION_42_MEDIA_PATH_VISIBLE_TO_WORLDMAP_LOADER
WORLDMAP_BIN_INVALID_MAGIC
ISO_META_GRID_MAP_FOLDER_LIST_EMPTY
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. MAP-8A recap

MAP-8A staged a Workshop payload that placed the map folder under:

```text
mods\<MapId>\42\media\maps\<MapId>\
```

MAP-8B changed the layout to additionally include:

```text
mods\<MapId>\42\media\maps\<MapId>\
```

while retaining the root `media` path for comparison.

Workshop item: 3740642200
Mod ID: pzmapforge_build42_candidate_v4_001

---

## 2. Downloaded Workshop verification

Workshop download verification was green:

```text
Workshop item: 3740642200
Mod ID: pzmapforge_build42_candidate_v4_001
42\media map path: PRESENT
root media map path: PRESENT
common folder: ABSENT
Required files: ALL PRESENT
  - 35_27.lotheader
  - world_35_27.lotpack
  - chunkdata_35_27.bin
  - streets.xml.bin
  - worldmap.xml.bin
  - worldmap-forest.xml.bin
  - worldmap.png
  - MAP8B_VERSION_42_MEDIA_PATH.txt
  - MAP8B_UPLOAD_CANARY_42_MEDIA.txt
  - MAP8B_UPLOAD_CANARY_ROOT.txt (at mod root)
```

---

## 3. Runtime visual observation

- Custom city / map appeared selectable in the map selection UI.
- Clicking it zoomed to an area of the world.
- Player spawned in a neighborhood-like area (likely vanilla / Muldraugh fallback
  or mixed worldgen -- not confirmed PZMapForge content).

---

## 4. Runtime log facts

PZ version: Project Zomboid Build 42.19.0

```text
Workshop item 3740642200: Ready
Workshop item installed to:
  D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\steamapps\workshop\content\108600\3740642200

Server loaded: pzmapforge_build42_candidate_v4_001
Client loaded: pzmapforge_build42_candidate_v4_001

Player MistaSchnazz fully connected at: 10878,10028,0
Player died at: 10885,10056,0
```

IsoMetaGrid server log:

```text
Looking in these map folders:
<End of map-folders list>
```

IsoMetaGrid client log:

```text
Looking in these map folders:
<End of map-folders list>
```

Worldmap asset loading attempts (client):

```text
C:\Users\Palmacede\Zomboid\Workshop\PZMapForge_B42_Private_Test\Contents\mods\pzmapforge_build42_candidate_v4_001\42\media\maps\pzmapforge_build42_candidate_v4_001\worldmap-forest.xml.bin
  -> java.io.IOException: invalid format (magic doesn't match)
     at zombie.worldMap.WorldMapBinary.read
     at zombie.worldMap.FileTask_LoadWorldMapBinary.call

C:\Users\Palmacede\Zomboid\Workshop\PZMapForge_B42_Private_Test\Contents\mods\pzmapforge_build42_candidate_v4_001\42\media\maps\pzmapforge_build42_candidate_v4_001\worldmap.xml.bin
  -> java.io.IOException: invalid format (magic doesn't match)
     at zombie.worldMap.WorldMapBinary.read
     at zombie.worldMap.FileTask_LoadWorldMapBinary.call
```

---

## 5. Classification

```text
MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH
```

---

## 6. Findings

### 6.1 What changed relative to MAP-7Y / MAP-8A

The 42\media path is now visible to at least the worldmap / city-selection asset
loader. The client attempted to read worldmap.xml.bin and worldmap-forest.xml.bin
from the 42\media\maps\<MapId>\ path and received a file-read attempt (not a
file-not-found). This is progress beyond MAP-7Y / MAP-8A where no such asset
read was logged.

### 6.2 IsoMetaGrid still does not mount the map folder

Both server and client IsoMetaGrid logs still show:

```text
Looking in these map folders:
<End of map-folders list>
```

The map folder is not listed. This means playable cell loading has not occurred.

### 6.3 worldmap .bin stubs are actively read and rejected

The generated worldmap.xml.bin and worldmap-forest.xml.bin stubs are now read
by the client worldmap loader. Both fail with:

```text
java.io.IOException: invalid format (magic doesn't match)
```

Stack: WorldMapBinary.read -> FileTask_LoadWorldMapBinary.call

This confirms the stubs contain no valid binary magic. The generated ASCII-marker
stubs (from MAP-7Y) do not satisfy the worldmap binary format.

### 6.4 Player fully connected

Player MistaSchnazz fully connected at 10878,10028,0. Player died at 10885,10056,0.
The player spawned and was active, but the world content at that coordinate is not
confirmed to be PZMapForge content (likely vanilla / Muldraugh fallback).

### 6.5 Binary writer gate status

The binary writer gate remains closed. IsoMetaGrid did not list the PZMapForge
map folder, so cell file loading has not been attempted by the engine.

---

## 7. Next branch candidates

```text
1. Remove invalid worldmap .bin stubs; rely on XML / PNG only.
   Rationale: stubs cause an error read that may interfere with city-selection
   logic. Removing them tests whether their absence is neutral or required.

2. Investigate the exact map.info / lots / IsoMetaGrid registration contract.
   Rationale: IsoMetaGrid still ignores the map folder. The 42\media path is
   visible to the worldmap loader but not to IsoMetaGrid. The registration
   contract that IsoMetaGrid requires may differ from the worldmap loader's
   path discovery.

3. Inspect Build 42 known-working map version-scoped structure without copying
   third-party files.
   Rationale: Understand what additional fields or files a known-working map
   provides in the 42\media path that the PZMapForge candidate does not.
```

---

## 8. Claim boundary

```text
MAP8B_PARTIAL_REGISTRATION_BREAKTHROUGH
MAP8B_VERSION_42_MEDIA_PATH_VISIBLE_TO_WORLDMAP_LOADER
WORLDMAP_BIN_INVALID_MAGIC
ISO_META_GRID_MAP_FOLDER_LIST_EMPTY
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
```

Non-claims:
- No playable PZMapForge export claimed.
- Binary writer gate is still closed.
- IsoMetaGrid map-folder registration has not occurred.
- Player spawn location is not confirmed as PZMapForge content.
- worldmap binary format is not yet understood.
