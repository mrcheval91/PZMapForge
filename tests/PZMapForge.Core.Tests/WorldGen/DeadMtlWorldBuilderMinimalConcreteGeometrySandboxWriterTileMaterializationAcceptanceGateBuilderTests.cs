using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateBuilderTests
    : IDisposable
{
    private readonly string _tempDir;
    private readonly string _map27eRoot;
    private readonly string _outputRoot;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateBuilderTests()
    {
        _tempDir    = Path.Combine(Path.GetTempPath(), "pzmapforge-map27f-core-test.local", Guid.NewGuid().ToString());
        _map27eRoot = Path.Combine(_tempDir, "map27e.local", "map_00");
        _outputRoot = Path.Combine(_tempDir, "output.local", "map_00");
        Directory.CreateDirectory(_map27eRoot);
        Directory.CreateDirectory(_outputRoot);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private void WriteMap27eFiles()
    {
        var e27Result = new
        {
            format                            = "MAP-27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET",
            map_id                            = "map_00",
            target_component_id               = "map_00_component_0001",
            verdict                           = "MAP27E_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET_COMPLETE",
            is_valid                          = true,
            review_stage                      = "SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET",
            sandbox_only                      = true,
            sandbox_materialized_source       = true,
            visual_qa_overlay_written         = true,
            pz_runtime_materialized           = false,
            reviewed_file_count               = 18,
            check_count                       = 40,
            passed_check_count                = 40,
            failed_check_count                = 0,
            count_match_summary               = "MATERIALIZED_CELL_COUNT(5340) == RENDERED_CELL_COUNT(5340): MATCH",
            overlay_png_width                 = 1024,
            overlay_png_height                = 1024,
            materialization_counts            = new
            {
                materialized_cell_count             = 5340,
                building_wall_candidate_cell_count  = 850,
                building_floor_candidate_cell_count = 2444,
                access_edge_cell_count              = 148,
                lot_space_cell_count                = 1898,
                component_residual_cell_count       = 0,
                material_kind_count                 = 5,
                layer_kind_count                    = 5,
            },
            overlay_counts                    = new
            {
                rendered_cell_count = 5340,
                overlay_width       = 1024,
                overlay_height      = 1024,
                scale               = 4,
            },
            writer_ready                      = false,
            runtime_valid                     = false,
            materialized                      = false,
            runtime_proof_claimed             = false,
            public_playable_packaging_claimed = false,
        };
        File.WriteAllText(Path.Combine(_map27eRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json"),
            JsonSerializer.Serialize(e27Result));
        File.WriteAllText(Path.Combine(_map27eRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.md"),
            "# MAP-27E");
        File.WriteAllText(Path.Combine(_map27eRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.csv"),
            "check_order,check_id,check_status");
        File.WriteAllText(Path.Combine(_map27eRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.summary.txt"),
            "MAP-27E summary");
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateResult BuildValid()
    {
        WriteMap27eFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateBuilder();
        return builder.Build(_map27eRoot, _outputRoot);
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
            "MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_COMPLETE",
            result.Verdict);
    }

    [Fact]
    public void Build_ValidInputs_38ChecksAllPass()
    {
        var result = BuildValid();
        Assert.Equal(38, result.CheckCount);
        Assert.Equal(38, result.PassedCheckCount);
        Assert.Equal(0,  result.FailedCheckCount);
    }

    [Fact]
    public void Build_ValidInputs_AcceptedForNextSandboxExperiment()
    {
        var result = BuildValid();
        Assert.True(result.AcceptedForNextSandboxExperiment);
    }

    [Fact]
    public void Build_ValidInputs_AcceptanceGateStatusCorrect()
    {
        var result = BuildValid();
        Assert.Equal("ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY", result.AcceptanceGateStatus);
    }

    [Fact]
    public void Build_ValidInputs_AcceptedForRuntimeWriterFalse()
    {
        var result = BuildValid();
        Assert.False(result.AcceptedForRuntimeWriter);
    }

    [Fact]
    public void Build_ValidInputs_AcceptedForPlayableExportFalse()
    {
        var result = BuildValid();
        Assert.False(result.AcceptedForPlayableExport);
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
    public void Build_ValidInputs_ReviewedFileCount18()
    {
        var result = BuildValid();
        Assert.Equal(18, result.ReviewedFileCount);
    }

    [Fact]
    public void Build_ValidInputs_ReviewCheckCount40()
    {
        var result = BuildValid();
        Assert.Equal(40, result.ReviewCheckCount);
        Assert.Equal(40, result.ReviewPassedCheckCount);
        Assert.Equal(0,  result.ReviewFailedCheckCount);
    }

    [Fact]
    public void Build_ValidInputs_MaterializedCellCount5340()
    {
        var result = BuildValid();
        Assert.Equal(5340, result.MaterializedCellCount);
    }

    [Fact]
    public void Build_ValidInputs_RenderedCellCount5340()
    {
        var result = BuildValid();
        Assert.Equal(5340, result.RenderedCellCount);
    }

    [Fact]
    public void Build_ValidInputs_WallCount850()
    {
        var result = BuildValid();
        Assert.Equal(850, result.BuildingWallCandidateCellCount);
    }

    [Fact]
    public void Build_ValidInputs_FloorCount2444()
    {
        var result = BuildValid();
        Assert.Equal(2444, result.BuildingFloorCandidateCellCount);
    }

    [Fact]
    public void Build_ValidInputs_AccessCount148()
    {
        var result = BuildValid();
        Assert.Equal(148, result.AccessEdgeCellCount);
    }

    [Fact]
    public void Build_ValidInputs_LotCount1898()
    {
        var result = BuildValid();
        Assert.Equal(1898, result.LotSpaceCellCount);
    }

    [Fact]
    public void Build_ValidInputs_ComponentResidualCount0()
    {
        var result = BuildValid();
        Assert.Equal(0, result.ComponentResidualCellCount);
    }

    [Fact]
    public void Build_ValidInputs_MaterialKindCount5()
    {
        var result = BuildValid();
        Assert.Equal(5, result.MaterialKindCount);
    }

    [Fact]
    public void Build_ValidInputs_LayerKindCount5()
    {
        var result = BuildValid();
        Assert.Equal(5, result.LayerKindCount);
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
    public void Build_ValidInputs_BlockingReasonsEmpty()
    {
        var result = BuildValid();
        Assert.Empty(result.BlockingReasons);
    }

    [Fact]
    public void Build_ValidInputs_AcceptanceReasonsNotEmpty()
    {
        var result = BuildValid();
        Assert.NotEmpty(result.AcceptanceReasons);
        Assert.Equal(7, result.AcceptanceReasons.Count);
    }

    [Fact]
    public void Build_ValidInputs_NextForbiddenSteps11()
    {
        var result = BuildValid();
        Assert.Equal(11, result.NextForbiddenSteps.Count);
    }

    [Fact]
    public void Build_ValidInputs_NextAllowedExperimentCorrect()
    {
        var result = BuildValid();
        Assert.Equal("MAP-27G_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK", result.NextAllowedExperimentName);
        Assert.Equal("SANDBOX_ONLY_NOT_RUNTIME", result.NextAllowedExperimentStatus);
    }

    [Fact]
    public void Build_ValidInputs_AcceptanceCriteriaNotEmpty()
    {
        var result = BuildValid();
        Assert.NotEmpty(result.AcceptanceCriteria);
        Assert.All(result.AcceptanceCriteria, c => Assert.Equal("PASS", c.Status));
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

    [Fact]
    public void Build_ValidInputs_ForbiddenScanPass()
    {
        var result = BuildValid();
        Assert.Contains("PASS", result.ForbiddenArtifactScan);
    }

    [Fact]
    public void Build_ValidInputs_Sha256NotEmpty()
    {
        var result = BuildValid();
        Assert.False(string.IsNullOrEmpty(result.SourceQaReviewPacketSha256));
    }

    [Fact]
    public void Build_ValidInputs_AcceptanceStageCorrect()
    {
        var result = BuildValid();
        Assert.Equal("SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE", result.AcceptanceStage);
    }

    [Fact]
    public void Build_MissingRoot_InvalidResult()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateBuilder();
        var result  = builder.Build(Path.Combine(_tempDir, "nonexistent.local"), _outputRoot);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingRequiredFile_InvalidResult()
    {
        WriteMap27eFiles();
        File.Delete(Path.Combine(_map27eRoot,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_qa_review_packet.json"));
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateBuilder();
        var result  = builder.Build(_map27eRoot, _outputRoot);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}
