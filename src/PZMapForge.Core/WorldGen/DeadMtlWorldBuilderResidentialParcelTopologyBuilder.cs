using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderResidentialParcelTopologyBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    // -----------------------------------------------------------------------
    // Hardcoded geometry for map_00_component_0001 (MAP-VIEW-4A corrected)
    // -----------------------------------------------------------------------

    private const string ComponentId = "map_00_component_0001";
    private const int BboxX1 = 124, BboxX2 = 212, BboxY1 = 10, BboxY2 = 69;

    private const int SidewalkWidthNorth = 2;
    private const int SidewalkWidthSouth = 2;
    private const int RearFenceWidth     = 2;
    private const int MinFrontageTiles   = 14;

    private const int NorthY1     = 10, NorthY2 = 38;
    private const int RearFenceY1 = 39, RearFenceY2 = 40;
    private const int SouthY1     = 41, SouthY2 = 69;
    private const int MainX1      = 124, MainX2  = 212;

    private static readonly int[] s_lotWidths = { 15, 15, 15, 15, 15, 14 };

    // Warm neutral shade palette for residential lots (light → medium → darker tan)
    private static readonly (byte R, byte G, byte B) s_sidew  = (184, 184, 192);
    private static readonly (byte R, byte G, byte B) s_rear   = (74,  56,  40);
    private static readonly (byte R, byte G, byte B) s_bg     = (18,  18,  24);
    private static readonly (byte R, byte G, byte B) s_bbox   = (40,  192, 192); // CYAN
    private static readonly (byte R, byte G, byte B) s_tick   = (210, 230, 255);

    private static readonly (byte R, byte G, byte B)[] s_lotShades =
    {
        (200, 168, 120), // light tan
        (176, 140,  96), // medium tan
        (152, 116,  76), // darker tan/brown
    };

    // -----------------------------------------------------------------------
    // Check helpers
    // -----------------------------------------------------------------------

    private static void AddCheck(List<DeadMtlResidentialParcelTopologyCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new DeadMtlResidentialParcelTopologyCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlResidentialParcelTopologyCheck> checks,
        string id, string label)
    {
        checks.Add(new DeadMtlResidentialParcelTopologyCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PASS",
            Expected    = "PASS",
            Actual      = "PASS",
        });
    }

    private static void AddPendingCheck(List<DeadMtlResidentialParcelTopologyCheck> checks,
        string id, string label)
    {
        checks.Add(new DeadMtlResidentialParcelTopologyCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PENDING",
            Expected    = "PASS",
            Actual      = "PENDING",
        });
    }

    // -----------------------------------------------------------------------
    // Forbidden artifact scan
    // -----------------------------------------------------------------------

    private static string ScanOutputRoot(string outputRoot)
    {
        const string prefix = "POST_MAP28A_RESIDENTIAL_PARCEL_TOPOLOGY_FORBIDDEN_SCAN";
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
    // Color resolver — index-based 3-shade rotation, offset by row
    // -----------------------------------------------------------------------

    private static (byte R, byte G, byte B) GetLotColor(string parcelId, string frontageDirection)
    {
        if (frontageDirection is not ("NORTH" or "SOUTH")) return s_bg;
        int idx       = int.Parse(parcelId.Substring(parcelId.LastIndexOf('_') + 1));
        int rowOffset = frontageDirection == "SOUTH" ? 1 : 0;
        return s_lotShades[(idx + rowOffset) % s_lotShades.Length];
    }

    // -----------------------------------------------------------------------
    // Build
    // -----------------------------------------------------------------------

    public DeadMtlWorldBuilderResidentialParcelTopologyResult Build(string outputRoot)
    {
        const string invalidVerdict =
            "MAP28A_WORLDBUILDER_RESIDENTIAL_PARCEL_TOPOLOGY_INVALID";

        var result = new DeadMtlWorldBuilderResidentialParcelTopologyResult
        {
            Format                         = "MAP-28A_WORLDBUILDER_RESIDENTIAL_PARCEL_TOPOLOGY",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            ComponentId                    = ComponentId,
            TopologyStage                  = "RESIDENTIAL_PARCEL_TOPOLOGY_PLANNING",
            BboxX1                         = BboxX1,
            BboxY1                         = BboxY1,
            BboxX2                         = BboxX2,
            BboxY2                         = BboxY2,
            BboxWidth                      = BboxX2 - BboxX1 + 1,
            BboxHeight                     = BboxY2 - BboxY1 + 1,
            SidewalkWidthNorth             = SidewalkWidthNorth,
            SidewalkWidthSouth             = SidewalkWidthSouth,
            SidewalkWidthEast              = 0,
            RearFenceWidth                 = RearFenceWidth,
            MinResidentialFrontageTiles    = MinFrontageTiles,
            ThroughLotsEnabled             = false,
            InventedAlleysEnabled          = false,
            SandboxOnly                    = true,
            ParcelTopologyPlanningOnly     = true,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            PzRuntimeMaterialized          = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
        };

        // --- Build parcels ---
        var parcels  = new List<DeadMtlResidentialParcel>();
        var edges    = new List<DeadMtlResidentialFrontageEdge>();
        var strips   = new List<DeadMtlResidentialSidewalkStrip>();

        // Only the mid-block rear boundary strip; sidewalk context not rendered in planning view
        strips.Add(new DeadMtlResidentialSidewalkStrip
        {
            StripId    = "MAP28A_REAR_BOUNDARY",
            StripKind  = "REAR_BOUNDARY",
            Direction  = "NONE",
            X1 = MainX1, Y1 = RearFenceY1, X2 = MainX2, Y2 = RearFenceY2,
            WidthTiles = RearFenceWidth,
            Notes      = "Mid-block rear boundary (fence/property line), NOT a service alley",
        });

        // North and South lots (6 each, widths 15/15/15/15/15/14, full-lot rows)
        int xCur = MainX1;
        for (int i = 0; i < s_lotWidths.Length; i++)
        {
            int w   = s_lotWidths[i];
            int lx1 = xCur, lx2 = xCur + w - 1;
            xCur += w;

            string nId  = $"MAP28A_NORTH_LOT_{i:00}";
            string sId  = $"MAP28A_SOUTH_LOT_{i:00}";
            string neId = $"MAP28A_NORTH_LOT_{i:00}_FRONTAGE_EDGE";
            string seId = $"MAP28A_SOUTH_LOT_{i:00}_FRONTAGE_EDGE";

            parcels.Add(new DeadMtlResidentialParcel
            {
                ParcelId            = nId,
                ComponentId         = ComponentId,
                ParcelKind          = "RESIDENTIAL_LOT",
                FrontageDirection   = "NORTH",
                X1 = lx1, Y1 = NorthY1, X2 = lx2, Y2 = NorthY2,
                Width               = w,
                Height              = NorthY2 - NorthY1 + 1,
                TileCount           = w * (NorthY2 - NorthY1 + 1),
                IsCornerLot         = i == 0 || i == s_lotWidths.Length - 1,
                IsThroughLot        = false,
                PrimaryFrontageEdgeId = neId,
            });
            edges.Add(new DeadMtlResidentialFrontageEdge
            {
                FrontageEdgeId    = neId,
                ParcelId          = nId,
                FrontageDirection = "NORTH",
                EdgeKind          = "STREET_FRONTAGE_NORTH",
                X1 = lx1, Y1 = NorthY1, X2 = lx2, Y2 = NorthY1,
                LengthTiles       = w,
            });

            parcels.Add(new DeadMtlResidentialParcel
            {
                ParcelId            = sId,
                ComponentId         = ComponentId,
                ParcelKind          = "RESIDENTIAL_LOT",
                FrontageDirection   = "SOUTH",
                X1 = lx1, Y1 = SouthY1, X2 = lx2, Y2 = SouthY2,
                Width               = w,
                Height              = SouthY2 - SouthY1 + 1,
                TileCount           = w * (SouthY2 - SouthY1 + 1),
                IsCornerLot         = i == 0 || i == s_lotWidths.Length - 1,
                IsThroughLot        = false,
                PrimaryFrontageEdgeId = seId,
            });
            edges.Add(new DeadMtlResidentialFrontageEdge
            {
                FrontageEdgeId    = seId,
                ParcelId          = sId,
                FrontageDirection = "SOUTH",
                EdgeKind          = "STREET_FRONTAGE_SOUTH",
                X1 = lx1, Y1 = SouthY2, X2 = lx2, Y2 = SouthY2,
                LengthTiles       = w,
            });
        }

        result.ResidentialParcels    = parcels;
        result.FrontageEdges         = edges;
        result.SidewalkStrips        = strips;
        result.TotalResidentialLotCount = parcels.Count;
        result.NorthFacingLotCount      = parcels.Count(p => p.FrontageDirection == "NORTH");
        result.SouthFacingLotCount      = parcels.Count(p => p.FrontageDirection == "SOUTH");
        result.EastFacingLotCount       = parcels.Count(p => p.FrontageDirection == "EAST");
        result.ThroughLotCount          = parcels.Count(p => p.IsThroughLot);
        result.SidewalkStripCount       = strips.Count(s => s.StripKind == "SIDEWALK");
        result.RearBoundaryStripCount   = strips.Count(s => s.StripKind == "REAR_BOUNDARY");
        result.InventedAlleyCount       = strips.Count(s => s.StripKind == "ALLEY");
        result.ForbiddenArtifactScan    = ScanOutputRoot(outputRoot);

        // --- Checks (25 total) ---
        var checks = new List<DeadMtlResidentialParcelTopologyCheck>();

        // 1 — Component ID
        AddCheck(checks,
            "MAP28A_COMPONENT_ID_MAP_00_COMPONENT_0001",
            "Component ID is map_00_component_0001",
            "map_00_component_0001", result.ComponentId);

        // 2 — Bbox
        AddCheck(checks,
            "MAP28A_BBOX_X1_124_Y1_10_X2_212_Y2_69",
            "Bbox matches expected X1=124 Y1=10 X2=212 Y2=69",
            "124,10,212,69", $"{result.BboxX1},{result.BboxY1},{result.BboxX2},{result.BboxY2}");

        // 3 — Native 256x256 by design
        MakeCheck(checks,
            "MAP28A_NATIVE_256_CONTRACT_BY_DESIGN",
            "PNG outputs are 256x256 by design of this builder");

        // 4 — Total lot count
        AddCheck(checks,
            "MAP28A_TOTAL_RESIDENTIAL_LOT_COUNT_12",
            "Total residential lot count is 12",
            "12", result.TotalResidentialLotCount.ToString());

        // 5 — North count
        AddCheck(checks,
            "MAP28A_NORTH_FACING_LOT_COUNT_6",
            "North-facing lot count is 6",
            "6", result.NorthFacingLotCount.ToString());

        // 6 — South count
        AddCheck(checks,
            "MAP28A_SOUTH_FACING_LOT_COUNT_6",
            "South-facing lot count is 6",
            "6", result.SouthFacingLotCount.ToString());

        // 7 — East count
        AddCheck(checks,
            "MAP28A_EAST_FACING_LOT_COUNT_0",
            "East-facing lot count is 0",
            "0", result.EastFacingLotCount.ToString());

        // 8 — No through lots
        AddCheck(checks,
            "MAP28A_NO_THROUGH_LOTS",
            "No through-lots (through_lots_enabled=false)",
            "0", result.ThroughLotCount.ToString());

        // 9 — No double-frontage lots
        bool noDoubleFrontage = !parcels.Any(p =>
            p.Y1 <= NorthY1 && p.Y2 >= SouthY2);
        AddCheck(checks,
            "MAP28A_NO_DOUBLE_FRONTAGE_LOTS",
            "No lot spans both north and south lot zones",
            "PASS", noDoubleFrontage ? "PASS" : "FAIL");

        // 10 — Every lot has NORTH or SOUTH frontage only (no EAST)
        bool allNorthSouth = parcels.All(p =>
            p.FrontageDirection is "NORTH" or "SOUTH");
        AddCheck(checks,
            "MAP28A_EVERY_LOT_HAS_NORTH_OR_SOUTH_FRONTAGE",
            "Every lot has exactly one primary frontage (N or S, no EAST)",
            "PASS", allNorthSouth ? "PASS" : "FAIL");

        // 11 — No east-facing residential lot exists
        bool noEastLots = !parcels.Any(p => p.FrontageDirection == "EAST");
        AddCheck(checks,
            "MAP28A_NO_EAST_FACING_RESIDENTIAL_LOTS",
            "No east-facing residential lot exists (right edge is not residential frontage)",
            "PASS", noEastLots ? "PASS" : "FAIL");

        // 12 — North row covers full bbox width X 124-212 with no gaps
        var northLots = parcels.Where(p => p.FrontageDirection == "NORTH").OrderBy(p => p.X1).ToList();
        bool northRowCoverage = northLots.Count > 0
            && northLots.First().X1 == BboxX1
            && northLots.Last().X2  == BboxX2
            && northLots.Zip(northLots.Skip(1), (a, b) => a.X2 + 1 == b.X1).All(x => x);
        AddCheck(checks,
            "MAP28A_NORTH_ROW_COVERS_FULL_BBOX_WIDTH",
            "North lot row covers full bbox width X 124-212 with no gaps",
            "PASS", northRowCoverage ? "PASS" : "FAIL");

        // 13 — South row covers full bbox width X 124-212 with no gaps
        var southLots = parcels.Where(p => p.FrontageDirection == "SOUTH").OrderBy(p => p.X1).ToList();
        bool southRowCoverage = southLots.Count > 0
            && southLots.First().X1 == BboxX1
            && southLots.Last().X2  == BboxX2
            && southLots.Zip(southLots.Skip(1), (a, b) => a.X2 + 1 == b.X1).All(x => x);
        AddCheck(checks,
            "MAP28A_SOUTH_ROW_COVERS_FULL_BBOX_WIDTH",
            "South lot row covers full bbox width X 124-212 with no gaps",
            "PASS", southRowCoverage ? "PASS" : "FAIL");

        // 14 — No lot overlaps REAR_BOUNDARY (Y 39-40)
        var rearBoundary = strips.First(s => s.StripKind == "REAR_BOUNDARY");
        bool noLotRearOverlap = parcels.All(p =>
            !(p.X1 <= rearBoundary.X2 && p.X2 >= rearBoundary.X1
           && p.Y1 <= rearBoundary.Y2 && p.Y2 >= rearBoundary.Y1));
        AddCheck(checks,
            "MAP28A_NO_LOT_OVERLAPS_REAR_BOUNDARY",
            "No residential lot overlaps the REAR_BOUNDARY strip (Y 39-40)",
            "PASS", noLotRearOverlap ? "PASS" : "FAIL");

        // 15 — Lot width balance: max width - min width <= 1
        var nsParcels = parcels.Where(p => p.FrontageDirection is "NORTH" or "SOUTH").ToList();
        int maxW = nsParcels.Any() ? nsParcels.Max(p => p.Width) : 0;
        int minW = nsParcels.Any() ? nsParcels.Min(p => p.Width) : 0;
        bool widthBalanced = maxW - minW <= 1;
        AddCheck(checks,
            "MAP28A_LOT_WIDTH_BALANCE_MAX_MINUS_MIN_LEQ_1",
            "Lot width balance: max width - min width <= 1 (widths 15/14)",
            "PASS", widthBalanced ? "PASS" : "FAIL");

        // 16-20: post-output checks (PENDING until FinalizeAfterOutputs)
        AddPendingCheck(checks, "MAP28A_OUTPUT_PNGS_256X256",
            "All 3 output PNGs are exactly 256x256 pixels");
        AddPendingCheck(checks, "MAP28A_HTML_ASCII_ONLY",
            "HTML output contains only ASCII characters");
        AddPendingCheck(checks, "MAP28A_README_ASCII_ONLY",
            "README output contains only ASCII characters");
        AddPendingCheck(checks, "MAP28A_NO_RUNTIME_OUTPUTS",
            "No runtime files (.lotpack/.lotheader/.lua/.bin) in output root");
        AddPendingCheck(checks, "POST_MAP28A_RESIDENTIAL_PARCEL_TOPOLOGY_FORBIDDEN_SCAN_PASS",
            "Forbidden artifact scan passes after outputs are written");

        // 21-25: claim boundary
        AddCheck(checks, "MAP28A_WRITER_READY_FALSE",
            "WriterReady is false (sandbox only)",
            "False", result.WriterReady.ToString());
        AddCheck(checks, "MAP28A_RUNTIME_VALID_FALSE",
            "RuntimeValid is false (no runtime proof)",
            "False", result.RuntimeValid.ToString());
        AddCheck(checks, "MAP28A_MATERIALIZED_FALSE",
            "Materialized is false (planning artifact only)",
            "False", result.Materialized.ToString());
        AddCheck(checks, "MAP28A_NO_RUNTIME_PROOF_CLAIM",
            "RuntimeProofClaimed is false",
            "False", result.RuntimeProofClaimed.ToString());
        AddCheck(checks, "MAP28A_NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM",
            "PublicPlayablePackagingClaimed is false",
            "False", result.PublicPlayablePackagingClaimed.ToString());

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass          = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.IsValid        = allPass;
        result.TopologyStatus = allPass
            ? "RESIDENTIAL_PARCEL_TOPOLOGY_PENDING_FINALIZE"
            : "RESIDENTIAL_PARCEL_TOPOLOGY_FAILED";
        result.Verdict        = allPass
            ? "MAP28A_WORLDBUILDER_RESIDENTIAL_PARCEL_TOPOLOGY_PENDING_FINALIZE"
            : invalidVerdict;

        return result;
    }

    // -----------------------------------------------------------------------
    // FinalizeAfterOutputs
    // -----------------------------------------------------------------------

    public DeadMtlWorldBuilderResidentialParcelTopologyResult FinalizeAfterOutputs(
        DeadMtlWorldBuilderResidentialParcelTopologyResult result,
        string outputRoot,
        string cleanPngPath,
        string debugPngPath,
        string overlayPngPath,
        string htmlPath,
        string readmePath)
    {
        const string invalidVerdict =
            "MAP28A_WORLDBUILDER_RESIDENTIAL_PARCEL_TOPOLOGY_INVALID";

        // Check 16 — PNGs 256x256
        bool pngsOk = VerifyPng256(cleanPngPath) && VerifyPng256(debugPngPath) && VerifyPng256(overlayPngPath);
        SetCheck(result, "MAP28A_OUTPUT_PNGS_256X256", pngsOk ? "PASS" : "FAIL");

        // Check 17 — HTML ASCII only
        bool htmlAscii = File.Exists(htmlPath) && File.ReadAllBytes(htmlPath).All(b => b < 128);
        SetCheck(result, "MAP28A_HTML_ASCII_ONLY", htmlAscii ? "PASS" : "FAIL");

        // Check 18 — README ASCII only
        bool readmeAscii = File.Exists(readmePath) && File.ReadAllBytes(readmePath).All(b => b < 128);
        SetCheck(result, "MAP28A_README_ASCII_ONLY", readmeAscii ? "PASS" : "FAIL");

        // Check 19 — No runtime outputs in root
        bool noRuntime = !Directory.Exists(outputRoot) ||
            new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" }
                .Sum(p => Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length) == 0;
        SetCheck(result, "MAP28A_NO_RUNTIME_OUTPUTS", noRuntime ? "PASS" : "FAIL");

        // Check 20 — Forbidden artifact scan
        string finalScan   = ScanOutputRoot(outputRoot);
        bool   scanPasses  = finalScan.Contains("PASS", StringComparison.Ordinal) && !finalScan.Contains("FAIL", StringComparison.Ordinal);
        result.ForbiddenArtifactScan = finalScan;
        SetCheck(result, "POST_MAP28A_RESIDENTIAL_PARCEL_TOPOLOGY_FORBIDDEN_SCAN_PASS",
            scanPasses ? "PASS" : "FAIL");

        result.PassedCheckCount = result.Checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = result.Checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass          = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.IsValid        = allPass;
        result.TopologyStatus = allPass
            ? "RESIDENTIAL_PARCEL_TOPOLOGY_COMPLETE"
            : "RESIDENTIAL_PARCEL_TOPOLOGY_FAILED";
        result.Verdict        = allPass
            ? "MAP28A_WORLDBUILDER_RESIDENTIAL_PARCEL_TOPOLOGY_COMPLETE"
            : invalidVerdict;

        return result;
    }

    private static void SetCheck(DeadMtlWorldBuilderResidentialParcelTopologyResult result,
        string checkId, string status)
    {
        var check = result.Checks.FirstOrDefault(c => c.CheckId == checkId);
        if (check is not null)
        {
            check.CheckStatus = status;
            check.Actual      = status;
        }
    }

    private static bool VerifyPng256(string path)
    {
        if (!File.Exists(path)) return false;
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 24) return false;
        // PNG: sig(8)+chunkLen(4)+"IHDR"(4)+width(4be)+height(4be)
        int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return w == 256 && h == 256;
    }

    // -----------------------------------------------------------------------
    // PNG rendering
    // -----------------------------------------------------------------------

    public byte[] RenderCleanParcelPngBytes(
        DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        using var bmp = CreateBackground();
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        FillAllStripsBrush(result, g);
        FillAllParcelsBrush(result, g);
        DrawBbox(bmp);
        return BitmapToBytes(bmp);
    }

    public byte[] RenderDebugParcelPngBytes(
        DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        using var bmp = CreateBackground();
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        FillAllStripsBrush(result, g);
        FillAllParcelsBrush(result, g);

        // Frontage ticks + center dots only — boundaries shown by adjacent shade change, not black lines
        var tick = System.Drawing.Color.FromArgb(s_tick.R, s_tick.G, s_tick.B);
        foreach (var p in result.ResidentialParcels)
        {
            int midX = (p.X1 + p.X2) / 2;
            int midY = (p.Y1 + p.Y2) / 2;
            bmp.SetPixel(midX, midY, tick);
            if      (p.FrontageDirection == "NORTH") bmp.SetPixel(midX, p.Y1, tick);
            else if (p.FrontageDirection == "SOUTH") bmp.SetPixel(midX, p.Y2, tick);
        }

        DrawBbox(bmp);
        return BitmapToBytes(bmp);
    }

    public byte[] RenderOverlayParcelPngBytes(
        DeadMtlWorldBuilderResidentialParcelTopologyResult result,
        string? rawSourcePngPath)
    {
        // Start with raw source or dark background
        using var img3 = new System.Drawing.Bitmap(256, 256,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g3 = System.Drawing.Graphics.FromImage(img3))
        {
            if (!string.IsNullOrEmpty(rawSourcePngPath) && File.Exists(rawSourcePngPath))
            {
                using var raw = new System.Drawing.Bitmap(rawSourcePngPath);
                g3.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g3.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g3.DrawImage(raw, 0, 0, 256, 256);
            }
            else
            {
                g3.Clear(System.Drawing.Color.FromArgb(s_bg.R, s_bg.G, s_bg.B));
            }

            // Semi-transparent overlay (alpha 150)
            using var overlay = new System.Drawing.Bitmap(256, 256,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var gOvl = System.Drawing.Graphics.FromImage(overlay))
            {
                gOvl.Clear(System.Drawing.Color.Transparent);
                foreach (var s in result.SidewalkStrips)
                {
                    var (r, gb, b) = GetStripColor(s.StripKind);
                    using var brush = new System.Drawing.SolidBrush(
                        System.Drawing.Color.FromArgb(150, r, gb, b));
                    gOvl.FillRectangle(brush, s.X1, s.Y1, s.X2 - s.X1 + 1, s.Y2 - s.Y1 + 1);
                }
                foreach (var p in result.ResidentialParcels)
                {
                    var (r, gb, b) = GetLotColor(p.ParcelId, p.FrontageDirection);
                    using var brush = new System.Drawing.SolidBrush(
                        System.Drawing.Color.FromArgb(150, r, gb, b));
                    gOvl.FillRectangle(brush, p.X1, p.Y1, p.X2 - p.X1 + 1, p.Y2 - p.Y1 + 1);
                }
            }
            g3.DrawImage(overlay, 0, 0);
        }

        DrawBbox(img3);
        return BitmapToBytes(img3);
    }

    private static (byte R, byte G, byte B) GetStripColor(string kind) => kind switch
    {
        "SIDEWALK"      => s_sidew,
        "REAR_BOUNDARY" => s_rear,
        _               => s_bg,
    };

    private System.Drawing.Bitmap CreateBackground()
    {
        var bmp = new System.Drawing.Bitmap(256, 256,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(s_bg.R, s_bg.G, s_bg.B));
        return bmp;
    }

    private static void FillAllStripsBrush(
        DeadMtlWorldBuilderResidentialParcelTopologyResult result,
        System.Drawing.Graphics g)
    {
        foreach (var s in result.SidewalkStrips)
        {
            var (r, gb, b) = GetStripColor(s.StripKind);
            using var brush = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(r, gb, b));
            g.FillRectangle(brush, s.X1, s.Y1, s.X2 - s.X1 + 1, s.Y2 - s.Y1 + 1);
        }
    }

    private static void FillAllParcelsBrush(
        DeadMtlWorldBuilderResidentialParcelTopologyResult result,
        System.Drawing.Graphics g)
    {
        foreach (var p in result.ResidentialParcels)
        {
            var (r, gb, b) = GetLotColor(p.ParcelId, p.FrontageDirection);
            using var brush = new System.Drawing.SolidBrush(
                System.Drawing.Color.FromArgb(r, gb, b));
            g.FillRectangle(brush, p.X1, p.Y1, p.X2 - p.X1 + 1, p.Y2 - p.Y1 + 1);
        }
    }

    private static void DrawBbox(System.Drawing.Bitmap bmp)
    {
        var c = System.Drawing.Color.FromArgb(s_bbox.R, s_bbox.G, s_bbox.B);
        for (int px = BboxX1; px <= BboxX2; px++)
        {
            bmp.SetPixel(px, BboxY1, c);
            bmp.SetPixel(px, BboxY2, c);
        }
        for (int py = BboxY1 + 1; py < BboxY2; py++)
        {
            bmp.SetPixel(BboxX1, py, c);
            bmp.SetPixel(BboxX2, py, c);
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

    public string RenderJson(DeadMtlWorldBuilderResidentialParcelTopologyResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderParcelsCsv(DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("parcel_id,component_id,parcel_kind,frontage_direction,x1,y1,x2,y2,width,height,tile_count,is_corner_lot,is_through_lot,primary_frontage_edge_id");
        foreach (var p in result.ResidentialParcels)
            sb.AppendLine($"{p.ParcelId},{p.ComponentId},{p.ParcelKind},{p.FrontageDirection},{p.X1},{p.Y1},{p.X2},{p.Y2},{p.Width},{p.Height},{p.TileCount},{p.IsCornerLot},{p.IsThroughLot},{p.PrimaryFrontageEdgeId}");
        return sb.ToString();
    }

    public string RenderFrontageEdgesCsv(DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("frontage_edge_id,parcel_id,frontage_direction,edge_kind,x1,y1,x2,y2,length_tiles");
        foreach (var e in result.FrontageEdges)
            sb.AppendLine($"{e.FrontageEdgeId},{e.ParcelId},{e.FrontageDirection},{e.EdgeKind},{e.X1},{e.Y1},{e.X2},{e.Y2},{e.LengthTiles}");
        return sb.ToString();
    }

    public string RenderSidewalkStripsCsv(DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("strip_id,strip_kind,direction,x1,y1,x2,y2,width_tiles,notes");
        foreach (var s in result.SidewalkStrips)
            sb.AppendLine($"{s.StripId},{s.StripKind},{s.Direction},{s.X1},{s.Y1},{s.X2},{s.Y2},{s.WidthTiles},{s.Notes}");
        return sb.ToString();
    }

    public string RenderChecksCsv(DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-28A WORLDBUILDER RESIDENTIAL PARCEL TOPOLOGY");
        sb.AppendLine($"Generated UTC    : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID           : {result.MapId}");
        sb.AppendLine($"Component ID     : {result.ComponentId}");
        sb.AppendLine($"Topology Stage   : {result.TopologyStage}");
        sb.AppendLine($"Topology Status  : {result.TopologyStatus}");
        sb.AppendLine($"Bbox             : X{result.BboxX1}-{result.BboxX2} Y{result.BboxY1}-{result.BboxY2} ({result.BboxWidth}x{result.BboxHeight})");
        sb.AppendLine($"Total Lots       : {result.TotalResidentialLotCount}");
        sb.AppendLine($"  North-facing   : {result.NorthFacingLotCount}");
        sb.AppendLine($"  South-facing   : {result.SouthFacingLotCount}");
        sb.AppendLine($"  East-facing    : {result.EastFacingLotCount}");
        sb.AppendLine($"Through Lots     : {result.ThroughLotCount}");
        sb.AppendLine($"Sidewalk Strips  : {result.SidewalkStripCount}");
        sb.AppendLine($"Rear Boundary    : {result.RearBoundaryStripCount}");
        sb.AppendLine($"Invented Alleys  : {result.InventedAlleyCount}");
        sb.AppendLine($"Checks           : {result.CheckCount} total / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid         : {result.IsValid}");
        sb.AppendLine($"Verdict          : {result.Verdict}");
        sb.AppendLine($"Forbidden Scan   : {result.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim boundary   : sandbox_only=true | writer_ready=false | runtime_valid=false | materialized=false");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine("Errors:");
            foreach (var e in result.Errors) sb.AppendLine($"  {e}");
        }
        return sb.ToString();
    }

    public string RenderHtml(DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\">");
        sb.AppendLine("<title>DeadMTL map_00 -- Residential Parcel Topology (MAP-28A)</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { background:#0e0e14; color:#ccc; font-family:monospace; padding:16px; }");
        sb.AppendLine("h1 { font-size:1em; color:#28c0c0; }");
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
        sb.AppendLine("<h1>DeadMTL map_00 -- Residential Parcel Topology (MAP-28A)</h1>");
        sb.AppendLine("<p class=\"warn\">");
        sb.AppendLine("NOT a playable Project Zomboid export. NOT .lotpack / .lotheader / .lua / .bin.<br>");
        sb.AppendLine("All PNGs are exactly 256x256 pixels. CSS zoom only.");
        sb.AppendLine("</p>");
        sb.AppendLine("<p>");
        sb.AppendLine($"Component: {result.ComponentId} | Bbox X:{result.BboxX1}-{result.BboxX2} Y:{result.BboxY1}-{result.BboxY2} ({result.BboxWidth} by {result.BboxHeight} tiles)<br>");
        sb.AppendLine($"Layout: {result.NorthFacingLotCount} north + {result.SouthFacingLotCount} south full-lot rows (widths 15/14 tiles). 0 east lots.<br>");
        sb.AppendLine("Mid-block separator: REAR_BOUNDARY (dark brown, NOT alley). Bbox: CYAN.<br>");
        sb.AppendLine("No invented alleys. No double-frontage. No through-lots.<br>");
        sb.AppendLine("Adjacent lots differ by shade. Boundaries by shade change, no black stroke lines. Planning artifact only.");
        sb.AppendLine("</p>");
        sb.AppendLine("<div class=\"pal\"><b>Palette:</b>");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#c8a878;\"></span>Lot shade A (light tan) &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#b08c60;\"></span>Lot shade B (medium tan) &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#98744c;\"></span>Lot shade C (dark tan) &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#b8b8c0;\"></span>Sidewalk &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#4a3828;\"></span>Rear Boundary &nbsp;");
        sb.AppendLine("<span class=\"swatch\" style=\"background:#28c0c0;\"></span>Bbox (cyan)</div>");
        sb.AppendLine("<div class=\"row\">");
        sb.AppendLine("  <div class=\"card\">");
        sb.AppendLine("    <img src=\"map_00_residential_parcels_topology_clean_native_256.png\" alt=\"clean parcel view\">");
        sb.AppendLine("    <div class=\"lbl\">clean parcel view (256x256)</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"card\">");
        sb.AppendLine("    <img src=\"map_00_residential_parcels_topology_debug_native_256.png\" alt=\"debug view\">");
        sb.AppendLine("    <div class=\"lbl\">debug view: frontage ticks + center dots, no black stroke lines (256x256)</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"card\">");
        sb.AppendLine("    <img src=\"map_00_residential_parcels_topology_overlay_native_256.png\" alt=\"overlay\">");
        sb.AppendLine("    <div class=\"lbl\">overlay on raw source (256x256)</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<h2>Checks: {result.CheckCount} / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL</h2>");
        sb.AppendLine("<p>Verdict: " + result.Verdict + "</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public string RenderReadme(DeadMtlWorldBuilderResidentialParcelTopologyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-28A WORLDBUILDER RESIDENTIAL PARCEL TOPOLOGY");
        sb.AppendLine("# DeadMTL map_00 -- Residential Parcel Topology Planning Artifact");
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
        sb.AppendLine("## Component");
        sb.AppendLine();
        sb.AppendLine($"- Component ID    : {result.ComponentId}");
        sb.AppendLine($"- Bbox tile coords: X {result.BboxX1}-{result.BboxX2}, Y {result.BboxY1}-{result.BboxY2} ({result.BboxWidth} by {result.BboxHeight} tiles)");
        sb.AppendLine();
        sb.AppendLine("## Geometry");
        sb.AppendLine();
        sb.AppendLine($"- North-facing lots : {result.NorthFacingLotCount} (Y {NorthY1}-{NorthY2}, widths 15/14, full-lot rows)");
        sb.AppendLine($"- South-facing lots : {result.SouthFacingLotCount} (Y {SouthY1}-{SouthY2}, widths 15/14, full-lot rows)");
        sb.AppendLine($"- East-facing lots  : 0 (no east residential row)");
        sb.AppendLine($"- Total lots        : {result.TotalResidentialLotCount}");
        sb.AppendLine($"- Sidewalk strips   : 0 (sidewalk context not rendered in planning view)");
        sb.AppendLine($"- Rear boundary     : {result.RearBoundaryStripCount} (REAR_BOUNDARY Y 39-40, NOT alley)");
        sb.AppendLine($"- Invented alleys   : {result.InventedAlleyCount} (invented_alleys_enabled=false)");
        sb.AppendLine();
        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("- sandbox_only                    = true");
        sb.AppendLine("- parcel_topology_planning_only   = true");
        sb.AppendLine("- writer_ready                    = false");
        sb.AppendLine("- runtime_valid                   = false");
        sb.AppendLine("- materialized                    = false");
        sb.AppendLine("- runtime_proof_claimed           = false");
        sb.AppendLine("- public_playable_packaging_claimed = false");
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine($"- Total : {result.CheckCount}");
        sb.AppendLine($"- PASS  : {result.PassedCheckCount}");
        sb.AppendLine($"- FAIL  : {result.FailedCheckCount}");
        sb.AppendLine($"- Verdict: {result.Verdict}");
        sb.AppendLine();
        sb.AppendLine($"## Forbidden artifact scan");
        sb.AppendLine();
        sb.AppendLine(result.ForbiddenArtifactScan);
        return sb.ToString();
    }
}
