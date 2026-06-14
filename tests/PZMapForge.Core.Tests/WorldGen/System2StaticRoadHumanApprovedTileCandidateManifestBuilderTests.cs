using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadHumanApprovedTileCandidateManifestBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-manifest", Path.GetRandomFileName());

    public System2StaticRoadHumanApprovedTileCandidateManifestBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteTempJson(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private static string AppliedReviewJson(
        string status1 = "NEEDS_MANUAL_REVIEW",
        string status2 = "NEEDS_MANUAL_REVIEW",
        string confidence1 = "LOCAL_TEXT_MATCH_RANKED_ONLY",
        string confidence2 = "LOCAL_TEXT_MATCH_RANKED_ONLY") => $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-candidate-review-applied.v1",
  "status": "HUMAN_REVIEW_APPLIED_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_review_json": "test.json",
  "source_decisions_csv": "test.csv",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "review_items": [
        {
          "rank": 1,
          "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "media/tiles/exterior.tiles",
          "score": 88,
          "score_reasons": ["tile_name contains asphalt"],
          "review_status": "{{status1}}",
          "human_note": "Good match",
          "recommended_next_action": "Keep for future visual/runtime validation; not writer-ready yet.",
          "confidence": "{{confidence1}}"
        },
        {
          "rank": 2,
          "tile_name": "floors_exterior_street_asphalt_02_0",
          "source_file": "media/tiles/exterior.tiles",
          "score": 80,
          "score_reasons": ["tile_name contains asphalt"],
          "review_status": "{{status2}}",
          "human_note": "",
          "recommended_next_action": "Inspect in TileZed or tile sheet preview before writer use.",
          "confidence": "{{confidence2}}"
        }
      ]
    }
  ],
  "totals": { "family_count": 1, "review_item_count": 2, "needs_manual_review_count": 2,
              "approved_count": 0, "rejected_count": 0, "applied_decision_count": 0, "unknown_decision_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false, "writer_ready_claim": false }
}
""";

    // -----------------------------------------------------------------------
    // Missing input
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenInputMissing()
    {
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(
            Path.Combine(_tempDir, "no-such-file.json"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Applied review JSON not found"));
    }

    // -----------------------------------------------------------------------
    // Approved count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WithOneApprovedCandidate_ExtractsApprovedCountOne()
    {
        var path   = WriteTempJson("applied.json",
            AppliedReviewJson("APPROVED_BY_HUMAN_REVIEW", "NEEDS_MANUAL_REVIEW",
                "HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN", "LOCAL_TEXT_MATCH_RANKED_ONLY"));
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.Manifest!.Totals.ApprovedCount);
    }

    // -----------------------------------------------------------------------
    // Rejected count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WithOneRejectedCandidate_ExtractsRejectedCountOne()
    {
        var path   = WriteTempJson("applied.json",
            AppliedReviewJson("REJECTED_BY_HUMAN_REVIEW", "NEEDS_MANUAL_REVIEW",
                "HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN", "LOCAL_TEXT_MATCH_RANKED_ONLY"));
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.Manifest!.Totals.RejectedCount);
    }

    // -----------------------------------------------------------------------
    // Pending count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WithPendingCandidates_ExtractsPendingCountCorrectly()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Manifest!.Totals.PendingCount);
    }

    // -----------------------------------------------------------------------
    // Unedited applied review keeps all pending
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_UneditedAppliedReview_KeepsAllPending()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Manifest!.Totals.PendingCount);
        Assert.Equal(0, result.Manifest.Totals.ApprovedCount);
        Assert.Equal(0, result.Manifest.Totals.RejectedCount);
    }

    // -----------------------------------------------------------------------
    // Approved candidate confidence
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ApprovedCandidate_ConfidenceRemainsHumanReviewOnly()
    {
        var path   = WriteTempJson("applied.json",
            AppliedReviewJson("APPROVED_BY_HUMAN_REVIEW", "NEEDS_MANUAL_REVIEW",
                "HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN", "LOCAL_TEXT_MATCH_RANKED_ONLY"));
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid);
        var approved = result.Manifest!.ApprovedCandidates.First();
        Assert.Equal("HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN", approved.Confidence);
    }

    // -----------------------------------------------------------------------
    // Approved candidate recommended_next_action
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ApprovedCandidate_RecommendedNextAction_SaysNotWriterReady()
    {
        var path   = WriteTempJson("applied.json",
            AppliedReviewJson("APPROVED_BY_HUMAN_REVIEW", "NEEDS_MANUAL_REVIEW",
                "HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN", "LOCAL_TEXT_MATCH_RANKED_ONLY"));
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid);
        var approved = result.Manifest!.ApprovedCandidates.First();
        Assert.Contains("not writer-ready", approved.RecommendedNextAction, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // runtime_status and writer_status unchanged
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RuntimeStatus_StaysNotRuntimeProven()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Manifest!.RuntimeStatus);
    }

    [Fact]
    public void Build_WriterStatus_StaysNotImplemented()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal("NOT_IMPLEMENTED", result.Manifest!.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse_IncludingWriterReadyClaim()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.False(result.Manifest!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Manifest.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Manifest.ClaimBoundary.RuntimeProven);
        Assert.False(result.Manifest.ClaimBoundary.PublicPlayableClaim);
        Assert.False(result.Manifest.ClaimBoundary.WriterReadyClaim);
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsMap22NTitle()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);
        var md     = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.RenderMarkdown(result.Manifest!);

        Assert.Contains("MAP-22N", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsWarningAboutHumanApproval()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);
        var md     = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.RenderMarkdown(result.Manifest!);

        Assert.True(
            md.Contains("Human approval is not runtime proof", StringComparison.OrdinalIgnoreCase) ||
            md.Contains("not writer readiness", StringComparison.OrdinalIgnoreCase),
            "Markdown should warn about human approval not being runtime proof");
    }

    [Fact]
    public void RenderMarkdown_ContainsApprovedSection()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);
        var md     = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.RenderMarkdown(result.Manifest!);

        Assert.Contains("Approved candidates", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsRejectedSection()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);
        var md     = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.RenderMarkdown(result.Manifest!);

        Assert.Contains("Rejected candidates", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsPendingSection()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);
        var md     = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.RenderMarkdown(result.Manifest!);

        Assert.Contains("pending manual review", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);
        var md     = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.RenderMarkdown(result.Manifest!);

        Assert.Contains("MAP22N_SYSTEM2_STATIC_ROAD_HUMAN_APPROVED_TILE_CANDIDATES_COMPLETE",
            md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeaderWithBucketColumn()
    {
        var path   = WriteTempJson("applied.json", AppliedReviewJson());
        var result = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.Build(path);
        var csv    = System2StaticRoadHumanApprovedTileCandidateManifestBuilder.RenderCsv(result.Manifest!);

        Assert.Contains(
            "bucket,candidate_family,role,rank,tile_name,source_file,score,score_reasons," +
            "human_note,review_status,confidence,recommended_next_action",
            csv, StringComparison.Ordinal);
    }
}
