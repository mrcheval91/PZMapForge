using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderLotheaderHexDisassemblyProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36c-cli-test", Path.GetRandomFileName());

    public DeadMtlWorldBuilderLotheaderHexDisassemblyProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private string OutputRoot      => Path.Combine(_tempDir, "output.local");
    private string MinimalPath     => Path.Combine(_tempDir, "minimal.lotheader");
    private string VisiblePath     => Path.Combine(_tempDir, "visible.lotheader");
    private string ResultJson      => Path.Combine(OutputRoot, "deadmtl-lotheader-hex-disassembly-result.json");

    private void WriteFixtureFiles()
    {
        Directory.CreateDirectory(OutputRoot);
        // minimal: prefix(0x11×10) + body(0xAA×30) + suffix(0xFF×10) = 50 bytes
        var minimal = Enumerable.Repeat((byte)0x11, 10)
            .Concat(Enumerable.Repeat((byte)0xAA, 30))
            .Concat(Enumerable.Repeat((byte)0xFF, 10))
            .ToArray();
        // visible: prefix(0x11×10) + body(0xBB×50) + suffix(0xFF×10) = 70 bytes
        var visible = Enumerable.Repeat((byte)0x11, 10)
            .Concat(Enumerable.Repeat((byte)0xBB, 50))
            .Concat(Enumerable.Repeat((byte)0xFF, 10))
            .ToArray();
        File.WriteAllBytes(MinimalPath, minimal);
        File.WriteAllBytes(VisiblePath, visible);
    }

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-lotheader-hex-disassembly" }
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

    private string[] MakeBaseArgs() => new[]
    {
        "--minimal-lotheader", MinimalPath,
        "--visible-lotheader", VisiblePath,
        "--output-root",       OutputRoot,
    };

    // -----------------------------------------------------------------------
    // MAP36C_CLI_1: Valid args exits zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Cli1_ValidArgsExitsZero()
    {
        WriteFixtureFiles();
        var (code, _, err) = RunCli(MakeBaseArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP36C_CLI_2: result.json written with correct format field
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Cli2_ResultJsonWrittenWithCorrectFormat()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(ResultJson), "Result JSON must be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(ResultJson));
        Assert.Equal("MAP36C_LOTHEADER_HEX_DISASSEMBLY_V1",
            doc.RootElement.GetProperty("format").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP36C_CLI_3: lotheader-byte-regions.csv written with correct header
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Cli3_ByteRegionsCsvWritten()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-lotheader-hex-disassembly-byte-regions.csv");
        Assert.True(File.Exists(csvPath), "byte-regions.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("region_id", content);
        Assert.Contains("start_offset", content);
        Assert.Contains("interpretation", content);
    }

    // -----------------------------------------------------------------------
    // MAP36C_CLI_4: lotheader-diff-runs.csv written with correct header
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Cli4_DiffRunsCsvWritten()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-lotheader-hex-disassembly-diff-runs.csv");
        Assert.True(File.Exists(csvPath), "diff-runs.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("run_id", content);
        Assert.Contains("run_type", content);
        Assert.Contains("start_offset", content);
    }

    // -----------------------------------------------------------------------
    // MAP36C_CLI_5: candidate-structure.md written with claim boundary content
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Cli5_CandidateStructureMarkdownWrittenWithClaimBoundary()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string mdPath = Path.Combine(OutputRoot, "deadmtl-lotheader-hex-disassembly-candidate-structure.md");
        Assert.True(File.Exists(mdPath), "candidate-structure.md must be written");
        string content = File.ReadAllText(mdPath);
        Assert.Contains("runtime_binary_written", content);
        Assert.Contains("playable_export_claimed", content);
        Assert.Contains("false", content);
    }

    // -----------------------------------------------------------------------
    // MAP36C_CLI_6: Output root without .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Cli6_OutputRootWithoutLocalExitsNonzero()
    {
        WriteFixtureFiles();
        string badRoot = Path.Combine(_tempDir, "no-sandbox-dir");
        Directory.CreateDirectory(badRoot);
        var (code, _, _) = RunCli("--minimal-lotheader", MinimalPath, "--visible-lotheader", VisiblePath, "--output-root", badRoot);
        Assert.True(code != 0, $"Expected nonzero exit for output root without .local, got {code}");
    }
}
