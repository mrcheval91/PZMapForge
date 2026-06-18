using System.Diagnostics;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditProcessTests
    : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map27h-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string GetMap27fRoot() => Path.Combine(_tempDir, "map27f.local", "map_00");
    private string GetMap27cRoot() => Path.Combine(_tempDir, "map27c.local", "map_00");
    private string GetMap27gRoot() => Path.Combine(_tempDir, "map27g.local", "map_00");
    private string GetOutputRoot() => Path.Combine(_tempDir, "output.local", "map_00");

    private string GetOutputJson() =>
        Path.Combine(GetOutputRoot(),
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json");

    private void WriteMap27fFiles()
    {
        var root = GetMap27fRoot();
        Directory.CreateDirectory(root);
        var f27 = new
        {
            format = "MAP-27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE",
            verdict = "MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_COMPLETE",
            acceptance_gate_status = "ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY",
            accepted_for_next_sandbox_experiment = true,
            accepted_for_runtime_writer = false,
            accepted_for_playable_export = false,
            sandbox_only = true,
            sandbox_materialized_source = true,
            visual_qa_overlay_written = true,
            pz_runtime_materialized = false,
            materialized_cell_count = 5340,
            rendered_cell_count = 5340,
            count_match_summary = "MATERIALIZED_CELL_COUNT(5340) == RENDERED_CELL_COUNT(5340): MATCH",
            building_wall_candidate_cell_count = 850,
            building_floor_candidate_cell_count = 2444,
            access_edge_cell_count = 148,
            lot_space_cell_count = 1898,
            component_residual_cell_count = 0,
            material_kind_count = 5,
            layer_kind_count = 5,
            overlay_png_width = 1024,
            overlay_png_height = 1024,
            is_valid = true,
            writer_ready = false,
            runtime_valid = false,
            materialized = false,
            runtime_proof_claimed = false,
            public_playable_packaging_claimed = false,
            check_count = 38,
            passed_check_count = 38,
            failed_check_count = 0,
            next_allowed_experiment_name = "MAP-27G_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK",
            next_allowed_experiment_status = "SANDBOX_ONLY_NOT_RUNTIME",
        };
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"),
            JsonSerializer.Serialize(f27));
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md"), "# MAP-27F");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv"),
            "check_order,check_id,check_status");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt"),
            "MAP-27F summary");
    }

    private void WriteMap27cFiles()
    {
        var root = GetMap27cRoot();
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"),
            "{\"format\":\"MAP-27C\",\"is_valid\":true,\"sandbox_only\":true,\"materialized_cell_count\":5340}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialized_cells.csv"),
            "cell_x,cell_y,material_kind\n0,0,WALL\n");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_material_palette.json"),
            "{\"materials\":[\"WALL\",\"FLOOR\",\"ACCESS\",\"LOT\",\"RESIDUAL\"]}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_layer_stack.json"),
            "{\"layers\":[\"STRUCTURE\",\"FLOOR\",\"EDGE\",\"SPACE\",\"RESIDUAL\"]}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialization_replay_log.json"),
            "{\"replay_log_entry_count\":5340}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialization_ownership_summary.json"),
            "{\"ownership_summary\":true}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json"),
            "{\"forbidden_output_guard\":true,\"all_clean\":true}");
    }

    private void WriteMap27gOutput()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();
        var map27gRoot = GetMap27gRoot();
        Directory.CreateDirectory(map27gRoot);
        var lockBuilder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var lockResult  = lockBuilder.Build(GetMap27fRoot(), GetMap27cRoot(), map27gRoot);
        File.WriteAllText(
            Path.Combine(map27gRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json"),
            lockBuilder.RenderJson(lockResult));
    }

    private string[] BuildArgs(
        string? replayLockRootOverride = null,
        string? outputRootOverride     = null)
    {
        string map27gRoot = replayLockRootOverride ?? GetMap27gRoot();
        string outputRoot = outputRootOverride     ?? GetOutputRoot();
        Directory.CreateDirectory(outputRoot);
        return new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit",
            "--replay-lock-root", map27gRoot,
            "--output-root",  outputRoot,
            "--output-json",  Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json"),
            "--output-md",    Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.md"),
            "--output-csv",   Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.csv"),
            "--summary",      Path.Combine(outputRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.summary.txt"),
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
        WriteMap27gOutput();
        var (code, _, _) = Run(BuildArgs());
        Assert.Equal(0, code);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (code, _, stderr) = Run(new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit",
        });
        Assert.Equal(1, code);
        Assert.Contains("--replay-lock-root", stderr);
    }

    [Fact]
    public void MissingReplayLockRoot_ExitCode1()
    {
        var badRoot = Path.Combine(_tempDir, "nonexistent.local", "map_00");
        var (code, _, _) = Run(BuildArgs(replayLockRootOverride: badRoot));
        Assert.Equal(1, code);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        WriteMap27gOutput();
        var badRoot = Path.GetTempPath();
        var (code, _, stderr) = Run(BuildArgs(outputRootOverride: badRoot));
        Assert.Equal(1, code);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void Writes4Outputs()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        string root = GetOutputRoot();
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.md")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.summary.txt")));
    }

    [Fact]
    public void VerdictComplete()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "MAP27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT_COMPLETE",
            doc.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public void AuditStatusVerified()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("VERIFIED_LOCKED_REPLAY_SOURCE_SET",
            doc.RootElement.GetProperty("audit_status").GetString());
    }

    [Fact]
    public void ReplayLockIdMatches()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("replay_lock_id_matches").GetBoolean());
    }

    [Fact]
    public void CheckCount45AllPass()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(45, doc.RootElement.GetProperty("check_count").GetInt32());
        Assert.Equal(45, doc.RootElement.GetProperty("passed_check_count").GetInt32());
        Assert.Equal(0,  doc.RootElement.GetProperty("failed_check_count").GetInt32());
    }

    [Fact]
    public void NoLotpack()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var files = Directory.GetFiles(GetOutputRoot(), "*.lotpack", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    [Fact]
    public void NoLua()
    {
        WriteMap27gOutput();
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
        Assert.Contains("--replay-lock-root", content);
        Assert.Contains("--output-root",      content);
        Assert.Contains("--output-json",      content);
        Assert.Contains("--output-md",        content);
        Assert.Contains("--output-csv",       content);
        Assert.Contains("--summary",          content);
    }

    [Fact]
    public void PointsAtCanonicalPaths()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-replay-lock",       content);
        Assert.Contains("worldbuilder-minimal-concrete-geometry-sandbox-writer-tile-materialization-locked-replay-audit", content);
    }

    [Fact]
    public void SandboxMaterializedSourceTrue()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("sandbox_materialized_source").GetBoolean());
    }

    [Fact]
    public void ForbiddenArtifactScanContainsPass()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Contains("PASS", doc.RootElement.GetProperty("forbidden_artifact_scan").GetString() ?? "");
    }

    [Fact]
    public void ClaimBoundaryAuditPresent()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        var boundary = doc.RootElement.GetProperty("claim_boundary_audit").GetString() ?? "";
        Assert.Contains("writer_ready=false", boundary);
        Assert.Contains("runtime_proof_claimed=false", boundary);
    }

    [Fact]
    public void NextForbiddenStepsCount11()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        var steps = doc.RootElement.GetProperty("next_forbidden_steps");
        Assert.Equal(11, steps.GetArrayLength());
    }

    [Fact]
    public void CheckIds_ContainMap27gPrefixedIds()
    {
        WriteMap27gOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        var ids = doc.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Select(c => c.GetProperty("check_id").GetString() ?? "")
            .ToList();
        Assert.Contains("MAP27G_REPLAY_LOCK_ROOT_EXISTS",                       ids);
        Assert.Contains("MAP27G_VERDICT_COMPLETE",                              ids);
        Assert.Contains("LOCKED_FILE_ROLES_EXACT_ORDER",                        ids);
        Assert.Contains("LOCKED_FILE_1_ACCEPTANCE_GATE_RESULT_JSON_EXISTS",     ids);
        Assert.Contains("SANDBOX_MATERIALIZED_SOURCE_TRUE",                     ids);
        Assert.Contains("MATERIALIZED_CELL_COUNT_5340",                         ids);
        Assert.Contains("POST_AUDIT_FORBIDDEN_SCAN_PASS",                       ids);
        Assert.Contains("FORBIDDEN_STEPS_LISTED",                               ids);
    }
}
