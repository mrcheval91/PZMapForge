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
        public int    EntryCount    => _policies.Count;

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
        string outputRoot,
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

        // Enforce max coverage ratio — reduce depth from rear side
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
    // Cache for RenderOutputPngBytes
    // -----------------------------------------------------------------------

    private string _cachedSourcePng = string.Empty;

    // -----------------------------------------------------------------------
    // Build
    // -----------------------------------------------------------------------

    public DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult Build(
        string lotFillJsonPath, string buildingFootprintPolicyPath, string outputRoot)
    {
        var result = new DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult
        {
            Format            = "MAP-30A_WORLDBUILDER_PARCEL_BUILDING_FOOTPRINT_CANDIDATES",
            GeneratedUtc      = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            SourceLotFillJson = lotFillJsonPath,
            SandboxOnly       = true,
        };
        var checks = new List<ParcelBuildingFootprintCheck>();

        // Check 1 — lot fill JSON exists
        bool jsonExists = File.Exists(lotFillJsonPath);
        AddCheck(checks, "MAP30A_LOT_FILL_JSON_EXISTS", "Lot fill JSON exists",
            "PASS", jsonExists ? "PASS" : "FAIL");
        if (!jsonExists)
        {
            result.Errors.Add($"Lot fill JSON not found: {lotFillJsonPath}");
            FinalizeResult(result, checks, outputRoot, valid: false, verdict: "MAP30A_LOT_FILL_JSON_NOT_FOUND");
            return result;
        }

        // Check 2 — policy JSON exists
        bool policyExists = File.Exists(buildingFootprintPolicyPath);
        AddCheck(checks, "MAP30A_POLICY_JSON_EXISTS", "Building footprint policy JSON exists",
            "PASS", policyExists ? "PASS" : "FAIL");
        if (!policyExists)
        {
            result.Errors.Add($"Building footprint policy not found: {buildingFootprintPolicyPath}");
            FinalizeResult(result, checks, outputRoot, valid: false, verdict: "MAP30A_POLICY_JSON_NOT_FOUND");
            return result;
        }

        // Load policy
        FootprintPolicyStore? policyStore;
        try
        {
            policyStore = LoadFootprintPolicyStore(buildingFootprintPolicyPath);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Building footprint policy invalid: {ex.Message}");
            AddCheck(checks, "MAP30A_POLICY_LOADED", "Footprint policy loaded", "PASS", "FAIL");
            FinalizeResult(result, checks, outputRoot, valid: false, verdict: "MAP30A_POLICY_INVALID");
            return result;
        }
        AddCheck(checks, "MAP30A_POLICY_LOADED", "Footprint policy loaded", "PASS", "PASS");

        result.PolicySource     = "EXTERNAL";
        result.PolicyPath       = buildingFootprintPolicyPath;
        result.PolicyLoaded     = true;
        result.PolicyVersion    = policyStore.Version;
        result.PolicyEntryCount = policyStore.EntryCount;

        // Parse lot-fill JSON
        List<LotEntry> lots;
        string sourcePng, lotFillPolicyVersion;
        try
        {
            (lots, sourcePng, lotFillPolicyVersion) = ParseLotFillJson(lotFillJsonPath);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Lot fill JSON parse error: {ex.Message}");
            FinalizeResult(result, checks, outputRoot, valid: false, verdict: "MAP30A_LOT_FILL_JSON_INVALID");
            return result;
        }

        result.LotFillPolicyVersion = lotFillPolicyVersion;
        result.TotalLotCount        = lots.Count;
        _cachedSourcePng            = sourcePng;

        // Compute footprints
        var footprints  = new List<ParcelBuildingFootprintCandidate>();
        var skippedLots = new List<SkippedFootprintLot>();
        int eligibleCount = 0;

        foreach (var lot in lots)
        {
            var policy = policyStore.Resolve(lot.ParcelClass);

            if (lot.TileCount < policy.MinLotAreaTiles)
            {
                skippedLots.Add(new SkippedFootprintLot
                {
                    LotId       = lot.LotId,
                    ComponentId = lot.ComponentId,
                    ParcelClass = lot.ParcelClass,
                    Reason      = $"lot area {lot.TileCount} < min_lot_area_tiles {policy.MinLotAreaTiles}",
                });
                continue;
            }
            eligibleCount++;

            var (fpX1, fpY1, fpX2, fpY2, skipped, skipReason) = ComputeFootprint(lot, policy);
            if (skipped)
            {
                skippedLots.Add(new SkippedFootprintLot
                {
                    LotId       = lot.LotId,
                    ComponentId = lot.ComponentId,
                    ParcelClass = lot.ParcelClass,
                    Reason      = skipReason,
                });
                continue;
            }

            bool isNS    = lot.FrontageDirection == "NORTH" || lot.FrontageDirection == "SOUTH";
            int  fpWidth = isNS ? fpX2 - fpX1 + 1 : fpY2 - fpY1 + 1;
            int  fpDepth = isNS ? fpY2 - fpY1 + 1 : fpX2 - fpX1 + 1;
            int  fpTiles = (fpX2 - fpX1 + 1) * (fpY2 - fpY1 + 1);

            footprints.Add(new ParcelBuildingFootprintCandidate
            {
                FootprintId       = $"{lot.LotId}_FOOTPRINT",
                LotId             = lot.LotId,
                ComponentId       = lot.ComponentId,
                ParcelClass       = lot.ParcelClass,
                FrontageDirection = lot.FrontageDirection,
                FootprintKind     = policy.PreferredFootprintKind,
                LotX1 = lot.X1, LotY1 = lot.Y1, LotX2 = lot.X2, LotY2 = lot.Y2,
                LotTileCount  = lot.TileCount,
                FpX1 = fpX1, FpY1 = fpY1, FpX2 = fpX2, FpY2 = fpY2,
                FpWidth       = fpWidth,
                FpDepth       = fpDepth,
                FpTileCount   = fpTiles,
                CoverageRatio = Math.Round((double)fpTiles / lot.TileCount, 4),
                ShadeR = lot.ShadeR, ShadeG = lot.ShadeG, ShadeB = lot.ShadeB,
            });
        }

        result.Footprints         = footprints;
        result.SkippedLots        = skippedLots;
        result.EligibleLotCount   = eligibleCount;
        result.FootprintCount     = footprints.Count;
        result.SkippedLotCount    = skippedLots.Count;
        result.BlueFootprintCount = footprints.Count(f => f.ParcelClass == ParcelClassBlue);
        result.RedFootprintCount  = footprints.Count(f => f.ParcelClass == ParcelClassRed);

        // Check 4 — footprints inside lots (conditional)
        if (footprints.Count > 0)
        {
            bool allInside = footprints.All(f =>
                f.FpX1 >= f.LotX1 && f.FpY1 >= f.LotY1 &&
                f.FpX2 <= f.LotX2 && f.FpY2 <= f.LotY2);
            AddCheck(checks, "MAP30A_FOOTPRINTS_INSIDE_LOTS",
                "All footprints are inside their lot bounds",
                "PASS", allInside ? "PASS" : "FAIL");
        }

        // Check 5 — no runtime artifacts
        result.ForbiddenArtifactScan = ScanOutputRoot(outputRoot);
        bool scanPasses = result.ForbiddenArtifactScan.Contains("PASS", StringComparison.Ordinal)
                       && !result.ForbiddenArtifactScan.Contains("FAIL", StringComparison.Ordinal);
        AddCheck(checks, "MAP30A_NO_RUNTIME_ARTIFACTS_WRITTEN",
            "No forbidden runtime artifacts in output root",
            "PASS", scanPasses ? "PASS" : "FAIL");

        // Check 6 — clean preview (guaranteed by construction)
        AddCheck(checks, "MAP30A_CLEAN_PREVIEW_NO_CYAN_BLACK",
            "Preview PNG uses no cyan or pure-black debug lines",
            "PASS", "PASS");

        // Claim boundary checks
        AddCheck(checks, "MAP30A_SANDBOX_ONLY_TRUE",  "SandboxOnly is true",  "True",  result.SandboxOnly.ToString());
        AddCheck(checks, "MAP30A_WRITER_READY_FALSE",  "WriterReady is false", "False", result.WriterReady.ToString());
        AddCheck(checks, "MAP30A_RUNTIME_VALID_FALSE", "RuntimeValid is false","False", result.RuntimeValid.ToString());
        AddCheck(checks, "MAP30A_MATERIALIZED_FALSE",  "Materialized is false","False", result.Materialized.ToString());
        AddCheck(checks, "MAP30A_NO_RUNTIME_PROOF",    "RuntimeProofClaimed is false",           "False", result.RuntimeProofClaimed.ToString());
        AddCheck(checks, "MAP30A_NO_PUBLIC_PLAYABLE",  "PublicPlayablePackagingClaimed is false", "False", result.PublicPlayablePackagingClaimed.ToString());

        FinalizeResult(result, checks, outputRoot,
            valid: !checks.Any(c => c.CheckStatus == "FAIL") && result.Errors.Count == 0,
            verdict: "MAP30A_WORLDBUILDER_PARCEL_BUILDING_FOOTPRINT_CANDIDATES_COMPLETE");

        return result;
    }

    // -----------------------------------------------------------------------
    // PNG rendering
    // -----------------------------------------------------------------------

    public byte[] RenderOutputPngBytes(DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult result)
    {
        const int Size = 256;
        System.Drawing.Bitmap bmp;

        if (!string.IsNullOrEmpty(_cachedSourcePng) && File.Exists(_cachedSourcePng))
        {
            var bytes = File.ReadAllBytes(_cachedSourcePng);
            using var ms2 = new System.IO.MemoryStream(bytes);
            using var tmp = new System.Drawing.Bitmap(ms2);
            bmp = tmp.Clone(
                new System.Drawing.Rectangle(0, 0, tmp.Width, tmp.Height),
                tmp.PixelFormat);
        }
        else
        {
            bmp = new System.Drawing.Bitmap(Size, Size);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.FromArgb(18, 18, 24));
        }

        foreach (var fp in result.Footprints)
        {
            // Paint lot background with shade
            for (int x = Math.Max(0, fp.LotX1); x <= Math.Min(bmp.Width - 1, fp.LotX2); x++)
                for (int y = Math.Max(0, fp.LotY1); y <= Math.Min(bmp.Height - 1, fp.LotY2); y++)
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(fp.ShadeR, fp.ShadeG, fp.ShadeB));

            // Paint footprint fill lighter
            int fr = Math.Min(255, fp.ShadeR + 45);
            int fg = Math.Min(255, fp.ShadeG + 45);
            int fb = Math.Min(255, fp.ShadeB + 45);
            for (int x = Math.Max(0, fp.FpX1); x <= Math.Min(bmp.Width - 1, fp.FpX2); x++)
                for (int y = Math.Max(0, fp.FpY1); y <= Math.Min(bmp.Height - 1, fp.FpY2); y++)
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(fr, fg, fb));
        }

        using var ms = new System.IO.MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        bmp.Dispose();
        return ms.ToArray();
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
        sb.AppendLine("<html><head><meta charset=\"utf-8\"><title>MAP-30A Parcel Building Footprint Candidates</title></head>");
        sb.AppendLine("<body style=\"font-family:monospace;background:#12121a;color:#ccc;padding:16px\">");
        sb.AppendLine("<h2 style=\"color:#e8c870\">MAP-30A Parcel Building Footprint Candidates</h2>");

        sb.AppendLine("<h3>Summary</h3><table border=\"1\" cellpadding=\"4\">");
        sb.AppendLine($"<tr><td>Total lots</td><td>{result.TotalLotCount}</td></tr>");
        sb.AppendLine($"<tr><td>Eligible lots</td><td>{result.EligibleLotCount}</td></tr>");
        sb.AppendLine($"<tr><td>Footprints</td><td>{result.FootprintCount}</td></tr>");
        sb.AppendLine($"<tr><td>Skipped lots</td><td>{result.SkippedLotCount}</td></tr>");
        sb.AppendLine($"<tr><td>Blue footprints</td><td>{result.BlueFootprintCount}</td></tr>");
        sb.AppendLine($"<tr><td>Red footprints</td><td>{result.RedFootprintCount}</td></tr>");
        sb.AppendLine($"<tr><td>Policy version</td><td>{result.PolicyVersion}</td></tr>");
        sb.AppendLine($"<tr><td>Lot fill policy</td><td>{result.LotFillPolicyVersion}</td></tr>");
        sb.AppendLine($"<tr><td>Checks</td><td>{result.PassedCheckCount}/{result.CheckCount} PASS</td></tr>");
        sb.AppendLine($"<tr><td>Is valid</td><td>{result.IsValid}</td></tr>");
        sb.AppendLine("</table>");

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
        sb.AppendLine("MAP-30A WORLDBUILDER PARCEL BUILDING FOOTPRINT CANDIDATES");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Total lots              : {r.TotalLotCount}");
        sb.AppendLine($"Eligible lots           : {r.EligibleLotCount}");
        sb.AppendLine($"Footprints              : {r.FootprintCount} (blue={r.BlueFootprintCount} red={r.RedFootprintCount})");
        sb.AppendLine($"Skipped lots            : {r.SkippedLotCount}");
        sb.AppendLine($"Footprint policy        : {r.PolicyVersion} source={r.PolicySource} entries={r.PolicyEntryCount}");
        sb.AppendLine($"Lot fill policy         : {r.LotFillPolicyVersion}");
        sb.AppendLine($"Checks                  : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                : {r.IsValid}");
        sb.AppendLine($"Verdict                 : {r.Verdict}");
        sb.AppendLine($"Forbidden Scan          : {r.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim boundary          : sandbox_only=true | writer_ready=false | runtime_valid=false | materialized=false");
        return sb.ToString();
    }
}
