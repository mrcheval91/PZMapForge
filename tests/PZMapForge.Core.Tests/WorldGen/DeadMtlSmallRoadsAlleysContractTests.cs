using System.Text.Json;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlSmallRoadsAlleysContractTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string DocsDir =>
        Path.Combine(RepoRoot, "docs", "authoring");

    private static string PackDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack");

    private static string ScriptsDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts");

    private static string ContractDoc =>
        Path.Combine(DocsDir, "DEADMTL_SMALL_ROADS_ALLEYS_CONTRACT.md");

    private static string ContractJson =>
        Path.Combine(PackDir, "road-layers-contract.json");

    private static string PlaceholderScript =>
        Path.Combine(ScriptsDir, "generate-road-layer-placeholders.ps1");

    // -----------------------------------------------------------------------
    // Existence
    // -----------------------------------------------------------------------

    [Fact]
    public void ContractDoc_Exists() =>
        Assert.True(File.Exists(ContractDoc), $"Expected: {ContractDoc}");

    [Fact]
    public void ContractJson_Exists() =>
        Assert.True(File.Exists(ContractJson), $"Expected: {ContractJson}");

    [Fact]
    public void PlaceholderScript_Exists() =>
        Assert.True(File.Exists(PlaceholderScript), $"Expected: {PlaceholderScript}");

    // -----------------------------------------------------------------------
    // Contract doc content
    // -----------------------------------------------------------------------

    [Fact]
    public void ContractDoc_Mentions1pxPerMeter()
    {
        var text = File.ReadAllText(ContractDoc);
        Assert.True(
            text.Contains("1 px", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("1px", StringComparison.OrdinalIgnoreCase),
            "Contract doc should mention 1 px = 1 m scale");
        Assert.True(
            text.Contains("meter", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("1 m", StringComparison.OrdinalIgnoreCase),
            "Contract doc should mention meter scale");
    }

    [Fact]
    public void ContractDoc_MentionsAlleyOrRuelle()
    {
        var text = File.ReadAllText(ContractDoc);
        Assert.True(
            text.Contains("alley", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ruelle", StringComparison.OrdinalIgnoreCase),
            "Contract doc should mention alleys or ruelles");
    }

    [Fact]
    public void ContractDoc_MentionsSystemTwoRequired()
    {
        var text = File.ReadAllText(ContractDoc);
        Assert.Contains("SYSTEM_2_REQUIRED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractDoc_MentionsNormalRoadWE00()
    {
        var text = File.ReadAllText(ContractDoc);
        Assert.Contains("normal_road_WE_00", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractDoc_MentionsHighwayNS00()
    {
        var text = File.ReadAllText(ContractDoc);
        Assert.Contains("highway_NS_00", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractDoc_MentionsFutureProofTasks()
    {
        var text = File.ReadAllText(ContractDoc);
        Assert.True(
            text.Contains("MAP-22B", StringComparison.Ordinal) ||
            text.Contains("MAP-22C", StringComparison.Ordinal) ||
            text.Contains("MAP-22D", StringComparison.Ordinal),
            "Contract doc should reference future MAP-22B/C/D proof tasks");
    }

    // -----------------------------------------------------------------------
    // Contract JSON content
    // -----------------------------------------------------------------------

    [Fact]
    public void ContractJson_IsValidJson()
    {
        var text = File.ReadAllText(ContractJson);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public void ContractJson_ContainsRoadsAlleys()
    {
        var text = File.ReadAllText(ContractJson);
        Assert.Contains("roads_alleys", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractJson_RoadsAlleysIsSystemTwoRequired()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ContractJson));
        var layers = doc.RootElement.GetProperty("layers").EnumerateArray();
        var alleyLayer = layers.FirstOrDefault(l =>
            l.TryGetProperty("id", out var id) &&
            id.GetString() == "roads_alleys");

        Assert.True(alleyLayer.ValueKind != JsonValueKind.Undefined,
            "roads_alleys layer not found in contract JSON");
        Assert.Equal("SYSTEM_2_REQUIRED",
            alleyLayer.GetProperty("status").GetString());
    }

    [Fact]
    public void ContractJson_HasFiveRoadLayers()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ContractJson));
        var count = doc.RootElement.GetProperty("layers").GetArrayLength();
        Assert.Equal(5, count);
    }

    // -----------------------------------------------------------------------
    // Placeholder script content
    // -----------------------------------------------------------------------

    [Fact]
    public void PlaceholderScript_DoesNotContainCompileWorldgen()
    {
        var text = File.ReadAllText(PlaceholderScript);
        Assert.DoesNotContain("compile-worldgen", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlaceholderScript_DoesNotContainInstall()
    {
        var text = File.ReadAllText(PlaceholderScript);
        Assert.DoesNotContain("-Install", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaceholderScript_ReferencesRoadsAlleys()
    {
        var text = File.ReadAllText(PlaceholderScript);
        Assert.Contains("roads_alleys.png", text, StringComparison.Ordinal);
    }
}
