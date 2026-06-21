using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderLotpackTileWalkProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36e-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderLotpackTileWalkProcessTests() =>
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
    private string MinimalPath => Path.Combine(_tempDir, "min.lotpack");
    private string VisiblePath => Path.Combine(_tempDir, "vis.lotpack");
    private string ResultJson  => Path.Combine(OutputRoot, "deadmtl-lotpack-tile-walk-result.json");

    private void WriteFixtureFiles()
    {
        Directory.CreateDirectory(OutputRoot);
        // minimal: 2-byte header + "TILEDATA" + "PACKFOOT" + 10×8 null = 98 bytes
        var minimal = new List<byte> { 0xAA, 0xBB };
        minimal.AddRange(System.Text.Encoding.ASCII.GetBytes("TILEDATA"));
        minimal.AddRange(System.Text.Encoding.ASCII.GetBytes("PACKFOOT"));
        minimal.AddRange(Enumerable.Repeat((byte)0x00, 80));
        // visible: 2-byte header + "TILEDATA" + "PACKFOOT" + 7×8 variant = 74 bytes (SMALLER)
        var visible = new List<byte> { 0xAA, 0xBB };
        visible.AddRange(System.Text.Encoding.ASCII.GetBytes("TILEDATA"));
        visible.AddRange(System.Text.Encoding.ASCII.GetBytes("PACKFOOT"));
        for (int i = 0; i < 7; i++)
            visible.AddRange(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
        File.WriteAllBytes(MinimalPath, minimal.ToArray());
        File.WriteAllBytes(VisiblePath, visible.ToArray());
    }

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-lotpack-tile-walk" }
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
        "--minimal-lotpack", MinimalPath,
        "--visible-lotpack", VisiblePath,
        "--output-root",     OutputRoot,
    };

    // -----------------------------------------------------------------------
    // MAP36E_CLI_1: Valid args exits zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36E_Cli1_ValidArgsExitsZero()
    {
        WriteFixtureFiles();
        var (code, _, err) = RunCli(MakeBaseArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP36E_CLI_2: result.json written with correct format field
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36E_Cli2_ResultJsonWrittenWithCorrectFormat()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(ResultJson), "Result JSON must be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(ResultJson));
        Assert.Equal("MAP36E_LOTPACK_TILE_WALK_V1",
            doc.RootElement.GetProperty("format").GetString());
        Assert.Equal("TRUNCATION_OR_REPACK_DELTA_UNVERIFIED",
            doc.RootElement.GetProperty("size_delta_classification").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP36E_CLI_3: candidate-record-sizes.csv written with delta column
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36E_Cli3_CandidateRecordSizesCsvWritten()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-lotpack-candidate-record-sizes.csv");
        Assert.True(File.Exists(csvPath), "candidate-record-sizes.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("record_size", content);
        Assert.Contains("divides_delta_exactly", content);
        Assert.Contains("GUESS_NOT_VERIFIED", content);
    }

    // -----------------------------------------------------------------------
    // MAP36E_CLI_4: string-table.csv written with at least one entry
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36E_Cli4_StringTableCsvWrittenWithEntries()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string csvPath = Path.Combine(OutputRoot, "deadmtl-lotpack-string-table.csv");
        Assert.True(File.Exists(csvPath), "string-table.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("offset", content);
        Assert.Contains("TILEDATA", content);
    }

    // -----------------------------------------------------------------------
    // MAP36E_CLI_5: candidate-structure.md has TRUNCATION claim
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36E_Cli5_CandidateStructureMarkdownHasTruncationAndClaimBoundary()
    {
        WriteFixtureFiles();
        RunCli(MakeBaseArgs());
        string mdPath = Path.Combine(OutputRoot, "deadmtl-lotpack-candidate-structure.md");
        Assert.True(File.Exists(mdPath), "candidate-structure.md must be written");
        string content = File.ReadAllText(mdPath);
        Assert.Contains("TRUNCATION_OR_REPACK_DELTA_UNVERIFIED", content);
        Assert.Contains("runtime_binary_written", content);
        Assert.Contains("playable_export_claimed", content);
    }

    // -----------------------------------------------------------------------
    // MAP36E_CLI_6: Output root without .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36E_Cli6_OutputRootWithoutLocalExitsNonzero()
    {
        WriteFixtureFiles();
        string badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-map36e-nosandbox-" + Path.GetRandomFileName());
        Directory.CreateDirectory(badRoot);
        var (code, _, _) = RunCli("--minimal-lotpack", MinimalPath, "--visible-lotpack", VisiblePath, "--output-root", badRoot);
        Assert.True(code != 0, $"Expected nonzero exit for output root without .local, got {code}");
    }
}
