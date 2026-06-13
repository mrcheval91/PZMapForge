using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class CompileWorldGenPngProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-wg-png-cli-tests", Path.GetRandomFileName());

    public CompileWorldGenPngProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string SamplePalette =>
        Path.Combine(RepoRoot, "examples", "worldgen", "worldgen-png-palette.json");

    private string LocalManifestPath =>
        Path.Combine(_tempDir, ".local", "worldgen", "worldgen_layers.json");

    private static readonly Color Water = Color.FromArgb(255, 0, 0, 255);

    private string MakePng(int width, int height, Color fill)
    {
        var path = Path.Combine(_tempDir, $"img-{Guid.NewGuid():N}.png");
        using var bmp = new Bitmap(width, height);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(fill);
        bmp.Save(path, ImageFormat.Png);
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
    // Test 1: water PNG exits 0 and writes manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_WaterPng_ExitsZeroAndWritesManifest()
    {
        var png = MakePng(4, 3, Water);
        var (code, stdout, stderr) = RunCli(
            "compile-worldgen-png",
            "--input",    png,
            "--palette",  SamplePalette,
            "--map-id",   "test_map",
            "--origin-x", "10000",
            "--origin-y", "8000",
            "--output",   LocalManifestPath);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(LocalManifestPath), $"Manifest not found: {LocalManifestPath}");
    }

    // -----------------------------------------------------------------------
    // Test 2: stdout contains Status OK
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_WaterPng_StdoutContainsStatusOk()
    {
        var png = MakePng(4, 3, Water);
        var (_, stdout, _) = RunCli(
            "compile-worldgen-png",
            "--input",    png,
            "--palette",  SamplePalette,
            "--map-id",   "test_map",
            "--origin-x", "0",
            "--origin-y", "0",
            "--output",   LocalManifestPath);

        Assert.Contains("Status:        OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 3: manifest is valid JSON with correct format
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_WaterPng_ManifestHasCorrectFormat()
    {
        var png = MakePng(2, 2, Water);
        RunCli("compile-worldgen-png",
            "--input",    png,
            "--palette",  SamplePalette,
            "--map-id",   "format_test",
            "--origin-x", "0",
            "--origin-y", "0",
            "--output",   LocalManifestPath);

        var json = File.ReadAllText(LocalManifestPath);
        Assert.Contains("pzmapforge.worldgen.layers.v1", json, StringComparison.Ordinal);
        Assert.Contains("format_test",                  json, StringComparison.Ordinal);
        Assert.Contains("static_modules",               json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 4: generated manifest can be fed to compile-worldgen
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_ManifestFeedsIntoCompileWorldGen()
    {
        var png = MakePng(3, 2, Water);
        RunCli("compile-worldgen-png",
            "--input",    png,
            "--palette",  SamplePalette,
            "--map-id",   "pipeline_test",
            "--origin-x", "10682",
            "--origin-y", "8240",
            "--output",   LocalManifestPath);

        Assert.True(File.Exists(LocalManifestPath));

        var luaPath = Path.Combine(_tempDir, ".local", "worldgen", "WorldGenOverride.lua");
        var (code, stdout, stderr) = RunCli(
            "compile-worldgen",
            "--input",  LocalManifestPath,
            "--output", luaPath);

        Assert.True(code == 0, $"compile-worldgen failed. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(luaPath));

        var lua = File.ReadAllText(luaPath);
        Assert.Contains("PZMAPFORGE_WORLDGENOVERRIDE_LOADED",    lua, StringComparison.Ordinal);
        Assert.Contains("worldgen[\"static_modules\"] = {",     lua, StringComparison.Ordinal);
        Assert.Contains("biome = worldgen.biomes.water",         lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 5: missing --input exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_MissingInput_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli(
            "compile-worldgen-png",
            "--palette",  SamplePalette,
            "--map-id",   "x",
            "--origin-x", "0",
            "--origin-y", "0",
            "--output",   LocalManifestPath);

        Assert.NotEqual(0, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 6: missing --palette exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_MissingPalette_ExitsNonZero()
    {
        var png = MakePng(2, 2, Water);
        var (code, _, stderr) = RunCli(
            "compile-worldgen-png",
            "--input",    png,
            "--map-id",   "x",
            "--origin-x", "0",
            "--origin-y", "0",
            "--output",   LocalManifestPath);

        Assert.NotEqual(0, code);
        Assert.Contains("--palette", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 7: missing --map-id exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_MissingMapId_ExitsNonZero()
    {
        var png = MakePng(2, 2, Water);
        var (code, _, stderr) = RunCli(
            "compile-worldgen-png",
            "--input",    png,
            "--palette",  SamplePalette,
            "--origin-x", "0",
            "--origin-y", "0",
            "--output",   LocalManifestPath);

        Assert.NotEqual(0, code);
        Assert.Contains("--map-id", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 8: missing --output exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_MissingOutput_ExitsNonZero()
    {
        var png = MakePng(2, 2, Water);
        var (code, _, stderr) = RunCli(
            "compile-worldgen-png",
            "--input",    png,
            "--palette",  SamplePalette,
            "--map-id",   "x",
            "--origin-x", "0",
            "--origin-y", "0");

        Assert.NotEqual(0, code);
        Assert.Contains("--output", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 9: output outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_OutputOutsideLocal_ExitsNonZero()
    {
        var png       = MakePng(2, 2, Water);
        var badOutput = Path.Combine(_tempDir, "not-local", "manifest.json");
        var (code, _, stderr) = RunCli(
            "compile-worldgen-png",
            "--input",    png,
            "--palette",  SamplePalette,
            "--map-id",   "x",
            "--origin-x", "0",
            "--origin-y", "0",
            "--output",   badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 10: PNG not found exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenPng_PngNotFound_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli(
            "compile-worldgen-png",
            "--input",    Path.Combine(_tempDir, "nonexistent.png"),
            "--palette",  SamplePalette,
            "--map-id",   "x",
            "--origin-x", "0",
            "--origin-y", "0",
            "--output",   LocalManifestPath);

        Assert.NotEqual(0, code);
        Assert.Contains("not found", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
