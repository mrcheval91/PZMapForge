using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36a-core-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string SeedDir     => Path.Combine(_tempDir, "seed.local");
    private string SourceDir   => Path.Combine(_tempDir, "source.local");
    private string InstalledDir => Path.Combine(_tempDir, "installed.local");

    private static void WriteFile(string dir, string name, byte[] data)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, name), data);
    }

    private void WriteSeedFiles(byte[] lotheader, byte[] chunkdata, byte[] lotpack)
    {
        WriteFile(SeedDir, "35_27.lotheader",    lotheader);
        WriteFile(SeedDir, "chunkdata_35_27.bin", chunkdata);
        WriteFile(SeedDir, "world_35_27.lotpack", lotpack);
    }

    private void WriteSourceFiles(byte[] lotheader, byte[] chunkdata, byte[] lotpack)
    {
        WriteFile(SourceDir, "35_27.lotheader",    lotheader);
        WriteFile(SourceDir, "chunkdata_35_27.bin", chunkdata);
        WriteFile(SourceDir, "world_35_27.lotpack", lotpack);
    }

    private void WriteDefaultFixtures()
    {
        WriteSeedFiles(new byte[100], new byte[50], new byte[200]);
        WriteSourceFiles(
            Enumerable.Repeat((byte)0xFF, 200).ToArray(),
            Enumerable.Repeat((byte)0xAB, 150).ToArray(),
            new byte[180]);
    }

    private string WriteFakeMap31bJson(int opCount = 3, int cellCount = 500,
        bool emitsBinary = false, bool runtimeConsumable = false, bool sandboxOnly = true)
    {
        string path = Path.Combine(_tempDir, "fake-map31b-ops.json");
        File.WriteAllText(path, $@"{{
  ""emitted_operation_count"": {opCount},
  ""emitted_total_planned_cell_count"": {cellCount},
  ""emitted_operation_records"": [
    {{
      ""emits_binary_file"": {emitsBinary.ToString().ToLowerInvariant()},
      ""runtime_consumable"": {runtimeConsumable.ToString().ToLowerInvariant()},
      ""sandbox_only"": {sandboxOnly.ToString().ToLowerInvariant()}
    }}
  ]
}}");
        return path;
    }

    private static DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditBuilder NewBuilder()
        => new DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditBuilder();

    // -----------------------------------------------------------------------
    // MAP36A_CORE_1: lotheader sizes correctly loaded
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core1_LoadsLotheaderSizes()
    {
        WriteSeedFiles(new byte[100], new byte[1], new byte[1]);
        WriteSourceFiles(new byte[200], new byte[1], new byte[1]);

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.Equal(100L, result.LotHeaderAnatomy.MinimalSize);
        Assert.Equal(200L, result.LotHeaderAnatomy.VisibleSize);
        Assert.Equal(100L, result.LotHeaderAnatomy.SizeDelta);
    }

    // -----------------------------------------------------------------------
    // MAP36A_CORE_2: chunkdata sizes correctly loaded
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core2_LoadsChunkdataSizes()
    {
        WriteSeedFiles(new byte[1], new byte[50], new byte[1]);
        WriteSourceFiles(new byte[1], new byte[150], new byte[1]);

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.Equal(50L,  result.ChunkdataAnatomy.MinimalSize);
        Assert.Equal(150L, result.ChunkdataAnatomy.VisibleSize);
        Assert.Equal(100L, result.ChunkdataAnatomy.SizeDelta);
    }

    // -----------------------------------------------------------------------
    // MAP36A_CORE_3: SHA256 computed and differs between minimal and visible
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core3_Sha256NotEmptyAndDifferent()
    {
        WriteSeedFiles(new byte[32], new byte[32], new byte[32]);
        WriteSourceFiles(
            Enumerable.Repeat((byte)0xFF, 32).ToArray(),
            Enumerable.Repeat((byte)0xAB, 32).ToArray(),
            Enumerable.Repeat((byte)0xCD, 32).ToArray());

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.NotEmpty(result.LotHeaderAnatomy.MinimalSha256);
        Assert.NotEmpty(result.LotHeaderAnatomy.VisibleSha256);
        Assert.NotEqual(result.LotHeaderAnatomy.MinimalSha256, result.LotHeaderAnatomy.VisibleSha256);

        Assert.NotEmpty(result.ChunkdataAnatomy.MinimalSha256);
        Assert.NotEmpty(result.ChunkdataAnatomy.VisibleSha256);
        Assert.NotEqual(result.ChunkdataAnatomy.MinimalSha256, result.ChunkdataAnatomy.VisibleSha256);

        Assert.NotEmpty(result.LotpackAnatomy.MinimalSha256);
        Assert.NotEmpty(result.LotpackAnatomy.VisibleSha256);
        Assert.NotEqual(result.LotpackAnatomy.MinimalSha256, result.LotpackAnatomy.VisibleSha256);
    }

    // -----------------------------------------------------------------------
    // MAP36A_CORE_4: common prefix length less than file size when files differ
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core4_CommonPrefixLessThanFileSizeWhenFilesDiffer()
    {
        // First byte differs → CommonPrefixLength = 0
        byte[] minChunk = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        byte[] visChunk = new byte[] { 0xFF, 0x01, 0x02, 0x03 };
        WriteSeedFiles(new byte[1], minChunk, new byte[1]);
        WriteSourceFiles(new byte[1], visChunk, new byte[1]);

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.Equal(0L, result.ChunkdataAnatomy.CommonPrefixLength);
        Assert.True(result.ChunkdataAnatomy.CommonPrefixLength < result.ChunkdataAnatomy.VisibleSize);
        Assert.Equal(0L, result.ChunkdataAnatomy.FirstDifferingByteOffset);
    }

    // -----------------------------------------------------------------------
    // MAP36A_CORE_5: entropy values in [0, 8]
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core5_EntropyValuesInRange()
    {
        // Sequential bytes → moderate entropy
        byte[] sequentialBytes = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
        WriteSeedFiles(sequentialBytes, sequentialBytes, sequentialBytes);
        WriteSourceFiles(
            Enumerable.Repeat((byte)0xAA, 64).ToArray(),
            Enumerable.Repeat((byte)0xBB, 64).ToArray(),
            Enumerable.Repeat((byte)0xCC, 64).ToArray());

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.True(result.LotHeaderAnatomy.MinimalEntropy  is >= 0 and <= 8, $"MinLotheaderEntropy={result.LotHeaderAnatomy.MinimalEntropy}");
        Assert.True(result.LotHeaderAnatomy.VisibleEntropy  is >= 0 and <= 8, $"VisLotheaderEntropy={result.LotHeaderAnatomy.VisibleEntropy}");
        Assert.True(result.ChunkdataAnatomy.MinimalEntropy  is >= 0 and <= 8, $"MinChunkdataEntropy={result.ChunkdataAnatomy.MinimalEntropy}");
        Assert.True(result.ChunkdataAnatomy.VisibleEntropy  is >= 0 and <= 8, $"VisChunkdataEntropy={result.ChunkdataAnatomy.VisibleEntropy}");
        Assert.True(result.LotpackAnatomy.MinimalEntropy    is >= 0 and <= 8, $"MinLotpackEntropy={result.LotpackAnatomy.MinimalEntropy}");
        Assert.True(result.LotpackAnatomy.VisibleEntropy    is >= 0 and <= 8, $"VisLotpackEntropy={result.LotpackAnatomy.VisibleEntropy}");
    }

    // -----------------------------------------------------------------------
    // MAP36A_CORE_6: MAP-31B cross-reference correctly captures emits_binary_file and sandbox_only
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core6_Map31bCrossRefEmitsBinaryFileFalseAndSandboxOnlyTrue()
    {
        WriteDefaultFixtures();
        string jsonPath = WriteFakeMap31bJson(opCount: 5, cellCount: 5340, emitsBinary: false, sandboxOnly: true);

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, jsonPath);

        Assert.True(result.Map31bCrossRef.EmitterJsonFound);
        Assert.Equal(5,    result.Map31bCrossRef.EmittedOperationCount);
        Assert.Equal(5340, result.Map31bCrossRef.EmittedTotalPlannedCellCount);
        Assert.False(result.Map31bCrossRef.EmitsBinaryFile);
        Assert.True(result.Map31bCrossRef.SandboxOnly);
        Assert.Contains("NOT_CONNECTED", result.Map31bCrossRef.Map31bGeometryToBinaryGap);
    }

    // -----------------------------------------------------------------------
    // MAP36A_CORE_7: chunkdata size delta = visible_size - minimal_size
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core7_ChunkdataSizeDeltaComputed()
    {
        WriteSeedFiles(new byte[1], new byte[50], new byte[1]);
        WriteSourceFiles(new byte[1], new byte[200], new byte[1]);

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.Equal(200L - 50L, result.ChunkdataAnatomy.SizeDelta);
        Assert.Equal(result.ChunkdataAnatomy.VisibleSize - result.ChunkdataAnatomy.MinimalSize,
            result.ChunkdataAnatomy.SizeDelta);
    }

    // -----------------------------------------------------------------------
    // MAP36A_CORE_8: lotpack visible smaller than minimal when that is the case
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core8_LotpackVisibleSmallerThanMinimal()
    {
        WriteSeedFiles(new byte[1], new byte[1], new byte[200]);
        WriteSourceFiles(new byte[1], new byte[1], new byte[150]);

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.Equal(200L, result.LotpackAnatomy.MinimalSize);
        Assert.Equal(150L, result.LotpackAnatomy.VisibleSize);
        Assert.True(result.LotpackAnatomy.VisibleSize < result.LotpackAnatomy.MinimalSize);
        Assert.Equal(-50L, result.LotpackAnatomy.SizeDelta);
        Assert.Contains("SMALLER", result.LotpackAnatomy.SizeDeltaObservation);
    }

    // -----------------------------------------------------------------------
    // MAP36A_CORE_9: claim boundary fields all false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Core9_ClaimBoundaryAllFalse()
    {
        WriteDefaultFixtures();

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.False(result.RuntimeBinaryWritten);
        Assert.False(result.GeometryInjected);
        Assert.False(result.PlayableExportClaimed);
        Assert.False(result.WorkshopUploadPerformed);
        Assert.False(result.SteamInstallWrite);
        var claimCheck = result.Checks.Single(c => c.CheckId == "MAP36A_CLAIM_BOUNDARY_CLEAN");
        Assert.Equal("PASS", claimCheck.CheckStatus);
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CORE_1: missing MAP-35A visible source dir results in invalid
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Core1_MissingMap35aSourceDirResultsInInvalid()
    {
        WriteSeedFiles(new byte[10], new byte[10], new byte[10]);

        var result = NewBuilder().Build(SeedDir, Path.Combine(_tempDir, "no-such-source.local"), string.Empty, string.Empty);

        Assert.False(result.Map35aSourceDirFound);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CORE_2: source path containing "Dru" is rejected
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Core2_SourcePathWithDruIsRejected()
    {
        WriteSeedFiles(new byte[10], new byte[10], new byte[10]);
        string druPath = Path.Combine(_tempDir, "Dru_source.local");
        Directory.CreateDirectory(druPath);

        var result = NewBuilder().Build(SeedDir, druPath, string.Empty, string.Empty);

        Assert.True(result.Map35aSourceRejected);
        Assert.NotEmpty(result.Map35aSourceRejectionReason);
        Assert.False(result.IsValid);
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CORE_3: file inventory records files common to both seed and source
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Core3_FileInventoryRecordsCommonFiles()
    {
        WriteSeedFiles(new byte[1], new byte[1], new byte[1]);
        WriteSourceFiles(new byte[1], new byte[1], new byte[1]);
        File.WriteAllText(Path.Combine(SeedDir,   "map.info"), "s");
        File.WriteAllText(Path.Combine(SourceDir, "map.info"), "v");

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.Contains("map.info", result.CommonFiles);
        Assert.Contains(result.FileInventory, r => r.FileName == "map.info" && r.Presence == "common");
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CORE_4: file inventory records files only in minimal seed
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Core4_FileInventoryRecordsOnlyMinimalFiles()
    {
        WriteSeedFiles(new byte[1], new byte[1], new byte[1]);
        WriteSourceFiles(new byte[1], new byte[1], new byte[1]);
        File.WriteAllText(Path.Combine(SeedDir, "worldmap.png"), "seed-only");

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.Contains("worldmap.png", result.Map33aOnlyFiles);
        Assert.Contains(result.FileInventory, r => r.FileName == "worldmap.png" && r.Presence == "map33a_only");
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CORE_5: file inventory records files only in visible source
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Core5_FileInventoryRecordsOnlyVisibleFiles()
    {
        WriteSeedFiles(new byte[1], new byte[1], new byte[1]);
        WriteSourceFiles(new byte[1], new byte[1], new byte[1]);
        File.WriteAllText(Path.Combine(SourceDir, "spawnregions.lua"), "visible-only");

        var result = NewBuilder().Build(SeedDir, SourceDir, string.Empty, string.Empty);

        Assert.Contains("spawnregions.lua", result.Map35aOnlyFiles);
        Assert.Contains(result.FileInventory, r => r.FileName == "spawnregions.lua" && r.Presence == "map35a_only");
    }
}
