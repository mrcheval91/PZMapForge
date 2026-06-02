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
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private static readonly Color GrassColor = Color.FromArgb(100, 140, 70);
    private static readonly Color RoadColor  = Color.FromArgb(70, 70, 70);

    private string MakeSolid(string name, int width, int height, Color color)
    {
        var path = Path.Combine(_tempDir, name);

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.Clear(color);
        bitmap.Save(path, ImageFormat.Png);

        return path;
    }

    private string WriteManifest(string layerName, string imageName, string allowedKinds)
    {
        var path = Path.Combine(_tempDir, "manifest.json");

        File.WriteAllText(
            path,
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
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        processStartInfo.ArgumentList.Add("run");
        processStartInfo.ArgumentList.Add("--project");
        processStartInfo.ArgumentList.Add(CliProjectPath);
        processStartInfo.ArgumentList.Add("--configuration");
        processStartInfo.ArgumentList.Add("Release");
        processStartInfo.ArgumentList.Add("--no-build");
        processStartInfo.ArgumentList.Add("--");

        foreach (var arg in args)
            processStartInfo.ArgumentList.Add(arg);

        using var process = Process.Start(processStartInfo)!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return (process.ExitCode, stdout, stderr);
    }

    [Fact]
    public void LayerValidate_ValidLayer_ExitsZeroStatusOk()
    {
        MakeSolid("terrain.png", 300, 300, GrassColor);
        var manifest = WriteManifest("terrain", "terrain.png", "\"grass\"");

        var (code, stdout, stderr) = RunCli(
            "layer-validate",
            "--layers", manifest,
            "--palette", PalettePath);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.Contains("Status:     OK", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void LayerValidate_MissingImage_ExitsOne()
    {
        var manifest = WriteManifest("terrain", "terrain.png", "\"grass\"");

        var (code, _, _) = RunCli(
            "layer-validate",
            "--layers", manifest,
            "--palette", PalettePath);

        Assert.Equal(1, code);
    }

    [Fact]
    public void LayerValidate_DisallowedKind_ExitsOne()
    {
        MakeSolid("roads.png", 300, 300, RoadColor);
        var manifest = WriteManifest("roads", "roads.png", "\"row_house\"");

        var (code, _, stderr) = RunCli(
            "layer-validate",
            "--layers", manifest,
            "--palette", PalettePath);

        Assert.Equal(1, code);
        Assert.Contains("road", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LayerValidate_NonSquare_WithoutResize_ExitsOne()
    {
        MakeSolid("terrain.png", 150, 150, GrassColor);
        var manifest = WriteManifest("terrain", "terrain.png", "\"grass\"");

        var (code, _, _) = RunCli(
            "layer-validate",
            "--layers", manifest,
            "--palette", PalettePath);

        Assert.Equal(1, code);
    }

    [Fact]
    public void LayerValidate_NonSquare_WithResize_ExitsZero()
    {
        MakeSolid("terrain.png", 150, 150, GrassColor);
        var manifest = WriteManifest("terrain", "terrain.png", "\"grass\"");

        var (code, stdout, stderr) = RunCli(
            "layer-validate",
            "--layers", manifest,
            "--palette", PalettePath,
            "--resize");

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.Contains("Status:     OK", stdout, StringComparison.Ordinal);
    }
}