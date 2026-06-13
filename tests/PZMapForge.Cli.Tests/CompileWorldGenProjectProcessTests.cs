using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class CompileWorldGenProjectProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-wg-project-cli-tests", Path.GetRandomFileName());

    public CompileWorldGenProjectProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string LocalOutputDir  => Path.Combine(_tempDir, ".local", "worldgen");
    private string LocalOutputFile => Path.Combine(LocalOutputDir, "worldgen_layers.json");

    private static readonly Color Water      = Color.FromArgb(255,   0,   0, 255);
    private static readonly Color SandBank   = Color.FromArgb(255, 216, 192, 128);
    private static readonly Color Transparent = Color.FromArgb(0,    0,   0,   0);

    private string MakePng(string name, int width, int height, Color fill)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp = new Bitmap(width, height);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(fill);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string WritePalette(string name = "palette.json")
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, """
            {
              "format": "pzmapforge.worldgen.png-palette.v1",
              "entries": [
                { "color": "#0000FF", "type": "biome",  "key": "water" },
                { "color": "#D8C080", "type": "biome",  "key": "sand_bank" },
                { "color": "#00AA00", "type": "biome",  "key": "grass_plain" },
                { "color": "#FF0000", "type": "prefab", "key": "normal_road_WE_00" },
                { "color": "#AA0000", "type": "prefab", "key": "highway_NS_00" }
              ]
            }
            """, Encoding.UTF8);
        return path;
    }

    private string WriteProject(string mapId, int width, int height, int originX, int originY,
        IEnumerable<(string id, string pngName, string paletteName, int priority)> layers)
    {
        var layersJson = string.Join(",\n    ", layers.Select(l =>
            $"{{\"id\":\"{l.id}\",\"path\":\"{l.pngName}\",\"palette\":\"{l.paletteName}\",\"priority\":{l.priority}}}"));

        var json = $$"""
            {
              "format": "pzmapforge.worldgen.project.v1",
              "map_id": "{{mapId}}",
              "origin_x": {{originX}},
              "origin_y": {{originY}},
              "width": {{width}},
              "height": {{height}},
              "layers": [
                {{layersJson}}
              ]
            }
            """;

        var path = Path.Combine(_tempDir, $"project-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
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
    // Test 1: single water layer → exit 0 and writes manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_SingleLayer_ExitsZeroAndWritesManifest()
    {
        WritePalette();
        MakePng("water.png", 4, 3, Water);
        var project = WriteProject("test_map", 4, 3, 10000, 8000, [
            ("water", "water.png", "palette.json", 10),
        ]);

        var (code, stdout, stderr) = RunCli(
            "compile-worldgen-project",
            "--input",  project,
            "--output", LocalOutputFile);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(LocalOutputFile), $"Manifest not found: {LocalOutputFile}");
    }

    // -----------------------------------------------------------------------
    // Test 2: stdout contains Status OK
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_SingleLayer_StdoutContainsStatusOk()
    {
        WritePalette();
        MakePng("water.png", 4, 3, Water);
        var project = WriteProject("test_map", 4, 3, 0, 0, [
            ("water", "water.png", "palette.json", 10),
        ]);

        var (_, stdout, _) = RunCli(
            "compile-worldgen-project",
            "--input",  project,
            "--output", LocalOutputFile);

        Assert.Contains("Status:        OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 3: generated manifest has correct format in JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_GeneratedManifest_HasCorrectFormat()
    {
        WritePalette();
        MakePng("water.png", 4, 4, Water);
        var project = WriteProject("format_test", 4, 4, 0, 0, [
            ("water", "water.png", "palette.json", 10),
        ]);

        RunCli("compile-worldgen-project", "--input", project, "--output", LocalOutputFile);

        var json = File.ReadAllText(LocalOutputFile);
        Assert.Contains("pzmapforge.worldgen.layers.v1", json, StringComparison.Ordinal);
        Assert.Contains("format_test",                  json, StringComparison.Ordinal);
        Assert.Contains("static_modules",               json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 4: manifest feeds into compile-worldgen → Lua output is valid
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_ManifestFeedsIntoCompileWorldGen()
    {
        WritePalette();
        MakePng("water.png", 4, 3, Water);
        var project = WriteProject("pipeline_test", 4, 3, 10682, 8240, [
            ("water", "water.png", "palette.json", 10),
        ]);

        RunCli("compile-worldgen-project", "--input", project, "--output", LocalOutputFile);
        Assert.True(File.Exists(LocalOutputFile));

        var luaPath = Path.Combine(_tempDir, ".local", "worldgen", "WorldGenOverride.lua");
        var (code, stdout, stderr) = RunCli(
            "compile-worldgen",
            "--input",  LocalOutputFile,
            "--output", luaPath);

        Assert.True(code == 0, $"compile-worldgen failed. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(luaPath));

        var lua = File.ReadAllText(luaPath);
        Assert.Contains("PZMAPFORGE_WORLDGENOVERRIDE_LOADED",    lua, StringComparison.Ordinal);
        Assert.Contains("worldgen[\"static_modules\"] = {",     lua, StringComparison.Ordinal);
        Assert.Contains("biome = worldgen.biomes.water",         lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 5: two layers with priority override
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_TwoLayers_HigherPriorityWins()
    {
        WritePalette();
        MakePng("water.png",    4, 4, Water);
        MakePng("sandbank.png", 4, 4, SandBank);
        var project = WriteProject("override_test", 4, 4, 0, 0, [
            ("water",    "water.png",    "palette.json", 10),
            ("sandbank", "sandbank.png", "palette.json", 50),
        ]);

        RunCli("compile-worldgen-project", "--input", project, "--output", LocalOutputFile);

        var json = File.ReadAllText(LocalOutputFile);
        Assert.Contains("sand_bank", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"key\": \"water\"", json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 6: missing --input exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_MissingInput_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli("compile-worldgen-project", "--output", LocalOutputFile);

        Assert.NotEqual(0, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 7: missing --output exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_MissingOutput_ExitsNonZero()
    {
        var path = Path.Combine(_tempDir, "p.json");
        File.WriteAllText(path, "{}");
        var (code, _, stderr) = RunCli("compile-worldgen-project", "--input", path);

        Assert.NotEqual(0, code);
        Assert.Contains("--output", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 8: output outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_OutputOutsideLocal_ExitsNonZero()
    {
        WritePalette();
        MakePng("water.png", 2, 2, Water);
        var project   = WriteProject("test_map", 2, 2, 0, 0, [("water", "water.png", "palette.json", 10)]);
        var badOutput = Path.Combine(_tempDir, "not-local", "manifest.json");

        var (code, _, stderr) = RunCli("compile-worldgen-project", "--input", project, "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 9: project file not found exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_ProjectNotFound_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli(
            "compile-worldgen-project",
            "--input",  Path.Combine(_tempDir, "nonexistent.json"),
            "--output", LocalOutputFile);

        Assert.NotEqual(0, code);
        Assert.Contains("not found", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 10: missing layer PNG exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGenProject_MissingLayerPng_ExitsNonZero()
    {
        WritePalette();
        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("water", "nonexistent.png", "palette.json", 10),
        ]);

        var (code, _, stderr) = RunCli("compile-worldgen-project", "--input", project, "--output", LocalOutputFile);

        Assert.NotEqual(0, code);
        Assert.Contains("PNG not found", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
