using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordBuilder
{
    private const string ExpectedMap26DVerdict =
        "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE";
    private const string Format =
        "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-experiment-scope-record.v1";

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult Build(
        string manifestJsonPath, string manifestSummaryPath)
    {
        var errors = new List<string>();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult
        {
            Format                     = Format,
            GeneratedUtc               = DateTime.UtcNow.ToString("o"),
            SourceManifestPath         = manifestJsonPath,
            WriterReady                = false,
            RuntimeValid               = false,
            Materialized               = false,
            ApprovedForWriterExperiment = false,
            WriterExperimentGateStatus = "LOCKED_PENDING_OPERATOR_APPROVAL",
            FutureExperimentName       = "MAP-26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN",
            FutureExperimentStatus     = "NOT_AUTHORIZED",
        };

        if (!File.Exists(manifestJsonPath))
        {
            errors.Add($"Source manifest not found: {manifestJsonPath}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_INVALID";
            return result;
        }

        if (!File.Exists(manifestSummaryPath))
        {
            errors.Add($"Source manifest summary not found: {manifestSummaryPath}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_INVALID";
            return result;
        }

        byte[] manifestBytes = File.ReadAllBytes(manifestJsonPath);
        result.SourceManifestSha256 = Convert.ToHexString(SHA256.HashData(manifestBytes)).ToLower();

        string srcVerdict       = string.Empty;
        bool   srcIsValid       = false;
        int    srcArtifacts     = 0;
        int    srcHashed        = 0;
        int    srcChecks        = 0;
        int    srcPassedChecks  = 0;
        string mapId            = "map_00";
        string targetCompId     = string.Empty;
        bool   srcWriterReady   = true;
        bool   srcRuntimeValid  = true;
        bool   srcMaterialized  = true;
        bool   srcApproved      = true;
        string srcGateStatus    = string.Empty;

        try
        {
            using var doc  = JsonDocument.Parse(File.ReadAllText(manifestJsonPath));
            var root = doc.RootElement;

            srcVerdict      = root.TryGetProperty("verdict",                      out var v)   ? v.GetString()   ?? "" : "";
            srcIsValid      = root.TryGetProperty("is_valid",                     out var iv)  && iv.GetBoolean();
            srcArtifacts    = root.TryGetProperty("input_artifact_count",         out var ia)  ? ia.GetInt32()       : 0;
            srcHashed       = root.TryGetProperty("hashed_input_artifact_count",  out var ha)  ? ha.GetInt32()       : 0;
            srcChecks       = root.TryGetProperty("manifest_check_count",         out var mc)  ? mc.GetInt32()       : 0;
            srcPassedChecks = root.TryGetProperty("passed_manifest_check_count",  out var pmc) ? pmc.GetInt32()      : 0;
            mapId           = root.TryGetProperty("map_id",                       out var mi)  ? mi.GetString()  ?? "map_00" : "map_00";
            targetCompId    = root.TryGetProperty("target_component_id",          out var tc)  ? tc.GetString()  ?? "" : "";
            srcWriterReady  = root.TryGetProperty("writer_ready",                 out var wr)  && wr.GetBoolean();
            srcRuntimeValid = root.TryGetProperty("runtime_valid",                out var rv)  && rv.GetBoolean();
            srcMaterialized = root.TryGetProperty("materialized",                 out var mat) && mat.GetBoolean();
            srcApproved     = root.TryGetProperty("approved_for_writer_experiment", out var awe) && awe.GetBoolean();
            srcGateStatus   = root.TryGetProperty("writer_experiment_gate_status", out var gs) ? gs.GetString()  ?? "" : "";
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse source manifest: {ex.Message}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_INVALID";
            return result;
        }

        result.MapId                             = mapId;
        result.TargetComponentId                 = targetCompId;
        result.SourceManifestVerdict             = srcVerdict;
        result.SourceManifestIsValid             = srcIsValid;
        result.SourceManifestInputArtifacts      = srcArtifacts;
        result.SourceManifestHashedInputArtifacts = srcHashed;
        result.SourceManifestChecks              = srcChecks;
        result.SourceManifestPassedChecks        = srcPassedChecks;

        result.AllowedFutureActions  = BuildAllowedFutureActions();
        result.ForbiddenActions      = BuildForbiddenActions();
        result.RequiredPreconditions = BuildRequiredPreconditions();
        result.RollbackRequirements  = BuildRollbackRequirements();
        result.RiskRegister          = BuildRiskRegister();

        var checks = new List<DeadMtlWorldBuilderWriterExperimentScopeCheck>();
        int ord = 1;

        AddCheck(checks, ord++, "MAP26D_MANIFEST_EXISTS",
            "MAP-26D manifest file exists on disk",
            "true", "true",
            "Manifest file exists and was read.");

        AddCheck(checks, ord++, "MAP26D_MANIFEST_HASHED",
            "MAP-26D manifest SHA-256 computed",
            "true", "true",
            $"SHA-256: {result.SourceManifestSha256}.");

        AddCheck(checks, ord++, "MAP26D_VERDICT_COMPLETE",
            "MAP-26D verdict is complete",
            ExpectedMap26DVerdict, srcVerdict,
            $"Source manifest verdict: {srcVerdict}.");

        AddCheck(checks, ord++, "MAP26D_IS_VALID_TRUE",
            "MAP-26D is_valid is true",
            "true", srcIsValid.ToString().ToLower(),
            $"Source manifest is_valid: {srcIsValid}.");

        AddCheck(checks, ord++, "MAP26D_ARTIFACTS_8_OF_8",
            "MAP-26D input artifact count is 8",
            "8", srcArtifacts.ToString(),
            $"input_artifact_count: {srcArtifacts}.");

        AddCheck(checks, ord++, "MAP26D_HASHED_ARTIFACTS_8_OF_8",
            "MAP-26D hashed input artifact count is 8",
            "8", srcHashed.ToString(),
            $"hashed_input_artifact_count: {srcHashed}.");

        AddCheck(checks, ord++, "MAP26D_CHECKS_17_OF_17_PASS",
            "MAP-26D passed manifest check count is 17",
            "17", srcPassedChecks.ToString(),
            $"passed_manifest_check_count: {srcPassedChecks}.");

        AddCheck(checks, ord++, "WRITER_READY_FALSE",
            "writer_ready is false",
            "false", srcWriterReady.ToString().ToLower(),
            $"writer_ready: {srcWriterReady}.");

        AddCheck(checks, ord++, "RUNTIME_VALID_FALSE",
            "runtime_valid is false",
            "false", srcRuntimeValid.ToString().ToLower(),
            $"runtime_valid: {srcRuntimeValid}.");

        AddCheck(checks, ord++, "MATERIALIZED_FALSE",
            "materialized is false",
            "false", srcMaterialized.ToString().ToLower(),
            $"materialized: {srcMaterialized}.");

        AddCheck(checks, ord++, "APPROVED_FOR_WRITER_EXPERIMENT_FALSE",
            "approved_for_writer_experiment is false",
            "false", srcApproved.ToString().ToLower(),
            $"approved_for_writer_experiment: {srcApproved}.");

        AddCheck(checks, ord++, "GATE_STATUS_LOCKED",
            "writer_experiment_gate_status is LOCKED_PENDING_OPERATOR_APPROVAL",
            "LOCKED_PENDING_OPERATOR_APPROVAL", srcGateStatus,
            $"writer_experiment_gate_status: {srcGateStatus}.");

        AddCheck(checks, ord++, "FUTURE_EXPERIMENT_NOT_AUTHORIZED",
            "future_experiment_status is NOT_AUTHORIZED",
            "NOT_AUTHORIZED", result.FutureExperimentStatus,
            $"future_experiment_status: {result.FutureExperimentStatus}.");

        AddCheck(checks, ord++, "FORBIDDEN_ACTIONS_LISTED",
            "forbidden_actions list is non-empty",
            "true", (result.ForbiddenActions.Count > 0).ToString().ToLower(),
            $"{result.ForbiddenActions.Count} forbidden actions listed.");

        AddCheck(checks, ord++, "ROLLBACK_REQUIREMENTS_LISTED",
            "rollback_requirements list is non-empty",
            "true", (result.RollbackRequirements.Count > 0).ToString().ToLower(),
            $"{result.RollbackRequirements.Count} rollback requirements listed.");

        AddCheck(checks, ord++, "RISK_REGISTER_LISTED",
            "risk_register list is non-empty",
            "true", (result.RiskRegister.Count > 0).ToString().ToLower(),
            $"{result.RiskRegister.Count} risks listed.");

        AddCheck(checks, ord++, "MAP26D_MANIFEST_SUMMARY_EXISTS",
            "MAP-26D manifest summary file exists on disk",
            "true", "true",
            "Manifest summary file exists and was verified.");

        result.Checks              = checks;
        result.ScopeCheckCount     = checks.Count;
        result.PassedScopeCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedScopeCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass   = result.FailedScopeCheckCount == 0;
        result.IsValid = allPass;
        result.Errors  = errors;
        result.Verdict = allPass
            ? "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE"
            : "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_INVALID";

        return result;
    }

    private static void AddCheck(
        List<DeadMtlWorldBuilderWriterExperimentScopeCheck> checks,
        int order, string id, string label, string expected, string actual, string details)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal);
        checks.Add(new DeadMtlWorldBuilderWriterExperimentScopeCheck
        {
            CheckOrder  = order,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = pass ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
            Details     = details,
        });
    }

    private static List<DeadMtlWorldBuilderWriterExperimentScopeAllowedAction> BuildAllowedFutureActions() => new()
    {
        new() { ActionOrder = 1, ActionId = "READ_MAP26D_MANIFEST",           ActionLabel = "Read MAP-26D manifest JSON",           Status = "FUTURE_ALLOWED_ONLY_AFTER_OPERATOR_APPROVAL", Details = "Read the locked, hash-verified MAP-26D input bundle manifest." },
        new() { ActionOrder = 2, ActionId = "READ_HASHED_INPUT_ARTIFACTS",    ActionLabel = "Read hashed input artifacts",          Status = "FUTURE_ALLOWED_ONLY_AFTER_OPERATOR_APPROVAL", Details = "Read the 8 SHA-256 verified input artifacts referenced in the MAP-26D manifest." },
        new() { ActionOrder = 3, ActionId = "DESIGN_WRITER_DRY_RUN_SCHEMA",   ActionLabel = "Design writer dry-run schema",         Status = "FUTURE_ALLOWED_ONLY_AFTER_OPERATOR_APPROVAL", Details = "Design the output schema for a future writer dry-run experiment." },
        new() { ActionOrder = 4, ActionId = "DESIGN_OUTPUT_SANDBOX_CONSTRAINTS", ActionLabel = "Design output sandbox constraints", Status = "FUTURE_ALLOWED_ONLY_AFTER_OPERATOR_APPROVAL", Details = "Define which output paths are allowed (.local only) and which are forbidden." },
        new() { ActionOrder = 5, ActionId = "DESIGN_ROLLBACK_CHECKS",         ActionLabel = "Design rollback checks",              Status = "FUTURE_ALLOWED_ONLY_AFTER_OPERATOR_APPROVAL", Details = "Design automated checks that verify rollback state after a writer experiment." },
        new() { ActionOrder = 6, ActionId = "DESIGN_NO_INSTALL_GUARDS",       ActionLabel = "Design no-install guards",            Status = "FUTURE_ALLOWED_ONLY_AFTER_OPERATOR_APPROVAL", Details = "Design forbidden-output guards ensuring no PZ install mutation occurs." },
    };

    private static List<DeadMtlWorldBuilderWriterExperimentScopeForbiddenAction> BuildForbiddenActions() => new()
    {
        new() { ActionOrder = 1,  ActionId = "COMPILE_MAP_00_PNG",              ActionLabel = "Compile map_00.png",                  Reason = "Compiling map_00.png is a runtime operation not authorized at this stage." },
        new() { ActionOrder = 2,  ActionId = "WRITE_LOTPACK",                   ActionLabel = "Write .lotpack",                      Reason = "Writing lotpack files requires runtime proof that does not exist." },
        new() { ActionOrder = 3,  ActionId = "WRITE_WORLDGENOVERRIDE_LUA",      ActionLabel = "Write WorldGenOverride.lua",           Reason = "Writing WorldGenOverride.lua would install into the PZ runtime, which is not authorized." },
        new() { ActionOrder = 4,  ActionId = "CALL_COMPILE_WORLDGEN",           ActionLabel = "Call compile-worldgen",               Reason = "compile-worldgen is a runtime command. Not authorized at writer experiment scope stage." },
        new() { ActionOrder = 5,  ActionId = "INSTALL_INTO_PROJECT_ZOMBOID",    ActionLabel = "Install into Project Zomboid",         Reason = "Installing files into PZ game directories is not authorized at this stage." },
        new() { ActionOrder = 6,  ActionId = "CLAIM_RUNTIME_PROOF",             ActionLabel = "Claim runtime proof",                 Reason = "No runtime proof exists. writer_ready=false, runtime_valid=false, materialized=false." },
        new() { ActionOrder = 7,  ActionId = "CLAIM_WRITER_READY",              ActionLabel = "Claim writer readiness",              Reason = "writer_ready remains false. No writer experiment has been authorized or executed." },
        new() { ActionOrder = 8,  ActionId = "CLAIM_PUBLIC_PLAYABLE_PACKAGING", ActionLabel = "Claim public playable packaging",      Reason = "No public playable packaging exists. materialized=false." },
        new() { ActionOrder = 9,  ActionId = "MUTATE_SOURCE_INPUTS",            ActionLabel = "Mutate source input artifacts",        Reason = "MAP-26A/B/C/D outputs are frozen. Mutating them would break the hash-verified chain." },
        new() { ActionOrder = 10, ActionId = "UNLOCK_WRITER_EXPERIMENT_GATE",   ActionLabel = "Unlock writer experiment gate",        Reason = "The gate is LOCKED_PENDING_OPERATOR_APPROVAL. MAP-26E does not authorize unlocking." },
    };

    private static List<DeadMtlWorldBuilderWriterExperimentScopeRequiredPrecondition> BuildRequiredPreconditions() => new()
    {
        new() { PreconditionOrder = 1, PreconditionId = "MAP26D_MANIFEST_VALID",                      PreconditionLabel = "MAP-26D manifest is valid",                        Status = "REQUIRED", Details = "The MAP-26D manifest must be valid before any writer experiment is considered." },
        new() { PreconditionOrder = 2, PreconditionId = "MAP26D_INPUT_ARTIFACTS_HASHED",              PreconditionLabel = "MAP-26D input artifacts are hashed",               Status = "REQUIRED", Details = "All 8 input artifacts must have verified SHA-256 hashes." },
        new() { PreconditionOrder = 3, PreconditionId = "OPERATOR_APPROVAL_REQUIRED",                 PreconditionLabel = "Operator approval required",                       Status = "REQUIRED", Details = "Explicit operator approval must be recorded before any writer experiment begins." },
        new() { PreconditionOrder = 4, PreconditionId = "FUTURE_TASK_MUST_BE_DRY_RUN_FIRST",          PreconditionLabel = "Future writer task must be dry-run first",          Status = "REQUIRED", Details = "No direct writer execution. The first writer task must be a dry-run design only." },
        new() { PreconditionOrder = 5, PreconditionId = "FUTURE_TASK_MUST_OUTPUT_TO_DOT_LOCAL_ONLY",  PreconditionLabel = "Future writer outputs to .local only",              Status = "REQUIRED", Details = "All future writer experiment outputs must be confined to .local paths." },
        new() { PreconditionOrder = 6, PreconditionId = "FUTURE_TASK_MUST_HAVE_FORBIDDEN_OUTPUT_GUARDS", PreconditionLabel = "Future writer must have forbidden output guards", Status = "REQUIRED", Details = "Explicit guards preventing .lotpack, WorldGenOverride.lua, and PZ install must be present." },
        new() { PreconditionOrder = 7, PreconditionId = "FUTURE_TASK_MUST_HAVE_ROLLBACK_PLAN",        PreconditionLabel = "Future writer must have rollback plan",             Status = "REQUIRED", Details = "A documented rollback plan must exist before any writer experiment executes." },
    };

    private static List<DeadMtlWorldBuilderWriterExperimentScopeRollbackRequirement> BuildRollbackRequirements() => new()
    {
        new() { RequirementOrder = 1, RequirementId = "NO_TRACKED_RUNTIME_FILES",                  RequirementLabel = "No tracked runtime files",                  Details = "No PZ runtime files may be committed to the repository as a result of a writer experiment." },
        new() { RequirementOrder = 2, RequirementId = "NO_PZ_INSTALL_MUTATION",                    RequirementLabel = "No PZ install mutation",                    Details = "The Project Zomboid installation directory must not be modified by any writer experiment." },
        new() { RequirementOrder = 3, RequirementId = "DOT_LOCAL_OUTPUTS_ONLY",                    RequirementLabel = ".local outputs only",                       Details = "All writer experiment outputs must be in .local directories excluded from git tracking." },
        new() { RequirementOrder = 4, RequirementId = "GIT_STATUS_MUST_IDENTIFY_ONLY_TASK_FILES",  RequirementLabel = "Git status must show only task files",      Details = "After a writer experiment, git status must show only the expected task source files." },
        new() { RequirementOrder = 5, RequirementId = "FORBIDDEN_ARTIFACT_SCAN_REQUIRED",          RequirementLabel = "Forbidden artifact scan required",          Details = "After a writer experiment, an automated scan must confirm no .lotpack, .lotheader, or WorldGenOverride.lua files were created outside .local." },
    };

    private static List<DeadMtlWorldBuilderWriterExperimentScopeRisk> BuildRiskRegister() => new()
    {
        new() { RiskOrder = 1, RiskId = "WRITER_OUTPUT_CONFUSED_WITH_RUNTIME_PROOF",   RiskLabel = "Writer output confused with runtime proof",   RiskLevel = "HIGH",   Mitigation = "Always display is_valid, writer_ready, runtime_valid, materialized in all outputs. Never omit claim boundary fields." },
        new() { RiskOrder = 2, RiskId = "ACCIDENTAL_LOTPACK_CREATION",                  RiskLabel = "Accidental lotpack creation",                  RiskLevel = "HIGH",   Mitigation = "Forbidden output guard must scan for .lotpack and .lotheader after any writer run. Gate must block commit if found." },
        new() { RiskOrder = 3, RiskId = "ACCIDENTAL_WORLDGENOVERRIDE_CREATION",         RiskLabel = "Accidental WorldGenOverride.lua creation",     RiskLevel = "HIGH",   Mitigation = "Forbidden output guard must scan for WorldGenOverride.lua after any writer run. Gate must block commit if found." },
        new() { RiskOrder = 4, RiskId = "COMPILE_WORLDGEN_CALLED_BY_HELPER_SCRIPT",     RiskLabel = "compile-worldgen called by helper script",     RiskLevel = "MEDIUM", Mitigation = "All future writer helper scripts must be statically verified to not contain compile-worldgen calls." },
        new() { RiskOrder = 5, RiskId = "SOURCE_ARTIFACT_MUTATION",                     RiskLabel = "Source artifact mutation",                     RiskLevel = "MEDIUM", Mitigation = "MAP-26A/B/C/D outputs are read-only inputs. SHA-256 hashes must be re-verified before any writer experiment." },
        new() { RiskOrder = 6, RiskId = "APPROVAL_GATE_MISINTERPRETED",                 RiskLabel = "Approval gate misinterpreted",                 RiskLevel = "MEDIUM", Mitigation = "approved_for_writer_experiment=false and future_experiment_status=NOT_AUTHORIZED must be stated in all outputs. MAP-26E does not authorize MAP-26F." },
    };

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult result) =>
        JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-26E WorldBuilder Minimal Concrete Geometry Writer Experiment Scope Record");
        sb.AppendLine();
        sb.AppendLine("## Purpose");
        sb.AppendLine();
        sb.AppendLine("MAP-26E reads the MAP-26D writer input manifest and produces a deterministic");
        sb.AppendLine("writer experiment scope / approval record. It answers:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("What exactly would a future writer experiment be allowed to attempt,");
        sb.AppendLine("and what is still forbidden?");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("MAP-26E does NOT authorize the future writer experiment. It documents what that");
        sb.AppendLine("experiment would need to be. Authorization requires a separate operator decision.");
        sb.AppendLine();
        sb.AppendLine("## Source Manifest");
        sb.AppendLine();
        sb.AppendLine($"- Path: `{result.SourceManifestPath}`");
        sb.AppendLine($"- SHA-256: `{result.SourceManifestSha256}`");
        sb.AppendLine($"- Verdict: `{result.SourceManifestVerdict}`");
        sb.AppendLine($"- is_valid: `{result.SourceManifestIsValid}`");
        sb.AppendLine($"- input_artifacts: `{result.SourceManifestInputArtifacts}`");
        sb.AppendLine($"- hashed_input_artifacts: `{result.SourceManifestHashedInputArtifacts}`");
        sb.AppendLine($"- manifest_checks: `{result.SourceManifestChecks}`");
        sb.AppendLine($"- passed_checks: `{result.SourceManifestPassedChecks}`");
        sb.AppendLine();
        sb.AppendLine("## Allowed Future Actions");
        sb.AppendLine();
        sb.AppendLine("| # | Action ID | Status |");
        sb.AppendLine("|---|-----------|--------|");
        foreach (var a in result.AllowedFutureActions)
            sb.AppendLine($"| {a.ActionOrder} | `{a.ActionId}` | `{a.Status}` |");
        sb.AppendLine();
        sb.AppendLine("## Forbidden Actions");
        sb.AppendLine();
        sb.AppendLine("| # | Action ID | Reason |");
        sb.AppendLine("|---|-----------|--------|");
        foreach (var f in result.ForbiddenActions)
            sb.AppendLine($"| {f.ActionOrder} | `{f.ActionId}` | {f.Reason} |");
        sb.AppendLine();
        sb.AppendLine("## Required Preconditions");
        sb.AppendLine();
        sb.AppendLine("| # | Precondition ID | Status |");
        sb.AppendLine("|---|-----------------|--------|");
        foreach (var p in result.RequiredPreconditions)
            sb.AppendLine($"| {p.PreconditionOrder} | `{p.PreconditionId}` | `{p.Status}` |");
        sb.AppendLine();
        sb.AppendLine("## Rollback Requirements");
        sb.AppendLine();
        sb.AppendLine("| # | Requirement ID | Details |");
        sb.AppendLine("|---|----------------|---------|");
        foreach (var r in result.RollbackRequirements)
            sb.AppendLine($"| {r.RequirementOrder} | `{r.RequirementId}` | {r.Details} |");
        sb.AppendLine();
        sb.AppendLine("## Risk Register");
        sb.AppendLine();
        sb.AppendLine("| # | Risk ID | Level | Mitigation |");
        sb.AppendLine("|---|---------|-------|------------|");
        foreach (var r in result.RiskRegister)
            sb.AppendLine($"| {r.RiskOrder} | `{r.RiskId}` | `{r.RiskLevel}` | {r.Mitigation} |");
        sb.AppendLine();
        sb.AppendLine("## Scope Checks");
        sb.AppendLine();
        sb.AppendLine($"Checks: {result.ScopeCheckCount} | Pass: {result.PassedScopeCheckCount} | Fail: {result.FailedScopeCheckCount}");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status |");
        sb.AppendLine("|---|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | `{c.CheckId}` | `{c.CheckStatus}` |");
        sb.AppendLine();
        sb.AppendLine("## Writer Experiment Gate");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"approved_for_writer_experiment : {result.ApprovedForWriterExperiment.ToString().ToLower()}");
        sb.AppendLine($"writer_experiment_gate_status  : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"future_experiment_name         : {result.FutureExperimentName}");
        sb.AppendLine($"future_experiment_status       : {result.FutureExperimentStatus}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("MAP-26E does NOT authorize MAP-26F. It only documents what MAP-26F would need to be.");
        sb.AppendLine("Unlocking the gate requires a separate operator decision and a separate task.");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"writer_ready                   : {result.WriterReady.ToString().ToLower()}");
        sb.AppendLine($"runtime_valid                  : {result.RuntimeValid.ToString().ToLower()}");
        sb.AppendLine($"materialized                   : {result.Materialized.ToString().ToLower()}");
        sb.AppendLine($"approved_for_writer_experiment : {result.ApprovedForWriterExperiment.ToString().ToLower()}");
        sb.AppendLine($"writer_experiment_gate_status  : {result.WriterExperimentGateStatus}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("This scope record does not create geometry, write PZ runtime files, write lotpack,");
        sb.AppendLine("write WorldGenOverride.lua, call compile-worldgen, or install anything into Project Zomboid.");
        sb.AppendLine();
        sb.AppendLine("## What This Proves");
        sb.AppendLine();
        sb.AppendLine("- MAP-26D manifest exists, is SHA-256 hashed, and has a complete verdict.");
        sb.AppendLine("- MAP-26D is_valid=true, all 8 input artifacts exist and are hashed.");
        sb.AppendLine("- MAP-26D passed all 17 manifest checks.");
        sb.AppendLine("- writer_ready, runtime_valid, materialized remain false.");
        sb.AppendLine("- approved_for_writer_experiment remains false.");
        sb.AppendLine("- The writer experiment gate is LOCKED_PENDING_OPERATOR_APPROVAL.");
        sb.AppendLine("- Forbidden actions, required preconditions, rollback requirements, and risk register are documented.");
        sb.AppendLine();
        sb.AppendLine("## What This Does NOT Prove");
        sb.AppendLine();
        sb.AppendLine("- Runtime validity in Project Zomboid.");
        sb.AppendLine("- Writer readiness for PZ map compilation.");
        sb.AppendLine("- Authorization to run MAP-26F or any writer experiment.");
        sb.AppendLine("- Public playable packaging.");
        sb.AppendLine();
        sb.Append($"## Verdict: `{result.Verdict}`");
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual,details");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual},{EscapeCsv(c.Details)}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"map_id                          : {result.MapId}");
        sb.AppendLine($"target_component                : {result.TargetComponentId}");
        sb.AppendLine($"source_manifest_artifacts       : {result.SourceManifestInputArtifacts}");
        sb.AppendLine($"source_manifest_hashed_artifacts: {result.SourceManifestHashedInputArtifacts}");
        sb.AppendLine($"source_manifest_checks          : {result.SourceManifestChecks}");
        sb.AppendLine($"source_manifest_passed_checks   : {result.SourceManifestPassedChecks}");
        sb.AppendLine($"scope_checks                    : {result.ScopeCheckCount}");
        sb.AppendLine($"passed_scope_checks             : {result.PassedScopeCheckCount}");
        sb.AppendLine($"failed_scope_checks             : {result.FailedScopeCheckCount}");
        sb.AppendLine($"writer_ready                    : {(result.WriterReady    ? 1 : 0)}");
        sb.AppendLine($"runtime_valid                   : {(result.RuntimeValid   ? 1 : 0)}");
        sb.AppendLine($"materialized                    : {(result.Materialized   ? 1 : 0)}");
        sb.AppendLine($"approved_for_writer_experiment  : {(result.ApprovedForWriterExperiment ? 1 : 0)}");
        sb.AppendLine($"writer_experiment_gate_status   : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"future_experiment_name          : {result.FutureExperimentName}");
        sb.AppendLine($"future_experiment_status        : {result.FutureExperimentStatus}");
        sb.Append($"verdict                         : {result.Verdict}");
        return sb.ToString();
    }

    private static string EscapeCsv(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
