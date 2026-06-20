using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map33a-core-test.local", Guid.NewGuid().ToString());

    public DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string Map32AManifest => Path.Combine(_tempDir, "map32a-manifest.local.json");
    private string BinarySeedRoot => Path.Combine(_tempDir, "seed.local");
    private string OutputRoot     => Path.Combine(_tempDir, "candidate.local");

    private static string MakeMap32AManifest() => """
        {
          "format": "MAP32A_TEST",
          "lot_count": 98,
          "footprint_count": 96,
          "skipped_lot_count": 2,
          "sector_counts": [
            { "sector_id": "DEFAULT",          "lot_count": 47 },
            { "sector_id": "DOWNTOWN_CORE",    "lot_count": 26 },
            { "sector_id": "OPEN_RESIDENTIAL", "lot_count": 25 }
          ],
          "verdict": "MAP32A_MATERIALIZED_RUNTIME_CANDIDATE_STAGED"
        }
        """;

    private void WriteFixtures()
    {
        File.WriteAllText(Map32AManifest, MakeMap32AManifest());

        Directory.CreateDirectory(BinarySeedRoot);
        // Required binary seed stubs — non-empty bytes
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "35_27.lotheader"),    new byte[] { 0x4C, 0x4F, 0x54, 0x48, 0x01, 0x00 });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "world_35_27.lotpack"), new byte[] { 0x4C, 0x4F, 0x54, 0x50, 0x01, 0x00, 0x00, 0x00 });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "chunkdata_35_27.bin"), new byte[] { 0x01, 0x02, 0x03, 0x04 });
        // Optional sidecars
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "streets.xml.bin"),         new byte[] { 0xAA, 0xBB });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "worldmap.xml.bin"),         new byte[] { 0xCC, 0xDD });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "worldmap-forest.xml.bin"),  new byte[] { 0xEE, 0xFF });

        Directory.CreateDirectory(OutputRoot);
    }

    private DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder NewBuilder() =>
        new DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder();

    // -----------------------------------------------------------------------
    // MAP-33A 1. Output root outside .local is rejected
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_RejectsOutputRoot_OutsideLocal()
    {
        WriteFixtures();
        string badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-map33a-noloc", Guid.NewGuid().ToString());
        Directory.CreateDirectory(badRoot);
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, badRoot);
        Assert.False(r.IsValid);
        Assert.True(r.Errors.Count > 0);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP33A_OUTPUT_ROOT_UNDER_LOCAL" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP-33A 2. Fails if MAP-32A manifest missing
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_FailsIfMap32AManifestMissing()
    {
        WriteFixtures();
        var r = NewBuilder().Build(
            Path.Combine(_tempDir, "no_such_manifest.json"),
            BinarySeedRoot,
            OutputRoot);
        Assert.False(r.IsValid);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP33A_INPUT_MAP32A_MANIFEST_EXISTS" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP-33A 3. Fails if binary seed root missing
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_FailsIfBinarySeedRootMissing()
    {
        WriteFixtures();
        var r = NewBuilder().Build(
            Map32AManifest,
            Path.Combine(_tempDir, "no_such_seed_dir"),
            OutputRoot);
        Assert.False(r.IsValid);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP33A_BINARY_SEED_ROOT_EXISTS" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP-33A 4. Discovers required binary seed files
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_DiscoversBinarySeedFiles()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        Assert.Equal(3, r.BinarySeedFilesDiscovered.Count);
        Assert.Contains("35_27.lotheader",    r.BinarySeedFilesDiscovered);
        Assert.Contains("world_35_27.lotpack", r.BinarySeedFilesDiscovered);
        Assert.Contains("chunkdata_35_27.bin", r.BinarySeedFilesDiscovered);
    }

    // -----------------------------------------------------------------------
    // MAP-33A 5. Writes staged mod.info
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_Writes_StagedModInfo()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        Assert.Empty(r.Errors);
        string modInfoPath = Path.Combine(OutputRoot, "mod.info");
        Assert.True(File.Exists(modInfoPath));
        string content = File.ReadAllText(modInfoPath);
        Assert.Contains("id=", content);
        Assert.Contains(DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder.MapId, content);
    }

    // -----------------------------------------------------------------------
    // MAP-33A 6. Writes staged map.info
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_Writes_StagedMapInfo()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        string mapInfoPath = Path.Combine(OutputRoot, "media", "maps",
            DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder.MapName, "map.info");
        Assert.True(File.Exists(mapInfoPath));
        string content = File.ReadAllText(mapInfoPath);
        Assert.Contains("title=", content);
        Assert.Contains(DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder.MapName, content);
    }

    // -----------------------------------------------------------------------
    // MAP-33A 7. Writes spawnpoints.lua
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_Writes_SpawnpointsLua()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        string spawnPath = Path.Combine(OutputRoot, "media", "maps",
            DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder.MapName, "spawnpoints.lua");
        Assert.True(File.Exists(spawnPath));
        string content = File.ReadAllText(spawnPath);
        Assert.Contains("SpawnPoints", content);
        Assert.Contains("worldX", content);
    }

    // -----------------------------------------------------------------------
    // MAP-33A 8. Writes objects.lua
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_Writes_ObjectsLua()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        string objectsPath = Path.Combine(OutputRoot, "media", "maps",
            DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder.MapName, "objects.lua");
        Assert.True(File.Exists(objectsPath));
    }

    // -----------------------------------------------------------------------
    // MAP-33A 9. Required binary files are written to staged map folder
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_WritesBinaryFiles_ToStagedMapFolder()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        Assert.Equal(3, r.BinarySeedFilesWritten.Count);
        string mapDir = Path.Combine(OutputRoot, "media", "maps",
            DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder.MapName);
        Assert.True(File.Exists(Path.Combine(mapDir, "35_27.lotheader")));
        Assert.True(File.Exists(Path.Combine(mapDir, "world_35_27.lotpack")));
        Assert.True(File.Exists(Path.Combine(mapDir, "chunkdata_35_27.bin")));
    }

    // -----------------------------------------------------------------------
    // MAP-33A 10. Required binary files are non-empty
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_RequiredBinaryFiles_AreNonEmpty()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        foreach (var path in r.BinarySeedFilesWritten)
            Assert.True(new FileInfo(path).Length > 0, $"{Path.GetFileName(path)} must be non-empty");
    }

    // -----------------------------------------------------------------------
    // MAP-33A 11. binary_cell_materialized=true when required seed files copied
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_BinaryCellMaterialized_True()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        Assert.True(r.BinaryCellMaterialized, "binary_cell_materialized must be true when all required binary files are copied and non-empty");
        Assert.True(r.RequiredBinaryFilesPresent);
        Assert.True(r.BinarySeedFileSha256.ContainsKey("35_27.lotheader"),    "SHA256 entry for lotheader");
        Assert.True(r.BinarySeedFileSha256.ContainsKey("world_35_27.lotpack"), "SHA256 entry for lotpack");
        Assert.True(r.BinarySeedFileSha256.ContainsKey("chunkdata_35_27.bin"), "SHA256 entry for chunkdata");
    }

    // -----------------------------------------------------------------------
    // MAP-33A 12. geometry_from_map31b_materialized=false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_GeometryFromMap31b_False()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        Assert.False(r.GeometryFromMap31bMaterialized,
            "geometry_from_map31b_materialized must be false (seed is MAP-7Y sidecar, not MAP-31B encoded geometry)");
    }

    // -----------------------------------------------------------------------
    // MAP-33A 13. Runtime/playable/public claims remain false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_ClaimFlags_AllFalse()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        Assert.False(r.RuntimeProofClaimed);
        Assert.False(r.RuntimeValid);
        Assert.False(r.PlayableExportClaimed);
        Assert.False(r.PublicPlayablePackagingClaimed);
        Assert.False(r.RawSourcePngMutated);
        Assert.False(r.LiveWorkshopWrite);
        Assert.False(r.PzInstallWrite);
        Assert.True(r.SandboxOnly);
    }

    // -----------------------------------------------------------------------
    // MAP-33A 14. Authoring counts carried forward from MAP-32A manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_AuthoringCounts_CarriedForward()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        Assert.Equal(98, r.LotCount);
        Assert.Equal(96, r.FootprintCount);
        Assert.Equal(2,  r.SkippedLotCount);
    }

    // -----------------------------------------------------------------------
    // MAP-33A 15. Sector counts carried forward
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_SectorCounts_CarriedForward()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        Assert.Equal(3, r.SectorCounts.Count);
        Assert.Contains(r.SectorCounts, sc => sc.SectorId == "DEFAULT"          && sc.LotCount == 47);
        Assert.Contains(r.SectorCounts, sc => sc.SectorId == "DOWNTOWN_CORE"    && sc.LotCount == 26);
        Assert.Contains(r.SectorCounts, sc => sc.SectorId == "OPEN_RESIDENTIAL" && sc.LotCount == 25);
    }

    // -----------------------------------------------------------------------
    // MAP-33A 16. All MAP33A checks pass
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_AllChecks_Pass()
    {
        WriteFixtures();
        var r = NewBuilder().Build(Map32AManifest, BinarySeedRoot, OutputRoot);
        var map33aChecks = r.Checks.Where(c => c.CheckId.StartsWith("MAP33A")).ToList();
        Assert.True(map33aChecks.Count > 0, "No MAP33A checks found");
        foreach (var c in map33aChecks)
            Assert.True(c.CheckStatus == "PASS",
                $"Check {c.CheckId} FAILED: expected={c.Expected} actual={c.Actual}");
    }
}
