# MAP-9F Direct Spawn Coordinate Runtime Result

Classification: MAP9F_DIRECT_SPAWN_REACHES_COORDS_BUT_STATIC_CELL_MOUNT_REMAINS_UNPROVEN

## Boundary

This is a human Project Zomboid Build 42 runtime result.

Claude did not run Project Zomboid.
Claude did not upload Workshop.
Claude did not write Steam or Project Zomboid runtime folders.
No playable terrain claim is made.

## Server configuration tested

Server:

PZMF_B42_MAP9F_NO_MULDRAUGH_001

Key settings:

Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001
WorkshopItems=3740642200
SpawnPoint=10746,8288,0

The configured spawn point is inside candidate cell 35_27.

Cell 35_27 tile range:

X 10500..10799
Y 8100..8399

Spawn point:

X 10746
Y 8288
Z 0

## Runtime evidence

Server log:

CreatePlayerPacket.processServer > position:10746,8288,0

Client log:

SpawnPoints.initSpawnBuildings > initSpawnBuildings: no room or building at 10746,8288,0.

Client and server both reached CellLoader.LoadCellBinaryChunk.

## Negative evidence

Searches for these strings returned no meaningful terrain-load evidence:

35_27
world_35_27
chunkdata_35_27
lotheader
map_1074_828

Save-folder scan found:

Numeric chunk count: 0
Expected spawn chunk count: 0

## Interpretation

The direct SpawnPoint was accepted by the server.

Therefore the current blocker is not the SpawnPoint coordinate itself.

However, the runtime still does not prove that the candidate static terrain files were selected or mounted:

35_27.lotheader
world_35_27.lotpack
chunkdata_35_27.bin

Because the current candidate is an empty-grass diagnostic cell with no buildings, the warning "no room or building" is not by itself a failure signal.

The empty-grass candidate is now too weak as a runtime proof target.

## Next branch

MAP-9G: visible canary terrain writer.

Goal:

Write an unmistakable visible terrain canary into cell 35_27 near the direct spawn point, then rerun the same direct-spawn server.

The next proof must answer:

Can the player visibly see a non-default PZMapForge-authored tile/object at or near 10746,8288?

## Claim boundary

Direct spawn coordinate accepted: true
Static candidate terrain mount proven: false
Playable terrain claim allowed: false
Public playable claim allowed: false
