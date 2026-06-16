using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilderTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string AdjacencyGraphPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-component-adjacency-graph",
            "map_00", "map_00.component_adjacency_graph.json");

    private static string ConnectedComponentsPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-connected-component-extraction",
            "map_00", "map_00.connected_component_extraction.json");

    private static string ComponentIntentsPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-component-intent-classification",
            "map_00", "map_00.component_intent_classification.json");

    private static string GeometryPrimitiveSchemaPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-geometry-primitive-schema",
            "map_00", "map_00.geometry_primitive_schema.json");

    private static string GeometryPreflightPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-concrete-geometry-preflight",
            "map_00", "map_00.concrete_geometry_preflight.json");

    private static DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionResult BuildReal() =>
        DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.Build(
            AdjacencyGraphPath,
            ConnectedComponentsPath,
            ComponentIntentsPath,
            GeometryPrimitiveSchemaPath,
            GeometryPreflightPath);

    private static string Missing => Path.Combine(RepoRoot, "does-not-exist.json");

    // -----------------------------------------------------------------------
    // Missing file errors
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Fails_WhenAdjacencyGraphMissing()
    {
        var result = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.Build(
            Missing, ConnectedComponentsPath, ComponentIntentsPath,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Build_Fails_WhenConnectedComponentsMissing()
    {
        var result = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.Build(
            AdjacencyGraphPath, Missing, ComponentIntentsPath,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Build_Fails_WhenComponentIntentsMissing()
    {
        var result = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.Build(
            AdjacencyGraphPath, ConnectedComponentsPath, Missing,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Build_Fails_WhenGeometryPrimitiveSchemaMissing()
    {
        var result = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.Build(
            AdjacencyGraphPath, ConnectedComponentsPath, ComponentIntentsPath,
            Missing, GeometryPreflightPath);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Build_Fails_WhenGeometryPreflightMissing()
    {
        var result = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.Build(
            AdjacencyGraphPath, ConnectedComponentsPath, ComponentIntentsPath,
            GeometryPrimitiveSchemaPath, Missing);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    // -----------------------------------------------------------------------
    // Valid build
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Succeeds_WithRealAdjacencyGraph()
    {
        var result = BuildReal();
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // -----------------------------------------------------------------------
    // Format and status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Extraction_Format_IsCorrect()
    {
        var e = BuildReal().Extraction;
        Assert.Equal("pzmapforge.deadmtl.worldbuilder.adjacency-planning-candidate-extraction.v1", e.Format);
    }

    [Fact]
    public void Extraction_Status_IsContractOnly()
    {
        var e = BuildReal().Extraction;
        Assert.Equal("ADJACENCY_PLANNING_CANDIDATE_EXTRACTION_CONTRACT_ONLY", e.Status);
    }

    [Fact]
    public void Extraction_RuntimeStatus_IsNotRuntimeProven()
    {
        var e = BuildReal().Extraction;
        Assert.Equal("NOT_RUNTIME_PROVEN", e.RuntimeStatus);
    }

    [Fact]
    public void Extraction_WriterStatus_IsNotImplemented()
    {
        var e = BuildReal().Extraction;
        Assert.Equal("NOT_IMPLEMENTED", e.WriterStatus);
    }

    [Fact]
    public void Extraction_GenerationStatus_IsNotExecuted()
    {
        var e = BuildReal().Extraction;
        Assert.Equal("NOT_EXECUTED", e.GenerationStatus);
    }

    [Fact]
    public void Extraction_GeometryStatus_IsCandidateExtractionOnly()
    {
        var e = BuildReal().Extraction;
        Assert.Equal("CANDIDATE_EXTRACTION_ONLY_NO_GEOMETRY_CREATED", e.GeometryStatus);
    }

    [Fact]
    public void Extraction_ExtractionStatus_IsCandidatesExtracted()
    {
        var e = BuildReal().Extraction;
        Assert.Equal("ADJACENCY_PLANNING_CANDIDATES_EXTRACTED", e.ExtractionStatus);
    }

    [Fact]
    public void Extraction_MaterializationStatus_IsNotMaterialized()
    {
        var e = BuildReal().Extraction;
        Assert.Equal("NOT_MATERIALIZED", e.MaterializationStatus);
    }

    // -----------------------------------------------------------------------
    // Extraction contract
    // -----------------------------------------------------------------------

    [Fact]
    public void ExtractionContract_TileId_IsMap00()
    {
        var ec = BuildReal().Extraction.ExtractionContract;
        Assert.Equal("map_00", ec.TileId);
    }

    [Fact]
    public void ExtractionContract_SourceAdjacencyGraphContract_IsMap25M()
    {
        var ec = BuildReal().Extraction.ExtractionContract;
        Assert.Equal("MAP25M_COMPONENT_ADJACENCY_GRAPH", ec.SourceAdjacencyGraphContract);
    }

    [Fact]
    public void ExtractionContract_TotalAdjacencyEdgesInput_Is82()
    {
        var ec = BuildReal().Extraction.ExtractionContract;
        Assert.Equal(82, ec.TotalAdjacencyEdgesInput);
    }

    [Fact]
    public void ExtractionContract_CandidateRecordsExtracted_Is82()
    {
        var ec = BuildReal().Extraction.ExtractionContract;
        Assert.Equal(82, ec.CandidateRecordsExtracted);
    }

    [Fact]
    public void ExtractionContract_ActionableCandidates_Is71()
    {
        var ec = BuildReal().Extraction.ExtractionContract;
        Assert.Equal(71, ec.ActionableCandidates);
    }

    [Fact]
    public void ExtractionContract_IgnoredCandidates_Is11()
    {
        var ec = BuildReal().Extraction.ExtractionContract;
        Assert.Equal(11, ec.IgnoredCandidates);
    }

    [Fact]
    public void ExtractionContract_ExtractionProducesGeometry_IsFalse()
    {
        var ec = BuildReal().Extraction.ExtractionContract;
        Assert.False(ec.ExtractionProducesGeometry);
    }

    [Fact]
    public void ExtractionContract_ExtractionProducesMaterialization_IsFalse()
    {
        var ec = BuildReal().Extraction.ExtractionContract;
        Assert.False(ec.ExtractionProducesMaterialization);
    }

    // -----------------------------------------------------------------------
    // Candidate count totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Totals_TotalCandidateRecords_Is82()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(82, t.TotalCandidateRecords);
    }

    [Fact]
    public void Totals_ActionableCandidateCount_Is71()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(71, t.ActionableCandidateCount);
    }

    [Fact]
    public void Totals_IgnoredCandidateCount_Is11()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(11, t.IgnoredCandidateCount);
    }

    [Fact]
    public void Totals_FrontagePlanningCandidateCount_Is26()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(26, t.FrontagePlanningCandidateCount);
    }

    [Fact]
    public void Totals_RearServiceAccessPlanningCandidateCount_Is26()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(26, t.RearServiceAccessPlanningCandidateCount);
    }

    [Fact]
    public void Totals_StreetNetworkTouchpointCandidateCount_Is10()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(10, t.StreetNetworkTouchpointCandidateCount);
    }

    [Fact]
    public void Totals_GreenspaceAccessCandidateCount_Is1()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(1, t.GreenspaceAccessCandidateCount);
    }

    [Fact]
    public void Totals_CivicGreenspaceContextCandidateCount_Is2()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(2, t.CivicGreenspaceContextCandidateCount);
    }

    [Fact]
    public void Totals_MixedLotBlockBoundaryCandidateCount_Is6()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(6, t.MixedLotBlockBoundaryCandidateCount);
    }

    [Fact]
    public void Totals_IgnoredBoundaryAdjacencyCount_Is11()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(11, t.IgnoredBoundaryAdjacencyCount);
    }

    // -----------------------------------------------------------------------
    // Contact totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Totals_FrontageContactTotalPx_Is1860()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(1860, t.FrontageContactTotalPx);
    }

    [Fact]
    public void Totals_RearServiceAccessContactTotalPx_Is1713()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(1713, t.RearServiceAccessContactTotalPx);
    }

    [Fact]
    public void Totals_StreetNetworkContactTotalPx_Is91()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(91, t.StreetNetworkContactTotalPx);
    }

    [Fact]
    public void Totals_GreenspaceAccessContactTotalPx_Is291()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(291, t.GreenspaceAccessContactTotalPx);
    }

    [Fact]
    public void Totals_CivicGreenspaceContactTotalPx_Is158()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(158, t.CivicGreenspaceContactTotalPx);
    }

    [Fact]
    public void Totals_MixedLotBlockContactTotalPx_Is164()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(164, t.MixedLotBlockContactTotalPx);
    }

    [Fact]
    public void Totals_IgnoredContactTotalPx_Is258()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(258, t.IgnoredContactTotalPx);
    }

    // -----------------------------------------------------------------------
    // Zero geometry counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Totals_CreatedGeometryCount_IsZero()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(0, t.CreatedGeometryCount);
    }

    [Fact]
    public void Totals_WriterReadyCount_IsZero()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(0, t.WriterReadyCount);
    }

    [Fact]
    public void Totals_RuntimeValidatedCount_IsZero()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(0, t.RuntimeValidatedCount);
    }

    [Fact]
    public void Totals_MaterializedCount_IsZero()
    {
        var t = BuildReal().Extraction.Totals;
        Assert.Equal(0, t.MaterializedCount);
    }

    // -----------------------------------------------------------------------
    // Candidate record ordering and IDs
    // -----------------------------------------------------------------------

    [Fact]
    public void Candidates_Count_Is82()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.Equal(82, candidates.Count);
    }

    [Fact]
    public void AllCandidateIds_AreUnique()
    {
        var candidates = BuildReal().Extraction.Candidates;
        var unique = candidates.Select(c => c.CandidateId).Distinct().Count();
        Assert.Equal(candidates.Count, unique);
    }

    [Fact]
    public void FirstCandidateId_IsMap00Candidate0001()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.Equal("map_00_candidate_0001", candidates[0].CandidateId);
    }

    [Fact]
    public void LastCandidateId_IsMap00Candidate0082()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.Equal("map_00_candidate_0082", candidates[^1].CandidateId);
    }

    // -----------------------------------------------------------------------
    // Candidate fixed field values
    // -----------------------------------------------------------------------

    [Fact]
    public void AllCandidates_ContactLengthPx_IsPositive()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.All(candidates, c => Assert.True(c.ContactLengthPx > 0,
            $"Candidate {c.CandidateId} has contact_length_px={c.ContactLengthPx}"));
    }

    [Fact]
    public void AllCandidates_ContactUnits_IsSourcePixelEdges()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.All(candidates, c => Assert.Equal("SOURCE_PIXEL_EDGES", c.ContactUnits));
    }

    [Fact]
    public void AllCandidates_ExtractionStatus_IsExtracted()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.All(candidates, c =>
            Assert.Equal("ADJACENCY_PLANNING_CANDIDATE_EXTRACTED", c.ExtractionStatus));
    }

    [Fact]
    public void AllCandidates_GeometryStatus_IsNoGeometryCreated()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.All(candidates, c =>
            Assert.Equal("NO_GEOMETRY_CREATED", c.GeometryStatus));
    }

    // -----------------------------------------------------------------------
    // Actionable vs ignored
    // -----------------------------------------------------------------------

    [Fact]
    public void IgnoredCandidates_IsActionable_IsFalse()
    {
        var ignored = BuildReal().Extraction.Candidates
            .Where(c => c.CandidateType == "IGNORED_BOUNDARY_ADJACENCY")
            .ToList();
        Assert.True(ignored.Count > 0);
        Assert.All(ignored, c => Assert.False(c.IsActionable));
    }

    [Fact]
    public void ActionableCandidates_IsActionable_IsTrue()
    {
        var actionable = BuildReal().Extraction.Candidates
            .Where(c => c.CandidateType != "IGNORED_BOUNDARY_ADJACENCY")
            .ToList();
        Assert.True(actionable.Count > 0);
        Assert.All(actionable, c => Assert.True(c.IsActionable));
    }

    // -----------------------------------------------------------------------
    // Candidate type priorities
    // -----------------------------------------------------------------------

    [Fact]
    public void FrontageCandidates_Priority_Is10()
    {
        var candidates = BuildReal().Extraction.Candidates
            .Where(c => c.CandidateType == "FRONTAGE_PLANNING_CANDIDATE").ToList();
        Assert.True(candidates.Count > 0);
        Assert.All(candidates, c => Assert.Equal(10, c.CandidatePriority));
    }

    [Fact]
    public void RearServiceAccessCandidates_Priority_Is20()
    {
        var candidates = BuildReal().Extraction.Candidates
            .Where(c => c.CandidateType == "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE").ToList();
        Assert.True(candidates.Count > 0);
        Assert.All(candidates, c => Assert.Equal(20, c.CandidatePriority));
    }

    [Fact]
    public void IgnoredCandidates_Priority_Is999()
    {
        var candidates = BuildReal().Extraction.Candidates
            .Where(c => c.CandidateType == "IGNORED_BOUNDARY_ADJACENCY").ToList();
        Assert.True(candidates.Count > 0);
        Assert.All(candidates, c => Assert.Equal(999, c.CandidatePriority));
    }

    // -----------------------------------------------------------------------
    // Blocked by requirements
    // -----------------------------------------------------------------------

    [Fact]
    public void IgnoredCandidates_BlockedByRequirements_IsNone()
    {
        var ignored = BuildReal().Extraction.Candidates
            .Where(c => c.CandidateType == "IGNORED_BOUNDARY_ADJACENCY").ToList();
        Assert.True(ignored.Count > 0);
        Assert.All(ignored, c =>
        {
            Assert.Single(c.BlockedByRequirements);
            Assert.Equal("NONE", c.BlockedByRequirements[0]);
        });
    }

    [Fact]
    public void ActionableCandidates_BlockedByThreeRequirements()
    {
        var actionable = BuildReal().Extraction.Candidates
            .Where(c => c.CandidateType != "IGNORED_BOUNDARY_ADJACENCY").ToList();
        Assert.True(actionable.Count > 0);
        Assert.All(actionable, c =>
        {
            Assert.Equal(3, c.BlockedByRequirements.Count);
            Assert.Contains("CONCRETE_GEOMETRY_GENERATOR_NOT_IMPLEMENTED", c.BlockedByRequirements);
            Assert.Contains("STATIC_TILE_WRITER_NOT_IMPLEMENTED",          c.BlockedByRequirements);
            Assert.Contains("RUNTIME_VALIDATION_NOT_RUN",                  c.BlockedByRequirements);
        });
    }

    // -----------------------------------------------------------------------
    // Source edge passthrough
    // -----------------------------------------------------------------------

    [Fact]
    public void AllCandidates_SourceEdgeId_IsNonEmpty()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.All(candidates, c => Assert.False(string.IsNullOrEmpty(c.SourceEdgeId)));
    }

    [Fact]
    public void AllCandidates_SourceEdgeOrder_IsPositive()
    {
        var candidates = BuildReal().Extraction.Candidates;
        Assert.All(candidates, c => Assert.True(c.SourceEdgeOrder > 0));
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false
    // -----------------------------------------------------------------------

    [Fact]
    public void ClaimBoundary_WritesLotpack_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.WritesLotpack);
    }

    [Fact]
    public void ClaimBoundary_WritesWorldgenLua_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.WritesWorldgenLua);
    }

    [Fact]
    public void ClaimBoundary_RuntimeProven_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.RuntimeProven);
    }

    [Fact]
    public void ClaimBoundary_PublicPlayableClaim_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.PublicPlayableClaim);
    }

    [Fact]
    public void ClaimBoundary_WriterReadyClaim_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.WriterReadyClaim);
    }

    [Fact]
    public void ClaimBoundary_GeneratesTerrainNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.GeneratesTerrainNow);
    }

    [Fact]
    public void ClaimBoundary_GeneratesBuildingsNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.GeneratesBuildingsNow);
    }

    [Fact]
    public void ClaimBoundary_GeneratesSidewalksNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.GeneratesSidewalksNow);
    }

    [Fact]
    public void ClaimBoundary_SubdividesLotsNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.SubdividesLotsNow);
    }

    [Fact]
    public void ClaimBoundary_CapturesChunkLayersNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.CapturesChunkLayersNow);
    }

    [Fact]
    public void ClaimBoundary_PlacesFencesNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.PlacesFencesNow);
    }

    [Fact]
    public void ClaimBoundary_PlacesUniqueBuildingsNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.PlacesUniqueBuildingsNow);
    }

    [Fact]
    public void ClaimBoundary_SelectsConcreteBuildingIdsNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.SelectsConcreteBuildingIdsNow);
    }

    [Fact]
    public void ClaimBoundary_CreatesConcreteGeometryNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.CreatesConcreteGeometryNow);
    }

    [Fact]
    public void ClaimBoundary_MaterializesLayoutNow_IsFalse()
    {
        Assert.False(BuildReal().Extraction.ClaimBoundary.MaterializesLayoutNow);
    }

    // -----------------------------------------------------------------------
    // Markdown, CSV, Summary
    // -----------------------------------------------------------------------

    [Fact]
    public void Markdown_ContainsInputChainSection()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("## Input Chain", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsExtractionContractSection()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("## Extraction Contract", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsCandidateTypeMappingSection()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("## Candidate Type Mapping", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsPlanningCandidateRecordsSection()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("## Planning Candidate Records", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsExtractionTotalsSection()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("## Extraction Totals", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsContactTotalsByCandidateTypeSection()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("## Contact Totals by Candidate Type", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsWhyThisStillCannotExecuteSection()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("## Why This Still Cannot Execute", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsClaimBoundarySection()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("## Claim Boundary", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsVerdict()
    {
        var e  = BuildReal().Extraction;
        var md = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderMarkdown(e);
        Assert.Contains("MAP25N_WORLDBUILDER_ADJACENCY_PLANNING_CANDIDATE_EXTRACTION_CONTRACT_COMPLETE", md,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Csv_Header_IsCorrect()
    {
        var e      = BuildReal().Extraction;
        var csv    = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderCsv(e);
        var header = csv.Split('\n')[0].Trim();
        const string expected =
            "candidate_order,candidate_id,source_edge_id,source_edge_order,source_adjacency_relationship," +
            "candidate_type,candidate_priority,candidate_family," +
            "component_a_id,component_a_order,component_a_source_color," +
            "component_a_intent,component_a_intent_family," +
            "component_b_id,component_b_order,component_b_source_color," +
            "component_b_intent,component_b_intent_family," +
            "contact_length_px,contact_units,is_actionable," +
            "future_geometry_requirement_id,blocked_by_requirements," +
            "extraction_status,geometry_status,notes";
        Assert.True(string.Equals(expected, header, StringComparison.Ordinal),
            $"CSV header mismatch.\nExpected: {expected}\nActual:   {header}");
    }

    [Fact]
    public void Summary_ContainsVerdict()
    {
        var result  = BuildReal();
        var summary = DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder.RenderSummary(result);
        Assert.Contains("MAP25N_WORLDBUILDER_ADJACENCY_PLANNING_CANDIDATE_EXTRACTION_CONTRACT_COMPLETE", summary,
            StringComparison.Ordinal);
    }
}
