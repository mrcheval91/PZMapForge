using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderSourceMaskRegionExtractionBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-smre-tests", Path.GetRandomFileName());

    public DeadMtlWorldBuilderSourceMaskRegionExtractionBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string RealSourcePng =>
        @"E:\Omni\Zomboid\assets\raw\map_00.png";

    private static string RealMetadataPath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "tiles",
            "map_00.zone_metadata.json");

    private static string RealGeometryPrimitiveSchemaPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-geometry-primitive-schema",
            "map_00", "map_00.geometry_primitive_schema.json");

    private static string RealGeometryPreflightPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-concrete-geometry-preflight",
            "map_00", "map_00.concrete_geometry_preflight.json");

    private DeadMtlWorldBuilderSourceMaskRegionExtractionResult RunReal() =>
        DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder.Build(
            RealSourcePng, RealMetadataPath,
            RealGeometryPrimitiveSchemaPath, RealGeometryPreflightPath);

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
    public void Build_Error_WhenSourcePngMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder.Build(
            Path.Combine(_tempDir, "no.png"), f, f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("source PNG"));
    }

    [Fact]
    public void Build_Error_WhenMetadataMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder.Build(
            RealSourcePng, Path.Combine(_tempDir, "no_meta.json"), f, f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("metadata"));
    }

    [Fact]
    public void Build_Error_WhenGeometryPrimitiveSchemaMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder.Build(
            RealSourcePng, RealMetadataPath, Path.Combine(_tempDir, "no_gps.json"), f);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found") && e.Contains("geometry primitive schema"));
    }

    [Fact]
    public void Build_Error_WhenGeometryPreflightMissing()
    {
        var f = WriteMinimalFile("f.json");
        var result = DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder.Build(
            RealSourcePng, RealMetadataPath, f, Path.Combine(_tempDir, "no_gp.json"));
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
            "pzmapforge.deadmtl.worldbuilder.source-mask-region-extraction.v1",
            result.Extraction.Format);
    }

    // -----------------------------------------------------------------------
    // Status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Status_IsContractOnly()
    {
        var result = RunReal();
        Assert.Equal("SOURCE_MASK_REGION_EXTRACTION_CONTRACT_ONLY", result.Extraction.Status);
    }

    [Fact]
    public void Build_RuntimeStatus_IsNotRuntimeProven()
    {
        var result = RunReal();
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Extraction.RuntimeStatus);
    }

    [Fact]
    public void Build_WriterStatus_IsNotImplemented()
    {
        var result = RunReal();
        Assert.Equal("NOT_IMPLEMENTED", result.Extraction.WriterStatus);
    }

    [Fact]
    public void Build_GenerationStatus_IsNotExecuted()
    {
        var result = RunReal();
        Assert.Equal("NOT_EXECUTED", result.Extraction.GenerationStatus);
    }

    [Fact]
    public void Build_GeometryStatus_IsMaskRegionsOnly()
    {
        var result = RunReal();
        Assert.Equal("MASK_REGIONS_ONLY_NO_GEOMETRY_CREATED", result.Extraction.GeometryStatus);
    }

    [Fact]
    public void Build_ExtractionStatus_IsExtracted()
    {
        var result = RunReal();
        Assert.Equal("SOURCE_MASK_REGIONS_EXTRACTED", result.Extraction.ExtractionStatus);
    }

    // -----------------------------------------------------------------------
    // Source PNG contract values
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_SourceContract_WidthAndHeight_Are256()
    {
        var sc = RunReal().Extraction.SourceContract;
        Assert.Equal(256, sc.SourcePngWidthPx);
        Assert.Equal(256, sc.SourcePngHeightPx);
    }

    [Fact]
    public void Build_SourceContract_PixelCount_Is65536()
    {
        var sc = RunReal().Extraction.SourceContract;
        Assert.Equal(65536, sc.SourcePixelCount);
    }

    [Fact]
    public void Build_SourceContract_AuthoringScale()
    {
        var sc = RunReal().Extraction.SourceContract;
        Assert.Equal("1_PIXEL_EQUALS_1_WORLD_TILE", sc.AuthoringScale);
    }

    [Fact]
    public void Build_SourceContract_BoundsArePixelBoundsNotGeometry()
    {
        var sc = RunReal().Extraction.SourceContract;
        Assert.True(sc.BoundsArePixelBoundsNotGeometry);
    }

    // -----------------------------------------------------------------------
    // Region counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MaskRegionCount_IsSeven()
    {
        var result = RunReal();
        Assert.Equal(7, result.Extraction.Totals.MaskRegionCount);
    }

    [Fact]
    public void Build_KnownColorRegionCount_IsSeven()
    {
        var result = RunReal();
        Assert.Equal(7, result.Extraction.Totals.KnownColorRegionCount);
    }

    [Fact]
    public void Build_UnknownColorRegionCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Extraction.Totals.UnknownColorRegionCount);
    }

    [Fact]
    public void Build_MetadataMatchedRegionCount_IsSeven()
    {
        var result = RunReal();
        Assert.Equal(7, result.Extraction.Totals.MetadataMatchedRegionCount);
    }

    [Fact]
    public void Build_MetadataMissingRegionCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Extraction.Totals.MetadataMissingRegionCount);
    }

    [Fact]
    public void Build_MaskRegionPixelTotal_Is65536()
    {
        var result = RunReal();
        Assert.Equal(65536, result.Extraction.Totals.MaskRegionPixelTotal);
    }

    // -----------------------------------------------------------------------
    // Exact pixel counts for all 7 colors
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ResidentialPixelCount_Is36740()
    {
        var result = RunReal();
        Assert.Equal(36740, result.Extraction.Totals.ResidentialPixelCount);
    }

    [Fact]
    public void Build_MainRoadPixelCount_Is11378()
    {
        var result = RunReal();
        Assert.Equal(11378, result.Extraction.Totals.MainRoadPixelCount);
    }

    [Fact]
    public void Build_GreenspacePixelCount_Is10898()
    {
        var result = RunReal();
        Assert.Equal(10898, result.Extraction.Totals.GreenspacePixelCount);
    }

    [Fact]
    public void Build_BackAlleyPixelCount_Is3746()
    {
        var result = RunReal();
        Assert.Equal(3746, result.Extraction.Totals.BackAlleyPixelCount);
    }

    [Fact]
    public void Build_CivicPlaceholderPixelCount_Is960()
    {
        var result = RunReal();
        Assert.Equal(960, result.Extraction.Totals.CivicPlaceholderPixelCount);
    }

    [Fact]
    public void Build_CommercialPixelCount_Is926()
    {
        var result = RunReal();
        Assert.Equal(926, result.Extraction.Totals.CommercialPixelCount);
    }

    [Fact]
    public void Build_IgnorePixelCount_Is888()
    {
        var result = RunReal();
        Assert.Equal(888, result.Extraction.Totals.IgnorePixelCount);
    }

    // -----------------------------------------------------------------------
    // All regions have correct primitive type and geometry status
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AllRegions_HaveMaskRegionPrimitiveType()
    {
        var regions = RunReal().Extraction.MaskRegions;
        Assert.All(regions, r => Assert.Equal("MASK_REGION", r.PrimitiveType));
    }

    [Fact]
    public void Build_AllRegions_HaveSourceMaskOnlyGeometryStatus()
    {
        var regions = RunReal().Extraction.MaskRegions;
        Assert.All(regions, r => Assert.Equal("SOURCE_MASK_ONLY_NO_GEOMETRY_CREATED", r.GeometryStatus));
    }

    // -----------------------------------------------------------------------
    // All bounds within 0..255
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AllBounds_WithinSourcePngRange()
    {
        var regions = RunReal().Extraction.MaskRegions;
        Assert.All(regions, r =>
        {
            Assert.True(r.BoundsMinX >= 0 && r.BoundsMinX <= 255);
            Assert.True(r.BoundsMinY >= 0 && r.BoundsMinY <= 255);
            Assert.True(r.BoundsMaxX >= 0 && r.BoundsMaxX <= 255);
            Assert.True(r.BoundsMaxY >= 0 && r.BoundsMaxY <= 255);
        });
    }

    // -----------------------------------------------------------------------
    // Future geometry requirement mapping
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ResidentialRegion_MapsToLotGeometry()
    {
        var region = RunReal().Extraction.MaskRegions
            .Single(r => r.SourceColor == "#7200FF");
        Assert.Contains("LOT_GEOMETRY", region.FutureGeometryRequirementIds);
    }

    [Fact]
    public void Build_CommercialRegion_MapsToLotGeometry()
    {
        var region = RunReal().Extraction.MaskRegions
            .Single(r => r.SourceColor == "#42CCFF");
        Assert.Contains("LOT_GEOMETRY", region.FutureGeometryRequirementIds);
    }

    [Fact]
    public void Build_MainRoadRegion_MapsToMainRoadCorridorAndSidewalk()
    {
        var region = RunReal().Extraction.MaskRegions
            .Single(r => r.SourceColor == "#FF6600");
        Assert.Contains("MAIN_ROAD_CORRIDOR_GEOMETRY", region.FutureGeometryRequirementIds);
        Assert.Contains("SIDEWALK_GEOMETRY",            region.FutureGeometryRequirementIds);
    }

    [Fact]
    public void Build_BackAlleyRegion_MapsToBackAlleyCorridorGeometry()
    {
        var region = RunReal().Extraction.MaskRegions
            .Single(r => r.SourceColor == "#F000FF");
        Assert.Contains("BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY", region.FutureGeometryRequirementIds);
    }

    [Fact]
    public void Build_CivicRegion_MapsToUniqueBuildingBinding()
    {
        var region = RunReal().Extraction.MaskRegions
            .Single(r => r.SourceColor == "#B2BD87");
        Assert.Contains("UNIQUE_BUILDING_BINDING", region.FutureGeometryRequirementIds);
    }

    [Fact]
    public void Build_IgnoreRegion_HasNoneFutureReqAndIgnoreConsumer()
    {
        var region = RunReal().Extraction.MaskRegions
            .Single(r => r.SourceColor == "#000000");
        Assert.Contains("NONE",   region.FutureGeometryRequirementIds);
        Assert.Contains("IGNORE", region.FutureConsumers);
    }

    // -----------------------------------------------------------------------
    // Zero-counts
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CreatedGeometryCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Extraction.Totals.CreatedGeometryCount);
    }

    [Fact]
    public void Build_WriterReadyRegionCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Extraction.Totals.WriterReadyRegionCount);
    }

    [Fact]
    public void Build_RuntimeValidatedRegionCount_IsZero()
    {
        var result = RunReal();
        Assert.Equal(0, result.Extraction.Totals.RuntimeValidatedRegionCount);
    }

    // -----------------------------------------------------------------------
    // Validation rules
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ValidationRules_NoRuntimeWriterMaterializationClaims()
    {
        var vr = RunReal().Extraction.ValidationRules;
        Assert.True(vr.NoRuntimeClaimFromExtraction);
        Assert.True(vr.NoWriterClaimFromExtraction);
        Assert.True(vr.NoMaterializationFromExtraction);
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false (15 fields)
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var cb = RunReal().Extraction.ClaimBoundary;
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
        var markdown = DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder.RenderMarkdown(result.Extraction);
        Assert.Contains("MAP-25J",                          markdown, StringComparison.Ordinal);
        Assert.Contains("Input Chain",                      markdown, StringComparison.Ordinal);
        Assert.Contains("Source PNG Contract",              markdown, StringComparison.Ordinal);
        Assert.Contains("Mask Region Records",              markdown, StringComparison.Ordinal);
        Assert.Contains("Pixel Count Totals",               markdown, StringComparison.Ordinal);
        Assert.Contains("Metadata Match Validation",        markdown, StringComparison.Ordinal);
        Assert.Contains("Future Geometry Requirement Mapping", markdown, StringComparison.Ordinal);
        Assert.Contains("Why This Still Cannot Execute",    markdown, StringComparison.Ordinal);
        Assert.Contains("Claim Boundary",                   markdown, StringComparison.Ordinal);
        Assert.Contains("MAP25J_WORLDBUILDER_SOURCE_MASK_REGION_EXTRACTION_CONTRACT_COMPLETE",
            markdown, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsHeader()
    {
        var result = RunReal();
        var csv    = DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder.RenderCsv(result.Extraction);
        Assert.StartsWith(
            "region_order,source_color,role,zone_type,street_class,pixel_count,",
            csv);
    }

    // -----------------------------------------------------------------------
    // Summary verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var result  = RunReal();
        var summary = DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder.RenderSummary(result);
        Assert.Contains("MAP25J_WORLDBUILDER_SOURCE_MASK_REGION_EXTRACTION_CONTRACT_COMPLETE",
            summary, StringComparison.Ordinal);
    }
}
