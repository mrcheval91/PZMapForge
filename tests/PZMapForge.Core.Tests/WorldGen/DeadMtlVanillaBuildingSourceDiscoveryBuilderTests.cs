using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlVanillaBuildingSourceDiscoveryBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-vanilla-disc", Path.GetRandomFileName());

    public DeadMtlVanillaBuildingSourceDiscoveryBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private string MakeRoot(string label)
    {
        var path = Path.Combine(_tempDir, label);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Touch(string dir, string filename)
    {
        var path = Path.Combine(dir, filename);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private static (string Label, string Path, string Kind)[] SingleRoot(string path, string kind = "VANILLA_COMPILED_MAP") =>
        new[] { ("test_root", path, kind) };

    // -----------------------------------------------------------------------
    // Empty / missing roots
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsValid_WhenNoRoots()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            Array.Empty<(string, string, string)>());
        Assert.True(result.IsValid);
        Assert.Equal(0, result.Discovery!.Totals.MapFolderGroupCount);
    }

    [Fact]
    public void Build_ReturnsValid_WhenScanRootDoesNotExist()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            new[] { ("missing", Path.Combine(_tempDir, "no_such_dir"), "VANILLA_COMPILED_MAP") });
        Assert.True(result.IsValid);
        Assert.Equal(0, result.Discovery!.Totals.MapFolderGroupCount);
    }

    [Fact]
    public void Build_ScanRoots_MarkedNotExists_WhenPathAbsent()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            new[] { ("ghost", Path.Combine(_tempDir, "ghost"), "VANILLA_COMPILED_MAP") });
        Assert.True(result.IsValid);
        Assert.False(result.Discovery!.ScanRoots[0].Exists);
    }

    [Fact]
    public void Build_ScanRoots_MarkedExists_WhenPathPresent()
    {
        var root   = MakeRoot("existing");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.True(result.Discovery!.ScanRoots[0].Exists);
    }

    // -----------------------------------------------------------------------
    // Qualifying files
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_FindsGroup_WhenDirHasLotheaderFile()
    {
        var root = MakeRoot("pz");
        Touch(root, "0_0.lotheader");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.True(result.IsValid);
        Assert.Equal(1, result.Discovery!.Totals.MapFolderGroupCount);
        Assert.Equal(1, result.Discovery.Totals.LotheaderCount);
    }

    [Fact]
    public void Build_FindsGroup_WhenDirHasLotpackFile()
    {
        var root = MakeRoot("pz");
        Touch(root, "0_0.lotpack");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal(1, result.Discovery!.Totals.LotpackCount);
    }

    [Fact]
    public void Build_FindsGroup_WhenDirHasTmxFile()
    {
        var root = MakeRoot("pz");
        Touch(root, "map.tmx");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal(1, result.Discovery!.Totals.TmxCount);
    }

    [Fact]
    public void Build_FindsGroup_WhenDirHasTbxFile()
    {
        var root = MakeRoot("pz");
        Touch(root, "house.tbx");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal(1, result.Discovery!.Totals.TbxCount);
        Assert.Equal(1, result.Discovery.Totals.MapFolderGroupCount);
    }

    [Fact]
    public void Build_FindsGroup_WhenDirHasBuildingFile()
    {
        var root = MakeRoot("pz");
        Touch(root, "store.building");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal(1, result.Discovery!.Totals.BuildingCount);
    }

    [Fact]
    public void Build_FindsGroup_WhenDirHasMapInfo()
    {
        var root = MakeRoot("pz");
        Touch(root, "map.info");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal(1, result.Discovery!.Totals.MapFolderGroupCount);
        Assert.True(result.Discovery.MapFolderGroups[0].Counts.MapInfoPresent);
    }

    [Fact]
    public void Build_DoesNotFindGroup_WhenDirHasOnlyUnknownFiles()
    {
        var root = MakeRoot("pz");
        Touch(root, "readme.txt");
        Touch(root, "config.xml");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal(0, result.Discovery!.Totals.MapFolderGroupCount);
    }

    // -----------------------------------------------------------------------
    // Subdirectory discovery
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_FindsGroup_InSubdirectory()
    {
        var root   = MakeRoot("pz");
        var subdir = Path.Combine(root, "media", "maps", "Muldraugh_KY");
        Directory.CreateDirectory(subdir);
        Touch(subdir, "0_0.lotheader");

        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal(1, result.Discovery!.Totals.MapFolderGroupCount);
    }

    // -----------------------------------------------------------------------
    // Kind assignment
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Kind_IsVanillaCompiledMap_WhenKindProvided()
    {
        var root = MakeRoot("pz");
        Touch(root, "0_0.lotheader");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            new[] { ("pz_install", root, "VANILLA_COMPILED_MAP") });
        Assert.Equal("VANILLA_COMPILED_MAP", result.Discovery!.MapFolderGroups[0].Kind);
    }

    [Fact]
    public void Build_Kind_IsModdingToolExample_WhenKindProvided()
    {
        var root = MakeRoot("tools");
        Touch(root, "example.tmx");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            new[] { ("modding_tools", root, "MODDING_TOOL_EXAMPLE") });
        Assert.Equal("MODDING_TOOL_EXAMPLE", result.Discovery!.MapFolderGroups[0].Kind);
    }

    // -----------------------------------------------------------------------
    // Claim: editable_template_source
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_EditableTemplateSource_True_WhenTbxFilePresent()
    {
        var root = MakeRoot("ws");
        Touch(root, "house.tbx");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root, "WORKSPACE_COMPILED_MAP"));
        Assert.True(result.Discovery!.MapFolderGroups[0].Claim.EditableTemplateSource);
    }

    [Fact]
    public void Build_EditableTemplateSource_True_WhenBuildingFilePresent()
    {
        var root = MakeRoot("ws");
        Touch(root, "store.building");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root, "WORKSPACE_COMPILED_MAP"));
        Assert.True(result.Discovery!.MapFolderGroups[0].Claim.EditableTemplateSource);
    }

    [Fact]
    public void Build_EditableTemplateSource_False_WhenOnlyCompiledFilesPresent()
    {
        var root = MakeRoot("pz");
        Touch(root, "0_0.lotheader");
        Touch(root, "0_0.lotpack");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.False(result.Discovery!.MapFolderGroups[0].Claim.EditableTemplateSource);
    }

    // -----------------------------------------------------------------------
    // Claim: compiled_map_source
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CompiledMapSource_True_WhenLotheaderPresent()
    {
        var root = MakeRoot("pz");
        Touch(root, "0_0.lotheader");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.True(result.Discovery!.MapFolderGroups[0].Claim.CompiledMapSource);
    }

    [Fact]
    public void Build_CompiledMapSource_False_WhenOnlyTbxPresent()
    {
        var root = MakeRoot("ws");
        Touch(root, "house.tbx");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.False(result.Discovery!.MapFolderGroups[0].Claim.CompiledMapSource);
    }

    // -----------------------------------------------------------------------
    // Sample files
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_SampleFiles_OnlyBinaryExtensions()
    {
        var root = MakeRoot("pz");
        Touch(root, "0_0.lotheader");
        Touch(root, "readme.txt");
        Touch(root, "map.info");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        var sample = result.Discovery!.MapFolderGroups[0].SampleFiles;
        Assert.All(sample, f => Assert.True(
            f.EndsWith(".lotheader", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".lotpack",   StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".tmx",       StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".tbx",       StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".building",  StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Build_SampleFiles_MaxTen()
    {
        var root = MakeRoot("pz");
        for (var i = 0; i < 15; i++) Touch(root, $"{i}_0.lotheader");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.True(result.Discovery!.MapFolderGroups[0].SampleFiles.Count <= 10);
    }

    // -----------------------------------------------------------------------
    // Totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Totals_CountsMultipleGroups()
    {
        var root   = MakeRoot("pz");
        var subA   = Path.Combine(root, "mapA"); Directory.CreateDirectory(subA);
        var subB   = Path.Combine(root, "mapB"); Directory.CreateDirectory(subB);
        Touch(subA, "0_0.lotheader");
        Touch(subB, "0_0.lotheader");
        Touch(subB, "0_0.lotpack");

        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.True(result.IsValid);
        Assert.Equal(2, result.Discovery!.Totals.MapFolderGroupCount);
        Assert.Equal(2, result.Discovery.Totals.LotheaderCount);
        Assert.Equal(1, result.Discovery.Totals.LotpackCount);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var root   = MakeRoot("pz");
        Touch(root, "0_0.lotheader");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        var cb = result.Discovery!.ClaimBoundary;
        Assert.False(cb.WritesLotpack);
        Assert.False(cb.WritesWorldgenLua);
        Assert.False(cb.RuntimeProven);
        Assert.False(cb.PublicPlayableClaim);
        Assert.False(cb.WriterReadyClaim);
        Assert.False(cb.BuildingExtractionClaim);
        Assert.False(cb.EditableVanillaCatalogueClaim);
    }

    // -----------------------------------------------------------------------
    // Recommendation
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Recommendation_NoBuildingSources_WhenNoGroups()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            Array.Empty<(string, string, string)>());
        Assert.Equal("NO_BUILDING_SOURCES_FOUND", result.Discovery!.Recommendation);
    }

    [Fact]
    public void Build_Recommendation_NoEditableCatalogue_WhenOnlyCompiledMapGroupsFound()
    {
        var root = MakeRoot("pz");
        Touch(root, "0_0.lotheader");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal(
            "NO_EDITABLE_VANILLA_CATALOGUE_FOUND_USE_COMPILED_MAP_EXTRACTION_AND_MANUAL_DEADMTL_CATALOGUE",
            result.Discovery!.Recommendation);
    }

    [Fact]
    public void Build_Recommendation_EditableFound_WhenTbxGroupFound()
    {
        var root = MakeRoot("ws");
        Touch(root, "house.tbx");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        Assert.Equal("EDITABLE_TEMPLATE_SOURCES_FOUND", result.Discovery!.Recommendation);
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsMap24ATitle()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            Array.Empty<(string, string, string)>());
        var md = DeadMtlVanillaBuildingSourceDiscoveryBuilder.RenderMarkdown(result.Discovery!);
        Assert.Contains("MAP-24A", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsWarning()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            Array.Empty<(string, string, string)>());
        var md = DeadMtlVanillaBuildingSourceDiscoveryBuilder.RenderMarkdown(result.Discovery!);
        Assert.True(
            md.Contains("WARNING", StringComparison.OrdinalIgnoreCase) ||
            md.Contains("Discovery only", StringComparison.OrdinalIgnoreCase),
            "markdown should contain a warning");
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            Array.Empty<(string, string, string)>());
        var md = DeadMtlVanillaBuildingSourceDiscoveryBuilder.RenderMarkdown(result.Discovery!);
        Assert.Contains("MAP24A_VANILLA_BUILDING_SOURCE_DISCOVERY_COMPLETE",
            md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeader()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            Array.Empty<(string, string, string)>());
        var csv = DeadMtlVanillaBuildingSourceDiscoveryBuilder.RenderCsv(result.Discovery!);
        Assert.Contains(
            "kind,root,map_folder,relative_path,lotheader_count,lotpack_count,tmx_count,tbx_count,building_count,map_info_present,mod_info_present,objects_lua_present,spawnpoints_lua_present,spawnregions_lua_present,editable_template_source,compiled_map_source,vanilla_source,sample_files",
            csv, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderCsv_ContainsGroupRow_WhenGroupFound()
    {
        var root = MakeRoot("pz");
        Touch(root, "0_0.lotheader");
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(SingleRoot(root));
        var csv    = DeadMtlVanillaBuildingSourceDiscoveryBuilder.RenderCsv(result.Discovery!);
        Assert.Contains("VANILLA_COMPILED_MAP", csv, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Format / status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Discovery_HasCorrectFormat()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            Array.Empty<(string, string, string)>());
        Assert.Equal(
            "pzmapforge.deadmtl.vanilla-building-source-discovery.v1",
            result.Discovery!.Format);
    }

    [Fact]
    public void Build_Discovery_HasNotRuntimeProvenStatus()
    {
        var result = DeadMtlVanillaBuildingSourceDiscoveryBuilder.Build(
            Array.Empty<(string, string, string)>());
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Discovery!.RuntimeStatus);
    }
}
