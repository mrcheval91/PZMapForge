using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadPlacementPlanBuilder
{
    private static readonly Dictionary<string, string> IntentToRole =
        new(StringComparer.Ordinal)
        {
            ["local_street_asphalt"]        = "road_surface",
            ["alley_ruelle_asphalt"]        = "road_surface",
            ["service_lane"]                = "road_surface",
            ["parking_access"]              = "road_surface",
            ["sidewalk_or_pedestrian_cut"]  = "pedestrian_cut",
            ["intersection_node"]           = "road_node",
            ["road_turn_node"]              = "road_node",
            ["dead_end_node"]               = "road_node",
        };

    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static System2StaticRoadPlacementPlanResult Build(string extractPath)
    {
        var result = new System2StaticRoadPlacementPlanResult();

        if (!File.Exists(extractPath))
        {
            result.Errors.Add($"Extract file not found: {extractPath}");
            return result;
        }

        var placements     = new List<System2StaticRoadPlacementRecord>();
        var positionsSeen  = new HashSet<(int, int)>();
        var duplicateCount = 0;
        var runSources     = 0;
        var nodeSources    = 0;
        int originX, originY, width, height;

        try
        {
            var json = File.ReadAllText(extractPath, Encoding.UTF8);
            using var doc  = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            originX = root.GetProperty("origin_x").GetInt32();
            originY = root.GetProperty("origin_y").GetInt32();
            width   = root.TryGetProperty("width",  out var wP) ? wP.GetInt32() : 0;
            height  = root.TryGetProperty("height", out var hP) ? hP.GetInt32() : 0;

            if (!root.TryGetProperty("layers", out var layersElem)
                || layersElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Extract JSON missing 'layers' array");
                return result;
            }

            foreach (var layerElem in layersElem.EnumerateArray())
            {
                var layerId    = layerElem.TryGetProperty("id",    out var idP)    ? idP.GetString()    ?? "" : "";
                var layerClass = layerElem.TryGetProperty("class", out var classP) ? classP.GetString() ?? "" : "";

                if (layerElem.TryGetProperty("runs", out var runsElem)
                    && runsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var runElem in runsElem.EnumerateArray())
                    {
                        runSources++;
                        var y      = runElem.GetProperty("y").GetInt32();
                        var xStart = runElem.GetProperty("x_start").GetInt32();
                        var xEnd   = runElem.GetProperty("x_end").GetInt32();
                        var intent = runElem.TryGetProperty("intent", out var iP) ? iP.GetString() ?? "" : "";
                        var color  = runElem.TryGetProperty("color",  out var cP) ? cP.GetString() ?? "" : "";

                        if (!IntentToRole.TryGetValue(intent, out var role))
                        {
                            result.Errors.Add($"Unknown intent '{intent}' in layer '{layerId}' run at y={y}");
                            continue;
                        }

                        for (var px = xStart; px <= xEnd; px++)
                        {
                            var wx = originX + px;
                            var wy = originY + y;
                            if (!positionsSeen.Add((wx, wy))) duplicateCount++;

                            placements.Add(new System2StaticRoadPlacementRecord(
                                WorldX:    wx,
                                WorldY:    wy,
                                PixelX:    px,
                                PixelY:    y,
                                LayerId:   layerId,
                                LayerClass: layerClass,
                                Intent:    intent,
                                Role:      role,
                                Color:     color));
                        }
                    }
                }

                if (layerElem.TryGetProperty("nodes", out var nodesElem)
                    && nodesElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var nodeElem in nodesElem.EnumerateArray())
                    {
                        nodeSources++;
                        var pixelX = nodeElem.GetProperty("pixel_x").GetInt32();
                        var pixelY = nodeElem.GetProperty("pixel_y").GetInt32();
                        var worldX = nodeElem.GetProperty("world_x").GetInt32();
                        var worldY = nodeElem.GetProperty("world_y").GetInt32();
                        var intent = nodeElem.TryGetProperty("intent", out var iP) ? iP.GetString() ?? "" : "";
                        var color  = nodeElem.TryGetProperty("color",  out var cP) ? cP.GetString() ?? "" : "";

                        if (!IntentToRole.TryGetValue(intent, out var role))
                        {
                            result.Errors.Add($"Unknown intent '{intent}' in layer '{layerId}' node at ({pixelX},{pixelY})");
                            continue;
                        }

                        if (!positionsSeen.Add((worldX, worldY))) duplicateCount++;

                        placements.Add(new System2StaticRoadPlacementRecord(
                            WorldX:    worldX,
                            WorldY:    worldY,
                            PixelX:    pixelX,
                            PixelY:    pixelY,
                            LayerId:   layerId,
                            LayerClass: layerClass,
                            Intent:    intent,
                            Role:      role,
                            Color:     color));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse extract JSON: {ex.Message}");
            return result;
        }

        if (result.Errors.Count > 0)
        {
            result.IsValid = false;
            return result;
        }

        var byRole   = new Dictionary<string, int>(StringComparer.Ordinal);
        var byIntent = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var p in placements)
        {
            byRole[p.Role]     = (byRole.TryGetValue(p.Role,     out var rc) ? rc : 0) + 1;
            byIntent[p.Intent] = (byIntent.TryGetValue(p.Intent, out var ic) ? ic : 0) + 1;
        }

        result.IsValid = true;
        result.Plan    = new System2StaticRoadPlacementPlan
        {
            SourceExtract = extractPath,
            OriginX       = originX,
            OriginY       = originY,
            Width         = width,
            Height        = height,
            Placements    = placements,
            Totals        = new System2StaticRoadPlacementTotals
            {
                PlacementCount         = placements.Count,
                RunSourceCount         = runSources,
                NodeSourceCount        = nodeSources,
                DuplicatePositionCount = duplicateCount,
                ByRole                 = byRole,
                ByIntent               = byIntent,
            },
            ClaimBoundary = new System2PlacementClaimBoundary(),
        };

        return result;
    }
}
