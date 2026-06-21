using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderGeometryToBinaryBridgePlanBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private static readonly string[] s_sourceRejectionMarkers =
        { "Dru_map", "Dru", "workshop donor", "third-party" };

    private static readonly BridgePlanUnknownField[] s_requiredUnknowns =
    {
        new()
        {
            Id          = "UNKNOWN_001",
            Description = "lotheader tile/table structure not fully mapped — byte layout of tile entries, flags, and counts is undocumented",
            File        = "35_27.lotheader",
            Severity    = "blocking",
        },
        new()
        {
            Id          = "UNKNOWN_002",
            Description = "lotpack payload placement record structure not fully understood — interior record boundaries and tile reference encoding unknown",
            File        = "world_35_27.lotpack",
            Severity    = "blocking",
        },
        new()
        {
            Id          = "UNKNOWN_003",
            Description = "chunkdata semantics not fully understood — record count, record size, and chunk coordinate encoding are unverified guesses",
            File        = "chunkdata_35_27.bin",
            Severity    = "blocking",
        },
        new()
        {
            Id          = "UNKNOWN_004",
            Description = "MAP-31B bucket-to-tile-ID and layer mapping not runtime-verified — WALL/FLOOR/ACCESS/LOT bucket outputs have no confirmed PZ tile ID assignments",
            File        = "MAP-31B emitter JSON",
            Severity    = "blocking",
        },
        new()
        {
            Id          = "UNKNOWN_005",
            Description = "no safe writer contract for mutating Build 42 binary cell files — no test harness, no rollback mechanism, no mutation spec exists",
            File        = "all three binary files",
            Severity    = "blocking",
        },
        new()
        {
            Id          = "UNKNOWN_006",
            Description = "visible-cell candidate proves mountability (game loads the cell), not authorability — successful game load does not imply understanding of how to write geometry into the binary format",
            File        = "MAP-35A installed candidate",
            Severity    = "blocking",
        },
    };

    private static readonly BridgePlanExperiment[] s_candidateExperiments =
    {
        new()
        {
            Id          = "EXP_001",
            Description = "Hex-disassemble the visible-cell lotheader to derive tile table structure — compare byte-by-byte against minimal seed to isolate geometry-bearing regions",
            TargetFile  = "35_27.lotheader",
            ReadOnly    = true,
        },
        new()
        {
            Id          = "EXP_002",
            Description = "Hex-disassemble a range of chunkdata records to identify field boundaries — sample first 10 and last 10 records from visible chunkdata to find record-size candidates",
            TargetFile  = "chunkdata_35_27.bin",
            ReadOnly    = true,
        },
        new()
        {
            Id          = "EXP_003",
            Description = "Produce a read-only tile-walk report from lotpack without mutation — iterate lotpack bytes at candidate record boundaries and emit an offset+value table for review",
            TargetFile  = "world_35_27.lotpack",
            ReadOnly    = true,
        },
        new()
        {
            Id          = "EXP_004",
            Description = "Map MAP-31B WALL/FLOOR/ACCESS/LOT buckets to candidate PZ tile IDs from installed tilesheets — extract tilesheet names from local PZ install and cross-reference against known bucket intent categories",
            TargetFile  = "local PZ tilesheet directory",
            ReadOnly    = true,
        },
    };

    public DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult Build(
        string map36a1AuditJson,
        string map31bEmitterJson,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult
        {
            Format            = "MAP36B_GEOMETRY_TO_BINARY_BRIDGE_PLAN_V1",
            GeneratedUtc      = DateTime.UtcNow.ToString("O"),
            Map36a1AuditJson  = map36a1AuditJson  ?? string.Empty,
            Map31bEmitterJson = map31bEmitterJson ?? string.Empty,
            OutputRoot        = outputRoot        ?? string.Empty,
        };

        // Source rejection — check both input paths
        string combinedPaths = $"{result.Map36a1AuditJson}|{result.Map31bEmitterJson}";
        result.SourceRejected = s_sourceRejectionMarkers.Any(m =>
            combinedPaths.Contains(m, StringComparison.OrdinalIgnoreCase));

        if (result.SourceRejected)
        {
            string marker = s_sourceRejectionMarkers.First(m =>
                combinedPaths.Contains(m, StringComparison.OrdinalIgnoreCase));
            result.SourceRejectionReason = $"input path contains rejected marker: {marker}";
            result.Errors.Add(result.SourceRejectionReason);
        }

        // Input availability (only when not rejected)
        if (!result.SourceRejected)
        {
            result.AuditOutputFound   = !string.IsNullOrEmpty(result.Map36a1AuditJson)  && File.Exists(result.Map36a1AuditJson);
            result.EmitterOutputFound = !string.IsNullOrEmpty(result.Map31bEmitterJson) && File.Exists(result.Map31bEmitterJson);
        }

        // Emitter facts — read from emitter JSON if available, otherwise safe defaults
        result.SandboxOnly = true;
        if (result.EmitterOutputFound)
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(result.Map31bEmitterJson));
                var root = doc.RootElement;
                if (root.TryGetProperty("emitted_operation_records", out var ops) && ops.GetArrayLength() > 0)
                {
                    var first = ops[0];
                    if (first.TryGetProperty("emits_binary_file", out var ebf))
                        result.EmitsBinaryFile = ebf.GetBoolean();
                    if (first.TryGetProperty("sandbox_only", out var so))
                        result.SandboxOnly = so.GetBoolean();
                }
            }
            catch { /* emitter JSON parse error is non-fatal; keep defaults */ }
        }

        // Hardcoded plan content
        result.RequiredUnknownFields.AddRange(s_requiredUnknowns);
        result.CandidateExperiments.AddRange(s_candidateExperiments);
        result.RequiredUnknownCount     = result.RequiredUnknownFields.Count;
        result.CandidateExperimentCount = result.CandidateExperiments.Count;

        // Claim boundary (all false — this is a read-only audit/plan)
        result.RuntimeBinaryWritten    = false;
        result.GeometryInjected        = false;
        result.PlayableExportClaimed   = false;
        result.WorkshopUploadPerformed = false;
        result.SteamInstallWrite       = false;

        // Checks
        var checks = new List<BridgePlanCheck>();
        string actualAuditFound   = result.AuditOutputFound.ToString().ToLowerInvariant();
        string actualEmitterFound = result.EmitterOutputFound.ToString().ToLowerInvariant();
        AddCheck(checks, "MAP36B_AUDIT_OUTPUT_FOUND",
            "MAP-36A1 audit JSON recorded (informational)",
            actualAuditFound, actualAuditFound);
        AddCheck(checks, "MAP36B_EMITTER_OUTPUT_FOUND",
            "MAP-31B emitter JSON recorded (informational)",
            actualEmitterFound, actualEmitterFound);
        AddCheck(checks, "MAP36B_EMITS_BINARY_FILE_FALSE",
            "MAP-31B emitter does not emit binary files",
            "false", result.EmitsBinaryFile.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_SANDBOX_ONLY_TRUE",
            "MAP-31B emitter operates sandbox-only",
            "true", result.SandboxOnly.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_NO_RUNTIME_BINARY_WRITE",
            "runtime_binary_written is false",
            "false", result.RuntimeBinaryWritten.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_GEOMETRY_INJECTED_FALSE",
            "geometry_injected is false",
            "false", result.GeometryInjected.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_PLAYABLE_EXPORT_CLAIMED_FALSE",
            "playable_export_claimed is false",
            "false", result.PlayableExportClaimed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_WORKSHOP_UPLOAD_PERFORMED_FALSE",
            "workshop_upload_performed is false",
            "false", result.WorkshopUploadPerformed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_STEAM_INSTALL_WRITE_FALSE",
            "steam_install_write is false",
            "false", result.SteamInstallWrite.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_REQUIRED_UNKNOWN_COUNT_GE_6",
            "at least 6 blocking unknowns documented",
            "true", (result.RequiredUnknownCount >= 6).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_CANDIDATE_EXPERIMENT_COUNT_GE_4",
            "at least 4 candidate next experiments documented",
            "true", (result.CandidateExperimentCount >= 4).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36B_OUTPUT_ROOT_CONTAINS_LOCAL",
            "output root path is sandboxed under .local",
            "true", result.OutputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase).ToString().ToLowerInvariant());

        result.Checks          = checks;
        result.CheckCount      = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid         = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict         = result.IsValid
            ? "MAP36B_BRIDGE_PLAN_COMPLETE"
            : "MAP36B_BRIDGE_PLAN_FAILED";

        return result;
    }

    private static void AddCheck(List<BridgePlanCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new BridgePlanCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }

    public string RenderJson(DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},{CsvEscape(c.Description)},{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"format              : {result.Format}");
        sb.AppendLine($"generated_utc       : {result.GeneratedUtc}");
        sb.AppendLine($"audit_output_found  : {result.AuditOutputFound}");
        sb.AppendLine($"emitter_output_found: {result.EmitterOutputFound}");
        sb.AppendLine($"emits_binary_file   : {result.EmitsBinaryFile}");
        sb.AppendLine($"sandbox_only        : {result.SandboxOnly}");
        sb.AppendLine($"required_unknowns   : {result.RequiredUnknownCount}");
        sb.AppendLine($"candidate_exps      : {result.CandidateExperimentCount}");
        sb.AppendLine($"runtime_binary_written   : {result.RuntimeBinaryWritten}");
        sb.AppendLine($"geometry_injected        : {result.GeometryInjected}");
        sb.AppendLine($"playable_export_claimed  : {result.PlayableExportClaimed}");
        sb.AppendLine($"workshop_upload_performed: {result.WorkshopUploadPerformed}");
        sb.AppendLine($"steam_install_write      : {result.SteamInstallWrite}");
        sb.AppendLine($"checks              : {result.CheckCount} total / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL");
        sb.AppendLine($"verdict             : {result.Verdict}");
        return sb.ToString();
    }

    public string RenderBridgePlanMarkdown(DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36B: Geometry-to-Binary Bridge Plan");
        sb.AppendLine();
        sb.AppendLine($"Generated: {result.GeneratedUtc}");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| runtime_binary_written | {result.RuntimeBinaryWritten.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| geometry_injected | {result.GeometryInjected.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| playable_export_claimed | {result.PlayableExportClaimed.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| workshop_upload_performed | {result.WorkshopUploadPerformed.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| steam_install_write | {result.SteamInstallWrite.ToString().ToLowerInvariant()} |");
        sb.AppendLine();
        sb.AppendLine("No binary files written. No geometry injection. Read-only bridge plan artifact.");
        sb.AppendLine();
        sb.AppendLine("## Required Unknowns (Blocking)");
        sb.AppendLine();
        sb.AppendLine("| ID | Description | File | Severity |");
        sb.AppendLine("|----|-------------|------|----------|");
        foreach (var u in result.RequiredUnknownFields)
            sb.AppendLine($"| {u.Id} | {u.Description} | {u.File} | {u.Severity} |");
        sb.AppendLine();
        sb.AppendLine("## Candidate Next Experiments");
        sb.AppendLine();
        sb.AppendLine("| ID | Description | Target File | Read Only |");
        sb.AppendLine("|----|-------------|-------------|-----------|");
        foreach (var e in result.CandidateExperiments)
            sb.AppendLine($"| {e.Id} | {e.Description} | {e.TargetFile} | {e.ReadOnly.ToString().ToLowerInvariant()} |");
        sb.AppendLine();
        sb.AppendLine("## Input Availability");
        sb.AppendLine();
        sb.AppendLine($"- audit_output_found: {result.AuditOutputFound}");
        sb.AppendLine($"- emitter_output_found: {result.EmitterOutputFound}");
        sb.AppendLine($"- emits_binary_file: {result.EmitsBinaryFile}");
        sb.AppendLine($"- sandbox_only: {result.SandboxOnly}");
        sb.AppendLine();
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine($"**{result.Verdict}** — {result.PassedCheckCount}/{result.CheckCount} checks PASS");
        return sb.ToString();
    }

    public string RenderUnknownsCsv(DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("id,description,file,severity");
        foreach (var u in result.RequiredUnknownFields)
            sb.AppendLine($"{u.Id},{CsvEscape(u.Description)},{CsvEscape(u.File)},{u.Severity}");
        return sb.ToString();
    }

    public string RenderExperimentsCsv(DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("id,description,target_file,read_only");
        foreach (var e in result.CandidateExperiments)
            sb.AppendLine($"{e.Id},{CsvEscape(e.Description)},{CsvEscape(e.TargetFile)},{e.ReadOnly.ToString().ToLowerInvariant()}");
        return sb.ToString();
    }

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
