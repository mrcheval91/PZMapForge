using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderBucketToTilesheetCrossReferenceProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36f-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderBucketToTilesheetCrossReferenceProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private string OutputRoot => Path.Combine(_tempDir, "output.local");
    private string ResultJson => Path.Combine(OutputRoot, "deadmtl-bucket-to-tilesheet-cross-reference-result.json");

    private string MakePzFixture()
    {
        string tilesDir = Path.Combine(_tempDir, "pz-fixture", "media", "tiles");
        Directory.CreateDirectory(tilesDir);
        File.WriteAllText(Path.Combine(tilesDir, "location_walls_01.txt"),
            "wall\nlocation_walls_exterior_01\nwalls_concrete_01");
        File.WriteAllText(Path.Combine(tilesDir, "floors_exterior_01.txt"),
            "floor\nfloors_exterior_concrete_01\npavement");
        File.WriteAllText(Path.Combine(tilesDir, "vegetation_01.txt"),
            "grass\ntree\nvegetation_plants_01");
        File.WriteAllText(Path.Combine(tilesDir, "roads_01.txt"),
            "road\nstreet\nasphalt");
        return Path.Combine(_tempDir, "pz-fixture");
    }

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-bucket-to-tilesheet-cross-reference" }
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
    // MAP36F_CLI_1: Valid args exits zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36F_Cli1_ValidArgsExitsZero()
    {
        Directory.CreateDirectory(OutputRoot);
        var (code, _, err) = RunCli("--output-root", OutputRoot);
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP36F_CLI_2: result.json written with correct format field
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36F_Cli2_ResultJsonWrittenWithCorrectFormat()
    {
        Directory.CreateDirectory(OutputRoot);
        RunCli("--output-root", OutputRoot);
        Assert.True(File.Exists(ResultJson), "Result JSON must be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(ResultJson));
        Assert.Equal("MAP36F_BUCKET_TO_TILESHEET_CROSS_REFERENCE_V1",
            doc.RootElement.GetProperty("format").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP36F_CLI_3: bucket-intent-inventory.csv written with entries
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36F_Cli3_BucketIntentCsvWritten()
    {
        Directory.CreateDirectory(OutputRoot);
        RunCli("--output-root", OutputRoot);
        string csvPath = Path.Combine(OutputRoot, "deadmtl-bucket-intent-inventory.csv");
        Assert.True(File.Exists(csvPath), "bucket-intent-inventory.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("bucket", content);
        Assert.Contains("WALL", content);
        Assert.Contains("FLOOR", content);
        Assert.Contains("ROAD", content);
    }

    // -----------------------------------------------------------------------
    // MAP36F_CLI_4: candidate mapping CSV written with correct columns
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36F_Cli4_CandidateMappingCsvWritten()
    {
        Directory.CreateDirectory(OutputRoot);
        string pz = MakePzFixture();
        RunCli("--output-root", OutputRoot, "--pz-install-root", pz);
        string csvPath = Path.Combine(OutputRoot, "deadmtl-bucket-to-tilesheet-candidates.csv");
        Assert.True(File.Exists(csvPath), "bucket-to-tilesheet-candidates.csv must be written");
        string content = File.ReadAllText(csvPath);
        Assert.Contains("bucket", content);
        Assert.Contains("candidate_tilesheet_or_tile_name", content);
        Assert.Contains("confidence_label", content);
        Assert.Contains("verified_runtime_tile_id", content);
        Assert.Contains("False", content);
    }

    // -----------------------------------------------------------------------
    // MAP36F_CLI_5: markdown has CANDIDATE/UNVERIFIED wording and claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36F_Cli5_MarkdownHasClaimBoundaryAndCandidateWording()
    {
        Directory.CreateDirectory(OutputRoot);
        RunCli("--output-root", OutputRoot);
        string mdPath = Path.Combine(OutputRoot, "deadmtl-bucket-to-tilesheet-cross-reference.md");
        Assert.True(File.Exists(mdPath), "cross-reference.md must be written");
        string content = File.ReadAllText(mdPath);
        Assert.Contains("CANDIDATE", content);
        Assert.Contains("UNVERIFIED", content);
        Assert.Contains("runtime_binary_written", content);
        Assert.Contains("verified_runtime_tile_id", content);
        Assert.Contains("read_only_probe", content);
    }

    // -----------------------------------------------------------------------
    // MAP36F_CLI_6: Output root without .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36F_Cli6_OutputRootWithoutLocalExitsNonzero()
    {
        string badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-map36f-nosandbox-" + Path.GetRandomFileName());
        Directory.CreateDirectory(badRoot);
        var (code, _, _) = RunCli("--output-root", badRoot);
        Assert.True(code != 0, $"Expected nonzero exit for output root without .local, got {code}");
    }
}
