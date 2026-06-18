using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _outputRoot;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilderTests()
    {
        _tempDir    = Path.Combine(Path.GetTempPath(), "pzmapforge-map27b-core-test.local", Guid.NewGuid().ToString());
        _outputRoot = Path.Combine(_tempDir, "out.local");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_outputRoot);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string WriteMap27AResult()
    {
        var obj = new
        {
            format               = "MAP-27A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_V0",
            map_id               = "map_00",
            target_component_id  = "map_00_component_0001",
            verdict              = "MAP27A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_V0_COMPLETE",
            is_valid             = true,
            sandbox_only         = true,
            writer_stage         = "SANDBOX_WRITER_V0",
            operation_count      = 17,
            operation_file_count = 5,
            runtime_valid        = false,
            materialized         = false,
            runtime_proof_claimed= false,
            public_playable_packaging_claimed = false
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_v0.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteComponentOp()
    {
        var obj = new
        {
            format          = "MAP-27A_SANDBOX_WRITER_COMPONENT_OPERATIONS",
            operation_count = 1,
            operations = new object[]
            {
                new { operation_order = 1, operation_kind = "COMPONENT_ENVELOPE_WRITE",
                      operation_group = "COMPONENT_ENVELOPE",
                      target_component_id = "map_00_component_0001",
                      min_x = 124, min_y = 10, max_x = 212, max_y = 69,
                      width_px = 89, height_px = 60, runtime_effect = "NONE" }
            }
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_component_operations.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteLotOp()
    {
        var lots = Enumerable.Range(1, 7).Select(i => (object)new
        {
            operation_order = i,
            operation_kind  = "LOT_BOUNDARY_WRITE",
            operation_group = "LOT_BOUNDARY",
            lot_id          = $"map_00_comp0001_lot_{i:D4}",
            min_x = 124 + (i - 1) * 13,
            min_y = 10,
            max_x = 124 + (i - 1) * 13 + 12,
            max_y = 69,
            width_px  = 13,
            height_px = 60,
            runtime_effect = "NONE"
        }).ToArray();
        var obj = new { format = "MAP-27A_SANDBOX_WRITER_LOT_OPERATIONS", operation_count = 7, operations = lots };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_lot_operations.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteBuildingSlotOp()
    {
        var slots = Enumerable.Range(1, 7).Select(i => (object)new
        {
            operation_order = i,
            operation_kind  = "BUILDING_FOOTPRINT_WRITE",
            operation_group = "BUILDING_FOOTPRINT",
            slot_id         = $"map_00_comp0001_slot_{i:D4}",
            min_x = 126 + (i - 1) * 13,
            min_y = 13,
            max_x = 134 + (i - 1) * 13,
            max_y = 66,
            width_px  = 9,
            height_px = 54,
            runtime_effect = "NONE"
        }).ToArray();
        var obj = new { format = "MAP-27A_SANDBOX_WRITER_BUILDING_SLOT_OPERATIONS", operation_count = 7, operations = slots };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_building_slot_operations.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteAccessOp()
    {
        var ops = new object[]
        {
            new { operation_order = 1, operation_kind = "ACCESS_LINK_WRITE", operation_group = "ACCESS_LINK",
                  access_id = "map_00_comp0001_frontage_access", access_kind = "FRONTAGE_ACCESS", side = "NORTH",
                  min_x = 0, min_y = 0, max_x = 0, max_y = 0, width_px = 0, height_px = 0, runtime_effect = "NONE" },
            new { operation_order = 2, operation_kind = "ACCESS_LINK_WRITE", operation_group = "ACCESS_LINK",
                  access_id = "map_00_comp0001_rear_service_access", access_kind = "REAR_SERVICE_ACCESS", side = "EAST",
                  min_x = 0, min_y = 0, max_x = 0, max_y = 0, width_px = 0, height_px = 0, runtime_effect = "NONE" }
        };
        var obj = new { format = "MAP-27A_SANDBOX_WRITER_ACCESS_OPERATIONS", operation_count = 2, operations = ops };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_access_operations.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteForbiddenGuard()
    {
        string[] families  = { "LOT_PACK_RUNTIME_BINARY", "LOT_HEADER_RUNTIME_BINARY", "WORLDGEN_OVERRIDE_LUA",
                                "RUNTIME_LUA", "PROJECT_ZOMBOID_INSTALL_PATH", "STEAM_WORKSHOP_OUTPUT",
                                "COMPILE_WORLDGEN_INVOCATION", "MAP_00_PNG_MUTATION" };
        string[] patterns  = { "*.lotpack", "*.lotheader", "WorldGenOverride.lua",
                                "*.lua", "*/Project Zomboid/*", "*/steamapps/workshop/*",
                                "compile-worldgen", "map_00.png" };
        var guards = families.Select((fid, idx) => new
        {
            guard_order     = idx + 1,
            family_id       = fid,
            blocked_pattern = patterns[idx],
            status          = "NOT_EMITTED"
        }).ToArray();
        var obj = new
        {
            format       = "MAP-27A_SANDBOX_WRITER_FORBIDDEN_OUTPUT_GUARD",
            guard_count  = guards.Length,
            guards       = guards
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_forbidden_output_guard.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferResult RunBuild()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        return builder.Build(
            WriteMap27AResult(),
            WriteComponentOp(),
            WriteLotOp(),
            WriteBuildingSlotOp(),
            WriteAccessOp(),
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
        Assert.Equal("MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_COMPLETE", r.Verdict);
    }

    [Fact]
    public void Build_ValidInputs_SandboxOnlyTrue()
    {
        var r = RunBuild();
        Assert.True(r.SandboxOnly);
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
    public void Build_ValidInputs_WriterStageTileBufferV0()
    {
        var r = RunBuild();
        Assert.Equal("SANDBOX_WRITER_TILE_BUFFER_V0", r.WriterStage);
    }

    [Fact]
    public void Build_ValidInputs_BufferDimensions256()
    {
        var r = RunBuild();
        Assert.Equal(256, r.BufferWidth);
        Assert.Equal(256, r.BufferHeight);
    }

    [Fact]
    public void Build_ValidInputs_InputOperationCount17()
    {
        var r = RunBuild();
        Assert.Equal(17, r.InputOperationCount);
    }

    [Fact]
    public void Build_ValidInputs_ReplayEntryCount17()
    {
        var r = RunBuild();
        Assert.Equal(17, r.ReplayEntryCount);
    }

    [Fact]
    public void Build_ValidInputs_TouchedCellCountGreaterThanZero()
    {
        var r = RunBuild();
        Assert.True(r.TouchedCellCount > 0);
    }

    [Fact]
    public void Build_ValidInputs_CollisionCountGreaterThanZero()
    {
        var r = RunBuild();
        Assert.True(r.CollisionCount > 0);
    }

    [Fact]
    public void Build_ValidInputs_OwnershipKindCount4()
    {
        var r = RunBuild();
        Assert.Equal(4, r.OwnershipKindCount);
    }

    [Fact]
    public void Build_ValidInputs_CheckCountIs38()
    {
        var r = RunBuild();
        Assert.Equal(38, r.CheckCount);
    }

    [Fact]
    public void Build_ValidInputs_NoFailedChecks()
    {
        var r = RunBuild();
        Assert.Equal(0, r.FailedCheckCount);
    }

    [Fact]
    public void Build_ValidInputs_Writes5ExtraOutputFiles()
    {
        RunBuild();
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_buffer_cells.csv")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_buffer_ownership.json")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_buffer_replay_log.json")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_buffer_collision_report.json")));
        Assert.True(File.Exists(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json")));
    }

    [Fact]
    public void Build_ValidInputs_OutputFilesListCount5()
    {
        var r = RunBuild();
        Assert.Equal(5, r.OutputFiles.Count);
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
    public void Build_MissingSandboxWriterResult_IsValidFalse()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        var r = builder.Build(
            Path.Combine(_tempDir, "missing.json"),
            WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            _outputRoot);
        Assert.False(r.IsValid);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_MissingOperationFile_IsValidFalse()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        var r = builder.Build(
            WriteMap27AResult(),
            Path.Combine(_tempDir, "missing_comp.json"),
            WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            _outputRoot);
        Assert.False(r.IsValid);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void RenderJson_ContainsVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        var r   = RunBuild();
        var json = builder.RenderJson(r);
        Assert.Contains("MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_COMPLETE", json);
    }

    [Fact]
    public void RenderMarkdown_ContainsChecksHeader()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        var r   = RunBuild();
        var md  = builder.RenderMarkdown(r);
        Assert.Contains("## Checks", md);
    }

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        var r   = RunBuild();
        var csv = builder.RenderCsv(r);
        Assert.Contains("check_order,check_id,check_status", csv);
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        var r       = RunBuild();
        var summary = builder.RenderSummary(r);
        Assert.Contains("MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_COMPLETE", summary);
    }

    [Fact]
    public void RenderSummary_SandboxOnlyIs1()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        var r       = RunBuild();
        var summary = builder.RenderSummary(r);
        Assert.Contains("Sandbox Only       : 1", summary);
    }

    [Fact]
    public void RenderSummary_WriterReadyIs0()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder();
        var r       = RunBuild();
        var summary = builder.RenderSummary(r);
        Assert.Contains("Writer Ready       : 0", summary);
    }

    [Fact]
    public void Build_ValidInputs_AccessLinkPrimaryOwnedCellCountGreaterThanZero()
    {
        var r = RunBuild();
        var check = r.Checks.FirstOrDefault(c => c.CheckId == "ACCESS_LINK_PRIMARY_OWNED_GT_0");
        Assert.NotNull(check);
        Assert.Equal("PASS", check.CheckStatus);
    }

    [Fact]
    public void Build_ValidInputs_AccessReplayEntriesHavePositiveAppliedCellCount()
    {
        RunBuild();
        var json = File.ReadAllText(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_buffer_replay_log.json"));
        using var doc = JsonDocument.Parse(json);
        var entries = doc.RootElement.GetProperty("replay_entries").EnumerateArray().ToList();
        var accessEntries = entries.Where(e =>
            e.GetProperty("operation_kind").GetString() == "ACCESS_LINK_WRITE").ToList();
        Assert.NotEmpty(accessEntries);
        foreach (var entry in accessEntries)
            Assert.True(entry.GetProperty("applied_cell_count").GetInt32() > 0);
    }

    [Fact]
    public void Build_ValidInputs_OperationIdsContainHashSeparator()
    {
        RunBuild();
        var csv = File.ReadAllText(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_buffer_cells.csv"));
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1).Take(5).ToList();
        Assert.NotEmpty(lines);
        foreach (var line in lines)
        {
            var cols = line.Split(',');
            Assert.True(cols.Length >= 6, $"Expected at least 6 columns, got {cols.Length}");
            Assert.Contains("#", cols[5]);
        }
    }

    [Fact]
    public void Build_ValidInputs_OwnershipReportAccessLinkPrimaryOwnedGreaterThanZero()
    {
        RunBuild();
        var json = File.ReadAllText(Path.Combine(_outputRoot, "map_00.sandbox_writer_tile_buffer_ownership.json"));
        using var doc = JsonDocument.Parse(json);
        bool found = false;
        foreach (var record in doc.RootElement.GetProperty("ownership").EnumerateArray())
        {
            if (record.GetProperty("kind").GetString() == "ACCESS_LINK")
            {
                Assert.True(record.GetProperty("primary_owned_cell_count").GetInt32() > 0);
                found = true;
                break;
            }
        }
        Assert.True(found, "No ACCESS_LINK ownership record found");
    }
}
