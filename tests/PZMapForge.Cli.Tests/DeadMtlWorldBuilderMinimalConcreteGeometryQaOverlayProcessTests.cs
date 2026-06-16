using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Threading.Tasks;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-qa-overlay-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-qa-overlay.ps1");

    // -----------------------------------------------------------------------
    // Fixture generation — deterministic, self-contained
    // -----------------------------------------------------------------------

    private string MakeFixturePng()
    {
        var path = Path.Combine(_tempDir, "fixture.png");
        using var bmp = new Bitmap(256, 256);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(Color.White);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string MakeFixtureMvpJson()
    {
        const string tileId      = "map_00";
        const string componentId = "map_00_component_0001";

        var lots  = new List<DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord>();
        var slots = new List<DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord>();

        int bboxMinX = 10, bboxMinY = 10, lotWidth = 14, lotHeight = 60;
        for (int i = 0; i < 7; i++)
        {
            int lotMinX = bboxMinX + i * lotWidth;
            int lotMaxX = lotMinX + lotWidth - 1;
            string lotId = $"{tileId}_comp0001_lot_{i + 1:D4}";

            lots.Add(new DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord
            {
                LotOrder       = i + 1,
                LotId          = lotId,
                ComponentOrder = 1,
                ComponentId    = componentId,
                MinX           = lotMinX,
                MinY           = bboxMinY,
                MaxX           = lotMaxX,
                MaxY           = bboxMinY + lotHeight - 1,
                WidthPx        = lotWidth,
                HeightPx       = lotHeight,
            });

            slots.Add(new DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord
            {
                SlotOrder      = i + 1,
                SlotId         = $"{tileId}_comp0001_slot_{i + 1:D4}",
                LotId          = lotId,
                LotOrder       = i + 1,
                ComponentOrder = 1,
                ComponentId    = componentId,
                MinX           = lotMinX + 2,
                MinY           = bboxMinY + 3,
                MaxX           = lotMaxX - 2,
                MaxY           = bboxMinY + lotHeight - 1 - 3,
                WidthPx        = lotWidth - 4,
                HeightPx       = lotHeight - 6,
                SlotStatus     = "ACCEPTED",
                GeometryStatus = "CONCRETE_PIXEL_GEOMETRY_CREATED",
            });
        }

        int bboxMaxX = bboxMinX + 7 * lotWidth - 1;
        int bboxMaxY = bboxMinY + lotHeight - 1;

        var mvp = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult
        {
            TileId               = tileId,
            TargetComponentOrder = 1,
            GeometryMvpContract  = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpContract
            {
                TargetComponentId    = componentId,
                TargetIntent         = "RESIDENTIAL_LOT_BLOCK",
                AccessReadinessClass = "DUAL_ACCESS_CANDIDATE",
            },
            ComponentGeometry = new DeadMtlWorldBuilderMinimalConcreteComponentGeometryRecord
            {
                ComponentOrder = 1,
                ComponentId    = componentId,
                Intent         = "RESIDENTIAL_LOT_BLOCK",
                MinX           = bboxMinX,
                MinY           = bboxMinY,
                MaxX           = bboxMaxX,
                MaxY           = bboxMaxY,
                WidthPx        = bboxMaxX - bboxMinX + 1,
                HeightPx       = bboxMaxY - bboxMinY + 1,
                PixelCount     = (bboxMaxX - bboxMinX + 1) * (bboxMaxY - bboxMinY + 1),
            },
            LotGeometry          = lots,
            BuildingSlotGeometry = slots,
            IsValid              = true,
            Verdict              = "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE",
        };

        var path = Path.Combine(_tempDir, "fixture-mvp.json");
        File.WriteAllText(path, JsonSerializer.Serialize(mvp));
        return path;
    }

    private (string Png, string GeometryMvp) MakeValidFixtures() =>
        (MakeFixturePng(), MakeFixtureMvpJson());

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

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private (string Png, string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var dir = Path.Combine(_tempDir, ".local", subdir);
        Directory.CreateDirectory(dir);
        return (
            Path.Combine(dir, "overlay.png"),
            Path.Combine(dir, "overlay.json"),
            Path.Combine(dir, "overlay.md"),
            Path.Combine(dir, "overlay.csv"),
            Path.Combine(dir, "overlay.summary.txt")
        );
    }

    private (int ExitCode, string Stdout, string Stderr) RunBuild(string subdir = "default")
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var (png, j, m, c, s) = MakeOutputPaths(subdir);
        return RunCli(
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png",           pngPath,
            "--geometry-mvp",  geometryMvpPath,
            "--output-png",    png,
            "--output-json",   j,
            "--output-md",     m,
            "--output-csv",    c,
            "--summary",       s);
    }

    // -----------------------------------------------------------------------
    // Exit codes
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithValidInputs()
    {
        var (code, stdout, stderr) = RunBuild();
        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingArgs()
    {
        var (code, _, _) = RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay");
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var outsideDir = Path.Combine(_tempDir, "not-local");
        Directory.CreateDirectory(outsideDir);
        var (code, _, _) = RunCli(
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png",          pngPath,
            "--geometry-mvp", geometryMvpPath,
            "--output-png",   Path.Combine(outsideDir, "overlay.png"),
            "--output-json",  Path.Combine(outsideDir, "out.json"),
            "--output-md",    Path.Combine(outsideDir, "out.md"),
            "--output-csv",   Path.Combine(outsideDir, "out.csv"),
            "--summary",      Path.Combine(outsideDir, "out.txt"));
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenGeometryMvpMissing()
    {
        var pngPath = MakeFixturePng();
        var (png, j, m, c, s) = MakeOutputPaths("missing-mvp");
        var (code, _, _) = RunCli(
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png",          pngPath,
            "--geometry-mvp", Path.Combine(_tempDir, "does-not-exist.json"),
            "--output-png",   png,
            "--output-json",  j,
            "--output-md",    m,
            "--output-csv",   c,
            "--summary",      s);
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputPng()
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var (png, j, m, c, s) = MakeOutputPaths("png");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png", pngPath, "--geometry-mvp", geometryMvpPath,
            "--output-png", png, "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(png), "output PNG not created");
    }

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var (png, j, m, c, s) = MakeOutputPaths("json");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png", pngPath, "--geometry-mvp", geometryMvpPath,
            "--output-png", png, "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(j), "output JSON not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var (png, j, m, c, s) = MakeOutputPaths("md");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png", pngPath, "--geometry-mvp", geometryMvpPath,
            "--output-png", png, "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(m), "output MD not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var (png, j, m, c, s) = MakeOutputPaths("csv");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png", pngPath, "--geometry-mvp", geometryMvpPath,
            "--output-png", png, "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(c), "output CSV not created");
    }

    [Fact]
    public void Cli_CreatesOutputSummary()
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var (png, j, m, c, s) = MakeOutputPaths("summary");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png", pngPath, "--geometry-mvp", geometryMvpPath,
            "--output-png", png, "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(s), "output summary not created");
    }

    // -----------------------------------------------------------------------
    // No forbidden files
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_DoesNotEmit_LotpackOrWorldGenOverrideFiles()
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var (png, j, m, c, s) = MakeOutputPaths("forbidden");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png", pngPath, "--geometry-mvp", geometryMvpPath,
            "--output-png", png, "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);

        var produced = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(produced, p => p.EndsWith(".lotpack", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(produced, p => p.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Stdout / output verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsTitle()
    {
        var (_, stdout, _) = RunBuild("title");
        Assert.Contains("MAP-26B WorldBuilder minimal concrete geometry QA overlay", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunBuild("stdout-verdict");
        Assert.Contains("MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE",
            stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsVerdict()
    {
        var (pngPath, geometryMvpPath) = MakeValidFixtures();
        var (png, j, m, c, s) = MakeOutputPaths("json-verdict");
        RunCli("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
            "--png", pngPath, "--geometry-mvp", geometryMvpPath,
            "--output-png", png, "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        var json = File.ReadAllText(j);
        Assert.Contains("MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE", json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Unknown command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsQaOverlayCommand()
    {
        var (_, stdout, stderr) = RunCli("unknown-xyz-command");
        var combined = stdout + stderr;
        Assert.Contains("deadmtl-build-worldbuilder-minimal-concrete-geometry-qa-overlay",
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
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_CONTRACT.md");
        Assert.True(File.Exists(docPath), $"Doc not found: {docPath}");
    }

    [Fact]
    public void Doc_MentionsNotWriterReady()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_CONTRACT.md");
        var text = File.ReadAllText(docPath);
        Assert.True(
            text.Contains("writer_ready", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("not writer-ready", StringComparison.OrdinalIgnoreCase),
            "doc should mention writer-ready status");
    }
}
