using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-qa-review-packet", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture generation — deterministic, self-contained
    // -----------------------------------------------------------------------

    private static DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult MakeFixtureMvpResult(
        string componentId = "map_00_component_0001")
    {
        const string tileId = "map_00";
        int bboxMinX = 10, bboxMinY = 10, lotWidth = 14, lotHeight = 60;

        var lots  = new List<DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord>();
        var slots = new List<DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord>();

        for (int i = 0; i < 7; i++)
        {
            int lotMinX = bboxMinX + i * lotWidth;
            int lotMaxX = lotMinX + lotWidth - 1;
            int lotMinY = bboxMinY;
            int lotMaxY = bboxMinY + lotHeight - 1;
            string lotId = $"{tileId}_comp0001_lot_{i + 1:D4}";

            lots.Add(new DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord
            {
                LotOrder = i + 1, LotId = lotId, ComponentOrder = 1, ComponentId = componentId,
                MinX = lotMinX, MinY = lotMinY, MaxX = lotMaxX, MaxY = lotMaxY,
                WidthPx = lotWidth, HeightPx = lotHeight,
            });
            slots.Add(new DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord
            {
                SlotOrder = i + 1, SlotId = $"{tileId}_comp0001_slot_{i + 1:D4}",
                LotId = lotId, LotOrder = i + 1, ComponentOrder = 1, ComponentId = componentId,
                MinX = lotMinX + 2, MinY = lotMinY + 3, MaxX = lotMaxX - 2, MaxY = lotMaxY - 3,
                WidthPx = lotWidth - 4, HeightPx = lotHeight - 6,
                SlotStatus = "ACCEPTED", GeometryStatus = "CONCRETE_PIXEL_GEOMETRY_CREATED",
            });
        }

        int bboxMaxX = bboxMinX + 7 * lotWidth - 1;
        int bboxMaxY = bboxMinY + lotHeight - 1;

        return new DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult
        {
            TileId               = tileId,
            TargetComponentOrder = 1,
            GeometryMvpContract  = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpContract
            {
                TargetComponentId    = componentId,
                TargetIntent         = "RESIDENTIAL_LOT_BLOCK",
                AccessReadinessClass = "DUAL_ACCESS_CANDIDATE",
            },
            ComponentGeometry = new DeadMtlWorldBuilderMinimalConcreteComponentGeometryRecord
            {
                ComponentOrder = 1, ComponentId = componentId, Intent = "RESIDENTIAL_LOT_BLOCK",
                MinX = bboxMinX, MinY = bboxMinY, MaxX = bboxMaxX, MaxY = bboxMaxY,
                WidthPx = bboxMaxX - bboxMinX + 1, HeightPx = bboxMaxY - bboxMinY + 1,
                PixelCount = (bboxMaxX - bboxMinX + 1) * (bboxMaxY - bboxMinY + 1),
            },
            LotGeometry          = lots,
            BuildingSlotGeometry = slots,
            IsValid  = true,
            Verdict  = "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE",
        };
    }

    private static DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult MakeFixtureOverlayResult(
        string mapId = "map_00",
        string componentId = "map_00_component_0001",
        bool writerReady = false,
        bool runtimeValid = false,
        bool materialized = false,
        int? overrideLotCount = null,
        int? overrideSlotCount = null,
        int? overrideFeatureCount = null,
        DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBbox? overrideBbox = null)
    {
        int bboxMinX = 10, bboxMinY = 10, lotWidth = 14, lotHeight = 60;
        var features = new List<DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature>();

        int bboxMaxX = bboxMinX + 7 * lotWidth - 1;
        int bboxMaxY = bboxMinY + lotHeight - 1;

        // COMPONENT_BBOX
        features.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature
        {
            FeatureOrder = 1, FeatureKind = "COMPONENT_BBOX",
            FeatureId = componentId, X = bboxMinX, Y = bboxMinY,
            Width = bboxMaxX - bboxMinX + 1, Height = bboxMaxY - bboxMinY + 1,
            Right = bboxMaxX, Bottom = bboxMaxY, Scale = 4, DrawOrder = 1,
            OverlayX = bboxMinX * 4, OverlayY = bboxMinY * 4,
            OverlayWidth = (bboxMaxX - bboxMinX + 1) * 4, OverlayHeight = (bboxMaxY - bboxMinY + 1) * 4,
        });

        for (int i = 0; i < 7; i++)
        {
            int lotMinX = bboxMinX + i * lotWidth;
            int lotMaxX = lotMinX + lotWidth - 1;
            features.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature
            {
                FeatureOrder = i + 2, FeatureKind = "LOT_RECTANGLE",
                FeatureId = $"map_00_comp0001_lot_{i + 1:D4}",
                X = lotMinX, Y = bboxMinY, Width = lotWidth, Height = lotHeight,
                Right = lotMaxX, Bottom = bboxMaxY, Scale = 4, DrawOrder = 2,
                OverlayX = lotMinX * 4, OverlayY = bboxMinY * 4,
                OverlayWidth = lotWidth * 4, OverlayHeight = lotHeight * 4,
            });
        }

        for (int i = 0; i < 7; i++)
        {
            int lotMinX = bboxMinX + i * lotWidth;
            int lotMaxX = lotMinX + lotWidth - 1;
            int slotMinX = lotMinX + 2, slotMaxX = lotMaxX - 2;
            int slotMinY = bboxMinY + 3, slotMaxY = bboxMaxY - 3;
            features.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature
            {
                FeatureOrder = i + 9, FeatureKind = "BUILDING_SLOT_RECTANGLE",
                FeatureId = $"map_00_comp0001_slot_{i + 1:D4}",
                X = slotMinX, Y = slotMinY, Width = slotMaxX - slotMinX + 1, Height = slotMaxY - slotMinY + 1,
                Right = slotMaxX, Bottom = slotMaxY, Scale = 4, DrawOrder = 3,
                OverlayX = slotMinX * 4, OverlayY = slotMinY * 4,
                OverlayWidth = (slotMaxX - slotMinX + 1) * 4, OverlayHeight = (slotMaxY - slotMinY + 1) * 4,
            });
        }

        return new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult
        {
            MapId                    = mapId,
            TargetComponentId        = componentId,
            TargetComponentOrder     = 1,
            Intent                   = "RESIDENTIAL_LOT_BLOCK",
            AccessReadinessClass     = "DUAL_ACCESS_CANDIDATE",
            SourceWidthPx            = 256,
            SourceHeightPx           = 256,
            Scale                    = 4,
            ComponentBbox            = overrideBbox ?? new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBbox
            {
                MinX = bboxMinX, MinY = bboxMinY, MaxX = bboxMaxX, MaxY = bboxMaxY,
                WidthPx = bboxMaxX - bboxMinX + 1, HeightPx = bboxMaxY - bboxMinY + 1,
            },
            LotCount                 = overrideLotCount ?? 7,
            AcceptedBuildingSlotCount = overrideSlotCount ?? 7,
            OverlayFeatureCount      = overrideFeatureCount ?? features.Count,
            WriterReady              = writerReady,
            RuntimeValid             = runtimeValid,
            Materialized             = materialized,
            Features                 = features,
            IsValid                  = true,
            Verdict                  = "MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE",
        };
    }

    private static string MakeCsvFromOverlay(
        DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult overlay)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("feature_order,feature_kind,feature_id,label,x,y,width,height,right,bottom,scale,overlay_x,overlay_y,overlay_width,overlay_height,draw_order");
        foreach (var f in overlay.Features)
            sb.AppendLine($"{f.FeatureOrder},{f.FeatureKind},{f.FeatureId},{f.FeatureId},{f.X},{f.Y},{f.Width},{f.Height},{f.Right},{f.Bottom},{f.Scale},{f.OverlayX},{f.OverlayY},{f.OverlayWidth},{f.OverlayHeight},{f.DrawOrder}");
        return sb.ToString();
    }

    private (string MvpJson, string OverlayJson, string OverlayCsv, string OverlayPng) MakeValidFixtures(
        DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult? mvpOverride = null,
        DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult? overlayOverride = null)
    {
        var mvp     = mvpOverride     ?? MakeFixtureMvpResult();
        var overlay = overlayOverride ?? MakeFixtureOverlayResult();

        var opts = new JsonSerializerOptions { WriteIndented = true };
        var mvpJsonPath     = Path.Combine(_tempDir, "mvp.json");
        var overlayJsonPath = Path.Combine(_tempDir, "overlay.json");
        var overlayCsvPath  = Path.Combine(_tempDir, "overlay.csv");
        var overlayPngPath  = Path.Combine(_tempDir, "overlay.png");

        File.WriteAllText(mvpJsonPath,     JsonSerializer.Serialize(mvp, opts));
        File.WriteAllText(overlayJsonPath, JsonSerializer.Serialize(overlay, opts));
        File.WriteAllText(overlayCsvPath,  MakeCsvFromOverlay(overlay));
        File.WriteAllBytes(overlayPngPath, new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // stub — exists

        return (mvpJsonPath, overlayJsonPath, overlayCsvPath, overlayPngPath);
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult BuildFromFixtures(
        DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult? mvpOverride = null,
        DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult? overlayOverride = null)
    {
        var (mvpJson, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures(mvpOverride, overlayOverride);
        return new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder()
            .Build(mvpJson, overlayJson, overlayCsv, overlayPng);
    }

    // -----------------------------------------------------------------------
    // Guard tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MvpJsonMissing_IsInvalid()
    {
        var (_, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder()
            .Build(Path.Combine(_tempDir, "missing.json"), overlayJson, overlayCsv, overlayPng);
        Assert.False(result.IsValid);
        Assert.Contains("MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_INVALID", result.Verdict);
    }

    [Fact]
    public void Build_OverlayJsonMissing_IsInvalid()
    {
        var (mvpJson, _, overlayCsv, overlayPng) = MakeValidFixtures();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder()
            .Build(mvpJson, Path.Combine(_tempDir, "missing.json"), overlayCsv, overlayPng);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_OverlayCsvMissing_IsInvalid()
    {
        var (mvpJson, overlayJson, _, overlayPng) = MakeValidFixtures();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder()
            .Build(mvpJson, overlayJson, Path.Combine(_tempDir, "missing.csv"), overlayPng);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_OverlayPngMissing_IsInvalid()
    {
        var (mvpJson, overlayJson, overlayCsv, _) = MakeValidFixtures();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder()
            .Build(mvpJson, overlayJson, overlayCsv, Path.Combine(_tempDir, "missing.png"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_MvpJsonMalformed_IsInvalid()
    {
        var (_, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var badPath = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(badPath, "{ not json }}}");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder()
            .Build(badPath, overlayJson, overlayCsv, overlayPng);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_OverlayJsonMalformed_IsInvalid()
    {
        var (mvpJson, _, overlayCsv, overlayPng) = MakeValidFixtures();
        var badPath = Path.Combine(_tempDir, "bad-overlay.json");
        File.WriteAllText(badPath, "not valid json");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder()
            .Build(mvpJson, badPath, overlayCsv, overlayPng);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_ComponentIdMismatch_IsInvalid()
    {
        var mvp     = MakeFixtureMvpResult();
        var overlay = MakeFixtureOverlayResult(componentId: "map_00_component_XXXX");
        var result  = BuildFromFixtures(mvp, overlay);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "COMPONENT_ID_MATCHES" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_BboxMismatch_IsInvalid()
    {
        var mvp     = MakeFixtureMvpResult();
        var badBbox = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBbox
            { MinX = 0, MinY = 0, MaxX = 50, MaxY = 50, WidthPx = 51, HeightPx = 51 };
        var overlay = MakeFixtureOverlayResult(overrideBbox: badBbox);
        var result  = BuildFromFixtures(mvp, overlay);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "COMPONENT_BBOX_MATCHES" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_LotCountWrong_IsInvalid()
    {
        var overlay = MakeFixtureOverlayResult(overrideLotCount: 5);
        var result  = BuildFromFixtures(overlayOverride: overlay);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "LOT_COUNT_IS_7" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_SlotCountWrong_IsInvalid()
    {
        var overlay = MakeFixtureOverlayResult(overrideSlotCount: 3);
        var result  = BuildFromFixtures(overlayOverride: overlay);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "ACCEPTED_BUILDING_SLOT_COUNT_IS_7" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_WriterReadyTrue_IsInvalid()
    {
        var overlay = MakeFixtureOverlayResult(writerReady: true);
        var result  = BuildFromFixtures(overlayOverride: overlay);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "WRITER_READY_FALSE" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_RuntimeValidTrue_IsInvalid()
    {
        var overlay = MakeFixtureOverlayResult(runtimeValid: true);
        var result  = BuildFromFixtures(overlayOverride: overlay);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "RUNTIME_VALID_FALSE" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_MaterializedTrue_IsInvalid()
    {
        var overlay = MakeFixtureOverlayResult(materialized: true);
        var result  = BuildFromFixtures(overlayOverride: overlay);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "MATERIALIZED_FALSE" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // Valid build
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ValidFixtures_IsValid()
    {
        var result = BuildFromFixtures();
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Build_ValidFixtures_NoErrors()
    {
        var result = BuildFromFixtures();
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Build_ValidFixtures_AllChecksPassed()
    {
        var result = BuildFromFixtures();
        Assert.All(result.Checks, c => Assert.Equal("PASS", c.CheckStatus));
    }

    [Fact]
    public void Build_ValidFixtures_Verdict()
    {
        var result = BuildFromFixtures();
        Assert.Equal("MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE", result.Verdict);
    }

    [Fact]
    public void Build_ValidFixtures_ReviewCheckCount_Is18()
    {
        var result = BuildFromFixtures();
        Assert.Equal(18, result.ReviewCheckCount);
    }

    [Fact]
    public void Build_ValidFixtures_PassedCheckCount_Is18()
    {
        var result = BuildFromFixtures();
        Assert.Equal(18, result.PassedReviewCheckCount);
    }

    [Fact]
    public void Build_ValidFixtures_FailedCheckCount_Is0()
    {
        var result = BuildFromFixtures();
        Assert.Equal(0, result.FailedReviewCheckCount);
    }

    [Fact]
    public void Build_ValidFixtures_LotCount_Is7()
    {
        var result = BuildFromFixtures();
        Assert.Equal(7, result.LotCount);
    }

    [Fact]
    public void Build_ValidFixtures_AcceptedSlotCount_Is7()
    {
        var result = BuildFromFixtures();
        Assert.Equal(7, result.AcceptedBuildingSlotCount);
    }

    [Fact]
    public void Build_ValidFixtures_OverlayFeatureCount_Is15()
    {
        var result = BuildFromFixtures();
        Assert.Equal(15, result.OverlayFeatureCount);
    }

    [Fact]
    public void Build_ValidFixtures_CsvFeatureCount_Is15()
    {
        var result = BuildFromFixtures();
        Assert.Equal(15, result.CsvFeatureCount);
    }

    [Fact]
    public void Build_ValidFixtures_WriterReadyFalse()
    {
        var result = BuildFromFixtures();
        Assert.False(result.WriterReady);
    }

    [Fact]
    public void Build_ValidFixtures_RuntimeValidFalse()
    {
        var result = BuildFromFixtures();
        Assert.False(result.RuntimeValid);
    }

    [Fact]
    public void Build_ValidFixtures_MaterializedFalse()
    {
        var result = BuildFromFixtures();
        Assert.False(result.Materialized);
    }

    [Fact]
    public void Build_ValidFixtures_FeatureBreakdown_Is_1_7_7()
    {
        var result   = BuildFromFixtures();
        var bboxCount = result.FeatureBreakdown.First(f => f.FeatureKind == "COMPONENT_BBOX").Count;
        var lotCount  = result.FeatureBreakdown.First(f => f.FeatureKind == "LOT_RECTANGLE").Count;
        var slotCount = result.FeatureBreakdown.First(f => f.FeatureKind == "BUILDING_SLOT_RECTANGLE").Count;
        Assert.Equal(1, bboxCount);
        Assert.Equal(7, lotCount);
        Assert.Equal(7, slotCount);
    }

    [Fact]
    public void Build_ValidFixtures_SourceDimensions()
    {
        var result = BuildFromFixtures();
        Assert.Equal("256x256", result.SourceDimensions);
    }

    [Fact]
    public void Build_ValidFixtures_InclusiveSemanticsCheck_Passes()
    {
        var result = BuildFromFixtures();
        var check = result.Checks.First(c => c.CheckId == "INCLUSIVE_RIGHT_BOTTOM_SEMANTICS");
        Assert.Equal("PASS", check.CheckStatus);
    }

    [Fact]
    public void Build_ValidFixtures_BoundsCheck_Passes()
    {
        var result = BuildFromFixtures();
        var check = result.Checks.First(c => c.CheckId == "RECTANGLES_INSIDE_256_BOUNDS");
        Assert.Equal("PASS", check.CheckStatus);
    }

    // -----------------------------------------------------------------------
    // Render tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderJson_ContainsVerdict()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder();
        var json    = builder.RenderJson(result);
        Assert.Contains("MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE", json);
    }

    [Fact]
    public void RenderMarkdown_ContainsHeader()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder();
        var md      = builder.RenderMarkdown(result);
        Assert.Contains("# MAP-26C WorldBuilder Minimal Concrete Geometry QA Review Packet", md);
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder();
        var md      = builder.RenderMarkdown(result);
        Assert.Contains("does not create geometry", md);
        Assert.Contains("does not write lotpack", md);
        Assert.Contains("does not prove runtime validity", md);
        Assert.Contains("does not prove writer readiness", md);
    }

    [Fact]
    public void RenderCsv_HeaderAndRowCount()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder();
        var lines   = builder.RenderCsv(result)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal("check_order,check_id,check_label,check_status,expected,actual,details", lines[0]);
        Assert.Equal(19, lines.Length); // 1 header + 18 check rows
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder();
        var summary = builder.RenderSummary(result);
        Assert.Contains("MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE", summary);
    }

    [Fact]
    public void RenderSummary_ContainsReviewCheckCount()
    {
        var result  = BuildFromFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder();
        var summary = builder.RenderSummary(result);
        Assert.Contains("review_checks           : 18", summary);
        Assert.Contains("passed_review_checks    : 18", summary);
        Assert.Contains("failed_review_checks    : 0", summary);
    }
}
