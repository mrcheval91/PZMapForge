# MAP-9D2 Descriptor Registration Contract Runtime Result

Classification: MAP9D2_HUMAN_RUNTIME_DESCRIPTOR_REGISTRATION_RESULT

## Boundary

This is a human runtime result.

Claude did not run Project Zomboid.
Claude did not upload Workshop.
Claude did not write Steam/PZ folders.
No third-party files copied.
No playable terrain claim.

## Branch base

MAP-9D result commit:

a8e1e07 Record MAP-9D IsoMetaGrid registration runtime result

## Purpose

MAP-9D2 tested whether the MAP-9D blocker was caused by a descriptor/folder-shape mismatch against a known working Build 42 Workshop map mod.

Known working comparator:

Workshop item: 3355966216
Mod id: Dru_map

Observed working Dru_map descriptor/folder shape:

mods/Dru_map/mod.info
mods/Dru_map/42/mod.info
mods/Dru_map/common/media/maps/Dru_map/map.info

Dru_map mod.info did not contain a map= field.

## PZMapForge D2 test state

Workshop item:

3740642200

Mod id:

pzmapforge_build42_candidate_v4_001

Live folder shape was cleaned to Dru-like shape:

mods/pzmapforge_build42_candidate_v4_001/common/media/maps/PZMapForge/map.info

Removed root media folder experiments:

mods/pzmapforge_build42_candidate_v4_001/media

Removed Build 42 media folder experiments:

mods/pzmapforge_build42_candidate_v4_001/42/media

Removed map= from both mod.info files.

Root mod.info:

id=pzmapforge_build42_candidate_v4_001
name=PZMapForge MAP-8H Parent/Child Probe
modversion=1.0

42/mod.info:

id=pzmapforge_build42_candidate_v4_001
name=PZMapForge MAP-8H Parent/Child Probe
modversion=1.0

map.info:

title=PZMapForge
fixed2x=true
description=PZMapForge parent playable cell map. Diagnostic only. Not a playable claim.

## Active server configuration

Active server:

PZMF_B42_MAP8L_WORLDMAP_XML_001

INI lines:

Mods=pzmapforge_build42_candidate_v4_001
Map=PZMapForge;Muldraugh, KY
WorkshopItems=3740642200

spawnregions.lua:

function SpawnRegions()
    return {
        { name = "PZMapForge", file = "media/maps/PZMapForge/spawnpoints.lua" },
    }
end

## Runtime evidence

Server log:

[11-06-26 17:07:54.161] LOG  : Mod f:0 st:17,810,889> loading pzmapforge_build42_candidate_v4_001.
[11-06-26 17:08:12.158] LOG  : General f:0 st:17,828,887> IsoMetaGrid.Create: begin scanning directories.
[11-06-26 17:08:12.159] LOG  : General f:0 st:17,828,887> Looking in these map folders:.
[11-06-26 17:08:12.160] LOG  : General f:0 st:17,828,888> <End of map-folders list>.
[11-06-26 17:08:12.190] LOG  : General f:0 st:17,828,919> IsoMetaGrid.Create: finished scanning directories in 0.032 seconds.

Client log:

[11-06-26 17:08:39.596] LOG  : Mod f:0 st:0> loading pzmapforge_build42_candidate_v4_001.
[11-06-26 17:08:51.070] LOG  : General f:0 st:0> IsoMetaGrid.Create: begin scanning directories.
[11-06-26 17:08:51.071] LOG  : General f:0 st:0> Looking in these map folders:.
[11-06-26 17:08:51.071] LOG  : General f:0 st:0> <End of map-folders list>.
[11-06-26 17:08:51.106] LOG  : General f:0 st:0> IsoMetaGrid.Create: finished scanning directories in 0.035 seconds.

## Result classification

MAP9D2_DRU_STYLE_DESCRIPTOR_SHAPE_STILL_BLOCKED

## Interpretation

MAP-9D2 proves that matching the known working Dru_map high-level folder convention was not sufficient.

The following state still produced an empty IsoMetaGrid map-folder list:

- Workshop item exists.
- Mod loads.
- Root mod.info exists.
- 42/mod.info exists.
- No map= field is present, matching the Dru_map comparator.
- Map folder exists under common/media/maps.
- Server Map line references the exact map folder name.
- spawnregions.lua references the exact map folder path.

Therefore the blocker is no longer explained by:

- root media/maps versus common/media/maps placement
- 42/media/maps placement
- stale server Map line
- extra map= descriptor field
- spawnregions mismatch

## Remaining discriminators

The next likely discriminator is stricter identity matching or Workshop/cache registration behavior.

Recommended next branch:

MAP-9D3: strict mod-id/map-folder identity test

Candidate test:

Use only this map folder:

common/media/maps/pzmapforge_build42_candidate_v4_001

Use server line:

Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY

Use spawnregions:

media/maps/pzmapforge_build42_candidate_v4_001/spawnpoints.lua

Keep mod.info without map=.

Rationale:

Known working Dru_map has mod id, mod folder name, map folder name, and server Map token all aligned as Dru_map.

The D2 test used:

mod id: pzmapforge_build42_candidate_v4_001
map folder: PZMapForge

So D3 should test strict identity alignment before deeper Workshop/cache research.

## Claim boundary

Playable terrain mount proven: false
Canary writing pursued: false
worldmap.xml.bin pursued: false
Public playable claim allowed: false
