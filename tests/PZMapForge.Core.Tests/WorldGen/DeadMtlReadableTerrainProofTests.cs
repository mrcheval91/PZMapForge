using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlReadableTerrainProofTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ScriptsDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts");

    private static string DocsDir =>
        Path.Combine(RepoRoot, "docs", "authoring");

    private static string GeneratorScript =>
        Path.Combine(ScriptsDir, "generate-readable-terrain-proof-pack.ps1");

    private static string RunScript =>
        Path.Combine(ScriptsDir, "run-readable-terrain-proof-build.ps1");

    private static string ProofDoc =>
        Path.Combine(DocsDir, "DEADMTL_READABLE_TERRAIN_PROOF.md");

    private static string Readme =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");

    // -----------------------------------------------------------------------
    // Script and doc existence
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorScript_Exists() =>
        Assert.True(File.Exists(GeneratorScript), $"Expected: {GeneratorScript}");

    [Fact]
    public void RunScript_Exists() =>
        Assert.True(File.Exists(RunScript), $"Expected: {RunScript}");

    [Fact]
    public void ProofDoc_Exists() =>
        Assert.True(File.Exists(ProofDoc), $"Expected: {ProofDoc}");

    // -----------------------------------------------------------------------
    // Generator script content
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorScript_ContainsMapId()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("deadmtl_readable_terrain_proof_v1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_ContainsSpawnWorldX()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("10650", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_ContainsSpawnWorldY()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("8250", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_ContainsSpawnPixelX()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("70", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_ContainsSpawnPixelY()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("50", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_MentionsShapedShoreline()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.True(
            text.Contains("shoreline", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("shaped", StringComparison.OrdinalIgnoreCase),
            "Script should mention shaped shoreline to distinguish from isolated swatch approach");
    }

    [Fact]
    public void GeneratorScript_ReferencesTerrainBiomesPng()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("terrain_biomes.png", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_ReferencesTerrainRoadsPng()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("terrain_roads.png", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("#0000FF")]
    [InlineData("#D8C080")]
    [InlineData("#00AA00")]
    [InlineData("#55CC55")]
    [InlineData("#207020")]
    [InlineData("#145C14")]
    [InlineData("#0B4418")]
    [InlineData("#60A060")]
    [InlineData("#4F8F4F")]
    [InlineData("#3F7F50")]
    [InlineData("#FF6600")]
    [InlineData("#CC3300")]
    public void GeneratorScript_ContainsHexCode(string hex)
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains(hex, text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Run script content
    // -----------------------------------------------------------------------

    [Fact]
    public void RunScript_ContainsMapId()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("deadmtl_readable_terrain_proof_v1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_DefaultDoesNotInstall()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("if (-not $Install)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_ContainsVerdictString()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("MAP21B_READABLE_TERRAIN_PROOF_INSTALLED_RESTART_REQUIRED",
            text, StringComparison.Ordinal);
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

    // -----------------------------------------------------------------------
    // Proof doc content
    // -----------------------------------------------------------------------

    [Fact]
    public void ProofDoc_ExplainsMap21AContext()
    {
        var text = File.ReadAllText(ProofDoc);
        Assert.Contains("MAP-21A", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProofDoc_ExplainsIsolatedWaterAnomaly()
    {
        var text = File.ReadAllText(ProofDoc);
        Assert.True(
            text.Contains("anomaly", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("isolated", StringComparison.OrdinalIgnoreCase),
            "Proof doc should document the isolated water anomaly");
    }

    [Fact]
    public void ProofDoc_MentionsShapedShoreline()
    {
        var text = File.ReadAllText(ProofDoc);
        Assert.True(
            text.Contains("shoreline", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("shaped", StringComparison.OrdinalIgnoreCase),
            "Proof doc should mention shaped shoreline approach");
    }

    // -----------------------------------------------------------------------
    // README
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadmeMentionsReadableTerrainProof()
    {
        var text = File.ReadAllText(Readme);
        Assert.Contains("run-readable-terrain-proof-build.ps1", text, StringComparison.Ordinal);
    }
}
