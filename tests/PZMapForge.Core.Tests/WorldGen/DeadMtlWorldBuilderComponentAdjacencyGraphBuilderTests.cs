using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderComponentAdjacencyGraphBuilderTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string SourcePngPath =>
        Path.Combine("E:", "Omni", "Zomboid", "assets", "raw", "map_00.png");

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

    private static DeadMtlWorldBuilderComponentAdjacencyGraphResult BuildReal() =>
        DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.Build(
            SourcePngPath,
            ConnectedComponentsPath,
            ComponentIntentsPath,
            GeometryPrimitiveSchemaPath,
            GeometryPreflightPath);

    private static string Missing => Path.Combine(RepoRoot, "does-not-exist.json");

    // -----------------------------------------------------------------------
    // Missing file errors
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Fails_WhenSourcePngMissing()
    {
        var result = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.Build(
            Missing, ConnectedComponentsPath, ComponentIntentsPath,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Build_Fails_WhenConnectedComponentsMissing()
    {
        var result = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.Build(
            SourcePngPath, Missing, ComponentIntentsPath,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Build_Fails_WhenComponentIntentsMissing()
    {
        var result = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.Build(
            SourcePngPath, ConnectedComponentsPath, Missing,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Build_Fails_WhenGeometryPrimitiveSchemaMissing()
    {
        var result = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.Build(
            SourcePngPath, ConnectedComponentsPath, ComponentIntentsPath,
            Missing, GeometryPreflightPath);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void Build_Fails_WhenGeometryPreflightMissing()
    {
        var result = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.Build(
            SourcePngPath, ConnectedComponentsPath, ComponentIntentsPath,
            GeometryPrimitiveSchemaPath, Missing);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    // -----------------------------------------------------------------------
    // Valid build
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Succeeds_WithRealMap00()
    {
        var result = BuildReal();
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // -----------------------------------------------------------------------
    // Format and status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Graph_Format_IsCorrect()
    {
        var g = BuildReal().Graph;
        Assert.Equal("pzmapforge.deadmtl.worldbuilder.component-adjacency-graph.v1", g.Format);
    }

    [Fact]
    public void Graph_Status_IsContractOnly()
    {
        var g = BuildReal().Graph;
        Assert.Equal("COMPONENT_ADJACENCY_GRAPH_CONTRACT_ONLY", g.Status);
    }

    [Fact]
    public void Graph_RuntimeStatus_IsNotRuntimeProven()
    {
        var g = BuildReal().Graph;
        Assert.Equal("NOT_RUNTIME_PROVEN", g.RuntimeStatus);
    }

    [Fact]
    public void Graph_WriterStatus_IsNotImplemented()
    {
        var g = BuildReal().Graph;
        Assert.Equal("NOT_IMPLEMENTED", g.WriterStatus);
    }

    [Fact]
    public void Graph_GenerationStatus_IsNotExecuted()
    {
        var g = BuildReal().Graph;
        Assert.Equal("NOT_EXECUTED", g.GenerationStatus);
    }

    [Fact]
    public void Graph_GeometryStatus_IsAdjacencyGraphOnly()
    {
        var g = BuildReal().Graph;
        Assert.Equal("ADJACENCY_GRAPH_ONLY_NO_GEOMETRY_CREATED", g.GeometryStatus);
    }

    [Fact]
    public void Graph_AdjacencyStatus_IsGraphBuilt()
    {
        var g = BuildReal().Graph;
        Assert.Equal("COMPONENT_ADJACENCY_GRAPH_BUILT", g.AdjacencyStatus);
    }

    [Fact]
    public void Graph_MaterializationStatus_IsNotMaterialized()
    {
        var g = BuildReal().Graph;
        Assert.Equal("NOT_MATERIALIZED", g.MaterializationStatus);
    }

    // -----------------------------------------------------------------------
    // Adjacency graph contract values
    // -----------------------------------------------------------------------

    [Fact]
    public void Contract_TileId_IsMap00()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.Equal("map_00", agc.TileId);
    }

    [Fact]
    public void Contract_SourcePngWidthPx_Is256()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.Equal(256, agc.SourcePngWidthPx);
    }

    [Fact]
    public void Contract_SourcePngHeightPx_Is256()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.Equal(256, agc.SourcePngHeightPx);
    }

    [Fact]
    public void Contract_SourcePixelCount_Is65536()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.Equal(65536, agc.SourcePixelCount);
    }

    [Fact]
    public void Contract_DiagonalAdjacencyEnabled_IsFalse()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.False(agc.DiagonalAdjacencyEnabled);
    }

    [Fact]
    public void Contract_ConnectivityRule_IsFourWay()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.Equal("FOUR_WAY_NEIGHBOR_PIXELS", agc.ConnectivityRule);
    }

    [Fact]
    public void Contract_AdjacencyEdgesAreUndirected_IsTrue()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.True(agc.AdjacencyEdgesAreUndirected);
    }

    [Fact]
    public void Contract_AdjacencyEdgesAreNotGeometry_IsTrue()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.True(agc.AdjacencyEdgesAreNotGeometry);
    }

    [Fact]
    public void Contract_GeometryMustBeCreatedByFutureStep_IsTrue()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.True(agc.GeometryMustBeCreatedByFutureStep);
    }

    [Fact]
    public void Contract_SourceComponentContract_IsMap25K()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.Equal("MAP25K_CONNECTED_COMPONENT_EXTRACTION", agc.SourceComponentContract);
    }

    [Fact]
    public void Contract_SourceIntentContract_IsMap25L()
    {
        var agc = BuildReal().Graph.AdjacencyGraphContract;
        Assert.Equal("MAP25L_COMPONENT_INTENT_CLASSIFICATION", agc.SourceIntentContract);
    }

    // -----------------------------------------------------------------------
    // Component node and edge global totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Totals_ComponentNodeCount_Is45()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(45, t.ComponentNodeCount);
    }

    [Fact]
    public void Totals_AdjacencyEdgeCount_Is82()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(82, t.AdjacencyEdgeCount);
    }

    [Fact]
    public void Totals_KnownComponentEdgeCount_Is82()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(82, t.KnownComponentEdgeCount);
    }

    [Fact]
    public void Totals_UnknownComponentEdgeCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.UnknownComponentEdgeCount);
    }

    [Fact]
    public void Totals_SelfEdgeCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.SelfEdgeCount);
    }

    [Fact]
    public void Totals_DuplicateEdgeCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.DuplicateEdgeCount);
    }

    [Fact]
    public void Totals_DiagonalEdgeCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.DiagonalEdgeCount);
    }

    [Fact]
    public void Totals_ContactLengthTotalPx_Is4535()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(4535, t.ContactLengthTotalPx);
    }

    // -----------------------------------------------------------------------
    // Per-relationship edge counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Totals_FrontageCandidateEdgeCount_Is26()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(26, t.FrontageCandidateEdgeCount);
    }

    [Fact]
    public void Totals_RearOrServiceAccessCandidateEdgeCount_Is26()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(26, t.RearOrServiceAccessCandidateEdgeCount);
    }

    [Fact]
    public void Totals_StreetNetworkTouchpointEdgeCount_Is10()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(10, t.StreetNetworkTouchpointEdgeCount);
    }

    [Fact]
    public void Totals_GreenspaceAccessEdgeCount_Is1()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(1, t.GreenspaceAccessEdgeCount);
    }

    [Fact]
    public void Totals_GreenspaceCivicEdgeCount_Is2()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(2, t.GreenspaceCivicEdgeCount);
    }

    [Fact]
    public void Totals_MixedLotBlockEdgeCount_Is6()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(6, t.MixedLotBlockEdgeCount);
    }

    [Fact]
    public void Totals_IgnoreBoundaryAdjacencyEdgeCount_Is11()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(11, t.IgnoreBoundaryAdjacencyEdgeCount);
    }

    [Fact]
    public void Totals_OtherIntentAdjacencyEdgeCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.OtherIntentAdjacencyEdgeCount);
    }

    // -----------------------------------------------------------------------
    // Per-relationship contact totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Totals_FrontageCandidateContactTotalPx_Is1860()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(1860, t.FrontageCandidateContactTotalPx);
    }

    [Fact]
    public void Totals_RearOrServiceAccessContactTotalPx_Is1713()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(1713, t.RearOrServiceAccessContactTotalPx);
    }

    [Fact]
    public void Totals_StreetNetworkTouchpointContactTotalPx_Is91()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(91, t.StreetNetworkTouchpointContactTotalPx);
    }

    [Fact]
    public void Totals_GreenspaceAccessContactTotalPx_Is291()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(291, t.GreenspaceAccessContactTotalPx);
    }

    [Fact]
    public void Totals_GreenspaceCivicContactTotalPx_Is158()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(158, t.GreenspaceCivicContactTotalPx);
    }

    [Fact]
    public void Totals_MixedLotBlockContactTotalPx_Is164()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(164, t.MixedLotBlockContactTotalPx);
    }

    [Fact]
    public void Totals_IgnoreBoundaryAdjacencyContactTotalPx_Is258()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(258, t.IgnoreBoundaryAdjacencyContactTotalPx);
    }

    [Fact]
    public void Totals_OtherIntentAdjacencyContactTotalPx_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.OtherIntentAdjacencyContactTotalPx);
    }

    // -----------------------------------------------------------------------
    // Intent pair edge counts and contact totals
    // -----------------------------------------------------------------------

    private static List<DeadMtlWorldBuilderAdjacencyEdgeRecord> EdgesByIntentPair(
        List<DeadMtlWorldBuilderAdjacencyEdgeRecord> edges, string intentX, string intentY) =>
        edges.Where(e =>
            (e.ComponentAIntent == intentX && e.ComponentBIntent == intentY) ||
            (e.ComponentAIntent == intentY && e.ComponentBIntent == intentX))
            .ToList();

    [Fact]
    public void IntentPair_ResidentialMainRoad_EdgeCount22_Contact1794()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "RESIDENTIAL_LOT_BLOCK", "MAIN_ROAD_CORRIDOR");
        Assert.Equal(22, pair.Count);
        Assert.Equal(1794, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_CommercialMainRoad_EdgeCount4_Contact66()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "COMMERCIAL_LOT_BLOCK", "MAIN_ROAD_CORRIDOR");
        Assert.Equal(4, pair.Count);
        Assert.Equal(66, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_ResidentialBackAlley_EdgeCount23_Contact1681()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "RESIDENTIAL_LOT_BLOCK", "BACK_ALLEY_CORRIDOR");
        Assert.Equal(23, pair.Count);
        Assert.Equal(1681, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_CommercialBackAlley_EdgeCount3_Contact32()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "COMMERCIAL_LOT_BLOCK", "BACK_ALLEY_CORRIDOR");
        Assert.Equal(3, pair.Count);
        Assert.Equal(32, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_MainRoadBackAlley_EdgeCount10_Contact91()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "MAIN_ROAD_CORRIDOR", "BACK_ALLEY_CORRIDOR");
        Assert.Equal(10, pair.Count);
        Assert.Equal(91, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_MainRoadGreenspace_EdgeCount1_Contact291()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "MAIN_ROAD_CORRIDOR", "GREENSPACE_MASS");
        Assert.Equal(1, pair.Count);
        Assert.Equal(291, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_GreenspaceCivic_EdgeCount2_Contact158()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "GREENSPACE_MASS", "CIVIC_PLACEHOLDER");
        Assert.Equal(2, pair.Count);
        Assert.Equal(158, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_ResidentialCommercial_EdgeCount6_Contact164()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "RESIDENTIAL_LOT_BLOCK", "COMMERCIAL_LOT_BLOCK");
        Assert.Equal(6, pair.Count);
        Assert.Equal(164, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_MainRoadIgnore_EdgeCount5_Contact234()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "MAIN_ROAD_CORRIDOR", "IGNORE_BORDER");
        Assert.Equal(5, pair.Count);
        Assert.Equal(234, pair.Sum(e => e.ContactLengthPx));
    }

    [Fact]
    public void IntentPair_BackAlleyIgnore_EdgeCount6_Contact24()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var pair  = EdgesByIntentPair(edges, "BACK_ALLEY_CORRIDOR", "IGNORE_BORDER");
        Assert.Equal(6, pair.Count);
        Assert.Equal(24, pair.Sum(e => e.ContactLengthPx));
    }

    // -----------------------------------------------------------------------
    // Edge ID uniqueness and ordering
    // -----------------------------------------------------------------------

    [Fact]
    public void AllEdgeIds_AreUnique()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        var unique = edges.Select(e => e.EdgeId).Distinct().Count();
        Assert.Equal(edges.Count, unique);
    }

    [Fact]
    public void FirstEdgeId_IsMap00Adjacency0001()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.Equal("map_00_adjacency_0001", edges[0].EdgeId);
    }

    [Fact]
    public void LastEdgeId_IsMap00Adjacency0082()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.Equal("map_00_adjacency_0082", edges[^1].EdgeId);
    }

    [Fact]
    public void AllEdges_ComponentAOrder_LessThan_ComponentBOrder()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.All(edges, e => Assert.True(e.ComponentAOrder < e.ComponentBOrder,
            $"Edge {e.EdgeId}: A={e.ComponentAOrder} >= B={e.ComponentBOrder}"));
    }

    // -----------------------------------------------------------------------
    // Edge fixed field values
    // -----------------------------------------------------------------------

    [Fact]
    public void AllEdges_ContactLengthPx_IsPositive()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.All(edges, e => Assert.True(e.ContactLengthPx > 0,
            $"Edge {e.EdgeId} has contact_length_px={e.ContactLengthPx}"));
    }

    [Fact]
    public void AllEdges_IsUndirected_IsTrue()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.All(edges, e => Assert.True(e.IsUndirected));
    }

    [Fact]
    public void AllEdges_DiagonalContact_IsFalse()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.All(edges, e => Assert.False(e.DiagonalContact));
    }

    [Fact]
    public void AllEdges_ConnectivityRule_IsFourWay()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.All(edges, e => Assert.Equal("FOUR_WAY_NEIGHBOR_PIXELS", e.ConnectivityRule));
    }

    [Fact]
    public void AllEdges_EdgeStatus_IsPixelTouchAdjacency()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.All(edges, e => Assert.Equal("PIXEL_TOUCH_ADJACENCY_ONLY", e.EdgeStatus));
    }

    [Fact]
    public void AllEdges_GeometryStatus_IsAdjacencyOnly()
    {
        var edges = BuildReal().Graph.AdjacencyEdges;
        Assert.All(edges, e => Assert.Equal("ADJACENCY_ONLY_NO_GEOMETRY_CREATED", e.GeometryStatus));
    }

    // -----------------------------------------------------------------------
    // Blocked by requirements
    // -----------------------------------------------------------------------

    [Fact]
    public void IgnoreEdges_BlockedByRequirements_IsNone()
    {
        var edges = BuildReal().Graph.AdjacencyEdges
            .Where(e => e.AdjacencyRelationship == "IGNORE_BOUNDARY_ADJACENCY")
            .ToList();
        Assert.True(edges.Count > 0);
        Assert.All(edges, e =>
        {
            Assert.Single(e.BlockedByRequirements);
            Assert.Equal("NONE", e.BlockedByRequirements[0]);
        });
    }

    [Fact]
    public void NonIgnoreEdges_BlockedByThreeRequirements()
    {
        var edges = BuildReal().Graph.AdjacencyEdges
            .Where(e => e.AdjacencyRelationship != "IGNORE_BOUNDARY_ADJACENCY")
            .ToList();
        Assert.True(edges.Count > 0);
        Assert.All(edges, e =>
        {
            Assert.Equal(3, e.BlockedByRequirements.Count);
            Assert.Contains("CONCRETE_GEOMETRY_GENERATOR_NOT_IMPLEMENTED", e.BlockedByRequirements);
            Assert.Contains("STATIC_TILE_WRITER_NOT_IMPLEMENTED",          e.BlockedByRequirements);
            Assert.Contains("RUNTIME_VALIDATION_NOT_RUN",                  e.BlockedByRequirements);
        });
    }

    // -----------------------------------------------------------------------
    // Zero geometry counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Totals_CreatedGeometryCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.CreatedGeometryCount);
    }

    [Fact]
    public void Totals_WriterReadyEdgeCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.WriterReadyEdgeCount);
    }

    [Fact]
    public void Totals_RuntimeValidatedEdgeCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.RuntimeValidatedEdgeCount);
    }

    [Fact]
    public void Totals_MaterializedEdgeCount_IsZero()
    {
        var t = BuildReal().Graph.Totals;
        Assert.Equal(0, t.MaterializedEdgeCount);
    }

    // -----------------------------------------------------------------------
    // Validation rules
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidationRules_NoSelfEdges_IsTrue()
    {
        var vr = BuildReal().Graph.ValidationRules;
        Assert.True(vr.NoSelfEdges);
    }

    [Fact]
    public void ValidationRules_NoDuplicateUndirectedEdges_IsTrue()
    {
        var vr = BuildReal().Graph.ValidationRules;
        Assert.True(vr.NoDuplicateUndirectedEdges);
    }

    [Fact]
    public void ValidationRules_NoDiagonalAdjacency_IsTrue()
    {
        var vr = BuildReal().Graph.ValidationRules;
        Assert.True(vr.NoDiagonalAdjacency);
    }

    [Fact]
    public void ValidationRules_NoRuntimeClaimFromAdjacencyGraph_IsTrue()
    {
        var vr = BuildReal().Graph.ValidationRules;
        Assert.True(vr.NoRuntimeClaimFromAdjacencyGraph);
    }

    [Fact]
    public void ValidationRules_NoWriterClaimFromAdjacencyGraph_IsTrue()
    {
        var vr = BuildReal().Graph.ValidationRules;
        Assert.True(vr.NoWriterClaimFromAdjacencyGraph);
    }

    [Fact]
    public void ValidationRules_NoMaterializationFromAdjacencyGraph_IsTrue()
    {
        var vr = BuildReal().Graph.ValidationRules;
        Assert.True(vr.NoMaterializationFromAdjacencyGraph);
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false
    // -----------------------------------------------------------------------

    [Fact]
    public void ClaimBoundary_WritesLotpack_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.WritesLotpack);
    }

    [Fact]
    public void ClaimBoundary_WritesWorldgenLua_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.WritesWorldgenLua);
    }

    [Fact]
    public void ClaimBoundary_RuntimeProven_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.RuntimeProven);
    }

    [Fact]
    public void ClaimBoundary_PublicPlayableClaim_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.PublicPlayableClaim);
    }

    [Fact]
    public void ClaimBoundary_WriterReadyClaim_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.WriterReadyClaim);
    }

    [Fact]
    public void ClaimBoundary_GeneratesTerrainNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.GeneratesTerrainNow);
    }

    [Fact]
    public void ClaimBoundary_GeneratesBuildingsNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.GeneratesBuildingsNow);
    }

    [Fact]
    public void ClaimBoundary_GeneratesSidewalksNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.GeneratesSidewalksNow);
    }

    [Fact]
    public void ClaimBoundary_SubdividesLotsNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.SubdividesLotsNow);
    }

    [Fact]
    public void ClaimBoundary_CapturesChunkLayersNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.CapturesChunkLayersNow);
    }

    [Fact]
    public void ClaimBoundary_PlacesFencesNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.PlacesFencesNow);
    }

    [Fact]
    public void ClaimBoundary_PlacesUniqueBuildingsNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.PlacesUniqueBuildingsNow);
    }

    [Fact]
    public void ClaimBoundary_SelectsConcreteBuildingIdsNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.SelectsConcreteBuildingIdsNow);
    }

    [Fact]
    public void ClaimBoundary_CreatesConcreteGeometryNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.CreatesConcreteGeometryNow);
    }

    [Fact]
    public void ClaimBoundary_MaterializesLayoutNow_IsFalse()
    {
        Assert.False(BuildReal().Graph.ClaimBoundary.MaterializesLayoutNow);
    }

    // -----------------------------------------------------------------------
    // Markdown, CSV, Summary
    // -----------------------------------------------------------------------

    [Fact]
    public void Markdown_ContainsInputChainSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Input Chain", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsAdjacencyGraphContractSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Adjacency Graph Contract", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsAdjacencyDetectionRuleSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Adjacency Detection Rule", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsRelationshipClassificationRulesSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Relationship Classification Rules", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsAdjacencyEdgeRecordsSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Adjacency Edge Records", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsGraphTotalsSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Graph Totals", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsRelationshipContactTotalsSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Relationship Contact Totals", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsIntentPairEdgeTotalsSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Intent Pair Edge Totals", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsFuturePlanningUseSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Future Planning Use", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsWhyThisStillCannotExecuteSection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Why This Still Cannot Execute", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsClaimBoundarySection()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("## Claim Boundary", md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsVerdict()
    {
        var g  = BuildReal().Graph;
        var md = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderMarkdown(g);
        Assert.Contains("MAP25M_WORLDBUILDER_COMPONENT_ADJACENCY_GRAPH_CONTRACT_COMPLETE", md,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Csv_Header_IsCorrect()
    {
        var g   = BuildReal().Graph;
        var csv = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderCsv(g);
        var header = csv.Split('\n')[0].Trim();
        const string expected =
            "edge_order,edge_id,component_a_id,component_a_order,component_a_source_color," +
            "component_a_intent,component_a_intent_family," +
            "component_b_id,component_b_order,component_b_source_color," +
            "component_b_intent,component_b_intent_family," +
            "contact_length_px,contact_units,adjacency_relationship,intent_pair_key," +
            "is_undirected,connectivity_rule,diagonal_contact,edge_status,geometry_status," +
            "future_planning_use,blocked_by_requirements,notes";
        Assert.True(string.Equals(expected, header, StringComparison.Ordinal),
            $"CSV header mismatch.\nExpected: {expected}\nActual:   {header}");
    }

    [Fact]
    public void Summary_ContainsVerdict()
    {
        var result  = BuildReal();
        var summary = DeadMtlWorldBuilderComponentAdjacencyGraphBuilder.RenderSummary(result);
        Assert.Contains("MAP25M_WORLDBUILDER_COMPONENT_ADJACENCY_GRAPH_CONTRACT_COMPLETE", summary,
            StringComparison.Ordinal);
    }
}
