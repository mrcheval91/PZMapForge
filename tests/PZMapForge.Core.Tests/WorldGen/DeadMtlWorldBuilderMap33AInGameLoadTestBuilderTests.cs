using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMap33AInGameLoadTestBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map34a-core-test.local", Guid.NewGuid().ToString());

    public DeadMtlWorldBuilderMap33AInGameLoadTestBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string Map33AManifest     => Path.Combine(_tempDir, "map33a-manifest.local.json");
    private string SourceCandidateRoot => Path.Combine(_tempDir, "map33a-candidate.local");
    private string LocalModsRoot      => Path.Combine(_tempDir, "local-mods.local");
    private string OutputRoot         => Path.Combine(_tempDir, "output.local");
    private string FakeZomboidRoot    => Path.Combine(_tempDir, "zomboid-user.local");

    private static string MakeMap33AManifest(bool binaryCellMaterialized = true) => $@"{{
  ""format"": ""MAP33A_TEST"",
  ""binary_cell_materialized"": {(binaryCellMaterialized ? "true" : "false")},
  ""geometry_from_map31b_materialized"": false,
  ""verdict"": ""MAP33A_BINARY_SEEDED_RUNTIME_CANDIDATE_STAGED""
}}";

    private void WriteFixtures(bool binaryCellMaterialized = true)
    {
        File.WriteAllText(Map33AManifest, MakeMap33AManifest(binaryCellMaterialized));

        string mapDir = Path.Combine(SourceCandidateRoot, "media", "maps", "DeadMTL_MAP33A");
        Directory.CreateDirectory(mapDir);
        File.WriteAllText(Path.Combine(SourceCandidateRoot, "mod.info"), "id=DeadMTL_MAP33A\n");
        File.WriteAllBytes(Path.Combine(mapDir, "35_27.lotheader"),    new byte[] { 0x4C, 0x4F, 0x54, 0x48 });
        File.WriteAllBytes(Path.Combine(mapDir, "world_35_27.lotpack"), new byte[] { 0x4C, 0x4F, 0x54, 0x50, 0x01 });
        File.WriteAllBytes(Path.Combine(mapDir, "chunkdata_35_27.bin"), new byte[] { 0x01, 0x02, 0x03 });
        File.WriteAllText(Path.Combine(mapDir, "map.info"),       "title=DeadMTL_MAP33A\n");
        File.WriteAllText(Path.Combine(mapDir, "spawnpoints.lua"), "function SpawnPoints() end\n");
        File.WriteAllText(Path.Combine(mapDir, "objects.lua"),    "-- placeholder\n");

        Directory.CreateDirectory(LocalModsRoot);
        Directory.CreateDirectory(OutputRoot);
    }

    private string WriteFakeZomboidLog(string logContent)
    {
        Directory.CreateDirectory(FakeZomboidRoot);
        string consoleTxt = Path.Combine(FakeZomboidRoot, "console.txt");
        File.WriteAllText(consoleTxt, logContent);
        return FakeZomboidRoot;
    }

    private DeadMtlWorldBuilderMap33AInGameLoadTestBuilder NewBuilder() =>
        new DeadMtlWorldBuilderMap33AInGameLoadTestBuilder();

    private DeadMtlWorldBuilderMap33AInGameLoadTestResult RunInstallOnly() =>
        NewBuilder().Build(Map33AManifest, SourceCandidateRoot, LocalModsRoot, OutputRoot);

    private DeadMtlWorldBuilderMap33AInGameLoadTestResult RunCollectLogs(
        string zomboidRoot, string? observation = null) =>
        NewBuilder().Build(Map33AManifest, SourceCandidateRoot, LocalModsRoot, OutputRoot,
            collectLogs: true, zomboidUserRoot: zomboidRoot, operatorObservation: observation);

    private const string PartialPassLog = """
        loading DeadMTL_MAP33A
        mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/35_27.lotheader
        mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/chunkdata_35_27.bin
        mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/world_35_27.lotpack
        mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/spawnpoints.lua
        mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/map.info
        MapGroup something DeadMTL_MAP33A registered
        CellLoader.LoadCellBinaryChunk start
        Looking in these map folders:
        <End of map-folders list>
        initSpawnBuildings: no room or building at 10746,8288,0
        FluidContainerScript.load Sanitizing container name ERROR
        CraftRecipeComponentScript: Recipe Piano missing UiConfigScript
        """;

    // -----------------------------------------------------------------------
    // MAP-34A 1. Rejects Steam install path
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_RejectsSteamInstallPath()
    {
        WriteFixtures();
        string badMods = Path.Combine(_tempDir, "steamapps", "local-mods");
        Directory.CreateDirectory(badMods);
        var r = NewBuilder().Build(Map33AManifest, SourceCandidateRoot, badMods, OutputRoot);
        Assert.False(r.IsValid);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34A_LOCAL_USER_MODS_PATH_RESOLVED" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP-34A 2. Rejects Workshop upload path
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_RejectsWorkshopUploadPath()
    {
        WriteFixtures();
        string badMods = Path.Combine(_tempDir, "Workshop", "local-mods");
        Directory.CreateDirectory(badMods);
        var r = NewBuilder().Build(Map33AManifest, SourceCandidateRoot, badMods, OutputRoot);
        Assert.False(r.IsValid);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34A_LOCAL_USER_MODS_PATH_RESOLVED" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP-34A 3. Validates MAP-33A manifest with binary_cell_materialized=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_ValidatesMap33AManifest_BinaryCellMaterializedTrue()
    {
        WriteFixtures(binaryCellMaterialized: true);
        var r = RunInstallOnly();
        Assert.True(r.BinaryCellMaterialized);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34A_MAP33A_BINARY_CELL_MATERIALIZED_TRUE" && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP-34A 4. Rejects MAP-33A manifest with binary_cell_materialized=false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_RejectsMap33AManifest_BinaryCellMaterializedFalse()
    {
        WriteFixtures(binaryCellMaterialized: false);
        var r = RunInstallOnly();
        Assert.False(r.IsValid);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34A_MAP33A_BINARY_CELL_MATERIALIZED_TRUE" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP-34A 5. Copies candidate into local user mods folder
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_CopiesCandidateToLocalMods()
    {
        WriteFixtures();
        var r = RunInstallOnly();
        Assert.True(r.InstallPerformed);
        string installedRoot = Path.Combine(LocalModsRoot, DeadMtlWorldBuilderMap33AInGameLoadTestBuilder.InstalledFolderName);
        Assert.True(Directory.Exists(installedRoot), "Installed candidate folder must exist in local mods");
        Assert.True(File.Exists(Path.Combine(installedRoot, "mod.info")));
        string mapDir = Path.Combine(installedRoot, "media", "maps", "DeadMTL_MAP33A");
        Assert.True(File.Exists(Path.Combine(mapDir, "35_27.lotheader")));
        Assert.True(File.Exists(Path.Combine(mapDir, "world_35_27.lotpack")));
        Assert.True(File.Exists(Path.Combine(mapDir, "chunkdata_35_27.bin")));
    }

    // -----------------------------------------------------------------------
    // MAP-34A 6. Writes install marker
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_WritesInstallMarker()
    {
        WriteFixtures();
        var r = RunInstallOnly();
        Assert.True(r.InstallMarkerWritten);
        string markerPath = Path.Combine(LocalModsRoot,
            DeadMtlWorldBuilderMap33AInGameLoadTestBuilder.InstalledFolderName,
            DeadMtlWorldBuilderMap33AInGameLoadTestBuilder.InstallMarkerFileName);
        Assert.True(File.Exists(markerPath));
        string content = File.ReadAllText(markerPath);
        Assert.Contains("PZMAPFORGE_MAP34A_TEST_INSTALL_MARKER", content);
        Assert.Contains("binary_cell_materialized              : true", content);
        Assert.Contains("runtime_proof_claimed                 : false", content);
    }

    // -----------------------------------------------------------------------
    // MAP-34A 7. Classifies install-only as MAP34A_INSTALL_ONLY_READY
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_ClassifiesInstallOnly_Ready()
    {
        WriteFixtures();
        var r = RunInstallOnly();
        Assert.Equal("MAP34A_INSTALL_ONLY_READY", r.RuntimeClassification);
        Assert.Equal("MAP34A_INSTALL_ONLY_READY", r.Verdict);
        Assert.False(r.RuntimeLogCollectionAttempted);
    }

    // -----------------------------------------------------------------------
    // MAP-34A 8. Classifies missing logs as MAP34B_RUNTIME_EVIDENCE_INSUFFICIENT
    //            (collect-logs mode requires prior install)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_ClassifiesMissingLogs_EvidenceInsufficient()
    {
        WriteFixtures();
        // Install first so installed folder exists
        RunInstallOnly();
        // Collect-logs with empty zomboid root
        string emptyZomboid = Path.Combine(_tempDir, "empty-zomboid.local");
        Directory.CreateDirectory(emptyZomboid);
        var r = RunCollectLogs(emptyZomboid);
        Assert.True(r.RuntimeLogCollectionAttempted);
        Assert.False(r.RuntimeLogsFound);
        Assert.Equal("MAP34B_RUNTIME_EVIDENCE_INSUFFICIENT", r.RuntimeClassification);
    }

    // -----------------------------------------------------------------------
    // MAP-34A 9. geometry_from_map31b_materialized remains false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_KeepsMap31bGeometryClaim_False()
    {
        WriteFixtures();
        var r = RunInstallOnly();
        Assert.False(r.GeometryFromMap31bMaterialized);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34A_MAP31B_GEOMETRY_NOT_CLAIMED" && c.CheckStatus == "PASS");
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34A_NO_FINAL_DEADMTL_GEOMETRY_CLAIM" && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP-34A 10. Final playable/runtime claim remains false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_KeepsFinalPlayableClaim_False()
    {
        WriteFixtures();
        var r = RunInstallOnly();
        Assert.False(r.PlayableExportClaimed);
        Assert.False(r.RuntimeProofClaimed);
        Assert.False(r.PublicPackageClaimed);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34A_PLAYABLE_EXPORT_CLAIM_GATED" && c.CheckStatus == "PASS");
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34A_CLAIM_BOUNDARY_RECORDED"     && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP-34A 11. All MAP34A checks pass in install-only fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_AllChecks_Pass()
    {
        WriteFixtures();
        var r = RunInstallOnly();
        var map34aChecks = r.Checks.Where(c => c.CheckId.StartsWith("MAP34A")).ToList();
        Assert.True(map34aChecks.Count > 0, "No MAP34A checks found");
        foreach (var c in map34aChecks)
            Assert.True(c.CheckStatus == "PASS",
                $"Check {c.CheckId} FAILED: expected={c.Expected} actual={c.Actual}");
    }

    // -----------------------------------------------------------------------
    // MAP-34B 1. Classifies partial-pass fixture correctly
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_PartialPassWithOperatorObservation()
    {
        WriteFixtures();
        RunInstallOnly();
        string zomboidRoot = WriteFakeZomboidLog(PartialPassLog);
        var r = RunCollectLogs(zomboidRoot, "loaded in a field, empty, fallback type");
        Assert.Equal("MAP34B_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN", r.RuntimeClassification);
        Assert.True(r.CandidateModLoaded);
        Assert.True(r.CandidateBinaryFilesMounted);
        Assert.True(r.CandidateMapgroupRegistered);
        Assert.True(r.CandidateSpawnBlockerAbsent);
        Assert.True(r.CandidateBinaryChunkLoadAttempted);
        Assert.True(r.FallbackEmptyTerrainDetected);
        Assert.Equal("loaded in a field, empty, fallback type", r.OperatorObservation);
    }

    // -----------------------------------------------------------------------
    // MAP-34B 2. Ignores unrelated vanilla errors (FluidContainerScript, Recipe Piano)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_IgnoresUnrelatedVanillaErrors()
    {
        WriteFixtures();
        RunInstallOnly();
        string zomboidRoot = WriteFakeZomboidLog(PartialPassLog);
        var r = RunCollectLogs(zomboidRoot);
        // Unrelated errors were found but should not prevent partial pass
        Assert.True(r.UnrelatedErrorsFound, "Unrelated errors should be detected");
        Assert.False(r.CandidateSpecificErrorsFound, "Candidate-specific errors should not be found");
        Assert.Equal("MAP34B_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN", r.RuntimeClassification);
    }

    // -----------------------------------------------------------------------
    // MAP-34B 3. Classifies missing mod load as MAP34B_RUNTIME_MOD_NOT_LOADED
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_ClassifiesModNotLoaded()
    {
        WriteFixtures();
        RunInstallOnly();
        string zomboidRoot = WriteFakeZomboidLog("WARN: some vanilla warning\nLoading vanilla content only\n");
        var r = RunCollectLogs(zomboidRoot);
        Assert.False(r.CandidateModLoaded);
        Assert.Equal("MAP34B_RUNTIME_MOD_NOT_LOADED", r.RuntimeClassification);
    }

    // -----------------------------------------------------------------------
    // MAP-34B 4. Classifies spawn table error as MAP34B_RUNTIME_SPAWN_BLOCKED
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_ClassifiesSpawnBlocked_SpawnTableError()
    {
        WriteFixtures();
        RunInstallOnly();
        string log = """
            loading DeadMTL_MAP33A
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/35_27.lotheader
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/chunkdata_35_27.bin
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/world_35_27.lotpack
            MapGroup ... DeadMTL_MAP33A
            there is no spawn point table for the player's profession
            """;
        string zomboidRoot = WriteFakeZomboidLog(log);
        var r = RunCollectLogs(zomboidRoot);
        Assert.False(r.CandidateSpawnBlockerAbsent);
        Assert.Equal("MAP34B_RUNTIME_SPAWN_BLOCKED", r.RuntimeClassification);
    }

    // -----------------------------------------------------------------------
    // MAP-34B 5. Classifies -1,-1,-1 square error as MAP34B_RUNTIME_SPAWN_BLOCKED
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_ClassifiesSpawnBlocked_NegativeOneSquare()
    {
        WriteFixtures();
        RunInstallOnly();
        string log = """
            loading DeadMTL_MAP33A
            MapGroup ... DeadMTL_MAP33A
            can't create player at x,y,z=-1,-1,-1 because the square is null
            """;
        string zomboidRoot = WriteFakeZomboidLog(log);
        var r = RunCollectLogs(zomboidRoot);
        Assert.False(r.CandidateSpawnBlockerAbsent);
        Assert.Equal("MAP34B_RUNTIME_SPAWN_BLOCKED", r.RuntimeClassification);
    }

    // -----------------------------------------------------------------------
    // MAP-34B 6. Records operator observation in result
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_RecordsOperatorObservation()
    {
        WriteFixtures();
        RunInstallOnly();
        string zomboidRoot = WriteFakeZomboidLog(PartialPassLog);
        var r = RunCollectLogs(zomboidRoot, "loaded in a field, empty, fallback type");
        Assert.Equal("loaded in a field, empty, fallback type", r.OperatorObservation);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34B_OPERATOR_OBSERVATION_RECORDED" && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP-34B 7. playable_export_claimed remains false in collect-logs mode
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_KeepsPlayableClaimFalse()
    {
        WriteFixtures();
        RunInstallOnly();
        string zomboidRoot = WriteFakeZomboidLog(PartialPassLog);
        var r = RunCollectLogs(zomboidRoot, "loaded in a field, empty, fallback type");
        Assert.False(r.PlayableExportClaimed);
        Assert.False(r.RuntimeProofClaimed);
        Assert.False(r.PublicPackageClaimed);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34B_NO_PLAYABLE_EXPORT_CLAIM" && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP-34B 8. geometry_from_map31b_materialized remains false in collect-logs mode
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_KeepsGeometryClaimFalse()
    {
        WriteFixtures();
        RunInstallOnly();
        string zomboidRoot = WriteFakeZomboidLog(PartialPassLog);
        var r = RunCollectLogs(zomboidRoot);
        Assert.False(r.GeometryFromMap31bMaterialized);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP34B_NO_FINAL_GEOMETRY_CLAIM" && c.CheckStatus == "PASS");
    }

    // -----------------------------------------------------------------------
    // MAP-34B 9. All MAP34B checks pass for partial-pass fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_AllMAP34BChecksPass_PartialPassFixture()
    {
        WriteFixtures();
        RunInstallOnly();
        string zomboidRoot = WriteFakeZomboidLog(PartialPassLog);
        var r = RunCollectLogs(zomboidRoot, "loaded in a field, empty, fallback type");
        var map34bChecks = r.Checks.Where(c => c.CheckId.StartsWith("MAP34B")).ToList();
        Assert.True(map34bChecks.Count > 0, "No MAP34B checks found");
        foreach (var c in map34bChecks)
            Assert.True(c.CheckStatus == "PASS",
                $"Check {c.CheckId} FAILED: expected={c.Expected} actual={c.Actual}");
    }
}
