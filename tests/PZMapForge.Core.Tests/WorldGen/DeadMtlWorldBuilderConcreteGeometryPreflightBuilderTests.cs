using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderConcreteGeometryPreflightBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-cgp-tests", Path.GetRandomFileName());

    public DeadMtlWorldBuilderConcreteGeometryPreflightBuilderTests() =>
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

    private static string RealFutureLayoutPlanPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-future-world-layout-plan",
            "map_00", "map_00.future_world_layout_plan.json");

    private DeadMtlWorldBuilderConcreteGeometryPreflightResult RunReal() =>
        DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.Build(
            RealProfilePath, RealMetadataPath, RealLotPlanPath,
            RealSidewalkPlanPath, RealBuildingSelectionPlanPath,
            RealDependencyManifestPath, RealFutureLayoutPlanPath);

    private string WriteMinimalFile(string name) =>
        WritePath(name, "{}");

    private string WritePath(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Missing file errors for all 7 inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Error_WhenProfileMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.Build(
            Path.Combine(_tempDir, "no_profile.json"), f, f, f, f, f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("profile"));
    }

    [Fact]
    public void Build_Error_WhenMetadataMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.Build(
            f, Path.Combine(_tempDir, "no_meta.json"), f, f, f, f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("metadata"));
    }

    [Fact]
    public void Build_Error_WhenLotPlanMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.Build(
            f, f, Path.Combine(_tempDir, "no_lot.json"), f, f, f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("lot"));
    }

    [Fact]
    public void Build_Error_WhenSidewalkPlanMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.Build(
            f, f, f, Path.Combine(_tempDir, "no_sw.json"), f, f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("sidewalk"));
    }

    [Fact]
    public void Build_Error_WhenBuildingSelectionPlanMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.Build(
            f, f, f, f, Path.Combine(_tempDir, "no_bsp.json"), f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("building"));
    }

    [Fact]
    public void Build_Error_WhenDependencyManifestMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.Build(
            f, f, f, f, f, Path.Combine(_tempDir, "no_dep.json"), f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("dependency"));
    }

    [Fact]
    public void Build_Error_WhenFutureLayoutPlanMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.Build(
            f, f, f, f, f, f, Path.Combine(_tempDir, "no_flp.json"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("future layout"));
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
            "pzmapforge.deadmtl.worldbuilder.concrete-geometry-preflight.v1",
            result.Preflight.Format);
    }

    // -----------------------------------------------------------------------
    // Status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Status_IsContractOnly()
    {
        var result = RunReal();
        Assert.Equal("CONCRETE_GEOMETRY_PREFLIGHT_CONTRACT_ONLY", result.Preflight.Status);
    }

    [Fact]
    public void Build_RuntimeStatus_IsNotRuntimeProven()
    {
        var result = RunReal();
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Preflight.RuntimeStatus);
    }

    [Fact]
    public void Build_WriterStatus_IsNotImplemented()
    {
        var result = RunReal();
        Assert.Equal("NOT_IMPLEMENTED", result.Preflight.WriterStatus);
    }

    [Fact]
    public void Build_GenerationStatus_IsNotExecuted()
    {
        var result = RunReal();
        Assert.Equal("NOT_EXECUTED", result.Preflight.GenerationStatus);
    }

    [Fact]
    public void Build_GeometryStatus_IsNoGeometryCreated()
    {
        var result = RunReal();
        Assert.Equal("NO_GEOMETRY_CREATED", result.Preflight.GeometryStatus);
    }

    [Fact]
    public void Build_PreflightStatus_IsBlocked()
    {
        var result = RunReal();
        Assert.Equal("BLOCKED_PENDING_GEOMETRY_AND_WRITER", result.Preflight.PreflightStatus);
    }

    // -----------------------------------------------------------------------
    // Requirement counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RequirementCount_IsNine()
    {
        var result = RunReal();
        Assert.Equal(9, result.Preflight.Totals.RequirementCount);
    }

    [Fact]
    public void Build_BlockedRequirementCount_IsNine()
    {
        var result = RunReal();
        Assert.Equal(9, result.Preflight.Totals.BlockedRequirementCount);
    }

    [Fact]
    public void Build_CanExecuteNowCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Preflight.Totals.CanExecuteNowCount);
    }

    [Fact]
    public void Build_GeometryRequirementCount_IsSeven()
    {
        var result = RunReal();
        Assert.Equal(7, result.Preflight.Totals.GeometryRequirementCount);
    }

    [Fact]
    public void Build_WriterRequirementCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Preflight.Totals.WriterRequirementCount);
    }

    [Fact]
    public void Build_RuntimeRequirementCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Preflight.Totals.RuntimeRequirementCount);
    }

    [Fact]
    public void Build_ConcreteGeometryCreatedNowCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Preflight.Totals.ConcreteGeometryCreatedNowCount);
    }

    [Fact]
    public void Build_LayoutMaterializedNowCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Preflight.Totals.LayoutMaterializedNowCount);
    }

    // -----------------------------------------------------------------------
    // All 9 requirement IDs present
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AllNineRequirementIds_Present()
    {
        var ids = RunReal().Preflight.PreflightRequirements.Select(r => r.RequirementId).ToList();
        Assert.Contains("LOT_GEOMETRY",                          ids);
        Assert.Contains("MAIN_ROAD_CORRIDOR_GEOMETRY",           ids);
        Assert.Contains("BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY",  ids);
        Assert.Contains("SIDEWALK_GEOMETRY",                     ids);
        Assert.Contains("BUILDING_SLOT_GEOMETRY",                ids);
        Assert.Contains("UNIQUE_BUILDING_BINDING",               ids);
        Assert.Contains("FENCE_AND_LOT_BOUNDARY_GEOMETRY",       ids);
        Assert.Contains("TILE_WRITER_IMPLEMENTATION",            ids);
        Assert.Contains("RUNTIME_VALIDATION_PASS",               ids);
    }

    // -----------------------------------------------------------------------
    // Per-requirement color checks
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_LotGeometry_HasResidentialAndCommercialColors()
    {
        var req = RunReal().Preflight.PreflightRequirements
            .Single(r => r.RequirementId == "LOT_GEOMETRY");
        Assert.Contains("#7200FF", req.SourceComponentColors);
        Assert.Contains("#42CCFF", req.SourceComponentColors);
    }

    [Fact]
    public void Build_SidewalkGeometry_HasMainRoadColor()
    {
        var req = RunReal().Preflight.PreflightRequirements
            .Single(r => r.RequirementId == "SIDEWALK_GEOMETRY");
        Assert.Contains("#FF6600", req.SourceComponentColors);
    }

    [Fact]
    public void Build_BackAlleyGeometry_HasBackAlleyColor()
    {
        var req = RunReal().Preflight.PreflightRequirements
            .Single(r => r.RequirementId == "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY");
        Assert.Contains("#F000FF", req.SourceComponentColors);
    }

    [Fact]
    public void Build_UniqueBuildingBinding_HasCivicColor()
    {
        var req = RunReal().Preflight.PreflightRequirements
            .Single(r => r.RequirementId == "UNIQUE_BUILDING_BINDING");
        Assert.Contains("#B2BD87", req.SourceComponentColors);
    }

    // -----------------------------------------------------------------------
    // Writer and runtime status on specific requirements
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_TileWriterImplementation_HasNotImplementedWriterStatus()
    {
        var req = RunReal().Preflight.PreflightRequirements
            .Single(r => r.RequirementId == "TILE_WRITER_IMPLEMENTATION");
        Assert.Equal("NOT_IMPLEMENTED", req.WriterStatus);
    }

    [Fact]
    public void Build_RuntimeValidationPass_HasNotRuntimeProvenStatus()
    {
        var req = RunReal().Preflight.PreflightRequirements
            .Single(r => r.RequirementId == "RUNTIME_VALIDATION_PASS");
        Assert.Equal("NOT_RUNTIME_PROVEN", req.RuntimeStatus);
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false (15 fields)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var cb = RunReal().Preflight.ClaimBoundary;
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
        var markdown = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.RenderMarkdown(result.Preflight);
        Assert.Contains("MAP-25H",                    markdown, StringComparison.Ordinal);
        Assert.Contains("Input Chain",                markdown, StringComparison.Ordinal);
        Assert.Contains("Preflight Requirements",     markdown, StringComparison.Ordinal);
        Assert.Contains("Geometry Blockers",          markdown, StringComparison.Ordinal);
        Assert.Contains("Writer Blocker",             markdown, StringComparison.Ordinal);
        Assert.Contains("Runtime Validation Blocker", markdown, StringComparison.Ordinal);
        Assert.Contains("Claim Boundary",             markdown, StringComparison.Ordinal);
        Assert.Contains("MAP25H_WORLDBUILDER_CONCRETE_GEOMETRY_PREFLIGHT_CONTRACT_COMPLETE",
            markdown, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result = RunReal();
        var csv    = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.RenderCsv(result.Preflight);
        Assert.StartsWith("requirement_order,requirement_id,requirement_type,", csv);
    }

    // -----------------------------------------------------------------------
    // Summary verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = RunReal();
        var summary = DeadMtlWorldBuilderConcreteGeometryPreflightBuilder.RenderSummary(result);
        Assert.Contains("MAP25H_WORLDBUILDER_CONCRETE_GEOMETRY_PREFLIGHT_CONTRACT_COMPLETE",
            summary, StringComparison.Ordinal);
    }
}
