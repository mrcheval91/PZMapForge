# MAP-9D3 Strict Identity Registration Runtime Result

Classification: MAP9D3_HUMAN_RUNTIME_STRICT_IDENTITY_RESULT

## Boundary

This is a human runtime result.

Claude did not run Project Zomboid.
Claude did not upload Workshop.
Claude did not write Steam/PZ folders.
No third-party files copied.
No playable terrain claim.

## Branch base

MAP-9D2 result commit:

f829853 Record MAP-9D2 descriptor registration runtime result

## Purpose

MAP-9D3 tested whether IsoMetaGrid registration requires strict identity alignment between:

- mod id
- mod folder name
- map folder name
- server Map token
- spawnregions name/path

Known working comparator Dru_map appears to align these identities:

- mod id: Dru_map
- mod folder: Dru_map
- map folder: Dru_map
- server Map token: Dru_map

MAP-9D2 used a non-identical map folder name:

- mod id: pzmapforge_build42_candidate_v4_001
- map folder: PZMapForge

MAP-9D3 removed that difference.

## D3 runtime test state

Workshop item:

3740642200

Mod id:

pzmapforge_build42_candidate_v4_001

Live folder shape:

mods/pzmapforge_build42_candidate_v4_001/common/media/maps/pzmapforge_build42_candidate_v4_001/map.info

Removed experimental folders:

mods/pzmapforge_build42_candidate_v4_001/media
mods/pzmapforge_build42_candidate_v4_001/42/media
mods/pzmapforge_build42_candidate_v4_001/common/media/maps/PZMapForge

Root mod.info had no map= field.

42/mod.info had no map= field.

## Terrain file inventory proof

The D3 map folder contained the required terrain files at the map-folder level:

35_27.lotheader
chunkdata_35_27.bin
world_35_27.lotpack
map.info
spawnpoints.lua
objects.lua
worldmap.xml
worldmap.xml.bin
thumb.png

Required terrain file counts:

.lotheader count: 1
.lotpack count: 1
chunkdata count: 1
map.info count: 1
spawnpoints count: 1
objects count: 1

Therefore this was not a missing-.lotheader failure.

## Active server configuration

Active server:

PZMF_B42_MAP8L_WORLDMAP_XML_001

INI lines:

Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
WorkshopItems=3740642200

spawnregions.lua:

function SpawnRegions()
    return {
        { name = "pzmapforge_build42_candidate_v4_001", file = "media/maps/pzmapforge_build42_candidate_v4_001/spawnpoints.lua" },
    }
end

## Runtime evidence

Server log:

[11-06-26 17:16:32.588] LOG  : Mod f:0 st:18,329,316> loading pzmapforge_build42_candidate_v4_001.
[11-06-26 17:16:50.039] LOG  : General f:0 st:18,346,767> IsoMetaGrid.Create: begin scanning directories.
[11-06-26 17:16:50.039] LOG  : General f:0 st:18,346,767> Looking in these map folders:.
[11-06-26 17:16:50.040] LOG  : General f:0 st:18,346,769> <End of map-folders list>.
[11-06-26 17:16:50.074] LOG  : General f:0 st:18,346,802> IsoMetaGrid.Create: finished scanning directories in 0.034 seconds.

Client log:

[11-06-26 17:17:23.356] LOG  : Mod f:0 st:0> loading pzmapforge_build42_candidate_v4_001.
[11-06-26 17:17:34.144] LOG  : General f:0 st:0> IsoMetaGrid.Create: begin scanning directories.
[11-06-26 17:17:34.144] LOG  : General f:0 st:0> Looking in these map folders:.
[11-06-26 17:17:34.144] LOG  : General f:0 st:0> <End of map-folders list>.
[11-06-26 17:17:34.181] LOG  : General f:0 st:0> IsoMetaGrid.Create: finished scanning directories in 0.036 seconds.

## Result classification

MAP9D3_STRICT_IDENTITY_ALIGNMENT_STILL_BLOCKED

## Interpretation

MAP-9D3 proves that strict id/folder/Map-token alignment is not sufficient.

The blocker is no longer explained by:

- physical folder placement
- root media/maps versus common/media/maps
- 42/media/maps
- stale server Map token
- map folder name mismatch
- spawnregions mismatch
- mod.info map= field
- absence of mod.info map= field
- non-identical map folder naming
- missing .lotheader / .lotpack / chunkdata files

The mod loads, required terrain files exist, but IsoMetaGrid receives zero registered map folders.

## Next branch

MAP-9D4: known-good comparator runtime proof

Purpose:

Run the known working Dru_map configuration under the same Build 42 runtime and collect the same IsoMetaGrid log signal.

Required discriminator:

If Dru_map produces a non-empty "Looking in these map folders" list, then PZMapForge is missing a registration/cache/workshop contract detail.

If Dru_map also produces an empty "Looking in these map folders" list but still works, then the current MAP-9 signal assumption is wrong and IsoMetaGrid folder-list logging is not a reliable success discriminator in this Build 42 path.

Known comparator configuration:

Mods=Dru_map
Map=Dru_map;Muldraugh, KY
WorkshopItems=3355966216

## Claim boundary

Playable terrain mount proven: false
Canary writing pursued: false
worldmap.xml.bin pursued: false
Public playable claim allowed: false
