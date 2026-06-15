using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadFilteredLocalTileSurveyBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-filtered-survey", Path.GetRandomFileName());

    public System2StaticRoadFilteredLocalTileSurveyBuilderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private string WriteSurveyJson(string extraFamilies = "")
    {
        var json = $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-survey.v1",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "source_intents": ["primary roads", "alleys"]
    },
    {
      "candidate_family": "road_node_metadata_candidate",
      "role": "node_metadata",
      "source_intents": ["road connectivity"]
    }
    {{extraFamilies}}
  ]
}
""";
        var path = Path.Combine(_tempDir, "survey.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakePzRoot(params (string RelPath, string Content)[] files)
    {
        var root = Path.Combine(_tempDir, "pz");
        Directory.CreateDirectory(root);
        foreach (var (rel, content) in files)
        {
            var full = Path.Combine(root, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content, Encoding.UTF8);
        }
        return root;
    }

    // -----------------------------------------------------------------------
    // Input validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenSurveyFileMissing()
    {
        var result = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(Path.Combine(_tempDir, "no-such.json"), _tempDir);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Missing PZ root → zero-candidate survey, exit OK
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_IsValid_WhenPzRootMissing()
    {
        var surveyPath = WriteSurveyJson();
        var result = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        Assert.True(result.IsValid);
        Assert.NotNull(result.Survey);
    }

    [Fact]
    public void Build_ZeroCandidates_WhenPzRootMissing()
    {
        var surveyPath = WriteSurveyJson();
        var result = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        Assert.Equal(0, result.Survey!.Totals.CandidateTileCount);
    }

    // -----------------------------------------------------------------------
    // road_node_metadata_candidate → UNRESOLVED
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RoadNodeFamily_HasUnresolvedStatus()
    {
        var surveyPath = WriteSurveyJson();
        var pzRoot     = MakePzRoot();
        var result     = System2StaticRoadFilteredLocalTileSurveyBuilder.Build(surveyPath, pzRoot);

        var nodeFamily = result.Survey!.Families
            .First(f => f.CandidateFamily == "road_node_metadata_candidate");
        Assert.Equal("UNRESOLVED_NEEDS_TILE_SURVEY", nodeFamily.ResolutionStatus);
    }

    // -----------------------------------------------------------------------
    // Excluded path fragments keep polluted sources out
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ExcludesFiles_UnderProfanityPath()
    {
        var surveyPath = WriteSurveyJson();
        var pzRoot = MakePzRoot(
            (@"media\profanity\Dictionary.txt", "asphalt street road pavement asphalt_road_01")
        );
        var result = System2StaticRoadFilteredLocalTileSurveyBuilder.Build(surveyPath, pzRoot);
        Assert.Equal(0, result.Survey!.Totals.CandidateTileCount);
        Assert.True(result.Survey.Totals.ExcludedSourceFileCount >= 1);
    }

    [Fact]
    public void Build_ExcludesFiles_UnderScriptsItemsPath()
    {
        var surveyPath = WriteSurveyJson();
        var pzRoot = MakePzRoot(
            (@"media\scripts\items\weapons.txt", "asphalt_road_fragment street_item pavement_debris")
        );
        var result = System2StaticRoadFilteredLocalTileSurveyBuilder.Build(surveyPath, pzRoot);
        Assert.Equal(0, result.Survey!.Totals.CandidateTileCount);
        Assert.True(result.Survey.Totals.ExcludedSourceFileCount >= 1);
    }

    // -----------------------------------------------------------------------
    // Scanned files produce candidates
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_FindsCandidates_FromScannedTilesFile()
    {
        var surveyPath = WriteSurveyJson();
        var pzRoot = MakePzRoot(
            (@"media\tiles\exterior.tiles", "asphalt_road_01 asphalt_street_surface")
        );
        var result = System2StaticRoadFilteredLocalTileSurveyBuilder.Build(surveyPath, pzRoot);
        Assert.True(result.Survey!.Totals.CandidateTileCount > 0);
    }

    [Fact]
    public void Build_ScannedCount_IsCorrect()
    {
        var surveyPath = WriteSurveyJson();
        var pzRoot = MakePzRoot(
            (@"media\tiles\exterior.tiles",          "asphalt road"),
            (@"media\profanity\Dictionary.txt",      "asphalt")
        );
        var result = System2StaticRoadFilteredLocalTileSurveyBuilder.Build(surveyPath, pzRoot);
        Assert.Equal(1, result.Survey!.Totals.ScannedSourceFileCount);
        Assert.Equal(1, result.Survey!.Totals.ExcludedSourceFileCount);
    }

    // -----------------------------------------------------------------------
    // Confidence
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Confidence_IsLocalFilteredTextMatchOnly_WhenCandidatesFound()
    {
        var surveyPath = WriteSurveyJson();
        var pzRoot = MakePzRoot(
            (@"media\tiles\exterior.tiles", "asphalt_road_01")
        );
        var result = System2StaticRoadFilteredLocalTileSurveyBuilder.Build(surveyPath, pzRoot);
        var roadFamily = result.Survey!.Families
            .First(f => f.CandidateFamily == "asphalt_road_surface_candidate");
        Assert.Equal("LOCAL_FILTERED_TEXT_MATCH_ONLY", roadFamily.Confidence);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var surveyPath = WriteSurveyJson();
        var result     = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        var cb = result.Survey!.ClaimBoundary;
        Assert.False(cb.WritesLotpack);
        Assert.False(cb.WritesWorldgenLua);
        Assert.False(cb.RuntimeProven);
        Assert.False(cb.PublicPlayableClaim);
        Assert.False(cb.WriterReadyClaim);
    }

    // -----------------------------------------------------------------------
    // Status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_StatusFields_AreCorrect()
    {
        var surveyPath = WriteSurveyJson();
        var result     = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        Assert.Equal("LOCAL_TILE_SURVEY_FILTERED_ONLY", result.Survey!.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN",              result.Survey.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",                 result.Survey.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsMap22OTitle()
    {
        var surveyPath = WriteSurveyJson();
        var result     = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        var md = System2StaticRoadFilteredLocalTileSurveyBuilder.RenderMarkdown(result.Survey!);
        Assert.Contains("MAP-22O", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var surveyPath = WriteSurveyJson();
        var result     = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        var md = System2StaticRoadFilteredLocalTileSurveyBuilder.RenderMarkdown(result.Survey!);
        Assert.Contains("MAP22O_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_FILTERED_COMPLETE",
            md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var surveyPath = WriteSurveyJson();
        var result     = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        var md = System2StaticRoadFilteredLocalTileSurveyBuilder.RenderMarkdown(result.Survey!);
        Assert.Contains("writer_ready_claim", md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeader()
    {
        var surveyPath = WriteSurveyJson();
        var result     = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        var csv = System2StaticRoadFilteredLocalTileSurveyBuilder.RenderCsv(result.Survey!);
        Assert.Contains(
            "candidate_family,role,tile_name,source_file,match_reason,confidence,resolution_status",
            csv, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Source filter in JSON output
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_OutputJson_ContainsSourceFilterSection()
    {
        var surveyPath = WriteSurveyJson();
        var result     = System2StaticRoadFilteredLocalTileSurveyBuilder
            .Build(surveyPath, Path.Combine(_tempDir, "no-pz-root"));
        var json = JsonSerializer.Serialize(result.Survey,
            new JsonSerializerOptions { WriteIndented = true });
        Assert.Contains("excluded_path_fragments", json, StringComparison.Ordinal);
        Assert.Contains("preferred_path_fragments", json, StringComparison.Ordinal);
    }
}
