using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderComponentAccessProfileBuilderTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PlanningCandidatesPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "worldbuilder-adjacency-planning-candidate-extraction",
            "map_00", "map_00.adjacency_planning_candidate_extraction.json");

    private static string ComponentIntentsPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "worldbuilder-component-intent-classification",
            "map_00", "map_00.component_intent_classification.json");

    private static DeadMtlWorldBuilderComponentAccessProfileResult BuildResult() =>
        DeadMtlWorldBuilderComponentAccessProfileBuilder.Build(PlanningCandidatesPath, ComponentIntentsPath);

    // -----------------------------------------------------------------------
    // Missing file errors (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void PlanningCandidatesMissing_ReturnsError()
    {
        var r = DeadMtlWorldBuilderComponentAccessProfileBuilder.Build(
            "/nonexistent/candidates.json", ComponentIntentsPath);
        Assert.False(r.IsValid);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void ComponentIntentsMissing_ReturnsError()
    {
        var r = DeadMtlWorldBuilderComponentAccessProfileBuilder.Build(
            PlanningCandidatesPath, "/nonexistent/intents.json");
        Assert.False(r.IsValid);
        Assert.NotEmpty(r.Errors);
    }

    // -----------------------------------------------------------------------
    // Valid build (1)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Succeeds_WithValidInputs()
    {
        var r = BuildResult();
        Assert.True(r.IsValid, $"Errors: {string.Join("; ", r.Errors)}");
        Assert.NotEmpty(r.Verdict);
    }

    // -----------------------------------------------------------------------
    // Format (1)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_FormatString_Is_ComponentAccessProfileV1()
    {
        var r = BuildResult();
        Assert.Equal("pzmapforge.deadmtl.worldbuilder.component-access-profile.v1", r.Format);
    }

    // -----------------------------------------------------------------------
    // Status fields (7)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ProfileStatus_IsExtracted()
    {
        var r = BuildResult();
        Assert.True(r.Profiles.Count > 0);
        Assert.Equal("COMPONENT_ACCESS_PROFILE_EXTRACTED", r.Profiles[0].ProfileStatus);
    }

    [Fact]
    public void Build_GeometryStatus_IsNoGeometryCreated()
    {
        var r = BuildResult();
        Assert.True(r.Profiles.Count > 0);
        Assert.Equal("NO_GEOMETRY_CREATED", r.Profiles[0].GeometryStatus);
    }

    [Fact]
    public void Build_WriterReadyClaim_IsFalse()
    {
        var r = BuildResult();
        Assert.False(r.ProfileContract.WriterReadyClaim);
    }

    [Fact]
    public void Build_RuntimeProven_IsFalse()
    {
        var r = BuildResult();
        Assert.False(r.ProfileContract.RuntimeProven);
    }

    [Fact]
    public void Build_WritesLotpack_IsFalse()
    {
        var r = BuildResult();
        Assert.False(r.ProfileContract.WritesLotpack);
    }

    [Fact]
    public void Build_WritesWorldgenLua_IsFalse()
    {
        var r = BuildResult();
        Assert.False(r.ProfileContract.WritesWorldgenLua);
    }

    [Fact]
    public void Build_MaterializesLayoutNow_IsFalse()
    {
        var r = BuildResult();
        Assert.False(r.ProfileContract.MaterializesLayoutNow);
    }

    // -----------------------------------------------------------------------
    // Profile record count (1)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ProfileRecordCount_Is45()
    {
        var r = BuildResult();
        Assert.Equal(45, r.Profiles.Count);
    }

    // -----------------------------------------------------------------------
    // Access class totals (9)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_DualAccessCandidateCount_Is25()
    {
        var r = BuildResult();
        Assert.Equal(25, r.ProfileContract.DualAccessCandidateCount);
    }

    [Fact]
    public void Build_FrontageOnlyCandidateCount_Is1()
    {
        var r = BuildResult();
        Assert.Equal(1, r.ProfileContract.FrontageOnlyCandidateCount);
    }

    [Fact]
    public void Build_RearServiceOnlyCount_Is0()
    {
        var r = BuildResult();
        Assert.Equal(0, r.ProfileContract.RearServiceOnlyCandidateCount);
    }

    [Fact]
    public void Build_LandlockedCount_Is0()
    {
        var r = BuildResult();
        Assert.Equal(0, r.ProfileContract.LandlockedCandidateCount);
    }

    [Fact]
    public void Build_MainRoadCorridorNodeCount_Is1()
    {
        var r = BuildResult();
        Assert.Equal(1, r.ProfileContract.MainRoadCorridorNodeCount);
    }

    [Fact]
    public void Build_BackAlleyCorridorNodeCount_Is10()
    {
        var r = BuildResult();
        Assert.Equal(10, r.ProfileContract.BackAlleyCorridorNodeCount);
    }

    [Fact]
    public void Build_GreenspaceMassNodeCount_Is1()
    {
        var r = BuildResult();
        Assert.Equal(1, r.ProfileContract.GreenspaceMassNodeCount);
    }

    [Fact]
    public void Build_CivicPlaceholderNodeCount_Is2()
    {
        var r = BuildResult();
        Assert.Equal(2, r.ProfileContract.CivicPlaceholderNodeCount);
    }

    [Fact]
    public void Build_IgnoredBoundaryCount_Is5()
    {
        var r = BuildResult();
        Assert.Equal(5, r.ProfileContract.IgnoredBoundaryComponentCount);
    }

    // -----------------------------------------------------------------------
    // Component 1 (RES dual, 2 rear alleys) (7)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp1_FrontageCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 1);
        Assert.Equal(1, p.FrontageCandidateCount);
    }

    [Fact]
    public void Comp1_RearCount_Is2()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 1);
        Assert.Equal(2, p.RearServiceAccessCandidateCount);
    }

    [Fact]
    public void Comp1_MixedCount_Is0()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 1);
        Assert.Equal(0, p.MixedLotBlockCandidateCount);
    }

    [Fact]
    public void Comp1_AccessClass_IsDualAccessCandidate()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 1);
        Assert.Equal("DUAL_ACCESS_CANDIDATE", p.AccessReadinessClass);
    }

    [Fact]
    public void Comp1_PrimaryFrontageComponentId_IsComp23()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 1);
        Assert.Equal("map_00_component_0023", p.PrimaryFrontageComponentId);
    }

    [Fact]
    public void Comp1_PrimaryFrontageContactPx_Is178()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 1);
        Assert.Equal(178, p.PrimaryFrontageContactPx);
    }

    [Fact]
    public void Comp1_PrimaryRearServiceComponentId_IsComp30()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 1);
        Assert.Equal("map_00_component_0030", p.PrimaryRearServiceComponentId);
    }

    // -----------------------------------------------------------------------
    // Component 2 (primary_rear check) (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp2_PrimaryRearComponentId_IsComp26()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 2);
        Assert.Equal("map_00_component_0026", p.PrimaryRearServiceComponentId);
    }

    [Fact]
    public void Comp2_PrimaryRearContactPx_Is145()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 2);
        Assert.Equal(145, p.PrimaryRearServiceContactPx);
    }

    // -----------------------------------------------------------------------
    // Component 10 (RES with mixed candidate) (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp10_MixedLotBlockCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 10);
        Assert.Equal(1, p.MixedLotBlockCandidateCount);
    }

    [Fact]
    public void Comp10_TotalCandidateCount_Is3()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 10);
        Assert.Equal(3, p.TotalCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Component 18/22 (RES with mixed candidate) (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp18_MixedLotBlockCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 18);
        Assert.Equal(1, p.MixedLotBlockCandidateCount);
    }

    [Fact]
    public void Comp22_MixedLotBlockCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 22);
        Assert.Equal(1, p.MixedLotBlockCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Component 23 (MAIN_ROAD_CORRIDOR) (7)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp23_FrontageCount_Is26()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal(26, p.FrontageCandidateCount);
    }

    [Fact]
    public void Comp23_StreetNetworkCount_Is10()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal(10, p.StreetNetworkTouchpointCount);
    }

    [Fact]
    public void Comp23_GreenspaceAccessCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal(1, p.GreenspaceAccessCandidateCount);
    }

    [Fact]
    public void Comp23_IgnoreBoundaryCount_Is5()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal(5, p.IgnoreBoundaryAdjacencyCount);
    }

    [Fact]
    public void Comp23_TotalActionableCount_Is37()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal(37, p.TotalActionableCandidateCount);
    }

    [Fact]
    public void Comp23_TotalCandidateCount_Is42()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal(42, p.TotalCandidateCount);
    }

    [Fact]
    public void Comp23_AccessClass_IsMainRoadCorridorNode()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal("MAIN_ROAD_CORRIDOR_NODE", p.AccessReadinessClass);
    }

    // -----------------------------------------------------------------------
    // Component 23 primary candidates (not applicable → empty) (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp23_PrimaryFrontageComponentId_IsEmpty()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal(string.Empty, p.PrimaryFrontageComponentId);
    }

    [Fact]
    public void Comp23_PrimaryFrontageContactPx_IsZero()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 23);
        Assert.Equal(0, p.PrimaryFrontageContactPx);
    }

    // -----------------------------------------------------------------------
    // Component 24 (GREENSPACE) (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp24_GreenspaceCivicCount_Is2()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 24);
        Assert.Equal(2, p.GreenspaceCivicCandidateCount);
    }

    [Fact]
    public void Comp24_AccessClass_IsGreenspaceMassNode()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 24);
        Assert.Equal("GREENSPACE_MASS_NODE", p.AccessReadinessClass);
    }

    // -----------------------------------------------------------------------
    // Component 25 (BACK, 6 rear served) (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp25_RearServiceCount_Is6()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 25);
        Assert.Equal(6, p.RearServiceAccessCandidateCount);
    }

    [Fact]
    public void Comp25_TotalActionableCount_Is7()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 25);
        Assert.Equal(7, p.TotalActionableCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Component 26 (BACK, 7 rear served) (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp26_RearServiceCount_Is7()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 26);
        Assert.Equal(7, p.RearServiceAccessCandidateCount);
    }

    [Fact]
    public void Comp26_TotalActionableCount_Is8()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 26);
        Assert.Equal(8, p.TotalActionableCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Component 32 (BACK, no rear served, has ignore) (3)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp32_RearServiceCount_Is0()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 32);
        Assert.Equal(0, p.RearServiceAccessCandidateCount);
    }

    [Fact]
    public void Comp32_StreetNetworkCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 32);
        Assert.Equal(1, p.StreetNetworkTouchpointCount);
    }

    [Fact]
    public void Comp32_TotalActionableCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 32);
        Assert.Equal(1, p.TotalActionableCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Component 35/36 (CIVIC) (3)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp35_GreenspaceCivicCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 35);
        Assert.Equal(1, p.GreenspaceCivicCandidateCount);
    }

    [Fact]
    public void Comp35_AccessClass_IsCivicPlaceholderNode()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 35);
        Assert.Equal("CIVIC_PLACEHOLDER_NODE", p.AccessReadinessClass);
    }

    [Fact]
    public void Comp36_GreenspaceCivicCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 36);
        Assert.Equal(1, p.GreenspaceCivicCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Component 37 (COM dual) (4)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp37_FrontageCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 37);
        Assert.Equal(1, p.FrontageCandidateCount);
    }

    [Fact]
    public void Comp37_RearCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 37);
        Assert.Equal(1, p.RearServiceAccessCandidateCount);
    }

    [Fact]
    public void Comp37_MixedCount_Is2()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 37);
        Assert.Equal(2, p.MixedLotBlockCandidateCount);
    }

    [Fact]
    public void Comp37_AccessClass_IsDualAccessCandidate()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 37);
        Assert.Equal("DUAL_ACCESS_CANDIDATE", p.AccessReadinessClass);
    }

    // -----------------------------------------------------------------------
    // Component 38 (COM dual) (3)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp38_FrontageCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 38);
        Assert.Equal(1, p.FrontageCandidateCount);
    }

    [Fact]
    public void Comp38_RearCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 38);
        Assert.Equal(1, p.RearServiceAccessCandidateCount);
    }

    [Fact]
    public void Comp38_AccessClass_IsDualAccessCandidate()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 38);
        Assert.Equal("DUAL_ACCESS_CANDIDATE", p.AccessReadinessClass);
    }

    // -----------------------------------------------------------------------
    // Component 39 (COM dual) (3)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp39_FrontageCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 39);
        Assert.Equal(1, p.FrontageCandidateCount);
    }

    [Fact]
    public void Comp39_RearCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 39);
        Assert.Equal(1, p.RearServiceAccessCandidateCount);
    }

    [Fact]
    public void Comp39_MixedCount_Is2()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 39);
        Assert.Equal(2, p.MixedLotBlockCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Component 40 (COM frontage-only) (4)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp40_FrontageCount_Is1()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 40);
        Assert.Equal(1, p.FrontageCandidateCount);
    }

    [Fact]
    public void Comp40_RearCount_Is0()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 40);
        Assert.Equal(0, p.RearServiceAccessCandidateCount);
    }

    [Fact]
    public void Comp40_AccessClass_IsFrontageOnlyCandidate()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 40);
        Assert.Equal("FRONTAGE_ONLY_CANDIDATE", p.AccessReadinessClass);
    }

    [Fact]
    public void Comp40_PrimaryRearServiceComponentId_IsEmpty()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 40);
        Assert.Equal(string.Empty, p.PrimaryRearServiceComponentId);
    }

    // -----------------------------------------------------------------------
    // Component 41 (IGNORE) (3)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp41_TotalActionableCount_IsZero()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 41);
        Assert.Equal(0, p.TotalActionableCandidateCount);
    }

    [Fact]
    public void Comp41_TotalCandidateCount_Is3()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 41);
        Assert.Equal(3, p.TotalCandidateCount);
    }

    [Fact]
    public void Comp41_AccessClass_IsIgnoredBoundaryComponent()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 41);
        Assert.Equal("IGNORED_BOUNDARY_COMPONENT", p.AccessReadinessClass);
    }

    // -----------------------------------------------------------------------
    // Components 42-45 total candidate counts (4)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp42_TotalCandidateCount_Is2()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 42);
        Assert.Equal(2, p.TotalCandidateCount);
    }

    [Fact]
    public void Comp43_TotalCandidateCount_Is2()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 43);
        Assert.Equal(2, p.TotalCandidateCount);
    }

    [Fact]
    public void Comp44_TotalCandidateCount_Is2()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 44);
        Assert.Equal(2, p.TotalCandidateCount);
    }

    [Fact]
    public void Comp45_TotalCandidateCount_Is2()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 45);
        Assert.Equal(2, p.TotalCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Profile IDs and ordering (4)
    // -----------------------------------------------------------------------

    [Fact]
    public void FirstProfile_Id_IsMap00Profile0001()
    {
        var r = BuildResult();
        Assert.Equal("map_00_profile_0001", r.Profiles[0].ProfileId);
    }

    [Fact]
    public void LastProfile_Id_IsMap00Profile0045()
    {
        var r = BuildResult();
        Assert.Equal("map_00_profile_0045", r.Profiles[^1].ProfileId);
    }

    [Fact]
    public void All45ProfileIds_AreUnique()
    {
        var r = BuildResult();
        Assert.Equal(45, r.Profiles.Select(p => p.ProfileId).Distinct().Count());
    }

    [Fact]
    public void AllProfileOrders_AreUnique1To45()
    {
        var r      = BuildResult();
        var orders = r.Profiles.Select(p => p.ProfileOrder).OrderBy(x => x).ToList();
        Assert.Equal(Enumerable.Range(1, 45).ToList(), orders);
    }

    // -----------------------------------------------------------------------
    // Geometry zeroes (3)
    // -----------------------------------------------------------------------

    [Fact]
    public void CreatedGeometryCount_IsZero()
    {
        var r = BuildResult();
        Assert.Equal(0, r.ProfileContract.CreatedGeometryCount);
    }

    [Fact]
    public void WriterReadyProfileCount_IsZero()
    {
        var r = BuildResult();
        Assert.Equal(0, r.ProfileContract.WriterReadyProfileCount);
    }

    [Fact]
    public void RuntimeValidatedProfileCount_IsZero()
    {
        var r = BuildResult();
        Assert.Equal(0, r.ProfileContract.RuntimeValidatedProfileCount);
    }

    // -----------------------------------------------------------------------
    // Claim boundary — 8 more fields (8)
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratesTerrainNow_IsFalse()
    {
        Assert.False(BuildResult().ProfileContract.GeneratesTerrainNow);
    }

    [Fact]
    public void GeneratesBuildingsNow_IsFalse()
    {
        Assert.False(BuildResult().ProfileContract.GeneratesBuildingsNow);
    }

    [Fact]
    public void GeneratesSidewalksNow_IsFalse()
    {
        Assert.False(BuildResult().ProfileContract.GeneratesSidewalksNow);
    }

    [Fact]
    public void SubdividesLotsNow_IsFalse()
    {
        Assert.False(BuildResult().ProfileContract.SubdividesLotsNow);
    }

    [Fact]
    public void CapturesChunkLayersNow_IsFalse()
    {
        Assert.False(BuildResult().ProfileContract.CapturesChunkLayersNow);
    }

    [Fact]
    public void PlacesFencesNow_IsFalse()
    {
        Assert.False(BuildResult().ProfileContract.PlacesFencesNow);
    }

    [Fact]
    public void PlacesUniqueBuildingsNow_IsFalse()
    {
        Assert.False(BuildResult().ProfileContract.PlacesUniqueBuildingsNow);
    }

    [Fact]
    public void SelectsConcreteBuildingIdsNow_IsFalse()
    {
        Assert.False(BuildResult().ProfileContract.SelectsConcreteBuildingIdsNow);
    }

    // -----------------------------------------------------------------------
    // Blocked-by semantics (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void LotBlockProfiles_HaveBlockedByRequirements()
    {
        var r           = BuildResult();
        var lotProfiles = r.Profiles.Where(p =>
            p.AccessReadinessClass is "DUAL_ACCESS_CANDIDATE" or "FRONTAGE_ONLY_CANDIDATE" or
            "REAR_SERVICE_ONLY_CANDIDATE" or "LANDLOCKED_CANDIDATE");
        foreach (var p in lotProfiles)
            Assert.Equal(3, p.AccessReadinessBlockedBy.Count);
    }

    [Fact]
    public void NonLotBlockProfiles_HaveNoneBlockedBy()
    {
        var r              = BuildResult();
        var nonLotProfiles = r.Profiles.Where(p =>
            p.AccessReadinessClass is "MAIN_ROAD_CORRIDOR_NODE" or "BACK_ALLEY_CORRIDOR_NODE" or
            "GREENSPACE_MASS_NODE" or "CIVIC_PLACEHOLDER_NODE" or "IGNORED_BOUNDARY_COMPONENT");
        foreach (var p in nonLotProfiles)
        {
            Assert.Single(p.AccessReadinessBlockedBy);
            Assert.Equal("NONE", p.AccessReadinessBlockedBy[0]);
        }
    }

    // -----------------------------------------------------------------------
    // Dual access and frontage-only invariants (2)
    // -----------------------------------------------------------------------

    [Fact]
    public void AllDualAccessProfiles_HaveFrontageAndRearGeqOne()
    {
        var r = BuildResult();
        foreach (var p in r.Profiles.Where(p => p.AccessReadinessClass == "DUAL_ACCESS_CANDIDATE"))
        {
            Assert.True(p.FrontageCandidateCount >= 1, $"comp {p.ComponentOrder} dual but frontage=0");
            Assert.True(p.RearServiceAccessCandidateCount >= 1, $"comp {p.ComponentOrder} dual but rear=0");
        }
    }

    [Fact]
    public void AllFrontageOnlyProfiles_HaveRearEqZero()
    {
        var r = BuildResult();
        foreach (var p in r.Profiles.Where(p => p.AccessReadinessClass == "FRONTAGE_ONLY_CANDIDATE"))
            Assert.Equal(0, p.RearServiceAccessCandidateCount);
    }

    // -----------------------------------------------------------------------
    // Markdown doc sections (4)
    // -----------------------------------------------------------------------

    private static string DocText =>
        File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            "docs", "authoring", "DEADMTL_WORLDBUILDER_COMPONENT_ACCESS_PROFILE_CONTRACT.md"));

    [Fact]
    public void Doc_HasContractOnlyHeader()
    {
        Assert.Contains("CONTRACT ONLY", DocText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Doc_HasAccessReadinessClassificationSection()
    {
        Assert.Contains("Access Readiness Classification", DocText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Doc_HasExpectedTotalsSection()
    {
        Assert.Contains("Expected Totals", DocText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Doc_HasClaimBoundarySection()
    {
        Assert.Contains("Claim Boundary", DocText, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CSV header (1)
    // -----------------------------------------------------------------------

    [Fact]
    public void CsvHeaderCorrect()
    {
        var r      = BuildResult();
        var csv    = DeadMtlWorldBuilderComponentAccessProfileBuilder.RenderCsv(r);
        var header = csv.Split('\n')[0].TrimEnd('\r');
        var expected =
            "profile_order,profile_id,component_order,component_id,source_color,intent,intent_family," +
            "frontage_candidate_count,rear_service_access_candidate_count,mixed_lot_block_candidate_count," +
            "street_network_touchpoint_count,greenspace_access_candidate_count,greenspace_civic_candidate_count," +
            "ignore_boundary_adjacency_count,total_actionable_candidate_count,total_candidate_count," +
            "primary_frontage_component_id,primary_frontage_contact_px,primary_rear_service_component_id," +
            "primary_rear_service_contact_px,access_readiness_class,geometry_status";
        Assert.True(string.Equals(expected, header, StringComparison.Ordinal),
            $"CSV header mismatch.\nExpected: {expected}\nActual:   {header}");
    }

    // -----------------------------------------------------------------------
    // Comp 1 total count (1)
    // -----------------------------------------------------------------------

    [Fact]
    public void Comp1_TotalCandidateCount_Is3()
    {
        var p = BuildResult().Profiles.Single(x => x.ComponentOrder == 1);
        Assert.Equal(3, p.TotalCandidateCount);
    }
}
