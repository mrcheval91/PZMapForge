using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private string Map33aSeedDir   => Path.Combine(_tempDir, "seed.local");
    private string Map35aSourceDir => Path.Combine(_tempDir, "source.local");
    private string OutputRoot      => Path.Combine(_tempDir, "output.local");
    private string OutputResult    => Path.Combine(OutputRoot, "result.json");

    private void WriteFixtures()
    {
        Directory.CreateDirectory(Map33aSeedDir);
        File.WriteAllBytes(Path.Combine(Map33aSeedDir, "35_27.lotheader"),    new byte[100]);
        File.WriteAllBytes(Path.Combine(Map33aSeedDir, "chunkdata_35_27.bin"), new byte[50]);
        File.WriteAllBytes(Path.Combine(Map33aSeedDir, "world_35_27.lotpack"), new byte[200]);

        Directory.CreateDirectory(Map35aSourceDir);
        File.WriteAllBytes(Path.Combine(Map35aSourceDir, "35_27.lotheader"),    Enumerable.Repeat((byte)0xFF, 200).ToArray());
        File.WriteAllBytes(Path.Combine(Map35aSourceDir, "chunkdata_35_27.bin"), Enumerable.Repeat((byte)0xAB, 150).ToArray());
        File.WriteAllBytes(Path.Combine(Map35aSourceDir, "world_35_27.lotpack"), new byte[180]);

        Directory.CreateDirectory(OutputRoot);
    }

    private string[] MakeBaseArgs() => new[]
    {
        "--map33a-seed-dir",   Map33aSeedDir,
        "--map35a-source-dir", Map35aSourceDir,
        "--output-root",       OutputRoot,
        "--output-result",     OutputResult,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-visible-cell-binary-anatomy-audit" }
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
    // MAP36A_CLI_1: Valid args exits zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Cli1_ValidArgsExitsZero()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeBaseArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP36A_CLI_2: Result JSON written with correct format and cell_coord fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Cli2_ResultJsonWrittenWithCorrectFormat()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(OutputResult), "Result JSON must be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        Assert.Equal("MAP36A_VISIBLE_CELL_BINARY_ANATOMY_AUDIT_V1",
            doc.RootElement.GetProperty("format").GetString());
        Assert.Equal("35_27", doc.RootElement.GetProperty("cell_coord").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP36A_CLI_3: File size fields recorded correctly in result JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Cli3_FileSizesInResultJson()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(OutputResult));
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        var loth  = doc.RootElement.GetProperty("lotheader_anatomy");
        var chunk = doc.RootElement.GetProperty("chunkdata_anatomy");
        Assert.Equal(100L,  loth.GetProperty("minimal_size").GetInt64());
        Assert.Equal(200L,  loth.GetProperty("visible_size").GetInt64());
        Assert.Equal(100L,  loth.GetProperty("size_delta").GetInt64());
        Assert.Equal(50L,   chunk.GetProperty("minimal_size").GetInt64());
        Assert.Equal(150L,  chunk.GetProperty("visible_size").GetInt64());
        Assert.Equal(100L,  chunk.GetProperty("size_delta").GetInt64());
    }

    // -----------------------------------------------------------------------
    // MAP36A_CLI_4: MAP-31B cross-reference fields present and correct
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Cli4_Map31bCrossRefFieldsCorrect()
    {
        WriteFixtures();
        string fakeJson = Path.Combine(_tempDir, "fake-ops.json");
        File.WriteAllText(fakeJson, @"{
  ""emitted_operation_count"": 5,
  ""emitted_total_planned_cell_count"": 5340,
  ""emitted_operation_records"": [
    { ""emits_binary_file"": false, ""runtime_consumable"": false, ""sandbox_only"": true }
  ]
}");
        RunCli(MakeBaseArgs().Concat(new[] { "--map31b-emitter-json", fakeJson }).ToArray());
        Assert.True(File.Exists(OutputResult));
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        var xref = doc.RootElement.GetProperty("map31b_cross_ref");
        Assert.True(xref.GetProperty("emitter_json_found").GetBoolean());
        Assert.Equal(5,    xref.GetProperty("emitted_operation_count").GetInt32());
        Assert.Equal(5340, xref.GetProperty("emitted_total_planned_cell_count").GetInt32());
        Assert.False(xref.GetProperty("emits_binary_file").GetBoolean());
        Assert.True(xref.GetProperty("sandbox_only").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // MAP36A_CLI_5: Missing map33a seed dir exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Cli5_MissingMap33aSeedDirExitsNonzero()
    {
        Directory.CreateDirectory(Map35aSourceDir);
        File.WriteAllBytes(Path.Combine(Map35aSourceDir, "35_27.lotheader"),    new byte[10]);
        File.WriteAllBytes(Path.Combine(Map35aSourceDir, "chunkdata_35_27.bin"), new byte[10]);
        File.WriteAllBytes(Path.Combine(Map35aSourceDir, "world_35_27.lotpack"), new byte[10]);
        Directory.CreateDirectory(OutputRoot);

        var (code, _, _) = RunCli(
            "--map33a-seed-dir",   Path.Combine(_tempDir, "no_such_seed.local"),
            "--map35a-source-dir", Map35aSourceDir,
            "--output-root",       OutputRoot);
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP36A_CLI_6: Claim boundary fields all false in result JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A_Cli6_ClaimBoundaryAllFalseInJson()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(OutputResult));
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("runtime_binary_written").GetBoolean());
        Assert.False(root.GetProperty("geometry_injected").GetBoolean());
        Assert.False(root.GetProperty("playable_export_claimed").GetBoolean());
        Assert.False(root.GetProperty("workshop_upload_performed").GetBoolean());
        Assert.False(root.GetProperty("steam_install_write").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CLI_1: Byte diff CSV written with correct header columns
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Cli1_ByteDiffCsvWritten()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs());
        string byteDiffCsv = Path.Combine(OutputRoot, "deadmtl-visible-cell-binary-anatomy-audit-byte-diff.csv");
        Assert.True(File.Exists(byteDiffCsv), "Byte diff CSV must be written");
        string content = File.ReadAllText(byteDiffCsv);
        Assert.Contains("file_name", content);
        Assert.Contains("size_delta", content);
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CLI_2: Extracted strings TXT files written for all three binary files
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Cli2_ExtractedStringsTxtFilesWritten()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs());
        string stringsDir = Path.Combine(OutputRoot, "strings");
        Assert.True(File.Exists(Path.Combine(stringsDir, "35_27.lotheader.minimal.txt")));
        Assert.True(File.Exists(Path.Combine(stringsDir, "35_27.lotheader.visible.txt")));
        Assert.True(File.Exists(Path.Combine(stringsDir, "chunkdata_35_27.bin.minimal.txt")));
        Assert.True(File.Exists(Path.Combine(stringsDir, "chunkdata_35_27.bin.visible.txt")));
        Assert.True(File.Exists(Path.Combine(stringsDir, "world_35_27.lotpack.minimal.txt")));
        Assert.True(File.Exists(Path.Combine(stringsDir, "world_35_27.lotpack.visible.txt")));
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CLI_3: Markdown proof packet written and contains claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Cli3_MarkdownProofPacketWrittenWithClaimBoundary()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs());
        string proofMd = Path.Combine(OutputRoot, "deadmtl-visible-cell-binary-anatomy-audit-proof.md");
        Assert.True(File.Exists(proofMd), "Proof markdown must be written");
        string content = File.ReadAllText(proofMd);
        Assert.Contains("runtime_binary_written", content);
        Assert.Contains("playable_export_claimed", content);
        Assert.Contains("false", content);
    }

    // -----------------------------------------------------------------------
    // MAP36A1_CLI_4: Source path containing "workshop donor" exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36A1_Cli4_SourcePathWithWorkshopDonorExitsNonzero()
    {
        Directory.CreateDirectory(Map33aSeedDir);
        File.WriteAllBytes(Path.Combine(Map33aSeedDir, "35_27.lotheader"),    new byte[10]);
        File.WriteAllBytes(Path.Combine(Map33aSeedDir, "chunkdata_35_27.bin"), new byte[10]);
        File.WriteAllBytes(Path.Combine(Map33aSeedDir, "world_35_27.lotpack"), new byte[10]);
        Directory.CreateDirectory(OutputRoot);

        string rejectedSource = Path.Combine(_tempDir, "workshop donor.local");
        Directory.CreateDirectory(rejectedSource);

        var (code, _, _) = RunCli(
            "--map33a-seed-dir",   Map33aSeedDir,
            "--map35a-source-dir", rejectedSource,
            "--output-root",       OutputRoot);
        Assert.Equal(1, code);
    }
}
