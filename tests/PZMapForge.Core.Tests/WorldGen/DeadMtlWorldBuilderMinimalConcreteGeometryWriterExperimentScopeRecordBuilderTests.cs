using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-scope-record", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteManifestJson(
        string verdict = "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE",
        bool isValid = true,
        int inputArtifacts = 8,
        int hashedArtifacts = 8,
        int manifestChecks = 17,
        int passedChecks = 17,
        bool writerReady = false,
        bool runtimeValid = false,
        bool materialized = false,
        bool approvedForWriterExperiment = false,
        string gateStatus = "LOCKED_PENDING_OPERATOR_APPROVAL")
    {
        var path = Path.Combine(_tempDir, "manifest.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-input-manifest.v1",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            input_artifact_count = inputArtifacts,
            hashed_input_artifact_count = hashedArtifacts,
            writer_ready = writerReady,
            runtime_valid = runtimeValid,
            materialized = materialized,
            approved_for_writer_experiment = approvedForWriterExperiment,
            writer_experiment_gate_status = gateStatus,
            manifest_check_count = manifestChecks,
            passed_manifest_check_count = passedChecks,
            failed_manifest_check_count = manifestChecks - passedChecks,
            is_valid = isValid,
            verdict = verdict,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private string WriteSummaryTxt()
    {
        var path = Path.Combine(_tempDir, "summary.txt");
        File.WriteAllText(path, "verdict: MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE\n");
        return path;
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult BuildValid()
    {
        var manifest = WriteManifestJson();
        var summary  = WriteSummaryTxt();
        return new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder()
            .Build(manifest, summary);
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult BuildWith(
        string? verdict = null,
        bool isValid = true,
        int inputArtifacts = 8,
        int hashedArtifacts = 8,
        int passedChecks = 17,
        bool writerReady = false,
        bool runtimeValid = false,
        bool materialized = false,
        bool approved = false,
        string? gateStatus = null)
    {
        var manifest = WriteManifestJson(
            verdict ?? "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE",
            isValid, inputArtifacts, hashedArtifacts, 17, passedChecks,
            writerReady, runtimeValid, materialized, approved,
            gateStatus ?? "LOCKED_PENDING_OPERATOR_APPROVAL");
        var summary = WriteSummaryTxt();
        return new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder()
            .Build(manifest, summary);
    }

    // -----------------------------------------------------------------------
    // Guard tests — missing / malformed input
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingManifestJson_IsInvalid()
    {
        var summary = WriteSummaryTxt();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder()
            .Build("__nonexistent__.json", summary);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingManifestSummary_IsInvalid()
    {
        var manifest = WriteManifestJson();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder()
            .Build(manifest, "__nonexistent__.summary.txt");
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MalformedManifestJson_IsInvalid()
    {
        var bad = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(bad, "NOT JSON {{{{");
        var summary = WriteSummaryTxt();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder()
            .Build(bad, summary);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Check-level failures — wrong values in source manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WrongMap26DVerdict_IsInvalid()
    {
        var result = BuildWith(verdict: "WRONG_VERDICT");
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26D_VERDICT_COMPLETE").CheckStatus);
    }

    [Fact]
    public void Build_Map26DIsValidFalse_IsInvalid()
    {
        var result = BuildWith(isValid: false);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26D_IS_VALID_TRUE").CheckStatus);
    }

    [Fact]
    public void Build_WrongArtifactCount_IsInvalid()
    {
        var result = BuildWith(inputArtifacts: 7);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26D_ARTIFACTS_8_OF_8").CheckStatus);
    }

    [Fact]
    public void Build_WrongHashedArtifactCount_IsInvalid()
    {
        var result = BuildWith(hashedArtifacts: 7);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26D_HASHED_ARTIFACTS_8_OF_8").CheckStatus);
    }

    [Fact]
    public void Build_WrongPassedChecks_IsInvalid()
    {
        var result = BuildWith(passedChecks: 16);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26D_CHECKS_17_OF_17_PASS").CheckStatus);
    }

    [Fact]
    public void Build_WriterReadyTrue_IsInvalid()
    {
        var result = BuildWith(writerReady: true);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "WRITER_READY_FALSE").CheckStatus);
    }

    [Fact]
    public void Build_RuntimeValidTrue_IsInvalid()
    {
        var result = BuildWith(runtimeValid: true);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "RUNTIME_VALID_FALSE").CheckStatus);
    }

    [Fact]
    public void Build_MaterializedTrue_IsInvalid()
    {
        var result = BuildWith(materialized: true);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MATERIALIZED_FALSE").CheckStatus);
    }

    [Fact]
    public void Build_ApprovedTrue_IsInvalid()
    {
        var result = BuildWith(approved: true);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "APPROVED_FOR_WRITER_EXPERIMENT_FALSE").CheckStatus);
    }

    [Fact]
    public void Build_WrongGateStatus_IsInvalid()
    {
        var result = BuildWith(gateStatus: "UNLOCKED");
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "GATE_STATUS_LOCKED").CheckStatus);
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
    public void ValidFixture_ScopeCheckCount_Is17()
    {
        Assert.Equal(17, BuildValid().ScopeCheckCount);
    }

    [Fact]
    public void ValidFixture_PassedScopeCheckCount_Is17()
    {
        Assert.Equal(17, BuildValid().PassedScopeCheckCount);
    }

    [Fact]
    public void ValidFixture_FailedScopeCheckCount_Is0()
    {
        Assert.Equal(0, BuildValid().FailedScopeCheckCount);
    }

    // -----------------------------------------------------------------------
    // Valid build — source manifest fields
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_SourceManifestInputArtifacts_Is8()
    {
        Assert.Equal(8, BuildValid().SourceManifestInputArtifacts);
    }

    [Fact]
    public void ValidFixture_SourceManifestHashedInputArtifacts_Is8()
    {
        Assert.Equal(8, BuildValid().SourceManifestHashedInputArtifacts);
    }

    [Fact]
    public void ValidFixture_SourceManifestChecks_Is17()
    {
        Assert.Equal(17, BuildValid().SourceManifestChecks);
    }

    [Fact]
    public void ValidFixture_SourceManifestPassedChecks_Is17()
    {
        Assert.Equal(17, BuildValid().SourceManifestPassedChecks);
    }

    [Fact]
    public void ValidFixture_SourceManifestSha256_Is64HexChars()
    {
        Assert.Equal(64, BuildValid().SourceManifestSha256.Length);
    }

    // -----------------------------------------------------------------------
    // Valid build — gate / flags
    // -----------------------------------------------------------------------

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

    [Fact]
    public void ValidFixture_WriterExperimentGateStatus_IsLocked()
    {
        Assert.Equal("LOCKED_PENDING_OPERATOR_APPROVAL", BuildValid().WriterExperimentGateStatus);
    }

    [Fact]
    public void ValidFixture_FutureExperimentStatus_IsNotAuthorized()
    {
        Assert.Equal("NOT_AUTHORIZED", BuildValid().FutureExperimentStatus);
    }

    [Fact]
    public void ValidFixture_FutureExperimentName_IsMap26F()
    {
        Assert.Equal("MAP-26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN", BuildValid().FutureExperimentName);
    }

    [Fact]
    public void ValidFixture_Verdict_IsComplete()
    {
        Assert.Equal("MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE", BuildValid().Verdict);
    }

    // -----------------------------------------------------------------------
    // Valid build — lists
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_AllowedFutureActions_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().AllowedFutureActions);
    }

    [Fact]
    public void ValidFixture_ForbiddenActions_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().ForbiddenActions);
    }

    [Fact]
    public void ValidFixture_RequiredPreconditions_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().RequiredPreconditions);
    }

    [Fact]
    public void ValidFixture_RollbackRequirements_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().RollbackRequirements);
    }

    [Fact]
    public void ValidFixture_RiskRegister_NotEmpty()
    {
        Assert.NotEmpty(BuildValid().RiskRegister);
    }

    [Fact]
    public void ValidFixture_AllowedFutureActions_AllStatusFutureAllowed()
    {
        var result = BuildValid();
        Assert.All(result.AllowedFutureActions,
            a => Assert.Equal("FUTURE_ALLOWED_ONLY_AFTER_OPERATOR_APPROVAL", a.Status));
    }

    [Fact]
    public void ValidFixture_ForbiddenActions_ContainsCompileMapPng()
    {
        Assert.Contains(BuildValid().ForbiddenActions, f => f.ActionId == "COMPILE_MAP_00_PNG");
    }

    [Fact]
    public void ValidFixture_ForbiddenActions_ContainsWriteLotpack()
    {
        Assert.Contains(BuildValid().ForbiddenActions, f => f.ActionId == "WRITE_LOTPACK");
    }

    [Fact]
    public void ValidFixture_ForbiddenActions_ContainsWriteWorldGenOverrideLua()
    {
        Assert.Contains(BuildValid().ForbiddenActions, f => f.ActionId == "WRITE_WORLDGENOVERRIDE_LUA");
    }

    [Fact]
    public void ValidFixture_ForbiddenActions_ContainsUnlockGate()
    {
        Assert.Contains(BuildValid().ForbiddenActions, f => f.ActionId == "UNLOCK_WRITER_EXPERIMENT_GATE");
    }

    [Fact]
    public void ValidFixture_Check_FutureExperimentNotAuthorized_IsPass()
    {
        Assert.Equal("PASS", BuildValid().Checks.Single(c => c.CheckId == "FUTURE_EXPERIMENT_NOT_AUTHORIZED").CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_ForbiddenActionsListed_IsPass()
    {
        Assert.Equal("PASS", BuildValid().Checks.Single(c => c.CheckId == "FORBIDDEN_ACTIONS_LISTED").CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_RollbackRequirementsListed_IsPass()
    {
        Assert.Equal("PASS", BuildValid().Checks.Single(c => c.CheckId == "ROLLBACK_REQUIREMENTS_LISTED").CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_RiskRegisterListed_IsPass()
    {
        Assert.Equal("PASS", BuildValid().Checks.Single(c => c.CheckId == "RISK_REGISTER_LISTED").CheckStatus);
    }

    [Fact]
    public void ValidFixture_Check_ManifestSummaryExists_IsPass()
    {
        Assert.Equal("PASS", BuildValid().Checks.Single(c => c.CheckId == "MAP26D_MANIFEST_SUMMARY_EXISTS").CheckStatus);
    }

    // -----------------------------------------------------------------------
    // Render tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderJson_ContainsVerdict()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE",
            builder.RenderJson(result));
    }

    [Fact]
    public void RenderJson_ContainsApprovedForWriterExperimentFalse()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("\"approved_for_writer_experiment\": false", builder.RenderJson(result));
    }

    [Fact]
    public void RenderMarkdown_ContainsAllowedFutureActions()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("Allowed Future Actions", builder.RenderMarkdown(result));
    }

    [Fact]
    public void RenderMarkdown_ContainsForbiddenActions()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("Forbidden Actions", builder.RenderMarkdown(result));
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("Claim Boundary", builder.RenderMarkdown(result));
    }

    [Fact]
    public void RenderMarkdown_ContainsNotAuthorized()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("NOT_AUTHORIZED", builder.RenderMarkdown(result));
    }

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.StartsWith("check_order,", builder.RenderCsv(result));
    }

    [Fact]
    public void RenderCsv_Has17DataRows()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        var csv     = builder.RenderCsv(result);
        var lines   = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(18, lines.Length); // 1 header + 17 data rows
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE",
            builder.RenderSummary(result));
    }

    [Fact]
    public void RenderSummary_ContainsNotAuthorized()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("NOT_AUTHORIZED", builder.RenderSummary(result));
    }

    [Fact]
    public void RenderSummary_ApprovedForWriterExperiment_Is0()
    {
        var result  = BuildValid();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder();
        Assert.Contains("approved_for_writer_experiment  : 0", builder.RenderSummary(result));
    }
}
