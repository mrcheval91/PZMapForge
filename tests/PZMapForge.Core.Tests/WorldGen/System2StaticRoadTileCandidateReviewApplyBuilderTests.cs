using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadTileCandidateReviewApplyBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-review-apply", Path.GetRandomFileName());

    public System2StaticRoadTileCandidateReviewApplyBuilderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteTempFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private static string MinimalReviewJson() => """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-candidate-review.v1",
  "status": "REVIEW_PACKET_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_shortlist": "test.json",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "input_candidate_count": 50,
      "shortlisted_candidate_count": 2,
      "review_items": [
        {
          "rank": 1,
          "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "media/tiles/exterior.tiles",
          "score": 88,
          "score_reasons": ["tile_name contains asphalt", "tile_name contains street"],
          "review_status": "NEEDS_MANUAL_REVIEW",
          "recommended_next_action": "Inspect in TileZed or tile sheet preview before writer use.",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY"
        },
        {
          "rank": 2,
          "tile_name": "floors_exterior_street_asphalt_02_0",
          "source_file": "media/tiles/exterior.tiles",
          "score": 88,
          "score_reasons": ["tile_name contains asphalt"],
          "review_status": "NEEDS_MANUAL_REVIEW",
          "recommended_next_action": "Inspect in TileZed or tile sheet preview before writer use.",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY"
        }
      ]
    }
  ],
  "totals": { "family_count": 1, "review_item_count": 2, "needs_manual_review_count": 2,
              "approved_count": 0, "rejected_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";

    private static string UneditedCsv() =>
        "candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence\n" +
        "asphalt_road_surface_candidate,road_surface,1,floors_exterior_street_asphalt_01_0,media/tiles/exterior.tiles,88,tile_name contains asphalt,NEEDS_MANUAL_REVIEW,Inspect in TileZed,LOCAL_TEXT_MATCH_RANKED_ONLY\n" +
        "asphalt_road_surface_candidate,road_surface,2,floors_exterior_street_asphalt_02_0,media/tiles/exterior.tiles,88,tile_name contains asphalt,NEEDS_MANUAL_REVIEW,Inspect in TileZed,LOCAL_TEXT_MATCH_RANKED_ONLY\n";

    private static string ApproveItem1RejectItem2Csv() =>
        "candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence,human_note\n" +
        "asphalt_road_surface_candidate,road_surface,1,floors_exterior_street_asphalt_01_0,media/tiles/exterior.tiles,88,tile_name contains asphalt,APPROVED_BY_HUMAN_REVIEW,Inspect in TileZed,LOCAL_TEXT_MATCH_RANKED_ONLY,Looks like correct asphalt in TileZed\n" +
        "asphalt_road_surface_candidate,road_surface,2,floors_exterior_street_asphalt_02_0,media/tiles/exterior.tiles,88,tile_name contains asphalt,REJECTED_BY_HUMAN_REVIEW,Inspect in TileZed,LOCAL_TEXT_MATCH_RANKED_ONLY,Too light in color\n";

    // -----------------------------------------------------------------------
    // Missing inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenReviewJsonMissing()
    {
        var csv    = WriteTempFile("decisions.csv", UneditedCsv());
        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(
            Path.Combine(_tempDir, "no-such-review.json"), csv);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Review JSON not found"));
    }

    [Fact]
    public void Build_ReturnsError_WhenDecisionsCsvMissing()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var result     = System2StaticRoadTileCandidateReviewApplyBuilder.Build(
            reviewJson, Path.Combine(_tempDir, "no-such-decisions.csv"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Decisions CSV not found"));
    }

    // -----------------------------------------------------------------------
    // Approve and reject via edited CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_EditedCsv_CanApproveOneCandidate()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", ApproveItem1RejectItem2Csv());

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.Applied!.Totals.ApprovedCount);
    }

    [Fact]
    public void Build_EditedCsv_CanRejectOneCandidate()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", ApproveItem1RejectItem2Csv());

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.Applied!.Totals.RejectedCount);
    }

    // -----------------------------------------------------------------------
    // Unedited CSV preserves all NEEDS_MANUAL_REVIEW
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_UneditedCsv_KeepsAllNeedsManualReview()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", UneditedCsv());

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Applied!.Totals.NeedsManualReviewCount);
        Assert.Equal(0, result.Applied.Totals.ApprovedCount);
        Assert.Equal(0, result.Applied.Totals.RejectedCount);
    }

    // -----------------------------------------------------------------------
    // Invalid review_status: increments unknown_decision_count, does not fail
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_InvalidStatus_IncrementsUnknownCount_AndDoesNotFail()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var badCsv =
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence\n" +
            "asphalt_road_surface_candidate,road_surface,1,floors_exterior_street_asphalt_01_0,media/tiles/exterior.tiles,88,reasons,INVALID_STATUS,action,confidence\n";
        var csv = WriteTempFile("decisions.csv", badCsv);

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.Applied!.Totals.UnknownDecisionCount);
        Assert.NotEmpty(result.Warnings);
    }

    // -----------------------------------------------------------------------
    // Unmatched CSV row: increments unknown_decision_count, does not fail
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_UnmatchedRow_IncrementsUnknownCount_AndDoesNotFail()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var badCsv =
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence\n" +
            "asphalt_road_surface_candidate,road_surface,99,nonexistent_tile,media/tiles/exterior.tiles,0,reasons,APPROVED_BY_HUMAN_REVIEW,action,confidence\n";
        var csv = WriteTempFile("decisions.csv", badCsv);

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.Applied!.Totals.UnknownDecisionCount);
        Assert.NotEmpty(result.Warnings);
    }

    // -----------------------------------------------------------------------
    // Duplicate row: last row wins, counted once
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_DuplicateRow_LastWins()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var dupCsv =
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence\n" +
            "asphalt_road_surface_candidate,road_surface,1,floors_exterior_street_asphalt_01_0,media/tiles/exterior.tiles,88,reasons,APPROVED_BY_HUMAN_REVIEW,action,confidence\n" +
            "asphalt_road_surface_candidate,road_surface,1,floors_exterior_street_asphalt_01_0,media/tiles/exterior.tiles,88,reasons,REJECTED_BY_HUMAN_REVIEW,action,confidence\n";
        var csv = WriteTempFile("decisions.csv", dupCsv);

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        var item = result.Applied!.Families[0].ReviewItems.First(i => i.Rank == 1);
        Assert.Equal("REJECTED_BY_HUMAN_REVIEW", item.ReviewStatus);
        Assert.Equal(1, result.Applied.Totals.AppliedDecisionCount);
        Assert.NotEmpty(result.Warnings);
    }

    // -----------------------------------------------------------------------
    // Confidence assignment
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ApprovalConfidence_IsHumanReviewOnlyNotRuntimeProven()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", ApproveItem1RejectItem2Csv());

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        var approved = result.Applied!.Families[0].ReviewItems.First(i => i.ReviewStatus == "APPROVED_BY_HUMAN_REVIEW");
        Assert.Equal("HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN", approved.Confidence);
    }

    // -----------------------------------------------------------------------
    // Approval does not change runtime_status
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Approval_DoesNotChangeRuntimeStatus()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", ApproveItem1RejectItem2Csv());

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Applied!.RuntimeStatus);
    }

    // -----------------------------------------------------------------------
    // Totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Totals_CountsCorrectly()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", ApproveItem1RejectItem2Csv());

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Applied!.Totals.ReviewItemCount);
        Assert.Equal(1, result.Applied.Totals.ApprovedCount);
        Assert.Equal(1, result.Applied.Totals.RejectedCount);
        Assert.Equal(0, result.Applied.Totals.NeedsManualReviewCount);
        Assert.Equal(2, result.Applied.Totals.AppliedDecisionCount);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse_IncludingWriterReadyClaim()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", UneditedCsv());

        var result = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        Assert.True(result.IsValid);
        Assert.False(result.Applied!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Applied.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Applied.ClaimBoundary.RuntimeProven);
        Assert.False(result.Applied.ClaimBoundary.PublicPlayableClaim);
        Assert.False(result.Applied.ClaimBoundary.WriterReadyClaim);
    }

    // -----------------------------------------------------------------------
    // Markdown output
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsMap22MTitle()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", ApproveItem1RejectItem2Csv());
        var result     = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        var md = System2StaticRoadTileCandidateReviewApplyBuilder.RenderMarkdown(result.Applied!);
        Assert.Contains("MAP-22M", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsWarning()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", UneditedCsv());
        var result     = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        var md = System2StaticRoadTileCandidateReviewApplyBuilder.RenderMarkdown(result.Applied!);
        Assert.True(
            md.Contains("Human approval is not runtime proof", StringComparison.OrdinalIgnoreCase) ||
            md.Contains("not writer readiness", StringComparison.OrdinalIgnoreCase),
            "Markdown should contain warning about human approval not being runtime proof");
    }

    [Fact]
    public void RenderMarkdown_ContainsApprovedSection()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", ApproveItem1RejectItem2Csv());
        var result     = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        var md = System2StaticRoadTileCandidateReviewApplyBuilder.RenderMarkdown(result.Applied!);
        Assert.Contains("Approved candidates", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsRejectedSection()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", ApproveItem1RejectItem2Csv());
        var result     = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        var md = System2StaticRoadTileCandidateReviewApplyBuilder.RenderMarkdown(result.Applied!);
        Assert.Contains("Rejected candidates", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", UneditedCsv());
        var result     = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        var md = System2StaticRoadTileCandidateReviewApplyBuilder.RenderMarkdown(result.Applied!);
        Assert.Contains("MAP22M_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_APPLY_COMPLETE",
            md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV output
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeader()
    {
        var reviewJson = WriteTempFile("review.json", MinimalReviewJson());
        var csv        = WriteTempFile("decisions.csv", UneditedCsv());
        var result     = System2StaticRoadTileCandidateReviewApplyBuilder.Build(reviewJson, csv);

        var outputCsv = System2StaticRoadTileCandidateReviewApplyBuilder.RenderCsv(result.Applied!);
        Assert.Contains(
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons," +
            "review_status,human_note,recommended_next_action,confidence",
            outputCsv, StringComparison.Ordinal);
    }
}
