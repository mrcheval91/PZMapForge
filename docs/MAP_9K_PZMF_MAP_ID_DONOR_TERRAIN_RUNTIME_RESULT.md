# MAP-9K — PZMF Map Identity Donor Terrain Runtime Result

Classification: MAP9K_PZMF_MAP_ID_CAN_MOUNT_AND_RENDER_DONOR_TERRAIN

## Boundary

This is a human Project Zomboid Build 42 runtime result.

No public playable PZMapForge terrain claim is made.
No generated PZMapForge terrain was proven playable by this test.
This test used donor terrain files from the known-good Dru_map comparator.

## Test Setup

Workshop item:

3740642200

Mod id:

pzmapforge_build42_candidate_v4_001

Server map token:

pzmapforge_build42_candidate_v4_001

Runtime server settings:

SpawnPoint=12750,9450,0
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001
WorkshopItems=3740642200

Map folder tested:

common/media/maps/pzmapforge_build42_candidate_v4_001

The map folder contained full donor terrain copied from the known-good Dru_map comparator, but renamed under the PZMF map folder identity.

The test spawned into donor populated cell 42_31:

cellX=42
cellY=31
globalX=12750
globalY=9450
z=0

## Visual Result

The client rendered outdoor terrain successfully.

Visible result:

- grass rendered
- vegetation rendered
- trees/bushes rendered
- player visible in world

## Important Negative Method Finding

Previous checks are not valid failure proofs:

- Numeric save chunk count can remain 0 even while terrain visibly renders.
- Missing explicit lotheader filenames in logs can occur even while terrain visibly renders.
- `CellLoader.LoadCellBinaryChunk start` without visible filename is not enough to prove or disprove cell terrain load.

Visual runtime rendering is the decisive proof for this phase.

## Conclusion

The following are proven working for the PZMF workshop item:

- Workshop mount
- PZMF mod id
- PZMF map folder name
- `Map=pzmapforge_build42_candidate_v4_001`
- `common/media/maps/<mapid>` terrain mount path
- Build 42 runtime registration for the PZMF map identity

The remaining blocker is no longer map registration.

The remaining blocker is the PZMapForge-generated binary terrain payload:

- `.lotheader`
- `world_*.lotpack`
- `chunkdata_*.bin`

## Next Work

MAP-9L should restore generated PZMapForge terrain and use visual runtime rendering as the proof standard.

Suggested next classification target:

MAP9L_GENERATED_BINARY_TERRAIN_RENDERING_DISCRIMINATOR

## Claim Boundary

PZMF map identity can render donor terrain: true
PZMapForge-generated terrain renders: unproven
Playable PZMapForge map claim allowed: false
Public playable claim allowed: false
