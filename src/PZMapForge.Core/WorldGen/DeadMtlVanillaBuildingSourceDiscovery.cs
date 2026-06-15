using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlVanillaBuildingSourceDiscoveryScanRoot
{
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("exists")]
    public bool Exists { get; init; }
}

public sealed class DeadMtlVanillaBuildingSourceDiscoveryCounts
{
    [JsonPropertyName("lotheader_count")]
    public int LotheaderCount { get; init; }

    [JsonPropertyName("lotpack_count")]
    public int LotpackCount { get; init; }

    [JsonPropertyName("tmx_count")]
    public int TmxCount { get; init; }

    [JsonPropertyName("tbx_count")]
    public int TbxCount { get; init; }

    [JsonPropertyName("building_count")]
    public int BuildingCount { get; init; }

    [JsonPropertyName("map_info_present")]
    public bool MapInfoPresent { get; init; }

    [JsonPropertyName("mod_info_present")]
    public bool ModInfoPresent { get; init; }

    [JsonPropertyName("objects_lua_present")]
    public bool ObjectsLuaPresent { get; init; }

    [JsonPropertyName("spawnpoints_lua_present")]
    public bool SpawnpointsLuaPresent { get; init; }

    [JsonPropertyName("spawnregions_lua_present")]
    public bool SpawnregionsLuaPresent { get; init; }
}

public sealed class DeadMtlVanillaBuildingSourceDiscoveryGroupClaim
{
    [JsonPropertyName("editable_template_source")]
    public bool EditableTemplateSource { get; init; }

    [JsonPropertyName("compiled_map_source")]
    public bool CompiledMapSource { get; init; }

    [JsonPropertyName("vanilla_source")]
    public bool VanillaSource { get; init; }
}

public sealed class DeadMtlVanillaBuildingSourceDiscoveryMapFolderGroup
{
    [JsonPropertyName("root")]
    public string Root { get; init; } = string.Empty;

    [JsonPropertyName("map_folder")]
    public string MapFolder { get; init; } = string.Empty;

    [JsonPropertyName("relative_path")]
    public string RelativePath { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("counts")]
    public DeadMtlVanillaBuildingSourceDiscoveryCounts Counts { get; init; } = new();

    [JsonPropertyName("sample_files")]
    public List<string> SampleFiles { get; init; } = new();

    [JsonPropertyName("claim")]
    public DeadMtlVanillaBuildingSourceDiscoveryGroupClaim Claim { get; init; } = new();
}

public sealed class DeadMtlVanillaBuildingSourceDiscoveryFileTypeCounts
{
    [JsonPropertyName("lotheader")]
    public int Lotheader { get; init; }

    [JsonPropertyName("lotpack")]
    public int Lotpack { get; init; }

    [JsonPropertyName("tmx")]
    public int Tmx { get; init; }

    [JsonPropertyName("tbx")]
    public int Tbx { get; init; }

    [JsonPropertyName("building")]
    public int Building { get; init; }
}

public sealed class DeadMtlVanillaBuildingSourceDiscoveryTotals
{
    [JsonPropertyName("root_count")]
    public int RootCount { get; init; }

    [JsonPropertyName("map_folder_group_count")]
    public int MapFolderGroupCount { get; init; }

    [JsonPropertyName("lotheader_count")]
    public int LotheaderCount { get; init; }

    [JsonPropertyName("lotpack_count")]
    public int LotpackCount { get; init; }

    [JsonPropertyName("tmx_count")]
    public int TmxCount { get; init; }

    [JsonPropertyName("tbx_count")]
    public int TbxCount { get; init; }

    [JsonPropertyName("building_count")]
    public int BuildingCount { get; init; }

    [JsonPropertyName("editable_source_candidate_count")]
    public int EditableSourceCandidateCount { get; init; }

    [JsonPropertyName("compiled_map_candidate_count")]
    public int CompiledMapCandidateCount { get; init; }

    [JsonPropertyName("vanilla_compiled_map_candidate_count")]
    public int VanillaCompiledMapCandidateCount { get; init; }

    [JsonPropertyName("workspace_mod_map_candidate_count")]
    public int WorkspaceModMapCandidateCount { get; init; }

    [JsonPropertyName("tool_example_candidate_count")]
    public int ToolExampleCandidateCount { get; init; }
}

public sealed class DeadMtlVanillaBuildingSourceDiscoveryClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; init; } = false;

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; init; } = false;

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; init; } = false;

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; init; } = false;

    [JsonPropertyName("writer_ready_claim")]
    public bool WriterReadyClaim { get; init; } = false;

    [JsonPropertyName("building_extraction_claim")]
    public bool BuildingExtractionClaim { get; init; } = false;

    [JsonPropertyName("editable_vanilla_catalogue_claim")]
    public bool EditableVanillaCatalogueClaim { get; init; } = false;
}

public sealed class DeadMtlVanillaBuildingSourceDiscovery
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.vanilla-building-source-discovery.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "VANILLA_BUILDING_SOURCE_DISCOVERY_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("scan_roots")]
    public List<DeadMtlVanillaBuildingSourceDiscoveryScanRoot> ScanRoots { get; init; } = new();

    [JsonPropertyName("file_type_counts")]
    public DeadMtlVanillaBuildingSourceDiscoveryFileTypeCounts FileTypeCounts { get; init; } = new();

    [JsonPropertyName("map_folder_groups")]
    public List<DeadMtlVanillaBuildingSourceDiscoveryMapFolderGroup> MapFolderGroups { get; init; } = new();

    [JsonPropertyName("editable_source_candidates")]
    public List<string> EditableSourceCandidates { get; init; } = new();

    [JsonPropertyName("compiled_map_candidates")]
    public List<string> CompiledMapCandidates { get; init; } = new();

    [JsonPropertyName("tool_example_candidates")]
    public List<string> ToolExampleCandidates { get; init; } = new();

    [JsonPropertyName("workspace_mod_map_candidates")]
    public List<string> WorkspaceModMapCandidates { get; init; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlVanillaBuildingSourceDiscoveryTotals Totals { get; init; } = new();

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;

    [JsonPropertyName("claim_boundary")]
    public DeadMtlVanillaBuildingSourceDiscoveryClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class DeadMtlVanillaBuildingSourceDiscoveryResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public DeadMtlVanillaBuildingSourceDiscovery? Discovery { get; set; }
}
