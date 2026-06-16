using System.Runtime.Versioning;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilderTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PngPath =>
        Path.Combine("E:", "Omni", "Zomboid", "assets", "raw", "map_00.png");

    private static string ConnectedComponentsPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "worldbuilder-connected-component-extraction",
            "map_00", "map_00.connected_component_extraction.json");

    private static string AccessProfilePath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "worldbuilder-component-access-profile",
            "map_00", "map_00.component_access_profile.json");

    private static DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult BuildComp1()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder();
        return builder.Build(PngPath, ConnectedComponentsPath, AccessProfilePath, 1);
    }

    // -----------------------------------------------------------------------
    // Missing input guards
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingPng_IsInvalid()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder();
        var result  = builder.Build("missing.png", ConnectedComponentsPath, AccessProfilePath, 1);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingConnectedComponents_IsInvalid()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder();
        var result  = builder.Build(PngPath, "missing.json", AccessProfilePath, 1);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_MissingAccessProfile_IsInvalid()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder();
        var result  = builder.Build(PngPath, ConnectedComponentsPath, "missing.json", 1);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Valid build
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ValidInputs_IsValid()
    {
        var result = BuildComp1();
        Assert.True(result.IsValid, $"Errors: {string.Join("; ", result.Errors)}");
    }

    [Fact]
    public void Build_ValidInputs_NoErrors()
    {
        var result = BuildComp1();
        Assert.Empty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Format and verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void Result_Format_IsExact()
    {
        var result = BuildComp1();
        Assert.True(
            string.Equals(result.Format,
                "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1",
                StringComparison.Ordinal),
            $"Actual: {result.Format}");
    }

    [Fact]
    public void Result_Verdict_IsComplete()
    {
        var result = BuildComp1();
        Assert.True(
            string.Equals(result.Verdict,
                "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE",
                StringComparison.Ordinal),
            $"Actual: {result.Verdict}");
    }

    // -----------------------------------------------------------------------
    // Contract — target identity
    // -----------------------------------------------------------------------

    [Fact]
    public void Contract_TargetComponentOrder_IsOne()
    {
        Assert.Equal(1, BuildComp1().GeometryMvpContract.TargetComponentOrder);
    }

    [Fact]
    public void Contract_TargetComponentId_IsComp0001()
    {
        var c = BuildComp1().GeometryMvpContract;
        Assert.True(
            string.Equals(c.TargetComponentId, "map_00_component_0001", StringComparison.Ordinal),
            $"Actual: {c.TargetComponentId}");
    }

    [Fact]
    public void Contract_TargetIntent_IsResidentialLotBlock()
    {
        var c = BuildComp1().GeometryMvpContract;
        Assert.True(
            string.Equals(c.TargetIntent, "RESIDENTIAL_LOT_BLOCK", StringComparison.Ordinal),
            $"Actual: {c.TargetIntent}");
    }

    [Fact]
    public void Contract_AccessReadinessClass_IsDualAccess()
    {
        var c = BuildComp1().GeometryMvpContract;
        Assert.True(
            string.Equals(c.AccessReadinessClass, "DUAL_ACCESS_CANDIDATE", StringComparison.Ordinal),
            $"Actual: {c.AccessReadinessClass}");
    }

    // -----------------------------------------------------------------------
    // Contract — bounding box
    // -----------------------------------------------------------------------

    [Fact]
    public void Contract_Bbox_MinX_Is124()
    {
        Assert.Equal(124, BuildComp1().GeometryMvpContract.ComponentBboxMinX);
    }

    [Fact]
    public void Contract_Bbox_MinY_Is10()
    {
        Assert.Equal(10, BuildComp1().GeometryMvpContract.ComponentBboxMinY);
    }

    [Fact]
    public void Contract_Bbox_MaxX_Is212()
    {
        Assert.Equal(212, BuildComp1().GeometryMvpContract.ComponentBboxMaxX);
    }

    [Fact]
    public void Contract_Bbox_MaxY_Is69()
    {
        Assert.Equal(69, BuildComp1().GeometryMvpContract.ComponentBboxMaxY);
    }

    [Fact]
    public void Contract_Bbox_WidthPx_Is89()
    {
        Assert.Equal(89, BuildComp1().GeometryMvpContract.ComponentBboxWidthPx);
    }

    [Fact]
    public void Contract_Bbox_HeightPx_Is60()
    {
        Assert.Equal(60, BuildComp1().GeometryMvpContract.ComponentBboxHeightPx);
    }

    [Fact]
    public void Contract_Bbox_PixelCount_Is5340()
    {
        Assert.Equal(5340, BuildComp1().GeometryMvpContract.ComponentPixelCount);
    }

    // -----------------------------------------------------------------------
    // Contract — side detection
    // -----------------------------------------------------------------------

    [Fact]
    public void Contract_FrontageSide_IsValidCardinal()
    {
        var side = BuildComp1().GeometryMvpContract.FrontageSide;
        var valid = new[] { "NORTH", "SOUTH", "EAST", "WEST" };
        Assert.Contains(side, valid);
    }

    [Fact]
    public void Contract_FrontageContactPx_IsPositive()
    {
        Assert.True(BuildComp1().GeometryMvpContract.FrontageContactPx > 0);
    }

    [Fact]
    public void Contract_RearServiceSide_IsValidCardinal()
    {
        var side = BuildComp1().GeometryMvpContract.RearServiceSide;
        var valid = new[] { "NORTH", "SOUTH", "EAST", "WEST" };
        Assert.Contains(side, valid);
    }

    [Fact]
    public void Contract_RearServiceContactPx_IsPositive()
    {
        Assert.True(BuildComp1().GeometryMvpContract.RearServiceContactPx > 0);
    }

    [Fact]
    public void Contract_FrontageSide_DifferentFrom_RearSide()
    {
        var c = BuildComp1().GeometryMvpContract;
        Assert.NotEqual(c.FrontageSide, c.RearServiceSide);
    }

    // -----------------------------------------------------------------------
    // Contract — lot counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Contract_LotGeometryCount_IsPositive()
    {
        Assert.True(BuildComp1().GeometryMvpContract.LotGeometryCount > 0);
    }

    [Fact]
    public void Contract_LotGeometryCount_AtMostMaxLotCount()
    {
        Assert.True(BuildComp1().GeometryMvpContract.LotGeometryCount <= 8);
    }

    [Fact]
    public void Contract_BuildingSlotGeometryCount_EqualsLotCount()
    {
        var c = BuildComp1().GeometryMvpContract;
        Assert.Equal(c.LotGeometryCount, c.BuildingSlotGeometryCount);
    }

    [Fact]
    public void Contract_AcceptedBuildingSlotCount_IsPositive()
    {
        Assert.True(BuildComp1().GeometryMvpContract.AcceptedBuildingSlotCount > 0);
    }

    // -----------------------------------------------------------------------
    // Geometry counts — pivot claim
    // -----------------------------------------------------------------------

    [Fact]
    public void Contract_CreatedGeometryCount_IsPositive()
    {
        Assert.True(BuildComp1().GeometryMvpContract.CreatedGeometryCount > 0);
    }

    [Fact]
    public void Contract_WriterReadyGeometryCount_IsZero()
    {
        Assert.Equal(0, BuildComp1().GeometryMvpContract.WriterReadyGeometryCount);
    }

    [Fact]
    public void Contract_RuntimeValidatedGeometryCount_IsZero()
    {
        Assert.Equal(0, BuildComp1().GeometryMvpContract.RuntimeValidatedGeometryCount);
    }

    [Fact]
    public void Contract_MaterializedGeometryCount_IsZero()
    {
        Assert.Equal(0, BuildComp1().GeometryMvpContract.MaterializedGeometryCount);
    }

    // -----------------------------------------------------------------------
    // Component geometry record
    // -----------------------------------------------------------------------

    [Fact]
    public void ComponentGeometry_IsNotNull()
    {
        Assert.NotNull(BuildComp1().ComponentGeometry);
    }

    [Fact]
    public void ComponentGeometry_GeometryType_IsComponentBboxMvp()
    {
        var cg = BuildComp1().ComponentGeometry!;
        Assert.True(
            string.Equals(cg.GeometryType, "COMPONENT_BBOX_MVP", StringComparison.Ordinal),
            $"Actual: {cg.GeometryType}");
    }

    [Fact]
    public void ComponentGeometry_GeometryStatus_IsCreated()
    {
        var cg = BuildComp1().ComponentGeometry!;
        Assert.True(
            string.Equals(cg.GeometryStatus, "CONCRETE_PIXEL_GEOMETRY_CREATED", StringComparison.Ordinal),
            $"Actual: {cg.GeometryStatus}");
    }

    // -----------------------------------------------------------------------
    // Lot geometry records
    // -----------------------------------------------------------------------

    [Fact]
    public void LotGeometry_IsNotEmpty()
    {
        Assert.NotEmpty(BuildComp1().LotGeometry);
    }

    [Fact]
    public void LotGeometry_AllLots_HavePositiveWidthPx()
    {
        var lots = BuildComp1().LotGeometry;
        Assert.All(lots, lot => Assert.True(lot.WidthPx > 0, $"Lot {lot.LotId} has WidthPx={lot.WidthPx}"));
    }

    [Fact]
    public void LotGeometry_AllLots_HavePositiveHeightPx()
    {
        var lots = BuildComp1().LotGeometry;
        Assert.All(lots, lot => Assert.True(lot.HeightPx > 0, $"Lot {lot.LotId} has HeightPx={lot.HeightPx}"));
    }

    [Fact]
    public void LotGeometry_AllLots_MinXWithinBbox()
    {
        var result = BuildComp1();
        var bbox   = result.GeometryMvpContract;
        Assert.All(result.LotGeometry, lot =>
            Assert.True(lot.MinX >= bbox.ComponentBboxMinX, $"{lot.LotId} MinX={lot.MinX} < bbox {bbox.ComponentBboxMinX}"));
    }

    [Fact]
    public void LotGeometry_AllLots_MaxXWithinBbox()
    {
        var result = BuildComp1();
        var bbox   = result.GeometryMvpContract;
        Assert.All(result.LotGeometry, lot =>
            Assert.True(lot.MaxX <= bbox.ComponentBboxMaxX, $"{lot.LotId} MaxX={lot.MaxX} > bbox {bbox.ComponentBboxMaxX}"));
    }

    [Fact]
    public void LotGeometry_IdsAreUnique()
    {
        var lots = BuildComp1().LotGeometry;
        var ids  = lots.Select(l => l.LotId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void LotGeometry_AllLots_GeometryType_IsLotRectangleMvp()
    {
        var lots = BuildComp1().LotGeometry;
        Assert.All(lots, lot => Assert.True(
            string.Equals(lot.GeometryType, "LOT_RECTANGLE_MVP", StringComparison.Ordinal),
            $"Lot {lot.LotId} GeometryType={lot.GeometryType}"));
    }

    // -----------------------------------------------------------------------
    // Building slot geometry records
    // -----------------------------------------------------------------------

    [Fact]
    public void BuildingSlotGeometry_IsNotEmpty()
    {
        Assert.NotEmpty(BuildComp1().BuildingSlotGeometry);
    }

    [Fact]
    public void BuildingSlotGeometry_IdsAreUnique()
    {
        var slots = BuildComp1().BuildingSlotGeometry;
        var ids   = slots.Select(s => s.SlotId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void BuildingSlotGeometry_AcceptedSlots_HavePositiveDimensions()
    {
        var slots = BuildComp1().BuildingSlotGeometry
            .Where(s => s.SlotStatus == "ACCEPTED").ToList();
        Assert.All(slots, s =>
        {
            Assert.True(s.WidthPx  >= 4, $"{s.SlotId} WidthPx={s.WidthPx}");
            Assert.True(s.HeightPx >= 4, $"{s.SlotId} HeightPx={s.HeightPx}");
        });
    }

    [Fact]
    public void BuildingSlotGeometry_AcceptedSlots_GeometryStatus_IsCreated()
    {
        var slots = BuildComp1().BuildingSlotGeometry
            .Where(s => s.SlotStatus == "ACCEPTED").ToList();
        Assert.All(slots, s => Assert.True(
            string.Equals(s.GeometryStatus, "CONCRETE_PIXEL_GEOMETRY_CREATED", StringComparison.Ordinal),
            $"{s.SlotId} GeometryStatus={s.GeometryStatus}"));
    }

    [Fact]
    public void BuildingSlotGeometry_AtLeastOneAccepted()
    {
        var accepted = BuildComp1().BuildingSlotGeometry.Count(s => s.SlotStatus == "ACCEPTED");
        Assert.True(accepted > 0, "Expected at least one accepted building slot");
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void ClaimBoundary_WritesLotpack_IsFalse()
    {
        Assert.False(BuildComp1().ClaimBoundary.WritesLotpack);
    }

    [Fact]
    public void ClaimBoundary_WritesWorldgenLua_IsFalse()
    {
        Assert.False(BuildComp1().ClaimBoundary.WritesWorldgenLua);
    }

    [Fact]
    public void ClaimBoundary_RuntimeProven_IsFalse()
    {
        Assert.False(BuildComp1().ClaimBoundary.RuntimeProven);
    }

    [Fact]
    public void ClaimBoundary_PublicPlayableClaim_IsFalse()
    {
        Assert.False(BuildComp1().ClaimBoundary.PublicPlayableClaim);
    }

    [Fact]
    public void ClaimBoundary_WriterReadyClaim_IsFalse()
    {
        Assert.False(BuildComp1().ClaimBoundary.WriterReadyClaim);
    }

    [Fact]
    public void ClaimBoundary_WriterReadyGeometryCount_IsZero()
    {
        Assert.Equal(0, BuildComp1().ClaimBoundary.WriterReadyGeometryCount);
    }

    [Fact]
    public void ClaimBoundary_RuntimeValidatedGeometryCount_IsZero()
    {
        Assert.Equal(0, BuildComp1().ClaimBoundary.RuntimeValidatedGeometryCount);
    }

    [Fact]
    public void ClaimBoundary_MaterializedGeometryCount_IsZero()
    {
        Assert.Equal(0, BuildComp1().ClaimBoundary.MaterializedGeometryCount);
    }

    // -----------------------------------------------------------------------
    // Render methods
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderJson_ContainsVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder();
        var result  = BuildComp1();
        var json    = builder.RenderJson(result);
        Assert.Contains("MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE", json, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsMvpHeader()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder();
        var result  = BuildComp1();
        var md      = builder.RenderMarkdown(result);
        Assert.Contains("Minimal Concrete Geometry MVP", md, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderCsv_HeaderIsStable()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder();
        var result  = BuildComp1();
        var csv     = builder.RenderCsv(result);
        Assert.True(
            csv.StartsWith("record_order,record_type,record_id,component_order,component_id,", StringComparison.Ordinal),
            $"Unexpected CSV header start: {csv[..Math.Min(80, csv.Length)]}");
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder();
        var result  = BuildComp1();
        var summary = builder.RenderSummary(result);
        Assert.Contains("MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE", summary, StringComparison.Ordinal);
    }
}
