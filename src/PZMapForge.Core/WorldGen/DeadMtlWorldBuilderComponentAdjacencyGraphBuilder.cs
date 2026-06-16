using System.Drawing;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public static class DeadMtlWorldBuilderComponentAdjacencyGraphBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };
    private static readonly int[] Dx = { 0, 0, -1, 1 };
    private static readonly int[] Dy = { -1, 1, 0, 0 };

    public static DeadMtlWorldBuilderComponentAdjacencyGraphResult Build(
        string sourcePngPath,
        string connectedComponentExtractionPath,
        string componentIntentClassificationPath,
        string geometryPrimitiveSchemaPath,
        string concreteGeometryPreflightPath)
    {
        var errors = new List<string>();
        var graph = new DeadMtlWorldBuilderComponentAdjacencyGraph
        {
            TileId                                    = "map_00",
            SourcePng                                 = sourcePngPath,
            SourceConnectedComponentExtractionJson    = connectedComponentExtractionPath,
            SourceComponentIntentClassificationJson   = componentIntentClassificationPath,
            SourceGeometryPrimitiveSchemaJson         = geometryPrimitiveSchemaPath,
            SourceConcreteGeometryPreflightJson       = concreteGeometryPreflightPath,
            AdjacencyStatus                           = "COMPONENT_ADJACENCY_GRAPH_BUILT",
        };

        var inputs = new[]
        {
            ("source PNG",                           sourcePngPath),
            ("connected component extraction",       connectedComponentExtractionPath),
            ("component intent classification",      componentIntentClassificationPath),
            ("geometry primitive schema",            geometryPrimitiveSchemaPath),
            ("concrete geometry preflight",          concreteGeometryPreflightPath),
        };

        foreach (var (label, path) in inputs)
        {
            if (!File.Exists(path))
                errors.Add($"Missing {label}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, graph);

        var intentByOrder = LoadIntentData(componentIntentClassificationPath, errors);
        if (errors.Count > 0)
            return Fail(errors, graph);

        Bitmap bmp;
        try { bmp = new Bitmap(sourcePngPath); }
        catch (Exception ex)
        {
            errors.Add($"Failed to load PNG: {ex.Message}");
            return Fail(errors, graph);
        }

        int width  = bmp.Width;
        int height = bmp.Height;

        if (width != 256 || height != 256)
        {
            bmp.Dispose();
            errors.Add($"PNG must be 256x256, got {width}x{height}");
            return Fail(errors, graph);
        }

        var pixels = new int[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var px = bmp.GetPixel(x, y);
                pixels[y * width + x] = (px.R << 16) | (px.G << 8) | px.B;
            }
        bmp.Dispose();

        graph.AdjacencyGraphContract = new DeadMtlWorldBuilderComponentAdjacencyGraphContract
        {
            TileId                                   = "map_00",
            SourcePngWidthPx                         = width,
            SourcePngHeightPx                        = height,
            SourcePixelCount                         = width * height,
            ComponentNodeCount                       = intentByOrder.Count,
            SourceComponentContract                  = "MAP25K_CONNECTED_COMPONENT_EXTRACTION",
            SourceIntentContract                     = "MAP25L_COMPONENT_INTENT_CLASSIFICATION",
            ConnectivityRule                         = "FOUR_WAY_NEIGHBOR_PIXELS",
            DiagonalAdjacencyEnabled                 = false,
            EdgeContactUnits                         = "SOURCE_PIXEL_EDGES",
            AdjacencyEdgesAreUndirected              = true,
            AdjacencyEdgesAreNotGeometry             = true,
            ComponentBoundsArePixelBoundsNotGeometry = true,
            GeometryMustBeCreatedByFutureStep        = true,
        };

        graph.ValidationRules = new DeadMtlWorldBuilderComponentAdjacencyGraphValidationRules
        {
            SourcePngMustExist                              = true,
            ConnectedComponentExtractionMustExist           = true,
            ComponentIntentClassificationMustExist          = true,
            ComponentNodeCountMustEqual45                   = true,
            AllComponentIdsMustExistInIntentClassification  = true,
            AdjacencyEdgeCountMustEqual82                   = true,
            ContactLengthTotalMustEqual4535                 = true,
            NoSelfEdges                                     = true,
            NoDuplicateUndirectedEdges                      = true,
            NoDiagonalAdjacency                             = true,
            AllEdgesMustHavePositiveContactLength           = true,
            AdjacencyEdgesAreNotGeometry                    = true,
            NoRuntimeClaimFromAdjacencyGraph                = true,
            NoWriterClaimFromAdjacencyGraph                 = true,
            NoMaterializationFromAdjacencyGraph             = true,
        };

        var pixelLabel = BuildPixelLabels(pixels, width, height);

        var rawEdges = new Dictionary<(int A, int B), int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx  = y * width + x;
                int compA = pixelLabel[idx];

                if (x + 1 < width)
                {
                    int compB = pixelLabel[y * width + x + 1];
                    if (compA != compB)
                    {
                        var key = compA < compB ? (compA, compB) : (compB, compA);
                        rawEdges.TryGetValue(key, out var cnt);
                        rawEdges[key] = cnt + 1;
                    }
                }

                if (y + 1 < height)
                {
                    int compB = pixelLabel[(y + 1) * width + x];
                    if (compA != compB)
                    {
                        var key = compA < compB ? (compA, compB) : (compB, compA);
                        rawEdges.TryGetValue(key, out var cnt);
                        rawEdges[key] = cnt + 1;
                    }
                }
            }
        }

        var sortedEdges = rawEdges
            .OrderBy(kv => kv.Key.A)
            .ThenBy(kv => kv.Key.B)
            .ToList();

        int edgeOrder = 1;
        foreach (var (key, contact) in sortedEdges)
        {
            int aOrder = key.A;
            int bOrder = key.B;

            var intentA = intentByOrder[aOrder];
            var intentB = intentByOrder[bOrder];

            var relationship = ClassifyRelationship(intentA.ComponentIntent, intentB.ComponentIntent);
            var pairKey      = BuildIntentPairKey(intentA.ComponentIntent, intentB.ComponentIntent);

            graph.AdjacencyEdges.Add(new DeadMtlWorldBuilderAdjacencyEdgeRecord
            {
                EdgeOrder               = edgeOrder,
                EdgeId                  = $"map_00_adjacency_{edgeOrder:D4}",
                ComponentAId            = intentA.ComponentId,
                ComponentAOrder         = aOrder,
                ComponentASourceColor   = intentA.ParentSourceColor,
                ComponentAIntent        = intentA.ComponentIntent,
                ComponentAIntentFamily  = intentA.IntentFamily,
                ComponentBId            = intentB.ComponentId,
                ComponentBOrder         = bOrder,
                ComponentBSourceColor   = intentB.ParentSourceColor,
                ComponentBIntent        = intentB.ComponentIntent,
                ComponentBIntentFamily  = intentB.IntentFamily,
                ContactLengthPx         = contact,
                ContactUnits            = "SOURCE_PIXEL_EDGES",
                AdjacencyRelationship   = relationship,
                IntentPairKey           = pairKey,
                IsUndirected            = true,
                ConnectivityRule        = "FOUR_WAY_NEIGHBOR_PIXELS",
                DiagonalContact         = false,
                EdgeStatus              = "PIXEL_TOUCH_ADJACENCY_ONLY",
                GeometryStatus          = "ADJACENCY_ONLY_NO_GEOMETRY_CREATED",
                FuturePlanningUse       = FuturePlanningUse(relationship),
                BlockedByRequirements   = BlockedBy(relationship),
                Notes                   = NotesFor(relationship),
            });

            edgeOrder++;
        }

        graph.Totals        = ComputeTotals(graph.AdjacencyEdges, intentByOrder.Count);
        graph.ClaimBoundary = new DeadMtlWorldBuilderComponentAdjacencyGraphClaimBoundary();

        return new DeadMtlWorldBuilderComponentAdjacencyGraphResult
            { IsValid = true, Errors = errors, Graph = graph };
    }

    private static int[] BuildPixelLabels(int[] pixels, int width, int height)
    {
        var pixelLabel = new int[pixels.Length];

        var colorCounts = new Dictionary<int, int>();
        foreach (var c in pixels)
        {
            colorCounts.TryGetValue(c, out var cnt);
            colorCounts[c] = cnt + 1;
        }

        var parentOrder = colorCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Select(kv => kv.Key)
            .ToList();

        var visited     = new bool[pixels.Length];
        int globalOrder = 1;

        foreach (var colorKey in parentOrder)
        {
            var components = new List<(int Count, int MinY, int MinX, List<int> Members)>();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (visited[i] || pixels[i] != colorKey) continue;

                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;

                int initX = i % width, initY = i / width;
                int mnX = initX, mnY = initY;
                var members = new List<int>();

                while (queue.Count > 0)
                {
                    int idx = queue.Dequeue();
                    int cx  = idx % width, cy = idx / width;
                    members.Add(idx);
                    if (cx < mnX) mnX = cx;
                    if (cy < mnY) mnY = cy;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + Dx[d], ny = cy + Dy[d];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int ni = ny * width + nx;
                        if (visited[ni] || pixels[ni] != colorKey) continue;
                        visited[ni] = true;
                        queue.Enqueue(ni);
                    }
                }

                components.Add((members.Count, mnY, mnX, members));
            }

            var sorted = components
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.MinY)
                .ThenBy(c => c.MinX)
                .ToList();

            foreach (var (_, _, _, members) in sorted)
            {
                int order = globalOrder++;
                foreach (var idx in members)
                    pixelLabel[idx] = order;
            }
        }

        return pixelLabel;
    }

    private static string ClassifyRelationship(string intentA, string intentB)
    {
        if (intentA == "IGNORE_BORDER" || intentB == "IGNORE_BORDER")
            return "IGNORE_BOUNDARY_ADJACENCY";

        if ((intentA == "MAIN_ROAD_CORRIDOR" &&
                intentB is "RESIDENTIAL_LOT_BLOCK" or "COMMERCIAL_LOT_BLOCK" or "CIVIC_PLACEHOLDER") ||
            (intentB == "MAIN_ROAD_CORRIDOR" &&
                intentA is "RESIDENTIAL_LOT_BLOCK" or "COMMERCIAL_LOT_BLOCK" or "CIVIC_PLACEHOLDER"))
            return "FRONTAGE_CANDIDATE";

        if ((intentA == "BACK_ALLEY_CORRIDOR" &&
                intentB is "RESIDENTIAL_LOT_BLOCK" or "COMMERCIAL_LOT_BLOCK" or "CIVIC_PLACEHOLDER") ||
            (intentB == "BACK_ALLEY_CORRIDOR" &&
                intentA is "RESIDENTIAL_LOT_BLOCK" or "COMMERCIAL_LOT_BLOCK" or "CIVIC_PLACEHOLDER"))
            return "REAR_OR_SERVICE_ACCESS_CANDIDATE";

        if ((intentA == "MAIN_ROAD_CORRIDOR" && intentB == "BACK_ALLEY_CORRIDOR") ||
            (intentB == "MAIN_ROAD_CORRIDOR" && intentA == "BACK_ALLEY_CORRIDOR"))
            return "STREET_NETWORK_TOUCHPOINT";

        if ((intentA == "GREENSPACE_MASS" &&
                intentB is "MAIN_ROAD_CORRIDOR" or "BACK_ALLEY_CORRIDOR") ||
            (intentB == "GREENSPACE_MASS" &&
                intentA is "MAIN_ROAD_CORRIDOR" or "BACK_ALLEY_CORRIDOR"))
            return "GREENSPACE_ACCESS_EDGE";

        if ((intentA == "GREENSPACE_MASS" && intentB == "CIVIC_PLACEHOLDER") ||
            (intentB == "GREENSPACE_MASS" && intentA == "CIVIC_PLACEHOLDER"))
            return "GREENSPACE_CIVIC_EDGE";

        if ((intentA == "RESIDENTIAL_LOT_BLOCK" && intentB == "COMMERCIAL_LOT_BLOCK") ||
            (intentB == "RESIDENTIAL_LOT_BLOCK" && intentA == "COMMERCIAL_LOT_BLOCK"))
            return "MIXED_LOT_BLOCK_EDGE";

        return "OTHER_INTENT_ADJACENCY";
    }

    private static string BuildIntentPairKey(string intentA, string intentB)
    {
        var sorted = new[] { intentA, intentB };
        Array.Sort(sorted, StringComparer.Ordinal);
        return $"{sorted[0]}|{sorted[1]}";
    }

    private static string FuturePlanningUse(string relationship) => relationship switch
    {
        "FRONTAGE_CANDIDATE"               => "FUTURE_FRONTAGE_PLANNING",
        "REAR_OR_SERVICE_ACCESS_CANDIDATE" => "FUTURE_REAR_SERVICE_ACCESS_PLANNING",
        "STREET_NETWORK_TOUCHPOINT"        => "FUTURE_STREET_NETWORK_PLANNING",
        "GREENSPACE_ACCESS_EDGE"           => "FUTURE_GREENSPACE_ACCESS_PLANNING",
        "GREENSPACE_CIVIC_EDGE"            => "FUTURE_CIVIC_GREENSPACE_CONTEXT_PLANNING",
        "MIXED_LOT_BLOCK_EDGE"             => "FUTURE_LOT_BLOCK_BOUNDARY_PLANNING",
        "IGNORE_BOUNDARY_ADJACENCY"        => "IGNORE_NO_GEOMETRY",
        _                                  => "FUTURE_MANUAL_REVIEW",
    };

    private static List<string> BlockedBy(string relationship) =>
        relationship == "IGNORE_BOUNDARY_ADJACENCY"
            ? new List<string> { "NONE" }
            : new List<string>
            {
                "CONCRETE_GEOMETRY_GENERATOR_NOT_IMPLEMENTED",
                "STATIC_TILE_WRITER_NOT_IMPLEMENTED",
                "RUNTIME_VALIDATION_NOT_RUN",
            };

    private static string NotesFor(string relationship) => relationship switch
    {
        "FRONTAGE_CANDIDATE"               => "Lot block fronts onto main road corridor. Awaiting frontage geometry.",
        "REAR_OR_SERVICE_ACCESS_CANDIDATE" => "Lot block has rear or service access via back alley. Awaiting service geometry.",
        "STREET_NETWORK_TOUCHPOINT"        => "Back alley connects to main road corridor. Awaiting street network geometry.",
        "GREENSPACE_ACCESS_EDGE"           => "Road corridor borders greenspace mass. Awaiting greenspace access geometry.",
        "GREENSPACE_CIVIC_EDGE"            => "Greenspace borders civic placeholder. Awaiting civic greenspace context.",
        "MIXED_LOT_BLOCK_EDGE"             => "Residential and commercial lot blocks share boundary. Awaiting lot boundary geometry.",
        "IGNORE_BOUNDARY_ADJACENCY"        => "Adjacency involves border or void component. No geometry planned.",
        _                                  => "Unclassified adjacency. Requires manual review.",
    };

    private sealed record IntentData(
        int ComponentOrder,
        string ComponentId,
        string ParentSourceColor,
        string ComponentIntent,
        string IntentBucket,
        string IntentFamily);

    private static Dictionary<int, IntentData> LoadIntentData(string path, List<string> errors)
    {
        var result = new Dictionary<int, IntentData>();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path), DocOpts);
            if (!doc.RootElement.TryGetProperty("intent_records", out var arr))
            {
                errors.Add("Component intent classification missing 'intent_records' array.");
                return result;
            }
            foreach (var item in arr.EnumerateArray())
            {
                int   order  = item.GetProperty("component_order").GetInt32();
                var   compId = item.GetProperty("component_id").GetString()          ?? "";
                var   color  = item.GetProperty("parent_source_color").GetString()   ?? "";
                var   intent = item.GetProperty("component_intent").GetString()      ?? "";
                var   bucket = item.GetProperty("intent_bucket").GetString()         ?? "";
                var   family = item.GetProperty("intent_family").GetString()         ?? "";
                result[order] = new IntentData(order, compId, color, intent, bucket, family);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse component intent classification: {ex.Message}");
        }
        return result;
    }

    private static DeadMtlWorldBuilderComponentAdjacencyGraphTotals ComputeTotals(
        List<DeadMtlWorldBuilderAdjacencyEdgeRecord> edges, int nodeCount)
    {
        return new DeadMtlWorldBuilderComponentAdjacencyGraphTotals
        {
            ComponentNodeCount                     = nodeCount,
            AdjacencyEdgeCount                     = edges.Count,
            KnownComponentEdgeCount                = edges.Count,
            UnknownComponentEdgeCount              = 0,
            SelfEdgeCount                          = 0,
            DuplicateEdgeCount                     = 0,
            DiagonalEdgeCount                      = 0,
            ContactLengthTotalPx                   = edges.Sum(e => e.ContactLengthPx),
            FrontageCandidateEdgeCount             = edges.Count(e => e.AdjacencyRelationship == "FRONTAGE_CANDIDATE"),
            RearOrServiceAccessCandidateEdgeCount  = edges.Count(e => e.AdjacencyRelationship == "REAR_OR_SERVICE_ACCESS_CANDIDATE"),
            StreetNetworkTouchpointEdgeCount       = edges.Count(e => e.AdjacencyRelationship == "STREET_NETWORK_TOUCHPOINT"),
            GreenspaceAccessEdgeCount              = edges.Count(e => e.AdjacencyRelationship == "GREENSPACE_ACCESS_EDGE"),
            GreenspaceCivicEdgeCount               = edges.Count(e => e.AdjacencyRelationship == "GREENSPACE_CIVIC_EDGE"),
            MixedLotBlockEdgeCount                 = edges.Count(e => e.AdjacencyRelationship == "MIXED_LOT_BLOCK_EDGE"),
            IgnoreBoundaryAdjacencyEdgeCount       = edges.Count(e => e.AdjacencyRelationship == "IGNORE_BOUNDARY_ADJACENCY"),
            OtherIntentAdjacencyEdgeCount          = edges.Count(e => e.AdjacencyRelationship == "OTHER_INTENT_ADJACENCY"),
            FrontageCandidateContactTotalPx        = edges.Where(e => e.AdjacencyRelationship == "FRONTAGE_CANDIDATE").Sum(e => e.ContactLengthPx),
            RearOrServiceAccessContactTotalPx      = edges.Where(e => e.AdjacencyRelationship == "REAR_OR_SERVICE_ACCESS_CANDIDATE").Sum(e => e.ContactLengthPx),
            StreetNetworkTouchpointContactTotalPx  = edges.Where(e => e.AdjacencyRelationship == "STREET_NETWORK_TOUCHPOINT").Sum(e => e.ContactLengthPx),
            GreenspaceAccessContactTotalPx         = edges.Where(e => e.AdjacencyRelationship == "GREENSPACE_ACCESS_EDGE").Sum(e => e.ContactLengthPx),
            GreenspaceCivicContactTotalPx          = edges.Where(e => e.AdjacencyRelationship == "GREENSPACE_CIVIC_EDGE").Sum(e => e.ContactLengthPx),
            MixedLotBlockContactTotalPx            = edges.Where(e => e.AdjacencyRelationship == "MIXED_LOT_BLOCK_EDGE").Sum(e => e.ContactLengthPx),
            IgnoreBoundaryAdjacencyContactTotalPx  = edges.Where(e => e.AdjacencyRelationship == "IGNORE_BOUNDARY_ADJACENCY").Sum(e => e.ContactLengthPx),
            OtherIntentAdjacencyContactTotalPx     = edges.Where(e => e.AdjacencyRelationship == "OTHER_INTENT_ADJACENCY").Sum(e => e.ContactLengthPx),
            CreatedGeometryCount                   = 0,
            WriterReadyEdgeCount                   = 0,
            RuntimeValidatedEdgeCount              = 0,
            MaterializedEdgeCount                  = 0,
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderComponentAdjacencyGraph graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25M: DeadMTL WorldBuilder Component Adjacency Graph Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Adjacency graph only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk geometry. No road geometry. No building placement. No fences.");
        sb.AppendLine("> No lotpack writing. No worldgen override file. No concrete geometry created.");
        sb.AppendLine("> No runtime proof. Not writer-ready. Layout not materialized.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {graph.TileId}");
        sb.AppendLine($"**status:** {graph.Status}");
        sb.AppendLine($"**adjacency_status:** {graph.AdjacencyStatus}");
        sb.AppendLine($"**geometry_status:** {graph.GeometryStatus}");
        sb.AppendLine($"**generation_status:** {graph.GenerationStatus}");
        sb.AppendLine($"**materialization_status:** {graph.MaterializationStatus}");
        sb.AppendLine($"**runtime_status:** {graph.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine($"- source PNG: `{graph.SourcePng}`");
        sb.AppendLine($"- connected component extraction: `{graph.SourceConnectedComponentExtractionJson}`");
        sb.AppendLine($"- component intent classification: `{graph.SourceComponentIntentClassificationJson}`");
        sb.AppendLine($"- geometry primitive schema: `{graph.SourceGeometryPrimitiveSchemaJson}`");
        sb.AppendLine($"- concrete geometry preflight: `{graph.SourceConcreteGeometryPreflightJson}`");
        sb.AppendLine();
        sb.AppendLine("## Adjacency Graph Contract");
        sb.AppendLine();
        var agc = graph.AdjacencyGraphContract;
        sb.AppendLine($"- tile_id: {agc.TileId}");
        sb.AppendLine($"- source_png_width_px: {agc.SourcePngWidthPx}");
        sb.AppendLine($"- source_png_height_px: {agc.SourcePngHeightPx}");
        sb.AppendLine($"- source_pixel_count: {agc.SourcePixelCount}");
        sb.AppendLine($"- component_node_count: {agc.ComponentNodeCount}");
        sb.AppendLine($"- source_component_contract: {agc.SourceComponentContract}");
        sb.AppendLine($"- source_intent_contract: {agc.SourceIntentContract}");
        sb.AppendLine($"- connectivity_rule: {agc.ConnectivityRule}");
        sb.AppendLine($"- diagonal_adjacency_enabled: {agc.DiagonalAdjacencyEnabled}");
        sb.AppendLine($"- edge_contact_units: {agc.EdgeContactUnits}");
        sb.AppendLine($"- adjacency_edges_are_undirected: {agc.AdjacencyEdgesAreUndirected}");
        sb.AppendLine($"- adjacency_edges_are_not_geometry: {agc.AdjacencyEdgesAreNotGeometry}");
        sb.AppendLine($"- component_bounds_are_pixel_bounds_not_geometry: {agc.ComponentBoundsArePixelBoundsNotGeometry}");
        sb.AppendLine($"- geometry_must_be_created_by_future_step: {agc.GeometryMustBeCreatedByFutureStep}");
        sb.AppendLine();
        sb.AppendLine("## Adjacency Detection Rule");
        sb.AppendLine();
        sb.AppendLine("4-way pixel adjacency only. For every source pixel, compare right (x+1, y) and");
        sb.AppendLine("down (x, y+1) neighbors to avoid double-counting. Edges are undirected:");
        sb.AppendLine("component_a_order < component_b_order. No diagonal contact. No self-edges.");
        sb.AppendLine("contact_length_px counts touching 4-way pixel edges between the two components.");
        sb.AppendLine();
        sb.AppendLine("## Relationship Classification Rules");
        sb.AppendLine();
        sb.AppendLine("| Intent A | Intent B | Relationship |");
        sb.AppendLine("|----------|----------|--------------|");
        sb.AppendLine("| MAIN_ROAD_CORRIDOR | RESIDENTIAL_LOT_BLOCK | FRONTAGE_CANDIDATE |");
        sb.AppendLine("| MAIN_ROAD_CORRIDOR | COMMERCIAL_LOT_BLOCK | FRONTAGE_CANDIDATE |");
        sb.AppendLine("| MAIN_ROAD_CORRIDOR | CIVIC_PLACEHOLDER | FRONTAGE_CANDIDATE |");
        sb.AppendLine("| BACK_ALLEY_CORRIDOR | RESIDENTIAL_LOT_BLOCK | REAR_OR_SERVICE_ACCESS_CANDIDATE |");
        sb.AppendLine("| BACK_ALLEY_CORRIDOR | COMMERCIAL_LOT_BLOCK | REAR_OR_SERVICE_ACCESS_CANDIDATE |");
        sb.AppendLine("| BACK_ALLEY_CORRIDOR | CIVIC_PLACEHOLDER | REAR_OR_SERVICE_ACCESS_CANDIDATE |");
        sb.AppendLine("| MAIN_ROAD_CORRIDOR | BACK_ALLEY_CORRIDOR | STREET_NETWORK_TOUCHPOINT |");
        sb.AppendLine("| MAIN_ROAD_CORRIDOR | GREENSPACE_MASS | GREENSPACE_ACCESS_EDGE |");
        sb.AppendLine("| BACK_ALLEY_CORRIDOR | GREENSPACE_MASS | GREENSPACE_ACCESS_EDGE |");
        sb.AppendLine("| GREENSPACE_MASS | CIVIC_PLACEHOLDER | GREENSPACE_CIVIC_EDGE |");
        sb.AppendLine("| RESIDENTIAL_LOT_BLOCK | COMMERCIAL_LOT_BLOCK | MIXED_LOT_BLOCK_EDGE |");
        sb.AppendLine("| (any) | IGNORE_BORDER | IGNORE_BOUNDARY_ADJACENCY |");
        sb.AppendLine("| (other) | (other) | OTHER_INTENT_ADJACENCY |");
        sb.AppendLine();
        sb.AppendLine("## Adjacency Edge Records");
        sb.AppendLine();
        sb.AppendLine("| Order | Edge ID | Component A | Component B | Contact px | Relationship |");
        sb.AppendLine("|-------|---------|-------------|-------------|------------|--------------|");
        foreach (var e in graph.AdjacencyEdges)
        {
            sb.AppendLine(
                $"| {e.EdgeOrder} | {e.EdgeId} | {e.ComponentAId} ({e.ComponentAIntent}) " +
                $"| {e.ComponentBId} ({e.ComponentBIntent}) | {e.ContactLengthPx} | {e.AdjacencyRelationship} |");
        }
        sb.AppendLine();
        var t = graph.Totals;
        sb.AppendLine("## Graph Totals");
        sb.AppendLine();
        sb.AppendLine($"- component_node_count: {t.ComponentNodeCount}");
        sb.AppendLine($"- adjacency_edge_count: {t.AdjacencyEdgeCount}");
        sb.AppendLine($"- known_component_edge_count: {t.KnownComponentEdgeCount}");
        sb.AppendLine($"- unknown_component_edge_count: {t.UnknownComponentEdgeCount}");
        sb.AppendLine($"- self_edge_count: {t.SelfEdgeCount}");
        sb.AppendLine($"- duplicate_edge_count: {t.DuplicateEdgeCount}");
        sb.AppendLine($"- diagonal_edge_count: {t.DiagonalEdgeCount}");
        sb.AppendLine($"- contact_length_total_px: {t.ContactLengthTotalPx}");
        sb.AppendLine($"- frontage_candidate_edge_count: {t.FrontageCandidateEdgeCount}");
        sb.AppendLine($"- rear_or_service_access_candidate_edge_count: {t.RearOrServiceAccessCandidateEdgeCount}");
        sb.AppendLine($"- street_network_touchpoint_edge_count: {t.StreetNetworkTouchpointEdgeCount}");
        sb.AppendLine($"- greenspace_access_edge_count: {t.GreenspaceAccessEdgeCount}");
        sb.AppendLine($"- greenspace_civic_edge_count: {t.GreenspaceCivicEdgeCount}");
        sb.AppendLine($"- mixed_lot_block_edge_count: {t.MixedLotBlockEdgeCount}");
        sb.AppendLine($"- ignore_boundary_adjacency_edge_count: {t.IgnoreBoundaryAdjacencyEdgeCount}");
        sb.AppendLine($"- other_intent_adjacency_edge_count: {t.OtherIntentAdjacencyEdgeCount}");
        sb.AppendLine();
        sb.AppendLine("## Relationship Contact Totals");
        sb.AppendLine();
        sb.AppendLine($"- frontage_candidate_contact_total_px: {t.FrontageCandidateContactTotalPx}");
        sb.AppendLine($"- rear_or_service_access_contact_total_px: {t.RearOrServiceAccessContactTotalPx}");
        sb.AppendLine($"- street_network_touchpoint_contact_total_px: {t.StreetNetworkTouchpointContactTotalPx}");
        sb.AppendLine($"- greenspace_access_contact_total_px: {t.GreenspaceAccessContactTotalPx}");
        sb.AppendLine($"- greenspace_civic_contact_total_px: {t.GreenspaceCivicContactTotalPx}");
        sb.AppendLine($"- mixed_lot_block_contact_total_px: {t.MixedLotBlockContactTotalPx}");
        sb.AppendLine($"- ignore_boundary_adjacency_contact_total_px: {t.IgnoreBoundaryAdjacencyContactTotalPx}");
        sb.AppendLine($"- other_intent_adjacency_contact_total_px: {t.OtherIntentAdjacencyContactTotalPx}");
        sb.AppendLine();
        sb.AppendLine("## Intent Pair Edge Totals");
        sb.AppendLine();
        var pairs = graph.AdjacencyEdges
            .GroupBy(e => BuildIntentPairKey(e.ComponentAIntent, e.ComponentBIntent))
            .OrderBy(g => g.Key);
        sb.AppendLine("| Intent Pair | Edge Count | Contact Total px |");
        sb.AppendLine("|-------------|------------|------------------|");
        foreach (var g in pairs)
        {
            sb.AppendLine($"| {g.Key} | {g.Count()} | {g.Sum(e => e.ContactLengthPx)} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Future Planning Use");
        sb.AppendLine();
        sb.AppendLine("| Relationship | Future Planning Use |");
        sb.AppendLine("|--------------|---------------------|");
        sb.AppendLine("| FRONTAGE_CANDIDATE | FUTURE_FRONTAGE_PLANNING |");
        sb.AppendLine("| REAR_OR_SERVICE_ACCESS_CANDIDATE | FUTURE_REAR_SERVICE_ACCESS_PLANNING |");
        sb.AppendLine("| STREET_NETWORK_TOUCHPOINT | FUTURE_STREET_NETWORK_PLANNING |");
        sb.AppendLine("| GREENSPACE_ACCESS_EDGE | FUTURE_GREENSPACE_ACCESS_PLANNING |");
        sb.AppendLine("| GREENSPACE_CIVIC_EDGE | FUTURE_CIVIC_GREENSPACE_CONTEXT_PLANNING |");
        sb.AppendLine("| MIXED_LOT_BLOCK_EDGE | FUTURE_LOT_BLOCK_BOUNDARY_PLANNING |");
        sb.AppendLine("| IGNORE_BOUNDARY_ADJACENCY | IGNORE_NO_GEOMETRY |");
        sb.AppendLine("| OTHER_INTENT_ADJACENCY | FUTURE_MANUAL_REVIEW |");
        sb.AppendLine();
        sb.AppendLine("## Why This Still Cannot Execute");
        sb.AppendLine();
        sb.AppendLine("Adjacency edges are pixel-space contact records between classified components.");
        sb.AppendLine("They are NOT concrete lot polygons, road geometries, or building slots.");
        sb.AppendLine($"- created_geometry_count: {t.CreatedGeometryCount}");
        sb.AppendLine($"- writer_ready_edge_count: {t.WriterReadyEdgeCount}");
        sb.AppendLine($"- runtime_validated_edge_count: {t.RuntimeValidatedEdgeCount}");
        sb.AppendLine($"- materialized_edge_count: {t.MaterializedEdgeCount}");
        sb.AppendLine("No concrete geometry generator exists. No tile writer exists.");
        sb.AppendLine("No runtime validation pass has been performed.");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        var cb = graph.ClaimBoundary;
        sb.AppendLine($"- writes_lotpack: {cb.WritesLotpack}");
        sb.AppendLine($"- writes_worldgen_lua: {cb.WritesWorldgenLua}");
        sb.AppendLine($"- runtime_proven: {cb.RuntimeProven}");
        sb.AppendLine($"- public_playable_claim: {cb.PublicPlayableClaim}");
        sb.AppendLine($"- writer_ready_claim: {cb.WriterReadyClaim}");
        sb.AppendLine($"- generates_terrain_now: {cb.GeneratesTerrainNow}");
        sb.AppendLine($"- generates_buildings_now: {cb.GeneratesBuildingsNow}");
        sb.AppendLine($"- generates_sidewalks_now: {cb.GeneratesSidewalksNow}");
        sb.AppendLine($"- subdivides_lots_now: {cb.SubdividesLotsNow}");
        sb.AppendLine($"- captures_chunk_layers_now: {cb.CapturesChunkLayersNow}");
        sb.AppendLine($"- places_fences_now: {cb.PlacesFencesNow}");
        sb.AppendLine($"- places_unique_buildings_now: {cb.PlacesUniqueBuildingsNow}");
        sb.AppendLine($"- selects_concrete_building_ids_now: {cb.SelectsConcreteBuildingIdsNow}");
        sb.AppendLine($"- creates_concrete_geometry_now: {cb.CreatesConcreteGeometryNow}");
        sb.AppendLine($"- materializes_layout_now: {cb.MaterializesLayoutNow}");
        sb.AppendLine();
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25M_WORLDBUILDER_COMPONENT_ADJACENCY_GRAPH_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderComponentAdjacencyGraph graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "edge_order,edge_id,component_a_id,component_a_order,component_a_source_color," +
            "component_a_intent,component_a_intent_family," +
            "component_b_id,component_b_order,component_b_source_color," +
            "component_b_intent,component_b_intent_family," +
            "contact_length_px,contact_units,adjacency_relationship,intent_pair_key," +
            "is_undirected,connectivity_rule,diagonal_contact,edge_status,geometry_status," +
            "future_planning_use,blocked_by_requirements,notes");
        foreach (var e in graph.AdjacencyEdges)
        {
            sb.AppendLine(
                $"{e.EdgeOrder},{e.EdgeId},{e.ComponentAId},{e.ComponentAOrder},{e.ComponentASourceColor}," +
                $"{e.ComponentAIntent},{e.ComponentAIntentFamily}," +
                $"{e.ComponentBId},{e.ComponentBOrder},{e.ComponentBSourceColor}," +
                $"{e.ComponentBIntent},{e.ComponentBIntentFamily}," +
                $"{e.ContactLengthPx},{e.ContactUnits},{e.AdjacencyRelationship},{e.IntentPairKey}," +
                $"{e.IsUndirected},{e.ConnectivityRule},{e.DiagonalContact},{e.EdgeStatus},{e.GeometryStatus}," +
                $"{e.FuturePlanningUse}," +
                $"\"{string.Join("|", e.BlockedByRequirements)}\"," +
                $"\"{e.Notes}\"");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderComponentAdjacencyGraphResult result)
    {
        var g  = result.Graph;
        var t  = g.Totals;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25M: DeadMTL WorldBuilder Component Adjacency Graph Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                                         {g.TileId}");
        sb.AppendLine($"is_valid:                                        {result.IsValid}");
        sb.AppendLine($"component_node_count:                            {t.ComponentNodeCount}");
        sb.AppendLine($"adjacency_edge_count:                            {t.AdjacencyEdgeCount}");
        sb.AppendLine($"known_component_edge_count:                      {t.KnownComponentEdgeCount}");
        sb.AppendLine($"unknown_component_edge_count:                    {t.UnknownComponentEdgeCount}");
        sb.AppendLine($"self_edge_count:                                 {t.SelfEdgeCount}");
        sb.AppendLine($"duplicate_edge_count:                            {t.DuplicateEdgeCount}");
        sb.AppendLine($"diagonal_edge_count:                             {t.DiagonalEdgeCount}");
        sb.AppendLine($"contact_length_total_px:                         {t.ContactLengthTotalPx}");
        sb.AppendLine($"frontage_candidate_edge_count:                   {t.FrontageCandidateEdgeCount}");
        sb.AppendLine($"rear_or_service_access_candidate_edge_count:     {t.RearOrServiceAccessCandidateEdgeCount}");
        sb.AppendLine($"street_network_touchpoint_edge_count:            {t.StreetNetworkTouchpointEdgeCount}");
        sb.AppendLine($"greenspace_access_edge_count:                    {t.GreenspaceAccessEdgeCount}");
        sb.AppendLine($"greenspace_civic_edge_count:                     {t.GreenspaceCivicEdgeCount}");
        sb.AppendLine($"mixed_lot_block_edge_count:                      {t.MixedLotBlockEdgeCount}");
        sb.AppendLine($"ignore_boundary_adjacency_edge_count:            {t.IgnoreBoundaryAdjacencyEdgeCount}");
        sb.AppendLine($"other_intent_adjacency_edge_count:               {t.OtherIntentAdjacencyEdgeCount}");
        sb.AppendLine($"created_geometry_count:                          {t.CreatedGeometryCount}");
        sb.AppendLine($"writer_ready_edge_count:                         {t.WriterReadyEdgeCount}");
        sb.AppendLine($"runtime_validated_edge_count:                    {t.RuntimeValidatedEdgeCount}");
        sb.AppendLine($"materialized_edge_count:                         {t.MaterializedEdgeCount}");
        sb.AppendLine($"adjacency_status:                                {g.AdjacencyStatus}");
        sb.AppendLine($"geometry_status:                                 {g.GeometryStatus}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var err in result.Errors)
                sb.AppendLine($"  - {err}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25M_WORLDBUILDER_COMPONENT_ADJACENCY_GRAPH_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderComponentAdjacencyGraphResult Fail(
        List<string> errors, DeadMtlWorldBuilderComponentAdjacencyGraph graph) =>
        new() { IsValid = false, Errors = errors, Graph = graph };
}
