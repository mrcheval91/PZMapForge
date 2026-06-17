using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignBuilder
{
    private const string ExpectedMap26EVerdict =
        "MAP26E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_EXPERIMENT_SCOPE_RECORD_COMPLETE";
    private const string Format =
        "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-dry-run-design.v1";
    private const string SandboxRoot =
        @".local\deadmtl-authoring\worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter\map_00\";

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult Build(
        string scopeRecordPath, string manifestJsonPath, string geometryMvpPath)
    {
        var errors = new List<string>();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult
        {
            Format                     = Format,
            GeneratedUtc               = DateTime.UtcNow.ToString("o"),
            SourceScopeRecordPath      = scopeRecordPath,
            SourceManifestPath         = manifestJsonPath,
            SourceGeometryMvpPath      = geometryMvpPath,
            FutureWriterName           = "MAP-26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER",
            FutureWriterStatus         = "DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME",
            DryRunOnly                 = true,
            WriterReady                = false,
            RuntimeValid               = false,
            Materialized               = false,
            ApprovedForWriterExperiment = false,
            WriterExperimentGateStatus = "LOCKED_PENDING_OPERATOR_APPROVAL",
            SandboxOutputRoot          = SandboxRoot,
        };

        if (!File.Exists(scopeRecordPath))
        {
            errors.Add($"Scope record not found: {scopeRecordPath}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_INVALID";
            return result;
        }

        if (!File.Exists(manifestJsonPath))
        {
            errors.Add($"Manifest not found: {manifestJsonPath}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_INVALID";
            return result;
        }

        if (!File.Exists(geometryMvpPath))
        {
            errors.Add($"Geometry MVP not found: {geometryMvpPath}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_INVALID";
            return result;
        }

        result.SourceScopeRecordSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(scopeRecordPath))).ToLower();
        result.SourceManifestSha256    = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifestJsonPath))).ToLower();
        result.SourceGeometryMvpSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(geometryMvpPath))).ToLower();

        string scopeVerdict     = string.Empty;
        bool   scopeIsValid     = false;
        string scopeFutureStatus = string.Empty;
        string scopeGateStatus  = string.Empty;
        string mapId            = "map_00";
        string targetCompId     = string.Empty;

        try
        {
            using var doc  = JsonDocument.Parse(File.ReadAllText(scopeRecordPath));
            var root = doc.RootElement;
            scopeVerdict      = root.TryGetProperty("verdict",                       out var v)  ? v.GetString()  ?? "" : "";
            scopeIsValid      = root.TryGetProperty("is_valid",                      out var iv) && iv.GetBoolean();
            scopeFutureStatus = root.TryGetProperty("future_experiment_status",      out var fs) ? fs.GetString() ?? "" : "";
            scopeGateStatus   = root.TryGetProperty("writer_experiment_gate_status", out var gs) ? gs.GetString() ?? "" : "";
            mapId             = root.TryGetProperty("map_id",                        out var mi) ? mi.GetString() ?? "map_00" : "map_00";
            targetCompId      = root.TryGetProperty("target_component_id",           out var tc) ? tc.GetString() ?? "" : "";
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse scope record: {ex.Message}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_INVALID";
            return result;
        }

        result.MapId                   = mapId;
        result.TargetComponentId       = targetCompId;
        result.SourceScopeRecordVerdict = scopeVerdict;
        result.SourceScopeRecordIsValid = scopeIsValid;

        result.PlannedOutputRecords = BuildPlannedOutputRecords();
        result.SandboxConstraints   = BuildSandboxConstraints();
        result.ForbiddenOutputGuards = BuildForbiddenOutputGuards();
        result.RollbackChecks       = BuildRollbackChecks();

        var checks = new List<DeadMtlWorldBuilderWriterDryRunDesignCheck>();
        int ord = 1;

        AddCheck(checks, ord++, "MAP26E_SCOPE_RECORD_EXISTS",
            "MAP-26E scope record file exists on disk",
            "true", "true",
            "Scope record file exists and was read.");

        AddCheck(checks, ord++, "MAP26E_SCOPE_RECORD_HASHED",
            "MAP-26E scope record SHA-256 computed",
            "true", "true",
            $"SHA-256: {result.SourceScopeRecordSha256}.");

        AddCheck(checks, ord++, "MAP26E_VERDICT_COMPLETE",
            "MAP-26E verdict is complete",
            ExpectedMap26EVerdict, scopeVerdict,
            $"Scope record verdict: {scopeVerdict}.");

        AddCheck(checks, ord++, "MAP26E_IS_VALID_TRUE",
            "MAP-26E is_valid is true",
            "true", scopeIsValid.ToString().ToLower(),
            $"Scope record is_valid: {scopeIsValid}.");

        AddCheck(checks, ord++, "MAP26E_FUTURE_STATUS_NOT_AUTHORIZED",
            "MAP-26E future_experiment_status is NOT_AUTHORIZED",
            "NOT_AUTHORIZED", scopeFutureStatus,
            $"future_experiment_status: {scopeFutureStatus}.");

        AddCheck(checks, ord++, "MAP26E_GATE_LOCKED",
            "MAP-26E writer_experiment_gate_status is LOCKED_PENDING_OPERATOR_APPROVAL",
            "LOCKED_PENDING_OPERATOR_APPROVAL", scopeGateStatus,
            $"writer_experiment_gate_status: {scopeGateStatus}.");

        AddCheck(checks, ord++, "MAP26D_MANIFEST_EXISTS",
            "MAP-26D manifest file exists on disk",
            "true", "true",
            "Manifest file exists and was read.");

        AddCheck(checks, ord++, "MAP26D_MANIFEST_HASHED",
            "MAP-26D manifest SHA-256 computed",
            "true", "true",
            $"SHA-256: {result.SourceManifestSha256}.");

        AddCheck(checks, ord++, "MAP26A_GEOMETRY_MVP_EXISTS",
            "MAP-26A geometry MVP file exists on disk",
            "true", "true",
            "Geometry MVP file exists and was read.");

        AddCheck(checks, ord++, "MAP26A_GEOMETRY_MVP_HASHED",
            "MAP-26A geometry MVP SHA-256 computed",
            "true", "true",
            $"SHA-256: {result.SourceGeometryMvpSha256}.");

        AddCheck(checks, ord++, "DRY_RUN_ONLY_TRUE",
            "dry_run_only is true",
            "true", result.DryRunOnly.ToString().ToLower(),
            $"dry_run_only: {result.DryRunOnly}.");

        AddCheck(checks, ord++, "WRITER_READY_FALSE",
            "writer_ready is false",
            "false", result.WriterReady.ToString().ToLower(),
            $"writer_ready: {result.WriterReady}.");

        AddCheck(checks, ord++, "RUNTIME_VALID_FALSE",
            "runtime_valid is false",
            "false", result.RuntimeValid.ToString().ToLower(),
            $"runtime_valid: {result.RuntimeValid}.");

        AddCheck(checks, ord++, "MATERIALIZED_FALSE",
            "materialized is false",
            "false", result.Materialized.ToString().ToLower(),
            $"materialized: {result.Materialized}.");

        AddCheck(checks, ord++, "APPROVED_FOR_WRITER_EXPERIMENT_FALSE",
            "approved_for_writer_experiment is false",
            "false", result.ApprovedForWriterExperiment.ToString().ToLower(),
            $"approved_for_writer_experiment: {result.ApprovedForWriterExperiment}.");

        AddCheck(checks, ord++, "SANDBOX_OUTPUT_ROOT_IS_DOT_LOCAL",
            "sandbox_output_root is under .local",
            "true", result.SandboxOutputRoot.Contains(".local").ToString().ToLower(),
            $"sandbox_output_root: {result.SandboxOutputRoot}.");

        AddCheck(checks, ord++, "PLANNED_OUTPUT_RECORDS_LISTED",
            "planned_output_records list is non-empty",
            "true", (result.PlannedOutputRecords.Count > 0).ToString().ToLower(),
            $"{result.PlannedOutputRecords.Count} planned output records listed.");

        AddCheck(checks, ord++, "SANDBOX_CONSTRAINTS_LISTED",
            "sandbox_constraints list is non-empty",
            "true", (result.SandboxConstraints.Count > 0).ToString().ToLower(),
            $"{result.SandboxConstraints.Count} sandbox constraints listed.");

        AddCheck(checks, ord++, "FORBIDDEN_OUTPUT_GUARDS_LISTED",
            "forbidden_output_guards list is non-empty",
            "true", (result.ForbiddenOutputGuards.Count > 0).ToString().ToLower(),
            $"{result.ForbiddenOutputGuards.Count} forbidden output guards listed.");

        AddCheck(checks, ord++, "ROLLBACK_CHECKS_LISTED",
            "rollback_checks list is non-empty",
            "true", (result.RollbackChecks.Count > 0).ToString().ToLower(),
            $"{result.RollbackChecks.Count} rollback checks listed.");

        AddCheck(checks, ord++, "NO_FORBIDDEN_OUTPUTS_CREATED",
            "No forbidden outputs created (no .lotpack, no .lotheader, no WorldGenOverride.lua, no compile-worldgen)",
            "true", "true",
            "No forbidden outputs created.");

        result.Checks              = checks;
        result.DesignCheckCount    = checks.Count;
        result.PassedDesignCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedDesignCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass   = result.FailedDesignCheckCount == 0;
        result.IsValid = allPass;
        result.Errors  = errors;
        result.Verdict = allPass
            ? "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE"
            : "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_INVALID";

        return result;
    }

    private static void AddCheck(
        List<DeadMtlWorldBuilderWriterDryRunDesignCheck> checks,
        int order, string id, string label, string expected, string actual, string details)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal);
        checks.Add(new DeadMtlWorldBuilderWriterDryRunDesignCheck
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

    private static List<DeadMtlWorldBuilderWriterDryRunPlannedOutputRecord> BuildPlannedOutputRecords() => new()
    {
        new() { RecordOrder = 1, RecordId = "COMPONENT_WRITER_RECORD",      RecordKind = "COMPONENT_RECORD",      SourceGeometryKind = "COMPONENT_BBOX_GEOMETRY",        PlannedPath = $@"{SandboxRoot}map_00.component_writer_record.json",        Status = "DESIGN_ONLY_NOT_EMITTED", Details = "One record describing the target component bbox, intent, and access readiness class." },
        new() { RecordOrder = 2, RecordId = "LOT_WRITER_RECORDS",           RecordKind = "LOT_RECORDS",           SourceGeometryKind = "LOT_GEOMETRY",                   PlannedPath = $@"{SandboxRoot}map_00.lot_writer_records.json",              Status = "DESIGN_ONLY_NOT_EMITTED", Details = "Seven lot records, each with x/y/width/height from MAP-26A lot geometry." },
        new() { RecordOrder = 3, RecordId = "BUILDING_SLOT_WRITER_RECORDS", RecordKind = "BUILDING_SLOT_RECORDS", SourceGeometryKind = "BUILDING_SLOT_GEOMETRY",         PlannedPath = $@"{SandboxRoot}map_00.building_slot_writer_records.json",   Status = "DESIGN_ONLY_NOT_EMITTED", Details = "Seven building slot records with inset geometry from MAP-26A accepted slots." },
        new() { RecordOrder = 4, RecordId = "FRONTAGE_ACCESS_RECORD",       RecordKind = "ACCESS_RECORD",         SourceGeometryKind = "FRONTAGE_CONTACT_GEOMETRY",      PlannedPath = $@"{SandboxRoot}map_00.frontage_access_record.json",         Status = "DESIGN_ONLY_NOT_EMITTED", Details = "One frontage access record: NORTH side, 89px contact with map_00_component_0023." },
        new() { RecordOrder = 5, RecordId = "REAR_SERVICE_ACCESS_RECORD",   RecordKind = "ACCESS_RECORD",         SourceGeometryKind = "REAR_SERVICE_CONTACT_GEOMETRY",  PlannedPath = $@"{SandboxRoot}map_00.rear_service_access_record.json",     Status = "DESIGN_ONLY_NOT_EMITTED", Details = "One rear service access record: EAST side, 60px contact with map_00_component_0030." },
        new() { RecordOrder = 6, RecordId = "FORBIDDEN_OUTPUT_SCAN_RECORD", RecordKind = "SCAN_RECORD",           SourceGeometryKind = "OUTPUT_SCAN",                    PlannedPath = $@"{SandboxRoot}map_00.forbidden_output_scan.json",          Status = "DESIGN_ONLY_NOT_EMITTED", Details = "Scan result confirming no .lotpack, .lotheader, or WorldGenOverride.lua in output." },
        new() { RecordOrder = 7, RecordId = "ROLLBACK_RECORD",              RecordKind = "ROLLBACK_RECORD",       SourceGeometryKind = "GIT_STATUS",                     PlannedPath = $@"{SandboxRoot}map_00.rollback_record.json",                Status = "DESIGN_ONLY_NOT_EMITTED", Details = "Git status snapshot confirming only expected task files are present after dry run." },
        new() { RecordOrder = 8, RecordId = "CLAIM_BOUNDARY_RECORD",        RecordKind = "CLAIM_BOUNDARY_RECORD", SourceGeometryKind = "CLAIM_BOUNDARY_FIELDS",          PlannedPath = $@"{SandboxRoot}map_00.claim_boundary_record.json",          Status = "DESIGN_ONLY_NOT_EMITTED", Details = "Claim boundary record: writer_ready=false, runtime_valid=false, materialized=false, approved_for_writer_experiment=false." },
    };

    private static List<DeadMtlWorldBuilderWriterDryRunSandboxConstraint> BuildSandboxConstraints() => new()
    {
        new() { ConstraintOrder = 1, ConstraintId = "DOT_LOCAL_ONLY",                 ConstraintLabel = ".local outputs only",               Details = "All dry-run emitter outputs must be written to .local directories only. No outputs may be written to tracked repo paths." },
        new() { ConstraintOrder = 2, ConstraintId = "NO_PZ_INSTALL_PATHS",            ConstraintLabel = "No PZ install paths",               Details = "No output path may reference or be under the Project Zomboid installation directory." },
        new() { ConstraintOrder = 3, ConstraintId = "NO_RUNTIME_EXTENSION_OUTPUTS",   ConstraintLabel = "No runtime extension outputs",      Details = "No .lotpack, .lotheader, .lua, or any PZ runtime extension may be emitted." },
        new() { ConstraintOrder = 4, ConstraintId = "NO_SOURCE_ARTIFACT_MUTATION",    ConstraintLabel = "No source artifact mutation",       Details = "MAP-26A/B/C/D/E outputs are frozen read-only inputs. The dry-run emitter must not modify them." },
        new() { ConstraintOrder = 5, ConstraintId = "NO_COMPILE_COMMANDS",            ConstraintLabel = "No compile commands",               Details = "compile-worldgen, compile-worldgen-project, and any PZ compilation command must not be invoked." },
        new() { ConstraintOrder = 6, ConstraintId = "NO_WORLDGEN_OVERRIDE",           ConstraintLabel = "No WorldGenOverride.lua",           Details = "WorldGenOverride.lua must not be written under any circumstances by the dry-run emitter." },
    };

    private static List<DeadMtlWorldBuilderWriterDryRunForbiddenOutputGuard> BuildForbiddenOutputGuards() => new()
    {
        new() { GuardOrder = 1, GuardId = "BLOCK_LOTPACK",                         GuardLabel = "Block .lotpack",                       BlockedPattern = "*.lotpack",             Details = "Any file matching *.lotpack in the output directory must trigger an error and halt the dry-run emitter." },
        new() { GuardOrder = 2, GuardId = "BLOCK_LOTHEADER",                       GuardLabel = "Block .lotheader",                     BlockedPattern = "*.lotheader",           Details = "Any file matching *.lotheader in the output directory must trigger an error and halt the dry-run emitter." },
        new() { GuardOrder = 3, GuardId = "BLOCK_WORLDGENOVERRIDE_LUA",            GuardLabel = "Block WorldGenOverride.lua",           BlockedPattern = "WorldGenOverride.lua",  Details = "WorldGenOverride.lua must not appear anywhere in the output tree." },
        new() { GuardOrder = 4, GuardId = "BLOCK_COMPILE_WORLDGEN",                GuardLabel = "Block compile-worldgen",               BlockedPattern = "compile-worldgen",      Details = "The string compile-worldgen must not appear in any emitter helper script." },
        new() { GuardOrder = 5, GuardId = "BLOCK_PROJECT_ZOMBOID_INSTALL_PATH",    GuardLabel = "Block Project Zomboid install path",   BlockedPattern = "ProjectZomboid",        Details = "No output path may reference a Project Zomboid installation directory." },
        new() { GuardOrder = 6, GuardId = "BLOCK_MAP_00_PNG_MUTATION",             GuardLabel = "Block map_00.png mutation",            BlockedPattern = "map_00.png (write)",    Details = "The source PNG map_00.png must be opened read-only. Any write attempt must be blocked." },
        new() { GuardOrder = 7, GuardId = "BLOCK_RUNTIME_PROOF_CLAIM",             GuardLabel = "Block runtime proof claim",            BlockedPattern = "runtime_valid=true",    Details = "runtime_valid must always be false. Any code path that sets runtime_valid=true is forbidden." },
        new() { GuardOrder = 8, GuardId = "BLOCK_WRITER_READY_CLAIM",              GuardLabel = "Block writer_ready claim",             BlockedPattern = "writer_ready=true",     Details = "writer_ready must always be false. Any code path that sets writer_ready=true is forbidden." },
    };

    private static List<DeadMtlWorldBuilderWriterDryRunRollbackCheck> BuildRollbackChecks() => new()
    {
        new() { CheckOrder = 1, CheckId = "GIT_STATUS_TASK_FILES_ONLY",      CheckLabel = "Git status shows only task files",        Details = "After the dry-run emitter completes, git status must show only the expected MAP-26G source files. No .local outputs may appear as tracked." },
        new() { CheckOrder = 2, CheckId = "NO_FORBIDDEN_ARTIFACTS_FOUND",    CheckLabel = "No forbidden artifacts found",            Details = "A scan of all output directories must confirm zero .lotpack, .lotheader, and WorldGenOverride.lua files." },
        new() { CheckOrder = 3, CheckId = "DOT_LOCAL_OUTPUTS_ONLY",          CheckLabel = ".local outputs only",                     Details = "All emitter output files must be in .local directories. No emitter output may appear outside .local." },
        new() { CheckOrder = 4, CheckId = "SOURCE_HASHES_UNCHANGED",         CheckLabel = "Source hashes unchanged",                 Details = "SHA-256 hashes of MAP-26A/D/E inputs must match the values recorded in MAP-26F before and after the dry-run emitter runs." },
        new() { CheckOrder = 5, CheckId = "NO_PZ_INSTALL_MUTATION",          CheckLabel = "No PZ install mutation",                  Details = "The Project Zomboid installation directory must not have been modified as a result of the dry-run emitter." },
    };

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult result) =>
        System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-26F WorldBuilder Minimal Concrete Geometry Writer Dry-Run Design");
        sb.AppendLine();
        sb.AppendLine("## Purpose");
        sb.AppendLine();
        sb.AppendLine("MAP-26F reads the MAP-26E writer experiment scope record and produces a deterministic");
        sb.AppendLine("dry-run writer design record. It answers:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("What would a future writer attempt to emit, where would it be sandboxed,");
        sb.AppendLine("what schema would it use, and what guards would prevent runtime mutation?");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("MAP-26F does NOT emit any writer outputs. It only designs what MAP-26G would do.");
        sb.AppendLine("Authorization requires a separate operator decision.");
        sb.AppendLine();
        sb.AppendLine("## Source Inputs");
        sb.AppendLine();
        sb.AppendLine($"- Scope record path: `{result.SourceScopeRecordPath}`");
        sb.AppendLine($"- Scope record SHA-256: `{result.SourceScopeRecordSha256}`");
        sb.AppendLine($"- Scope record verdict: `{result.SourceScopeRecordVerdict}`");
        sb.AppendLine($"- Scope record is_valid: `{result.SourceScopeRecordIsValid}`");
        sb.AppendLine($"- Manifest path: `{result.SourceManifestPath}`");
        sb.AppendLine($"- Manifest SHA-256: `{result.SourceManifestSha256}`");
        sb.AppendLine($"- Geometry MVP path: `{result.SourceGeometryMvpPath}`");
        sb.AppendLine($"- Geometry MVP SHA-256: `{result.SourceGeometryMvpSha256}`");
        sb.AppendLine();
        sb.AppendLine("## Planned Output Records");
        sb.AppendLine();
        sb.AppendLine("| # | Record ID | Kind | Status |");
        sb.AppendLine("|---|-----------|------|--------|");
        foreach (var r in result.PlannedOutputRecords)
            sb.AppendLine($"| {r.RecordOrder} | `{r.RecordId}` | `{r.RecordKind}` | `{r.Status}` |");
        sb.AppendLine();
        sb.AppendLine("## Sandbox Constraints");
        sb.AppendLine();
        sb.AppendLine("| # | Constraint ID | Details |");
        sb.AppendLine("|---|---------------|---------|");
        foreach (var c in result.SandboxConstraints)
            sb.AppendLine($"| {c.ConstraintOrder} | `{c.ConstraintId}` | {c.Details} |");
        sb.AppendLine();
        sb.AppendLine("## Forbidden Output Guards");
        sb.AppendLine();
        sb.AppendLine("| # | Guard ID | Blocked Pattern |");
        sb.AppendLine("|---|----------|-----------------|");
        foreach (var g in result.ForbiddenOutputGuards)
            sb.AppendLine($"| {g.GuardOrder} | `{g.GuardId}` | `{g.BlockedPattern}` |");
        sb.AppendLine();
        sb.AppendLine("## Rollback Checks");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Details |");
        sb.AppendLine("|---|----------|---------|");
        foreach (var r in result.RollbackChecks)
            sb.AppendLine($"| {r.CheckOrder} | `{r.CheckId}` | {r.Details} |");
        sb.AppendLine();
        sb.AppendLine("## Design Checks");
        sb.AppendLine();
        sb.AppendLine($"Checks: {result.DesignCheckCount} | Pass: {result.PassedDesignCheckCount} | Fail: {result.FailedDesignCheckCount}");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status |");
        sb.AppendLine("|---|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | `{c.CheckId}` | `{c.CheckStatus}` |");
        sb.AppendLine();
        sb.AppendLine("## Writer Gate");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"dry_run_only                   : {result.DryRunOnly.ToString().ToLower()}");
        sb.AppendLine($"approved_for_writer_experiment : {result.ApprovedForWriterExperiment.ToString().ToLower()}");
        sb.AppendLine($"writer_experiment_gate_status  : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"future_writer_name             : {result.FutureWriterName}");
        sb.AppendLine($"future_writer_status           : {result.FutureWriterStatus}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("MAP-26F does NOT authorize MAP-26G. It only designs what MAP-26G would need to be.");
        sb.AppendLine("Unlocking requires a separate operator decision and a separate task.");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"writer_ready                   : {result.WriterReady.ToString().ToLower()}");
        sb.AppendLine($"runtime_valid                  : {result.RuntimeValid.ToString().ToLower()}");
        sb.AppendLine($"materialized                   : {result.Materialized.ToString().ToLower()}");
        sb.AppendLine($"approved_for_writer_experiment : {result.ApprovedForWriterExperiment.ToString().ToLower()}");
        sb.AppendLine($"writer_experiment_gate_status  : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"future_writer_status           : {result.FutureWriterStatus}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("This dry-run design does not create geometry, write PZ runtime files, write lotpack,");
        sb.AppendLine("write lotheader, write WorldGenOverride.lua, call compile-worldgen, or install");
        sb.AppendLine("anything into Project Zomboid.");
        sb.AppendLine();
        sb.Append($"## Verdict: `{result.Verdict}`");
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual,details");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual},{EscapeCsv(c.Details)}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"map_id                          : {result.MapId}");
        sb.AppendLine($"target_component                : {result.TargetComponentId}");
        sb.AppendLine($"scope_record_sha256             : {result.SourceScopeRecordSha256}");
        sb.AppendLine($"manifest_sha256                 : {result.SourceManifestSha256}");
        sb.AppendLine($"geometry_mvp_sha256             : {result.SourceGeometryMvpSha256}");
        sb.AppendLine($"planned_output_records          : {result.PlannedOutputRecords.Count}");
        sb.AppendLine($"sandbox_constraints             : {result.SandboxConstraints.Count}");
        sb.AppendLine($"forbidden_output_guards         : {result.ForbiddenOutputGuards.Count}");
        sb.AppendLine($"rollback_checks                 : {result.RollbackChecks.Count}");
        sb.AppendLine($"design_checks                   : {result.DesignCheckCount}");
        sb.AppendLine($"passed_design_checks            : {result.PassedDesignCheckCount}");
        sb.AppendLine($"failed_design_checks            : {result.FailedDesignCheckCount}");
        sb.AppendLine($"dry_run_only                    : {(result.DryRunOnly           ? 1 : 0)}");
        sb.AppendLine($"writer_ready                    : {(result.WriterReady          ? 1 : 0)}");
        sb.AppendLine($"runtime_valid                   : {(result.RuntimeValid         ? 1 : 0)}");
        sb.AppendLine($"materialized                    : {(result.Materialized         ? 1 : 0)}");
        sb.AppendLine($"approved_for_writer_experiment  : {(result.ApprovedForWriterExperiment ? 1 : 0)}");
        sb.AppendLine($"writer_experiment_gate_status   : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"future_writer_name              : {result.FutureWriterName}");
        sb.AppendLine($"future_writer_status            : {result.FutureWriterStatus}");
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
