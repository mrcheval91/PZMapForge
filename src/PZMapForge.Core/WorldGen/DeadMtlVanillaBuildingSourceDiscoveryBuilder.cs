using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlVanillaBuildingSourceDiscoveryBuilder
{
    public static readonly (string Label, string Path, string Kind)[] DefaultScanRoots =
    {
        ("pz_install",
         @"D:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid",
         "VANILLA_COMPILED_MAP"),
        ("modding_tools",
         @"D:\Program Files (x86)\Steam\steamapps\common\Project Zomboid Modding Tools",
         "MODDING_TOOL_EXAMPLE"),
        ("user_zomboid",
         @"C:\Users\Palmacede\Zomboid",
         "USER_MOD_COMPILED_MAP"),
        ("workspace",
         @"E:\Omni\Zomboid",
         "WORKSPACE_COMPILED_MAP"),
    };

    public static DeadMtlVanillaBuildingSourceDiscoveryResult Build(
        (string Label, string Path, string Kind)[]? scanRoots = null)
    {
        var result = new DeadMtlVanillaBuildingSourceDiscoveryResult();
        var roots  = scanRoots ?? DefaultScanRoots;

        var scanRootModels = roots.Select(r => new DeadMtlVanillaBuildingSourceDiscoveryScanRoot
        {
            Label  = r.Label,
            Path   = r.Path,
            Exists = Directory.Exists(r.Path),
        }).ToList();

        var groups = new List<DeadMtlVanillaBuildingSourceDiscoveryMapFolderGroup>();

        foreach (var (_, rootPath, kind) in roots)
        {
            if (!Directory.Exists(rootPath)) continue;

            foreach (var dir in SafeEnumerateAllDirs(rootPath))
            {
                var (qualifies, counts) = InspectDirectory(dir);
                if (!qualifies) continue;

                var relPath    = Path.GetRelativePath(rootPath, dir);
                var sampleFiles = CollectSampleFiles(dir);
                var claim      = BuildGroupClaim(kind, counts);

                groups.Add(new DeadMtlVanillaBuildingSourceDiscoveryMapFolderGroup
                {
                    Root         = rootPath,
                    MapFolder    = dir,
                    RelativePath = relPath,
                    Kind         = kind,
                    Counts       = counts,
                    SampleFiles  = sampleFiles,
                    Claim        = claim,
                });
            }
        }

        var editableCandidates     = groups.Where(g => g.Claim.EditableTemplateSource).Select(g => g.RelativePath).ToList();
        var compiledMapCandidates  = groups.Where(g => g.Claim.CompiledMapSource).Select(g => g.RelativePath).ToList();
        var toolExampleCandidates  = groups.Where(g => g.Kind == "MODDING_TOOL_EXAMPLE").Select(g => g.RelativePath).ToList();
        var workspaceModCandidates = groups.Where(g => g.Kind is "WORKSPACE_COMPILED_MAP" or "USER_MOD_COMPILED_MAP").Select(g => g.RelativePath).ToList();

        var fileTypeCounts = new DeadMtlVanillaBuildingSourceDiscoveryFileTypeCounts
        {
            Lotheader = groups.Sum(g => g.Counts.LotheaderCount),
            Lotpack   = groups.Sum(g => g.Counts.LotpackCount),
            Tmx       = groups.Sum(g => g.Counts.TmxCount),
            Tbx       = groups.Sum(g => g.Counts.TbxCount),
            Building  = groups.Sum(g => g.Counts.BuildingCount),
        };

        var totals = new DeadMtlVanillaBuildingSourceDiscoveryTotals
        {
            RootCount                      = roots.Length,
            MapFolderGroupCount            = groups.Count,
            LotheaderCount                 = fileTypeCounts.Lotheader,
            LotpackCount                   = fileTypeCounts.Lotpack,
            TmxCount                       = fileTypeCounts.Tmx,
            TbxCount                       = fileTypeCounts.Tbx,
            BuildingCount                  = fileTypeCounts.Building,
            EditableSourceCandidateCount   = editableCandidates.Count,
            CompiledMapCandidateCount      = compiledMapCandidates.Count,
            VanillaCompiledMapCandidateCount = groups.Count(g => g.Kind == "VANILLA_COMPILED_MAP"),
            WorkspaceModMapCandidateCount  = workspaceModCandidates.Count,
            ToolExampleCandidateCount      = toolExampleCandidates.Count,
        };

        var recommendation = editableCandidates.Count > 0
            ? "EDITABLE_TEMPLATE_SOURCES_FOUND"
            : groups.Count > 0
                ? "NO_EDITABLE_VANILLA_CATALOGUE_FOUND_USE_COMPILED_MAP_EXTRACTION_AND_MANUAL_DEADMTL_CATALOGUE"
                : "NO_BUILDING_SOURCES_FOUND";

        var discovery = new DeadMtlVanillaBuildingSourceDiscovery
        {
            ScanRoots                = scanRootModels,
            FileTypeCounts           = fileTypeCounts,
            MapFolderGroups          = groups,
            EditableSourceCandidates = editableCandidates,
            CompiledMapCandidates    = compiledMapCandidates,
            ToolExampleCandidates    = toolExampleCandidates,
            WorkspaceModMapCandidates = workspaceModCandidates,
            Totals                   = totals,
            Recommendation           = recommendation,
            ClaimBoundary            = new DeadMtlVanillaBuildingSourceDiscoveryClaimBoundary(),
        };

        result.IsValid   = true;
        result.Discovery = discovery;
        return result;
    }

    public static string RenderMarkdown(DeadMtlVanillaBuildingSourceDiscovery d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-24A: DeadMTL Vanilla Building Source Discovery");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Discovery only. No building extraction is claimed.");
        sb.AppendLine("> No editable vanilla catalogue is claimed. No runtime proof. Not writer-ready.");
        sb.AppendLine();
        sb.AppendLine($"**status:** {d.Status}");
        sb.AppendLine($"**runtime_status:** {d.RuntimeStatus}");
        sb.AppendLine($"**writer_status:** {d.WriterStatus}");
        sb.AppendLine();
        sb.AppendLine("## Scan Roots");
        sb.AppendLine();
        sb.AppendLine("| label | path | exists |");
        sb.AppendLine("|-------|------|--------|");
        foreach (var r in d.ScanRoots)
            sb.AppendLine($"| {r.Label} | {r.Path} | {r.Exists} |");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"- map_folder_group_count: {d.Totals.MapFolderGroupCount}");
        sb.AppendLine($"- lotheader_count: {d.Totals.LotheaderCount}");
        sb.AppendLine($"- lotpack_count: {d.Totals.LotpackCount}");
        sb.AppendLine($"- tmx_count: {d.Totals.TmxCount}");
        sb.AppendLine($"- tbx_count: {d.Totals.TbxCount}");
        sb.AppendLine($"- building_count: {d.Totals.BuildingCount}");
        sb.AppendLine($"- editable_source_candidate_count: {d.Totals.EditableSourceCandidateCount}");
        sb.AppendLine($"- compiled_map_candidate_count: {d.Totals.CompiledMapCandidateCount}");
        sb.AppendLine($"- vanilla_compiled_map_candidate_count: {d.Totals.VanillaCompiledMapCandidateCount}");
        sb.AppendLine($"- workspace_mod_map_candidate_count: {d.Totals.WorkspaceModMapCandidateCount}");
        sb.AppendLine($"- tool_example_candidate_count: {d.Totals.ToolExampleCandidateCount}");
        sb.AppendLine();
        sb.AppendLine("## Recommendation");
        sb.AppendLine();
        sb.AppendLine($"`{d.Recommendation}`");
        sb.AppendLine();
        if (d.MapFolderGroups.Count > 0)
        {
            sb.AppendLine("## Map Folder Groups");
            sb.AppendLine();
            sb.AppendLine("| kind | relative_path | lotheader | lotpack | tmx | tbx | building | map.info | editable | compiled | sample |");
            sb.AppendLine("|------|---------------|-----------|---------|-----|-----|----------|----------|----------|----------|--------|");
            foreach (var g in d.MapFolderGroups)
            {
                var sample = g.SampleFiles.Count > 0 ? string.Join("; ", g.SampleFiles.Take(3)) : "";
                sb.AppendLine($"| {g.Kind} | {g.RelativePath} | {g.Counts.LotheaderCount} | {g.Counts.LotpackCount} | {g.Counts.TmxCount} | {g.Counts.TbxCount} | {g.Counts.BuildingCount} | {g.Counts.MapInfoPresent} | {g.Claim.EditableTemplateSource} | {g.Claim.CompiledMapSource} | {sample} |");
            }
            sb.AppendLine();
        }
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine($"- writes_lotpack: {d.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"- writes_worldgen_lua: {d.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"- runtime_proven: {d.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"- public_playable_claim: {d.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"- writer_ready_claim: {d.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"- building_extraction_claim: {d.ClaimBoundary.BuildingExtractionClaim}");
        sb.AppendLine($"- editable_vanilla_catalogue_claim: {d.ClaimBoundary.EditableVanillaCatalogueClaim}");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP24A_VANILLA_BUILDING_SOURCE_DISCOVERY_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlVanillaBuildingSourceDiscovery d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("kind,root,map_folder,relative_path,lotheader_count,lotpack_count,tmx_count,tbx_count,building_count,map_info_present,mod_info_present,objects_lua_present,spawnpoints_lua_present,spawnregions_lua_present,editable_template_source,compiled_map_source,vanilla_source,sample_files");
        foreach (var g in d.MapFolderGroups)
        {
            var sampleCell = string.Join("|", g.SampleFiles);
            sb.AppendLine(
                $"{g.Kind},{CsvEscape(g.Root)},{CsvEscape(g.MapFolder)},{CsvEscape(g.RelativePath)}," +
                $"{g.Counts.LotheaderCount},{g.Counts.LotpackCount}," +
                $"{g.Counts.TmxCount},{g.Counts.TbxCount},{g.Counts.BuildingCount}," +
                $"{g.Counts.MapInfoPresent},{g.Counts.ModInfoPresent}," +
                $"{g.Counts.ObjectsLuaPresent},{g.Counts.SpawnpointsLuaPresent},{g.Counts.SpawnregionsLuaPresent}," +
                $"{g.Claim.EditableTemplateSource},{g.Claim.CompiledMapSource},{g.Claim.VanillaSource},{CsvEscape(sampleCell)}");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlVanillaBuildingSourceDiscovery d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-24A: DeadMTL Vanilla Building Source Discovery");
        sb.AppendLine();
        sb.AppendLine($"map_folder_group_count:             {d.Totals.MapFolderGroupCount}");
        sb.AppendLine($"lotheader_count:                    {d.Totals.LotheaderCount}");
        sb.AppendLine($"lotpack_count:                      {d.Totals.LotpackCount}");
        sb.AppendLine($"tmx_count:                          {d.Totals.TmxCount}");
        sb.AppendLine($"tbx_count:                          {d.Totals.TbxCount}");
        sb.AppendLine($"building_count:                     {d.Totals.BuildingCount}");
        sb.AppendLine($"editable_source_candidate_count:    {d.Totals.EditableSourceCandidateCount}");
        sb.AppendLine($"compiled_map_candidate_count:       {d.Totals.CompiledMapCandidateCount}");
        sb.AppendLine($"vanilla_compiled_map_count:         {d.Totals.VanillaCompiledMapCandidateCount}");
        sb.AppendLine($"workspace_mod_map_count:            {d.Totals.WorkspaceModMapCandidateCount}");
        sb.AppendLine($"tool_example_count:                 {d.Totals.ToolExampleCandidateCount}");
        sb.AppendLine();
        sb.AppendLine("CLAIM BOUNDARY");
        sb.AppendLine($"  writes_lotpack:                   {d.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"  writes_worldgen_lua:              {d.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"  runtime_proven:                   {d.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"  public_playable_claim:            {d.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"  writer_ready_claim:               {d.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"  building_extraction_claim:        {d.ClaimBoundary.BuildingExtractionClaim}");
        sb.AppendLine($"  editable_vanilla_catalogue_claim: {d.ClaimBoundary.EditableVanillaCatalogueClaim}");
        sb.AppendLine();
        sb.AppendLine($"RECOMMENDATION: {d.Recommendation}");
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP24A_VANILLA_BUILDING_SOURCE_DISCOVERY_COMPLETE");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Internals
    // -----------------------------------------------------------------------

    private static IEnumerable<string> SafeEnumerateAllDirs(string root)
    {
        yield return root;
        var queue = new Queue<string>();
        IEnumerable<string> first;
        try { first = Directory.EnumerateDirectories(root); }
        catch { yield break; }
        foreach (var d in first) queue.Enqueue(d);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;
            IEnumerable<string> subdirs;
            try { subdirs = Directory.EnumerateDirectories(current); }
            catch { continue; }
            foreach (var s in subdirs) queue.Enqueue(s);
        }
    }

    private static (bool Qualifies, DeadMtlVanillaBuildingSourceDiscoveryCounts Counts) InspectDirectory(string dir)
    {
        List<string> fileNames;
        try
        {
            fileNames = Directory.EnumerateFiles(dir)
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToList();
        }
        catch { return (false, new()); }

        int lotheader = fileNames.Count(n => n.EndsWith(".lotheader", StringComparison.OrdinalIgnoreCase));
        int lotpack   = fileNames.Count(n => n.EndsWith(".lotpack",   StringComparison.OrdinalIgnoreCase));
        int tmx       = fileNames.Count(n => n.EndsWith(".tmx",       StringComparison.OrdinalIgnoreCase));
        int tbx       = fileNames.Count(n => n.EndsWith(".tbx",       StringComparison.OrdinalIgnoreCase));
        int building  = fileNames.Count(n => n.EndsWith(".building",  StringComparison.OrdinalIgnoreCase));

        bool mapInfo      = fileNames.Any(n => n.Equals("map.info",         StringComparison.OrdinalIgnoreCase));
        bool modInfo      = fileNames.Any(n => n.Equals("mod.info",         StringComparison.OrdinalIgnoreCase));
        bool objectsLua   = fileNames.Any(n => n.Equals("objects.lua",      StringComparison.OrdinalIgnoreCase));
        bool spawnpoints  = fileNames.Any(n => n.Equals("spawnpoints.lua",  StringComparison.OrdinalIgnoreCase));
        bool spawnregions = fileNames.Any(n => n.Equals("spawnregions.lua", StringComparison.OrdinalIgnoreCase));

        bool qualifies = lotheader > 0 || lotpack > 0 || tmx > 0 || tbx > 0 || building > 0 || mapInfo;

        var counts = new DeadMtlVanillaBuildingSourceDiscoveryCounts
        {
            LotheaderCount         = lotheader,
            LotpackCount           = lotpack,
            TmxCount               = tmx,
            TbxCount               = tbx,
            BuildingCount          = building,
            MapInfoPresent         = mapInfo,
            ModInfoPresent         = modInfo,
            ObjectsLuaPresent      = objectsLua,
            SpawnpointsLuaPresent  = spawnpoints,
            SpawnregionsLuaPresent = spawnregions,
        };

        return (qualifies, counts);
    }

    private static List<string> CollectSampleFiles(string dir)
    {
        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(dir); }
        catch { return new(); }

        return files
            .Select(Path.GetFileName)
            .Where(n => n != null)
            .Cast<string>()
            .Where(n =>
                n.EndsWith(".lotheader", StringComparison.OrdinalIgnoreCase) ||
                n.EndsWith(".lotpack",   StringComparison.OrdinalIgnoreCase) ||
                n.EndsWith(".tmx",       StringComparison.OrdinalIgnoreCase) ||
                n.EndsWith(".tbx",       StringComparison.OrdinalIgnoreCase) ||
                n.EndsWith(".building",  StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }

    private static DeadMtlVanillaBuildingSourceDiscoveryGroupClaim BuildGroupClaim(
        string kind,
        DeadMtlVanillaBuildingSourceDiscoveryCounts counts)
    {
        bool editable = counts.TbxCount > 0 || counts.BuildingCount > 0;
        bool compiled = counts.LotheaderCount > 0 || counts.LotpackCount > 0;
        bool vanilla  = kind == "VANILLA_COMPILED_MAP";
        return new DeadMtlVanillaBuildingSourceDiscoveryGroupClaim
        {
            EditableTemplateSource = editable,
            CompiledMapSource      = compiled,
            VanillaSource          = vanilla,
        };
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
