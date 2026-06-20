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
    // Helpers
    // -----------------------------------------------------------------------

    private static System.Drawing.Bitmap LoadBitmap(byte[] pngBytes)
    {
        using var ms  = new System.IO.MemoryStream(pngBytes);
        using var tmp = new System.Drawing.Bitmap(ms);
        return tmp.Clone(new System.Drawing.Rectangle(0, 0, tmp.Width, tmp.Height), tmp.PixelFormat);
    }
}
