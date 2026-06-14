using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadTileCandidateReviewBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-review", Path.GetRandomFileName());

    public System2StaticRoadTileCandidateReviewBuilderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteShortlist(string json)
    {
        var path = Path.Combine(_tempDir, $"shortlist_{Path.GetRandomFileName()}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private static string MinimalShortlist() => """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-candidate-shortlist.v1",
  "status": "SHORTLIST_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_local_tile_survey": "test.json",
  "top_per_family": 25,
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "source_intents": ["local_street_asphalt"],
      "input_candidate_count": 50,
      "shortlisted_candidate_count": 2,
      "resolution_status": "SHORTLISTED_CANDIDATES_PRESENT",
      "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY",
      "candidates": [
        { "rank": 1, "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "media/tiles/exterior.tiles", "score": 88,
          "score_reasons": ["tile_name contains asphalt", "tile_name contains street"],
          "source_confidence": "LOCAL_TEXT_MATCH_ONLY",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY" },
        { "rank": 2, "tile_name": "floors_exterior_street_asphalt_02_0",
          "source_file": "media/tiles/exterior.tiles", "score": 88,
          "score_reasons": ["tile_name contains asphalt", "tile_name contains street"],
          "source_confidence": "LOCAL_TEXT_MATCH_ONLY",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY" }
      ],
      "notes": "Shortlist requires visual/runtime validation before writer use."
    },
    {
      "candidate_family": "road_node_metadata_candidate",
      "role": "road_node",
      "source_intents": ["intersection_node"],
      "input_candidate_count": 0,
      "shortlisted_candidate_count": 0,
      "resolution_status": "UNRESOLVED_METADATA_ONLY",
      "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY",
      "candidates": [],
      "notes": "road_node_metadata_candidate: metadata only."
    }
  ],
  "totals": {
    "family_count": 2,
    "input_candidate_count": 50,
    "shortlisted_candidate_count": 2,
    "families_with_shortlist": 1,
    "families_without_shortlist": 1
  },
  "claim_boundary": {
    "writes_lotpack": false,
    "writes_worldgen_lua": false,
    "runtime_proven": false,
    "public_playable_claim": false
  }
}
""";

    // -----------------------------------------------------------------------
    // Missing input
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenShortlistFileMissing()
    {
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(
            Path.Combine(_tempDir, "no-such-file.json"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Shortlist file not found"));
    }

    // -----------------------------------------------------------------------
    // Creates review model from fake shortlist
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CreatesReviewModel_FromFakeShortlist()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Review);
        Assert.Equal(2, result.Review.Families.Count);
    }

    // -----------------------------------------------------------------------
    // Format and status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_OutputFormat_IsCorrect()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal("pzmapforge.deadmtl.system2.static-road-tile-candidate-review.v1",
            result.Review!.Format);
        Assert.Equal("REVIEW_PACKET_ONLY", result.Review.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Review.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",    result.Review.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.False(result.Review!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Review.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Review.ClaimBoundary.RuntimeProven);
        Assert.False(result.Review.ClaimBoundary.PublicPlayableClaim);
    }

    // -----------------------------------------------------------------------
    // All review items default to NEEDS_MANUAL_REVIEW
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AllReviewItems_AreNeedsManualReview()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        Assert.True(result.IsValid);
        var allItems = result.Review!.Families.SelectMany(f => f.ReviewItems).ToList();
        Assert.All(allItems, item => Assert.Equal("NEEDS_MANUAL_REVIEW", item.ReviewStatus));
    }

    // -----------------------------------------------------------------------
    // No item is approved/rejected by default
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ApprovedCount_IsZero()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(0, result.Review!.Totals.ApprovedCount);
    }

    [Fact]
    public void Build_RejectedCount_IsZero()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(0, result.Review!.Totals.RejectedCount);
    }

    // -----------------------------------------------------------------------
    // Totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Totals_CountReviewItemsCorrectly()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Review!.Totals.ReviewItemCount);
        Assert.Equal(2, result.Review.Totals.NeedsManualReviewCount);
    }

    // -----------------------------------------------------------------------
    // Markdown output
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsTitle()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        var md = System2StaticRoadTileCandidateReviewBuilder.RenderMarkdown(result.Review!);
        Assert.Contains("MAP-22L", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsWarning()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        var md = System2StaticRoadTileCandidateReviewBuilder.RenderMarkdown(result.Review!);
        Assert.True(
            md.Contains("not runtime-proven", StringComparison.OrdinalIgnoreCase) ||
            md.Contains("text-match candidates only", StringComparison.OrdinalIgnoreCase),
            "Markdown should contain warning about candidates not being runtime-proven");
    }

    [Fact]
    public void RenderMarkdown_ContainsFamilySection()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        var md = System2StaticRoadTileCandidateReviewBuilder.RenderMarkdown(result.Review!);
        Assert.Contains("asphalt_road_surface_candidate", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsTileName()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        var md = System2StaticRoadTileCandidateReviewBuilder.RenderMarkdown(result.Review!);
        Assert.Contains("floors_exterior_street_asphalt_01_0", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        var md = System2StaticRoadTileCandidateReviewBuilder.RenderMarkdown(result.Review!);
        Assert.Contains("MAP22L_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_PACKET_COMPLETE",
            md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV output
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        var csv = System2StaticRoadTileCandidateReviewBuilder.RenderCsv(result.Review!);
        Assert.Contains("candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence",
            csv, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderCsv_ContainsCandidateRow()
    {
        var path   = WriteShortlist(MinimalShortlist());
        var result = System2StaticRoadTileCandidateReviewBuilder.Build(path);

        var csv = System2StaticRoadTileCandidateReviewBuilder.RenderCsv(result.Review!);
        Assert.Contains("floors_exterior_street_asphalt_01_0", csv, StringComparison.Ordinal);
    }
}
