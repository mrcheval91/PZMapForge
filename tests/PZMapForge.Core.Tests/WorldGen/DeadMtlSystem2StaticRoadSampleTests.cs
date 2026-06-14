using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlSystem2StaticRoadSampleTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ScriptsDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts");

    private static string DocsDir =>
        Path.Combine(RepoRoot, "docs", "authoring");

    private static string ReadmePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");

    private static string SampleScript =>
        Path.Combine(ScriptsDir, "generate-system2-static-road-sample.ps1");

    private static string RunHelperScript =>
        Path.Combine(ScriptsDir, "run-system2-static-road-sample-extract.ps1");

    private static string SampleDoc =>
        Path.Combine(DocsDir, "DEADMTL_SYSTEM2_STATIC_ROAD_SAMPLE_EXTRACT.md");

    // -----------------------------------------------------------------------
    // Existence
    // -----------------------------------------------------------------------

    [Fact]
    public void SampleScript_Exists() =>
        Assert.True(File.Exists(SampleScript), $"Expected: {SampleScript}");

    [Fact]
    public void RunHelperScript_Exists() =>
        Assert.True(File.Exists(RunHelperScript), $"Expected: {RunHelperScript}");

    [Fact]
    public void SampleDoc_Exists() =>
        Assert.True(File.Exists(SampleDoc), $"Expected: {SampleDoc}");

    // -----------------------------------------------------------------------
    // Sample generator: forbidden strings
    // -----------------------------------------------------------------------

    [Fact]
    public void SampleScript_DoesNotContainCompileWorldgen() =>
        Assert.DoesNotContain("compile-worldgen", File.ReadAllText(SampleScript), StringComparison.Ordinal);

    [Fact]
    public void SampleScript_DoesNotContainWorldGenOverrideLua() =>
        Assert.DoesNotContain("WorldGenOverride.lua", File.ReadAllText(SampleScript), StringComparison.Ordinal);

    [Fact]
    public void SampleScript_DoesNotContainLotpack() =>
        Assert.DoesNotContain(".lotpack", File.ReadAllText(SampleScript), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Run helper: forbidden strings
    // -----------------------------------------------------------------------

    [Fact]
    public void RunHelperScript_DoesNotContainCompileWorldgen() =>
        Assert.DoesNotContain("compile-worldgen", File.ReadAllText(RunHelperScript), StringComparison.Ordinal);

    [Fact]
    public void RunHelperScript_DoesNotContainWorldGenOverrideLua() =>
        Assert.DoesNotContain("WorldGenOverride.lua", File.ReadAllText(RunHelperScript), StringComparison.Ordinal);

    [Fact]
    public void RunHelperScript_DoesNotContainLotpack() =>
        Assert.DoesNotContain(".lotpack", File.ReadAllText(RunHelperScript), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Sample doc content
    // -----------------------------------------------------------------------

    [Fact]
    public void SampleDoc_MentionsNoRuntimeProof()
    {
        var text = File.ReadAllText(SampleDoc);
        Assert.True(
            text.Contains("not runtime proof", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("NOT_RUNTIME_PROVEN", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Runtime proof is NOT", StringComparison.OrdinalIgnoreCase),
            "doc should state no runtime proof");
    }

    [Fact]
    public void SampleDoc_MentionsNoLotpack()
    {
        var text = File.ReadAllText(SampleDoc);
        Assert.True(
            text.Contains("not write .lotpack", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Does NOT write `.lotpack`", StringComparison.OrdinalIgnoreCase),
            "doc should state no .lotpack");
    }

    // -----------------------------------------------------------------------
    // README
    // -----------------------------------------------------------------------

    [Fact]
    public void Readme_MentionsRunHelperScript() =>
        Assert.Contains("run-system2-static-road-sample-extract.ps1",
            File.ReadAllText(ReadmePath), StringComparison.Ordinal);
}
