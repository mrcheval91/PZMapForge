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
    public void Build_RearBoundaryStripCount_IsZero()
    {
        var r = RunBuild();
        Assert.Equal(0, r.RearBoundaryStripCount);
    }

    [Fact]
    public void Build_InventedAlleyCount_IsZero()
    {
        var r = RunBuild();
        Assert.Equal(0, r.InventedAlleyCount);
    }

    [Fact]
    public void Build_NorthSouthRowsAreAdjacentNoGap()
    {
        var r         = RunBuild();
        var northLots = r.ResidentialParcels.Where(p => p.FrontageDirection == "NORTH").ToList();
        var southLots = r.ResidentialParcels.Where(p => p.FrontageDirection == "SOUTH").ToList();
        Assert.True(northLots.Any() && southLots.Any(), "Expected north and south lots");
        Assert.True(northLots.All(p => p.Y2 == 39), "All north lots must end at Y2=39");
        Assert.True(southLots.All(p => p.Y1 == 40), "All south lots must start at Y1=40");
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
    // PNG pixel correctness
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCleanParcelPng_AllPixelsInBlockAreAllowedLotShades()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderCleanParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        var allowed = new HashSet<(byte R, byte G, byte B)>
        {
            (200, 168, 120),
            (176, 140,  96),
            (152, 116,  76),
        };
        for (int x = 124; x <= 212; x++)
            for (int y = 10; y <= 69; y++)
            {
                var px = bmp.GetPixel(x, y);
                Assert.True(allowed.Contains((px.R, px.G, px.B)),
                    $"Non-lot pixel ({px.R},{px.G},{px.B}) at ({x},{y}) in clean PNG");
            }
    }

    [Fact]
    public void RenderCleanParcelPng_RowBoundaryY39Y40_AreLotShades()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderCleanParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        var allowed = new HashSet<(byte R, byte G, byte B)>
        {
            (200, 168, 120), (176, 140, 96), (152, 116, 76),
        };
        int midX = (124 + 212) / 2;
        var p39 = bmp.GetPixel(midX, 39);
        var p40 = bmp.GetPixel(midX, 40);
        Assert.True(allowed.Contains((p39.R, p39.G, p39.B)),
            $"Y=39 at x={midX} is not a lot shade: ({p39.R},{p39.G},{p39.B})");
        Assert.True(allowed.Contains((p40.R, p40.G, p40.B)),
            $"Y=40 at x={midX} is not a lot shade: ({p40.R},{p40.G},{p40.B})");
        Assert.NotEqual(p39.ToArgb(), p40.ToArgb());
    }

    [Fact]
    public void RenderCleanParcelPng_NoBackgroundPixelInsideBbox()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderCleanParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        // Interior = inside the 1px cyan bbox border: X 125-211, Y 11-68
        for (int x = 125; x <= 211; x++)
            for (int y = 11; y <= 68; y++)
            {
                var px = bmp.GetPixel(x, y);
                Assert.False(px.R == 18 && px.G == 18 && px.B == 24,
                    $"Background pixel (18,18,24) found at ({x},{y}) inside bbox");
            }
    }

    [Fact]
    public void RenderDebugParcelPng_NoBorderColorInsideBboxInterior()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderDebugParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        for (int x = 125; x <= 211; x++)
            for (int y = 11; y <= 68; y++)
            {
                var px = bmp.GetPixel(x, y);
                Assert.False(px.R == 10 && px.G == 10 && px.B == 18,
                    $"Black border color (10,10,18) found at ({x},{y}) inside bbox in debug PNG");
            }
    }

    [Fact]
    public void RenderCleanParcelPng_AdjacentLotsInNorthRow_HaveDifferentShades()
    {
        var r    = RunBuild();
        var png  = MakeBuilder().RenderCleanParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        var lots = r.ResidentialParcels.Where(p => p.FrontageDirection == "NORTH")
                                       .OrderBy(p => p.X1).ToList();
        for (int i = 0; i < lots.Count - 1; i++)
        {
            int sampleY = (lots[i].Y1 + lots[i].Y2) / 2 + 1; // +1 to skip center tick pixel
            int midXA   = (lots[i].X1   + lots[i].X2)   / 2;
            int midXB   = (lots[i+1].X1 + lots[i+1].X2) / 2;
            var ca = bmp.GetPixel(midXA, sampleY);
            var cb = bmp.GetPixel(midXB, sampleY);
            Assert.False(ca.ToArgb() == cb.ToArgb(),
                $"Adjacent north lots {i} and {i+1} have same shade at y={sampleY}");
        }
    }

    [Fact]
    public void RenderCleanParcelPng_AtLeast2DistinctParcelShades()
    {
        var r    = RunBuild();
        var png  = MakeBuilder().RenderCleanParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        var shades = new HashSet<int>();
        foreach (var lot in r.ResidentialParcels.Where(p => p.FrontageDirection == "NORTH").OrderBy(p => p.X1))
        {
            int midX    = (lot.X1 + lot.X2) / 2;
            int sampleY = (lot.Y1 + lot.Y2) / 2 + 1;
            shades.Add(bmp.GetPixel(midX, sampleY).ToArgb());
        }
        Assert.True(shades.Count >= 2, $"Expected >= 2 distinct parcel shades, got {shades.Count}");
    }

    [Fact]
    public void RenderCleanParcelPng_NoCyanPixels()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderCleanParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        for (int x = 0; x < 256; x++)
            for (int y = 0; y < 256; y++)
            {
                var px = bmp.GetPixel(x, y);
                Assert.False(px.R == 40 && px.G == 192 && px.B == 192,
                    $"Cyan pixel (40,192,192) at ({x},{y}) in clean parcel PNG");
            }
    }

    [Fact]
    public void RenderDebugParcelPng_NoCyanPixels()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderDebugParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        for (int x = 0; x < 256; x++)
            for (int y = 0; y < 256; y++)
            {
                var px = bmp.GetPixel(x, y);
                Assert.False(px.R == 40 && px.G == 192 && px.B == 192,
                    $"Cyan pixel (40,192,192) at ({x},{y}) in debug parcel PNG");
            }
    }

    [Fact]
    public void RenderOverlayParcelPng_NoCyanPixels()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderOverlayParcelPngBytes(r, null);
        using var bmp = LoadBitmap(png);
        for (int x = 0; x < 256; x++)
            for (int y = 0; y < 256; y++)
            {
                var px = bmp.GetPixel(x, y);
                Assert.False(px.R == 40 && px.G == 192 && px.B == 192,
                    $"Cyan pixel (40,192,192) at ({x},{y}) in overlay parcel PNG");
            }
    }

    [Fact]
    public void RenderHtml_NoCyanOrBboxReferences()
    {
        var r    = RunBuild();
        var html = MakeBuilder().RenderHtml(r);
        Assert.DoesNotContain("cyan",    html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#28c0c0", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bbox",    html, StringComparison.Ordinal);
        Assert.DoesNotContain("bbox",    html, StringComparison.Ordinal);
        Assert.DoesNotContain("blue",    html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#3a5eae", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#4a6ebe", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#2a4e9e", html, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Street adjacency
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_NorthStreetAdjacency_IsTrue()
    {
        Assert.True(RunBuild().NorthStreetAdjacency);
    }

    [Fact]
    public void Build_SouthStreetAdjacency_IsTrue()
    {
        Assert.True(RunBuild().SouthStreetAdjacency);
    }

    [Fact]
    public void Build_EastStreetAdjacency_IsFalse()
    {
        Assert.False(RunBuild().EastStreetAdjacency);
    }

    // -----------------------------------------------------------------------
    // Frontage edge geometry
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_FrontageEdges_NoEastFacing()
    {
        Assert.DoesNotContain(RunBuild().FrontageEdges, e => e.FrontageDirection == "EAST");
    }

    [Fact]
    public void Build_NorthFrontageEdges_OnNorthY()
    {
        var r          = RunBuild();
        var northEdges = r.FrontageEdges.Where(e => e.FrontageDirection == "NORTH").ToList();
        Assert.True(northEdges.Any(), "Expected north frontage edges");
        Assert.All(northEdges, e =>
        {
            Assert.Equal(10, e.Y1);
            Assert.Equal(10, e.Y2);
        });
    }

    [Fact]
    public void Build_SouthFrontageEdges_OnSouthY()
    {
        var r          = RunBuild();
        var southEdges = r.FrontageEdges.Where(e => e.FrontageDirection == "SOUTH").ToList();
        Assert.True(southEdges.Any(), "Expected south frontage edges");
        Assert.All(southEdges, e =>
        {
            Assert.Equal(69, e.Y1);
            Assert.Equal(69, e.Y2);
        });
    }

    [Fact]
    public void Build_FrontageEdges_LengthEqualsParentLotWidth()
    {
        var r = RunBuild();
        foreach (var edge in r.FrontageEdges)
        {
            var parcel = r.ResidentialParcels.First(p => p.ParcelId == edge.ParcelId);
            Assert.Equal(parcel.Width, edge.LengthTiles);
        }
    }

    // -----------------------------------------------------------------------
    // Calculated column geometry
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ColumnWidths_MatchExpected_15x5and14x1()
    {
        var r       = RunBuild();
        var nLots   = r.ResidentialParcels.Where(p => p.FrontageDirection == "NORTH")
                                          .OrderBy(p => p.X1).ToList();
        var widths  = nLots.Select(p => p.Width).ToList();
        Assert.Equal(new[] { 15, 15, 15, 15, 15, 14 }, widths);
    }

    [Fact]
    public void Build_NorthAndSouthRows_UseIdenticalXRanges()
    {
        var r     = RunBuild();
        var nLots = r.ResidentialParcels.Where(p => p.FrontageDirection == "NORTH").OrderBy(p => p.X1).ToList();
        var sLots = r.ResidentialParcels.Where(p => p.FrontageDirection == "SOUTH").OrderBy(p => p.X1).ToList();
        Assert.Equal(nLots.Count, sLots.Count);
        for (int i = 0; i < nLots.Count; i++)
        {
            Assert.Equal(nLots[i].X1, sLots[i].X1);
            Assert.Equal(nLots[i].X2, sLots[i].X2);
        }
    }

    // -----------------------------------------------------------------------
    // Forbidden color pixel tests (all 3 PNGs)
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCleanParcelPng_NoForbiddenColors()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderCleanParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        AssertNoForbiddenPixels(bmp, "clean parcel PNG");
    }

    [Fact]
    public void RenderDebugParcelPng_NoForbiddenColors()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderDebugParcelPngBytes(r);
        using var bmp = LoadBitmap(png);
        AssertNoForbiddenPixels(bmp, "debug parcel PNG");
    }

    [Fact]
    public void RenderOverlayParcelPng_NoForbiddenColors()
    {
        var r   = RunBuild();
        var png = MakeBuilder().RenderOverlayParcelPngBytes(r, null);
        using var bmp = LoadBitmap(png);
        AssertNoForbiddenPixels(bmp, "overlay parcel PNG");
    }

    private static void AssertNoForbiddenPixels(System.Drawing.Bitmap bmp, string label)
    {
        var forbidden = new HashSet<(byte R, byte G, byte B)>
        {
            (58,  94,  174),
            (74,  110, 190),
            (42,  78,  158),
            (40,  192, 192),
            (210, 230, 255),
        };
        for (int x = 0; x < bmp.Width; x++)
            for (int y = 0; y < bmp.Height; y++)
            {
                var px = bmp.GetPixel(x, y);
                Assert.False(forbidden.Contains((px.R, px.G, px.B)),
                    $"Forbidden color ({px.R},{px.G},{px.B}) at ({x},{y}) in {label}");
                bool blueish = px.B >= 80 && px.B >= px.R + 25 && px.B >= px.G + 10;
                Assert.False(blueish,
                    $"Blue-ish pixel ({px.R},{px.G},{px.B}) at ({x},{y}) in {label}");
            }
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

    private static System.Drawing.Bitmap LoadBitmap(byte[] pngBytes)
    {
        using var ms  = new System.IO.MemoryStream(pngBytes);
        using var tmp = new System.Drawing.Bitmap(ms);
        return tmp.Clone(new System.Drawing.Rectangle(0, 0, tmp.Width, tmp.Height), tmp.PixelFormat);
    }
}
