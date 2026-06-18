using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Xunit;

#pragma warning disable CA1416

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketProcessTests
    : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map27e-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string GetMap27cRoot() => Path.Combine(_tempDir, "map27c.local", "map_00");
    private string GetMap27dRoot() => Path.Combine(_tempDir, "map27d.local", "map_00");
    private string GetOutputRoot() => Path.Combine(_tempDir, "output.local", "map_00");

    private string GetOutputJson() =>
        Path.Combine(GetOutputRoot(),
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json");

    private void WriteMap27cFiles()
    {
        var root = GetMap27cRoot();
        Directory.CreateDirectory(root);
        var result = new
        {
            format                            = "MAP-27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            map_id                            = "map_00",
            target_component_id               = "map_00_component_0001",
            verdict                           = "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            is_valid                          = true,
            sandbox_only                      = true,
            sandbox_materialized              = true,
            pz_runtime_materialized           = false,
            materialized_cell_count           = 5340,
            writer_ready                      = false,
            runtime_valid                     = false,
            materialized                      = false,
            runtime_proof_claimed             = false,
            public_playable_packaging_claimed = false,
        };
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"),
            JsonSerializer.Serialize(result));
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md"), "# MAP-27C");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv"), "check_order,check_id");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt"), "summary");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialized_cells.csv"), "x,y,material_kind,layer_kind");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_material_palette.json"),
            JsonSerializer.Serialize(new { material_kind_count = 5 }));
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_layer_stack.json"),
            JsonSerializer.Serialize(new { layer_kind_count = 5 }));
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialization_replay_log.json"),
            JsonSerializer.Serialize(new { replay_entry_count = 0 }));
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialization_ownership_summary.json"),
            JsonSerializer.Serialize(new { owner_kind_count = 4 }));
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json"),
            JsonSerializer.Serialize(new { guard_count = 0, guards = Array.Empty<object>() }));
    }

    private void WriteMap27dFiles()
    {
        var root = GetMap27dRoot();
        Directory.CreateDirectory(root);
        var d27Result = new
        {
            format                              = "MAP-27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0",
            map_id                              = "map_00",
            target_component_id                 = "map_00_component_0001",
            verdict                             = "MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_COMPLETE",
            is_valid                            = true,
            sandbox_only                        = true,
            visual_qa_overlay_written           = true,
            pz_runtime_materialized             = false,
            input_materialized_cell_count       = 5340,
            rendered_cell_count                 = 5340,
            building_wall_candidate_cell_count  = 850,
            building_floor_candidate_cell_count = 2444,
            access_edge_cell_count              = 148,
            lot_space_cell_count                = 1898,
            component_residual_cell_count       = 0,
            material_kind_count                 = 5,
            layer_kind_count                    = 5,
            overlay_width                       = 1024,
            overlay_height                      = 1024,
            scale                               = 4,
            writer_ready                        = false,
            runtime_valid                       = false,
            materialized                        = false,
            runtime_proof_claimed               = false,
            public_playable_packaging_claimed   = false,
        };
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json"),
            JsonSerializer.Serialize(d27Result));
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md"), "# MAP-27D");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv"), "check_order,check_id");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt"), "summary");

        using (var bmp = new Bitmap(1024, 1024))
            bmp.Save(Path.Combine(root, "map_00.sandbox_writer_tile_materializer_qa_overlay.png"), ImageFormat.Png);

        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json"),
            JsonSerializer.Serialize(new { format = "MAP-27D_LEGEND" }));
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv"), "material_kind,cell_count");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json"),
            JsonSerializer.Serialize(new { format = "MAP-27D_GUARD" }));
    }

    private string[] BuildArgs(
        string? map27cRootOverride = null,
        string? map27dRootOverride = null,
        string? outputRootOverride = null)
    {
        string map27cRoot = map27cRootOverride ?? GetMap27cRoot();
        string map27dRoot = map27dRootOverride ?? GetMap27dRoot();
        string outputRoot = outputRootOverride ?? GetOutputRoot();
        Directory.CreateDirectory(outputRoot);
        return new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet",
            "--tile-materializer-root",           map27cRoot,
            "--tile-materializer-qa-overlay-root", map27dRoot,
            "--output-root",  outputRoot,
            "--output-json",  Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json"),
            "--output-md",    Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.md"),
            "--output-csv",   Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.csv"),
            "--summary",      Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.summary.txt"),
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

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_ExitCode0()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        var (code, _, _) = Run(BuildArgs());
        Assert.Equal(0, code);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (code, _, stderr) = Run(new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet",
        });
        Assert.Equal(1, code);
        Assert.Contains("--tile-materializer-root", stderr);
    }

    [Fact]
    public void MissingMap27cRoot_ExitCode1()
    {
        WriteMap27dFiles();
        var badRoot = Path.Combine(_tempDir, "nonexistent.local", "map_00");
        var (code, _, _) = Run(BuildArgs(map27cRootOverride: badRoot));
        Assert.Equal(1, code);
    }

    [Fact]
    public void MissingMap27dRoot_ExitCode1()
    {
        WriteMap27cFiles();
        var badRoot = Path.Combine(_tempDir, "nonexistent.local", "map_00");
        var (code, _, _) = Run(BuildArgs(map27dRootOverride: badRoot));
        Assert.Equal(1, code);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        var badRoot = Path.GetTempPath();
        var (code, _, stderr) = Run(BuildArgs(outputRootOverride: badRoot));
        Assert.Equal(1, code);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void Writes4Outputs()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        string root = GetOutputRoot();
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.md")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.summary.txt")));
    }

    [Fact]
    public void VerdictComplete()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_COMPLETE",
            doc.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public void SandboxOnlyTrue()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("sandbox_only").GetBoolean());
    }

    [Fact]
    public void SandboxMaterializedSourceTrue()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("sandbox_materialized_source").GetBoolean());
    }

    [Fact]
    public void VisualQaOverlayWrittenTrue()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("visual_qa_overlay_written").GetBoolean());
    }

    [Fact]
    public void PzRuntimeMaterializedFalse()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("pz_runtime_materialized").GetBoolean());
    }

    [Fact]
    public void ReviewStageCorrect()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET",
            doc.RootElement.GetProperty("review_stage").GetString());
    }

    [Fact]
    public void ReviewedFileCount18()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(18, doc.RootElement.GetProperty("reviewed_file_count").GetInt32());
    }

    [Fact]
    public void CheckCount40AllPass()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(40, doc.RootElement.GetProperty("check_count").GetInt32());
        Assert.Equal(40, doc.RootElement.GetProperty("passed_check_count").GetInt32());
        Assert.Equal(0,  doc.RootElement.GetProperty("failed_check_count").GetInt32());
    }

    [Fact]
    public void NoLotpack()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        Run(BuildArgs());
        var files = Directory.GetFiles(GetOutputRoot(), "*.lotpack", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    [Fact]
    public void NoLua()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
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
        Assert.Contains("--tile-materializer-root",            content);
        Assert.Contains("--tile-materializer-qa-overlay-root", content);
        Assert.Contains("--output-root",                       content);
        Assert.Contains("--output-json",                       content);
        Assert.Contains("--output-md",                         content);
        Assert.Contains("--output-csv",                        content);
        Assert.Contains("--summary",                           content);
    }

    [Fact]
    public void PointsAtCanonicalPaths()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-v0",           content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materializer-qa-overlay-v0", content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet", content);
    }
}
