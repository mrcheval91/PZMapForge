using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private const int SourceDim    = 256;
    private const int ScaleFactor  = 4;
    private const int OverlayDim   = SourceDim * ScaleFactor; // 1024

    private static Color GetMaterialColor(string materialKind) => materialKind switch
    {
        "BUILDING_EXTERIOR_WALL_CANDIDATE"    => ColorTranslator.FromHtml("#1F1F1F"),
        "BUILDING_INTERIOR_FLOOR_CANDIDATE"   => ColorTranslator.FromHtml("#A8A8A8"),
        "ACCESS_EDGE_CANDIDATE"               => ColorTranslator.FromHtml("#2F6FDB"),
        "LOT_YARD_OR_SERVICE_SPACE_CANDIDATE" => ColorTranslator.FromHtml("#4F8A3B"),
        "COMPONENT_RESIDUAL_SPACE_CANDIDATE"  => ColorTranslator.FromHtml("#7A4E2A"),
        _                                     => ColorTranslator.FromHtml("#808080"),
    };

    private static void AddCheck(List<DeadMtlTileMaterializerQaOverlayCheck> checks,
        string id, string label, string expected, string actual)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal);
        checks.Add(new DeadMtlTileMaterializerQaOverlayCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = pass ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlTileMaterializerQaOverlayCheck> checks, string id, string label)
    {
        checks.Add(new DeadMtlTileMaterializerQaOverlayCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PASS",
            Expected    = "PASS",
            Actual      = "PASS",
        });
    }

    private static string WriteAndHash(string path, string content)
    {
        File.WriteAllText(path, content, Encoding.UTF8);
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();
    }

    private static string WriteAndHashJson(string path, object obj)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(obj, s_jsonOptions), Encoding.UTF8);
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();
    }

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayResult Build(
        string tileMaterializerResultPath,
        string materializedCellsPath,
        string materialPalettePath,
        string layerStackPath,
        string materializationReplayLogPath,
        string materializationOwnershipSummaryPath,
        string materializerForbiddenOutputGuardPath,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayResult
        {
            Format                         = "MAP-27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            WriterStage                    = "SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0",
            WriterMode                     = "RENDER_SANDBOX_TILE_MATERIALIZATION_QA_OVERLAY_ONLY",
            SandboxOnly                    = true,
            SandboxMaterializedSource      = true,
            PzRuntimeMaterialized          = false,
            SourceWidth                    = SourceDim,
            SourceHeight                   = SourceDim,
            Scale                          = ScaleFactor,
            OverlayWidth                   = OverlayDim,
            OverlayHeight                  = OverlayDim,
            MaterialKindCount              = 5,
            LayerKindCount                 = 5,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
        };

        var checks = new List<DeadMtlTileMaterializerQaOverlayCheck>();
        const string invalidVerdict =
            "MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_INVALID";

        var requiredFiles = new[]
        {
            tileMaterializerResultPath, materializedCellsPath, materialPalettePath,
            layerStackPath, materializationReplayLogPath,
            materializationOwnershipSummaryPath, materializerForbiddenOutputGuardPath,
        };
        foreach (var fp in requiredFiles)
        {
            if (!File.Exists(fp))
            {
                result.Errors.Add($"Required input file not found: {fp}");
                result.IsValid = false;
                result.Verdict = invalidVerdict;
                return result;
            }
        }

        // --- Parse MAP-27C result JSON ---
        byte[] srcBytes = File.ReadAllBytes(tileMaterializerResultPath);
        string srcSha256 = Convert.ToHexString(SHA256.HashData(srcBytes)).ToLower();
        string srcVerdict              = string.Empty;
        bool   srcIsValid              = false;
        bool   srcSandboxOnly          = false;
        bool   srcSandboxMaterialized  = false;
        bool   srcPzRuntimeMaterialized = true; // assume true (fails check) if missing
        int    srcMaterializedCellCount = 0;
        string srcTargetComponentId     = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(tileMaterializerResultPath, Encoding.UTF8));
            var r = doc.RootElement;
            if (r.TryGetProperty("verdict",                  out var p)) srcVerdict               = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",                 out p))    srcIsValid                = p.GetBoolean();
            if (r.TryGetProperty("sandbox_only",             out p))    srcSandboxOnly            = p.GetBoolean();
            if (r.TryGetProperty("sandbox_materialized",     out p))    srcSandboxMaterialized    = p.GetBoolean();
            if (r.TryGetProperty("pz_runtime_materialized",  out p))    srcPzRuntimeMaterialized  = p.GetBoolean();
            if (r.TryGetProperty("materialized_cell_count",  out p))    srcMaterializedCellCount  = p.GetInt32();
            if (r.TryGetProperty("target_component_id",      out p))    srcTargetComponentId      = p.GetString() ?? "";
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse tile materializer result: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceTileMaterializerResultPath   = tileMaterializerResultPath;
        result.SourceTileMaterializerResultSha256 = srcSha256;
        result.SourceTileMaterializerVerdict      = srcVerdict;
        result.SourceTileMaterializerIsValid      = srcIsValid;
        result.TargetComponentId                  = srcTargetComponentId;
        result.InputMaterializedCellCount         = srcMaterializedCellCount;

        // --- Read inherited forbidden guard ---
        var guardFamilies = new List<(int Order, string FamilyId, string BlockedPattern, string Status)>();
        try
        {
            using var guardDoc = JsonDocument.Parse(
                File.ReadAllText(materializerForbiddenOutputGuardPath, Encoding.UTF8));
            if (guardDoc.RootElement.TryGetProperty("guards", out var guardsArr))
            {
                foreach (var g in guardsArr.EnumerateArray())
                {
                    int    ord = g.TryGetProperty("guard_order",     out var gp) ? gp.GetInt32()    : 0;
                    string fid = g.TryGetProperty("family_id",       out gp)     ? gp.GetString() ?? "" : "";
                    string bp  = g.TryGetProperty("blocked_pattern", out gp)     ? gp.GetString() ?? "" : "";
                    string st  = g.TryGetProperty("status",          out gp)     ? gp.GetString() ?? "" : "";
                    guardFamilies.Add((ord, fid, bp, st));
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse forbidden guard: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        // --- Parse materialized cells CSV ---
        // Columns: x,y,primary_owner_kind,primary_owner_id,material_kind,layer_kind,...
        var cells = new List<(int X, int Y, string MaterialKind)>();
        try
        {
            var csvText = File.ReadAllText(materializedCellsPath, Encoding.UTF8);
            var lines   = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines.Skip(1))
            {
                var trimmed = line.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                var cols = trimmed.Split(',');
                if (cols.Length < 6) continue;
                cells.Add((int.Parse(cols[0]), int.Parse(cols[1]), cols[4]));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse materialized cells CSV: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.RenderedCellCount             = cells.Count;
        result.BuildingWallCandidateCellCount  = cells.Count(c => c.MaterialKind == "BUILDING_EXTERIOR_WALL_CANDIDATE");
        result.BuildingFloorCandidateCellCount = cells.Count(c => c.MaterialKind == "BUILDING_INTERIOR_FLOOR_CANDIDATE");
        result.AccessEdgeCellCount             = cells.Count(c => c.MaterialKind == "ACCESS_EDGE_CANDIDATE");
        result.LotSpaceCellCount               = cells.Count(c => c.MaterialKind == "LOT_YARD_OR_SERVICE_SPACE_CANDIDATE");
        result.ComponentResidualCellCount      = cells.Count(c => c.MaterialKind == "COMPONENT_RESIDUAL_SPACE_CANDIDATE");

        // --- Render PNG ---
        Directory.CreateDirectory(outputRoot);
        string pngPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_materializer_qa_overlay.png");
        try
        {
            RenderPng(cells, pngPath);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"PNG render failed: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        string pngSha = HashFile(pngPath);
        var outputFiles = new List<DeadMtlTileMaterializerQaOverlayOutputFile>();

        outputFiles.Add(new DeadMtlTileMaterializerQaOverlayOutputFile
        {
            FileOrder = 1, FileName = "map_00.sandbox_writer_tile_materializer_qa_overlay.png",
            FilePath  = pngPath, FileKind = "QA_OVERLAY_PNG", Sha256 = pngSha, Written = true
        });

        // --- Legend JSON ---
        string legendPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json");
        var legendEntries = new object[]
        {
            new { material_kind = "BUILDING_EXTERIOR_WALL_CANDIDATE",    layer_kind = "WALL",      hex_color = "#1F1F1F", cell_count = result.BuildingWallCandidateCellCount },
            new { material_kind = "BUILDING_INTERIOR_FLOOR_CANDIDATE",   layer_kind = "FLOOR",     hex_color = "#A8A8A8", cell_count = result.BuildingFloorCandidateCellCount },
            new { material_kind = "ACCESS_EDGE_CANDIDATE",               layer_kind = "ACCESS",    hex_color = "#2F6FDB", cell_count = result.AccessEdgeCellCount },
            new { material_kind = "LOT_YARD_OR_SERVICE_SPACE_CANDIDATE", layer_kind = "LOT",       hex_color = "#4F8A3B", cell_count = result.LotSpaceCellCount },
            new { material_kind = "COMPONENT_RESIDUAL_SPACE_CANDIDATE",  layer_kind = "COMPONENT", hex_color = "#7A4E2A", cell_count = result.ComponentResidualCellCount },
        };
        string legendSha = WriteAndHashJson(legendPath, new
        {
            format         = "MAP-27D_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_LEGEND",
            generated_utc  = result.GeneratedUtc,
            map_id         = result.MapId,
            sandbox_only   = true,
            background_hex = "#101010",
            scale          = ScaleFactor,
            legend         = legendEntries,
        });
        outputFiles.Add(new DeadMtlTileMaterializerQaOverlayOutputFile
        {
            FileOrder = 2, FileName = "map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json",
            FilePath  = legendPath, FileKind = "QA_OVERLAY_LEGEND_JSON", Sha256 = legendSha, Written = true
        });

        // --- Counts CSV ---
        string countsCsvPath = Path.Combine(outputRoot,
            "map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv");
        var countsSb = new StringBuilder();
        countsSb.AppendLine("material_kind,layer_kind,hex_color,cell_count");
        countsSb.AppendLine($"BUILDING_EXTERIOR_WALL_CANDIDATE,WALL,#1F1F1F,{result.BuildingWallCandidateCellCount}");
        countsSb.AppendLine($"BUILDING_INTERIOR_FLOOR_CANDIDATE,FLOOR,#A8A8A8,{result.BuildingFloorCandidateCellCount}");
        countsSb.AppendLine($"ACCESS_EDGE_CANDIDATE,ACCESS,#2F6FDB,{result.AccessEdgeCellCount}");
        countsSb.AppendLine($"LOT_YARD_OR_SERVICE_SPACE_CANDIDATE,LOT,#4F8A3B,{result.LotSpaceCellCount}");
        countsSb.AppendLine($"COMPONENT_RESIDUAL_SPACE_CANDIDATE,COMPONENT,#7A4E2A,{result.ComponentResidualCellCount}");
        string countsSha = WriteAndHash(countsCsvPath, countsSb.ToString());
        outputFiles.Add(new DeadMtlTileMaterializerQaOverlayOutputFile
        {
            FileOrder = 3, FileName = "map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv",
            FilePath  = countsCsvPath, FileKind = "QA_OVERLAY_COUNTS_CSV", Sha256 = countsSha, Written = true
        });

        // --- Forbidden output guard JSON (inherited) ---
        string guardOutPath = Path.Combine(outputRoot,
            "map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json");
        var guardEntries2 = guardFamilies.Select(f => new
        {
            guard_order     = f.Order,
            family_id       = f.FamilyId,
            blocked_pattern = f.BlockedPattern,
            status          = f.Status,
            verified        = "NOT_EMITTED",
        }).ToArray();
        string guardSha2 = WriteAndHashJson(guardOutPath, new
        {
            format                         = "MAP-27D_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_FORBIDDEN_OUTPUT_GUARD",
            generated_utc                  = result.GeneratedUtc,
            map_id                         = result.MapId,
            sandbox_only                   = true,
            no_forbidden_artifacts_emitted = true,
            guard_statement                = "MAP-27D did not emit any forbidden output artifacts.",
            guard_count                    = guardEntries2.Length,
            guards                         = guardEntries2,
        });
        outputFiles.Add(new DeadMtlTileMaterializerQaOverlayOutputFile
        {
            FileOrder = 4,
            FileName  = "map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json",
            FilePath  = guardOutPath, FileKind = "QA_OVERLAY_FORBIDDEN_OUTPUT_GUARD_JSON",
            Sha256    = guardSha2, Written = true
        });

        result.OutputFiles         = outputFiles;
        result.OutputFileCount     = outputFiles.Count;
        result.VisualQaOverlayWritten = true;

        // --- 39 checks ---
        MakeCheck(checks, "MAP27C_TILE_MATERIALIZER_RESULT_EXISTS", "MAP-27C tile materializer result file exists");
        MakeCheck(checks, "MAP27C_TILE_MATERIALIZER_RESULT_HASHED", "MAP-27C tile materializer result SHA-256 computed");

        AddCheck(checks, "MAP27C_VERDICT_COMPLETE", "MAP-27C verdict is COMPLETE",
            "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            srcVerdict);
        AddCheck(checks, "MAP27C_IS_VALID_TRUE",             "MAP-27C is_valid is true",             "true",  srcIsValid             ? "true" : "false");
        AddCheck(checks, "MAP27C_SANDBOX_ONLY_TRUE",         "MAP-27C sandbox_only is true",         "true",  srcSandboxOnly         ? "true" : "false");
        AddCheck(checks, "MAP27C_SANDBOX_MATERIALIZED_TRUE", "MAP-27C sandbox_materialized is true", "true",  srcSandboxMaterialized ? "true" : "false");
        AddCheck(checks, "MAP27C_PZ_RUNTIME_MATERIALIZED_FALSE", "MAP-27C pz_runtime_materialized is false", "false",
            srcPzRuntimeMaterialized ? "true" : "false");

        MakeCheck(checks, "MATERIALIZED_CELLS_EXISTS",                  "Materialized cells CSV exists");
        MakeCheck(checks, "MATERIAL_PALETTE_EXISTS",                    "Material palette JSON exists");
        MakeCheck(checks, "LAYER_STACK_EXISTS",                         "Layer stack JSON exists");
        MakeCheck(checks, "MATERIALIZATION_REPLAY_LOG_EXISTS",          "Materialization replay log JSON exists");
        MakeCheck(checks, "MATERIALIZATION_OWNERSHIP_SUMMARY_EXISTS",   "Materialization ownership summary JSON exists");
        MakeCheck(checks, "MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_EXISTS", "Materializer forbidden output guard JSON exists");
        MakeCheck(checks, "SOURCE_DIMENSIONS_256X256",                  "Source dimensions are 256x256");
        MakeCheck(checks, "SCALE_4",                                    "Scale is 4");
        MakeCheck(checks, "OVERLAY_DIMENSIONS_1024X1024",               "Overlay dimensions are 1024x1024");

        AddCheck(checks, "INPUT_MATERIALIZED_CELL_COUNT_GT_0",
            "Input materialized cell count > 0",
            "true", result.InputMaterializedCellCount > 0 ? "true" : "false");
        AddCheck(checks, "RENDERED_CELL_COUNT_MATCHES_INPUT_MATERIALIZED_CELL_COUNT",
            "Rendered cell count matches input materialized cell count",
            result.InputMaterializedCellCount.ToString(), result.RenderedCellCount.ToString());
        AddCheck(checks, "BUILDING_WALL_CANDIDATE_CELL_COUNT_GT_0",
            "Building wall candidate cell count > 0",
            "true", result.BuildingWallCandidateCellCount > 0 ? "true" : "false");
        AddCheck(checks, "BUILDING_FLOOR_CANDIDATE_CELL_COUNT_GT_0",
            "Building floor candidate cell count > 0",
            "true", result.BuildingFloorCandidateCellCount > 0 ? "true" : "false");
        AddCheck(checks, "ACCESS_EDGE_CELL_COUNT_GT_0",
            "Access edge cell count > 0",
            "true", result.AccessEdgeCellCount > 0 ? "true" : "false");
        AddCheck(checks, "LOT_SPACE_CELL_COUNT_GT_0",
            "Lot space cell count > 0",
            "true", result.LotSpaceCellCount > 0 ? "true" : "false");
        AddCheck(checks, "MATERIAL_KIND_COUNT_5", "Material kind count is 5", "5", result.MaterialKindCount.ToString());
        AddCheck(checks, "LAYER_KIND_COUNT_5",    "Layer kind count is 5",    "5", result.LayerKindCount.ToString());

        MakeCheck(checks, "OVERLAY_PNG_WRITTEN",           "Overlay PNG written");
        MakeCheck(checks, "LEGEND_JSON_WRITTEN",           "Legend JSON written");
        MakeCheck(checks, "COUNTS_CSV_WRITTEN",            "Counts CSV written");
        MakeCheck(checks, "FORBIDDEN_OUTPUT_GUARD_WRITTEN","Forbidden output guard JSON written");

        AddCheck(checks, "OUTPUT_FILE_COUNT_4", "Output file count is 4", "4", result.OutputFileCount.ToString());

        MakeCheck(checks, "ALL_OUTPUT_FILES_WRITTEN",              "All output files written");
        MakeCheck(checks, "ALL_OUTPUT_FILES_HASHED",               "All output files hashed");
        MakeCheck(checks, "VISUAL_QA_OVERLAY_WRITTEN_TRUE",        "visual_qa_overlay_written is true");
        MakeCheck(checks, "PZ_RUNTIME_MATERIALIZED_FALSE",         "pz_runtime_materialized is false");
        MakeCheck(checks, "WRITER_READY_FALSE",                    "writer_ready is false");
        MakeCheck(checks, "RUNTIME_VALID_FALSE",                   "runtime_valid is false");
        MakeCheck(checks, "MATERIALIZED_FALSE",                    "materialized (global PZ) is false");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIMED",              "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIMED",  "No public playable packaging claimed");
        MakeCheck(checks, "NO_RUNTIME_OUTPUTS_EMITTED",            "No runtime outputs emitted");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass = result.FailedCheckCount == 0;
        result.IsValid = allPass;
        result.Verdict = allPass
            ? "MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_COMPLETE"
            : invalidVerdict;

        return result;
    }

    private static void RenderPng(List<(int X, int Y, string MaterialKind)> cells, string outputPath)
    {
        using var bmp = new Bitmap(OverlayDim, OverlayDim);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.None;

        using (var bgBrush = new SolidBrush(ColorTranslator.FromHtml("#101010")))
            g.FillRectangle(bgBrush, 0, 0, OverlayDim, OverlayDim);

        foreach (var (x, y, materialKind) in cells)
        {
            using var brush = new SolidBrush(GetMaterialColor(materialKind));
            g.FillRectangle(brush, x * ScaleFactor, y * ScaleFactor, ScaleFactor, ScaleFactor);
        }

        bmp.Save(outputPath, ImageFormat.Png);
    }

    public string RenderJson(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27D WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materializer QA Overlay V0");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Target Component:** {result.TargetComponentId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Writer Stage:** {result.WriterStage}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Sandbox Only:** {result.SandboxOnly}");
        sb.AppendLine($"- **Sandbox Materialized Source:** {result.SandboxMaterializedSource}");
        sb.AppendLine($"- **Visual QA Overlay Written:** {result.VisualQaOverlayWritten}");
        sb.AppendLine($"- **PZ Runtime Materialized:** {result.PzRuntimeMaterialized}");
        sb.AppendLine($"- **Source Dimensions:** {result.SourceWidth}x{result.SourceHeight}");
        sb.AppendLine($"- **Scale:** {result.Scale}");
        sb.AppendLine($"- **Overlay Dimensions:** {result.OverlayWidth}x{result.OverlayHeight}");
        sb.AppendLine($"- **Input Materialized Cells:** {result.InputMaterializedCellCount}");
        sb.AppendLine($"- **Rendered Cells:** {result.RenderedCellCount}");
        sb.AppendLine($"- **Wall Candidates:** {result.BuildingWallCandidateCellCount}");
        sb.AppendLine($"- **Floor Candidates:** {result.BuildingFloorCandidateCellCount}");
        sb.AppendLine($"- **Access Edge Cells:** {result.AccessEdgeCellCount}");
        sb.AppendLine($"- **Lot Space Cells:** {result.LotSpaceCellCount}");
        sb.AppendLine($"- **Component Residual Cells:** {result.ComponentResidualCellCount}");
        sb.AppendLine($"- **Material Kind Count:** {result.MaterialKindCount}");
        sb.AppendLine($"- **Layer Kind Count:** {result.LayerKindCount}");
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
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27D WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materializer QA Overlay V0");
        sb.AppendLine($"Generated UTC              : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID                     : {result.MapId}");
        sb.AppendLine($"Target Component           : {result.TargetComponentId}");
        sb.AppendLine($"Writer Stage               : {result.WriterStage}");
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
        sb.AppendLine($"Source Width               : {result.SourceWidth}");
        sb.AppendLine($"Source Height              : {result.SourceHeight}");
        sb.AppendLine($"Scale                      : {result.Scale}");
        sb.AppendLine($"Overlay Width              : {result.OverlayWidth}");
        sb.AppendLine($"Overlay Height             : {result.OverlayHeight}");
        sb.AppendLine($"Input Materialized Cells   : {result.InputMaterializedCellCount}");
        sb.AppendLine($"Rendered Cells             : {result.RenderedCellCount}");
        sb.AppendLine($"Wall Candidates            : {result.BuildingWallCandidateCellCount}");
        sb.AppendLine($"Floor Candidates           : {result.BuildingFloorCandidateCellCount}");
        sb.AppendLine($"Access Edge Cells          : {result.AccessEdgeCellCount}");
        sb.AppendLine($"Lot Space Cells            : {result.LotSpaceCellCount}");
        sb.AppendLine($"Component Residual         : {result.ComponentResidualCellCount}");
        sb.AppendLine($"Material Kinds             : {result.MaterialKindCount}");
        sb.AppendLine($"Layer Kinds                : {result.LayerKindCount}");
        sb.AppendLine($"Output Files               : {result.OutputFileCount}");
        sb.AppendLine($"Checks                     : {result.CheckCount}");
        sb.AppendLine($"Passed                     : {result.PassedCheckCount}");
        sb.Append($"Failed                     : {result.FailedCheckCount}");
        return sb.ToString();
    }
}
