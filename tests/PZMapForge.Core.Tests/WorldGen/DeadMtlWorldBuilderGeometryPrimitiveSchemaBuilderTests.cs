using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-gps-tests", Path.GetRandomFileName());

    public DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilderTests() =>
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

    private static string RealFutureLayoutPlanPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-future-world-layout-plan",
            "map_00", "map_00.future_world_layout_plan.json");

    private static string RealGeometryPreflightPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-concrete-geometry-preflight",
            "map_00", "map_00.concrete_geometry_preflight.json");

    private DeadMtlWorldBuilderGeometryPrimitiveSchemaResult RunReal() =>
        DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder.Build(
            RealProfilePath, RealMetadataPath, RealFutureLayoutPlanPath, RealGeometryPreflightPath);

    private string WriteMinimalFile(string name)
    {
        var path = Path.Combine(_tempDir, name);
        System.IO.File.WriteAllText(path, "{}", System.Text.Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Missing file errors for all 4 inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Error_WhenProfileMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder.Build(
            Path.Combine(_tempDir, "no_profile.json"), f, f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("profile"));
    }

    [Fact]
    public void Build_Error_WhenMetadataMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder.Build(
            f, Path.Combine(_tempDir, "no_meta.json"), f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("metadata"));
    }

    [Fact]
    public void Build_Error_WhenFutureLayoutPlanMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder.Build(
            f, f, Path.Combine(_tempDir, "no_flp.json"), f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("future layout"));
    }

    [Fact]
    public void Build_Error_WhenGeometryPreflightMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder.Build(
            f, f, f, Path.Combine(_tempDir, "no_gp.json"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("geometry preflight"));
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
            "pzmapforge.deadmtl.worldbuilder.geometry-primitive-schema.v1",
            result.Schema.Format);
    }

    // -----------------------------------------------------------------------
    // Status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Status_IsContractOnly()
    {
        var result = RunReal();
        Assert.Equal("GEOMETRY_PRIMITIVE_SCHEMA_CONTRACT_ONLY", result.Schema.Status);
    }

    [Fact]
    public void Build_RuntimeStatus_IsNotRuntimeProven()
    {
        var result = RunReal();
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Schema.RuntimeStatus);
    }

    [Fact]
    public void Build_WriterStatus_IsNotImplemented()
    {
        var result = RunReal();
        Assert.Equal("NOT_IMPLEMENTED", result.Schema.WriterStatus);
    }

    [Fact]
    public void Build_GenerationStatus_IsNotExecuted()
    {
        var result = RunReal();
        Assert.Equal("NOT_EXECUTED", result.Schema.GenerationStatus);
    }

    [Fact]
    public void Build_GeometryStatus_IsSchemaOnlyNoGeometryCreated()
    {
        var result = RunReal();
        Assert.Equal("SCHEMA_ONLY_NO_GEOMETRY_CREATED", result.Schema.GeometryStatus);
    }

    [Fact]
    public void Build_SchemaStatus_IsPrimitiveTypesDefined()
    {
        var result = RunReal();
        Assert.Equal("PRIMITIVE_TYPES_DEFINED", result.Schema.SchemaStatus);
    }

    // -----------------------------------------------------------------------
    // Coordinate contract
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CoordinateContract_AuthoringScale()
    {
        var cc = RunReal().Schema.CoordinateContract;
        Assert.Equal("1_PIXEL_EQUALS_1_WORLD_TILE", cc.AuthoringScale);
    }

    [Fact]
    public void Build_CoordinateContract_SourceTileSize()
    {
        var cc = RunReal().Schema.CoordinateContract;
        Assert.Equal(256, cc.SourceTileWidthPx);
        Assert.Equal(256, cc.SourceTileHeightPx);
    }

    [Fact]
    public void Build_CoordinateContract_Axes()
    {
        var cc = RunReal().Schema.CoordinateContract;
        Assert.Equal("EAST_POSITIVE",  cc.XAxis);
        Assert.Equal("SOUTH_POSITIVE", cc.YAxis);
        Assert.Equal("LEVEL_POSITIVE", cc.ZAxis);
    }

    [Fact]
    public void Build_CoordinateContract_CoordinateUnitsAndPrecision()
    {
        var cc = RunReal().Schema.CoordinateContract;
        Assert.Equal("WORLD_TILES",                          cc.CoordinateUnits);
        Assert.Equal("INTEGER_TILE_COORDINATES_FIRST_PASS",  cc.CoordinatePrecision);
    }

    // -----------------------------------------------------------------------
    // Primitive type count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_PrimitiveTypeCount_IsTen()
    {
        var result = RunReal();
        Assert.Equal(10, result.Schema.Totals.PrimitiveTypeCount);
    }

    // -----------------------------------------------------------------------
    // All 10 primitive types present
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_PrimitiveTypes_AllTenPresent()
    {
        var types = RunReal().Schema.PrimitiveTypes.Select(p => p.PrimitiveType).ToList();
        Assert.Contains("POINT",           types);
        Assert.Contains("LINE_SEGMENT",    types);
        Assert.Contains("POLYLINE",        types);
        Assert.Contains("RECTANGLE",       types);
        Assert.Contains("POLYGON",         types);
        Assert.Contains("CORRIDOR",        types);
        Assert.Contains("STRIP",           types);
        Assert.Contains("SLOT",            types);
        Assert.Contains("BOUNDARY_LINE",   types);
        Assert.Contains("MASK_REGION",     types);
    }

    // -----------------------------------------------------------------------
    // Class counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CoordinatePrimitiveCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Schema.Totals.CoordinatePrimitiveCount);
    }

    [Fact]
    public void Build_LinearPrimitiveCount_IsThree()
    {
        var result = RunReal();
        Assert.Equal(3, result.Schema.Totals.LinearPrimitiveCount);
    }

    [Fact]
    public void Build_AreaPrimitiveCount_IsTwo()
    {
        var result = RunReal();
        Assert.Equal(2, result.Schema.Totals.AreaPrimitiveCount);
    }

    [Fact]
    public void Build_DerivedAreaPrimitiveCount_IsTwo()
    {
        var result = RunReal();
        Assert.Equal(2, result.Schema.Totals.DerivedAreaPrimitiveCount);
    }

    [Fact]
    public void Build_PlacementPrimitiveCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Schema.Totals.PlacementPrimitiveCount);
    }

    [Fact]
    public void Build_SourceRegionPrimitiveCount_IsOne()
    {
        var result = RunReal();
        Assert.Equal(1, result.Schema.Totals.SourceRegionPrimitiveCount);
    }

    // -----------------------------------------------------------------------
    // Zero-counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CreatedGeometryCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Schema.Totals.CreatedGeometryCount);
    }

    [Fact]
    public void Build_WriterReadyPrimitiveCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Schema.Totals.WriterReadyPrimitiveCount);
    }

    [Fact]
    public void Build_RuntimeValidatedPrimitiveCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Schema.Totals.RuntimeValidatedPrimitiveCount);
    }

    // -----------------------------------------------------------------------
    // Validation rules
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ValidationRules_TileBoundsAre0To255()
    {
        var vr = RunReal().Schema.ValidationRules;
        Assert.Equal(0,   vr.TileBoundsMinX);
        Assert.Equal(0,   vr.TileBoundsMinY);
        Assert.Equal(255, vr.TileBoundsMaxX);
        Assert.Equal(255, vr.TileBoundsMaxY);
    }

    [Fact]
    public void Build_ValidationRules_NoRuntimeWriterMaterializationClaims()
    {
        var vr = RunReal().Schema.ValidationRules;
        Assert.True(vr.NoRuntimeClaimFromSchema);
        Assert.True(vr.NoWriterClaimFromSchema);
        Assert.True(vr.NoMaterializationFromSchema);
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false (15 fields)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var cb = RunReal().Schema.ClaimBoundary;
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
        var markdown = DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder.RenderMarkdown(result.Schema);
        Assert.Contains("MAP-25I",                     markdown, StringComparison.Ordinal);
        Assert.Contains("Input Chain",                 markdown, StringComparison.Ordinal);
        Assert.Contains("Coordinate Contract",         markdown, StringComparison.Ordinal);
        Assert.Contains("Primitive Types",             markdown, StringComparison.Ordinal);
        Assert.Contains("Validation Rules",            markdown, StringComparison.Ordinal);
        Assert.Contains("Future Consumers",            markdown, StringComparison.Ordinal);
        Assert.Contains("Why This Still Cannot Execute", markdown, StringComparison.Ordinal);
        Assert.Contains("Claim Boundary",              markdown, StringComparison.Ordinal);
        Assert.Contains("MAP25I_WORLDBUILDER_GEOMETRY_PRIMITIVE_SCHEMA_CONTRACT_COMPLETE",
            markdown, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result = RunReal();
        var csv    = DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder.RenderCsv(result.Schema);
        Assert.StartsWith(
            "primitive_type,geometry_class,coordinate_fields,required_fields,optional_fields,",
            csv);
    }

    // -----------------------------------------------------------------------
    // Summary verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = RunReal();
        var summary = DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder.RenderSummary(result);
        Assert.Contains("MAP25I_WORLDBUILDER_GEOMETRY_PRIMITIVE_SCHEMA_CONTRACT_COMPLETE",
            summary, StringComparison.Ordinal);
    }
}
