# MAP-7T: Workshop K002 Runtime Payload Comparison

```text
MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED
K002_WORKSHOP_ITEM_INSTALLED_READY
K002_MOD_LOADED_NO_EXPECTED_MAP_EVIDENCE
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. Summary

MAP-7T records the K002 real Workshop activation test result and adds a
local-only runtime payload comparison tool to compare the actual downloaded
PZMapForge Workshop payload against the Dru_map reference payload.

K002 confirms the Workshop item now downloads, reaches Installed/Ready, and
loads the PZMapForge mod — but it still did not produce expected-map
lotheader/meta evidence or a visible custom PZMapForge world.

---

## 2. K002 facts

### 2.1 Wiring

```text
Workshop ID:  3740642200
Server:       PZMF_B42_WS_CANDIDATE_K_002
Mods:         Mods=pzmapforge_build42_candidate_v4_001
WorkshopItems: WorkshopItems=3740642200
Map:          Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY
```

### 2.2 Analyzer result

```text
classification=MAP7F_VARIANT_W_S_UPLOAD_K002_MAP_FOLDER_SCAN_EMPTY
candidate_loaded=True
player_data_received=True
game_loading_completed=True
map_folders_list_empty=True
spawn_building_warning=True
public_playable_claim_allowed=false
```

### 2.3 Evidence table

| Signal | Result |
|---|---|
| Workshop 3740642200 downloaded | YES |
| Workshop item: Installed | YES |
| Workshop item: Ready | YES |
| Workshop 3740642200 path reached | YES |
| Server loaded pzmapforge_build42_candidate_v4_001 | YES |
| Client Workshop 3740642200 Subscribed/Installed | YES |
| Client item state: Ready | YES |
| Client loaded pzmapforge_build42_candidate_v4_001 | YES |
| IsoMetaGrid map folder scan | EMPTY |
| Player data received | YES |
| game loading took | 43 seconds |
| exited GameLoadingState | YES |
| Game Mode: Multiplayer | YES |
| Expected-map lotheader/meta evidence | NO |
| Visible custom PZMapForge world | NO (fallback forest) |

### 2.4 Workshop install path observed

```text
D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid\steamapps\workshop\content\108600\3740642200
```

---

## 3. What K002 proves

- The previous broken-upload failure is fixed.
- Real PZMapForge Workshop activation now reaches Installed/Ready state.
- The PZMapForge mod loads at the mod registration level.
- The Workshop activation mechanism itself is working.

---

## 4. What K002 does not prove

- The PZMapForge candidate map folder is NOT mounted by IsoMetaGrid.
- No expected-map lotheader/meta evidence appeared.
- No built custom PZMapForge world was visible.
- The candidate is NOT ready for public playable claim.

---

## 5. Why empty map-folder scan is still failure evidence here

In MAP-7Q we established that empty printed client map-folder scan is not
decisive by itself for Workshop-activated mods (Dru_map succeeded with an
empty scan).

However, for K002, the empty scan is still failure evidence because:
1. No expected-map lotheader/meta evidence appeared (candidate lotheader files
   not referenced in the log).
2. No built custom world was visible (fallback forest, not PZMapForge content).
3. Dru_map succeeded because it had all three: Workshop Installed/Ready +
   expected-map lotheader evidence + built world.

K002 has the first signal (Workshop Installed/Ready) but lacks the second
and third. The empty scan is corroborating failure evidence in context.

---

## 6. Binary writer gate

```text
BINARY_WRITER_GATE_STILL_CLOSED

Gate opens when:
  expected_map_lotheader_meta_evidence_found=true
  OR: explicit binary format failure on candidate lotheader

Do not mutate LOTH/LOTP/chunkdata until this gate is cleared.
No binary writer changes from this task.
```

---

## 7. Next diagnostic: runtime payload comparison

The question becomes: what is different between the actual downloaded
PZMapForge Workshop payload (3740642200) and the Dru_map Workshop payload
(3355966216) at the file system level?

The runtime payload comparison tool (`scripts/inspect-build42-workshop-runtime-payload.ps1`)
reads both explicit operator-provided roots and compares:
- Top-level layout entries
- mod.info locations (root / 42/ / common/ / mods/<id> / Contents/mods/<id>)
- common/mod.info presence/absence
- media/maps/<mapid> path depth and location
- map.info fields (lots=, zoomX/Y/S)
- spawnpoints.lua / objects.lua / worldmap.xml presence
- lotheader / lotpack / chunkdata file names and sizes
- no-BOM status of text files

This comparison identifies the structural discriminator between a working
Workshop map mod (Dru_map) and a non-working one (PZMapForge K002).

---

## 8. Claim boundary

```text
MAP7T_K002_WORKSHOP_ACTIVATION_RECORDED
K002_WORKSHOP_ITEM_INSTALLED_READY
K002_MOD_LOADED_NO_EXPECTED_MAP_EVIDENCE
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

No playable PZMapForge export is claimed from this task.
No binary writer changes are made.
