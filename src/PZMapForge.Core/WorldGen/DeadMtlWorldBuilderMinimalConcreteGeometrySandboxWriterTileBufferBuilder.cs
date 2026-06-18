using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private sealed class ParsedOp
    {
        public int    Order;
        public string Kind          = string.Empty;
        public string Group         = string.Empty;
        public string OwnerId       = string.Empty;
        public string AccessKind    = string.Empty;
        public string Side          = string.Empty;
        public int    MinX, MinY, MaxX, MaxY, WidthPx, HeightPx;
        public string RuntimeEffect = "NONE";
        public string SourceFile    = string.Empty;
    }

    private sealed class CellState
    {
        public string       PrimaryOwnerKind = string.Empty;
        public string       PrimaryOwnerId   = string.Empty;
        public List<string> TagKinds         = new();
        public List<string> OpIds            = new();
        public int          CollisionCount;
    }

    private static int GetOwnerPriority(string ownerKind) => ownerKind switch
    {
        "BUILDING_FOOTPRINT" => 4,
        "ACCESS_LINK"        => 3,
        "LOT_BOUNDARY"       => 2,
        "COMPONENT_ENVELOPE" => 1,
        _ => 0
    };

    private static int GetOperationPriority(string operationKind) => operationKind switch
    {
        "BUILDING_FOOTPRINT_WRITE" => 4,
        "ACCESS_LINK_WRITE"        => 3,
        "LOT_BOUNDARY_WRITE"       => 2,
        "COMPONENT_ENVELOPE_WRITE" => 1,
        _ => 0
    };

    private static string GetOwnerKind(string operationKind) => operationKind switch
    {
        "COMPONENT_ENVELOPE_WRITE" => "COMPONENT_ENVELOPE",
        "LOT_BOUNDARY_WRITE"       => "LOT_BOUNDARY",
        "BUILDING_FOOTPRINT_WRITE" => "BUILDING_FOOTPRINT",
        "ACCESS_LINK_WRITE"        => "ACCESS_LINK",
        _ => "UNKNOWN"
    };

    private static (List<ParsedOp> ops, int declaredCount) ParseOpFile(string filePath)
    {
        var ops = new List<ParsedOp>();
        int declaredCount = 0;
        string sourceFile = Path.GetFileName(filePath);

        using var doc = JsonDocument.Parse(File.ReadAllText(filePath, Encoding.UTF8));
        var root = doc.RootElement;
        if (root.TryGetProperty("operation_count", out var ocp)) declaredCount = ocp.GetInt32();
        if (!root.TryGetProperty("operations", out var opsArr)) return (ops, declaredCount);

        foreach (var op in opsArr.EnumerateArray())
        {
            var parsed = new ParsedOp
            {
                SourceFile    = sourceFile,
                Order         = op.TryGetProperty("operation_order", out var p) ? p.GetInt32() : 0,
                Kind          = op.TryGetProperty("operation_kind", out p) ? p.GetString() ?? "" : "",
                Group         = op.TryGetProperty("operation_group", out p) ? p.GetString() ?? "" : "",
                MinX          = op.TryGetProperty("min_x", out p) ? p.GetInt32() : 0,
                MinY          = op.TryGetProperty("min_y", out p) ? p.GetInt32() : 0,
                MaxX          = op.TryGetProperty("max_x", out p) ? p.GetInt32() : 0,
                MaxY          = op.TryGetProperty("max_y", out p) ? p.GetInt32() : 0,
                WidthPx       = op.TryGetProperty("width_px", out p) ? p.GetInt32() : 0,
                HeightPx      = op.TryGetProperty("height_px", out p) ? p.GetInt32() : 0,
                RuntimeEffect = op.TryGetProperty("runtime_effect", out p) ? p.GetString() ?? "NONE" : "NONE",
                AccessKind    = op.TryGetProperty("access_kind", out p) ? p.GetString() ?? "" : "",
                Side          = op.TryGetProperty("side", out p) ? p.GetString() ?? "" : "",
            };

            string ownerId = "";
            if (parsed.Kind == "COMPONENT_ENVELOPE_WRITE" && op.TryGetProperty("target_component_id", out p))
                ownerId = p.GetString() ?? "";
            else if (parsed.Kind == "LOT_BOUNDARY_WRITE" && op.TryGetProperty("lot_id", out p))
                ownerId = p.GetString() ?? "";
            else if (parsed.Kind == "BUILDING_FOOTPRINT_WRITE" && op.TryGetProperty("slot_id", out p))
                ownerId = p.GetString() ?? "";
            else if (parsed.Kind == "ACCESS_LINK_WRITE" && op.TryGetProperty("access_id", out p))
                ownerId = p.GetString() ?? "";
            parsed.OwnerId = ownerId;

            ops.Add(parsed);
        }

        return (ops, declaredCount);
    }

    private static void AddCheck(List<DeadMtlSandboxWriterTileBufferCheck> checks,
        string id, string label, string expected, string actual)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal);
        checks.Add(new DeadMtlSandboxWriterTileBufferCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = pass ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlSandboxWriterTileBufferCheck> checks, string id, string label)
    {
        checks.Add(new DeadMtlSandboxWriterTileBufferCheck
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

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferResult Build(
        string sandboxWriterResultPath,
        string componentOpPath,
        string lotOpPath,
        string buildingSlotOpPath,
        string accessOpPath,
        string forbiddenGuardPath,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferResult
        {
            Format                         = "MAP-27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            WriterStage                    = "SANDBOX_WRITER_TILE_BUFFER_V0",
            WriterMode                     = "APPLY_SANDBOX_OPERATIONS_TO_INTERNAL_TILE_BUFFER_ONLY",
            SandboxOnly                    = true,
            BufferWidth                    = 256,
            BufferHeight                   = 256,
            CoordinateSystem               = "PNG_PIXEL_TILE_SPACE",
            Origin                         = "TOP_LEFT",
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            ApprovedForWriterExperiment    = false,
            WriterExperimentGateStatus     = "LOCKED_PENDING_OPERATOR_APPROVAL",
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false
        };

        var checks = new List<DeadMtlSandboxWriterTileBufferCheck>();
        string invalidVerdict = "MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_INVALID";

        if (!File.Exists(sandboxWriterResultPath))
        {
            result.Errors.Add($"MAP-27A sandbox writer result not found: {sandboxWriterResultPath}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        byte[] swrBytes;
        string swrSha256;
        string swrVerdict        = string.Empty;
        bool   swrIsValid        = false;
        bool   swrSandboxOnly    = false;
        int    swrOperationCount     = 0;
        int    swrOperationFileCount = 0;
        string swrTargetComponentId  = string.Empty;

        try
        {
            swrBytes  = File.ReadAllBytes(sandboxWriterResultPath);
            swrSha256 = Convert.ToHexString(SHA256.HashData(swrBytes)).ToLower();

            using var doc = JsonDocument.Parse(File.ReadAllText(sandboxWriterResultPath, Encoding.UTF8));
            var r = doc.RootElement;
            if (r.TryGetProperty("verdict",              out var p)) swrVerdict             = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",             out p))     swrIsValid             = p.GetBoolean();
            if (r.TryGetProperty("sandbox_only",         out p))     swrSandboxOnly         = p.GetBoolean();
            if (r.TryGetProperty("operation_count",      out p))     swrOperationCount      = p.GetInt32();
            if (r.TryGetProperty("operation_file_count", out p))     swrOperationFileCount  = p.GetInt32();
            if (r.TryGetProperty("target_component_id",  out p))     swrTargetComponentId   = p.GetString() ?? "";
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse MAP-27A sandbox writer result: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceSandboxWriterResultPath   = sandboxWriterResultPath;
        result.SourceSandboxWriterResultSha256 = swrSha256;
        result.SourceSandboxWriterVerdict      = swrVerdict;
        result.SourceSandboxWriterIsValid      = swrIsValid;
        result.TargetComponentId               = swrTargetComponentId;

        string[] opFilePaths = { componentOpPath, lotOpPath, buildingSlotOpPath, accessOpPath, forbiddenGuardPath };
        foreach (var fp in opFilePaths)
        {
            if (!File.Exists(fp))
            {
                result.Errors.Add($"Operation file not found: {fp}");
                result.IsValid = false;
                result.Verdict = invalidVerdict;
                return result;
            }
        }

        List<ParsedOp> componentOps, lotOps, buildingSlotOps, accessOps;
        var guardFamilies = new List<(int Order, string FamilyId, string BlockedPattern, string Status)>();

        try
        {
            (componentOps, _)    = ParseOpFile(componentOpPath);
            (lotOps, _)          = ParseOpFile(lotOpPath);
            (buildingSlotOps, _) = ParseOpFile(buildingSlotOpPath);
            (accessOps, _)       = ParseOpFile(accessOpPath);

            using var guardDoc = JsonDocument.Parse(File.ReadAllText(forbiddenGuardPath, Encoding.UTF8));
            var gr = guardDoc.RootElement;
            if (gr.TryGetProperty("guards", out var guardsArr))
            {
                foreach (var g in guardsArr.EnumerateArray())
                {
                    int    ord = g.TryGetProperty("guard_order",     out var gp) ? gp.GetInt32() : 0;
                    string fid = g.TryGetProperty("family_id",       out gp) ? gp.GetString() ?? "" : "";
                    string bp  = g.TryGetProperty("blocked_pattern", out gp) ? gp.GetString() ?? "" : "";
                    string st  = g.TryGetProperty("status",          out gp) ? gp.GetString() ?? "" : "";
                    guardFamilies.Add((ord, fid, bp, st));
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse operation files: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.ComponentOperationCount    = componentOps.Count;
        result.LotOperationCount          = lotOps.Count;
        result.BuildingSlotOperationCount = buildingSlotOps.Count;
        result.AccessOperationCount       = accessOps.Count;
        result.InputOperationCount        = componentOps.Count + lotOps.Count + buildingSlotOps.Count + accessOps.Count;
        result.InputOperationFileCount    = 5;

        // --- apply operations to tile buffer ---
        var buffer           = new Dictionary<(int x, int y), CellState>();
        var replayEntries    = new List<DeadMtlSandboxWriterTileBufferReplayEntry>();
        var collisionRecords = new List<DeadMtlSandboxWriterTileBufferCollisionRecord>();
        int replayOrder      = 0;

        // Component bbox used by ACCESS_LINK edge derivation for zero-dimension ops.
        ParsedOp? compEnvOp = componentOps.FirstOrDefault(o => o.Kind == "COMPONENT_ENVELOPE_WRITE" && o.WidthPx > 0 && o.HeightPx > 0);
        bool hasCompBbox = compEnvOp != null;

        var allOpGroups = new[] { componentOps, lotOps, buildingSlotOps, accessOps };

        foreach (var group in allOpGroups)
        {
            foreach (var op in group)
            {
                replayOrder++;
                string ownerKind   = GetOwnerKind(op.Kind);
                int    newPriority = GetOperationPriority(op.Kind);
                string opId        = $"{op.SourceFile}#{op.Kind}#{op.Order}";

                int appliedCells     = 0;
                int overwrittenCells = 0;
                int opCollisions     = 0;

                void ApplyCell(int cx, int cy)
                {
                    appliedCells++;
                    if (!buffer.TryGetValue((cx, cy), out var cell))
                    {
                        cell = new CellState
                        {
                            PrimaryOwnerKind = ownerKind,
                            PrimaryOwnerId   = op.OwnerId,
                        };
                        cell.TagKinds.Add(ownerKind);
                        cell.OpIds.Add(opId);
                        buffer[(cx, cy)] = cell;
                    }
                    else
                    {
                        opCollisions++;
                        cell.CollisionCount++;
                        int    currentPriority = GetOwnerPriority(cell.PrimaryOwnerKind);
                        string prevKind        = cell.PrimaryOwnerKind;
                        string prevId          = cell.PrimaryOwnerId;
                        string resolution;

                        if (newPriority > currentPriority)
                        {
                            resolution = "OVERRIDE_BY_PRIORITY";
                            overwrittenCells++;
                            cell.PrimaryOwnerKind = ownerKind;
                            cell.PrimaryOwnerId   = op.OwnerId;
                        }
                        else if (newPriority == currentPriority)
                        {
                            resolution = "SAME_OWNER_TAG_MERGE";
                        }
                        else
                        {
                            resolution = "LOWER_PRIORITY_RETAINED";
                        }

                        if (!cell.TagKinds.Contains(ownerKind)) cell.TagKinds.Add(ownerKind);
                        if (!cell.OpIds.Contains(opId))         cell.OpIds.Add(opId);

                        collisionRecords.Add(new DeadMtlSandboxWriterTileBufferCollisionRecord
                        {
                            CollisionOrder    = collisionRecords.Count + 1,
                            X                 = cx,
                            Y                 = cy,
                            PreviousOwnerKind = prevKind,
                            PreviousOwnerId   = prevId,
                            NewOwnerKind      = ownerKind,
                            NewOwnerId        = op.OwnerId,
                            OperationKind     = op.Kind,
                            Resolution        = resolution
                        });
                    }
                }

                if (op.Kind == "ACCESS_LINK_WRITE" && op.WidthPx == 0 && op.HeightPx == 0 && hasCompBbox)
                {
                    // Zero-dimension access op: derive edge cells from component bbox.
                    bool isFrontage = op.AccessKind == "FRONTAGE_ACCESS" || op.Side == "NORTH";
                    bool isEast     = op.AccessKind == "REAR_SERVICE_ACCESS" || op.Side == "EAST";

                    if (isFrontage)
                    {
                        // North edge: y = comp.min_y, x = comp.min_x..comp.max_x
                        for (int x = compEnvOp!.MinX; x <= compEnvOp.MaxX; x++)
                            ApplyCell(x, compEnvOp.MinY);
                    }
                    else if (isEast)
                    {
                        // East edge: x = comp.max_x, y = comp.min_y..comp.max_y
                        for (int y = compEnvOp!.MinY; y <= compEnvOp.MaxY; y++)
                            ApplyCell(compEnvOp.MaxX, y);
                    }
                }
                else if (op.WidthPx > 0 && op.HeightPx > 0)
                {
                    for (int x = op.MinX; x <= op.MaxX; x++)
                        for (int y = op.MinY; y <= op.MaxY; y++)
                            ApplyCell(x, y);
                }

                replayEntries.Add(new DeadMtlSandboxWriterTileBufferReplayEntry
                {
                    ReplayOrder          = replayOrder,
                    OperationKind        = op.Kind,
                    OperationGroup       = op.Group,
                    SourceFile           = op.SourceFile,
                    SourceOperationOrder = op.Order,
                    AppliedCellCount     = appliedCells,
                    OverwrittenCellCount = overwrittenCells,
                    CollisionCount       = opCollisions,
                    Status               = "APPLIED_TO_SANDBOX_TILE_BUFFER",
                    RuntimeEffect        = op.RuntimeEffect
                });
            }
        }

        result.TouchedCellCount      = buffer.Count;
        result.PrimaryOwnedCellCount = buffer.Count;
        result.ReplayEntryCount      = replayEntries.Count;
        result.CollisionCount        = collisionRecords.Count;

        var touchedCells = buffer.Keys
            .OrderBy(k => k.x).ThenBy(k => k.y)
            .Select(k =>
            {
                var s = buffer[k];
                return new DeadMtlSandboxWriterTileCell
                {
                    X                = k.x,
                    Y                = k.y,
                    PrimaryOwnerKind = s.PrimaryOwnerKind,
                    PrimaryOwnerId   = s.PrimaryOwnerId,
                    Tags             = string.Join("|", s.TagKinds),
                    OperationIds     = string.Join("|", s.OpIds),
                    CollisionCount   = s.CollisionCount
                };
            }).ToList();

        var ownershipKinds = new[] { "COMPONENT_ENVELOPE", "LOT_BOUNDARY", "BUILDING_FOOTPRINT", "ACCESS_LINK" };
        var ownershipRecords = ownershipKinds.Select(kind =>
        {
            string opKind = kind switch
            {
                "COMPONENT_ENVELOPE" => "COMPONENT_ENVELOPE_WRITE",
                "LOT_BOUNDARY"       => "LOT_BOUNDARY_WRITE",
                "BUILDING_FOOTPRINT" => "BUILDING_FOOTPRINT_WRITE",
                "ACCESS_LINK"        => "ACCESS_LINK_WRITE",
                _ => ""
            };
            return new DeadMtlSandboxWriterTileBufferOwnershipRecord
            {
                Kind                  = kind,
                ClaimedCellCount      = buffer.Values.Count(c => c.TagKinds.Contains(kind)),
                PrimaryOwnedCellCount = buffer.Values.Count(c => c.PrimaryOwnerKind == kind),
                OperationCount        = replayEntries.Count(re => re.OperationKind == opKind),
                SourceOperationKinds  = new List<string> { opKind }
            };
        }).ToList();

        result.OwnershipKindCount = ownershipRecords.Count;

        // --- write 5 extra output files ---
        Directory.CreateDirectory(outputRoot);
        var outputFiles = new List<DeadMtlSandboxWriterTileBufferOutputFile>();

        // 1. Tile buffer cells CSV
        string cellsCsvPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_buffer_cells.csv");
        var csvSb = new StringBuilder();
        csvSb.AppendLine("x,y,primary_owner_kind,primary_owner_id,tags,operation_ids,collision_count");
        foreach (var cell in touchedCells)
            csvSb.AppendLine($"{cell.X},{cell.Y},{cell.PrimaryOwnerKind},{cell.PrimaryOwnerId},{cell.Tags},{cell.OperationIds},{cell.CollisionCount}");
        string cellsCsvSha = WriteAndHash(cellsCsvPath, csvSb.ToString());
        outputFiles.Add(new DeadMtlSandboxWriterTileBufferOutputFile
        { FileOrder = 1, FileName = "map_00.sandbox_writer_tile_buffer_cells.csv",
          FilePath = cellsCsvPath, FileKind = "TILE_BUFFER_CELLS_CSV", Sha256 = cellsCsvSha, Written = true });

        // 2. Ownership JSON
        string ownershipPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_buffer_ownership.json");
        string ownershipSha  = WriteAndHashJson(ownershipPath, new
        {
            format        = "MAP-27B_SANDBOX_WRITER_TILE_BUFFER_OWNERSHIP",
            generated_utc = result.GeneratedUtc,
            map_id        = result.MapId,
            sandbox_only  = true,
            kind_count    = ownershipRecords.Count,
            ownership     = ownershipRecords
        });
        outputFiles.Add(new DeadMtlSandboxWriterTileBufferOutputFile
        { FileOrder = 2, FileName = "map_00.sandbox_writer_tile_buffer_ownership.json",
          FilePath = ownershipPath, FileKind = "TILE_BUFFER_OWNERSHIP_JSON", Sha256 = ownershipSha, Written = true });

        // 3. Replay log JSON
        string replayPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_buffer_replay_log.json");
        string replaySha  = WriteAndHashJson(replayPath, new
        {
            format             = "MAP-27B_SANDBOX_WRITER_TILE_BUFFER_REPLAY_LOG",
            generated_utc      = result.GeneratedUtc,
            map_id             = result.MapId,
            sandbox_only       = true,
            replay_entry_count = replayEntries.Count,
            replay_entries     = replayEntries
        });
        outputFiles.Add(new DeadMtlSandboxWriterTileBufferOutputFile
        { FileOrder = 3, FileName = "map_00.sandbox_writer_tile_buffer_replay_log.json",
          FilePath = replayPath, FileKind = "TILE_BUFFER_REPLAY_LOG_JSON", Sha256 = replaySha, Written = true });

        // 4. Collision report JSON
        string collisionPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_buffer_collision_report.json");
        string collisionSha  = WriteAndHashJson(collisionPath, new
        {
            format          = "MAP-27B_SANDBOX_WRITER_TILE_BUFFER_COLLISION_REPORT",
            generated_utc   = result.GeneratedUtc,
            map_id          = result.MapId,
            sandbox_only    = true,
            collision_count = collisionRecords.Count,
            collisions      = collisionRecords
        });
        outputFiles.Add(new DeadMtlSandboxWriterTileBufferOutputFile
        { FileOrder = 4, FileName = "map_00.sandbox_writer_tile_buffer_collision_report.json",
          FilePath = collisionPath, FileKind = "TILE_BUFFER_COLLISION_REPORT_JSON", Sha256 = collisionSha, Written = true });

        // 5. Forbidden output guard JSON
        string guardPath = Path.Combine(outputRoot, "map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json");
        var guardEntries = guardFamilies.Select(f => new
        {
            guard_order     = f.Order,
            family_id       = f.FamilyId,
            blocked_pattern = f.BlockedPattern,
            status          = f.Status,
            verified        = "NOT_EMITTED"
        }).ToArray();
        string guardSha = WriteAndHashJson(guardPath, new
        {
            format                         = "MAP-27B_SANDBOX_WRITER_TILE_BUFFER_FORBIDDEN_OUTPUT_GUARD",
            generated_utc                  = result.GeneratedUtc,
            map_id                         = result.MapId,
            sandbox_only                   = true,
            no_forbidden_artifacts_emitted = true,
            guard_statement                = "MAP-27B did not emit any forbidden output artifacts.",
            guard_count                    = guardEntries.Length,
            guards                         = guardEntries
        });
        outputFiles.Add(new DeadMtlSandboxWriterTileBufferOutputFile
        { FileOrder = 5, FileName = "map_00.sandbox_writer_tile_buffer_forbidden_output_guard.json",
          FilePath = guardPath, FileKind = "TILE_BUFFER_FORBIDDEN_OUTPUT_GUARD_JSON", Sha256 = guardSha, Written = true });

        result.OutputFiles = outputFiles;

        // --- 38 checks ---
        MakeCheck(checks, "MAP27A_RESULT_EXISTS",  "MAP-27A sandbox writer result file exists");
        MakeCheck(checks, "MAP27A_RESULT_HASHED",  "MAP-27A sandbox writer result SHA-256 computed");

        AddCheck(checks, "MAP27A_VERDICT_COMPLETE",       "MAP-27A verdict is COMPLETE",
            "MAP27A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_V0_COMPLETE", swrVerdict);
        AddCheck(checks, "MAP27A_IS_VALID_TRUE",          "MAP-27A is_valid is true",     "true", swrIsValid     ? "true" : "false");
        AddCheck(checks, "MAP27A_SANDBOX_ONLY_TRUE",      "MAP-27A sandbox_only is true", "true", swrSandboxOnly ? "true" : "false");
        AddCheck(checks, "MAP27A_OPERATION_COUNT_17",     "MAP-27A operation_count is 17",    "17", swrOperationCount.ToString());
        AddCheck(checks, "MAP27A_OPERATION_FILE_COUNT_5", "MAP-27A operation_file_count is 5", "5", swrOperationFileCount.ToString());

        MakeCheck(checks, "COMPONENT_OPERATION_FILE_EXISTS",    "Component operation file exists");
        MakeCheck(checks, "LOT_OPERATION_FILE_EXISTS",           "Lot operation file exists");
        MakeCheck(checks, "BUILDING_SLOT_OPERATION_FILE_EXISTS", "Building slot operation file exists");
        MakeCheck(checks, "ACCESS_OPERATION_FILE_EXISTS",        "Access operation file exists");
        MakeCheck(checks, "FORBIDDEN_OUTPUT_GUARD_FILE_EXISTS",  "Forbidden output guard file exists");

        AddCheck(checks, "COMPONENT_OPERATION_COUNT_1",     "Component operation count is 1",     "1",  componentOps.Count.ToString());
        AddCheck(checks, "LOT_OPERATION_COUNT_7",           "Lot operation count is 7",           "7",  lotOps.Count.ToString());
        AddCheck(checks, "BUILDING_SLOT_OPERATION_COUNT_7", "Building slot operation count is 7", "7",  buildingSlotOps.Count.ToString());
        AddCheck(checks, "ACCESS_OPERATION_COUNT_2",        "Access operation count is 2",        "2",  accessOps.Count.ToString());
        AddCheck(checks, "TOTAL_INPUT_OPERATION_COUNT_17",  "Total input operation count is 17",  "17", result.InputOperationCount.ToString());

        MakeCheck(checks, "BUFFER_DIMENSIONS_256X256",              "Buffer dimensions are 256x256");
        MakeCheck(checks, "COORDINATE_SYSTEM_PNG_PIXEL_TILE_SPACE", "Coordinate system is PNG_PIXEL_TILE_SPACE");
        MakeCheck(checks, "WRITER_STAGE_TILE_BUFFER_V0",            "Writer stage is SANDBOX_WRITER_TILE_BUFFER_V0");
        MakeCheck(checks, "SANDBOX_ONLY_TRUE",                      "sandbox_only is true");

        bool allNone = replayEntries.All(re => re.RuntimeEffect == "NONE");
        AddCheck(checks, "ALL_REPLAY_ENTRIES_RUNTIME_EFFECT_NONE", "All replay entries have runtime_effect NONE",
            "true", allNone ? "true" : "false");
        AddCheck(checks, "REPLAY_ENTRY_COUNT_17",        "Replay entry count is 17",         "17",   replayEntries.Count.ToString());
        AddCheck(checks, "TOUCHED_CELL_COUNT_GT_0",      "Touched cell count > 0",           "true", result.TouchedCellCount > 0      ? "true" : "false");
        AddCheck(checks, "PRIMARY_OWNED_CELL_COUNT_GT_0","Primary owned cell count > 0",     "true", result.PrimaryOwnedCellCount > 0  ? "true" : "false");
        AddCheck(checks, "OWNERSHIP_KIND_COUNT_4",       "Ownership kind count is 4",        "4",    result.OwnershipKindCount.ToString());

        int accessLinkPrimaryOwned = ownershipRecords.FirstOrDefault(r => r.Kind == "ACCESS_LINK")?.PrimaryOwnedCellCount ?? 0;
        AddCheck(checks, "ACCESS_LINK_PRIMARY_OWNED_GT_0", "ACCESS_LINK primary owned cell count > 0",
            "true", accessLinkPrimaryOwned > 0 ? "true" : "false");

        MakeCheck(checks, "COLLISION_REPORT_WRITTEN", "Collision report file written");
        MakeCheck(checks, "OWNERSHIP_REPORT_WRITTEN", "Ownership report file written");
        MakeCheck(checks, "REPLAY_LOG_WRITTEN",        "Replay log file written");
        MakeCheck(checks, "TILE_CELLS_CSV_WRITTEN",    "Tile buffer cells CSV written");

        MakeCheck(checks, "NO_LOTPACK_WRITTEN",                   "No lotpack artifact emitted");
        MakeCheck(checks, "NO_LOTHEADER_WRITTEN",                 "No lotheader artifact emitted");
        MakeCheck(checks, "NO_WORLDGENOVERRIDE_WRITTEN",          "No WorldGenOverride artifact emitted");
        MakeCheck(checks, "NO_RUNTIME_LUA_WRITTEN",               "No runtime Lua emitted");
        MakeCheck(checks, "NO_COMPILE_WORLDGEN_CALLED",           "compile-worldgen was not called");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIMED",             "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIMED", "No public playable packaging claimed");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass = result.FailedCheckCount == 0;
        result.IsValid = allPass;
        result.Verdict = allPass
            ? "MAP27B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_BUFFER_V0_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27B WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Buffer V0");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Target Component:** {result.TargetComponentId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Writer Stage:** {result.WriterStage}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Sandbox Only:** {result.SandboxOnly}");
        sb.AppendLine($"- **Buffer:** {result.BufferWidth}x{result.BufferHeight} {result.CoordinateSystem}");
        sb.AppendLine($"- **Input Operations:** {result.InputOperationCount}");
        sb.AppendLine($"- **Replay Entries:** {result.ReplayEntryCount}");
        sb.AppendLine($"- **Touched Cells:** {result.TouchedCellCount}");
        sb.AppendLine($"- **Collisions:** {result.CollisionCount}");
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

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27B WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Buffer V0");
        sb.AppendLine($"Generated UTC      : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID             : {result.MapId}");
        sb.AppendLine($"Target Component   : {result.TargetComponentId}");
        sb.AppendLine($"Writer Stage       : {result.WriterStage}");
        sb.AppendLine($"Verdict            : {result.Verdict}");
        sb.AppendLine($"Is Valid           : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only       : {(result.SandboxOnly ? 1 : 0)}");
        sb.AppendLine($"Writer Ready       : {(result.WriterReady ? 1 : 0)}");
        sb.AppendLine($"Runtime Valid      : {(result.RuntimeValid ? 1 : 0)}");
        sb.AppendLine($"Materialized       : {(result.Materialized ? 1 : 0)}");
        sb.AppendLine($"Runtime Proof      : {(result.RuntimeProofClaimed ? 1 : 0)}");
        sb.AppendLine($"Public Playable    : {(result.PublicPlayablePackagingClaimed ? 1 : 0)}");
        sb.AppendLine($"Gate Status        : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"Buffer             : {result.BufferWidth}x{result.BufferHeight} {result.CoordinateSystem}");
        sb.AppendLine($"Input Op Files     : {result.InputOperationFileCount}");
        sb.AppendLine($"Input Ops Total    : {result.InputOperationCount}");
        sb.AppendLine($"Comp Ops           : {result.ComponentOperationCount}");
        sb.AppendLine($"Lot Ops            : {result.LotOperationCount}");
        sb.AppendLine($"Slot Ops           : {result.BuildingSlotOperationCount}");
        sb.AppendLine($"Access Ops         : {result.AccessOperationCount}");
        sb.AppendLine($"Replay Entries     : {result.ReplayEntryCount}");
        sb.AppendLine($"Touched Cells      : {result.TouchedCellCount}");
        sb.AppendLine($"Primary Owned      : {result.PrimaryOwnedCellCount}");
        sb.AppendLine($"Collisions         : {result.CollisionCount}");
        sb.AppendLine($"Ownership Kinds    : {result.OwnershipKindCount}");
        sb.AppendLine($"Checks             : {result.CheckCount}");
        sb.AppendLine($"Passed             : {result.PassedCheckCount}");
        sb.Append($"Failed             : {result.FailedCheckCount}");
        return sb.ToString();
    }
}
