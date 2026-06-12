# MAP-9Q / MAP-9AD Build 42 Static Cell Investigation

## Stable result

MAP-9Q introduced `empty_grass_v5`, a minimal valid Build 42 lotheader profile matching the known-good Dru empty 35_27 header shape.

Known-good empty header shape:

- magic: `LOTH`
- version: `1`
- entry count: `94`
- tail offset: `2037`
- tail length: `1048`
- generated hash: `5B93D32AAC8B0637867DEAD5BF95448336A1C1BC941B5BCB5775DD89422906E0`

This replaces the earlier invalid 1024-entry synthetic registry.

## Runtime observations

PZMapForge map id and workshop/mod mounting are not the blocker. Donor terrain can render under the PZMapForge map id.

Observed tests:

- MAP-9K: PZMF map id can mount and render donor terrain.
- MAP-9Q: minimal valid empty lotheader does not crash and loads wilderness/fallback terrain.
- MAP-9S/W: populated donor files render donor-looking terrain under PZMF map id.
- MAP-9W: populated donor lotheader + populated donor lotpack + empty chunkdata still renders donor terrain.
- MAP-9X: destructive lotpack tile-index mutation did not create obvious street/floor terrain.
- MAP-9Z: limited lotheader-tail index mutation did not create obvious street/floor terrain.
- MAP-9AA: zeroed lotheader tail with populated lotpack still showed donor-looking terrain.
- MAP-9AD: same-size center chunk lotpack swap did not visibly affect the player area.

## Current conclusion

The project has a valid minimal Build 42 empty header writer.

The visible authored terrain carrier is still unresolved. It is not proven to be controlled by naive LOTP record mutation, chunkdata, or simple LOTH tail u32 tile-index edits.

No public playable claim is allowed from this work.
