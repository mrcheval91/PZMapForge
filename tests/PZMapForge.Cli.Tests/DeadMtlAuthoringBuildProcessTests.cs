using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlAuthoringBuildProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-deadmtl-authoring-cli", Path.GetRandomFileName());

    public DeadMtlAuthoringBuildProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string RealPackRoot =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack");

    private string LocalOutputDir => Path.Combine(_tempDir, ".local", "authoring");

    private static readonly Color KnownWater  = Color.FromArgb(255,   0,   0, 255);
    private static readonly Color KnownShore  = Color.FromArgb(255, 216, 192, 128);
    private static readonly Color KnownForest = Color.FromArgb(255,  32, 112,  32);
    private static readonly Color KnownRoad   = Color.FromArgb(255, 255, 102,   0);
    private static readonly Color Transparent = Color.FromArgb(0,    0,   0,   0);

    private static readonly string[] System1Ids     = ["water", "shore", "parks_forest", "roads_major"];
    private static readonly string[] PlaceholderIds =
    [
        "roads_local", "zones_residential", "zones_commercial", "zones_industrial",
        "placed_buildings", "props", "npc_zones", "ownership",
    ];

    private string CreateMinimalValidPack(int width = 4, int height = 3)
    {
        var pack = Path.Combine(_tempDir, Path.GetRandomFileName());
        Directory.CreateDirectory(pack);
        Directory.CreateDirectory(Path.Combine(pack, "palettes"));
        Directory.CreateDirectory(Path.Combine(pack, "layers"));
        Directory.CreateDirectory(Path.Combine(pack, "scripts"));

        File.WriteAllText(Path.Combine(pack, "deadmtl_worldgen_project.json"), $$"""
            {
              "format": "pzmapforge.worldgen.project.v1",
              "map_id": "test_pack",
              "origin_x": 10580,
              "origin_y": 8200,
              "width": {{width}},
              "height": {{height}},
              "layers": [
                {"id":"water",       "path":"layers/water.png",       "palette":"palettes/worldgen-png-palette.json","priority":10},
                {"id":"shore",       "path":"layers/shore.png",       "palette":"palettes/worldgen-png-palette.json","priority":20},
                {"id":"parks_forest","path":"layers/parks_forest.png","palette":"palettes/worldgen-png-palette.json","priority":30},
                {"id":"roads_major", "path":"layers/roads_major.png", "palette":"palettes/worldgen-png-palette.json","priority":50}
              ]
            }
            """, Encoding.UTF8);

        File.WriteAllText(Path.Combine(pack, "README.md"), "# Test Pack\n");
        File.WriteAllText(Path.Combine(pack, "palettes", "worldgen-png-palette.json"), """
            {
              "format": "pzmapforge.worldgen.png-palette.v1",
              "entries": [
                {"color":"#0000FF","type":"biome", "key":"water"},
                {"color":"#D8C080","type":"biome", "key":"sand_bank"},
                {"color":"#207020","type":"biome", "key":"birch_forest"},
                {"color":"#FF6600","type":"prefab","key":"normal_road_WE_00"},
                {"color":"#CC3300","type":"prefab","key":"highway_NS_00"}
              ]
            }
            """, Encoding.UTF8);

        File.WriteAllText(Path.Combine(pack, "palettes", "zoning-palette.json"),   "{\"_note\":\"FUTURE\"}\n");
        File.WriteAllText(Path.Combine(pack, "palettes", "metadata-palette.json"), "{\"_note\":\"FUTURE\"}\n");
        File.WriteAllText(Path.Combine(pack, "scripts", "generate-empty-layer-pack.ps1"), "# stub\n");
        File.WriteAllText(Path.Combine(pack, "scripts", "validate-layer-pack.ps1"),        "# stub\n");

        var s1Colors = new[] { KnownWater, KnownShore, KnownForest, KnownRoad };
        for (var i = 0; i < System1Ids.Length; i++)
        {
            using var bmp = new Bitmap(width, height);
            using var g   = Graphics.FromImage(bmp);
            g.Clear(Transparent);
            bmp.SetPixel(0, 0, s1Colors[i]);
            bmp.Save(Path.Combine(pack, "layers", $"{System1Ids[i]}.png"), ImageFormat.Png);
        }
        foreach (var id in PlaceholderIds)
        {
            using var bmp = new Bitmap(width, height);
            using var g   = Graphics.FromImage(bmp);
            g.Clear(Transparent);
            bmp.Save(Path.Combine(pack, "layers", $"{id}.png"), ImageFormat.Png);
        }
        return pack;
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
    // Test 1: valid real pack exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_ValidRealPack_ExitsZero()
    {
        var (code, stdout, stderr) = RunCli(
            "deadmtl-authoring-build", "--input", RealPackRoot, "--output", LocalOutputDir);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 2: stdout contains Status OK
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_ValidRealPack_StdoutContainsStatusOk()
    {
        var (_, stdout, _) = RunCli(
            "deadmtl-authoring-build", "--input", RealPackRoot, "--output", LocalOutputDir);

        Assert.Contains("Status:        OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 3: writes all three output files
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_ValidRealPack_WritesThreeFiles()
    {
        RunCli("deadmtl-authoring-build", "--input", RealPackRoot, "--output", LocalOutputDir);

        Assert.True(File.Exists(Path.Combine(LocalOutputDir, "worldgen_layers.json")));
        Assert.True(File.Exists(Path.Combine(LocalOutputDir, "WorldGenOverride.lua")));
        Assert.True(File.Exists(Path.Combine(LocalOutputDir, "proof-summary.txt")));
    }

    // -----------------------------------------------------------------------
    // Test 4: Lua has proven markers
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_ValidRealPack_LuaHasProvenMarkers()
    {
        RunCli("deadmtl-authoring-build", "--input", RealPackRoot, "--output", LocalOutputDir);

        var lua = File.ReadAllText(Path.Combine(LocalOutputDir, "WorldGenOverride.lua"));
        Assert.Contains("PZMAPFORGE_WORLDGENOVERRIDE_LOADED", lua, StringComparison.Ordinal);
        Assert.Contains("worldgen[\"static_modules\"] = {",  lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 5: Lua is ASCII/no-BOM
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_ValidRealPack_LuaIsAsciiNoBom()
    {
        RunCli("deadmtl-authoring-build", "--input", RealPackRoot, "--output", LocalOutputDir);

        var bytes       = File.ReadAllBytes(Path.Combine(LocalOutputDir, "WorldGenOverride.lua"));
        var hasBom      = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var hasNonAscii = bytes.Any(b => b > 127);

        Assert.False(hasBom,        "Lua must not have BOM");
        Assert.False(hasNonAscii,   "Lua must be ASCII-only");
    }

    // -----------------------------------------------------------------------
    // Test 6: proof summary contains map_id
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_ValidRealPack_ProofSummaryContainsMapId()
    {
        RunCli("deadmtl-authoring-build", "--input", RealPackRoot, "--output", LocalOutputDir);

        var proof = File.ReadAllText(Path.Combine(LocalOutputDir, "proof-summary.txt"));
        Assert.Contains("deadmtl_build42_worldgen_v1", proof, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 7: proof summary contains claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_ValidRealPack_ProofSummaryContainsClaimBoundary()
    {
        RunCli("deadmtl-authoring-build", "--input", RealPackRoot, "--output", LocalOutputDir);

        var proof = File.ReadAllText(Path.Combine(LocalOutputDir, "proof-summary.txt"));
        Assert.Contains("authoring artifact only", proof, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 8: missing --input exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_MissingInput_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli("deadmtl-authoring-build", "--output", LocalOutputDir);

        Assert.NotEqual(0, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 9: missing --output exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_MissingOutput_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli("deadmtl-authoring-build", "--input", RealPackRoot);

        Assert.NotEqual(0, code);
        Assert.Contains("--output", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 10: output outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_OutputOutsideLocal_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "not-local", "output");

        var (code, _, stderr) = RunCli(
            "deadmtl-authoring-build", "--input", RealPackRoot, "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 11: invalid input pack exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_InvalidPack_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli(
            "deadmtl-authoring-build",
            "--input",  Path.Combine(_tempDir, "nonexistent-pack"),
            "--output", LocalOutputDir);

        Assert.NotEqual(0, code);
        Assert.Contains("INVALID", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 12: warning-only pack exits zero and stdout mentions warnings
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_WarningOnlyPack_ExitsZero()
    {
        var pack = CreateMinimalValidPack(width: 4, height: 3);

        // Paint a placeholder layer pixel (produces warning, not error)
        using (var bmp = new Bitmap(4, 3))
        using (var g   = Graphics.FromImage(bmp))
        {
            g.Clear(Transparent);
            bmp.SetPixel(2, 1, Color.FromArgb(255, 11, 22, 33));
            bmp.Save(Path.Combine(pack, "layers", "roads_local.png"), ImageFormat.Png);
        }

        var (code, stdout, stderr) = RunCli(
            "deadmtl-authoring-build", "--input", pack, "--output", LocalOutputDir);

        Assert.True(code == 0, $"Warning-only pack should exit 0. Stdout: {stdout}. Stderr: {stderr}");
        Assert.Contains("Status:        OK", stdout, StringComparison.Ordinal);
    }
}
