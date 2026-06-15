using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadFilteredTileCandidateShortlistBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-filtered-shortlist", Path.GetRandomFileName());

    public System2StaticRoadFilteredTileCandidateShortlistBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private string WriteFilteredSurvey(string family, string role, params (string TileName, string SourceFile)[] tiles)
    {
        var tileItems = string.Join(",\n",
            tiles.Select(t => $$"""
        { "tile_name": "{{t.TileName}}", "source_file": "{{t.SourceFile.Replace("\\", "\\\\")}}", "match_reason": "matched term: asphalt", "confidence": "LOCAL_FILTERED_TEXT_MATCH_ONLY" }
"""));

        var json = $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-local-tile-survey-filtered.v1",
  "families": [
    {
      "candidate_family": "{{family}}",
      "role": "{{role}}",
      "resolution_status": "FILTERED_SURVEY_CANDIDATES_FOUND",
      "confidence": "LOCAL_FILTERED_TEXT_MATCH_ONLY",
      "search_terms": ["asphalt","road"],
      "candidate_tiles": [
        {{tileItems}}
      ]
    },
    {
      "candidate_family": "road_node_metadata_candidate",
      "role": "node_metadata",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY",
      "confidence": "NONE_YET",
      "search_terms": [],
      "candidate_tiles": []
    }
  ]
}
""";
        var path = Path.Combine(_tempDir, "filtered_survey.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Input validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenInputMissing()
    {
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder
            .Build(Path.Combine(_tempDir, "no-such.json"));
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Basic validity
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_IsValid_OnFakeFilteredSurvey()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Shortlist);
    }

    // -----------------------------------------------------------------------
    // Top N respected
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RespectsTopN()
    {
        var tiles = Enumerable.Range(1, 50)
            .Select(i => ($"asphalt_road_{i:D2}", @"media\tiles\e.tiles"))
            .ToArray();
        var path   = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface", tiles);
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path, topN: 10);

        var roadFamily = result.Shortlist!.Families
            .First(f => f.CandidateFamily == "asphalt_road_surface_candidate");
        Assert.True(roadFamily.Candidates.Count <= 10);
    }

    // -----------------------------------------------------------------------
    // road_node_metadata_candidate stays UNRESOLVED
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RoadNodeFamily_StaysUnresolvedMetadataOnly()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);

        var nodeFamily = result.Shortlist!.Families
            .First(f => f.CandidateFamily == "road_node_metadata_candidate");
        Assert.Equal("UNRESOLVED_METADATA_ONLY", nodeFamily.ResolutionStatus);
    }

    // -----------------------------------------------------------------------
    // Resolution statuses
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_FamilyWithCandidates_GetsFilteredShortlistedCandidatesPresent()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);

        var roadFamily = result.Shortlist!.Families
            .First(f => f.CandidateFamily == "asphalt_road_surface_candidate");
        Assert.Equal("FILTERED_SHORTLISTED_CANDIDATES_PRESENT", roadFamily.ResolutionStatus);
    }

    [Fact]
    public void Build_FamilyWithNoCandidates_GetsFilteredShortlistedNoCandidates()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface");
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);

        var roadFamily = result.Shortlist!.Families
            .First(f => f.CandidateFamily == "asphalt_road_surface_candidate");
        Assert.Equal("FILTERED_SHORTLISTED_NO_CANDIDATES", roadFamily.ResolutionStatus);
    }

    // -----------------------------------------------------------------------
    // Confidence
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ShortlistedCandidate_HasLocalFilteredTextMatchRankedOnly()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);

        var candidate = result.Shortlist!.Families
            .First(f => f.CandidateFamily == "asphalt_road_surface_candidate")
            .Candidates[0];
        Assert.Equal("LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY", candidate.Confidence);
        Assert.Equal("LOCAL_FILTERED_TEXT_MATCH_ONLY", candidate.SourceConfidence);
    }

    // -----------------------------------------------------------------------
    // Status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_StatusFields_AreCorrect()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);
        Assert.Equal("FILTERED_SHORTLIST_ONLY",  result.Shortlist!.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN",        result.Shortlist.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",           result.Shortlist.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);
        var cb     = result.Shortlist!.ClaimBoundary;
        Assert.False(cb.WritesLotpack);
        Assert.False(cb.WritesWorldgenLua);
        Assert.False(cb.RuntimeProven);
        Assert.False(cb.PublicPlayableClaim);
        Assert.False(cb.WriterReadyClaim);
    }

    // -----------------------------------------------------------------------
    // Scoring: strong asphalt tile ranks above weak generic match
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ScoringRanks_AsphaltRoadHigherThanGenericMatch()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("generic_surface_tile",       @"media\tiles\other.tiles"),
            ("asphalt_road_exterior_01",   @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);

        var candidates = result.Shortlist!.Families
            .First(f => f.CandidateFamily == "asphalt_road_surface_candidate")
            .Candidates;

        Assert.True(candidates.Count >= 1);
        Assert.Equal("asphalt_road_exterior_01", candidates[0].TileName);
    }

    // -----------------------------------------------------------------------
    // Negative terms reduce score
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_NegativeTerms_ReduceScore()
    {
        var path = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_wall",   @"media\tiles\exterior.tiles"),
            ("asphalt_road_clean",  @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);

        var candidates = result.Shortlist!.Families
            .First(f => f.CandidateFamily == "asphalt_road_surface_candidate")
            .Candidates;

        var wallCandidate  = candidates.First(c => c.TileName == "asphalt_road_wall");
        var cleanCandidate = candidates.First(c => c.TileName == "asphalt_road_clean");
        Assert.True(cleanCandidate.Score > wallCandidate.Score,
            $"clean ({cleanCandidate.Score}) should score higher than wall ({wallCandidate.Score})");
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsMap22PTitle()
    {
        var path   = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);
        var md     = System2StaticRoadFilteredTileCandidateShortlistBuilder.RenderMarkdown(result.Shortlist!);
        Assert.Contains("MAP-22P", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsWarning()
    {
        var path   = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);
        var md     = System2StaticRoadFilteredTileCandidateShortlistBuilder.RenderMarkdown(result.Shortlist!);
        Assert.True(
            md.Contains("not runtime proof", StringComparison.OrdinalIgnoreCase) ||
            md.Contains("not writer readiness", StringComparison.OrdinalIgnoreCase),
            "markdown should contain warning about not runtime proof / not writer readiness");
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var path   = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);
        var md     = System2StaticRoadFilteredTileCandidateShortlistBuilder.RenderMarkdown(result.Shortlist!);
        Assert.Contains("MAP22P_SYSTEM2_STATIC_ROAD_FILTERED_TILE_CANDIDATE_SHORTLIST_COMPLETE",
            md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeader()
    {
        var path   = WriteFilteredSurvey("asphalt_road_surface_candidate", "road_surface",
            ("asphalt_road_01", @"media\tiles\exterior.tiles"));
        var result = System2StaticRoadFilteredTileCandidateShortlistBuilder.Build(path);
        var csv    = System2StaticRoadFilteredTileCandidateShortlistBuilder.RenderCsv(result.Shortlist!);
        Assert.Contains(
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons," +
            "source_confidence,confidence,resolution_status",
            csv, StringComparison.Ordinal);
    }
}
