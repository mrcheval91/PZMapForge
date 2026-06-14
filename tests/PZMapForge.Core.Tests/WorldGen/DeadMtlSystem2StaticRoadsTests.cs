using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlSystem2StaticRoadsTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string DocsDir =>
        Path.Combine(RepoRoot, "docs", "authoring");

    private static string ExamplesDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack");

    private static string System2Doc =>
        Path.Combine(DocsDir, "DEADMTL_SYSTEM2_STATIC_ROADS_ALLEYS_OVERLAY.md");

    private static string ContractJson =>
        Path.Combine(ExamplesDir, "system2-static-road-overlay-contract.json");

    private static string IntentPalette =>
        Path.Combine(ExamplesDir, "palettes", "system2-static-road-intent-palette.json");

    private static string PlaceholderScript =>
        Path.Combine(ExamplesDir, "scripts", "generate-system2-static-road-placeholders.ps1");

    private static string Readme =>
        Path.Combine(ExamplesDir, "README.md");

    // -----------------------------------------------------------------------
    // System 2 doc existence and content
    // -----------------------------------------------------------------------

    [Fact]
    public void System2Doc_Exists() =>
        Assert.True(File.Exists(System2Doc), $"Expected: {System2Doc}");

    [Fact]
    public void System2Doc_MentionsMap22B() =>
        Assert.Contains("MAP-22B", File.ReadAllText(System2Doc), StringComparison.Ordinal);

    [Fact]
    public void System2Doc_MentionsMap22C() =>
        Assert.Contains("MAP-22C", File.ReadAllText(System2Doc), StringComparison.Ordinal);

    [Fact]
    public void System2Doc_Mentions1pxPerMeter()
    {
        var text = File.ReadAllText(System2Doc);
        Assert.True(
            text.Contains("1 px = 1 meter", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("1 pixel = 1 meter", StringComparison.OrdinalIgnoreCase),
            "doc should mention 1 px = 1 meter authoring scale");
    }

    [Fact]
    public void System2Doc_MentionsAlleyRuelle() =>
        Assert.Contains("alley_ruelle", File.ReadAllText(System2Doc), StringComparison.Ordinal);

    [Fact]
    public void System2Doc_MentionsNoLotpackWriter()
    {
        var text = File.ReadAllText(System2Doc);
        Assert.True(
            text.Contains("no .lotpack", StringComparison.OrdinalIgnoreCase) ||
            text.Contains(".lotpack writer", StringComparison.OrdinalIgnoreCase),
            "doc should state no .lotpack writer");
    }

    [Fact]
    public void System2Doc_MentionsNoRuntimeProof()
    {
        var text = File.ReadAllText(System2Doc);
        Assert.True(
            text.Contains("no runtime proof", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("not runtime-proven", StringComparison.OrdinalIgnoreCase),
            "doc should state no runtime proof claimed");
    }

    // -----------------------------------------------------------------------
    // System 2 overlay contract JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void ContractJson_Exists() =>
        Assert.True(File.Exists(ContractJson), $"Expected: {ContractJson}");

    [Fact]
    public void ContractJson_ContainsContractOnly() =>
        Assert.Contains("CONTRACT_ONLY", File.ReadAllText(ContractJson), StringComparison.Ordinal);

    [Fact]
    public void ContractJson_ContainsStaticRoadsAlleys() =>
        Assert.Contains("static_roads_alleys", File.ReadAllText(ContractJson), StringComparison.Ordinal);

    [Fact]
    public void ContractJson_AlleysIsSystemTwoRequired()
    {
        var text = File.ReadAllText(ContractJson);
        var idx  = text.IndexOf("static_roads_alleys", StringComparison.Ordinal);
        Assert.True(idx >= 0, "static_roads_alleys not found in contract JSON");
        var snippet = text.Substring(idx, Math.Min(300, text.Length - idx));
        Assert.Contains("SYSTEM_2_REQUIRED", snippet, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractJson_ContainsNotRuntimeProven() =>
        Assert.Contains("NOT_RUNTIME_PROVEN", File.ReadAllText(ContractJson), StringComparison.Ordinal);

    [Fact]
    public void ContractJson_ContainsSixLayers()
    {
        var text  = File.ReadAllText(ContractJson);
        var count = 0;
        var search = "\"id\"";
        var start  = 0;
        while ((start = text.IndexOf(search, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += search.Length;
        }
        Assert.Equal(6, count);
    }

    // -----------------------------------------------------------------------
    // System 2 intent palette
    // -----------------------------------------------------------------------

    [Fact]
    public void IntentPalette_Exists() =>
        Assert.True(File.Exists(IntentPalette), $"Expected: {IntentPalette}");

    [Fact]
    public void IntentPalette_ContainsLocalStreetAsphalt() =>
        Assert.Contains("local_street_asphalt", File.ReadAllText(IntentPalette), StringComparison.Ordinal);

    [Fact]
    public void IntentPalette_ContainsAlleyRuelleAsphalt() =>
        Assert.Contains("alley_ruelle_asphalt", File.ReadAllText(IntentPalette), StringComparison.Ordinal);

    [Fact]
    public void IntentPalette_ContainsContractOnly() =>
        Assert.Contains("CONTRACT_ONLY", File.ReadAllText(IntentPalette), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Placeholder generator script
    // -----------------------------------------------------------------------

    [Fact]
    public void PlaceholderScript_Exists() =>
        Assert.True(File.Exists(PlaceholderScript), $"Expected: {PlaceholderScript}");

    [Fact]
    public void PlaceholderScript_DoesNotContainCompileWorldgen()
    {
        var text = File.ReadAllText(PlaceholderScript);
        Assert.DoesNotContain("compile-worldgen", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlaceholderScript_DoesNotContainWorldGenOverrideLua()
    {
        var text = File.ReadAllText(PlaceholderScript);
        Assert.DoesNotContain("WorldGenOverride.lua", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaceholderScript_ContainsContractOnlyVerdict()
    {
        var text = File.ReadAllText(PlaceholderScript);
        Assert.Contains("MAP22D_SYSTEM2_STATIC_ROADS_ALLEYS_OVERLAY_CONTRACT_COMPLETE",
            text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // README
    // -----------------------------------------------------------------------

    [Fact]
    public void Readme_MentionsSystem2PlaceholderScript() =>
        Assert.Contains("generate-system2-static-road-placeholders.ps1",
            File.ReadAllText(Readme), StringComparison.Ordinal);
}
