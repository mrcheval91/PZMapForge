using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderGenerationDependencyManifestBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-dep-manifest-tests", Path.GetRandomFileName());

    public DeadMtlWorldBuilderGenerationDependencyManifestBuilderTests() =>
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

    private DeadMtlWorldBuilderGenerationDependencyManifestResult RunReal() =>
        DeadMtlWorldBuilderGenerationDependencyManifestBuilder.Build(
            RealProfilePath, RealMetadataPath, RealLotPlanPath,
            RealSidewalkPlanPath, RealBuildingSelectionPlanPath);

    private string WriteProfile() =>
        WriteJson("test_profile.json", new
        {
            format = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "test_baseline",
            profile_status = "AUTHORING_PROFILE_ONLY",
        });

    private string WriteMetadata() =>
        WriteJson("test_metadata.json", new
        {
            format = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id = "test",
            metadata_status = "AUTHORING_METADATA_ONLY",
        });

    private string WriteLotPlan() =>
        WriteJson("test_lot_plan.json", new
        {
            format = "pzmapforge.deadmtl.worldbuilder.lot-subdivision-plan.v1",
            tile_id = "test",
            status = "LOT_SUBDIVISION_PLAN_CONTRACT_ONLY",
        });

    private string WriteSidewalkPlan() =>
        WriteJson("test_sidewalk_plan.json", new
        {
            format = "pzmapforge.deadmtl.worldbuilder.sidewalk-generation-plan.v1",
            tile_id = "test",
            status = "SIDEWALK_GENERATION_PLAN_CONTRACT_ONLY",
        });

    private string WriteBuildingSelectionPlan() =>
        WriteJson("test_building_selection_plan.json", new
        {
            format = "pzmapforge.deadmtl.worldbuilder.building-selection-policy-plan.v1",
            tile_id = "test",
            status = "BUILDING_SELECTION_POLICY_PLAN_CONTRACT_ONLY",
        });

    private string WriteJson(string name, object obj)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, JsonSerializer.Serialize(obj), Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Missing file errors for all 5 inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Error_WhenProfileMissing()
    {
        var result = DeadMtlWorldBuilderGenerationDependencyManifestBuilder.Build(
            Path.Combine(_tempDir, "no_profile.json"),
            WriteMetadata(), WriteLotPlan(), WriteSidewalkPlan(), WriteBuildingSelectionPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("PROFILE_CONTRACT") && e.Contains("not found"));
    }

    [Fact]
    public void Build_Error_WhenMetadataMissing()
    {
        var result = DeadMtlWorldBuilderGenerationDependencyManifestBuilder.Build(
            WriteProfile(),
            Path.Combine(_tempDir, "no_metadata.json"),
            WriteLotPlan(), WriteSidewalkPlan(), WriteBuildingSelectionPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("RAW_TILE_ZONE_METADATA") && e.Contains("not found"));
    }

    [Fact]
    public void Build_Error_WhenLotPlanMissing()
    {
        var result = DeadMtlWorldBuilderGenerationDependencyManifestBuilder.Build(
            WriteProfile(), WriteMetadata(),
            Path.Combine(_tempDir, "no_lot_plan.json"),
            WriteSidewalkPlan(), WriteBuildingSelectionPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("LOT_SUBDIVISION_PLAN") && e.Contains("not found"));
    }

    [Fact]
    public void Build_Error_WhenSidewalkPlanMissing()
    {
        var result = DeadMtlWorldBuilderGenerationDependencyManifestBuilder.Build(
            WriteProfile(), WriteMetadata(), WriteLotPlan(),
            Path.Combine(_tempDir, "no_sidewalk_plan.json"),
            WriteBuildingSelectionPlan());
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("SIDEWALK_GENERATION_PLAN") && e.Contains("not found"));
    }

    [Fact]
    public void Build_Error_WhenBuildingSelectionPlanMissing()
    {
        var result = DeadMtlWorldBuilderGenerationDependencyManifestBuilder.Build(
            WriteProfile(), WriteMetadata(), WriteLotPlan(), WriteSidewalkPlan(),
            Path.Combine(_tempDir, "no_building_selection_plan.json"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Contains("BUILDING_SELECTION_POLICY_PLAN") && e.Contains("not found"));
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
            "pzmapforge.deadmtl.worldbuilder.generation-dependency-manifest.v1",
            result.Manifest.Format);
    }

    // -----------------------------------------------------------------------
    // Status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Status_IsContractOnly()
    {
        var result = RunReal();
        Assert.Equal("GENERATION_DEPENDENCY_MANIFEST_CONTRACT_ONLY", result.Manifest.Status);
    }

    [Fact]
    public void Build_RuntimeStatus_IsNotRuntimeProven()
    {
        var result = RunReal();
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Manifest.RuntimeStatus);
    }

    [Fact]
    public void Build_GenerationStatus_IsNotExecuted()
    {
        var result = RunReal();
        Assert.Equal("NOT_EXECUTED", result.Manifest.GenerationStatus);
    }

    [Fact]
    public void Build_WriterStatus_IsNotImplemented()
    {
        var result = RunReal();
        Assert.Equal("NOT_IMPLEMENTED", result.Manifest.WriterStatus);
    }

    [Fact]
    public void Build_PipelineStatus_IsOrderedManifestOnly()
    {
        var result = RunReal();
        Assert.Equal("ORDERED_MANIFEST_ONLY", result.Manifest.PipelineStatus);
    }

    // -----------------------------------------------------------------------
    // Step count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_StepCount_IsFive()
    {
        var result = RunReal();
        Assert.Equal(5, result.Manifest.Totals.StepCount);
    }

    [Fact]
    public void Build_Steps_HasFiveEntries()
    {
        var result = RunReal();
        Assert.Equal(5, result.Manifest.Steps.Count);
    }

    // -----------------------------------------------------------------------
    // Edge count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_DependencyEdgeCount_IsTen()
    {
        var result = RunReal();
        Assert.Equal(10, result.Manifest.Totals.DependencyEdgeCount);
    }

    [Fact]
    public void Build_DependencyEdges_HasTenEntries()
    {
        var result = RunReal();
        Assert.Equal(10, result.Manifest.DependencyEdges.Count);
    }

    // -----------------------------------------------------------------------
    // Step order exact
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_StepOrder_IsExact()
    {
        var steps = RunReal().Manifest.Steps;
        Assert.Equal("PROFILE_CONTRACT",             steps[0].StepId);
        Assert.Equal("RAW_TILE_ZONE_METADATA",       steps[1].StepId);
        Assert.Equal("LOT_SUBDIVISION_PLAN",         steps[2].StepId);
        Assert.Equal("SIDEWALK_GENERATION_PLAN",     steps[3].StepId);
        Assert.Equal("BUILDING_SELECTION_POLICY_PLAN", steps[4].StepId);
        Assert.Equal(1, steps[0].StepOrder);
        Assert.Equal(5, steps[4].StepOrder);
    }

    // -----------------------------------------------------------------------
    // All inputs exist and formats match
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AllInputsExist()
    {
        var result = RunReal();
        Assert.Equal(5, result.Manifest.Totals.ExistingInputCount);
        Assert.Equal(0, result.Manifest.Totals.MissingInputCount);
        Assert.All(result.Manifest.Steps, s => Assert.True(s.Exists, $"{s.StepId} should exist"));
    }

    [Fact]
    public void Build_AllFormatsMatch()
    {
        var result = RunReal();
        Assert.Equal(5, result.Manifest.Totals.FormatMatchCount);
        Assert.Equal(0, result.Manifest.Totals.FormatMismatchCount);
        Assert.All(result.Manifest.Steps, s =>
            Assert.Equal(s.ExpectedFormat, s.ActualFormat));
    }

    // -----------------------------------------------------------------------
    // Contract-only, runtime-proven, writer-ready, generation-executed
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ContractOnlyStepCount_IsFive()
    {
        var result = RunReal();
        Assert.Equal(5, result.Manifest.Totals.ContractOnlyStepCount);
    }

    [Fact]
    public void Build_RuntimeProvenStepCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Manifest.Totals.RuntimeProvenStepCount);
    }

    [Fact]
    public void Build_WriterReadyStepCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Manifest.Totals.WriterReadyStepCount);
    }

    [Fact]
    public void Build_GenerationExecutedStepCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Manifest.Totals.GenerationExecutedStepCount);
    }

    // -----------------------------------------------------------------------
    // All 10 dependency edges present
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AllTenEdgesPresent()
    {
        var edges = RunReal().Manifest.DependencyEdges;
        var pairs = edges.Select(e => (e.FromStepId, e.ToStepId)).ToHashSet();

        Assert.Contains(("PROFILE_CONTRACT",         "RAW_TILE_ZONE_METADATA"),       pairs);
        Assert.Contains(("PROFILE_CONTRACT",         "LOT_SUBDIVISION_PLAN"),         pairs);
        Assert.Contains(("RAW_TILE_ZONE_METADATA",   "LOT_SUBDIVISION_PLAN"),         pairs);
        Assert.Contains(("PROFILE_CONTRACT",         "SIDEWALK_GENERATION_PLAN"),     pairs);
        Assert.Contains(("RAW_TILE_ZONE_METADATA",   "SIDEWALK_GENERATION_PLAN"),     pairs);
        Assert.Contains(("LOT_SUBDIVISION_PLAN",     "SIDEWALK_GENERATION_PLAN"),     pairs);
        Assert.Contains(("PROFILE_CONTRACT",         "BUILDING_SELECTION_POLICY_PLAN"), pairs);
        Assert.Contains(("RAW_TILE_ZONE_METADATA",   "BUILDING_SELECTION_POLICY_PLAN"), pairs);
        Assert.Contains(("LOT_SUBDIVISION_PLAN",     "BUILDING_SELECTION_POLICY_PLAN"), pairs);
        Assert.Contains(("SIDEWALK_GENERATION_PLAN", "BUILDING_SELECTION_POLICY_PLAN"), pairs);
    }

    [Fact]
    public void Build_AllEdgesAreRequired()
    {
        var edges = RunReal().Manifest.DependencyEdges;
        Assert.All(edges, e => Assert.True(e.Required, $"Edge {e.FromStepId}->{e.ToStepId} must be required"));
    }

    [Fact]
    public void Build_AllEdgesAreRequiredInput()
    {
        var edges = RunReal().Manifest.DependencyEdges;
        Assert.All(edges, e => Assert.Equal("REQUIRED_INPUT", e.DependencyType));
    }

    // -----------------------------------------------------------------------
    // Specific dependency checks (depends_on and consumed_by per step)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ProfileContract_HasNoDependencies()
    {
        var step = RunReal().Manifest.Steps.Single(s => s.StepId == "PROFILE_CONTRACT");
        Assert.Empty(step.DependsOn);
    }

    [Fact]
    public void Build_BuildingSelectionPolicyPlan_DependsOnAllFour()
    {
        var step = RunReal().Manifest.Steps
            .Single(s => s.StepId == "BUILDING_SELECTION_POLICY_PLAN");
        Assert.Contains("PROFILE_CONTRACT",         step.DependsOn);
        Assert.Contains("RAW_TILE_ZONE_METADATA",   step.DependsOn);
        Assert.Contains("LOT_SUBDIVISION_PLAN",     step.DependsOn);
        Assert.Contains("SIDEWALK_GENERATION_PLAN", step.DependsOn);
    }

    [Fact]
    public void Build_LotSubdivisionPlan_DependsOnProfileAndMetadata()
    {
        var step = RunReal().Manifest.Steps.Single(s => s.StepId == "LOT_SUBDIVISION_PLAN");
        Assert.Contains("PROFILE_CONTRACT",       step.DependsOn);
        Assert.Contains("RAW_TILE_ZONE_METADATA", step.DependsOn);
        Assert.Equal(2, step.DependsOn.Count);
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var cb = RunReal().Manifest.ClaimBoundary;
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
    }

    // -----------------------------------------------------------------------
    // Markdown sections
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsExpectedSections()
    {
        var result   = RunReal();
        var markdown = DeadMtlWorldBuilderGenerationDependencyManifestBuilder.RenderMarkdown(result.Manifest);
        Assert.Contains("MAP-25F", markdown, StringComparison.Ordinal);
        Assert.Contains("PROFILE_CONTRACT",   markdown, StringComparison.Ordinal);
        Assert.Contains("LOT_SUBDIVISION_PLAN", markdown, StringComparison.Ordinal);
        Assert.Contains("Dependency Graph",   markdown, StringComparison.Ordinal);
        Assert.Contains("Claim Boundary",     markdown, StringComparison.Ordinal);
        Assert.Contains("MAP25F_WORLDBUILDER_GENERATION_DEPENDENCY_MANIFEST_CONTRACT_COMPLETE",
            markdown, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result = RunReal();
        var csv    = DeadMtlWorldBuilderGenerationDependencyManifestBuilder.RenderCsv(result.Manifest);
        Assert.StartsWith("step_order,step_id,source_stage,", csv);
    }

    // -----------------------------------------------------------------------
    // Summary verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = RunReal();
        var summary = DeadMtlWorldBuilderGenerationDependencyManifestBuilder.RenderSummary(result);
        Assert.Contains("MAP25F_WORLDBUILDER_GENERATION_DEPENDENCY_MANIFEST_CONTRACT_COMPLETE",
            summary, StringComparison.Ordinal);
    }
}
