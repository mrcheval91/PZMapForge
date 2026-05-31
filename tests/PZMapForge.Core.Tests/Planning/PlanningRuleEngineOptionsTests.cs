using PZMapForge.Core.ParsedCell;
using PZMapForge.Core.Planning;
using PZMapForge.Core.Primitives;
using PZMapForge.Core.Regions;
using Xunit;

namespace PZMapForge.Core.Tests.Planning;

/// <summary>
/// Tests for PlanningRuleOptions threshold configuration.
/// Uses tests/fixtures/parsed-cell/valid.json which has:
///   - 3 building_footprint primitives, each 1 pixel
///   - 1 ground_region (grass) primitive, 89992 pixels
/// </summary>
public sealed class PlanningRuleEngineOptionsTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ParsedCellFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell", "valid.json");

    private static PrimitiveClassificationResult ClassifyFromFixture()
    {
        var loaded = ParsedCellLoader.Load(ParsedCellFixture);
        Assert.True(loaded.IsValid, string.Join("; ", loaded.Errors));
        var codeToKind = loaded.Document!.Counts
            .ToDictionary(c => c.Code[0], c => c.Kind)
            .AsReadOnly();
        var regions = RegionExtractor.Extract(loaded.Grid!, codeToKind);
        return PrimitiveClassifier.Classify(regions);
    }

    private static int CountType(PlanningRuleResult result, string typeStr) =>
        result.Recommendations.Count(r => r.RecommendationTypeStr == typeStr);

    // -----------------------------------------------------------------------
    // Test 1: default options preserve current output
    // -----------------------------------------------------------------------

    [Fact]
    public void Options_DefaultPreservesCurrentOutput()
    {
        var primitives = ClassifyFromFixture();
        var withDefault  = PlanningRuleEngine.Evaluate(primitives, PlanningRuleOptions.Default);
        var withNoOption = PlanningRuleEngine.Evaluate(primitives);

        Assert.Equal(withDefault.RecommendationCount, withNoOption.RecommendationCount);
        Assert.Equal(withDefault.Summary.WarningCount, withNoOption.Summary.WarningCount);

        for (int i = 0; i < withDefault.Recommendations.Count; i++)
        {
            Assert.Equal(withDefault.Recommendations[i].RecommendationTypeStr,
                         withNoOption.Recommendations[i].RecommendationTypeStr);
            Assert.Equal(withDefault.Recommendations[i].SourcePrimitiveId,
                         withNoOption.Recommendations[i].SourcePrimitiveId);
        }
    }

    // -----------------------------------------------------------------------
    // Test 2: threshold = 0 suppresses all tiny_building warnings
    //   (1px buildings are NOT <= 0, so no tiny_building_candidate)
    // -----------------------------------------------------------------------

    [Fact]
    public void Options_ZeroTinyThreshold_SuppressesAllTinyWarnings()
    {
        var primitives = ClassifyFromFixture();
        var opts   = new PlanningRuleOptions(tinyBuildingPixelThreshold: 0);
        var result = PlanningRuleEngine.Evaluate(primitives, opts);

        Assert.Equal(0, CountType(result, "tiny_building_candidate"));
    }

    // -----------------------------------------------------------------------
    // Test 3: lower threshold (0) vs default (9) — default produces warnings,
    //         zero-threshold does not (boundary: pixel_count=1 vs threshold=0)
    // -----------------------------------------------------------------------

    [Fact]
    public void Options_LowerTinyThreshold_FewerWarnings()
    {
        var primitives  = ClassifyFromFixture();
        var withDefault = PlanningRuleEngine.Evaluate(primitives);
        var withZero    = PlanningRuleEngine.Evaluate(primitives,
            new PlanningRuleOptions(tinyBuildingPixelThreshold: 0));

        var defaultTiny = CountType(withDefault, "tiny_building_candidate");
        var zeroTiny    = CountType(withZero,    "tiny_building_candidate");

        Assert.True(defaultTiny > 0,    "Default threshold should produce tiny warnings");
        Assert.Equal(0, zeroTiny);
        Assert.True(zeroTiny < defaultTiny, "Zero threshold should produce fewer tiny warnings");
    }

    // -----------------------------------------------------------------------
    // Test 4: threshold exactly equal to pixel_count triggers the warning
    //   (valid fixture buildings: pixel_count=1; threshold=1 → 1 <= 1 → warning)
    // -----------------------------------------------------------------------

    [Fact]
    public void Options_TinyThresholdEqualsPixelCount_TriggersWarning()
    {
        var primitives = ClassifyFromFixture();
        var opts   = new PlanningRuleOptions(tinyBuildingPixelThreshold: 1);
        var result = PlanningRuleEngine.Evaluate(primitives, opts);

        // All 3 building_footprint primitives have pixel_count=1; 1 <= 1 → all trigger
        Assert.Equal(3, CountType(result, "tiny_building_candidate"));
    }

    // -----------------------------------------------------------------------
    // Test 5: higher LargeGroundPixelThreshold suppresses large_open_ground_area
    //   (grass = 89992px; threshold=100000 → 89992 NOT > 100000 → no note)
    // -----------------------------------------------------------------------

    [Fact]
    public void Options_HigherLargeGroundThreshold_SuppressesNote()
    {
        var primitives   = ClassifyFromFixture();
        var withDefault  = PlanningRuleEngine.Evaluate(primitives);
        var withHigher   = PlanningRuleEngine.Evaluate(primitives,
            new PlanningRuleOptions(largeGroundPixelThreshold: 100_000));

        Assert.True(CountType(withDefault, "large_open_ground_area") > 0,
            "Default threshold should produce large_open_ground_area for 89992px grass");
        Assert.Equal(0, CountType(withHigher, "large_open_ground_area"));
    }

    // -----------------------------------------------------------------------
    // Test 6: lower LargeGroundPixelThreshold triggers for smaller regions
    //   (threshold=1 → even small ground regions > 1px get the note)
    // -----------------------------------------------------------------------

    [Fact]
    public void Options_LowerLargeGroundThreshold_TriggersForLargeRegion()
    {
        var primitives = ClassifyFromFixture();
        var opts   = new PlanningRuleOptions(largeGroundPixelThreshold: 1);
        var result = PlanningRuleEngine.Evaluate(primitives, opts);

        // grass(89992px) > 1 → large_open_ground_area triggered
        Assert.True(CountType(result, "large_open_ground_area") > 0);
    }

    // -----------------------------------------------------------------------
    // Test 7: negative TinyBuildingPixelThreshold throws
    // -----------------------------------------------------------------------

    [Fact]
    public void Options_NegativeTinyThreshold_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PlanningRuleOptions(tinyBuildingPixelThreshold: -1));
    }

    // -----------------------------------------------------------------------
    // Test 8: negative LargeGroundPixelThreshold throws
    // -----------------------------------------------------------------------

    [Fact]
    public void Options_NegativeLargeGroundThreshold_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PlanningRuleOptions(largeGroundPixelThreshold: -1));
    }
}
