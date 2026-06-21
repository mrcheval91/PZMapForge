using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderGeometryToBinaryBridgePlanBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36b-core-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderGeometryToBinaryBridgePlanBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string OutputRoot => Path.Combine(_tempDir, "out.local");

    private static DeadMtlWorldBuilderGeometryToBinaryBridgePlanBuilder NewBuilder()
        => new DeadMtlWorldBuilderGeometryToBinaryBridgePlanBuilder();

    private string WriteFakeEmitterJson(bool emitsBinary = false, bool sandboxOnly = true)
    {
        string path = Path.Combine(_tempDir, "fake-emitter.json");
        File.WriteAllText(path, $@"{{
  ""emitted_operation_count"": 3,
  ""emitted_operation_records"": [
    {{
      ""emits_binary_file"": {emitsBinary.ToString().ToLowerInvariant()},
      ""sandbox_only"": {sandboxOnly.ToString().ToLowerInvariant()}
    }}
  ]
}}");
        return path;
    }

    // -----------------------------------------------------------------------
    // MAP36B_CORE_1: Valid build with no external files produces IsValid=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Core1_ValidBuildIsValidWithAllChecksPass()
    {
        var result = NewBuilder().Build(string.Empty, string.Empty, OutputRoot);
        Assert.True(result.IsValid, $"Expected IsValid=true but got Verdict={result.Verdict}, Errors=[{string.Join(";", result.Errors)}]");
        Assert.All(result.Checks, c => Assert.Equal("PASS", c.CheckStatus));
    }

    // -----------------------------------------------------------------------
    // MAP36B_CORE_2: RequiredUnknownCount >= 6
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Core2_RequiredUnknownCountAtLeastSix()
    {
        var result = NewBuilder().Build(string.Empty, string.Empty, OutputRoot);
        Assert.True(result.RequiredUnknownCount >= 6,
            $"Expected >= 6 required unknowns, got {result.RequiredUnknownCount}");
        Assert.True(result.RequiredUnknownFields.Count >= 6);
    }

    // -----------------------------------------------------------------------
    // MAP36B_CORE_3: CandidateExperimentCount >= 4
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Core3_CandidateExperimentCountAtLeastFour()
    {
        var result = NewBuilder().Build(string.Empty, string.Empty, OutputRoot);
        Assert.True(result.CandidateExperimentCount >= 4,
            $"Expected >= 4 candidate experiments, got {result.CandidateExperimentCount}");
        Assert.True(result.CandidateExperiments.Count >= 4);
    }

    // -----------------------------------------------------------------------
    // MAP36B_CORE_4: Claim boundary all false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Core4_ClaimBoundaryAllFalse()
    {
        var result = NewBuilder().Build(string.Empty, string.Empty, OutputRoot);
        Assert.False(result.RuntimeBinaryWritten,    "runtime_binary_written must be false");
        Assert.False(result.GeometryInjected,        "geometry_injected must be false");
        Assert.False(result.PlayableExportClaimed,   "playable_export_claimed must be false");
        Assert.False(result.WorkshopUploadPerformed, "workshop_upload_performed must be false");
        Assert.False(result.SteamInstallWrite,       "steam_install_write must be false");
    }

    // -----------------------------------------------------------------------
    // MAP36B_CORE_5: Source path with Dru marker is rejected
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Core5_SourcePathWithDruIsRejected()
    {
        string rejectedPath = Path.Combine(_tempDir, "Dru_source.local", "emitter.json");
        var result = NewBuilder().Build(string.Empty, rejectedPath, OutputRoot);
        Assert.True(result.SourceRejected, "Expected SourceRejected=true for Dru path");
        Assert.False(result.IsValid, "Rejected source must make IsValid=false");
        Assert.NotEmpty(result.SourceRejectionReason);
    }

    // -----------------------------------------------------------------------
    // MAP36B_CORE_6: Source path with "workshop donor" is rejected
    // -----------------------------------------------------------------------

    [Fact]
    public void Map36B_Core6_SourcePathWithWorkshopDonorIsRejected()
    {
        string rejectedPath = Path.Combine(_tempDir, "workshop donor.local", "emitter.json");
        var result = NewBuilder().Build(string.Empty, rejectedPath, OutputRoot);
        Assert.True(result.SourceRejected, "Expected SourceRejected=true for 'workshop donor' path");
        Assert.False(result.IsValid, "Rejected source must make IsValid=false");
    }
}
