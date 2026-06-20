using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderResidentialParcelTopologyBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map28a-core-test.local", Guid.NewGuid().ToString());

    public DeadMtlWorldBuilderResidentialParcelTopologyBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private DeadMtlWorldBuilderResidentialParcelTopologyBuilder MakeBuilder() =>
        new();

    private DeadMtlWorldBuilderResidentialParcelTopologyResult RunBuild() =>
        MakeBuilder().Build(_tempDir);

    // -----------------------------------------------------------------------
    // Component and bbox
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ComponentId_IsMap00Component0001()
    {
        var r = RunBuild();
        Assert.Equal("map_00_component_0001", r.ComponentId);
    }

    [Fact]
    public void Build_Bbox_MatchesExpected()
    {
        var r = RunBuild();
        Assert.Equal(124, r.BboxX1);
        Assert.Equal(10,  r.BboxY1);
        Assert.Equal(212, r.BboxX2);
        Assert.Equal(69,  r.BboxY2);
        Assert.Equal(89,  r.BboxWidth);
        Assert.Equal(60,  r.BboxHeight);
    }

    // -----------------------------------------------------------------------
    // Lot counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_TotalLotCount_Is12()
    {
        var r = RunBuild();
        Assert.Equal(12, r.TotalResidentialLotCount);
    }

    [Fact]
    public void Build_NorthLotCount_Is6()
    {
        var r = RunBuild();
        Assert.Equal(6, r.NorthFacingLotCount);
    }

    [Fact]
    public void Build_SouthLotCount_Is6()
    {
        var r = RunBuild();
        Assert.Equal(6, r.SouthFacingLotCount);
    }

    [Fact]
    public void Build_EastLotCount_IsZero()
    {
        var r = RunBuild();
        Assert.Equal(0, r.EastFacingLotCount);
    }

    [Fact]
    public void Build_ThroughLotCount_IsZero()
    {
        var r = RunBuild();
        Assert.Equal(0, r.ThroughLotCount);
    }

    // -----------------------------------------------------------------------
    // Lot geometry rules
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_NoDoubleFrontageLots()
    {
        var r = RunBuild();
        // No lot spans Y 12-67 (both north and south zones)
        Assert.DoesNotContain(r.ResidentialParcels, p => p.Y1 <= 12 && p.Y2 >= 67);
    }

    [Fact]
    public void Build_EveryLotHasNorthOrSouthFrontage()
    {
        var r = RunBuild();
        Assert.All(r.ResidentialParcels, p =>
            Assert.True(p.FrontageDirection is "NORTH" or "SOUTH",
                $"Lot {p.ParcelId} has unexpected frontage: {p.FrontageDirection}"));
    }

    [Fact]
    public void Build_NoEastFacingLots()
    {
        var r = RunBuild();
        Assert.DoesNotContain(r.ResidentialParcels, p => p.FrontageDirection == "EAST");
    }

    [Fact]
    public void Build_NorthSouthLotCountsMatch()
    {
        var r = RunBuild();
        Assert.Equal(r.NorthFacingLotCount, r.SouthFacingLotCount);
    }

    [Fact]
    public void Build_LotWidthBalance_MaxMinLeq1()
    {
        var r     = RunBuild();
        var ns    = r.ResidentialParcels.Where(p => p.FrontageDirection is "NORTH" or "SOUTH").ToList();
        int maxW  = ns.Max(p => p.Width);
        int minW  = ns.Min(p => p.Width);
        Assert.True(maxW - minW <= 1, $"Lot width spread {maxW - minW} > 1 (max={maxW}, min={minW})");
    }

    [Fact]
    public void Build_NorthRowCoversFullBboxWidth()
    {
        var r    = RunBuild();
        var lots = r.ResidentialParcels.Where(p => p.FrontageDirection == "NORTH")
                                       .OrderBy(p => p.X1).ToList();
        Assert.Equal(124, lots.First().X1);
        Assert.Equal(212, lots.Last().X2);
        for (int i = 0; i < lots.Count - 1; i++)
            Assert.Equal(lots[i].X2 + 1, lots[i + 1].X1);
    }

    // -----------------------------------------------------------------------
    // Strip records
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_SidewalkStripCount_IsZero()
    {
        var r = RunBuild();
        Assert.Equal(0, r.SidewalkStripCount);
    }

    [Fact]
    public void Build_RearBoundaryStripCount_Is1()
    {
        var r = RunBuild();
        Assert.Equal(1, r.RearBoundaryStripCount);
    }

    [Fact]
    public void Build_InventedAlleyCount_IsZero()
    {
        var r = RunBuild();
        Assert.Equal(0, r.InventedAlleyCount);
    }

    [Fact]
    public void Build_RearBoundaryStrip_KindIsNotAlley()
    {
        var r    = RunBuild();
        var rear = r.SidewalkStrips.FirstOrDefault(s => s.StripKind == "REAR_BOUNDARY");
        Assert.NotNull(rear);
        Assert.NotEqual("ALLEY", rear.StripKind);
    }

    [Fact]
    public void Build_FrontageEdges_OnePerLot()
    {
        var r = RunBuild();
        Assert.Equal(r.TotalResidentialLotCount, r.FrontageEdges.Count);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WriterReady_IsFalse()
    {
        var r = RunBuild();
        Assert.False(r.WriterReady);
    }

    [Fact]
    public void Build_RuntimeValid_IsFalse()
    {
        var r = RunBuild();
        Assert.False(r.RuntimeValid);
    }

    [Fact]
    public void Build_Materialized_IsFalse()
    {
        var r = RunBuild();
        Assert.False(r.Materialized);
    }

    [Fact]
    public void Build_RuntimeProofClaimed_IsFalse()
    {
        var r = RunBuild();
        Assert.False(r.RuntimeProofClaimed);
    }

    [Fact]
    public void Build_PublicPlayablePackagingClaimed_IsFalse()
    {
        var r = RunBuild();
        Assert.False(r.PublicPlayablePackagingClaimed);
    }

    [Fact]
    public void Build_SandboxOnly_IsTrue()
    {
        var r = RunBuild();
        Assert.True(r.SandboxOnly);
    }

    [Fact]
    public void Build_InventedAlleysEnabled_IsFalse()
    {
        var r = RunBuild();
        Assert.False(r.InventedAlleysEnabled);
    }

    // -----------------------------------------------------------------------
    // Checks (25 total, pre-finalize)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CheckCount_Is25()
    {
        var r = RunBuild();
        Assert.Equal(25, r.CheckCount);
    }

    [Fact]
    public void Build_EarlyChecks1Through15_AllPass()
    {
        var r = RunBuild();
        // Checks 1-15 are resolved during Build(); 16-20 are PENDING
        var earlyFail = r.Checks.Take(15).Where(c => c.CheckStatus == "FAIL").ToList();
        Assert.Empty(earlyFail);
    }

    [Fact]
    public void Build_Checks16To20_ArePending()
    {
        var r = RunBuild();
        var pending = r.Checks.Skip(15).Take(5).Where(c => c.CheckStatus == "PENDING").ToList();
        Assert.Equal(5, pending.Count);
    }

    [Fact]
    public void Build_ClaimBoundaryChecks21To25_AllPass()
    {
        var r = RunBuild();
        var claimChecks = r.Checks.Skip(20).Take(5).Where(c => c.CheckStatus == "PASS").ToList();
        Assert.Equal(5, claimChecks.Count);
    }

    // -----------------------------------------------------------------------
    // PNG rendering
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCleanParcelPng_Is256x256()
    {
        var r    = RunBuild();
        var png  = MakeBuilder().RenderCleanParcelPngBytes(r);
        var (w, h) = ReadPngDimensions(png);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    [Fact]
    public void RenderDebugParcelPng_Is256x256()
    {
        var r    = RunBuild();
        var png  = MakeBuilder().RenderDebugParcelPngBytes(r);
        var (w, h) = ReadPngDimensions(png);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    [Fact]
    public void RenderOverlayParcelPng_NoRawSource_Is256x256()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderOverlayParcelPngBytes(r, null);
        var (w, h) = ReadPngDimensions(png);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    // -----------------------------------------------------------------------
    // FinalizeAfterOutputs
    // -----------------------------------------------------------------------

    [Fact]
    public void FinalizeAfterOutputs_WithValidPngsAndAsciiFiles_AllChecksPass()
    {
        var b   = MakeBuilder();
        var r   = b.Build(_tempDir);

        // Write the outputs manually
        string cleanPng   = Path.Combine(_tempDir, "clean.png");
        string debugPng   = Path.Combine(_tempDir, "debug.png");
        string overlayPng = Path.Combine(_tempDir, "overlay.png");
        string html       = Path.Combine(_tempDir, "viewer.html");
        string readme     = Path.Combine(_tempDir, "README.txt");

        File.WriteAllBytes(cleanPng,   b.RenderCleanParcelPngBytes(r));
        File.WriteAllBytes(debugPng,   b.RenderDebugParcelPngBytes(r));
        File.WriteAllBytes(overlayPng, b.RenderOverlayParcelPngBytes(r, null));
        File.WriteAllText(html,        b.RenderHtml(r));
        File.WriteAllText(readme,      b.RenderReadme(r));

        var final = b.FinalizeAfterOutputs(r, _tempDir,
            cleanPng, debugPng, overlayPng, html, readme);

        Assert.Equal(0, final.FailedCheckCount);
        Assert.True(final.IsValid);
        Assert.Contains("COMPLETE", final.Verdict);
    }

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderJson_ContainsComponentId()
    {
        var r   = RunBuild();
        var json = MakeBuilder().RenderJson(r);
        Assert.Contains("map_00_component_0001", json);
    }

    [Fact]
    public void RenderParcelsCsv_Has12DataRows()
    {
        var r    = RunBuild();
        var csv  = MakeBuilder().RenderParcelsCsv(r);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // 1 header + 12 data rows
        Assert.Equal(13, rows.Length);
    }

    [Fact]
    public void RenderChecksCsv_Has25DataRows()
    {
        var r    = RunBuild();
        var csv  = MakeBuilder().RenderChecksCsv(r);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(26, rows.Length); // 1 header + 25 checks
    }

    [Fact]
    public void RenderHtml_IsAsciiOnly()
    {
        var r    = RunBuild();
        var html = MakeBuilder().RenderHtml(r);
        Assert.All(html, ch => Assert.True(ch < 128, $"Non-ASCII char 0x{(int)ch:X2}"));
    }

    [Fact]
    public void RenderReadme_IsAsciiOnly()
    {
        var r      = RunBuild();
        var readme = MakeBuilder().RenderReadme(r);
        Assert.All(readme, ch => Assert.True(ch < 128, $"Non-ASCII char 0x{(int)ch:X2}"));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (int w, int h) ReadPngDimensions(byte[] bytes)
    {
        // PNG: sig(8)+chunkLen(4)+"IHDR"(4)+width(4be)+height(4be)
        int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return (w, h);
    }
}
