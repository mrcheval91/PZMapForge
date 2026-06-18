using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _outputRoot;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilderTests()
    {
        _tempDir    = Path.Combine(Path.GetTempPath(), "pzmapforge-map27c-core-test.local", Guid.NewGuid().ToString());
        _outputRoot = Path.Combine(_tempDir, "out.local");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_outputRoot);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // Synthetic MAP-27B result JSON: 13 touched cells matching the fixture CSV.
    private string WriteTileBufferResult()
    {
        var obj = new
        {
            format              = "MAP-27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0",
            map_id              = "map_00",
            target_component_id = "map_00_component_0001",
            verdict             = "MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_COMPLETE",
            is_valid            = true,
            sandbox_only        = true,
            writer_stage        = "SANDBOX_WRITER_TILE_BUFFER_V0",
            touched_cell_count  = 13,
            buffer_width        = 256,
            buffer_height       = 256,
            writer_ready        = false,
            runtime_valid       = false,
            materialized        = false,
            runtime_proof_claimed = false,
            public_playable_packaging_claimed = false,
        };
        string path = Path.Combine(_tempDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    // 9 BUILDING_FOOTPRINT cells (3x3 at (10,10)-(12,12)), 2 ACCESS_LINK, 2 LOT_BOUNDARY = 13 total.
    private string WriteTileBufferCells()
    {
        var sb = new StringBuilder();
        sb.AppendLine("x,y,primary_owner_kind,primary_owner_id,tags,operation_ids,collision_count");
        for (int x = 10; x <= 12; x++)
            for (int y = 10; y <= 12; y++)
                sb.AppendLine($"{x},{y},BUILDING_FOOTPRINT,map_00_comp0001_slot_0001," +
                    "COMPONENT_ENVELOPE|LOT_BOUNDARY|BUILDING_FOOTPRINT," +
                    "map_00.sandbox_writer_building_slot_operations.json#BUILDING_FOOTPRINT_WRITE#1,2");
        sb.AppendLine("5,5,ACCESS_LINK,map_00_comp0001_frontage_access,ACCESS_LINK," +
            "map_00.sandbox_writer_access_operations.json#ACCESS_LINK_WRITE#1,0");
        sb.AppendLine("6,5,ACCESS_LINK,map_00_comp0001_frontage_access,ACCESS_LINK," +
            "map_00.sandbox_writer_access_operations.json#ACCESS_LINK_WRITE#1,0");
        sb.AppendLine("14,14,LOT_BOUNDARY,map_00_comp0001_lot_0001,LOT_BOUNDARY," +
            "map_00.sandbox_writer_lot_operations.json#LOT_BOUNDARY_WRITE#1,0");
        sb.AppendLine("15,14,LOT_BOUNDARY,map_00_comp0001_lot_0001,LOT_BOUNDARY," +
            "map_00.sandbox_writer_lot_operations.json#LOT_BOUNDARY_WRITE#1,0");
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_buffer_cells.csv");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private string WriteTileBufferOwnership()
    {
        var obj = new
        {
            format        = "MAP-27B_SANDBOX_WRITER_TILE_BUFFER_OWNERSHIP",
            generated_utc = "2026-06-18T00:00:00Z",
            map_id        = "map_00",
            sandbox_only  = true,
            kind_count    = 4,
            ownership = new object[]
            {
                new { kind = "COMPONENT_ENVELOPE", primary_owned_cell_count = 0, claimed_cell_count = 9 },
                new { kind = "LOT_BOUNDARY",       primary_owned_cell_count = 2, claimed_cell_count = 11 },
                new { kind = "BUILDING_FOOTPRINT", primary_owned_cell_count = 9, claimed_cell_count = 9 },
                new { kind = "ACCESS_LINK",        primary_owned_cell_count = 2, claimed_cell_count = 2 },
            }
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_buffer_ownership.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteTileBufferReplayLog()
    {
        var obj = new
        {
            format             = "MAP-27B_SANDBOX_WRITER_TILE_BUFFER_REPLAY_LOG",
            generated_utc      = "2026-06-18T00:00:00Z",
            map_id             = "map_00",
            sandbox_only       = true,
            replay_entry_count = 0,
            replay_entries     = Array.Empty<object>()
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_buffer_replay_log.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteTileBufferCollisionReport()
    {
        var obj = new
        {
            format          = "MAP-27B_SANDBOX_WRITER_TILE_BUFFER_COLLISION_REPORT",
            generated_utc   = "2026-06-18T00:00:00Z",
            map_id          = "map_00",
            sandbox_only    = true,
            collision_count = 0,
            collisions      = Array.Empty<object>()
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_buffer_collision_report.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteForbiddenGuard()
    {
        string[] families = { "LOT_PACK_RUNTIME_BINARY", "LOT_HEADER_RUNTIME_BINARY", "WORLDGEN_OVERRIDE_LUA",
                               "RUNTIME_LUA", "PROJECT_ZOMBOID_INSTALL_PATH", "STEAM_WORKSHOP_OUTPUT",
                               "COMPILE_WORLDGEN_INVOCATION", "MAP_00_PNG_MUTATION" };
        string[] patterns = { "*.lotpack", "*.lotheader", "WorldGenOverride.lua",
                               "*.lua", "*/Project Zomboid/*", "*/steamapps/workshop/*",
                               "compile-worldgen", "map_00.png" };
        var guards = families.Select((fid, idx) => new
        {
            guard_order     = idx + 1,
            family_id       = fid,
            blocked_pattern = patterns[idx],
            status          = "NOT_EMITTED"
        }).ToArray();
        var obj = new { format = "MAP-27B_SANDBOX_WRITER_TILE_BUFFER_FORBIDDEN_OUTPUT_GUARD", guard_count = guards.Length, guards };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerResult RunBuild()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        return builder.Build(
            WriteTileBufferResult(),
            WriteTileBufferCells(),
            WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(),
            WriteTileBufferCollisionReport(),
            WriteForbiddenGuard(),
            _outputRoot);
    }

    [Fact]
    public void Build_ValidInputs_IsValidTrue()
    {
        var r = RunBuild();
        Assert.True(r.IsValid);
    }

    [Fact]
    public void Build_ValidInputs_VerdictComplete()
    {
        var r = RunBuild();
        Assert.Equal("MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE", r.Verdict);
    }

    [Fact]
    public void Build_ValidInputs_SandboxOnlyTrue()
    {
        var r = RunBuild();
        Assert.True(r.SandboxOnly);
    }

    [Fact]
    public void Build_ValidInputs_SandboxMaterializedTrue()
    {
        var r = RunBuild();
        Assert.True(r.SandboxMaterialized);
    }

    [Fact]
    public void Build_ValidInputs_PzRuntimeMaterializedFalse()
    {
        var r = RunBuild();
        Assert.False(r.PzRuntimeMaterialized);
    }

    [Fact]
    public void Build_ValidInputs_WriterReadyFalse()
    {
        var r = RunBuild();
        Assert.False(r.WriterReady);
    }

    [Fact]
    public void Build_ValidInputs_RuntimeValidFalse()
    {
        var r = RunBuild();
        Assert.False(r.RuntimeValid);
    }

    [Fact]
    public void Build_ValidInputs_MaterializedFalse()
    {
        var r = RunBuild();
        Assert.False(r.Materialized);
    }

    [Fact]
    public void Build_ValidInputs_RuntimeProofClaimedFalse()
    {
        var r = RunBuild();
        Assert.False(r.RuntimeProofClaimed);
    }

    [Fact]
    public void Build_ValidInputs_PublicPlayablePackagingClaimedFalse()
    {
        var r = RunBuild();
        Assert.False(r.PublicPlayablePackagingClaimed);
    }

    [Fact]
    public void Build_ValidInputs_WriterStageTileMaterializerV0()
    {
        var r = RunBuild();
        Assert.Equal("SANDBOX_WRITER_TILE_MATERIALIZER_V0", r.WriterStage);
    }

    [Fact]
    public void Build_ValidInputs_MaterializedCellCountMatchesInput()
    {
        var r = RunBuild();
        Assert.Equal(r.InputTouchedCellCount, r.MaterializedCellCount);
        Assert.Equal(13, r.MaterializedCellCount);
    }

    [Fact]
    public void Build_ValidInputs_BuildingFootprintCellCountGt0()
    {
        var r = RunBuild();
        Assert.True(r.BuildingFootprintCellCount > 0);
        Assert.Equal(9, r.BuildingFootprintCellCount);
    }

    [Fact]
    public void Build_ValidInputs_BuildingWallCandidateCellCountGt0()
    {
        var r = RunBuild();
        Assert.True(r.BuildingWallCandidateCellCount > 0);
        Assert.Equal(8, r.BuildingWallCandidateCellCount);
    }

    [Fact]
    public void Build_ValidInputs_BuildingFloorCandidateCellCountGt0()
    {
        var r = RunBuild();
        Assert.True(r.BuildingFloorCandidateCellCount > 0);
        Assert.Equal(1, r.BuildingFloorCandidateCellCount);
    }

    [Fact]
    public void Build_ValidInputs_AccessEdgeCellCountGt0()
    {
        var r = RunBuild();
        Assert.True(r.AccessEdgeCellCount > 0);
        Assert.Equal(2, r.AccessEdgeCellCount);
    }

    [Fact]
    public void Build_ValidInputs_LotSpaceCellCountGt0()
    {
        var r = RunBuild();
        Assert.True(r.LotSpaceCellCount > 0);
        Assert.Equal(2, r.LotSpaceCellCount);
    }

    [Fact]
    public void Build_ValidInputs_MaterialKindCount5()
    {
        var r = RunBuild();
        Assert.Equal(5, r.MaterialKindCount);
    }

    [Fact]
    public void Build_ValidInputs_LayerKindCount5()
    {
        var r = RunBuild();
        Assert.Equal(5, r.LayerKindCount);
    }

    [Fact]
    public void Build_ValidInputs_CheckCountIs30()
    {
        var r = RunBuild();
        Assert.Equal(30, r.CheckCount);
    }

    [Fact]
    public void Build_ValidInputs_NoFailedChecks()
    {
        var r = RunBuild();
        Assert.Equal(0, r.FailedCheckCount);
    }

    [Fact]
    public void Build_ValidInputs_Writes6ExtraOutputFiles()
    {
        RunBuild();
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_materialized_cells.csv")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_material_palette.json")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_layer_stack.json")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_materialization_replay_log.json")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_materialization_ownership_summary.json")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json")));
    }

    [Fact]
    public void Build_ValidInputs_OutputFilesListCount6()
    {
        var r = RunBuild();
        Assert.Equal(6, r.OutputFiles.Count);
    }

    [Fact]
    public void Build_ValidInputs_AllOutputFilesWrittenTrue()
    {
        var r = RunBuild();
        Assert.True(r.OutputFiles.All(f => f.Written));
    }

    [Fact]
    public void Build_ValidInputs_AllOutputFilesHaveSha256()
    {
        var r = RunBuild();
        Assert.True(r.OutputFiles.All(f => !string.IsNullOrEmpty(f.Sha256)));
    }

    [Fact]
    public void Build_ValidInputs_NoLotpackInOutputRoot()
    {
        RunBuild();
        Assert.Empty(Directory.GetFiles(_outputRoot, "*.lotpack", SearchOption.AllDirectories));
    }

    [Fact]
    public void Build_ValidInputs_NoLuaInOutputRoot()
    {
        RunBuild();
        Assert.Empty(Directory.GetFiles(_outputRoot, "*.lua", SearchOption.AllDirectories));
    }

    [Fact]
    public void Build_MissingTileBufferResult_IsValidFalse()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        var r = builder.Build(
            Path.Combine(_tempDir, "missing.json"),
            WriteTileBufferCells(), WriteTileBufferOwnership(), WriteTileBufferReplayLog(),
            WriteTileBufferCollisionReport(), WriteForbiddenGuard(), _outputRoot);
        Assert.False(r.IsValid);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_MissingCellsCsv_IsValidFalse()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        var r = builder.Build(
            WriteTileBufferResult(),
            Path.Combine(_tempDir, "missing_cells.csv"),
            WriteTileBufferOwnership(), WriteTileBufferReplayLog(),
            WriteTileBufferCollisionReport(), WriteForbiddenGuard(), _outputRoot);
        Assert.False(r.IsValid);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void RenderJson_ContainsVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        var r    = RunBuild();
        var json = builder.RenderJson(r);
        Assert.Contains("MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE", json);
    }

    [Fact]
    public void RenderMarkdown_ContainsChecksHeader()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        var r  = RunBuild();
        var md = builder.RenderMarkdown(r);
        Assert.Contains("## Checks", md);
    }

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        var r   = RunBuild();
        var csv = builder.RenderCsv(r);
        Assert.Contains("check_order,check_id,check_status", csv);
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        var r       = RunBuild();
        var summary = builder.RenderSummary(r);
        Assert.Contains("MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE", summary);
    }

    [Fact]
    public void RenderSummary_SandboxOnlyIs1()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        var r       = RunBuild();
        var summary = builder.RenderSummary(r);
        Assert.Contains("Sandbox Only           : 1", summary);
    }

    [Fact]
    public void RenderSummary_WriterReadyIs0()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder();
        var r       = RunBuild();
        var summary = builder.RenderSummary(r);
        Assert.Contains("Writer Ready           : 0", summary);
    }

    [Fact]
    public void Build_ValidInputs_MaterializedCellsCsvHasMaterialKindColumn()
    {
        RunBuild();
        var csv   = File.ReadAllText(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_materialized_cells.csv"));
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length > 1, "CSV has no data rows");
        var dataLine = lines[1].TrimEnd('\r').Split(',');
        Assert.True(dataLine.Length >= 6, $"Expected at least 6 columns, got {dataLine.Length}");
        Assert.NotEmpty(dataLine[4]);
    }

    [Fact]
    public void Build_ValidInputs_MaterialPaletteHas5Records()
    {
        RunBuild();
        var json = File.ReadAllText(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_material_palette.json"));
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(5, doc.RootElement.GetProperty("palette").GetArrayLength());
    }

    [Fact]
    public void Build_ValidInputs_LayerStackHas5Records()
    {
        RunBuild();
        var json = File.ReadAllText(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_layer_stack.json"));
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(5, doc.RootElement.GetProperty("layers").GetArrayLength());
    }

    [Fact]
    public void Build_ValidInputs_LayerStackOrderIsCanonical()
    {
        RunBuild();
        var json   = File.ReadAllText(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_layer_stack.json"));
        using var doc = JsonDocument.Parse(json);
        var layers = doc.RootElement.GetProperty("layers").EnumerateArray().ToList();
        Assert.Equal("COMPONENT", layers[0].GetProperty("layer_kind").GetString());
        Assert.Equal("LOT",       layers[1].GetProperty("layer_kind").GetString());
        Assert.Equal("ACCESS",    layers[2].GetProperty("layer_kind").GetString());
        Assert.Equal("FLOOR",     layers[3].GetProperty("layer_kind").GetString());
        Assert.Equal("WALL",      layers[4].GetProperty("layer_kind").GetString());
    }
}
