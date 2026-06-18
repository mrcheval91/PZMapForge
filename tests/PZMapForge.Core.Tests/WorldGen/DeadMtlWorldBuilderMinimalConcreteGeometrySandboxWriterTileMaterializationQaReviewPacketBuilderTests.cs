using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

#pragma warning disable CA1416

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketBuilderTests
    : IDisposable
{
    private readonly string _tempDir;
    private readonly string _map27cRoot;
    private readonly string _map27dRoot;
    private readonly string _outputRoot;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketBuilderTests()
    {
        _tempDir    = Path.Combine(Path.GetTempPath(), "pzmapforge-map27e-core-test.local", Guid.NewGuid().ToString());
        _map27cRoot = Path.Combine(_tempDir, "map27c.local", "map_00");
        _map27dRoot = Path.Combine(_tempDir, "map27d.local", "map_00");
        _outputRoot = Path.Combine(_tempDir, "output.local", "map_00");
        Directory.CreateDirectory(_map27cRoot);
        Directory.CreateDirectory(_map27dRoot);
        Directory.CreateDirectory(_outputRoot);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private void WriteMap27cFiles()
    {
        var result = new
        {
            format                            = "MAP-27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            map_id                            = "map_00",
            target_component_id               = "map_00_component_0001",
            verdict                           = "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            is_valid                          = true,
            sandbox_only                      = true,
            sandbox_materialized              = true,
            pz_runtime_materialized           = false,
            writer_stage                      = "SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            materialized_cell_count           = 5340,
            writer_ready                      = false,
            runtime_valid                     = false,
            materialized                      = false,
            runtime_proof_claimed             = false,
            public_playable_packaging_claimed = false,
        };
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"),
            JsonSerializer.Serialize(result));
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.md"), "# MAP-27C");
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.csv"), "check_order,check_id,check_status");
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.summary.txt"), "MAP-27C summary");
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.sandbox_writer_tile_materialized_cells.csv"), "x,y,primary_owner_kind,primary_owner_id,material_kind,layer_kind");
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.sandbox_writer_tile_material_palette.json"),
            JsonSerializer.Serialize(new { format = "MAP-27C_SANDBOX_WRITER_TILE_MATERIAL_PALETTE", material_kind_count = 5 }));
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.sandbox_writer_tile_layer_stack.json"),
            JsonSerializer.Serialize(new { format = "MAP-27C_SANDBOX_WRITER_TILE_LAYER_STACK", layer_kind_count = 5 }));
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.sandbox_writer_tile_materialization_replay_log.json"),
            JsonSerializer.Serialize(new { format = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOG", replay_entry_count = 0 }));
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.sandbox_writer_tile_materialization_ownership_summary.json"),
            JsonSerializer.Serialize(new { format = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZATION_OWNERSHIP_SUMMARY", owner_kind_count = 4 }));
        File.WriteAllText(Path.Combine(_map27cRoot,
            "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json"),
            JsonSerializer.Serialize(new { format = "MAP-27C_SANDBOX_WRITER_TILE_MATERIALIZER_FORBIDDEN_OUTPUT_GUARD", guard_count = 0, guards = Array.Empty<object>() }));
    }

    private void WriteMap27dFiles()
    {
        var d27Result = new
        {
            format                            = "MAP-27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0",
            map_id                            = "map_00",
            target_component_id               = "map_00_component_0001",
            verdict                           = "MAP27D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0_COMPLETE",
            is_valid                          = true,
            sandbox_only                      = true,
            sandbox_materialized_source       = true,
            visual_qa_overlay_written         = true,
            pz_runtime_materialized           = false,
            writer_stage                      = "SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0",
            input_materialized_cell_count     = 5340,
            rendered_cell_count               = 5340,
            building_wall_candidate_cell_count  = 850,
            building_floor_candidate_cell_count = 2444,
            access_edge_cell_count            = 148,
            lot_space_cell_count              = 1898,
            component_residual_cell_count     = 0,
            material_kind_count               = 5,
            layer_kind_count                  = 5,
            overlay_width                     = 1024,
            overlay_height                    = 1024,
            scale                             = 4,
            writer_ready                      = false,
            runtime_valid                     = false,
            materialized                      = false,
            runtime_proof_claimed             = false,
            public_playable_packaging_claimed = false,
        };
        File.WriteAllText(Path.Combine(_map27dRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.json"),
            JsonSerializer.Serialize(d27Result));
        File.WriteAllText(Path.Combine(_map27dRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.md"), "# MAP-27D");
        File.WriteAllText(Path.Combine(_map27dRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.csv"), "check_order,check_id,check_status");
        File.WriteAllText(Path.Combine(_map27dRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_qa_overlay_v0.summary.txt"), "MAP-27D summary");

        string pngPath = Path.Combine(_map27dRoot, "map_00.sandbox_writer_tile_materializer_qa_overlay.png");
        using (var bmp = new Bitmap(1024, 1024))
            bmp.Save(pngPath, ImageFormat.Png);

        File.WriteAllText(Path.Combine(_map27dRoot,
            "map_00.sandbox_writer_tile_materializer_qa_overlay_legend.json"),
            JsonSerializer.Serialize(new { format = "MAP-27D_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_LEGEND" }));
        File.WriteAllText(Path.Combine(_map27dRoot,
            "map_00.sandbox_writer_tile_materializer_qa_overlay_counts.csv"), "material_kind,layer_kind,hex_color,cell_count");
        File.WriteAllText(Path.Combine(_map27dRoot,
            "map_00.sandbox_writer_tile_materializer_qa_overlay_forbidden_output_guard.json"),
            JsonSerializer.Serialize(new { format = "MAP-27D_SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_FORBIDDEN_OUTPUT_GUARD" }));
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketResult BuildValid()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketBuilder();
        return builder.Build(_map27cRoot, _map27dRoot, _outputRoot);
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ValidInputs_IsValidTrue()
    {
        var result = BuildValid();
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Build_ValidInputs_VerdictComplete()
    {
        var result = BuildValid();
        Assert.Equal(
            "MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_COMPLETE",
            result.Verdict);
    }

    [Fact]
    public void Build_ValidInputs_40ChecksAllPass()
    {
        var result = BuildValid();
        Assert.Equal(40, result.CheckCount);
        Assert.Equal(40, result.PassedCheckCount);
        Assert.Equal(0,  result.FailedCheckCount);
    }

    [Fact]
    public void Build_ValidInputs_ReviewedFileCount18()
    {
        var result = BuildValid();
        Assert.Equal(18, result.ReviewedFileCount);
        Assert.Equal(18, result.ReviewedFiles.Count);
    }

    [Fact]
    public void Build_ValidInputs_SandboxOnlyTrue()
    {
        var result = BuildValid();
        Assert.True(result.SandboxOnly);
    }

    [Fact]
    public void Build_ValidInputs_SandboxMaterializedSourceTrue()
    {
        var result = BuildValid();
        Assert.True(result.SandboxMaterializedSource);
    }

    [Fact]
    public void Build_ValidInputs_VisualQaOverlayWrittenTrue()
    {
        var result = BuildValid();
        Assert.True(result.VisualQaOverlayWritten);
    }

    [Fact]
    public void Build_ValidInputs_PzRuntimeMaterializedFalse()
    {
        var result = BuildValid();
        Assert.False(result.PzRuntimeMaterialized);
    }

    [Fact]
    public void Build_ValidInputs_WriterReadyFalse()
    {
        var result = BuildValid();
        Assert.False(result.WriterReady);
    }

    [Fact]
    public void Build_ValidInputs_RuntimeValidFalse()
    {
        var result = BuildValid();
        Assert.False(result.RuntimeValid);
    }

    [Fact]
    public void Build_ValidInputs_MaterializedFalse()
    {
        var result = BuildValid();
        Assert.False(result.Materialized);
    }

    [Fact]
    public void Build_ValidInputs_RuntimeProofClaimedFalse()
    {
        var result = BuildValid();
        Assert.False(result.RuntimeProofClaimed);
    }

    [Fact]
    public void Build_ValidInputs_PublicPlayablePackagingClaimedFalse()
    {
        var result = BuildValid();
        Assert.False(result.PublicPlayablePackagingClaimed);
    }

    [Fact]
    public void Build_ValidInputs_MaterializationCounts5340()
    {
        var result = BuildValid();
        Assert.Equal(5340, result.MaterializationCounts.MaterializedCellCount);
    }

    [Fact]
    public void Build_ValidInputs_RenderedCellCount5340()
    {
        var result = BuildValid();
        Assert.Equal(5340, result.OverlayCounts.RenderedCellCount);
    }

    [Fact]
    public void Build_ValidInputs_WallCount850()
    {
        var result = BuildValid();
        Assert.Equal(850, result.MaterializationCounts.BuildingWallCandidateCellCount);
    }

    [Fact]
    public void Build_ValidInputs_FloorCount2444()
    {
        var result = BuildValid();
        Assert.Equal(2444, result.MaterializationCounts.BuildingFloorCandidateCellCount);
    }

    [Fact]
    public void Build_ValidInputs_AccessCount148()
    {
        var result = BuildValid();
        Assert.Equal(148, result.MaterializationCounts.AccessEdgeCellCount);
    }

    [Fact]
    public void Build_ValidInputs_LotCount1898()
    {
        var result = BuildValid();
        Assert.Equal(1898, result.MaterializationCounts.LotSpaceCellCount);
    }

    [Fact]
    public void Build_ValidInputs_ComponentResidualCount0()
    {
        var result = BuildValid();
        Assert.Equal(0, result.MaterializationCounts.ComponentResidualCellCount);
    }

    [Fact]
    public void Build_ValidInputs_MaterialKindCount5()
    {
        var result = BuildValid();
        Assert.Equal(5, result.MaterializationCounts.MaterialKindCount);
    }

    [Fact]
    public void Build_ValidInputs_LayerKindCount5()
    {
        var result = BuildValid();
        Assert.Equal(5, result.MaterializationCounts.LayerKindCount);
    }

    [Fact]
    public void Build_ValidInputs_OverlayPngWidth1024()
    {
        var result = BuildValid();
        Assert.Equal(1024, result.OverlayPngWidth);
    }

    [Fact]
    public void Build_ValidInputs_OverlayPngHeight1024()
    {
        var result = BuildValid();
        Assert.Equal(1024, result.OverlayPngHeight);
    }

    [Fact]
    public void Build_ValidInputs_CountMatchSummaryContainsMatch()
    {
        var result = BuildValid();
        Assert.Contains("MATCH", result.CountMatchSummary);
    }

    [Fact]
    public void Build_ValidInputs_ReviewStageCorrect()
    {
        var result = BuildValid();
        Assert.Equal("SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET", result.ReviewStage);
    }

    [Fact]
    public void Build_ValidInputs_10Map27cFilesSourceStage()
    {
        var result = BuildValid();
        var map27cFiles = result.ReviewedFiles.Where(f => f.SourceStage == "MAP-27C").ToList();
        Assert.Equal(10, map27cFiles.Count);
    }

    [Fact]
    public void Build_ValidInputs_8Map27dFilesSourceStage()
    {
        var result = BuildValid();
        var map27dFiles = result.ReviewedFiles.Where(f => f.SourceStage == "MAP-27D").ToList();
        Assert.Equal(8, map27dFiles.Count);
    }

    [Fact]
    public void Build_ValidInputs_AllReviewedFilesHaveSha256()
    {
        var result = BuildValid();
        Assert.All(result.ReviewedFiles, f => Assert.False(string.IsNullOrEmpty(f.Sha256)));
    }

    [Fact]
    public void Build_MissingMap27cRoot_InvalidResult()
    {
        WriteMap27dFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketBuilder();
        var result  = builder.Build(Path.Combine(_tempDir, "nonexistent.local"), _map27dRoot, _outputRoot);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingMap27dRoot_InvalidResult()
    {
        WriteMap27cFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketBuilder();
        var result  = builder.Build(_map27cRoot, Path.Combine(_tempDir, "nonexistent.local"), _outputRoot);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingRequiredFile_InvalidResult()
    {
        WriteMap27cFiles();
        WriteMap27dFiles();
        File.Delete(Path.Combine(_map27dRoot, "map_00.sandbox_writer_tile_materializer_qa_overlay.png"));
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketBuilder();
        var result  = builder.Build(_map27cRoot, _map27dRoot, _outputRoot);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_ValidInputs_OverlayPngSha256NotEmpty()
    {
        var result = BuildValid();
        Assert.False(string.IsNullOrEmpty(result.OverlayPngSha256));
    }

    [Fact]
    public void Build_ValidInputs_ForbiddenScanPass()
    {
        var result = BuildValid();
        Assert.Contains("PASS", result.ForbiddenArtifactScan);
    }

    [Fact]
    public void Build_ValidInputs_TargetComponentId()
    {
        var result = BuildValid();
        Assert.Equal("map_00_component_0001", result.TargetComponentId);
    }

    [Fact]
    public void Build_ValidInputs_MapIdIsMap00()
    {
        var result = BuildValid();
        Assert.Equal("map_00", result.MapId);
    }
}
