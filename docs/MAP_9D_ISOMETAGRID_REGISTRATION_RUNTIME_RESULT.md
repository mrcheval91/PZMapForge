# MAP-9D IsoMetaGrid Registration Runtime Result

Classification: MAP9D_HUMAN_RUNTIME_ISOMETAGRID_REGISTRATION_RESULT

## Boundary

This is a human runtime result.

Claude did not run Project Zomboid.
Claude did not upload Workshop.
Claude did not write Steam/PZ folders.
No third-party files copied.
No playable terrain claim.

## Top commit before branch

2ddc157 Add MAP-9C IsoMetaGrid map folder registration research packet

## Human runtime context

Workshop item:

3740642200

Mod id:

pzmapforge_build42_candidate_v4_001

Active server:

PZMF_B42_MAP8L_WORLDMAP_XML_001

## Physical map folder evidence

Live map.info existed at:

D:\Program Files (x86)\Steam\steamapps\workshop\content\108600\3740642200\mods\pzmapforge_build42_candidate_v4_001\media\maps\PZMapForge\map.info

map.info contents:

title=PZMapForge
fixed2x=true
description=PZMapForge parent playable cell map. Diagnostic only. Not a playable claim.

## Active server configuration evidence

Active server INI lines:

Mods=pzmapforge_build42_candidate_v4_001
Map=PZMapForge;Muldraugh, KY
WorkshopItems=3740642200

Active spawnregions.lua:

function SpawnRegions()
    return {
        { name = "PZMapForge", file = "media/maps/PZMapForge/spawnpoints.lua" },
    }
end

## Tested variants

Variant A: common/media/maps/PZMapForge
Result: FAIL
Evidence: mod loaded, but IsoMetaGrid printed an empty map-folder list.

Variant B: common/media/maps/pzmapforge_build42_candidate_v4_001
Result: FAIL
Evidence: mod loaded, but IsoMetaGrid printed an empty map-folder list.

Variant C: 42/media/maps/PZMapForge
Result: FAIL
Evidence: mod loaded, but IsoMetaGrid printed an empty map-folder list.

Variant D: 42/media/maps/pzmapforge_build42_candidate_v4_001
Result: FAIL
Evidence: mod loaded, but IsoMetaGrid printed an empty map-folder list.

Variant E: media/maps/PZMapForge
Result: FAIL
Evidence: mod loaded, physical map.info existed, active server Map line was set to PZMapForge, but IsoMetaGrid still printed an empty map-folder list.

## Exact runtime failure signal

Server log:

[11-06-26 16:55:50.357] LOG  : Mod f:0 st:17,087,085> loading pzmapforge_build42_candidate_v4_001.
[11-06-26 16:56:07.973] LOG  : General f:0 st:17,104,702> IsoMetaGrid.Create: begin scanning directories.
[11-06-26 16:56:07.973] LOG  : General f:0 st:17,104,702> Looking in these map folders:.
[11-06-26 16:56:07.975] LOG  : General f:0 st:17,104,704> <End of map-folders list>.
[11-06-26 16:56:08.014] LOG  : General f:0 st:17,104,743> IsoMetaGrid.Create: finished scanning directories in 0.041 seconds.

Client log:

[11-06-26 16:56:35.764] LOG  : Mod f:0 st:0> loading pzmapforge_build42_candidate_v4_001.
[11-06-26 16:56:56.480] LOG  : General f:0 st:0> IsoMetaGrid.Create: begin scanning directories.
[11-06-26 16:56:56.480] LOG  : General f:0 st:0> Looking in these map folders:.
[11-06-26 16:56:56.481] LOG  : General f:0 st:0> <End of map-folders list>.
[11-06-26 16:56:56.510] LOG  : General f:0 st:0> IsoMetaGrid.Create: finished scanning directories in 0.029 seconds.

## Result classification

MAP9D_ISOMETAGRID_MAP_FOLDER_REGISTRATION_STILL_BLOCKED

## Interpretation

MAP-9D proves that the blocker is not simply one of these physical folder layouts:

common/media/maps/PZMapForge
common/media/maps/pzmapforge_build42_candidate_v4_001
42/media/maps/PZMapForge
42/media/maps/pzmapforge_build42_candidate_v4_001
media/maps/PZMapForge

MAP-9D also proves that changing the active server Map line to the exact live folder name did not cause IsoMetaGrid registration:

Map=PZMapForge;Muldraugh, KY

The mod continues to load, but IsoMetaGrid receives zero map folders.

## Claim boundary

Playable terrain mount proven: false
Canary writing pursued: false
worldmap.xml.bin pursued: false
Public playable claim allowed: false

## Next branch

MAP-9D2: mod.info / workshop descriptor / Build 42 map registration contract isolation

Recommended next focus:

- isolate whether Build 42 requires a different descriptor key than map=
- inspect known working Workshop map descriptor shape
- compare active mod.info and 42/mod.info against known working Build 42 map mod
- determine whether map folder registration is driven by mod.info, workshop metadata, server cache, or a Build 42-specific map manifest
- do not pursue canary writing
- do not pursue worldmap.xml.bin
- do not claim playable terrain
