# Primitive Classification

`scripts/classify-primitives.ps1` reads `regions.json` and maps each semantic
region to a higher-level map planning primitive.

---

## Why primitives

Semantic regions (grass, road, sidewalk, etc.) are low-level facts about
individual pixels. Primitives are higher-level planning concepts useful for
map design decisions: where can a player walk, where is a building, where
does the player spawn. Primitives collapse the low-level kinds into a smaller
set of actionable planning categories without losing the source detail.

---

## Kind-to-primitive mapping

| Semantic kind | Primitive type | Planning role |
|---|---|---|
| grass | ground_region | open ground or background area |
| road | road_region | driveable surface |
| sidewalk | sidewalk_region | pedestrian path |
| row_house | building_footprint | structure footprint |
| depanneur | building_footprint | structure footprint |
| garage | building_footprint | structure footprint |
| industrial_yard | yard_region | open industrial or service yard |
| landmark | landmark_marker | navigation reference point |
| spawn | spawn_marker | player spawn location |

Multiple semantic kinds can map to the same primitive type. The source kind
and code are preserved in the output for traceability.

---

## Output fields

### primitives[]

| Field | Type | Description |
|---|---|---|
| `primitive_id` | integer | 1-based index in sort order |
| `primitive_type` | string | Planning primitive type |
| `source_region_id` | integer | region_id from regions.json |
| `kind` | string | Source semantic kind |
| `code` | string | Single-character kind code |
| `pixel_count` | integer | Region size in pixels |
| `bounds.x` | integer | Leftmost column |
| `bounds.y` | integer | Topmost row |
| `bounds.width` | integer | Bounding box width |
| `bounds.height` | integer | Bounding box height |
| `centroid.x` | number | Mean x, rounded to 2 decimals |
| `centroid.y` | number | Mean y, rounded to 2 decimals |
| `planning_role` | string | Human-readable planning purpose |

### summary_by_primitive_type[]

| Field | Type | Description |
|---|---|---|
| `primitive_type` | string | Primitive type |
| `region_count` | integer | Number of primitives of this type |
| `total_pixels` | integer | Sum of pixel_count for this type |
| `largest_region_pixels` | integer | Max pixel_count for this type |

---

## Deterministic sort order

Primitives are sorted by:
1. `primitive_type` — ascending alphabetical
2. `pixel_count` — descending (largest first within type)
3. `bounds.y` — ascending (topmost first)
4. `bounds.x` — ascending (leftmost first)
5. `source_region_id` — ascending (BFS discovery order, tiebreaker)

After sorting, sequential `primitive_id` values (1, 2, 3, ...) are assigned.

---

## How to run

```powershell
powershell -ExecutionPolicy Bypass -File "scripts\classify-primitives.ps1"
```

Test (runs classification if needed, then validates):
```powershell
powershell -ExecutionPolicy Bypass -File "scripts\test-primitive-classification.ps1"
```

---

## Claim boundary

Primitive classification is a planning artifact only.
It does not produce a playable Project Zomboid map.
No lotpack, lotheader, or bin files are generated.
No Project Zomboid assets are copied or referenced.
`media/maps` is not touched.
Outputs remain under `.local/mapforge/` and are gitignored.
