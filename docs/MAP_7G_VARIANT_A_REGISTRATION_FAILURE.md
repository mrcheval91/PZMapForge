# MAP-7G: Variant A Map Registration Failure

```text
MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY
VARIANT_A_TESTED_MAP_LINE
CANDIDATE_NOT_IN_MAP_FOLDER_LIST
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7G records the Variant A manual retest result and fixes the analyzer
DebugLog prefix regex to handle the real Build 42 DebugLog format.

Variant A Map= line tested:
```ini
Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
```

Result: the candidate map ID does NOT appear in the IsoMetaGrid map folder
scan list. The map folder scan is empty. The candidate is not registered.

---

## 2. Variant A test evidence

### 2.1 Test conditions

- Map= line: `Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY`
- Server: PZMF_B42_METADATA_V4_TEST_001
- Mod: pzmapforge_build42_candidate_v4_001 (empty_grass_v4, no-BOM)
- Log: `.local/map7f-packet/logs/variant-a/DebugLog-variant-a-20260606-102949.txt`

### 2.2 Map folder scan (exact log extract)

```text
[06-06-26 10:27:38.628] LOG  : General      f:0 st:0> Looking in these map folders:.
[06-06-26 10:27:38.629] LOG  : General      f:0 st:0> <End of map-folders list>.
```

The map folder list is empty. The candidate ID does NOT appear.

### 2.3 Load progression

```text
loading pzmapforge_build42_candidate_v4_001       (mod loaded)
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders:
<End of map-folders list>                          (EMPTY -- candidate absent)
IsoMetaGrid.Create: finished scanning directories in 0.044 seconds
IsoMetaGrid.Create: begin loading
IsoMetaGrid.Create: finished loading in 11.002 seconds
initSpawnBuildings: no room or building at 150,150,0  (spawn warning persists)
game loading took 35 seconds
STATE: exit zombie.gameStates.GameLoadingState
Game Mode: Multiplayer
```

### 2.4 In-game observation

- Game mode Multiplayer reached.
- No city choice dialog appeared.
- Player spawned directly in a forest/grass/wilderness world (Muldraugh terrain).
- World was NOT a black void.
- Spawn warning persisted.

### 2.5 Key finding

The Map= line `pzmapforge_build42_candidate_v4_001;Muldraugh, KY` caused the
game to load Muldraugh terrain (not a black void), confirming that Muldraugh, KY
IS loaded when listed in the Map= line. However, `pzmapforge_build42_candidate_v4_001`
does NOT appear in the map folder scan list.

This means the candidate map ID is NOT being resolved to a filesystem path that
IsoMetaGrid can scan. The Map= line recognition mechanism for custom mods differs
from the built-in map entries.

---

## 3. Analyzer fix (MAP-7G)

### 3.1 MAP-7F fix was incomplete

The MAP-7F analyzer fix updated `Strip-DebugLogPrefix` to handle a synthetic
timestamped format: `[date] LOG : General     , timestamp> `.

But the real PZ Build 42 DebugLog format is:
```text
[date time.ms] LOG  : General      f:N st:N> message.
[date time.ms] WARN : General      f:N st:N at Class.method          > message.
```

The old regex `^\[.*?\]\s+LOG\s*:\s+\S+\s*,\s*\d+>\s*` does not match the
real format (which uses `f:N` not `, timestamp`). So the end marker
`<End of map-folders list>` was not stripped of its prefix and the anchored
check `^<End of map-folders list>` failed. Result: `map_folders_list_empty=False`
even for the real Variant A log.

### 3.2 Fixed regex

```powershell
$s = $Line -replace '^\[.*?\]\s+\w+\s*:\s+\w+\s+f:\d+[^>]*>\s*', ''
```

This handles:
- `f:N st:N> ` — LOG lines (normal)
- `f:N>` — early startup lines (no st:N)
- `f:N st:N at Class.method          > ` — WARN lines with at-path
- `f:N st:N,M,P at Class.method> ` — multiplayer lines with tuple st

For bare lines (no timestamp prefix), the regex does not match and the
line is returned unchanged.

### 3.3 MAP-7F tests updated

The synthetic fixtures in `test-build42-map7f-registration-diagnostic.ps1`
were updated to use the real `f:0 st:0>` format instead of the old
`, timestamp>` format. All 11 MAP-7F assertions still pass.

---

## 4. New analyzer parameters

### 4.1 -ExpectedMapId

When provided, populates `expected_map_id` in the output JSON.

### 4.2 -VariantLabel

When provided with `-ExpectedMapId` and an empty map folder scan, the
classification becomes `MAP7F_<VARIANT_KEY>_MAP_FOLDER_SCAN_EMPTY`.

Conversion: `VariantA` → `VARIANT_A` → `MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY`

### 4.3 Usage for Variant A

```powershell
powershell -ExecutionPolicy Bypass `
    -File .\scripts\inspect-build42-map7d-load-result.ps1 `
    -LogPath .\.local\map7f-logs\DebugLog-variant-A.txt `
    -Output .\.local\map7f-analysis\variant-A `
    -ExpectedMapId pzmapforge_build42_candidate_v4_001 `
    -VariantLabel VariantA
```

Expected output field:
```text
classification: MAP7F_VARIANT_A_MAP_FOLDER_SCAN_EMPTY
map_folders_list_empty: true
timestamped_debuglog_detected: true
expected_map_id: pzmapforge_build42_candidate_v4_001
variant_label: VariantA
```

---

## 5. Diagnosis

### 5.1 Map= line effect

The presence of `Muldraugh, KY` in the Map= line caused Muldraugh terrain to
load. This confirms:
- Built-in map entries (Muldraugh, KY) ARE resolved from the Map= line.
- Custom mod map IDs are NOT resolved the same way.

### 5.2 Why the candidate is absent from IsoMetaGrid scan

Hypothesis: IsoMetaGrid scans the filesystem path derived from the Map= entry
against a registry of known map mod folders. Custom mod map folders may require
explicit path registration, Workshop subscription, or a different discovery
mechanism than what the Map= line alone provides.

The candidate mod files exist under:
```text
<PZ mods>/<mod_folder>/42/media/maps/<map_id>/
```

IsoMetaGrid may be looking for map folders under:
```text
<PZ install>/media/maps/<map_id>/
```

...or via a different discovery path not covered by the current candidate layout.

### 5.3 Next steps

- Test Variants B and C to confirm the Map= line format is not the variable.
- Check whether the candidate map folder path needs to be under the PZ install
  media/maps directory (not in the mod folder).
- Check whether `Map=pzmapforge_build42_candidate_v4_001` requires a world.ini
  or similar discovery file.
- Investigate whether IsoMetaGrid reads a map-folder list from a separate config
  rather than the Map= server ini line.

---

## 6. Non-claims

- This document is a controlled diagnostic only.
- No load test was performed by any script.
- The Muldraugh world entry does NOT confirm the candidate map loaded.
- LOAD_TEST_NOT_PERFORMED
- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false: binding.
