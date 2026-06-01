using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level integration tests for the PZMapForge CLI.
/// Each test invokes the compiled CLI via `dotnet run` and verifies exit code
/// and artifact presence. Images are created programmatically in a temp
/// directory; no .local state is required before tests run.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CliProcessTests : IDisposable
{
    // Temp root — all test outputs go under _tempDir/.local/mapforge/
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-cli-tests", Path.GetRandomFileName());

    // .local/mapforge output path (passes the CLI path guard)
    private string OutputDir => Path.Combine(_tempDir, ".local", "mapforge");

    public CliProcessTests()
    {
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Paths
    // -----------------------------------------------------------------------

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private static string ParsedCellFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell", "valid.json");

    // -----------------------------------------------------------------------
    // Image helpers
    // -----------------------------------------------------------------------

    private string MakeGrassImage(int width, int height)
    {
        var path  = Path.Combine(_tempDir, $"grass-{width}x{height}-{Guid.NewGuid():N}.png");
        using var bmp = new Bitmap(width, height);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(255, 100, 140, 70));   // grass palette colour
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    // -----------------------------------------------------------------------
    // Process runner
    // -----------------------------------------------------------------------

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
        psi.ArgumentList.Add(CliProjectPath);
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Test 1: image-check valid 300x300 exits 0 and prints Status OK
    // -----------------------------------------------------------------------

    [Fact]
    public void ImageCheck_Valid300x300_ExitsZeroStatusOk()
    {
        var img = MakeGrassImage(300, 300);
        var (code, stdout, _) = RunCli("image-check", "--path", img, "--palette", PalettePath);

        Assert.Equal(0, code);
        Assert.Contains("Status:          OK", stdout, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 2: image-check non-300x300 without --resize exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void ImageCheck_NonSquare_WithoutResize_ExitsOne()
    {
        var img  = MakeGrassImage(150, 150);
        var (code, _, _) = RunCli("image-check", "--path", img, "--palette", PalettePath);

        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Test 3: image-check non-300x300 with --resize exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void ImageCheck_NonSquare_WithResize_ExitsZero()
    {
        var img  = MakeGrassImage(150, 150);
        var (code, stdout, _) = RunCli("image-check", "--path", img, "--palette", PalettePath, "--resize");

        Assert.Equal(0, code);
        Assert.Contains("OK", stdout, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 4: image-export valid writes parsed-cell.json
    // -----------------------------------------------------------------------

    [Fact]
    public void ImageExport_Valid_WritesParsedCellJson()
    {
        var img  = MakeGrassImage(300, 300);
        var (code, _, _) = RunCli(
            "image-export",
            "--path",    img,
            "--palette", PalettePath,
            "--output",  OutputDir);

        Assert.Equal(0, code);
        Assert.True(File.Exists(Path.Combine(OutputDir, "parsed-cell.json")));
    }

    // -----------------------------------------------------------------------
    // Test 5: plan-check on valid parsed-cell fixture exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void PlanCheck_ValidFixture_ExitsZero()
    {
        var (code, stdout, _) = RunCli("plan-check", "--path", ParsedCellFixture);

        Assert.Equal(0, code);
        Assert.Contains("OK", stdout, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 6: plan-export writes plan-recommendations.json and plan-report.md
    // -----------------------------------------------------------------------

    [Fact]
    public void PlanExport_ValidFixture_WritesPlanArtifacts()
    {
        var (code, _, _) = RunCli(
            "plan-export",
            "--path",   ParsedCellFixture,
            "--output", OutputDir);

        Assert.Equal(0, code);
        Assert.True(File.Exists(Path.Combine(OutputDir, "plan-recommendations.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "plan-report.md")));
    }

    // -----------------------------------------------------------------------
    // Test 7: full-pipeline exits 0 and writes all three artifacts
    // -----------------------------------------------------------------------

    [Fact]
    public void FullPipeline_Valid300x300_WritesAllArtifacts()
    {
        var img  = MakeGrassImage(300, 300);
        var (code, _, _) = RunCli(
            "full-pipeline",
            "--path",    img,
            "--palette", PalettePath,
            "--output",  OutputDir);

        Assert.Equal(0, code);
        Assert.True(File.Exists(Path.Combine(OutputDir, "parsed-cell.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "regions.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "primitives.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "plan-recommendations.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "plan-report.md")));
    }

    // -----------------------------------------------------------------------
    // Test 8: full-pipeline refuses non-.local output
    // -----------------------------------------------------------------------

    [Fact]
    public void FullPipeline_NonLocalOutput_ExitsOne()
    {
        var img       = MakeGrassImage(300, 300);
        var badOutput = _tempDir;   // no ".local" segment

        var (code, _, stderr) = RunCli(
            "full-pipeline",
            "--path",    img,
            "--palette", PalettePath,
            "--output",  badOutput);

        Assert.Equal(1, code);
        Assert.Contains("refusing", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 9: plan-check with non-integer --tiny-threshold exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void PlanCheck_NonIntegerTinyThreshold_ExitsOne()
    {
        var (code, _, _) = RunCli(
            "plan-check",
            "--path",            ParsedCellFixture,
            "--tiny-threshold",  "abc");

        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Test 10: plan-check with negative --tiny-threshold exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void PlanCheck_NegativeTinyThreshold_ExitsOne()
    {
        var (code, _, _) = RunCli(
            "plan-check",
            "--path",            ParsedCellFixture,
            "--tiny-threshold",  "-1");

        Assert.Equal(1, code);
    }
}
