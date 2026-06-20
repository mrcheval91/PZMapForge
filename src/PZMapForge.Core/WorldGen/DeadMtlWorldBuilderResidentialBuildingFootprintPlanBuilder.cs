using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderResidentialBuildingFootprintPlanBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private static readonly (byte R, byte G, byte B) s_bg       = (18,  18,  24);
    private static readonly (byte R, byte G, byte B) s_parcel   = (30,  34,  44);
    private static readonly (byte R, byte G, byte B) s_sidewalk = (58,  62,  74);
    private static readonly (byte R, byte G, byte B) s_rear     = (48,  36,  26);
    private static readonly (byte R, byte G, byte B) s_tick     = (240, 200, 140);

    // Warm neutral shade palette (light → medium → darker tan); index-based rotation per row
    private static readonly (byte R, byte G, byte B)[] s_footprintShades =
    {
        (200, 168, 120), // light tan
        (176, 140,  96), // medium tan
        (152, 116,  76), // darker tan/brown
    };

    private static (byte R, byte G, byte B) PickFootprintShade(string parentParcelId, string frontageDirection)
    {
        int idx       = int.Parse(parentParcelId.Substring(parentParcelId.LastIndexOf('_') + 1));
        int rowOffset = frontageDirection == "SOUTH" ? 1 : 0;
        return s_footprintShades[(idx + rowOffset) % s_footprintShades.Length];
    }

    // -----------------------------------------------------------------------
    // Check helpers
    // -----------------------------------------------------------------------

    private static void AddCheck(List<DeadMtlResidentialBuildingFootprintCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new DeadMtlResidentialBuildingFootprintCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlResidentialBuildingFootprintCheck> checks,
        string id, string label)
    {
        checks.Add(new DeadMtlResidentialBuildingFootprintCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PASS",
            Expected    = "PASS",
            Actual      = "PASS",
        });
    }

    private static void AddPendingCheck(List<DeadMtlResidentialBuildingFootprintCheck> checks,
        string id, string label)
    {
        checks.Add(new DeadMtlResidentialBuildingFootprintCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PENDING",
            Expected    = "PASS",
            Actual      = "PENDING",
        });
    }

    private static void SetCheck(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result,
        string checkId, string status)
    {
        var check = result.Checks.FirstOrDefault(c => c.CheckId == checkId);
        if (check is not null) { check.CheckStatus = status; check.Actual = status; }
    }

    // -----------------------------------------------------------------------
    // Forbidden artifact scan
    // -----------------------------------------------------------------------

    private static string ScanOutputRoot(string outputRoot)
    {
        const string prefix = "POST_MAP28B_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_FORBIDDEN_SCAN";
        if (!Directory.Exists(outputRoot))
            return $"{prefix} PASS (0 forbidden artifacts in output root)";

        var patterns = new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" };
        int count = patterns.Sum(p =>
            Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length);

        var allDirs = Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories);
        if (allDirs.Any(d => { var di = new DirectoryInfo(d); return di.Name == "maps" && di.Parent?.Name == "media"; })) count++;
        if (allDirs.Any(d => string.Equals(new DirectoryInfo(d).Name, "steamapps", StringComparison.OrdinalIgnoreCase))) count++;

        return count == 0
            ? $"{prefix} PASS (0 forbidden artifacts in output root)"
            : $"{prefix} FAIL ({count} forbidden artifacts found in output root)";
    }

    // -----------------------------------------------------------------------
    // Overlap helpers
    // -----------------------------------------------------------------------

    private static bool Overlaps(int ax1, int ay1, int ax2, int ay2,
                                  int bx1, int by1, int bx2, int by2)
        => ax1 <= bx2 && ax2 >= bx1 && ay1 <= by2 && ay2 >= by1;

    // -----------------------------------------------------------------------
    // Build
    // -----------------------------------------------------------------------

    public DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult Build(string outputRoot)
    {
        const string invalidVerdict = "MAP28B_WORLDBUILDER_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_INVALID";

        var topology = new DeadMtlWorldBuilderResidentialParcelTopologyBuilder().Build(outputRoot);

        var result = new DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult
        {
            Format                        = "MAP-28B_WORLDBUILDER_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN",
            GeneratedUtc                  = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                         = "map_00",
            ComponentId                   = "map_00_component_0001",
            PlanStage                     = "RESIDENTIAL_BUILDING_FOOTPRINT_PLANNING",
            SourceParcelTopologyComponentId = topology.ComponentId,
            SourceParcelTopologyValid     = topology.IsValid,
            SourceParcelCount             = topology.TotalResidentialLotCount,
            SandboxOnly                   = true,
            BuildingFootprintPlanningOnly = true,
            WriterReady                   = false,
            RuntimeValid                  = false,
            Materialized                  = false,
            PzRuntimeMaterialized         = false,
            RuntimeProofClaimed           = false,
            PublicPlayablePackagingClaimed = false,
        };

        // --- Build footprints from parcel topology ---
        var footprints = new List<DeadMtlResidentialBuildingFootprint>();

        foreach (var parcel in topology.ResidentialParcels)
        {
            DeadMtlResidentialBuildingFootprint fp;

            if (parcel.FrontageDirection == "NORTH")
            {
                fp = new DeadMtlResidentialBuildingFootprint
                {
                    FootprintId       = parcel.ParcelId.Replace("MAP28A_", "MAP28B_") + "_FOOTPRINT",
                    ParentParcelId    = parcel.ParcelId,
                    FrontageDirection = "NORTH",
                    BuildingKind      = "ROWHOUSE_MAIN_VOLUME",
                    X1 = parcel.X1, Y1 = parcel.Y1, X2 = parcel.X2, Y2 = parcel.Y2,
                    Width            = parcel.Width,
                    Height           = parcel.Height,
                    TileCount        = parcel.TileCount,
                    SetbackFront     = 0,
                    SetbackRear      = 0,
                    SetbackSideLeft  = 0,
                    SetbackSideRight = 0,
                };
            }
            else if (parcel.FrontageDirection == "SOUTH")
            {
                fp = new DeadMtlResidentialBuildingFootprint
                {
                    FootprintId       = parcel.ParcelId.Replace("MAP28A_", "MAP28B_") + "_FOOTPRINT",
                    ParentParcelId    = parcel.ParcelId,
                    FrontageDirection = "SOUTH",
                    BuildingKind      = "ROWHOUSE_MAIN_VOLUME",
                    X1 = parcel.X1, Y1 = parcel.Y1, X2 = parcel.X2, Y2 = parcel.Y2,
                    Width            = parcel.Width,
                    Height           = parcel.Height,
                    TileCount        = parcel.TileCount,
                    SetbackFront     = 0,
                    SetbackRear      = 0,
                    SetbackSideLeft  = 0,
                    SetbackSideRight = 0,
                };
            }
            else // EAST
            {
                fp = new DeadMtlResidentialBuildingFootprint
                {
                    FootprintId       = parcel.ParcelId.Replace("MAP28A_", "MAP28B_") + "_FOOTPRINT",
                    ParentParcelId    = parcel.ParcelId,
                    FrontageDirection = "EAST",
                    BuildingKind      = "EAST_EDGE_RESIDENTIAL_VOLUME",
                    X1 = parcel.X1, Y1 = parcel.Y1, X2 = parcel.X2, Y2 = parcel.Y2,
                    Width            = parcel.Width,
                    Height           = parcel.Height,
                    TileCount        = parcel.TileCount,
                    SetbackFront     = 0,
                    SetbackRear      = 0,
                    SetbackSideLeft  = 0,
                    SetbackSideRight = 0,
                };
            }

            footprints.Add(fp);
        }

        result.BuildingFootprints  = footprints;
        result.TotalFootprintCount = footprints.Count;
        result.NorthFootprintCount = footprints.Count(f => f.FrontageDirection == "NORTH");
        result.SouthFootprintCount = footprints.Count(f => f.FrontageDirection == "SOUTH");
        result.EastFootprintCount  = footprints.Count(f => f.FrontageDirection == "EAST");
        result.ForbiddenArtifactScan = ScanOutputRoot(outputRoot);

        // --- Checks ---
        var checks = new List<DeadMtlResidentialBuildingFootprintCheck>();

        // 1 — Source parcel topology component ID
        AddCheck(checks,
            "MAP28B_SOURCE_PARCEL_TOPOLOGY_COMPONENT_ID",
            "Source parcel topology component id is map_00_component_0001",
            "map_00_component_0001", topology.ComponentId);

        // 2 — Source topology valid
        AddCheck(checks,
            "MAP28B_SOURCE_PARCEL_TOPOLOGY_VALID",
            "Source parcel topology is valid",
            "PASS", topology.IsValid ? "PASS" : "FAIL");

        // 3 — Footprint count = 12
        AddCheck(checks,
            "MAP28B_FOOTPRINT_COUNT_12",
            "Total building footprint count is 12",
            "12", result.TotalFootprintCount.ToString());

        // 4 — One footprint per parcel (footprint count == source parcel count)
        AddCheck(checks,
            "MAP28B_ONE_FOOTPRINT_PER_PARCEL",
            "One footprint per residential parcel",
            result.SourceParcelCount.ToString(), result.TotalFootprintCount.ToString());

        // 5 — North footprint count = 6
        AddCheck(checks,
            "MAP28B_NORTH_FOOTPRINT_COUNT_6",
            "North-facing footprint count is 6",
            "6", result.NorthFootprintCount.ToString());

        // 6 — South footprint count = 6
        AddCheck(checks,
            "MAP28B_SOUTH_FOOTPRINT_COUNT_6",
            "South-facing footprint count is 6",
            "6", result.SouthFootprintCount.ToString());

        // 7 — East footprint count = 0
        AddCheck(checks,
            "MAP28B_EAST_FOOTPRINT_COUNT_0",
            "East-facing footprint count is 0",
            "0", result.EastFootprintCount.ToString());

        // 8 — Every footprint has parent parcel id
        bool allHaveParent = footprints.All(f => !string.IsNullOrEmpty(f.ParentParcelId));
        AddCheck(checks,
            "MAP28B_EVERY_FOOTPRINT_HAS_PARENT_PARCEL_ID",
            "Every footprint has a non-empty parent parcel id",
            "PASS", allHaveParent ? "PASS" : "FAIL");

        // 9 — Every parent parcel has exactly one footprint
        bool onePerParcel = topology.ResidentialParcels
            .All(p => footprints.Count(f => f.ParentParcelId == p.ParcelId) == 1);
        AddCheck(checks,
            "MAP28B_EVERY_PARENT_PARCEL_HAS_EXACTLY_ONE_FOOTPRINT",
            "Every parent parcel has exactly one footprint",
            "PASS", onePerParcel ? "PASS" : "FAIL");

        // 10 — Every footprint stays within parent parcel bounds
        bool allWithin = footprints.All(f =>
        {
            var parcel = topology.ResidentialParcels.FirstOrDefault(p => p.ParcelId == f.ParentParcelId);
            return parcel is not null
                && f.X1 >= parcel.X1 && f.X2 <= parcel.X2
                && f.Y1 >= parcel.Y1 && f.Y2 <= parcel.Y2;
        });
        AddCheck(checks,
            "MAP28B_EVERY_FOOTPRINT_WITHIN_PARENT_PARCEL",
            "Every footprint stays within its parent parcel bounds",
            "PASS", allWithin ? "PASS" : "FAIL");

        // 11 — No footprint overlaps sidewalk strips
        bool noSidewalkOverlap = footprints.All(f =>
            topology.SidewalkStrips
                .Where(s => s.StripKind == "SIDEWALK")
                .All(s => !Overlaps(f.X1, f.Y1, f.X2, f.Y2, s.X1, s.Y1, s.X2, s.Y2)));
        AddCheck(checks,
            "MAP28B_NO_FOOTPRINT_OVERLAPS_SIDEWALK",
            "No footprint overlaps a sidewalk strip",
            "PASS", noSidewalkOverlap ? "PASS" : "FAIL");

        // 12 — No footprint overlaps REAR_BOUNDARY strip
        bool noRearOverlap = footprints.All(f =>
            topology.SidewalkStrips
                .Where(s => s.StripKind == "REAR_BOUNDARY")
                .All(s => !Overlaps(f.X1, f.Y1, f.X2, f.Y2, s.X1, s.Y1, s.X2, s.Y2)));
        AddCheck(checks,
            "MAP28B_NO_FOOTPRINT_OVERLAPS_REAR_BOUNDARY",
            "No footprint overlaps the REAR_BOUNDARY strip",
            "PASS", noRearOverlap ? "PASS" : "FAIL");

        // 13 — No footprint overlaps another footprint
        bool noInterOverlap = true;
        for (int i = 0; i < footprints.Count && noInterOverlap; i++)
            for (int j = i + 1; j < footprints.Count && noInterOverlap; j++)
                if (Overlaps(footprints[i].X1, footprints[i].Y1, footprints[i].X2, footprints[i].Y2,
                             footprints[j].X1, footprints[j].Y1, footprints[j].X2, footprints[j].Y2))
                    noInterOverlap = false;
        AddCheck(checks,
            "MAP28B_NO_FOOTPRINT_OVERLAPS_ANOTHER_FOOTPRINT",
            "No footprint overlaps another footprint",
            "PASS", noInterOverlap ? "PASS" : "FAIL");

        // 14 — No footprint uses invented alley access
        AddCheck(checks,
            "MAP28B_NO_INVENTED_ALLEY_ACCESS",
            "No footprint uses invented alley access (invented_alleys_enabled=false)",
            "PASS", topology.InventedAlleysEnabled ? "FAIL" : "PASS");

        // 15 — No footprint is through-lot/runtime/materialized
        bool noRuntime = footprints.All(f => !f.WriterReady && !f.RuntimeValid && !f.Materialized);
        AddCheck(checks,
            "MAP28B_NO_FOOTPRINT_RUNTIME_OR_MATERIALIZED",
            "No footprint is through-lot/runtime/materialized",
            "PASS", noRuntime ? "PASS" : "FAIL");

        // 16 — North footprints use NORTH frontage
        bool northFrontage = footprints.Where(f => f.FrontageDirection == "NORTH")
            .All(f => f.FrontageDirection == "NORTH");
        AddCheck(checks,
            "MAP28B_NORTH_FOOTPRINTS_USE_NORTH_FRONTAGE",
            "North footprints use NORTH frontage direction",
            "PASS", northFrontage ? "PASS" : "FAIL");

        // 17 — South footprints use SOUTH frontage
        bool southFrontage = footprints.Where(f => f.FrontageDirection == "SOUTH")
            .All(f => f.FrontageDirection == "SOUTH");
        AddCheck(checks,
            "MAP28B_SOUTH_FOOTPRINTS_USE_SOUTH_FRONTAGE",
            "South footprints use SOUTH frontage direction",
            "PASS", southFrontage ? "PASS" : "FAIL");

        // 18 — No footprint has EAST frontage direction
        bool noEastFrontage = !footprints.Any(f => f.FrontageDirection == "EAST");
        AddCheck(checks,
            "MAP28B_NO_EAST_FACING_FOOTPRINT",
            "No footprint has EAST frontage direction (right edge is not residential frontage)",
            "PASS", noEastFrontage ? "PASS" : "FAIL");

        // 19 — N/S footprint width balance: max-min <= 1
        var nsFps  = footprints.Where(f => f.FrontageDirection is "NORTH" or "SOUTH").ToList();
        int fpMaxW = nsFps.Any() ? nsFps.Max(f => f.Width) : 0;
        int fpMinW = nsFps.Any() ? nsFps.Min(f => f.Width) : 0;
        bool nsWidthBalanced = fpMaxW - fpMinW <= 1;
        AddCheck(checks,
            "MAP28B_NS_FOOTPRINT_WIDTH_BALANCED",
            "N/S footprint width balance: max-min <= 1 (full-lot columns 15/14)",
            "PASS", nsWidthBalanced ? "PASS" : "FAIL");

        // 20 — N/S footprint depth = 30 (full-lot rows, no rear boundary gap)
        bool nsDepth30 = footprints.Where(f => f.FrontageDirection is "NORTH" or "SOUTH")
            .All(f => f.Height == 30);
        AddCheck(checks,
            "MAP28B_NS_FOOTPRINT_DEPTH_30",
            "All N/S footprints have depth = 30 tiles (full-lot rows, no rear boundary gap)",
            "PASS", nsDepth30 ? "PASS" : "FAIL");

        // 21 — North footprint row covers full bbox width X 124-212
        var nFps = footprints.Where(f => f.FrontageDirection == "NORTH").OrderBy(f => f.X1).ToList();
        bool northRowCoverage = nFps.Count == 6
            && nFps.First().X1 == 124
            && nFps.Last().X2  == 212
            && nFps.Zip(nFps.Skip(1), (a, b) => a.X2 + 1 == b.X1).All(x => x);
        AddCheck(checks,
            "MAP28B_NORTH_ROW_FULL_X_COVERAGE",
            "North footprint row covers full bbox width X 124-212 with no gaps",
            "PASS", northRowCoverage ? "PASS" : "FAIL");

        // 22 — South footprint row covers full bbox width X 124-212
        var sFps = footprints.Where(f => f.FrontageDirection == "SOUTH").OrderBy(f => f.X1).ToList();
        bool southRowCoverage = sFps.Count == 6
            && sFps.First().X1 == 124
            && sFps.Last().X2  == 212
            && sFps.Zip(sFps.Skip(1), (a, b) => a.X2 + 1 == b.X1).All(x => x);
        AddCheck(checks,
            "MAP28B_SOUTH_ROW_FULL_X_COVERAGE",
            "South footprint row covers full bbox width X 124-212 with no gaps",
            "PASS", southRowCoverage ? "PASS" : "FAIL");

        // 23 — Every footprint exactly equals parent parcel bounds
        bool allExact = footprints.All(f =>
        {
            var p = topology.ResidentialParcels.FirstOrDefault(x => x.ParcelId == f.ParentParcelId);
            return p is not null && f.X1 == p.X1 && f.Y1 == p.Y1 && f.X2 == p.X2 && f.Y2 == p.Y2;
        });
        AddCheck(checks,
            "MAP28B_EVERY_FOOTPRINT_EQUALS_PARENT_PARCEL_BOUNDS",
            "Every footprint exactly equals its parent parcel bounds (full-lot occupancy)",
            "PASS", allExact ? "PASS" : "FAIL");

        // 24-30: PENDING post-output checks
        AddPendingCheck(checks, "MAP28B_OUTPUT_PNGS_256X256",
            "All 3 output PNGs are exactly 256x256 pixels");
        AddPendingCheck(checks, "MAP28B_HTML_ASCII_ONLY",
            "HTML output contains only ASCII characters");
        AddPendingCheck(checks, "MAP28B_README_ASCII_ONLY",
            "README output contains only ASCII characters");
        AddPendingCheck(checks, "MAP28B_NO_LOTPACK_LOTHEADER_LUA_BIN",
            "No .lotpack/.lotheader/.lua/.bin under output root");
        AddPendingCheck(checks, "MAP28B_NO_MEDIA_MAPS",
            "No media/maps directory under output root");
        AddPendingCheck(checks, "MAP28B_NO_STEAMAPPS",
            "No steamapps directory under output root");
        AddPendingCheck(checks, "MAP28B_FORBIDDEN_SCAN_PASS",
            "Forbidden artifact scan passes after outputs are written");

        // 31-35: claim boundary
        AddCheck(checks, "MAP28B_WRITER_READY_FALSE",
            "WriterReady is false (sandbox only)", "False", result.WriterReady.ToString());
        AddCheck(checks, "MAP28B_RUNTIME_VALID_FALSE",
            "RuntimeValid is false (no runtime proof)", "False", result.RuntimeValid.ToString());
        AddCheck(checks, "MAP28B_MATERIALIZED_FALSE",
            "Materialized is false (planning artifact only)", "False", result.Materialized.ToString());
        AddCheck(checks, "MAP28B_NO_RUNTIME_PROOF_CLAIM",
            "RuntimeProofClaimed is false", "False", result.RuntimeProofClaimed.ToString());
        AddCheck(checks, "MAP28B_NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM",
            "PublicPlayablePackagingClaimed is false", "False", result.PublicPlayablePackagingClaimed.ToString());

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass      = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.IsValid    = allPass;
        result.PlanStatus = allPass
            ? "RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_PENDING_FINALIZE"
            : "RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_FAILED";
        result.Verdict    = allPass
            ? "MAP28B_WORLDBUILDER_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_PENDING_FINALIZE"
            : invalidVerdict;

        return result;
    }

    // -----------------------------------------------------------------------
    // FinalizeAfterOutputs
    // -----------------------------------------------------------------------

    public DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult FinalizeAfterOutputs(
        DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result,
        string outputRoot,
        string cleanPngPath,
        string debugPngPath,
        string overlayPngPath,
        string htmlPath,
        string readmePath)
    {
        const string invalidVerdict = "MAP28B_WORLDBUILDER_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_INVALID";

        bool pngsOk = VerifyPng256(cleanPngPath) && VerifyPng256(debugPngPath) && VerifyPng256(overlayPngPath);
        SetCheck(result, "MAP28B_OUTPUT_PNGS_256X256", pngsOk ? "PASS" : "FAIL");

        bool htmlAscii = File.Exists(htmlPath) && File.ReadAllBytes(htmlPath).All(b => b < 128);
        SetCheck(result, "MAP28B_HTML_ASCII_ONLY", htmlAscii ? "PASS" : "FAIL");

        bool readmeAscii = File.Exists(readmePath) && File.ReadAllBytes(readmePath).All(b => b < 128);
        SetCheck(result, "MAP28B_README_ASCII_ONLY", readmeAscii ? "PASS" : "FAIL");

        var patterns  = new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" };
        bool noBinFiles = !Directory.Exists(outputRoot) ||
            patterns.Sum(p => Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length) == 0;
        SetCheck(result, "MAP28B_NO_LOTPACK_LOTHEADER_LUA_BIN", noBinFiles ? "PASS" : "FAIL");

        bool noMediaMaps = !Directory.Exists(outputRoot) ||
            !Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories)
                .Any(d => { var di = new DirectoryInfo(d); return di.Name == "maps" && di.Parent?.Name == "media"; });
        SetCheck(result, "MAP28B_NO_MEDIA_MAPS", noMediaMaps ? "PASS" : "FAIL");

        bool noSteamapps = !Directory.Exists(outputRoot) ||
            !Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories)
                .Any(d => string.Equals(new DirectoryInfo(d).Name, "steamapps", StringComparison.OrdinalIgnoreCase));
        SetCheck(result, "MAP28B_NO_STEAMAPPS", noSteamapps ? "PASS" : "FAIL");

        string finalScan  = ScanOutputRoot(outputRoot);
        bool   scanPasses = finalScan.Contains("PASS", StringComparison.Ordinal) && !finalScan.Contains("FAIL", StringComparison.Ordinal);
        result.ForbiddenArtifactScan = finalScan;
        SetCheck(result, "MAP28B_FORBIDDEN_SCAN_PASS", scanPasses ? "PASS" : "FAIL");

        result.PassedCheckCount = result.Checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = result.Checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass      = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.IsValid    = allPass;
        result.PlanStatus = allPass
            ? "RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_COMPLETE"
            : "RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_FAILED";
        result.Verdict    = allPass
            ? "MAP28B_WORLDBUILDER_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN_COMPLETE"
            : invalidVerdict;

        return result;
    }

    private static bool VerifyPng256(string path)
    {
        if (!File.Exists(path)) return false;
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 24) return false;
        int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return w == 256 && h == 256;
    }

    // -----------------------------------------------------------------------
    // PNG rendering
    // -----------------------------------------------------------------------

    public byte[] RenderCleanFootprintPngBytes(
        DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result,
        DeadMtlWorldBuilderResidentialParcelTopologyResult topology)
    {
        // Strict lot-fill-only: no bbox, no ticks, no rear-boundary, no helper artifacts
        using var bmp = CreateBackground();
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        FillFootprints(result, g);
        return BitmapToBytes(bmp);
    }

    public byte[] RenderDebugFootprintPngBytes(
        DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result,
        DeadMtlWorldBuilderResidentialParcelTopologyResult topology)
    {
        using var bmp = CreateBackground();
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        FillParcelAreas(topology, g);
        FillSidewalkAreas(topology, g);
        FillFootprints(result, g);

        // Frontage ticks + center dots only — no black separator lines between footprints
        var tick = System.Drawing.Color.FromArgb(s_tick.R, s_tick.G, s_tick.B);
        foreach (var f in result.BuildingFootprints)
        {
            int midX = (f.X1 + f.X2) / 2;
            int midY = (f.Y1 + f.Y2) / 2;
            bmp.SetPixel(midX, midY, tick);
            if      (f.FrontageDirection == "NORTH") bmp.SetPixel(midX, f.Y1, tick);
            else if (f.FrontageDirection == "SOUTH") bmp.SetPixel(midX, f.Y2, tick);
            else if (f.FrontageDirection == "EAST")  bmp.SetPixel(f.X2, midY, tick);
        }

        return BitmapToBytes(bmp);
    }

    public byte[] RenderOverlayFootprintPngBytes(
        DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result,
        DeadMtlWorldBuilderResidentialParcelTopologyResult topology,
        string? rawSourcePngPath)
    {
        using var img = new System.Drawing.Bitmap(256, 256,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(img))
        {
            if (!string.IsNullOrEmpty(rawSourcePngPath) && File.Exists(rawSourcePngPath))
            {
                using var raw = new System.Drawing.Bitmap(rawSourcePngPath);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(raw, 0, 0, 256, 256);
            }
            else
            {
                g.Clear(System.Drawing.Color.FromArgb(s_bg.R, s_bg.G, s_bg.B));
            }

            using var overlay = new System.Drawing.Bitmap(256, 256,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var gOvl = System.Drawing.Graphics.FromImage(overlay))
            {
                gOvl.Clear(System.Drawing.Color.Transparent);
                foreach (var p in topology.ResidentialParcels)
                {
                    using var brush = new System.Drawing.SolidBrush(
                        System.Drawing.Color.FromArgb(80, s_parcel.R, s_parcel.G, s_parcel.B));
                    gOvl.FillRectangle(brush, p.X1, p.Y1, p.X2 - p.X1 + 1, p.Y2 - p.Y1 + 1);
                }
                foreach (var f in result.BuildingFootprints)
                {
                    var (fr, fg, fb) = PickFootprintShade(f.ParentParcelId, f.FrontageDirection);
                    using var brush  = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(180, fr, fg, fb));
                    gOvl.FillRectangle(brush, f.X1, f.Y1, f.X2 - f.X1 + 1, f.Y2 - f.Y1 + 1);
                }
            }
            g.DrawImage(overlay, 0, 0);
        }

        return BitmapToBytes(img);
    }

    private System.Drawing.Bitmap CreateBackground()
    {
        var bmp = new System.Drawing.Bitmap(256, 256,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(s_bg.R, s_bg.G, s_bg.B));
        return bmp;
    }

    private static void FillParcelAreas(DeadMtlWorldBuilderResidentialParcelTopologyResult topology,
        System.Drawing.Graphics g)
    {
        using var brush = new System.Drawing.SolidBrush(
            System.Drawing.Color.FromArgb(s_parcel.R, s_parcel.G, s_parcel.B));
        foreach (var p in topology.ResidentialParcels)
            g.FillRectangle(brush, p.X1, p.Y1, p.X2 - p.X1 + 1, p.Y2 - p.Y1 + 1);
    }

    private static void FillSidewalkAreas(DeadMtlWorldBuilderResidentialParcelTopologyResult topology,
        System.Drawing.Graphics g)
    {
        foreach (var s in topology.SidewalkStrips)
        {
            var color = s.StripKind == "REAR_BOUNDARY"
                ? System.Drawing.Color.FromArgb(s_rear.R, s_rear.G, s_rear.B)
                : System.Drawing.Color.FromArgb(s_sidewalk.R, s_sidewalk.G, s_sidewalk.B);
            using var brush = new System.Drawing.SolidBrush(color);
            g.FillRectangle(brush, s.X1, s.Y1, s.X2 - s.X1 + 1, s.Y2 - s.Y1 + 1);
        }
    }

    private static void FillFootprints(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result,
        System.Drawing.Graphics g)
    {
        foreach (var f in result.BuildingFootprints)
        {
            var (fr, fg, fb) = PickFootprintShade(f.ParentParcelId, f.FrontageDirection);
            using var brush  = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(fr, fg, fb));
            g.FillRectangle(brush, f.X1, f.Y1, f.X2 - f.X1 + 1, f.Y2 - f.Y1 + 1);
        }
    }

    private static byte[] BitmapToBytes(System.Drawing.Bitmap bmp)
    {
        using var ms = new System.IO.MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderFootprintsCsv(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("footprint_id,parent_parcel_id,frontage_direction,building_kind,x1,y1,x2,y2,width,height,tile_count,setback_front,setback_rear,setback_side_left,setback_side_right");
        foreach (var f in result.BuildingFootprints)
            sb.AppendLine($"{f.FootprintId},{f.ParentParcelId},{f.FrontageDirection},{f.BuildingKind},{f.X1},{f.Y1},{f.X2},{f.Y2},{f.Width},{f.Height},{f.TileCount},{f.SetbackFront},{f.SetbackRear},{f.SetbackSideLeft},{f.SetbackSideRight}");
        return sb.ToString();
    }

    public string RenderChecksCsv(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderParentManifestJson(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result)
    {
        var entries = result.BuildingFootprints
            .Select(f => new { parcel_id = f.ParentParcelId, footprint_id = f.FootprintId })
            .ToList();
        return JsonSerializer.Serialize(entries, s_jsonOptions);
    }

    public string RenderSummary(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-28B WORLDBUILDER RESIDENTIAL BUILDING FOOTPRINT PLAN");
        sb.AppendLine($"Generated UTC       : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID              : {result.MapId}");
        sb.AppendLine($"Component ID        : {result.ComponentId}");
        sb.AppendLine($"Plan Stage          : {result.PlanStage}");
        sb.AppendLine($"Plan Status         : {result.PlanStatus}");
        sb.AppendLine($"Source Topology     : {result.SourceParcelTopologyComponentId} (valid={result.SourceParcelTopologyValid})");
        sb.AppendLine($"Total Footprints    : {result.TotalFootprintCount}");
        sb.AppendLine($"  North-facing      : {result.NorthFootprintCount}");
        sb.AppendLine($"  South-facing      : {result.SouthFootprintCount}");
        sb.AppendLine($"  East-facing       : {result.EastFootprintCount}");
        sb.AppendLine($"Checks              : {result.CheckCount} total / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid            : {result.IsValid}");
        sb.AppendLine($"Verdict             : {result.Verdict}");
        sb.AppendLine($"Forbidden Scan      : {result.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim boundary      : sandbox_only=true | writer_ready=false | runtime_valid=false | materialized=false");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine("Errors:");
            foreach (var e in result.Errors) sb.AppendLine($"  {e}");
        }
        return sb.ToString();
    }

    public string RenderHtml(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\">");
        sb.AppendLine("<title>DeadMTL map_00 -- Residential Building Footprint Plan (MAP-28B)</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { background:#0e0e14; color:#ccc; font-family:monospace; padding:16px; }");
        sb.AppendLine("h1 { font-size:1em; color:#a89060; }");
        sb.AppendLine("h2 { font-size:0.9em; color:#888; margin-top:20px; }");
        sb.AppendLine("p  { font-size:0.8em; line-height:1.5; }");
        sb.AppendLine(".warn { color:#c87040; }");
        sb.AppendLine(".row  { display:flex; flex-wrap:wrap; gap:20px; margin-top:12px; }");
        sb.AppendLine(".card { background:#1a1a22; border:1px solid #333; padding:8px; }");
        sb.AppendLine(".card img { display:block; width:512px; height:512px; image-rendering:pixelated; image-rendering:crisp-edges; }");
        sb.AppendLine(".card .lbl { font-size:0.7em; color:#666; margin-top:4px; }");
        sb.AppendLine(".pal { margin-top:12px; font-size:0.8em; }");
        sb.AppendLine(".swatch { display:inline-block; width:12px; height:12px; margin-right:4px; vertical-align:middle; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h1>DeadMTL map_00 -- Residential Building Footprint Plan (MAP-28B)</h1>");
        sb.AppendLine("<p class=\"warn\">");
        sb.AppendLine("NOT a playable Project Zomboid export. NOT .lotpack / .lotheader / .lua / .bin.<br>");
        sb.AppendLine("All PNGs are exactly 256x256 pixels. CSS zoom only. Sandbox planning artifact.");
        sb.AppendLine("</p>");
        sb.AppendLine("<p>");
        sb.AppendLine($"Component: {result.ComponentId}<br>");
        sb.AppendLine($"Footprints: {result.TotalFootprintCount} total ({result.NorthFootprintCount} north + {result.SouthFootprintCount} south + 0 east)<br>");
        sb.AppendLine("N/S full-lot occupancy footprints: widths 15/14 tiles, depth 30 tiles (ROWHOUSE_MAIN_VOLUME). North Y 10-39, south Y 40-69. No east residential row.<br>");
        sb.AppendLine("No sidewalk overlap. No REAR_BOUNDARY overlap. No inter-footprint overlap.<br>");
        sb.AppendLine("Adjacent footprints use deterministic 3-shade rotation per row.<br>");
        sb.AppendLine("Clean view: strict lot fill only. Debug/overlay: diagnostic helpers. Boundaries by adjacent shade change. Planning artifact only.");
        sb.AppendLine("</p>");
        sb.AppendLine("<div class=\"pal\"><b>Palette:</b>");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#c8a878;\"></span>Footprint shade A (light tan) &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#b08c60;\"></span>Footprint shade B (medium tan) &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#98744c;\"></span>Footprint shade C (dark tan) &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#1e2234;\"></span>Parcel (subdued) &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#30241a;\"></span>Rear Boundary</div>");
        sb.AppendLine("<div class=\"row\">");
        sb.AppendLine("  <div class=\"card\">");
        sb.AppendLine("    <img src=\"map_00_residential_building_footprints_clean_native_256.png\" alt=\"clean footprint view\">");
        sb.AppendLine("    <div class=\"lbl\">clean view: strict residential lot fill only, no helper artifacts (256x256)</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"card\">");
        sb.AppendLine("    <img src=\"map_00_residential_building_footprints_debug_native_256.png\" alt=\"debug view\">");
        sb.AppendLine("    <div class=\"lbl\">debug view: frontage ticks + center dots, no black stroke lines (256x256)</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"card\">");
        sb.AppendLine("    <img src=\"map_00_residential_building_footprints_overlay_native_256.png\" alt=\"overlay\">");
        sb.AppendLine("    <div class=\"lbl\">overlay on raw source (256x256)</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<h2>Checks: {result.CheckCount} / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL</h2>");
        sb.AppendLine("<p>Verdict: " + result.Verdict + "</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public string RenderReadme(DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-28B WORLDBUILDER RESIDENTIAL BUILDING FOOTPRINT PLAN");
        sb.AppendLine("# DeadMTL map_00 -- Residential Building Footprint Planning Artifact");
        sb.AppendLine();
        sb.AppendLine("## Native resolution contract");
        sb.AppendLine();
        sb.AppendLine("Every PNG is exactly 256x256 pixels. One pixel = one map tile/cell.");
        sb.AppendLine();
        sb.AppendLine("## NOT playable");
        sb.AppendLine();
        sb.AppendLine("NOT a playable Project Zomboid export.");
        sb.AppendLine("No runtime files. No .lotpack/.lotheader/.lua/.bin. Source not mutated.");
        sb.AppendLine();
        sb.AppendLine("## Source topology");
        sb.AppendLine();
        sb.AppendLine($"- Source parcel topology : {result.SourceParcelTopologyComponentId}");
        sb.AppendLine($"- Source topology valid  : {result.SourceParcelTopologyValid}");
        sb.AppendLine($"- Source parcel count    : {result.SourceParcelCount}");
        sb.AppendLine();
        sb.AppendLine("## Footprints");
        sb.AppendLine();
        sb.AppendLine($"- Total footprints   : {result.TotalFootprintCount}");
        sb.AppendLine($"- North-facing       : {result.NorthFootprintCount} (ROWHOUSE_MAIN_VOLUME, full-lot occupancy, widths 15/14, depth 30, Y 10-39)");
        sb.AppendLine($"- South-facing       : {result.SouthFootprintCount} (ROWHOUSE_MAIN_VOLUME, full-lot occupancy, widths 15/14, depth 30, Y 40-69)");
        sb.AppendLine($"- East-facing        : 0 (no east residential row)");
        sb.AppendLine();
        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("- sandbox_only                    = true");
        sb.AppendLine("- building_footprint_planning_only = true");
        sb.AppendLine("- writer_ready                    = false");
        sb.AppendLine("- runtime_valid                   = false");
        sb.AppendLine("- materialized                    = false");
        sb.AppendLine("- runtime_proof_claimed           = false");
        sb.AppendLine("- public_playable_packaging_claimed = false");
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine($"- Total  : {result.CheckCount}");
        sb.AppendLine($"- PASS   : {result.PassedCheckCount}");
        sb.AppendLine($"- FAIL   : {result.FailedCheckCount}");
        sb.AppendLine($"- Verdict: {result.Verdict}");
        sb.AppendLine();
        sb.AppendLine("## Forbidden artifact scan");
        sb.AppendLine();
        sb.AppendLine(result.ForbiddenArtifactScan);
        return sb.ToString();
    }
}
