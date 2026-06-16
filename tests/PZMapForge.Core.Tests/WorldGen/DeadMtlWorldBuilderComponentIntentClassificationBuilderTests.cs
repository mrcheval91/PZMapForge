using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderComponentIntentClassificationBuilderTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string MetadataPath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "tiles",
            "map_00.zone_metadata.json");

    private static string SourceMaskRegionsPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-source-mask-region-extraction",
            "map_00", "map_00.source_mask_region_extraction.json");

    private static string ConnectedComponentsPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-connected-component-extraction",
            "map_00", "map_00.connected_component_extraction.json");

    private static string GeometryPrimitiveSchemaPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-geometry-primitive-schema",
            "map_00", "map_00.geometry_primitive_schema.json");

    private static string GeometryPreflightPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-concrete-geometry-preflight",
            "map_00", "map_00.concrete_geometry_preflight.json");

    private static DeadMtlWorldBuilderComponentIntentClassificationResult BuildValid() =>
        DeadMtlWorldBuilderComponentIntentClassificationBuilder.Build(
            MetadataPath, SourceMaskRegionsPath, ConnectedComponentsPath,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);

    // -----------------------------------------------------------------------
    // Missing file errors
    // -----------------------------------------------------------------------

    [Fact]
    public void MissingMetadata_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderComponentIntentClassificationBuilder.Build(
            "nonexistent.json", SourceMaskRegionsPath, ConnectedComponentsPath,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("metadata") && e.Contains("not found"));
    }

    [Fact]
    public void MissingSourceMaskRegionExtraction_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderComponentIntentClassificationBuilder.Build(
            MetadataPath, "nonexistent.json", ConnectedComponentsPath,
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("source mask region extraction") && e.Contains("not found"));
    }

    [Fact]
    public void MissingConnectedComponentExtraction_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderComponentIntentClassificationBuilder.Build(
            MetadataPath, SourceMaskRegionsPath, "nonexistent.json",
            GeometryPrimitiveSchemaPath, GeometryPreflightPath);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("connected component extraction") && e.Contains("not found"));
    }

    [Fact]
    public void MissingGeometryPrimitiveSchema_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderComponentIntentClassificationBuilder.Build(
            MetadataPath, SourceMaskRegionsPath, ConnectedComponentsPath,
            "nonexistent.json", GeometryPreflightPath);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("geometry primitive schema") && e.Contains("not found"));
    }

    [Fact]
    public void MissingConcreteGeometryPreflight_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderComponentIntentClassificationBuilder.Build(
            MetadataPath, SourceMaskRegionsPath, ConnectedComponentsPath,
            GeometryPrimitiveSchemaPath, "nonexistent.json");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("concrete geometry preflight") && e.Contains("not found"));
    }

    // -----------------------------------------------------------------------
    // Valid build
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_ReturnsValid()
    {
        var r = BuildValid();
        Assert.True(r.IsValid, $"Errors: {string.Join(", ", r.Errors)}");
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void ValidBuild_FormatCorrect()
    {
        Assert.Equal("pzmapforge.deadmtl.worldbuilder.component-intent-classification.v1",
            BuildValid().Classification.Format);
    }

    [Fact]
    public void ValidBuild_StatusFieldsCorrect()
    {
        var cl = BuildValid().Classification;
        Assert.Equal("COMPONENT_INTENT_CLASSIFICATION_CONTRACT_ONLY",   cl.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN",                               cl.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",                                  cl.WriterStatus);
        Assert.Equal("NOT_EXECUTED",                                     cl.GenerationStatus);
        Assert.Equal("INTENT_CLASSIFICATION_ONLY_NO_GEOMETRY_CREATED",  cl.GeometryStatus);
        Assert.Equal("COMPONENT_INTENTS_CLASSIFIED",                     cl.ClassificationStatus);
        Assert.Equal("NOT_MATERIALIZED",                                 cl.MaterializationStatus);
    }

    // -----------------------------------------------------------------------
    // Classification contract
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_ClassificationContractValues()
    {
        var cc = BuildValid().Classification.ClassificationContract;
        Assert.Equal("map_00",                                          cc.TileId);
        Assert.Equal(45,                                                cc.ParentConnectedComponentCount);
        Assert.Equal("MAP25K_CONNECTED_COMPONENT_EXTRACTION",           cc.SourceComponentContract);
        Assert.Equal("CONNECTED_COMPONENTS_ONLY_NO_GEOMETRY_CREATED",  cc.ClassificationInputStatus);
        Assert.Equal("INTENT_BUCKETS_ONLY",                            cc.ClassificationOutputStatus);
        Assert.True(cc.ComponentBoundsArePixelBoundsNotGeometry);
        Assert.True(cc.IntentRecordsAreNotGeometry);
        Assert.True(cc.GeometryMustBeCreatedByFutureStep);
    }

    // -----------------------------------------------------------------------
    // Global totals
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_ParentConnectedComponentCount45()
    {
        Assert.Equal(45, BuildValid().Classification.Totals.ParentConnectedComponentCount);
    }

    [Fact]
    public void ValidBuild_ClassifiedComponentCount45()
    {
        Assert.Equal(45, BuildValid().Classification.Totals.ClassifiedComponentCount);
    }

    [Fact]
    public void ValidBuild_UnclassifiedComponentCount0()
    {
        Assert.Equal(0, BuildValid().Classification.Totals.UnclassifiedComponentCount);
    }

    [Fact]
    public void ValidBuild_IntentBucketCount7()
    {
        Assert.Equal(7, BuildValid().Classification.Totals.IntentBucketCount);
    }

    [Fact]
    public void ValidBuild_ClassifiedPixelTotal65536()
    {
        Assert.Equal(65536, BuildValid().Classification.Totals.ClassifiedPixelTotal);
    }

    // -----------------------------------------------------------------------
    // Intent counts by bucket
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_ResidentialLotBlockCount22()
    {
        Assert.Equal(22, BuildValid().Classification.Totals.ResidentialLotBlockCount);
    }

    [Fact]
    public void ValidBuild_CommercialLotBlockCount4()
    {
        Assert.Equal(4, BuildValid().Classification.Totals.CommercialLotBlockCount);
    }

    [Fact]
    public void ValidBuild_MainRoadCorridorCount1()
    {
        Assert.Equal(1, BuildValid().Classification.Totals.MainRoadCorridorCount);
    }

    [Fact]
    public void ValidBuild_BackAlleyCorridorCount10()
    {
        Assert.Equal(10, BuildValid().Classification.Totals.BackAlleyCorridorCount);
    }

    [Fact]
    public void ValidBuild_GreenspaceMassCount1()
    {
        Assert.Equal(1, BuildValid().Classification.Totals.GreenspaceMassCount);
    }

    [Fact]
    public void ValidBuild_CivicPlaceholderCount2()
    {
        Assert.Equal(2, BuildValid().Classification.Totals.CivicPlaceholderCount);
    }

    [Fact]
    public void ValidBuild_IgnoreBorderCount5()
    {
        Assert.Equal(5, BuildValid().Classification.Totals.IgnoreBorderCount);
    }

    // -----------------------------------------------------------------------
    // Pixel totals by bucket
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_ResidentialLotBlockPixelTotal36740()
    {
        Assert.Equal(36740, BuildValid().Classification.Totals.ResidentialLotBlockPixelTotal);
    }

    [Fact]
    public void ValidBuild_CommercialLotBlockPixelTotal926()
    {
        Assert.Equal(926, BuildValid().Classification.Totals.CommercialLotBlockPixelTotal);
    }

    [Fact]
    public void ValidBuild_MainRoadCorridorPixelTotal11378()
    {
        Assert.Equal(11378, BuildValid().Classification.Totals.MainRoadCorridorPixelTotal);
    }

    [Fact]
    public void ValidBuild_BackAlleyCorridorPixelTotal3746()
    {
        Assert.Equal(3746, BuildValid().Classification.Totals.BackAlleyCorridorPixelTotal);
    }

    [Fact]
    public void ValidBuild_GreenspaceMassPixelTotal10898()
    {
        Assert.Equal(10898, BuildValid().Classification.Totals.GreenspaceMassPixelTotal);
    }

    [Fact]
    public void ValidBuild_CivicPlaceholderPixelTotal960()
    {
        Assert.Equal(960, BuildValid().Classification.Totals.CivicPlaceholderPixelTotal);
    }

    [Fact]
    public void ValidBuild_IgnoreBorderPixelTotal888()
    {
        Assert.Equal(888, BuildValid().Classification.Totals.IgnoreBorderPixelTotal);
    }

    // -----------------------------------------------------------------------
    // All record field values
    // -----------------------------------------------------------------------

    [Fact]
    public void AllRecords_GeometryStatusIntentOnly()
    {
        var records = BuildValid().Classification.IntentRecords;
        Assert.All(records, r =>
            Assert.Equal("INTENT_ONLY_NO_GEOMETRY_CREATED", r.GeometryStatus));
    }

    [Fact]
    public void AllRecords_SourceComponentStatusConnectedPixelComponentOnly()
    {
        var records = BuildValid().Classification.IntentRecords;
        Assert.All(records, r =>
            Assert.Equal("CONNECTED_PIXEL_COMPONENT_ONLY", r.SourceComponentStatus));
    }

    [Fact]
    public void AllRecords_ClassificationConfidenceMatch()
    {
        var records = BuildValid().Classification.IntentRecords;
        Assert.All(records, r =>
            Assert.Equal("COMPONENT_COLOR_ROLE_METADATA_MATCH", r.ClassificationConfidence));
    }

    [Fact]
    public void AllRecords_BoundsWithin0To255()
    {
        var records = BuildValid().Classification.IntentRecords;
        Assert.All(records, r =>
        {
            Assert.InRange(r.BoundsMinX, 0, 255);
            Assert.InRange(r.BoundsMinY, 0, 255);
            Assert.InRange(r.BoundsMaxX, 0, 255);
            Assert.InRange(r.BoundsMaxY, 0, 255);
        });
    }

    // -----------------------------------------------------------------------
    // Future geometry requirements per bucket
    // -----------------------------------------------------------------------

    [Fact]
    public void ResidentialRecords_IncludeLotGeometry()
    {
        var records = BuildValid().Classification.IntentRecords
            .Where(r => r.IntentBucket == "RESIDENTIAL_LOT_BLOCK").ToList();
        Assert.NotEmpty(records);
        Assert.All(records, r =>
            Assert.Contains("LOT_GEOMETRY", r.FutureGeometryRequirementIds));
    }

    [Fact]
    public void CommercialRecords_IncludeLotGeometry()
    {
        var records = BuildValid().Classification.IntentRecords
            .Where(r => r.IntentBucket == "COMMERCIAL_LOT_BLOCK").ToList();
        Assert.NotEmpty(records);
        Assert.All(records, r =>
            Assert.Contains("LOT_GEOMETRY", r.FutureGeometryRequirementIds));
    }

    [Fact]
    public void MainRoadRecord_IncludesMainRoadCorridorAndSidewalk()
    {
        var record = BuildValid().Classification.IntentRecords
            .Single(r => r.IntentBucket == "MAIN_ROAD_CORRIDOR");
        Assert.Contains("MAIN_ROAD_CORRIDOR_GEOMETRY", record.FutureGeometryRequirementIds);
        Assert.Contains("SIDEWALK_GEOMETRY",            record.FutureGeometryRequirementIds);
    }

    [Fact]
    public void BackAlleyRecords_IncludeBackAlleyCorridorGeometry()
    {
        var records = BuildValid().Classification.IntentRecords
            .Where(r => r.IntentBucket == "BACK_ALLEY_CORRIDOR").ToList();
        Assert.NotEmpty(records);
        Assert.All(records, r =>
            Assert.Contains("BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY", r.FutureGeometryRequirementIds));
    }

    [Fact]
    public void CivicRecords_IncludeUniqueBuildingBinding()
    {
        var records = BuildValid().Classification.IntentRecords
            .Where(r => r.IntentBucket == "CIVIC_PLACEHOLDER").ToList();
        Assert.NotEmpty(records);
        Assert.All(records, r =>
            Assert.Contains("UNIQUE_BUILDING_BINDING", r.FutureGeometryRequirementIds));
    }

    [Fact]
    public void IgnoreRecords_FutureReqIncludesNone()
    {
        var records = BuildValid().Classification.IntentRecords
            .Where(r => r.IntentBucket == "IGNORE_BORDER").ToList();
        Assert.NotEmpty(records);
        Assert.All(records, r =>
            Assert.Contains("NONE", r.FutureGeometryRequirementIds));
    }

    [Fact]
    public void IgnoreRecords_FutureConsumerIncludesIgnore()
    {
        var records = BuildValid().Classification.IntentRecords
            .Where(r => r.IntentBucket == "IGNORE_BORDER").ToList();
        Assert.NotEmpty(records);
        Assert.All(records, r =>
            Assert.Contains("IGNORE", r.FutureConsumers));
    }

    // -----------------------------------------------------------------------
    // Zero counts
    // -----------------------------------------------------------------------

    [Fact]
    public void CreatedGeometryCount_IsZero()
    {
        Assert.Equal(0, BuildValid().Classification.Totals.CreatedGeometryCount);
    }

    [Fact]
    public void WriterReadyIntentCount_IsZero()
    {
        Assert.Equal(0, BuildValid().Classification.Totals.WriterReadyIntentCount);
    }

    [Fact]
    public void RuntimeValidatedIntentCount_IsZero()
    {
        Assert.Equal(0, BuildValid().Classification.Totals.RuntimeValidatedIntentCount);
    }

    [Fact]
    public void MaterializedIntentCount_IsZero()
    {
        Assert.Equal(0, BuildValid().Classification.Totals.MaterializedIntentCount);
    }

    // -----------------------------------------------------------------------
    // Validation rules
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidationRules_NoRuntimeClaim()
    {
        Assert.True(BuildValid().Classification.ValidationRules.NoRuntimeClaimFromIntentClassification);
    }

    [Fact]
    public void ValidationRules_NoWriterClaim()
    {
        Assert.True(BuildValid().Classification.ValidationRules.NoWriterClaimFromIntentClassification);
    }

    [Fact]
    public void ValidationRules_NoMaterializationClaim()
    {
        Assert.True(BuildValid().Classification.ValidationRules.NoMaterializationFromIntentClassification);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void ClaimBoundary_AllFalse()
    {
        var cb = BuildValid().Classification.ClaimBoundary;
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
    // Rendering
    // -----------------------------------------------------------------------

    [Fact]
    public void Markdown_ContainsRequiredSections()
    {
        var r  = BuildValid();
        var md = DeadMtlWorldBuilderComponentIntentClassificationBuilder.RenderMarkdown(r.Classification);
        Assert.Contains("## Input Chain",                        md, StringComparison.Ordinal);
        Assert.Contains("## Classification Contract",            md, StringComparison.Ordinal);
        Assert.Contains("## Intent Bucket Definitions",          md, StringComparison.Ordinal);
        Assert.Contains("## Component Intent Records",           md, StringComparison.Ordinal);
        Assert.Contains("## Intent Count Totals",                md, StringComparison.Ordinal);
        Assert.Contains("## Pixel Total Validation",             md, StringComparison.Ordinal);
        Assert.Contains("## Future Geometry Requirement Mapping", md, StringComparison.Ordinal);
        Assert.Contains("## Why This Still Cannot Execute",      md, StringComparison.Ordinal);
        Assert.Contains("## Claim Boundary",                     md, StringComparison.Ordinal);
        Assert.Contains("## Verdict",                            md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsVerdict()
    {
        var r  = BuildValid();
        var md = DeadMtlWorldBuilderComponentIntentClassificationBuilder.RenderMarkdown(r.Classification);
        Assert.Contains("MAP25L_WORLDBUILDER_COMPONENT_INTENT_CLASSIFICATION_CONTRACT_COMPLETE",
            md, StringComparison.Ordinal);
    }

    [Fact]
    public void Csv_HeaderCorrect()
    {
        var r   = BuildValid();
        var csv = DeadMtlWorldBuilderComponentIntentClassificationBuilder.RenderCsv(r.Classification);
        Assert.StartsWith(
            "intent_order,component_id,component_order,parent_source_color," +
            "role,zone_type,street_class,component_intent,intent_bucket,intent_family," +
            "classification_reason,classification_confidence," +
            "pixel_count,bounds_min_x,bounds_min_y,bounds_max_x,bounds_max_y,bounds_width,bounds_height," +
            "bounds_status,source_component_status,geometry_status," +
            "future_geometry_requirement_ids,future_consumers,blocked_by_requirements,notes",
            csv, StringComparison.Ordinal);
    }

    [Fact]
    public void Summary_ContainsVerdict()
    {
        var r   = BuildValid();
        var sum = DeadMtlWorldBuilderComponentIntentClassificationBuilder.RenderSummary(r);
        Assert.Contains("MAP25L_WORLDBUILDER_COMPONENT_INTENT_CLASSIFICATION_CONTRACT_COMPLETE",
            sum, StringComparison.Ordinal);
    }
}
