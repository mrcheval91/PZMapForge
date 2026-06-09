# MAP-8L Runtime Result

```text
MAP8L_WORLDMAP_XML_FAILED_TO_MOUNT
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
```

---

## 1. Context

MAP-8L deployed a substantial PZMapForge-owned worldmap.xml (1915 bytes, 44 lines)
into Workshop item 3740642200 to test whether IsoMetaGrid parent mount requires
a substantial worldmap.xml rather than a 52-byte skeletal stub.

Downloaded Workshop verification confirmed the MAP-8L worldmap.xml was present:

```text
XML declaration present.
generator="PZMapForge-map8l"
describes worldX=35 worldY=27
no Project Russia content.
```

---

## 2. Server config

```text
Server name: PZMF_B42_MAP8L_WORLDMAP_XML_001
WorkshopItems=3740642200
Mods=pzmapforge_build42_candidate_v4_001
Map=pzmapforge_build42_candidate_v4_001;PZMapForge;Muldraugh, KY
```

---

## 3. Runtime result

```text
Server classification: MAP7F_MAP8L_WORLDMAP_XML_SERVER_MAP_FOLDER_SCAN_EMPTY
Client classification: MAP7F_MAP8L_WORLDMAP_XML_CLIENT_MAP_FOLDER_SCAN_EMPTY
```

IsoMetaGrid log:

```text
IsoMetaGrid.Create: begin scanning directories
Looking in these map folders: <End of map-folders list>
```

WorldMapDataAssetManager failed to load:

```text
common\media\maps\pzmapforge_build42_candidate_v4_001\worldmap.xml
common\media\maps\PZMapForge\worldmap.xml
```

Player evidence:

```text
Player fully connected at: 10746,8288,0
Player disconnected at:    10777,8287,0
```

---

## 4. What is resolved

```text
map8l_worldmap_xml_deployed             = true
downloaded_workshop_worldmap_xml_was_map8l = true
server_config_correct                   = true
candidate_loaded                        = true
player_spawn_coordinate                 = 10746,8288,0
spawn_coordinate_matches_35_27          = true
```

---

## 5. What failed

```text
iso_meta_grid_map_folder_list_empty     = true
parent_folder_listed_by_isometagrid     = false
worldmap_xml_asset_failed_to_load       = true
child_worldmap_xml_failed_to_load       = true
parent_worldmap_xml_failed_to_load      = true
worldmap_xml_bin_present                = false
invalid_magic_logged                    = false
lotheader_parse_attempt_logged          = false
generated_cell_content_mounted          = false
```

IsoMetaGrid still does not mount PZMapForge parent folder.
Substantial text worldmap.xml alone does not unlock IsoMetaGrid mount.

---

## 6. Interpretation

```text
map.info unlikely main blocker:
  MAP-8K found no lots field and fixed2x=true on both candidate and reference parent.

streets.xml.bin unlikely main blocker:
  MAP-8K found streets.xml.bin absent on both candidate and reference (Project Russia) parent.

worldmap.xml text unlikely main blocker:
  MAP-8L proved substantial worldmap.xml does not unlock IsoMetaGrid mount.

Strongest remaining hypothesis:
  worldmap.xml.bin sidecar required for IsoMetaGrid parent folder mount.
  MAP-8K confirmed reference_worldmap_xml_bin_present=true,
  candidate_worldmap_xml_bin_present=false.

This is a hypothesis. worldmap.xml.bin is not yet proven required.
The task must not claim Build 42 requires worldmap.xml.bin without evidence.
```

---

## 7. Next branch

```text
next_branch=worldmap_xml_bin_binary_format_investigation
```

Staged in MAP-8M investigation plan:
1. Presence/shape inventory of worldmap.xml.bin in candidate vs reference.
2. Format research gate (binary reading requires explicit operator approval).
3. Binary writer gate opens only when IsoMetaGrid logs a parse attempt against PZMapForge.

---

## 8. Claim boundary

```text
MAP8L_WORLDMAP_XML_FAILED_TO_MOUNT
BINARY_WRITER_GATE_STILL_CLOSED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_PZ_RUN_BY_CLAUDE
NO_WORKSHOP_UPLOAD_BY_CLAUDE
NO_THIRD_PARTY_FILES_COPIED
```

Non-claims:
- No playable PZMapForge export claimed.
- Binary writer gate remains closed.
- worldmap.xml.bin is the leading hypothesis, not proven requirement.
- Spawned in fallback/vanilla terrain. Cell content not confirmed mounted.
