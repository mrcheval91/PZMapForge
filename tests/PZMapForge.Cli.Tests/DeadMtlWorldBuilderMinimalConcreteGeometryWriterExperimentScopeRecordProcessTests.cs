using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-scope-record-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteManifestJson()
    {
        var path = Path.Combine(_tempDir, "manifest.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-input-manifest.v1",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            input_artifact_count = 8,
            hashed_input_artifact_count = 8,
            writer_ready = false,
            runtime_valid = false,
            materialized = false,
            approved_for_writer_experiment = false,
            writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL",
            manifest_check_count = 17,
            passed_manifest_check_count = 17,
            failed_manifest_check_count = 0,
            is_valid = true,
            verdict = "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private string WriteManifestSummary()
    {
        var path = Path.Combine(_tempDir, "manifest.summary.txt");
        File.WriteAllText(path, "verdict: MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE\n");
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var dir = Path.Combine(_tempDir, ".local", subdir);
        Directory.CreateDirectory(dir);
        return (
            Path.Combine(dir, "scope.json"),
            Path.Combine(dir, "scope.md"),
            Path.Combine(dir, "scope.csv"),
            Path.Combine(dir, "scope.summary.txt")
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

    private const string Cmd = "deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record";

    private (int ExitCode, string Stdout, string Stderr) RunBuild(string subdir = "default")
    {
        var manifest = WriteManifestJson();
        var summary  = WriteManifestSummary();
        var (j, m, c, s) = MakeOutputPaths(subdir);
        return RunCli(Cmd,
            "--writer-input-manifest",         manifest,
            "--writer-input-manifest-summary", summary,
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
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var manifest = WriteManifestJson();
        var summary  = WriteManifestSummary();
        var outside  = Path.Combine(_tempDir, "not-local");
        Directory.CreateDirectory(outside);
        var (code, _, _) = RunCli(Cmd,
            "--writer-input-manifest",         manifest,
            "--writer-input-manifest-summary", summary,
            "--output-json", Path.Combine(outside, "out.json"),
            "--output-md",   Path.Combine(outside, "out.md"),
            "--output-csv",  Path.Combine(outside, "out.csv"),
            "--summary",     Path.Combine(outside, "out.txt"));
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingManifest()
    {
        var summary  = WriteManifestSummary();
        var (j, m, c, s) = MakeOutputPaths("no-manifest");
        var (code, _, _) = RunCli(Cmd,
            "--writer-input-manifest",         "__nonexistent__.json",
            "--writer-input-manifest-summary", summary,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingManifestSummary()
    {
        var manifest = WriteManifestJson();
        var (j, m, c, s) = MakeOutputPaths("no-summary");
        var (code, _, _) = RunCli(Cmd,
            "--writer-input-manifest",         manifest,
            "--writer-input-manifest-summary", "__nonexistent__.summary.txt",
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var manifest = WriteManifestJson();
        var summary  = WriteManifestSummary();
        var (j, m, c, s) = MakeOutputPaths("json");
        RunCli(Cmd,
            "--writer-input-manifest", manifest, "--writer-input-manifest-summary", summary,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(j), "output JSON not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var manifest = WriteManifestJson();
        var summary  = WriteManifestSummary();
        var (j, m, c, s) = MakeOutputPaths("md");
        RunCli(Cmd,
            "--writer-input-manifest", manifest, "--writer-input-manifest-summary", summary,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(m), "output MD not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var manifest = WriteManifestJson();
        var summary  = WriteManifestSummary();
        var (j, m, c, s) = MakeOutputPaths("csv");
        RunCli(Cmd,
            "--writer-input-manifest", manifest, "--writer-input-manifest-summary", summary,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(c), "output CSV not created");
    }

    [Fact]
    public void Cli_CreatesOutputSummary()
    {
        var manifest = WriteManifestJson();
        var summary  = WriteManifestSummary();
        var (j, m, c, s) = MakeOutputPaths("smry");
        RunCli(Cmd,
            "--writer-input-manifest", manifest, "--writer-input-manifest-summary", summary,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(s), "output summary not created");
    }

    // -----------------------------------------------------------------------
    // Stdout / output content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsTitle()
    {
        var (_, stdout, _) = RunBuild("title");
        Assert.Contains("MAP-26E WorldBuilder minimal concrete geometry writer experiment scope record",
            stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunBuild("verdict");
        Assert.Contains("MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE",
            stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsNotAuthorized()
    {
        var (_, stdout, _) = RunBuild("auth");
        Assert.Contains("NOT_AUTHORIZED", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsVerdict()
    {
        var manifest = WriteManifestJson();
        var summary  = WriteManifestSummary();
        var (j, m, c, s) = MakeOutputPaths("jsonverdict");
        RunCli(Cmd,
            "--writer-input-manifest", manifest, "--writer-input-manifest-summary", summary,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Contains("MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE",
            File.ReadAllText(j), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ApprovedForWriterExperiment_IsFalse()
    {
        var manifest = WriteManifestJson();
        var summary  = WriteManifestSummary();
        var (j, m, c, s) = MakeOutputPaths("jsonapproved");
        RunCli(Cmd,
            "--writer-input-manifest", manifest, "--writer-input-manifest-summary", summary,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Contains("\"approved_for_writer_experiment\": false",
            File.ReadAllText(j), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Forbidden files
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_DoesNotEmit_LotpackOrWorldGenOverrideFiles()
    {
        RunBuild("forbidden");
        var produced = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(produced, p => p.EndsWith(".lotpack", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(produced, p => p.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Unknown command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsScopeRecordCommand()
    {
        var (_, stdout, stderr) = RunCli("unknown-xyz-command");
        Assert.Contains("deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-experiment-scope-record",
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

    // -----------------------------------------------------------------------
    // Doc
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_Exists()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD.md");
        Assert.True(File.Exists(docPath), $"Doc not found: {docPath}");
    }

    [Fact]
    public void Doc_MentionsWriterGate()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD.md");
        var text = File.ReadAllText(docPath);
        Assert.True(
            text.Contains("LOCKED_PENDING_OPERATOR_APPROVAL", StringComparison.Ordinal) ||
            text.Contains("writer_experiment_gate_status", StringComparison.OrdinalIgnoreCase),
            "doc should mention the writer gate status");
    }

    [Fact]
    public void Doc_MentionsClaimBoundary()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD.md");
        var text = File.ReadAllText(docPath);
        Assert.True(
            text.Contains("writer_ready", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Claim Boundary", StringComparison.OrdinalIgnoreCase),
            "doc should mention writer_ready or claim boundary");
    }
}
