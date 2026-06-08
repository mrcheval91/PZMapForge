# MAP-7V: K004 / K006 Control Results

```text
MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED
MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED
BINARY_FORMAT_INVESTIGATION_PAUSED
RUNTIME_MAP_REGISTRATION_IS_NEXT_BRANCH
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7V records two human-only control tests that close the current
binary-format investigation branch and redirect focus to runtime map
registration / map folder mounting.

K004 tested coordinate-aligned binaries. K006 tested zero binaries.
Both confirm: the active gap is not in the binary file format. The
active gap is that the PZMapForge map folder is not being mounted or
registered for IsoMetaGrid.

---

## 2. K004: Coordinate-aligned test

### 2.1 Setup

```text
Workshop ID: 3740642200
Mod ID:      pzmapforge_build42_candidate_v4_001

Payload:
  35_27.lotheader
  world_35_27.lotpack
  chunkdata_35_27.bin

map.info:
  zoomX=10505
  zoomY=12220
  zoomS=14.5

spawnpoints.lua:
  worldX=35, worldY=27, posX=246, posY=188
```

### 2.2 Evidence

| Signal | Result |
|---|---|
| Workshop Ready | YES |
| Mod loaded | YES |
| Player reached multiplayer | YES |
| Spawnpoint target honored | YES — warning at 10746,8288,0 |
| Visible result | fallback forest |
| Expected candidate lotheader/meta evidence | ABSENT |
| Map folder scan | EMPTY |
| Coordinate alignment sufficient | NO |

### 2.3 Interpretation

K004 proves coordinate alignment alone is not sufficient.
The spawn target was honored (warning confirmed worldX=35/worldY=27
translates to world-coords 10746,8288,0), but the map loaded into
the fallback forest. The candidate binaries were present but did not
cause the map folder to be mounted.

---

## 3. K006: No-binary fixed-spawn control

### 3.1 Setup

```text
Workshop ID: 3740642200
Mod ID:      pzmapforge_build42_candidate_v4_001

Payload:
  mod.info (present)
  42/mod.info (present)
  common/media/maps/<MapId>/ (present)
  map.info (present)
  objects.lua (present)
  spawnpoints.lua — SpawnPoints() style (present)

  lotheader count: 0
  lotpack count:   0
  chunkdata count: 0
```

### 3.2 Evidence

| Signal | Result |
|---|---|
| Workshop Ready | YES |
| Mod loaded | YES (server loaded pzmapforge_build42_candidate_v4_001) |
| IsoMetaGrid map folder scan | EMPTY |
| Spawn target honored | YES — no room/building at 10746,8288,0 |
| Expected candidate lotheader evidence | ABSENT |
| Server reached SANITY CHECK FAIL | YES |
| Binary file participation | ZERO |

### 3.3 Why SANITY CHECK FAIL is not binary evidence

K006 had zero PZMapForge lotheader, lotpack, and chunkdata files.
SANITY CHECK FAIL occurred AFTER the candidate was loaded and the spawn
was attempted. It is not a binary file parse error. It is not caused by
PZMapForge binary files.

SANITY CHECK FAIL in the context of a no-binary Workshop mod loaded at a
specific cell coordinate is standard PZ behavior when the spawn point
target lacks the required map infrastructure.

This failure cannot be attributed to PZMapForge binary format quality.

### 3.4 Interpretation

K006 proves:
- `spawnpoints.lua` can be active without any lotheader/lotpack/chunkdata.
- The spawn point coordinate is honored by PZ regardless of binary presence.
- The map folder scan remains empty even with a properly structured mod.
- The active blocker is NOT missing binary files.
- The active blocker is that the map folder is not being MOUNTED/REGISTERED
  for IsoMetaGrid even when the Workshop mod is ready.

---

## 4. Combined interpretation

### 4.1 What K004 + K006 prove

```text
Workshop activation:  WORKS
Mod loading:          WORKS
spawnpoints.lua:      WORKS (spawn coordinates honored)
Coordinate alignment: NOT the discriminator
Binary presence:      NOT the discriminator
```

### 4.2 What is not proven

```text
PZMapForge binary files are NOT participating in runtime map loading.
The map folder is NOT being mounted/registered for IsoMetaGrid.
The binary file format has NOT been validated or rejected.
No explicit binary parse error (EOFException) has been observed.
```

### 4.3 Why binary format investigation is paused

K006 shows that even with zero PZMapForge binary files, the result is
identical: Workshop Ready + mod loaded + spawn honored + fallback forest.

Adding binary files back (K004) did not change the outcome.

Therefore the binary file format is not the current discriminator.
Investigating LOTH/LOTP/chunkdata format details would be misdirected
work until the map folder mounting gap is closed.

---

## 5. Previous fallback forest results cannot be binary evidence

All results from K002, K004, K006, and the earlier variant tests A–I
produced fallback forest. K006 confirms this result is achievable with
ZERO binary files. The fallback forest result was never binary evidence.

It was always evidence of missing map registration/mounting, not
missing binary format correctness.

---

## 6. Next branch: runtime map registration / map folder mounting

The question to answer:

> What does PZ check in order to mount a Workshop mod's map folder
> with IsoMetaGrid, and what does the PZMapForge mod currently lack?

Investigation targets:
1. Server-side IsoMetaGrid scan log (client log may not show all evidence).
2. Whether PZ requires a specific mod structure at the Steam Workshop
   content path level (not just inside the mod subfolder).
3. Whether map folder mounting requires a `worldmap.lua` or `maps.lua` index file.
4. Whether the `mods/<modid>` subfolder path is what IsoMetaGrid actually scans.

---

## 7. Claim boundary

```text
MAP7V_K004_COORDINATE_ALIGNED_RESULT_RECORDED
MAP7V_K006_ZERO_BINARY_CONTROL_RECORDED
BINARY_FORMAT_INVESTIGATION_PAUSED
RUNTIME_MAP_REGISTRATION_IS_NEXT_BRANCH
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_PZ_RUN_BY_SCRIPT
NO_WORKSHOP_UPLOAD_BY_SCRIPT
```

Non-claims:
- No playable PZMapForge export claimed.
- No PZ load success claimed.
- No binary format success claimed.
- No binary parse failure observed.
- No explicit EOFException on candidate lotheader observed.
