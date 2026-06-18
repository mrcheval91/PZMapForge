using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using Xunit;

#pragma warning disable CA1416

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayProcessTests
    : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map27d-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteMaterializerResult()
    {
        var obj = new
        {
            format                            = "MAP-27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            map_id                            = "map_00",
            target_component_id               = "map_00_component_0001",
            verdict                           = "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            is_valid                          = true,
            sandbox_only                      = true,
            sandbox_materialized              = true,
            pz_runtime_materialized           = false,
            writer_stage                      = "SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            materialized_cell_count           = 13,
            writer_ready                      = false,
            runtime_valid                     = false,
            materialized                      = false,
            runtime_proof_claimed             = false,
            public_playable_packaging_claimed = false,
        };
        string path = Path.Combine(_tempDir,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteMaterializedCells()
    {
        var sb = new StringBuilder();
        sb.AppendLine("x,y,primary_owner_kind,primary_owner_id,material_kind,layer_kind,source_operation_ids,collision_count");
        for (int x = 10; x <= 12; x++)
        {
            for (int y = 10; y <= 12; y++)
            {
                bool isCenter    = x == 11 && y == 11;
                string matKind   = isCenter ? "BUILDING_INTERIOR_FLOOR_CANDIDATE" : "BUILDING_EXTERIOR_WALL_CANDIDATE";
                string layerKind = isCenter ? "FLOOR" : "WALL";
                sb.AppendLine($"{x},{y},BUILDING_FOOTPRINT,map_00_comp0001_slot_0001,{matKind},{layerKind}," +
                    "map_00.sandbox_writer_building_slot_operations.json#BUILDING_FOOTPRINT_WRITE#1,2");
            }
        }
        sb.AppendLine("5,5,ACCESS_LINK,map_00_comp0001_frontage_access,ACCESS_EDGE_CANDIDATE,ACCESS," +
            "map_00.sandbox_writer_access_operations.json#ACCESS_LINK_WRITE#1,0");
        sb.AppendLine("6,5,ACCESS_LINK,map_00_comp0001_frontage_access,ACCESS_EDGE_CANDIDATE,ACCESS," +
            "map_00.sandbox_writer_access_operations.json#ACCESS_LINK_WRITE#1,0");
        sb.AppendLine("14,14,LOT_BOUNDARY,map_00_comp0001_lot_0001,LOT_YARD_OR_SERVICE_SPACE_CANDIDATE,LOT," +
            "map_00.sandbox_writer_lot_operations.json#LOT_BOUNDARY_WRITE#1,0");
        sb.AppendLine("15,14,LOT_BOUNDARY,map_00_comp0001_lot_0001,LOT_YARD_OR_SERVICE_SPACE_CANDIDATE,LOT," +
            "map_00.sandbox_writer_lot_operations.json#LOT_BOUNDARY_WRITE#1,0");
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_materialized_cells.csv");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private string WriteMaterialPalette()
    {
        var obj = new { format = "MAP-27C_SANDBOX_WRITER_TILE_MATERIAL_PALETTE", map_id = "map_00", sandbox_only = true, material_kind_count = 5 };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_material_palette.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteLayerStack()
    {
        var obj = new { format = "MAP-27C_SANDBOX_WRITER_TILE_LAYER_STACK", map_id = "map_00", sandbox_only = true, layer_kind_count = 5 };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_layer_stack.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteMaterializationReplayLog()
    {
        var obj = new { format = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOG", map_id = "map_00", replay_entry_count = 0 };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_materialization_replay_log.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteMaterializationOwnershipSummary()
    {
        var obj = new { format = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZATION_OWNERSHIP_SUMMARY", map_id = "map_00", owner_kind_count = 4 };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_materialization_ownership_summary.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteMaterializerForbiddenOutputGuard()
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
            status          = "NOT_EMITTED",
        }).ToArray();
        var obj = new { format = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZER_FORBIDDEN_OUTPUT_GUARD", guard_count = guards.Length, guards };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string[] BuildArgs(
        string? resultOverride = null,
        string? outputRootOverride = null)
    {
        string outputRoot = outputRootOverride ?? Path.Combine(_tempDir, "output.local");
        Directory.CreateDirectory(outputRoot);
        return new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0",
            "--tile-materializer-result",           resultOverride ?? WriteMaterializerResult(),
            "--materialized-cells",                 WriteMaterializedCells(),
            "--material-palette",                   WriteMaterialPalette(),
            "--layer-stack",                        WriteLayerStack(),
            "--materialization-replay-log",         WriteMaterializationReplayLog(),
            "--materialization-ownership-summary",  WriteMaterializationOwnershipSummary(),
            "--materializer-forbidden-output-guard",WriteMaterializerForbiddenOutputGuard(),
            "--output-root",  outputRoot,
            "--output-json",  Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json"),
            "--output-md",    Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md"),
            "--output-csv",   Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv"),
            "--summary",      Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt"),
        };
    }

    private static (int ExitCode, string Stdout, string Stderr) Run(string[] args)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private string GetOutputRoot() => Path.Combine(_tempDir, "output.local");

    private string GetOutputJson() =>
        Path.Combine(GetOutputRoot(),
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json");

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_ExitCode0()
    {
        var (code, _, _) = Run(BuildArgs());
        Assert.Equal(0, code);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (code, _, stderr) = Run(new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0",
        });
        Assert.Equal(1, code);
        Assert.Contains("--tile-materializer-result", stderr);
    }

    [Fact]
    public void MissingTileMaterializerResult_ExitCode1()
    {
        var (code, _, _) = Run(BuildArgs(resultOverride: Path.Combine(_tempDir, "missing.json")));
        Assert.Equal(1, code);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        var badRoot = Path.GetTempPath();
        var (code, _, stderr) = Run(BuildArgs(outputRootOverride: badRoot));
        Assert.Equal(1, code);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void Writes4MainOutputs()
    {
        Run(BuildArgs());
        string root = GetOutputRoot();
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt")));
    }

    [Fact]
    public void Writes4ExtraOutputFiles()
    {
        Run(BuildArgs());
        string root = GetOutputRoot();
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_materializer_qa_overlay.png")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json")));
    }

    [Fact]
    public void VerdictComplete()
    {
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        string? verdict = doc.RootElement.GetProperty("verdict").GetString();
        Assert.Equal(
            "MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_COMPLETE",
            verdict);
    }

    [Fact]
    public void SandboxOnlyTrue()
    {
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("sandbox_only").GetBoolean());
    }

    [Fact]
    public void SandboxMaterializedSourceTrue()
    {
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("sandbox_materialized_source").GetBoolean());
    }

    [Fact]
    public void VisualQaOverlayWrittenTrue()
    {
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("visual_qa_overlay_written").GetBoolean());
    }

    [Fact]
    public void PzRuntimeMaterializedFalse()
    {
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("pz_runtime_materialized").GetBoolean());
    }

    [Fact]
    public void WriterStageTileMaterializerQaOverlayV0()
    {
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0",
            doc.RootElement.GetProperty("writer_stage").GetString());
    }

    [Fact]
    public void NoLotpack()
    {
        Run(BuildArgs());
        var files = Directory.GetFiles(GetOutputRoot(), "*.lotpack", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    [Fact]
    public void NoLua()
    {
        Run(BuildArgs());
        var files = Directory.GetFiles(GetOutputRoot(), "*.lua", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    [Fact]
    public void HelperScript_Exists()
        => Assert.True(File.Exists(HelperScript));

    [Fact]
    public void DoesNotContainCompileWorldgen()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("compile-worldgen", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DoesNotContainWorldGenOverrideLua()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua", content);
    }

    [Fact]
    public void DoesNotContainDotLotpack()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotpack", content);
    }

    [Fact]
    public void DoesNotContainDotLotheader()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotheader", content);
    }

    [Fact]
    public void ContainsCanonicalArgNames()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("--tile-materializer-result",          content);
        Assert.Contains("--materialized-cells",                content);
        Assert.Contains("--material-palette",                  content);
        Assert.Contains("--layer-stack",                       content);
        Assert.Contains("--materialization-replay-log",        content);
        Assert.Contains("--materialization-ownership-summary", content);
        Assert.Contains("--materializer-forbidden-output-guard", content);
    }

    [Fact]
    public void PointsAtDeadmtlAuthoringPaths()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0", content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0", content);
    }

    [Fact]
    public void OverlayPngIs1024x1024()
    {
        Run(BuildArgs());
        string pngPath = Path.Combine(GetOutputRoot(), "map_00.sandbox_writer_tile_materializer_qa_overlay.png");
        Assert.True(File.Exists(pngPath));
        using var bmp = new Bitmap(pngPath);
        Assert.Equal(1024, bmp.Width);
        Assert.Equal(1024, bmp.Height);
    }
}
