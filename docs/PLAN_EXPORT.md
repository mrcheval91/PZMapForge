# Plan Export

`PlanningArtifactWriter.Write()` and the `plan-export` CLI command export the
`PlanningRuleResult` to deterministic local artifacts.

The `full-pipeline` command chains the entire image-to-planning workflow
(image-export → ParsedCellLoader → RegionExtractor → PrimitiveClassifier →
PlanningRuleEngine → PlanningArtifactWriter) in a single CLI invocation.

---

## Claim boundary

`claim_boundary = planning_artifact_only_not_pz_load_tested`

Planning recommendations are artifact-only. No lotpack, no playable export,
no PZ asset reference.

---

## Outputs

Both files are written to the `--output` directory (default: `.local/mapforge/`):

| File | Purpose |
|---|---|
| `plan-recommendations.json` | Machine-readable JSON artifact (schema v0.1) |
| `plan-report.md` | Human-readable grouped markdown report |

---

## JSON structure

```json
{
  "schema": "pzmapforge.plan-recommendations.v0.1",
  "claim_boundary": "planning_artifact_only_not_pz_load_tested",
  "generated_at_utc": "2026-01-01T00:00:00Z",
  "source": {
    "parsed_cell_path": ".local/mapforge/parsed-cell.json",
    "generated_by": "PZMapForge.Cli plan-export"
  },
  "width": 300,
  "height": 300,
  "primitive_count": 20,
  "recommendation_count": 20,
  "warning_count": 0,
  "thresholds_used": {
    "tiny_building_pixel_threshold": 9,
    "large_ground_pixel_threshold": 50000
  },
  "recommendations": [...],
  "summary": {
    "total_pixels": 90000,
    "primitive_count": 20,
    "recommendation_count": 20,
    "warning_count": 0,
    "counts_by_recommendation_type": {...},
    "counts_by_severity": {...}
  }
}
```

Each recommendation entry:

| Field | Notes |
|---|---|
| `recommendation_id` | 1-based sequential index in sort order |
| `recommendation_type` | snake_case type string |
| `severity` | "warning" or "info" |
| `planning_role` | human-readable purpose |
| `source_primitive_id` | 0 for global rules (missing_spawn_marker etc.) |
| `primitive_type` | source primitive type string (empty for global) |
| `pixel_count` | source primitive pixel count (0 for global) |
| `bounds` | bounding box or null for global |
| `message` | concatenation of type and role |

---

## Determinism

For the same input, two runs produce identical JSON when `generated_at_utc` is
held constant. The sort order (severity, type, pixel_count DESC, y, x,
primitive_id) is fully deterministic.

To test determinism without mocking the clock, pass `overrideGeneratedAt`
to `PlanningArtifactWriter.Write()`.

---

## Output path safety

The CLI refuses to write outside a `.local/` directory unless `--output`
explicitly points to an allowed path. This prevents accidentally writing
artifacts to source directories or media/maps.

---

## How to run

```
dotnet run --project src/PZMapForge.Cli -- plan-export --path .local/mapforge/parsed-cell.json
```

With explicit output directory:

```
dotnet run --project src/PZMapForge.Cli -- plan-export --path .local/mapforge/parsed-cell.json --output .local/mapforge
```
