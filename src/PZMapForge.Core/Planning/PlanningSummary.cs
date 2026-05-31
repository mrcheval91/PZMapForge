namespace PZMapForge.Core.Planning;

public sealed class PlanningSummary
{
    public int                              TotalPixels                  { get; }
    public int                              PrimitiveCount               { get; }
    public int                              RecommendationCount          { get; }
    public int                              WarningCount                 { get; }
    public IReadOnlyDictionary<string, int> CountsByRecommendationType   { get; }
    public IReadOnlyDictionary<string, int> CountsBySeverity             { get; }

    internal PlanningSummary(
        int totalPixels, int primitiveCount, int recommendationCount, int warningCount,
        IReadOnlyDictionary<string, int> countsByType,
        IReadOnlyDictionary<string, int> countsBySeverity)
    {
        TotalPixels                = totalPixels;
        PrimitiveCount             = primitiveCount;
        RecommendationCount        = recommendationCount;
        WarningCount               = warningCount;
        CountsByRecommendationType = countsByType;
        CountsBySeverity           = countsBySeverity;
    }
}
