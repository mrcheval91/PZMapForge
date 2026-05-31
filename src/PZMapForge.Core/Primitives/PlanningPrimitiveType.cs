namespace PZMapForge.Core.Primitives;

/// <summary>
/// Planning primitive types. Matches the PS classify-primitives.ps1 kindMap exactly.
/// Multiple semantic kinds can map to the same primitive type
/// (e.g. row_house, depanneur, garage all map to BuildingFootprint).
/// </summary>
public enum PlanningPrimitiveType
{
    GroundRegion,       // grass
    RoadRegion,         // road
    SidewalkRegion,     // sidewalk
    BuildingFootprint,  // row_house, depanneur, garage
    YardRegion,         // industrial_yard
    LandmarkMarker,     // landmark
    SpawnMarker,        // spawn
}
