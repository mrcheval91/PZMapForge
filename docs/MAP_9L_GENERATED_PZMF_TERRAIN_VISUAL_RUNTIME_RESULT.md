# MAP-9L — Generated PZMF Terrain Visual Runtime Result

Classification: MAP9L_GENERATED_PZMF_BINARY_TERRAIN_RENDERS_VISUALLY

## Boundary

This is a human Project Zomboid Build 42 runtime result.

No public playable map claim is made.
No building, spawn building, or full gameplay claim is made.

## Runtime Setup

Workshop item:

3740642200

Mod id:

pzmapforge_build42_candidate_v4_001

Map token:

pzmapforge_build42_candidate_v4_001

Runtime server settings:

SpawnPoint=10746,8288,0
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001
WorkshopItems=3740642200

Target generated cell:

35_27

Coordinate math:

cellX=35
cellY=27
globalX=10746
globalY=8288
z=0

## Generated File Signature

Live generated PZMF files at test time:

35_27.lotheader:
- size: 29646
- sha256: 9C5C2D62128FB1D24901BDAE2042BFADE9D6301F48375E438F42EDD1A5A61757

world_35_27.lotpack:
- size: 1056780
- sha256: C6DAF830B06FFC883ECA7E6F290DBC8AAE975AA28E04E69449450D648AFE959E

chunkdata_35_27.bin:
- size: 1026
- sha256: 5E2D8BD2247F04440AACE59349C8D5AF5F7ECF01F7F8325538714C0C357A8256

## Visual Result

The client rendered terrain from the generated PZMF cell.

Observed visual result:

- outdoor terrain rendered
- grass/ground rendered
- trees/vegetation rendered
- player visible in world
- no black void at the test coordinate

## Important Method Correction

Earlier MAP-9 tests treated these as possible failure signs:

- no explicit `35_27.lotheader` log
- no explicit `world_35_27.lotpack` log
- numeric save chunk count remaining 0

MAP-9K and MAP-9L show those are not valid failure criteria.

Visual runtime rendering is the proof standard for this phase.

## Conclusion

Generated PZMapForge Build 42 binary terrain can render visually in Project Zomboid Build 42.

The following are now proven at runtime:

- PZMF Workshop item mounts
- PZMF mod id loads
- PZMF map token works
- PZMF map folder identity works
- generated `35_27` terrain can render visually
- direct coordinate spawn into generated cell works

## Remaining Blockers

The result is not yet a playable custom map claim.

Remaining work:

- add visible authored canary tile/object
- add or discover valid spawn building/RoomDef strategy
- remove reliance on donor terrain tests
- produce a clean generated package from the CLI, not only restored live workshop files
- test with a fresh player/save after clean generated install
- document exact generated binary profile used

## Next Work

MAP-9M should add an unmistakable visible canary to the generated terrain near the spawn coordinate.

Target:

globalX=10746
globalY=8288
cell=35_27

Suggested classification target:

MAP9M_VISIBLE_AUTHORED_CANARY_RENDERS

## Claim Boundary

Generated terrain renders visually: true
Authored canary renders: unproven
Spawn building/RoomDef works: unproven
Playable PZMapForge map claim allowed: false
Public playable claim allowed: false
