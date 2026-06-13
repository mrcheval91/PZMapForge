using System.Text.Json;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlPaletteProofTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ScriptsDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts");

    private static string PalettesDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "palettes");

    private static string DocsDir =>
        Path.Combine(RepoRoot, "docs", "authoring");

    private static string ChartScript =>
        Path.Combine(ScriptsDir, "generate-palette-color-charts.ps1");

    private static string ProofPackScript =>
        Path.Combine(ScriptsDir, "generate-worldgen-palette-proof-pack.ps1");

    private static string RunScript =>
        Path.Combine(ScriptsDir, "run-worldgen-palette-proof-build.ps1");

    // -----------------------------------------------------------------------
    // Script existence
    // -----------------------------------------------------------------------

    [Fact]
    public void ChartScript_Exists() =>
        Assert.True(File.Exists(ChartScript), $"Expected: {ChartScript}");

    [Fact]
    public void ProofPackScript_Exists() =>
        Assert.True(File.Exists(ProofPackScript), $"Expected: {ProofPackScript}");

    [Fact]
    public void RunScript_Exists() =>
        Assert.True(File.Exists(RunScript), $"Expected: {RunScript}");

    // -----------------------------------------------------------------------
    // Chart script content
    // -----------------------------------------------------------------------

    [Fact]
    public void ChartScript_ReferencesChartPng()
    {
        var text = File.ReadAllText(ChartScript);
        Assert.Contains("worldgen-png-palette.chart.png", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChartScript_ReferencesSwatchesTxt()
    {
        var text = File.ReadAllText(ChartScript);
        Assert.Contains("worldgen-png-palette.swatches.txt", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChartScript_ReferencesLayerGuideTxt()
    {
        var text = File.ReadAllText(ChartScript);
        Assert.Contains("worldgen-png-palette.layer-guide.txt", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChartScript_ContainsVisualConfirmedStatus()
    {
        var text = File.ReadAllText(ChartScript);
        Assert.Contains("VISUAL_CONFIRMED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChartScript_ContainsKnownInCodeStatus()
    {
        var text = File.ReadAllText(ChartScript);
        Assert.Contains("KNOWN_IN_CODE", text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Proof pack script content
    // -----------------------------------------------------------------------

    [Fact]
    public void ProofPackScript_ContainsMapId()
    {
        var text = File.ReadAllText(ProofPackScript);
        Assert.Contains("deadmtl_worldgen_palette_probe_v1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProofPackScript_ContainsSpawnX()
    {
        var text = File.ReadAllText(ProofPackScript);
        Assert.Contains("10650", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProofPackScript_ContainsSpawnY()
    {
        var text = File.ReadAllText(ProofPackScript);
        Assert.Contains("8250", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProofPackScript_ContainsSpawnPixelX()
    {
        var text = File.ReadAllText(ProofPackScript);
        Assert.Contains("70", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProofPackScript_ContainsSpawnPixelY()
    {
        var text = File.ReadAllText(ProofPackScript);
        Assert.Contains("50", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProofPackScript_ContainsAllTwelveHexCodes()
    {
        var text     = File.ReadAllText(ProofPackScript);
        var hexCodes = new[]
        {
            "#0000FF", "#D8C080", "#00AA00", "#55CC55",
            "#207020", "#145C14", "#0B4418", "#60A060",
            "#4F8F4F", "#3F7F50", "#FF6600", "#CC3300",
        };
        foreach (var hex in hexCodes)
            Assert.Contains(hex, text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Run script content
    // -----------------------------------------------------------------------

    [Fact]
    public void RunScript_ContainsInstallParameter()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("-Install", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_DefaultDoesNotInstall()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("if (-not $Install)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_ContainsMapId()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("deadmtl_worldgen_palette_probe_v1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_InstallDestinationAfterGuard()
    {
        var text           = File.ReadAllText(RunScript);
        var guardIdx       = text.IndexOf("if (-not $Install)", StringComparison.Ordinal);
        var installDestIdx = text.IndexOf("pzmapforge_build42_candidate_v4_001", StringComparison.Ordinal);

        Assert.True(installDestIdx > guardIdx,
            "Install destination appears before the -Install guard");
    }

    [Fact]
    public void RunScript_ContainsVerdictString()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("MAP21A_WORLDGEN_PALETTE_PROOF_INSTALLED_RESTART_REQUIRED",
            text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Main DeadMTL palette JSON — expanded to 12 entries
    // -----------------------------------------------------------------------

    [Fact]
    public void MainPalette_HasTwelveEntries()
    {
        using var doc = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(PalettesDir, "worldgen-png-palette.json")));
        var count = doc.RootElement.GetProperty("entries").GetArrayLength();
        Assert.Equal(12, count);
    }

    [Theory]
    [InlineData("#0000FF", "biome",  "water")]
    [InlineData("#D8C080", "biome",  "sand_bank")]
    [InlineData("#00AA00", "biome",  "grass_plain")]
    [InlineData("#55CC55", "biome",  "flower_plain")]
    [InlineData("#207020", "biome",  "birch_forest")]
    [InlineData("#145C14", "biome",  "oak_forest")]
    [InlineData("#0B4418", "biome",  "pine_forest")]
    [InlineData("#60A060", "biome",  "light_birch_forest")]
    [InlineData("#4F8F4F", "biome",  "light_oak_forest")]
    [InlineData("#3F7F50", "biome",  "light_pine_forest")]
    [InlineData("#FF6600", "prefab", "normal_road_WE_00")]
    [InlineData("#CC3300", "prefab", "highway_NS_00")]
    public void MainPalette_ContainsExpectedEntry(string hex, string type, string key)
    {
        using var doc = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(PalettesDir, "worldgen-png-palette.json")));
        var entries = doc.RootElement.GetProperty("entries").EnumerateArray();
        var found   = entries.Any(e =>
            string.Equals(e.GetProperty("color").GetString(), hex,  StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.GetProperty("type").GetString(),  type, StringComparison.Ordinal) &&
            string.Equals(e.GetProperty("key").GetString(),   key,  StringComparison.Ordinal));
        Assert.True(found, $"Palette missing: {hex} {type}:{key}");
    }

    // -----------------------------------------------------------------------
    // Docs
    // -----------------------------------------------------------------------

    [Fact]
    public void PaletteProofDoc_Exists()
    {
        var path = Path.Combine(DocsDir, "DEADMTL_WORLDGEN_PALETTE_PROOF.md");
        Assert.True(File.Exists(path), $"Expected: {path}");
    }

    [Fact]
    public void PaletteProofDoc_ContainsProofChain()
    {
        var text = File.ReadAllText(Path.Combine(DocsDir, "DEADMTL_WORLDGEN_PALETTE_PROOF.md"));
        Assert.Contains("Proof chain", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PaletteProofDoc_DistinguishesCompilerAndVisual()
    {
        var text = File.ReadAllText(Path.Combine(DocsDir, "DEADMTL_WORLDGEN_PALETTE_PROOF.md"));
        Assert.Contains("VISUAL_CONFIRMED", text, StringComparison.Ordinal);
        Assert.Contains("KNOWN_IN_CODE",    text, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadmeContainsPaletteChartsSection()
    {
        var readme = File.ReadAllText(Path.Combine(
            RepoRoot, "examples", "deadmtl-layer-pack", "README.md"));
        Assert.Contains("Palette charts and color proof", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadmeContainsRunWorldgenPaletteProofScript()
    {
        var readme = File.ReadAllText(Path.Combine(
            RepoRoot, "examples", "deadmtl-layer-pack", "README.md"));
        Assert.Contains("run-worldgen-palette-proof-build.ps1", readme, StringComparison.Ordinal);
    }
}
