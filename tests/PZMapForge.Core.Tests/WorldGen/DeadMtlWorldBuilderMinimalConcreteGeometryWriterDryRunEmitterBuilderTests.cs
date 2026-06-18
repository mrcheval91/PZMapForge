using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-dry-run-emitter", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteDryRunDesignJson(
        string verdict = "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE",
        bool dryRunOnly = true,
        string futureWriterStatus = "DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME")
    {
        var path = Path.Combine(_tempDir, "design.json");
        var obj = new
        {
            format               = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-dry-run-design.v1",
            map_id               = "map_00",
            target_component_id  = "map_00_component_0001",
            verdict              = verdict,
            dry_run_only         = dryRunOnly,
            future_writer_status = futureWriterStatus,
            is_valid             = true,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private string WriteGeometryMvpJson()
    {
        var path = Path.Combine(_tempDir, "mvp.json");
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
            lot_geometry = BuildLotGeometryFixture(),
            building_slot_geometry = BuildSlotGeometryFixture(),
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private static object[] BuildLotGeometryFixture()
    {
        return new[]
        {
            (object)new { lot_order=1, lot_id="map_00_comp0001_lot_0001", component_id="map_00_component_0001", min_x=124, min_y=10, max_x=136, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=2, lot_id="map_00_comp0001_lot_0002", component_id="map_00_component_0001", min_x=137, min_y=10, max_x=149, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=3, lot_id="map_00_comp0001_lot_0003", component_id="map_00_component_0001", min_x=150, min_y=10, max_x=162, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=4, lot_id="map_00_comp0001_lot_0004", component_id="map_00_component_0001", min_x=163, min_y=10, max_x=175, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=5, lot_id="map_00_comp0001_lot_0005", component_id="map_00_component_0001", min_x=176, min_y=10, max_x=188, max_y=69, width_px=13, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=6, lot_id="map_00_comp0001_lot_0006", component_id="map_00_component_0001", min_x=189, min_y=10, max_x=200, max_y=69, width_px=12, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { lot_order=7, lot_id="map_00_comp0001_lot_0007", component_id="map_00_component_0001", min_x=201, min_y=10, max_x=212, max_y=69, width_px=12, height_px=60, frontage_side="NORTH", rear_service_side="EAST", geometry_type="LOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
        };
    }

    private static object[] BuildSlotGeometryFixture()
    {
        return new[]
        {
            (object)new { slot_order=1, slot_id="map_00_comp0001_slot_0001", lot_id="map_00_comp0001_lot_0001", lot_order=1, component_id="map_00_component_0001", min_x=126, min_y=13, max_x=134, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=2, slot_id="map_00_comp0001_slot_0002", lot_id="map_00_comp0001_lot_0002", lot_order=2, component_id="map_00_component_0001", min_x=139, min_y=13, max_x=147, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=3, slot_id="map_00_comp0001_slot_0003", lot_id="map_00_comp0001_lot_0003", lot_order=3, component_id="map_00_component_0001", min_x=152, min_y=13, max_x=160, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=4, slot_id="map_00_comp0001_slot_0004", lot_id="map_00_comp0001_lot_0004", lot_order=4, component_id="map_00_component_0001", min_x=165, min_y=13, max_x=173, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=5, slot_id="map_00_comp0001_slot_0005", lot_id="map_00_comp0001_lot_0005", lot_order=5, component_id="map_00_component_0001", min_x=178, min_y=13, max_x=186, max_y=66, width_px=9, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=6, slot_id="map_00_comp0001_slot_0006", lot_id="map_00_comp0001_lot_0006", lot_order=6, component_id="map_00_component_0001", min_x=191, min_y=13, max_x=198, max_y=66, width_px=8, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
            new { slot_order=7, slot_id="map_00_comp0001_slot_0007", lot_id="map_00_comp0001_lot_0007", lot_order=7, component_id="map_00_component_0001", min_x=203, min_y=13, max_x=210, max_y=66, width_px=8, height_px=54, frontage_setback_px=3, rear_setback_px=3, side_inset_px=2, slot_status="ACCEPTED", geometry_type="BUILDING_SLOT_RECTANGLE_MVP", geometry_status="CONCRETE_PIXEL_GEOMETRY_CREATED" },
        };
    }

    private string OutputRoot(string sub = "default") =>
        Path.Combine(_tempDir, ".local", sub);

    private DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult BuildValid(string sub = "valid")
    {
        var design = WriteDryRunDesignJson();
        var mvp    = WriteGeometryMvpJson();
        return new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder()
            .Build(design, mvp, OutputRoot(sub));
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult BuildWith(
        string? designVerdict = null,
        bool designDryRunOnly = true,
        string? futureWriterStatus = null,
        string sub = "custom")
    {
        var design = WriteDryRunDesignJson(
            designVerdict       ?? "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE",
            designDryRunOnly,
            futureWriterStatus  ?? "DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME");
        var mvp = WriteGeometryMvpJson();
        return new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder()
            .Build(design, mvp, OutputRoot(sub));
    }

    // -----------------------------------------------------------------------
    // Guard tests — missing inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingDryRunDesign_IsInvalid()
    {
        var mvp = WriteGeometryMvpJson();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder()
            .Build("__nonexistent_design__.json", mvp, OutputRoot("miss-design"));
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingGeometryMvp_IsInvalid()
    {
        var design = WriteDryRunDesignJson();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder()
            .Build(design, "__nonexistent_mvp__.json", OutputRoot("miss-mvp"));
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MalformedDryRunDesign_IsInvalid()
    {
        var bad = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(bad, "NOT JSON {{{{");
        var mvp = WriteGeometryMvpJson();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder()
            .Build(bad, mvp, OutputRoot("malformed"));
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Check-level failures
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WrongMap26FVerdict_IsInvalid()
    {
        var result = BuildWith(designVerdict: "WRONG_VERDICT");
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26F_VERDICT_COMPLETE").CheckStatus);
    }

    [Fact]
    public void Build_Map26FDryRunOnlyFalse_IsInvalid()
    {
        var result = BuildWith(designDryRunOnly: false);
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26F_DRY_RUN_ONLY_TRUE").CheckStatus);
    }

    [Fact]
    public void Build_Map26FFutureWriterStatusWrong_IsInvalid()
    {
        var result = BuildWith(futureWriterStatus: "AUTHORIZED");
        Assert.False(result.IsValid);
        Assert.Equal("FAIL", result.Checks.Single(c => c.CheckId == "MAP26F_FUTURE_WRITER_STATUS_DESIGN_ONLY").CheckStatus);
    }

    // -----------------------------------------------------------------------
    // Valid build — structural
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_IsValid()
    {
        var result = BuildValid();
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void ValidFixture_NoErrors()
    {
        Assert.Empty(BuildValid().Errors);
    }

    [Fact]
    public void ValidFixture_AllChecksPass()
    {
        var result  = BuildValid("all-pass");
        var failing = result.Checks.Where(c => c.CheckStatus != "PASS").Select(c => c.CheckId).ToList();
        Assert.Empty(failing);
    }

    [Fact]
    public void ValidFixture_CheckCount_Is25()
    {
        Assert.Equal(25, BuildValid("cc25").CheckCount);
    }

    [Fact]
    public void ValidFixture_PassedCheckCount_Is25()
    {
        Assert.Equal(25, BuildValid("pcc25").PassedCheckCount);
    }

    [Fact]
    public void ValidFixture_FailedCheckCount_Is0()
    {
        Assert.Equal(0, BuildValid("fcc0").FailedCheckCount);
    }

    // -----------------------------------------------------------------------
    // Geometry stability checks
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_TargetComponent_IsStable()
    {
        var result = BuildValid("tc");
        Assert.Equal("map_00_component_0001", result.TargetComponentId);
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "TARGET_COMPONENT_STABLE").CheckStatus);
    }

    [Fact]
    public void ValidFixture_BboxStable()
    {
        var result = BuildValid("bbox");
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "COMPONENT_BBOX_STABLE").CheckStatus);
    }

    [Fact]
    public void ValidFixture_LotCount_Is7()
    {
        var result = BuildValid("lc7");
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "LOT_COUNT_7").CheckStatus);
    }

    [Fact]
    public void ValidFixture_BuildingSlotCount_Is7()
    {
        var result = BuildValid("sc7");
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "BUILDING_SLOT_COUNT_7").CheckStatus);
    }

    [Fact]
    public void ValidFixture_FrontageAccess_IsStable()
    {
        var result = BuildValid("fa");
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "FRONTAGE_ACCESS_STABLE").CheckStatus);
    }

    [Fact]
    public void ValidFixture_RearServiceAccess_IsStable()
    {
        var result = BuildValid("rs");
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "REAR_SERVICE_ACCESS_STABLE").CheckStatus);
    }

    // -----------------------------------------------------------------------
    // Emitted records
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_EmittedRecordCount_Is8()
    {
        Assert.Equal(8, BuildValid("er8").EmittedRecordCount);
    }

    [Fact]
    public void ValidFixture_AllEmittedRecordsHashed()
    {
        var result = BuildValid("erh");
        Assert.All(result.EmittedRecords, r => Assert.Equal(64, r.Sha256.Length));
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "ALL_EMITTED_RECORDS_HASHED").CheckStatus);
    }

    [Fact]
    public void ValidFixture_ForbiddenScan_Pass()
    {
        var result = BuildValid("fsp");
        Assert.True(result.ForbiddenOutputScan.ScanPassed);
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "FORBIDDEN_OUTPUT_SCAN_PASS").CheckStatus);
    }

    [Fact]
    public void ValidFixture_RollbackRecord_Pass()
    {
        var result = BuildValid("rr");
        Assert.True(result.RollbackRecord.DotLocalOutputsOnly);
        Assert.True(result.RollbackRecord.SourceHashesUnchanged);
        Assert.False(result.RollbackRecord.RuntimeFilesCreated);
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "ROLLBACK_RECORD_PASS").CheckStatus);
    }

    [Fact]
    public void ValidFixture_ClaimBoundary_Pass()
    {
        var result = BuildValid("cb");
        Assert.False(result.ClaimBoundary.WriterReady);
        Assert.False(result.ClaimBoundary.RuntimeValid);
        Assert.False(result.ClaimBoundary.Materialized);
        Assert.Equal("PASS", result.Checks.Single(c => c.CheckId == "CLAIM_BOUNDARY_RECORD_PASS").CheckStatus);
    }

    // -----------------------------------------------------------------------
    // Claim boundary flags
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_DryRunOnly_IsTrue()
    {
        Assert.True(BuildValid("dro").DryRunOnly);
    }

    [Fact]
    public void ValidFixture_WriterReady_IsFalse()
    {
        Assert.False(BuildValid("wr").WriterReady);
    }

    [Fact]
    public void ValidFixture_RuntimeValid_IsFalse()
    {
        Assert.False(BuildValid("rv").RuntimeValid);
    }

    [Fact]
    public void ValidFixture_Materialized_IsFalse()
    {
        Assert.False(BuildValid("mat").Materialized);
    }

    [Fact]
    public void ValidFixture_ApprovedForWriterExperiment_IsFalse()
    {
        Assert.False(BuildValid("awe").ApprovedForWriterExperiment);
    }

    [Fact]
    public void ValidFixture_Verdict_IsComplete()
    {
        Assert.Equal(
            "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_COMPLETE",
            BuildValid("vc").Verdict);
    }

    // -----------------------------------------------------------------------
    // Render tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_Has25DataRows()
    {
        var result  = BuildValid("csv25");
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder();
        var csv     = builder.RenderCsv(result);
        var lines   = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(26, lines.Length); // 1 header + 25 data rows
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var result  = BuildValid("mdcb");
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder();
        Assert.Contains("Claim Boundary", builder.RenderMarkdown(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = BuildValid("sumv");
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder();
        Assert.Contains("MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_COMPLETE",
            builder.RenderSummary(result));
    }
}
