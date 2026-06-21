using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderLotpackTileWalkBuilderTests
{
    // minimal > visible: matches real-world lotpack behaviour (visible is SMALLER)
    // minimal: 2-byte header + "TILEDATA"(8) + "PACKFOOT"(8) + 10×8 null = 98 bytes
    // visible: 2-byte header + "TILEDATA"(8) + "PACKFOOT"(8) + 7×8 variant = 74 bytes
    private static byte[] MakeMinimal()
    {
        var b = new List<byte> { 0xAA, 0xBB };
        b.AddRange(System.Text.Encoding.ASCII.GetBytes("TILEDATA"));
        b.AddRange(System.Text.Encoding.ASCII.GetBytes("PACKFOOT"));
        b.AddRange(Enumerable.Repeat((byte)0x00, 80));
        return b.ToArray(); // 98 bytes
    }

    private static byte[] MakeVisible()
    {
        var b = new List<byte> { 0xAA, 0xBB };
        b.AddRange(System.Text.Encoding.ASCII.GetBytes("TILEDATA"));
        b.AddRange(System.Text.Encoding.ASCII.GetBytes("PACKFOOT"));
        // 7 records with non-zero variant data
        for (int i = 0; i < 7; i++)
            b.AddRange(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
        return b.ToArray(); // 74 bytes
    }

    private static string WriteTempFile(string dir, string name, byte[] data)
    {
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        File.WriteAllBytes(path, data);
        return path;
    }

    private static string TempDir() =>
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36e-core-test.local", Path.GetRandomFileName());

    [Fact]
    public void ValidBuild_VisibleSmaller_IsValid_AllChecksPass()
    {
        var dir    = TempDir();
        string min = WriteTempFile(dir, "min.lotpack", MakeMinimal());
        string vis = WriteTempFile(dir, "vis.lotpack", MakeVisible());
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderLotpackTileWalkBuilder();
        var result  = builder.Build(min, vis, out_);

        Assert.True(result.IsValid, string.Join("; ", result.Checks.Where(c => c.CheckStatus == "FAIL").Select(c => c.CheckId)));
        Assert.All(result.Checks, c => Assert.Equal("PASS", c.CheckStatus));
        Assert.Equal("MAP36E_LOTPACK_TILE_WALK_V1", result.Format);
        Assert.Equal("MAP36E_LOTPACK_TILE_WALK_COMPLETE", result.Verdict);
    }

    [Fact]
    public void ValidBuild_SizeDelta_IsNegative_ClassifiedCorrectly()
    {
        var dir    = TempDir();
        string min = WriteTempFile(dir, "min.lotpack", MakeMinimal());
        string vis = WriteTempFile(dir, "vis.lotpack", MakeVisible());
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderLotpackTileWalkBuilder();
        var result  = builder.Build(min, vis, out_);

        Assert.True(result.SizeDelta < 0);
        Assert.Equal("TRUNCATION_OR_REPACK_DELTA_UNVERIFIED", result.SizeDeltaClassification);
        Assert.Equal(MakeVisible().Length - MakeMinimal().Length, result.SizeDelta);
    }

    [Fact]
    public void ValidBuild_StringTable_ContainsTileData()
    {
        var dir    = TempDir();
        string min = WriteTempFile(dir, "min.lotpack", MakeMinimal());
        string vis = WriteTempFile(dir, "vis.lotpack", MakeVisible());
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderLotpackTileWalkBuilder();
        var result  = builder.Build(min, vis, out_);

        Assert.NotEmpty(result.StringTable);
        Assert.Contains(result.StringTable, s => s.Preview.Contains("TILEDATA"));
    }

    [Fact]
    public void ValidBuild_ByteFrequency_Has256Entries()
    {
        var dir    = TempDir();
        string min = WriteTempFile(dir, "min.lotpack", MakeMinimal());
        string vis = WriteTempFile(dir, "vis.lotpack", MakeVisible());
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderLotpackTileWalkBuilder();
        var result  = builder.Build(min, vis, out_);

        Assert.Equal(256, result.ByteFrequency.Count);
        for (int i = 0; i < 256; i++)
            Assert.Equal(i, result.ByteFrequency[i].ByteValue);
    }

    [Fact]
    public void ValidBuild_OffsetSamples_EmittedAndContainsAnchor()
    {
        var dir    = TempDir();
        string min = WriteTempFile(dir, "min.lotpack", MakeMinimal());
        string vis = WriteTempFile(dir, "vis.lotpack", MakeVisible());
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderLotpackTileWalkBuilder();
        var result  = builder.Build(min, vis, out_);

        Assert.NotEmpty(result.OffsetSamples);
        Assert.Contains(result.OffsetSamples, s => s.SampleOffset == 0 && s.Source == "minimal");
        Assert.Contains(result.OffsetSamples, s => s.SampleOffset == 0 && s.Source == "visible");
    }

    [Fact]
    public void Build_OutputRootMissingLocal_CheckFails()
    {
        var dir    = Path.Combine(Path.GetTempPath(), "pzmapforge-map36e-nosandbox-" + Path.GetRandomFileName());
        string min = WriteTempFile(dir, "min.lotpack", MakeMinimal());
        string vis = WriteTempFile(dir, "vis.lotpack", MakeVisible());
        string out_ = Path.Combine(dir, "out-no-sandbox");

        var builder = new DeadMtlWorldBuilderLotpackTileWalkBuilder();
        var result  = builder.Build(min, vis, out_);

        var check = result.Checks.Single(c => c.CheckId == "MAP36E_OUTPUT_ROOT_CONTAINS_LOCAL");
        Assert.Equal("FAIL", check.CheckStatus);
    }
}
