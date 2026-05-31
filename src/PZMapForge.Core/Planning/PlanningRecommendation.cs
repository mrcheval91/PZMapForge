using PZMapForge.Core.Regions;

namespace PZMapForge.Core.Planning;

/// <summary>
/// A single deterministic planning recommendation derived from a classified primitive
/// or from a global rule (SourcePrimitiveId == 0 for global recommendations).
/// </summary>
public sealed class PlanningRecommendation
{
    /// <summary>0 = global recommendation not tied to a specific primitive.</summary>
    public int                       SourcePrimitiveId     { get; }
    public string                    SourcePrimitiveTypeStr { get; }
    public PlanningRecommendationType RecommendationType   { get; }
    public string                    RecommendationTypeStr { get; }
    public PlanningSeverity          Severity              { get; }
    public string                    PlanningRole          { get; }
    public int                       PixelCount            { get; }
    public RegionBounds?             Bounds                { get; }

    internal PlanningRecommendation(
        int sourcePrimitiveId, string sourcePrimitiveTypeStr,
        PlanningRecommendationType recommendationType, PlanningSeverity severity,
        string planningRole, int pixelCount, RegionBounds? bounds)
    {
        SourcePrimitiveId      = sourcePrimitiveId;
        SourcePrimitiveTypeStr = sourcePrimitiveTypeStr;
        RecommendationType     = recommendationType;
        RecommendationTypeStr  = recommendationType.ToTypeString();
        Severity               = severity;
        PlanningRole           = planningRole;
        PixelCount             = pixelCount;
        Bounds                 = bounds;
    }
}
