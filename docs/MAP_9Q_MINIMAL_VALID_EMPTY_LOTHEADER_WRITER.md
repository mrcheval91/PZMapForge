# MAP-9Q — Minimal Valid Empty Lotheader Writer

Classification: MAP9Q_MINIMAL_VALID_EMPTY_LOTHEADER_WRITER_GENERATES_KNOWN_GOOD_HEADER_SHAPE

## Boundary

This is a Project Zomboid Build 42 binary-format diagnostic.

No playable map claim is made.
No authored visible canary claim is made.
No building, room, object, or spawn-building claim is made.

## Why MAP-9Q Exists

MAP-9N proved that generic forest can render even when generated cell files are removed.

Therefore generic forest is fallback/procedural wilderness and is not proof that generated static files are controlling terrain.

MAP-9O and MAP-9P then showed that a donor `.lotheader` from Dru_map is the decisive component for dense donor-looking terrain under the PZMF map id.

The generated PZMF `.lotheader` was identified as the main blocker.

## Previous Bad Header

The previous generated PZMF header used:

- LOTH magic
- version 1
- 1024 fake sequential tile entries:
  - blends_grassoverlays_01_0
  - ...
  - blends_grassoverlays_01_1023
- 1048-byte mostly-zero trailer

Observed generated signature:

- size: 29646
- entry_count: 1024
- tail_length: 1048
- sha256: 9C5C2D62128FB1D24901BDAE2042BFADE9D6301F48375E438F42EDD1A5A61757

This was not a valid minimal empty-cell shape.

## Known-Good Empty Reference

Dru_map empty/light cell `35_27.lotheader`:

- size: 3085
- magic: LOTH
- version: 1
- entry_count: 94
- first entry: blends_grassoverlays_01_0
- last entry: vegetation_trees_01_9
- tail_offset: 2037
- tail_length: 1048
- sha256: 5B93D32AAC8B0637867DEAD5BF95448336A1C1BC941B5BCB5775DD89422906E0

The 1048-byte trailer begins with:

- U32 8
- U32 8
- then zeroes

## Implementation

A new diagnostic profile was added:

empty_grass_v5

This profile writes a Dru-empty-style minimal valid lotheader registry:

- 94 LF-terminated tile names
- entry_count = 94
- same canonical 1048-byte empty trailer
- no 1024 fake contiguous grass-overlay registry
- no BOM for game-read text files

## Generated MAP-9Q Result

PZMapForge generated:

35_27.lotheader:

- size: 3085
- sha256: 5B93D32AAC8B0637867DEAD5BF95448336A1C1BC941B5BCB5775DD89422906E0
- magic: LOTH
- version: 1
- entry_count: 94
- first_entry: blends_grassoverlays_01_0
- last_entry: vegetation_trees_01_9
- tail_offset: 2037
- tail_length: 1048

world_35_27.lotpack:

- size: 1056780
- sha256: C6DAF830B06FFC883ECA7E6F290DBC8AAE975AA28E04E69449450D648AFE959E

chunkdata_35_27.bin:

- size: 1026
- sha256: 5E2D8BD2247F04440AACE59349C8D5AF5F7ECF01F7F8325538714C0C357A8256

## Runtime Check

The generated v5 files were installed into the live PZMF workshop map folder.

Server config:

SpawnPoint=10746,8288,0
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001
WorkshopItems=3740642200

Observed result:

- player spawned in world
- generic forest/wilderness rendered
- no black void
- no crash observed

## Interpretation

MAP-9Q is successful as a structural lotheader fix.

The game still renders fallback/procedural wilderness, which is expected because no authored visible tile/object has been encoded yet.

The important improvement is that PZMapForge no longer emits the malformed 1024-entry fake tile registry for the new diagnostic profile.

## Remaining Work

MAP-9R should decode actual visible placement.

Possible next targets:

- compare `world_35_27.lotpack` from Dru empty/light and PZMF generated
- decode populated `42_31.lotheader` tail records
- determine whether visible tile placement is carried in lotpack chunk payload, populated lotheader tail records, or both
- generate one unmistakable authored canary after the carrier is identified

## Claim Boundary

Minimal valid empty lotheader writer: proven
Runtime no-crash/black for v5: observed
Authored visible canary: unproven
Playable PZMapForge map: false
Public playable claim allowed: false
