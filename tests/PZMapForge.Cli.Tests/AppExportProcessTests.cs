using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for the app-export CLI command.
/// All tests use programmatically generated images only.
/// No real PZ install is required.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class AppExportProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-app-export-tests", Path.GetRandomFileName());

    public AppExportProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private string CreateTestImage()
    {
        var imgPath = Path.Combine(_tempDir, "test.png");
        using var bmp = new Bitmap(300, 300);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(255, 100, 140, 70));
        bmp.Save(imgPath, ImageFormat.Png);
        return imgPath;
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
        psi.ArgumentList.Add(CliProjectPath);
        psi.ArgumentList.Add("--configuration");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("--no-build");
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Test 1: valid image writes index.html and exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_ValidImage_WritesIndexHtml()
    {
        var imgPath   = CreateTestImage();
        var outputDir = Path.Combine(_tempDir, ".local", "app");

        var (code, stdout, stderr) = RunCli(
            "app-export",
            "--path",    imgPath,
            "--palette", PalettePath,
            "--output",  outputDir);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(Path.Combine(outputDir, "index.html")),
            "index.html was not written");
    }

    // -----------------------------------------------------------------------
    // Test 2: index.html contains claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_IndexHtml_ContainsClaimBoundary()
    {
        var imgPath   = CreateTestImage();
        var outputDir = Path.Combine(_tempDir, ".local", "app");

        var (code, _, _) = RunCli(
            "app-export",
            "--path",    imgPath,
            "--palette", PalettePath,
            "--output",  outputDir);

        Assert.Equal(0, code);

        var html = File.ReadAllText(Path.Combine(outputDir, "index.html"));
        Assert.Contains("planning_artifact_only_not_pz_load_tested", html, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 3: index.html links artifact file names
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_IndexHtml_ContainsArtifactLinks()
    {
        var imgPath   = CreateTestImage();
        var outputDir = Path.Combine(_tempDir, ".local", "app");

        var (code, _, _) = RunCli(
            "app-export",
            "--path",    imgPath,
            "--palette", PalettePath,
            "--output",  outputDir);

        Assert.Equal(0, code);

        var html = File.ReadAllText(Path.Combine(outputDir, "index.html"));
        Assert.Contains("parsed-cell.json",          html, StringComparison.Ordinal);
        Assert.Contains("regions.json",              html, StringComparison.Ordinal);
        Assert.Contains("primitives.json",           html, StringComparison.Ordinal);
        Assert.Contains("plan-recommendations.json", html, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 4: refuses output outside .local
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_NonLocalOutput_ExitsOne()
    {
        var imgPath   = CreateTestImage();
        var badOutput = _tempDir;  // no .local segment

        var (code, _, stderr) = RunCli(
            "app-export",
            "--path",    imgPath,
            "--palette", PalettePath,
            "--output",  badOutput);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 5: exits nonzero when --path is missing
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_MissingPath_ExitsOne()
    {
        var (code, _, stderr) = RunCli("app-export", "--palette", PalettePath);

        Assert.Equal(1, code);
        Assert.Contains("--path", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
