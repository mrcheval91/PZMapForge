# Region Extraction

`scripts/extract-regions.ps1` reads `parsed-cell.json` and finds every
contiguous region of the same semantic kind using 4-neighbor BFS flood-fill.

---

## What it does

1. Reads `.local/mapforge/parsed-cell.json` (runs `validate.ps1` if missing).
2. Builds a flat `char[]` code grid from the `rows` field.
3. Runs BFS flood-fill with 4-neighbor connectivity (up, down, left, right).
   Diagonal pixels are NOT connected.
4. For each region computes: kind, code, pixel_count, bounding box, centroid.
5. Sorts regions deterministically (see below).
6. Writes `.local/mapforge/regions.json` and `.local/mapforge/regions-report.md`.

---

## 4-neighbor connectivity

Two pixels are in the same region if they share the same semantic kind and are
adjacent horizontally or vertically. Diagonal adjacency does not connect regions.

```
  [ ]
[x][x][x]
  [ ]
```

The four checked neighbors for pixel (y, x): (y-1,x), (y+1,x), (y,x-1), (y,x+1).

---

## Sort order

Regions are sorted deterministically by:
1. `kind` — ascending alphabetical
2. `pixel_count` — descending (largest region first within a kind)
3. `bounds.y` — ascending (topmost first)
4. `bounds.x` — ascending (leftmost first)
5. `region_id` (original BFS order) — ascending as final tiebreaker

After sorting, sequential `region_id` values (1, 2, 3, ...) are assigned.

---

## Output fields

### regions[]

| Field | Type | Description |
|---|---|---|
| `region_id` | integer | 1-based index in sort order |
| `kind` | string | Semantic kind (e.g., grass, road) |
| `code` | string | Single-character code from palette |
| `pixel_count` | integer | Number of pixels in the region |
| `bounds.x` | integer | Leftmost column of bounding box |
| `bounds.y` | integer | Topmost row of bounding box |
| `bounds.width` | integer | Width of bounding box in pixels |
| `bounds.height` | integer | Height of bounding box in pixels |
| `centroid.x` | number | Mean x position, rounded to 2 decimals |
| `centroid.y` | number | Mean y position, rounded to 2 decimals |

### summary_by_kind[]

| Field | Type | Description |
|---|---|---|
| `kind` | string | Semantic kind |
| `code` | string | Single-character code |
| `region_count` | integer | Number of separate contiguous regions |
| `total_pixels` | integer | Sum of pixel_count for all regions of this kind |
| `largest_region_pixels` | integer | pixel_count of the largest region of this kind |

---

## Implementation note

PS5.1's `[int]` cast uses banker's rounding, not truncation. The expression
`[int]($cur / $W)` is WRONG for flat-index to row decomposition because
`[int](0.6) = 1` instead of 0.

The correct decomposition uses integer modulo:
```powershell
$cx = $cur % $W
$cy = ($cur - $cx) / $W   # exact because ($cur - $cx) is divisible by $W
```

---

## How to run

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\extract-regions.ps1"
```

Test (runs extraction if needed, then validates):
```powershell
powershell -ExecutionPolicy Bypass -File "scripts\test-region-extraction.ps1"
```

---

## Claim boundary

Region extraction is a planning artifact only.
It does not produce a playable Project Zomboid map.
No lotpack, lotheader, or bin files are generated.
`media/maps` is not touched.
Outputs remain under `.local/mapforge/` and are gitignored.
