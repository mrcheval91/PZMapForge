# MAP-38M: Chunkdata Structural Analysis (Partial — Semantics Still Undecoded)

Date: 2026-07-10
Status: PARTIAL — records observations, does not claim a decoded format

Byte-level analysis of real vanilla `chunkdata_X_Y.bin` files
(`D:\...\ProjectZomboid\media\maps\Muldraugh, KY\`), run via
`.local/deadmtl-authoring/chunkdata-analysis/analyze-chunkdata.ps1`
(not committed — ad hoc analysis script, not product code).

## Observations

| Cell | Total size | Body size | Byte values present |
|---|---|---|---|
| 34_26 | 1026 | 1024 | 0x00 only (all-zero) |
| 34_27 | 1026 | 1024 | 0x00 only (all-zero) |
| 35_26 | 3010 | 3008 | 0x00, 0x01, 0x02, 0x03, 0x08 |
| 35_27 | 3138 | 3136 | 0x00, 0x01, 0x02, 0x03, 0x08 |
| 35_28 | 2306 | 2304 | 0x00, 0x01, 0x02, 0x03, 0x08 (no `00000000 02000000` marker at all) |
| 36_27 | 3906 | 3904 | 0x00, 0x01, 0x02, 0x03, 0x08 (marker occurs once, not repeating) |

Every real chunkdata file seen — across simple rural cells and complex
cells — uses only 5 distinct byte values: `0x00, 0x01, 0x02, 0x03, 0x08`.
This strongly suggests chunkdata encodes a small enumerated/categorical
value per some spatial unit, not tile indices (which would need a much
wider byte range, as lotpack's tile_index fields do).

Cells 35_26 and 35_27 show a recognizable 96-byte repeating unit (the byte
sequence `00 00 00 00 02 00 00 00` recurs at consistent 96-byte intervals),
but the body size does not divide evenly by 96 (3008/96=31.33,
3136/96=32.67), so the repeat is not a uniform tiling of the whole body —
something else (a header, a trailing partial record, or a different
structure for part of the file) is present alongside the repeating region.
Cell 35_28 shows no such marker at all despite a similar byte-value
palette, and 36_27 shows the marker exactly once (not repeating), meaning
whatever this structure is, it is not present/relevant in every cell the
same way.

## What this does NOT establish

- No confirmed field-level decode (no record boundaries, no meaning
  assigned to 0x00/0x01/0x02/0x03/0x08).
- No confirmed relationship between this byte pattern and the lotpack's own
  Type-A/Type-B chunk encoding (chunkdata does not appear to have a
  per-chunk offset table the way lotpack does — 1024 entries x 8 bytes
  would be 8192 bytes alone, larger than any chunkdata file observed, so
  chunkdata's addressing scheme, if any, is different from lotpack's).

## Why this wasn't pursued further this session

MAP-38L already confirmed all-zero chunkdata is sufficient for the current
visual test (ground-tile rendering via lotpack). Decoding chunkdata's exact
semantics is real, open-ended reverse-engineering work with no external
reference material found so far (unlike the coordinate-grid fix, which had
a public wiki source). Session time was redirected toward objects.lua zone
placement (MAP-38N), which had a clear, directly-inspectable real reference
(vanilla Muldraugh's own map-wide `objects.lua`) and was judged more
tractable in the time available.

## If resumed

Next concrete step would be: extract the full body of a mid-complexity
cell (e.g. 35_26, 3008 bytes) as raw bytes, segment strictly on the 96-byte
boundaes where they exist, and manually classify each segment's internal
structure (looks like `[00000000][run-length-or-count][N x (some field)]`
similar in spirit to lotpack's Type-A/B split, but with a different width).
Cross-reference against a build with vegetation zones already correctly
placed (see MAP-38N) to see whether chunkdata correlates with
vegetation/object density rather than ground tile content.
