using PZMapForge.Core.Regions;

namespace PZMapForge.Core.Primitives;

/// <summary>
/// Maps semantic regions to planning primitive types.
/// Ports scripts/classify-primitives.ps1 exactly.
///
/// Sort order: primitive_type ASC, pixel_count DESC,
///             bounds.y ASC, bounds.x ASC, source_region_id ASC.
/// </summary>
public static class PrimitiveClassifier
{
    // Mirrors the PS $kindMap exactly: kind string -> (type, typeString, role)
    private static readonly Dictionary<string, (PlanningPrimitiveType Type, string TypeStr, string Role)>
        KindMapping = new(StringComparer.Ordinal)
        {
            ["grass"]           = (PlanningPrimitiveType.GroundRegion,      "ground_region",      "open ground or background area"),
            ["road"]            = (PlanningPrimitiveType.RoadRegion,        "road_region",        "driveable surface"),
            ["sidewalk"]        = (PlanningPrimitiveType.SidewalkRegion,    "sidewalk_region",    "pedestrian path"),
            ["row_house"]       = (PlanningPrimitiveType.BuildingFootprint, "building_footprint", "structure footprint"),
            ["depanneur"]       = (PlanningPrimitiveType.BuildingFootprint, "building_footprint", "structure footprint"),
            ["garage"]          = (PlanningPrimitiveType.BuildingFootprint, "building_footprint", "structure footprint"),
            ["industrial_yard"] = (PlanningPrimitiveType.YardRegion,        "yard_region",        "open industrial or service yard"),
            ["landmark"]        = (PlanningPrimitiveType.LandmarkMarker,    "landmark_marker",    "navigation reference point"),
            ["spawn"]           = (PlanningPrimitiveType.SpawnMarker,       "spawn_marker",       "player spawn location"),
        };

    /// <summary>
    /// Classifies every region in <paramref name="regions"/> into a planning primitive.
    /// Throws <see cref="ArgumentException"/> if any region kind has no mapping.
    /// </summary>
    public static PrimitiveClassificationResult Classify(RegionExtractionResult regions)
    {
        // Map to unsorted intermediates carrying all sort keys
        var unsorted = new List<(
            PlanningPrimitiveType type, string typeStr, string role,
            int sourceId, string kind, char code,
            int px, int bx, int by, int bw, int bh,
            double cx, double cy)>(regions.Regions.Count);

        foreach (var r in regions.Regions)
        {
            if (!KindMapping.TryGetValue(r.Kind, out var mapping))
                throw new ArgumentException(
                    $"No primitive mapping for kind '{r.Kind}'. " +
                    "Update PrimitiveClassifier.KindMapping to handle new kinds.");

            unsorted.Add((
                mapping.Type, mapping.TypeStr, mapping.Role,
                r.RegionId, r.Kind, r.Code,
                r.PixelCount,
                r.Bounds.X, r.Bounds.Y, r.Bounds.Width, r.Bounds.Height,
                r.Centroid.X, r.Centroid.Y));
        }

        // Deterministic sort: primitive_type ASC, pixel_count DESC,
        //                     bounds.y ASC, bounds.x ASC, source_region_id ASC
        var sorted = unsorted
            .OrderBy(p         => p.typeStr,   StringComparer.Ordinal)
            .ThenByDescending(p => p.px)
            .ThenBy(p          => p.by)
            .ThenBy(p          => p.bx)
            .ThenBy(p          => p.sourceId)
            .ToList();

        // Assign sequential primitive_id, build final list
        var primitives = new List<PlanningPrimitive>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++)
        {
            var s = sorted[i];
            var prim = new PlanningPrimitive(
                s.type, s.typeStr,
                s.sourceId, s.kind, s.code, s.px,
                new RegionBounds(s.bx, s.by, s.bw, s.bh),
                new RegionCentroid(s.cx, s.cy),
                s.role)
            { PrimitiveId = i + 1 };
            primitives.Add(prim);
        }

        // Build summary_by_primitive_type sorted by type string ASC
        var typeMap = new Dictionary<string, PrimitiveKindSummary>(StringComparer.Ordinal);
        foreach (var p in primitives)
        {
            if (!typeMap.TryGetValue(p.PrimitiveTypeStr, out var summary))
            {
                summary = new PrimitiveKindSummary(p.PrimitiveType, p.PrimitiveTypeStr);
                typeMap[p.PrimitiveTypeStr] = summary;
            }
            summary.RegionCount++;
            summary.TotalPixels += p.PixelCount;
            if (p.PixelCount > summary.LargestRegionPixels)
                summary.LargestRegionPixels = p.PixelCount;
        }

        var summaryList = typeMap.Values
            .OrderBy(s => s.PrimitiveTypeString, StringComparer.Ordinal)
            .ToList();

        return new PrimitiveClassificationResult(primitives, summaryList);
    }

    /// <summary>Returns true if the given kind has a mapping; false otherwise.</summary>
    public static bool IsKnownKind(string kind) => KindMapping.ContainsKey(kind);
}
