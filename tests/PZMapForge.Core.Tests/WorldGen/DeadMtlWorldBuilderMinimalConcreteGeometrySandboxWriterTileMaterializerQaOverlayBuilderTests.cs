using System.Drawing;
using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

#pragma warning disable CA1416

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayBuilderTests
    : IDisposable
{
    private readonly string _tempDir;
    private readonly string _outputRoot;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayBuilderTests()
    {
        _tempDir    = Path.Combine(Path.GetTempPath(), "pzmapforge-map27d-core-test.local", Guid.NewGuid().ToString());
        _outputRoot = Path.Combine(_tempDir, "out.local");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_outputRoot);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    // Synthetic MAP-27C result JSON: 13 materialized cells matching the fixture CSV.
    private string WriteMaterializerResult()
    {
        var obj = new
        {
            format                         = "MAP-27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            map_id                         = "map_00",
            target_component_id            = "map_00_component_0001",
            verdict                        = "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            is_valid                       = true,
            sandbox_only                   = true,
            sandbox_materialized           = true,
            pz_runtime_materialized        = false,
            writer_stage                   = "SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            materialized_cell_count        = 13,
            writer_ready                   = false,
            runtime_valid                  = false,
            materialized                   = false,
            runtime_proof_claimed          = false,
            public_playable_packaging_claimed = false,
        };
        string path = Path.Combine(_tempDir,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    // 9 BUILDING_FOOTPRINT cells (3x3 at (10,10)-(12,12)):
    //   (11,11) = interior FLOOR, all others = WALL
    // 2 ACCESS_LINK, 2 LOT_BOUNDARY = 13 total.
    private string WriteMaterializedCells()
    {
        var sb = new StringBuilder();
        sb.AppendLine("x,y,primary_owner_kind,primary_owner_id,material_kind,layer_kind,source_operation_ids,collision_count");
        for (int x = 10; x <= 12; x++)
        {
            for (int y = 10; y <= 12; y++)
            {
                bool isCenter    = x == 11 && y == 11;
                string matKind   = isCenter ? "BUILDING_INTERIOR_FLOOR_CANDIDATE" : "BUILDING_EXTERIOR_WALL_CANDIDATE";
                string layerKind = isCenter ? "FLOOR" : "WALL";
                sb.AppendLine($"{x},{y},BUILDING_FOOTPRINT,map_00_comp0001_slot_0001,{matKind},{layerKind}," +
                    "map_00.sandbox_writer_building_slot_operations.json#BUILDING_FOOTPRINT_WRITE#1,2");
            }
        }
        sb.AppendLine("5,5,ACCESS_LINK,map_00_comp0001_frontage_access,ACCESS_EDGE_CANDIDATE,ACCESS," +
            "map_00.sandbox_writer_access_operations.json#ACCESS_LINK_WRITE#1,0");
        sb.AppendLine("6,5,ACCESS_LINK,map_00_comp0001_frontage_access,ACCESS_EDGE_CANDIDATE,ACCESS," +
            "map_00.sandbox_writer_access_operations.json#ACCESS_LINK_WRITE#1,0");
        sb.AppendLine("14,14,LOT_BOUNDARY,map_00_comp0001_lot_0001,LOT_YARD_OR_SERVICE_SPACE_CANDIDATE,LOT," +
            "map_00.sandbox_writer_lot_operations.json#LOT_BOUNDARY_WRITE#1,0");
        sb.AppendLine("15,14,LOT_BOUNDARY,map_00_comp0001_lot_0001,LOT_YARD_OR_SERVICE_SPACE_CANDIDATE,LOT," +
            "map_00.sandbox_writer_lot_operations.json#LOT_BOUNDARY_WRITE#1,0");
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_materialized_cells.csv");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private string WriteMaterialPalette()
    {
        var obj = new
        {
            format              = "MAP-27C_SANDBOX_WRITER_TILE_MATERIAL_PALETTE",
            generated_utc       = "2026-06-18T00:00:00Z",
            map_id              = "map_00",
            sandbox_only        = true,
            material_kind_count = 5,
            palette             = Array.Empty<object>(),
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_material_palette.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteLayerStack()
    {
        var obj = new
        {
            format           = "MAP-27C_SANDBOX_WRITER_TILE_LAYER_STACK",
            generated_utc    = "2026-06-18T00:00:00Z",
            map_id           = "map_00",
            sandbox_only     = true,
            layer_kind_count = 5,
            layers           = Array.Empty<object>(),
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_layer_stack.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteMaterializationReplayLog()
    {
        var obj = new
        {
            format             = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOG",
            generated_utc      = "2026-06-18T00:00:00Z",
            map_id             = "map_00",
            sandbox_only       = true,
            replay_entry_count = 0,
            replay_entries     = Array.Empty<object>(),
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_materialization_replay_log.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteMaterializationOwnershipSummary()
    {
        var obj = new
        {
            format           = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZATION_OWNERSHIP_SUMMARY",
            generated_utc    = "2026-06-18T00:00:00Z",
            map_id           = "map_00",
            sandbox_only     = true,
            owner_kind_count = 4,
            ownership        = Array.Empty<object>(),
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_materialization_ownership_summary.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string WriteMaterializerForbiddenOutputGuard()
    {
        string[] families = { "LOT_PACK_RUNTIME_BINARY", "LOT_HEADER_RUNTIME_BINARY", "WORLDGEN_OVERRIDE_LUA",
                               "RUNTIME_LUA", "PROJECT_ZOMBOID_INSTALL_PATH", "STEAM_WORKSHOP_OUTPUT",
                               "COMPILE_WORLDGEN_INVOCATION", "MAP_00_PNG_MUTATION" };
        string[] patterns = { "*.lotpack", "*.lotheader", "WorldGenOverride.lua",
                               "*.lua", "*/Project Zomboid/*", "*/steamapps/workshop/*",
                               "compile-worldgen", "map_00.png" };
        var guards = families.Select((fid, idx) => new
        {
            guard_order     = idx + 1,
            family_id       = fid,
            blocked_pattern = patterns[idx],
            status          = "NOT_EMITTED",
        }).ToArray();
        var obj = new
        {
            format      = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZER_FORBIDDEN_OUTPUT_GUARD",
            guard_count = guards.Length,
            guards,
        };
        string path = Path.Combine(_tempDir, "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayResult RunBuild()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayBuilder();
        return builder.Build(
            WriteMaterializerResult(),
            WriteMaterializedCells(),
            WriteMaterialPalette(),
            WriteLayerStack(),
            WriteMaterializationReplayLog(),
            WriteMaterializationOwnershipSummary(),
            WriteMaterializerForbiddenOutputGuard(),
            _outputRoot);
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact] public void Build_ValidInputs_IsValidTrue()
    { var r = RunBuild(); Assert.True(r.IsValid); }

    [Fact] public void Build_ValidInputs_VerdictComplete()
    {
        var r = RunBuild();
        Assert.Equal(
            "MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_COMPLETE",
            r.Verdict);
    }

    [Fact] public void Build_ValidInputs_SandboxOnlyTrue()
    { var r = RunBuild(); Assert.True(r.SandboxOnly); }

    [Fact] public void Build_ValidInputs_SandboxMaterializedSourceTrue()
    { var r = RunBuild(); Assert.True(r.SandboxMaterializedSource); }

    [Fact] public void Build_ValidInputs_VisualQaOverlayWrittenTrue()
    { var r = RunBuild(); Assert.True(r.VisualQaOverlayWritten); }

    [Fact] public void Build_ValidInputs_PzRuntimeMaterializedFalse()
    { var r = RunBuild(); Assert.False(r.PzRuntimeMaterialized); }

    [Fact] public void Build_ValidInputs_WriterReadyFalse()
    { var r = RunBuild(); Assert.False(r.WriterReady); }

    [Fact] public void Build_ValidInputs_RuntimeValidFalse()
    { var r = RunBuild(); Assert.False(r.RuntimeValid); }

    [Fact] public void Build_ValidInputs_MaterializedFalse()
    { var r = RunBuild(); Assert.False(r.Materialized); }

    [Fact] public void Build_ValidInputs_RuntimeProofClaimedFalse()
    { var r = RunBuild(); Assert.False(r.RuntimeProofClaimed); }

    [Fact] public void Build_ValidInputs_PublicPlayablePackagingClaimedFalse()
    { var r = RunBuild(); Assert.False(r.PublicPlayablePackagingClaimed); }

    [Fact] public void Build_ValidInputs_WriterStageTileMaterializerQaOverlayV0()
    { var r = RunBuild(); Assert.Equal("SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0", r.WriterStage); }

    [Fact] public void Build_ValidInputs_SourceWidth256()
    { var r = RunBuild(); Assert.Equal(256, r.SourceWidth); }

    [Fact] public void Build_ValidInputs_SourceHeight256()
    { var r = RunBuild(); Assert.Equal(256, r.SourceHeight); }

    [Fact] public void Build_ValidInputs_Scale4()
    { var r = RunBuild(); Assert.Equal(4, r.Scale); }

    [Fact] public void Build_ValidInputs_OverlayWidth1024()
    { var r = RunBuild(); Assert.Equal(1024, r.OverlayWidth); }

    [Fact] public void Build_ValidInputs_OverlayHeight1024()
    { var r = RunBuild(); Assert.Equal(1024, r.OverlayHeight); }

    [Fact] public void Build_ValidInputs_InputMaterializedCellCount13()
    { var r = RunBuild(); Assert.Equal(13, r.InputMaterializedCellCount); }

    [Fact] public void Build_ValidInputs_RenderedCellCountMatchesInput()
    { var r = RunBuild(); Assert.Equal(r.InputMaterializedCellCount, r.RenderedCellCount); }

    [Fact] public void Build_ValidInputs_BuildingWallCandidateCellCount8()
    { var r = RunBuild(); Assert.Equal(8, r.BuildingWallCandidateCellCount); }

    [Fact] public void Build_ValidInputs_BuildingFloorCandidateCellCount1()
    { var r = RunBuild(); Assert.Equal(1, r.BuildingFloorCandidateCellCount); }

    [Fact] public void Build_ValidInputs_AccessEdgeCellCount2()
    { var r = RunBuild(); Assert.Equal(2, r.AccessEdgeCellCount); }

    [Fact] public void Build_ValidInputs_LotSpaceCellCount2()
    { var r = RunBuild(); Assert.Equal(2, r.LotSpaceCellCount); }

    [Fact] public void Build_ValidInputs_ComponentResidualCellCount0()
    { var r = RunBuild(); Assert.Equal(0, r.ComponentResidualCellCount); }

    [Fact] public void Build_ValidInputs_MaterialKindCount5()
    { var r = RunBuild(); Assert.Equal(5, r.MaterialKindCount); }

    [Fact] public void Build_ValidInputs_LayerKindCount5()
    { var r = RunBuild(); Assert.Equal(5, r.LayerKindCount); }

    [Fact] public void Build_ValidInputs_CheckCount39()
    { var r = RunBuild(); Assert.Equal(39, r.CheckCount); }

    [Fact] public void Build_ValidInputs_NoFailedChecks()
    { var r = RunBuild(); Assert.Equal(0, r.FailedCheckCount); }

    [Fact] public void Build_ValidInputs_OutputFileCount4()
    { var r = RunBuild(); Assert.Equal(4, r.OutputFileCount); }

    [Fact] public void Build_ValidInputs_OverlayPngWritten()
    {
        var r = RunBuild();
        var pngFile = r.OutputFiles.FirstOrDefault(f => f.FileKind == "QA_OVERLAY_PNG");
        Assert.NotNull(pngFile);
        Assert.True(pngFile!.Written);
        Assert.True(File.Exists(pngFile.FilePath));
    }

    [Fact] public void Build_ValidInputs_LegendJsonWritten()
    {
        var r = RunBuild();
        var f = r.OutputFiles.FirstOrDefault(f => f.FileKind == "QA_OVERLAY_LEGEND_JSON");
        Assert.NotNull(f);
        Assert.True(f!.Written);
        Assert.True(File.Exists(f.FilePath));
    }

    [Fact] public void Build_ValidInputs_CountsCsvWritten()
    {
        var r = RunBuild();
        var f = r.OutputFiles.FirstOrDefault(f => f.FileKind == "QA_OVERLAY_COUNTS_CSV");
        Assert.NotNull(f);
        Assert.True(f!.Written);
        Assert.True(File.Exists(f.FilePath));
    }

    [Fact] public void Build_ValidInputs_ForbiddenGuardWritten()
    {
        var r = RunBuild();
        var f = r.OutputFiles.FirstOrDefault(f => f.FileKind == "QA_OVERLAY_FORBIDDEN_OUTPUT_GUARD_JSON");
        Assert.NotNull(f);
        Assert.True(f!.Written);
        Assert.True(File.Exists(f.FilePath));
    }

    [Fact] public void Build_ValidInputs_AllOutputFilesWrittenTrue()
    { var r = RunBuild(); Assert.True(r.OutputFiles.All(f => f.Written)); }

    [Fact] public void Build_ValidInputs_AllOutputFilesHaveSha256()
    { var r = RunBuild(); Assert.True(r.OutputFiles.All(f => !string.IsNullOrEmpty(f.Sha256))); }

    [Fact] public void Build_ValidInputs_OverlayPngIs1024x1024()
    {
        var r = RunBuild();
        var pngFile = r.OutputFiles.First(f => f.FileKind == "QA_OVERLAY_PNG");
        using var bmp = new Bitmap(pngFile.FilePath);
        Assert.Equal(1024, bmp.Width);
        Assert.Equal(1024, bmp.Height);
    }

    [Fact] public void Build_MissingMaterializerResult_IsValidFalse()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayBuilder();
        var r = builder.Build(
            Path.Combine(_tempDir, "missing.json"),
            WriteMaterializedCells(),
            WriteMaterialPalette(),
            WriteLayerStack(),
            WriteMaterializationReplayLog(),
            WriteMaterializationOwnershipSummary(),
            WriteMaterializerForbiddenOutputGuard(),
            _outputRoot);
        Assert.False(r.IsValid);
    }

    [Fact] public void Build_MissingMaterializedCells_IsValidFalse()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayBuilder();
        var r = builder.Build(
            WriteMaterializerResult(),
            Path.Combine(_tempDir, "missing.csv"),
            WriteMaterialPalette(),
            WriteLayerStack(),
            WriteMaterializationReplayLog(),
            WriteMaterializationOwnershipSummary(),
            WriteMaterializerForbiddenOutputGuard(),
            _outputRoot);
        Assert.False(r.IsValid);
    }

    [Fact] public void RenderSummary_ContainsVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayBuilder();
        var r = RunBuild();
        var summary = builder.RenderSummary(r);
        Assert.Contains("MAP27D_", summary);
    }
}
