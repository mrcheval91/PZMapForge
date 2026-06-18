using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateProcessTests
    : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map27f-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string GetMap27eRoot() => Path.Combine(_tempDir, "map27e.local", "map_00");
    private string GetOutputRoot() => Path.Combine(_tempDir, "output.local", "map_00");

    private string GetOutputJson() =>
        Path.Combine(GetOutputRoot(),
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json");

    private void WriteMap27eFiles()
    {
        var root = GetMap27eRoot();
        Directory.CreateDirectory(root);
        var e27Result = new
        {
            format                            = "MAP-27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET",
            map_id                            = "map_00",
            target_component_id               = "map_00_component_0001",
            verdict                           = "MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_COMPLETE",
            is_valid                          = true,
            review_stage                      = "SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET",
            sandbox_only                      = true,
            sandbox_materialized_source       = true,
            visual_qa_overlay_written         = true,
            pz_runtime_materialized           = false,
            reviewed_file_count               = 18,
            check_count                       = 40,
            passed_check_count                = 40,
            failed_check_count                = 0,
            count_match_summary               = "MATERIALIZED_CELL_COUNT(5340) == RENDERED_CELL_COUNT(5340): MATCH",
            overlay_png_width                 = 1024,
            overlay_png_height                = 1024,
            materialization_counts            = new
            {
                materialized_cell_count             = 5340,
                building_wall_candidate_cell_count  = 850,
                building_floor_candidate_cell_count = 2444,
                access_edge_cell_count              = 148,
                lot_space_cell_count                = 1898,
                component_residual_cell_count       = 0,
                material_kind_count                 = 5,
                layer_kind_count                    = 5,
            },
            overlay_counts                    = new
            {
                rendered_cell_count = 5340,
                overlay_width       = 1024,
                overlay_height      = 1024,
                scale               = 4,
            },
            writer_ready                      = false,
            runtime_valid                     = false,
            materialized                      = false,
            runtime_proof_claimed             = false,
            public_playable_packaging_claimed = false,
        };
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json"),
            JsonSerializer.Serialize(e27Result));
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.md"),
            "# MAP-27E");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.csv"),
            "check_order,check_id,check_status");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.summary.txt"),
            "MAP-27E summary");
    }

    private string[] BuildArgs(
        string? map27eRootOverride = null,
        string? outputRootOverride = null)
    {
        string map27eRoot = map27eRootOverride ?? GetMap27eRoot();
        string outputRoot = outputRootOverride ?? GetOutputRoot();
        Directory.CreateDirectory(outputRoot);
        return new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate",
            "--qa-review-packet-root", map27eRoot,
            "--output-root",  outputRoot,
            "--output-json",  Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"),
            "--output-md",    Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md"),
            "--output-csv",   Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv"),
            "--summary",      Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt"),
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
        WriteMap27eFiles();
        var (code, _, _) = Run(BuildArgs());
        Assert.Equal(0, code);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (code, _, stderr) = Run(new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate",
        });
        Assert.Equal(1, code);
        Assert.Contains("--qa-review-packet-root", stderr);
    }

    [Fact]
    public void MissingMap27eRoot_ExitCode1()
    {
        var badRoot = Path.Combine(_tempDir, "nonexistent.local", "map_00");
        var (code, _, _) = Run(BuildArgs(map27eRootOverride: badRoot));
        Assert.Equal(1, code);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        WriteMap27eFiles();
        var badRoot = Path.GetTempPath();
        var (code, _, stderr) = Run(BuildArgs(outputRootOverride: badRoot));
        Assert.Equal(1, code);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void Writes4Outputs()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        string root = GetOutputRoot();
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt")));
    }

    [Fact]
    public void VerdictComplete()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_COMPLETE",
            doc.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public void SandboxOnlyTrue()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("sandbox_only").GetBoolean());
    }

    [Fact]
    public void PzRuntimeMaterializedFalse()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("pz_runtime_materialized").GetBoolean());
    }

    [Fact]
    public void AcceptedForNextSandboxExperimentTrue()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("accepted_for_next_sandbox_experiment").GetBoolean());
    }

    [Fact]
    public void AcceptedForRuntimeWriterFalse()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("accepted_for_runtime_writer").GetBoolean());
    }

    [Fact]
    public void AcceptedForPlayableExportFalse()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("accepted_for_playable_export").GetBoolean());
    }

    [Fact]
    public void CheckCount38AllPass()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(38, doc.RootElement.GetProperty("check_count").GetInt32());
        Assert.Equal(38, doc.RootElement.GetProperty("passed_check_count").GetInt32());
        Assert.Equal(0,  doc.RootElement.GetProperty("failed_check_count").GetInt32());
    }

    [Fact]
    public void NoLotpack()
    {
        WriteMap27eFiles();
        Run(BuildArgs());
        var files = Directory.GetFiles(GetOutputRoot(), "*.lotpack", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    [Fact]
    public void NoLua()
    {
        WriteMap27eFiles();
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
        Assert.Contains("--qa-review-packet-root", content);
        Assert.Contains("--output-root",           content);
        Assert.Contains("--output-json",           content);
        Assert.Contains("--output-md",             content);
        Assert.Contains("--output-csv",            content);
        Assert.Contains("--summary",               content);
    }

    [Fact]
    public void PointsAtCanonicalPaths()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-qa-review-packet", content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-acceptance-gate",  content);
    }
}
