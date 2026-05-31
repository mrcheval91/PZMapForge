# Planning Rules

`PlanningRuleEngine.Evaluate(PrimitiveClassificationResult)` (or the overload
with `PlanningRuleOptions`) turns classified planning primitives into deterministic
planning recommendations.

---

## Claim boundary

Planning recommendations are artifact-only.
`claim_boundary = planning_artifact_only_not_pz_load_tested`.
No lotpack, no playable export, no PZ asset reference.

---

## Rules

### Per-primitive rules

| Primitive type | Recommendation type | Severity | Notes |
|---|---|---|---|
| ground_region | open_ground_area | info | background terrain |
| ground_region | large_open_ground_area | info | if pixel_count > 50000 |
| road_region | road_corridor_candidate | info | traffic spine |
| sidewalk_region | sidewalk_band_candidate | info | pedestrian buffer |
| building_footprint | building_footprint_candidate | info | structure placement |
| building_footprint | tiny_building_candidate | warning | if pixel_count <= 9 |
| yard_region | yard_candidate | info | industrial / service area |
| landmark_marker | landmark_marker | info | orientation marker |
| spawn_marker | spawn_marker | info | spawn point |

### Global rules

| Condition | Recommendation type | Severity |
|---|---|---|
| No spawn_marker primitive in plan | missing_spawn_marker | warning |

---

## Thresholds

| Threshold | Value | Trigger |
|---|---|---|
| `TinyBuildingPixelThreshold` | 9 pixels | building_footprint with pixel_count <= threshold gets tiny_building_candidate warning |
| `LargeGroundPixelThreshold` | 50,000 pixels | ground_region with pixel_count > threshold gets large_open_ground_area info |

Thresholds are configurable via `PlanningRuleOptions`:

```csharp
var opts = new PlanningRuleOptions(tinyBuildingPixelThreshold: 0, largeGroundPixelThreshold: 100_000);
var result = PlanningRuleEngine.Evaluate(primitives, opts);
```

Both values must be >= 0; negative values throw `ArgumentOutOfRangeException`.
The no-options overload uses `PlanningRuleOptions.Default` (9 and 50000).

The CLI exposes the same flags for `plan-check` and `plan-export`:

```
plan-check  --path <path> [--tiny-threshold <int>] [--large-threshold <int>]
plan-export --path <path> [--output <dir>] [--tiny-threshold <int>] [--large-threshold <int>]
```

Both commands print the thresholds used in their output. Non-integer values and
negative values exit with code 1 and a clear error message.

---

## Sort order

Recommendations are sorted deterministically:

1. severity (Warning before Info)
2. recommendation_type ASC (alphabetical string)
3. pixel_count DESC
4. bounds.y ASC
5. bounds.x ASC
6. source_primitive_id ASC

---

## Output model

`PlanningRuleResult` contains:
- `ClaimBoundary` — always `planning_artifact_only_not_pz_load_tested`
- `RecommendationCount` — total recommendations
- `Recommendations` — sorted list of `PlanningRecommendation`
- `Summary` — `PlanningSummary` with counts by type and severity

Each `PlanningRecommendation` carries:
- `SourcePrimitiveId` — primitive that triggered it (0 for global rules)
- `RecommendationTypeStr` — snake_case type identifier
- `Severity` — Warning or Info
- `PlanningRole` — human-readable planning purpose
- `PixelCount` — pixel count of the source primitive (0 for global rules)
- `Bounds` — bounding box (null for global rules)

---

## How to run

```
dotnet run --project src/PZMapForge.Cli -- plan-check --path .local/mapforge/parsed-cell.json
```

Prints: dimensions, primitive count, recommendation count, warning count, status.
Exits 0 on success, 1 on invalid input or classification failure.
