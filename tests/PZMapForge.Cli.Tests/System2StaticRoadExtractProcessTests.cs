using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadExtractProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-system2-extract-cli", Path.GetRandomFileName());

    public System2StaticRoadExtractProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

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
        psi.ArgumentList.Add(CliProject);
        psi.ArgumentList.Add("--configuration");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("--no-build");
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string MakeTransparentPng(string relPath)
    {
        var full = Path.Combine(_tempDir, relPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using var bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        bmp.Save(full, ImageFormat.Png);
        return full;
    }

    private string MakeContractJson()
    {
        var layers = new[]
        {
            ("static_roads_local",         "layers/static_roads_local.png",         "local_street"),
            ("static_roads_alleys",        "layers/static_roads_alleys.png",        "alley_ruelle"),
            ("static_roads_service",       "layers/static_roads_service.png",       "service_lane"),
            ("static_roads_parking_access","layers/static_roads_parking_access.png","parking_access"),
            ("static_pedestrian_cuts",     "layers/static_pedestrian_cuts.png",     "pedestrian_cut"),
            ("static_road_nodes",          "layers/static_road_nodes.png",          "intersection_turn_deadend_nodes"),
        };
        var layersJson = string.Join(",\n", layers.Select(l =>
            $$"""{ "id": "{{l.Item1}}", "file": "{{l.Item2}}", "class": "{{l.Item3}}", "status": "SYSTEM_2_REQUIRED" }"""));
        var json = $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-overlay-contract.v1",
  "source_reason": "cli-test",
  "layers": [ {{layersJson}} ]
}
""";
        var path = Path.Combine(_tempDir, "contract.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakePaletteJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-intent-palette.v1",
  "entries": [
    { "intent": "local_street_asphalt",    "hex": "#404040" },
    { "intent": "alley_ruelle_asphalt",    "hex": "#303030" },
    { "intent": "service_lane",            "hex": "#505050" },
    { "intent": "parking_access",          "hex": "#606060" },
    { "intent": "sidewalk_or_pedestrian_cut", "hex": "#B0B0B0" },
    { "intent": "intersection_node",       "hex": "#FF00FF" },
    { "intent": "road_turn_node",          "hex": "#00FFFF" },
    { "intent": "dead_end_node",           "hex": "#FF9900" }
  ]
}
""";
        var path = Path.Combine(_tempDir, "palette.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private (string contract, string palette) MakeFullFixture()
    {
        foreach (var layer in new[]
        {
            "static_roads_local", "static_roads_alleys", "static_roads_service",
            "static_roads_parking_access", "static_pedestrian_cuts", "static_road_nodes",
        })
            MakeTransparentPng($"layers/{layer}.png");

        return (MakeContractJson(), MakePaletteJson());
    }

    // -----------------------------------------------------------------------
    // Exit 0 on valid transparent fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithTransparentLayers()
    {
        var (contract, palette) = MakeFullFixture();
        var outJson    = Path.Combine(_tempDir, ".local", "extract.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "extract-summary.txt");

        var (code, stdout, stderr) = RunCli(
            "system2-extract-static-roads",
            "--input",    contract,
            "--palette",  palette,
            "--root",     _tempDir,
            "--output",   outJson,
            "--summary",  summaryTxt);

        Assert.True(code == 0,
            $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Output JSON is created and valid
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson_WhenSuccessful()
    {
        var (contract, palette) = MakeFullFixture();
        var outJson    = Path.Combine(_tempDir, ".local", "extract.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "extract-summary.txt");

        RunCli("system2-extract-static-roads",
            "--input", contract, "--palette", palette, "--root", _tempDir,
            "--output", outJson, "--summary", summaryTxt);

        Assert.True(File.Exists(outJson), "extract.json not created");
        using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal("pzmapforge.deadmtl.system2.static-road-extract.v1",
            doc.RootElement.GetProperty("format").GetString());
    }

    // -----------------------------------------------------------------------
    // Summary TXT is created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesSummaryTxt_WhenSuccessful()
    {
        var (contract, palette) = MakeFullFixture();
        var outJson    = Path.Combine(_tempDir, ".local", "extract.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "extract-summary.txt");

        RunCli("system2-extract-static-roads",
            "--input", contract, "--palette", palette, "--root", _tempDir,
            "--output", outJson, "--summary", summaryTxt);

        Assert.True(File.Exists(summaryTxt), "extract-summary.txt not created");
        var text = File.ReadAllText(summaryTxt);
        Assert.Contains("VERDICT: MAP22E_SYSTEM2_STATIC_ROAD_INTENT_EXTRACT_COMPLETE", text,
            StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // VERDICT printed to stdout
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_PrintsVerdict_WhenSuccessful()
    {
        var (contract, palette) = MakeFullFixture();
        var outJson    = Path.Combine(_tempDir, ".local", "extract.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "extract-summary.txt");

        var (code, stdout, _) = RunCli(
            "system2-extract-static-roads",
            "--input", contract, "--palette", palette, "--root", _tempDir,
            "--output", outJson, "--summary", summaryTxt);

        Assert.Equal(0, code);
        Assert.Contains("MAP22E_SYSTEM2_STATIC_ROAD_INTENT_EXTRACT_COMPLETE", stdout,
            StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Claim boundary in JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputJson_ClaimBoundaryWritesLotpackIsFalse()
    {
        var (contract, palette) = MakeFullFixture();
        var outJson    = Path.Combine(_tempDir, ".local", "extract.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "extract-summary.txt");

        RunCli("system2-extract-static-roads",
            "--input", contract, "--palette", palette, "--root", _tempDir,
            "--output", outJson, "--summary", summaryTxt);

        using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        var boundary = doc.RootElement.GetProperty("claim_boundary");
        Assert.False(boundary.GetProperty("writes_lotpack").GetBoolean());
        Assert.False(boundary.GetProperty("runtime_proven").GetBoolean());
        Assert.False(boundary.GetProperty("public_playable_claim").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Missing required args -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-extract-static-roads");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Output path not under .local -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputPathNotUnderLocal()
    {
        var (contract, palette) = MakeFullFixture();
        var badOutput  = Path.Combine(_tempDir, "extract.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "extract-summary.txt");

        var (code, _, stderr) = RunCli(
            "system2-extract-static-roads",
            "--input", contract, "--palette", palette, "--root", _tempDir,
            "--output", badOutput, "--summary", summaryTxt);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // system2-extract-static-roads in UnknownCommand list
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsSystem2ExtractStaticRoads()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-extract-static-roads", stderr, StringComparison.Ordinal);
    }
}
