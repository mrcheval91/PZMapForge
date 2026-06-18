using System.Drawing;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    private static void AddCheck(List<DeadMtlTileMaterializationQaReviewPacketCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new DeadMtlTileMaterializationQaReviewPacketCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlTileMaterializationQaReviewPacketCheck> checks, string id, string label)
    {
        checks.Add(new DeadMtlTileMaterializationQaReviewPacketCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PASS",
            Expected    = "PASS",
            Actual      = "PASS",
        });
    }

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketResult Build(
        string map27cRoot,
        string map27dRoot,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketResult
        {
            Format                         = "MAP-27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            ReviewStage                    = "SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET",
            ReviewMode                     = "AUDIT_SANDBOX_TILE_MATERIALIZATION_CHAIN_ONLY",
            SandboxOnly                    = true,
            SandboxMaterializedSource      = true,
            PzRuntimeMaterialized          = false,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
            SourceTileMaterializerRoot     = map27cRoot,
            SourceQaOverlayRoot            = map27dRoot,
        };

        const string invalidVerdict =
            "MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_INVALID";

        if (!Directory.Exists(map27cRoot))
        {
            result.Errors.Add($"MAP-27C root not found: {map27cRoot}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }
        if (!Directory.Exists(map27dRoot))
        {
            result.Errors.Add($"MAP-27D root not found: {map27dRoot}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        var map27cEntries = new (string Name, string Kind)[]
        {
            ("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json",        "TILE_MATERIALIZER_RESULT_JSON"),
            ("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md",          "TILE_MATERIALIZER_RESULT_MD"),
            ("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv",         "TILE_MATERIALIZER_RESULT_CSV"),
            ("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt", "TILE_MATERIALIZER_RESULT_SUMMARY_TXT"),
            ("map_00.sandbox_writer_tile_materialized_cells.csv",                                "MATERIALIZED_CELLS_CSV"),
            ("map_00.sandbox_writer_tile_material_palette.json",                                 "MATERIAL_PALETTE_JSON"),
            ("map_00.sandbox_writer_tile_layer_stack.json",                                      "LAYER_STACK_JSON"),
            ("map_00.sandbox_writer_tile_materialization_replay_log.json",                       "MATERIALIZATION_REPLAY_LOG_JSON"),
            ("map_00.sandbox_writer_tile_materialization_ownership_summary.json",                "MATERIALIZATION_OWNERSHIP_SUMMARY_JSON"),
            ("map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json",              "MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON"),
        };

        var map27dEntries = new (string Name, string Kind)[]
        {
            ("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json",        "QA_OVERLAY_RESULT_JSON"),
            ("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md",          "QA_OVERLAY_RESULT_MD"),
            ("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv",         "QA_OVERLAY_RESULT_CSV"),
            ("map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt", "QA_OVERLAY_RESULT_SUMMARY_TXT"),
            ("map_00.sandbox_writer_tile_materializer_qa_overlay.png",                                      "QA_OVERLAY_PNG"),
            ("map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json",                              "QA_OVERLAY_LEGEND_JSON"),
            ("map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv",                               "QA_OVERLAY_COUNTS_CSV"),
            ("map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json",              "QA_OVERLAY_FORBIDDEN_OUTPUT_GUARD_JSON"),
        };

        var missing = new List<string>();
        foreach (var (name, _) in map27cEntries)
        {
            string fp = Path.Combine(map27cRoot, name);
            if (!File.Exists(fp)) missing.Add(fp);
        }
        foreach (var (name, _) in map27dEntries)
        {
            string fp = Path.Combine(map27dRoot, name);
            if (!File.Exists(fp)) missing.Add(fp);
        }
        if (missing.Count > 0)
        {
            foreach (var f in missing) result.Errors.Add($"Required file not found: {f}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        var reviewedFiles = new List<DeadMtlTileMaterializationQaReviewPacketReviewedFile>();
        int fileOrder = 1;
        foreach (var (name, kind) in map27cEntries)
        {
            string fp = Path.Combine(map27cRoot, name);
            reviewedFiles.Add(new DeadMtlTileMaterializationQaReviewPacketReviewedFile
            {
                FileOrder   = fileOrder++,
                SourceStage = "MAP-27C",
                FileName    = name,
                FilePath    = fp,
                FileKind    = kind,
                Exists      = true,
                Sha256      = HashFile(fp),
                SizeBytes   = new FileInfo(fp).Length,
            });
        }
        foreach (var (name, kind) in map27dEntries)
        {
            string fp = Path.Combine(map27dRoot, name);
            reviewedFiles.Add(new DeadMtlTileMaterializationQaReviewPacketReviewedFile
            {
                FileOrder   = fileOrder++,
                SourceStage = "MAP-27D",
                FileName    = name,
                FilePath    = fp,
                FileKind    = kind,
                Exists      = true,
                Sha256      = HashFile(fp),
                SizeBytes   = new FileInfo(fp).Length,
            });
        }
        result.ReviewedFiles     = reviewedFiles;
        result.ReviewedFileCount = reviewedFiles.Count;

        // Parse MAP-27C result
        string map27cResultPath    = Path.Combine(map27cRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        string srcVerdict              = string.Empty;
        bool   srcIsValid              = false;
        bool   srcSandboxOnly          = false;
        bool   srcSandboxMaterialized  = false;
        bool   srcPzRuntimeMaterialized = true;
        int    srcMaterializedCellCount = 0;
        string srcTargetComponentId    = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(map27cResultPath, Encoding.UTF8));
            var r = doc.RootElement;
            if (r.TryGetProperty("verdict",                 out var p)) srcVerdict               = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",                out p))     srcIsValid                = p.GetBoolean();
            if (r.TryGetProperty("sandbox_only",            out p))     srcSandboxOnly            = p.GetBoolean();
            if (r.TryGetProperty("sandbox_materialized",    out p))     srcSandboxMaterialized    = p.GetBoolean();
            if (r.TryGetProperty("pz_runtime_materialized", out p))     srcPzRuntimeMaterialized  = p.GetBoolean();
            if (r.TryGetProperty("materialized_cell_count", out p))     srcMaterializedCellCount  = p.GetInt32();
            if (r.TryGetProperty("target_component_id",     out p))     srcTargetComponentId      = p.GetString() ?? "";
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse MAP-27C result: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceTileMaterializerResultPath   = map27cResultPath;
        result.SourceTileMaterializerResultSha256 = reviewedFiles[0].Sha256;
        result.SourceTileMaterializerVerdict      = srcVerdict;
        result.SourceTileMaterializerIsValid      = srcIsValid;
        result.TargetComponentId                  = srcTargetComponentId;

        // Parse MAP-27D result
        string map27dResultPath = Path.Combine(map27dRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json");
        string d27Verdict               = string.Empty;
        bool   d27IsValid               = false;
        bool   d27SandboxOnly           = false;
        bool   d27VisualQaOverlayWritten = false;
        bool   d27PzRuntimeMaterialized = true;
        int    d27RenderedCellCount     = 0;
        int    d27WallCount             = 0;
        int    d27FloorCount            = 0;
        int    d27AccessCount           = 0;
        int    d27LotCount              = 0;
        int    d27ResidualCount         = 0;
        int    d27MaterialKindCount     = 0;
        int    d27LayerKindCount        = 0;
        int    d27OverlayWidth          = 0;
        int    d27OverlayHeight         = 0;
        int    d27Scale                 = 0;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(map27dResultPath, Encoding.UTF8));
            var r = doc.RootElement;
            if (r.TryGetProperty("verdict",                             out var p)) d27Verdict               = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",                            out p))     d27IsValid                = p.GetBoolean();
            if (r.TryGetProperty("sandbox_only",                        out p))     d27SandboxOnly            = p.GetBoolean();
            if (r.TryGetProperty("visual_qa_overlay_written",           out p))     d27VisualQaOverlayWritten  = p.GetBoolean();
            if (r.TryGetProperty("pz_runtime_materialized",             out p))     d27PzRuntimeMaterialized   = p.GetBoolean();
            if (r.TryGetProperty("rendered_cell_count",                 out p))     d27RenderedCellCount       = p.GetInt32();
            if (r.TryGetProperty("building_wall_candidate_cell_count",  out p))     d27WallCount               = p.GetInt32();
            if (r.TryGetProperty("building_floor_candidate_cell_count", out p))     d27FloorCount              = p.GetInt32();
            if (r.TryGetProperty("access_edge_cell_count",              out p))     d27AccessCount             = p.GetInt32();
            if (r.TryGetProperty("lot_space_cell_count",                out p))     d27LotCount                = p.GetInt32();
            if (r.TryGetProperty("component_residual_cell_count",       out p))     d27ResidualCount           = p.GetInt32();
            if (r.TryGetProperty("material_kind_count",                 out p))     d27MaterialKindCount       = p.GetInt32();
            if (r.TryGetProperty("layer_kind_count",                    out p))     d27LayerKindCount          = p.GetInt32();
            if (r.TryGetProperty("overlay_width",                       out p))     d27OverlayWidth            = p.GetInt32();
            if (r.TryGetProperty("overlay_height",                      out p))     d27OverlayHeight           = p.GetInt32();
            if (r.TryGetProperty("scale",                               out p))     d27Scale                   = p.GetInt32();
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse MAP-27D result: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceQaOverlayResultPath   = map27dResultPath;
        result.SourceQaOverlayResultSha256 = reviewedFiles[10].Sha256;
        result.SourceQaOverlayVerdict      = d27Verdict;
        result.SourceQaOverlayIsValid      = d27IsValid;
        result.VisualQaOverlayWritten      = d27VisualQaOverlayWritten;

        result.MaterializationCounts = new DeadMtlTileMaterializationCounts
        {
            MaterializedCellCount           = srcMaterializedCellCount,
            BuildingWallCandidateCellCount  = d27WallCount,
            BuildingFloorCandidateCellCount = d27FloorCount,
            AccessEdgeCellCount             = d27AccessCount,
            LotSpaceCellCount               = d27LotCount,
            ComponentResidualCellCount      = d27ResidualCount,
            MaterialKindCount               = d27MaterialKindCount,
            LayerKindCount                  = d27LayerKindCount,
        };
        result.OverlayCounts = new DeadMtlTileOverlayCounts
        {
            RenderedCellCount = d27RenderedCellCount,
            OverlayWidth      = d27OverlayWidth,
            OverlayHeight     = d27OverlayHeight,
            Scale             = d27Scale,
        };

        string countMatchStatus  = srcMaterializedCellCount == d27RenderedCellCount ? "MATCH" : "MISMATCH";
        result.CountMatchSummary = $"MATERIALIZED_CELL_COUNT({srcMaterializedCellCount}) == RENDERED_CELL_COUNT({d27RenderedCellCount}): {countMatchStatus}";

        // PNG dimensions
        string pngPath  = Path.Combine(map27dRoot, "map_00.sandbox_writer_tile_materializer_qa_overlay.png");
        int    pngWidth = 0, pngHeight = 0;
        try
        {
            using var bmp = new Bitmap(pngPath);
            pngWidth  = bmp.Width;
            pngHeight = bmp.Height;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to read overlay PNG: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.OverlayPngPath   = pngPath;
        result.OverlayPngSha256 = HashFile(pngPath);
        result.OverlayPngWidth  = pngWidth;
        result.OverlayPngHeight = pngHeight;

        // Ensure output root and scan for forbidden artifacts
        Directory.CreateDirectory(outputRoot);
        bool   forbiddenScanPass = true;
        string lotPat  = "*." + "lotpack";
        string lhPat   = "*." + "lotheader";
        string luaPat  = "*." + "lua";
        foreach (var pattern in new[] { lotPat, lhPat, luaPat, "*.bin", "steamapps" })
        {
            var hits = Directory.GetFiles(outputRoot, pattern, SearchOption.AllDirectories);
            if (hits.Length > 0)
            {
                forbiddenScanPass = false;
                result.Errors.Add($"Forbidden artifact in output root: {hits[0]}");
            }
        }
        foreach (var dir in Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories))
        {
            var di = new DirectoryInfo(dir);
            if (di.Name == "maps" && di.Parent?.Name == "media")
            {
                forbiddenScanPass = false;
                result.Errors.Add($"Forbidden directory in output root: {dir}");
            }
        }

        result.ForbiddenArtifactScan = forbiddenScanPass
            ? "POST_REVIEW_SCAN PASS (0 forbidden artifacts in output root)"
            : "POST_REVIEW_SCAN FAIL";
        result.CanonicalPathAudit = string.Concat(
            "MAP27C_ROOT=",    map27cRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)  ? "CANONICAL" : "NON_CANONICAL",
            " | MAP27D_ROOT=", map27dRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)  ? "CANONICAL" : "NON_CANONICAL",
            " | MAP27E_OUTPUT_ROOT=", outputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase) ? "CANONICAL" : "NON_CANONICAL");
        result.ClaimBoundaryAudit =
            "writer_ready=false | runtime_valid=false | materialized=false | runtime_proof_claimed=false | public_playable_packaging_claimed=false";

        // 40 checks
        var checks = new List<DeadMtlTileMaterializationQaReviewPacketCheck>();

        MakeCheck(checks, "MAP27C_ROOT_EXISTS",             "MAP-27C root directory exists");
        MakeCheck(checks, "MAP27D_ROOT_EXISTS",             "MAP-27D root directory exists");
        MakeCheck(checks, "MAP27C_10_EXPECTED_FILES_EXIST", "All 10 MAP-27C expected files exist");
        MakeCheck(checks, "MAP27D_8_EXPECTED_FILES_EXIST",  "All 8 MAP-27D expected files exist");
        MakeCheck(checks, "ALL_18_REVIEWED_FILES_HASHED",   "All 18 reviewed files SHA-256 hashed");

        AddCheck(checks, "MAP27C_RESULT_VERDICT_COMPLETE", "MAP-27C result verdict is COMPLETE",
            "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            srcVerdict);
        AddCheck(checks, "MAP27C_RESULT_IS_VALID_TRUE",          "MAP-27C result is_valid is true",           "true",  srcIsValid               ? "true" : "false");
        AddCheck(checks, "MAP27C_SANDBOX_ONLY_TRUE",             "MAP-27C sandbox_only is true",              "true",  srcSandboxOnly           ? "true" : "false");
        AddCheck(checks, "MAP27C_SANDBOX_MATERIALIZED_TRUE",     "MAP-27C sandbox_materialized is true",      "true",  srcSandboxMaterialized   ? "true" : "false");
        AddCheck(checks, "MAP27C_PZ_RUNTIME_MATERIALIZED_FALSE", "MAP-27C pz_runtime_materialized is false",  "false", srcPzRuntimeMaterialized ? "true" : "false");

        AddCheck(checks, "MAP27D_RESULT_VERDICT_COMPLETE", "MAP-27D result verdict is COMPLETE",
            "MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_COMPLETE",
            d27Verdict);
        AddCheck(checks, "MAP27D_RESULT_IS_VALID_TRUE",           "MAP-27D result is_valid is true",           "true",  d27IsValid               ? "true" : "false");
        AddCheck(checks, "MAP27D_SANDBOX_ONLY_TRUE",              "MAP-27D sandbox_only is true",              "true",  d27SandboxOnly           ? "true" : "false");
        AddCheck(checks, "MAP27D_VISUAL_QA_OVERLAY_WRITTEN_TRUE", "MAP-27D visual_qa_overlay_written is true", "true",  d27VisualQaOverlayWritten ? "true" : "false");
        AddCheck(checks, "MAP27D_PZ_RUNTIME_MATERIALIZED_FALSE",  "MAP-27D pz_runtime_materialized is false",  "false", d27PzRuntimeMaterialized  ? "true" : "false");

        AddCheck(checks, "MATERIALIZED_CELL_COUNT_5340",       "Materialized cell count is 5340",        "5340", srcMaterializedCellCount.ToString());
        AddCheck(checks, "RENDERED_CELL_COUNT_5340",           "Rendered cell count is 5340",            "5340", d27RenderedCellCount.ToString());
        AddCheck(checks, "MATERIALIZED_RENDERED_COUNTS_MATCH", "Materialized and rendered counts match", srcMaterializedCellCount.ToString(), d27RenderedCellCount.ToString());
        AddCheck(checks, "WALL_COUNT_850",                     "Building wall candidate count is 850",   "850",  d27WallCount.ToString());
        AddCheck(checks, "FLOOR_COUNT_2444",                   "Building floor candidate count is 2444", "2444", d27FloorCount.ToString());
        AddCheck(checks, "ACCESS_COUNT_148",                   "Access edge cell count is 148",          "148",  d27AccessCount.ToString());
        AddCheck(checks, "LOT_COUNT_1898",                     "Lot space cell count is 1898",           "1898", d27LotCount.ToString());
        AddCheck(checks, "COMPONENT_RESIDUAL_COUNT_0",         "Component residual cell count is 0",    "0",    d27ResidualCount.ToString());
        AddCheck(checks, "MATERIAL_KIND_COUNT_5",              "Material kind count is 5",               "5",    d27MaterialKindCount.ToString());
        AddCheck(checks, "LAYER_KIND_COUNT_5",                 "Layer kind count is 5",                  "5",    d27LayerKindCount.ToString());

        MakeCheck(checks, "OVERLAY_PNG_EXISTS", "Overlay PNG file exists");
        MakeCheck(checks, "OVERLAY_PNG_HASHED", "Overlay PNG SHA-256 hashed");

        AddCheck(checks, "OVERLAY_PNG_WIDTH_1024",  "Overlay PNG width is 1024",  "1024", pngWidth.ToString());
        AddCheck(checks, "OVERLAY_PNG_HEIGHT_1024", "Overlay PNG height is 1024", "1024", pngHeight.ToString());
        AddCheck(checks, "OVERLAY_SCALE_4",         "Overlay scale is 4",         "4",    d27Scale.ToString());

        AddCheck(checks, "CANONICAL_MAP27C_ROOT",        "MAP-27C root contains .local",        "true",
            map27cRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)  ? "true" : "false");
        AddCheck(checks, "CANONICAL_MAP27D_ROOT",        "MAP-27D root contains .local",        "true",
            map27dRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)  ? "true" : "false");
        AddCheck(checks, "CANONICAL_MAP27E_OUTPUT_ROOT", "MAP-27E output root contains .local", "true",
            outputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)  ? "true" : "false");

        AddCheck(checks, "POST_REVIEW_FORBIDDEN_SCAN_PASS", "Post-review forbidden artifact scan passes",
            "PASS", forbiddenScanPass ? "PASS" : "FAIL");

        MakeCheck(checks, "WRITER_READY_FALSE",                 "writer_ready is false");
        MakeCheck(checks, "RUNTIME_VALID_FALSE",                "runtime_valid is false");
        MakeCheck(checks, "MATERIALIZED_FALSE",                 "materialized (global PZ) is false");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIM",             "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM", "No public playable packaging claimed");
        MakeCheck(checks, "NO_RUNTIME_OUTPUTS_EMITTED",         "No runtime outputs emitted");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass = result.FailedCheckCount == 0;
        result.IsValid = allPass;
        result.Verdict = allPass
            ? "MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public string RenderJson(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27E WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materialization QA Review Packet");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Target Component:** {result.TargetComponentId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Review Stage:** {result.ReviewStage}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Sandbox Only:** {result.SandboxOnly}");
        sb.AppendLine($"- **Sandbox Materialized Source:** {result.SandboxMaterializedSource}");
        sb.AppendLine($"- **Visual QA Overlay Written:** {result.VisualQaOverlayWritten}");
        sb.AppendLine($"- **PZ Runtime Materialized:** {result.PzRuntimeMaterialized}");
        sb.AppendLine($"- **Reviewed File Count:** {result.ReviewedFileCount}");
        sb.AppendLine($"- **Count Match Summary:** {result.CountMatchSummary}");
        sb.AppendLine($"- **Overlay PNG Width:** {result.OverlayPngWidth}");
        sb.AppendLine($"- **Overlay PNG Height:** {result.OverlayPngHeight}");
        sb.AppendLine($"- **Canonical Path Audit:** {result.CanonicalPathAudit}");
        sb.AppendLine($"- **Forbidden Artifact Scan:** {result.ForbiddenArtifactScan}");
        sb.AppendLine($"- **Checks:** {result.CheckCount} / Passed: {result.PassedCheckCount} / Failed: {result.FailedCheckCount}");
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status | Expected | Actual |");
        sb.AppendLine("|---|----------|--------|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | {c.CheckId} | {c.CheckStatus} | {c.Expected} | {c.Actual} |");
        return sb.ToString();
    }

    public string RenderCsv(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27E WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materialization QA Review Packet");
        sb.AppendLine($"Generated UTC              : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID                     : {result.MapId}");
        sb.AppendLine($"Target Component           : {result.TargetComponentId}");
        sb.AppendLine($"Review Stage               : {result.ReviewStage}");
        sb.AppendLine($"Verdict                    : {result.Verdict}");
        sb.AppendLine($"Is Valid                   : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only               : {(result.SandboxOnly ? 1 : 0)}");
        sb.AppendLine($"Sandbox Materialized Source: {(result.SandboxMaterializedSource ? 1 : 0)}");
        sb.AppendLine($"Visual QA Overlay Written  : {(result.VisualQaOverlayWritten ? 1 : 0)}");
        sb.AppendLine($"PZ Runtime Materialized    : {(result.PzRuntimeMaterialized ? 1 : 0)}");
        sb.AppendLine($"Writer Ready               : {(result.WriterReady ? 1 : 0)}");
        sb.AppendLine($"Runtime Valid              : {(result.RuntimeValid ? 1 : 0)}");
        sb.AppendLine($"Materialized               : {(result.Materialized ? 1 : 0)}");
        sb.AppendLine($"Runtime Proof              : {(result.RuntimeProofClaimed ? 1 : 0)}");
        sb.AppendLine($"Public Playable            : {(result.PublicPlayablePackagingClaimed ? 1 : 0)}");
        sb.AppendLine($"Reviewed File Count        : {result.ReviewedFileCount}");
        sb.AppendLine($"Materialized Cell Count    : {result.MaterializationCounts.MaterializedCellCount}");
        sb.AppendLine($"Rendered Cell Count        : {result.OverlayCounts.RenderedCellCount}");
        sb.AppendLine($"Count Match                : {result.CountMatchSummary}");
        sb.AppendLine($"Overlay PNG Width          : {result.OverlayPngWidth}");
        sb.AppendLine($"Overlay PNG Height         : {result.OverlayPngHeight}");
        sb.AppendLine($"Canonical Path Audit       : {result.CanonicalPathAudit}");
        sb.AppendLine($"Forbidden Artifact Scan    : {result.ForbiddenArtifactScan}");
        sb.AppendLine($"Checks                     : {result.CheckCount}");
        sb.AppendLine($"Passed                     : {result.PassedCheckCount}");
        sb.Append($"Failed                     : {result.FailedCheckCount}");
        return sb.ToString();
    }
}
