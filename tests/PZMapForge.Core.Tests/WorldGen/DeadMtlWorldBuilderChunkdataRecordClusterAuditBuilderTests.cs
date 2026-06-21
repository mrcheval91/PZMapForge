using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderChunkdataRecordClusterAuditBuilderTests
{
    private static string MakeTempDir() =>
        Path.Combine(Path.GetTempPath(), "pzmapforge-map37a-core-test.local", Path.GetRandomFileName());

    private static string WriteFixtureFile(string dir, string name, byte[] data)
    {
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        File.WriteAllBytes(path, data);
        return path;
    }

    // Fixture mirrors real chunkdata: 2-byte header + 8-byte records
    // minimal: 2 + 128 * 8 = 1026 bytes
    // visible: 2 + 2272 * 8 = 18178 bytes
    // First 128 records identical in both; visible has 2144 extra records
    private static byte[] MakeMinimal()
    {
        var b = new byte[2 + 128 * 8];
        b[0] = 0xAB; b[1] = 0xCD;
        for (int i = 0; i < 128; i++)
            for (int j = 0; j < 8; j++)
                b[2 + i * 8 + j] = (byte)((i % 200) + 1);
        return b;
    }

    private static byte[] MakeVisible()
    {
        var b = new byte[2 + 2272 * 8];
        b[0] = 0xAB; b[1] = 0xCD;
        // First 128 same as minimal
        for (int i = 0; i < 128; i++)
            for (int j = 0; j < 8; j++)
                b[2 + i * 8 + j] = (byte)((i % 200) + 1);
        // 2144 extra records
        for (int i = 128; i < 2272; i++)
            for (int j = 0; j < 8; j++)
                b[2 + i * 8 + j] = (byte)(0x40 + (i % 60));
        return b;
    }

    // MAP37A_CORE_1: Valid fixture with 2-byte header + 8-byte records is valid
    [Fact]
    public void ValidBuild_BothFilesPresent_IsValid_AllChecksPass()
    {
        var dir = MakeTempDir();
        string minPath = WriteFixtureFile(dir, "chunk_min.bin", MakeMinimal());
        string visPath = WriteFixtureFile(dir, "chunk_vis.bin", MakeVisible());
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataRecordClusterAuditBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        Assert.True(result.IsValid, $"IsValid must be true. Errors: {string.Join("; ", result.Errors)}");
        Assert.All(result.Checks, c => Assert.Equal("PASS", c.CheckStatus));
        Assert.Equal("MAP37A_CHUNKDATA_RECORD_CLUSTER_AUDIT_V1", result.Format);
        Assert.Equal("MAP37A_CHUNKDATA_RECORD_CLUSTER_AUDIT_COMPLETE", result.Verdict);
    }

    // MAP37A_CORE_2: header=2 width=8 exact-fit hypothesis has ExactFitScore=3
    [Fact]
    public void ValidBuild_Header2Width8ExactFit_ScoreIsThree()
    {
        var dir = MakeTempDir();
        string minPath = WriteFixtureFile(dir, "chunk_min.bin", MakeMinimal());
        string visPath = WriteFixtureFile(dir, "chunk_vis.bin", MakeVisible());
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataRecordClusterAuditBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        var h2w8 = result.CandidateHeaderSizes.FirstOrDefault(r => r.HeaderSize == 2 && r.RecordWidth == 8);
        Assert.NotNull(h2w8);
        Assert.Equal(0, h2w8.MinimalRemainder);
        Assert.Equal(0, h2w8.VisibleRemainder);
        Assert.Equal(3, h2w8.ExactFitScore);
        Assert.Equal(128,  h2w8.MinimalFullRecordCount);
        Assert.Equal(2272, h2w8.VisibleFullRecordCount);
    }

    // MAP37A_CORE_3: visible extra records = 2144
    [Fact]
    public void ValidBuild_VisibleExtraRecords_AreCorrect()
    {
        var dir = MakeTempDir();
        string minPath = WriteFixtureFile(dir, "chunk_min.bin", MakeMinimal());
        string visPath = WriteFixtureFile(dir, "chunk_vis.bin", MakeVisible());
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataRecordClusterAuditBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        var visExtra = result.ChangedRecordWindows.FirstOrDefault(w => w.WindowType == "VISIBLE_EXTRA");
        Assert.NotNull(visExtra);
        Assert.Equal(128,  visExtra.StartRecordIndex);
        Assert.Equal(2271, visExtra.EndRecordIndex);
        Assert.Equal(2144, visExtra.LengthRecords);
    }

    // MAP37A_CORE_4: column statistics emit exactly 8 columns for width=8
    [Fact]
    public void ValidBuild_ColumnStatistics_EightColumnsForWidth8()
    {
        var dir = MakeTempDir();
        string minPath = WriteFixtureFile(dir, "chunk_min.bin", MakeMinimal());
        string visPath = WriteFixtureFile(dir, "chunk_vis.bin", MakeVisible());
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataRecordClusterAuditBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        var visCols = result.RecordColumnStatistics.Where(c => c.Source == "visible").ToList();
        Assert.Equal(8, visCols.Count);
        Assert.All(visCols, c => Assert.Equal(8, c.RecordWidth));
        for (int i = 0; i < 8; i++)
            Assert.Equal(i, visCols[i].ColumnIndex);
    }

    // MAP37A_CORE_5: changed record windows include both DIFF (or SAME) and VISIBLE_EXTRA
    [Fact]
    public void ValidBuild_ChangedRecordWindows_IncludesSameAndVisibleExtra()
    {
        var dir = MakeTempDir();
        string minPath = WriteFixtureFile(dir, "chunk_min.bin", MakeMinimal());
        string visPath = WriteFixtureFile(dir, "chunk_vis.bin", MakeVisible());
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderChunkdataRecordClusterAuditBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        Assert.True(result.ChangedRecordWindows.Count >= 2, "Must have at least 2 windows (overlap + VISIBLE_EXTRA)");
        Assert.Contains(result.ChangedRecordWindows, w => w.WindowType == "VISIBLE_EXTRA");
        Assert.Contains(result.ChangedRecordWindows, w => w.WindowType == "SAME" || w.WindowType == "DIFF");
        Assert.All(result.ChangedRecordWindows, w => Assert.Equal("CANDIDATE_RECORD_WINDOW_UNVERIFIED", w.Label));
    }

    // MAP37A_CORE_6: claim boundary stays false / read_only_probe true
    [Fact]
    public void Build_OutputRootMissingLocal_CheckFails()
    {
        // Use a path that contains no ".local" at any segment.
        var dir = Path.Combine(Path.GetTempPath(), "pzmapforge-map37a-nosandbox-" + Path.GetRandomFileName());
        string minPath = WriteFixtureFile(dir, "chunk_min.bin", MakeMinimal());
        string visPath = WriteFixtureFile(dir, "chunk_vis.bin", MakeVisible());
        string outRoot = Path.Combine(dir, "out-no-sandbox");

        var builder = new DeadMtlWorldBuilderChunkdataRecordClusterAuditBuilder();
        var result  = builder.Build(minPath, visPath, outRoot);

        Assert.False(result.RuntimeBinaryWritten);
        Assert.False(result.GeometryInjected);
        Assert.False(result.PlayableExportClaimed);
        Assert.False(result.VerifiedChunkdataFormat);
        Assert.True(result.ReadOnlyProbe);

        var localCheck = result.Checks.Single(c => c.CheckId == "MAP37A_OUTPUT_ROOT_CONTAINS_LOCAL");
        Assert.Equal("FAIL", localCheck.CheckStatus);
    }
}
