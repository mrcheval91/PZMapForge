using PZMapForge.Core.Regions;

namespace PZMapForge.Core.Primitives;

public sealed class PlanningPrimitive
{
    public int                  PrimitiveId      { get; internal set; }
    public PlanningPrimitiveType PrimitiveType   { get; }
    public string               PrimitiveTypeStr { get; }
    public int                  SourceRegionId   { get; }
    public string               Kind             { get; }
    public char                 Code             { get; }
    public int                  PixelCount       { get; }
    public RegionBounds         Bounds           { get; }
    public RegionCentroid       Centroid         { get; }
    public string               PlanningRole     { get; }

    internal PlanningPrimitive(
        PlanningPrimitiveType type, string typeStr,
        int sourceRegionId, string kind, char code,
        int pixelCount, RegionBounds bounds, RegionCentroid centroid,
        string planningRole)
    {
        PrimitiveType   = type;
        PrimitiveTypeStr = typeStr;
        SourceRegionId  = sourceRegionId;
        Kind            = kind;
        Code            = code;
        PixelCount      = pixelCount;
        Bounds          = bounds;
        Centroid        = centroid;
        PlanningRole    = planningRole;
    }
}
