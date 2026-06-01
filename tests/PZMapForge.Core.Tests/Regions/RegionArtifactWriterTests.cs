using System.Text.Json;
using PZMapForge.Core.ParsedCell;
using PZMapForge.Core.Regions;
using Xunit;

namespace PZMapForge.Core.Tests.Regions;

public sealed class RegionArtifactWriterTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public RegionArtifactWriterTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ParsedCellFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell", "valid.json");

    private static RegionExtractionResult ExtractFromFixture()
    {
        var loaded = ParsedCellLoader.Load(ParsedCellFixture);
        Assert.True(loaded.IsValid, string.Join("; ", loaded.Errors));
        var codeToKind = loaded.Document!.Counts
            .ToDictionary(c => c.Code[0], c => c.Kind)
            .AsReadOnly();
        return RegionExtractor.Extract(loaded.Grid!, codeToKind);
    }

    private (string JsonPath, string MdPath, RegionExtractionResult Result) WriteDefault()
    {
        var result      = ExtractFromFixture();
        var (json, md)  = RegionArtifactWriter.Write(_tempDir, 300, 300, "parsed-cell.json", result);
        return (json, md, result);
    }

    // -----------------------------------------------------------------------
    // JSON tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_CreatesRegionsJson()
    {
        var (json, _, _) = WriteDefault();
        Assert.True(File.Exists(json));
    }

    [Fact]
    public void Write_Schema_IsCorrect()
    {
        var (json, _, _) = WriteDefault();
        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal("pzmapforge.regions.v0.1",
            doc.RootElement.GetProperty("schema").GetString());
    }

    [Fact]
    public void Write_ClaimBoundary_IsCorrect()
    {
        var (json, _, _) = WriteDefault();
        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal("planning_artifact_only_not_pz_load_tested",
            doc.RootElement.GetProperty("claim_boundary").GetString());
    }

    [Fact]
    public void Write_RegionCount_Matches()
    {
        var (json, _, result) = WriteDefault();
        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal(result.TotalRegions,
            doc.RootElement.GetProperty("total_regions").GetInt32());
    }

    [Fact]
    public void Write_SummaryPixelTotal_Is90000()
    {
        var (json, _, _) = WriteDefault();
        var doc     = JsonDocument.Parse(File.ReadAllText(json));
        var summary = doc.RootElement.GetProperty("summary_by_kind").EnumerateArray();
        Assert.Equal(90000, summary.Sum(s => s.GetProperty("total_pixels").GetInt32()));
    }

    // -----------------------------------------------------------------------
    // Markdown tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_CreatesRegionsReportMd()
    {
        var (_, md, _) = WriteDefault();
        Assert.True(File.Exists(md));
    }

    [Fact]
    public void Write_Markdown_ContainsClaimBoundary()
    {
        var (_, md, _) = WriteDefault();
        Assert.Contains("planning_artifact_only_not_pz_load_tested",
            File.ReadAllText(md), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_Markdown_ContainsSummaryByKind()
    {
        var (_, md, _) = WriteDefault();
        var content = File.ReadAllText(md);
        Assert.Contains("Summary by kind", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("grass", content, StringComparison.OrdinalIgnoreCase);
    }
}
