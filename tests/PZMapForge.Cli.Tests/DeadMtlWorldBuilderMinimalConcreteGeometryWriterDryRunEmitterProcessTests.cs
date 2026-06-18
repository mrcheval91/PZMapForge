using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-dry-run-emitter-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteDryRunDesignJson()
    {
        var path = Path.Combine(_tempDir, "design.json");
        var obj = new
        {
            format               = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-dry-run-design.v1",
            map_id               = "map_00",
            target_component_id  = "map_00_component_0001",
            verdict              = "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE",
            dry_run_only         = true,
            future_writer_status = "DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME",
            is_valid             = true,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private string WriteGeometryMvpJson()
    {
        var path = Path.Combine(_tempDir, "mvp.json");
        var lots = new object[]
        {
            new { lot_order=1, lot_id="map_00_comp0001_lot_0001", component_id="map_00_component_0001", min_x=124, min_y=10, max_x=136, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=2, lot_id="map_00_comp0001_lot_0002", component_id="map_00_component_0001", min_x=137, min_y=10, max_x=149, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=3, lot_id="map_00_comp0001_lot_0003", component_id="map_00_component_0001", min_x=150, min_y=10, max_x=162, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=4, lot_id="map_00_comp0001_lot_0004", component_id="map_00_component_0001", min_x=163, min_y=10, max_x=175, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=5, lot_id="map_00_comp0001_lot_0005", component_id="map_00_component_0001", min_x=176, min_y=10, max_x=188, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=6, lot_id="map_00_comp0001_lot_0006", component_id="map_00_component_0001", min_x=189, min_y=10, max_x=200, max_y=69, width_px=12, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=7, lot_id="map_00_comp0001_lot_0007", component_id="map_00_component_0001", min_x=201, min_y=10, max_x=212, max_y=69, width_px=12, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
        };
        var slots = new object[]
        {
            new { slot_order=1, slot_id="map_00_comp0001_slot_0001", lot_id="map_00_comp0001_lot_0001", lot_order=1, component_id="map_00_component_0001", min_x=126, min_y=13, max_x=134, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=2, slot_id="map_00_comp0001_slot_0002", lot_id="map_00_comp0001_lot_0002", lot_order=2, component_id="map_00_component_0001", min_x=139, min_y=13, max_x=147, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=3, slot_id="map_00_comp0001_slot_0003", lot_id="map_00_comp0001_lot_0003", lot_order=3, component_id="map_00_component_0001", min_x=152, min_y=13, max_x=160, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=4, slot_id="map_00_comp0001_slot_0004", lot_id="map_00_comp0001_lot_0004", lot_order=4, component_id="map_00_component_0001", min_x=165, min_y=13, max_x=173, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=5, slot_id="map_00_comp0001_slot_0005", lot_id="map_00_comp0001_lot_0005", lot_order=5, component_id="map_00_component_0001", min_x=178, min_y=13, max_x=186, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=6, slot_id="map_00_comp0001_slot_0006", lot_id="map_00_comp0001_lot_0006", lot_order=6, component_id="map_00_component_0001", min_x=191, min_y=13, max_x=198, max_y=66, width_px=8, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=7, slot_id="map_00_comp0001_slot_0007", lot_id="map_00_comp0001_lot_0007", lot_order=7, component_id="map_00_component_0001", min_x=203, min_y=13, max_x=210, max_y=66, width_px=8, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
        };
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1",
            tile_id = "map_00",
            geometry_mvp_contract = new
            {
                target_component_id              = "map_00_component_0001",
                target_intent                    = "RESIDENTIAL_LOT_BLOCK",
                access_readiness_class           = "DUAL_ACCESS_CANDIDATE",
                component_bbox_min_x             = 124,
                component_bbox_min_y             = 10,
                component_bbox_max_x             = 212,
                component_bbox_max_y             = 69,
                component_bbox_width_px          = 89,
                component_bbox_height_px         = 60,
                lot_geometry_count               = 7,
                accepted_building_slot_count     = 7,
                frontage_side                    = "NORTH",
                primary_frontage_component_id    = "map_00_component_0023",
                frontage_contact_px              = 89,
                rear_service_side                = "EAST",
                primary_rear_service_component_id = "map_00_component_0030",
                rear_service_contact_px          = 60,
            },
            lot_geometry           = lots,
            building_slot_geometry = slots,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private (string OutputRoot, string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var root = Path.Combine(_tempDir, ".local", subdir, "emitter");
        var main = Path.Combine(_tempDir, ".local", subdir, "main");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(main);
        return (
            root,
            Path.Combine(main, "emitter.json"),
            Path.Combine(main, "emitter.md"),
            Path.Combine(main, "emitter.csv"),
            Path.Combine(main, "emitter.summary.txt")
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

        using var proc     = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private const string Cmd = "deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter";

    private (int ExitCode, string Stdout, string Stderr) RunBuild(string subdir = "default")
    {
        var design = WriteDryRunDesignJson();
        var mvp    = WriteGeometryMvpJson();
        var (root, j, m, c, s) = MakeOutputPaths(subdir);
        return RunCli(Cmd,
            "--dry-run-design",   design,
            "--geometry-mvp",     mvp,
            "--output-root",      root,
            "--output-json",      j,
            "--output-md",        m,
            "--output-csv",       c,
            "--summary",          s);
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
        var (code, _, _) = RunCli(Cmd);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingDryRunDesign()
    {
        var mvp = WriteGeometryMvpJson();
        var (root, j, m, c, s) = MakeOutputPaths("no-design");
        var (code, _, _) = RunCli(Cmd,
            "--dry-run-design", "__nonexistent_design__.json",
            "--geometry-mvp",   mvp,
            "--output-root",    root,
            "--output-json",    j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingGeometry()
    {
        var design = WriteDryRunDesignJson();
        var (root, j, m, c, s) = MakeOutputPaths("no-mvp");
        var (code, _, _) = RunCli(Cmd,
            "--dry-run-design", design,
            "--geometry-mvp",   "__nonexistent_mvp__.json",
            "--output-root",    root,
            "--output-json",    j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputRootNotUnderLocal()
    {
        var design = WriteDryRunDesignJson();
        var mvp    = WriteGeometryMvpJson();
        var outside = Path.Combine(_tempDir, "not-local");
        Directory.CreateDirectory(outside);
        var (_, j, m, c, s) = MakeOutputPaths("or-not-local");
        var (code, _, _) = RunCli(Cmd,
            "--dry-run-design", design,
            "--geometry-mvp",   mvp,
            "--output-root",    outside,
            "--output-json",    j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputFilesNotUnderLocal()
    {
        var design = WriteDryRunDesignJson();
        var mvp    = WriteGeometryMvpJson();
        var outside = Path.Combine(_tempDir, "not-local-files");
        Directory.CreateDirectory(outside);
        var root = Path.Combine(_tempDir, ".local", "good-root");
        Directory.CreateDirectory(root);
        var (code, _, _) = RunCli(Cmd,
            "--dry-run-design", design,
            "--geometry-mvp",   mvp,
            "--output-root",    root,
            "--output-json",    Path.Combine(outside, "out.json"),
            "--output-md",      Path.Combine(outside, "out.md"),
            "--output-csv",     Path.Combine(outside, "out.csv"),
            "--summary",        Path.Combine(outside, "out.txt"));
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Creates4MainOutputs()
    {
        var design = WriteDryRunDesignJson();
        var mvp    = WriteGeometryMvpJson();
        var (root, j, m, c, s) = MakeOutputPaths("main4");
        RunCli(Cmd,
            "--dry-run-design", design, "--geometry-mvp", mvp, "--output-root", root,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(j), "output JSON not created");
        Assert.True(File.Exists(m), "output MD not created");
        Assert.True(File.Exists(c), "output CSV not created");
        Assert.True(File.Exists(s), "output summary not created");
    }

    [Fact]
    public void Cli_Creates8DryRunRecords()
    {
        var design = WriteDryRunDesignJson();
        var mvp    = WriteGeometryMvpJson();
        var (root, j, m, c, s) = MakeOutputPaths("rec8");
        RunCli(Cmd,
            "--dry-run-design", design, "--geometry-mvp", mvp, "--output-root", root,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(Path.Combine(root, "map_00.component_writer_record.json")),      "component_writer_record not created");
        Assert.True(File.Exists(Path.Combine(root, "map_00.lot_writer_records.json")),           "lot_writer_records not created");
        Assert.True(File.Exists(Path.Combine(root, "map_00.building_slot_writer_records.json")), "building_slot_writer_records not created");
        Assert.True(File.Exists(Path.Combine(root, "map_00.frontage_access_record.json")),       "frontage_access_record not created");
        Assert.True(File.Exists(Path.Combine(root, "map_00.rear_service_access_record.json")),   "rear_service_access_record not created");
        Assert.True(File.Exists(Path.Combine(root, "map_00.forbidden_output_scan.json")),        "forbidden_output_scan not created");
        Assert.True(File.Exists(Path.Combine(root, "map_00.rollback_record.json")),              "rollback_record not created");
        Assert.True(File.Exists(Path.Combine(root, "map_00.claim_boundary_record.json")),        "claim_boundary_record not created");
    }

    // -----------------------------------------------------------------------
    // Stdout / output content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsTitle()
    {
        var (_, stdout, _) = RunBuild("title");
        Assert.Contains("MAP-26G WorldBuilder minimal concrete geometry writer dry-run emitter",
            stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunBuild("verdict");
        Assert.Contains("MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_COMPLETE",
            stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsDryRunRecordsEmitted()
    {
        var (_, stdout, _) = RunBuild("status");
        Assert.Contains("DRY_RUN_RECORDS_EMITTED_TO_DOT_LOCAL_ONLY", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsDryRunOnlyTrue()
    {
        var design = WriteDryRunDesignJson();
        var mvp    = WriteGeometryMvpJson();
        var (root, j, m, c, s) = MakeOutputPaths("jsondr");
        RunCli(Cmd,
            "--dry-run-design", design, "--geometry-mvp", mvp, "--output-root", root,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Contains("\"dry_run_only\": true", File.ReadAllText(j), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsEmittedRecordCount8()
    {
        var design = WriteDryRunDesignJson();
        var mvp    = WriteGeometryMvpJson();
        var (root, j, m, c, s) = MakeOutputPaths("jsonrc8");
        RunCli(Cmd,
            "--dry-run-design", design, "--geometry-mvp", mvp, "--output-root", root,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Contains("\"emitted_record_count\": 8", File.ReadAllText(j), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Forbidden files
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_DoesNotEmit_ForbiddenFiles()
    {
        RunBuild("forbidden");
        var produced = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(produced, p => p.EndsWith(".lotpack",             StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(produced, p => p.EndsWith(".lotheader",           StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(produced, p => p.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Unknown command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsDryRunEmitterCommand()
    {
        var (_, stdout, stderr) = RunCli("unknown-xyz-emitter-command");
        Assert.Contains("deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-emitter",
            stdout + stderr, StringComparison.Ordinal);
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
        Assert.DoesNotContain("compile-worldgen", File.ReadAllText(HelperScript), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_WorldGenOverrideLua()
    {
        Assert.DoesNotContain("WorldGenOverride.lua", File.ReadAllText(HelperScript), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotpack()
    {
        Assert.DoesNotContain(".lotpack", File.ReadAllText(HelperScript), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotheader()
    {
        Assert.DoesNotContain(".lotheader", File.ReadAllText(HelperScript), StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Doc
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_Exists()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER.md");
        Assert.True(File.Exists(docPath), $"Doc not found: {docPath}");
    }
}
