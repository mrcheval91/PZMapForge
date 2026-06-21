using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderVisibleCellRuntimeCandidateBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map35a-core-test.local", Guid.NewGuid().ToString());

    public DeadMtlWorldBuilderVisibleCellRuntimeCandidateBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string PrimarySourceRoot  => Path.Combine(_tempDir, "source-primary.local");
    private string FallbackSourceRoot => Path.Combine(_tempDir, "source-fallback.local");
    private string LocalModsRoot      => Path.Combine(_tempDir, "local-mods.local");
    private string OutputRoot         => Path.Combine(_tempDir, "output.local");
    private string FakeZomboidRoot    => Path.Combine(_tempDir, "zomboid-user.local");

    private void WriteSourceFiles(string root, int chunkdataSize = 18178, int lotheaderSize = 63209)
    {
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root, "35_27.lotheader"),    new byte[lotheaderSize]);
        File.WriteAllBytes(Path.Combine(root, "world_35_27.lotpack"), new byte[1056152]);
        File.WriteAllBytes(Path.Combine(root, "chunkdata_35_27.bin"), new byte[chunkdataSize]);
    }

    private void WriteFixtures(bool writePrimary = true, bool writeFallback = false, int chunkdataSize = 18178)
    {
        if (writePrimary)  WriteSourceFiles(PrimarySourceRoot, chunkdataSize: chunkdataSize);
        if (writeFallback) WriteSourceFiles(FallbackSourceRoot, chunkdataSize: 1026);
        Directory.CreateDirectory(LocalModsRoot);
        Directory.CreateDirectory(OutputRoot);
    }

    private DeadMtlWorldBuilderVisibleCellRuntimeCandidateBuilder NewBuilder() => new();

    private DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult RunStageOnly() =>
        NewBuilder().Build(PrimarySourceRoot, FallbackSourceRoot, LocalModsRoot, OutputRoot);

    private DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult RunInstallOnly() =>
        NewBuilder().Build(PrimarySourceRoot, FallbackSourceRoot, LocalModsRoot, OutputRoot, performInstall: true);

    private DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult RunCollectLogs(
        string? zomboidRoot = null, string? observation = null) =>
        NewBuilder().Build(PrimarySourceRoot, FallbackSourceRoot, LocalModsRoot, OutputRoot,
            performInstall: false, collectLogs: true,
            zomboidUserRoot: zomboidRoot, operatorObservation: observation);

    private string WriteFakeZomboidLog(string logContent)
    {
        Directory.CreateDirectory(FakeZomboidRoot);
        File.WriteAllText(Path.Combine(FakeZomboidRoot, "console.txt"), logContent);
        return FakeZomboidRoot;
    }

    private const string VisibleTerrainLog =
        "loading DeadMTL_MAP35A\n" +
        "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/35_27.lotheader\n" +
        "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/chunkdata_35_27.bin\n" +
        "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/world_35_27.lotpack\n" +
        "MapGroup something DeadMTL_MAP35A registered\n" +
        "CellLoader.LoadCellBinaryChunk start\n";

    private const string FallbackTerrainLog =
        "loading DeadMTL_MAP35A\n" +
        "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/35_27.lotheader\n" +
        "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/chunkdata_35_27.bin\n" +
        "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/world_35_27.lotpack\n" +
        "MapGroup something DeadMTL_MAP35A registered\n" +
        "CellLoader.LoadCellBinaryChunk start\n" +
        "Looking in these map folders:\n" +
        "<End of map-folders list>\n" +
        "initSpawnBuildings: no room or building at 10746,8288,0\n" +
        "FluidContainerScript.load Sanitizing container name ERROR\n";

    // -----------------------------------------------------------------------
    // MAP35A_CORE_1: Rejects Steam install path
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core1_RejectsSteamInstallPath()
    {
        WriteFixtures();
        string badMods = Path.Combine(_tempDir, "steamapps", "local-mods.local");
        Directory.CreateDirectory(badMods);
        var r = NewBuilder().Build(PrimarySourceRoot, FallbackSourceRoot, badMods, OutputRoot);
        Assert.False(r.IsValid);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP35A_LOCAL_INSTALL_PATH_SAFE" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_2: Rejects Workshop path
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core2_RejectsWorkshopPath()
    {
        WriteFixtures();
        string badMods = Path.Combine(_tempDir, "Workshop", "local-mods.local");
        Directory.CreateDirectory(badMods);
        var r = NewBuilder().Build(PrimarySourceRoot, FallbackSourceRoot, badMods, OutputRoot);
        Assert.False(r.IsValid);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP35A_LOCAL_INSTALL_PATH_SAFE" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_3: Rejects source paths containing "Dru"
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core3_RejectsDruSourcePath()
    {
        Directory.CreateDirectory(LocalModsRoot);
        Directory.CreateDirectory(OutputRoot);
        string druRoot = Path.Combine(_tempDir, "Dru_map_source.local");
        WriteSourceFiles(druRoot);
        var r = NewBuilder().Build(druRoot, string.Empty, LocalModsRoot, OutputRoot);
        Assert.False(r.IsValid);
        Assert.Contains(r.RejectedSources, s => s.Reason.Contains("Dru") || s.Reason.Contains("rejected_marker"));
        Assert.Contains(r.Checks, c => c.CheckId == "MAP35A_SOURCE_REJECTS_THIRD_PARTY_DONORS" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_4: Selects primary source when present and valid
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core4_SelectsPrimarySourceRoot()
    {
        WriteFixtures(writePrimary: true, writeFallback: true);
        var r = RunStageOnly();
        Assert.Equal(PrimarySourceRoot, r.SelectedSourceRoot);
        Assert.Equal("REPO_OWNED_LOCAL_GENERATED_PZMAPFORGE_BUILD42_CANDIDATE", r.SelectedSourceClassification);
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_5: Falls back to fallback source when primary missing
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core5_FallsBackToFallbackSourceWhenPrimaryMissing()
    {
        WriteFixtures(writePrimary: false, writeFallback: true);
        var r = RunStageOnly();
        Assert.Equal(FallbackSourceRoot, r.SelectedSourceRoot);
        Assert.Contains(r.RejectedSources, s => s.SourceRoot == PrimarySourceRoot);
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_6: Emits source_size_advantage_over_map33a=true for larger files
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core6_SourceSizeAdvantageTrue_WhenFilesLarger()
    {
        WriteFixtures(chunkdataSize: 18178);
        var r = RunStageOnly();
        Assert.True(r.SourceSizeAdvantageOverMap33a);
        Assert.Contains(r.Checks,
            c => c.CheckId == "MAP35A_REQUIRED_SOURCE_CELL_FILES_LARGER_THAN_MAP33A_MINIMAL_SEED" && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_7: Non-blocking when source files not larger than MAP33A
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core7_SizeAdvantage_NonBlockingWhenSmaller()
    {
        WriteFixtures(chunkdataSize: 500);
        var r = RunStageOnly();
        Assert.False(r.SourceSizeAdvantageOverMap33a);
        Assert.Contains(r.Checks,
            c => c.CheckId == "MAP35A_REQUIRED_SOURCE_CELL_FILES_LARGER_THAN_MAP33A_MINIMAL_SEED" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_8: StageOnly writes B42 layout and binary files
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core8_StageOnly_WritesB42LayoutAndBinaryFiles()
    {
        WriteFixtures();
        var r = RunStageOnly();
        Assert.True(r.StagePerformed);
        Assert.False(r.InstallPerformed);
        Assert.True(r.B42LayoutWritten);
        Assert.True(r.BinaryCellMaterialized);
        Assert.Equal(3, r.RequiredBinaryFilesWritten.Count);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP35A_B42_LAYOUT_WRITTEN" && c.CheckStatus == "PASS");
        Assert.Contains(r.Checks, c => c.CheckId == "MAP35A_REQUIRED_BINARY_FILES_WRITTEN" && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_9: InstallOnly writes install marker and sets install_performed=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core9_InstallOnly_WritesInstallMarker()
    {
        WriteFixtures();
        var r = RunInstallOnly();
        Assert.True(r.InstallPerformed);
        Assert.True(r.InstallMarkerWritten);
        Assert.Equal("MAP35A_INSTALL_ONLY_READY", r.RuntimeClassification);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP35A_INSTALL_MARKER_WRITTEN" && c.CheckStatus == "PASS");
        string markerPath = Path.Combine(LocalModsRoot,
            DeadMtlWorldBuilderVisibleCellRuntimeCandidateBuilder.InstalledFolderName,
            DeadMtlWorldBuilderVisibleCellRuntimeCandidateBuilder.InstallMarkerFileName);
        Assert.True(File.Exists(markerPath));
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_10: CollectLogs rejects if not installed
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core10_CollectLogs_RejectsIfNotInstalled()
    {
        WriteFixtures();
        var zomboidRoot = WriteFakeZomboidLog("loading DeadMTL_MAP35A\n");
        var r = RunCollectLogs(zomboidRoot);
        Assert.False(r.IsValid);
        Assert.Equal("MAP35A_COLLECT_REJECTED_NOT_INSTALLED", r.Verdict);
        Assert.False(r.InstallPerformed);
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_11: CollectLogs does NOT reinstall after prior install
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core11_CollectLogs_DoesNotReinstall()
    {
        WriteFixtures();
        RunInstallOnly();

        string installedRoot = Path.Combine(LocalModsRoot,
            DeadMtlWorldBuilderVisibleCellRuntimeCandidateBuilder.InstalledFolderName);
        string markerPath = Path.Combine(installedRoot,
            DeadMtlWorldBuilderVisibleCellRuntimeCandidateBuilder.InstallMarkerFileName);
        string originalContent = File.ReadAllText(markerPath);

        var zomboidRoot = WriteFakeZomboidLog(FallbackTerrainLog);
        var r = RunCollectLogs(zomboidRoot, "empty field terrain");

        Assert.False(r.InstallPerformed);
        Assert.Equal(originalContent, File.ReadAllText(markerPath));
        Assert.Contains(r.Checks, c => c.CheckId == "MAP35A_COLLECT_LOGS_DOES_NOT_REINSTALL" && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_12: Classifies visible terrain pass when observation confirms
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core12_ClassifiesVisibleTerrainPass()
    {
        WriteFixtures();
        RunInstallOnly();
        var zomboidRoot = WriteFakeZomboidLog(VisibleTerrainLog);
        var r = RunCollectLogs(zomboidRoot, "visible terrain with roads and trees");
        Assert.Equal("MAP35A_RUNTIME_VISIBLE_CELL_PASS", r.RuntimeClassification);
        Assert.True(r.VisibleTerrainDetected);
        Assert.False(r.FallbackEmptyTerrainDetected);
    }

    // -----------------------------------------------------------------------
    // MAP35A_CORE_13: Classifies fallback terrain partial pass
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Core13_ClassifiesFallbackTerrainPartialPass()
    {
        WriteFixtures();
        RunInstallOnly();
        var zomboidRoot = WriteFakeZomboidLog(FallbackTerrainLog);
        var r = RunCollectLogs(zomboidRoot, "loaded in empty field fallback terrain");
        Assert.Equal("MAP35A_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN", r.RuntimeClassification);
        Assert.True(r.FallbackEmptyTerrainDetected);
        Assert.False(r.VisibleTerrainDetected);
    }
}
