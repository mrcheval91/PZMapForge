using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-dry-run-design-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-design.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteScopeRecordJson()
    {
        var path = Path.Combine(_tempDir, "scope.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-experiment-scope-record.v1",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            is_valid = true,
            verdict = "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE",
            future_experiment_status = "NOT_AUTHORIZED",
            writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private string WriteManifestJson()
    {
        var path = Path.Combine(_tempDir, "manifest.json");
        var obj = new
        {
            format  = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-input-manifest.v1",
            map_id  = "map_00",
            verdict = "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private string WriteGeometryMvpJson()
    {
        var path = Path.Combine(_tempDir, "mvp.json");
        var obj = new
        {
            format  = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1",
            tile_id = "map_00",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var dir = Path.Combine(_tempDir, ".local", subdir);
        Directory.CreateDirectory(dir);
        return (
            Path.Combine(dir, "design.json"),
            Path.Combine(dir, "design.md"),
            Path.Combine(dir, "design.csv"),
            Path.Combine(dir, "design.summary.txt")
        );
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(CliProject);
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc     = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private const string Cmd = "deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-design";

    private (int ExitCode, string Stdout, string Stderr) RunBuild(string subdir = "default")
    {
        var scope    = WriteScopeRecordJson();
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        var (j, m, c, s) = MakeOutputPaths(subdir);
        return RunCli(Cmd,
            "--scope-record",          scope,
            "--writer-input-manifest", manifest,
            "--geometry-mvp",          mvp,
            "--output-json", j,
            "--output-md",   m,
            "--output-csv",  c,
            "--summary",     s);
    }

    // -----------------------------------------------------------------------
    // Exit codes
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithValidInputs()
    {
        var (code, stdout, stderr) = RunBuild("exit0");
        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingArgs()
    {
        var (code, _, _) = RunCli(Cmd);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingScopeRecord()
    {
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        var (j, m, c, s) = MakeOutputPaths("no-scope");
        var (code, _, _) = RunCli(Cmd,
            "--scope-record",          "__nonexistent_scope__.json",
            "--writer-input-manifest", manifest,
            "--geometry-mvp",          mvp,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingManifest()
    {
        var scope = WriteScopeRecordJson();
        var mvp   = WriteGeometryMvpJson();
        var (j, m, c, s) = MakeOutputPaths("no-manifest");
        var (code, _, _) = RunCli(Cmd,
            "--scope-record",          scope,
            "--writer-input-manifest", "__nonexistent_manifest__.json",
            "--geometry-mvp",          mvp,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingGeometry()
    {
        var scope    = WriteScopeRecordJson();
        var manifest = WriteManifestJson();
        var (j, m, c, s) = MakeOutputPaths("no-geometry");
        var (code, _, _) = RunCli(Cmd,
            "--scope-record",          scope,
            "--writer-input-manifest", manifest,
            "--geometry-mvp",          "__nonexistent_mvp__.json",
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var scope    = WriteScopeRecordJson();
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        var outside  = Path.Combine(_tempDir, "not-local");
        Directory.CreateDirectory(outside);
        var (code, _, _) = RunCli(Cmd,
            "--scope-record",          scope,
            "--writer-input-manifest", manifest,
            "--geometry-mvp",          mvp,
            "--output-json", Path.Combine(outside, "out.json"),
            "--output-md",   Path.Combine(outside, "out.md"),
            "--output-csv",  Path.Combine(outside, "out.csv"),
            "--summary",     Path.Combine(outside, "out.txt"));
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesAllFourOutputs()
    {
        var scope    = WriteScopeRecordJson();
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        var (j, m, c, s) = MakeOutputPaths("all4");
        RunCli(Cmd,
            "--scope-record", scope, "--writer-input-manifest", manifest, "--geometry-mvp", mvp,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(j), "output JSON not created");
        Assert.True(File.Exists(m), "output MD not created");
        Assert.True(File.Exists(c), "output CSV not created");
        Assert.True(File.Exists(s), "output summary not created");
    }

    // -----------------------------------------------------------------------
    // Stdout / output content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsTitle()
    {
        var (_, stdout, _) = RunBuild("title");
        Assert.Contains("MAP-26F WorldBuilder minimal concrete geometry writer dry-run design",
            stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunBuild("verdict");
        Assert.Contains("MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE",
            stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsDesignOnlyNotAuthorized()
    {
        var (_, stdout, _) = RunBuild("auth");
        Assert.Contains("DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsDryRunOnlyTrue()
    {
        var scope    = WriteScopeRecordJson();
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        var (j, m, c, s) = MakeOutputPaths("jsondryr");
        RunCli(Cmd,
            "--scope-record", scope, "--writer-input-manifest", manifest, "--geometry-mvp", mvp,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Contains("\"dry_run_only\": true", File.ReadAllText(j), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Forbidden files
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_DoesNotEmit_ForbiddenFiles()
    {
        RunBuild("forbidden");
        var produced = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(produced, p => p.EndsWith(".lotpack",            StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(produced, p => p.EndsWith(".lotheader",          StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(produced, p => p.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Unknown command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsDryRunDesignCommand()
    {
        var (_, stdout, stderr) = RunCli("unknown-xyz-command");
        Assert.Contains("deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-design",
            stdout + stderr, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Helper script
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_Exists()
    {
        Assert.True(File.Exists(HelperScript), $"Helper script not found: {HelperScript}");
    }

    [Fact]
    public void HelperScript_DoesNotContain_CompileWorldgen()
    {
        Assert.DoesNotContain("compile-worldgen", File.ReadAllText(HelperScript), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_WorldGenOverrideLua()
    {
        Assert.DoesNotContain("WorldGenOverride.lua", File.ReadAllText(HelperScript), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotpack()
    {
        Assert.DoesNotContain(".lotpack", File.ReadAllText(HelperScript), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotheader()
    {
        Assert.DoesNotContain(".lotheader", File.ReadAllText(HelperScript), StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Doc
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_Exists()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN.md");
        Assert.True(File.Exists(docPath), $"Doc not found: {docPath}");
    }
}
