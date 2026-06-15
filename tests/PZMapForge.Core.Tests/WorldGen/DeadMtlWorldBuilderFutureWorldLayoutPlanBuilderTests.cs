using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderFutureWorldLayoutPlanBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-fwlp-tests", Path.GetRandomFileName());

    public DeadMtlWorldBuilderFutureWorldLayoutPlanBuilderTests() =>
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

    private static string RealBuildingSelectionPlanPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-building-selection-policy-plan",
            "map_00", "map_00.building_selection_policy_plan.json");

    private static string RealDependencyManifestPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-generation-dependency-manifest",
            "map_00", "map_00.generation_dependency_manifest.json");

    private DeadMtlWorldBuilderFutureWorldLayoutPlanResult RunReal() =>
        DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.Build(
            RealProfilePath, RealMetadataPath, RealLotPlanPath,
            RealSidewalkPlanPath, RealBuildingSelectionPlanPath, RealDependencyManifestPath);

    private string WriteMinimalFile(string name, string format) =>
        WriteJson(name, new { format });

    private string WriteJson(string name, object obj)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, JsonSerializer.Serialize(obj), Encoding.UTF8);
        return path;
    }

    private string WriteMetadata() =>
        WriteJson("test_metadata.json", new
        {
            format = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id = "test",
            metadata_status = "AUTHORING_METADATA_ONLY",
            color_roles = new[]
            {
                new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", street_class = "" },
                new { color = "#000000", role = "IGNORE", zone_type = "VOID_OR_BORDER", street_class = "" },
            },
        });

    // -----------------------------------------------------------------------
    // Missing file errors for all 6 inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Error_WhenProfileMissing()
    {
        var m  = WriteMetadata();
        var f1 = WriteMinimalFile("p1.json", "x");
        var result = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.Build(
            Path.Combine(_tempDir, "no_profile.json"), m, f1, f1, f1, f1);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("profile"));
    }

    [Fact]
    public void Build_Error_WhenMetadataMissing()
    {
        var f1 = WriteMinimalFile("p1.json", "x");
        var result = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.Build(
            f1, Path.Combine(_tempDir, "no_meta.json"), f1, f1, f1, f1);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("metadata"));
    }

    [Fact]
    public void Build_Error_WhenLotPlanMissing()
    {
        var m  = WriteMetadata();
        var f1 = WriteMinimalFile("p1.json", "x");
        var result = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.Build(
            f1, m, Path.Combine(_tempDir, "no_lot.json"), f1, f1, f1);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("lot"));
    }

    [Fact]
    public void Build_Error_WhenSidewalkPlanMissing()
    {
        var m  = WriteMetadata();
        var f1 = WriteMinimalFile("p1.json", "x");
        var result = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.Build(
            f1, m, f1, Path.Combine(_tempDir, "no_sw.json"), f1, f1);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("sidewalk"));
    }

    [Fact]
    public void Build_Error_WhenBuildingSelectionPlanMissing()
    {
        var m  = WriteMetadata();
        var f1 = WriteMinimalFile("p1.json", "x");
        var result = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.Build(
            f1, m, f1, f1, Path.Combine(_tempDir, "no_bsp.json"), f1);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("building"));
    }

    [Fact]
    public void Build_Error_WhenDependencyManifestMissing()
    {
        var m  = WriteMetadata();
        var f1 = WriteMinimalFile("p1.json", "x");
        var result = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.Build(
            f1, m, f1, f1, f1, Path.Combine(_tempDir, "no_dep.json"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("dependency"));
    }

    // -----------------------------------------------------------------------
    // Real map_00 valid
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_IsValid_ForRealMap00()
    {
        var result = RunReal();
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // -----------------------------------------------------------------------
    // Format
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Format_IsCorrect()
    {
        var result = RunReal();
        Assert.Equal(
            "pzmapforge.deadmtl.worldbuilder.future-world-layout-plan.v1",
            result.Plan.Format);
    }

    // -----------------------------------------------------------------------
    // Status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Status_IsContractOnly()
    {
        var result = RunReal();
        Assert.Equal("FUTURE_WORLD_LAYOUT_PLAN_CONTRACT_ONLY", result.Plan.Status);
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
    public void Build_LayoutStatus_IsNotMaterialized()
    {
        var result = RunReal();
        Assert.Equal("NOT_MATERIALIZED", result.Plan.LayoutStatus);
    }

    [Fact]
    public void Build_GeometryStatus_IsNoGeometryCreated()
    {
        var result = RunReal();
        Assert.Equal("NO_GEOMETRY_CREATED", result.Plan.GeometryStatus);
    }

    // -----------------------------------------------------------------------
    // Layout component counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_LayoutComponentCount_IsSeven()
    {
        var result = RunReal();
        Assert.Equal(7, result.Plan.Totals.LayoutComponentCount);
    }

    [Fact]
    public void Build_ZoneLayoutComponentCount_IsThree()
    {
        var result = RunReal();
        Assert.Equal(3, result.Plan.Totals.ZoneLayoutComponentCount);
    }

    [Fact]
    public void Build_StreetLayoutComponentCount_IsTwo()
    {
        var result = RunReal();
        Assert.Equal(2, result.Plan.Totals.StreetLayoutComponentCount);
    }

    [Fact]
    public void Build_UniquePlaceholderLayoutComponentCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.UniquePlaceholderLayoutComponentCount);
    }

    [Fact]
    public void Build_IgnoreLayoutComponentCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Plan.Totals.IgnoreLayoutComponentCount);
    }

    // -----------------------------------------------------------------------
    // Per-component assertions
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ResidentialComponent_HasCorrectFutureAction()
    {
        var comp = RunReal().Plan.LayoutComponents
            .Single(c => c.ZoneType == "RESIDENTIAL");
        Assert.Equal("FUTURE_SUBDIVIDE_INTO_LOTS_AND_ASSIGN_RESIDENTIAL_BUILDINGS", comp.FutureAction);
    }

    [Fact]
    public void Build_CommercialComponent_HasCorrectFutureAction()
    {
        var comp = RunReal().Plan.LayoutComponents
            .Single(c => c.ZoneType == "COMMERCIAL");
        Assert.Equal("FUTURE_SUBDIVIDE_INTO_LOTS_AND_ASSIGN_COMMERCIAL_BUILDINGS", comp.FutureAction);
    }

    [Fact]
    public void Build_GreenspaceComponent_HasNoProcecduralBuildings()
    {
        var comp = RunReal().Plan.LayoutComponents
            .Single(c => c.ZoneType == "GREENSPACE");
        Assert.Equal("NO_PROCEDURAL_BUILDINGS", comp.BuildingPolicySource);
        Assert.Equal("FUTURE_GENERATE_GREENSPACE_LAYER", comp.FutureAction);
    }

    [Fact]
    public void Build_CivicPlaceholder_RequiresUniqueBuildingBinding()
    {
        var comp = RunReal().Plan.LayoutComponents
            .Single(c => c.ComponentType == "UNIQUE_PLACEHOLDER_LAYOUT_COMPONENT");
        Assert.Equal("FUTURE_BIND_UNIQUE_BUILDING_ID_AND_PLACE", comp.FutureAction);
        Assert.Equal("UNIQUE_PLACEHOLDER_POLICY", comp.LotPolicySource);
    }

    [Fact]
    public void Build_MainRoadComponent_ProvidesMainFrontage()
    {
        var comp = RunReal().Plan.LayoutComponents
            .Single(c => c.StreetClass == "MAIN_ROAD");
        Assert.Equal("FUTURE_GENERATE_MAIN_ROAD_CORRIDOR_AND_SIDEWALK_CONTEXT", comp.FutureAction);
        Assert.Equal("PROVIDES_MAIN_FRONTAGE", comp.FrontageRule);
        Assert.Equal("SIDEWALK_GENERATION_PLAN", comp.SidewalkPolicySource);
    }

    [Fact]
    public void Build_BackAlleyComponent_HasNoFrontageAndNoSidewalks()
    {
        var comp = RunReal().Plan.LayoutComponents
            .Single(c => c.StreetClass == "BACK_ALLEY");
        Assert.Equal("FUTURE_GENERATE_BACK_ALLEY_SERVICE_CORRIDOR", comp.FutureAction);
        Assert.Equal("NO_PRIMARY_FRONTAGE", comp.FrontageRule);
        Assert.Equal("NO_SIDEWALKS", comp.SidewalkPolicySource);
        Assert.Equal("BACK_ALLEY_SERVICE_ONLY", comp.ServiceAccessRule);
    }

    [Fact]
    public void Build_IgnoreComponent_IsIgnored()
    {
        var comp = RunReal().Plan.LayoutComponents
            .Single(c => c.ComponentType == "IGNORE_LAYOUT_COMPONENT");
        Assert.Equal("IGNORE", comp.FutureAction);
    }

    // -----------------------------------------------------------------------
    // Future execution requirements
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_FutureExecutionRequirements_AllTrueExceptCanExecuteNow()
    {
        var req = RunReal().Plan.FutureExecutionRequirements;
        Assert.True(req.RequiresActualLotGeometry);
        Assert.True(req.RequiresActualSidewalkGeometry);
        Assert.True(req.RequiresBuildingCatalogue);
        Assert.True(req.RequiresUniqueBuildingBindings);
        Assert.True(req.RequiresTileWriter);
        Assert.True(req.RequiresRuntimeValidation);
        Assert.False(req.CanExecuteNow);
    }

    [Fact]
    public void Build_BlockedReason_IsContractOnly()
    {
        var req = RunReal().Plan.FutureExecutionRequirements;
        Assert.Equal("CONTRACT_ONLY_NO_GEOMETRY_NO_WRITER_NO_RUNTIME_PROOF", req.BlockedReason);
    }

    // -----------------------------------------------------------------------
    // Zero-counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ConcreteGeometryCreatedNowCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Plan.Totals.ConcreteGeometryCreatedNowCount);
    }

    [Fact]
    public void Build_ConcreteBuildingIdsSelectedNowCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Plan.Totals.ConcreteBuildingIdsSelectedNowCount);
    }

    [Fact]
    public void Build_LayoutMaterializedNowCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Plan.Totals.LayoutMaterializedNowCount);
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false (15 fields)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var cb = RunReal().Plan.ClaimBoundary;
        Assert.False(cb.WritesLotpack);
        Assert.False(cb.WritesWorldgenLua);
        Assert.False(cb.RuntimeProven);
        Assert.False(cb.PublicPlayableClaim);
        Assert.False(cb.WriterReadyClaim);
        Assert.False(cb.GeneratesTerrainNow);
        Assert.False(cb.GeneratesBuildingsNow);
        Assert.False(cb.GeneratesSidewalksNow);
        Assert.False(cb.SubdividesLotsNow);
        Assert.False(cb.CapturesChunkLayersNow);
        Assert.False(cb.PlacesFencesNow);
        Assert.False(cb.PlacesUniqueBuildingsNow);
        Assert.False(cb.SelectsConcreteBuildingIdsNow);
        Assert.False(cb.CreatesConcreteGeometryNow);
        Assert.False(cb.MaterializesLayoutNow);
    }

    // -----------------------------------------------------------------------
    // Markdown sections and verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsRequiredSectionsAndVerdict()
    {
        var result   = RunReal();
        var markdown = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.RenderMarkdown(result.Plan);
        Assert.Contains("MAP-25G", markdown, StringComparison.Ordinal);
        Assert.Contains("Input Chain",          markdown, StringComparison.Ordinal);
        Assert.Contains("Layout Components",    markdown, StringComparison.Ordinal);
        Assert.Contains("Residential Layout Policy",     markdown, StringComparison.Ordinal);
        Assert.Contains("Commercial Layout Policy",      markdown, StringComparison.Ordinal);
        Assert.Contains("Greenspace Layout Policy",      markdown, StringComparison.Ordinal);
        Assert.Contains("Civic Unique Placeholder",      markdown, StringComparison.Ordinal);
        Assert.Contains("Future Execution Requirements", markdown, StringComparison.Ordinal);
        Assert.Contains("Claim Boundary",  markdown, StringComparison.Ordinal);
        Assert.Contains("MAP25G_WORLDBUILDER_FUTURE_WORLD_LAYOUT_PLAN_CONTRACT_COMPLETE",
            markdown, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result = RunReal();
        var csv    = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.RenderCsv(result.Plan);
        Assert.StartsWith("color,role,zone_type,street_class,component_type,", csv);
    }

    // -----------------------------------------------------------------------
    // Summary verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = RunReal();
        var summary = DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder.RenderSummary(result);
        Assert.Contains("MAP25G_WORLDBUILDER_FUTURE_WORLD_LAYOUT_PLAN_CONTRACT_COMPLETE",
            summary, StringComparison.Ordinal);
    }
}
