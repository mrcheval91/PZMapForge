using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderConnectedComponentExtractionBuilderTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string SourcePng =>
        @"E:\Omni\Zomboid\assets\raw\map_00.png";

    private static string MetadataPath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "tiles",
            "map_00.zone_metadata.json");

    private static string GeometryPrimitiveSchemaPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-geometry-primitive-schema",
            "map_00", "map_00.geometry_primitive_schema.json");

    private static string SourceMaskRegionsPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-source-mask-region-extraction",
            "map_00", "map_00.source_mask_region_extraction.json");

    private static DeadMtlWorldBuilderConnectedComponentExtractionResult BuildValid() =>
        DeadMtlWorldBuilderConnectedComponentExtractionBuilder.Build(
            SourcePng, MetadataPath, GeometryPrimitiveSchemaPath, SourceMaskRegionsPath);

    // -----------------------------------------------------------------------
    // Missing file errors
    // -----------------------------------------------------------------------

    [Fact]
    public void MissingSourcePng_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderConnectedComponentExtractionBuilder.Build(
            "nonexistent.png", MetadataPath, GeometryPrimitiveSchemaPath, SourceMaskRegionsPath);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("source PNG") && e.Contains("not found"));
    }

    [Fact]
    public void MissingMetadata_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderConnectedComponentExtractionBuilder.Build(
            SourcePng, "nonexistent.json", GeometryPrimitiveSchemaPath, SourceMaskRegionsPath);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("metadata") && e.Contains("not found"));
    }

    [Fact]
    public void MissingGeometryPrimitiveSchema_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderConnectedComponentExtractionBuilder.Build(
            SourcePng, MetadataPath, "nonexistent.json", SourceMaskRegionsPath);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("geometry primitive schema") && e.Contains("not found"));
    }

    [Fact]
    public void MissingSourceMaskRegionExtraction_ReturnsInvalid()
    {
        var r = DeadMtlWorldBuilderConnectedComponentExtractionBuilder.Build(
            SourcePng, MetadataPath, GeometryPrimitiveSchemaPath, "nonexistent.json");
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("source mask region extraction") && e.Contains("not found"));
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
        var r = BuildValid();
        Assert.Equal("pzmapforge.deadmtl.worldbuilder.connected-component-extraction.v1",
            r.Extraction.Format);
    }

    [Fact]
    public void ValidBuild_StatusFieldsCorrect()
    {
        var e = BuildValid().Extraction;
        Assert.Equal("CONNECTED_COMPONENT_EXTRACTION_CONTRACT_ONLY", e.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN",                           e.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",                              e.WriterStatus);
        Assert.Equal("NOT_EXECUTED",                                 e.GenerationStatus);
        Assert.Equal("CONNECTED_COMPONENTS_ONLY_NO_GEOMETRY_CREATED", e.GeometryStatus);
        Assert.Equal("CONNECTED_COMPONENTS_EXTRACTED",               e.ExtractionStatus);
        Assert.Equal("FOUR_WAY_PIXEL_CONNECTIVITY",                  e.ConnectivityStatus);
    }

    // -----------------------------------------------------------------------
    // Source / Connectivity Contract
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_SourceContractValues()
    {
        var sc = BuildValid().Extraction.SourceContract;
        Assert.Equal("map_00",                   sc.TileId);
        Assert.Equal(256,                        sc.SourcePngWidthPx);
        Assert.Equal(256,                        sc.SourcePngHeightPx);
        Assert.Equal(65536,                      sc.SourcePixelCount);
        Assert.Equal("1_PIXEL_EQUALS_1_WORLD_TILE", sc.AuthoringScale);
        Assert.Equal("TOP_LEFT",                 sc.PixelOrigin);
        Assert.Equal("EAST_POSITIVE",            sc.XAxis);
        Assert.Equal("SOUTH_POSITIVE",           sc.YAxis);
        Assert.Equal("SOURCE_PIXELS",            sc.CoordinateUnits);
        Assert.Equal("FOUR_WAY_NEIGHBOR_PIXELS", sc.ConnectivityRule);
        Assert.True(sc.BoundsArePixelBoundsNotGeometry);
        Assert.True(sc.ComponentsAreNotGeometry);
    }

    [Fact]
    public void ValidBuild_DiagonalConnectivityDisabled()
    {
        Assert.False(BuildValid().Extraction.SourceContract.DiagonalConnectivityEnabled);
    }

    [Fact]
    public void ValidBuild_ConnectivityRuleIsFourWay()
    {
        Assert.Equal("FOUR_WAY_NEIGHBOR_PIXELS",
            BuildValid().Extraction.SourceContract.ConnectivityRule);
    }

    // -----------------------------------------------------------------------
    // Global totals
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_ParentMaskRegionCount7()
    {
        Assert.Equal(7, BuildValid().Extraction.Totals.ParentMaskRegionCount);
    }

    [Fact]
    public void ValidBuild_ConnectedComponentCount45()
    {
        Assert.Equal(45, BuildValid().Extraction.Totals.ConnectedComponentCount);
    }

    [Fact]
    public void ValidBuild_KnownColorComponentCount45()
    {
        Assert.Equal(45, BuildValid().Extraction.Totals.KnownColorComponentCount);
    }

    [Fact]
    public void ValidBuild_UnknownColorComponentCount0()
    {
        Assert.Equal(0, BuildValid().Extraction.Totals.UnknownColorComponentCount);
    }

    [Fact]
    public void ValidBuild_ComponentPixelTotal65536()
    {
        Assert.Equal(65536, BuildValid().Extraction.Totals.ComponentPixelTotal);
    }

    // -----------------------------------------------------------------------
    // Component counts by color
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_ResidentialComponentCount22()
    {
        Assert.Equal(22, BuildValid().Extraction.Totals.ResidentialComponentCount);
    }

    [Fact]
    public void ValidBuild_MainRoadComponentCount1()
    {
        Assert.Equal(1, BuildValid().Extraction.Totals.MainRoadComponentCount);
    }

    [Fact]
    public void ValidBuild_GreenspaceComponentCount1()
    {
        Assert.Equal(1, BuildValid().Extraction.Totals.GreenspaceComponentCount);
    }

    [Fact]
    public void ValidBuild_BackAlleyComponentCount10()
    {
        Assert.Equal(10, BuildValid().Extraction.Totals.BackAlleyComponentCount);
    }

    [Fact]
    public void ValidBuild_CivicPlaceholderComponentCount2()
    {
        Assert.Equal(2, BuildValid().Extraction.Totals.CivicPlaceholderComponentCount);
    }

    [Fact]
    public void ValidBuild_CommercialComponentCount4()
    {
        Assert.Equal(4, BuildValid().Extraction.Totals.CommercialComponentCount);
    }

    [Fact]
    public void ValidBuild_IgnoreComponentCount5()
    {
        Assert.Equal(5, BuildValid().Extraction.Totals.IgnoreComponentCount);
    }

    // -----------------------------------------------------------------------
    // Pixel totals by color
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidBuild_ResidentialPixelTotal36740()
    {
        Assert.Equal(36740, BuildValid().Extraction.Totals.ResidentialPixelTotal);
    }

    [Fact]
    public void ValidBuild_MainRoadPixelTotal11378()
    {
        Assert.Equal(11378, BuildValid().Extraction.Totals.MainRoadPixelTotal);
    }

    [Fact]
    public void ValidBuild_GreenspacePixelTotal10898()
    {
        Assert.Equal(10898, BuildValid().Extraction.Totals.GreenspacePixelTotal);
    }

    [Fact]
    public void ValidBuild_BackAlleyPixelTotal3746()
    {
        Assert.Equal(3746, BuildValid().Extraction.Totals.BackAlleyPixelTotal);
    }

    [Fact]
    public void ValidBuild_CivicPlaceholderPixelTotal960()
    {
        Assert.Equal(960, BuildValid().Extraction.Totals.CivicPlaceholderPixelTotal);
    }

    [Fact]
    public void ValidBuild_CommercialPixelTotal926()
    {
        Assert.Equal(926, BuildValid().Extraction.Totals.CommercialPixelTotal);
    }

    [Fact]
    public void ValidBuild_IgnorePixelTotal888()
    {
        Assert.Equal(888, BuildValid().Extraction.Totals.IgnorePixelTotal);
    }

    // -----------------------------------------------------------------------
    // All component field values
    // -----------------------------------------------------------------------

    [Fact]
    public void AllComponents_PrimitiveTypeMaskRegion()
    {
        var components = BuildValid().Extraction.ConnectedComponents;
        Assert.All(components, c => Assert.Equal("MASK_REGION", c.PrimitiveType));
    }

    [Fact]
    public void AllComponents_StatusConnectedPixelComponentOnly()
    {
        var components = BuildValid().Extraction.ConnectedComponents;
        Assert.All(components, c =>
            Assert.Equal("CONNECTED_PIXEL_COMPONENT_ONLY", c.ComponentStatus));
    }

    [Fact]
    public void AllComponents_GeometryStatusConnectedComponentOnlyNoGeometry()
    {
        var components = BuildValid().Extraction.ConnectedComponents;
        Assert.All(components, c =>
            Assert.Equal("CONNECTED_COMPONENT_ONLY_NO_GEOMETRY_CREATED", c.GeometryStatus));
    }

    [Fact]
    public void AllComponents_BoundsWithin0To255()
    {
        var components = BuildValid().Extraction.ConnectedComponents;
        Assert.All(components, c =>
        {
            Assert.InRange(c.BoundsMinX, 0, 255);
            Assert.InRange(c.BoundsMinY, 0, 255);
            Assert.InRange(c.BoundsMaxX, 0, 255);
            Assert.InRange(c.BoundsMaxY, 0, 255);
        });
    }

    // -----------------------------------------------------------------------
    // Component IDs
    // -----------------------------------------------------------------------

    [Fact]
    public void AllComponentIds_AreUnique()
    {
        var ids = BuildValid().Extraction.ConnectedComponents.Select(c => c.ComponentId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void FirstComponentId_IsMap00Component0001()
    {
        var first = BuildValid().Extraction.ConnectedComponents.First();
        Assert.Equal("map_00_component_0001", first.ComponentId);
    }

    [Fact]
    public void LastComponentId_IsMap00Component0045()
    {
        var last = BuildValid().Extraction.ConnectedComponents.Last();
        Assert.Equal("map_00_component_0045", last.ComponentId);
    }

    // -----------------------------------------------------------------------
    // Specific component validations
    // -----------------------------------------------------------------------

    [Fact]
    public void MainRoad_OneComponentWithPixelCount11378()
    {
        var road = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.StreetClass == "MAIN_ROAD").ToList();
        Assert.Single(road);
        Assert.Equal(11378, road[0].PixelCount);
    }

    [Fact]
    public void Greenspace_OneComponentWithPixelCount10898()
    {
        var gs = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.ZoneType == "GREENSPACE").ToList();
        Assert.Single(gs);
        Assert.Equal(10898, gs[0].PixelCount);
    }

    [Fact]
    public void ResidentialComponents_SumTo36740()
    {
        var total = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.ZoneType == "RESIDENTIAL")
            .Sum(c => c.PixelCount);
        Assert.Equal(36740, total);
    }

    [Fact]
    public void BackAlleyComponents_SumTo3746()
    {
        var total = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.StreetClass == "BACK_ALLEY")
            .Sum(c => c.PixelCount);
        Assert.Equal(3746, total);
    }

    [Fact]
    public void CivicComponents_SumTo960()
    {
        var total = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.Role == "UNIQUE_PLACEHOLDER")
            .Sum(c => c.PixelCount);
        Assert.Equal(960, total);
    }

    [Fact]
    public void CommercialComponents_SumTo926()
    {
        var total = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.ZoneType == "COMMERCIAL")
            .Sum(c => c.PixelCount);
        Assert.Equal(926, total);
    }

    [Fact]
    public void IgnoreComponents_SumTo888()
    {
        var total = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.Role == "IGNORE")
            .Sum(c => c.PixelCount);
        Assert.Equal(888, total);
    }

    [Fact]
    public void IgnoreComponents_FutureReqIncludesNone()
    {
        var ignore = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.Role == "IGNORE").ToList();
        Assert.NotEmpty(ignore);
        Assert.All(ignore, c =>
            Assert.Contains("NONE", c.FutureGeometryRequirementIds));
    }

    [Fact]
    public void IgnoreComponents_FutureConsumerIncludesIgnore()
    {
        var ignore = BuildValid().Extraction.ConnectedComponents
            .Where(c => c.Role == "IGNORE").ToList();
        Assert.NotEmpty(ignore);
        Assert.All(ignore, c =>
            Assert.Contains("IGNORE", c.FutureConsumers));
    }

    // -----------------------------------------------------------------------
    // Zero counts
    // -----------------------------------------------------------------------

    [Fact]
    public void CreatedGeometryCount_IsZero()
    {
        Assert.Equal(0, BuildValid().Extraction.Totals.CreatedGeometryCount);
    }

    [Fact]
    public void WriterReadyComponentCount_IsZero()
    {
        Assert.Equal(0, BuildValid().Extraction.Totals.WriterReadyComponentCount);
    }

    [Fact]
    public void RuntimeValidatedComponentCount_IsZero()
    {
        Assert.Equal(0, BuildValid().Extraction.Totals.RuntimeValidatedComponentCount);
    }

    // -----------------------------------------------------------------------
    // Validation rules
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidationRules_NoDiagonalConnectivity()
    {
        Assert.True(BuildValid().Extraction.ValidationRules.NoDiagonalConnectivity);
    }

    [Fact]
    public void ValidationRules_NoRuntimeClaim()
    {
        Assert.True(BuildValid().Extraction.ValidationRules.NoRuntimeClaimFromComponentExtraction);
    }

    [Fact]
    public void ValidationRules_NoWriterClaim()
    {
        Assert.True(BuildValid().Extraction.ValidationRules.NoWriterClaimFromComponentExtraction);
    }

    [Fact]
    public void ValidationRules_NoMaterializationClaim()
    {
        Assert.True(BuildValid().Extraction.ValidationRules.NoMaterializationFromComponentExtraction);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void ClaimBoundary_AllFalse()
    {
        var cb = BuildValid().Extraction.ClaimBoundary;
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
        var md = DeadMtlWorldBuilderConnectedComponentExtractionBuilder.RenderMarkdown(r.Extraction);
        Assert.Contains("## Input Chain",                        md, StringComparison.Ordinal);
        Assert.Contains("## Source PNG / Connectivity Contract", md, StringComparison.Ordinal);
        Assert.Contains("## Connected Component Records",        md, StringComparison.Ordinal);
        Assert.Contains("## Component Count Totals",             md, StringComparison.Ordinal);
        Assert.Contains("## Pixel Total Validation",             md, StringComparison.Ordinal);
        Assert.Contains("## Parent Mask Region Mapping",         md, StringComparison.Ordinal);
        Assert.Contains("## Future Geometry Requirement Mapping", md, StringComparison.Ordinal);
        Assert.Contains("## Why This Still Cannot Execute",      md, StringComparison.Ordinal);
        Assert.Contains("## Claim Boundary",                     md, StringComparison.Ordinal);
        Assert.Contains("## Verdict",                            md, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_ContainsVerdict()
    {
        var r  = BuildValid();
        var md = DeadMtlWorldBuilderConnectedComponentExtractionBuilder.RenderMarkdown(r.Extraction);
        Assert.Contains("MAP25K_WORLDBUILDER_CONNECTED_COMPONENT_EXTRACTION_CONTRACT_COMPLETE",
            md, StringComparison.Ordinal);
    }

    [Fact]
    public void Csv_HeaderCorrect()
    {
        var r   = BuildValid();
        var csv = DeadMtlWorldBuilderConnectedComponentExtractionBuilder.RenderCsv(r.Extraction);
        Assert.StartsWith(
            "component_order,component_id,parent_region_order,parent_source_color," +
            "role,zone_type,street_class,local_component_index,pixel_count," +
            "bounds_min_x,bounds_min_y,bounds_max_x,bounds_max_y,bounds_width,bounds_height," +
            "bounds_status,connectivity_rule,primitive_type,component_status,geometry_status," +
            "source_confidence,future_geometry_requirement_ids,future_consumers,notes",
            csv, StringComparison.Ordinal);
    }

    [Fact]
    public void Summary_ContainsVerdict()
    {
        var r   = BuildValid();
        var sum = DeadMtlWorldBuilderConnectedComponentExtractionBuilder.RenderSummary(r);
        Assert.Contains("MAP25K_WORLDBUILDER_CONNECTED_COMPONENT_EXTRACTION_CONTRACT_COMPLETE",
            sum, StringComparison.Ordinal);
    }
}
