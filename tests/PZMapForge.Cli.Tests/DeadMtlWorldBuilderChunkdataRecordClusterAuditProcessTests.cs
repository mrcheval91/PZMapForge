using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderChunkdataRecordClusterAuditProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map37a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderChunkdataRecordClusterAuditProcessTests() =>
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
    private string ResultJson  => Path.Combine(OutputRoot, "deadmtl-chunkdata-record-cluster-audit-result.json");

    private void WriteFixtureFiles()
    {
        Directory.CreateDirectory(OutputRoot);
        // minimal: 2-byte header + 128 * 8-byte records = 1026 bytes
        var minimal = new byte[2 + 128 * 8];
        minimal[0] = 0xAB; minimal[1] = 0xCD;
        for (int i = 0; i < 128; i++)
            for (int j = 0; j < 8; j++)
                minimal[2 + i * 8 + j] = (byte)((i % 200) + 1);
        // visible: 2-byte header + 2272 * 8-byte records = 18178 bytes
        var visible = new byte[2 + 2272 * 8];
        visible[0] = 0xAB; visible[1] = 0xCD;
        for (int i = 0; i < 128; i++)
            for (int j = 0; j < 8; j++)
                visible[2 + i * 8 + j] = (byte)((i % 200) + 1);
        for (int i = 128; i < 2272; i++)
            for (int j = 0; j < 8; j++)
                visible[2 + i * 8 + j] = (byte)(0x40 + (i % 60));
        File.WriteAllBytes(MinimalPath, minimal);
        File.WriteAllBytes(VisiblePath, visible);
    }

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-chunkdata-record-cluster-audit" }
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

    // MAP37A_CLI_1: Valid args exit zero
    [Fact]
    public void Map37A_Cli1_ValidArgsExitsZero()
    {
        WriteFixtureFiles();
        var (code, _, err) = RunCli(MakeBaseArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // MAP37A_CLI_2: result JSON format is MAP37A_CHUNKDATA_RECORD_CLUSTER_AUDIT_V1
    [Fact]
    public void Map37A_Cli2_ResultJsonWrittenWithCorrectFormat()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(ResultJson), "Result JSON must be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(ResultJson));
        Assert.Equal("MAP37A_CHUNKDATA_RECORD_CLUSTER_AUDIT_V1",
            doc.RootElement.GetProperty("format").GetString());
    }

    // MAP37A_CLI_3: header candidates CSV exists with correct columns
    [Fact]
    public void Map37A_Cli3_HeaderCandidatesCsvWritten()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-chunkdata-header-candidates.csv");
        Assert.True(File.Exists(csvPath), "header-candidates.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("header_size", content);
        Assert.Contains("record_width", content);
        Assert.Contains("exact_fit_score", content);
        Assert.Contains("GUESS_NOT_VERIFIED", content);
    }

    // MAP37A_CLI_4: column statistics CSV exists with correct columns
    [Fact]
    public void Map37A_Cli4_ColumnStatisticsCsvWritten()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-chunkdata-column-statistics.csv");
        Assert.True(File.Exists(csvPath), "column-statistics.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("column_index", content);
        Assert.Contains("distinct_count", content);
        Assert.Contains("most_common_byte", content);
    }

    // MAP37A_CLI_5: changed record windows CSV exists and includes VISIBLE_EXTRA
    [Fact]
    public void Map37A_Cli5_ChangedRecordWindowsCsvIncludesVisibleExtra()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-chunkdata-changed-record-windows.csv");
        Assert.True(File.Exists(csvPath), "changed-record-windows.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("window_type", content);
        Assert.Contains("VISIBLE_EXTRA", content);
        Assert.Contains("CANDIDATE_RECORD_WINDOW_UNVERIFIED", content);
    }

    // MAP37A_CLI_6: output-root without .local exits nonzero
    [Fact]
    public void Map37A_Cli6_OutputRootWithoutLocalExitsNonzero()
    {
        WriteFixtureFiles();
        string badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-map37a-nosandbox-" + Path.GetRandomFileName());
        Directory.CreateDirectory(badRoot);
        var (code, _, _) = RunCli("--minimal-chunkdata", MinimalPath, "--visible-chunkdata", VisiblePath, "--output-root", badRoot);
        Assert.True(code != 0, $"Expected nonzero exit for output root without .local, got {code}");
    }
}
