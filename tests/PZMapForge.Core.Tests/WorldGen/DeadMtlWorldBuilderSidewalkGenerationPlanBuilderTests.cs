using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderSidewalkGenerationPlanBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-sidewalk-plan-tests", Path.GetRandomFileName());

    public DeadMtlWorldBuilderSidewalkGenerationPlanBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string RealProfilePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "neighborhoods",
            "deadmtl_baseline_neighborhood_profile.json");

    private static string RealMetadataPath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "tiles",
            "map_00.zone_metadata.json");

    private static string RealLotPlanPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-lot-subdivision-plan",
            "map_00", "map_00.lot_subdivision_plan.json");

    private DeadMtlWorldBuilderSidewalkGenerationPlanResult RunReal() =>
        DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.Build(
            RealProfilePath, RealMetadataPath, RealLotPlanPath);

    private string WriteMetadata(string tileId, object[] colorRoles)
    {
        var path = Path.Combine(_tempDir, $"{tileId}.zone_metadata.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id = tileId,
            metadata_status = "AUTHORING_METADATA_ONLY",
            color_roles = colorRoles,
            claim_boundary = new
            {
                writes_lotpack = false,
                writes_worldgen_lua = false,
                runtime_proven = false,
                public_playable_claim = false,
                writer_ready_claim = false,
                generates_buildings_now = false,
                generates_sidewalks_now = false,
                subdivides_lots_now = false,
                captures_chunk_layers_now = false,
            },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj), Encoding.UTF8);
        return path;
    }

    private string WriteProfile(int leftWidth = 2, int rightWidth = 2, string swSource = "INSIDE_STREET_ZONE")
    {
        var path = Path.Combine(_tempDir, "test_profile.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "test_baseline",
            profile_status = "AUTHORING_PROFILE_ONLY",
            sidewalk_policy = new
            {
                default_has_sidewalks = true,
                sidewalk_source = swSource,
                default_left_width_tiles = leftWidth,
                default_right_width_tiles = rightWidth,
                allow_asymmetric_sidewalks = true,
                allow_street_override = true,
            },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj), Encoding.UTF8);
        return path;
    }

    private string WriteLotPlan(string tileId = "test")
    {
        var path = Path.Combine(_tempDir, $"{tileId}.lot_subdivision_plan.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.lot-subdivision-plan.v1",
            tile_id = tileId,
            status = "LOT_SUBDIVISION_PLAN_CONTRACT_ONLY",
            subdivision_status = "NOT_EXECUTED",
            plan_items = Array.Empty<object>(),
            totals = new { plan_item_count = 0 },
            claim_boundary = new { writes_lotpack = false },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj), Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Missing file errors
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Error_WhenProfileMissing()
    {
        var result = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.Build(
            Path.Combine(_tempDir, "no_profile.json"),
            WriteMetadata("t1", Array.Empty<object>()),
            WriteLotPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("profile"));
    }

    [Fact]
    public void Build_Error_WhenMetadataMissing()
    {
        var result = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.Build(
            WriteProfile(),
            Path.Combine(_tempDir, "no_metadata.json"),
            WriteLotPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public void Build_Error_WhenLotPlanMissing()
    {
        var result = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.Build(
            WriteProfile(),
            WriteMetadata("t2", Array.Empty<object>()),
            Path.Combine(_tempDir, "no_lot_plan.json"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    // -----------------------------------------------------------------------
    // Real map_00 results
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_IsValid_ForRealMap00()
    {
        var result = RunReal();
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Build_TileId_IsMap00()
    {
        var result = RunReal();
        Assert.Equal("map_00", result.Plan.TileId);
    }

    [Fact]
    public void Build_Format_IsCorrect()
    {
        var result = RunReal();
        Assert.Equal("pzmapforge.deadmtl.worldbuilder.sidewalk-generation-plan.v1", result.Plan.Format);
    }

    [Fact]
    public void Build_Status_IsContractOnly()
    {
        var result = RunReal();
        Assert.Equal("SIDEWALK_GENERATION_PLAN_CONTRACT_ONLY", result.Plan.Status);
    }

    [Fact]
    public void Build_RuntimeStatus_IsNotRuntimeProven()
    {
        var result = RunReal();
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Plan.RuntimeStatus);
    }

    [Fact]
    public void Build_WriterStatus_IsNotImplemented()
    {
        var result = RunReal();
        Assert.Equal("NOT_IMPLEMENTED", result.Plan.WriterStatus);
    }

    [Fact]
    public void Build_GenerationStatus_IsNotExecuted()
    {
        var result = RunReal();
        Assert.Equal("NOT_EXECUTED", result.Plan.GenerationStatus);
    }

    [Fact]
    public void Build_PlanItemCount_IsTwo_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(2, result.Plan.Totals.PlanItemCount);
    }

    [Fact]
    public void Build_MainRoadSidewalkLaterCount_IsOne_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.MainRoadSidewalkLaterCount);
    }

    [Fact]
    public void Build_BackAlleyNoSidewalkCount_IsOne_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.BackAlleyNoSidewalkCount);
    }

    [Fact]
    public void Build_LeftWidthTilesTotal_IsTwo_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(2, result.Plan.Totals.LeftWidthTilesTotal);
    }

    [Fact]
    public void Build_RightWidthTilesTotal_IsTwo_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(2, result.Plan.Totals.RightWidthTilesTotal);
    }

    [Fact]
    public void Build_SidewalkSource_IsInsideStreetZone_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal("INSIDE_STREET_ZONE", result.Plan.Totals.SidewalkSource);
    }

    // -----------------------------------------------------------------------
    // Plan item policies
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MainRoadItem_HasSidewalkLaterAction()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "MAIN_ROAD");
        Assert.Equal("PLAN_GENERATE_SIDEWALKS_LATER", item.SidewalkAction);
    }

    [Fact]
    public void Build_MainRoadItem_UsesProfileWidths()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "MAIN_ROAD");
        Assert.Equal(2, item.LeftWidthTiles);
        Assert.Equal(2, item.RightWidthTiles);
    }

    [Fact]
    public void Build_MainRoadItem_UsesInsideStreetZone()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "MAIN_ROAD");
        Assert.Equal("INSIDE_STREET_ZONE", item.SidewalkSource);
    }

    [Fact]
    public void Build_MainRoadItem_SourcePolicy_IsNeighborhoodProfile()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "MAIN_ROAD");
        Assert.Equal("NEIGHBORHOOD_PROFILE", item.SourcePolicy);
    }

    [Fact]
    public void Build_BackAlleyItem_HasNoSidewalkAction()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "BACK_ALLEY");
        Assert.Equal("PLAN_NO_SIDEWALKS", item.SidewalkAction);
    }

    [Fact]
    public void Build_BackAlleyItem_WidthIsZero()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "BACK_ALLEY");
        Assert.Equal(0, item.LeftWidthTiles);
        Assert.Equal(0, item.RightWidthTiles);
    }

    [Fact]
    public void Build_BackAlleyItem_SourceIsNone()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "BACK_ALLEY");
        Assert.Equal("NONE", item.SidewalkSource);
    }

    [Fact]
    public void Build_BackAlleyItem_SourcePolicy_IsBackAlleyRule()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "BACK_ALLEY");
        Assert.Equal("BACK_ALLEY_RULE", item.SourcePolicy);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse_ForRealMap00()
    {
        var result = RunReal();
        var cb = result.Plan.ClaimBoundary;
        Assert.False(cb.WritesLotpack);
        Assert.False(cb.WritesWorldgenLua);
        Assert.False(cb.RuntimeProven);
        Assert.False(cb.PublicPlayableClaim);
        Assert.False(cb.WriterReadyClaim);
        Assert.False(cb.GeneratesBuildingsNow);
        Assert.False(cb.GeneratesSidewalksNow);
        Assert.False(cb.SubdividesLotsNow);
        Assert.False(cb.CapturesChunkLayersNow);
        Assert.False(cb.PlacesFencesNow);
        Assert.False(cb.PlacesUniqueBuildingsNow);
    }

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("MAP25D_WORLDBUILDER_SIDEWALK_GENERATION_PLAN_CONTRACT_COMPLETE", md);
    }

    [Fact]
    public void RenderMarkdown_ContainsSidewalkSection()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Sidewalk", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsBackAlleySection()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Back Alley", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Claim Boundary", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result = RunReal();
        var csv = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.RenderCsv(result.Plan);
        Assert.Contains("color,role,street_class,sidewalk_action", csv);
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result = RunReal();
        var summary = DeadMtlWorldBuilderSidewalkGenerationPlanBuilder.RenderSummary(result);
        Assert.Contains("MAP25D_WORLDBUILDER_SIDEWALK_GENERATION_PLAN_CONTRACT_COMPLETE", summary);
    }
}
