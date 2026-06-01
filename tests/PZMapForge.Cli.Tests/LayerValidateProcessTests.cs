using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class LayerValidateProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-layer-validate-tests", Path.GetRandomFileName());

    public LayerValidateProcessTests() => Directory.CreateDirectory(_tempDir);

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

    private static readonly Color GrassColor    = Color.FromArgb(100, 140, 70);
    private static readonly Color RoadColor     = Color.FromArgb( 70,  70, 70);

    private string MakeSolid(string name, int w, int h, Color color)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp = new Bitmap(w, h);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(color);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string WriteManifest(string layerName, string imageName, string allowedKinds)
    {
        var path = Path.Combine(_tempDir, "manifest.json");
        File.WriteAllText(path,
            "{\"schema\":\"pzmapforge.layer-manifest.v0.1\"," +
            "\"claim_boundary\":\"planning_artifact_only_not_pz_load_tested\"," +
            "\"width\":300,\"height\":300," +
            "\"layers\":[{\"name\":\"" + layerName + "\",\"path\":\"" + imageName +
            "\",\"allowed_kinds\":[" + allowedKinds + "]}]," +
            "\"precedence\":[\"" + layerName + "\"]}",
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
    // Test 1: valid grass-only layer exits 0 and prints Status OK
    // -----------------------------------------------------------------------

    [Fact]
    public void LayerValidate_ValidLayer_ExitsZeroStatusOk()
    {
        MakeSolid("terrain.png", 300, 300, GrassColor);
        var manifest = WriteManifest("terrain", "terrain.png", "\"grass\"");

        var (code, stdout, _) = RunCli(
            "layer-validate",
            "--layers",  manifest,
            "--palette", PalettePath);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}");
        Assert.Contains("Status:     OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 2: missing layer image exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void LayerValidate_MissingImage_ExitsOne()
    {
        // terrain.png intentionally NOT created
        var manifest = WriteManifest("terrain", "terrain.png", "\"grass\"");

        var (code, _, _) = RunCli(
            "layer-validate",
            "--layers",  manifest,
            "--palette", PalettePath);

        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Test 3: disallowed kind in layer exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void LayerValidate_DisallowedKind_ExitsOne()
    {
        MakeSolid("roads.png", 300, 300, RoadColor); // all "road"
        var manifest = WriteManifest("roads", "roads.png", "\"row_house\""); // road not allowed

        var (code, _, stderr) = RunCli(
            "layer-validate",
            "--layers",  manifest,
            "--palette", PalettePath);

        Assert.Equal(1, code);
        Assert.Contains("road", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 4: non-300x300 without --resize exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void LayerValidate_NonSquare_WithoutResize_ExitsOne()
    {
        MakeSolid("terrain.png", 150, 150, GrassColor);
        var manifest = WriteManifest("terrain", "terrain.png", "\"grass\"");

        var (code, _, _) = RunCli(
            "layer-validate",
            "--layers",  manifest,
            "--palette", PalettePath);

        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Test 5: non-300x300 with --resize exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void LayerValidate_NonSquare_WithResize_ExitsZero()
    {
        MakeSolid("terrain.png", 150, 150, GrassColor);
        var manifest = WriteManifest("terrain", "terrain.png", "\"grass\"");

        var (code, stdout, stderr) = RunCli(
            "layer-validate",
            "--layers",  manifest,
            "--palette", PalettePath,
            "--resize");

        Assert.True(code == 0, $"Exited {code}. Stderr: {stderr}");
        Assert.Contains("Status:     OK", stdout, StringComparison.Ordinal);
    }
}
