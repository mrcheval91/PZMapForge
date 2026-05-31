namespace PZMapForge.Core.Planning;

/// <summary>
/// Deterministic planning recommendation type.
/// String representations (snake_case) are the canonical keys in reports and summaries.
/// </summary>
public enum PlanningRecommendationType
{
    // ground
    OpenGroundArea,
    LargeOpenGroundArea,

    // roads and pedestrian
    RoadCorridorCandidate,
    SidewalkBandCandidate,

    // structures
    BuildingFootprintCandidate,
    TinyBuildingCandidate,

    // yards
    YardCandidate,

    // markers
    LandmarkMarker,
    SpawnMarker,
    MissingSpawnMarker,
}

public static class PlanningRecommendationTypeExtensions
{
    public static string ToTypeString(this PlanningRecommendationType t) => t switch
    {
        PlanningRecommendationType.OpenGroundArea              => "open_ground_area",
        PlanningRecommendationType.LargeOpenGroundArea         => "large_open_ground_area",
        PlanningRecommendationType.RoadCorridorCandidate       => "road_corridor_candidate",
        PlanningRecommendationType.SidewalkBandCandidate       => "sidewalk_band_candidate",
        PlanningRecommendationType.BuildingFootprintCandidate  => "building_footprint_candidate",
        PlanningRecommendationType.TinyBuildingCandidate       => "tiny_building_candidate",
        PlanningRecommendationType.YardCandidate               => "yard_candidate",
        PlanningRecommendationType.LandmarkMarker              => "landmark_marker",
        PlanningRecommendationType.SpawnMarker                 => "spawn_marker",
        PlanningRecommendationType.MissingSpawnMarker          => "missing_spawn_marker",
        _ => t.ToString().ToLowerInvariant(),
    };
}
