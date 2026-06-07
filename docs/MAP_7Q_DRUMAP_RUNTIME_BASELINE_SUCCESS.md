# MAP-7Q: Dru_map Runtime Baseline Success and Corrected Evidence Model

```text
MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS
EMPTY_CLIENT_SCAN_NOT_DECISIVE
DRUMAP_BASELINE_RUNTIME_SUCCESSFUL
VARIANTS_ABCDEFGHI_EXHAUSTED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7Q records the Dru_map baseline result (from MAP-7P) as runtime successful
and corrects the Build 42 analyzer evidence model.

The MAP-7P analyzer produced `MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY`,
but the human result was unambiguously successful: player spawned into a real
built Drummondville/Dru_map world with roads and houses visible.

This means the old assumption -- that an empty printed client map-folder scan
is a decisive failure signal in Build 42 coop/server -- is false.

The analyzer is updated with a stronger multi-signal runtime success model.

---

## 2. Dru_map baseline human result

### 2.1 Server wiring

```text
Server:         PZMF_B42_DRUMAP_BASELINE_001
Mods:           Mods=Dru_map
WorkshopItems:  WorkshopItems=3355966216
Map:            Map=Dru_map;Muldraugh, KY
Public:         false
spawnregions:   media/maps/Dru_map/spawnpoints.lua
```

### 2.2 Human visual result

- Dru_map worked.
- Player spawned into a real built Drummondville/Dru_map-looking world.
- Roads and houses were visible.
- This was NOT the fallback forest world.
- The game reached multiplayer.

### 2.3 Runtime evidence in log

```text
Workshop ID 3355966216: Installed / Ready
Dru_map loaded
Player data received from the server
Game reached multiplayer
IsoMetaGrid/loadCell/RoomDef referenced real Dru_map lotheader files:
  43_30.lotheader
  42_31.lotheader
  43_31.lotheader
  (and related lotheader/meta evidence)
```

### 2.4 What the old analyzer produced

```text
MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY
```

The client log printed:
```text
Looking in these map folders:
<End of map-folders list>
```

The old analyzer treated this as a failure. This was incorrect.

---

## 3. Corrected evidence model

### 3.1 The incorrect assumption

> "If the printed client map-folder scan is empty, the map is not loaded."

This assumption was formed from PZMapForge candidate test results A through I.
It does not hold for Build 42 coop/server with Workshop-activated mods.

### 3.2 The correct evidence model

For Build 42 coop/server, the decisive runtime success signals are:

| Signal | Meaning |
|---|---|
| Workshop ID Installed / Ready | Steam has downloaded and mounted the Workshop mod |
| Expected mod loaded | The expected mod/map identifier appeared in the log |
| lotheader/meta load evidence | IsoMetaGrid reached and processed lotheader files for the expected map |
| Player data received | Server confirmed player session |
| Game Mode: Multiplayer | Client entered multiplayer state |
| Human visual built-world | Visible roads/buildings confirm map registration, not fallback world |

### 3.3 Why the client scan can print empty

In Build 42 coop/server, IsoMetaGrid may print the map-folder scan list
before Workshop mod assets are fully mounted into the scan path.
Workshop mods may be registered through a separate runtime mount path
that does not appear in the printed scan list.

The printed scan list is a diagnostic hint, not the authority.
The authority is whether the runtime reached actual lotheader/meta loading
for the expected map and the player entered the game world.

### 3.4 What remains true for PZMapForge candidate

The PZMapForge candidate (local loose mod) has NOT produced any of:
- Workshop ID Installed / Ready state
- lotheader/meta load evidence
- Player entry into a built custom-map world

The PZMapForge candidate still only reaches:
- Mod loaded at the PZ mod registration level
- Empty map folder scan
- Forest/fallback world

This confirms: the PZMapForge candidate is NOT mounted/activated the same way
that Workshop-downloaded Dru_map is. The empty scan is still a valid failure
signal for the PZMapForge candidate because it lacks the Workshop runtime
activation that Dru_map has.

---

## 4. Static variants exhausted

```text
A through I: MAP_FOLDER_SCAN_EMPTY confirmed.
VARIANTS_ABCDEFGHI_EXHAUSTED.
```

No further static layout changes are needed. All layout combinations
within the local mod path have been tested.

---

## 5. Next investigation: runtime activation / mounting

The discriminator between Dru_map (works) and PZMapForge candidate (does not)
is runtime activation -- specifically the Workshop subscription / download /
Installed / Ready flow and the runtime mount path that Workshop-downloaded
mods use to register with IsoMetaGrid.

Investigation targets:
1. `WorkshopItems=` line in server ini -- is this what triggers the mount?
2. Workshop Installed/Ready state -- does PZ require this to mount a map?
3. Local mod activation path -- does a local loose mod without Workshop flow
   ever reach the same mount path as a Workshop-downloaded mod?
4. WorkshopItems pointing to a local folder -- does PZ support this?

### 5.1 What NOT to investigate next

- Do not mutate static mod package layout further (variants exhausted).
- Do not investigate LOTH/LOTP/chunkdata binary format until PZMapForge
  candidate produces lotheader/meta load evidence comparable to Dru_map.
- Do not claim PZMapForge playable export.

### 5.2 The lotheader/binary writer evidence gate

Until PZMapForge candidate produces:
```text
lotheader/meta load evidence referencing PZMapForge lotheader files
```
...the binary writer quality (LOTH/LOTP/chunkdata format) is not the active
blocker and should not be the investigation focus.

---

## 6. Analyzer update summary

Updated: `scripts/inspect-build42-map7d-load-result.ps1`

New extraction fields:
- `expected_mod_loaded` -- "loading $ExpectedMapId" detected
- `workshop_id_3355966216_seen` -- "3355966216" detected in log
- `workshop_download_seen` -- Workshop download signal detected
- `workshop_installed_seen` -- Workshop Installed signal detected
- `workshop_ready_seen` -- Workshop Ready signal detected
- `multiplayer_reached` -- "Game Mode: Multiplayer" detected
- `lotheader_meta_evidence_found` -- ".lotheader" detected in log
- `lotheader_meta_paths_or_names` -- unique lotheader file names extracted
- `runtime_success_evidence_found` -- multi-signal composite
- `empty_client_map_folder_scan_decisive` -- false when runtime success overrides
- `visual_confirmation_required` -- true for DruMapBaseline

New classification (DruMapBaseline mode with runtime evidence):
```text
MAP7Q_DRUMAP_BASELINE_RUNTIME_SUCCESS
```

Fires when `VariantLabel = DruMapBaseline` AND runtime success evidence exists,
even if `map_folders_list_empty = true`.

Existing classifications preserved (backward compatible):
- `MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_FOUND`
- `MAP7P_DRUMAP_BASELINE_MAP_FOLDER_SCAN_EMPTY`
- `MAP7F_VARIANT_*_MAP_FOLDER_SCAN_EMPTY`

Schema bumped: v0.3 -> v0.4.

---

## 7. Claim boundary

```text
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
LOAD_TEST_NOT_PERFORMED
NO_BINARY_WRITER_CHANGE
NO_PZ_ASSETS_OUTSIDE_LOCAL
NO_FORBIDDEN_PATH_WRITES
```

Dru_map runtime success is NOT a PZMapForge playable export claim.
It is a reference data point for the runtime activation diagnostic.
PZMapForge candidate has not reached lotheader/meta load evidence.
No playable PZMapForge export is claimed from this task.
