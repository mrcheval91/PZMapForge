using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBuilder
{
    private const int ExpectedLotCount          = 7;
    private const int ExpectedAcceptedSlotCount = 7;
    private const int ExpectedOverlayFeatures   = 15;
    private const int ExpectedCsvFeatures       = 15;
    private const int ExpectedSourceWidth       = 256;
    private const int ExpectedSourceHeight      = 256;
    private const int ExpectedBboxCount         = 1;
    private const int ExpectedLotRectCount      = 7;
    private const int ExpectedSlotRectCount     = 7;

    public DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult Build(
        string geometryMvpPath,
        string qaOverlayJsonPath,
        string qaOverlayCsvPath,
        string qaOverlayPngPath)
    {
        var checks = new List<DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketCheck>();
        var errors = new List<string>();
        int order  = 1;

        // --- file existence checks ---

        bool mvpExists = File.Exists(geometryMvpPath);
        AddCheck(checks, order++, "GEOMETRY_MVP_EXISTS", "MAP-26A geometry MVP JSON exists",
            "true", mvpExists.ToString().ToLower(),
            mvpExists ? "File found." : $"Not found: {geometryMvpPath}");
        if (!mvpExists) errors.Add($"Geometry MVP JSON not found: {geometryMvpPath}");

        bool overlayJsonExists = File.Exists(qaOverlayJsonPath);
        AddCheck(checks, order++, "QA_OVERLAY_JSON_EXISTS", "MAP-26B QA overlay JSON exists",
            "true", overlayJsonExists.ToString().ToLower(),
            overlayJsonExists ? "File found." : $"Not found: {qaOverlayJsonPath}");
        if (!overlayJsonExists) errors.Add($"QA overlay JSON not found: {qaOverlayJsonPath}");

        bool overlayCsvExists = File.Exists(qaOverlayCsvPath);
        AddCheck(checks, order++, "QA_OVERLAY_CSV_EXISTS", "MAP-26B QA overlay CSV exists",
            "true", overlayCsvExists.ToString().ToLower(),
            overlayCsvExists ? "File found." : $"Not found: {qaOverlayCsvPath}");
        if (!overlayCsvExists) errors.Add($"QA overlay CSV not found: {qaOverlayCsvPath}");

        bool overlayPngExists = File.Exists(qaOverlayPngPath);
        AddCheck(checks, order++, "QA_OVERLAY_PNG_EXISTS", "MAP-26B QA overlay PNG exists",
            "true", overlayPngExists.ToString().ToLower(),
            overlayPngExists ? "File found." : $"Not found: {qaOverlayPngPath}");
        if (!overlayPngExists) errors.Add($"QA overlay PNG not found: {qaOverlayPngPath}");

        if (errors.Count > 0)
            return Invalid(geometryMvpPath, qaOverlayJsonPath, qaOverlayCsvPath, qaOverlayPngPath, errors, checks, order);

        // --- parse MVP ---
        DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult? mvp;
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            mvp = JsonSerializer.Deserialize<DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult>(
                File.ReadAllText(geometryMvpPath), opts);
        }
        catch (Exception ex)
        {
            errors.Add($"Geometry MVP JSON cannot be parsed: {ex.Message}");
            return Invalid(geometryMvpPath, qaOverlayJsonPath, qaOverlayCsvPath, qaOverlayPngPath, errors, checks, order);
        }
        if (mvp == null || mvp.ComponentGeometry == null || mvp.LotGeometry.Count == 0)
        {
            errors.Add("Geometry MVP JSON missing expected records.");
            return Invalid(geometryMvpPath, qaOverlayJsonPath, qaOverlayCsvPath, qaOverlayPngPath, errors, checks, order);
        }

        // --- parse overlay JSON ---
        DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult? overlay;
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            overlay = JsonSerializer.Deserialize<DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult>(
                File.ReadAllText(qaOverlayJsonPath), opts);
        }
        catch (Exception ex)
        {
            errors.Add($"QA overlay JSON cannot be parsed: {ex.Message}");
            return Invalid(geometryMvpPath, qaOverlayJsonPath, qaOverlayCsvPath, qaOverlayPngPath, errors, checks, order);
        }
        if (overlay == null)
        {
            errors.Add("QA overlay JSON deserialized to null.");
            return Invalid(geometryMvpPath, qaOverlayJsonPath, qaOverlayCsvPath, qaOverlayPngPath, errors, checks, order);
        }

        // --- parse overlay CSV ---
        List<string[]> csvRows;
        try
        {
            var lines = File.ReadAllLines(qaOverlayCsvPath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
            // first line is header
            csvRows = lines.Skip(1).Select(l => l.Split(',')).ToList();
        }
        catch (Exception ex)
        {
            errors.Add($"QA overlay CSV cannot be parsed: {ex.Message}");
            return Invalid(geometryMvpPath, qaOverlayJsonPath, qaOverlayCsvPath, qaOverlayPngPath, errors, checks, order);
        }

        // --- derived values ---
        var comp       = mvp.ComponentGeometry;
        string mvpMapId       = mvp.TileId ?? string.Empty;
        string overlayMapId   = overlay.MapId;
        string mvpCompId      = comp.ComponentId;
        string overlayCompId  = overlay.TargetComponentId;

        // MAP_ID_MATCHES
        bool mapIdMatch = string.Equals(mvpMapId, overlayMapId, StringComparison.Ordinal);
        AddCheck(checks, order++, "MAP_ID_MATCHES", "MAP-26A and MAP-26B map IDs match",
            mvpMapId, overlayMapId,
            mapIdMatch ? "IDs match." : $"MVP={mvpMapId}, overlay={overlayMapId}");
        if (!mapIdMatch) errors.Add($"Map ID mismatch: MVP={mvpMapId}, overlay={overlayMapId}.");

        // COMPONENT_ID_MATCHES
        bool compIdMatch = string.Equals(mvpCompId, overlayCompId, StringComparison.Ordinal);
        AddCheck(checks, order++, "COMPONENT_ID_MATCHES", "MAP-26A and MAP-26B component IDs match",
            mvpCompId, overlayCompId,
            compIdMatch ? "IDs match." : $"MVP={mvpCompId}, overlay={overlayCompId}");
        if (!compIdMatch) errors.Add($"Component ID mismatch: MVP={mvpCompId}, overlay={overlayCompId}.");

        // COMPONENT_BBOX_MATCHES
        var overlayBbox = overlay.ComponentBbox;
        bool bboxMatch = overlayBbox != null &&
            comp.MinX == overlayBbox.MinX && comp.MinY == overlayBbox.MinY &&
            comp.MaxX == overlayBbox.MaxX && comp.MaxY == overlayBbox.MaxY &&
            comp.WidthPx == overlayBbox.WidthPx && comp.HeightPx == overlayBbox.HeightPx;
        string mvpBboxStr = $"({comp.MinX},{comp.MinY})->({comp.MaxX},{comp.MaxY}) {comp.WidthPx}x{comp.HeightPx}px";
        string overlayBboxStr = overlayBbox != null
            ? $"({overlayBbox.MinX},{overlayBbox.MinY})->({overlayBbox.MaxX},{overlayBbox.MaxY}) {overlayBbox.WidthPx}x{overlayBbox.HeightPx}px"
            : "null";
        AddCheck(checks, order++, "COMPONENT_BBOX_MATCHES", "MAP-26A and MAP-26B component bboxes match",
            mvpBboxStr, overlayBboxStr,
            bboxMatch ? "Bboxes match." : $"MVP={mvpBboxStr}, overlay={overlayBboxStr}");
        if (!bboxMatch) errors.Add($"Component bbox mismatch: MVP={mvpBboxStr}, overlay={overlayBboxStr}.");

        // LOT_COUNT_IS_7
        int lotCount = overlay.LotCount;
        bool lotCountOk = lotCount == ExpectedLotCount;
        AddCheck(checks, order++, "LOT_COUNT_IS_7", "Lot count is 7",
            ExpectedLotCount.ToString(), lotCount.ToString(),
            lotCountOk ? "Lot count correct." : $"Expected {ExpectedLotCount}, got {lotCount}.");
        if (!lotCountOk) errors.Add($"Lot count is {lotCount}, expected {ExpectedLotCount}.");

        // ACCEPTED_BUILDING_SLOT_COUNT_IS_7
        int slotCount = overlay.AcceptedBuildingSlotCount;
        bool slotCountOk = slotCount == ExpectedAcceptedSlotCount;
        AddCheck(checks, order++, "ACCEPTED_BUILDING_SLOT_COUNT_IS_7", "Accepted building slot count is 7",
            ExpectedAcceptedSlotCount.ToString(), slotCount.ToString(),
            slotCountOk ? "Slot count correct." : $"Expected {ExpectedAcceptedSlotCount}, got {slotCount}.");
        if (!slotCountOk) errors.Add($"Accepted building slot count is {slotCount}, expected {ExpectedAcceptedSlotCount}.");

        // OVERLAY_FEATURE_COUNT_IS_15
        int featureCount = overlay.OverlayFeatureCount;
        bool featureCountOk = featureCount == ExpectedOverlayFeatures;
        AddCheck(checks, order++, "OVERLAY_FEATURE_COUNT_IS_15", "Overlay feature count is 15",
            ExpectedOverlayFeatures.ToString(), featureCount.ToString(),
            featureCountOk ? "Feature count correct." : $"Expected {ExpectedOverlayFeatures}, got {featureCount}.");
        if (!featureCountOk) errors.Add($"Overlay feature count is {featureCount}, expected {ExpectedOverlayFeatures}.");

        // CSV_FEATURE_COUNT_IS_15
        int csvFeatureCount = csvRows.Count;
        bool csvCountOk = csvFeatureCount == ExpectedCsvFeatures;
        AddCheck(checks, order++, "CSV_FEATURE_COUNT_IS_15", "CSV feature row count is 15",
            ExpectedCsvFeatures.ToString(), csvFeatureCount.ToString(),
            csvCountOk ? "CSV row count correct." : $"Expected {ExpectedCsvFeatures}, got {csvFeatureCount}.");
        if (!csvCountOk) errors.Add($"CSV feature count is {csvFeatureCount}, expected {ExpectedCsvFeatures}.");

        // CSV_FEATURE_BREAKDOWN_IS_1_7_7
        int csvBboxCount = csvRows.Count(r => r.Length > 1 && r[1] == "COMPONENT_BBOX");
        int csvLotCount  = csvRows.Count(r => r.Length > 1 && r[1] == "LOT_RECTANGLE");
        int csvSlotCount = csvRows.Count(r => r.Length > 1 && r[1] == "BUILDING_SLOT_RECTANGLE");
        bool breakdownOk = csvBboxCount == ExpectedBboxCount && csvLotCount == ExpectedLotRectCount && csvSlotCount == ExpectedSlotRectCount;
        string breakdownActual = $"{csvBboxCount}/{csvLotCount}/{csvSlotCount}";
        string breakdownExpected = $"{ExpectedBboxCount}/{ExpectedLotRectCount}/{ExpectedSlotRectCount}";
        AddCheck(checks, order++, "CSV_FEATURE_BREAKDOWN_IS_1_7_7", "CSV feature breakdown is 1 COMPONENT_BBOX / 7 LOT_RECTANGLE / 7 BUILDING_SLOT_RECTANGLE",
            breakdownExpected, breakdownActual,
            breakdownOk ? "Breakdown correct." : $"Expected {breakdownExpected}, got {breakdownActual}.");
        if (!breakdownOk) errors.Add($"CSV feature breakdown is {breakdownActual}, expected {breakdownExpected}.");

        // INCLUSIVE_RIGHT_BOTTOM_SEMANTICS — check every feature in overlay JSON
        var badSemantics = overlay.Features
            .Where(f => f.Right != f.X + f.Width - 1 || f.Bottom != f.Y + f.Height - 1)
            .Select(f => f.FeatureId)
            .ToList();
        bool semanticsOk = badSemantics.Count == 0;
        AddCheck(checks, order++, "INCLUSIVE_RIGHT_BOTTOM_SEMANTICS", "All features use inclusive right/bottom semantics (right = x+width-1, bottom = y+height-1)",
            "all features", semanticsOk ? "all features" : string.Join(", ", badSemantics),
            semanticsOk ? "All features use inclusive semantics." : $"Non-inclusive: {string.Join(", ", badSemantics)}");
        if (!semanticsOk) errors.Add($"Inclusive semantics violated on features: {string.Join(", ", badSemantics)}.");

        // RECTANGLES_INSIDE_256_BOUNDS
        var outOfBounds = overlay.Features
            .Where(f => f.X < 0 || f.Y < 0 || f.Right >= ExpectedSourceWidth || f.Bottom >= ExpectedSourceHeight)
            .Select(f => f.FeatureId)
            .ToList();
        bool boundsOk = outOfBounds.Count == 0;
        AddCheck(checks, order++, "RECTANGLES_INSIDE_256_BOUNDS", "All rectangles are inside 256x256 source bounds",
            $"all inside {ExpectedSourceWidth}x{ExpectedSourceHeight}",
            boundsOk ? "all inside bounds" : string.Join(", ", outOfBounds),
            boundsOk ? "All rectangles inside bounds." : $"Out of bounds: {string.Join(", ", outOfBounds)}");
        if (!boundsOk) errors.Add($"Rectangles outside bounds: {string.Join(", ", outOfBounds)}.");

        // WRITER_READY_FALSE
        bool writerReadyFalse = !overlay.WriterReady;
        AddCheck(checks, order++, "WRITER_READY_FALSE", "writer_ready is false",
            "false", overlay.WriterReady.ToString().ToLower(),
            writerReadyFalse ? "writer_ready is false." : "writer_ready is true — VIOLATION.");
        if (!writerReadyFalse) errors.Add("writer_ready is true — boundary violation.");

        // RUNTIME_VALID_FALSE
        bool runtimeValidFalse = !overlay.RuntimeValid;
        AddCheck(checks, order++, "RUNTIME_VALID_FALSE", "runtime_valid is false",
            "false", overlay.RuntimeValid.ToString().ToLower(),
            runtimeValidFalse ? "runtime_valid is false." : "runtime_valid is true — VIOLATION.");
        if (!runtimeValidFalse) errors.Add("runtime_valid is true — boundary violation.");

        // MATERIALIZED_FALSE
        bool materializedFalse = !overlay.Materialized;
        AddCheck(checks, order++, "MATERIALIZED_FALSE", "materialized is false",
            "false", overlay.Materialized.ToString().ToLower(),
            materializedFalse ? "materialized is false." : "materialized is true — VIOLATION.");
        if (!materializedFalse) errors.Add("materialized is true — boundary violation.");

        // NO_FORBIDDEN_WRITER_RUNTIME_MATERIALIZATION_CLAIMS
        string overlayVerdict = overlay.Verdict ?? string.Empty;
        bool noForbiddenClaims = !overlayVerdict.Contains("WRITER_READY", StringComparison.OrdinalIgnoreCase) &&
            !overlayVerdict.Contains("RUNTIME_VALID", StringComparison.OrdinalIgnoreCase) &&
            !overlayVerdict.Contains("MATERIALIZED", StringComparison.OrdinalIgnoreCase);
        AddCheck(checks, order++, "NO_FORBIDDEN_WRITER_RUNTIME_MATERIALIZATION_CLAIMS",
            "Overlay verdict contains no forbidden writer/runtime/materialization claims",
            "no forbidden claims", noForbiddenClaims ? "no forbidden claims" : $"forbidden term in verdict: {overlayVerdict}",
            noForbiddenClaims ? "No forbidden claims in verdict." : $"Forbidden term found in overlay verdict: {overlayVerdict}");
        if (!noForbiddenClaims) errors.Add($"Forbidden claim in overlay verdict: {overlayVerdict}.");

        // --- build result ---
        int passed = checks.Count(c => c.CheckStatus == "PASS");
        int failed = checks.Count(c => c.CheckStatus == "FAIL");

        var featureBreakdown = new List<DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketFeatureBreakdown>
        {
            new() { FeatureKind = "COMPONENT_BBOX",         Count = csvBboxCount },
            new() { FeatureKind = "LOT_RECTANGLE",          Count = csvLotCount  },
            new() { FeatureKind = "BUILDING_SLOT_RECTANGLE", Count = csvSlotCount },
        };

        bool isValid   = errors.Count == 0;
        string verdict = isValid
            ? "MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE"
            : "MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_INVALID";

        string bboxStr = overlayBbox != null
            ? $"({overlayBbox.MinX},{overlayBbox.MinY})->({overlayBbox.MaxX},{overlayBbox.MaxY}) {overlayBbox.WidthPx}x{overlayBbox.HeightPx}px"
            : string.Empty;

        return new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult
        {
            Format                    = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-qa-review-packet.v1",
            GeneratedUtc              = DateTime.UtcNow.ToString("o"),
            MapId                     = overlayMapId,
            GeometryMvpPath           = geometryMvpPath,
            QaOverlayJsonPath         = qaOverlayJsonPath,
            QaOverlayCsvPath          = qaOverlayCsvPath,
            QaOverlayPngPath          = qaOverlayPngPath,
            TargetComponentId         = overlayCompId,
            TargetComponentOrder      = overlay.TargetComponentOrder,
            Intent                    = overlay.Intent,
            AccessReadinessClass      = overlay.AccessReadinessClass,
            ComponentBbox             = overlayBbox != null ? new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBbox
            {
                MinX    = overlayBbox.MinX,    MinY    = overlayBbox.MinY,
                MaxX    = overlayBbox.MaxX,    MaxY    = overlayBbox.MaxY,
                WidthPx = overlayBbox.WidthPx, HeightPx = overlayBbox.HeightPx,
            } : null,
            SourceDimensions          = $"{ExpectedSourceWidth}x{ExpectedSourceHeight}",
            LotCount                  = lotCount,
            AcceptedBuildingSlotCount = slotCount,
            OverlayFeatureCount       = featureCount,
            CsvFeatureCount           = csvFeatureCount,
            ReviewCheckCount          = checks.Count,
            PassedReviewCheckCount    = passed,
            FailedReviewCheckCount    = failed,
            WriterReady               = false,
            RuntimeValid              = false,
            Materialized              = false,
            IsValid                   = isValid,
            Verdict                   = verdict,
            Checks                    = checks,
            FeatureBreakdown          = featureBreakdown,
            Errors                    = errors,
        };
    }

    // -----------------------------------------------------------------------

    private static void AddCheck(
        List<DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketCheck> checks,
        int order, string id, string label, string expected, string actual, string details)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal) ||
            (expected == "true"  && actual == "true")  ||
            (expected == "false" && actual == "false") ||
            details.StartsWith("File found", StringComparison.Ordinal) ||
            details.EndsWith("correct.", StringComparison.Ordinal) ||
            details.StartsWith("IDs match", StringComparison.Ordinal) ||
            details.StartsWith("Bboxes match", StringComparison.Ordinal) ||
            details.StartsWith("All features", StringComparison.Ordinal) ||
            details.StartsWith("All rectangles", StringComparison.Ordinal) ||
            details.StartsWith("writer_ready is false", StringComparison.Ordinal) ||
            details.StartsWith("runtime_valid is false", StringComparison.Ordinal) ||
            details.StartsWith("materialized is false", StringComparison.Ordinal) ||
            details.StartsWith("No forbidden claims", StringComparison.Ordinal);

        checks.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketCheck
        {
            CheckOrder   = order,
            CheckId      = id,
            CheckLabel   = label,
            CheckStatus  = pass ? "PASS" : "FAIL",
            Expected     = expected,
            Actual       = actual,
            Details      = details,
        });
    }

    private static DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult Invalid(
        string geometryMvpPath,
        string qaOverlayJsonPath,
        string qaOverlayCsvPath,
        string qaOverlayPngPath,
        List<string> errors,
        List<DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketCheck> checks,
        int nextOrder) =>
        new()
        {
            Format            = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-qa-review-packet.v1",
            GeneratedUtc      = DateTime.UtcNow.ToString("o"),
            GeometryMvpPath   = geometryMvpPath,
            QaOverlayJsonPath = qaOverlayJsonPath,
            QaOverlayCsvPath  = qaOverlayCsvPath,
            QaOverlayPngPath  = qaOverlayPngPath,
            Errors            = errors,
            Checks            = checks,
            ReviewCheckCount  = checks.Count,
            PassedReviewCheckCount = checks.Count(c => c.CheckStatus == "PASS"),
            FailedReviewCheckCount = checks.Count(c => c.CheckStatus == "FAIL"),
            IsValid           = false,
            Verdict           = "MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_INVALID",
        };

    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult result)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(result, opts);
    }

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-26C WorldBuilder Minimal Concrete Geometry QA Review Packet");
        sb.AppendLine();
        sb.AppendLine($"- map id: {result.MapId}");
        sb.AppendLine($"- geometry MVP path: {result.GeometryMvpPath}");
        sb.AppendLine($"- QA overlay JSON path: {result.QaOverlayJsonPath}");
        sb.AppendLine($"- QA overlay CSV path: {result.QaOverlayCsvPath}");
        sb.AppendLine($"- QA overlay PNG path: {result.QaOverlayPngPath}");
        sb.AppendLine($"- target component id/order: {result.TargetComponentId} (order {result.TargetComponentOrder})");
        sb.AppendLine($"- intent: {result.Intent}");
        sb.AppendLine($"- access class: {result.AccessReadinessClass}");
        if (result.ComponentBbox is { } bbox)
            sb.AppendLine($"- component bbox: ({bbox.MinX},{bbox.MinY})->({bbox.MaxX},{bbox.MaxY}) {bbox.WidthPx}x{bbox.HeightPx}px");
        sb.AppendLine($"- source dimensions: {result.SourceDimensions}");
        sb.AppendLine($"- lot count: {result.LotCount}");
        sb.AppendLine($"- accepted building slot count: {result.AcceptedBuildingSlotCount}");
        sb.AppendLine($"- overlay feature count: {result.OverlayFeatureCount}");
        sb.AppendLine($"- CSV feature count: {result.CsvFeatureCount}");
        sb.AppendLine($"- review check count: {result.ReviewCheckCount}");
        sb.AppendLine($"- passed review check count: {result.PassedReviewCheckCount}");
        sb.AppendLine($"- failed review check count: {result.FailedReviewCheckCount}");
        sb.AppendLine($"- writer_ready: {result.WriterReady}");
        sb.AppendLine($"- runtime_valid: {result.RuntimeValid}");
        sb.AppendLine($"- materialized: {result.Materialized}");
        sb.AppendLine($"- verdict: {result.Verdict}");
        sb.AppendLine();
        sb.AppendLine("## Review Checks");
        sb.AppendLine();
        sb.AppendLine("| check_order | check_id | check_label | check_status | expected | actual | details |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | {c.CheckId} | {c.CheckLabel} | {c.CheckStatus} | {c.Expected} | {c.Actual} | {c.Details} |");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("This review packet is QA-only.");
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

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult result)
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

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult result)
    {
        var bbox = result.ComponentBbox;
        string bboxStr = bbox != null
            ? $"({bbox.MinX},{bbox.MinY})->({bbox.MaxX},{bbox.MaxY}) {bbox.WidthPx}x{bbox.HeightPx}px"
            : string.Empty;
        return $"""
map_id                  : {result.MapId}
target_component        : {result.TargetComponentId}
intent                  : {result.Intent}
access_class            : {result.AccessReadinessClass}
component_bbox          : {bboxStr}
source_dimensions       : {result.SourceDimensions}
lot_rectangles          : {result.LotCount}
building_slot_rectangles: {result.AcceptedBuildingSlotCount}
overlay_features        : {result.OverlayFeatureCount}
csv_features            : {result.CsvFeatureCount}
review_checks           : {result.ReviewCheckCount}
passed_review_checks    : {result.PassedReviewCheckCount}
failed_review_checks    : {result.FailedReviewCheckCount}
writer_ready            : {(result.WriterReady ? 1 : 0)}
runtime_valid           : {(result.RuntimeValid ? 1 : 0)}
materialized            : {(result.Materialized ? 1 : 0)}
verdict                 : {result.Verdict}
""";
    }
}
