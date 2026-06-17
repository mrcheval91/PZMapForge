using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBuilder
{
    private const string ExpectedMap26AVerdict  = "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE";
    private const string ExpectedMap26BVerdict  = "MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE";
    private const string ExpectedMap26CVerdict  = "MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE";
    private const string ExpectedComponentId    = "map_00_component_0001";
    private const string ExpectedBbox           = "(124,10)->(212,69) 89x60px";
    private const string ExpectedSourceDims     = "256x256";
    private const int    ExpectedLotCount       = 7;
    private const int    ExpectedSlotCount      = 7;
    private const int    ExpectedFeatureCount   = 15;
    private const int    ExpectedPassedReviewChecks = 18;
    private const int    ArtifactCount          = 8;
    private const int    CheckCount             = 17;
    private const string Format                 = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-input-manifest.v1";

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult Build(
        string geometryMvpPath,
        string qaOverlayJsonPath,
        string qaOverlayCsvPath,
        string qaOverlayPngPath,
        string qaReviewJsonPath,
        string qaReviewCsvPath,
        string qaReviewMdPath,
        string qaReviewSummaryPath)
    {
        var checks = new List<DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestCheck>();
        var errors = new List<string>();
        int order  = 1;

        // --- build and hash artifact list ---
        var artifactDefs = new (int ArtifactOrder, string ArtifactId, string ArtifactKind, string Path, string SourceTask)[]
        {
            (1, "MAP26A_GEOMETRY_MVP_JSON",        "GEOMETRY_MVP_JSON",         geometryMvpPath,    "MAP-26A"),
            (2, "MAP26B_QA_OVERLAY_JSON",          "QA_OVERLAY_JSON",           qaOverlayJsonPath,  "MAP-26B"),
            (3, "MAP26B_QA_OVERLAY_CSV",           "QA_OVERLAY_CSV",            qaOverlayCsvPath,   "MAP-26B"),
            (4, "MAP26B_QA_OVERLAY_PNG",           "QA_OVERLAY_PNG",            qaOverlayPngPath,   "MAP-26B"),
            (5, "MAP26C_QA_REVIEW_PACKET_JSON",    "QA_REVIEW_PACKET_JSON",     qaReviewJsonPath,   "MAP-26C"),
            (6, "MAP26C_QA_REVIEW_PACKET_CSV",     "QA_REVIEW_PACKET_CSV",      qaReviewCsvPath,    "MAP-26C"),
            (7, "MAP26C_QA_REVIEW_PACKET_MD",      "QA_REVIEW_PACKET_MARKDOWN", qaReviewMdPath,     "MAP-26C"),
            (8, "MAP26C_QA_REVIEW_PACKET_SUMMARY", "QA_REVIEW_PACKET_SUMMARY",  qaReviewSummaryPath,"MAP-26C"),
        };

        var artifacts = new List<DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestArtifact>();
        foreach (var d in artifactDefs)
        {
            bool exists   = File.Exists(d.Path);
            string sha256 = string.Empty;
            long size     = 0;
            if (exists)
            {
                var bytes = File.ReadAllBytes(d.Path);
                sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLower();
                size   = bytes.Length;
            }
            artifacts.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestArtifact
            {
                ArtifactOrder = d.ArtifactOrder,
                ArtifactId    = d.ArtifactId,
                ArtifactKind  = d.ArtifactKind,
                Path          = d.Path,
                Exists        = exists,
                Sha256        = sha256,
                SizeBytes     = size,
                SourceTask    = d.SourceTask,
                Required      = true,
            });
        }

        int existCount  = artifacts.Count(a => a.Exists);
        int hashedCount = artifacts.Count(a => !string.IsNullOrEmpty(a.Sha256));

        // check 1: all artifacts exist
        bool allExist = existCount == ArtifactCount;
        AddCheck(checks, order++,
            "ALL_REQUIRED_INPUT_ARTIFACTS_EXIST",
            "All 8 required input artifacts exist",
            ArtifactCount.ToString(), existCount.ToString(),
            allExist ? $"All {ArtifactCount} artifacts exist." : $"{existCount}/{ArtifactCount} artifacts found.");
        if (!allExist) errors.Add($"Not all required artifacts exist: {existCount}/{ArtifactCount}.");

        // check 2: all artifacts hashed
        bool allHashed = hashedCount == ArtifactCount;
        AddCheck(checks, order++,
            "ALL_REQUIRED_INPUT_ARTIFACTS_HASHED",
            "All 8 required input artifacts are SHA-256 hashed",
            ArtifactCount.ToString(), hashedCount.ToString(),
            allHashed ? $"All {ArtifactCount} artifacts hashed." : $"{hashedCount}/{ArtifactCount} artifacts hashed.");
        if (!allHashed) errors.Add($"Not all required artifacts hashed: {hashedCount}/{ArtifactCount}.");

        if (errors.Count > 0)
            return Invalid(artifacts, checks, errors);

        // --- parse MAP-26A verdict ---
        string map26aVerdict;
        try   { map26aVerdict = ReadVerdict(geometryMvpPath); }
        catch (Exception ex) { errors.Add($"Cannot parse MAP-26A JSON: {ex.Message}"); return Invalid(artifacts, checks, errors); }

        // --- parse MAP-26B verdict ---
        string map26bVerdict;
        try   { map26bVerdict = ReadVerdict(qaOverlayJsonPath); }
        catch (Exception ex) { errors.Add($"Cannot parse MAP-26B JSON: {ex.Message}"); return Invalid(artifacts, checks, errors); }

        // --- parse MAP-26C JSON ---
        string map26cVerdict, mapId, componentId, intent, accessClass, sourceDimensions;
        int    componentOrder, bboxMinX, bboxMinY, bboxMaxX, bboxMaxY, bboxW, bboxH;
        int    lotCount, slotCount, featureCount, reviewCheckCount, passedReviewChecks, failedReviewChecks;
        bool   writerReady, runtimeValid, materialized;

        try
        {
            using var doc  = JsonDocument.Parse(File.ReadAllText(qaReviewJsonPath));
            var root       = doc.RootElement;
            map26cVerdict  = root.GetProperty("verdict").GetString()                  ?? string.Empty;
            mapId          = root.GetProperty("map_id").GetString()                   ?? string.Empty;
            componentId    = root.GetProperty("target_component_id").GetString()      ?? string.Empty;
            componentOrder = root.GetProperty("target_component_order").GetInt32();
            intent         = root.GetProperty("intent").GetString()                   ?? string.Empty;
            accessClass    = root.GetProperty("access_readiness_class").GetString()   ?? string.Empty;
            var bbox       = root.GetProperty("component_bbox");
            bboxMinX       = bbox.GetProperty("min_x").GetInt32();
            bboxMinY       = bbox.GetProperty("min_y").GetInt32();
            bboxMaxX       = bbox.GetProperty("max_x").GetInt32();
            bboxMaxY       = bbox.GetProperty("max_y").GetInt32();
            bboxW          = bbox.GetProperty("width_px").GetInt32();
            bboxH          = bbox.GetProperty("height_px").GetInt32();
            sourceDimensions      = root.GetProperty("source_dimensions").GetString()          ?? string.Empty;
            lotCount              = root.GetProperty("lot_count").GetInt32();
            slotCount             = root.GetProperty("accepted_building_slot_count").GetInt32();
            featureCount          = root.GetProperty("overlay_feature_count").GetInt32();
            reviewCheckCount      = root.GetProperty("review_check_count").GetInt32();
            passedReviewChecks    = root.GetProperty("passed_review_check_count").GetInt32();
            failedReviewChecks    = root.GetProperty("failed_review_check_count").GetInt32();
            writerReady           = root.GetProperty("writer_ready").GetBoolean();
            runtimeValid          = root.GetProperty("runtime_valid").GetBoolean();
            materialized          = root.GetProperty("materialized").GetBoolean();
        }
        catch (Exception ex)
        {
            errors.Add($"Cannot parse MAP-26C review JSON: {ex.Message}");
            return Invalid(artifacts, checks, errors);
        }

        string actualBbox = $"({bboxMinX},{bboxMinY})->({bboxMaxX},{bboxMaxY}) {bboxW}x{bboxH}px";

        // check 3
        bool map26aOk = string.Equals(map26aVerdict, ExpectedMap26AVerdict, StringComparison.Ordinal);
        AddCheck(checks, order++, "MAP26A_VERDICT_COMPLETE", "MAP-26A verdict is complete",
            ExpectedMap26AVerdict, map26aVerdict,
            map26aOk ? "Verdict matches." : $"Expected {ExpectedMap26AVerdict}, got {map26aVerdict}.");
        if (!map26aOk) errors.Add($"MAP-26A verdict mismatch: {map26aVerdict}.");

        // check 4
        bool map26bOk = string.Equals(map26bVerdict, ExpectedMap26BVerdict, StringComparison.Ordinal);
        AddCheck(checks, order++, "MAP26B_VERDICT_COMPLETE", "MAP-26B verdict is complete",
            ExpectedMap26BVerdict, map26bVerdict,
            map26bOk ? "Verdict matches." : $"Expected {ExpectedMap26BVerdict}, got {map26bVerdict}.");
        if (!map26bOk) errors.Add($"MAP-26B verdict mismatch: {map26bVerdict}.");

        // check 5
        bool map26cOk = string.Equals(map26cVerdict, ExpectedMap26CVerdict, StringComparison.Ordinal);
        AddCheck(checks, order++, "MAP26C_VERDICT_COMPLETE", "MAP-26C verdict is complete",
            ExpectedMap26CVerdict, map26cVerdict,
            map26cOk ? "Verdict matches." : $"Expected {ExpectedMap26CVerdict}, got {map26cVerdict}.");
        if (!map26cOk) errors.Add($"MAP-26C verdict mismatch: {map26cVerdict}.");

        // check 6
        bool reviewChecksOk = passedReviewChecks == ExpectedPassedReviewChecks;
        AddCheck(checks, order++, "MAP26C_REVIEW_CHECKS_18_OF_18_PASS", "MAP-26C passed review check count is 18",
            ExpectedPassedReviewChecks.ToString(), passedReviewChecks.ToString(),
            reviewChecksOk ? $"Passed review checks: {passedReviewChecks}/{reviewCheckCount}." : $"Expected 18/18, got {passedReviewChecks}/{reviewCheckCount}.");
        if (!reviewChecksOk) errors.Add($"MAP-26C review checks: {passedReviewChecks}/{reviewCheckCount} passed, expected 18/18.");

        // check 7
        bool compIdOk = string.Equals(componentId, ExpectedComponentId, StringComparison.Ordinal);
        AddCheck(checks, order++, "COMPONENT_ID_STABLE", "Target component ID is stable",
            ExpectedComponentId, componentId,
            compIdOk ? "Component ID matches." : $"Expected {ExpectedComponentId}, got {componentId}.");
        if (!compIdOk) errors.Add($"Component ID mismatch: {componentId}.");

        // check 8
        bool bboxOk = string.Equals(actualBbox, ExpectedBbox, StringComparison.Ordinal);
        AddCheck(checks, order++, "COMPONENT_BBOX_STABLE", "Component bbox is stable",
            ExpectedBbox, actualBbox,
            bboxOk ? "Bbox matches." : $"Expected {ExpectedBbox}, got {actualBbox}.");
        if (!bboxOk) errors.Add($"Component bbox mismatch: {actualBbox}.");

        // check 9
        bool lotOk = lotCount == ExpectedLotCount;
        AddCheck(checks, order++, "LOT_COUNT_STABLE_7", "Lot count is stable at 7",
            ExpectedLotCount.ToString(), lotCount.ToString(),
            lotOk ? "Lot count stable." : $"Expected {ExpectedLotCount}, got {lotCount}.");
        if (!lotOk) errors.Add($"Lot count mismatch: {lotCount}.");

        // check 10
        bool slotOk = slotCount == ExpectedSlotCount;
        AddCheck(checks, order++, "ACCEPTED_BUILDING_SLOT_COUNT_STABLE_7", "Accepted building slot count is stable at 7",
            ExpectedSlotCount.ToString(), slotCount.ToString(),
            slotOk ? "Slot count stable." : $"Expected {ExpectedSlotCount}, got {slotCount}.");
        if (!slotOk) errors.Add($"Slot count mismatch: {slotCount}.");

        // check 11
        bool featureOk = featureCount == ExpectedFeatureCount;
        AddCheck(checks, order++, "OVERLAY_FEATURE_COUNT_STABLE_15", "Overlay feature count is stable at 15",
            ExpectedFeatureCount.ToString(), featureCount.ToString(),
            featureOk ? "Feature count stable." : $"Expected {ExpectedFeatureCount}, got {featureCount}.");
        if (!featureOk) errors.Add($"Feature count mismatch: {featureCount}.");

        // check 12
        bool dimsOk = string.Equals(sourceDimensions, ExpectedSourceDims, StringComparison.Ordinal);
        AddCheck(checks, order++, "SOURCE_DIMENSIONS_STABLE_256X256", "Source dimensions are stable at 256x256",
            ExpectedSourceDims, sourceDimensions,
            dimsOk ? "Source dimensions stable." : $"Expected {ExpectedSourceDims}, got {sourceDimensions}.");
        if (!dimsOk) errors.Add($"Source dimensions mismatch: {sourceDimensions}.");

        // check 13
        bool writerReadyFalse = !writerReady;
        AddCheck(checks, order++, "WRITER_READY_FALSE", "writer_ready is false",
            "false", writerReady.ToString().ToLower(),
            writerReadyFalse ? "writer_ready is false." : "writer_ready is true — VIOLATION.");
        if (!writerReadyFalse) errors.Add("writer_ready is true — boundary violation.");

        // check 14
        bool runtimeValidFalse = !runtimeValid;
        AddCheck(checks, order++, "RUNTIME_VALID_FALSE", "runtime_valid is false",
            "false", runtimeValid.ToString().ToLower(),
            runtimeValidFalse ? "runtime_valid is false." : "runtime_valid is true — VIOLATION.");
        if (!runtimeValidFalse) errors.Add("runtime_valid is true — boundary violation.");

        // check 15
        bool materializedFalse = !materialized;
        AddCheck(checks, order++, "MATERIALIZED_FALSE", "materialized is false",
            "false", materialized.ToString().ToLower(),
            materializedFalse ? "materialized is false." : "materialized is true — VIOLATION.");
        if (!materializedFalse) errors.Add("materialized is true — boundary violation.");

        // check 16 — gate is always locked; approved_for_writer_experiment is always false
        AddCheck(checks, order++,
            "APPROVED_FOR_WRITER_EXPERIMENT_FALSE",
            "approved_for_writer_experiment is false: gate remains LOCKED_PENDING_OPERATOR_APPROVAL",
            "false", "false",
            "approved_for_writer_experiment is false: gate remains LOCKED_PENDING_OPERATOR_APPROVAL.");

        // check 17 — static: this builder never creates forbidden outputs
        AddCheck(checks, order++,
            "NO_FORBIDDEN_OUTPUTS_CREATED",
            "No forbidden outputs created (no map_00.png compile, no lotpack, no WorldGenOverride.lua)",
            "true", "true",
            "No forbidden outputs created.");

        bool   isValid = errors.Count == 0;
        string verdict = isValid
            ? "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE"
            : "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_INVALID";

        int passed = checks.Count(c => c.CheckStatus == "PASS");
        int failed = checks.Count(c => c.CheckStatus == "FAIL");

        return new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult
        {
            Format                    = Format,
            GeneratedUtc              = DateTime.UtcNow.ToString("o"),
            MapId                     = mapId,
            TargetComponentId         = componentId,
            TargetComponentOrder      = componentOrder,
            Intent                    = intent,
            AccessReadinessClass      = accessClass,
            ComponentBbox             = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBbox
            {
                MinX = bboxMinX, MinY = bboxMinY, MaxX = bboxMaxX, MaxY = bboxMaxY,
                WidthPx = bboxW, HeightPx = bboxH,
            },
            SourceDimensions          = sourceDimensions,
            LotCount                  = lotCount,
            AcceptedBuildingSlotCount = slotCount,
            OverlayFeatureCount       = featureCount,
            ReviewCheckCount          = reviewCheckCount,
            PassedReviewCheckCount    = passedReviewChecks,
            FailedReviewCheckCount    = failedReviewChecks,
            InputArtifactCount        = artifacts.Count,
            HashedInputArtifactCount  = hashedCount,
            WriterReady               = false,
            RuntimeValid              = false,
            Materialized              = false,
            ApprovedForWriterExperiment = false,
            WriterExperimentGateStatus  = "LOCKED_PENDING_OPERATOR_APPROVAL",
            ManifestCheckCount        = checks.Count,
            PassedManifestCheckCount  = passed,
            FailedManifestCheckCount  = failed,
            IsValid                   = isValid,
            Verdict                   = verdict,
            InputArtifacts            = artifacts,
            Checks                    = checks,
            Errors                    = errors,
        };
    }

    // -----------------------------------------------------------------------

    private static string ReadVerdict(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("verdict").GetString() ?? string.Empty;
    }

    private static void AddCheck(
        List<DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestCheck> checks,
        int order, string id, string label, string expected, string actual, string details)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal);
        checks.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestCheck
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

    private static DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult Invalid(
        List<DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestArtifact> artifacts,
        List<DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestCheck> checks,
        List<string> errors) =>
        new()
        {
            Format                   = Format,
            GeneratedUtc             = DateTime.UtcNow.ToString("o"),
            InputArtifacts           = artifacts,
            InputArtifactCount       = artifacts.Count,
            HashedInputArtifactCount = artifacts.Count(a => !string.IsNullOrEmpty(a.Sha256)),
            WriterReady              = false,
            RuntimeValid             = false,
            Materialized             = false,
            ApprovedForWriterExperiment = false,
            WriterExperimentGateStatus  = "LOCKED_PENDING_OPERATOR_APPROVAL",
            ManifestCheckCount       = checks.Count,
            PassedManifestCheckCount = checks.Count(c => c.CheckStatus == "PASS"),
            FailedManifestCheckCount = checks.Count(c => c.CheckStatus == "FAIL"),
            IsValid                  = false,
            Verdict                  = "MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_INVALID",
            Checks                   = checks,
            Errors                   = errors,
        };

    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult result)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(result, opts);
    }

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-26D WorldBuilder Minimal Concrete Geometry Writer Input Manifest");
        sb.AppendLine();
        sb.AppendLine($"- map id: {result.MapId}");
        sb.AppendLine($"- target component id: {result.TargetComponentId} (order {result.TargetComponentOrder})");
        sb.AppendLine($"- intent: {result.Intent}");
        sb.AppendLine($"- access class: {result.AccessReadinessClass}");
        if (result.ComponentBbox is { } bbox)
            sb.AppendLine($"- component bbox: ({bbox.MinX},{bbox.MinY})->({bbox.MaxX},{bbox.MaxY}) {bbox.WidthPx}x{bbox.HeightPx}px");
        sb.AppendLine($"- source dimensions: {result.SourceDimensions}");
        sb.AppendLine($"- lot count: {result.LotCount}");
        sb.AppendLine($"- accepted building slot count: {result.AcceptedBuildingSlotCount}");
        sb.AppendLine($"- overlay feature count: {result.OverlayFeatureCount}");
        sb.AppendLine($"- review check count: {result.ReviewCheckCount}");
        sb.AppendLine($"- passed review check count: {result.PassedReviewCheckCount}");
        sb.AppendLine($"- failed review check count: {result.FailedReviewCheckCount}");
        sb.AppendLine($"- input artifacts: {result.InputArtifactCount}");
        sb.AppendLine($"- hashed input artifacts: {result.HashedInputArtifactCount}");
        sb.AppendLine($"- manifest checks: {result.ManifestCheckCount}");
        sb.AppendLine($"- passed manifest checks: {result.PassedManifestCheckCount}");
        sb.AppendLine($"- failed manifest checks: {result.FailedManifestCheckCount}");
        sb.AppendLine($"- writer_ready: {result.WriterReady}");
        sb.AppendLine($"- runtime_valid: {result.RuntimeValid}");
        sb.AppendLine($"- materialized: {result.Materialized}");
        sb.AppendLine($"- approved_for_writer_experiment: {result.ApprovedForWriterExperiment}");
        sb.AppendLine($"- writer_experiment_gate_status: {result.WriterExperimentGateStatus}");
        sb.AppendLine($"- verdict: {result.Verdict}");
        sb.AppendLine();
        sb.AppendLine("## Input Artifacts");
        sb.AppendLine();
        sb.AppendLine("| order | artifact_id | kind | exists | sha256 (first 16) | size_bytes | source_task |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var a in result.InputArtifacts)
        {
            var sha16 = a.Sha256.Length >= 16 ? a.Sha256[..16] + "..." : a.Sha256;
            sb.AppendLine($"| {a.ArtifactOrder} | {a.ArtifactId} | {a.ArtifactKind} | {a.Exists} | {sha16} | {a.SizeBytes} | {a.SourceTask} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Manifest Checks");
        sb.AppendLine();
        sb.AppendLine("| check_order | check_id | check_label | check_status | expected | actual | details |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | {c.CheckId} | {c.CheckLabel} | {c.CheckStatus} | {c.Expected} | {c.Actual} | {c.Details} |");
        sb.AppendLine();
        sb.AppendLine("## Writer Experiment Gate");
        sb.AppendLine();
        sb.AppendLine($"- approved_for_writer_experiment: {result.ApprovedForWriterExperiment}");
        sb.AppendLine($"- writer_experiment_gate_status: {result.WriterExperimentGateStatus}");
        sb.AppendLine();
        sb.AppendLine("This manifest does not authorize a writer experiment.");
        sb.AppendLine("The gate remains LOCKED_PENDING_OPERATOR_APPROVAL.");
        sb.AppendLine("A future writer experiment may read this manifest as a verified input bundle.");
        sb.AppendLine("Operator approval is required to change approved_for_writer_experiment to true.");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("This manifest is a hash-verified input bundle only.");
        sb.AppendLine("It does not create geometry.");
        sb.AppendLine("It does not write PZ runtime files.");
        sb.AppendLine("It does not write lotpack.");
        sb.AppendLine("It does not write WorldGenOverride.lua.");
        sb.AppendLine("It does not call compile-worldgen.");
        sb.AppendLine("It does not install anything into Project Zomboid.");
        sb.AppendLine("It does not prove runtime validity.");
        sb.AppendLine("It does not prove writer readiness.");
        sb.AppendLine("It does not prove public playable packaging.");
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_label,check_status,expected,actual,details");
        foreach (var c in result.Checks)
        {
            string Esc(string s) => s.Contains(',') ? $"\"{s}\"" : s;
            sb.AppendLine($"{c.CheckOrder},{Esc(c.CheckId)},{Esc(c.CheckLabel)},{c.CheckStatus},{Esc(c.Expected)},{Esc(c.Actual)},{Esc(c.Details)}");
        }
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult result)
    {
        var bbox = result.ComponentBbox;
        string bboxStr = bbox != null
            ? $"({bbox.MinX},{bbox.MinY})->({bbox.MaxX},{bbox.MaxY}) {bbox.WidthPx}x{bbox.HeightPx}px"
            : string.Empty;
        return $"""
map_id                          : {result.MapId}
target_component                : {result.TargetComponentId}
intent                          : {result.Intent}
access_class                    : {result.AccessReadinessClass}
component_bbox                  : {bboxStr}
source_dimensions               : {result.SourceDimensions}
lot_rectangles                  : {result.LotCount}
building_slot_rectangles        : {result.AcceptedBuildingSlotCount}
overlay_features                : {result.OverlayFeatureCount}
review_checks                   : {result.ReviewCheckCount}
passed_review_checks            : {result.PassedReviewCheckCount}
failed_review_checks            : {result.FailedReviewCheckCount}
input_artifacts                 : {result.InputArtifactCount}
hashed_input_artifacts          : {result.HashedInputArtifactCount}
manifest_checks                 : {result.ManifestCheckCount}
passed_manifest_checks          : {result.PassedManifestCheckCount}
failed_manifest_checks          : {result.FailedManifestCheckCount}
writer_ready                    : {(result.WriterReady ? 1 : 0)}
runtime_valid                   : {(result.RuntimeValid ? 1 : 0)}
materialized                    : {(result.Materialized ? 1 : 0)}
approved_for_writer_experiment  : {(result.ApprovedForWriterExperiment ? 1 : 0)}
writer_experiment_gate_status   : {result.WriterExperimentGateStatus}
verdict                         : {result.Verdict}
""";
    }
}
