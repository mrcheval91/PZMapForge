using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map29a-core-test.local", Guid.NewGuid().ToString());

    public DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // -----------------------------------------------------------------------
    // Fixture PNG creators
    // -----------------------------------------------------------------------

    // North/south fixture: blue rect X124..212 Y10..69, orange road N (Y8-9) and S (Y70-71)
    private string MakeNsFixturePng()
    {
        var path = Path.Combine(_tempDir, "ns_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24); // dark background
        // Orange road north Y=8..9
        for (int x = 124; x <= 212; x++) { bmp.SetPixel(x, 8, Orange()); bmp.SetPixel(x, 9, Orange()); }
        // Orange road south Y=70..71
        for (int x = 124; x <= 212; x++) { bmp.SetPixel(x, 70, Orange()); bmp.SetPixel(x, 71, Orange()); }
        // Blue rect X124..212 Y10..69
        for (int x = 124; x <= 212; x++)
            for (int y = 10; y <= 69; y++)
                bmp.SetPixel(x, y, Blue());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // East-only fixture: blue rect X50..90 Y10..60, orange road east (X=91)
    private string MakeEastFixturePng()
    {
        var path = Path.Combine(_tempDir, "east_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int y = 10; y <= 60; y++) bmp.SetPixel(91, y, Orange()); // east side
        for (int x = 50; x <= 90; x++)
            for (int y = 10; y <= 60; y++)
                bmp.SetPixel(x, y, Blue());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // No-street fixture: blue rect, no orange
    private string MakeNoStreetFixturePng()
    {
        var path = Path.Combine(_tempDir, "nostreet_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int x = 60; x <= 100; x++)
            for (int y = 20; y <= 60; y++)
                bmp.SetPixel(x, y, Blue());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // Replacement fixture: blue rect plus some non-blue pixels that must not change
    private string MakeReplacementFixturePng()
    {
        var path = Path.Combine(_tempDir, "replacement_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int x = 100; x <= 150; x++) { bmp.SetPixel(x, 5, Orange()); bmp.SetPixel(x, 66, Orange()); }
        for (int x = 100; x <= 150; x++)
            for (int y = 6; y <= 65; y++)
                bmp.SetPixel(x, y, Blue());
        // a non-blue marker pixel we can verify is preserved
        bmp.SetPixel(10, 10, System.Drawing.Color.FromArgb(255, 200, 100, 50));
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    private static System.Drawing.Color Blue()   => System.Drawing.Color.FromArgb(58, 94, 174);
    private static System.Drawing.Color Orange() => System.Drawing.Color.FromArgb(220, 120, 30);

    private static void Fill(System.Drawing.Bitmap bmp, byte r, byte g, byte b)
    {
        using var g2 = System.Drawing.Graphics.FromImage(bmp);
        g2.Clear(System.Drawing.Color.FromArgb(r, g, b));
    }

    private DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillBuilder MakeBuilder() => new();

    // -----------------------------------------------------------------------
    // NS fixture: blue component detection
    // -----------------------------------------------------------------------

    [Fact]
    public void NsFixture_DetectedBlueComponentCount_Is1()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(1, r.DetectedBlueComponentCount);
    }

    [Fact]
    public void NsFixture_ProcessedComponentCount_Is1()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(1, r.ProcessedQuadrilateralComponentCount);
    }

    [Fact]
    public void NsFixture_UnsupportedComponentCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(0, r.UnsupportedBlueComponentCount);
    }

    // -----------------------------------------------------------------------
    // NS fixture: street adjacency
    // -----------------------------------------------------------------------

    [Fact]
    public void NsFixture_NorthStreetAdjacency_IsTrue()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.True(r.Components[0].NorthStreetAdjacency);
    }

    [Fact]
    public void NsFixture_SouthStreetAdjacency_IsTrue()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.True(r.Components[0].SouthStreetAdjacency);
    }

    [Fact]
    public void NsFixture_EastStreetAdjacency_IsFalse()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.False(r.Components[0].EastStreetAdjacency);
    }

    [Fact]
    public void NsFixture_WestStreetAdjacency_IsFalse()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.False(r.Components[0].WestStreetAdjacency);
    }

    // -----------------------------------------------------------------------
    // NS fixture: lot counts
    // -----------------------------------------------------------------------

    [Fact]
    public void NsFixture_TotalLotCount_Is12()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(12, r.TotalLotCount);
    }

    [Fact]
    public void NsFixture_NorthLotCount_Is6()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(6, r.Lots.Count(l => l.FrontageDirection == "NORTH"));
    }

    [Fact]
    public void NsFixture_SouthLotCount_Is6()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(6, r.Lots.Count(l => l.FrontageDirection == "SOUTH"));
    }

    [Fact]
    public void NsFixture_EastLotCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(0, r.Lots.Count(l => l.FrontageDirection == "EAST"));
    }

    // -----------------------------------------------------------------------
    // NS fixture: column widths 15x5 + 14x1
    // -----------------------------------------------------------------------

    [Fact]
    public void NsFixture_NorthLots_ColumnWidths_Are_15x5and14x1()
    {
        var r    = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        var ns   = r.Lots.Where(l => l.FrontageDirection == "NORTH").OrderBy(l => l.X1).ToList();
        var widths = ns.Select(l => l.Width).ToArray();
        Assert.Equal(6, widths.Length);
        int count15 = widths.Count(w => w == 15);
        int count14 = widths.Count(w => w == 14);
        Assert.Equal(5, count15);
        Assert.Equal(1, count14);
    }

    [Fact]
    public void NsFixture_NorthAndSouthRows_UseIdenticalXRanges()
    {
        var r    = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        var nLots = r.Lots.Where(l => l.FrontageDirection == "NORTH").OrderBy(l => l.X1).ToList();
        var sLots = r.Lots.Where(l => l.FrontageDirection == "SOUTH").OrderBy(l => l.X1).ToList();
        Assert.Equal(nLots.Count, sLots.Count);
        for (int i = 0; i < nLots.Count; i++)
        {
            Assert.Equal(nLots[i].X1, sLots[i].X1);
            Assert.Equal(nLots[i].X2, sLots[i].X2);
        }
    }

    // -----------------------------------------------------------------------
    // NS fixture: facade edges
    // -----------------------------------------------------------------------

    [Fact]
    public void NsFixture_TotalFacadeEdgeCount_Is12()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(12, r.TotalFacadeEdgeCount);
    }

    [Fact]
    public void NsFixture_NoEastFacadeEdge()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.DoesNotContain(r.FacadeEdges, e => e.FrontageDirection == "EAST");
    }

    [Fact]
    public void NsFixture_NorthFacadeEdges_OnNorthY()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        var comp = r.Components[0];
        var nEdges = r.FacadeEdges.Where(e => e.FrontageDirection == "NORTH").ToList();
        Assert.All(nEdges, e =>
        {
            Assert.Equal(e.Y1, e.Y2);
            Assert.Equal(comp.BboxY1, e.Y1);
        });
    }

    [Fact]
    public void NsFixture_SouthFacadeEdges_OnSouthY()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        var comp = r.Components[0];
        var sEdges = r.FacadeEdges.Where(e => e.FrontageDirection == "SOUTH").ToList();
        Assert.All(sEdges, e =>
        {
            Assert.Equal(e.Y1, e.Y2);
            Assert.Equal(comp.BboxY2, e.Y1);
        });
    }

    // -----------------------------------------------------------------------
    // NS fixture: checks pass
    // -----------------------------------------------------------------------

    [Fact]
    public void NsFixture_Build_NoFailedChecks()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        var failed = r.Checks.Where(c => c.CheckStatus == "FAIL").ToList();
        Assert.Empty(failed);
    }

    // -----------------------------------------------------------------------
    // East fixture: east-facing lots + facades
    // -----------------------------------------------------------------------

    [Fact]
    public void EastFixture_EastStreetAdjacency_IsTrue()
    {
        var r = MakeBuilder().Build(MakeEastFixturePng(), _tempDir);
        Assert.True(r.Components[0].EastStreetAdjacency);
    }

    [Fact]
    public void EastFixture_NorthStreetAdjacency_IsFalse()
    {
        var r = MakeBuilder().Build(MakeEastFixturePng(), _tempDir);
        Assert.False(r.Components[0].NorthStreetAdjacency);
    }

    [Fact]
    public void EastFixture_SouthStreetAdjacency_IsFalse()
    {
        var r = MakeBuilder().Build(MakeEastFixturePng(), _tempDir);
        Assert.False(r.Components[0].SouthStreetAdjacency);
    }

    [Fact]
    public void EastFixture_AllLotsHaveEastFrontage()
    {
        var r = MakeBuilder().Build(MakeEastFixturePng(), _tempDir);
        Assert.True(r.Lots.Count > 0);
        Assert.All(r.Lots, l => Assert.Equal("EAST", l.FrontageDirection));
    }

    [Fact]
    public void EastFixture_AllFacadeEdgesAreEast()
    {
        var r = MakeBuilder().Build(MakeEastFixturePng(), _tempDir);
        Assert.True(r.FacadeEdges.Count > 0);
        Assert.All(r.FacadeEdges, e => Assert.Equal("EAST", e.FrontageDirection));
    }

    [Fact]
    public void EastFixture_EastFacadeEdges_OnEastX()
    {
        var r = MakeBuilder().Build(MakeEastFixturePng(), _tempDir);
        var comp = r.Components[0];
        Assert.All(r.FacadeEdges, e =>
        {
            Assert.Equal(e.X1, e.X2);
            Assert.Equal(comp.BboxX2, e.X1);
        });
    }

    [Fact]
    public void EastFixture_NoNorthOrSouthFacade()
    {
        var r = MakeBuilder().Build(MakeEastFixturePng(), _tempDir);
        Assert.DoesNotContain(r.FacadeEdges, e => e.FrontageDirection == "NORTH");
        Assert.DoesNotContain(r.FacadeEdges, e => e.FrontageDirection == "SOUTH");
    }

    // -----------------------------------------------------------------------
    // No-street fixture: unsupported component
    // -----------------------------------------------------------------------

    [Fact]
    public void NoStreetFixture_UnsupportedComponentCount_Is1()
    {
        var r = MakeBuilder().Build(MakeNoStreetFixturePng(), _tempDir);
        Assert.Equal(1, r.UnsupportedBlueComponentCount);
    }

    [Fact]
    public void NoStreetFixture_IsValid_IsFalse()
    {
        var r = MakeBuilder().Build(MakeNoStreetFixturePng(), _tempDir);
        Assert.False(r.IsValid);
    }

    [Fact]
    public void NoStreetFixture_TotalLotCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeNoStreetFixturePng(), _tempDir);
        Assert.Equal(0, r.TotalLotCount);
    }

    [Fact]
    public void NoStreetFixture_TotalFacadeEdgeCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeNoStreetFixturePng(), _tempDir);
        Assert.Equal(0, r.TotalFacadeEdgeCount);
    }

    [Fact]
    public void NoStreetFixture_UnsupportedReason_ContainsNoStreet()
    {
        var r = MakeBuilder().Build(MakeNoStreetFixturePng(), _tempDir);
        Assert.Contains("no street", r.Components[0].UnsupportedReason, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Replacement fixture: pixel replacement correctness
    // -----------------------------------------------------------------------

    [Fact]
    public void ReplacementFixture_OutputPng_ZeroOriginalBluePixels()
    {
        var fixturePath = MakeReplacementFixturePng();
        var b = MakeBuilder();
        var r = b.Build(fixturePath, _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        for (int x = 0; x < bmp.Width; x++)
        for (int y = 0; y < bmp.Height; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.False(px.R == 58 && px.G == 94 && px.B == 174,
                $"Original blue (58,94,174) found at ({x},{y})");
        }
    }

    [Fact]
    public void ReplacementFixture_OutputPng_OriginalBlueBecomesBeigeShade()
    {
        var fixturePath = MakeReplacementFixturePng();
        var b = MakeBuilder();
        var r = b.Build(fixturePath, _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);

        var allowed = new HashSet<(byte R, byte G, byte B)>
        {
            (200, 168, 120), (176, 140, 96), (152, 116, 76),
        };
        for (int x = 100; x <= 150; x++)
        for (int y = 6; y <= 65; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.True(allowed.Contains((px.R, px.G, px.B)),
                $"Non-beige pixel ({px.R},{px.G},{px.B}) at ({x},{y}) in replaced area");
        }
    }

    [Fact]
    public void ReplacementFixture_OutputPng_NonBluePixelsPreserved()
    {
        var fixturePath = MakeReplacementFixturePng();
        var b = MakeBuilder();
        var r = b.Build(fixturePath, _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        // Marker pixel at (10,10) should be preserved
        var px = bmp.GetPixel(10, 10);
        Assert.Equal(200, (int)px.R);
        Assert.Equal(100, (int)px.G);
        Assert.Equal(50,  (int)px.B);
    }

    [Fact]
    public void ReplacementFixture_OutputPng_DimensionsMatch()
    {
        var fixturePath = MakeReplacementFixturePng();
        var b = MakeBuilder();
        var r = b.Build(fixturePath, _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        int w = (outBytes[16] << 24) | (outBytes[17] << 16) | (outBytes[18] << 8) | outBytes[19];
        int h = (outBytes[20] << 24) | (outBytes[21] << 16) | (outBytes[22] << 8) | outBytes[23];
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_SandboxOnly_IsTrue()   => Assert.True(MakeBuilder().Build(MakeNsFixturePng(), _tempDir).SandboxOnly);
    [Fact]
    public void Build_WriterReady_IsFalse()  => Assert.False(MakeBuilder().Build(MakeNsFixturePng(), _tempDir).WriterReady);
    [Fact]
    public void Build_RuntimeValid_IsFalse() => Assert.False(MakeBuilder().Build(MakeNsFixturePng(), _tempDir).RuntimeValid);
    [Fact]
    public void Build_Materialized_IsFalse() => Assert.False(MakeBuilder().Build(MakeNsFixturePng(), _tempDir).Materialized);
    [Fact]
    public void Build_RuntimeProofClaimed_IsFalse()            => Assert.False(MakeBuilder().Build(MakeNsFixturePng(), _tempDir).RuntimeProofClaimed);
    [Fact]
    public void Build_PublicPlayablePackagingClaimed_IsFalse() => Assert.False(MakeBuilder().Build(MakeNsFixturePng(), _tempDir).PublicPlayablePackagingClaimed);

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderJson_ContainsDetectedComponents()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeNsFixturePng(), _tempDir);
        var json = b.RenderJson(r);
        Assert.Contains("detected_blue_component_count", json);
    }

    [Fact]
    public void RenderLotsCsv_Has12DataRows()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeNsFixturePng(), _tempDir);
        var csv = b.RenderLotsCsv(r);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(13, rows.Length); // header + 12
    }

    [Fact]
    public void RenderFacadesCsv_Has12DataRows()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeNsFixturePng(), _tempDir);
        var csv = b.RenderFacadesCsv(r);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(13, rows.Length); // header + 12
    }

    [Fact]
    public void RenderHtml_IsAsciiOnly()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeNsFixturePng(), _tempDir);
        var html = b.RenderHtml(r);
        Assert.All(html, ch => Assert.True(ch < 128, $"Non-ASCII 0x{(int)ch:X2}"));
    }

    [Fact]
    public void RenderHtml_DoesNotContainForbiddenStrings()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeNsFixturePng(), _tempDir);
        var html = b.RenderHtml(r);
        Assert.DoesNotContain("steamapps",           html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WorldGenOverride.lua", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("media/maps",           html, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Mixed-adjacency fixtures
    // -----------------------------------------------------------------------

    // Blue rect X50..90 Y10..60, bboxW=41 bboxH=51.
    // North threshold=max(2,ceil(41*0.25))=11. East threshold=max(2,ceil(51*0.25))=13.
    // Orange north Y=9 X=50..60 → 11 contacts (ratio=11/41≈0.268, just qualifies).
    // Orange south Y=61 X=50..60 → 11 contacts (ratio 0.268).
    // Orange east X=91 Y=10..60 → 51 contacts (ratio=1.0).
    // Best V (1.0) > best H (0.268) → SelectFrontageGroup = VERTICAL → EAST facades.
    private string MakeMixedEastDominantFixturePng()
    {
        var path = Path.Combine(_tempDir, "mixed_east_dominant_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int x = 50; x <= 60; x++) { bmp.SetPixel(x, 9, Orange()); bmp.SetPixel(x, 61, Orange()); }
        for (int y = 10; y <= 60; y++) bmp.SetPixel(91, y, Orange());
        for (int x = 50; x <= 90; x++)
            for (int y = 10; y <= 60; y++)
                bmp.SetPixel(x, y, Blue());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // Same geometry X50..90 Y10..60.
    // Orange north Y=9 X=50..90 → 41 contacts (ratio=1.0).
    // Orange south Y=61 X=50..90 → 41 contacts (ratio=1.0).
    // Orange east X=91 Y=10..22 → 13 contacts (ratio=13/51≈0.255, just qualifies).
    // Best H (1.0) > best V (0.255) → SelectFrontageGroup = HORIZONTAL → N/S facades.
    private string MakeMixedNsDominantFixturePng()
    {
        var path = Path.Combine(_tempDir, "mixed_ns_dominant_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int x = 50; x <= 90; x++) { bmp.SetPixel(x, 9, Orange()); bmp.SetPixel(x, 61, Orange()); }
        for (int y = 10; y <= 22; y++) bmp.SetPixel(91, y, Orange());
        for (int x = 50; x <= 90; x++)
            for (int y = 10; y <= 60; y++)
                bmp.SetPixel(x, y, Blue());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // -----------------------------------------------------------------------
    // Frontage group — existing fixtures
    // -----------------------------------------------------------------------

    [Fact]
    public void NsFixture_SelectedFrontageGroup_IsHorizontal()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal("HORIZONTAL", r.Components[0].SelectedFrontageGroup);
    }

    [Fact]
    public void EastFixture_SelectedFrontageGroup_IsVertical()
    {
        var r = MakeBuilder().Build(MakeEastFixturePng(), _tempDir);
        Assert.Equal("VERTICAL", r.Components[0].SelectedFrontageGroup);
    }

    // -----------------------------------------------------------------------
    // Mixed east-dominant fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void MixedEastDominant_SelectedFrontageGroup_IsVertical()
    {
        var r = MakeBuilder().Build(MakeMixedEastDominantFixturePng(), _tempDir);
        Assert.Equal("VERTICAL", r.Components[0].SelectedFrontageGroup);
    }

    [Fact]
    public void MixedEastDominant_AllFacadeEdgesAreEast()
    {
        var r = MakeBuilder().Build(MakeMixedEastDominantFixturePng(), _tempDir);
        Assert.True(r.FacadeEdges.Count > 0);
        Assert.All(r.FacadeEdges, e => Assert.Equal("EAST", e.FrontageDirection));
    }

    [Fact]
    public void MixedEastDominant_NoNorthOrSouthFacades()
    {
        var r = MakeBuilder().Build(MakeMixedEastDominantFixturePng(), _tempDir);
        Assert.DoesNotContain(r.FacadeEdges, e => e.FrontageDirection == "NORTH");
        Assert.DoesNotContain(r.FacadeEdges, e => e.FrontageDirection == "SOUTH");
    }

    [Fact]
    public void MixedEastDominant_TopAndBottomEastLotsAreCorner()
    {
        var r    = MakeBuilder().Build(MakeMixedEastDominantFixturePng(), _tempDir);
        var comp = r.Components[0];
        var lots = r.Lots.ToList();
        Assert.True(lots.Single(l => l.Y1 == comp.BboxY1).IsCornerLot,
            "Top east lot (Y1=bboxY1) should be corner because north is also street-adjacent");
        Assert.True(lots.Single(l => l.Y2 == comp.BboxY2).IsCornerLot,
            "Bottom east lot (Y2=bboxY2) should be corner because south is also street-adjacent");
        Assert.False(lots.Single(l => l.Y1 != comp.BboxY1 && l.Y2 != comp.BboxY2).IsCornerLot,
            "Middle lot should not be corner");
    }

    [Fact]
    public void MixedEastDominant_EastContactRatio_GreaterThan_NorthContactRatio()
    {
        var r    = MakeBuilder().Build(MakeMixedEastDominantFixturePng(), _tempDir);
        var comp = r.Components[0];
        Assert.True(comp.EastStreetContactRatio > comp.NorthStreetContactRatio,
            $"E={comp.EastStreetContactRatio} should beat N={comp.NorthStreetContactRatio}");
    }

    // -----------------------------------------------------------------------
    // Mixed NS-dominant fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void MixedNsDominant_SelectedFrontageGroup_IsHorizontal()
    {
        var r = MakeBuilder().Build(MakeMixedNsDominantFixturePng(), _tempDir);
        Assert.Equal("HORIZONTAL", r.Components[0].SelectedFrontageGroup);
    }

    [Fact]
    public void MixedNsDominant_NorthAndSouthFacadesGenerated()
    {
        var r = MakeBuilder().Build(MakeMixedNsDominantFixturePng(), _tempDir);
        Assert.Contains(r.FacadeEdges, e => e.FrontageDirection == "NORTH");
        Assert.Contains(r.FacadeEdges, e => e.FrontageDirection == "SOUTH");
    }

    [Fact]
    public void MixedNsDominant_NoEastFacades()
    {
        var r = MakeBuilder().Build(MakeMixedNsDominantFixturePng(), _tempDir);
        Assert.DoesNotContain(r.FacadeEdges, e => e.FrontageDirection == "EAST");
    }

    [Fact]
    public void MixedNsDominant_RightmostLotsAreCorner()
    {
        var r    = MakeBuilder().Build(MakeMixedNsDominantFixturePng(), _tempDir);
        var comp = r.Components[0];
        var rightmost    = r.Lots.Where(l => l.X2 == comp.BboxX2).ToList();
        var nonRightmost = r.Lots.Where(l => l.X2 != comp.BboxX2).ToList();
        Assert.True(rightmost.Count > 0, "Should have rightmost lots");
        Assert.All(rightmost,    l => Assert.True(l.IsCornerLot,  $"Rightmost lot X2={l.X2} should be corner (east-adjacent)"));
        Assert.All(nonRightmost, l => Assert.False(l.IsCornerLot, $"Non-rightmost lot X2={l.X2} should not be corner"));
    }

    [Fact]
    public void MixedNsDominant_NorthContactRatio_GreaterThan_EastContactRatio()
    {
        var r    = MakeBuilder().Build(MakeMixedNsDominantFixturePng(), _tempDir);
        var comp = r.Components[0];
        Assert.True(comp.NorthStreetContactRatio > comp.EastStreetContactRatio,
            $"N={comp.NorthStreetContactRatio} should beat E={comp.EastStreetContactRatio}");
    }

    // -----------------------------------------------------------------------
    // Source hash check row
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CheckRow_SourcePngHashUnchanged_Exists()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP29A_SOURCE_PNG_HASH_UNCHANGED");
    }

    // -----------------------------------------------------------------------
    // MAP-29B — Red fixture factories
    // -----------------------------------------------------------------------

    // Red rect X60..120 Y10..50, orange road NORTH (Y=8..9): bboxW=61, bboxH=41.
    // North threshold=max(2,ceil(61*0.25))=16. Orange N: 61 contacts (ratio=1.0). HORIZONTAL.
    private string MakeRedNorthFixturePng()
    {
        var path = Path.Combine(_tempDir, "red_north_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int x = 60; x <= 120; x++) { bmp.SetPixel(x, 8, Orange()); bmp.SetPixel(x, 9, Orange()); }
        for (int x = 60; x <= 120; x++)
            for (int y = 10; y <= 50; y++)
                bmp.SetPixel(x, y, SourceRed());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // Red rect X50..90 Y10..60, orange road EAST (X=91): bboxW=41, bboxH=51. VERTICAL/EAST.
    private string MakeRedEastFixturePng()
    {
        var path = Path.Combine(_tempDir, "red_east_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int y = 10; y <= 60; y++) bmp.SetPixel(91, y, Orange());
        for (int x = 50; x <= 90; x++)
            for (int y = 10; y <= 60; y++)
                bmp.SetPixel(x, y, SourceRed());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // Orange-only fixture — orange pixels must NOT be classified as red components.
    private string MakeOrangeOnlyFixturePng()
    {
        var path = Path.Combine(_tempDir, "orange_only_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int x = 60; x <= 120; x++)
            for (int y = 10; y <= 50; y++)
                bmp.SetPixel(x, y, Orange());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // Mixed fixture: blue rect (NS adjacent) + red rect (N adjacent) + shared orange roads.
    private string MakeMixedBlueRedFixturePng()
    {
        var path = Path.Combine(_tempDir, "mixed_blue_red_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        // Blue NS block: X10..60 Y10..50, orange N (Y=8..9) and S (Y=51..52)
        for (int x = 10; x <= 60; x++) { bmp.SetPixel(x, 8, Orange()); bmp.SetPixel(x, 9, Orange()); }
        for (int x = 10; x <= 60; x++) { bmp.SetPixel(x, 51, Orange()); bmp.SetPixel(x, 52, Orange()); }
        for (int x = 10; x <= 60; x++)
            for (int y = 10; y <= 50; y++)
                bmp.SetPixel(x, y, Blue());
        // Red N block: X100..160 Y80..120, orange N (Y=78..79)
        for (int x = 100; x <= 160; x++) { bmp.SetPixel(x, 78, Orange()); bmp.SetPixel(x, 79, Orange()); }
        for (int x = 100; x <= 160; x++)
            for (int y = 80; y <= 120; y++)
                bmp.SetPixel(x, y, SourceRed());
        // Keep a marker orange pixel far from components to verify preservation
        bmp.SetPixel(200, 200, Orange());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    private static System.Drawing.Color SourceRed() => System.Drawing.Color.FromArgb(206, 0, 0);

    // -----------------------------------------------------------------------
    // MAP-29B — Red north fixture: detection
    // -----------------------------------------------------------------------

    [Fact]
    public void RedNorthFixture_DetectedRedComponentCount_Is1()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.Equal(1, r.DetectedRedComponentCount);
    }

    [Fact]
    public void RedNorthFixture_ProcessedRedComponentCount_Is1()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.Equal(1, r.ProcessedRedComponentCount);
    }

    [Fact]
    public void RedNorthFixture_UnsupportedRedComponentCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.Equal(0, r.UnsupportedRedComponentCount);
    }

    [Fact]
    public void RedNorthFixture_NorthStreetAdjacency_IsTrue()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        var redComp = r.Components.Single(c => c.ParcelClass == "RED_RESIDENTIAL_OR_COMMERCIAL");
        Assert.True(redComp.NorthStreetAdjacency);
    }

    [Fact]
    public void RedNorthFixture_SelectedFrontageGroup_IsHorizontal()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        var redComp = r.Components.Single(c => c.ParcelClass == "RED_RESIDENTIAL_OR_COMMERCIAL");
        Assert.Equal("HORIZONTAL", redComp.SelectedFrontageGroup);
    }

    [Fact]
    public void RedNorthFixture_RedLotCount_Gt0()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.True(r.RedLotCount > 0, "Expected at least one red lot");
    }

    [Fact]
    public void RedNorthFixture_RedFacadeEdgeCount_Gt0()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.True(r.RedFacadeEdgeCount > 0, "Expected at least one red facade edge");
    }

    [Fact]
    public void RedNorthFixture_BlueComponentCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.Equal(0, r.DetectedBlueComponentCount);
    }

    // -----------------------------------------------------------------------
    // MAP-29B — Red north fixture: output uses only red shades
    // -----------------------------------------------------------------------

    [Fact]
    public void RedNorthFixture_OutputUsesOnlyRedShades()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeRedNorthFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);

        var allowedRed = new HashSet<(byte R, byte G, byte B)>
        {
            (166, 0, 0), (196, 20, 20), (226, 40, 40),
        };
        // Pixels in the red rect area must be one of the three red shades
        for (int x = 60; x <= 120; x++)
        for (int y = 10; y <= 50; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.True(allowedRed.Contains((px.R, px.G, px.B)),
                $"Non-red-shade pixel ({px.R},{px.G},{px.B}) at ({x},{y}) in red area");
        }
    }

    [Fact]
    public void RedNorthFixture_OutputPng_SourceRedNotRemaining()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeRedNorthFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        for (int x = 0; x < bmp.Width; x++)
        for (int y = 0; y < bmp.Height; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.False(px.R == 206 && px.G == 0 && px.B == 0,
                $"Source red (206,0,0) still present at ({x},{y})");
        }
    }

    [Fact]
    public void RedNorthFixture_OutputPng_OrangePreserved()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeRedNorthFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        // Orange road pixels at Y=8..9 must still be orange (220,120,30)
        for (int x = 60; x <= 120; x++)
        {
            var p8 = bmp.GetPixel(x, 8);
            var p9 = bmp.GetPixel(x, 9);
            Assert.True(p8.R == 220 && p8.G == 120 && p8.B == 30,
                $"Orange pixel at ({x},8) was changed to ({p8.R},{p8.G},{p8.B})");
            Assert.True(p9.R == 220 && p9.G == 120 && p9.B == 30,
                $"Orange pixel at ({x},9) was changed to ({p9.R},{p9.G},{p9.B})");
        }
    }

    // -----------------------------------------------------------------------
    // MAP-29B — Red east fixture: EAST/vertical frontage
    // -----------------------------------------------------------------------

    [Fact]
    public void RedEastFixture_SelectedFrontageGroup_IsVertical()
    {
        var r = MakeBuilder().Build(MakeRedEastFixturePng(), _tempDir);
        var redComp = r.Components.Single(c => c.ParcelClass == "RED_RESIDENTIAL_OR_COMMERCIAL");
        Assert.Equal("VERTICAL", redComp.SelectedFrontageGroup);
    }

    [Fact]
    public void RedEastFixture_AllRedFacadeEdgesAreEast()
    {
        var r = MakeBuilder().Build(MakeRedEastFixturePng(), _tempDir);
        Assert.True(r.RedFacadeEdgeCount > 0);
        var redEdges = r.FacadeEdges.Where(e => e.ComponentId.StartsWith("MAP29B_RED_")).ToList();
        Assert.All(redEdges, e => Assert.Equal("EAST", e.FrontageDirection));
    }

    [Fact]
    public void RedEastFixture_EastFacadeEdges_OnEastX()
    {
        var r = MakeBuilder().Build(MakeRedEastFixturePng(), _tempDir);
        var redComp = r.Components.Single(c => c.ParcelClass == "RED_RESIDENTIAL_OR_COMMERCIAL");
        var redEdges = r.FacadeEdges.Where(e => e.ComponentId == redComp.ComponentId).ToList();
        Assert.All(redEdges, e =>
        {
            Assert.Equal(e.X1, e.X2);
            Assert.Equal(redComp.BboxX2, e.X1);
        });
    }

    // -----------------------------------------------------------------------
    // MAP-29B — Orange-only: no red component detected
    // -----------------------------------------------------------------------

    [Fact]
    public void OrangeOnlyFixture_DetectedRedComponentCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeOrangeOnlyFixturePng(), _tempDir);
        Assert.Equal(0, r.DetectedRedComponentCount);
    }

    [Fact]
    public void OrangeOnlyFixture_RedLotCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeOrangeOnlyFixturePng(), _tempDir);
        Assert.Equal(0, r.RedLotCount);
    }

    // -----------------------------------------------------------------------
    // MAP-29B — Mixed blue+red fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void MixedFixture_BlueLotCount_Gt0()
    {
        var r = MakeBuilder().Build(MakeMixedBlueRedFixturePng(), _tempDir);
        Assert.True(r.BlueLotCount > 0, "Expected blue lots");
    }

    [Fact]
    public void MixedFixture_RedLotCount_Gt0()
    {
        var r = MakeBuilder().Build(MakeMixedBlueRedFixturePng(), _tempDir);
        Assert.True(r.RedLotCount > 0, "Expected red lots");
    }

    [Fact]
    public void MixedFixture_BlueAreaRemainsBeige_RedAreaRemainsRedShades()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeMixedBlueRedFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);

        var beigeShades = new HashSet<(byte R, byte G, byte B)>
            { (200, 168, 120), (176, 140, 96), (152, 116, 76) };
        var redShades   = new HashSet<(byte R, byte G, byte B)>
            { (166, 0, 0), (196, 20, 20), (226, 40, 40) };

        // Blue area (X10..60 Y10..50) must be beige shades only
        for (int x = 10; x <= 60; x++)
        for (int y = 10; y <= 50; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.True(beigeShades.Contains((px.R, px.G, px.B)),
                $"Blue area pixel ({px.R},{px.G},{px.B}) at ({x},{y}) is not a beige shade");
        }

        // Red area (X100..160 Y80..120) must be red shades only
        for (int x = 100; x <= 160; x++)
        for (int y = 80; y <= 120; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.True(redShades.Contains((px.R, px.G, px.B)),
                $"Red area pixel ({px.R},{px.G},{px.B}) at ({x},{y}) is not a red shade");
        }
    }

    [Fact]
    public void MixedFixture_OrangePreserved()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeMixedBlueRedFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        // Far marker orange pixel at (200,200) must be preserved
        var px = bmp.GetPixel(200, 200);
        Assert.True(px.R == 220 && px.G == 120 && px.B == 30,
            $"Orange marker at (200,200) was changed to ({px.R},{px.G},{px.B})");
    }

    [Fact]
    public void MixedFixture_SourceRedNotRemainingInOutput()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeMixedBlueRedFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        for (int x = 100; x <= 160; x++)
        for (int y = 80; y <= 120; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.False(px.R == 206 && px.G == 0 && px.B == 0,
                $"Source red (206,0,0) still at ({x},{y}) in red area");
        }
    }

    [Fact]
    public void MixedFixture_CleanOutput_NoForbiddenColors()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeMixedBlueRedFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        for (int x = 0; x < bmp.Width; x++)
        for (int y = 0; y < bmp.Height; y++)
        {
            var px = bmp.GetPixel(x, y);
            // No pure cyan debug markers
            Assert.False(px.R == 40 && px.G == 192 && px.B == 192,
                $"Forbidden cyan at ({x},{y})");
            // No pure black debug markers (pure black = 0,0,0 — background is 18,18,24)
            Assert.False(px.R == 0 && px.G == 0 && px.B == 0,
                $"Forbidden pure black at ({x},{y})");
        }
    }

    // -----------------------------------------------------------------------
    // MAP-29B1 — Sliver fixture: N+S red component too shallow to keep as 2 lots
    // -----------------------------------------------------------------------

    // Red rect X50..74 Y10..18 (25w × 9h), orange N (Y8..9) and S (Y19..20).
    // target_red=18: lotCount=round(25/18)=1 column.
    // N+S: 2 rows of heights 4 and 5. Both < min_depth_red=10. Merge → 1 lot.
    private string MakeSliverNsRedFixturePng()
    {
        var path = Path.Combine(_tempDir, "sliver_ns_red_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int x = 50; x <= 74; x++) { bmp.SetPixel(x, 8, Orange()); bmp.SetPixel(x, 9, Orange()); }
        for (int x = 50; x <= 74; x++) { bmp.SetPixel(x, 19, Orange()); bmp.SetPixel(x, 20, Orange()); }
        for (int x = 50; x <= 74; x++)
            for (int y = 10; y <= 18; y++)
                bmp.SetPixel(x, y, SourceRed());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    // Red rect X50..100 Y10..50 (51w × 41h), orange N (Y8..9) only.
    // target_red=18: lotCount=round(51/18)=3 columns. widths≈17. height=41.
    // 17 >= min_frontage=12, 41 >= min_depth=10, 17*41=697 >= min_area=160. No merge.
    private string MakeAboveMinimumRedFixturePng()
    {
        var path = Path.Combine(_tempDir, "above_minimum_red_fixture.png");
        using var bmp = new System.Drawing.Bitmap(256, 256);
        Fill(bmp, 18, 18, 24);
        for (int x = 50; x <= 100; x++) { bmp.SetPixel(x, 8, Orange()); bmp.SetPixel(x, 9, Orange()); }
        for (int x = 50; x <= 100; x++)
            for (int y = 10; y <= 50; y++)
                bmp.SetPixel(x, y, SourceRed());
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    [Fact]
    public void SliverNsRedFixture_MergedToSingleLot()
    {
        var r = MakeBuilder().Build(MakeSliverNsRedFixturePng(), _tempDir);
        Assert.Equal(1, r.RedLotCount);
    }

    [Fact]
    public void SliverNsRedFixture_RedUndersizedMergeCountGt0()
    {
        var r = MakeBuilder().Build(MakeSliverNsRedFixturePng(), _tempDir);
        Assert.True(r.RedUndersizedLotMergeCount > 0,
            $"Expected red merges > 0, got {r.RedUndersizedLotMergeCount}");
    }

    [Fact]
    public void SliverNsRedFixture_TotalMergeCountGt0()
    {
        var r = MakeBuilder().Build(MakeSliverNsRedFixturePng(), _tempDir);
        Assert.True(r.UndersizedLotMergeCount > 0,
            $"Expected total merges > 0, got {r.UndersizedLotMergeCount}");
    }

    [Fact]
    public void SliverNsRedFixture_OutputUsesOnlyRedShades()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeSliverNsRedFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        var allowed = new HashSet<(byte R, byte G, byte B)>
            { (166, 0, 0), (196, 20, 20), (226, 40, 40) };
        for (int x = 50; x <= 74; x++)
        for (int y = 10; y <= 18; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.True(allowed.Contains((px.R, px.G, px.B)),
                $"Non-red-shade ({px.R},{px.G},{px.B}) at ({x},{y}) after merge");
        }
    }

    [Fact]
    public void SliverNsRedFixture_SourceRedNotRemaining()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeSliverNsRedFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        for (int x = 0; x < bmp.Width; x++)
        for (int y = 0; y < bmp.Height; y++)
        {
            var px = bmp.GetPixel(x, y);
            Assert.False(px.R == 206 && px.G == 0 && px.B == 0,
                $"Source red (206,0,0) still present at ({x},{y})");
        }
    }

    [Fact]
    public void SliverNsRedFixture_OrangePreserved()
    {
        var b = MakeBuilder();
        var r = b.Build(MakeSliverNsRedFixturePng(), _tempDir);
        var outBytes = b.RenderOutputPngBytes(r);
        using var bmp = LoadBitmap(outBytes);
        for (int x = 50; x <= 74; x++)
        {
            var p8 = bmp.GetPixel(x, 8);
            Assert.True(p8.R == 220 && p8.G == 120 && p8.B == 30,
                $"Orange at ({x},8) was replaced by ({p8.R},{p8.G},{p8.B})");
        }
    }

    [Fact]
    public void SliverNsRedFixture_CheckRow_MAP29B1_NoUndersizedRed_Exists()
    {
        var r = MakeBuilder().Build(MakeSliverNsRedFixturePng(), _tempDir);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP29B1_NO_UNDERSIZED_RED_LOTS");
    }

    [Fact]
    public void SliverNsRedFixture_CheckRow_MAP29B1_UnderizedLotsMerged_Exists()
    {
        var r = MakeBuilder().Build(MakeSliverNsRedFixturePng(), _tempDir);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP29B1_UNDERSIZED_LOTS_MERGED");
    }

    [Fact]
    public void AboveMinimumRedFixture_NoMerge()
    {
        var r = MakeBuilder().Build(MakeAboveMinimumRedFixturePng(), _tempDir);
        Assert.Equal(0, r.RedUndersizedLotMergeCount);
    }

    [Fact]
    public void AboveMinimumRedFixture_RedLotCount_Is3()
    {
        var r = MakeBuilder().Build(MakeAboveMinimumRedFixturePng(), _tempDir);
        Assert.Equal(3, r.RedLotCount);
    }

    [Fact]
    public void NsFixture_BlueMergeCount_IsZero()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Equal(0, r.BlueUndersizedLotMergeCount);
    }

    [Fact]
    public void NsFixture_LotSizingPolicyVersion_Present()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.False(string.IsNullOrEmpty(r.LotSizingPolicyVersion),
            "LotSizingPolicyVersion should be set");
    }

    [Fact]
    public void NsFixture_CheckRow_MAP29B1_NoUndersizedBlue_Exists()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP29B1_NO_UNDERSIZED_BLUE_LOTS");
    }

    // -----------------------------------------------------------------------
    // MAP-29B — Checks row for red source
    // -----------------------------------------------------------------------

    [Fact]
    public void RedNorthFixture_CheckRow_MAP29B_RedProcessed_Exists()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP29B_EVERY_RED_COMPONENT_PROCESSED");
    }

    [Fact]
    public void RedNorthFixture_CheckRow_MAP29B_ZeroSourceRed_Exists()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP29B_OUTPUT_PNG_ZERO_SOURCE_RED");
    }

    // -----------------------------------------------------------------------
    // MAP-29B — Parcel class
    // -----------------------------------------------------------------------

    [Fact]
    public void NsFixture_Components_HaveBlueParcelClass()
    {
        var r = MakeBuilder().Build(MakeNsFixturePng(), _tempDir);
        Assert.All(r.Components, c => Assert.Equal("BLUE_RESIDENTIAL", c.ParcelClass));
    }

    [Fact]
    public void RedNorthFixture_Components_HaveRedParcelClass()
    {
        var r = MakeBuilder().Build(MakeRedNorthFixturePng(), _tempDir);
        Assert.All(r.Components, c => Assert.Equal("RED_RESIDENTIAL_OR_COMMERCIAL", c.ParcelClass));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static System.Drawing.Bitmap LoadBitmap(byte[] pngBytes)
    {
        using var ms  = new System.IO.MemoryStream(pngBytes);
        using var tmp = new System.Drawing.Bitmap(ms);
        return tmp.Clone(new System.Drawing.Rectangle(0, 0, tmp.Width, tmp.Height), tmp.PixelFormat);
    }
}
