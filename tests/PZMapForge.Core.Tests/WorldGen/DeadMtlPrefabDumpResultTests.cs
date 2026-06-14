using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlPrefabDumpResultTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string DocsDir =>
        Path.Combine(RepoRoot, "docs", "authoring");

    private static string ResultTxt =>
        Path.Combine(DocsDir, "MAP22B_PREFAB_DUMP_RESULT.txt");

    private static string DiscoveryDoc =>
        Path.Combine(DocsDir, "DEADMTL_WORLDGEN_PREFAB_DISCOVERY.md");

    private static string SmallRoadsDoc =>
        Path.Combine(DocsDir, "DEADMTL_SMALL_ROADS_ALLEYS_CONTRACT.md");

    private static string ContractJson =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "road-layers-contract.json");

    // -----------------------------------------------------------------------
    // Result sidecar existence and content
    // -----------------------------------------------------------------------

    [Fact]
    public void PrefabDumpResult_Exists() =>
        Assert.True(File.Exists(ResultTxt), $"Expected: {ResultTxt}");

    [Fact]
    public void PrefabDumpResult_IsAsciiOnly()
    {
        var bytes = File.ReadAllBytes(ResultTxt);
        Assert.False(bytes.Any(b => b > 127), "MAP22B_PREFAB_DUMP_RESULT.txt must be ASCII-only");
    }

    [Fact]
    public void PrefabDumpResult_ContainsDumpLoaded()
    {
        var text = File.ReadAllText(ResultTxt);
        Assert.Contains("PZMAPFORGE_PREFAB_DUMP_LOADED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PrefabDumpResult_ContainsHighwayNS00()
    {
        var text = File.ReadAllText(ResultTxt);
        Assert.Contains("PZMAPFORGE_PREFAB_KEY=highway_NS_00", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PrefabDumpResult_ContainsNormalRoadWE00()
    {
        var text = File.ReadAllText(ResultTxt);
        Assert.Contains("PZMAPFORGE_PREFAB_KEY=normal_road_WE_00", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PrefabDumpResult_ContainsPrefabCount2()
    {
        var text = File.ReadAllText(ResultTxt);
        Assert.Contains("PZMAPFORGE_PREFAB_COUNT=2", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PrefabDumpResult_ContainsRoadLikeCount2()
    {
        var text = File.ReadAllText(ResultTxt);
        Assert.Contains("PZMAPFORGE_ROADLIKE_PREFAB_COUNT=2", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PrefabDumpResult_ContainsVerdict()
    {
        var text = File.ReadAllText(ResultTxt);
        Assert.Contains(
            "VERDICT: MAP22B_WORLDGEN_PREFAB_DISCOVERY_RUNTIME_CONFIRMED_ONLY_TWO_PREFABS",
            text, StringComparison.Ordinal);
    }

    [Fact]
    public void PrefabDumpResult_MentionsSystemTwoRequired()
    {
        var text = File.ReadAllText(ResultTxt);
        Assert.Contains("SYSTEM_2_REQUIRED", text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Discovery doc reflects the runtime result
    // -----------------------------------------------------------------------

    [Fact]
    public void DiscoveryDoc_MentionsRuntimeResult()
    {
        var text = File.ReadAllText(DiscoveryDoc);
        Assert.Contains("MAP-22B runtime result", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiscoveryDoc_MentionsPrefabCount2()
    {
        var text = File.ReadAllText(DiscoveryDoc);
        Assert.Contains("PZMAPFORGE_PREFAB_COUNT=2", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscoveryDoc_MentionsMap22cVerdict()
    {
        var text = File.ReadAllText(DiscoveryDoc);
        Assert.Contains("MAP22C_WORLDGEN_SMALL_ROAD_PATH_CLOSED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscoveryDoc_MentionsSystemTwoRequired()
    {
        var text = File.ReadAllText(DiscoveryDoc);
        Assert.Contains("SYSTEM_2_REQUIRED", text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Small roads contract doc reflects the runtime result
    // -----------------------------------------------------------------------

    [Fact]
    public void SmallRoadsContractDoc_MentionsMap22BResult()
    {
        var text = File.ReadAllText(SmallRoadsDoc);
        Assert.Contains("MAP-22B result", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SmallRoadsContractDoc_MentionsMap22DPath()
    {
        var text = File.ReadAllText(SmallRoadsDoc);
        Assert.Contains("MAP-22D", text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Contract JSON notes mention MAP-22B finding
    // -----------------------------------------------------------------------

    [Fact]
    public void ContractJson_AlleyNotesMap22B()
    {
        var text = File.ReadAllText(ContractJson);
        var idx = text.IndexOf("roads_alleys", StringComparison.Ordinal);
        Assert.True(idx >= 0, "roads_alleys not found in contract JSON");
        var snippet = text.Substring(idx, Math.Min(300, text.Length - idx));
        Assert.Contains("MAP-22B", snippet, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractJson_LocalNotesMap22B()
    {
        var text = File.ReadAllText(ContractJson);
        var idx = text.IndexOf("roads_local", StringComparison.Ordinal);
        Assert.True(idx >= 0, "roads_local not found in contract JSON");
        var snippet = text.Substring(idx, Math.Min(300, text.Length - idx));
        Assert.Contains("MAP-22B", snippet, StringComparison.Ordinal);
    }
}
