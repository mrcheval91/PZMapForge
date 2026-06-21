using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderLotheaderHexDisassemblyBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36c-core-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderLotheaderHexDisassemblyBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string OutputRoot  => _tempDir;
    private string MinimalPath => Path.Combine(_tempDir, "minimal.lotheader");
    private string VisiblePath => Path.Combine(_tempDir, "visible.lotheader");

    // Fixture: 10-byte common prefix (0x11), then 30 bytes diff, then 10-byte suffix (0xFF)
    // minimal = [0x11×10][0xAA×30][0xFF×10] = 50 bytes
    // visible = [0x11×10][0xBB×50][0xFF×10] = 70 bytes
    // prefix=10, suffix=10, delta=20 (visible is larger)
    private void WriteFixtureFiles()
    {
        var minimal = Enumerable.Repeat((byte)0x11, 10)
            .Concat(Enumerable.Repeat((byte)0xAA, 30))
            .Concat(Enumerable.Repeat((byte)0xFF, 10))
            .ToArray();
        var visible = Enumerable.Repeat((byte)0x11, 10)
            .Concat(Enumerable.Repeat((byte)0xBB, 50))
            .Concat(Enumerable.Repeat((byte)0xFF, 10))
            .ToArray();
        File.WriteAllBytes(MinimalPath, minimal);
        File.WriteAllBytes(VisiblePath, visible);
    }

    private static DeadMtlWorldBuilderLotheaderHexDisassemblyBuilder NewBuilder()
        => new DeadMtlWorldBuilderLotheaderHexDisassemblyBuilder();

    // -----------------------------------------------------------------------
    // MAP36C_CORE_1: Valid build with fixture files produces IsValid=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Core1_ValidBuildWithFixtureFilesIsValid()
    {
        WriteFixtureFiles();
        var result = NewBuilder().Build(MinimalPath, VisiblePath, OutputRoot);
        Assert.True(result.IsValid,
            $"Expected IsValid=true; Verdict={result.Verdict}; Errors=[{string.Join(";", result.Errors)}]; " +
            $"Failed checks: [{string.Join(";", result.Checks.Where(c => c.CheckStatus == "FAIL").Select(c => c.CheckId))}]");
        Assert.All(result.Checks, c => Assert.Equal("PASS", c.CheckStatus));
    }

    // -----------------------------------------------------------------------
    // MAP36C_CORE_2: Diff runs detected when files differ
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Core2_DiffRunsDetectedWhenFilesExist()
    {
        WriteFixtureFiles();
        var result = NewBuilder().Build(MinimalPath, VisiblePath, OutputRoot);
        Assert.True(result.DiffRuns.Count > 0,
            $"Expected at least 1 diff run, got {result.DiffRuns.Count}");
        // Must have at least one DIFF or EXPANSION run
        Assert.True(result.DiffRuns.Any(r => r.RunType is "DIFF" or "EXPANSION"),
            "Expected at least one DIFF or EXPANSION run");
    }

    // -----------------------------------------------------------------------
    // MAP36C_CORE_3: Common prefix detected when leading bytes match
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Core3_CommonPrefixDetectedWithMatchingLeadBytes()
    {
        WriteFixtureFiles(); // prefix = 10 bytes (0x11 × 10)
        var result = NewBuilder().Build(MinimalPath, VisiblePath, OutputRoot);
        Assert.True(result.CommonPrefixLength >= 10,
            $"Expected common_prefix_length >= 10, got {result.CommonPrefixLength}");
    }

    // -----------------------------------------------------------------------
    // MAP36C_CORE_4: Common suffix detected when trailing bytes match
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Core4_CommonSuffixDetectedWithMatchingTrailBytes()
    {
        WriteFixtureFiles(); // suffix = 10 bytes (0xFF × 10)
        var result = NewBuilder().Build(MinimalPath, VisiblePath, OutputRoot);
        Assert.True(result.CommonSuffixLength >= 10,
            $"Expected common_suffix_length >= 10, got {result.CommonSuffixLength}");
    }

    // -----------------------------------------------------------------------
    // MAP36C_CORE_5: Claim boundary always false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Core5_ClaimBoundaryAllFalse()
    {
        // Claim boundary is independent of file availability
        var result = NewBuilder().Build(string.Empty, string.Empty, OutputRoot);
        Assert.False(result.RuntimeBinaryWritten,    "runtime_binary_written must be false");
        Assert.False(result.GeometryInjected,        "geometry_injected must be false");
        Assert.False(result.PlayableExportClaimed,   "playable_export_claimed must be false");
        Assert.False(result.WorkshopUploadPerformed, "workshop_upload_performed must be false");
        Assert.False(result.SteamInstallWrite,       "steam_install_write must be false");
    }

    // -----------------------------------------------------------------------
    // MAP36C_CORE_6: Missing minimal file makes result invalid
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36C_Core6_MinimalFileNotFoundMakesInvalid()
    {
        // Only provide visible file — minimal is missing
        var visible = Enumerable.Repeat((byte)0xBB, 100).ToArray();
        File.WriteAllBytes(VisiblePath, visible);

        var result = NewBuilder().Build(MinimalPath, VisiblePath, OutputRoot);
        Assert.False(result.MinimalLotheaderFound, "minimal_lotheader_found should be false");
        Assert.False(result.IsValid, "IsValid should be false when minimal file is missing");
        Assert.NotEmpty(result.Errors);
    }
}
