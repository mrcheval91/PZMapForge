# TMX Integrity Validation

`scripts/test-tmx-integrity.ps1` validates the structural integrity of the
generated `parsed-cell-basic.tmx` without opening TileZed.

---

## Why it exists

The TMX is generated deterministically by `image-mapforge.ps1` from the parsed
cell grid. Until now, correctness was only verified visually (opened in TileZed).
This validator proves the file is structurally sound by reading and decoding it
programmatically:

- The XML is well-formed.
- All required map, tileset, layer, and data attributes are present and correct.
- The base64+gzip layer payload decodes and decompresses without error.
- The decompressed payload contains exactly `width * height * 4` bytes.
- Reinterpreted as uint32 little-endian GIDs, the count is `width * height`.
- All GIDs are in the valid palette range 1..9 (no zero or out-of-range tiles).

This closes IMPLEMENTATION.md gap 2.

---

## TMX format summary

```xml
<map version="1.0" orientation="orthogonal"
     width="300" height="300" tilewidth="32" tileheight="32">
  <tileset firstgid="1" tilewidth="32" tileheight="32">
    <image source="parsed-cell-tiles.png" width="288" height="32"/>
  </tileset>
  <layer name="Ground" width="300" height="300">
    <data encoding="base64" compression="gzip">
      [base64-encoded gzip-compressed uint32 LE GIDs]
    </data>
  </layer>
</map>
```

---

## Payload encoding

1. Build a `uint32[]` of 90000 GID values (one per cell, row-major).
2. Copy to a `byte[]` using `System.Buffer.BlockCopy` (native LE on x86/x64 Windows).
3. Gzip-compress using `System.IO.Compression.GZipStream`.
4. Base64-encode using `[Convert]::ToBase64String`.

Decoding reverses this: base64 → gzip decompress → `BlockCopy` to `uint32[]`.

---

## Assertions (21 total)

| # | Assertion |
|---|---|
| 1 | TMX file exists |
| 2 | XML parses without error |
| 3 | map version == "1.0" |
| 4 | orientation == "orthogonal" |
| 5 | map width == 300 |
| 6 | map height == 300 |
| 7 | tilewidth == 32 |
| 8 | tileheight == 32 |
| 9 | tileset firstgid == 1 |
| 10 | tileset image source == "parsed-cell-tiles.png" |
| 11 | layer named "Ground" exists |
| 12 | layer width == 300 |
| 13 | layer height == 300 |
| 14 | data encoding == "base64" |
| 15 | data compression == "gzip" |
| 16 | base64 decodes without error |
| 17 | gzip decompresses without error |
| 18 | decompressed byte length == 360000 |
| 19 | uint32 GID count == 90000 |
| 20 | all GIDs >= 1 (no zero GIDs) |
| 21 | all GIDs <= 9 (valid palette range) |

---

## How to run

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\test-tmx-integrity.ps1"
```

If `parsed-cell-basic.tmx` is missing, the script runs `image-mapforge.ps1`
directly (not `validate.ps1`) to avoid a recursion loop.

---

## Claim boundary

TMX integrity validation is a structural check of a planning artifact.
The TMX passes this validator but is NOT a Project Zomboid load-tested export.
No lotpack, lotheader, or bin files are produced or validated here.
`media/maps` is not touched.
