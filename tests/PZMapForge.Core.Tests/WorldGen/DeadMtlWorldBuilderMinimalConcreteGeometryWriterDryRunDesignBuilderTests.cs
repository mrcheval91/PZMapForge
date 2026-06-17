using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-dry-run-design", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteScopeRecordJson(
        string verdict = "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE",
        bool isValid = true,
        string futureStatus = "NOT_AUTHORIZED",
        string gateStatus = "LOCKED_PENDING_OPERATOR_APPROVAL")
    {
        var path = Path.Combine(_tempDir, "scope.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-experiment-scope-record.v1",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            is_valid = isValid,
            verdict = verdict,
            future_experiment_status = futureStatus,
            writer_experiment_gate_status = gateStatus,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private string WriteManifestJson()
    {
        var path = Path.Combine(_tempDir, "manifest.json");
        var obj = new
        {
            format  = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-input-manifest.v1",
            map_id  = "map_00",
            verdict = "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private string WriteGeometryMvpJson()
    {
        var path = Path.Combine(_tempDir, "mvp.json");
        var obj = new
        {
            format  = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1",
            tile_id = "map_00",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult BuildValid()
    {
        var scope    = WriteScopeRecordJson();
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        return new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder()
            .Build(scope, manifest, mvp);
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult BuildWith(
        string? scopeVerdict = null,
        bool scopeIsValid = true,
        string? futureStatus = null,
        string? gateStatus = null)
    {
        var scope    = WriteScopeRecordJson(
            scopeVerdict ?? "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE",
            scopeIsValid,
            futureStatus ?? "NOT_AUTHORIZED",
            gateStatus   ?? "LOCKED_PENDING_OPERATOR_APPROVAL");
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        return new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder()
            .Build(scope, manifest, mvp);
    }

    // -----------------------------------------------------------------------
    // Guard tests — missing inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingScopeRecord_IsInvalid()
    {
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder()
            .Build("__nonexistent_scope__.json", manifest, mvp);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingManifest_IsInvalid()
    {
        var scope = WriteScopeRecordJson();
        var mvp   = WriteGeometryMvpJson();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder()
            .Build(scope, "__nonexistent_manifest__.json", mvp);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingGeometryMvp_IsInvalid()
    {
        var scope    = WriteScopeRecordJson();
        var manifest = WriteManifestJson();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder()
            .Build(scope, manifest, "__nonexistent_mvp__.json");
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MalformedScopeRecord_IsInvalid()
    {
        var bad = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(bad, "NOT JSON {{{{");
        var manifest = WriteManifestJson();
        var mvp      = WriteGeometryMvpJson();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder()
            .Build(bad, manifest, mvp);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Check-level failures
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WrongMap26EVerdict_IsInvalid()
    {
        var result = BuildWith(scopeVerdict: "WRONG_VERDICT");
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26E_VERDICT_COMPLETE").CheckStatus);
    }

    [Fact]
    public void Build_Map26EIsValidFalse_IsInvalid()
    {
        var result = BuildWith(scopeIsValid: false);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26E_IS_VALID_TRUE").CheckStatus);
    }

    [Fact]
    public void Build_Map26EFutureStatusNotAuthorized_WrongValue_IsInvalid()
    {
        var result = BuildWith(futureStatus: "AUTHORIZED");
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26E_FUTURE_STATUS_NOT_AUTHORIZED").CheckStatus);
    }

    [Fact]
    public void Build_Map26EGateNotLocked_IsInvalid()
    {
        var result = BuildWith(gateStatus: "UNLOCKED");
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26E_GATE_LOCKED").CheckStatus);
    }

    // -----------------------------------------------------------------------
    // Valid build — structural
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_IsValid()
    {
        var result = BuildValid();
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void ValidFixture_NoErrors()
    {
        Assert.Empty(BuildValid().Errors);
    }

    [Fact]
    public void ValidFixture_AllChecksPass()
    {
        var result  = BuildValid();
        var failing = result.Checks.Where(c => c.CheckStatus != "PASS").Select(c => c.CheckId).ToList();
        Assert.Empty(failing);
    }

    [Fact]
    public void ValidFixture_DesignCheckCount_Is21()
    {
        Assert.Equal(21, BuildValid().DesignCheckCount);
    }

    [Fact]
    public void ValidFixture_PassedDesignCheckCount_Is21()
    {
        Assert.Equal(21, BuildValid().PassedDesignCheckCount);
    }

    [Fact]
    public void ValidFixture_FailedDesignCheckCount_Is0()
    {
        Assert.Equal(0, BuildValid().FailedDesignCheckCount);
    }

    // -----------------------------------------------------------------------
    // Valid build — claim boundary flags
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_DryRunOnly_IsTrue()
    {
        Assert.True(BuildValid().DryRunOnly);
    }

    [Fact]
    public void ValidFixture_WriterReady_IsFalse()
    {
        Assert.False(BuildValid().WriterReady);
    }

    [Fact]
    public void ValidFixture_RuntimeValid_IsFalse()
    {
        Assert.False(BuildValid().RuntimeValid);
    }

    [Fact]
    public void ValidFixture_Materialized_IsFalse()
    {
        Assert.False(BuildValid().Materialized);
    }

    [Fact]
    public void ValidFixture_ApprovedForWriterExperiment_IsFalse()
    {
        Assert.False(BuildValid().ApprovedForWriterExperiment);
    }

    // -----------------------------------------------------------------------
    // Valid build — lists
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_PlannedOutputRecords_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().PlannedOutputRecords);
    }

    [Fact]
    public void ValidFixture_SandboxConstraints_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().SandboxConstraints);
    }

    [Fact]
    public void ValidFixture_ForbiddenOutputGuards_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().ForbiddenOutputGuards);
    }

    [Fact]
    public void ValidFixture_RollbackChecks_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().RollbackChecks);
    }

    [Fact]
    public void ValidFixture_PlannedOutputRecords_AllStatusDesignOnly()
    {
        var result = BuildValid();
        Assert.All(result.PlannedOutputRecords,
            r => Assert.Equal("DESIGN_ONLY_NOT_EMITTED", r.Status));
    }

    [Fact]
    public void ValidFixture_Verdict_IsComplete()
    {
        Assert.Equal(
            "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE",
            BuildValid().Verdict);
    }

    [Fact]
    public void ValidFixture_SourceScopeRecordSha256_Is64HexChars()
    {
        Assert.Equal(64, BuildValid().SourceScopeRecordSha256.Length);
    }

    [Fact]
    public void ValidFixture_SourceManifestSha256_Is64HexChars()
    {
        Assert.Equal(64, BuildValid().SourceManifestSha256.Length);
    }

    [Fact]
    public void ValidFixture_SourceGeometryMvpSha256_Is64HexChars()
    {
        Assert.Equal(64, BuildValid().SourceGeometryMvpSha256.Length);
    }

    // -----------------------------------------------------------------------
    // Render tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_Has21DataRows()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder();
        var csv     = builder.RenderCsv(result);
        var lines   = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(22, lines.Length); // 1 header + 21 data rows
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder();
        Assert.Contains("Claim Boundary", builder.RenderMarkdown(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsDesignOnlyNotAuthorized()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder();
        Assert.Contains("DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME", builder.RenderMarkdown(result));
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder();
        Assert.Contains("MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE",
            builder.RenderSummary(result));
    }

    [Fact]
    public void RenderSummary_ContainsDryRunOnly1()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder();
        Assert.Contains("dry_run_only                    : 1", builder.RenderSummary(result));
    }

    [Fact]
    public void RenderJson_ContainsDryRunOnlyTrue()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder();
        Assert.Contains("\"dry_run_only\": true", builder.RenderJson(result), StringComparison.Ordinal);
    }
}
