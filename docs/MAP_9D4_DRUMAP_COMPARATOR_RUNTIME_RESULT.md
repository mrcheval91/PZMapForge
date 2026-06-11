# MAP-9D4 Dru_map Comparator Runtime Result

Classification: MAP9D4_HUMAN_RUNTIME_DRUMAP_COMPARATOR_RESULT

## Boundary

This is a human runtime comparator result.

Claude did not run Project Zomboid.
Claude did not upload Workshop.
Claude did not write Steam/PZ folders.
No third-party files copied.
No PZMapForge playable terrain claim.

## Purpose

MAP-9D4 tested whether the empty IsoMetaGrid map-folder list is a valid failure signal in Build 42.

Previous MAP-9D/MAP-9D2/MAP-9D3 tests treated this signal as failure:

IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>

MAP-9D4 ran a known-good comparator mod, Dru_map, under the same Build 42 runtime.

## Comparator

Workshop item:

3355966216

Mod id:

Dru_map

Server:

PZMF_B42_DRUMAP_BASELINE_001

Known comparator server config:

Mods=Dru_map
Map=Dru_map;Muldraugh, KY
WorkshopItems=3355966216

## Comparator file inventory

Dru_map contained:

.lotheader count: 4130
.lotpack count: 4130
chunkdata count: 4130
map.info exists: True
spawnpoints.lua exists: True
objects.lua exists: True

## Runtime evidence

Server log:

[11-06-26 17:28:56.779] LOG  : Mod f:0 st:19,073,507> loading Dru_map.
[11-06-26 17:29:18.441] LOG  : General f:0 st:19,095,170> IsoMetaGrid.Create: begin scanning directories.
[11-06-26 17:29:18.442] LOG  : General f:0 st:19,095,170> Looking in these map folders:.
[11-06-26 17:29:18.443] LOG  : General f:0 st:19,095,171> <End of map-folders list>.
[11-06-26 17:29:18.557] LOG  : General f:0 st:19,095,286> IsoMetaGrid.Create: finished scanning directories in 0.116 seconds.

Despite the empty folder list, the same run then loaded comparator lotheaders:

[11-06-26 17:29:22.245] ERROR: General f:0 st:19,098,973 at IsoMetaGrid$MetaGridLoaderThread.loadCell> duplicate RoomDef.metaID for room at x=10812, y=8098, level=0, filename=42_31.lotheader.
[11-06-26 17:29:23.468] LOG  : General f:0 st:19,100,196> thread 1/8 loading 64_38.lotheader.
[11-06-26 17:29:23.468] LOG  : General f:0 st:19,100,197> thread 2/8 loading 72_39.lotheader.
[11-06-26 17:29:25.717] LOG  : General f:0 st:19,102,446> IsoMetaGrid.Create: finished loading in 7.096 seconds.

Client log:

[11-06-26 17:29:50.313] LOG  : Mod f:0 st:0> loading Dru_map.
[11-06-26 17:30:05.492] LOG  : General f:0 st:0> IsoMetaGrid.Create: begin scanning directories.
[11-06-26 17:30:05.492] LOG  : General f:0 st:0> Looking in these map folders:.
[11-06-26 17:30:05.493] LOG  : General f:0 st:0> <End of map-folders list>.
[11-06-26 17:30:05.592] LOG  : General f:0 st:0> IsoMetaGrid.Create: finished scanning directories in 0.099 seconds.

Despite the empty folder list, the same client run then loaded comparator lotheaders:

[11-06-26 17:30:08.302] ERROR: General f:0 st:0 at IsoMetaGrid$MetaGridLoaderThread.loadCell> duplicate RoomDef.metaID for room at x=10812, y=8098, level=0, filename=42_31.lotheader.
[11-06-26 17:30:18.376] LOG  : General f:0 st:0> IsoMetaGrid.Create: finished loading in 12.732 seconds.

## Result classification

MAP9D4_EMPTY_MAP_FOLDER_LIST_IS_NOT_A_RELIABLE_FAILURE_SIGNAL

## Interpretation

MAP-9D4 proves that this log sequence is not sufficient to classify map registration failure in this Build 42 runtime path:

Looking in these map folders:
<End of map-folders list>

The known-good Dru_map comparator prints the same empty folder-list section, but then proceeds to load .lotheader files through IsoMetaGrid$MetaGridLoaderThread.loadCell.

Therefore MAP-9D, MAP-9D2, and MAP-9D3 remain useful runtime evidence, but their original empty-list failure interpretation is superseded by MAP-9D4.

The new runtime discriminator must be lotheader loading evidence, not the "Looking in these map folders" list.

## New discriminator

For PZMapForge, search for whether the candidate cell is actually loaded:

35_27.lotheader
world_35_27.lotpack
chunkdata_35_27.bin

Success signal:

IsoMetaGrid$MetaGridLoaderThread.loadCell references 35_27.lotheader
or
thread N/N loading 35_27.lotheader

Failure signal:

PZMapForge mod loads, candidate files exist, but the runtime never references 35_27.lotheader.

## Next branch

MAP-9E: candidate lotheader load discriminator

Purpose:

Run/search PZMapForge strict-identity runtime logs for candidate lotheader loading evidence.

Do not use the empty "Looking in these map folders" list as the primary discriminator anymore.

## Claim boundary

PZMapForge playable terrain mount proven: false
Canary writing pursued: false
worldmap.xml.bin pursued: false
Public playable claim allowed: false
