using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-bspp-tests", Path.GetRandomFileName());

    public DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilderTests() =>
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

    private static string RealSidewalkPlanPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-sidewalk-generation-plan",
            "map_00", "map_00.sidewalk_generation_plan.json");

    private DeadMtlWorldBuilderBuildingSelectionPolicyPlanResult RunReal() =>
        DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.Build(
            RealProfilePath, RealMetadataPath, RealLotPlanPath, RealSidewalkPlanPath);

    private string WriteJson(string name, object obj)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, JsonSerializer.Serialize(obj), Encoding.UTF8);
        return path;
    }

    private string WriteProfile() => WriteJson("profile.json", new
    {
        format = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
        profile_id = "test",
        profile_status = "AUTHORING_PROFILE_ONLY",
        sidewalk_policy = new { default_has_sidewalks = true, sidewalk_source = "INSIDE_STREET_ZONE",
            default_left_width_tiles = 2, default_right_width_tiles = 2 },
    });

    private string WriteMetadata(string tileId = "test") => WriteJson($"{tileId}.meta.json", new
    {
        format = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
        tile_id = tileId,
        metadata_status = "AUTHORING_METADATA_ONLY",
        color_roles = Array.Empty<object>(),
        claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false,
            runtime_proven = false, public_playable_claim = false, writer_ready_claim = false,
            generates_buildings_now = false, generates_sidewalks_now = false,
            subdivides_lots_now = false, captures_chunk_layers_now = false },
    });

    private string WriteLotPlan() => WriteJson("lot_plan.json", new
    {
        format = "pzmapforge.deadmtl.worldbuilder.lot-subdivision-plan.v1",
        tile_id = "test",
        status = "LOT_SUBDIVISION_PLAN_CONTRACT_ONLY",
        subdivision_status = "NOT_EXECUTED",
        plan_items = Array.Empty<object>(),
        totals = new { plan_item_count = 0 },
        claim_boundary = new { writes_lotpack = false },
    });

    private string WriteSidewalkPlan() => WriteJson("sidewalk_plan.json", new
    {
        format = "pzmapforge.deadmtl.worldbuilder.sidewalk-generation-plan.v1",
        tile_id = "test",
        status = "SIDEWALK_GENERATION_PLAN_CONTRACT_ONLY",
        generation_status = "NOT_EXECUTED",
        plan_items = Array.Empty<object>(),
        totals = new { plan_item_count = 0 },
        claim_boundary = new { writes_lotpack = false },
    });

    // -----------------------------------------------------------------------
    // Missing file errors
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Error_WhenProfileMissing()
    {
        var result = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.Build(
            Path.Combine(_tempDir, "no.json"), WriteMetadata(), WriteLotPlan(), WriteSidewalkPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("profile"));
    }

    [Fact]
    public void Build_Error_WhenMetadataMissing()
    {
        var result = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.Build(
            WriteProfile(), Path.Combine(_tempDir, "no.json"), WriteLotPlan(), WriteSidewalkPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public void Build_Error_WhenLotPlanMissing()
    {
        var result = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.Build(
            WriteProfile(), WriteMetadata(), Path.Combine(_tempDir, "no.json"), WriteSidewalkPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public void Build_Error_WhenSidewalkPlanMissing()
    {
        var result = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.Build(
            WriteProfile(), WriteMetadata(), WriteLotPlan(), Path.Combine(_tempDir, "no.json"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    // -----------------------------------------------------------------------
    // Real map_00 — top-level
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_IsValid_ForRealMap00()
    {
        var result = RunReal();
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Build_Format_IsCorrect()
    {
        var result = RunReal();
        Assert.Equal("pzmapforge.deadmtl.worldbuilder.building-selection-policy-plan.v1", result.Plan.Format);
    }

    [Fact]
    public void Build_TileId_IsMap00()
    {
        var result = RunReal();
        Assert.Equal("map_00", result.Plan.TileId);
    }

    [Fact]
    public void Build_Status_IsContractOnly()
    {
        var result = RunReal();
        Assert.Equal("BUILDING_SELECTION_POLICY_PLAN_CONTRACT_ONLY", result.Plan.Status);
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
    public void Build_PlacementStatus_IsNotPlaced()
    {
        var result = RunReal();
        Assert.Equal("NOT_PLACED", result.Plan.PlacementStatus);
    }

    // -----------------------------------------------------------------------
    // Real map_00 — totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_PlanItemCount_IsSeven()
    {
        var result = RunReal();
        Assert.Equal(7, result.Plan.Totals.PlanItemCount);
    }

    [Fact]
    public void Build_ResidentialSelectionLaterCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.ResidentialSelectionLaterCount);
    }

    [Fact]
    public void Build_CommercialSelectionLaterCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.CommercialSelectionLaterCount);
    }

    [Fact]
    public void Build_UniqueBuildingRequiredLaterCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.UniqueBuildingRequiredLaterCount);
    }

    [Fact]
    public void Build_GreenspaceNoSelectionCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.GreenspaceNoSelectionCount);
    }

    [Fact]
    public void Build_StreetNoSelectionCount_IsTwo()
    {
        var result = RunReal();
        Assert.Equal(2, result.Plan.Totals.StreetNoSelectionCount);
    }

    [Fact]
    public void Build_IgnoreCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.IgnoreCount);
    }

    [Fact]
    public void Build_ConcreteBuildingIdsSelectedNowCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Plan.Totals.ConcreteBuildingIdsSelectedNowCount);
    }

    // -----------------------------------------------------------------------
    // Residential item
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ResidentialItem_HasResidentialSelectionAction()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "RESIDENTIAL");
        Assert.Equal("PLAN_SELECT_RESIDENTIAL_BUILDING_FAMILY_LATER", item.SelectionAction);
    }

    [Fact]
    public void Build_ResidentialItem_HasFutureFitBuildingToLot()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "RESIDENTIAL");
        Assert.Equal("FUTURE_FIT_BUILDING_TO_LOT", item.PlacementMode);
    }

    [Fact]
    public void Build_ResidentialItem_HasPreferMainRoadFrontage()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "RESIDENTIAL");
        Assert.Equal("PREFER_MAIN_ROAD_FRONTAGE", item.FrontageRequirement);
    }

    [Fact]
    public void Build_ResidentialItem_HasRowUniformFrontage()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "RESIDENTIAL");
        Assert.Equal("ROW_UNIFORM_FRONTAGE", item.FacadeRule);
    }

    [Fact]
    public void Build_ResidentialItem_AllowedFamiliesIncludeDuplexTriplex()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "RESIDENTIAL");
        Assert.Contains("duplex",           item.AllowedBuildingFamilies);
        Assert.Contains("triplex",          item.AllowedBuildingFamilies);
        Assert.Contains("plex_block",       item.AllowedBuildingFamilies);
        Assert.Contains("apartment_lowrise", item.AllowedBuildingFamilies);
    }

    // -----------------------------------------------------------------------
    // Commercial item
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CommercialItem_HasCommercialSelectionAction()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "COMMERCIAL");
        Assert.Equal("PLAN_SELECT_COMMERCIAL_BUILDING_FAMILY_LATER", item.SelectionAction);
    }

    [Fact]
    public void Build_CommercialItem_HasCommercialFrontage()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "COMMERCIAL");
        Assert.Equal("COMMERCIAL_FRONTAGE", item.FacadeRule);
    }

    [Fact]
    public void Build_CommercialItem_AllowedFamiliesIncludeStorefront()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "COMMERCIAL");
        Assert.Contains("depanneur",              item.AllowedBuildingFamilies);
        Assert.Contains("pharmacy",               item.AllowedBuildingFamilies);
        Assert.Contains("restaurant",             item.AllowedBuildingFamilies);
        Assert.Contains("main_street_storefront", item.AllowedBuildingFamilies);
        Assert.Contains("office_small",           item.AllowedBuildingFamilies);
    }

    // -----------------------------------------------------------------------
    // Civic unique placeholder
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CivicItem_RequiresUniqueBuildingIdLater()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.Role == "UNIQUE_PLACEHOLDER");
        Assert.Equal("PLAN_REQUIRE_UNIQUE_BUILDING_ID_LATER", item.SelectionAction);
        Assert.Equal("FUTURE_UNIQUE_PLACEHOLDER_BINDING",     item.PlacementMode);
    }

    [Fact]
    public void Build_CivicItem_AllowedFamiliesIncludeCivic()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.Role == "UNIQUE_PLACEHOLDER");
        Assert.Contains("government",       item.AllowedBuildingFamilies);
        Assert.Contains("library",          item.AllowedBuildingFamilies);
        Assert.Contains("community_center", item.AllowedBuildingFamilies);
        Assert.Contains("institutional",    item.AllowedBuildingFamilies);
        Assert.Contains("special_building", item.AllowedBuildingFamilies);
    }

    // -----------------------------------------------------------------------
    // Greenspace, streets, ignore
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_GreenspaceItem_HasNoBuildingPlacement()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "GREENSPACE");
        Assert.Equal("PLAN_NO_BUILDING_SELECTION_GREENSPACE", item.SelectionAction);
        Assert.Equal("NO_BUILDING_PLACEMENT",                 item.PlacementMode);
    }

    [Fact]
    public void Build_MainRoadItem_HasNoBuildingPlacement()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "MAIN_ROAD");
        Assert.Equal("PLAN_NO_BUILDING_SELECTION_STREET", item.SelectionAction);
        Assert.Equal("NO_BUILDING_PLACEMENT",             item.PlacementMode);
    }

    [Fact]
    public void Build_BackAlleyItem_HasNoBuildingPlacement()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "BACK_ALLEY");
        Assert.Equal("PLAN_NO_BUILDING_SELECTION_STREET", item.SelectionAction);
        Assert.Equal("NO_BUILDING_PLACEMENT",             item.PlacementMode);
    }

    [Fact]
    public void Build_IgnoreItem_HasPlanIgnore()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.Role == "IGNORE");
        Assert.Equal("PLAN_IGNORE",           item.SelectionAction);
        Assert.Equal("NO_BUILDING_PLACEMENT", item.PlacementMode);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
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
        Assert.False(cb.SelectsConcreteBuildingIdsNow);
    }

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("MAP25E_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT_COMPLETE", md);
    }

    [Fact]
    public void RenderMarkdown_ContainsResidentialSection()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Residential Selection Policy", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsCommercialSection()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Commercial Selection Policy", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsCivicSection()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Civic Unique Placeholder Policy", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsGreenspaceSection()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Greenspace Exclusion", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsStreetSection()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Street and Back Alley Exclusion", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("Claim Boundary", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result = RunReal();
        var csv = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderCsv(result.Plan);
        Assert.Contains("color,role,zone_type,street_class,selection_action", csv);
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result = RunReal();
        var summary = DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder.RenderSummary(result);
        Assert.Contains("MAP25E_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT_COMPLETE", summary);
    }
}
