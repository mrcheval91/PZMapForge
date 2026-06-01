using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level integration test for the layer-pipeline CLI command.
/// Creates temp layer images and a manifest, runs the command, verifies artifacts.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class LayerPipelineProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-layer-pipeline-tests", Path.GetRandomFileName());

    private string OutputDir => Path.Combine(_tempDir, ".local", "mapforge");

    public LayerPipelineProcessTests()
    {
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(OutputDir);
    }

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

    private string MakeGrassImage(string name)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp = new Bitmap(300, 300);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(100, 140, 70)); // grass
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string WriteManifest(string imageName)
    {
        var path = Path.Combine(_tempDir, "manifest.json");
        File.WriteAllText(path,
            "{" +
            "\"schema\":\"pzmapforge.layer-manifest.v0.1\"," +
            "\"claim_boundary\":\"planning_artifact_only_not_pz_load_tested\"," +
            "\"width\":300,\"height\":300," +
            "\"layers\":[{\"name\":\"terrain\",\"path\":\"" + imageName + "\",\"allowed_kinds\":[\"grass\"]}]," +
            "\"precedence\":[\"terrain\"]" +
            "}",
            Encoding.UTF8);
        return path;
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
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Test 11: layer-pipeline writes all 8 artifacts and they contain the claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void LayerPipeline_Valid_WritesAllArtifactsWithClaimBoundary()
    {
        MakeGrassImage("terrain.png");
        var manifestPath = WriteManifest("terrain.png");

        var (code, stdout, stderr) = RunCli(
            "layer-pipeline",
            "--layers",  manifestPath,
            "--palette", PalettePath,
            "--output",  OutputDir);

        // Exit 0
        Assert.True(code == 0, $"layer-pipeline exited {code}.\nStdout: {stdout}\nStderr: {stderr}");

        // All 8 artifacts present
        Assert.True(File.Exists(Path.Combine(OutputDir, "parsed-cell.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "layer-merge-report.md")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "regions.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "regions-report.md")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "primitives.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "primitives-report.md")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "plan-recommendations.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "plan-report.md")));

        // Claim boundary present in key markdown artifacts
        Assert.Contains(
            "planning_artifact_only_not_pz_load_tested",
            File.ReadAllText(Path.Combine(OutputDir, "layer-merge-report.md")),
            StringComparison.Ordinal);
        Assert.Contains(
            "planning_artifact_only_not_pz_load_tested",
            File.ReadAllText(Path.Combine(OutputDir, "plan-report.md")),
            StringComparison.Ordinal);
    }
}
