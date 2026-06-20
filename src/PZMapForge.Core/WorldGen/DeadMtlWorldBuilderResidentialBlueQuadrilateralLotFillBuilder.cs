using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillBuilder
{
    private static readonly JsonSerializerOptions s_json = new() { WriteIndented = true };

    private const double FillRatioThreshold      = 0.70;
    private const int    TargetFrontageTiles      = 15;
    private const double AdjacencyThresholdRatio  = 0.25;
    private const int    AdjacencyMinCount        = 2;

    private static readonly (byte R, byte G, byte B)[] s_shades =
    {
        (200, 168, 120),
        (176, 140,  96),
        (152, 116,  76),
    };

    private static readonly (byte R, byte G, byte B)[] s_redShades =
    {
        (166,  0,  0),
        (196, 20, 20),
        (226, 40, 40),
    };

    private const string ParcelClassBlue = "BLUE_RESIDENTIAL";
    private const string ParcelClassRed  = "RED_RESIDENTIAL_OR_COMMERCIAL";

    private sealed record LotSizingPolicy(
        string ParcelClass,
        string NeighborhoodSector,
        int    TargetFrontageTiles,
        int    MinFrontageTiles,
        int    MinDepthTiles,
        int    MinAreaTiles,
        bool   MergeUndersizedLots);

    private sealed class LotSizingPolicyStore
    {
        private readonly List<LotSizingPolicy> _policies;
        public string Version       { get; }
        public string DefaultSector { get; }
        public int    EntryCount    => _policies.Count;

        public LotSizingPolicyStore(string version, string defaultSector, List<LotSizingPolicy> policies)
        {
            Version       = version;
            DefaultSector = defaultSector;
            _policies     = policies;
        }

        public LotSizingPolicy Resolve(string parcelClass, string sector = "DEFAULT")
        {
            foreach (var p in _policies)
                if (p.ParcelClass == parcelClass && p.NeighborhoodSector == sector)
                    return p;
            foreach (var p in _policies)
                if (p.ParcelClass == parcelClass && p.NeighborhoodSector == "DEFAULT")
                    return p;
            return parcelClass == ParcelClassRed
                ? new(parcelClass, "DEFAULT", 18, 12, 10, 160, true)
                : new(parcelClass, "DEFAULT", 15,  8,  8,  96, true);
        }
    }

    private static readonly LotSizingPolicyStore s_defaultPolicyStore = new(
        "MAP29B1_DEFAULT_V1", "DEFAULT",
        new List<LotSizingPolicy>
        {
            new(ParcelClassBlue, "DEFAULT", 15,  8,  8,  96, true),
            new(ParcelClassRed,  "DEFAULT", 18, 12, 10, 160, true),
        });

    private LotSizingPolicyStore _policyStore = s_defaultPolicyStore;

    private LotSizingPolicy GetLotSizingPolicy(string parcelClass, string sector = "DEFAULT")
        => _policyStore.Resolve(parcelClass, sector);

    private static LotSizingPolicyStore LoadPolicyStore(string path)
    {
        var json      = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;
        string version       = root.GetProperty("policy_version").GetString() ?? "";
        string defaultSector = root.TryGetProperty("default_sector", out var ds)
            ? ds.GetString() ?? "DEFAULT" : "DEFAULT";
        var policies = new List<LotSizingPolicy>();
        foreach (var entry in root.GetProperty("policies").EnumerateArray())
        {
            policies.Add(new LotSizingPolicy(
                entry.GetProperty("parcel_class").GetString()        ?? "",
                entry.GetProperty("neighborhood_sector").GetString() ?? "DEFAULT",
                entry.GetProperty("target_frontage_tiles").GetInt32(),
                entry.GetProperty("min_frontage_tiles").GetInt32(),
                entry.GetProperty("min_depth_tiles").GetInt32(),
                entry.GetProperty("min_area_tiles").GetInt32(),
                entry.GetProperty("merge_undersized_lots").GetBoolean()
            ));
        }
        if (policies.Count == 0)
            throw new InvalidOperationException("Policy file contains no policy entries.");
        return new LotSizingPolicyStore(version, defaultSector, policies);
    }

    private static bool IsUndersized(QuadrilateralLot lot, LotSizingPolicy policy)
    {
        bool isNS    = lot.FrontageDirection == "NORTH" || lot.FrontageDirection == "SOUTH";
        int  frontage = isNS ? lot.Width  : lot.Height;
        int  depth    = isNS ? lot.Height : lot.Width;
        return frontage < policy.MinFrontageTiles
            || depth    < policy.MinDepthTiles
            || lot.TileCount < policy.MinAreaTiles;
    }

    private static int SharedEdgeLength(QuadrilateralLot a, QuadrilateralLot b)
    {
        if (a.X2 + 1 == b.X1 || b.X2 + 1 == a.X1)
        {
            int oy = Math.Min(a.Y2, b.Y2) - Math.Max(a.Y1, b.Y1) + 1;
            return oy > 0 ? oy : 0;
        }
        if (a.Y2 + 1 == b.Y1 || b.Y2 + 1 == a.Y1)
        {
            int ox = Math.Min(a.X2, b.X2) - Math.Max(a.X1, b.X1) + 1;
            return ox > 0 ? ox : 0;
        }
        return 0;
    }

    // -----------------------------------------------------------------------
    // Color classifiers (documented, deterministic)
    // -----------------------------------------------------------------------

    // IsResidentialBlue: exact known residential blue colors OR conservative blue classifier.
    // Excludes cyan, orange, and near-black.
    private static bool IsResidentialBlue(byte r, byte g, byte b)
    {
        if ((r == 58  && g == 94  && b == 174) ||
            (r == 74  && g == 110 && b == 190) ||
            (r == 42  && g == 78  && b == 158))
            return true;
        if (b < 120)            return false;
        if (b < r + 25)         return false;
        if (b < g + 10)         return false;
        if (r < 30 && g < 30)   return false; // black background
        if (IsStreetAccessOrange(r, g, b)) return false;
        // exclude cyan
        if (r == 40 && g == 192 && b == 192) return false;
        return true;
    }

    // IsStreetAccessOrange: conservative orange family classifier for street/access detection.
    private static bool IsStreetAccessOrange(byte r, byte g, byte b)
        => r >= 180 && g >= 80 && g <= 180 && b <= 80 && r >= g + 40 && g >= b + 20;

    // IsSourceRed: broad parcelable red classifier for BFS component extraction.
    // Excludes orange street/access (g >= 80).
    private static bool IsSourceRed(byte r, byte g, byte b)
    {
        if (r == 206 && g == 0 && b == 0) return true;
        if (r < 140) return false;
        if (g > 40)  return false;
        if (b > 40)  return false;
        if (IsStreetAccessOrange(r, g, b)) return false;
        return true;
    }

    // IsOriginalSourceRed: tight classifier for FinalizeAfterOutputs scan only.
    // Must NOT match output shades (166,0,0), (196,20,20), (226,40,40):
    //   shade 0 excluded by r < 180, shades 1+2 excluded by g > 5 / b > 5.
    private static bool IsOriginalSourceRed(byte r, byte g, byte b)
        => r >= 180 && g <= 5 && b <= 5;

    // -----------------------------------------------------------------------
    // Connected-component extraction (4-connectivity BFS)
    // -----------------------------------------------------------------------

    private static List<(int X1, int Y1, int X2, int Y2, int PixelCount, HashSet<(int x, int y)> Pixels)>
        ExtractComponents(System.Drawing.Bitmap bmp, Func<byte, byte, byte, bool> classifier)
    {
        int w = bmp.Width, h = bmp.Height;
        var visited = new bool[w, h];
        var result  = new List<(int, int, int, int, int, HashSet<(int, int)>)>();

        for (int sy = 0; sy < h; sy++)
        for (int sx = 0; sx < w; sx++)
        {
            if (visited[sx, sy]) continue;
            var px = bmp.GetPixel(sx, sy);
            if (!classifier(px.R, px.G, px.B)) continue;

            var pixels = new HashSet<(int, int)>();
            var queue  = new Queue<(int, int)>();
            queue.Enqueue((sx, sy));
            visited[sx, sy] = true;

            int x1 = sx, y1 = sy, x2 = sx, y2 = sy;

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                pixels.Add((cx, cy));
                if (cx < x1) x1 = cx; if (cx > x2) x2 = cx;
                if (cy < y1) y1 = cy; if (cy > y2) y2 = cy;

                foreach (var (nx, ny) in new[] { (cx-1,cy),(cx+1,cy),(cx,cy-1),(cx,cy+1) })
                {
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (visited[nx, ny]) continue;
                    var npx = bmp.GetPixel(nx, ny);
                    if (!classifier(npx.R, npx.G, npx.B)) continue;
                    visited[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }
            result.Add((x1, y1, x2, y2, pixels.Count, pixels));
        }

        // Sort by bboxY1 then bboxX1 for deterministic ordering
        result.Sort((a, b) =>
        {
            int c = a.Item2.CompareTo(b.Item2);
            return c != 0 ? c : a.Item1.CompareTo(b.Item1);
        });
        return result;
    }

    // -----------------------------------------------------------------------
    // Street adjacency detection
    // -----------------------------------------------------------------------

    private static int CountOrangeOnSide(System.Drawing.Bitmap bmp, int x1, int y1, int x2, int y2)
    {
        int count = 0;
        for (int x = x1; x <= x2; x++)
        for (int y = y1; y <= y2; y++)
        {
            if (x < 0 || y < 0 || x >= bmp.Width || y >= bmp.Height) continue;
            var px = bmp.GetPixel(x, y);
            if (IsStreetAccessOrange(px.R, px.G, px.B)) count++;
        }
        return count;
    }

    private static bool MeetsAdjacencyThreshold(int contactCount, int sideLength)
        => contactCount >= Math.Max(AdjacencyMinCount, (int)Math.Ceiling(sideLength * AdjacencyThresholdRatio));

    // -----------------------------------------------------------------------
    // Lot calculation helpers
    // -----------------------------------------------------------------------

    private static IReadOnlyList<(int S1, int S2, int Span)> SplitInclusiveSpan(int start, int end, int count)
    {
        int total     = end - start + 1;
        int baseSpan  = total / count;
        int remainder = total % count;
        var ranges    = new List<(int, int, int)>(count);
        int cursor    = start;
        for (int i = 0; i < count; i++)
        {
            int span = baseSpan + (i < remainder ? 1 : 0);
            ranges.Add((cursor, cursor + span - 1, span));
            cursor += span;
        }
        return ranges;
    }

    private static int CalculateLotCount(int frontageSpan, int targetFrontageTiles)
        => Math.Max(1, (int)Math.Round((double)frontageSpan / targetFrontageTiles));

    private static (byte R, byte G, byte B) GetShadeFromPalette(
        (byte R, byte G, byte B)[] palette,
        int componentOrder, int primaryIndex, int secondaryIndex)
        => palette[(componentOrder * 7 + primaryIndex + secondaryIndex) % palette.Length];

    private static void ReassignShades(
        List<QuadrilateralLot> lots, int compOrder, (byte R, byte G, byte B)[] shades)
    {
        if (lots.Count == 0) return;
        bool isNS = lots[0].FrontageDirection == "NORTH" || lots[0].FrontageDirection == "SOUTH";
        if (isNS)
        {
            var groups = lots.GroupBy(l => l.FrontageDirection)
                             .OrderBy(g => g.Key == "NORTH" ? 0 : 1);
            int rowOffset = 0;
            foreach (var grp in groups)
            {
                var ordered = grp.OrderBy(l => l.X1).ToList();
                for (int ci = 0; ci < ordered.Count; ci++)
                {
                    var shade = GetShadeFromPalette(shades, compOrder, ci, rowOffset);
                    var lot   = ordered[ci];
                    lot.ShadeR = shade.R; lot.ShadeG = shade.G; lot.ShadeB = shade.B;
                    lot.ShadeRgb = $"{shade.R},{shade.G},{shade.B}";
                }
                rowOffset++;
            }
        }
        else
        {
            var groups = lots.GroupBy(l => l.FrontageDirection)
                             .OrderBy(g => g.Key == "WEST" ? 0 : 1);
            int colOffset = 0;
            foreach (var grp in groups)
            {
                var ordered = grp.OrderBy(l => l.Y1).ToList();
                for (int ri = 0; ri < ordered.Count; ri++)
                {
                    var shade = GetShadeFromPalette(shades, compOrder, ri, colOffset);
                    var lot   = ordered[ri];
                    lot.ShadeR = shade.R; lot.ShadeG = shade.G; lot.ShadeB = shade.B;
                    lot.ShadeRgb = $"{shade.R},{shade.G},{shade.B}";
                }
                colOffset++;
            }
        }
    }

    private static List<QuadrilateralFacadeEdge> RebuildFacadeEdges(
        List<QuadrilateralLot> lots, DetectedBlueComponent comp, int compOrder)
    {
        var edges = new List<QuadrilateralFacadeEdge>();
        if (lots.Count == 0) return edges;
        bool isNS = lots[0].FrontageDirection == "NORTH" || lots[0].FrontageDirection == "SOUTH";
        int frontageSpan = isNS ? comp.BboxWidth : comp.BboxHeight;

        foreach (var lot in lots)
        {
            string edgeId = $"{lot.LotId}_FACADE_EDGE";
            lot.PrimaryFacadeEdgeId = edgeId;

            int x1, y1, x2, y2, length, contactCount;
            switch (lot.FrontageDirection)
            {
                case "NORTH":
                    x1 = lot.X1; y1 = lot.Y1; x2 = lot.X2; y2 = lot.Y1;
                    length = lot.Width; contactCount = comp.NorthStreetContactCount;
                    break;
                case "SOUTH":
                    x1 = lot.X1; y1 = lot.Y2; x2 = lot.X2; y2 = lot.Y2;
                    length = lot.Width; contactCount = comp.SouthStreetContactCount;
                    break;
                case "EAST":
                    x1 = lot.X2; y1 = lot.Y1; x2 = lot.X2; y2 = lot.Y2;
                    length = lot.Height; contactCount = comp.EastStreetContactCount;
                    break;
                default: // WEST
                    x1 = lot.X1; y1 = lot.Y1; x2 = lot.X1; y2 = lot.Y2;
                    length = lot.Height; contactCount = comp.WestStreetContactCount;
                    break;
            }

            edges.Add(new QuadrilateralFacadeEdge
            {
                ComponentId        = comp.ComponentId,
                LotId              = lot.LotId,
                FacadeEdgeId       = edgeId,
                FrontageDirection  = lot.FrontageDirection,
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                LengthTiles        = length,
                StreetContactCount = contactCount,
                StreetContactRatio = Math.Round((double)contactCount / frontageSpan, 4),
            });
        }
        return edges;
    }

    private static (List<QuadrilateralLot> Lots, List<QuadrilateralFacadeEdge> Edges, int MergeCount)
        MergeUndersizedLots(
            List<QuadrilateralLot>        inputLots,
            List<QuadrilateralFacadeEdge> inputEdges,
            DetectedBlueComponent         comp,
            int                           compOrder,
            (byte R, byte G, byte B)[]    shades,
            LotSizingPolicy               policy)
    {
        var working    = new List<QuadrilateralLot>(inputLots);
        int mergeCount = 0;

        bool anyMerged;
        do
        {
            anyMerged = false;
            var undersizedList = working
                .Where(l => IsUndersized(l, policy))
                .OrderBy(l => l.LotId)
                .ToList();

            foreach (var undersized in undersizedList)
            {
                if (!working.Contains(undersized)) continue;
                if (working.Count == 1) break;

                QuadrilateralLot? best    = null;
                int bestSameDir = -1, bestEdge = -1, bestArea = -1, bestIdx = int.MaxValue;

                for (int i = 0; i < working.Count; i++)
                {
                    var candidate = working[i];
                    if (ReferenceEquals(candidate, undersized)) continue;
                    int edgeLen = SharedEdgeLength(undersized, candidate);
                    if (edgeLen <= 0) continue;
                    int sameDir = candidate.FrontageDirection == undersized.FrontageDirection ? 1 : 0;
                    int area    = candidate.TileCount;
                    bool isBetter =
                        best is null
                        || sameDir  > bestSameDir
                        || (sameDir == bestSameDir && edgeLen >  bestEdge)
                        || (sameDir == bestSameDir && edgeLen == bestEdge && area >  bestArea)
                        || (sameDir == bestSameDir && edgeLen == bestEdge && area == bestArea && i < bestIdx);
                    if (isBetter)
                    { best = candidate; bestSameDir = sameDir; bestEdge = edgeLen; bestArea = area; bestIdx = i; }
                }

                if (best is null) continue;

                best.X1 = Math.Min(undersized.X1, best.X1);
                best.Y1 = Math.Min(undersized.Y1, best.Y1);
                best.X2 = Math.Max(undersized.X2, best.X2);
                best.Y2 = Math.Max(undersized.Y2, best.Y2);
                best.Width     = best.X2 - best.X1 + 1;
                best.Height    = best.Y2 - best.Y1 + 1;
                best.TileCount = best.Width * best.Height;
                working.Remove(undersized);
                mergeCount++;
                anyMerged = true;
            }
        } while (anyMerged && working.Count > 1);

        if (mergeCount > 0)
        {
            ReassignShades(working, compOrder, shades);
            return (working, RebuildFacadeEdges(working, comp, compOrder), mergeCount);
        }
        return (working, inputEdges, 0);
    }

    // -----------------------------------------------------------------------
    // Build
    // -----------------------------------------------------------------------

    public DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult Build(
        string sourcePngPath, string outputRoot, string? lotSizingPolicyPath = null)
    {
        var result = new DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult
        {
            Format       = "MAP-29A_WORLDBUILDER_RESIDENTIAL_BLUE_QUADRILATERAL_LOT_FILL",
            GeneratedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            SourcePng    = sourcePngPath,
            MapId        = "map_00",
            SandboxOnly  = true,
        };

        var checks = new List<QuadrilateralLotFillCheck>();

        // Reset to builtin defaults; override if external path provided
        _policyStore    = s_defaultPolicyStore;
        string policySource = "BUILTIN_DEFAULT";
        string policyPath   = string.Empty;
        bool   policyLoaded = false;

        if (lotSizingPolicyPath is not null)
        {
            if (!File.Exists(lotSizingPolicyPath))
            {
                result.LotSizingPolicySource     = "EXTERNAL_NOT_FOUND";
                result.LotSizingPolicyPath       = lotSizingPolicyPath;
                result.LotSizingPolicyLoaded     = false;
                result.LotSizingPolicyVersion    = _policyStore.Version;
                result.LotSizingPolicyEntryCount = _policyStore.EntryCount;
                result.Errors.Add($"Lot sizing policy file not found: {lotSizingPolicyPath}");
                FinalizeResult(result, checks, outputRoot, valid: false, verdict: "MAP29C_POLICY_NOT_FOUND");
                return result;
            }
            try
            {
                _policyStore = LoadPolicyStore(lotSizingPolicyPath);
                policySource = "EXTERNAL";
                policyPath   = lotSizingPolicyPath;
                policyLoaded = true;
            }
            catch (Exception ex)
            {
                result.LotSizingPolicySource     = "EXTERNAL_INVALID";
                result.LotSizingPolicyPath       = lotSizingPolicyPath;
                result.LotSizingPolicyLoaded     = false;
                result.LotSizingPolicyVersion    = _policyStore.Version;
                result.LotSizingPolicyEntryCount = _policyStore.EntryCount;
                result.Errors.Add($"Lot sizing policy invalid: {ex.Message}");
                FinalizeResult(result, checks, outputRoot, valid: false, verdict: "MAP29C_POLICY_INVALID");
                return result;
            }
        }

        var bluePolicy = GetLotSizingPolicy(ParcelClassBlue);
        var redPolicy  = GetLotSizingPolicy(ParcelClassRed);

        // Check 1 — source PNG exists and is valid
        bool sourceExists = File.Exists(sourcePngPath);
        AddCheck(checks, "MAP29A_SOURCE_PNG_EXISTS", "Source PNG exists", "PASS",
            sourceExists ? "PASS" : "FAIL");

        if (!sourceExists)
        {
            result.Errors.Add($"Source PNG not found: {sourcePngPath}");
            FinalizeResult(result, checks, outputRoot, valid: false, verdict: "MAP29A_SOURCE_PNG_NOT_FOUND");
            return result;
        }

        // Compute source hash
        result.SourcePngSha256 = ComputeSha256(sourcePngPath);

        // Load source and get dimensions
        byte[] sourceBytes = File.ReadAllBytes(sourcePngPath);
        using var srcBmp = LoadBitmap(sourceBytes);

        bool is256 = srcBmp.Width == 256 && srcBmp.Height == 256;
        AddCheck(checks, "MAP29A_SOURCE_PNG_256x256", "Source PNG is 256x256",
            "PASS", is256 ? "PASS" : "FAIL");
        AddPendingCheck(checks, "MAP29A_SOURCE_PNG_HASH_UNCHANGED",
            "Source PNG SHA256 unchanged after all operations");
        if (!is256)
        {
            result.Errors.Add($"Source PNG is {srcBmp.Width}x{srcBmp.Height}, expected 256x256");
            FinalizeResult(result, checks, outputRoot, valid: false, verdict: "MAP29A_SOURCE_PNG_WRONG_SIZE");
            return result;
        }

        // Extract connected blue components
        var rawComponents = ExtractComponents(srcBmp, IsResidentialBlue);

        result.DetectedBlueComponentCount = rawComponents.Count;
        AddCheck(checks, "MAP29A_DETECTED_BLUE_COMPONENT_COUNT_GT0",
            "Detected at least one blue component",
            "PASS", rawComponents.Count > 0 ? "PASS" : "FAIL");

        // Process each component
        var allLots    = new List<QuadrilateralLot>();
        var allEdges   = new List<QuadrilateralFacadeEdge>();
        var components = new List<DetectedBlueComponent>();
        // pixel → lot shade mapping for output PNG
        var pixelShadeMap = new Dictionary<(int x, int y), (byte R, byte G, byte B)>();
        int totalBlueMergeCount = 0;
        int totalRedMergeCount  = 0;

        for (int ci = 0; ci < rawComponents.Count; ci++)
        {
            var (bx1, by1, bx2, by2, pixCount, pixels) = rawComponents[ci];
            string compId = $"MAP29A_BLUE_COMPONENT_{ci:000}";

            int bboxW    = bx2 - bx1 + 1;
            int bboxH    = by2 - by1 + 1;
            double ratio = (double)pixCount / (bboxW * bboxH);

            bool processable    = ratio >= FillRatioThreshold;
            string unsupReason  = string.Empty;

            // Street adjacency (scan 1 pixel outside bbox on each side)
            int nContact = CountOrangeOnSide(srcBmp, bx1, by1 - 1, bx2, by1 - 1);
            int sContact = CountOrangeOnSide(srcBmp, bx1, by2 + 1, bx2, by2 + 1);
            int eContact = CountOrangeOnSide(srcBmp, bx2 + 1, by1, bx2 + 1, by2);
            int wContact = CountOrangeOnSide(srcBmp, bx1 - 1, by1, bx1 - 1, by2);

            bool nAdj = MeetsAdjacencyThreshold(nContact, bboxW);
            bool sAdj = MeetsAdjacencyThreshold(sContact, bboxW);
            bool eAdj = MeetsAdjacencyThreshold(eContact, bboxH);
            bool wAdj = MeetsAdjacencyThreshold(wContact, bboxH);

            if (processable && !nAdj && !sAdj && !eAdj && !wAdj)
            {
                processable = false;
                unsupReason = "no street/access adjacency found on any side";
            }
            else if (!processable)
            {
                unsupReason = $"fill_ratio={ratio:F2} below threshold {FillRatioThreshold:F2}";
            }

            var comp = new DetectedBlueComponent
            {
                ComponentId             = compId,
                ParcelClass             = ParcelClassBlue,
                BboxX1                  = bx1, BboxY1 = by1, BboxX2 = bx2, BboxY2 = by2,
                BboxWidth               = bboxW,
                BboxHeight              = bboxH,
                PixelCount              = pixCount,
                FillRatio               = Math.Round(ratio, 4),
                Processable             = processable,
                UnsupportedReason       = unsupReason,
                NorthStreetAdjacency    = nAdj,
                SouthStreetAdjacency    = sAdj,
                EastStreetAdjacency     = eAdj,
                WestStreetAdjacency     = wAdj,
                NorthStreetContactCount = nContact,
                SouthStreetContactCount = sContact,
                EastStreetContactCount  = eContact,
                WestStreetContactCount  = wContact,
            };

            comp.NorthStreetContactRatio = bboxW > 0 ? Math.Round((double)nContact / bboxW, 4) : 0;
            comp.SouthStreetContactRatio = bboxW > 0 ? Math.Round((double)sContact / bboxW, 4) : 0;
            comp.EastStreetContactRatio  = bboxH > 0 ? Math.Round((double)eContact / bboxH, 4) : 0;
            comp.WestStreetContactRatio  = bboxH > 0 ? Math.Round((double)wContact / bboxH, 4) : 0;

            if (processable)
            {
                var (lots, edges, orientCase, frontageGroup, primarySides, mergeBlue) = CalculateLots(comp, ci, s_shades, bluePolicy);
                totalBlueMergeCount   += mergeBlue;
                comp.OrientationCase       = orientCase;
                comp.SelectedFrontageGroup = frontageGroup;
                comp.SelectedPrimarySides  = primarySides;
                comp.LotCount         = lots.Count;
                comp.FacadeEdgeCount  = edges.Count;
                comp.LotIds           = lots.Select(l => l.LotId).ToList();
                comp.FacadeEdgeIds    = edges.Select(e => e.FacadeEdgeId).ToList();

                // Map pixels to shade from their owning lot
                foreach (var lot in lots)
                    foreach (var (px, py) in pixels
                        .Where(p => p.x >= lot.X1 && p.x <= lot.X2 && p.y >= lot.Y1 && p.y <= lot.Y2))
                        pixelShadeMap[(px, py)] = ((byte)lot.ShadeR, (byte)lot.ShadeG, (byte)lot.ShadeB);

                allLots.AddRange(lots);
                allEdges.AddRange(edges);
            }

            components.Add(comp);
        }

        // -----------------------------------------------------------------------
        // Red component extraction and processing
        // -----------------------------------------------------------------------

        var rawRedComponents = ExtractComponents(srcBmp, IsSourceRed);
        result.DetectedRedComponentCount = rawRedComponents.Count;
        int totalSourceRedPixels = 0;

        for (int rci = 0; rci < rawRedComponents.Count; rci++)
        {
            var (bx1r, by1r, bx2r, by2r, pixCountR, pixelsR) = rawRedComponents[rci];
            string compIdR = $"MAP29B_RED_COMPONENT_{rci:000}";

            totalSourceRedPixels += pixCountR;

            int bboxWR    = bx2r - bx1r + 1;
            int bboxHR    = by2r - by1r + 1;
            double ratioR = (double)pixCountR / (bboxWR * bboxHR);

            bool processableR   = ratioR >= FillRatioThreshold;
            string unsupReasonR = string.Empty;

            int nContactR = CountOrangeOnSide(srcBmp, bx1r, by1r - 1, bx2r, by1r - 1);
            int sContactR = CountOrangeOnSide(srcBmp, bx1r, by2r + 1, bx2r, by2r + 1);
            int eContactR = CountOrangeOnSide(srcBmp, bx2r + 1, by1r, bx2r + 1, by2r);
            int wContactR = CountOrangeOnSide(srcBmp, bx1r - 1, by1r, bx1r - 1, by2r);

            bool nAdjR = MeetsAdjacencyThreshold(nContactR, bboxWR);
            bool sAdjR = MeetsAdjacencyThreshold(sContactR, bboxWR);
            bool eAdjR = MeetsAdjacencyThreshold(eContactR, bboxHR);
            bool wAdjR = MeetsAdjacencyThreshold(wContactR, bboxHR);

            if (processableR && !nAdjR && !sAdjR && !eAdjR && !wAdjR)
            {
                processableR = false;
                unsupReasonR = "no street/access adjacency found on any side";
            }
            else if (!processableR)
            {
                unsupReasonR = $"fill_ratio={ratioR:F2} below threshold {FillRatioThreshold:F2}";
            }

            var compR = new DetectedBlueComponent
            {
                ComponentId             = compIdR,
                ParcelClass             = ParcelClassRed,
                BboxX1                  = bx1r, BboxY1 = by1r, BboxX2 = bx2r, BboxY2 = by2r,
                BboxWidth               = bboxWR,
                BboxHeight              = bboxHR,
                PixelCount              = pixCountR,
                FillRatio               = Math.Round(ratioR, 4),
                Processable             = processableR,
                UnsupportedReason       = unsupReasonR,
                NorthStreetAdjacency    = nAdjR,
                SouthStreetAdjacency    = sAdjR,
                EastStreetAdjacency     = eAdjR,
                WestStreetAdjacency     = wAdjR,
                NorthStreetContactCount = nContactR,
                SouthStreetContactCount = sContactR,
                EastStreetContactCount  = eContactR,
                WestStreetContactCount  = wContactR,
            };

            compR.NorthStreetContactRatio = bboxWR > 0 ? Math.Round((double)nContactR / bboxWR, 4) : 0;
            compR.SouthStreetContactRatio = bboxWR > 0 ? Math.Round((double)sContactR / bboxWR, 4) : 0;
            compR.EastStreetContactRatio  = bboxHR > 0 ? Math.Round((double)eContactR / bboxHR, 4) : 0;
            compR.WestStreetContactRatio  = bboxHR > 0 ? Math.Round((double)wContactR / bboxHR, 4) : 0;

            if (processableR)
            {
                var (lotsR, edgesR, orientCaseR, frontageGroupR, primarySidesR, mergeRed) = CalculateLots(compR, rci, s_redShades, redPolicy);
                totalRedMergeCount    += mergeRed;
                compR.OrientationCase       = orientCaseR;
                compR.SelectedFrontageGroup = frontageGroupR;
                compR.SelectedPrimarySides  = primarySidesR;
                compR.LotCount        = lotsR.Count;
                compR.FacadeEdgeCount = edgesR.Count;
                compR.LotIds          = lotsR.Select(l => l.LotId).ToList();
                compR.FacadeEdgeIds   = edgesR.Select(e => e.FacadeEdgeId).ToList();

                foreach (var lot in lotsR)
                    foreach (var (px, py) in pixelsR
                        .Where(p => p.x >= lot.X1 && p.x <= lot.X2 && p.y >= lot.Y1 && p.y <= lot.Y2))
                        pixelShadeMap[(px, py)] = ((byte)lot.ShadeR, (byte)lot.ShadeG, (byte)lot.ShadeB);

                allLots.AddRange(lotsR);
                allEdges.AddRange(edgesR);
            }

            components.Add(compR);
        }

        _lastTotalSourceRedPixelCount = totalSourceRedPixels;

        // -----------------------------------------------------------------------
        // Populate result totals
        // -----------------------------------------------------------------------

        result.Components   = components;
        result.Lots         = allLots;
        result.FacadeEdges  = allEdges;

        var blueComps = components.Where(c => c.ParcelClass == ParcelClassBlue).ToList();
        var redComps  = components.Where(c => c.ParcelClass == ParcelClassRed).ToList();

        result.ProcessedQuadrilateralComponentCount = blueComps.Count(c => c.Processable);
        result.UnsupportedBlueComponentCount        = blueComps.Count(c => !c.Processable);
        result.BlueLotCount                         = allLots.Count(l => l.ComponentId.StartsWith("MAP29A_BLUE_"));
        result.BlueFacadeEdgeCount                  = allEdges.Count(e => e.ComponentId.StartsWith("MAP29A_BLUE_"));
        result.ProcessedRedComponentCount           = redComps.Count(c => c.Processable);
        result.UnsupportedRedComponentCount         = redComps.Count(c => !c.Processable);
        result.RedLotCount                          = allLots.Count(l => l.ComponentId.StartsWith("MAP29B_RED_"));
        result.RedFacadeEdgeCount                   = allEdges.Count(e => e.ComponentId.StartsWith("MAP29B_RED_"));
        result.TotalLotCount                        = allLots.Count;
        result.TotalFacadeEdgeCount                 = allEdges.Count;

        result.LotSizingPolicySource     = policySource;
        result.LotSizingPolicyPath       = policyPath;
        result.LotSizingPolicyLoaded     = policyLoaded;
        result.LotSizingPolicyVersion    = _policyStore.Version;
        result.LotSizingPolicyEntryCount = _policyStore.EntryCount;
        result.BlueUndersizedLotMergeCount   = totalBlueMergeCount;
        result.RedUndersizedLotMergeCount    = totalRedMergeCount;
        result.UndersizedLotMergeCount       = totalBlueMergeCount + totalRedMergeCount;

        // Store pixel map for RenderOutputPngBytes
        _lastSourceBytes  = sourceBytes;
        _lastPixelShadeMap = pixelShadeMap;

        // Check 3 — unsupported count = 0
        AddCheck(checks, "MAP29A_UNSUPPORTED_BLUE_COMPONENT_COUNT_0",
            "Unsupported blue component count is 0",
            "0", result.UnsupportedBlueComponentCount.ToString());

        // Check 4 — every component processed
        bool allProcessed = result.UnsupportedBlueComponentCount == 0;
        AddCheck(checks, "MAP29A_EVERY_BLUE_COMPONENT_PROCESSED",
            "Every detected blue component is processable",
            "PASS", allProcessed ? "PASS" : "FAIL");

        // Check 5 — every component has at least one street adjacency (already caught by unsupported)
        bool allHaveStreet = components.Where(c => c.Processable)
            .All(c => c.NorthStreetAdjacency || c.SouthStreetAdjacency
                   || c.EastStreetAdjacency  || c.WestStreetAdjacency);
        AddCheck(checks, "MAP29A_ALL_PROCESSED_HAVE_STREET_ADJACENCY",
            "All processed components have at least one street/access adjacency",
            "PASS", allHaveStreet ? "PASS" : "FAIL");

        // Check 6 — no east facade on east_adjacency=false components
        bool noIllegalEastFacade = allEdges
            .Where(e => e.FrontageDirection == "EAST")
            .All(e =>
            {
                var comp = components.First(c => c.ComponentId == e.ComponentId);
                return comp.EastStreetAdjacency;
            });
        AddCheck(checks, "MAP29A_NO_EAST_FACADE_WITHOUT_EAST_ADJACENCY",
            "No EAST facade edge exists on a component with east_street_adjacency=false",
            "PASS", noIllegalEastFacade ? "PASS" : "FAIL");

        // Check 7 — facade direction matches street adjacency for all components
        bool facadesMatchAdjacency = allEdges.All(e =>
        {
            var comp = components.FirstOrDefault(c => c.ComponentId == e.ComponentId);
            if (comp is null) return false;
            return e.FrontageDirection switch
            {
                "NORTH" => comp.NorthStreetAdjacency,
                "SOUTH" => comp.SouthStreetAdjacency,
                "EAST"  => comp.EastStreetAdjacency,
                "WEST"  => comp.WestStreetAdjacency,
                _       => false,
            };
        });
        AddCheck(checks, "MAP29A_FACADES_MATCH_STREET_ADJACENCY",
            "Every facade edge direction matches a street-adjacent side of its component",
            "PASS", facadesMatchAdjacency ? "PASS" : "FAIL");

        // Check 8 — adjacent lots in same row/column use different shades
        bool shadesDistinct = CheckAdjacentShadesDistinct(components, allLots);
        AddCheck(checks, "MAP29A_ADJACENT_LOTS_DIFFERENT_SHADES",
            "Adjacent lots use different beige shades",
            "PASS", shadesDistinct ? "PASS" : "FAIL");

        // Check 9 — lots cover each processable component fully (edge-to-edge)
        bool fullCoverage = CheckLotCoverage(components, allLots);
        AddCheck(checks, "MAP29A_LOTS_COVER_COMPONENTS_FULLY",
            "Lots cover each processable component with no gaps (edge-to-edge)",
            "PASS", fullCoverage ? "PASS" : "FAIL");

        // Red quality checks — only emitted if red components were detected
        if (result.DetectedRedComponentCount > 0)
        {
            bool redAllProcessed = result.UnsupportedRedComponentCount == 0;
            AddCheck(checks, "MAP29B_EVERY_RED_COMPONENT_PROCESSED",
                "Every detected red component is processable",
                "PASS", redAllProcessed ? "PASS" : "FAIL");

            bool redAllHaveStreet = components
                .Where(c => c.ParcelClass == ParcelClassRed && c.Processable)
                .All(c => c.NorthStreetAdjacency || c.SouthStreetAdjacency
                       || c.EastStreetAdjacency  || c.WestStreetAdjacency);
            AddCheck(checks, "MAP29B_ALL_PROCESSED_RED_HAVE_STREET_ADJACENCY",
                "All processed red components have at least one street/access adjacency",
                "PASS", redAllHaveStreet ? "PASS" : "FAIL");
        }

        // MAP29B output check (PENDING, resolved in FinalizeAfterOutputs)
        AddPendingCheck(checks, "MAP29B_OUTPUT_PNG_ZERO_SOURCE_RED",
            "Output PNG contains zero source-red pixels after processing");

        // MAP29B1 undersized-lot checks
        bool noUndersizedBlueMultiLot = !allLots
            .Where(l => l.ComponentId.StartsWith("MAP29A_BLUE_"))
            .GroupBy(l => l.ComponentId)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Any(l => IsUndersized(l, bluePolicy));
        AddCheck(checks, "MAP29B1_NO_UNDERSIZED_BLUE_LOTS",
            "No blue lots are undersized in multi-lot components after merge pass",
            "PASS", noUndersizedBlueMultiLot ? "PASS" : "FAIL");

        if (result.DetectedRedComponentCount > 0)
        {
            bool noUndersizedRedMultiLot = !allLots
                .Where(l => l.ComponentId.StartsWith("MAP29B_RED_"))
                .GroupBy(l => l.ComponentId)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .Any(l => IsUndersized(l, redPolicy));
            AddCheck(checks, "MAP29B1_NO_UNDERSIZED_RED_LOTS",
                "No red lots are undersized in multi-lot components after merge pass",
                "PASS", noUndersizedRedMultiLot ? "PASS" : "FAIL");
        }

        // MAP29C policy proof check
        AddCheck(checks, "MAP29C_LOT_SIZING_POLICY_ACTIVE",
            $"Lot sizing policy active: source={policySource} version={_policyStore.Version} entries={_policyStore.EntryCount}",
            "PASS", "PASS");

        if (result.UndersizedLotMergeCount > 0)
        {
            AddCheck(checks, "MAP29B1_UNDERSIZED_LOTS_MERGED",
                $"Undersized lots merged: blue={totalBlueMergeCount} red={totalRedMergeCount} total={result.UndersizedLotMergeCount}",
                "PASS", "PASS");
        }

        // Checks 10-12: PENDING (require output PNG)
        AddPendingCheck(checks, "MAP29A_EVERY_BLUE_PIXEL_REPLACED",
            "Every original residential-blue pixel is replaced in output PNG");
        AddPendingCheck(checks, "MAP29A_OUTPUT_PNG_ZERO_BLUE",
            "Output PNG contains zero exact residential-blue pixels");
        AddPendingCheck(checks, "MAP29A_OUTPUT_PNG_DIMENSIONS_MATCH",
            "Output PNG dimensions match input PNG (256x256)");

        // Checks 13-15: forbidden artifacts + claim boundary
        result.ForbiddenArtifactScan = ScanOutputRoot(outputRoot);
        bool scanPasses = result.ForbiddenArtifactScan.Contains("PASS", StringComparison.Ordinal)
                       && !result.ForbiddenArtifactScan.Contains("FAIL", StringComparison.Ordinal);
        AddCheck(checks, "MAP29A_FORBIDDEN_ARTIFACTS_ABSENT",
            "No forbidden runtime artifacts in output root",
            "PASS", scanPasses ? "PASS" : "FAIL");

        AddCheck(checks, "MAP29A_SANDBOX_ONLY_TRUE",  "SandboxOnly is true",  "True", result.SandboxOnly.ToString());
        AddCheck(checks, "MAP29A_WRITER_READY_FALSE",  "WriterReady is false", "False", result.WriterReady.ToString());
        AddCheck(checks, "MAP29A_RUNTIME_VALID_FALSE", "RuntimeValid is false","False", result.RuntimeValid.ToString());
        AddCheck(checks, "MAP29A_MATERIALIZED_FALSE",  "Materialized is false","False", result.Materialized.ToString());
        AddCheck(checks, "MAP29A_NO_RUNTIME_PROOF",    "RuntimeProofClaimed is false",            "False", result.RuntimeProofClaimed.ToString());
        AddCheck(checks, "MAP29A_NO_PUBLIC_PLAYABLE",  "PublicPlayablePackagingClaimed is false",  "False", result.PublicPlayablePackagingClaimed.ToString());

        FinalizeResult(result, checks, outputRoot,
            valid: !checks.Any(c => c.CheckStatus == "FAIL") && result.Errors.Count == 0,
            verdict: "MAP29A_WORLDBUILDER_RESIDENTIAL_BLUE_QUADRILATERAL_LOT_FILL_PENDING_FINALIZE");

        return result;
    }

    // Cache for RenderOutputPngBytes (populated during Build)
    private byte[]? _lastSourceBytes;
    private Dictionary<(int x, int y), (byte R, byte G, byte B)>? _lastPixelShadeMap;
    private int _lastTotalSourceRedPixelCount;

    // -----------------------------------------------------------------------
    // FinalizeAfterOutputs
    // -----------------------------------------------------------------------

    public DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult FinalizeAfterOutputs(
        DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult result,
        string outputRoot, string outputPngPath, string htmlPath)
    {
        bool pngOk = false;
        bool zeroBluePng = false;
        bool dimOk = false;

        int sourceRedRemaining = 0;

        if (File.Exists(outputPngPath))
        {
            var outBytes = File.ReadAllBytes(outputPngPath);
            using var outBmp = LoadBitmap(outBytes);

            dimOk  = outBmp.Width == 256 && outBmp.Height == 256;
            zeroBluePng = true;
            pngOk  = true;

            for (int x = 0; x < outBmp.Width; x++)
            for (int y = 0; y < outBmp.Height; y++)
            {
                var px = outBmp.GetPixel(x, y);
                if (IsResidentialBlue(px.R, px.G, px.B)) { zeroBluePng = false; pngOk = false; }
                if (IsOriginalSourceRed(px.R, px.G, px.B)) sourceRedRemaining++;
            }
        }

        result.SourceRedPixelsRemaining = sourceRedRemaining;
        result.SourceRedPixelsReplaced  = _lastTotalSourceRedPixelCount - sourceRedRemaining;

        SetCheck(result, "MAP29B_OUTPUT_PNG_ZERO_SOURCE_RED",
            sourceRedRemaining == 0 ? "PASS" : "FAIL");
        SetCheck(result, "MAP29A_EVERY_BLUE_PIXEL_REPLACED",
            pngOk ? "PASS" : "FAIL");
        SetCheck(result, "MAP29A_OUTPUT_PNG_ZERO_BLUE",
            zeroBluePng ? "PASS" : "FAIL");
        SetCheck(result, "MAP29A_OUTPUT_PNG_DIMENSIONS_MATCH",
            dimOk ? "PASS" : "FAIL");

        // Verify source PNG hash unchanged
        if (File.Exists(result.SourcePng) && !string.IsNullOrEmpty(result.SourcePngSha256))
        {
            string afterHash = ComputeSha256(result.SourcePng);
            bool hashOk = string.Equals(result.SourcePngSha256, afterHash, StringComparison.OrdinalIgnoreCase);
            SetCheck(result, "MAP29A_SOURCE_PNG_HASH_UNCHANGED", hashOk ? "PASS" : "FAIL");
        }

        // Re-scan forbidden artifacts
        result.ForbiddenArtifactScan = ScanOutputRoot(outputRoot);
        bool scanPasses = result.ForbiddenArtifactScan.Contains("PASS", StringComparison.Ordinal)
                       && !result.ForbiddenArtifactScan.Contains("FAIL", StringComparison.Ordinal);
        SetCheck(result, "MAP29A_FORBIDDEN_ARTIFACTS_ABSENT", scanPasses ? "PASS" : "FAIL");

        result.PassedCheckCount = result.Checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = result.Checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass  = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.IsValid = allPass;
        result.Verdict = allPass
            ? "MAP29A_WORLDBUILDER_RESIDENTIAL_BLUE_QUADRILATERAL_LOT_FILL_COMPLETE"
            : "MAP29A_WORLDBUILDER_RESIDENTIAL_BLUE_QUADRILATERAL_LOT_FILL_INVALID";

        return result;
    }

    // -----------------------------------------------------------------------
    // Lot + facade calculation
    // -----------------------------------------------------------------------

    // Selects HORIZONTAL or VERTICAL primary frontage group by best single-side contact ratio.
    // Tie-breakers: total contact count → longer frontage span → deterministic VERTICAL preference.
    private static string SelectFrontageGroup(DetectedBlueComponent comp)
    {
        bool hasH = comp.NorthStreetAdjacency || comp.SouthStreetAdjacency;
        bool hasV = comp.EastStreetAdjacency  || comp.WestStreetAdjacency;

        if (!hasH) return "VERTICAL";
        if (!hasV) return "HORIZONTAL";

        double bestH = 0, bestV = 0;
        int totalH = 0, totalV = 0;

        if (comp.NorthStreetAdjacency) { double r = (double)comp.NorthStreetContactCount / comp.BboxWidth;  if (r > bestH) bestH = r; totalH += comp.NorthStreetContactCount; }
        if (comp.SouthStreetAdjacency) { double r = (double)comp.SouthStreetContactCount / comp.BboxWidth;  if (r > bestH) bestH = r; totalH += comp.SouthStreetContactCount; }
        if (comp.EastStreetAdjacency)  { double r = (double)comp.EastStreetContactCount  / comp.BboxHeight; if (r > bestV) bestV = r; totalV += comp.EastStreetContactCount; }
        if (comp.WestStreetAdjacency)  { double r = (double)comp.WestStreetContactCount  / comp.BboxHeight; if (r > bestV) bestV = r; totalV += comp.WestStreetContactCount; }

        if (bestV > bestH) return "VERTICAL";
        if (bestH > bestV) return "HORIZONTAL";
        if (totalV > totalH) return "VERTICAL";
        if (totalH > totalV) return "HORIZONTAL";
        if (comp.BboxHeight > comp.BboxWidth) return "VERTICAL";
        if (comp.BboxWidth > comp.BboxHeight) return "HORIZONTAL";
        return "VERTICAL"; // deterministic tie-breaker: EAST > WEST > NORTH > SOUTH
    }

    private static (List<QuadrilateralLot> Lots, List<QuadrilateralFacadeEdge> Edges, string OrientCase, string FrontageGroup, List<string> PrimarySides, int MergeCount)
        CalculateLots(DetectedBlueComponent comp, int compOrder, (byte R, byte G, byte B)[] shades, LotSizingPolicy policy)
    {
        bool nAdj = comp.NorthStreetAdjacency;
        bool sAdj = comp.SouthStreetAdjacency;
        bool eAdj = comp.EastStreetAdjacency;
        bool wAdj = comp.WestStreetAdjacency;

        string group = SelectFrontageGroup(comp);

        List<QuadrilateralLot>        lots;
        List<QuadrilateralFacadeEdge> edges;
        string                        orientCase;
        List<string>                  primarySides;

        if (group == "HORIZONTAL")
        {
            if (nAdj && sAdj) { orientCase = "A_NS"; primarySides = new() { "NORTH", "SOUTH" }; }
            else if (nAdj)    { orientCase = "B_N";  primarySides = new() { "NORTH" }; }
            else              { orientCase = "C_S";  primarySides = new() { "SOUTH" }; }

            (lots, edges) = BuildNSCase(comp, compOrder, nAdj, sAdj,
                cornerFromEast: eAdj, cornerFromWest: wAdj, shades, policy.TargetFrontageTiles);
        }
        else
        {
            if (eAdj && wAdj) { orientCase = "D_EW"; primarySides = new() { "EAST", "WEST" }; }
            else if (eAdj)    { orientCase = "E_E";  primarySides = new() { "EAST" }; }
            else              { orientCase = "F_W";  primarySides = new() { "WEST" }; }

            (lots, edges) = BuildEWCase(comp, compOrder, eAdj, wAdj,
                cornerFromNorth: nAdj, cornerFromSouth: sAdj, shades, policy.TargetFrontageTiles);
        }

        int mergeCount = 0;
        if (policy.MergeUndersizedLots && lots.Count > 1)
            (lots, edges, mergeCount) = MergeUndersizedLots(lots, edges, comp, compOrder, shades, policy);

        return (lots, edges, orientCase, group, primarySides, mergeCount);
    }

    private static (List<QuadrilateralLot> Lots, List<QuadrilateralFacadeEdge> Edges)
        BuildNSCase(DetectedBlueComponent comp, int compOrder, bool nAdj, bool sAdj,
            bool cornerFromEast, bool cornerFromWest, (byte R, byte G, byte B)[] shades,
            int targetFrontageTiles)
    {
        var lots  = new List<QuadrilateralLot>();
        var edges = new List<QuadrilateralFacadeEdge>();

        int frontageSpan = comp.BboxX2 - comp.BboxX1 + 1;
        int lotCount     = CalculateLotCount(frontageSpan, targetFrontageTiles);
        var columns      = SplitInclusiveSpan(comp.BboxX1, comp.BboxX2, lotCount);

        IReadOnlyList<(int S1, int S2, int Span)> rows;
        string[] rowDirs;
        int[]    rowOffsets;

        if (nAdj && sAdj)
        {
            rows       = SplitInclusiveSpan(comp.BboxY1, comp.BboxY2, 2);
            rowDirs    = new[] { "NORTH", "SOUTH" };
            rowOffsets = new[] { 0, 1 };
        }
        else if (nAdj)
        {
            rows       = new[] { (comp.BboxY1, comp.BboxY2, comp.BboxHeight) };
            rowDirs    = new[] { "NORTH" };
            rowOffsets = new[] { 0 };
        }
        else
        {
            rows       = new[] { (comp.BboxY1, comp.BboxY2, comp.BboxHeight) };
            rowDirs    = new[] { "SOUTH" };
            rowOffsets = new[] { 0 };
        }

        for (int ri = 0; ri < rows.Count; ri++)
        {
            var (ry1, ry2, _) = rows[ri];
            string dir        = rowDirs[ri];
            int rowOffset     = rowOffsets[ri];

            for (int ci = 0; ci < columns.Count; ci++)
            {
                var (cx1, cx2, cw) = columns[ci];
                bool isCorner = (cornerFromWest && cx1 == comp.BboxX1) ||
                                (cornerFromEast && cx2 == comp.BboxX2);
                var  shade    = GetShadeFromPalette(shades, compOrder, ci, rowOffset);

                string lotId  = $"{comp.ComponentId}_{dir}_LOT_{ci:00}";
                string edgeId = $"{lotId}_FACADE_EDGE";

                int facadeY = dir == "NORTH" ? ry1 : ry2;
                int lotH    = ry2 - ry1 + 1;

                lots.Add(new QuadrilateralLot
                {
                    ComponentId         = comp.ComponentId,
                    LotId               = lotId,
                    FrontageDirection   = dir,
                    X1 = cx1, Y1 = ry1, X2 = cx2, Y2 = ry2,
                    Width               = cw,
                    Height              = lotH,
                    TileCount           = cw * lotH,
                    ShadeR              = shade.R, ShadeG = shade.G, ShadeB = shade.B,
                    ShadeRgb            = $"{shade.R},{shade.G},{shade.B}",
                    IsCornerLot         = isCorner,
                    PrimaryFacadeEdgeId = edgeId,
                });

                int contactCount = dir == "NORTH" ? comp.NorthStreetContactCount : comp.SouthStreetContactCount;
                edges.Add(new QuadrilateralFacadeEdge
                {
                    ComponentId        = comp.ComponentId,
                    LotId              = lotId,
                    FacadeEdgeId       = edgeId,
                    FrontageDirection  = dir,
                    X1 = cx1, Y1 = facadeY, X2 = cx2, Y2 = facadeY,
                    LengthTiles        = cw,
                    StreetContactCount = contactCount,
                    StreetContactRatio = Math.Round((double)contactCount / frontageSpan, 4),
                });
            }
        }

        return (lots, edges);
    }

    private static (List<QuadrilateralLot> Lots, List<QuadrilateralFacadeEdge> Edges)
        BuildEWCase(DetectedBlueComponent comp, int compOrder, bool eAdj, bool wAdj,
            bool cornerFromNorth, bool cornerFromSouth, (byte R, byte G, byte B)[] shades,
            int targetFrontageTiles)
    {
        var lots  = new List<QuadrilateralLot>();
        var edges = new List<QuadrilateralFacadeEdge>();

        int frontageSpan = comp.BboxY2 - comp.BboxY1 + 1;
        int lotCount     = CalculateLotCount(frontageSpan, targetFrontageTiles);
        var rows         = SplitInclusiveSpan(comp.BboxY1, comp.BboxY2, lotCount);

        IReadOnlyList<(int S1, int S2, int Span)> cols;
        string[] colDirs;
        int[]    colOffsets;

        if (eAdj && wAdj)
        {
            cols       = SplitInclusiveSpan(comp.BboxX1, comp.BboxX2, 2);
            colDirs    = new[] { "WEST", "EAST" };
            colOffsets = new[] { 0, 1 };
        }
        else if (eAdj)
        {
            cols       = new[] { (comp.BboxX1, comp.BboxX2, comp.BboxWidth) };
            colDirs    = new[] { "EAST" };
            colOffsets = new[] { 0 };
        }
        else
        {
            cols       = new[] { (comp.BboxX1, comp.BboxX2, comp.BboxWidth) };
            colDirs    = new[] { "WEST" };
            colOffsets = new[] { 0 };
        }

        for (int ci = 0; ci < cols.Count; ci++)
        {
            var (cx1, cx2, _) = cols[ci];
            string dir        = colDirs[ci];
            int colOffset     = colOffsets[ci];

            for (int ri = 0; ri < rows.Count; ri++)
            {
                var (ry1, ry2, rh) = rows[ri];
                bool isCorner = (cornerFromNorth && ry1 == comp.BboxY1) ||
                                (cornerFromSouth && ry2 == comp.BboxY2);
                var  shade    = GetShadeFromPalette(shades, compOrder, ri, colOffset);

                string lotId  = $"{comp.ComponentId}_{dir}_LOT_{ri:00}";
                string edgeId = $"{lotId}_FACADE_EDGE";

                int facadeX = dir == "WEST" ? cx1 : cx2;
                int lotW    = cx2 - cx1 + 1;

                lots.Add(new QuadrilateralLot
                {
                    ComponentId         = comp.ComponentId,
                    LotId               = lotId,
                    FrontageDirection   = dir,
                    X1 = cx1, Y1 = ry1, X2 = cx2, Y2 = ry2,
                    Width               = lotW,
                    Height              = rh,
                    TileCount           = lotW * rh,
                    ShadeR              = shade.R, ShadeG = shade.G, ShadeB = shade.B,
                    ShadeRgb            = $"{shade.R},{shade.G},{shade.B}",
                    IsCornerLot         = isCorner,
                    PrimaryFacadeEdgeId = edgeId,
                });

                int contactCount = dir == "EAST" ? comp.EastStreetContactCount : comp.WestStreetContactCount;
                edges.Add(new QuadrilateralFacadeEdge
                {
                    ComponentId        = comp.ComponentId,
                    LotId              = lotId,
                    FacadeEdgeId       = edgeId,
                    FrontageDirection  = dir,
                    X1 = facadeX, Y1 = ry1, X2 = facadeX, Y2 = ry2,
                    LengthTiles        = rh,
                    StreetContactCount = contactCount,
                    StreetContactRatio = Math.Round((double)contactCount / frontageSpan, 4),
                });
            }
        }

        return (lots, edges);
    }

    // -----------------------------------------------------------------------
    // Validation helpers
    // -----------------------------------------------------------------------

    private static bool CheckAdjacentShadesDistinct(
        List<DetectedBlueComponent> comps, List<QuadrilateralLot> lots)
    {
        foreach (var comp in comps.Where(c => c.Processable))
        {
            var compLots = lots.Where(l => l.ComponentId == comp.ComponentId).ToList();
            // Check horizontal adjacency (same Y1/Y2, X2+1 == next X1)
            var byRow = compLots.GroupBy(l => (l.Y1, l.Y2));
            foreach (var row in byRow)
            {
                var ordered = row.OrderBy(l => l.X1).ToList();
                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    if (ordered[i].X2 + 1 != ordered[i+1].X1) continue;
                    if (ordered[i].ShadeR == ordered[i+1].ShadeR &&
                        ordered[i].ShadeG == ordered[i+1].ShadeG &&
                        ordered[i].ShadeB == ordered[i+1].ShadeB) return false;
                }
            }
            // Check vertical adjacency (same X1/X2, Y2+1 == next Y1)
            var byCol = compLots.GroupBy(l => (l.X1, l.X2));
            foreach (var col in byCol)
            {
                var ordered = col.OrderBy(l => l.Y1).ToList();
                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    if (ordered[i].Y2 + 1 != ordered[i+1].Y1) continue;
                    if (ordered[i].ShadeR == ordered[i+1].ShadeR &&
                        ordered[i].ShadeG == ordered[i+1].ShadeG &&
                        ordered[i].ShadeB == ordered[i+1].ShadeB) return false;
                }
            }
        }
        return true;
    }

    private static bool CheckLotCoverage(
        List<DetectedBlueComponent> comps, List<QuadrilateralLot> lots)
    {
        foreach (var comp in comps.Where(c => c.Processable))
        {
            var compLots = lots.Where(l => l.ComponentId == comp.ComponentId).ToList();
            if (compLots.Count == 0) return false;

            // Verify all lots are within component bbox
            if (compLots.Any(l => l.X1 < comp.BboxX1 || l.X2 > comp.BboxX2 ||
                                   l.Y1 < comp.BboxY1 || l.Y2 > comp.BboxY2)) return false;

            // Verify rows cover X span fully
            var byRow = compLots.GroupBy(l => (l.Y1, l.Y2));
            foreach (var row in byRow)
            {
                var ordered = row.OrderBy(l => l.X1).ToList();
                if (ordered.First().X1 != comp.BboxX1) return false;
                if (ordered.Last().X2 != comp.BboxX2)  return false;
                for (int i = 0; i < ordered.Count - 1; i++)
                    if (ordered[i].X2 + 1 != ordered[i+1].X1) return false;
            }

            // Verify rows or cols cover Y span fully
            var byCol = compLots.GroupBy(l => (l.X1, l.X2));
            foreach (var col in byCol)
            {
                var ordered = col.OrderBy(l => l.Y1).ToList();
                if (ordered.First().Y1 != comp.BboxY1) return false;
                if (ordered.Last().Y2  != comp.BboxY2)  return false;
                for (int i = 0; i < ordered.Count - 1; i++)
                    if (ordered[i].Y2 + 1 != ordered[i+1].Y1) return false;
            }
        }
        return true;
    }

    // -----------------------------------------------------------------------
    // Output PNG rendering
    // -----------------------------------------------------------------------

    public byte[] RenderOutputPngBytes(
        DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult result)
    {
        byte[] sourceBytes = _lastSourceBytes
            ?? (File.Exists(result.SourcePng) ? File.ReadAllBytes(result.SourcePng) : null)
            ?? throw new InvalidOperationException("Source PNG bytes not available");

        var shadeMap = _lastPixelShadeMap
            ?? BuildShadeMapFromResult(result);

        using var bmp = LoadBitmap(sourceBytes);

        foreach (var ((x, y), (r, g, b)) in shadeMap)
            bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(r, g, b));

        using var ms = new System.IO.MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }

    private static Dictionary<(int x, int y), (byte R, byte G, byte B)>
        BuildShadeMapFromResult(DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult result)
    {
        var map = new Dictionary<(int, int), (byte, byte, byte)>();
        if (!File.Exists(result.SourcePng)) return map;

        var classMap = result.Components.ToDictionary(c => c.ComponentId, c => c.ParcelClass);

        var sourceBytes = File.ReadAllBytes(result.SourcePng);
        using var bmp = LoadBitmap(sourceBytes);

        foreach (var lot in result.Lots)
        {
            byte r = (byte)lot.ShadeR, g = (byte)lot.ShadeG, b = (byte)lot.ShadeB;
            bool isRed = classMap.TryGetValue(lot.ComponentId, out var cls)
                      && cls == ParcelClassRed;
            for (int x = lot.X1; x <= lot.X2; x++)
            for (int y = lot.Y1; y <= lot.Y2; y++)
            {
                var px = bmp.GetPixel(x, y);
                bool isTarget = isRed
                    ? IsSourceRed(px.R, px.G, px.B)
                    : IsResidentialBlue(px.R, px.G, px.B);
                if (isTarget)
                    map[(x, y)] = (r, g, b);
            }
        }
        return map;
    }

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult r) =>
        JsonSerializer.Serialize(r, s_json);

    public string RenderLotsCsv(DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("component_id,lot_id,frontage_direction,x1,y1,x2,y2,width,height,tile_count,shade_rgb,is_corner_lot,primary_facade_edge_id");
        foreach (var l in r.Lots)
            sb.AppendLine($"{l.ComponentId},{l.LotId},{l.FrontageDirection},{l.X1},{l.Y1},{l.X2},{l.Y2},{l.Width},{l.Height},{l.TileCount},{l.ShadeRgb},{l.IsCornerLot},{l.PrimaryFacadeEdgeId}");
        return sb.ToString();
    }

    public string RenderFacadesCsv(DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("component_id,lot_id,facade_edge_id,frontage_direction,x1,y1,x2,y2,length_tiles,street_contact_count,street_contact_ratio");
        foreach (var e in r.FacadeEdges)
            sb.AppendLine($"{e.ComponentId},{e.LotId},{e.FacadeEdgeId},{e.FrontageDirection},{e.X1},{e.Y1},{e.X2},{e.Y2},{e.LengthTiles},{e.StreetContactCount},{e.StreetContactRatio}");
        return sb.ToString();
    }

    public string RenderChecksCsv(DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in r.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-29A/MAP-29B WORLDBUILDER RESIDENTIAL QUADRILATERAL LOT FILL");
        sb.AppendLine($"Generated UTC           : {r.GeneratedUtc}");
        sb.AppendLine($"Source PNG              : {r.SourcePng}");
        sb.AppendLine($"Source SHA256           : {r.SourcePngSha256[..16]}...");
        sb.AppendLine($"Map ID                  : {r.MapId}");
        sb.AppendLine($"Blue components         : {r.DetectedBlueComponentCount} detected / {r.ProcessedQuadrilateralComponentCount} processed / {r.UnsupportedBlueComponentCount} unsupported");
        sb.AppendLine($"Blue lots               : {r.BlueLotCount}");
        sb.AppendLine($"Blue facade edges       : {r.BlueFacadeEdgeCount}");
        sb.AppendLine($"Red components          : {r.DetectedRedComponentCount} detected / {r.ProcessedRedComponentCount} processed / {r.UnsupportedRedComponentCount} unsupported");
        sb.AppendLine($"Red lots                : {r.RedLotCount}");
        sb.AppendLine($"Red facade edges        : {r.RedFacadeEdgeCount}");
        sb.AppendLine($"Source red replaced     : {r.SourceRedPixelsReplaced} / remaining {r.SourceRedPixelsRemaining}");
        sb.AppendLine($"Total lots              : {r.TotalLotCount}");
        sb.AppendLine($"Total facade edges      : {r.TotalFacadeEdgeCount}");
        sb.AppendLine($"Lot sizing policy       : {r.LotSizingPolicyVersion} source={r.LotSizingPolicySource} entries={r.LotSizingPolicyEntryCount}");
        sb.AppendLine($"Merges (MAP29B1)        : total={r.UndersizedLotMergeCount} blue={r.BlueUndersizedLotMergeCount} red={r.RedUndersizedLotMergeCount}");
        sb.AppendLine($"Checks                  : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                : {r.IsValid}");
        sb.AppendLine($"Verdict                 : {r.Verdict}");
        sb.AppendLine($"Forbidden Scan          : {r.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim boundary          : sandbox_only=true | writer_ready=false | runtime_valid=false | materialized=false");
        if (r.Errors.Count > 0) { sb.AppendLine("Errors:"); foreach (var e in r.Errors) sb.AppendLine($"  {e}"); }
        return sb.ToString();
    }

    public string RenderHtml(DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\">");
        sb.AppendLine("<title>DeadMTL MAP-29A Residential Blue Quadrilateral Lot Fill</title>");
        sb.AppendLine("<style>body{background:#0e0e14;color:#ccc;font-family:monospace;padding:16px;}");
        sb.AppendLine("h1{font-size:1em;color:#a89060;}h2{font-size:0.9em;color:#888;margin-top:20px;}");
        sb.AppendLine("p{font-size:0.8em;line-height:1.5;}.warn{color:#c87040;}");
        sb.AppendLine(".card{background:#1a1a22;border:1px solid #333;padding:8px;display:inline-block;}");
        sb.AppendLine(".card img{display:block;width:512px;height:512px;image-rendering:pixelated;image-rendering:crisp-edges;}");
        sb.AppendLine(".card .lbl{font-size:0.7em;color:#666;margin-top:4px;}</style></head><body>");
        sb.AppendLine("<h1>DeadMTL MAP-29A Residential Blue Quadrilateral Lot Fill</h1>");
        sb.AppendLine("<p class=\"warn\">NOT a playable Project Zomboid export. NOT .lotpack/.lotheader/.lua/.bin.</p>");
        sb.AppendLine($"<p>Source: {r.SourcePng}<br>");
        sb.AppendLine($"Blue components: {r.DetectedBlueComponentCount} detected / {r.ProcessedQuadrilateralComponentCount} processed / {r.UnsupportedBlueComponentCount} unsupported<br>");
        sb.AppendLine($"Blue lots: {r.BlueLotCount} | Blue facade edges: {r.BlueFacadeEdgeCount}<br>");
        sb.AppendLine($"Red components: {r.DetectedRedComponentCount} detected / {r.ProcessedRedComponentCount} processed / {r.UnsupportedRedComponentCount} unsupported<br>");
        sb.AppendLine($"Red lots: {r.RedLotCount} | Red facade edges: {r.RedFacadeEdgeCount} | Source red replaced: {r.SourceRedPixelsReplaced} / remaining: {r.SourceRedPixelsRemaining}<br>");
        sb.AppendLine($"Total lots: {r.TotalLotCount} | Total facade edges: {r.TotalFacadeEdgeCount}</p>");
        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("  <img src=\"map_00_residential_blue_lot_fill_output_native_256.png\" alt=\"lot fill output\">");
        sb.AppendLine("  <div class=\"lbl\">output: residential beige lot fill replacing all blue shapes (256x256)</div>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<h2>Checks: {r.CheckCount} / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL</h2>");
        sb.AppendLine($"<p>Verdict: {r.Verdict}</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static void AddCheck(List<QuadrilateralLotFillCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new QuadrilateralLotFillCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void AddPendingCheck(List<QuadrilateralLotFillCheck> checks, string id, string label)
    {
        checks.Add(new QuadrilateralLotFillCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PENDING",
            Expected    = "PASS",
            Actual      = "PENDING",
        });
    }

    private static void SetCheck(
        DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult result,
        string checkId, string status)
    {
        var c = result.Checks.FirstOrDefault(x => x.CheckId == checkId);
        if (c is not null) { c.CheckStatus = status; c.Actual = status; }
    }

    private static void FinalizeResult(
        DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult result,
        List<QuadrilateralLotFillCheck> checks,
        string outputRoot, bool valid, string verdict)
    {
        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = valid && result.FailedCheckCount == 0;
        result.Verdict          = result.IsValid ? verdict : "MAP29A_WORLDBUILDER_RESIDENTIAL_BLUE_QUADRILATERAL_LOT_FILL_INVALID";
        result.ForbiddenArtifactScan = ScanOutputRoot(outputRoot);
    }

    private static string ComputeSha256(string path)
    {
        using var fs   = File.OpenRead(path);
        using var sha  = SHA256.Create();
        var hash = sha.ComputeHash(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ScanOutputRoot(string outputRoot)
    {
        const string prefix = "POST_MAP29A_FORBIDDEN_SCAN";
        if (!Directory.Exists(outputRoot))
            return $"{prefix} PASS (0 forbidden artifacts in output root)";
        var patterns = new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" };
        int count = patterns.Sum(p => Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length);
        var allDirs = Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories);
        if (allDirs.Any(d => { var di = new DirectoryInfo(d); return di.Name == "maps" && di.Parent?.Name == "media"; })) count++;
        if (allDirs.Any(d => string.Equals(new DirectoryInfo(d).Name, "steamapps", StringComparison.OrdinalIgnoreCase))) count++;
        return count == 0
            ? $"{prefix} PASS (0 forbidden artifacts in output root)"
            : $"{prefix} FAIL ({count} forbidden artifacts found)";
    }

    private static System.Drawing.Bitmap LoadBitmap(byte[] pngBytes)
    {
        using var ms  = new System.IO.MemoryStream(pngBytes);
        using var tmp = new System.Drawing.Bitmap(ms);
        return tmp.Clone(
            new System.Drawing.Rectangle(0, 0, tmp.Width, tmp.Height),
            tmp.PixelFormat);
    }
}
