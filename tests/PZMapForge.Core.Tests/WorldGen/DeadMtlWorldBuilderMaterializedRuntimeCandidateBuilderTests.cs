using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map32a-core-test.local", Guid.NewGuid().ToString());

    public DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string LotFillJson  => Path.Combine(_tempDir, "lot-fill.local.json");
    private string FootprintJson => Path.Combine(_tempDir, "footprint.local.json");
    private string OutputRoot   => Path.Combine(_tempDir, "candidate.local");

    private static string MakeLotFillJson() => """
        {
          "lot_sizing_policy_version": "MAP29C_V1",
          "components": [
            { "component_id": "COMP_TEST", "parcel_class": "BLUE_RESIDENTIAL" }
          ],
          "lots": [
            {
              "component_id": "COMP_TEST", "lot_id": "LOT_A", "frontage_direction": "NORTH",
              "x1": 0, "y1": 0, "x2": 13, "y2": 26, "width": 14, "height": 27, "tile_count": 378,
              "shade_r": 200, "shade_g": 168, "shade_b": 120
            },
            {
              "component_id": "COMP_TEST", "lot_id": "LOT_B", "frontage_direction": "NORTH",
              "x1": 20, "y1": 0, "x2": 33, "y2": 26, "width": 14, "height": 27, "tile_count": 378,
              "shade_r": 200, "shade_g": 168, "shade_b": 120
            }
          ]
        }
        """;

    private static string MakeFootprintJson() => """
        {
          "format": "MAP31B_TEST",
          "footprint_count": 1,
          "skipped_lot_count": 1,
          "total_lot_count": 2,
          "sector_counts": [
            { "sector_id": "DEFAULT", "lot_count": 2 }
          ]
        }
        """;

    private void WriteFixtures()
    {
        File.WriteAllText(LotFillJson,   MakeLotFillJson());
        File.WriteAllText(FootprintJson, MakeFootprintJson());
        Directory.CreateDirectory(OutputRoot);
    }

    private DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilder NewBuilder() =>
        new DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilder();

    // -----------------------------------------------------------------------
    // MAP-32A 1. Output root outside .local is rejected
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_RejectsOutputRoot_OutsideLocal()
    {
        WriteFixtures();
        string badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-map32a-noloc", Guid.NewGuid().ToString());
        Directory.CreateDirectory(badRoot);
        var r = NewBuilder().Build(LotFillJson, FootprintJson, badRoot);
        Assert.False(r.IsValid);
        Assert.True(r.Errors.Count > 0);
        Assert.Contains(r.Checks, c => c.CheckId == "MAP32A_OUTPUT_ROOT_UNDER_LOCAL" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // MAP-32A 2. Staged mod.info is written and contains id=
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_Writes_StagedModInfo()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        Assert.Empty(r.Errors);
        string modInfoPath = Path.Combine(OutputRoot, "mod.info");
        Assert.True(File.Exists(modInfoPath), "mod.info must exist");
        string content = File.ReadAllText(modInfoPath);
        Assert.Contains("id=", content);
        Assert.Contains(DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilder.MapId, content);
    }

    // -----------------------------------------------------------------------
    // MAP-32A 3. Staged map.info is written and contains title=
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_Writes_StagedMapInfo()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        string mapInfoPath = Path.Combine(OutputRoot, "media", "maps",
            DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilder.MapName, "map.info");
        Assert.True(File.Exists(mapInfoPath), "map.info must exist");
        string content = File.ReadAllText(mapInfoPath);
        Assert.Contains("title=", content);
        Assert.Contains(DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilder.MapName, content);
    }

    // -----------------------------------------------------------------------
    // MAP-32A 4. spawnpoints.lua is written and contains SpawnPoints function
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_Writes_SpawnpointsLua()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        Assert.True(r.SpawnpointsWritten);
        string spawnPath = Path.Combine(OutputRoot, "media", "maps",
            DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilder.MapName, "spawnpoints.lua");
        Assert.True(File.Exists(spawnPath));
        string content = File.ReadAllText(spawnPath);
        Assert.Contains("SpawnPoints", content);
        Assert.Contains("worldX", content);
    }

    // -----------------------------------------------------------------------
    // MAP-32A 5. objects.lua is written
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_Writes_ObjectsLua()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        Assert.True(r.ObjectsLuaWritten);
        string objectsPath = Path.Combine(OutputRoot, "media", "maps",
            DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilder.MapName, "objects.lua");
        Assert.True(File.Exists(objectsPath));
    }

    // -----------------------------------------------------------------------
    // MAP-32A 6. LotCount is consumed from lot-fill JSON (2 lots in fixture)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_Consumes_LotCount_FromLotFillJson()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        Assert.Equal(2, r.LotCount);
    }

    // -----------------------------------------------------------------------
    // MAP-32A 7. FootprintCount is consumed from footprint JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_Consumes_FootprintCount_FromFootprintJson()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        Assert.Equal(1, r.FootprintCount);
        Assert.Equal(1, r.SkippedLotCount);
    }

    // -----------------------------------------------------------------------
    // MAP-32A 8. Sector counts are carried forward from footprint JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_CarriesSectorCounts_Forward()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        Assert.True(r.SectorCounts.Count > 0, "SectorCounts must be non-empty");
        Assert.Equal("DEFAULT", r.SectorCounts[0].SectorId);
        Assert.Equal(2, r.SectorCounts[0].LotCount);
    }

    // -----------------------------------------------------------------------
    // MAP-32A 9. Raw source PNG is not mutated
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_DoesNotMutate_RawSourcePng()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        Assert.False(r.RawSourcePngMutated);
    }

    // -----------------------------------------------------------------------
    // MAP-32A 10. Runtime proof is not claimed
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_DoesNotClaim_RuntimeProof()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        Assert.False(r.RuntimeProofClaimed);
        Assert.False(r.RuntimeValid);
        Assert.False(r.PlayableExportClaimed);
        Assert.False(r.PublicPlayablePackagingClaimed);
    }

    // -----------------------------------------------------------------------
    // MAP-32A 11. Binary materialization state is explicit (Path B — false)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_BinaryMaterializationState_IsExplicit()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        // Path B: binary writer not available; gap must be documented
        Assert.False(r.BinaryCellMaterialized);
        Assert.False(string.IsNullOrEmpty(r.BinaryMaterializationGap));
        Assert.Contains("PATH_B", r.BinaryMaterializationGap);
        Assert.True(r.BinaryCellFilesWritten.Count == 0,
            "No binary cell files written under Path B");
    }

    // -----------------------------------------------------------------------
    // MAP-32A 12. All MAP32A checks pass
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_AllChecks_Pass()
    {
        WriteFixtures();
        var r = NewBuilder().Build(LotFillJson, FootprintJson, OutputRoot);
        var map32aChecks = r.Checks.Where(c => c.CheckId.StartsWith("MAP32A")).ToList();
        Assert.True(map32aChecks.Count > 0, "No MAP32A checks found");
        foreach (var c in map32aChecks)
            Assert.True(c.CheckStatus == "PASS",
                $"Check {c.CheckId} FAILED: expected={c.Expected} actual={c.Actual}");
    }
}
