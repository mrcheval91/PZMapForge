# MAP-9E Candidate Lotheader Load Discriminator

Classification: MAP9E_CANDIDATE_MOD_LOADS_BUT_35_27_LOTHEADER_NOT_REFERENCED

## Boundary

This is a human runtime result.

Claude did not run Project Zomboid.
Claude did not upload Workshop.
Claude did not write Steam/PZ folders.
No third-party files copied.
No playable terrain claim.

## Supersedes older discriminator

MAP-9D4 proved that this log sequence is not a reliable failure signal in Build 42:

IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>

Known-good Dru_map prints the same empty list, then proceeds to load lotheaders.

Therefore the new runtime discriminator is:

Does IsoMetaGrid reference/load the candidate .lotheader file?

## Candidate target

PZMapForge candidate cell:

35_27

Expected files:

35_27.lotheader
world_35_27.lotpack
chunkdata_35_27.bin

## Candidate mod load evidence

Search across all current Project Zomboid logs found a PZMapForge mod load event:

[11-06-26 17:27:41.497] LOG  : Mod f:0> loading pzmapforge_build42_candidate_v4_001.

## Candidate lotheader search evidence

Exact search across all current Project Zomboid logs for:

35_27
world_35_27
chunkdata_35_27

returned no hits.

## Comparator evidence

Dru_map produced lotheader load evidence in the same runtime family:

IsoMetaGrid$MetaGridLoaderThread.loadCell
filename=42_31.lotheader

Therefore Build 42 logs do show lotheader loading when terrain is actually being loaded.

## Result classification

MAP9E_CANDIDATE_MOD_LOADS_BUT_35_27_LOTHEADER_NOT_REFERENCED

## Interpretation

PZMapForge is past the basic mod-load stage.

The current blocker is not proven to be:

- missing .lotheader file
- missing .lotpack file
- missing chunkdata file
- empty "Looking in these map folders" list
- casing mismatch
- stale Map token
- missing terrain files in the physical map folder

The current blocker is:

The runtime loads the mod id, but does not select or load the candidate cell terrain file 35_27.lotheader.

## Next branch

MAP-9F: cell coordinate / map.info / lotheader selection contract

Primary question:

Why does Build 42 not select 35_27.lotheader after the mod is loaded?

Next discriminators:

- compare Dru_map map.info fields against PZMapForge map.info
- inspect whether lots=NONE, zoomX, zoomY, zoomS affect lotheader discovery
- inspect whether cell coordinate 35_27 is outside the active loaded world window or hidden by Muldraugh ordering
- test whether Map order affects candidate cell selection
- test whether removing Muldraugh changes lotheader selection
- search logs for map bounds and cell selection around 35_27
- do not use the empty map-folder-list line as the primary discriminator anymore

## Claim boundary

Playable terrain mount proven: false
Canary writing pursued: false
worldmap.xml.bin pursued: false
Public playable claim allowed: false
