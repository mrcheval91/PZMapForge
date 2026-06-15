using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderLotSubdivisionPlanBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-lot-plan-tests", Path.GetRandomFileName());

    public DeadMtlWorldBuilderLotSubdivisionPlanBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string RealMetadataPath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "tiles",
            "map_00.zone_metadata.json");

    private static string RealProfilePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "neighborhoods",
            "deadmtl_baseline_neighborhood_profile.json");

    private DeadMtlWorldBuilderLotSubdivisionPlanResult RunReal(string inspectionPath = "") =>
        DeadMtlWorldBuilderLotSubdivisionPlanBuilder.Build(
            RealMetadataPath, RealProfilePath, inspectionPath);

    private string WriteMetadata(string tileId, object[] colorRoles, object? claimBoundary = null)
    {
        var cb = claimBoundary ?? new
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
        };
        var path = Path.Combine(_tempDir, $"{tileId}.zone_metadata.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id = tileId,
            metadata_status = "AUTHORING_METADATA_ONLY",
            color_roles = colorRoles,
            claim_boundary = cb,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj), Encoding.UTF8);
        return path;
    }

    private string WriteProfile(string neighborhoodId = "test_baseline")
    {
        var path = Path.Combine(_tempDir, $"{neighborhoodId}.json");
        var obj = new
        {
            format = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            neighborhood_id = neighborhoodId,
            status = "PROFILE_CONTRACT_ONLY",
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj), Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Missing file errors
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Error_WhenMetadataFileMissing()
    {
        var result = DeadMtlWorldBuilderLotSubdivisionPlanBuilder.Build(
            Path.Combine(_tempDir, "no_such.json"), WriteProfile(), "");
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public void Build_Error_WhenProfileFileMissing()
    {
        var meta = WriteMetadata("t1", new object[]
        {
            new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", street_class = "" }
        });
        var result = DeadMtlWorldBuilderLotSubdivisionPlanBuilder.Build(
            meta, Path.Combine(_tempDir, "no_profile.json"), "");
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
    public void Build_TileId_IsMap00_ForRealMetadata()
    {
        var result = RunReal();
        Assert.Equal("map_00", result.Plan.TileId);
    }

    [Fact]
    public void Build_PlanItemCount_IsSeven_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(7, result.Plan.Totals.PlanItemCount);
    }

    [Fact]
    public void Build_ResidentialSubdivideLaterCount_IsOne_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.ResidentialSubdivideLaterCount);
    }

    [Fact]
    public void Build_CommercialSubdivideLaterCount_IsOne_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.CommercialSubdivideLaterCount);
    }

    [Fact]
    public void Build_StreetNoSubdivisionCount_IsTwo_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(2, result.Plan.Totals.StreetNoSubdivisionCount);
    }

    [Fact]
    public void Build_UniquePlaceholderCount_IsOne_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.UniquePlaceholderNoSubdivisionCount);
    }

    [Fact]
    public void Build_IgnoreCount_IsOne_ForRealMap00()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.IgnoreCount);
    }

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
    // Plan item policies
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ResidentialItem_HasCorrectPolicies()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "RESIDENTIAL");
        Assert.Equal("PLAN_SUBDIVIDE_RESIDENTIAL_LATER",              item.SubdivisionAction);
        Assert.Equal("PREFER_MAIN_ROAD",                              item.FrontagePolicy);
        Assert.Equal("ALLOW_BACK_ALLEY_REAR_ACCESS",                  item.RearAccessPolicy);
        Assert.Equal("FUTURE_FROM_NEIGHBORHOOD_PROFILE_ON_MAIN_ROAD", item.SidewalkGenerationPolicy);
        Assert.Equal("FUTURE_LOT_LINES_AND_FENCES",                   item.LotLinePolicy);
        Assert.Equal("FUTURE_FIT_BUILDING_TO_LOT",                    item.BuildingSelectionPolicy);
        Assert.Equal("ROW_UNIFORM_FRONTAGE",                          item.FacadeOrientationPolicy);
    }

    [Fact]
    public void Build_CommercialItem_HasCorrectPolicies()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "COMMERCIAL");
        Assert.Equal("PLAN_SUBDIVIDE_COMMERCIAL_LATER",               item.SubdivisionAction);
        Assert.Equal("PREFER_MAIN_ROAD",                              item.FrontagePolicy);
        Assert.Equal("BACK_ALLEY_SERVICE_ONLY",                       item.RearAccessPolicy);
        Assert.Equal("FUTURE_FROM_NEIGHBORHOOD_PROFILE_ON_MAIN_ROAD", item.SidewalkGenerationPolicy);
        Assert.Equal("FUTURE_LOT_LINES_AND_FENCES",                   item.LotLinePolicy);
        Assert.Equal("FUTURE_FIT_BUILDING_TO_LOT",                    item.BuildingSelectionPolicy);
        Assert.Equal("COMMERCIAL_FRONTAGE",                           item.FacadeOrientationPolicy);
    }

    [Fact]
    public void Build_MainRoadItem_HasCorrectPolicies()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "MAIN_ROAD");
        Assert.Equal("PLAN_NO_SUBDIVISION_STREET_CORRIDOR",           item.SubdivisionAction);
        Assert.Equal("NOT_APPLICABLE",                                item.FrontagePolicy);
        Assert.Equal("FUTURE_FROM_NEIGHBORHOOD_PROFILE_ON_MAIN_ROAD", item.SidewalkGenerationPolicy);
        Assert.Equal("NO_BUILDING_SELECTION",                         item.BuildingSelectionPolicy);
    }

    [Fact]
    public void Build_BackAlleyItem_HasCorrectPolicies()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.StreetClass == "BACK_ALLEY");
        Assert.Equal("PLAN_NO_SUBDIVISION_STREET_CORRIDOR", item.SubdivisionAction);
        Assert.Equal("NO_PRIMARY_FRONTAGE",                 item.FrontagePolicy);
        Assert.Equal("BACK_ALLEY_SERVICE_ONLY",             item.RearAccessPolicy);
        Assert.Equal("NO_SIDEWALKS",                        item.SidewalkGenerationPolicy);
        Assert.Equal("NOT_APPLICABLE",                      item.LotLinePolicy);
        Assert.Equal("NO_BUILDING_SELECTION",               item.BuildingSelectionPolicy);
        Assert.Equal("NO_PRIMARY_FACADE",                   item.FacadeOrientationPolicy);
    }

    [Fact]
    public void Build_GreenspaceItem_HasCorrectPolicies()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.ZoneType == "GREENSPACE");
        Assert.Equal("PLAN_NO_SUBDIVISION_GREENSPACE", item.SubdivisionAction);
        Assert.Equal("NOT_APPLICABLE",                 item.FrontagePolicy);
        Assert.Equal("NOT_APPLICABLE",                 item.SidewalkGenerationPolicy);
        Assert.Equal("NOT_APPLICABLE",                 item.BuildingSelectionPolicy);
    }

    [Fact]
    public void Build_UniquePlaceholderItem_HasCorrectPolicies()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.Role == "UNIQUE_PLACEHOLDER");
        Assert.Equal("PLAN_NO_SUBDIVISION_UNIQUE_PLACEHOLDER", item.SubdivisionAction);
        Assert.Equal("MAIN_ROAD_ONLY_IF_AVAILABLE",            item.FrontagePolicy);
        Assert.Equal("NOT_APPLICABLE",                         item.RearAccessPolicy);
        Assert.Equal("NOT_APPLICABLE",                         item.SidewalkGenerationPolicy);
        Assert.Equal("NOT_APPLICABLE",                         item.LotLinePolicy);
        Assert.Equal("FUTURE_UNIQUE_BUILDING_ID_REQUIRED",     item.BuildingSelectionPolicy);
        Assert.Equal("NOT_APPLICABLE",                         item.FacadeOrientationPolicy);
    }

    [Fact]
    public void Build_IgnoreItem_HasCorrectPolicies()
    {
        var result = RunReal();
        var item = result.Plan.PlanItems.Single(i => i.Role == "IGNORE");
        Assert.Equal("PLAN_IGNORE",           item.SubdivisionAction);
        Assert.Equal("NOT_APPLICABLE",        item.FrontagePolicy);
        Assert.Equal("NOT_APPLICABLE",        item.SidewalkGenerationPolicy);
        Assert.Equal("NOT_APPLICABLE",        item.LotLinePolicy);
        Assert.Equal("NO_BUILDING_SELECTION", item.BuildingSelectionPolicy);
        Assert.Equal("NOT_APPLICABLE",        item.FacadeOrientationPolicy);
    }

    // -----------------------------------------------------------------------
    // Color normalization guards
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Error_WhenWrongGreenNormalizedColor()
    {
        var meta = WriteMetadata("t_green", new object[]
        {
            new { color = "#00AA00", role = "ZONE", zone_type = "GREENSPACE", street_class = "" }
        });
        var result = DeadMtlWorldBuilderLotSubdivisionPlanBuilder.Build(meta, WriteProfile(), "");
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("#00AA00") && e.Contains("#00AA10"));
    }

    [Fact]
    public void Build_Error_WhenWrongMagentaNormalizedColor()
    {
        var meta = WriteMetadata("t_magenta", new object[]
        {
            new { color = "#FF00FF", role = "STREET_CORRIDOR", zone_type = "", street_class = "BACK_ALLEY" }
        });
        var result = DeadMtlWorldBuilderLotSubdivisionPlanBuilder.Build(meta, WriteProfile(), "");
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("#FF00FF") && e.Contains("#F000FF"));
    }

    // -----------------------------------------------------------------------
    // Status and format fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Status_IsContractOnly()
    {
        var result = RunReal();
        Assert.Equal("LOT_SUBDIVISION_PLAN_CONTRACT_ONLY", result.Plan.Status);
    }

    [Fact]
    public void Build_SubdivisionStatus_IsNotExecuted()
    {
        var result = RunReal();
        Assert.Equal("NOT_EXECUTED", result.Plan.SubdivisionStatus);
    }

    [Fact]
    public void Build_RuntimeStatus_IsNotRuntimeProven()
    {
        var result = RunReal();
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Plan.RuntimeStatus);
    }

    [Fact]
    public void Build_Format_IsCorrect()
    {
        var result = RunReal();
        Assert.Equal("pzmapforge.deadmtl.worldbuilder.lot-subdivision-plan.v1", result.Plan.Format);
    }

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var result = RunReal();
        var md = DeadMtlWorldBuilderLotSubdivisionPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("MAP25C_WORLDBUILDER_LOT_SUBDIVISION_PLAN_CONTRACT_COMPLETE", md);
    }

    [Fact]
    public void RenderMarkdown_ContainsCsvHeader()
    {
        var result = RunReal();
        var csv = DeadMtlWorldBuilderLotSubdivisionPlanBuilder.RenderCsv(result.Plan);
        Assert.Contains("color,role,zone_type,street_class,subdivision_action", csv);
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result = RunReal();
        var summary = DeadMtlWorldBuilderLotSubdivisionPlanBuilder.RenderSummary(result);
        Assert.Contains("MAP25C_WORLDBUILDER_LOT_SUBDIVISION_PLAN_CONTRACT_COMPLETE", summary);
    }

    [Fact]
    public void RenderSummary_ContainsTileId()
    {
        var result = RunReal();
        var summary = DeadMtlWorldBuilderLotSubdivisionPlanBuilder.RenderSummary(result);
        Assert.Contains("map_00", summary);
    }
}
