namespace PZMapForge.Core.Planning;

public sealed class PlanningRuleResult
{
    public string                            ClaimBoundary       { get; }
    public int                               RecommendationCount => Recommendations.Count;
    public IReadOnlyList<PlanningRecommendation> Recommendations { get; }
    public PlanningSummary                   Summary             { get; }

    internal PlanningRuleResult(
        IReadOnlyList<PlanningRecommendation> recommendations,
        PlanningSummary                       summary)
    {
        ClaimBoundary   = "planning_artifact_only_not_pz_load_tested";
        Recommendations = recommendations;
        Summary         = summary;
    }
}
