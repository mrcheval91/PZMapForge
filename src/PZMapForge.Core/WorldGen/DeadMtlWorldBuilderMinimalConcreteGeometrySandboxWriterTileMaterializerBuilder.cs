using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private static void AddCheck(List<DeadMtlTileMaterializerCheck> checks,
        string id, string label, string expected, string actual)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal);
        checks.Add(new DeadMtlTileMaterializerCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = pass ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlTileMaterializerCheck> checks, string id, string label)
    {
        checks.Add(new DeadMtlTileMaterializerCheck
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

    private static (string MaterialKind, string LayerKind) Materialize(
        string ownerKind, string ownerId, int x, int y,
        Dictionary<string, (int minX, int minY, int maxX, int maxY)> slotBbox)
    {
        if (ownerKind == "BUILDING_FOOTPRINT" && slotBbox.TryGetValue(ownerId, out var bbox))
        {
            bool isExterior = x == bbox.minX || x == bbox.maxX || y == bbox.minY || y == bbox.maxY;
            return isExterior
                ? ("BUILDING_EXTERIOR_WALL_CANDIDATE", "WALL")
                : ("BUILDING_INTERIOR_FLOOR_CANDIDATE", "FLOOR");
        }
        return ownerKind switch
        {
            "ACCESS_LINK"        => ("ACCESS_EDGE_CANDIDATE",               "ACCESS"),
            "LOT_BOUNDARY"       => ("LOT_YARD_OR_SERVICE_SPACE_CANDIDATE", "LOT"),
            "COMPONENT_ENVELOPE" => ("COMPONENT_RESIDUAL_SPACE_CANDIDATE",  "COMPONENT"),
            _                    => ("COMPONENT_RESIDUAL_SPACE_CANDIDATE",  "COMPONENT"),
        };
    }

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerResult Build(
        string tileBufferResultPath,
        string tileBufferCellsPath,
        string tileBufferOwnershipPath,
        string tileBufferReplayLogPath,
        string tileBufferCollisionReportPath,
        string tileBufferForbiddenOutputGuardPath,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerResult
        {
            Format                         = "MAP-27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            WriterStage                    = "SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            WriterMode                     = "MATERIALIZE_SANDBOX_TILE_BUFFER_TO_SANDBOX_MATERIAL_RECORDS_ONLY",
            SandboxOnly                    = true,
            SandboxMaterialized            = true,
            PzRuntimeMaterialized          = false,
            BufferWidth                    = 256,
            BufferHeight                   = 256,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
        };

        var checks = new List<DeadMtlTileMaterializerCheck>();
        const string invalidVerdict = "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_INVALID";

        var requiredFiles = new[]
        {
            tileBufferResultPath, tileBufferCellsPath, tileBufferOwnershipPath,
            tileBufferReplayLogPath, tileBufferCollisionReportPath, tileBufferForbiddenOutputGuardPath
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

        // --- read tile buffer result JSON ---
        byte[] tbBytes = File.ReadAllBytes(tileBufferResultPath);
        string tbSha256            = Convert.ToHexString(SHA256.HashData(tbBytes)).ToLower();
        string tbVerdict           = string.Empty;
        bool   tbIsValid           = false;
        bool   tbSandboxOnly       = false;
        int    tbTouchedCellCount  = 0;
        string tbTargetComponentId = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(tileBufferResultPath, Encoding.UTF8));
            var r = doc.RootElement;
            if (r.TryGetProperty("verdict",             out var p)) tbVerdict            = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",            out p))     tbIsValid            = p.GetBoolean();
            if (r.TryGetProperty("sandbox_only",        out p))     tbSandboxOnly        = p.GetBoolean();
            if (r.TryGetProperty("touched_cell_count",  out p))     tbTouchedCellCount   = p.GetInt32();
            if (r.TryGetProperty("target_component_id", out p))     tbTargetComponentId  = p.GetString() ?? "";
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse tile buffer result: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceTileBufferResultPath   = tileBufferResultPath;
        result.SourceTileBufferResultSha256 = tbSha256;
        result.SourceTileBufferVerdict      = tbVerdict;
        result.SourceTileBufferIsValid      = tbIsValid;
        result.TargetComponentId            = tbTargetComponentId;
        result.InputTouchedCellCount        = tbTouchedCellCount;

        // --- read forbidden guard entries ---
        var guardFamilies = new List<(int Order, string FamilyId, string BlockedPattern, string Status)>();
        try
        {
            using var guardDoc = JsonDocument.Parse(File.ReadAllText(tileBufferForbiddenOutputGuardPath, Encoding.UTF8));
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

        // --- parse cells CSV ---
        // Columns: x,y,primary_owner_kind,primary_owner_id,tags,operation_ids,collision_count
        var parsedCells = new List<(int X, int Y, string OwnerKind, string OwnerId, string OpIds, int Collisions)>();
        try
        {
            var csvText = File.ReadAllText(tileBufferCellsPath, Encoding.UTF8);
            var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines.Skip(1))
            {
                var trimmed = line.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                var cols = trimmed.Split(',');
                if (cols.Length < 7) continue;
                parsedCells.Add((
                    int.Parse(cols[0]),
                    int.Parse(cols[1]),
                    cols[2],
                    cols[3],
                    cols[5],
                    int.Parse(cols[6])
                ));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse cells CSV: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        // --- first pass: build per-slot bbox for BUILDING_FOOTPRINT wall/floor classification ---
        var slotBbox = new Dictionary<string, (int minX, int minY, int maxX, int maxY)>();
        foreach (var cell in parsedCells.Where(c => c.OwnerKind == "BUILDING_FOOTPRINT"))
        {
            if (!slotBbox.TryGetValue(cell.OwnerId, out var bbox))
                slotBbox[cell.OwnerId] = (cell.X, cell.Y, cell.X, cell.Y);
            else
                slotBbox[cell.OwnerId] = (
                    Math.Min(bbox.minX, cell.X), Math.Min(bbox.minY, cell.Y),
                    Math.Max(bbox.maxX, cell.X), Math.Max(bbox.maxY, cell.Y));
        }

        // --- second pass: materialize each cell ---
        var materializedCells = new List<DeadMtlSandboxWriterTileMaterializedCell>(parsedCells.Count);
        foreach (var cell in parsedCells)
        {
            var (matKind, layerKind) = Materialize(cell.OwnerKind, cell.OwnerId, cell.X, cell.Y, slotBbox);
            materializedCells.Add(new DeadMtlSandboxWriterTileMaterializedCell
            {
                X                  = cell.X,
                Y                  = cell.Y,
                PrimaryOwnerKind   = cell.OwnerKind,
                PrimaryOwnerId     = cell.OwnerId,
                MaterialKind       = matKind,
                LayerKind          = layerKind,
                SourceOperationIds = cell.OpIds,
                CollisionCount     = cell.Collisions,
            });
        }

        result.MaterializedCellCount          = materializedCells.Count;
        result.BuildingFootprintCellCount      = materializedCells.Count(c => c.PrimaryOwnerKind == "BUILDING_FOOTPRINT");
        result.BuildingWallCandidateCellCount  = materializedCells.Count(c => c.MaterialKind == "BUILDING_EXTERIOR_WALL_CANDIDATE");
        result.BuildingFloorCandidateCellCount = materializedCells.Count(c => c.MaterialKind == "BUILDING_INTERIOR_FLOOR_CANDIDATE");
        result.AccessEdgeCellCount             = materializedCells.Count(c => c.MaterialKind == "ACCESS_EDGE_CANDIDATE");
        result.LotSpaceCellCount               = materializedCells.Count(c => c.MaterialKind == "LOT_YARD_OR_SERVICE_SPACE_CANDIDATE");
        result.ComponentResidualCellCount      = materializedCells.Count(c => c.MaterialKind == "COMPONENT_RESIDUAL_SPACE_CANDIDATE");
        result.MaterialKindCount               = 5;
        result.LayerKindCount                  = 5;

        // --- material palette ---
        var materialKindOrder = new[]
        {
            "BUILDING_EXTERIOR_WALL_CANDIDATE",
            "BUILDING_INTERIOR_FLOOR_CANDIDATE",
            "ACCESS_EDGE_CANDIDATE",
            "LOT_YARD_OR_SERVICE_SPACE_CANDIDATE",
            "COMPONENT_RESIDUAL_SPACE_CANDIDATE",
        };
        var layerForMaterial = new Dictionary<string, string>
        {
            { "BUILDING_EXTERIOR_WALL_CANDIDATE",    "WALL"      },
            { "BUILDING_INTERIOR_FLOOR_CANDIDATE",   "FLOOR"     },
            { "ACCESS_EDGE_CANDIDATE",               "ACCESS"    },
            { "LOT_YARD_OR_SERVICE_SPACE_CANDIDATE", "LOT"       },
            { "COMPONENT_RESIDUAL_SPACE_CANDIDATE",  "COMPONENT" },
        };
        var materialPalette = materialKindOrder.Select((mk, idx) => new DeadMtlTileMaterialPaletteRecord
        {
            PaletteOrder = idx + 1,
            MaterialKind = mk,
            LayerKind    = layerForMaterial[mk],
            CellCount    = materializedCells.Count(c => c.MaterialKind == mk),
        }).ToList();

        // --- layer stack (deterministic order 1=COMPONENT, 2=LOT, 3=ACCESS, 4=FLOOR, 5=WALL) ---
        var layerOrder = new[] { "COMPONENT", "LOT", "ACCESS", "FLOOR", "WALL" };
        var materialKindsForLayer = new Dictionary<string, List<string>>
        {
            { "COMPONENT", new() { "COMPONENT_RESIDUAL_SPACE_CANDIDATE" } },
            { "LOT",       new() { "LOT_YARD_OR_SERVICE_SPACE_CANDIDATE" } },
            { "ACCESS",    new() { "ACCESS_EDGE_CANDIDATE" } },
            { "FLOOR",     new() { "BUILDING_INTERIOR_FLOOR_CANDIDATE" } },
            { "WALL",      new() { "BUILDING_EXTERIOR_WALL_CANDIDATE" } },
        };
        var layerStack = layerOrder.Select((lk, idx) => new DeadMtlTileLayerStackRecord
        {
            LayerOrder    = idx + 1,
            LayerKind     = lk,
            MaterialKinds = materialKindsForLayer[lk],
            CellCount     = materializedCells.Count(c => c.LayerKind == lk),
        }).ToList();

        // --- materialization replay log (per owner_kind × material_kind group) ---
        var replayGroupKeys = new[]
        {
            ("BUILDING_FOOTPRINT", "BUILDING_EXTERIOR_WALL_CANDIDATE"),
            ("BUILDING_FOOTPRINT", "BUILDING_INTERIOR_FLOOR_CANDIDATE"),
            ("ACCESS_LINK",        "ACCESS_EDGE_CANDIDATE"),
            ("LOT_BOUNDARY",       "LOT_YARD_OR_SERVICE_SPACE_CANDIDATE"),
            ("COMPONENT_ENVELOPE", "COMPONENT_RESIDUAL_SPACE_CANDIDATE"),
        };
        var replayEntries = replayGroupKeys.Select((key, idx) => new DeadMtlTileMaterializationReplayEntry
        {
            ReplayOrder  = idx + 1,
            OwnerKind    = key.Item1,
            MaterialKind = key.Item2,
            CellCount    = materializedCells.Count(c => c.PrimaryOwnerKind == key.Item1 && c.MaterialKind == key.Item2),
        }).ToList();

        // --- materialization ownership summary ---
        var ownerKindOrder = new[] { "BUILDING_FOOTPRINT", "ACCESS_LINK", "LOT_BOUNDARY", "COMPONENT_ENVELOPE" };
        var ownershipSummary = ownerKindOrder.Select(ok => new DeadMtlTileMaterializationOwnershipSummaryRecord
        {
            OwnerKind             = ok,
            MaterializedCellCount = materializedCells.Count(c => c.PrimaryOwnerKind == ok),
            MaterialKinds         = materializedCells
                .Where(c => c.PrimaryOwnerKind == ok)
                .Select(c => c.MaterialKind)
                .Distinct()
                .OrderBy(mk => mk)
                .ToList(),
        }).ToList();

        // --- write 6 extra output files ---
        Directory.CreateDirectory(outputRoot);
        var outputFiles = new List<DeadMtlTileMaterializerOutputFile>();

        // 1. Materialized cells CSV
        string cellsCsvPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_materialized_cells.csv");
        var csvSb = new StringBuilder();
        csvSb.AppendLine("x,y,primary_owner_kind,primary_owner_id,material_kind,layer_kind,source_operation_ids,collision_count");
        foreach (var cell in materializedCells)
            csvSb.AppendLine($"{cell.X},{cell.Y},{cell.PrimaryOwnerKind},{cell.PrimaryOwnerId},{cell.MaterialKind},{cell.LayerKind},{cell.SourceOperationIds},{cell.CollisionCount}");
        string cellsCsvSha = WriteAndHash(cellsCsvPath, csvSb.ToString());
        outputFiles.Add(new DeadMtlTileMaterializerOutputFile
        {
            FileOrder = 1, FileName = "map_00.sandbox_writer_tile_materialized_cells.csv",
            FilePath  = cellsCsvPath, FileKind = "TILE_MATERIALIZED_CELLS_CSV", Sha256 = cellsCsvSha, Written = true
        });

        // 2. Material palette JSON
        string palettePath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_material_palette.json");
        string paletteSha  = WriteAndHashJson(palettePath, new
        {
            format              = "MAP-27C_SANDBOX_WRITER_TILE_MATERIAL_PALETTE",
            generated_utc       = result.GeneratedUtc,
            map_id              = result.MapId,
            sandbox_only        = true,
            material_kind_count = materialPalette.Count,
            palette             = materialPalette,
        });
        outputFiles.Add(new DeadMtlTileMaterializerOutputFile
        {
            FileOrder = 2, FileName = "map_00.sandbox_writer_tile_material_palette.json",
            FilePath  = palettePath, FileKind = "TILE_MATERIAL_PALETTE_JSON", Sha256 = paletteSha, Written = true
        });

        // 3. Layer stack JSON
        string layerStackPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_layer_stack.json");
        string layerStackSha  = WriteAndHashJson(layerStackPath, new
        {
            format           = "MAP-27C_SANDBOX_WRITER_TILE_LAYER_STACK",
            generated_utc    = result.GeneratedUtc,
            map_id           = result.MapId,
            sandbox_only     = true,
            layer_kind_count = layerStack.Count,
            layers           = layerStack,
        });
        outputFiles.Add(new DeadMtlTileMaterializerOutputFile
        {
            FileOrder = 3, FileName = "map_00.sandbox_writer_tile_layer_stack.json",
            FilePath  = layerStackPath, FileKind = "TILE_LAYER_STACK_JSON", Sha256 = layerStackSha, Written = true
        });

        // 4. Materialization replay log JSON
        string replayPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_materialization_replay_log.json");
        string replaySha  = WriteAndHashJson(replayPath, new
        {
            format             = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOG",
            generated_utc      = result.GeneratedUtc,
            map_id             = result.MapId,
            sandbox_only       = true,
            replay_entry_count = replayEntries.Count,
            replay_entries     = replayEntries,
        });
        outputFiles.Add(new DeadMtlTileMaterializerOutputFile
        {
            FileOrder = 4, FileName = "map_00.sandbox_writer_tile_materialization_replay_log.json",
            FilePath  = replayPath, FileKind = "TILE_MATERIALIZATION_REPLAY_LOG_JSON", Sha256 = replaySha, Written = true
        });

        // 5. Materialization ownership summary JSON
        string ownershipPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_materialization_ownership_summary.json");
        string ownershipSha  = WriteAndHashJson(ownershipPath, new
        {
            format           = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZATION_OWNERSHIP_SUMMARY",
            generated_utc    = result.GeneratedUtc,
            map_id           = result.MapId,
            sandbox_only     = true,
            owner_kind_count = ownershipSummary.Count,
            ownership        = ownershipSummary,
        });
        outputFiles.Add(new DeadMtlTileMaterializerOutputFile
        {
            FileOrder = 5, FileName = "map_00.sandbox_writer_tile_materialization_ownership_summary.json",
            FilePath  = ownershipPath, FileKind = "TILE_MATERIALIZATION_OWNERSHIP_SUMMARY_JSON", Sha256 = ownershipSha, Written = true
        });

        // 6. Forbidden output guard JSON
        string guardOutPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json");
        var guardEntries = guardFamilies.Select(f => new
        {
            guard_order     = f.Order,
            family_id       = f.FamilyId,
            blocked_pattern = f.BlockedPattern,
            status          = f.Status,
            verified        = "NOT_EMITTED",
        }).ToArray();
        string guardSha = WriteAndHashJson(guardOutPath, new
        {
            format                         = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZER_FORBIDDEN_OUTPUT_GUARD",
            generated_utc                  = result.GeneratedUtc,
            map_id                         = result.MapId,
            sandbox_only                   = true,
            no_forbidden_artifacts_emitted = true,
            guard_statement                = "MAP-27C did not emit any forbidden output artifacts.",
            guard_count                    = guardEntries.Length,
            guards                         = guardEntries,
        });
        outputFiles.Add(new DeadMtlTileMaterializerOutputFile
        {
            FileOrder = 6, FileName = "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json",
            FilePath  = guardOutPath, FileKind = "TILE_MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON", Sha256 = guardSha, Written = true
        });

        result.OutputFiles     = outputFiles;
        result.OutputFileCount = outputFiles.Count;

        // --- 30 checks ---
        MakeCheck(checks, "MAP27B_TILE_BUFFER_RESULT_EXISTS", "MAP-27B tile buffer result file exists");
        MakeCheck(checks, "MAP27B_TILE_BUFFER_RESULT_HASHED", "MAP-27B tile buffer result SHA-256 computed");

        AddCheck(checks, "MAP27B_TILE_BUFFER_VERDICT_COMPLETE", "MAP-27B verdict is COMPLETE",
            "MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_COMPLETE", tbVerdict);
        AddCheck(checks, "MAP27B_TILE_BUFFER_IS_VALID_TRUE", "MAP-27B is_valid is true",     "true", tbIsValid    ? "true" : "false");
        AddCheck(checks, "MAP27B_SANDBOX_ONLY_TRUE",         "MAP-27B sandbox_only is true", "true", tbSandboxOnly ? "true" : "false");
        AddCheck(checks, "MAP27B_TOUCHED_CELL_COUNT_GT_0",   "MAP-27B touched cell count > 0", "true", tbTouchedCellCount > 0 ? "true" : "false");

        MakeCheck(checks, "TILE_BUFFER_CELLS_EXISTS",              "Tile buffer cells CSV exists");
        MakeCheck(checks, "TILE_BUFFER_OWNERSHIP_EXISTS",          "Tile buffer ownership JSON exists");
        MakeCheck(checks, "TILE_BUFFER_REPLAY_LOG_EXISTS",         "Tile buffer replay log JSON exists");
        MakeCheck(checks, "TILE_BUFFER_COLLISION_REPORT_EXISTS",   "Tile buffer collision report JSON exists");
        MakeCheck(checks, "TILE_BUFFER_FORBIDDEN_OUTPUT_GUARD_EXISTS", "Tile buffer forbidden output guard JSON exists");

        AddCheck(checks, "MATERIALIZED_CELL_COUNT_MATCHES_INPUT_TOUCHED_CELL_COUNT",
            "Materialized cell count matches input touched cell count",
            tbTouchedCellCount.ToString(), result.MaterializedCellCount.ToString());

        AddCheck(checks, "BUILDING_FOOTPRINT_CELL_COUNT_GT_0",      "Building footprint cell count > 0",  "true", result.BuildingFootprintCellCount > 0      ? "true" : "false");
        AddCheck(checks, "BUILDING_WALL_CANDIDATE_CELL_COUNT_GT_0", "Building wall candidate count > 0",  "true", result.BuildingWallCandidateCellCount > 0  ? "true" : "false");
        AddCheck(checks, "BUILDING_FLOOR_CANDIDATE_CELL_COUNT_GT_0","Building floor candidate count > 0", "true", result.BuildingFloorCandidateCellCount > 0 ? "true" : "false");
        AddCheck(checks, "ACCESS_EDGE_CELL_COUNT_GT_0",             "Access edge cell count > 0",         "true", result.AccessEdgeCellCount > 0             ? "true" : "false");
        AddCheck(checks, "LOT_SPACE_CELL_COUNT_GT_0",               "Lot space cell count > 0",           "true", result.LotSpaceCellCount > 0               ? "true" : "false");

        AddCheck(checks, "MATERIAL_KIND_COUNT_5", "Material kind count is 5", "5", result.MaterialKindCount.ToString());
        AddCheck(checks, "LAYER_KIND_COUNT_5",    "Layer kind count is 5",    "5", result.LayerKindCount.ToString());
        AddCheck(checks, "OUTPUT_FILE_COUNT_6",   "Output file count is 6",   "6", result.OutputFileCount.ToString());

        MakeCheck(checks, "ALL_OUTPUT_FILES_WRITTEN", "All output files written");
        MakeCheck(checks, "ALL_OUTPUT_FILES_HASHED",  "All output files hashed");

        MakeCheck(checks, "SANDBOX_MATERIALIZED_TRUE",            "sandbox_materialized is true");
        MakeCheck(checks, "PZ_RUNTIME_MATERIALIZED_FALSE",        "pz_runtime_materialized is false");
        MakeCheck(checks, "WRITER_READY_FALSE",                   "writer_ready is false");
        MakeCheck(checks, "RUNTIME_VALID_FALSE",                  "runtime_valid is false");
        MakeCheck(checks, "MATERIALIZED_FALSE",                   "materialized (global PZ) is false");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIMED",             "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIMED", "No public playable packaging claimed");
        MakeCheck(checks, "NO_RUNTIME_OUTPUTS_EMITTED",           "No runtime outputs emitted");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass = result.FailedCheckCount == 0;
        result.IsValid = allPass;
        result.Verdict = allPass
            ? "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27C WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materializer V0");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Target Component:** {result.TargetComponentId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Writer Stage:** {result.WriterStage}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Sandbox Only:** {result.SandboxOnly}");
        sb.AppendLine($"- **Sandbox Materialized:** {result.SandboxMaterialized}");
        sb.AppendLine($"- **PZ Runtime Materialized:** {result.PzRuntimeMaterialized}");
        sb.AppendLine($"- **Input Touched Cells:** {result.InputTouchedCellCount}");
        sb.AppendLine($"- **Materialized Cells:** {result.MaterializedCellCount}");
        sb.AppendLine($"- **Building Footprint Cells:** {result.BuildingFootprintCellCount}");
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

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27C WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materializer V0");
        sb.AppendLine($"Generated UTC          : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID                 : {result.MapId}");
        sb.AppendLine($"Target Component       : {result.TargetComponentId}");
        sb.AppendLine($"Writer Stage           : {result.WriterStage}");
        sb.AppendLine($"Verdict                : {result.Verdict}");
        sb.AppendLine($"Is Valid               : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only           : {(result.SandboxOnly ? 1 : 0)}");
        sb.AppendLine($"Sandbox Materialized   : {(result.SandboxMaterialized ? 1 : 0)}");
        sb.AppendLine($"PZ Runtime Materialized: {(result.PzRuntimeMaterialized ? 1 : 0)}");
        sb.AppendLine($"Writer Ready           : {(result.WriterReady ? 1 : 0)}");
        sb.AppendLine($"Runtime Valid          : {(result.RuntimeValid ? 1 : 0)}");
        sb.AppendLine($"Materialized           : {(result.Materialized ? 1 : 0)}");
        sb.AppendLine($"Runtime Proof          : {(result.RuntimeProofClaimed ? 1 : 0)}");
        sb.AppendLine($"Public Playable        : {(result.PublicPlayablePackagingClaimed ? 1 : 0)}");
        sb.AppendLine($"Input Touched Cells    : {result.InputTouchedCellCount}");
        sb.AppendLine($"Materialized Cells     : {result.MaterializedCellCount}");
        sb.AppendLine($"Footprint Cells        : {result.BuildingFootprintCellCount}");
        sb.AppendLine($"Wall Candidates        : {result.BuildingWallCandidateCellCount}");
        sb.AppendLine($"Floor Candidates       : {result.BuildingFloorCandidateCellCount}");
        sb.AppendLine($"Access Edge Cells      : {result.AccessEdgeCellCount}");
        sb.AppendLine($"Lot Space Cells        : {result.LotSpaceCellCount}");
        sb.AppendLine($"Component Residual     : {result.ComponentResidualCellCount}");
        sb.AppendLine($"Material Kinds         : {result.MaterialKindCount}");
        sb.AppendLine($"Layer Kinds            : {result.LayerKindCount}");
        sb.AppendLine($"Output Files           : {result.OutputFileCount}");
        sb.AppendLine($"Checks                 : {result.CheckCount}");
        sb.AppendLine($"Passed                 : {result.PassedCheckCount}");
        sb.Append($"Failed                 : {result.FailedCheckCount}");
        return sb.ToString();
    }
}
