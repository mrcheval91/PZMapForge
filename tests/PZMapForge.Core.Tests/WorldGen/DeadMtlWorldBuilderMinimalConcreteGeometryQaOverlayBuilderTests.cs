using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-qa-overlay-core", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture generation — deterministic, self-contained
    // -----------------------------------------------------------------------

    private string MakeFixturePng(int width = 256, int height = 256)
    {
        var path = Path.Combine(_tempDir, "fixture.png");
        using var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(Color.White);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private static DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult MakeFixtureMvpResult()
    {
        const string tileId      = "map_00";
        const string componentId = "map_00_component_0001";

        var lots  = new List<DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord>();
        var slots = new List<DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord>();

        int bboxMinX = 10, bboxMinY = 10, lotWidth = 14, lotHeight = 60;
        for (int i = 0; i < 7; i++)
        {
            int lotMinX = bboxMinX + i * lotWidth;
            int lotMaxX = lotMinX + lotWidth - 1;
            int lotMinY = bboxMinY;
            int lotMaxY = bboxMinY + lotHeight - 1;
            string lotId = $"{tileId}_comp0001_lot_{i + 1:D4}";

            lots.Add(new DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord
            {
                LotOrder        = i + 1,
                LotId           = lotId,
                ComponentOrder  = 1,
                ComponentId     = componentId,
                MinX            = lotMinX,
                MinY            = lotMinY,
                MaxX            = lotMaxX,
                MaxY            = lotMaxY,
                WidthPx         = lotWidth,
                HeightPx        = lotHeight,
                FrontageSide    = "NORTH",
                RearServiceSide = "EAST",
            });

            slots.Add(new DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord
            {
                SlotOrder      = i + 1,
                SlotId         = $"{tileId}_comp0001_slot_{i + 1:D4}",
                LotId          = lotId,
                LotOrder       = i + 1,
                ComponentOrder = 1,
                ComponentId    = componentId,
                MinX           = lotMinX + 2,
                MinY           = lotMinY + 3,
                MaxX           = lotMaxX - 2,
                MaxY           = lotMaxY - 3,
                WidthPx        = lotWidth - 4,
                HeightPx       = lotHeight - 6,
                SlotStatus     = "ACCEPTED",
                GeometryStatus = "CONCRETE_PIXEL_GEOMETRY_CREATED",
            });
        }

        int bboxMaxX = bboxMinX + 7 * lotWidth - 1;
        int bboxMaxY = bboxMinY + lotHeight - 1;

        var component = new DeadMtlWorldBuilderMinimalConcreteComponentGeometryRecord
        {
            ComponentOrder  = 1,
            ComponentId     = componentId,
            Intent          = "RESIDENTIAL_LOT_BLOCK",
            MinX            = bboxMinX,
            MinY            = bboxMinY,
            MaxX            = bboxMaxX,
            MaxY            = bboxMaxY,
            WidthPx         = bboxMaxX - bboxMinX + 1,
            HeightPx        = bboxMaxY - bboxMinY + 1,
            PixelCount      = (bboxMaxX - bboxMinX + 1) * (bboxMaxY - bboxMinY + 1),
            FrontageSide    = "NORTH",
            RearServiceSide = "EAST",
        };

        return new DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult
        {
            Format               = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1",
            TileId               = tileId,
            TargetComponentOrder = 1,
            GeometryMvpContract  = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpContract
            {
                TargetComponentId    = componentId,
                TargetIntent         = "RESIDENTIAL_LOT_BLOCK",
                AccessReadinessClass = "DUAL_ACCESS_CANDIDATE",
            },
            ComponentGeometry    = component,
            LotGeometry          = lots,
            BuildingSlotGeometry = slots,
            IsValid              = true,
            Verdict              = "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE",
        };
    }

    private string MakeFixtureMvpJson(DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult? overrideResult = null)
    {
        var path = Path.Combine(_tempDir, "fixture-mvp.json");
        var result = overrideResult ?? MakeFixtureMvpResult();
        File.WriteAllText(path, JsonSerializer.Serialize(result));
        return path;
    }

    private (string Png, string Json) MakeValidFixtures() =>
        (MakeFixturePng(), MakeFixtureMvpJson());

    private static DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult Build(
        string pngPath, string geometryMvpPath, string outputPngPath) =>
        new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilder()
            .Build(pngPath, geometryMvpPath, outputPngPath);

    private string OutputPngPath => Path.Combine(_tempDir, "overlay.png");

    // -----------------------------------------------------------------------
    // Missing input guards
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingPng_IsInvalid()
    {
        var json = MakeFixtureMvpJson();
        var result = Build("missing.png", json, OutputPngPath);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingGeometryMvp_IsInvalid()
    {
        var png = MakeFixturePng();
        var result = Build(png, "missing.json", OutputPngPath);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MalformedGeometryMvpJson_IsInvalid()
    {
        var png = MakeFixturePng();
        var badJsonPath = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(badJsonPath, "{ this is not valid json");
        var result = Build(png, badJsonPath, OutputPngPath);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_GeometryMvpMissingRecords_IsInvalid()
    {
        var png = MakeFixturePng();
        var emptyJsonPath = Path.Combine(_tempDir, "empty.json");
        File.WriteAllText(emptyJsonPath, "{ \"tile_id\": \"map_00\" }");
        var result = Build(png, emptyJsonPath, OutputPngPath);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("does not contain the expected"));
    }

    [Fact]
    public void Build_SourcePngWrongDimensions_IsInvalid()
    {
        var png  = MakeFixturePng(width: 128, height: 128);
        var json = MakeFixtureMvpJson();
        var result = Build(png, json, OutputPngPath);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("dimensions"));
    }

    [Fact]
    public void Build_RectangleOutsideSourceBounds_IsInvalid()
    {
        var png = MakeFixturePng();
        var fixtureResult = MakeFixtureMvpResult();
        fixtureResult.ComponentGeometry!.MaxX = 300;
        fixtureResult.ComponentGeometry!.WidthPx = 300 - fixtureResult.ComponentGeometry.MinX + 1;
        var json = MakeFixtureMvpJson(fixtureResult);
        var result = Build(png, json, OutputPngPath);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("outside source image bounds"));
    }

    [Fact]
    public void Build_NonPositiveWidth_IsInvalid()
    {
        var png = MakeFixturePng();
        var fixtureResult = MakeFixtureMvpResult();
        fixtureResult.ComponentGeometry!.WidthPx = 0;
        var json = MakeFixtureMvpJson(fixtureResult);
        var result = Build(png, json, OutputPngPath);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("non-positive width/height"));
    }

    // -----------------------------------------------------------------------
    // Valid build
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ValidInputs_IsValid()
    {
        var (png, json) = MakeValidFixtures();
        var result = Build(png, json, OutputPngPath);
        Assert.True(result.IsValid, $"Errors: {string.Join("; ", result.Errors)}");
    }

    [Fact]
    public void Build_ValidInputs_NoErrors()
    {
        var (png, json) = MakeValidFixtures();
        var result = Build(png, json, OutputPngPath);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Build_ValidInputs_WritesOverlayPng()
    {
        var (png, json) = MakeValidFixtures();
        var outPng = Path.Combine(_tempDir, "valid-overlay.png");
        Build(png, json, outPng);
        Assert.True(File.Exists(outPng), "overlay PNG not written");
    }

    // -----------------------------------------------------------------------
    // Verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void Result_Verdict_IsComplete()
    {
        var (png, json) = MakeValidFixtures();
        var result = Build(png, json, OutputPngPath);
        Assert.True(
            string.Equals(result.Verdict,
                "MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE",
                StringComparison.Ordinal),
            $"Actual: {result.Verdict}");
    }

    // -----------------------------------------------------------------------
    // Source dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void Result_SourceDimensions_Are256()
    {
        var (png, json) = MakeValidFixtures();
        var result = Build(png, json, OutputPngPath);
        Assert.Equal(256, result.SourceWidthPx);
        Assert.Equal(256, result.SourceHeightPx);
    }

    [Fact]
    public void Result_Scale_IsFour()
    {
        var (png, json) = MakeValidFixtures();
        Assert.Equal(4, Build(png, json, OutputPngPath).Scale);
    }

    // -----------------------------------------------------------------------
    // Target identity / contract passthrough
    // -----------------------------------------------------------------------

    [Fact]
    public void Result_TargetComponentId_IsComp0001()
    {
        var (png, json) = MakeValidFixtures();
        var result = Build(png, json, OutputPngPath);
        Assert.True(
            string.Equals(result.TargetComponentId, "map_00_component_0001", StringComparison.Ordinal),
            $"Actual: {result.TargetComponentId}");
    }

    [Fact]
    public void Result_Intent_IsResidentialLotBlock()
    {
        var (png, json) = MakeValidFixtures();
        var result = Build(png, json, OutputPngPath);
        Assert.True(
            string.Equals(result.Intent, "RESIDENTIAL_LOT_BLOCK", StringComparison.Ordinal),
            $"Actual: {result.Intent}");
    }

    [Fact]
    public void Result_AccessReadinessClass_IsDualAccess()
    {
        var (png, json) = MakeValidFixtures();
        var result = Build(png, json, OutputPngPath);
        Assert.True(
            string.Equals(result.AccessReadinessClass, "DUAL_ACCESS_CANDIDATE", StringComparison.Ordinal),
            $"Actual: {result.AccessReadinessClass}");
    }

    [Fact]
    public void Result_ComponentBbox_MatchesFixture()
    {
        var (png, json) = MakeValidFixtures();
        var bbox = Build(png, json, OutputPngPath).ComponentBbox!;
        Assert.Equal(10,  bbox.MinX);
        Assert.Equal(10,  bbox.MinY);
        Assert.Equal(107, bbox.MaxX);
        Assert.Equal(69,  bbox.MaxY);
        Assert.Equal(98,  bbox.WidthPx);
        Assert.Equal(60,  bbox.HeightPx);
    }

    // -----------------------------------------------------------------------
    // Feature breakdown
    // -----------------------------------------------------------------------

    [Fact]
    public void Result_LotCount_IsSeven()
    {
        var (png, json) = MakeValidFixtures();
        Assert.Equal(7, Build(png, json, OutputPngPath).LotCount);
    }

    [Fact]
    public void Result_AcceptedBuildingSlotCount_IsSeven()
    {
        var (png, json) = MakeValidFixtures();
        Assert.Equal(7, Build(png, json, OutputPngPath).AcceptedBuildingSlotCount);
    }

    [Fact]
    public void Result_OverlayFeatureCount_IsFifteen()
    {
        var (png, json) = MakeValidFixtures();
        Assert.Equal(15, Build(png, json, OutputPngPath).OverlayFeatureCount);
    }

    [Fact]
    public void Result_Features_HasOneComponentBbox()
    {
        var (png, json) = MakeValidFixtures();
        var features = Build(png, json, OutputPngPath).Features;
        Assert.Equal(1, features.Count(f => f.FeatureKind == "COMPONENT_BBOX"));
    }

    [Fact]
    public void Result_Features_HasSevenLotRectangles()
    {
        var (png, json) = MakeValidFixtures();
        var features = Build(png, json, OutputPngPath).Features;
        Assert.Equal(7, features.Count(f => f.FeatureKind == "LOT_RECTANGLE"));
    }

    [Fact]
    public void Result_Features_HasSevenBuildingSlotRectangles()
    {
        var (png, json) = MakeValidFixtures();
        var features = Build(png, json, OutputPngPath).Features;
        Assert.Equal(7, features.Count(f => f.FeatureKind == "BUILDING_SLOT_RECTANGLE"));
    }

    [Fact]
    public void Result_Features_AllHavePositiveDimensions()
    {
        var (png, json) = MakeValidFixtures();
        var features = Build(png, json, OutputPngPath).Features;
        Assert.All(features, f =>
        {
            Assert.True(f.Width  > 0, $"{f.FeatureId} width={f.Width}");
            Assert.True(f.Height > 0, $"{f.FeatureId} height={f.Height}");
        });
    }

    [Fact]
    public void Result_Features_AllOverlayCoordinatesAreScaledByFour()
    {
        var (png, json) = MakeValidFixtures();
        var features = Build(png, json, OutputPngPath).Features;
        Assert.All(features, f =>
        {
            Assert.Equal(f.X * 4, f.OverlayX);
            Assert.Equal(f.Y * 4, f.OverlayY);
            Assert.Equal(f.Width  * 4, f.OverlayWidth);
            Assert.Equal(f.Height * 4, f.OverlayHeight);
        });
    }

    [Fact]
    public void Result_Features_RightBottom_AreInclusive()
    {
        var (png, json) = MakeValidFixtures();
        var features = Build(png, json, OutputPngPath).Features;
        Assert.All(features, f =>
        {
            Assert.Equal(f.X + f.Width  - 1, f.Right);
            Assert.Equal(f.Y + f.Height - 1, f.Bottom);
        });
    }

    [Fact]
    public void Result_Features_OrderIsSequential()
    {
        var (png, json) = MakeValidFixtures();
        var features = Build(png, json, OutputPngPath).Features;
        var orders   = features.Select(f => f.FeatureOrder).ToList();
        Assert.Equal(Enumerable.Range(1, orders.Count), orders);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Result_WriterReady_IsFalse()
    {
        var (png, json) = MakeValidFixtures();
        Assert.False(Build(png, json, OutputPngPath).WriterReady);
    }

    [Fact]
    public void Result_RuntimeValid_IsFalse()
    {
        var (png, json) = MakeValidFixtures();
        Assert.False(Build(png, json, OutputPngPath).RuntimeValid);
    }

    [Fact]
    public void Result_Materialized_IsFalse()
    {
        var (png, json) = MakeValidFixtures();
        Assert.False(Build(png, json, OutputPngPath).Materialized);
    }

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderJson_ContainsVerdict()
    {
        var (png, json) = MakeValidFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilder();
        var result  = Build(png, json, OutputPngPath);
        var renderedJson = builder.RenderJson(result);
        Assert.Contains("MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE", renderedJson, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsHeader()
    {
        var (png, json) = MakeValidFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilder();
        var result  = Build(png, json, OutputPngPath);
        var md      = builder.RenderMarkdown(result);
        Assert.Contains("MAP-26B WorldBuilder Minimal Concrete Geometry QA Overlay", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_DoesNotClaimRuntimeOrMaterialization()
    {
        var (png, json) = MakeValidFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilder();
        var result  = Build(png, json, OutputPngPath);
        var md      = builder.RenderMarkdown(result);
        Assert.Contains("not runtime", md, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not materialization", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderCsv_HeaderIsStable()
    {
        var (png, json) = MakeValidFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilder();
        var result  = Build(png, json, OutputPngPath);
        var csv     = builder.RenderCsv(result);
        Assert.True(
            csv.StartsWith("feature_order,feature_kind,feature_id,label,x,y,width,height,right,bottom,scale,overlay_x,overlay_y,overlay_width,overlay_height,draw_order", StringComparison.Ordinal),
            $"Unexpected CSV header: {csv[..Math.Min(120, csv.Length)]}");
    }

    [Fact]
    public void RenderCsv_HasFifteenDataRows()
    {
        var (png, json) = MakeValidFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilder();
        var result  = Build(png, json, OutputPngPath);
        var csv     = builder.RenderCsv(result);
        var lines   = csv.Split('\n').Where(l => l.Trim().Length > 0).ToList();
        Assert.Equal(16, lines.Count); // 1 header + 15 features
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var (png, json) = MakeValidFixtures();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilder();
        var result  = Build(png, json, OutputPngPath);
        var summary = builder.RenderSummary(result);
        Assert.Contains("MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE", summary, StringComparison.Ordinal);
    }
}
