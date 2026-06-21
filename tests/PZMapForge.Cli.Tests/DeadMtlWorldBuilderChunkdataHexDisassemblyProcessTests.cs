using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderChunkdataHexDisassemblyProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36d-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderChunkdataHexDisassemblyProcessTests() =>
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
    private string MinimalPath => Path.Combine(_tempDir, "minimal_chunkdata.bin");
    private string VisiblePath => Path.Combine(_tempDir, "visible_chunkdata.bin");
    private string ResultJson  => Path.Combine(OutputRoot, "deadmtl-chunkdata-hex-disassembly-result.json");

    private void WriteFixtureFiles()
    {
        Directory.CreateDirectory(OutputRoot);
        // minimal: 2-byte header + 8×10 records = 82 bytes
        var minimal = new byte[] { 0xAB, 0xCD }
            .Concat(Enumerable.Range(0, 10).SelectMany(i => Enumerable.Repeat((byte)(i + 1), 8)))
            .ToArray();
        // visible: 2-byte header + 8×20 records = 162 bytes
        var visible = new byte[] { 0xAB, 0xCD }
            .Concat(Enumerable.Range(0, 10).SelectMany(i => Enumerable.Repeat((byte)(i + 1), 8)))
            .Concat(Enumerable.Range(10, 10).SelectMany(i => Enumerable.Repeat((byte)(0x80 + i), 8)))
            .ToArray();
        File.WriteAllBytes(MinimalPath, minimal);
        File.WriteAllBytes(VisiblePath, visible);
    }

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-chunkdata-hex-disassembly" }
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
        "--minimal-chunkdata", MinimalPath,
        "--visible-chunkdata", VisiblePath,
        "--output-root",       OutputRoot,
    };

    // -----------------------------------------------------------------------
    // MAP36D_CLI_1: Valid args exits zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36D_Cli1_ValidArgsExitsZero()
    {
        WriteFixtureFiles();
        var (code, _, err) = RunCli(MakeBaseArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP36D_CLI_2: result.json written with correct format field
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36D_Cli2_ResultJsonWrittenWithCorrectFormat()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(ResultJson), "Result JSON must be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(ResultJson));
        Assert.Equal("MAP36D_CHUNKDATA_HEX_DISASSEMBLY_V1",
            doc.RootElement.GetProperty("format").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP36D_CLI_3: diff-runs.csv written with correct header
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36D_Cli3_DiffRunsCsvWritten()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-chunkdata-diff-runs.csv");
        Assert.True(File.Exists(csvPath), "diff-runs.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("run_id", content);
        Assert.Contains("run_type", content);
        Assert.Contains("start_offset", content);
    }

    // -----------------------------------------------------------------------
    // MAP36D_CLI_4: candidate-record-sizes.csv written with minus2 columns
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36D_Cli4_CandidateRecordSizesCsvWritten()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-chunkdata-candidate-record-sizes.csv");
        Assert.True(File.Exists(csvPath), "candidate-record-sizes.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("record_size", content);
        Assert.Contains("divides_visible_minus2_exactly", content);
        Assert.Contains("divides_minimal_minus2_exactly", content);
        Assert.Contains("GUESS_NOT_VERIFIED", content);
    }

    // -----------------------------------------------------------------------
    // MAP36D_CLI_5: candidate-structure.md written with claim boundary content
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36D_Cli5_CandidateStructureMarkdownWrittenWithClaimBoundary()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string mdPath = Path.Combine(OutputRoot, "deadmtl-chunkdata-candidate-structure.md");
        Assert.True(File.Exists(mdPath), "candidate-structure.md must be written");
        string content = File.ReadAllText(mdPath);
        Assert.Contains("runtime_binary_written", content);
        Assert.Contains("playable_export_claimed", content);
        Assert.Contains("false", content);
        Assert.Contains("GUESS_NOT_VERIFIED", content);
    }

    // -----------------------------------------------------------------------
    // MAP36D_CLI_6: Output root without .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36D_Cli6_OutputRootWithoutLocalExitsNonzero()
    {
        WriteFixtureFiles();
        // Use a path with no ".local" segment anywhere.
        string badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-map36d-nosandbox-" + Path.GetRandomFileName());
        Directory.CreateDirectory(badRoot);
        var (code, _, _) = RunCli("--minimal-chunkdata", MinimalPath, "--visible-chunkdata", VisiblePath, "--output-root", badRoot);
        Assert.True(code != 0, $"Expected nonzero exit for output root without .local, got {code}");
    }
}
