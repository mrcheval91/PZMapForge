using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderParcelBuildingFootprintCandidatesBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map30a-core-test.local", Guid.NewGuid().ToString());

    public DeadMtlWorldBuilderParcelBuildingFootprintCandidatesBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string MakePolicyJson() =>
        """
        {
          "policy_version": "MAP30A_TEST_V1",
          "default_sector": "DEFAULT",
          "policies": [
            {
              "parcel_class": "BLUE_RESIDENTIAL",
              "neighborhood_sector": "DEFAULT",
              "min_lot_area_tiles": 96,
              "front_setback_tiles": 2,
              "rear_setback_tiles": 3,
              "side_setback_tiles": 1,
              "min_footprint_width_tiles": 6,
              "min_footprint_depth_tiles": 6,
              "max_lot_coverage_ratio": 0.60,
              "preferred_footprint_kind": "RESIDENTIAL_RECTANGLE"
            },
            {
              "parcel_class": "RED_RESIDENTIAL_OR_COMMERCIAL",
              "neighborhood_sector": "DEFAULT",
              "min_lot_area_tiles": 120,
              "front_setback_tiles": 0,
              "rear_setback_tiles": 2,
              "side_setback_tiles": 0,
              "min_footprint_width_tiles": 6,
              "min_footprint_depth_tiles": 6,
              "max_lot_coverage_ratio": 0.85,
              "preferred_footprint_kind": "COMMERCIAL_RECTANGLE"
            }
          ]
        }
        """;

    private string MakeLotFillJson(
        string compId, string parcelClass, string lotId,
        string dir, int x1, int y1, int x2, int y2,
        int sr = 200, int sg = 168, int sb = 120)
    {
        int w  = x2 - x1 + 1;
        int h  = y2 - y1 + 1;
        int tc = w * h;
        return $$"""
        {
          "lot_sizing_policy_version": "MAP29C_V1",
          "components": [
            { "component_id": "{{compId}}", "parcel_class": "{{parcelClass}}" }
          ],
          "lots": [
            {
              "component_id": "{{compId}}",
              "lot_id": "{{lotId}}",
              "frontage_direction": "{{dir}}",
              "x1": {{x1}}, "y1": {{y1}}, "x2": {{x2}}, "y2": {{y2}},
              "width": {{w}}, "height": {{h}}, "tile_count": {{tc}},
              "shade_r": {{sr}}, "shade_g": {{sg}}, "shade_b": {{sb}}
            }
          ]
        }
        """;
    }

    private string MakeMultiLotFillJson(
        string compId, string parcelClass,
        (string lotId, string dir, int x1, int y1, int x2, int y2)[] lots)
    {
        var lotLines = string.Join(",\n", lots.Select(l =>
        {
            int w = l.x2 - l.x1 + 1, h = l.y2 - l.y1 + 1, tc = w * h;
            return $$$"""
                {{
                  "component_id": "{{{compId}}}",
                  "lot_id": "{{{l.lotId}}}",
                  "frontage_direction": "{{{l.dir}}}",
                  "x1": {{{l.x1}}}, "y1": {{{l.y1}}}, "x2": {{{l.x2}}}, "y2": {{{l.y2}}},
                  "width": {{{w}}}, "height": {{{h}}}, "tile_count": {{{tc}}},
                  "shade_r": 200, "shade_g": 168, "shade_b": 120
                }}
            """;
        }));
        return $$"""
        {
          "lot_sizing_policy_version": "MAP29C_V1",
          "components": [
            { "component_id": "{{compId}}", "parcel_class": "{{parcelClass}}" }
          ],
          "lots": [ {{lotLines}} ]
        }
        """;
    }

    private (string lotFillPath, string policyPath) WriteFixtures(string lotFillJson, string? policyJson = null)
    {
        var lotPath = Path.Combine(_tempDir, "lot-fill.json");
        var polPath = Path.Combine(_tempDir, "policy.json");
        File.WriteAllText(lotPath, lotFillJson);
        File.WriteAllText(polPath, policyJson ?? MakePolicyJson());
        return (lotPath, polPath);
    }

    private DeadMtlWorldBuilderParcelBuildingFootprintCandidatesBuilder NewBuilder() =>
        new DeadMtlWorldBuilderParcelBuildingFootprintCandidatesBuilder();

    // -----------------------------------------------------------------------
    // 1. NORTH blue lot — FpY1 == lotY1 + front_setback (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void NorthBlueLot_FootprintY1_EqualsFrontSetback()
    {
        // 14×27 lot: tile_count=378. setbacks: front=2 rear=3 side=1
        // raw fp: x1=lotX1+1=1, x2=lotX2-1=12, y1=lotY1+2=2, y2=lotY2-3=23
        // fpWidth=12, fpDepth=22, fpArea=264, coverage=0.699 > 0.60
        // maxArea=floor(378*0.60)=226, maxDepth=226/12=18
        // clipped: y2=y1+18-1=2+17=19; fpDepth=18 ≥ 6 OK
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_BLUE_N", "BLUE_RESIDENTIAL", "LOT_N",
                "NORTH", x1: 0, y1: 0, x2: 13, y2: 26));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Empty(r.Errors);
        Assert.Equal(1, r.FootprintCount);
        var fp = r.Footprints[0];
        Assert.Equal(0 + 2, fp.FpY1);  // lotY1 + front_setback
    }

    // -----------------------------------------------------------------------
    // 2. EAST blue lot — FpX2 == lotX2 - front_setback (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void EastBlueLot_FootprintX2_EqualsFrontSetback()
    {
        // 27×14 lot: tile_count=378. EAST: front=right side of X
        // raw fp: x1=lotX1+rear=0+3=3, x2=lotX2-front=26-2=24, y1=lotY1+side=0+1=1, y2=lotY2-side=13-1=12
        // fpWidth (EW)=y2-y1+1=12, fpDepth=x2-x1+1=22, fpArea=264, coverage=0.699>0.60
        // maxArea=226, maxDepth=226/12=18; EAST: x1=x2-maxDepth+1=24-17=7
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_BLUE_E", "BLUE_RESIDENTIAL", "LOT_E",
                "EAST", x1: 0, y1: 0, x2: 26, y2: 13));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Empty(r.Errors);
        Assert.Equal(1, r.FootprintCount);
        var fp = r.Footprints[0];
        Assert.Equal(26 - 2, fp.FpX2);  // lotX2 - front_setback
    }

    // -----------------------------------------------------------------------
    // 3. RED lot NORTH — FpY1 == lotY1 (zero front setback)
    // -----------------------------------------------------------------------

    [Fact]
    public void RedNorthLot_FpY1_EqualsLotY1_ZeroFrontSetback()
    {
        // 26×11 lot: tile_count=286 >= 120 (red min). RED front=0 rear=2 side=0
        // raw fp: x1=0, x2=25, y1=0, y2=8 (=11-2-1)
        // fpArea=26×9=234, coverage=234/286=0.818 ≤ 0.85 → no clip
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("MAP29B_RED_COMP", "RED_RESIDENTIAL_OR_COMMERCIAL", "LOT_RED_N",
                "NORTH", x1: 0, y1: 0, x2: 25, y2: 10));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Empty(r.Errors);
        Assert.Equal(1, r.FootprintCount);
        Assert.Equal(0, r.Footprints[0].FpY1);  // zero front setback
    }

    // -----------------------------------------------------------------------
    // 4. Too-small lot → skipped, FootprintCount == 0
    // -----------------------------------------------------------------------

    [Fact]
    public void TooSmallLot_IsSkipped()
    {
        // 9×9=81 tiles < min_lot_area_tiles 96 for BLUE
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_TINY", "BLUE_RESIDENTIAL", "LOT_TINY",
                "NORTH", x1: 0, y1: 0, x2: 8, y2: 8));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Equal(0, r.FootprintCount);
        Assert.Equal(1, r.SkippedLotCount);
        Assert.Contains("min_lot_area_tiles", r.SkippedLots[0].Reason);
    }

    // -----------------------------------------------------------------------
    // 5. Coverage ratio is always ≤ max
    // -----------------------------------------------------------------------

    [Fact]
    public void AllFootprints_CoverageRatio_WithinPolicy()
    {
        // Use same 14×27 NORTH blue lot — after clip coverage should be ≤ 0.60
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_COV", "BLUE_RESIDENTIAL", "LOT_COV",
                "NORTH", x1: 0, y1: 0, x2: 13, y2: 26));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Equal(1, r.FootprintCount);
        Assert.True(r.Footprints[0].CoverageRatio <= 0.60 + 0.001);
    }

    // -----------------------------------------------------------------------
    // 6. All footprints are inside their lot bounds
    // -----------------------------------------------------------------------

    [Fact]
    public void AllFootprints_InsideLotBounds()
    {
        var lots = new[]
        {
            ("LOT_A", "NORTH", 0,  0, 13, 26),
            ("LOT_B", "SOUTH", 0, 27, 13, 53),
        };
        var (lf, pol) = WriteFixtures(
            MakeMultiLotFillJson("COMP_MULTI", "BLUE_RESIDENTIAL", lots));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        foreach (var fp in r.Footprints)
        {
            Assert.True(fp.FpX1 >= fp.LotX1, $"{fp.LotId}: FpX1 outside lot");
            Assert.True(fp.FpY1 >= fp.LotY1, $"{fp.LotId}: FpY1 outside lot");
            Assert.True(fp.FpX2 <= fp.LotX2, $"{fp.LotId}: FpX2 outside lot");
            Assert.True(fp.FpY2 <= fp.LotY2, $"{fp.LotId}: FpY2 outside lot");
        }
    }

    // -----------------------------------------------------------------------
    // 7. Policy file loads (r.PolicyLoaded == true)
    // -----------------------------------------------------------------------

    [Fact]
    public void PolicyFile_Loads_PolicyLoadedTrue()
    {
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_POL", "BLUE_RESIDENTIAL", "LOT_POL",
                "NORTH", x1: 0, y1: 0, x2: 13, y2: 26));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.True(r.PolicyLoaded);
        Assert.Equal("MAP30A_TEST_V1", r.PolicyVersion);
        Assert.Equal(2, r.PolicyEntryCount);
    }

    // -----------------------------------------------------------------------
    // 8. Invalid policy JSON → result has errors
    // -----------------------------------------------------------------------

    [Fact]
    public void InvalidPolicyJson_ResultHasError()
    {
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_BAD", "BLUE_RESIDENTIAL", "LOT_BAD",
                "NORTH", x1: 0, y1: 0, x2: 13, y2: 26),
            policyJson: "{ this is not valid json }");
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.NotEmpty(r.Errors);
        Assert.False(r.IsValid);
    }

    // -----------------------------------------------------------------------
    // 9. Claim boundary flags
    // -----------------------------------------------------------------------

    [Fact]
    public void ClaimBoundaryFlags_AreCorrect()
    {
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_CB", "BLUE_RESIDENTIAL", "LOT_CB",
                "NORTH", x1: 0, y1: 0, x2: 13, y2: 26));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.True(r.SandboxOnly);
        Assert.False(r.WriterReady);
        Assert.False(r.RuntimeValid);
        Assert.False(r.Materialized);
        Assert.False(r.RuntimeProofClaimed);
        Assert.False(r.PublicPlayablePackagingClaimed);
    }

    // -----------------------------------------------------------------------
    // 10. Missing lot-fill JSON → early exit with error
    // -----------------------------------------------------------------------

    [Fact]
    public void MissingLotFillJson_ReturnsError()
    {
        var pol = Path.Combine(_tempDir, "policy.json");
        File.WriteAllText(pol, MakePolicyJson());
        var r = NewBuilder().Build(
            Path.Combine(_tempDir, "nonexistent.json"), pol, _tempDir);
        Assert.NotEmpty(r.Errors);
        Assert.False(r.IsValid);
    }

    // -----------------------------------------------------------------------
    // 11. InferParcelClass fallback: MAP29B_RED_* prefix → RED
    // -----------------------------------------------------------------------

    [Fact]
    public void InferParcelClass_RedPrefix_YieldsRedPolicy()
    {
        // Component has no explicit parcel_class in JSON — rely on compId prefix inference
        // Use a lot big enough for RED (≥120): 12×13=156
        var json = """
        {
          "lot_sizing_policy_version": "MAP29C_V1",
          "components": [
            { "component_id": "MAP29B_RED_INFER" }
          ],
          "lots": [
            {
              "component_id": "MAP29B_RED_INFER",
              "lot_id": "LOT_INFER_RED",
              "frontage_direction": "NORTH",
              "x1": 0, "y1": 0, "x2": 11, "y2": 12,
              "width": 12, "height": 13, "tile_count": 156,
              "shade_r": 166, "shade_g": 0, "shade_b": 0
            }
          ]
        }
        """;
        var (lf, pol) = WriteFixtures(json);
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Equal(1, r.FootprintCount);
        Assert.Equal("RED_RESIDENTIAL_OR_COMMERCIAL", r.Footprints[0].ParcelClass);
        Assert.Equal("COMMERCIAL_RECTANGLE", r.Footprints[0].FootprintKind);
    }

    // -----------------------------------------------------------------------
    // 12. SOUTH lot — rear setback shrinks from north (FpY1 pushed down)
    // -----------------------------------------------------------------------

    [Fact]
    public void SouthBlueLot_FootprintY2_EqualsFrontSetback()
    {
        // 14×27 SOUTH: front=bottom, rear=top
        // raw fp: x1=1, x2=12, y1=lotY1+rear=0+3=3, y2=lotY2-front=26-2=24
        // fpWidth=12, fpDepth=22, coverage same as NORTH → clip to 18 → y1=y2-18+1=24-17=7
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_BLUE_S", "BLUE_RESIDENTIAL", "LOT_S",
                "SOUTH", x1: 0, y1: 0, x2: 13, y2: 26));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Equal(1, r.FootprintCount);
        var fp = r.Footprints[0];
        Assert.Equal(26 - 2, fp.FpY2);  // lotY2 - front_setback
    }

    // -----------------------------------------------------------------------
    // 13. No footprint output PNG uses cyan (40,192,192) or pure black
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputPng_NoCyanOrPureBlack()
    {
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_PNG", "BLUE_RESIDENTIAL", "LOT_PNG",
                "NORTH", x1: 0, y1: 0, x2: 13, y2: 26));
        var r      = NewBuilder().Build(lf, pol, _tempDir);
        var builder = new DeadMtlWorldBuilderParcelBuildingFootprintCandidatesBuilder();
        _ = builder.Build(lf, pol, _tempDir); // warmup for source cache
        var bytes = builder.RenderOutputPngBytes(r);

        var hasCyanOrBlack = false;
        using var ms  = new System.IO.MemoryStream(bytes);
        using var bmp = new System.Drawing.Bitmap(ms);
        for (int x = 0; x < bmp.Width && !hasCyanOrBlack; x++)
            for (int y = 0; y < bmp.Height && !hasCyanOrBlack; y++)
            {
                var c = bmp.GetPixel(x, y);
                if ((c.R == 40 && c.G == 192 && c.B == 192) ||
                    (c.R == 0  && c.G == 0   && c.B == 0))
                    hasCyanOrBlack = true;
            }
        Assert.False(hasCyanOrBlack);
    }

    // -----------------------------------------------------------------------
    // 14. WEST lot — FpX2 == lotX2 - rear_setback after clip
    // -----------------------------------------------------------------------

    [Fact]
    public void WestBlueLot_SetbacksApplied()
    {
        // 27×14 WEST: front=left (X1 side), rear=right (X2 side)
        // raw fp: x1=lotX1+front=0+2=2, x2=lotX2-rear=26-3=23, y1=0+1=1, y2=13-1=12
        // fpWidth (EW)=y2-y1+1=12, fpDepth=x2-x1+1=22, coverage=0.699>0.60 → clip
        // maxArea=226, maxDepth=226/12=18; WEST: x2=x1+18-1=2+17=19
        var (lf, pol) = WriteFixtures(
            MakeLotFillJson("COMP_BLUE_W", "BLUE_RESIDENTIAL", "LOT_W",
                "WEST", x1: 0, y1: 0, x2: 26, y2: 13));
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Equal(1, r.FootprintCount);
        var fp = r.Footprints[0];
        Assert.Equal(0 + 2, fp.FpX1);  // lotX1 + front_setback
    }

    // -----------------------------------------------------------------------
    // MAP-30B fixture helper: one valid lot + one skipped lot
    // Valid: 14×27 NORTH BLUE at (0,0)→(13,26), shade (200,168,120)
    //   Footprint: x1=1,y1=2,x2=12,y2=19 (after clip)  shade+45=(245,213,165)
    // Skipped: 9×9 NORTH BLUE at (30,0)→(38,8), tile_count=81 < 96 min → skipped
    //   Painted with SkippedLotColor (152,112,52)
    // -----------------------------------------------------------------------

    private string MakeTwoLotFillJson()
    {
        const string compId = "COMP_MULTI30B";
        return $$"""
        {
          "lot_sizing_policy_version": "MAP29C_V1",
          "components": [
            { "component_id": "{{compId}}", "parcel_class": "BLUE_RESIDENTIAL" }
          ],
          "lots": [
            {
              "component_id": "{{compId}}",
              "lot_id": "LOT_VALID",
              "frontage_direction": "NORTH",
              "x1": 0, "y1": 0, "x2": 13, "y2": 26,
              "width": 14, "height": 27, "tile_count": 378,
              "shade_r": 200, "shade_g": 168, "shade_b": 120
            },
            {
              "component_id": "{{compId}}",
              "lot_id": "LOT_SKIPPED",
              "frontage_direction": "NORTH",
              "x1": 30, "y1": 0, "x2": 38, "y2": 8,
              "width": 9, "height": 9, "tile_count": 81,
              "shade_r": 200, "shade_g": 168, "shade_b": 120
            }
          ]
        }
        """;
    }

    private System.Drawing.Bitmap RenderToBitmap(string lf, string pol)
    {
        var builder = NewBuilder();
        var r       = builder.Build(lf, pol, _tempDir);
        var bytes   = builder.RenderOutputPngBytes(r);
        using var ms = new System.IO.MemoryStream(bytes);
        return new System.Drawing.Bitmap(ms);
    }

    // -----------------------------------------------------------------------
    // MAP-30B 15. Skipped lot is painted with SkippedLotColor in preview
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_SkippedLot_IsPaintedWithSkippedLotColor()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        using var bmp = RenderToBitmap(lf, pol);

        var (sr, sg, sb) = DeadMtlWorldBuilderParcelBuildingFootprintCandidatesBuilder.SkippedLotPreviewColor;
        // Pixel at (30,0) is inside the skipped lot (30,0)→(38,8)
        var p = bmp.GetPixel(30, 0);
        Assert.Equal(sr, (int)p.R);
        Assert.Equal(sg, (int)p.G);
        Assert.Equal(sb, (int)p.B);
    }

    // -----------------------------------------------------------------------
    // MAP-30B 16. Valid lot base color painted outside footprint region
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_ValidLot_BasePaintedOutsideFootprint()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        using var bmp = RenderToBitmap(lf, pol);

        // Lot (0,0)→(13,26), fpY1=2, fpX1=1. Pixel at (0,0) is in side-setback region → lot base
        var p = bmp.GetPixel(0, 0);
        Assert.Equal(200, (int)p.R);
        Assert.Equal(168, (int)p.G);
        Assert.Equal(120, (int)p.B);
    }

    // -----------------------------------------------------------------------
    // MAP-30B 17. Footprint overlay is lighter than lot base
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_FootprintOverlay_IsLighterThanLotBase()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        using var bmp = RenderToBitmap(lf, pol);

        // Inside footprint: (1,2) → should be lot shade + 45
        var fpPx   = bmp.GetPixel(1, 2);
        // Lot base: (0,0) → should be lot shade
        var basePx = bmp.GetPixel(0, 0);
        Assert.True(fpPx.R >= basePx.R, "Footprint R should be >= lot base R");
        Assert.True(fpPx.G >= basePx.G, "Footprint G should be >= lot base G");
        Assert.True(fpPx.B >= basePx.B, "Footprint B should be >= lot base B");
        Assert.True(fpPx.R > basePx.R || fpPx.G > basePx.G || fpPx.B > basePx.B,
            "Footprint pixel should be strictly lighter than lot base in at least one channel");
    }

    // -----------------------------------------------------------------------
    // MAP-30B 18. Output PNG has no debug cyan pixels
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_OutputPng_NoCyanDebugPixels()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        using var bmp = RenderToBitmap(lf, pol);

        bool foundCyan = false;
        for (int x = 0; x < bmp.Width && !foundCyan; x++)
            for (int y = 0; y < bmp.Height && !foundCyan; y++)
            {
                var c = bmp.GetPixel(x, y);
                if (c.R < 60 && c.G > 180 && c.B > 180)
                    foundCyan = true;
            }
        Assert.False(foundCyan, "Output PNG must contain no debug cyan pixels");
    }

    // -----------------------------------------------------------------------
    // MAP-30B 19. Output PNG has no pure black pixels
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_OutputPng_NoPureBlackPixels()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        using var bmp = RenderToBitmap(lf, pol);

        bool foundBlack = false;
        for (int x = 0; x < bmp.Width && !foundBlack; x++)
            for (int y = 0; y < bmp.Height && !foundBlack; y++)
            {
                var c = bmp.GetPixel(x, y);
                if (c.R == 0 && c.G == 0 && c.B == 0)
                    foundBlack = true;
            }
        Assert.False(foundBlack, "Output PNG must contain no pure black pixels");
    }

    // -----------------------------------------------------------------------
    // MAP-30B 20. Source-blue is not inside lot bounds after painting
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_OutputPng_NoSourceBlueInsideLotBounds()
    {
        // Inject a source-blue pixel at (5,5) inside the valid lot, to verify the builder paints over it.
        // We can't inject into the source PNG easily, but we can verify that after painting,
        // the pixel at a lot-interior coordinate is NOT source-blue (r<100, b>140, b>r+50).
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        using var bmp = RenderToBitmap(lf, pol);

        // Check representative interior pixels of both lots
        var lotBounds = new[] { (0, 0, 13, 26), (30, 0, 38, 8) };
        foreach (var (x1, y1, x2, y2) in lotBounds)
        {
            for (int x = x1; x <= x2; x++)
                for (int y = y1; y <= y2; y++)
                {
                    var c = bmp.GetPixel(x, y);
                    bool isSourceBlue = c.R < 100 && c.B > 140 && c.B > c.R + 50;
                    bool isSourceRed  = c.R >= 180 && c.G <= 5 && c.B <= 5;
                    Assert.False(isSourceBlue || isSourceRed,
                        $"Pixel at ({x},{y}) has source parcel color rgb({c.R},{c.G},{c.B})");
                }
        }
    }

    // -----------------------------------------------------------------------
    // MAP-30B 21. PreviewPaintedLotCount == TotalLotCount
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_AllLots_PaintedInPreview()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Equal(r.TotalLotCount, r.PreviewPaintedLotCount);
    }

    // -----------------------------------------------------------------------
    // MAP-30B 22. Skipped lot preview color in result JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_SkippedLotPreviewColor_InResult()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.False(string.IsNullOrEmpty(r.SkippedLotPreviewColorRgb));
        Assert.StartsWith("rgb(", r.SkippedLotPreviewColorRgb);
    }

    // -----------------------------------------------------------------------
    // MAP-30B 23. MAP30B checks present in result
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_Checks_ArePresent()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        var r = NewBuilder().Build(lf, pol, _tempDir);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP30B_ALL_LOTS_PAINTED_IN_PREVIEW");
        Assert.Contains(r.Checks, c => c.CheckId == "MAP30B_SKIPPED_LOTS_VISIBLE_IN_PREVIEW");
        Assert.Contains(r.Checks, c => c.CheckId == "MAP30B_OUTPUT_PNG_NO_DEBUG_CYAN_OR_BLACK");
        Assert.Contains(r.Checks, c => c.CheckId == "MAP30B_OUTPUT_PNG_NO_SOURCE_BLUE_RED_INSIDE_LOTS");
        Assert.Contains(r.Checks, c => c.CheckId == "MAP30B_NO_RUNTIME_ARTIFACTS_WRITTEN");
        Assert.Contains(r.Checks, c => c.CheckId == "MAP30B_CLAIM_BOUNDARY_FALSE");
    }

    // -----------------------------------------------------------------------
    // MAP-30B 24. All MAP30B checks PASS
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_AllNewChecks_Pass()
    {
        var (lf, pol) = WriteFixtures(MakeTwoLotFillJson());
        var r = NewBuilder().Build(lf, pol, _tempDir);
        var map30bChecks = r.Checks.Where(c => c.CheckId.StartsWith("MAP30B")).ToList();
        Assert.True(map30bChecks.Count > 0, "No MAP30B checks found");
        foreach (var c in map30bChecks)
            Assert.True(c.CheckStatus == "PASS", $"Check {c.CheckId} FAILED: expected={c.Expected} actual={c.Actual}");
    }
}
