using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerProcessTests : IDisposable
{
    private readonly string _tempDir;
    private static readonly string s_cliProject =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj"));
    private static readonly string s_repoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerProcessTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pzmapforge-map27c-cli-test.local", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

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
                new { kind = "COMPONENT_ENVELOPE", primary_owned_cell_count = 0 },
                new { kind = "LOT_BOUNDARY",       primary_owned_cell_count = 2 },
                new { kind = "BUILDING_FOOTPRINT", primary_owned_cell_count = 9 },
                new { kind = "ACCESS_LINK",        primary_owned_cell_count = 2 },
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
        string tbResult, string tbCells, string tbOwnership, string tbReplay,
        string tbCollision, string tbGuard,
        string? outputRoot = null, string? outputJson = null, string? outputMd = null,
        string? outputCsv = null, string? summary = null)
    {
        string root = outputRoot ?? Path.Combine(_tempDir, "out.local");
        return new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0",
            "--tile-buffer-result",              tbResult,
            "--tile-buffer-cells",               tbCells,
            "--tile-buffer-ownership",           tbOwnership,
            "--tile-buffer-replay-log",          tbReplay,
            "--tile-buffer-collision-report",    tbCollision,
            "--tile-buffer-forbidden-output-guard", tbGuard,
            "--output-root",  root,
            "--output-json",  outputJson ?? Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"),
            "--output-md",    outputMd   ?? Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md"),
            "--output-csv",   outputCsv  ?? Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv"),
            "--summary",      summary    ?? Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt"),
        };
    }

    [Fact]
    public void ValidFixture_ExitCode0()
    {
        var args = BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard());
        var (exitCode, _, _) = RunCli(args);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (exitCode, _, stderr) = RunCli(new[]
            { "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0" });
        Assert.Equal(1, exitCode);
        Assert.Contains("--tile-buffer-result", stderr);
    }

    [Fact]
    public void MissingTileBufferResult_ExitCode1()
    {
        var root = Path.Combine(_tempDir, "out.local");
        var args = BuildArgs(
            Path.Combine(_tempDir, "missing.json"),
            WriteTileBufferCells(), WriteTileBufferOwnership(), WriteTileBufferReplayLog(),
            WriteTileBufferCollisionReport(), WriteForbiddenGuard(), root);
        var (exitCode, _, _) = RunCli(args);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        var badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-no-dotlocal-" + Guid.NewGuid().ToString("N")[..8]);
        var (exitCode, _, stderr) = RunCli(new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0",
            "--tile-buffer-result",              WriteTileBufferResult(),
            "--tile-buffer-cells",               WriteTileBufferCells(),
            "--tile-buffer-ownership",           WriteTileBufferOwnership(),
            "--tile-buffer-replay-log",          WriteTileBufferReplayLog(),
            "--tile-buffer-collision-report",    WriteTileBufferCollisionReport(),
            "--tile-buffer-forbidden-output-guard", WriteForbiddenGuard(),
            "--output-root",  badRoot,
            "--output-json",  Path.Combine(badRoot, "x.json"),
            "--output-md",    Path.Combine(badRoot, "x.md"),
            "--output-csv",   Path.Combine(badRoot, "x.csv"),
            "--summary",      Path.Combine(badRoot, "x.txt"),
        });
        Assert.Equal(1, exitCode);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void ValidFixture_Writes4MainOutputs()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        var outMd   = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md");
        var outCsv  = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv");
        var outTxt  = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt");
        var args = BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(),
            root, outJson, outMd, outCsv, outTxt);
        RunCli(args);
        Assert.True(File.Exists(outJson));
        Assert.True(File.Exists(outMd));
        Assert.True(File.Exists(outCsv));
        Assert.True(File.Exists(outTxt));
    }

    [Fact]
    public void ValidFixture_Writes6ExtraOutputFiles()
    {
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(), root));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_materialized_cells.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_material_palette.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_layer_stack.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_materialization_replay_log.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_materialization_ownership_summary.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json")));
    }

    [Fact]
    public void OutputJson_VerdictComplete()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        RunCli(BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal("MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            doc.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public void OutputJson_SandboxOnlyTrue()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        RunCli(BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.True(doc.RootElement.GetProperty("sandbox_only").GetBoolean());
    }

    [Fact]
    public void OutputJson_SandboxMaterializedTrue()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        RunCli(BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.True(doc.RootElement.GetProperty("sandbox_materialized").GetBoolean());
    }

    [Fact]
    public void OutputJson_PzRuntimeMaterializedFalse()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        RunCli(BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.False(doc.RootElement.GetProperty("pz_runtime_materialized").GetBoolean());
    }

    [Fact]
    public void OutputJson_WriterStageTileMaterializerV0()
    {
        var root    = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        RunCli(BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(),
            root, outJson));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal("SANDBOX_WRITER_TILE_MATERIALIZER_V0", doc.RootElement.GetProperty("writer_stage").GetString());
    }

    [Fact]
    public void OutputJson_NoLotpackInOutputRoot()
    {
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(), root));
        Assert.Empty(Directory.GetFiles(root, "*.lotpack", SearchOption.AllDirectories));
    }

    [Fact]
    public void OutputJson_NoLuaInOutputRoot()
    {
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(WriteTileBufferResult(), WriteTileBufferCells(), WriteTileBufferOwnership(),
            WriteTileBufferReplayLog(), WriteTileBufferCollisionReport(), WriteForbiddenGuard(), root));
        Assert.Empty(Directory.GetFiles(root, "*.lua", SearchOption.AllDirectories));
    }

    [Fact]
    public void HelperScript_Exists()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1");
        Assert.True(File.Exists(scriptPath), $"Helper script not found: {scriptPath}");
    }

    [Fact]
    public void HelperScript_DoesNotContainCompileWorldgen()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1"));
        Assert.DoesNotContain("compile-worldgen", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContainWorldGenOverrideLua()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1"));
        Assert.DoesNotContain("WorldGenOverride.lua", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContainDotLotpack()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1"));
        Assert.DoesNotContain(".lotpack", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContainDotLotheader()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1"));
        Assert.DoesNotContain(".lotheader", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_ContainsCanonicalArgNames()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1"));
        Assert.Contains("--tile-buffer-result",              content);
        Assert.Contains("--tile-buffer-cells",               content);
        Assert.Contains("--tile-buffer-ownership",           content);
        Assert.Contains("--tile-buffer-replay-log",          content);
        Assert.Contains("--tile-buffer-collision-report",    content);
        Assert.Contains("--tile-buffer-forbidden-output-guard", content);
    }

    [Fact]
    public void HelperScript_PointsAtDeadmtlAuthoringPaths()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1"));
        Assert.Contains("deadmtl-authoring",                                                       content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-buffer-v0",    content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0", content);
    }

    [Fact]
    public void HelperScript_UsesCanonicalMainOutputNames()
    {
        string content = File.ReadAllText(Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0.ps1"));
        Assert.Contains("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json",       content);
        Assert.Contains("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md",         content);
        Assert.Contains("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt",content);
    }
}
