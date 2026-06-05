# MAP-6A: Build 42 Versioned Discovery Proof

```text
Schema:           pzmapforge.discovery-proof.v0.1
Claim boundary:   evidence_record_only_not_load_tested_not_playable
PZ build:         Build 42
Session:          manual-b42-test-001
MAP-5B status:    LOAD_TEST_INCONCLUSIVE
Binary hypotheses: SUPERSEDED BY MAP-6B (see section 5)
Spawn location:   CONFIRMED VISIBLE (maptest_a spawn-region variant)
```

> **NOTE (MAP-6B):** Section 5 binary hypothesis status was superseded by MAP-6B
> manual runtime evidence. The PLAUSIBLE entries below are no longer valid.
> See `docs/MAP_6B_BINARY_FORMAT_FAILURE_RECORD.md` for current status.

---

## 1. Summary

A manual Build 42 test session proved that the PZMapForge mod loads in Build 42,
reaching the spawn-selection screen without crashing. This is the first confirmed
evidence that:

1. The Build 42 mod-loading path for PZMapForge-generated mods is **versioned
   loose-mod layout** (`<mods>/<folder>/42/mod.info`).
2. The game engine proceeds to the spawn-selection screen without error.
3. The custom PZMapForge spawn location did NOT appear in the spawn list.

No playable export is claimed. The binary file hypotheses remain untested because
the map was not loaded as a playable cell — only the mod registration was confirmed.

---

## 2. Paths tried and results

| Path | Result |
|---|---|
| `Zomboid\mods\pzmapforge_manual_b42_001\` (flat, MAP-5D Workshop layout) | Not visible in Mods screen |
| `Zomboid\Workshop\pzmapforge_manual_b42_001\` (Workshop item folder) | Not visible in Mods screen |
| `Zomboid\mods\pzmapforge_manual_b42_001\mod.info` (flat loose-mod, MAP-5A layout) | Not visible in Mods screen |
| `Zomboid\mods\pzmapforge_manual_b42_001_42\42\mod.info` | **Appeared in Mods screen** |
| `Zomboid\mods\pzmapforge_manual_b42_001_42dot0\42.0\mod.info` | **Appeared in Mods screen** |

### Confirmed Build 42 versioned loose-mod layout

```text
<Zomboid user dir>\mods\<mod_folder_name>\
  <version_number>\
    mod.info
    media\
      maps\
        <map_id>\
          map.info
          spawnpoints.lua
          ...
```

Where `<version_number>` is `42` or `42.0`. Both work. The version number must
match the `versionMin` and `pzversion` fields in `mod.info`.

---

## 3. PZ log proof

The PZ console/debug log confirmed the mod was loaded:

```
loading pzmapforge_manual_b42_001_42
```

This is direct evidence that the PZMapForge-generated mod was recognized and
processed by Build 42. The game reached the spawn-selection screen without crash.

---

## 4. Spawn-selection observation

The spawn-selection screen displayed only vanilla locations:

- Muldraugh, KY
- Riverside, KY
- Rosewood, KY
- West Point, KY

The custom PZMapForge spawn location was **not visible**.

### What this means

- The mod was loaded. The engine did not crash on the `.lotheader`, `.lotpack`,
  or `chunkdata_*.bin` files. No error was observed for the binary files.
- The custom map/spawn was not registered in the spawn-selection list.
- This is a **cell registration gap**, not a binary file failure.

### Why the spawn location may be invisible

Two candidate causes (not yet confirmed):

1. **Cell coordinate out of range**: The test cell was at (0,0). Build 42's world
   grid may not include (0,0) as a valid spawn region. Known working mods use
   non-zero coordinates (RED-Speedway: 25_15 to 26_17; ModTemplate: 1,1).

2. **Spawn registration**: The `lots` field in `map.info` or the `spawnpoints.lua`
   `worldX`/`worldY` values may not match a registered game region. Mods that
   appear in the spawn list may need coordinates within a valid region range OR
   a specific registration mechanism.

---

## 5. Binary hypothesis status after MAP-6A evidence

> **SUPERSEDED by MAP-6B.** The entries below reflected MAP-6A observations
> (mod registration only, no cell load). MAP-6B runtime evidence falsified the
> PLAUSIBLE entries. See `docs/MAP_6B_BINARY_FORMAT_FAILURE_RECORD.md`.

| Hypothesis | MAP-6A status | MAP-6B status | Basis |
|---|---|---|---|
| `.lotheader` 0-entry is accepted for blank cell | ~~PLAUSIBLE~~ | **FAILING_PLACEHOLDER_FORMAT** | EOFException at IsoLot.readInt |
| `.lotpack` zero-offset table is accepted | ~~PLAUSIBLE~~ | **UNPROVEN_AFTER_LOTHEADER_FAILURE** | Not reached; lotheader blocked load |
| `chunkdata_*.bin` 902-byte all-zero grid is accepted | ~~PLAUSIBLE~~ | **UNPROVEN_AFTER_LOTHEADER_FAILURE** | Not reached; lotheader blocked load |
| `objects.lua` comment-only is accepted | not hypothesized | **INVALID_OR_NOT_ACCEPTED** | LuaManager exception confirmed |

No playable export claim. Real binary format implementation required.

---

## 6. Current gap

```text
GAP: Custom PZMapForge spawn location not visible in Build 42 spawn-selection screen.
```

Required to close this gap:
- [ ] Try non-zero cell coordinates (e.g., worldX=1/worldY=1 per ModTemplate).
- [ ] Try cell coordinates within a known working range (e.g., 25_15 from RED-Speedway).
- [ ] Confirm whether `lots` field in `map.info` affects spawn-region registration.
- [ ] Confirm exact `worldX`/`worldY` origin convention for Build 42.
- [ ] Confirm spawn into the cell (player enters the world at the custom location).

See `scripts/prepare-spawn-region-test-packet.ps1` to generate test variants.

---

## 7. Required proof before any playable export claim

No playable export claim is permitted until:
- [ ] Custom spawn location appears in Build 42 spawn-selection screen.
- [ ] Operator successfully spawns into the custom cell.
- [ ] Operator records the result in a load-test record under `.local/`.
- [ ] The record is reviewed and a decision is made.

---

## 8. Non-claims

- This document does not claim the binary files are correct.
- This document does not claim the mod is playable.
- MAP-5B remains LOAD_TEST_INCONCLUSIVE.
- MAP-5A binary hypotheses remain UNTESTED.
- No playable Project Zomboid export claim.
