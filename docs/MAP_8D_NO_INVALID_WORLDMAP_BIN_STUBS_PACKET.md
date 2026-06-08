# MAP-8D: No Invalid Worldmap Bin Stubs Probe Packet

```text
MAP8D_NO_INVALID_WORLDMAP_BIN_STUBS_PROBE_STAGED
MAP8D_VERSION_42_MEDIA_PATH_RETAINED
INVALID_WORLDMAP_BIN_STUBS_REMOVED
STREETS_XML_BIN_REMOVED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
```

---

## 1. MAP-8B finding that motivates this probe

MAP-8B established the partial registration breakthrough:

- The version-scoped `42\media\maps\<MapId>\` path is visible to the worldmap
  / city-selection asset loader.
- The client attempted to read `worldmap.xml.bin` and `worldmap-forest.xml.bin`
  from that path.
- Both reads failed with:
  `java.io.IOException: invalid format (magic doesn't match)`
- IsoMetaGrid map folder list still empty.

The generated ASCII-marker stubs (from MAP-7Y, carried into MAP-8B) do not
satisfy the worldmap binary format. Their presence caused an active error read.

---

## 2. Hypothesis

The invalid `.bin` stubs may be actively interfering with the worldmap loader
or city-selection logic. Removing them may:

1. Allow the worldmap loader to fall back to the uncompiled XML/PNG sources cleanly.
2. Remove the invalid magic failure from the log, making other signals clearer.
3. Have no effect (fallback forest persists with or without stubs).

---

## 3. Streets.xml.bin removal decision

`streets.xml.bin` was NOT logged as read in the MAP-8B runtime. Only
`worldmap.xml.bin` and `worldmap-forest.xml.bin` were attempted.

However: `streets.xml.bin` is also a generated ASCII-marker stub with no
valid binary magic. It belongs to the same category of generated invalid stubs.
The principle of removing all invalid generated `.bin` stubs applies:

```text
STREETS_XML_BIN_REMOVED=true
Rationale: generated ASCII-marker stub, no valid binary magic.
Not proven benign. Prefer clean probe.
```

If streets.xml.bin is required for IsoMetaGrid map registration, the map
folder scan would remain empty regardless (as seen in MAP-8B). Removing it
produces a cleaner diagnostic.

---

## 4. Staged package layout

```text
staged-workshop-no-worldmap-bin/<MapId>/     <- mod root (= Contents\mods\<MapId>\ on Workshop)
  mod.info
  poster.png
  42/
    mod.info
    media/
      maps/
        <MapId>/                              <- version-scoped map folder
          map.info                 RETAINED
          spawnpoints.lua          RETAINED
          objects.lua              RETAINED
          thumb.png                RETAINED
          worldmap.xml             RETAINED
          worldmap-forest.xml      RETAINED
          worldmap.png             RETAINED
          35_27.lotheader          RETAINED
          world_35_27.lotpack      RETAINED
          chunkdata_35_27.bin      RETAINED
          MAP8D_NO_WORLDMAP_BIN_STUBS.txt   NEW
          [worldmap.xml.bin]       REMOVED
          [worldmap-forest.xml.bin] REMOVED
          [streets.xml.bin]        REMOVED
```

---

## 5. What is NOT included

The following files from MAP-8B are intentionally absent:

- `worldmap.xml.bin` — had invalid magic, now removed.
- `worldmap-forest.xml.bin` — had invalid magic, now removed.
- `streets.xml.bin` — ASCII-marker stub, no valid binary magic; removed.

The root media path (present in MAP-8B for comparison) is NOT included in
MAP-8D. Only the version-scoped `42\media\maps\<MapId>\` path is staged.

---

## 6. Expected test outcomes

### Outcome 1: Worldmap loader falls back cleanly (no binary error)

No `invalid format (magic doesn't match)` in log.
Custom city selector still visible. Player connects.
IsoMetaGrid scan result still the key signal.

If IsoMetaGrid scan becomes non-empty: map folder has mounted.
Binary writer gate opens.

### Outcome 2: Same fallback forest, no new errors

Removing stubs has no effect on registration. IsoMetaGrid still ignores the
map folder. The discriminator is elsewhere (map.info fields, mod.info contract,
or version-scoped path discovery vs IsoMetaGrid vs worldmap loader).

### Outcome 3: New error referencing missing file

PZ expects a `.bin` file that is now absent. Error message provides a new signal.

---

## 7. Binary writer gate

```text
BINARY_WRITER_GATE_STILL_CLOSED

Gate opens when:
  expected_map_lotheader_meta_evidence_found=true
  OR: IsoMetaGrid map folder scan becomes non-empty for candidate
  OR: explicit lotheader parse error on candidate files

Removing worldmap .bin stubs is NOT a binary writer change.
35_27.lotheader / world_35_27.lotpack / chunkdata_35_27.bin are UNCHANGED.
```

---

## 8. Human upload and wire instructions

See the packet checklist:
`scripts/prepare-build42-map8d-no-invalid-worldmap-bin-packet.ps1`

All Workshop upload and server wiring steps are HUMAN-ONLY.
This script does NOT upload to Steam Workshop.
This script does NOT run Project Zomboid.

---

## 9. Claim boundary

```text
MAP8D_NO_INVALID_WORLDMAP_BIN_STUBS_PROBE_STAGED
MAP8D_VERSION_42_MEDIA_PATH_RETAINED
INVALID_WORLDMAP_BIN_STUBS_REMOVED
STREETS_XML_BIN_REMOVED
BINARY_WRITER_GATE_STILL_CLOSED
LOAD_TEST_NOT_PERFORMED
PUBLIC_PLAYABLE_CLAIM_ALLOWED=false
NO_BINARY_WRITER_CHANGES
NO_THIRD_PARTY_FILES_COPIED
NO_PZ_RUN_BY_SCRIPT
NO_AUTOMATIC_WORKSHOP_UPLOAD
```

Non-claims:
- No playable PZMapForge export claimed.
- Binary writer gate is still closed.
- Removing stubs does not constitute a binary format fix.
- Player spawn location is not confirmed as PZMapForge content.
