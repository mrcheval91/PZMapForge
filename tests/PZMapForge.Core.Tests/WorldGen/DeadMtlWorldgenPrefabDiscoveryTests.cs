using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldgenPrefabDiscoveryTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ScriptsDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts");

    private static string DocsDir =>
        Path.Combine(RepoRoot, "docs", "authoring");

    private static string GeneratorScript =>
        Path.Combine(ScriptsDir, "generate-worldgen-prefab-dump-lua.ps1");

    private static string RunScript =>
        Path.Combine(ScriptsDir, "run-worldgen-prefab-dump.ps1");

    private static string HarvestScript =>
        Path.Combine(ScriptsDir, "harvest-worldgen-prefab-dump-logs.ps1");

    private static string DiscoveryDoc =>
        Path.Combine(DocsDir, "DEADMTL_WORLDGEN_PREFAB_DISCOVERY.md");

    private static string Readme =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");

    // -----------------------------------------------------------------------
    // Existence
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorScript_Exists() =>
        Assert.True(File.Exists(GeneratorScript), $"Expected: {GeneratorScript}");

    [Fact]
    public void RunScript_Exists() =>
        Assert.True(File.Exists(RunScript), $"Expected: {RunScript}");

    [Fact]
    public void HarvestScript_Exists() =>
        Assert.True(File.Exists(HarvestScript), $"Expected: {HarvestScript}");

    [Fact]
    public void DiscoveryDoc_Exists() =>
        Assert.True(File.Exists(DiscoveryDoc), $"Expected: {DiscoveryDoc}");

    // -----------------------------------------------------------------------
    // Generator script content
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorScript_ContainsPrefabDumpLoaded()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("PZMAPFORGE_PREFAB_DUMP_LOADED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_ContainsPrefabKeyMarker()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("PZMAPFORGE_PREFAB_KEY=", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_ContainsRoadLikePrefabKeyMarker()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("PZMAPFORGE_ROADLIKE_PREFAB_KEY=", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_ContainsRoadFilter()
    {
        var text = File.ReadAllText(GeneratorScript);
        Assert.Contains("road",    text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alley",   text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("highway", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lane",    text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path",    text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Run script content
    // -----------------------------------------------------------------------

    [Fact]
    public void RunScript_HasInstallGuard()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("if (-not $Install)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_ContainsVerdictString()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("MAP22B_WORLDGEN_PREFAB_DUMP_INSTALLED_RESTART_REQUIRED",
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
    // Harvest script content
    // -----------------------------------------------------------------------

    [Fact]
    public void HarvestScript_ReferencesHarvestFile()
    {
        var text = File.ReadAllText(HarvestScript);
        Assert.Contains("prefab-dump-harvest.txt", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HarvestScript_SearchesPrefabKeyMarker()
    {
        var text = File.ReadAllText(HarvestScript);
        Assert.Contains("PZMAPFORGE_PREFAB_KEY=", text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Discovery doc content
    // -----------------------------------------------------------------------

    [Fact]
    public void DiscoveryDoc_StatesNotVisualProof()
    {
        var text = File.ReadAllText(DiscoveryDoc);
        Assert.True(
            text.Contains("not a visual proof", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("does not prove", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("not prove any prefab visually", StringComparison.OrdinalIgnoreCase),
            "Discovery doc should state that the probe is not a visual proof");
    }

    [Fact]
    public void DiscoveryDoc_RequiresVisualProofForDiscoveredKeys()
    {
        var text = File.ReadAllText(DiscoveryDoc);
        Assert.True(
            text.Contains("VISUAL_CONFIRMED", StringComparison.Ordinal) ||
            text.Contains("visual confirmation", StringComparison.OrdinalIgnoreCase),
            "Discovery doc should require visual confirmation for discovered keys");
    }

    // -----------------------------------------------------------------------
    // README
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadmeMentionsPrefabDumpScript()
    {
        var text = File.ReadAllText(Readme);
        Assert.Contains("run-worldgen-prefab-dump.ps1", text, StringComparison.Ordinal);
    }
}
