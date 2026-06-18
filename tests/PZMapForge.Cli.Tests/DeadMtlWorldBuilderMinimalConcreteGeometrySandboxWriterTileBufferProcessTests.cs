using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferProcessTests : IDisposable
{
    private readonly string _tempDir;
    private static readonly string s_cliProject =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj"));
    private static readonly string s_repoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferProcessTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pzmapforge-map27b-cli-test.local", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
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
        var obj = new { format = "MAP-27A_SANDBOX_WRITER_FORBIDDEN_OUTPUT_GUARD", guard_count = guards.Length, guards };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_forbidden_output_guard.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private (int exitCode, string stdout, string stderr) RunCli(string[] args)
    {
        var psi = new ProcessStartInfo("dotnet", $"run --project \"{s_cliProject}\" -- " + string.Join(" ", args))
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false
        };
        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private string[] BuildArgs(
        string swrResult, string compOp, string lotOp, string slotOp, string accessOp, string guard,
        string? outputRoot = null, string? outputJson = null, string? outputMd = null,
        string? outputCsv = null, string? summary = null)
    {
        string root = outputRoot ?? Path.Combine(_tempDir, "out.local");
        return new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0",
            "--sandbox-writer-result",    swrResult,
            "--component-operations",     compOp,
            "--lot-operations",           lotOp,
            "--building-slot-operations", slotOp,
            "--access-operations",        accessOp,
            "--forbidden-output-guard",   guard,
            "--output-root",              root,
            "--output-json",              outputJson ?? Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json"),
            "--output-md",                outputMd   ?? Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.md"),
            "--output-csv",               outputCsv  ?? Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.csv"),
            "--summary",                  summary    ?? Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.summary.txt")
        };
    }

    [Fact]
    public void ValidFixture_ExitCode0()
    {
        var args = BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard());
        var (exitCode, _, _) = RunCli(args);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (exitCode, _, stderr) = RunCli(new[] { "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0" });
        Assert.Equal(1, exitCode);
        Assert.Contains("--sandbox-writer-result", stderr);
    }

    [Fact]
    public void MissingSandboxWriterResult_ExitCode1()
    {
        var root = Path.Combine(_tempDir, "out.local");
        var args = BuildArgs(
            Path.Combine(_tempDir, "missing.json"),
            WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            root);
        var (exitCode, _, _) = RunCli(args);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        var badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-no-dotlocal-" + Guid.NewGuid().ToString("N")[..8]);
        var (exitCode, _, stderr) = RunCli(new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0",
            "--sandbox-writer-result",    WriteMap27AResult(),
            "--component-operations",     WriteComponentOp(),
            "--lot-operations",           WriteLotOp(),
            "--building-slot-operations", WriteBuildingSlotOp(),
            "--access-operations",        WriteAccessOp(),
            "--forbidden-output-guard",   WriteForbiddenGuard(),
            "--output-root",              badRoot,
            "--output-json",              Path.Combine(badRoot, "x.json"),
            "--output-md",                Path.Combine(badRoot, "x.md"),
            "--output-csv",               Path.Combine(badRoot, "x.csv"),
            "--summary",                  Path.Combine(badRoot, "x.txt")
        });
        Assert.Equal(1, exitCode);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void ValidFixture_Writes4MainOutputs()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json");
        var outMd   = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.md");
        var outCsv  = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.csv");
        var outTxt  = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.summary.txt");
        var args = BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            root, outJson, outMd, outCsv, outTxt);
        RunCli(args);
        Assert.True(File.Exists(outJson));
        Assert.True(File.Exists(outMd));
        Assert.True(File.Exists(outCsv));
        Assert.True(File.Exists(outTxt));
    }

    [Fact]
    public void ValidFixture_Writes5ExtraOutputFiles()
    {
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(), root));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_buffer_cells.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_buffer_ownership.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_buffer_replay_log.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_buffer_collision_report.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json")));
    }

    [Fact]
    public void OutputJson_VerdictComplete()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_tile_buffer_v0.json");
        RunCli(BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal("MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_COMPLETE",
            doc.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public void OutputJson_SandboxOnlyTrue()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_tile_buffer_v0.json");
        RunCli(BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.True(doc.RootElement.GetProperty("sandbox_only").GetBoolean());
    }

    [Fact]
    public void OutputJson_WriterStageTileBufferV0()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_tile_buffer_v0.json");
        RunCli(BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal("SANDBOX_WRITER_TILE_BUFFER_V0", doc.RootElement.GetProperty("writer_stage").GetString());
    }

    [Fact]
    public void OutputJson_RuntimeValidFalse()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_tile_buffer_v0.json");
        RunCli(BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.False(doc.RootElement.GetProperty("runtime_valid").GetBoolean());
    }

    [Fact]
    public void OutputJson_InputOperationCount17()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_tile_buffer_v0.json");
        RunCli(BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal(17, doc.RootElement.GetProperty("input_operation_count").GetInt32());
    }

    [Fact]
    public void OutputJson_NoLotpackInOutputRoot()
    {
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(), root));
        Assert.Empty(Directory.GetFiles(root, "*.lotpack", SearchOption.AllDirectories));
    }

    [Fact]
    public void OutputJson_NoLuaInOutputRoot()
    {
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(WriteMap27AResult(), WriteComponentOp(), WriteLotOp(), WriteBuildingSlotOp(), WriteAccessOp(), WriteForbiddenGuard(), root));
        Assert.Empty(Directory.GetFiles(root, "*.lua", SearchOption.AllDirectories));
    }

    [Fact]
    public void HelperScript_Exists()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1");
        Assert.True(File.Exists(scriptPath), $"Helper script not found: {scriptPath}");
    }

    [Fact]
    public void HelperScript_DoesNotContainCompileWorldgen()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1");
        string content = File.ReadAllText(scriptPath);
        Assert.DoesNotContain("compile-worldgen", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContainWorldGenOverrideLua()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1");
        string content = File.ReadAllText(scriptPath);
        Assert.DoesNotContain("WorldGenOverride.lua", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContainDotLotpack()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1");
        string content = File.ReadAllText(scriptPath);
        Assert.DoesNotContain(".lotpack", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacyArgNames_ExitCode0()
    {
        var root = Path.Combine(_tempDir, "out.local");
        var args = new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0",
            "--sandbox-writer-result", WriteMap27AResult(),
            "--component-op",          WriteComponentOp(),
            "--lot-op",                WriteLotOp(),
            "--building-slot-op",      WriteBuildingSlotOp(),
            "--access-op",             WriteAccessOp(),
            "--forbidden-guard",       WriteForbiddenGuard(),
            "--output-root",           root,
            "--output-json",           Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json"),
            "--output-md",             Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.md"),
            "--output-csv",            Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.csv"),
            "--summary",               Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.summary.txt")
        };
        var (exitCode, _, _) = RunCli(args);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void CanonicalArgNames_ExitCode0()
    {
        var root = Path.Combine(_tempDir, "out.local");
        var args = new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0",
            "--sandbox-writer-result",    WriteMap27AResult(),
            "--component-operations",     WriteComponentOp(),
            "--lot-operations",           WriteLotOp(),
            "--building-slot-operations", WriteBuildingSlotOp(),
            "--access-operations",        WriteAccessOp(),
            "--forbidden-output-guard",   WriteForbiddenGuard(),
            "--output-root",              root,
            "--output-json",              Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json"),
            "--output-md",                Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.md"),
            "--output-csv",               Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.csv"),
            "--summary",                  Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.summary.txt")
        };
        var (exitCode, _, _) = RunCli(args);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void HelperScript_ContainsCanonicalArgNames()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1"));
        Assert.Contains("--component-operations",     content);
        Assert.Contains("--lot-operations",           content);
        Assert.Contains("--building-slot-operations", content);
        Assert.Contains("--access-operations",        content);
        Assert.Contains("--forbidden-output-guard",   content);
    }

    [Fact]
    public void HelperScript_PointsAtDeadmtlAuthoringPaths()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1"));
        Assert.Contains("deadmtl-authoring",                                               content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-v0",        content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0", content);
    }

    [Fact]
    public void HelperScript_UsesCanonicalMainOutputNames()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1"));
        Assert.Contains("map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.json",       content);
        Assert.Contains("map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.md",         content);
        Assert.Contains("map_00.minimal_concrete_geometry_sandbox_writer_tile_buffer_v0.summary.txt",content);
    }

    [Fact]
    public void HelperScript_DoesNotContainDotLotheader()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0.ps1"));
        Assert.DoesNotContain(".lotheader", content, StringComparison.OrdinalIgnoreCase);
    }
}
