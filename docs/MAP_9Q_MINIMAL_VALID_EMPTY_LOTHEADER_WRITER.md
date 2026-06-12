# MAP-9Q — Minimal Valid Empty Lotheader Writer

Classification: MAP9Q_MINIMAL_VALID_EMPTY_LOTHEADER_WRITER_GENERATES_KNOWN_GOOD_HEADER_SHAPE

## Boundary

This is a Project Zomboid Build 42 binary-format diagnostic.

No playable map claim is made.
No authored visible canary claim is made.
No building, room, object, or spawn-building claim is made.

## Result

PZMapForge now has diagnostic profile:

empty_grass_v5

This profile replaces the previous fake 1024-entry lotheader registry with a known-good empty/light Dru_map-style registry.

## Why

MAP-9N proved that generic wilderness can render even when generated 35_27 files are removed.

Therefore generic forest is fallback/procedural wilderness and is not proof of authored static terrain.

MAP-9O and MAP-9P showed that a donor Dru_map `.lotheader` is the decisive component for dense donor-looking terrain under the PZMF map id.

## Bad Previous Header

Previous generated PZMF 35_27.lotheader:

- size: 29646
- magic: LOTH
- version: 1
- entry_count: 1024
- entries: blends_grassoverlays_01_0 through blends_grassoverlays_01_1023
- tail_length: 1048
- sha256: 9C5C2D62128FB1D24901BDAE2042BFADE9D6301F48375E438F42EDD1A5A61757

Problem:

The tile registry was a fake static database, not a dynamic list of actually used tiles.

## Known-Good Empty Reference

Dru_map empty/light 35_27.lotheader:

- size: 3085
- magic: LOTH
- version: 1
- entry_count: 94
- first_entry: blends_grassoverlays_01_0
- last_entry: vegetation_trees_01_9
- tail_offset: 2037
- tail_length: 1048
- sha256: 5B93D32AAC8B0637867DEAD5BF95448336A1C1BC941B5BCB5775DD89422906E0

## MAP-9Q Generated Result

Generated PZMF empty_grass_v5 35_27.lotheader:

- size: 3085
- sha256: 5B93D32AAC8B0637867DEAD5BF95448336A1C1BC941B5BCB5775DD89422906E0
- magic: LOTH
- version: 1
- entry_count: 94
- first_entry: blends_grassoverlays_01_0
- last_entry: vegetation_trees_01_9
- tail_offset: 2037
- tail_length: 1048

Generated PZMF empty_grass_v5 world_35_27.lotpack:

- size: 1056780
- sha256: C6DAF830B06FFC883ECA7E6F290DBC8AAE975AA28E04E69449450D648AFE959E

Generated PZMF empty_grass_v5 chunkdata_35_27.bin:

- size: 1026
- sha256: 5E2D8BD2247F04440AACE59349C8D5AF5F7ECF01F7F8325538714C0C357A8256

## Runtime Check

The generated v5 files were installed into the live PZMF workshop map folder.

Server config:

SpawnPoint=10746,8288,0
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001
WorkshopItems=3740642200

Observed:

- player spawned
- fallback/procedural forest rendered
- no black void
- no crash observed

## Interpretation

MAP-9Q succeeds as a structural `.lotheader` fix.

The generated profile now emits a known-good minimal empty lotheader shape.

This does not yet prove authored visible terrain.

## Next Work

MAP-9R should locate visible authored placement.

Candidate carriers:

- `world_*.lotpack` chunk payload
- populated `.lotheader` tail records
- both

Next diagnostic:

Compare PZMF generated v5 `world_35_27.lotpack` against Dru empty/light `world_35_27.lotpack` and Dru populated `world_42_31.lotpack`.

## Claim Boundary

Minimal valid empty lotheader writer: proven
Runtime no-crash/black for v5: observed
Authored visible canary: unproven
Playable PZMapForge map: false
Public playable claim allowed: false
