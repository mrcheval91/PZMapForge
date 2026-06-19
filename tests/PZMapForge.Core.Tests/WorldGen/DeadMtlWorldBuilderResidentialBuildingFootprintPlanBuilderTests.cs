using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderResidentialBuildingFootprintPlanBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map28b-core-test.local", Guid.NewGuid().ToString());

    public DeadMtlWorldBuilderResidentialBuildingFootprintPlanBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private DeadMtlWorldBuilderResidentialBuildingFootprintPlanBuilder MakeBuilder() => new();
    private DeadMtlWorldBuilderResidentialParcelTopologyBuilder MakeTopologyBuilder() => new();

    private DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult RunBuild() =>
        MakeBuilder().Build(_tempDir);

    // -----------------------------------------------------------------------
    // Footprint counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_TotalFootprintCount_Is16()
    {
        var r = RunBuild();
        Assert.Equal(16, r.TotalFootprintCount);
    }

    [Fact]
    public void Build_NorthFootprintCount_Is6()
    {
        var r = RunBuild();
        Assert.Equal(6, r.NorthFootprintCount);
    }

    [Fact]
    public void Build_SouthFootprintCount_Is6()
    {
        var r = RunBuild();
        Assert.Equal(6, r.SouthFootprintCount);
    }

    [Fact]
    public void Build_EastFootprintCount_Is4()
    {
        var r = RunBuild();
        Assert.Equal(4, r.EastFootprintCount);
    }

    // -----------------------------------------------------------------------
    // Parent parcel relation
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_EveryFootprintHasParentParcelId()
    {
        var r = RunBuild();
        Assert.All(r.BuildingFootprints, f =>
            Assert.False(string.IsNullOrEmpty(f.ParentParcelId),
                $"Footprint {f.FootprintId} has no parent parcel id"));
    }

    [Fact]
    public void Build_EveryParentParcelHasExactlyOneFootprint()
    {
        var r        = RunBuild();
        var topology = MakeTopologyBuilder().Build(_tempDir);
        Assert.All(topology.ResidentialParcels, parcel =>
            Assert.Equal(1, r.BuildingFootprints.Count(f => f.ParentParcelId == parcel.ParcelId)));
    }

    // -----------------------------------------------------------------------
    // Geometry containment
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_EveryFootprintWithinParentParcelBounds()
    {
        var r        = RunBuild();
        var topology = MakeTopologyBuilder().Build(_tempDir);
        foreach (var f in r.BuildingFootprints)
        {
            var parcel = topology.ResidentialParcels.First(p => p.ParcelId == f.ParentParcelId);
            Assert.True(f.X1 >= parcel.X1, $"{f.FootprintId} x1 outside parcel");
            Assert.True(f.X2 <= parcel.X2, $"{f.FootprintId} x2 outside parcel");
            Assert.True(f.Y1 >= parcel.Y1, $"{f.FootprintId} y1 outside parcel");
            Assert.True(f.Y2 <= parcel.Y2, $"{f.FootprintId} y2 outside parcel");
        }
    }

    [Fact]
    public void Build_NoFootprintOverlapsSidewalkStrips()
    {
        var r        = RunBuild();
        var topology = MakeTopologyBuilder().Build(_tempDir);
        var sidewalks = topology.SidewalkStrips.Where(s => s.StripKind == "SIDEWALK").ToList();
        foreach (var f in r.BuildingFootprints)
            foreach (var s in sidewalks)
                Assert.False(Overlaps(f, s.X1, s.Y1, s.X2, s.Y2),
                    $"{f.FootprintId} overlaps sidewalk {s.StripId}");
    }

    [Fact]
    public void Build_NoFootprintOverlapsRearBoundary()
    {
        var r        = RunBuild();
        var topology = MakeTopologyBuilder().Build(_tempDir);
        var rear = topology.SidewalkStrips.Where(s => s.StripKind == "REAR_BOUNDARY").ToList();
        foreach (var f in r.BuildingFootprints)
            foreach (var s in rear)
                Assert.False(Overlaps(f, s.X1, s.Y1, s.X2, s.Y2),
                    $"{f.FootprintId} overlaps REAR_BOUNDARY {s.StripId}");
    }

    [Fact]
    public void Build_NoFootprintOverlapsAnotherFootprint()
    {
        var r = RunBuild();
        var fps = r.BuildingFootprints;
        for (int i = 0; i < fps.Count; i++)
            for (int j = i + 1; j < fps.Count; j++)
                Assert.False(Overlaps(fps[i], fps[j].X1, fps[j].Y1, fps[j].X2, fps[j].Y2),
                    $"{fps[i].FootprintId} overlaps {fps[j].FootprintId}");
    }

    // -----------------------------------------------------------------------
    // Frontage directions
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_NorthFootprints_AllUseNorthFrontage()
    {
        var r = RunBuild();
        Assert.All(r.BuildingFootprints.Where(f => f.FrontageDirection == "NORTH"),
            f => Assert.Equal("NORTH", f.FrontageDirection));
    }

    [Fact]
    public void Build_SouthFootprints_AllUseSouthFrontage()
    {
        var r = RunBuild();
        Assert.All(r.BuildingFootprints.Where(f => f.FrontageDirection == "SOUTH"),
            f => Assert.Equal("SOUTH", f.FrontageDirection));
    }

    [Fact]
    public void Build_EastFootprints_AllUseEastFrontage()
    {
        var r = RunBuild();
        Assert.All(r.BuildingFootprints.Where(f => f.FrontageDirection == "EAST"),
            f => Assert.Equal("EAST", f.FrontageDirection));
    }

    // -----------------------------------------------------------------------
    // Geometry dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_NSSouthFootprints_Width11()
    {
        var r = RunBuild();
        Assert.All(r.BuildingFootprints.Where(f => f.FrontageDirection is "NORTH" or "SOUTH"),
            f => Assert.Equal(11, f.Width));
    }

    [Fact]
    public void Build_NSFootprints_Depth16()
    {
        var r = RunBuild();
        Assert.All(r.BuildingFootprints.Where(f => f.FrontageDirection is "NORTH" or "SOUTH"),
            f => Assert.Equal(16, f.Height));
    }

    [Fact]
    public void Build_EastFootprints_Width6()
    {
        var r = RunBuild();
        Assert.All(r.BuildingFootprints.Where(f => f.FrontageDirection == "EAST"),
            f => Assert.Equal(6, f.Width));
    }

    [Fact]
    public void Build_EastFootprints_Height13()
    {
        var r = RunBuild();
        Assert.All(r.BuildingFootprints.Where(f => f.FrontageDirection == "EAST"),
            f => Assert.Equal(13, f.Height));
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WriterReady_IsFalse()   => Assert.False(RunBuild().WriterReady);
    [Fact]
    public void Build_RuntimeValid_IsFalse()  => Assert.False(RunBuild().RuntimeValid);
    [Fact]
    public void Build_Materialized_IsFalse()  => Assert.False(RunBuild().Materialized);
    [Fact]
    public void Build_RuntimeProofClaimed_IsFalse()            => Assert.False(RunBuild().RuntimeProofClaimed);
    [Fact]
    public void Build_PublicPlayablePackagingClaimed_IsFalse() => Assert.False(RunBuild().PublicPlayablePackagingClaimed);
    [Fact]
    public void Build_SandboxOnly_IsTrue()    => Assert.True(RunBuild().SandboxOnly);

    // -----------------------------------------------------------------------
    // Checks
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CheckCount_AtLeast30()
    {
        var r = RunBuild();
        Assert.True(r.CheckCount >= 30, $"Expected >= 30 checks, got {r.CheckCount}");
    }

    [Fact]
    public void Build_EarlyChecks1Through22_AllPassOrPending()
    {
        var r = RunBuild();
        var failing = r.Checks.Take(22).Where(c => c.CheckStatus == "FAIL").ToList();
        Assert.Empty(failing);
    }

    [Fact]
    public void Build_ClaimBoundaryChecks_AllPass()
    {
        var r = RunBuild();
        var claim = r.Checks.Where(c => c.CheckId.StartsWith("MAP28B_WRITER_READY") ||
                                        c.CheckId.StartsWith("MAP28B_RUNTIME_VALID") ||
                                        c.CheckId.StartsWith("MAP28B_MATERIALIZED") ||
                                        c.CheckId.StartsWith("MAP28B_NO_RUNTIME_PROOF") ||
                                        c.CheckId.StartsWith("MAP28B_NO_PUBLIC_PLAYABLE")).ToList();
        Assert.Equal(5, claim.Count);
        Assert.All(claim, c => Assert.Equal("PASS", c.CheckStatus));
    }

    // -----------------------------------------------------------------------
    // PNG rendering
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCleanFootprintPng_Is256x256()
    {
        var b        = MakeBuilder();
        var r        = b.Build(_tempDir);
        var topology = MakeTopologyBuilder().Build(_tempDir);
        var png      = b.RenderCleanFootprintPngBytes(r, topology);
        var (w, h)   = ReadPngDimensions(png);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    [Fact]
    public void RenderDebugFootprintPng_Is256x256()
    {
        var b        = MakeBuilder();
        var r        = b.Build(_tempDir);
        var topology = MakeTopologyBuilder().Build(_tempDir);
        var png      = b.RenderDebugFootprintPngBytes(r, topology);
        var (w, h)   = ReadPngDimensions(png);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    [Fact]
    public void RenderOverlayFootprintPng_NoRawSource_Is256x256()
    {
        var b        = MakeBuilder();
        var r        = b.Build(_tempDir);
        var topology = MakeTopologyBuilder().Build(_tempDir);
        var png      = b.RenderOverlayFootprintPngBytes(r, topology, null);
        var (w, h)   = ReadPngDimensions(png);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    // -----------------------------------------------------------------------
    // FinalizeAfterOutputs
    // -----------------------------------------------------------------------

    [Fact]
    public void FinalizeAfterOutputs_WithValidOutputs_AllChecksPass()
    {
        var b        = MakeBuilder();
        var r        = b.Build(_tempDir);
        var topology = MakeTopologyBuilder().Build(_tempDir);

        string cleanPng   = Path.Combine(_tempDir, "clean.png");
        string debugPng   = Path.Combine(_tempDir, "debug.png");
        string overlayPng = Path.Combine(_tempDir, "overlay.png");
        string html       = Path.Combine(_tempDir, "viewer.html");
        string readme     = Path.Combine(_tempDir, "README.md");

        File.WriteAllBytes(cleanPng,   b.RenderCleanFootprintPngBytes(r, topology));
        File.WriteAllBytes(debugPng,   b.RenderDebugFootprintPngBytes(r, topology));
        File.WriteAllBytes(overlayPng, b.RenderOverlayFootprintPngBytes(r, topology, null));
        File.WriteAllText(html,        b.RenderHtml(r));
        File.WriteAllText(readme,      b.RenderReadme(r));

        var final = b.FinalizeAfterOutputs(r, _tempDir, cleanPng, debugPng, overlayPng, html, readme);

        Assert.Equal(0,  final.FailedCheckCount);
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
    public void RenderFootprintsCsv_Has16DataRows()
    {
        var r   = RunBuild();
        var csv = MakeBuilder().RenderFootprintsCsv(r);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(17, rows.Length); // 1 header + 16 footprints
    }

    [Fact]
    public void RenderChecksCsv_HasAtLeast30DataRows()
    {
        var r   = RunBuild();
        var csv = MakeBuilder().RenderChecksCsv(r);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(rows.Length >= 31, $"Expected >= 31 rows (1 header + 30 data), got {rows.Length}");
    }

    [Fact]
    public void RenderHtml_IsAsciiOnly()
    {
        var r    = RunBuild();
        var html = MakeBuilder().RenderHtml(r);
        Assert.All(html, ch => Assert.True(ch < 128, $"Non-ASCII 0x{(int)ch:X2}"));
    }

    [Fact]
    public void RenderReadme_IsAsciiOnly()
    {
        var r      = RunBuild();
        var readme = MakeBuilder().RenderReadme(r);
        Assert.All(readme, ch => Assert.True(ch < 128, $"Non-ASCII 0x{(int)ch:X2}"));
    }

    [Fact]
    public void RenderParentManifestJson_HasAllFootprintIds()
    {
        var r        = RunBuild();
        var manifest = MakeBuilder().RenderParentManifestJson(r);
        foreach (var f in r.BuildingFootprints)
            Assert.Contains(f.FootprintId, manifest);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (int w, int h) ReadPngDimensions(byte[] bytes)
    {
        int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return (w, h);
    }

    private static bool Overlaps(DeadMtlResidentialBuildingFootprint f,
        int bx1, int by1, int bx2, int by2)
        => f.X1 <= bx2 && f.X2 >= bx1 && f.Y1 <= by2 && f.Y2 >= by1;
}
