# MAP-7W: Runtime Map Registration / Map Folder Mounting Contract

```text
MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED
BINARY_FORMAT_INVESTIGATION_PAUSED
RUNTIME_MAP_REGISTRATION_IS_ACTIVE_BRANCH
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. MAP-7V recap

MAP-7V recorded K004 (coordinate-aligned binaries) and K006 (zero binaries).
Both produced the same fallback-forest outcome. Binary presence is not the
discriminator. Binary format investigation is paused.

The active blocker is runtime map registration / map folder mounting:
- Workshop activation works.
- Mod loading works.
- spawnpoints.lua works and spawn coordinates are honored.
- IsoMetaGrid is not mounting the PZMapForge map folder.

---

## 2. Why binary investigation is paused

K006 proved that zero PZMapForge binary files produces the same outcome as
full binary presence. The fallback-forest result and empty map-folder scan
were never binary evidence — they are map-registration evidence.

No explicit binary parse error (EOFException on candidate lotheader) has
been observed. Without this signal, investigating binary format is misdirected.

---

## 3. Known-good reference

```text
Mod:           Dru_map
Workshop ID:   3355966216
Map folder:    common/media/maps/Dru_map/
Result:        Built world visible. Candidate-specific lotheader evidence.
```

---

## 4. Candidate

```text
Mod:           pzmapforge_build42_candidate_v4_001
Workshop ID:   3740642200
Map folder:    common/media/maps/pzmapforge_build42_candidate_v4_001/
Result:        Fallback forest. No expected-map lotheader evidence.
```

---

## 5. What MAP-7T/7U established

The mod-root layout match was confirmed (MAP-7U):
- root mod.info: YES / YES
- 42/mod.info: YES / YES
- common/mod.info: no / no
- common/media/maps: YES / YES
- map.info: YES / YES
- lots=NONE: YES / YES
- zoomX/Y fields: YES / YES
- spawnpoints.lua: YES / YES
- objects.lua: YES / YES
- worldmap.xml: YES / YES
- worldmap-forest.xml: YES / YES
- BOM violations: 0 / 0

However: the previous comparison was field-presence-only. It did not fully
inventory every file in the map folder, compare exact key/value content of
mod.info and map.info, or check for additional map-registration files.

---

## 6. Hypotheses for the registration/mounting gap

The following are hypotheses only. None is proven. The inspector should
determine which, if any, are supported by the local reference file set.

**H1: Missing map-folder registration or index file**
Map folders in known-working mods may include a `map.bin` or similar index file
that PZMapForge has never generated. This file may be required for IsoMetaGrid
to recognize and mount the map folder.
Status: Unconfirmed hypothesis. Inspector must compare file sets.

**H2: mod.info field/value mismatch beyond presence**
Field-presence comparison matched. But exact values may differ. For example,
the `id=` value or `modId=` field might require exact casing or content.
Status: Unconfirmed. Inspector must compare key/value content.

**H3: map.info field/value mismatch beyond presence**
zoom values were aligned in K004 but the comparison was manual. The inspector
should compare all map.info key/value pairs against the reference.
Status: Unconfirmed. Inspector must compare key/value content.

**H4: Workshop runtime path differs from copied reference root**
The Dru_map reference used in comparisons was a local copy. The actual
Workshop-downloaded Dru_map payload path may differ in structure from the copy.
Status: Unconfirmed. Would require operator to supply the actual Workshop root.

**H5: Server-side IsoMetaGrid log has more evidence than client-side**
Client logs showed empty map-folder scan. Server logs may show the actual
IsoMetaGrid mount attempt and any registration errors.
Status: Server logs not yet captured.

**H6: server.ini Map= syntax requirement**
Map=pzmapforge_build42_candidate_v4_001;Muldraugh, KY may not be sufficient
if PZ Build 42 requires a registered map folder value that differs from the
Workshop mod folder name.
Status: Unconfirmed.

**H7: spawnregions.lua server-side registration**
The server _spawnregions.lua may need to reference the mod's spawn regions.
This was the blocker in MAP-6P/6Q. May still be relevant even with Workshop
activation.
Status: Unconfirmed for Workshop-activated mods.

---

## 7. Inspector

New tool: `scripts/inspect-build42-map-registration-contract.ps1`

Compares:
- Exact file set under common/media/maps/<MapId>/
- All mod.info and map.info key/value pairs
- spawnpoints.lua style and coordinates
- Binary file inventory
- BOM/ASCII status
- Log evidence (if logs roots supplied)

Outputs:
- `map-registration-contract.json`
- `map-registration-contract.md`

Key output fields:
- `exact_file_set_match` — whether file sets are identical
- `reference_files_missing_in_candidate_count` — files to investigate
- `reference_has_map_bin` / `candidate_has_map_bin` — H1 discriminator
- `map_bin_discriminator` — true if only reference has map.bin
- `map_info_value_differences_count` — H3 discriminator
- `mod_info_value_differences_count` — H2 discriminator
- `runtime_mount_discriminator_found` — whether any clear gap was found

---

## 8. Claim boundary

```text
MAP7W_RUNTIME_MAP_REGISTRATION_INSPECTOR_ADDED
BINARY_FORMAT_INVESTIGATION_PAUSED
RUNTIME_MAP_REGISTRATION_IS_ACTIVE_BRANCH
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
- map.bin or other missing file is NOT declared as the cause until the
  inspector proves it from the local Dru_map reference.
