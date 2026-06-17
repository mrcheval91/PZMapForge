using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-qa-review-packet-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-qa-review-packet.ps1");

    // -----------------------------------------------------------------------
    // Fixture generation
    // -----------------------------------------------------------------------

    private static DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult MakeFixtureMvpResult()
    {
        const string tileId = "map_00", componentId = "map_00_component_0001";
        int bboxMinX = 10, bboxMinY = 10, lotWidth = 14, lotHeight = 60;
        var lots  = new List<DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord>();
        var slots = new List<DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord>();
        for (int i = 0; i < 7; i++)
        {
            int lotMinX = bboxMinX + i * lotWidth, lotMaxX = lotMinX + lotWidth - 1;
            string lotId = $"{tileId}_comp0001_lot_{i + 1:D4}";
            lots.Add(new DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord
            {
                LotOrder = i+1, LotId = lotId, ComponentOrder = 1, ComponentId = componentId,
                MinX = lotMinX, MinY = bboxMinY, MaxX = lotMaxX, MaxY = bboxMinY + lotHeight - 1,
                WidthPx = lotWidth, HeightPx = lotHeight,
            });
            slots.Add(new DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord
            {
                SlotOrder = i+1, SlotId = $"{tileId}_comp0001_slot_{i+1:D4}",
                LotId = lotId, LotOrder = i+1, ComponentOrder = 1, ComponentId = componentId,
                MinX = lotMinX+2, MinY = bboxMinY+3, MaxX = lotMaxX-2, MaxY = bboxMinY+lotHeight-4,
                WidthPx = lotWidth-4, HeightPx = lotHeight-6,
                SlotStatus = "ACCEPTED", GeometryStatus = "CONCRETE_PIXEL_GEOMETRY_CREATED",
            });
        }
        int bboxMaxX = bboxMinX + 7 * lotWidth - 1, bboxMaxY = bboxMinY + lotHeight - 1;
        return new DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult
        {
            TileId = tileId, TargetComponentOrder = 1,
            GeometryMvpContract = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpContract
            {
                TargetComponentId = componentId, TargetIntent = "RESIDENTIAL_LOT_BLOCK",
                AccessReadinessClass = "DUAL_ACCESS_CANDIDATE",
            },
            ComponentGeometry = new DeadMtlWorldBuilderMinimalConcreteComponentGeometryRecord
            {
                ComponentOrder = 1, ComponentId = componentId, Intent = "RESIDENTIAL_LOT_BLOCK",
                MinX = bboxMinX, MinY = bboxMinY, MaxX = bboxMaxX, MaxY = bboxMaxY,
                WidthPx = bboxMaxX - bboxMinX + 1, HeightPx = bboxMaxY - bboxMinY + 1,
                PixelCount = (bboxMaxX - bboxMinX + 1) * (bboxMaxY - bboxMinY + 1),
            },
            LotGeometry = lots, BuildingSlotGeometry = slots,
            IsValid = true, Verdict = "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE",
        };
    }

    private static DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult MakeFixtureOverlayResult()
    {
        const string componentId = "map_00_component_0001";
        int bboxMinX = 10, bboxMinY = 10, lotWidth = 14, lotHeight = 60;
        int bboxMaxX = bboxMinX + 7 * lotWidth - 1, bboxMaxY = bboxMinY + lotHeight - 1;
        var features = new List<DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature>();
        features.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature
        {
            FeatureOrder = 1, FeatureKind = "COMPONENT_BBOX", FeatureId = componentId,
            X = bboxMinX, Y = bboxMinY, Width = bboxMaxX - bboxMinX + 1, Height = bboxMaxY - bboxMinY + 1,
            Right = bboxMaxX, Bottom = bboxMaxY, Scale = 4, DrawOrder = 1,
            OverlayX = bboxMinX * 4, OverlayY = bboxMinY * 4,
            OverlayWidth = (bboxMaxX - bboxMinX + 1) * 4, OverlayHeight = (bboxMaxY - bboxMinY + 1) * 4,
        });
        for (int i = 0; i < 7; i++)
        {
            int lotMinX = bboxMinX + i * lotWidth, lotMaxX = lotMinX + lotWidth - 1;
            features.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature
            {
                FeatureOrder = i + 2, FeatureKind = "LOT_RECTANGLE",
                FeatureId = $"map_00_comp0001_lot_{i + 1:D4}",
                X = lotMinX, Y = bboxMinY, Width = lotWidth, Height = lotHeight,
                Right = lotMaxX, Bottom = bboxMaxY, Scale = 4, DrawOrder = 2,
                OverlayX = lotMinX * 4, OverlayY = bboxMinY * 4,
                OverlayWidth = lotWidth * 4, OverlayHeight = lotHeight * 4,
            });
        }
        for (int i = 0; i < 7; i++)
        {
            int lotMinX = bboxMinX + i * lotWidth, lotMaxX = lotMinX + lotWidth - 1;
            int sx = lotMinX+2, ex = lotMaxX-2, sy = bboxMinY+3, ey = bboxMaxY-3;
            features.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature
            {
                FeatureOrder = i + 9, FeatureKind = "BUILDING_SLOT_RECTANGLE",
                FeatureId = $"map_00_comp0001_slot_{i + 1:D4}",
                X = sx, Y = sy, Width = ex-sx+1, Height = ey-sy+1,
                Right = ex, Bottom = ey, Scale = 4, DrawOrder = 3,
                OverlayX = sx*4, OverlayY = sy*4, OverlayWidth = (ex-sx+1)*4, OverlayHeight = (ey-sy+1)*4,
            });
        }
        return new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult
        {
            MapId = "map_00", TargetComponentId = componentId, TargetComponentOrder = 1,
            Intent = "RESIDENTIAL_LOT_BLOCK", AccessReadinessClass = "DUAL_ACCESS_CANDIDATE",
            SourceWidthPx = 256, SourceHeightPx = 256, Scale = 4,
            ComponentBbox = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBbox
            {
                MinX = bboxMinX, MinY = bboxMinY, MaxX = bboxMaxX, MaxY = bboxMaxY,
                WidthPx = bboxMaxX - bboxMinX + 1, HeightPx = bboxMaxY - bboxMinY + 1,
            },
            LotCount = 7, AcceptedBuildingSlotCount = 7, OverlayFeatureCount = 15,
            WriterReady = false, RuntimeValid = false, Materialized = false,
            Features = features, IsValid = true,
            Verdict = "MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE",
        };
    }

    private (string MvpJson, string OverlayJson, string OverlayCsv, string OverlayPng) MakeValidFixtures()
    {
        var mvp     = MakeFixtureMvpResult();
        var overlay = MakeFixtureOverlayResult();
        var opts    = new JsonSerializerOptions { WriteIndented = true };

        var mvpJson     = Path.Combine(_tempDir, "mvp.json");
        var overlayJson = Path.Combine(_tempDir, "overlay.json");
        var overlayCsv  = Path.Combine(_tempDir, "overlay.csv");
        var overlayPng  = Path.Combine(_tempDir, "overlay.png");

        File.WriteAllText(mvpJson,     JsonSerializer.Serialize(mvp, opts));
        File.WriteAllText(overlayJson, JsonSerializer.Serialize(overlay, opts));

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("feature_order,feature_kind,feature_id,label,x,y,width,height,right,bottom,scale,overlay_x,overlay_y,overlay_width,overlay_height,draw_order");
        foreach (var f in overlay.Features)
            csv.AppendLine($"{f.FeatureOrder},{f.FeatureKind},{f.FeatureId},{f.FeatureId},{f.X},{f.Y},{f.Width},{f.Height},{f.Right},{f.Bottom},{f.Scale},{f.OverlayX},{f.OverlayY},{f.OverlayWidth},{f.OverlayHeight},{f.DrawOrder}");
        File.WriteAllText(overlayCsv, csv.ToString());
        File.WriteAllBytes(overlayPng, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        return (mvpJson, overlayJson, overlayCsv, overlayPng);
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var dir = Path.Combine(_tempDir, ".local", subdir);
        Directory.CreateDirectory(dir);
        return (
            Path.Combine(dir, "packet.json"),
            Path.Combine(dir, "packet.md"),
            Path.Combine(dir, "packet.csv"),
            Path.Combine(dir, "packet.summary.txt")
        );
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(CliProject);
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc       = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private (int ExitCode, string Stdout, string Stderr) RunBuild(string subdir = "default")
    {
        var (mvpJson, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths(subdir);
        return RunCli(
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet",
            "--geometry-mvp",    mvpJson,
            "--qa-overlay-json", overlayJson,
            "--qa-overlay-csv",  overlayCsv,
            "--qa-overlay-png",  overlayPng,
            "--output-json",     j,
            "--output-md",       m,
            "--output-csv",      c,
            "--summary",         s);
    }

    // -----------------------------------------------------------------------
    // Exit codes
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithValidInputs()
    {
        var (code, stdout, stderr) = RunBuild("exit0");
        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingArgs()
    {
        var (code, _, _) = RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet");
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var (mvpJson, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var outside = Path.Combine(_tempDir, "not-local");
        Directory.CreateDirectory(outside);
        var (code, _, _) = RunCli(
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet",
            "--geometry-mvp",    mvpJson,
            "--qa-overlay-json", overlayJson,
            "--qa-overlay-csv",  overlayCsv,
            "--qa-overlay-png",  overlayPng,
            "--output-json",     Path.Combine(outside, "out.json"),
            "--output-md",       Path.Combine(outside, "out.md"),
            "--output-csv",      Path.Combine(outside, "out.csv"),
            "--summary",         Path.Combine(outside, "out.txt"));
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var (mvpJson, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("json");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet",
            "--geometry-mvp", mvpJson, "--qa-overlay-json", overlayJson,
            "--qa-overlay-csv", overlayCsv, "--qa-overlay-png", overlayPng,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(j), "output JSON not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var (mvpJson, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("md");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet",
            "--geometry-mvp", mvpJson, "--qa-overlay-json", overlayJson,
            "--qa-overlay-csv", overlayCsv, "--qa-overlay-png", overlayPng,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(m), "output MD not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var (mvpJson, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("csv");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet",
            "--geometry-mvp", mvpJson, "--qa-overlay-json", overlayJson,
            "--qa-overlay-csv", overlayCsv, "--qa-overlay-png", overlayPng,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(c), "output CSV not created");
    }

    [Fact]
    public void Cli_CreatesOutputSummary()
    {
        var (mvpJson, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("summary");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet",
            "--geometry-mvp", mvpJson, "--qa-overlay-json", overlayJson,
            "--qa-overlay-csv", overlayCsv, "--qa-overlay-png", overlayPng,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(s), "output summary not created");
    }

    // -----------------------------------------------------------------------
    // Stdout / output content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsTitle()
    {
        var (_, stdout, _) = RunBuild("title");
        Assert.Contains("MAP-26C WorldBuilder minimal concrete geometry QA review packet", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunBuild("verdict");
        Assert.Contains("MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsVerdict()
    {
        var (mvpJson, overlayJson, overlayCsv, overlayPng) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("jsonverdict");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet",
            "--geometry-mvp", mvpJson, "--qa-overlay-json", overlayJson,
            "--qa-overlay-csv", overlayCsv, "--qa-overlay-png", overlayPng,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        var json = File.ReadAllText(j);
        Assert.Contains("MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE", json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Forbidden files
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_DoesNotEmit_LotpackOrWorldGenOverrideFiles()
    {
        RunBuild("forbidden");
        var produced = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(produced, p => p.EndsWith(".lotpack", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(produced, p => p.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Unknown command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsQaReviewPacketCommand()
    {
        var (_, stdout, stderr) = RunCli("unknown-xyz-command");
        var combined = stdout + stderr;
        Assert.Contains("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-review-packet",
            combined, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Helper script
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_Exists()
    {
        Assert.True(File.Exists(HelperScript), $"Helper script not found: {HelperScript}");
    }

    [Fact]
    public void HelperScript_DoesNotContain_CompileWorldgen()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("compile-worldgen", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_WorldGenOverrideLua()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotpack()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotpack", text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Doc
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_Exists()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET.md");
        Assert.True(File.Exists(docPath), $"Doc not found: {docPath}");
    }

    [Fact]
    public void Doc_MentionsClaimBoundary()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET.md");
        var text = File.ReadAllText(docPath);
        Assert.True(
            text.Contains("writer_ready", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Claim Boundary", StringComparison.OrdinalIgnoreCase),
            "doc should mention writer_ready or claim boundary");
    }
}
