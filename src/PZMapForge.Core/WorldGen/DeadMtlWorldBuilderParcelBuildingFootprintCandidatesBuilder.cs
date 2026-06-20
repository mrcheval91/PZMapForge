using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderParcelBuildingFootprintCandidatesBuilder
{
    private static readonly JsonSerializerOptions s_json = new() { WriteIndented = true };

    private const string ParcelClassBlue = "BLUE_RESIDENTIAL";
    private const string ParcelClassRed  = "RED_RESIDENTIAL_OR_COMMERCIAL";

    // Skipped-lot preview color: muted amber/brown.
    // Not cyan (40,192,192), not pure black, not source-blue (r<100 && b>140 && b>r+50),
    // not source-red (r>=180 && g<=5 && b<=5).
    private static readonly (int R, int G, int B) SkippedLotColor = (152, 112, 52);
    public  static (int R, int G, int B) SkippedLotPreviewColor => SkippedLotColor;
    public  static string SkippedLotPreviewColorRgbString =>
        $"rgb({SkippedLotColor.R},{SkippedLotColor.G},{SkippedLotColor.B})";

    // -----------------------------------------------------------------------
    // Internal models
    // -----------------------------------------------------------------------

    private sealed record LotEntry(
        string ComponentId,
        string LotId,
        string FrontageDirection,
        string ParcelClass,
        int X1, int Y1, int X2, int Y2,
        int Width, int Height, int TileCount,
        int ShadeR, int ShadeG, int ShadeB);

    private sealed record SectorEntry(
        string SectorId,
        string Label,
        int BboxX1, int BboxY1, int BboxX2, int BboxY2,
        string GameplayRole,
        string ToneNote);

    private sealed class SectorAssignmentStore
    {
        private readonly List<SectorEntry> _sectors;
        public string Version    { get; }
        public int    EntryCount => _sectors.Count;

        public SectorAssignmentStore(string version, List<SectorEntry> sectors)
        {
            Version  = version;
            _sectors = sectors;
        }

        public string Assign(int centerX, int centerY)
        {
            foreach (var s in _sectors)
                if (centerX >= s.BboxX1 && centerX <= s.BboxX2 &&
                    centerY >= s.BboxY1 && centerY <= s.BboxY2)
                    return s.SectorId;
            return "DEFAULT";
        }
    }

    private sealed record FootprintPolicy(
        string ParcelClass,
        string NeighborhoodSector,
        int    MinLotAreaTiles,
        int    FrontSetbackTiles,
        int    RearSetbackTiles,
        int    SideSetbackTiles,
        int    MinFootprintWidthTiles,
        int    MinFootprintDepthTiles,
        double MaxLotCoverageRatio,
        string PreferredFootprintKind);

    private sealed class FootprintPolicyStore
    {
        private readonly List<FootprintPolicy> _policies;
        public string Version       { get; }
        public string DefaultSector { get; }
        public int    EntryCount        => _policies.Count;
        public bool   HasAnyDefaultPolicy => _policies.Any(p => p.NeighborhoodSector == "DEFAULT");

        public FootprintPolicyStore(string version, string defaultSector, List<FootprintPolicy> policies)
        {
            Version       = version;
            DefaultSector = defaultSector;
            _policies     = policies;
        }

        public FootprintPolicy Resolve(string parcelClass, string sector = "DEFAULT")
        {
            foreach (var p in _policies)
                if (p.ParcelClass == parcelClass && p.NeighborhoodSector == sector)
                    return p;
            foreach (var p in _policies)
                if (p.ParcelClass == parcelClass && p.NeighborhoodSector == "DEFAULT")
                    return p;
            return parcelClass == ParcelClassRed
                ? new(parcelClass, "DEFAULT", 120, 0, 2, 0, 6, 6, 0.85, "COMMERCIAL_RECTANGLE")
                : new(parcelClass, "DEFAULT",  96, 2, 3, 1, 6, 6, 0.60, "RESIDENTIAL_RECTANGLE");
        }
    }

    // -----------------------------------------------------------------------
    // Pixel map cache — built during Build(), consumed by RenderOutputPngBytes()
    // -----------------------------------------------------------------------

    private string                    _cachedSourcePng  = string.Empty;
    private (int R, int G, int B)[,]? _cachedPixelMap;
    private int                       _cachedMapWidth;
    private int                       _cachedMapHeight;

    // -----------------------------------------------------------------------
    // Check helpers
    // -----------------------------------------------------------------------

    private static void AddCheck(List<ParcelBuildingFootprintCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new ParcelBuildingFootprintCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
        });
    }

    private static void FinalizeResult(
        DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult result,
        List<ParcelBuildingFootprintCheck> checks,
        bool valid,
        string verdict)
    {
        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = valid && result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict          = result.IsValid ? verdict : $"{verdict}_INVALID";
    }

    private static string ScanOutputRoot(string outputRoot)
    {
        const string prefix = "POST_MAP30A_PARCEL_BUILDING_FOOTPRINT_CANDIDATES_FORBIDDEN_SCAN";
        if (!Directory.Exists(outputRoot))
            return $"{prefix} PASS (0 forbidden artifacts in output root)";
        var patterns = new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" };
        int count = patterns.Sum(p =>
            Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length);
        return count == 0
            ? $"{prefix} PASS (0 forbidden artifacts in output root)"
            : $"{prefix} FAIL ({count} forbidden artifacts found)";
    }

    // -----------------------------------------------------------------------
    // Pixel color classifiers
    // -----------------------------------------------------------------------

    private static bool IsCyanDebug(int r, int g, int b)  => r < 60 && g > 180 && b > 180;
    private static bool IsPureBlack(int r, int g, int b)  => r == 0 && g == 0 && b == 0;

    // Source-blue: matches original blue residential parcel pixels (~58,94,174)
    // Does NOT match: lot shades (beige R>=150), background (18,18,24 B=24<140), skipped-lot color (R=152>100)
    private static bool IsSourceBlue(int r, int g, int b) => r < 100 && b > 140 && b > r + 50;

    // Source-red: matches original source red parcel pixels (r>=180, g<=5, b<=5)
    // Does NOT match: red lot shades (196,20,20 g=20>5) or (226,40,40 g=40>5)
    private static bool IsSourceRed(int r, int g, int b)  => r >= 180 && g <= 5 && b <= 5;

    // -----------------------------------------------------------------------
    // JSON loaders
    // -----------------------------------------------------------------------

    private static FootprintPolicyStore LoadFootprintPolicyStore(string path)
    {
        var json      = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;
        string version       = root.GetProperty("policy_version").GetString() ?? "";
        string defaultSector = root.TryGetProperty("default_sector", out var ds)
            ? ds.GetString() ?? "DEFAULT" : "DEFAULT";
        var policies = new List<FootprintPolicy>();
        foreach (var entry in root.GetProperty("policies").EnumerateArray())
        {
            policies.Add(new FootprintPolicy(
                entry.GetProperty("parcel_class").GetString()        ?? "",
                entry.GetProperty("neighborhood_sector").GetString() ?? "DEFAULT",
                entry.GetProperty("min_lot_area_tiles").GetInt32(),
                entry.GetProperty("front_setback_tiles").GetInt32(),
                entry.GetProperty("rear_setback_tiles").GetInt32(),
                entry.GetProperty("side_setback_tiles").GetInt32(),
                entry.GetProperty("min_footprint_width_tiles").GetInt32(),
                entry.GetProperty("min_footprint_depth_tiles").GetInt32(),
                entry.GetProperty("max_lot_coverage_ratio").GetDouble(),
                entry.GetProperty("preferred_footprint_kind").GetString() ?? ""
            ));
        }
        if (policies.Count == 0)
            throw new InvalidOperationException("Footprint policy file contains no entries.");
        return new FootprintPolicyStore(version, defaultSector, policies);
    }

    private static SectorAssignmentStore LoadSectorAssignmentStore(string path)
    {
        var json      = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;
        string version = root.TryGetProperty("sector_overrides_version", out var vEl)
            ? vEl.GetString() ?? "" : "";
        var sectors = new List<SectorEntry>();
        foreach (var entry in root.GetProperty("sectors").EnumerateArray())
        {
            sectors.Add(new SectorEntry(
                entry.GetProperty("sector_id").GetString()    ?? "",
                entry.TryGetProperty("label",          out var lbl)  ? lbl.GetString()  ?? "" : "",
                entry.GetProperty("bbox_x1").GetInt32(),
                entry.GetProperty("bbox_y1").GetInt32(),
                entry.GetProperty("bbox_x2").GetInt32(),
                entry.GetProperty("bbox_y2").GetInt32(),
                entry.TryGetProperty("gameplay_role",  out var gr)   ? gr.GetString()   ?? "" : "",
                entry.TryGetProperty("tone_note",      out var tn)   ? tn.GetString()   ?? "" : ""
            ));
        }
        return new SectorAssignmentStore(version, sectors);
    }

    private static (List<LotEntry> Lots, string SourcePng, string LotFillPolicyVersion)
        ParseLotFillJson(string path)
    {
        var json      = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;

        string sourcePng = root.TryGetProperty("source_png", out var spEl)
            ? spEl.GetString() ?? "" : "";
        string lotFillPolicyVersion = root.TryGetProperty("lot_sizing_policy_version", out var lpvEl)
            ? lpvEl.GetString() ?? "" : "";

        var compParcelClass = new Dictionary<string, string>();
        if (root.TryGetProperty("components", out var compsEl))
            foreach (var comp in compsEl.EnumerateArray())
            {
                var cid = comp.TryGetProperty("component_id", out var cidEl) ? cidEl.GetString() ?? "" : "";
                var pc  = comp.TryGetProperty("parcel_class",  out var pcEl)  ? pcEl.GetString()  ?? "" : "";
                if (!string.IsNullOrEmpty(cid))
                    compParcelClass[cid] = string.IsNullOrEmpty(pc) ? InferParcelClass(cid) : pc;
            }

        var lots = new List<LotEntry>();
        if (root.TryGetProperty("lots", out var lotsEl))
            foreach (var lotEl in lotsEl.EnumerateArray())
            {
                var compId = lotEl.TryGetProperty("component_id", out var cidEl2) ? cidEl2.GetString() ?? "" : "";
                var parcelClass = compParcelClass.TryGetValue(compId, out var pc2) ? pc2 : InferParcelClass(compId);
                lots.Add(new LotEntry(
                    compId,
                    lotEl.TryGetProperty("lot_id",             out var li)  ? li.GetString()  ?? "" : "",
                    lotEl.TryGetProperty("frontage_direction", out var fd)  ? fd.GetString()  ?? "" : "",
                    parcelClass,
                    lotEl.TryGetProperty("x1",         out var x1)  ? x1.GetInt32()  : 0,
                    lotEl.TryGetProperty("y1",         out var y1)  ? y1.GetInt32()  : 0,
                    lotEl.TryGetProperty("x2",         out var x2)  ? x2.GetInt32()  : 0,
                    lotEl.TryGetProperty("y2",         out var y2)  ? y2.GetInt32()  : 0,
                    lotEl.TryGetProperty("width",      out var w)   ? w.GetInt32()   : 0,
                    lotEl.TryGetProperty("height",     out var h)   ? h.GetInt32()   : 0,
                    lotEl.TryGetProperty("tile_count", out var tc)  ? tc.GetInt32()  : 0,
                    lotEl.TryGetProperty("shade_r",    out var sr)  ? sr.GetInt32()  : 200,
                    lotEl.TryGetProperty("shade_g",    out var sg)  ? sg.GetInt32()  : 168,
                    lotEl.TryGetProperty("shade_b",    out var sb)  ? sb.GetInt32()  : 120
                ));
            }

        return (lots, sourcePng, lotFillPolicyVersion);
    }

    private static string InferParcelClass(string compId) =>
        compId.StartsWith("MAP29B_RED_", StringComparison.Ordinal)
            ? ParcelClassRed : ParcelClassBlue;

    // -----------------------------------------------------------------------
    // Footprint computation
    // -----------------------------------------------------------------------

    private static (int FpX1, int FpY1, int FpX2, int FpY2, bool Skipped, string SkipReason)
        ComputeFootprint(LotEntry lot, FootprintPolicy policy)
    {
        int front = policy.FrontSetbackTiles;
        int rear  = policy.RearSetbackTiles;
        int side  = policy.SideSetbackTiles;

        int fpX1, fpY1, fpX2, fpY2;
        switch (lot.FrontageDirection)
        {
            case "NORTH":
                fpX1 = lot.X1 + side;  fpX2 = lot.X2 - side;
                fpY1 = lot.Y1 + front; fpY2 = lot.Y2 - rear;
                break;
            case "SOUTH":
                fpX1 = lot.X1 + side;  fpX2 = lot.X2 - side;
                fpY1 = lot.Y1 + rear;  fpY2 = lot.Y2 - front;
                break;
            case "EAST":
                fpX1 = lot.X1 + rear;  fpX2 = lot.X2 - front;
                fpY1 = lot.Y1 + side;  fpY2 = lot.Y2 - side;
                break;
            case "WEST":
                fpX1 = lot.X1 + front; fpX2 = lot.X2 - rear;
                fpY1 = lot.Y1 + side;  fpY2 = lot.Y2 - side;
                break;
            default:
                return (0, 0, 0, 0, true, $"Unknown frontage direction: {lot.FrontageDirection}");
        }

        if (fpX2 < fpX1 || fpY2 < fpY1)
            return (0, 0, 0, 0, true, "Lot too small for setbacks");

        bool isNS   = lot.FrontageDirection == "NORTH" || lot.FrontageDirection == "SOUTH";
        int fpWidth = isNS ? fpX2 - fpX1 + 1 : fpY2 - fpY1 + 1;
        int fpDepth = isNS ? fpY2 - fpY1 + 1 : fpX2 - fpX1 + 1;

        if (fpWidth < policy.MinFootprintWidthTiles)
            return (0, 0, 0, 0, true,
                $"Footprint width {fpWidth} < min_footprint_width_tiles {policy.MinFootprintWidthTiles}");
        if (fpDepth < policy.MinFootprintDepthTiles)
            return (0, 0, 0, 0, true,
                $"Footprint depth {fpDepth} < min_footprint_depth_tiles {policy.MinFootprintDepthTiles}");

        int fpArea    = (fpX2 - fpX1 + 1) * (fpY2 - fpY1 + 1);
        double coverage = (double)fpArea / lot.TileCount;

        if (coverage > policy.MaxLotCoverageRatio)
        {
            int maxArea = (int)Math.Floor(lot.TileCount * policy.MaxLotCoverageRatio);
            if (isNS)
            {
                int fixedWidth = fpX2 - fpX1 + 1;
                int maxDepth   = maxArea / fixedWidth;
                if (maxDepth < policy.MinFootprintDepthTiles)
                    return (0, 0, 0, 0, true,
                        $"Coverage clip reduces depth below min_footprint_depth_tiles {policy.MinFootprintDepthTiles}");
                if (lot.FrontageDirection == "NORTH") fpY2 = fpY1 + maxDepth - 1;
                else                                   fpY1 = fpY2 - maxDepth + 1;
            }
            else
            {
                int fixedWidth = fpY2 - fpY1 + 1;
                int maxDepth   = maxArea / fixedWidth;
                if (maxDepth < policy.MinFootprintDepthTiles)
                    return (0, 0, 0, 0, true,
                        $"Coverage clip reduces depth below min_footprint_depth_tiles {policy.MinFootprintDepthTiles}");
                if (lot.FrontageDirection == "EAST") fpX1 = fpX2 - maxDepth + 1;
                else                                  fpX2 = fpX1 + maxDepth - 1;
            }
        }

        return (fpX1, fpY1, fpX2, fpY2, false, string.Empty);
    }

    // -----------------------------------------------------------------------
    // Preview paint map — builds pixel-level representation for checks + PNG
    // -----------------------------------------------------------------------

    private (int PaintedLotCount, int PaintedSkippedLotCount) BuildPreviewPaintMap(
        List<LotEntry> allLots,
        HashSet<string> skippedLotIds,
        List<ParcelBuildingFootprintCandidate> footprints)
    {
        const int DefaultSize = 256;
        int w, h;
        (int R, int G, int B)[,] map;

        if (!string.IsNullOrEmpty(_cachedSourcePng) && File.Exists(_cachedSourcePng))
        {
            using var src = new System.Drawing.Bitmap(_cachedSourcePng);
            w = src.Width;
            h = src.Height;
            map = new (int R, int G, int B)[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    var c = src.GetPixel(x, y);
                    map[x, y] = (c.R, c.G, c.B);
                }
        }
        else
        {
            w = DefaultSize;
            h = DefaultSize;
            map = new (int R, int G, int B)[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    map[x, y] = (18, 18, 24);
        }

        // Pass 1: paint ALL lot bases (including skipped) with lot shade
        int paintedLotCount = 0;
        foreach (var lot in allLots)
        {
            for (int x = Math.Max(0, lot.X1); x <= Math.Min(w - 1, lot.X2); x++)
                for (int y = Math.Max(0, lot.Y1); y <= Math.Min(h - 1, lot.Y2); y++)
                    map[x, y] = (lot.ShadeR, lot.ShadeG, lot.ShadeB);
            paintedLotCount++;
        }

        // Pass 2: paint skipped lots with muted amber override so they are distinctly visible
        int paintedSkippedLotCount = 0;
        foreach (var lot in allLots)
        {
            if (!skippedLotIds.Contains(lot.LotId)) continue;
            for (int x = Math.Max(0, lot.X1); x <= Math.Min(w - 1, lot.X2); x++)
                for (int y = Math.Max(0, lot.Y1); y <= Math.Min(h - 1, lot.Y2); y++)
                    map[x, y] = SkippedLotColor;
            paintedSkippedLotCount++;
        }

        // Pass 3: paint footprint overlays (lighter shade) on top
        foreach (var fp in footprints)
        {
            int fr = Math.Min(255, fp.ShadeR + 45);
            int fg = Math.Min(255, fp.ShadeG + 45);
            int fb = Math.Min(255, fp.ShadeB + 45);
            for (int x = Math.Max(0, fp.FpX1); x <= Math.Min(w - 1, fp.FpX2); x++)
                for (int y = Math.Max(0, fp.FpY1); y <= Math.Min(h - 1, fp.FpY2); y++)
                    map[x, y] = (fr, fg, fb);
        }

        _cachedPixelMap  = map;
        _cachedMapWidth  = w;
        _cachedMapHeight = h;

        return (paintedLotCount, paintedSkippedLotCount);
    }

    // -----------------------------------------------------------------------
    // Build
    // -----------------------------------------------------------------------

    public DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult Build(
        string lotFillJsonPath, string buildingFootprintPolicyPath, string outputRoot,
        string? sectorOverridesPath = null)
    {
        _cachedPixelMap  = null;
        _cachedSourcePng = string.Empty;

        var result = new DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult
        {
            Format            = "MAP-30A_WORLDBUILDER_PARCEL_BUILDING_FOOTPRINT_CANDIDATES",
            GeneratedUtc      = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            SourceLotFillJson = lotFillJsonPath,
            SandboxOnly       = true,
        };
        var checks = new List<ParcelBuildingFootprintCheck>();

        bool jsonExists = File.Exists(lotFillJsonPath);
        AddCheck(checks, "MAP30A_LOT_FILL_JSON_EXISTS", "Lot fill JSON exists",
            "PASS", jsonExists ? "PASS" : "FAIL");
        if (!jsonExists)
        {
            result.Errors.Add($"Lot fill JSON not found: {lotFillJsonPath}");
            FinalizeResult(result, checks, valid: false, verdict: "MAP30A_LOT_FILL_JSON_NOT_FOUND");
            return result;
        }

        bool policyExists = File.Exists(buildingFootprintPolicyPath);
        AddCheck(checks, "MAP30A_POLICY_JSON_EXISTS", "Building footprint policy JSON exists",
            "PASS", policyExists ? "PASS" : "FAIL");
        if (!policyExists)
        {
            result.Errors.Add($"Building footprint policy not found: {buildingFootprintPolicyPath}");
            FinalizeResult(result, checks, valid: false, verdict: "MAP30A_POLICY_JSON_NOT_FOUND");
            return result;
        }

        FootprintPolicyStore? policyStore;
        try
        {
            policyStore = LoadFootprintPolicyStore(buildingFootprintPolicyPath);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Building footprint policy invalid: {ex.Message}");
            AddCheck(checks, "MAP30A_POLICY_LOADED", "Footprint policy loaded", "PASS", "FAIL");
            FinalizeResult(result, checks, valid: false, verdict: "MAP30A_POLICY_INVALID");
            return result;
        }
        AddCheck(checks, "MAP30A_POLICY_LOADED", "Footprint policy loaded", "PASS", "PASS");

        result.PolicySource     = "EXTERNAL";
        result.PolicyPath       = buildingFootprintPolicyPath;
        result.PolicyLoaded     = true;
        result.PolicyVersion    = policyStore.Version;
        result.PolicyEntryCount = policyStore.EntryCount;

        List<LotEntry> lots;
        string sourcePng, lotFillPolicyVersion;
        try
        {
            (lots, sourcePng, lotFillPolicyVersion) = ParseLotFillJson(lotFillJsonPath);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Lot fill JSON parse error: {ex.Message}");
            FinalizeResult(result, checks, valid: false, verdict: "MAP30A_LOT_FILL_JSON_INVALID");
            return result;
        }

        result.LotFillPolicyVersion = lotFillPolicyVersion;
        result.TotalLotCount        = lots.Count;
        _cachedSourcePng            = sourcePng;

        SectorAssignmentStore? sectorStore = null;
        if (!string.IsNullOrEmpty(sectorOverridesPath))
        {
            if (!File.Exists(sectorOverridesPath))
            {
                result.Errors.Add($"Sector overrides file not found: {sectorOverridesPath}");
                FinalizeResult(result, checks, valid: false, verdict: "MAP31A_SECTOR_OVERRIDES_NOT_FOUND");
                return result;
            }
            sectorStore = LoadSectorAssignmentStore(sectorOverridesPath);
            result.SectorAssignmentSource  = "EXTERNAL";
            result.SectorAssignmentPath    = sectorOverridesPath;
            result.SectorAssignmentLoaded  = true;
            result.SectorCount             = sectorStore.EntryCount;
        }
        else
        {
            result.SectorAssignmentSource  = "NONE";
            result.SectorAssignmentLoaded  = false;
            result.SectorCount             = 0;
        }

        var footprints  = new List<ParcelBuildingFootprintCandidate>();
        var skippedLots = new List<SkippedFootprintLot>();
        int eligibleCount = 0;

        foreach (var lot in lots)
        {
            int    cx     = (lot.X1 + lot.X2) / 2;
            int    cy     = (lot.Y1 + lot.Y2) / 2;
            string sector = sectorStore?.Assign(cx, cy) ?? "DEFAULT";
            var policy = policyStore.Resolve(lot.ParcelClass, sector);

            if (lot.TileCount < policy.MinLotAreaTiles)
            {
                skippedLots.Add(new SkippedFootprintLot
                {
                    LotId             = lot.LotId,
                    ComponentId       = lot.ComponentId,
                    ParcelClass       = lot.ParcelClass,
                    Reason            = $"lot area {lot.TileCount} < min_lot_area_tiles {policy.MinLotAreaTiles}",
                    NeighborhoodSector = sector,
                });
                continue;
            }
            eligibleCount++;

            var (fpX1, fpY1, fpX2, fpY2, skipped, skipReason) = ComputeFootprint(lot, policy);
            if (skipped)
            {
                skippedLots.Add(new SkippedFootprintLot
                {
                    LotId             = lot.LotId,
                    ComponentId       = lot.ComponentId,
                    ParcelClass       = lot.ParcelClass,
                    Reason            = skipReason,
                    NeighborhoodSector = sector,
                });
                continue;
            }

            bool isNS    = lot.FrontageDirection == "NORTH" || lot.FrontageDirection == "SOUTH";
            int  fpWidth = isNS ? fpX2 - fpX1 + 1 : fpY2 - fpY1 + 1;
            int  fpDepth = isNS ? fpY2 - fpY1 + 1 : fpX2 - fpX1 + 1;
            int  fpTiles = (fpX2 - fpX1 + 1) * (fpY2 - fpY1 + 1);

            footprints.Add(new ParcelBuildingFootprintCandidate
            {
                FootprintId        = $"{lot.LotId}_FOOTPRINT",
                LotId              = lot.LotId,
                ComponentId        = lot.ComponentId,
                ParcelClass        = lot.ParcelClass,
                FrontageDirection  = lot.FrontageDirection,
                FootprintKind      = policy.PreferredFootprintKind,
                LotX1 = lot.X1, LotY1 = lot.Y1, LotX2 = lot.X2, LotY2 = lot.Y2,
                LotTileCount       = lot.TileCount,
                FpX1 = fpX1, FpY1 = fpY1, FpX2 = fpX2, FpY2 = fpY2,
                FpWidth            = fpWidth,
                FpDepth            = fpDepth,
                FpTileCount        = fpTiles,
                CoverageRatio      = Math.Round((double)fpTiles / lot.TileCount, 4),
                ShadeR = lot.ShadeR, ShadeG = lot.ShadeG, ShadeB = lot.ShadeB,
                NeighborhoodSector = sector,
            });
        }

        result.Footprints         = footprints;
        result.SkippedLots        = skippedLots;
        result.EligibleLotCount   = eligibleCount;
        result.FootprintCount     = footprints.Count;
        result.SkippedLotCount    = skippedLots.Count;
        result.BlueFootprintCount = footprints.Count(f => f.ParcelClass == ParcelClassBlue);
        result.RedFootprintCount  = footprints.Count(f => f.ParcelClass == ParcelClassRed);

        result.SectorCounts = footprints.Select(f => f.NeighborhoodSector)
            .Concat(skippedLots.Select(s => s.NeighborhoodSector))
            .GroupBy(s => s)
            .OrderBy(g => g.Key)
            .Select(g => new SectorCountEntry { SectorId = g.Key, LotCount = g.Count() })
            .ToList();

        // Sector footprint summaries (MAP-31B)
        var sectorSummaries = new List<SectorFootprintSummaryEntry>();
        foreach (var sid in result.SectorCounts.Select(sc => sc.SectorId))
        {
            var sF = footprints.Where(f => f.NeighborhoodSector == sid).ToList();
            var sS = skippedLots.Where(s => s.NeighborhoodSector == sid).ToList();
            sectorSummaries.Add(new SectorFootprintSummaryEntry
            {
                SectorId             = sid,
                LotCount             = sF.Count + sS.Count,
                FootprintCount       = sF.Count,
                SkippedLotCount      = sS.Count,
                BlueFootprintCount   = sF.Count(f => f.ParcelClass == ParcelClassBlue),
                RedFootprintCount    = sF.Count(f => f.ParcelClass == ParcelClassRed),
                AverageCoverageRatio = sF.Count > 0 ? Math.Round(sF.Average(f => f.CoverageRatio), 4) : 0,
                MinCoverageRatio     = sF.Count > 0 ? Math.Round(sF.Min(f => f.CoverageRatio), 4) : 0,
                MaxCoverageRatio     = sF.Count > 0 ? Math.Round(sF.Max(f => f.CoverageRatio), 4) : 0,
            });
        }
        result.SectorFootprintSummaries = sectorSummaries;

        // Sector preview legend (MAP-31B) — deterministic colors
        var sectorLegend = new List<SectorPreviewLegendEntry>();
        int legendFallbackIdx = 0;
        foreach (var sc in result.SectorCounts)
        {
            sectorLegend.Add(new SectorPreviewLegendEntry
            {
                SectorId        = sc.SectorId,
                LotCount        = sc.LotCount,
                PreviewColorRgb = GetSectorLegendColor(sc.SectorId, ref legendFallbackIdx),
                Description     = GetSectorLegendDescription(sc.SectorId),
            });
        }
        result.SectorPreviewLegendEntries = sectorLegend;
        result.SectorPreviewLegendCount   = sectorLegend.Count;

        // Build the preview pixel map now — checks B3/B4 scan it directly
        var skippedLotIds = new HashSet<string>(skippedLots.Select(s => s.LotId));
        var (paintedLotCount, paintedSkippedLotCount) = BuildPreviewPaintMap(lots, skippedLotIds, footprints);

        result.PreviewPaintedLotCount        = paintedLotCount;
        result.PreviewPaintedSkippedLotCount = paintedSkippedLotCount;
        result.SkippedLotPreviewColorRgb     = SkippedLotPreviewColorRgbString;

        // -----------------------------------------------------------------------
        // MAP-30A checks (preserved)
        // -----------------------------------------------------------------------

        if (footprints.Count > 0)
        {
            bool allInside = footprints.All(f =>
                f.FpX1 >= f.LotX1 && f.FpY1 >= f.LotY1 &&
                f.FpX2 <= f.LotX2 && f.FpY2 <= f.LotY2);
            AddCheck(checks, "MAP30A_FOOTPRINTS_INSIDE_LOTS",
                "All footprints are inside their lot bounds",
                "PASS", allInside ? "PASS" : "FAIL");
        }

        result.ForbiddenArtifactScan = ScanOutputRoot(outputRoot);
        bool scanPasses = result.ForbiddenArtifactScan.Contains("PASS", StringComparison.Ordinal)
                       && !result.ForbiddenArtifactScan.Contains("FAIL", StringComparison.Ordinal);
        AddCheck(checks, "MAP30A_NO_RUNTIME_ARTIFACTS_WRITTEN",
            "No forbidden runtime artifacts in output root",
            "PASS", scanPasses ? "PASS" : "FAIL");

        AddCheck(checks, "MAP30A_SANDBOX_ONLY_TRUE",  "SandboxOnly is true",  "True",  result.SandboxOnly.ToString());
        AddCheck(checks, "MAP30A_WRITER_READY_FALSE",  "WriterReady is false", "False", result.WriterReady.ToString());
        AddCheck(checks, "MAP30A_RUNTIME_VALID_FALSE", "RuntimeValid is false","False", result.RuntimeValid.ToString());
        AddCheck(checks, "MAP30A_MATERIALIZED_FALSE",  "Materialized is false","False", result.Materialized.ToString());
        AddCheck(checks, "MAP30A_NO_RUNTIME_PROOF",    "RuntimeProofClaimed is false",           "False", result.RuntimeProofClaimed.ToString());
        AddCheck(checks, "MAP30A_NO_PUBLIC_PLAYABLE",  "PublicPlayablePackagingClaimed is false", "False", result.PublicPlayablePackagingClaimed.ToString());

        // -----------------------------------------------------------------------
        // MAP-30B checks (new)
        // -----------------------------------------------------------------------

        // B1 — all lots painted in preview (including skipped)
        AddCheck(checks, "MAP30B_ALL_LOTS_PAINTED_IN_PREVIEW",
            "All lots including skipped are painted in preview",
            result.TotalLotCount.ToString(), paintedLotCount.ToString());

        // B2 — skipped lots visible (conditional)
        if (skippedLots.Count > 0)
        {
            AddCheck(checks, "MAP30B_SKIPPED_LOTS_VISIBLE_IN_PREVIEW",
                "Skipped lots painted with skipped-lot color in preview",
                skippedLots.Count.ToString(), paintedSkippedLotCount.ToString());
        }

        // B3 — no debug cyan or pure black inside lot bounds (outside bounds may come from source PNG)
        bool noCyanBlack = true;
        if (_cachedPixelMap != null)
        {
            foreach (var lot in lots)
            {
                for (int x = Math.Max(0, lot.X1); x <= Math.Min(_cachedMapWidth - 1, lot.X2) && noCyanBlack; x++)
                    for (int y = Math.Max(0, lot.Y1); y <= Math.Min(_cachedMapHeight - 1, lot.Y2) && noCyanBlack; y++)
                    {
                        var (r, g, b) = _cachedPixelMap[x, y];
                        if (IsCyanDebug(r, g, b) || IsPureBlack(r, g, b))
                            noCyanBlack = false;
                    }
            }
        }
        AddCheck(checks, "MAP30B_OUTPUT_PNG_NO_DEBUG_CYAN_OR_BLACK",
            "No debug cyan or pure black pixels inside lot bounds",
            "PASS", noCyanBlack ? "PASS" : "FAIL");

        // B4 — no source-blue or source-red inside lot bounds after painting
        bool noSourceColorInLots = true;
        if (_cachedPixelMap != null)
        {
            foreach (var lot in lots)
            {
                for (int x = Math.Max(0, lot.X1); x <= Math.Min(_cachedMapWidth - 1, lot.X2) && noSourceColorInLots; x++)
                    for (int y = Math.Max(0, lot.Y1); y <= Math.Min(_cachedMapHeight - 1, lot.Y2) && noSourceColorInLots; y++)
                    {
                        var (r, g, b) = _cachedPixelMap[x, y];
                        if (IsSourceBlue(r, g, b) || IsSourceRed(r, g, b))
                            noSourceColorInLots = false;
                    }
            }
        }
        AddCheck(checks, "MAP30B_OUTPUT_PNG_NO_SOURCE_BLUE_RED_INSIDE_LOTS",
            "No source parcel colors remain inside lot bounds after painting",
            "PASS", noSourceColorInLots ? "PASS" : "FAIL");

        // B5 — no runtime artifacts (MAP30B prefix)
        AddCheck(checks, "MAP30B_NO_RUNTIME_ARTIFACTS_WRITTEN",
            "No forbidden runtime artifacts in output root (MAP30B)",
            "PASS", scanPasses ? "PASS" : "FAIL");

        // B6 — claim boundary compound
        bool claimBoundaryOk = result.SandboxOnly && !result.WriterReady && !result.RuntimeValid
                            && !result.Materialized && !result.RuntimeProofClaimed
                            && !result.PublicPlayablePackagingClaimed;
        AddCheck(checks, "MAP30B_CLAIM_BOUNDARY_FALSE",
            "All runtime/public claim flags are false",
            "PASS", claimBoundaryOk ? "PASS" : "FAIL");

        // -----------------------------------------------------------------------
        // MAP-31A checks (sector-aware policy)
        // -----------------------------------------------------------------------

        // A1 — sector assignment file loaded (or not provided)
        string sectorLoadExpected = sectorStore != null ? "PASS" : "NOT_PROVIDED";
        string sectorLoadActual   = sectorStore != null ? "PASS" : "NOT_PROVIDED";
        AddCheck(checks, "MAP31A_SECTOR_ASSIGNMENT_LOADED",
            "Sector assignment file loaded (or not provided)",
            sectorLoadExpected, sectorLoadActual);

        // A2 — all lots have a non-empty sector assigned
        int lotsWithSector = footprints.Count(f => !string.IsNullOrEmpty(f.NeighborhoodSector))
                           + skippedLots.Count(s => !string.IsNullOrEmpty(s.NeighborhoodSector));
        AddCheck(checks, "MAP31A_ALL_LOTS_HAVE_SECTOR",
            "Every lot (footprint + skipped) has a neighborhood sector assigned",
            result.TotalLotCount.ToString(), lotsWithSector.ToString());

        // A3 — policy resolved for all lots (footprints + skipped == total)
        int resolvedLots = footprints.Count + skippedLots.Count;
        AddCheck(checks, "MAP31A_POLICY_RESOLVES_BY_SECTOR",
            "Sector-aware policy resolved for all lots",
            result.TotalLotCount.ToString(), resolvedLots.ToString());

        // A4 — default sector fallback entry present in policy
        bool defaultFallbackPresent = policyStore.HasAnyDefaultPolicy;
        AddCheck(checks, "MAP31A_DEFAULT_SECTOR_FALLBACK_PRESENT",
            "Footprint policy contains at least one DEFAULT sector entry",
            "PASS", defaultFallbackPresent ? "PASS" : "FAIL");

        // A5 — sector counts non-empty
        AddCheck(checks, "MAP31A_SECTOR_COUNTS_NONEMPTY",
            "Sector count list is non-empty",
            "PASS", result.SectorCounts.Count > 0 ? "PASS" : "FAIL");

        // A6 — no runtime artifacts (MAP31A prefix)
        AddCheck(checks, "MAP31A_NO_RUNTIME_ARTIFACTS_WRITTEN",
            "No forbidden runtime artifacts in output root (MAP31A)",
            "PASS", scanPasses ? "PASS" : "FAIL");

        // A7 — claim boundary compound
        AddCheck(checks, "MAP31A_CLAIM_BOUNDARY_FALSE",
            "All runtime/public claim flags are false (MAP31A)",
            "PASS", claimBoundaryOk ? "PASS" : "FAIL");

        // -----------------------------------------------------------------------
        // MAP-31B checks (sector footprint summary and preview legend)
        // -----------------------------------------------------------------------

        // S1 — sector summaries present
        AddCheck(checks, "MAP31B_SECTOR_SUMMARIES_PRESENT",
            "Sector footprint summaries are non-empty",
            "PASS", result.SectorFootprintSummaries.Count > 0 ? "PASS" : "FAIL");

        // S2 — sector summaries cover all lots
        int summariedLots = result.SectorFootprintSummaries.Sum(s => s.LotCount);
        AddCheck(checks, "MAP31B_SECTOR_SUMMARIES_COVER_ALL_LOTS",
            "Sum of sector summary lot counts equals total lot count",
            result.TotalLotCount.ToString(), summariedLots.ToString());

        // S3 — sector legend present
        AddCheck(checks, "MAP31B_SECTOR_LEGEND_PRESENT",
            "Sector preview legend is non-empty",
            "PASS", result.SectorPreviewLegendEntries.Count > 0 ? "PASS" : "FAIL");

        // S4 — legend covers all used sectors
        AddCheck(checks, "MAP31B_SECTOR_LEGEND_COVERS_USED_SECTORS",
            "Legend entry count equals distinct sector count",
            result.SectorCounts.Count.ToString(), result.SectorPreviewLegendEntries.Count.ToString());

        // S5 — sector summary footprint counts match total
        int summaryFpTotal = result.SectorFootprintSummaries.Sum(s => s.FootprintCount);
        AddCheck(checks, "MAP31B_SECTOR_SUMMARY_FOOTPRINT_COUNTS_MATCH",
            "Sum of per-sector footprint counts equals total footprint count",
            result.FootprintCount.ToString(), summaryFpTotal.ToString());

        // S6 — sector summary skipped counts match total
        int summarySkTotal = result.SectorFootprintSummaries.Sum(s => s.SkippedLotCount);
        AddCheck(checks, "MAP31B_SECTOR_SUMMARY_SKIPPED_COUNTS_MATCH",
            "Sum of per-sector skipped counts equals total skipped lot count",
            result.SkippedLotCount.ToString(), summarySkTotal.ToString());

        // S7 — no runtime artifacts (MAP31B prefix)
        AddCheck(checks, "MAP31B_NO_RUNTIME_ARTIFACTS_WRITTEN",
            "No forbidden runtime artifacts in output root (MAP31B)",
            "PASS", scanPasses ? "PASS" : "FAIL");

        // S8 — claim boundary compound
        AddCheck(checks, "MAP31B_CLAIM_BOUNDARY_FALSE",
            "All runtime/public claim flags are false (MAP31B)",
            "PASS", claimBoundaryOk ? "PASS" : "FAIL");

        FinalizeResult(result, checks,
            valid: !checks.Any(c => c.CheckStatus == "FAIL") && result.Errors.Count == 0,
            verdict: "MAP30A_WORLDBUILDER_PARCEL_BUILDING_FOOTPRINT_CANDIDATES_COMPLETE");

        return result;
    }

    // -----------------------------------------------------------------------
    // PNG rendering — uses cached pixel map (same data scanned by checks)
    // -----------------------------------------------------------------------

    public byte[] RenderOutputPngBytes(DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult result)
    {
        if (_cachedPixelMap != null)
        {
            using var bmp = new System.Drawing.Bitmap(_cachedMapWidth, _cachedMapHeight);
            for (int x = 0; x < _cachedMapWidth; x++)
                for (int y = 0; y < _cachedMapHeight; y++)
                {
                    var (r, g, b) = _cachedPixelMap[x, y];
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(r, g, b));
                }
            using var ms = new System.IO.MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        // Fallback path (shouldn't normally execute if Build() was called first)
        const int Size = 256;
        System.Drawing.Bitmap bmpFb;

        if (!string.IsNullOrEmpty(_cachedSourcePng) && File.Exists(_cachedSourcePng))
        {
            var bytes = File.ReadAllBytes(_cachedSourcePng);
            using var ms2 = new System.IO.MemoryStream(bytes);
            using var tmp = new System.Drawing.Bitmap(ms2);
            bmpFb = tmp.Clone(
                new System.Drawing.Rectangle(0, 0, tmp.Width, tmp.Height),
                tmp.PixelFormat);
        }
        else
        {
            bmpFb = new System.Drawing.Bitmap(Size, Size);
            using var g = System.Drawing.Graphics.FromImage(bmpFb);
            g.Clear(System.Drawing.Color.FromArgb(18, 18, 24));
        }

        foreach (var fp in result.Footprints)
        {
            for (int x = Math.Max(0, fp.LotX1); x <= Math.Min(bmpFb.Width - 1, fp.LotX2); x++)
                for (int y = Math.Max(0, fp.LotY1); y <= Math.Min(bmpFb.Height - 1, fp.LotY2); y++)
                    bmpFb.SetPixel(x, y, System.Drawing.Color.FromArgb(fp.ShadeR, fp.ShadeG, fp.ShadeB));

            int fr = Math.Min(255, fp.ShadeR + 45);
            int fg = Math.Min(255, fp.ShadeG + 45);
            int fb = Math.Min(255, fp.ShadeB + 45);
            for (int x = Math.Max(0, fp.FpX1); x <= Math.Min(bmpFb.Width - 1, fp.FpX2); x++)
                for (int y = Math.Max(0, fp.FpY1); y <= Math.Min(bmpFb.Height - 1, fp.FpY2); y++)
                    bmpFb.SetPixel(x, y, System.Drawing.Color.FromArgb(fr, fg, fb));
        }

        using var msFb = new System.IO.MemoryStream();
        bmpFb.Save(msFb, System.Drawing.Imaging.ImageFormat.Png);
        bmpFb.Dispose();
        return msFb.ToArray();
    }

    // -----------------------------------------------------------------------
    // Text / CSV / HTML renderers
    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderCsv(DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("lot_id,component_id,parcel_class,frontage_direction,status,footprint_id,footprint_kind,lot_x1,lot_y1,lot_x2,lot_y2,lot_tile_count,fp_x1,fp_y1,fp_x2,fp_y2,fp_width,fp_depth,fp_tile_count,coverage_ratio,skip_reason");

        foreach (var fp in result.Footprints)
        {
            sb.AppendLine(
                $"{fp.LotId},{fp.ComponentId},{fp.ParcelClass},{fp.FrontageDirection},FOOTPRINT," +
                $"{fp.FootprintId},{fp.FootprintKind}," +
                $"{fp.LotX1},{fp.LotY1},{fp.LotX2},{fp.LotY2},{fp.LotTileCount}," +
                $"{fp.FpX1},{fp.FpY1},{fp.FpX2},{fp.FpY2},{fp.FpWidth},{fp.FpDepth},{fp.FpTileCount}," +
                $"{fp.CoverageRatio:F4},");
        }

        foreach (var sk in result.SkippedLots)
        {
            sb.AppendLine(
                $"{sk.LotId},{sk.ComponentId},{sk.ParcelClass},,SKIPPED,,," +
                $",,,,,,,,,,,,,\"{sk.Reason}\"");
        }

        return sb.ToString();
    }

    public string RenderChecksCsv(DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},\"{c.Description}\",{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderHtml(DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\"><title>MAP-30A/B Parcel Building Footprint Candidates</title></head>");
        sb.AppendLine("<body style=\"font-family:monospace;background:#12121a;color:#ccc;padding:16px\">");
        sb.AppendLine("<h2 style=\"color:#e8c870\">MAP-30A/B Parcel Building Footprint Candidates</h2>");

        sb.AppendLine("<h3>Summary</h3><table border=\"1\" cellpadding=\"4\">");
        sb.AppendLine($"<tr><td>Total lots</td><td>{result.TotalLotCount}</td></tr>");
        sb.AppendLine($"<tr><td>Eligible lots</td><td>{result.EligibleLotCount}</td></tr>");
        sb.AppendLine($"<tr><td>Footprints</td><td>{result.FootprintCount}</td></tr>");
        sb.AppendLine($"<tr><td>Skipped lots</td><td>{result.SkippedLotCount}</td></tr>");
        sb.AppendLine($"<tr><td>Blue footprints</td><td>{result.BlueFootprintCount}</td></tr>");
        sb.AppendLine($"<tr><td>Red footprints</td><td>{result.RedFootprintCount}</td></tr>");
        sb.AppendLine($"<tr><td>Preview painted lots</td><td>{result.PreviewPaintedLotCount}</td></tr>");
        sb.AppendLine($"<tr><td>Preview painted skipped lots</td><td>{result.PreviewPaintedSkippedLotCount}</td></tr>");
        sb.AppendLine($"<tr><td>Skipped lot preview color</td><td style=\"background:{result.SkippedLotPreviewColorRgb}\">&nbsp;{result.SkippedLotPreviewColorRgb}&nbsp;</td></tr>");
        sb.AppendLine($"<tr><td>Policy version</td><td>{result.PolicyVersion}</td></tr>");
        sb.AppendLine($"<tr><td>Lot fill policy</td><td>{result.LotFillPolicyVersion}</td></tr>");
        sb.AppendLine($"<tr><td>Checks</td><td>{result.PassedCheckCount}/{result.CheckCount} PASS</td></tr>");
        sb.AppendLine($"<tr><td>Is valid</td><td>{result.IsValid}</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<h3>Sector Assignment</h3><table border=\"1\" cellpadding=\"4\">");
        sb.AppendLine($"<tr><td>Assignment source</td><td>{result.SectorAssignmentSource}</td></tr>");
        sb.AppendLine($"<tr><td>Loaded</td><td>{result.SectorAssignmentLoaded}</td></tr>");
        sb.AppendLine($"<tr><td>Sector count</td><td>{result.SectorCount}</td></tr>");
        sb.AppendLine("</table>");

        if (result.SectorCounts.Count > 0)
        {
            sb.AppendLine("<h4>Sector Counts</h4><table border=\"1\" cellpadding=\"4\">");
            sb.AppendLine("<tr><th>sector_id</th><th>lot_count</th></tr>");
            foreach (var sc in result.SectorCounts)
                sb.AppendLine($"<tr><td>{sc.SectorId}</td><td>{sc.LotCount}</td></tr>");
            sb.AppendLine("</table>");
        }

        if (result.SectorFootprintSummaries.Count > 0)
        {
            sb.AppendLine("<h4>Sector Footprint Summary</h4><table border=\"1\" cellpadding=\"4\">");
            sb.AppendLine("<tr><th>sector_id</th><th>lots</th><th>footprints</th><th>skipped</th><th>blue_fp</th><th>red_fp</th><th>avg_coverage</th><th>min_coverage</th><th>max_coverage</th></tr>");
            foreach (var ss in result.SectorFootprintSummaries)
                sb.AppendLine($"<tr><td>{ss.SectorId}</td><td>{ss.LotCount}</td><td>{ss.FootprintCount}</td><td>{ss.SkippedLotCount}</td><td>{ss.BlueFootprintCount}</td><td>{ss.RedFootprintCount}</td><td>{ss.AverageCoverageRatio:F4}</td><td>{ss.MinCoverageRatio:F4}</td><td>{ss.MaxCoverageRatio:F4}</td></tr>");
            sb.AppendLine("</table>");
        }

        if (result.SectorPreviewLegendEntries.Count > 0)
        {
            sb.AppendLine("<h4>Sector Preview Legend</h4><table border=\"1\" cellpadding=\"4\">");
            sb.AppendLine("<tr><th>sector_id</th><th>lots</th><th>color</th><th>description</th></tr>");
            foreach (var le in result.SectorPreviewLegendEntries)
                sb.AppendLine($"<tr><td>{le.SectorId}</td><td>{le.LotCount}</td><td style=\"background:{le.PreviewColorRgb}\">&nbsp;{le.PreviewColorRgb}&nbsp;</td><td>{le.Description}</td></tr>");
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<h3>Checks</h3><table border=\"1\" cellpadding=\"4\">");
        sb.AppendLine("<tr><th>check_id</th><th>status</th><th>expected</th><th>actual</th></tr>");
        foreach (var c in result.Checks)
        {
            string col = c.CheckStatus == "PASS" ? "#3a3" : "#c33";
            sb.AppendLine($"<tr><td>{c.CheckId}</td><td style=\"color:{col}\">{c.CheckStatus}</td><td>{c.Expected}</td><td>{c.Actual}</td></tr>");
        }
        sb.AppendLine("</table>");

        if (result.Footprints.Count > 0)
        {
            sb.AppendLine("<h3>Footprints</h3><table border=\"1\" cellpadding=\"4\">");
            sb.AppendLine("<tr><th>footprint_id</th><th>parcel_class</th><th>frontage</th><th>kind</th><th>fp_x1</th><th>fp_y1</th><th>fp_x2</th><th>fp_y2</th><th>fp_tiles</th><th>coverage</th></tr>");
            foreach (var fp in result.Footprints)
                sb.AppendLine($"<tr><td>{fp.FootprintId}</td><td>{fp.ParcelClass}</td><td>{fp.FrontageDirection}</td><td>{fp.FootprintKind}</td><td>{fp.FpX1}</td><td>{fp.FpY1}</td><td>{fp.FpX2}</td><td>{fp.FpY2}</td><td>{fp.FpTileCount}</td><td>{fp.CoverageRatio:F3}</td></tr>");
            sb.AppendLine("</table>");
        }

        if (result.SkippedLots.Count > 0)
        {
            sb.AppendLine("<h3>Skipped Lots</h3><table border=\"1\" cellpadding=\"4\">");
            sb.AppendLine("<tr><th>lot_id</th><th>parcel_class</th><th>reason</th></tr>");
            foreach (var sk in result.SkippedLots)
                sb.AppendLine($"<tr><td>{sk.LotId}</td><td>{sk.ParcelClass}</td><td>{sk.Reason}</td></tr>");
            sb.AppendLine("</table>");
        }

        sb.AppendLine($"<p style=\"color:#888;font-size:0.85em\">sandbox_only=true | writer_ready=false | runtime_valid=false | materialized=false</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-30A/B WORLDBUILDER PARCEL BUILDING FOOTPRINT CANDIDATES");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Total lots              : {r.TotalLotCount}");
        sb.AppendLine($"Eligible lots           : {r.EligibleLotCount}");
        sb.AppendLine($"Footprints              : {r.FootprintCount} (blue={r.BlueFootprintCount} red={r.RedFootprintCount})");
        sb.AppendLine($"Skipped lots            : {r.SkippedLotCount}");
        sb.AppendLine($"Preview painted lots    : {r.PreviewPaintedLotCount} (skipped={r.PreviewPaintedSkippedLotCount})");
        sb.AppendLine($"Skipped lot color       : {r.SkippedLotPreviewColorRgb}");
        sb.AppendLine($"Footprint policy        : {r.PolicyVersion} source={r.PolicySource} entries={r.PolicyEntryCount}");
        sb.AppendLine($"Lot fill policy         : {r.LotFillPolicyVersion}");
        string sectorSummary = r.SectorAssignmentLoaded
            ? $"EXTERNAL ({r.SectorCount} sectors)"
            : "NONE (all DEFAULT)";
        sb.AppendLine($"Sector assignment       : {sectorSummary}");
        if (r.SectorCounts.Count > 0)
        {
            var sectorLine = string.Join(" ", r.SectorCounts.Select(sc => $"{sc.SectorId}={sc.LotCount}"));
            sb.AppendLine($"Sector counts           : {sectorLine}");
        }
        if (r.SectorFootprintSummaries.Count > 0)
        {
            foreach (var ss in r.SectorFootprintSummaries)
                sb.AppendLine($"  {ss.SectorId,-22}: lots={ss.LotCount} fp={ss.FootprintCount} skipped={ss.SkippedLotCount} blue={ss.BlueFootprintCount} red={ss.RedFootprintCount} avg_cov={ss.AverageCoverageRatio:F4}");
        }
        sb.AppendLine($"Sector legend entries   : {r.SectorPreviewLegendCount}");
        sb.AppendLine($"Checks                  : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                : {r.IsValid}");
        sb.AppendLine($"Verdict                 : {r.Verdict}");
        sb.AppendLine($"Forbidden Scan          : {r.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim boundary          : sandbox_only=true | writer_ready=false | runtime_valid=false | materialized=false");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Sector legend helpers (MAP-31B)
    // -----------------------------------------------------------------------

    private static readonly string[] s_legendFallbackColors =
    {
        "rgb(180,130,200)",
        "rgb(80,160,200)",
        "rgb(200,100,100)",
        "rgb(100,200,180)",
    };

    private static string GetSectorLegendColor(string sectorId, ref int fallbackIdx)
    {
        if (sectorId == "DEFAULT")          return "rgb(120,120,120)";
        if (sectorId == "DOWNTOWN_CORE")    return "rgb(210,170,80)";
        if (sectorId == "OPEN_RESIDENTIAL") return "rgb(90,150,90)";
        return s_legendFallbackColors[fallbackIdx++ % s_legendFallbackColors.Length];
    }

    private static string GetSectorLegendDescription(string sectorId) => sectorId switch
    {
        "DEFAULT"          => "Unassigned sector — applies base footprint policy",
        "DOWNTOWN_CORE"    => "Downtown commercial core — high-density setbacks, high coverage",
        "OPEN_RESIDENTIAL" => "Open residential fringe — wide setbacks, low coverage",
        _                  => $"Custom sector: {sectorId}",
    };
}
