using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderChunkdataHexDisassemblyBuilderTests
{
    private static string MakeTempDir() =>
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36d-core-test.local", Path.GetRandomFileName());

    private static string WriteFixtureFile(string dir, string name, byte[] data)
    {
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        File.WriteAllBytes(path, data);
        return path;
    }

    // Fixture: 2-byte header + 8×10 = 82 bytes (minimal)
    //          2-byte header + 8×20 = 162 bytes (visible)
    // Prefix: 2 bytes (0xAB, 0xCD — header), then diverge immediately
    private static byte[] MakeMinimal()
    {
        var b = new byte[2 + 8 * 10];
        b[0] = 0xAB; b[1] = 0xCD;
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 8; j++)
                b[2 + i * 8 + j] = (byte)(i + 1);
        return b;
    }

    private static byte[] MakeVisible()
    {
        var b = new byte[2 + 8 * 20];
        b[0] = 0xAB; b[1] = 0xCD;
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 8; j++)
                b[2 + i * 8 + j] = (byte)(i + 1);
        // extra 10 records with different values
        for (int i = 10; i < 20; i++)
            for (int j = 0; j < 8; j++)
                b[2 + i * 8 + j] = (byte)(0x80 + i);
        return b;
    }

    [Fact]
    public void ValidBuild_BothFilesPresent_IsValid_AllChecksPass()
    {
        var dir = MakeTempDir();
        byte[] minData = MakeMinimal();
        byte[] visData = MakeVisible();
        string minPath = WriteFixtureFile(dir, "chunkdata_min.bin", minData);
        string visPath = WriteFixtureFile(dir, "chunkdata_vis.bin", visData);
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataHexDisassemblyBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        Assert.True(result.IsValid);
        Assert.All(result.Checks, c => Assert.Equal("PASS", c.CheckStatus));
        Assert.Equal("MAP36D_CHUNKDATA_HEX_DISASSEMBLY_V1", result.Format);
        Assert.Equal("MAP36D_CHUNKDATA_HEX_DISASSEMBLY_COMPLETE", result.Verdict);
    }

    [Fact]
    public void ValidBuild_SizesAndDelta_AreCorrect()
    {
        var dir = MakeTempDir();
        byte[] minData = MakeMinimal();
        byte[] visData = MakeVisible();
        string minPath = WriteFixtureFile(dir, "chunkdata_min.bin", minData);
        string visPath = WriteFixtureFile(dir, "chunkdata_vis.bin", visData);
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataHexDisassemblyBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        Assert.Equal(minData.Length, result.MinimalSize);
        Assert.Equal(visData.Length, result.VisibleSize);
        Assert.Equal(visData.Length - minData.Length, result.SizeDelta);
        Assert.True(result.SizeDelta > 0);
    }

    [Fact]
    public void ValidBuild_CandidateRecordSizes_ContainsExpectedSizes()
    {
        var dir = MakeTempDir();
        byte[] minData = MakeMinimal();
        byte[] visData = MakeVisible();
        string minPath = WriteFixtureFile(dir, "chunkdata_min.bin", minData);
        string visPath = WriteFixtureFile(dir, "chunkdata_vis.bin", visData);
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataHexDisassemblyBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        Assert.Equal(8, result.CandidateRecordSizes.Count);
        int[] expectedSizes = { 4, 8, 12, 16, 24, 32, 48, 64 };
        Assert.Equal(expectedSizes, result.CandidateRecordSizes.Select(r => r.RecordSize).ToArray());
        Assert.All(result.CandidateRecordSizes, r => Assert.Equal("GUESS_NOT_VERIFIED", r.Label));
    }

    [Fact]
    public void ValidBuild_RecordSamples_Emitted()
    {
        var dir = MakeTempDir();
        byte[] minData = MakeMinimal();
        byte[] visData = MakeVisible();
        string minPath = WriteFixtureFile(dir, "chunkdata_min.bin", minData);
        string visPath = WriteFixtureFile(dir, "chunkdata_vis.bin", visData);
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataHexDisassemblyBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        Assert.NotEmpty(result.RecordSamples8Byte);
        Assert.NotEmpty(result.RecordSamples16Byte);
        Assert.All(result.RecordSamples8Byte,  s => Assert.Equal(8,  s.RecordSize));
        Assert.All(result.RecordSamples16Byte, s => Assert.Equal(16, s.RecordSize));
    }

    [Fact]
    public void ValidBuild_ClaimBoundary_AllFalse()
    {
        var dir = MakeTempDir();
        byte[] minData = MakeMinimal();
        byte[] visData = MakeVisible();
        string minPath = WriteFixtureFile(dir, "chunkdata_min.bin", minData);
        string visPath = WriteFixtureFile(dir, "chunkdata_vis.bin", visData);
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataHexDisassemblyBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        Assert.False(result.RuntimeBinaryWritten);
        Assert.False(result.GeometryInjected);
        Assert.False(result.PlayableExportClaimed);
        Assert.False(result.WorkshopUploadPerformed);
        Assert.False(result.SteamInstallWrite);
    }

    [Fact]
    public void Build_OutputRootMissingLocal_CheckFails()
    {
        // Use a path that contains no ".local" at any segment.
        var dir = Path.Combine(Path.GetTempPath(), "pzmapforge-map36d-nosandbox-" + Path.GetRandomFileName());
        byte[] minData = MakeMinimal();
        byte[] visData = MakeVisible();
        string minPath = WriteFixtureFile(dir, "chunkdata_min.bin", minData);
        string visPath = WriteFixtureFile(dir, "chunkdata_vis.bin", visData);
        string outRoot = Path.Combine(dir, "out-no-sandbox");

        var builder = new DeadMtlWorldBuilderChunkdataHexDisassemblyBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        var localCheck = result.Checks.Single(c => c.CheckId == "MAP36D_OUTPUT_ROOT_CONTAINS_LOCAL");
        Assert.Equal("FAIL", localCheck.CheckStatus);
    }
}
