using System.Text.Json;
using PZMapForge.Core.ParsedCell;
using PZMapForge.Core.Planning;
using PZMapForge.Core.Primitives;
using PZMapForge.Core.Regions;
using Xunit;

namespace PZMapForge.Core.Tests.Planning;

public sealed class PlanningArtifactWriterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public PlanningArtifactWriterTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // Shared fixture
    // -----------------------------------------------------------------------

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ParsedCellFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell", "valid.json");

    private static (PlanningRuleResult Result, int Width, int Height) BuildRuleResult()
    {
        var loaded = ParsedCellLoader.Load(ParsedCellFixture);
        var codeToKind = loaded.Document!.Counts
            .ToDictionary(c => c.Code[0], c => c.Kind)
            .AsReadOnly();
        var regions    = RegionExtractor.Extract(loaded.Grid!, codeToKind);
        var primitives = PrimitiveClassifier.Classify(regions);
        var result     = PlanningRuleEngine.Evaluate(primitives);
        return (result, loaded.Grid!.Width, loaded.Grid!.Height);
    }

    private static readonly DateTimeOffset FixedTs = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private (string JsonPath, string MdPath) WriteFixture(string? dir = null)
    {
        var (result, w, h) = BuildRuleResult();
        return PlanningArtifactWriter.Write(
            dir ?? _tempDir, w, h,
            ParsedCellFixture, "test", result,
            overrideGeneratedAt: FixedTs);
    }

    // -----------------------------------------------------------------------
    // Test 1: JSON file is created
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_CreatesJsonFile()
    {
        var (json, _) = WriteFixture();
        Assert.True(File.Exists(json));
    }

    // -----------------------------------------------------------------------
    // Test 2: Markdown file is created
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_CreatesMdFile()
    {
        var (_, md) = WriteFixture();
        Assert.True(File.Exists(md));
    }

    // -----------------------------------------------------------------------
    // Test 3: schema sentinel
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_JsonSchema_IsCorrect()
    {
        var (json, _) = WriteFixture();
        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal("pzmapforge.plan-recommendations.v0.1",
            doc.RootElement.GetProperty("schema").GetString());
    }

    // -----------------------------------------------------------------------
    // Test 4: claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_JsonClaimBoundary_IsCorrect()
    {
        var (json, _) = WriteFixture();
        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal("planning_artifact_only_not_pz_load_tested",
            doc.RootElement.GetProperty("claim_boundary").GetString());
    }

    // -----------------------------------------------------------------------
    // Test 5: recommendation_count matches PlanningRuleResult
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_RecommendationCount_Matches()
    {
        var (result, w, h) = BuildRuleResult();
        var (json, _) = PlanningArtifactWriter.Write(
            _tempDir, w, h, ParsedCellFixture, "test", result, FixedTs);

        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal(result.RecommendationCount,
            doc.RootElement.GetProperty("recommendation_count").GetInt32());
    }

    // -----------------------------------------------------------------------
    // Test 6: warning_count matches PlanningRuleResult
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_WarningCount_Matches()
    {
        var (result, w, h) = BuildRuleResult();
        var (json, _) = PlanningArtifactWriter.Write(
            _tempDir, w, h, ParsedCellFixture, "test", result, FixedTs);

        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal(result.Summary.WarningCount,
            doc.RootElement.GetProperty("warning_count").GetInt32());
    }

    // -----------------------------------------------------------------------
    // Test 7: markdown contains claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_Markdown_ContainsClaimBoundary()
    {
        var (_, md) = WriteFixture();
        var content = File.ReadAllText(md);
        Assert.Contains("planning_artifact_only_not_pz_load_tested", content,
            StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 8: deterministic output with fixed timestamp
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_IsDeterministic()
    {
        var (result, w, h) = BuildRuleResult();

        var dir1 = Path.Combine(_tempDir, "run1");
        var dir2 = Path.Combine(_tempDir, "run2");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);

        var (j1, _) = PlanningArtifactWriter.Write(
            dir1, w, h, ParsedCellFixture, "test", result, FixedTs);
        var (j2, _) = PlanningArtifactWriter.Write(
            dir2, w, h, ParsedCellFixture, "test", result, FixedTs);

        Assert.Equal(File.ReadAllText(j1), File.ReadAllText(j2));
    }
}
