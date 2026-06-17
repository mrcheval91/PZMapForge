using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-writer-input-manifest", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteMvpJson(string verdict = "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE")
    {
        var path = Path.Combine(_tempDir, "mvp.json");
        File.WriteAllText(path,
            $"{{\"format\":\"pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1\"," +
            $"\"tile_id\":\"map_00\",\"verdict\":\"{verdict}\"}}");
        return path;
    }

    private string WriteOverlayJson(string verdict = "MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE")
    {
        var path = Path.Combine(_tempDir, "overlay.json");
        File.WriteAllText(path,
            $"{{\"format\":\"pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-qa-overlay.v1\"," +
            $"\"map_id\":\"map_00\",\"verdict\":\"{verdict}\"}}");
        return path;
    }

    private string WriteOverlayCsv()
    {
        var path = Path.Combine(_tempDir, "overlay.csv");
        File.WriteAllText(path, "feature_order,feature_kind,feature_id\n1,COMPONENT_BBOX,map_00_component_0001\n");
        return path;
    }

    private string WriteOverlayPng()
    {
        var path = Path.Combine(_tempDir, "overlay.png");
        File.WriteAllBytes(path, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        return path;
    }

    private string WriteReviewJson(
        string verdict = "MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE",
        string componentId = "map_00_component_0001",
        int bboxMinX = 124,
        int lotCount = 7,
        int slotCount = 7,
        int featureCount = 15,
        bool writerReady = false,
        bool runtimeValid = false,
        bool materialized = false,
        int passedReviewChecks = 18)
    {
        var path = Path.Combine(_tempDir, "review.json");
        int widthPx = 212 - bboxMinX + 1;
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"format\": \"pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-qa-review-packet.v1\",");
        sb.AppendLine($"  \"map_id\": \"map_00\",");
        sb.AppendLine($"  \"target_component_id\": \"{componentId}\",");
        sb.AppendLine($"  \"target_component_order\": 1,");
        sb.AppendLine($"  \"intent\": \"RESIDENTIAL_LOT_BLOCK\",");
        sb.AppendLine($"  \"access_readiness_class\": \"DUAL_ACCESS_CANDIDATE\",");
        sb.AppendLine($"  \"component_bbox\": {{\"min_x\": {bboxMinX}, \"min_y\": 10, \"max_x\": 212, \"max_y\": 69, \"width_px\": {widthPx}, \"height_px\": 60}},");
        sb.AppendLine($"  \"source_dimensions\": \"256x256\",");
        sb.AppendLine($"  \"lot_count\": {lotCount},");
        sb.AppendLine($"  \"accepted_building_slot_count\": {slotCount},");
        sb.AppendLine($"  \"overlay_feature_count\": {featureCount},");
        sb.AppendLine($"  \"review_check_count\": 18,");
        sb.AppendLine($"  \"passed_review_check_count\": {passedReviewChecks},");
        sb.AppendLine($"  \"failed_review_check_count\": {18 - passedReviewChecks},");
        sb.AppendLine($"  \"writer_ready\": {writerReady.ToString().ToLower()},");
        sb.AppendLine($"  \"runtime_valid\": {runtimeValid.ToString().ToLower()},");
        sb.AppendLine($"  \"materialized\": {materialized.ToString().ToLower()},");
        sb.AppendLine($"  \"verdict\": \"{verdict}\"");
        sb.Append("}");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private string WriteReviewCsv()
    {
        var path = Path.Combine(_tempDir, "review.csv");
        File.WriteAllText(path, "check_order,check_id,check_status\n1,GEOMETRY_MVP_EXISTS,PASS\n");
        return path;
    }

    private string WriteReviewMd()
    {
        var path = Path.Combine(_tempDir, "review.md");
        File.WriteAllText(path, "# MAP-26C WorldBuilder Minimal Concrete Geometry QA Review Packet\n\n## Claim Boundary\n\nQA-only.\n");
        return path;
    }

    private string WriteReviewSummary()
    {
        var path = Path.Combine(_tempDir, "review.summary.txt");
        File.WriteAllText(path, "verdict: MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE\n");
        return path;
    }

    private (string mvp, string oj, string oc, string op, string rj, string rc, string rm, string rs)
        MakeValidFixtures(
            string? mvpVerdict = null,
            string? overlayVerdict = null,
            string? reviewVerdict = null,
            string? componentId = null,
            int bboxMinX = 124,
            int lotCount = 7,
            int slotCount = 7,
            int featureCount = 15,
            bool writerReady = false,
            bool runtimeValid = false,
            bool materialized = false,
            int passedReviewChecks = 18)
    {
        return (
            WriteMvpJson(mvpVerdict ?? "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE"),
            WriteOverlayJson(overlayVerdict ?? "MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE"),
            WriteOverlayCsv(),
            WriteOverlayPng(),
            WriteReviewJson(reviewVerdict ?? "MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE",
                componentId ?? "map_00_component_0001",
                bboxMinX, lotCount, slotCount, featureCount,
                writerReady, runtimeValid, materialized, passedReviewChecks),
            WriteReviewCsv(),
            WriteReviewMd(),
            WriteReviewSummary()
        );
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult BuildFromFixtures(
        string? mvpVerdict = null,
        string? overlayVerdict = null,
        string? reviewVerdict = null,
        string? componentId = null,
        int bboxMinX = 124,
        int lotCount = 7,
        int slotCount = 7,
        int featureCount = 15,
        bool writerReady = false,
        bool runtimeValid = false,
        bool materialized = false,
        int passedReviewChecks = 18)
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures(
            mvpVerdict, overlayVerdict, reviewVerdict, componentId,
            bboxMinX, lotCount, slotCount, featureCount,
            writerReady, runtimeValid, materialized, passedReviewChecks);
        return new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder()
            .Build(mvp, oj, oc, op, rj, rc, rm, rs);
    }

    // -----------------------------------------------------------------------
    // Guard tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingMvpJson_IsInvalid()
    {
        var (_, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder()
            .Build("__nonexistent__.json", oj, oc, op, rj, rc, rm, rs);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingOverlayJson_IsInvalid()
    {
        var (mvp, _, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder()
            .Build(mvp, "__nonexistent__.json", oc, op, rj, rc, rm, rs);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_MissingOverlayPng_IsInvalid()
    {
        var (mvp, oj, oc, _, rj, rc, rm, rs) = MakeValidFixtures();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder()
            .Build(mvp, oj, oc, "__nonexistent__.png", rj, rc, rm, rs);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_MissingReviewJson_IsInvalid()
    {
        var (mvp, oj, oc, op, _, rc, rm, rs) = MakeValidFixtures();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder()
            .Build(mvp, oj, oc, op, "__nonexistent__.json", rc, rm, rs);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_MalformedReviewJson_IsInvalid()
    {
        var (mvp, oj, oc, op, _, rc, rm, rs) = MakeValidFixtures();
        var bad = Path.Combine(_tempDir, "bad_review.json");
        File.WriteAllText(bad, "NOT VALID JSON {{{{");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder()
            .Build(mvp, oj, oc, op, bad, rc, rm, rs);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongMap26AVerdict_IsInvalid()
    {
        var result = BuildFromFixtures(mvpVerdict: "WRONG_VERDICT");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongMap26BVerdict_IsInvalid()
    {
        var result = BuildFromFixtures(overlayVerdict: "WRONG_VERDICT");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongMap26CVerdict_IsInvalid()
    {
        var result = BuildFromFixtures(reviewVerdict: "WRONG_VERDICT");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongComponentId_IsInvalid()
    {
        var result = BuildFromFixtures(componentId: "map_00_component_9999");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongBbox_IsInvalid()
    {
        var result = BuildFromFixtures(bboxMinX: 125);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongLotCount_IsInvalid()
    {
        var result = BuildFromFixtures(lotCount: 6);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongSlotCount_IsInvalid()
    {
        var result = BuildFromFixtures(slotCount: 6);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongFeatureCount_IsInvalid()
    {
        var result = BuildFromFixtures(featureCount: 14);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WriterReadyTrue_IsInvalid()
    {
        var result = BuildFromFixtures(writerReady: true);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_RuntimeValidTrue_IsInvalid()
    {
        var result = BuildFromFixtures(runtimeValid: true);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_MaterializedTrue_IsInvalid()
    {
        var result = BuildFromFixtures(materialized: true);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WrongPassedReviewChecks_IsInvalid()
    {
        var result = BuildFromFixtures(passedReviewChecks: 17);
        Assert.False(result.IsValid);
    }

    // -----------------------------------------------------------------------
    // Valid build tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_IsValid()
    {
        var result = BuildFromFixtures();
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void ValidFixture_NoErrors()
    {
        var result = BuildFromFixtures();
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidFixture_AllChecksPass()
    {
        var result = BuildFromFixtures();
        var failing = result.Checks.Where(c => c.CheckStatus != "PASS").Select(c => c.CheckId).ToList();
        Assert.Empty(failing);
    }

    [Fact]
    public void ValidFixture_ManifestCheckCount_Is17()
    {
        var result = BuildFromFixtures();
        Assert.Equal(17, result.ManifestCheckCount);
    }

    [Fact]
    public void ValidFixture_PassedManifestCheckCount_Is17()
    {
        var result = BuildFromFixtures();
        Assert.Equal(17, result.PassedManifestCheckCount);
    }

    [Fact]
    public void ValidFixture_FailedManifestCheckCount_Is0()
    {
        var result = BuildFromFixtures();
        Assert.Equal(0, result.FailedManifestCheckCount);
    }

    [Fact]
    public void ValidFixture_InputArtifactCount_Is8()
    {
        var result = BuildFromFixtures();
        Assert.Equal(8, result.InputArtifactCount);
    }

    [Fact]
    public void ValidFixture_HashedInputArtifactCount_Is8()
    {
        var result = BuildFromFixtures();
        Assert.Equal(8, result.HashedInputArtifactCount);
    }

    [Fact]
    public void ValidFixture_AllArtifacts_Sha256_NonEmpty()
    {
        var result = BuildFromFixtures();
        foreach (var a in result.InputArtifacts)
            Assert.False(string.IsNullOrEmpty(a.Sha256), $"SHA-256 empty for {a.ArtifactId}");
    }

    [Fact]
    public void ValidFixture_AllArtifacts_Sha256_Is64HexChars()
    {
        var result = BuildFromFixtures();
        foreach (var a in result.InputArtifacts)
            Assert.Equal(64, a.Sha256.Length);
    }

    [Fact]
    public void ValidFixture_LotCount_Is7()
    {
        var result = BuildFromFixtures();
        Assert.Equal(7, result.LotCount);
    }

    [Fact]
    public void ValidFixture_SlotCount_Is7()
    {
        var result = BuildFromFixtures();
        Assert.Equal(7, result.AcceptedBuildingSlotCount);
    }

    [Fact]
    public void ValidFixture_OverlayFeatureCount_Is15()
    {
        var result = BuildFromFixtures();
        Assert.Equal(15, result.OverlayFeatureCount);
    }

    [Fact]
    public void ValidFixture_PassedReviewCheckCount_Is18()
    {
        var result = BuildFromFixtures();
        Assert.Equal(18, result.PassedReviewCheckCount);
    }

    [Fact]
    public void ValidFixture_SourceDimensions_Is256x256()
    {
        var result = BuildFromFixtures();
        Assert.Equal("256x256", result.SourceDimensions);
    }

    [Fact]
    public void ValidFixture_WriterReady_IsFalse()
    {
        var result = BuildFromFixtures();
        Assert.False(result.WriterReady);
    }

    [Fact]
    public void ValidFixture_RuntimeValid_IsFalse()
    {
        var result = BuildFromFixtures();
        Assert.False(result.RuntimeValid);
    }

    [Fact]
    public void ValidFixture_Materialized_IsFalse()
    {
        var result = BuildFromFixtures();
        Assert.False(result.Materialized);
    }

    [Fact]
    public void ValidFixture_ApprovedForWriterExperiment_IsFalse()
    {
        var result = BuildFromFixtures();
        Assert.False(result.ApprovedForWriterExperiment);
    }

    [Fact]
    public void ValidFixture_WriterExperimentGateStatus_IsLocked()
    {
        var result = BuildFromFixtures();
        Assert.Equal("LOCKED_PENDING_OPERATOR_APPROVAL", result.WriterExperimentGateStatus);
    }

    [Fact]
    public void ValidFixture_Verdict_IsComplete()
    {
        var result = BuildFromFixtures();
        Assert.Equal("MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE", result.Verdict);
    }

    [Fact]
    public void ValidFixture_Check_AllArtifactsExist_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "ALL_REQUIRED_INPUT_ARTIFACTS_EXIST");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_AllArtifactsHashed_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "ALL_REQUIRED_INPUT_ARTIFACTS_HASHED");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_Map26aVerdictComplete_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "MAP26A_VERDICT_COMPLETE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_Map26bVerdictComplete_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "MAP26B_VERDICT_COMPLETE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_Map26cVerdictComplete_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "MAP26C_VERDICT_COMPLETE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_ReviewChecks18of18_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "MAP26C_REVIEW_CHECKS_18_OF_18_PASS");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_ComponentIdStable_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "COMPONENT_ID_STABLE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_BboxStable_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "COMPONENT_BBOX_STABLE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_LotCountStable7_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "LOT_COUNT_STABLE_7");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_SlotCountStable7_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "ACCEPTED_BUILDING_SLOT_COUNT_STABLE_7");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_FeatureCountStable15_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "OVERLAY_FEATURE_COUNT_STABLE_15");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_SourceDimensionsStable_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "SOURCE_DIMENSIONS_STABLE_256X256");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_WriterReadyFalse_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "WRITER_READY_FALSE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_RuntimeValidFalse_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "RUNTIME_VALID_FALSE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_MaterializedFalse_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "MATERIALIZED_FALSE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_ApprovedForWriterExperimentFalse_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "APPROVED_FOR_WRITER_EXPERIMENT_FALSE");
        Assert.Equal("PASS", c.CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_NoForbiddenOutputs_IsPass()
    {
        var result = BuildFromFixtures();
        var c = result.Checks.Single(x => x.CheckId == "NO_FORBIDDEN_OUTPUTS_CREATED");
        Assert.Equal("PASS", c.CheckStatus);
    }

    // -----------------------------------------------------------------------
    // Render tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderJson_ContainsVerdict()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var json    = builder.RenderJson(result);
        Assert.Contains("MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE", json);
    }

    [Fact]
    public void RenderJson_ContainsInputArtifacts()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var json    = builder.RenderJson(result);
        Assert.Contains("MAP26A_GEOMETRY_MVP_JSON", json);
    }

    [Fact]
    public void RenderMarkdown_ContainsWriterGate()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var md      = builder.RenderMarkdown(result);
        Assert.Contains("Writer Experiment Gate", md);
        Assert.Contains("LOCKED_PENDING_OPERATOR_APPROVAL", md);
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var md      = builder.RenderMarkdown(result);
        Assert.Contains("Claim Boundary", md);
    }

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var csv     = builder.RenderCsv(result);
        Assert.StartsWith("check_order,", csv);
    }

    [Fact]
    public void RenderCsv_Has17DataRows()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var csv     = builder.RenderCsv(result);
        var lines   = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(18, lines.Length); // 1 header + 17 data rows
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var summary = builder.RenderSummary(result);
        Assert.Contains("MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE", summary);
    }

    [Fact]
    public void RenderSummary_ContainsGateStatus()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var summary = builder.RenderSummary(result);
        Assert.Contains("LOCKED_PENDING_OPERATOR_APPROVAL", summary);
    }

    [Fact]
    public void RenderSummary_ApprovedForWriterExperiment_Is0()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder();
        var summary = builder.RenderSummary(result);
        Assert.Contains("approved_for_writer_experiment  : 0", summary);
    }
}
