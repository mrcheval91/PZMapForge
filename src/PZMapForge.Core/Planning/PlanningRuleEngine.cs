using PZMapForge.Core.Primitives;
using PZMapForge.Core.Regions;

namespace PZMapForge.Core.Planning;

/// <summary>
/// Produces deterministic planning recommendations from a classified primitive set.
///
/// Sort order: severity (Warning before Info), recommendation_type ASC,
///             pixel_count DESC, bounds.y ASC, bounds.x ASC, source_primitive_id ASC.
///
/// Default thresholds (see PlanningRuleOptions.Default):
///   tiny_building_candidate  : pixel_count &lt;= 9  (3x3 area or smaller)
///   large_open_ground_area   : pixel_count &gt; 50000
///
/// Global rules:
///   missing_spawn_marker warning is emitted when no spawn_marker primitive exists.
///
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public static class PlanningRuleEngine
{
    /// <summary>Evaluates with default thresholds (PlanningRuleOptions.Default).</summary>
    public static PlanningRuleResult Evaluate(PrimitiveClassificationResult primitives) =>
        Evaluate(primitives, PlanningRuleOptions.Default);

    /// <summary>Evaluates with caller-supplied threshold options.</summary>
    public static PlanningRuleResult Evaluate(
        PrimitiveClassificationResult primitives,
        PlanningRuleOptions           options)
    {
        var raw      = new List<PlanningRecommendation>(primitives.PrimitiveCount * 2);
        var hasSpawn = false;

        foreach (var p in primitives.Primitives)
        {
            switch (p.PrimitiveType)
            {
                case PlanningPrimitiveType.RoadRegion:
                    raw.Add(Make(p, PlanningRecommendationType.RoadCorridorCandidate,
                        PlanningSeverity.Info, "traffic spine / routing corridor"));
                    break;

                case PlanningPrimitiveType.SidewalkRegion:
                    raw.Add(Make(p, PlanningRecommendationType.SidewalkBandCandidate,
                        PlanningSeverity.Info, "pedestrian buffer / frontage band"));
                    break;

                case PlanningPrimitiveType.BuildingFootprint:
                    raw.Add(Make(p, PlanningRecommendationType.BuildingFootprintCandidate,
                        PlanningSeverity.Info, "structure placement candidate"));
                    if (p.PixelCount <= options.TinyBuildingPixelThreshold)
                        raw.Add(Make(p, PlanningRecommendationType.TinyBuildingCandidate,
                            PlanningSeverity.Warning, "very small footprint — review placement"));
                    break;

                case PlanningPrimitiveType.YardRegion:
                    raw.Add(Make(p, PlanningRecommendationType.YardCandidate,
                        PlanningSeverity.Info, "industrial yard / service area"));
                    break;

                case PlanningPrimitiveType.LandmarkMarker:
                    raw.Add(Make(p, PlanningRecommendationType.LandmarkMarker,
                        PlanningSeverity.Info, "orientation / named location marker"));
                    break;

                case PlanningPrimitiveType.SpawnMarker:
                    hasSpawn = true;
                    raw.Add(Make(p, PlanningRecommendationType.SpawnMarker,
                        PlanningSeverity.Info, "spawn planning marker"));
                    break;

                case PlanningPrimitiveType.GroundRegion:
                    raw.Add(Make(p, PlanningRecommendationType.OpenGroundArea,
                        PlanningSeverity.Info, "background terrain / filler area"));
                    if (p.PixelCount > options.LargeGroundPixelThreshold)
                        raw.Add(Make(p, PlanningRecommendationType.LargeOpenGroundArea,
                            PlanningSeverity.Info, "large open area — consider subdivision"));
                    break;
            }
        }

        if (!hasSpawn)
            raw.Add(new PlanningRecommendation(
                0, string.Empty,
                PlanningRecommendationType.MissingSpawnMarker,
                PlanningSeverity.Warning,
                "no spawn marker found — add at least one spawn point",
                0, null));

        var sorted = raw
            .OrderBy(r  => (int)r.Severity)
            .ThenBy(r   => r.RecommendationTypeStr, StringComparer.Ordinal)
            .ThenByDescending(r => r.PixelCount)
            .ThenBy(r   => r.Bounds?.Y ?? 0)
            .ThenBy(r   => r.Bounds?.X ?? 0)
            .ThenBy(r   => r.SourcePrimitiveId)
            .ToList();

        return new PlanningRuleResult(sorted, BuildSummary(sorted, primitives));
    }

    private static PlanningRecommendation Make(
        PlanningPrimitive p, PlanningRecommendationType type,
        PlanningSeverity severity, string role) =>
        new(p.PrimitiveId, p.PrimitiveTypeStr, type, severity, role, p.PixelCount, p.Bounds);

    private static PlanningSummary BuildSummary(
        List<PlanningRecommendation> recs, PrimitiveClassificationResult primitives)
    {
        var byType     = new Dictionary<string, int>(StringComparer.Ordinal);
        var bySeverity = new Dictionary<string, int>(StringComparer.Ordinal);
        var warnings   = 0;

        foreach (var r in recs)
        {
            byType[r.RecommendationTypeStr] = byType.GetValueOrDefault(r.RecommendationTypeStr, 0) + 1;
            var sevStr = r.Severity.ToString().ToLowerInvariant();
            bySeverity[sevStr] = bySeverity.GetValueOrDefault(sevStr, 0) + 1;
            if (r.Severity == PlanningSeverity.Warning) warnings++;
        }

        return new PlanningSummary(
            primitives.TotalPixels,
            primitives.PrimitiveCount,
            recs.Count,
            warnings,
            byType.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                  .ToDictionary(kv => kv.Key, kv => kv.Value)
                  .AsReadOnly(),
            bySeverity.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                      .ToDictionary(kv => kv.Key, kv => kv.Value)
                      .AsReadOnly());
    }
}
