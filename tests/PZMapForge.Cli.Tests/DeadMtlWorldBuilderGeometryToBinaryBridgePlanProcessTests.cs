using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderGeometryToBinaryBridgePlanProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36b-cli-test", Path.GetRandomFileName());

    public DeadMtlWorldBuilderGeometryToBinaryBridgePlanProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private string OutputRoot  => Path.Combine(_tempDir, "output.local");
    private string ResultJson  => Path.Combine(OutputRoot, "deadmtl-geometry-to-binary-bridge-plan-result.json");

    private string[] MakeBaseArgs() => new[]
    {
        "--output-root", OutputRoot,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-geometry-to-binary-bridge-plan" }
            .Concat(extraArgs)
            .ToArray();

        var psi = new ProcessStartInfo("dotnet",
            $"run --project \"{CliProject}\" -- " +
            string.Join(" ", allArgs.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)))
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            WorkingDirectory       = RepoRoot,
        };

        using var proc = Process.Start(psi)!;
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(180_000);
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // MAP36B_CLI_1: Valid args exits zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Cli1_ValidArgsExitsZero()
    {
        Directory.CreateDirectory(OutputRoot);
        var (code, _, err) = RunCli(MakeBaseArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP36B_CLI_2: result.json written with correct format field
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Cli2_ResultJsonWrittenWithCorrectFormat()
    {
        Directory.CreateDirectory(OutputRoot);
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(ResultJson), "Result JSON must be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(ResultJson));
        Assert.Equal("MAP36B_GEOMETRY_TO_BINARY_BRIDGE_PLAN_V1",
            doc.RootElement.GetProperty("format").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP36B_CLI_3: bridge-plan.md written with claim boundary content
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Cli3_BridgePlanMarkdownWrittenWithClaimBoundary()
    {
        Directory.CreateDirectory(OutputRoot);
        RunCli(MakeBaseArgs());
        string mdPath = Path.Combine(OutputRoot, "deadmtl-geometry-to-binary-bridge-plan.md");
        Assert.True(File.Exists(mdPath), "bridge-plan.md must be written");
        string content = File.ReadAllText(mdPath);
        Assert.Contains("runtime_binary_written", content);
        Assert.Contains("playable_export_claimed", content);
        Assert.Contains("false", content);
    }

    // -----------------------------------------------------------------------
    // MAP36B_CLI_4: required-unknowns.csv written with correct header
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Cli4_RequiredUnknownsCsvWritten()
    {
        Directory.CreateDirectory(OutputRoot);
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-geometry-to-binary-bridge-plan-required-unknowns.csv");
        Assert.True(File.Exists(csvPath), "required-unknowns.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("id", content);
        Assert.Contains("description", content);
        Assert.Contains("severity", content);
    }

    // -----------------------------------------------------------------------
    // MAP36B_CLI_5: candidate-next-experiments.csv written
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Cli5_CandidateExperimentsCsvWritten()
    {
        Directory.CreateDirectory(OutputRoot);
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-geometry-to-binary-bridge-plan-candidate-next-experiments.csv");
        Assert.True(File.Exists(csvPath), "candidate-next-experiments.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("id", content);
        Assert.Contains("read_only", content);
    }

    // -----------------------------------------------------------------------
    // MAP36B_CLI_6: Output root without .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Cli6_OutputRootWithoutLocalExitsNonzero()
    {
        string badRoot = Path.Combine(_tempDir, "no-sandbox-dir");
        Directory.CreateDirectory(badRoot);
        var (code, _, _) = RunCli("--output-root", badRoot);
        Assert.True(code != 0, $"Expected nonzero exit for output root without .local, got {code}");
    }
}
