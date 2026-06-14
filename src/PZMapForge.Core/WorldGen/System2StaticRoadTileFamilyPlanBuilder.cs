using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadTileFamilyPlanBuilder
{
    private static readonly Dictionary<string, string> IntentToFamily =
        new(StringComparer.Ordinal)
        {
            ["local_street_asphalt"]       = "asphalt_road_surface_candidate",
            ["alley_ruelle_asphalt"]       = "asphalt_alley_surface_candidate",
            ["service_lane"]               = "asphalt_service_lane_candidate",
            ["parking_access"]             = "asphalt_parking_access_candidate",
            ["sidewalk_or_pedestrian_cut"] = "concrete_or_sidewalk_candidate",
            ["intersection_node"]          = "road_node_metadata_candidate",
            ["road_turn_node"]             = "road_node_metadata_candidate",
            ["dead_end_node"]              = "road_node_metadata_candidate",
        };

    private const string Confidence = "LOW_METADATA_ONLY";

    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static System2StaticRoadTileFamilyPlanResult Build(string placementPlanPath)
    {
        var result = new System2StaticRoadTileFamilyPlanResult();

        if (!File.Exists(placementPlanPath))
        {
            result.Errors.Add($"Placement plan file not found: {placementPlanPath}");
            return result;
        }

        var records       = new List<System2StaticRoadTileFamilyRecord>();
        var unmappedCount = 0;

        try
        {
            var json = File.ReadAllText(placementPlanPath, Encoding.UTF8);
            using var doc  = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (!root.TryGetProperty("placements", out var placementsElem)
                || placementsElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Placement plan JSON missing 'placements' array");
                return result;
            }

            foreach (var p in placementsElem.EnumerateArray())
            {
                var worldX     = p.GetProperty("world_x").GetInt32();
                var worldY     = p.GetProperty("world_y").GetInt32();
                var pixelX     = p.GetProperty("pixel_x").GetInt32();
                var pixelY     = p.GetProperty("pixel_y").GetInt32();
                var layerId    = p.TryGetProperty("layer_id", out var liP) ? liP.GetString() ?? "" : "";
                var layerClass = p.TryGetProperty("class",    out var lcP) ? lcP.GetString() ?? "" : "";
                var intent     = p.TryGetProperty("intent",   out var iP)  ? iP.GetString()  ?? "" : "";
                var role       = p.TryGetProperty("role",     out var rP)  ? rP.GetString()  ?? "" : "";
                var color      = p.TryGetProperty("color",    out var cP)  ? cP.GetString()  ?? "" : "";

                if (!IntentToFamily.TryGetValue(intent, out var family))
                {
                    result.Errors.Add($"Unknown intent '{intent}' in placement at world ({worldX},{worldY})");
                    unmappedCount++;
                    continue;
                }

                records.Add(new System2StaticRoadTileFamilyRecord(
                    WorldX:          worldX,
                    WorldY:          worldY,
                    PixelX:          pixelX,
                    PixelY:          pixelY,
                    LayerId:         layerId,
                    LayerClass:      layerClass,
                    Intent:          intent,
                    Role:            role,
                    CandidateFamily: family,
                    Confidence:      Confidence,
                    Color:           color));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse placement plan JSON: {ex.Message}");
            return result;
        }

        if (result.Errors.Count > 0)
        {
            result.IsValid = false;
            return result;
        }

        result.IsValid = true;
        result.Plan    = new System2StaticRoadTileFamilyPlan
        {
            SourcePlacementPlan = placementPlanPath,
            Records             = records,
            Totals              = BuildTotals(records, unmappedCount),
        };
        return result;
    }

    private static System2StaticRoadTileFamilyTotals BuildTotals(
        List<System2StaticRoadTileFamilyRecord> records, int unmappedCount)
    {
        var byFamily = new Dictionary<string, int>(StringComparer.Ordinal);
        var byRole   = new Dictionary<string, int>(StringComparer.Ordinal);
        var byIntent = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var r in records)
        {
            byFamily[r.CandidateFamily] = (byFamily.TryGetValue(r.CandidateFamily, out var fc) ? fc : 0) + 1;
            byRole[r.Role]              = (byRole.TryGetValue(r.Role,              out var rc) ? rc : 0) + 1;
            byIntent[r.Intent]          = (byIntent.TryGetValue(r.Intent,          out var ic) ? ic : 0) + 1;
        }

        return new System2StaticRoadTileFamilyTotals
        {
            RecordCount       = records.Count,
            ByCandidateFamily = byFamily,
            ByRole            = byRole,
            ByIntent          = byIntent,
            UnmappedCount     = unmappedCount,
        };
    }
}
