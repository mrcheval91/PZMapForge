using PZMapForge.Core.ParsedCell;
using PZMapForge.Core.Planning;
using PZMapForge.Core.Primitives;
using PZMapForge.Core.Regions;
using Xunit;

namespace PZMapForge.Core.Tests.Planning;

public sealed class PlanningRuleEngineTests
{
    // -----------------------------------------------------------------------
    // Shared fixtures
    // -----------------------------------------------------------------------

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ParsedCellFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell", "valid.json");

    private static PlanningRuleResult EvaluateFromFixture()
    {
        var loaded = ParsedCellLoader.Load(ParsedCellFixture);
        Assert.True(loaded.IsValid, string.Join("; ", loaded.Errors));

        var codeToKind = loaded.Document!.Counts
            .ToDictionary(c => c.Code[0], c => c.Kind)
            .AsReadOnly();
        var regions    = RegionExtractor.Extract(loaded.Grid!, codeToKind);
        var primitives = PrimitiveClassifier.Classify(regions);
        return PlanningRuleEngine.Evaluate(primitives);
    }

    // -----------------------------------------------------------------------
    // Test 1: valid fixture produces non-empty recommendations
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_ValidFixture_NonEmpty()
    {
        var result = EvaluateFromFixture();
        Assert.True(result.RecommendationCount > 0);
    }

    // -----------------------------------------------------------------------
    // Test 2: claim boundary is correct
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_ClaimBoundary_IsCorrect()
    {
        var result = EvaluateFromFixture();
        Assert.Equal("planning_artifact_only_not_pz_load_tested", result.ClaimBoundary);
    }

    // -----------------------------------------------------------------------
    // Test 3: recommendation count consistency
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_RecommendationCount_MatchesList()
    {
        var result = EvaluateFromFixture();
        Assert.Equal(result.Recommendations.Count, result.RecommendationCount);
        Assert.Equal(result.RecommendationCount, result.Summary.RecommendationCount);
    }

    // -----------------------------------------------------------------------
    // Tests 4-10: per-primitive-type recommendation type
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("road_region",        "road_corridor_candidate")]
    [InlineData("sidewalk_region",    "sidewalk_band_candidate")]
    [InlineData("building_footprint", "building_footprint_candidate")]
    [InlineData("yard_region",        "yard_candidate")]
    [InlineData("landmark_marker",    "landmark_marker")]
    [InlineData("spawn_marker",       "spawn_marker")]
    [InlineData("ground_region",      "open_ground_area")]
    public void Evaluate_PrimitiveType_CreatesExpectedRecommendation(
        string primitiveTypeStr, string expectedTypeStr)
    {
        var result = EvaluateFromFixture();
        var match  = result.Recommendations
            .FirstOrDefault(r => r.SourcePrimitiveTypeStr == primitiveTypeStr
                              && r.RecommendationTypeStr  == expectedTypeStr);
        Assert.NotNull(match);
    }

    // -----------------------------------------------------------------------
    // Test 11: missing spawn produces missing_spawn_marker warning
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_NoSpawn_ProducesMissingSpawnWarning()
    {
        // Build a tiny result with only a grass primitive and no spawn
        var grassRegion = SemanticRegion.CreateForTesting(
            "grass", 'g', 100,
            new RegionBounds(0, 0, 10, 10),
            new RegionCentroid(5, 5), regionId: 1);

        var fakeRegions = RegionExtractionResult.CreateForTesting(
            [grassRegion],
            [RegionKindSummary.CreateForTesting("grass", 'g', 1, 100, 100)]);

        var codeToKind = new Dictionary<char, string> { ['g'] = "grass" }.AsReadOnly();
        var primitives = PrimitiveClassifier.Classify(fakeRegions);
        var result     = PlanningRuleEngine.Evaluate(primitives);

        var warning = result.Recommendations
            .FirstOrDefault(r => r.RecommendationTypeStr == "missing_spawn_marker");
        Assert.NotNull(warning);
        Assert.Equal(PlanningSeverity.Warning, warning!.Severity);
        Assert.Equal(0, warning.SourcePrimitiveId);
    }

    // -----------------------------------------------------------------------
    // Test 12: determinism across two runs
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_IsDeterministic()
    {
        var r1 = EvaluateFromFixture();
        var r2 = EvaluateFromFixture();

        Assert.Equal(r1.RecommendationCount, r2.RecommendationCount);
        for (int i = 0; i < r1.Recommendations.Count; i++)
        {
            Assert.Equal(r1.Recommendations[i].RecommendationTypeStr,
                         r2.Recommendations[i].RecommendationTypeStr);
            Assert.Equal(r1.Recommendations[i].SourcePrimitiveId,
                         r2.Recommendations[i].SourcePrimitiveId);
            Assert.Equal(r1.Recommendations[i].PixelCount,
                         r2.Recommendations[i].PixelCount);
        }
    }

    // -----------------------------------------------------------------------
    // Test 13: counts_by_recommendation_type are stable
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_CountsByType_AreStable()
    {
        var r1 = EvaluateFromFixture();
        var r2 = EvaluateFromFixture();

        Assert.Equal(r1.Summary.CountsByRecommendationType.Count,
                     r2.Summary.CountsByRecommendationType.Count);
        foreach (var (key, val) in r1.Summary.CountsByRecommendationType)
            Assert.Equal(val, r2.Summary.CountsByRecommendationType[key]);
    }

    // -----------------------------------------------------------------------
    // Test 14: counts_by_severity are stable
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_CountsBySeverity_AreStable()
    {
        var r1 = EvaluateFromFixture();
        var r2 = EvaluateFromFixture();

        Assert.Equal(r1.Summary.CountsBySeverity.Count,
                     r2.Summary.CountsBySeverity.Count);
        foreach (var (key, val) in r1.Summary.CountsBySeverity)
            Assert.Equal(val, r2.Summary.CountsBySeverity[key]);
    }

    // -----------------------------------------------------------------------
    // Test 15: all primitive-derived recommendations carry source primitive id
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_PrimitiveDerived_HaveSourceId()
    {
        var result = EvaluateFromFixture();
        foreach (var rec in result.Recommendations)
        {
            // Global recommendations (missing_spawn_marker) have SourcePrimitiveId == 0
            if (rec.RecommendationTypeStr == "missing_spawn_marker")
                Assert.Equal(0, rec.SourcePrimitiveId);
            else
                Assert.True(rec.SourcePrimitiveId > 0,
                    $"{rec.RecommendationTypeStr} should have a source primitive id > 0");
        }
    }
}
