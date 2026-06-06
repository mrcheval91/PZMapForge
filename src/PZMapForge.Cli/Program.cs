using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using PZMapForge.Core.ImageParsing;
using PZMapForge.Core.Layers;
using PZMapForge.Core.LocalPz;
using PZMapForge.Core.Palette;
using PZMapForge.Core.ParsedCell;
using PZMapForge.Core.Planning;
using PZMapForge.Core.Primitives;
using PZMapForge.Core.Regions;

// Claim boundary: PZMapForge CLI is a planning tool only.
// It does not produce a playable Project Zomboid export.

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: pzmapforge <command> [options]");
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  palette-check     --palette <path>");
    Console.Error.WriteLine("  parsed-cell-check --path <path>");
    Console.Error.WriteLine("  region-check      --path <path>");
    Console.Error.WriteLine("  primitive-check   --path <path>");
    Console.Error.WriteLine("  image-check       --path <image> --palette <palette> [--resize]");
    Console.Error.WriteLine("  image-export      --path <image> --palette <palette> [--output <dir>] [--resize]");
    Console.Error.WriteLine("  full-pipeline     --path <image> --palette <palette> [--output <dir>] [--resize]");
    Console.Error.WriteLine("                    [--tiny-threshold <int>] [--large-threshold <int>]");
    Console.Error.WriteLine("  plan-check        --path <path> [--tiny-threshold <int>] [--large-threshold <int>]");
    Console.Error.WriteLine("  plan-export       --path <path> [--output <dir>] [--tiny-threshold <int>] [--large-threshold <int>]");
    Console.Error.WriteLine("  layer-pipeline    --layers <manifest> --palette <palette> [--output <dir>] [--resize]");
    Console.Error.WriteLine("                    [--tiny-threshold <int>] [--large-threshold <int>]");
    Console.Error.WriteLine("  layer-validate    --layers <manifest> --palette <palette> [--resize]");
    Console.Error.WriteLine("  local-tile-survey --config <path> [--output <dir>]");
    Console.Error.WriteLine("  app-export        --path <image> --palette <palette> [--output <dir>] [--run-name <name>] [--annotation <image>] [--svg-selection <json>] [--resize]");
    Console.Error.WriteLine("                    [--tiny-threshold <int>] [--large-threshold <int>]");
    Console.Error.WriteLine("  map-plan          --source <path> --output <dir>");
    Console.Error.WriteLine("  map-scaffold      --source <path> --output <dir>");
    Console.Error.WriteLine("  map-export-experimental  --map-id <id> --output <dir> [--cell-x <int>] [--cell-y <int>] [--build42-package]");
    Console.Error.WriteLine("  inspect-build42-experimental-package  --package <dir> --output <dir>");
    return 1;
}

return args[0] switch
{
    "image-check"       => ImageCheckCommand(args[1..]),
    "image-export"      => ImageExportCommand(args[1..]),
    "full-pipeline"     => FullPipelineCommand(args[1..]),
    "palette-check"     => PaletteCheckCommand(args[1..]),
    "parsed-cell-check" => ParsedCellCheckCommand(args[1..]),
    "region-check"      => RegionCheckCommand(args[1..]),
    "primitive-check"   => PrimitiveCheckCommand(args[1..]),
    "plan-check"        => PlanCheckCommand(args[1..]),
    "plan-export"       => PlanExportCommand(args[1..]),
    "layer-pipeline"    => LayerPipelineCommand(args[1..]),
    "layer-validate"    => LayerValidateCommand(args[1..]),
    "local-tile-survey" => LocalTileSurveyCommand(args[1..]),
    "app-export"        => AppExportCommand(args[1..]),
    "map-plan"          => MapPlanCommand(args[1..]),
    "map-scaffold"           => MapScaffoldCommand(args[1..]),
    "map-export-experimental"                => MapExportExperimentalCommand(args[1..]),
    "inspect-build42-experimental-package"  => InspectBuild42ExperimentalPackageCommand(args[1..]),
    _ => UnknownCommand(args[0]),
};

static int PaletteCheckCommand(string[] args)
{
    var palettePath = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--palette" or "-p") { palettePath = args[i + 1]; break; }
    }

    if (string.IsNullOrWhiteSpace(palettePath))
    {
        Console.Error.WriteLine("palette-check requires --palette <path>");
        return 1;
    }

    var result = PaletteLoader.Load(palettePath);

    Console.WriteLine($"Schema:     {result.Document?.Schema ?? "(unreadable)"}");
    Console.WriteLine($"Dimensions: {result.Document?.CellWidth ?? 0}x{result.Document?.CellHeight ?? 0}");
    Console.WriteLine($"Kinds:      {result.Document?.Kinds?.Count ?? 0}");

    var gids  = result.Document?.Kinds?.Select(k => k.Gid).OrderBy(g => g).ToList() ?? [];
    var range = gids.Count > 0 ? $"{gids.First()}..{gids.Last()}" : "none";
    Console.WriteLine($"GID range:  {range}");

    if (result.IsValid) { Console.WriteLine("Status:     OK"); return 0; }

    Console.WriteLine("Status:     INVALID");
    foreach (var e in result.Errors) Console.Error.WriteLine($"  error: {e}");
    return 1;
}

static int ParsedCellCheckCommand(string[] args)
{
    var jsonPath = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--path" or "-p") { jsonPath = args[i + 1]; break; }
    }

    if (string.IsNullOrWhiteSpace(jsonPath))
    {
        Console.Error.WriteLine("parsed-cell-check requires --path <path>");
        return 1;
    }

    var result = ParsedCellLoader.Load(jsonPath);

    Console.WriteLine($"Schema:     {result.Document?.Schema ?? "(unreadable)"}");
    Console.WriteLine($"Dimensions: {result.Document?.Width ?? 0}x{result.Document?.Height ?? 0}");
    Console.WriteLine($"Rows:       {result.Document?.Rows?.Count ?? 0}");
    Console.WriteLine($"Kinds:      {result.Document?.Counts?.Count ?? 0}");

    if (result.IsValid) { Console.WriteLine("Status:     OK"); return 0; }

    Console.WriteLine("Status:     INVALID");
    foreach (var e in result.Errors) Console.Error.WriteLine($"  error: {e}");
    return 1;
}

static int RegionCheckCommand(string[] args)
{
    var jsonPath = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--path" or "-p") { jsonPath = args[i + 1]; break; }
    }

    if (string.IsNullOrWhiteSpace(jsonPath))
    {
        Console.Error.WriteLine("region-check requires --path <path>");
        return 1;
    }

    var cellResult = ParsedCellLoader.Load(jsonPath);
    if (!cellResult.IsValid)
    {
        Console.WriteLine("Status:     INVALID (parsed-cell failed)");
        foreach (var e in cellResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }

    var grid      = cellResult.Grid!;
    var codeToKind = cellResult.Document!.Counts
        .ToDictionary(c => c.Code[0], c => c.Kind)
        .AsReadOnly();

    var result = RegionExtractor.Extract(grid, codeToKind);

    Console.WriteLine($"Dimensions: {grid.Width}x{grid.Height}");
    Console.WriteLine($"Regions:    {result.TotalRegions}");
    Console.WriteLine($"Kinds:      {result.SummaryByKind.Count}");
    Console.WriteLine($"Pixels:     {result.TotalPixels}");
    Console.WriteLine("Status:     OK");
    return 0;
}

static int PrimitiveCheckCommand(string[] args)
{
    var jsonPath = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--path" or "-p") { jsonPath = args[i + 1]; break; }
    }

    if (string.IsNullOrWhiteSpace(jsonPath))
    {
        Console.Error.WriteLine("primitive-check requires --path <path>");
        return 1;
    }

    var cellResult = ParsedCellLoader.Load(jsonPath);
    if (!cellResult.IsValid)
    {
        Console.WriteLine("Status:     INVALID (parsed-cell failed)");
        foreach (var e in cellResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }

    var grid      = cellResult.Grid!;
    var codeToKind = cellResult.Document!.Counts
        .ToDictionary(c => c.Code[0], c => c.Kind)
        .AsReadOnly();

    PrimitiveClassificationResult result;
    try
    {
        var regions = RegionExtractor.Extract(grid, codeToKind);
        result      = PrimitiveClassifier.Classify(regions);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status:     INVALID (classification failed)");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    Console.WriteLine($"Dimensions:     {grid.Width}x{grid.Height}");
    Console.WriteLine($"Regions:        {result.Primitives.Count}");
    Console.WriteLine($"Primitives:     {result.PrimitiveCount}");
    Console.WriteLine($"Primitive types:{result.SummaryByPrimitiveType.Count}");
    Console.WriteLine($"Pixels:         {result.TotalPixels}");
    Console.WriteLine("Status:         OK");
    return 0;
}

static int PlanCheckCommand(string[] args)
{
    var jsonPath = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--path" or "-p") { jsonPath = args[i + 1]; break; }
    }

    if (string.IsNullOrWhiteSpace(jsonPath))
    {
        Console.Error.WriteLine("plan-check requires --path <path>");
        return 1;
    }

    var (opts, optErr) = ParsePlanningOptions(args);
    if (optErr != 0) return optErr;

    var cellResult = ParsedCellLoader.Load(jsonPath);
    if (!cellResult.IsValid)
    {
        Console.WriteLine("Status:           INVALID (parsed-cell failed)");
        foreach (var e in cellResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }

    var grid       = cellResult.Grid!;
    var codeToKind = cellResult.Document!.Counts
        .ToDictionary(c => c.Code[0], c => c.Kind)
        .AsReadOnly();

    PlanningRuleResult result;
    try
    {
        var regions    = RegionExtractor.Extract(grid, codeToKind);
        var primitives = PrimitiveClassifier.Classify(regions);
        result         = PlanningRuleEngine.Evaluate(primitives, opts!);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status:           INVALID (planning evaluation failed)");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    Console.WriteLine($"Dimensions:       {grid.Width}x{grid.Height}");
    Console.WriteLine($"Primitives:       {result.Summary.PrimitiveCount}");
    Console.WriteLine($"Recommendations:  {result.RecommendationCount}");
    Console.WriteLine($"Warnings:         {result.Summary.WarningCount}");
    Console.WriteLine($"Tiny threshold:   {opts!.TinyBuildingPixelThreshold}");
    Console.WriteLine($"Large threshold:  {opts!.LargeGroundPixelThreshold}");
    Console.WriteLine("Status:           OK");
    return 0;
}

static int PlanExportCommand(string[] args)
{
    var jsonPath  = string.Empty;
    var outputDir = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--path"   or "-p") { jsonPath  = args[i + 1]; }
        if (args[i] is "--output" or "-o") { outputDir = args[i + 1]; }
    }

    var (opts, optErr) = ParsePlanningOptions(args);
    if (optErr != 0) return optErr;

    if (string.IsNullOrWhiteSpace(jsonPath))
    {
        Console.Error.WriteLine("plan-export requires --path <path>");
        return 1;
    }

    // Default output: .local/mapforge (same dir as parsed-cell)
    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Path.GetDirectoryName(Path.GetFullPath(jsonPath)) ?? ".";

    // Safety: refuse output outside .local/ unless caller explicitly chose --output
    var outputFull  = Path.GetFullPath(outputDir);
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"plan-export: refusing to write outside a .local/ directory: {outputFull}");
        Console.Error.WriteLine("  Pass --output to an explicit .local/ path.");
        return 1;
    }

    var cellResult = ParsedCellLoader.Load(jsonPath);
    if (!cellResult.IsValid)
    {
        Console.WriteLine("Status: INVALID (parsed-cell failed)");
        foreach (var e in cellResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }

    var grid      = cellResult.Grid!;
    var codeToKind = cellResult.Document!.Counts
        .ToDictionary(c => c.Code[0], c => c.Kind)
        .AsReadOnly();

    PlanningRuleResult planResult;
    try
    {
        var regions    = RegionExtractor.Extract(grid, codeToKind);
        var primitives = PrimitiveClassifier.Classify(regions);
        planResult     = PlanningRuleEngine.Evaluate(primitives, opts!);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status: INVALID (planning evaluation failed)");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    var (jsonOut, mdOut) = PlanningArtifactWriter.Write(
        outputDir, grid.Width, grid.Height,
        Path.GetFullPath(jsonPath), "PZMapForge.Cli plan-export",
        planResult, opts);

    Console.WriteLine($"Plan JSON:       {jsonOut}");
    Console.WriteLine($"Plan report:     {mdOut}");
    Console.WriteLine($"Primitives:      {planResult.Summary.PrimitiveCount}");
    Console.WriteLine($"Recommendations: {planResult.RecommendationCount}");
    Console.WriteLine($"Warnings:        {planResult.Summary.WarningCount}");
    Console.WriteLine($"Tiny threshold:  {opts!.TinyBuildingPixelThreshold}");
    Console.WriteLine($"Large threshold: {opts!.LargeGroundPixelThreshold}");
    Console.WriteLine("Status:          OK");
    return 0;
}

static int MapPlanCommand(string[] args)
{
    // Dry-run only. Writes map-export-plan.json and map-export-plan.md.
    // Does not compile, export, or write any playable Project Zomboid output.
    var sourcePath = string.Empty;
    var outputDir  = string.Empty;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--source" or "-s" && i + 1 < args.Length) sourcePath = args[++i];
        else if (args[i] is "--output" or "-o" && i + 1 < args.Length) outputDir  = args[++i];
    }

    if (string.IsNullOrWhiteSpace(sourcePath))
    {
        Console.Error.WriteLine("map-plan requires --source <path>");
        return 1;
    }
    if (string.IsNullOrWhiteSpace(outputDir))
    {
        Console.Error.WriteLine("map-plan requires --output <dir>");
        return 1;
    }

    var outputFull = Path.GetFullPath(outputDir);

    // Refuse media/maps output
    if (outputFull.Contains("media" + Path.DirectorySeparatorChar + "maps",
            StringComparison.OrdinalIgnoreCase) ||
        outputFull.Contains("media/maps", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"map-plan: refusing to write to media/maps: {outputFull}");
        return 1;
    }

    // Require .local/ output
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"map-plan: refusing to write outside a .local/ directory: {outputFull}");
        return 1;
    }

    if (!File.Exists(sourcePath))
    {
        Console.Error.WriteLine($"map-plan: source file not found: {sourcePath}");
        return 1;
    }

    string sourceJson;
    try { sourceJson = File.ReadAllText(sourcePath, Encoding.UTF8); }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"map-plan: failed to read source file: {ex.Message}");
        return 1;
    }

    JsonDocument sourceDoc;
    try { sourceDoc = JsonDocument.Parse(sourceJson); }
    catch (JsonException ex)
    {
        Console.Error.WriteLine($"map-plan: source file is not valid JSON: {ex.Message}");
        return 1;
    }

    var root = sourceDoc.RootElement;

    if (!root.TryGetProperty("schema", out var schemaProp) ||
        schemaProp.GetString() != "pzmapforge.map-source.v0.1")
    {
        Console.Error.WriteLine("map-plan: source schema must be 'pzmapforge.map-source.v0.1'");
        return 1;
    }

    if (!root.TryGetProperty("claim_boundary", out var cbProp) ||
        cbProp.GetString() != "map_source_only_not_exported_not_pz_load_tested")
    {
        Console.Error.WriteLine(
            "map-plan: claim_boundary must be 'map_source_only_not_exported_not_pz_load_tested'");
        return 1;
    }

    var mapId    = root.TryGetProperty("map_id",    out var idProp) ? idProp.GetString()   ?? string.Empty : string.Empty;
    var cellSize = root.TryGetProperty("cell_size", out var csProp) ? csProp.GetInt32()    : 0;

    if (!root.TryGetProperty("cells", out var cellsProp) ||
        cellsProp.ValueKind != JsonValueKind.Array)
    {
        Console.Error.WriteLine("map-plan: source must have a 'cells' array");
        return 1;
    }

    var cells = cellsProp.EnumerateArray().ToList();
    if (cells.Count == 0)
    {
        Console.Error.WriteLine("map-plan: cells array must not be empty");
        return 1;
    }

    var spawnCount    = 0;
    var zoneCount     = 0;
    var terrainCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    foreach (var cell in cells)
    {
        if (cell.TryGetProperty("spawn_points", out var sp) && sp.ValueKind == JsonValueKind.Array)
            spawnCount += sp.GetArrayLength();
        if (cell.TryGetProperty("zones", out var z) && z.ValueKind == JsonValueKind.Array)
            zoneCount += z.GetArrayLength();
        if (cell.TryGetProperty("terrain", out var t))
        {
            var terrain = t.GetString() ?? "unknown";
            terrainCounts[terrain] = terrainCounts.GetValueOrDefault(terrain) + 1;
        }
    }

    Directory.CreateDirectory(outputFull);

    var sourceFileName = Path.GetFileName(sourcePath);
    var generatedAt    = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    var plan = new
    {
        schema                     = "pzmapforge.map-export-plan.v0.1",
        source_schema              = "pzmapforge.map-source.v0.1",
        claim_boundary             = "map_plan_only_not_exported_not_pz_load_tested",
        generated_at_utc           = generatedAt,
        source_file_name           = sourceFileName,
        map_id                     = mapId,
        cell_size                  = cellSize,
        cell_count                 = cells.Count,
        spawn_point_count          = spawnCount,
        zone_count                 = zoneCount,
        terrain_counts             = terrainCounts,
        dry_run                    = true,
        execute_supported          = false,
        would_write = new[]
        {
            "future mod.info",
            $"future media/maps/{mapId}/ map directory",
            "future spawn definition",
            "future compiled cell files"
        },
        written_files = new[] { "map-export-plan.json", "map-export-plan.md" },
        compiled_outputs_written   = false,
        local_mod_scaffold_written = false,
        media_maps_touched         = false,
        pz_assets_read_or_copied   = false,
        playable_export_generated  = false,
        notes = new[]
        {
            "Dry-run plan only.",
            "No playable Project Zomboid export generated.",
            "No media/maps writes.",
            "No PZ assets read or copied."
        },
        scaffold_contract_version        = "0.1",
        text_only_scaffold_supported_now = false,
        text_only_scaffold_written       = false,
        scaffold_execute_supported       = false,
        future_scaffold_files = new[]
        {
            new { path = "future mod.info",
                  kind = "metadata",        written_now = false, reason = "MAP-3A contract only" },
            new { path = $"future media/maps/{mapId}/map.info",
                  kind = "map_metadata",    written_now = false, reason = "MAP-3A contract only" },
            new { path = $"future media/maps/{mapId}/spawnpoints.lua",
                  kind = "spawn_definition", written_now = false, reason = "MAP-3A contract only" },
            new { path = $"future media/maps/{mapId}/README_PZMAPFORGE_BOUNDARY.txt",
                  kind = "boundary_note",   written_now = false, reason = "MAP-3A contract only" },
        }
    };

    var jsonOpts    = new JsonSerializerOptions { WriteIndented = true };
    var jsonOutPath = Path.Combine(outputFull, "map-export-plan.json");
    var mdOutPath   = Path.Combine(outputFull, "map-export-plan.md");

    File.WriteAllText(jsonOutPath, JsonSerializer.Serialize(plan, jsonOpts), Encoding.UTF8);

    var terrainSummary = string.Join(", ",
        terrainCounts.Select(kv => $"{kv.Key}: {kv.Value}"));

    var md = $"""
# Map Export Plan

Map ID:           {mapId}
Source schema:    pzmapforge.map-source.v0.1
Claim boundary:   map_plan_only_not_exported_not_pz_load_tested
Generated:        {generatedAt}
Source file:      {sourceFileName}

## Map summary

| Field | Value |
|---|---|
| Cell count | {cells.Count} |
| Cell size | {cellSize} |
| Spawn point count | {spawnCount} |
| Zone count | {zoneCount} |
| Terrain counts | {terrainSummary} |

## Dry-run plan

This is a dry-run plan only. No files are compiled or exported.

The following would be written by a future compiler (not written now):

- future mod.info
- future media/maps/{mapId}/ map directory
- future spawn definition
- future compiled cell files

Files written by this command:

- map-export-plan.json
- map-export-plan.md

## Future text-only scaffold contract

MAP-3A defines the future text-only scaffold only. It does not write the scaffold.

A future MAP-3B writer may create:
- future mod.info
- future media/maps/{mapId}/map.info
- future media/maps/{mapId}/spawnpoints.lua
- future media/maps/{mapId}/README_PZMAPFORGE_BOUNDARY.txt

Written now:
- none of the scaffold files

## Non-claims

- No playable Project Zomboid export generated.
- No compiled outputs written.
- No local mod scaffold written.
- No media/maps writes.
- No PZ assets read or copied.
- No SVG geometry converted.
- No coordinate math performed.
""";

    File.WriteAllText(mdOutPath, md, Encoding.UTF8);

    Console.WriteLine($"Plan JSON:                 {jsonOutPath}");
    Console.WriteLine($"Plan report:               {mdOutPath}");
    Console.WriteLine($"Map ID:                    {mapId}");
    Console.WriteLine($"Cells:                     {cells.Count}");
    Console.WriteLine($"Spawn points:              {spawnCount}");
    Console.WriteLine($"Zones:                     {zoneCount}");
    Console.WriteLine($"Dry run:                   true");
    Console.WriteLine($"Execute supported:         false");
    Console.WriteLine($"Compiled outputs written:  false");
    Console.WriteLine($"Playable export generated: false");
    Console.WriteLine($"media/maps touched:        false");
    Console.WriteLine($"PZ assets read or copied:  false");
    Console.WriteLine("Status:                   OK");
    return 0;
}

static int MapScaffoldCommand(string[] args)
{
    // Claim boundary: text-only local scaffold writer (MAP-3B).
    // Writes exactly four text files under a .local output directory.
    // Does not write compiled outputs, PZ assets, or playable export.
    var sourcePath = string.Empty;
    var outputDir  = string.Empty;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--source" or "-s" && i + 1 < args.Length) sourcePath = args[++i];
        else if (args[i] is "--output" or "-o" && i + 1 < args.Length) outputDir  = args[++i];
    }

    if (string.IsNullOrWhiteSpace(sourcePath))
    {
        Console.Error.WriteLine("map-scaffold requires --source <path>");
        return 1;
    }
    if (string.IsNullOrWhiteSpace(outputDir))
    {
        Console.Error.WriteLine("map-scaffold requires --output <dir>");
        return 1;
    }

    var outputFull = Path.GetFullPath(outputDir);

    // Refuse media/maps output path (checked before .local guard)
    if (outputFull.Contains("media" + Path.DirectorySeparatorChar + "maps",
            StringComparison.OrdinalIgnoreCase) ||
        outputFull.Contains("media/maps", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"map-scaffold: refusing to write to media/maps: {outputFull}");
        Console.Error.WriteLine("  Pass --output to a .local/ path that is not itself a media/maps path.");
        return 1;
    }

    // Require .local/ output
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"map-scaffold: refusing to write outside a .local/ directory: {outputFull}");
        Console.Error.WriteLine("  Pass --output to an explicit .local/ path.");
        return 1;
    }

    if (!File.Exists(sourcePath))
    {
        Console.Error.WriteLine($"map-scaffold: source file not found: {sourcePath}");
        return 1;
    }

    string sourceJson;
    try { sourceJson = File.ReadAllText(sourcePath, Encoding.UTF8); }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"map-scaffold: failed to read source file: {ex.Message}");
        return 1;
    }

    JsonDocument sourceDoc;
    try { sourceDoc = JsonDocument.Parse(sourceJson); }
    catch (JsonException ex)
    {
        Console.Error.WriteLine($"map-scaffold: source file is not valid JSON: {ex.Message}");
        return 1;
    }

    var root = sourceDoc.RootElement;

    if (!root.TryGetProperty("schema", out var schemaProp) ||
        schemaProp.GetString() != "pzmapforge.map-source.v0.1")
    {
        Console.Error.WriteLine("map-scaffold: source schema must be 'pzmapforge.map-source.v0.1'");
        return 1;
    }

    if (!root.TryGetProperty("claim_boundary", out var cbProp) ||
        cbProp.GetString() != "map_source_only_not_exported_not_pz_load_tested")
    {
        Console.Error.WriteLine(
            "map-scaffold: claim_boundary must be 'map_source_only_not_exported_not_pz_load_tested'");
        return 1;
    }

    var mapId = root.TryGetProperty("map_id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
    if (string.IsNullOrWhiteSpace(mapId))
    {
        Console.Error.WriteLine("map-scaffold: source must have a non-empty 'map_id'");
        return 1;
    }

    if (!root.TryGetProperty("cells", out var cellsProp) ||
        cellsProp.ValueKind != JsonValueKind.Array)
    {
        Console.Error.WriteLine("map-scaffold: source must have a 'cells' array");
        return 1;
    }

    if (cellsProp.GetArrayLength() == 0)
    {
        Console.Error.WriteLine("map-scaffold: cells array must not be empty");
        return 1;
    }

    // Create output directories
    var mapDirPath = Path.Combine(outputFull, "media", "maps", mapId);
    Directory.CreateDirectory(outputFull);
    Directory.CreateDirectory(mapDirPath);

    // Write 1: mod.info
    var modInfoContent = $"""
name=PZMapForge Minimal Scaffold - {mapId}
id=pzmapforge_{mapId}
description=Generated by PZMapForge MAP-3B. Text-only scaffold. Not playable. No compiled map files. No PZ assets included. Not load-tested.
""";
    File.WriteAllText(Path.Combine(outputFull, "mod.info"), modInfoContent, Encoding.UTF8);

    // Write 2: media/maps/<map_id>/map.info
    var mapInfoContent = $"""
# PZMapForge MAP-3B text-only scaffold
# Not a validated Project Zomboid map.info. Not playable. Not load-tested.
map_id={mapId}
claim_boundary=map_scaffold_text_only_not_compiled_not_pz_load_tested
compiled_outputs_written=false
playable_export_generated=false
pz_assets_included=false
media_maps_scope=.local_only
""";
    File.WriteAllText(Path.Combine(mapDirPath, "map.info"), mapInfoContent, Encoding.UTF8);

    // Write 3: media/maps/<map_id>/spawnpoints.lua
    var spawnpointsContent = """
-- PZMapForge MAP-3B text-only scaffold
-- Placeholder only. Not load-tested. No coordinate math performed.
-- Source spawn points are not converted to validated Project Zomboid spawn coordinates.
-- No playable export generated.
-- No PZ assets included.
-- This file requires local inspection and completion before any Project Zomboid load test.

SpawnPoints = {}
""";
    File.WriteAllText(Path.Combine(mapDirPath, "spawnpoints.lua"), spawnpointsContent, Encoding.UTF8);

    // Write 4: media/maps/<map_id>/README_PZMAPFORGE_BOUNDARY.txt
    var readmeContent = $"""
PZMapForge MAP-3B Text-Only Scaffold
=====================================

Generated by PZMapForge MAP-3B.
Text-only scaffold. Not a playable Project Zomboid map.
map_id: {mapId}

Boundary:
- No lotpack, lotheader, or bin files written.
- No worldmap files written.
- No PZ assets included.
- No SVG geometry converted.
- No coordinate math performed.
- Not load-tested.
- Output is under .local only.
- No compiled outputs written.

This scaffold is evidence of the file structure only.
It does not constitute a playable Project Zomboid map export.
""";
    File.WriteAllText(Path.Combine(mapDirPath, "README_PZMAPFORGE_BOUNDARY.txt"), readmeContent, Encoding.UTF8);

    Console.WriteLine($"Scaffold root:              {outputFull}");
    Console.WriteLine($"Files written:              4");
    Console.WriteLine($"Map ID:                     {mapId}");
    Console.WriteLine($"text_only_scaffold_written: true");
    Console.WriteLine($"compiled_outputs_written:   false");
    Console.WriteLine($"playable_export_generated:  false");
    Console.WriteLine($"media_maps_scope:            .local_only");
    Console.WriteLine($"pz_assets_read_or_copied:   false");
    Console.WriteLine("Status:                     OK");
    return 0;
}

static int MapExportExperimentalCommand(string[] args)
{
    // Experimental local-only compiled empty cell writer (MAP-5A).
    // Authorized by MAP-4H: MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY.
    // Writes hypothesis-only binary files under .local only.
    // Not a playable export. Not load-tested. Experimental only.
    var mapId                   = string.Empty;
    var outputDir               = string.Empty;
    var cellX                   = 0;
    var cellY                   = 0;
    var build42Package          = false;
    var lotheaderCandidate      = "current_failed";
    var build42CandidateWriter  = false;
    var build42CandidateProfile = "empty_grass_v0";

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--map-id" && i + 1 < args.Length)                   mapId                   = args[++i];
        else if (args[i] is "--output" or "-o" && i + 1 < args.Length)           outputDir               = args[++i];
        else if (args[i] is "--build42-package")                                  build42Package          = true;
        else if (args[i] is "--lotheader-candidate" && i + 1 < args.Length)      lotheaderCandidate      = args[++i];
        else if (args[i] is "--build42-candidate-writer")                         build42CandidateWriter  = true;
        else if (args[i] is "--build42-candidate-profile" && i + 1 < args.Length) build42CandidateProfile = args[++i];
        else if (args[i] is "--cell-x" && i + 1 < args.Length)
        {
            if (int.TryParse(args[++i], out var cx)) cellX = cx;
        }
        else if (args[i] is "--cell-y" && i + 1 < args.Length)
        {
            if (int.TryParse(args[++i], out var cy)) cellY = cy;
        }
    }

    if (string.IsNullOrWhiteSpace(mapId))
    {
        Console.Error.WriteLine("map-export-experimental requires --map-id <id>");
        return 1;
    }
    if (string.IsNullOrWhiteSpace(outputDir))
    {
        Console.Error.WriteLine("map-export-experimental requires --output <dir>");
        return 1;
    }

    var outputFull = Path.GetFullPath(outputDir);

    // Refuse PZ install paths
    var pzPaths = new[]
    {
        "steamapps/common/ProjectZomboid",
        "steamapps/common/Project Zomboid",
        "steamapps" + Path.DirectorySeparatorChar + "common" + Path.DirectorySeparatorChar + "ProjectZomboid",
        "steamapps" + Path.DirectorySeparatorChar + "common" + Path.DirectorySeparatorChar + "Project Zomboid",
    };
    foreach (var pzPath in pzPaths)
    {
        if (outputFull.Contains(pzPath, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine(
                $"map-export-experimental: refusing to write to PZ install path: {outputFull}");
            return 1;
        }
    }

    // Refuse media/maps in output path
    if (outputFull.Contains("media" + Path.DirectorySeparatorChar + "maps",
            StringComparison.OrdinalIgnoreCase) ||
        outputFull.Contains("media/maps", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"map-export-experimental: refusing to write to media/maps: {outputFull}");
        return 1;
    }

    // Require .local/ output
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"map-export-experimental: refusing to write outside a .local/ directory: {outputFull}");
        return 1;
    }

    // ---- Build 42 candidate writer MVP (MAP-6L) ----
    if (build42CandidateWriter)
    {
        return Build42CandidateWriterCommand(mapId, outputFull, cellX, cellY, build42CandidateProfile);
    }

    // ---- Build 42 Workshop-style nested package layout ----
    if (build42Package)
    {
        var generatedAtB42 = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var cellCoordB42   = $"{cellX}_{cellY}";
        var pkgRoot        = Path.Combine(outputFull, mapId + "_build42_workshop");
        var modRoot        = Path.Combine(pkgRoot, "Contents", "mods", mapId);
        var mapDataDir     = Path.Combine(modRoot, "media", "maps", mapId);

        Directory.CreateDirectory(pkgRoot);
        Directory.CreateDirectory(modRoot);
        Directory.CreateDirectory(mapDataDir);

        // workshop.txt
        var workshopTxtContent = $"""
version=1
title=PZMapForge Experimental - {mapId}
description=EXPERIMENTAL OUTPUT -- NOT VALIDATED. Generated by PZMapForge MAP-5D.
description=Not a playable Project Zomboid map. Not load-tested. Do not redistribute.
tags=
visibility=private
""";
        File.WriteAllText(Path.Combine(pkgRoot, "workshop.txt"), workshopTxtContent, Encoding.UTF8);

        // preview.png and poster.png — placeholder images
        var previewLabel = $"PZMapForge {mapId}";
        WritePlaceholderPng(Path.Combine(pkgRoot, "preview.png"), previewLabel, "EXPERIMENTAL -- NOT VALIDATED");
        WritePlaceholderPng(Path.Combine(modRoot,  "poster.png"), previewLabel, "EXPERIMENTAL -- NOT VALIDATED");

        // Contents/mods/<id>/mod.info  (Build 42 fields)
        var modInfoB42Content = $"""
name=PZMapForge Experimental - {mapId}
id={mapId}
description=EXPERIMENTAL OUTPUT -- NOT VALIDATED. Not a playable Project Zomboid map. Not load-tested.
category=map
modversion=1.0
pzversion=42.0
versionMin=42.0
poster=poster.png
icon=poster.png
""";
        File.WriteAllText(Path.Combine(modRoot, "mod.info"), modInfoB42Content, Encoding.UTF8);

        // map.info  (ModTemplate style — no lots field)
        var mapInfoB42Content = $"""
title=PZMapForge Experimental - {mapId}
description=EXPERIMENTAL OUTPUT -- NOT VALIDATED. Not a playable Project Zomboid map. Not load-tested.
""";
        File.WriteAllText(Path.Combine(mapDataDir, "map.info"), mapInfoB42Content, Encoding.UTF8);

        // spawnpoints.lua  (profession-keyed, worldX/Y = cell coords)
        var spawnB42Content = $$"""
-- PZMapForge MAP-5D experimental output -- NOT VALIDATED -- not a playable Project Zomboid map.
-- Spawn point is hypothesis-only. Coordinates not verified against Build 42 system.
function SpawnPoints()
return {
  all = {
    { worldX = {{cellX}}, worldY = {{cellY}}, posX = 150, posY = 150, posZ = 0 }
  },
}
end
""";
        File.WriteAllText(Path.Combine(mapDataDir, "spawnpoints.lua"), spawnB42Content, Encoding.UTF8);

        // objects.lua — syntactically valid empty Lua (MAP-6C fix; return {} is not load-tested).
        File.WriteAllText(Path.Combine(mapDataDir, "objects.lua"), "return {}\n", Encoding.UTF8);

        // thumb.png — tiny placeholder for map directory
        WritePlaceholderPng(Path.Combine(mapDataDir, "thumb.png"), mapId, string.Empty);

        // README
        var readmeB42Content = $"""
EXPERIMENTAL OUTPUT -- NOT VALIDATED
=====================================

Generated by PZMapForge MAP-5D (Build 42 Workshop package layout).
Generated at: {generatedAtB42}
Map ID: {mapId}
Cell: ({cellX}, {cellY})
Package layout: Build 42 Workshop (Contents/mods nested)

BOUNDARY STATEMENT:
- Not a playable Project Zomboid map.
- Not load-tested.
- Generated binary files are hypothesis-only.
- Do not redistribute.
- Do not claim Project Zomboid compatibility.
- No PZ assets copied or read.
- Manual load test required before any claim changes.
- Output is under .local only.

BINARY HYPOTHESES (unchanged from MAP-5A):
- .lotheader: 8 bytes (zero header + 0-entry count).
- .lotpack: 7208 bytes (hdrA=900, hdrB=7204, all-zero offset table).
- chunkdata_*.bin: 902 bytes (0x0001 header + 900 zero bytes).

MAP-5B result: LOAD_TEST_INCONCLUSIVE (Build 42 mod discovery blocker, not binary failure).
MAP-5D adds correct Build 42 Workshop package layout to address packaging blocker.
""";
        File.WriteAllText(
            Path.Combine(mapDataDir, "README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt"),
            readmeB42Content, Encoding.UTF8);

        // Binary: .lotheader — MAP-6C/MAP-6D candidate writer gate
        var (lotheaderB42, lotheaderB42CandidateStatus, lotheaderB42EntryCount, lotheaderB42Entries) =
            BuildLotheaderForCandidate(lotheaderCandidate);
        var lotheaderB42Sha256     = string.Join("", SHA256.HashData(lotheaderB42).Select(b => b.ToString("x2")));
        var lotheaderB42FirstBytes = string.Join("", lotheaderB42.Take(32).Select(b => b.ToString("x2")));
        File.WriteAllBytes(Path.Combine(mapDataDir, $"{cellCoordB42}.lotheader"), lotheaderB42);

        // Binary: .lotpack
        var lotpackB42 = new byte[7208];
        lotpackB42[0] = 0x84; lotpackB42[1] = 0x03;
        lotpackB42[4] = 0x24; lotpackB42[5] = 0x1C;
        File.WriteAllBytes(Path.Combine(mapDataDir, $"world_{cellCoordB42}.lotpack"), lotpackB42);

        // Binary: chunkdata_*.bin
        var chunkdataB42 = new byte[902];
        chunkdataB42[1] = 0x01;
        File.WriteAllBytes(Path.Combine(mapDataDir, $"chunkdata_{cellCoordB42}.bin"), chunkdataB42);

        // Report JSON
        var b42FilesWritten = new[]
        {
            "workshop.txt",
            "preview.png",
            $"Contents/mods/{mapId}/mod.info",
            $"Contents/mods/{mapId}/poster.png",
            $"Contents/mods/{mapId}/media/maps/{mapId}/map.info",
            $"Contents/mods/{mapId}/media/maps/{mapId}/spawnpoints.lua",
            $"Contents/mods/{mapId}/media/maps/{mapId}/objects.lua",
            $"Contents/mods/{mapId}/media/maps/{mapId}/thumb.png",
            $"Contents/mods/{mapId}/media/maps/{mapId}/README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt",
            $"Contents/mods/{mapId}/media/maps/{mapId}/{cellCoordB42}.lotheader",
            $"Contents/mods/{mapId}/media/maps/{mapId}/world_{cellCoordB42}.lotpack",
            $"Contents/mods/{mapId}/media/maps/{mapId}/chunkdata_{cellCoordB42}.bin",
            $"Contents/mods/{mapId}/experimental-map-export-report.json",
            $"Contents/mods/{mapId}/experimental-map-export-report.md",
        };

        var b42Report = new
        {
            schema                     = "pzmapforge.experimental-map-export-report.v0.1",
            claim_boundary             = "experimental_local_only_not_playable_not_load_tested",
            generated_at_utc           = generatedAtB42,
            map_id                     = mapId,
            cell_x                     = cellX,
            cell_y                     = cellY,
            package_layout             = "build42_workshop",
            package_root               = (pkgRoot.Replace('\\', '/')),
            file_count                 = b42FilesWritten.Length,
            files_written              = b42FilesWritten,
            binary_files_written       = 3,
            text_files_written         = 11,
            playable_export_generated  = false,
            load_tested                = false,
            pz_assets_copied           = false,
            pz_assets_read             = false,
            media_maps_touched_in_repo = false,
            output_under_local         = true,
            experimental_writer        = true,
            assumptions = new[]
            {
                "lotheader: assuming 0-entry tileset list is accepted for blank cell",
                "lotpack: assuming chunk offset 0 means no chunk data",
                "chunkdata: assuming 902-byte all-zero chunk grid is accepted for empty cell",
                "build42: package uses Contents/mods nested layout per ModTemplate",
            },
            manual_load_test_required  = true,
            binary_runtime_status      = lotheaderCandidate is "newline_tileset_table"
                                                              or "newline_tileset_table_minimal"
                                         ? "candidate_generated_not_load_tested"
                                         : "failing_placeholder_format",
            lotheader_runtime_status   = "eof_exception_observed",
            lotpack_runtime_status     = "unproven_after_lotheader_failure",
            chunkdata_runtime_status   = "unproven_after_lotheader_failure",
            objects_lua_runtime_status = "syntax_candidate_not_load_tested",
            lotheader_candidate        = lotheaderCandidate,
            lotheader_candidate_status = lotheaderB42CandidateStatus,
            lotheader_entry_count      = lotheaderB42EntryCount,
            lotheader_entries          = lotheaderB42Entries,
            lotheader_sha256           = lotheaderB42Sha256,
            lotheader_first_bytes      = lotheaderB42FirstBytes,
            lotheader_byte_count       = lotheaderB42.Length,
            geometry_model_status      = "mismatch_suspected_not_verified",
            geometry_model_basis       = "30x30_chunk_grid_from_300x300_cell_build41_workshop_evidence",
            target_build42_cell_size   = "operator_reported_256_unverified",
        };

        var b42JsonOpts = new JsonSerializerOptions { WriteIndented = true };
        var b42JsonPath = Path.Combine(modRoot, "experimental-map-export-report.json");
        File.WriteAllText(b42JsonPath, JsonSerializer.Serialize(b42Report, b42JsonOpts), Encoding.UTF8);

        var b42Md = $"""
# Experimental Map Export Report (Build 42 Workshop Package)

Schema:         pzmapforge.experimental-map-export-report.v0.1
Claim boundary: experimental_local_only_not_playable_not_load_tested
Package layout: build42_workshop
Generated:      {generatedAtB42}
Map ID:         {mapId}
Cell:           ({cellX}, {cellY})
Package root:   {pkgRoot.Replace('\\', '/')}

## BOUNDARY

**EXPERIMENTAL OUTPUT -- NOT VALIDATED**

Not a playable Project Zomboid map.
Not load-tested. No PZ assets copied. Experimental only.
MAP-5B result: LOAD_TEST_INCONCLUSIVE (packaging blocker, not binary failure).
MAP-5D generates correct Build 42 Workshop nested layout.

## Non-claims

- Not playable.
- Not load-tested.
- No PZ assets copied or read.
- No repo media/maps writes.
- Experimental only. No compatibility claim.
""";
        var b42MdPath = Path.Combine(modRoot, "experimental-map-export-report.md");
        File.WriteAllText(b42MdPath, b42Md, Encoding.UTF8);

        Console.WriteLine($"Package root:                    {pkgRoot}");
        Console.WriteLine($"Map ID:                          {mapId}");
        Console.WriteLine($"Cell:                            ({cellX}, {cellY})");
        Console.WriteLine($"Package layout:                  build42_workshop");
        Console.WriteLine($"Files written:                   {b42FilesWritten.Length}");
        Console.WriteLine($"playable_export_generated:       false");
        Console.WriteLine($"load_tested:                     false");
        Console.WriteLine($"experimental_writer:             true");
        Console.WriteLine($"pz_assets_copied:                false");
        Console.WriteLine($"media_maps_touched_in_repo:      false");
        Console.WriteLine($"manual_load_test_required:       true");
        Console.WriteLine("Status:                          OK (EXPERIMENTAL -- NOT VALIDATED -- BUILD 42 PACKAGE)");
        return 0;
    }

    // ---- Flat layout (MAP-5A original, unchanged) ----
    var generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    var cellCoord   = $"{cellX}_{cellY}";
    var mapDir      = Path.Combine(outputFull, "media", "maps", mapId);

    Directory.CreateDirectory(outputFull);
    Directory.CreateDirectory(mapDir);

    // mod.info
    var modInfoContent = $"""
name=PZMapForge Experimental - {mapId}
id={mapId}
description=EXPERIMENTAL OUTPUT -- NOT VALIDATED. Generated by PZMapForge MAP-5A. Not a playable Project Zomboid map. Not load-tested. Hypothesis-only binary files. Do not redistribute.
poster=
""";
    File.WriteAllText(Path.Combine(outputFull, "mod.info"), modInfoContent, Encoding.UTF8);

    // media/maps/<map_id>/map.info
    var mapInfoContent = $"""
title=PZMapForge Experimental - {mapId}
lots={mapId}
description=EXPERIMENTAL OUTPUT -- NOT VALIDATED. Not a playable Project Zomboid map. Not load-tested.
fixed2x=true
""";
    File.WriteAllText(Path.Combine(mapDir, "map.info"), mapInfoContent, Encoding.UTF8);

    // media/maps/<map_id>/spawnpoints.lua
    var spawnContent = $$"""
-- PZMapForge MAP-5A experimental output -- NOT VALIDATED -- not a playable Project Zomboid map.
-- Spawn point is hypothesis-only. Not load-tested. Coordinates not verified.
function SpawnPoints()
return {
  all = {
    { worldX = {{cellX}}, worldY = {{cellY}}, posX = 150, posY = 150, posZ = 0 }
  },
}
end
""";
    File.WriteAllText(Path.Combine(mapDir, "spawnpoints.lua"), spawnContent, Encoding.UTF8);

    // media/maps/<map_id>/objects.lua — syntactically valid empty Lua (MAP-6C fix; return {} is not load-tested).
    File.WriteAllText(Path.Combine(mapDir, "objects.lua"), "return {}\n", Encoding.UTF8);

    // media/maps/<map_id>/README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt
    var readmeContent = $"""
EXPERIMENTAL OUTPUT -- NOT VALIDATED
=====================================

Generated by PZMapForge MAP-5A experimental compiled cell writer.
Generated at: {generatedAt}
Map ID: {mapId}
Cell: ({cellX}, {cellY})

BOUNDARY STATEMENT:
- Not a playable Project Zomboid map.
- Not load-tested.
- Generated binary files are hypothesis-only.
- Do not redistribute.
- Do not claim Project Zomboid compatibility.
- No PZ assets copied or read.
- Manual load test required before any claim changes.
- Output is under .local only.

BINARY HYPOTHESES:
- .lotheader: 8 bytes (zero header + 0-entry count). Assumes PZ accepts blank-cell lotheader.
- .lotpack: 7208 bytes (hdrA=900, hdrB=7204, all-zero offset table). Assumes offset=0 means no chunk data.
- chunkdata_*.bin: 902 bytes (0x0001 header + 900 zero bytes). Matches observed empty grass cell pattern.

PZMapForge MAP-4H decision: MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY
""";
    File.WriteAllText(
        Path.Combine(mapDir, "README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt"),
        readmeContent, Encoding.UTF8);

    // Binary: .lotheader — MAP-6C/MAP-6D candidate writer gate
    var (lotheaderBytes, lotheaderCandidateStatus, lotheaderEntryCount, lotheaderEntries) =
        BuildLotheaderForCandidate(lotheaderCandidate);
    var lotheaderSha256     = string.Join("", SHA256.HashData(lotheaderBytes).Select(b => b.ToString("x2")));
    var lotheaderFirstBytes = string.Join("", lotheaderBytes.Take(32).Select(b => b.ToString("x2")));
    File.WriteAllBytes(Path.Combine(mapDir, $"{cellCoord}.lotheader"), lotheaderBytes);

    // Binary: .lotpack (7208 bytes — hdrA=900, hdrB=7204, all-zero offset table)
    var lotpackBytes = new byte[7208];
    lotpackBytes[0] = 0x84; lotpackBytes[1] = 0x03; // hdrA U32 LE 900
    lotpackBytes[4] = 0x24; lotpackBytes[5] = 0x1C; // hdrB U32 LE 7204
    File.WriteAllBytes(Path.Combine(mapDir, $"world_{cellCoord}.lotpack"), lotpackBytes);

    // Binary: chunkdata_*.bin (902 bytes — 0x0001 header + 900 zero bytes)
    var chunkdataBytes = new byte[902];
    chunkdataBytes[1] = 0x01;
    File.WriteAllBytes(Path.Combine(mapDir, $"chunkdata_{cellCoord}.bin"), chunkdataBytes);

    // Report JSON
    var filesWritten = new[]
    {
        "mod.info",
        $"media/maps/{mapId}/map.info",
        $"media/maps/{mapId}/spawnpoints.lua",
        $"media/maps/{mapId}/objects.lua",
        $"media/maps/{mapId}/README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt",
        $"media/maps/{mapId}/{cellCoord}.lotheader",
        $"media/maps/{mapId}/world_{cellCoord}.lotpack",
        $"media/maps/{mapId}/chunkdata_{cellCoord}.bin",
        "experimental-map-export-report.json",
        "experimental-map-export-report.md",
    };

    var report = new
    {
        schema                     = "pzmapforge.experimental-map-export-report.v0.1",
        claim_boundary             = "experimental_local_only_not_playable_not_load_tested",
        generated_at_utc           = generatedAt,
        map_id                     = mapId,
        cell_x                     = cellX,
        cell_y                     = cellY,
        output_path                = (outputFull.Replace('\\', '/')),
        file_count                 = filesWritten.Length,
        files_written              = filesWritten,
        binary_files_written       = 3,
        text_files_written         = 7,
        playable_export_generated  = false,
        load_tested                = false,
        pz_assets_copied           = false,
        pz_assets_read             = false,
        media_maps_touched_in_repo = false,
        output_under_local         = true,
        experimental_writer        = true,
        assumptions = new[]
        {
            "lotheader: assuming 0-entry tileset list is accepted for blank cell",
            "lotpack: assuming chunk offset 0 means no chunk data",
            "chunkdata: assuming 902-byte all-zero chunk grid is accepted for empty cell",
        },
        manual_load_test_required  = true,
        binary_runtime_status      = lotheaderCandidate is "newline_tileset_table"
                                                          or "newline_tileset_table_minimal"
                                     ? "candidate_generated_not_load_tested"
                                     : "failing_placeholder_format",
        lotheader_runtime_status   = "eof_exception_observed",
        lotpack_runtime_status     = "unproven_after_lotheader_failure",
        chunkdata_runtime_status   = "unproven_after_lotheader_failure",
        objects_lua_runtime_status = "syntax_candidate_not_load_tested",
        lotheader_candidate        = lotheaderCandidate,
        lotheader_candidate_status = lotheaderCandidateStatus,
        lotheader_entry_count      = lotheaderEntryCount,
        lotheader_entries          = lotheaderEntries,
        lotheader_sha256           = lotheaderSha256,
        lotheader_first_bytes      = lotheaderFirstBytes,
        lotheader_byte_count       = lotheaderBytes.Length,
        geometry_model_status      = "mismatch_suspected_not_verified",
        geometry_model_basis       = "30x30_chunk_grid_from_300x300_cell_build41_workshop_evidence",
        target_build42_cell_size   = "operator_reported_256_unverified",
    };

    var jsonOpts      = new JsonSerializerOptions { WriteIndented = true };
    var reportJsonPath = Path.Combine(outputFull, "experimental-map-export-report.json");
    File.WriteAllText(reportJsonPath,
        JsonSerializer.Serialize(report, jsonOpts), Encoding.UTF8);

    // Report Markdown
    var md = $"""
# Experimental Map Export Report

Schema:         pzmapforge.experimental-map-export-report.v0.1
Claim boundary: experimental_local_only_not_playable_not_load_tested
Generated:      {generatedAt}
Map ID:         {mapId}
Cell:           ({cellX}, {cellY})

## BOUNDARY

**EXPERIMENTAL OUTPUT -- NOT VALIDATED**

Not a playable Project Zomboid map.
Not load-tested.
No PZ assets copied.
No repo media/maps writes.
Experimental only.

## Files written

| File | Type | Bytes |
|---|---|---|
| mod.info | text | - |
| media/maps/{mapId}/map.info | text | - |
| media/maps/{mapId}/spawnpoints.lua | text | - |
| media/maps/{mapId}/objects.lua | text | - |
| media/maps/{mapId}/README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt | text | - |
| media/maps/{mapId}/{cellCoord}.lotheader | binary | 8 |
| media/maps/{mapId}/world_{cellCoord}.lotpack | binary | 7208 |
| media/maps/{mapId}/chunkdata_{cellCoord}.bin | binary | 902 |
| experimental-map-export-report.json | report | - |
| experimental-map-export-report.md | report | - |

## Binary hypotheses

| File | Size | Hypothesis |
|---|---|---|
| {cellCoord}.lotheader | 8 bytes | 4-byte zero header + U32 LE 0 entry count |
| world_{cellCoord}.lotpack | 7208 bytes | hdrA=900, hdrB=7204, all-zero offset table |
| chunkdata_{cellCoord}.bin | 902 bytes | 0x0001 header + 900-byte all-zero chunk grid |

## Assumptions

- lotheader: assuming 0-entry tileset list is accepted for blank cell
- lotpack: assuming chunk offset 0 means no chunk data
- chunkdata: assuming 902-byte all-zero chunk grid is accepted for empty cell

## Manual load test instructions

1. Copy the output directory to your PZ mods folder.
2. Launch Project Zomboid in sandbox mode.
3. Enable the mod and start a new game.
4. Check if the map cell is accessible and record the result.
5. Document the result in a local evidence file under .local/.

## Non-claims

- Not playable.
- Not load-tested.
- No PZ assets copied or read.
- No repo media/maps writes.
- Experimental only. No compatibility claim.
""";

    var reportMdPath = Path.Combine(outputFull, "experimental-map-export-report.md");
    File.WriteAllText(reportMdPath, md, Encoding.UTF8);

    Console.WriteLine($"Output:                          {outputFull}");
    Console.WriteLine($"Map ID:                          {mapId}");
    Console.WriteLine($"Cell:                            ({cellX}, {cellY})");
    Console.WriteLine($"Files written:                   {filesWritten.Length}");
    Console.WriteLine($"Binary files:                    3");
    Console.WriteLine($"playable_export_generated:       false");
    Console.WriteLine($"load_tested:                     false");
    Console.WriteLine($"experimental_writer:             true");
    Console.WriteLine($"pz_assets_copied:                false");
    Console.WriteLine($"media_maps_touched_in_repo:      false");
    Console.WriteLine($"manual_load_test_required:       true");
    Console.WriteLine("Status:                          OK (EXPERIMENTAL -- NOT VALIDATED)");
    return 0;
}

static int InspectBuild42ExperimentalPackageCommand(string[] args)
{
    // Self-inspection command for MAP-5D Build 42 experimental packages (MAP-5E).
    // Verifies a generated package is internally complete and writes a report.
    // Does NOT copy the package. Does NOT write to PZ folders. NOT a load test.
    var packageDir = string.Empty;
    var outputDir  = string.Empty;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--package" or "-p" && i + 1 < args.Length) packageDir = args[++i];
        else if (args[i] is "--output"  or "-o" && i + 1 < args.Length) outputDir  = args[++i];
    }

    if (string.IsNullOrWhiteSpace(packageDir))
    {
        Console.Error.WriteLine("inspect-build42-experimental-package requires --package <dir>");
        return 1;
    }
    if (string.IsNullOrWhiteSpace(outputDir))
    {
        Console.Error.WriteLine("inspect-build42-experimental-package requires --output <dir>");
        return 1;
    }

    var outputFull  = Path.GetFullPath(outputDir);
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"inspect-build42-experimental-package: refusing to write outside a .local/ directory: {outputFull}");
        return 1;
    }

    var packageFull     = Path.GetFullPath(packageDir);
    var pkgLocalMarker  = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!packageFull.Contains(pkgLocalMarker, StringComparison.OrdinalIgnoreCase) &&
        !packageFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"inspect-build42-experimental-package: --package must be under a .local/ directory: {packageFull}");
        Console.Error.WriteLine("  Only inspect packages generated by PZMapForge under .local/.");
        return 1;
    }

    if (!Directory.Exists(packageFull))
    {
        Console.Error.WriteLine(
            $"inspect-build42-experimental-package: package directory not found: {packageFull}");
        return 1;
    }

    var generatedAt     = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    var checkResults    = new System.Collections.Generic.List<object>();
    var passCount       = 0;
    var failCount       = 0;

    void Check(string name, bool condition, string detail)
    {
        var result = condition ? "PASS" : "FAIL";
        checkResults.Add(new { name, result, detail });
        if (condition) passCount++; else failCount++;
        Console.WriteLine($"  {result,-4}  {name}");
        if (!condition) Console.Error.WriteLine($"         FAIL: {detail}");
    }

    // ------------------------------------------------------------------
    // Discover report JSON by scanning Contents/mods/<id>/
    // ------------------------------------------------------------------
    var contentsModsDir = Path.Combine(packageFull, "Contents", "mods");
    JsonDocument? reportDoc   = null;
    string        mapId       = string.Empty;
    int           cellX       = 0;
    int           cellY       = 0;
    string        modRoot     = string.Empty;
    string        mapDataDir  = string.Empty;

    Check("package_root_exists", Directory.Exists(packageFull),
          $"package root directory: {packageFull}");
    Check("workshop_txt_exists", File.Exists(Path.Combine(packageFull, "workshop.txt")),
          "workshop.txt at package root");
    Check("preview_png_exists", File.Exists(Path.Combine(packageFull, "preview.png")),
          "preview.png at package root");
    Check("contents_mods_directory", Directory.Exists(contentsModsDir),
          "Contents/mods/ directory");

    if (Directory.Exists(contentsModsDir))
    {
        var modDirs = Directory.GetDirectories(contentsModsDir);
        foreach (var modDir in modDirs)
        {
            var candidate = Path.Combine(modDir, "experimental-map-export-report.json");
            if (!File.Exists(candidate)) continue;
            try
            {
                reportDoc = JsonDocument.Parse(File.ReadAllText(candidate, Encoding.UTF8));
                modRoot   = modDir;
                mapId     = reportDoc.RootElement.TryGetProperty("map_id",  out var mId)  ? mId.GetString()  ?? string.Empty : string.Empty;
                cellX     = reportDoc.RootElement.TryGetProperty("cell_x",  out var cx)   ? cx.GetInt32()    : 0;
                cellY     = reportDoc.RootElement.TryGetProperty("cell_y",  out var cy)   ? cy.GetInt32()    : 0;
                mapDataDir = Path.Combine(modRoot, "media", "maps", mapId);
                break;
            }
            catch { /* skip unparseable */ }
        }
    }

    var cellCoord = $"{cellX}_{cellY}";
    Check("report_json_found", reportDoc is not null,
          "experimental-map-export-report.json found under Contents/mods/<id>/");

    if (reportDoc is not null)
    {
        var root = reportDoc.RootElement;

        Check("report_package_layout_build42",
              root.TryGetProperty("package_layout", out var pl) && pl.GetString() == "build42_workshop",
              "package_layout == build42_workshop");
        Check("report_playable_export_generated_false",
              root.TryGetProperty("playable_export_generated", out var peg) && !peg.GetBoolean(),
              "playable_export_generated == false");
        Check("report_load_tested_false",
              root.TryGetProperty("load_tested", out var lt) && !lt.GetBoolean(),
              "load_tested == false");
        Check("report_experimental_writer_true",
              root.TryGetProperty("experimental_writer", out var ew) && ew.GetBoolean(),
              "experimental_writer == true");

        // mod-level files
        Check("mod_info_exists", File.Exists(Path.Combine(modRoot, "mod.info")),
              $"Contents/mods/{mapId}/mod.info");
        var modInfoText = File.Exists(Path.Combine(modRoot, "mod.info"))
            ? File.ReadAllText(Path.Combine(modRoot, "mod.info"), Encoding.UTF8)
            : string.Empty;
        Check("mod_info_has_category_map",
              modInfoText.Contains("category=map", StringComparison.Ordinal),
              "mod.info contains category=map");
        Check("poster_png_exists", File.Exists(Path.Combine(modRoot, "poster.png")),
              $"Contents/mods/{mapId}/poster.png");

        // map data files
        Check("map_info_exists", File.Exists(Path.Combine(mapDataDir, "map.info")),
              $"media/maps/{mapId}/map.info");
        Check("spawnpoints_lua_exists", File.Exists(Path.Combine(mapDataDir, "spawnpoints.lua")),
              $"media/maps/{mapId}/spawnpoints.lua");
        Check("objects_lua_exists", File.Exists(Path.Combine(mapDataDir, "objects.lua")),
              $"media/maps/{mapId}/objects.lua");
        Check("thumb_png_exists", File.Exists(Path.Combine(mapDataDir, "thumb.png")),
              $"media/maps/{mapId}/thumb.png");
        Check("readme_experimental_exists",
              File.Exists(Path.Combine(mapDataDir, "README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt")),
              $"media/maps/{mapId}/README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt");

        // binary files
        var lotheaderPath = Path.Combine(mapDataDir, $"{cellCoord}.lotheader");
        var lotpackPath   = Path.Combine(mapDataDir, $"world_{cellCoord}.lotpack");
        var chunkdataPath = Path.Combine(mapDataDir, $"chunkdata_{cellCoord}.bin");

        var lotheaderInfo  = File.Exists(lotheaderPath) ? new FileInfo(lotheaderPath) : null;
        var lotpackInfo    = File.Exists(lotpackPath)   ? new FileInfo(lotpackPath)   : null;
        var chunkdataInfo  = File.Exists(chunkdataPath) ? new FileInfo(chunkdataPath) : null;

        Check("lotheader_8_bytes",
              lotheaderInfo?.Length == 8,
              $"{cellCoord}.lotheader: size={lotheaderInfo?.Length ?? -1} (expected 8)");

        var lotpackOk = lotpackInfo?.Length == 7208;
        if (lotpackOk && lotpackInfo is not null)
        {
            var lpBytes = File.ReadAllBytes(lotpackPath);
            lotpackOk = lpBytes.Length >= 8 &&
                        lpBytes[0] == 0x84 && lpBytes[1] == 0x03 &&
                        lpBytes[4] == 0x24 && lpBytes[5] == 0x1C;
        }
        Check("lotpack_7208_bytes_and_header", lotpackOk,
              $"world_{cellCoord}.lotpack: size={lotpackInfo?.Length ?? -1} (expected 7208), header 84030000241c0000");

        var chunkdataOk = chunkdataInfo?.Length == 902;
        if (chunkdataOk && chunkdataInfo is not null)
        {
            var cdBytes = File.ReadAllBytes(chunkdataPath);
            chunkdataOk = cdBytes.Length >= 2 && cdBytes[0] == 0x00 && cdBytes[1] == 0x01;
        }
        Check("chunkdata_902_bytes_and_header", chunkdataOk,
              $"chunkdata_{cellCoord}.bin: size={chunkdataInfo?.Length ?? -1} (expected 902), header 0001");

        // total file count
        var actualFileCount = Directory.Exists(packageFull)
            ? Directory.GetFiles(packageFull, "*", SearchOption.AllDirectories).Length
            : 0;
        Check("total_file_count_14", actualFileCount == 14,
              $"total files under package root: {actualFileCount} (expected 14)");
    }

    var overallResult = failCount == 0 ? "PASS" : "FAIL";

    // ------------------------------------------------------------------
    // Write output report
    // ------------------------------------------------------------------
    Directory.CreateDirectory(outputFull);

    var inspectionReport = new
    {
        schema                   = "pzmapforge.build42-experimental-package-inspection.v0.1",
        claim_boundary           = "packaging_inspection_only_not_load_tested",
        generated_at_utc         = generatedAt,
        package_root             = packageFull.Replace('\\', '/'),
        map_id                   = mapId,
        overall_result           = overallResult,
        check_count              = passCount + failCount,
        passed_count             = passCount,
        failed_count             = failCount,
        checks                   = checkResults.ToArray(),
        playable_export_claimed  = false,
        load_tested              = false,
        no_files_copied          = true,
        no_pz_assets_read        = true,
    };

    var jsonOpts      = new JsonSerializerOptions { WriteIndented = true };
    var reportJsonPath = Path.Combine(outputFull, "build42-experimental-package-inspection.json");
    File.WriteAllText(reportJsonPath, JsonSerializer.Serialize(inspectionReport, jsonOpts), Encoding.UTF8);

    var checkRows = string.Join("\n", checkResults.Select(c =>
    {
        var d = (dynamic)c;
        return $"| {(string)d.result,-4} | `{(string)d.name}` | {(string)d.detail} |";
    }));

    var md = $"""
# Build 42 Experimental Package Inspection

Schema:        pzmapforge.build42-experimental-package-inspection.v0.1
Claim:         packaging_inspection_only_not_load_tested
Generated:     {generatedAt}
Package:       {packageFull.Replace('\\', '/')}
Overall:       {overallResult}
Checks:        {passCount + failCount} total, {passCount} passed, {failCount} failed

## Results

| Result | Check | Detail |
|---|---|---|
{checkRows}

## Non-claims

- Packaging inspection only. Not a load test.
- No files copied. No PZ assets read.
- No playable export claimed.
- MAP-5B remains LOAD_TEST_INCONCLUSIVE.
- Binary hypotheses remain UNTESTED.
""";
    File.WriteAllText(Path.Combine(outputFull, "build42-experimental-package-inspection.md"), md, Encoding.UTF8);

    Console.WriteLine($"Package:        {packageFull}");
    Console.WriteLine($"Checks:         {passCount + failCount}  passed={passCount}  failed={failCount}");
    Console.WriteLine($"Overall result: {overallResult}");
    Console.WriteLine($"Report:         {reportJsonPath}");
    Console.WriteLine($"playable_export_claimed:  false");
    Console.WriteLine($"load_tested:              false");
    return failCount > 0 ? 1 : 0;
}

static int Build42CandidateWriterCommand(
    string mapId, string outputFull, int cellX, int cellY, string profile)
{
    // MAP-6L: Build 42 candidate writer MVP.
    // Writes LOTP, LOTH, and chunkdata under .local using MAP-6J/MAP-6K contract.
    // Generates a versioned loose-mod layout under 42/ (confirmed by MAP-6A).
    // Profile: empty_grass_v0 — minimal all-zero/grass hypothesis.
    // Not load-tested. Not a playable export. Candidate only.

    if (profile != "empty_grass_v0" && profile != "empty_grass_v1" &&
        profile != "empty_grass_v2" && profile != "empty_grass_v3")
    {
        Console.Error.WriteLine(
            $"build42-candidate-writer: unknown profile '{profile}'. Supported profiles: empty_grass_v0, empty_grass_v1, empty_grass_v2, empty_grass_v3.");
        return 1;
    }

    var cellCoord    = $"{cellX}_{cellY}";
    var candidateDir = Path.Combine(outputFull, mapId + "_build42_candidate");
    var versionedDir = Path.Combine(candidateDir, "42");
    var mapDataDir   = Path.Combine(versionedDir, "media", "maps", mapId);
    var generatedAt  = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    Directory.CreateDirectory(versionedDir);
    Directory.CreateDirectory(mapDataDir);

    // ---- mod.info ----
    File.WriteAllText(Path.Combine(versionedDir, "mod.info"), $"""
name=PZMapForge Build42 Candidate - {mapId}
id={mapId}
description=BUILD42 CANDIDATE -- NOT VALIDATED -- NOT A PLAYABLE EXPORT. Generated by PZMapForge MAP-6L. Not load-tested. Not playable. Candidate binary format only. Do not redistribute.
category=map
modversion=1.0
pzversion=42.0
versionMin=42.0
poster=poster.png
icon=poster.png
""", Encoding.UTF8);

    // ---- Placeholder poster.png ----
    WritePlaceholderPng(Path.Combine(versionedDir, "poster.png"),
        $"PZMapForge Candidate {mapId}", "BUILD42 CANDIDATE -- NOT VALIDATED");

    // ---- map.info ----
    File.WriteAllText(Path.Combine(mapDataDir, "map.info"), $"""
title=PZMapForge Build42 Candidate - {mapId}
lots={mapId}
description=BUILD42 CANDIDATE -- NOT VALIDATED. Not a playable Project Zomboid map. Not load-tested.
fixed2x=true
""", Encoding.UTF8);

    // ---- spawnpoints.lua ----
    // Profile empty_grass_v3 (MAP-7C): uses unemployed key and explicit PZ spawn fields.
    //   MAP-7A retest showed spawn NullPointerException in getSpawnRegionsAux.
    //   The 'all' key used in v0-v2 may not be valid; 'unemployed' is a known PZ profession key.
    if (profile == "empty_grass_v3")
    {
        File.WriteAllText(Path.Combine(mapDataDir, "spawnpoints.lua"), $$"""
-- PZMapForge MAP-7C: candidate spawn point for experimental empty cell.
-- Not load-tested. Not a playable Project Zomboid map.
function SpawnPoints()
    return {
        unemployed = {
            { worldX = {{cellX}}, worldY = {{cellY}}, posX = 150, posY = 150, posZ = 0 },
        },
    }
end
""", Encoding.UTF8);
    }
    else
    {
        File.WriteAllText(Path.Combine(mapDataDir, "spawnpoints.lua"), $$"""
-- PZMapForge MAP-6L Build42 candidate output -- NOT VALIDATED -- not a playable Project Zomboid map.
-- Spawn point is candidate-only. Not load-tested. Coordinates not verified.
function SpawnPoints()
return {
  all = {
    { worldX = {{cellX}}, worldY = {{cellY}}, posX = 128, posY = 128, posZ = 0 }
  },
}
end
""", Encoding.UTF8);
    }

    // ---- objects.lua ----
    // Profile empty_grass_v3 (MAP-7C): comment-only to avoid the return {} Lua lexer issue from MAP-7A retest.
    //   MAP-7A: LexState.token2str ArrayIndexOutOfBoundsException index 65022 on return {}.
    //   Comment-only avoids any evaluable Lua token while still being a valid Lua file.
    var objectsLuaContent = profile == "empty_grass_v3"
        ? "-- PZMapForge MAP-7C: no objects or zones for this experimental empty cell.\n-- objects.lua is a placeholder. Not load-tested. Not a playable Project Zomboid map.\n"
        : "return {}\n";
    File.WriteAllText(Path.Combine(mapDataDir, "objects.lua"), objectsLuaContent, Encoding.UTF8);

    // ---- thumb.png ----
    WritePlaceholderPng(Path.Combine(mapDataDir, "thumb.png"),
        mapId, "Build42 candidate");

    // ---- README boundary ----
    File.WriteAllText(Path.Combine(mapDataDir, "README_PZMAPFORGE_BOUNDARY_BUILD42_CANDIDATE.txt"), $"""
BUILD42 CANDIDATE -- NOT VALIDATED
====================================

Generated by PZMapForge MAP-6L (Build 42 candidate writer MVP).
Profile: {profile}
Generated at: {generatedAt}
Map ID: {mapId}
Cell: ({cellX}, {cellY})

BOUNDARY STATEMENT:
- NOT a playable Project Zomboid map.
- NOT load-tested.
- Binary files are candidate-only, generated from MAP-6J/MAP-6K evidence.
- Do not redistribute.
- Do not claim Project Zomboid compatibility.
- No PZ assets copied or read.
- Manual load test required before any claim changes.
- Output is under .local only.

BINARY CANDIDATE FORMATS ({profile}):
- chunkdata: 1026 bytes, header 00 01, 1024-byte all-zero body (MAP-6H evidence).
- lotheader: LOTH magic, version 1, {profile switch { "empty_grass_v3" => "1024 generated entries + 1048-byte stable trailer (MAP-6Z/MAP-7C)", "empty_grass_v2" => "1024 generated entries + 1048-byte stable trailer from MAP-6Y research (MAP-6Z)", "empty_grass_v1" => "1024 generated entries blends_grassoverlays_01_0..._01_1023 (MAP-6S)", _ => "1 entry blends_grassoverlays_01_0 (MAP-4E committed evidence only)" }}.
- objects.lua: {(profile == "empty_grass_v3" ? "comment-only placeholder (MAP-7C: avoids MAP-7A Lua lexer error)" : "return {} (candidate, may need fix)") }
- spawnpoints.lua: {(profile == "empty_grass_v3" ? "unemployed key format (MAP-7C: explicit spawn profession)" : "all key format (candidate)")}.
- lotpack: LOTP magic, version 1, 1024 chunks x 1024 zero bytes (MAP-6K most_common_size).
""", Encoding.UTF8);

    // ---- chunkdata_x_y.bin (MAP-6L: 1026 bytes, 00 01 header + 1024 zero bytes) ----
    var chunkdataBytes = new byte[1026];
    chunkdataBytes[0] = 0x00;
    chunkdataBytes[1] = 0x01;
    // bytes 2-1025: all zero (body 1024 bytes, all-zero hypothesis from MAP-6H)
    File.WriteAllBytes(Path.Combine(mapDataDir, $"chunkdata_{cellCoord}.bin"), chunkdataBytes);

    // ---- 0_0.lotheader ----
    // Profile empty_grass_v0 (MAP-6L): 1 committed entry from MAP-4E evidence.
    // Profile empty_grass_v1 (MAP-6S): 1024 generated contiguous grass overlay entries.
    //   Entry range: blends_grassoverlays_01_0 ... blends_grassoverlays_01_1023.
    //   Entries are generated -- not copied from any reference mod. Candidate only.
    //   loth_known_risk: generated_entries_may_not_match_loaded_tile_definitions
    // Profile empty_grass_v2 (MAP-6Z): same 1024 entries as v1 + canonical 1048-byte trailer.
    //   Trailer is the MAP-6Y stable literal block (80 Dru_map simple cells, all identical).
    //   loth_known_risk: stable_reference_block_may_not_match_generated_tile_table_or_cell_payload
    var lothEntries = (profile == "empty_grass_v1" || profile == "empty_grass_v2" || profile == "empty_grass_v3")
        ? Enumerable.Range(0, 1024).Select(i => $"blends_grassoverlays_01_{i}").ToArray()
        : new[] { "blends_grassoverlays_01_0" }; // MAP-4E committed evidence
    var lothEntryData = Encoding.ASCII.GetBytes(string.Join("\n", lothEntries) + "\n");
    // MAP-6Z: canonical 1048-byte simple-cell trailer from MAP-6Y reference research.
    // Source: 80 Dru_map simple cells (all_1048_blocks_identical=true). First two U32LE=8, rest zero.
    // SHA-256: 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7
    // Not copied from PZ game assets. Derived from MAP-6Y analysis. Candidate only.
    var lothTrailer = (profile == "empty_grass_v2" || profile == "empty_grass_v3") ? BuildMap6yCanonicalTrailer() : Array.Empty<byte>();
    var lothBytes   = new byte[12 + lothEntryData.Length + lothTrailer.Length];
    lothBytes[0] = 0x4C; lothBytes[1] = 0x4F; lothBytes[2] = 0x54; lothBytes[3] = 0x48; // LOTH
    lothBytes[4] = 0x01; // version = 1 (LE)
    var entryCountLeBytes = BitConverter.GetBytes((uint)lothEntries.Length);
    Array.Copy(entryCountLeBytes, 0, lothBytes, 8, 4);
    Array.Copy(lothEntryData, 0, lothBytes, 12, lothEntryData.Length);
    if (lothTrailer.Length > 0)
        Array.Copy(lothTrailer, 0, lothBytes, 12 + lothEntryData.Length, lothTrailer.Length);
    File.WriteAllBytes(Path.Combine(mapDataDir, $"{cellCoord}.lotheader"), lothBytes);

    // ---- world_x_y.lotpack (MAP-6L: LOTP format, 1024 chunks x 1024 zero bytes) ----
    // Contract (MAP-6J): header(12) + offset_table(1024*8=8192) + payload(1024*1024)
    // = 8204 + 1024*1024 = 1,056,780 bytes
    const int lotpChunkCount    = 1024;
    const int lotpChunkPayload  = 1024; // zero bytes per chunk
    const int lotpHeaderSize    = 12;
    const int lotpTableSize     = lotpChunkCount * 8;   // 8192
    const int lotpFirstOffset   = lotpHeaderSize + lotpTableSize; // 8204
    const int lotpExpectedSize  = lotpFirstOffset + lotpChunkCount * lotpChunkPayload; // 1,056,780

    var lotpBytes = new byte[lotpExpectedSize];
    // Header
    lotpBytes[0] = 0x4C; lotpBytes[1] = 0x4F; lotpBytes[2] = 0x54; lotpBytes[3] = 0x50; // LOTP
    lotpBytes[4] = 0x01; // version = 1 (LE byte 0)
    // chunk_count = 1024 as U32 LE: 0x00 0x04 0x00 0x00
    lotpBytes[8] = 0x00; lotpBytes[9] = 0x04; lotpBytes[10] = 0x00; lotpBytes[11] = 0x00;
    // Offset table: sequential U64 LE offsets, one per chunk
    for (var i = 0; i < lotpChunkCount; i++)
    {
        var entryPos = lotpHeaderSize + i * 8;
        var offset   = (long)lotpFirstOffset + (long)i * lotpChunkPayload;
        var offBytes = BitConverter.GetBytes(offset); // little-endian U64
        Array.Copy(offBytes, 0, lotpBytes, entryPos, 8);
    }
    // Payload: all-zero (already initialised by new byte[])
    File.WriteAllBytes(Path.Combine(mapDataDir, $"world_{cellCoord}.lotpack"), lotpBytes);

    // ---- Report JSON ----
    var lothSha256 = string.Join("", SHA256.HashData(lothBytes).Select(b => b.ToString("x2")));
    var lotpSha256 = string.Join("", SHA256.HashData(lotpBytes).Select(b => b.ToString("x2")));
    var cdataSha256= string.Join("", SHA256.HashData(chunkdataBytes).Select(b => b.ToString("x2")));

    var filesWritten = new[]
    {
        "mod.info", "poster.png",
        $"media/maps/{mapId}/map.info",
        $"media/maps/{mapId}/spawnpoints.lua",
        $"media/maps/{mapId}/objects.lua",
        $"media/maps/{mapId}/thumb.png",
        $"media/maps/{mapId}/README_PZMAPFORGE_BOUNDARY_BUILD42_CANDIDATE.txt",
        $"media/maps/{mapId}/{cellCoord}.lotheader",
        $"media/maps/{mapId}/world_{cellCoord}.lotpack",
        $"media/maps/{mapId}/chunkdata_{cellCoord}.bin",
        "experimental-map-export-report.json",
        "experimental-map-export-report.md",
    };

    var report = new
    {
        schema                       = "pzmapforge.build42-candidate-report.v0.1",
        claim_boundary               = "build42_candidate_only_not_load_tested_not_playable",
        generated_at_utc             = generatedAt,
        map_id                       = mapId,
        cell_x                       = cellX,
        cell_y                       = cellY,
        candidate_dir                = candidateDir.Replace('\\', '/'),
        build42_candidate_writer     = true,
        build42_candidate_profile    = profile,
        writer_implemented           = true,
        writer_scope                 = "candidate_only_not_load_tested",
        load_tested                  = false,
        playable_export_generated    = false,
        playable_export_claimed      = false,
        pz_assets_copied             = false,
        pz_assets_read               = false,
        media_maps_touched_in_repo   = false,
        files_written                = filesWritten,
        file_count                   = filesWritten.Length,
        chunkdata_candidate          = "zero_body_1024",
        chunkdata_status             = "generated_not_load_tested",
        chunkdata_size_bytes         = 1026,
        chunkdata_sha256             = cdataSha256,
        loth_candidate_profile       = profile,
        loth_entry_count             = lothEntries.Length,
        loth_entries                 = lothEntries.Length > 3
            ? new[] { lothEntries[0], $"...({lothEntries.Length - 2} more)...", lothEntries[^1] }
            : lothEntries,
        loth_entries_source          = (profile == "empty_grass_v1" || profile == "empty_grass_v2" || profile == "empty_grass_v3")
            ? "generated_contiguous_range_not_copied_from_reference"
            : "committed_evidence_only_map4e",
        loth_entry_strategy          = profile switch
        {
            "empty_grass_v3" => "generated_contiguous_grass_overlay_range_with_map6y_stable_trailer_and_fixed_lua_metadata",
            "empty_grass_v2" => "generated_contiguous_grass_overlay_range_with_map6y_stable_trailer",
            "empty_grass_v1" => "generated_contiguous_grass_overlay_range",
            _                => "single_committed_entry_map4e",
        },
        loth_known_risk              = profile switch
        {
            "empty_grass_v3" or "empty_grass_v2" => "stable_reference_block_may_not_match_generated_tile_table_or_cell_payload",
            "empty_grass_v1" => "generated_entries_may_not_match_loaded_tile_definitions",
            _                => "single_entry_insufficient_per_map6r_evidence",
        },
        loth_status                  = "generated_not_load_tested",
        loth_size_bytes              = lothBytes.Length,
        loth_sha256                  = lothSha256,
        loth_trailer_strategy        = (profile == "empty_grass_v2" || profile == "empty_grass_v3")
            ? "map6y_stable_literal_1048_block"
            : "none_no_trailer",
        loth_trailer_size            = lothTrailer.Length,
        loth_trailer_status          = (profile == "empty_grass_v2" || profile == "empty_grass_v3")
            ? "generated_not_load_tested"
            : "not_applicable",
        loth_trailer_sha256          = lothTrailer.Length > 0
            ? string.Join("", SHA256.HashData(lothTrailer).Select(b => b.ToString("x2")))
            : "",
        lua_metadata_strategy        = profile == "empty_grass_v3"
            ? "objects_lua_comment_only"
            : "return_empty_table",
        objects_lua_strategy         = profile == "empty_grass_v3"
            ? "comment_only_lua"
            : "return_empty_table",
        objects_lua_known_risk       = profile == "empty_grass_v3"
            ? "build42_may_expect_specific_zone_table_format"
            : "return_table_led_to_lexer_exception_in_map7a",
        spawnpoints_strategy         = profile == "empty_grass_v3"
            ? "minimal_unemployed_spawnpoint"
            : "all_key_spawn_point",
        lotp_candidate_profile       = profile,
        lotp_payload_strategy        = "uniform_zero_1024_per_chunk",
        lotp_offset_strategy         = "sequential_u64_offsets",
        lotp_chunk_count             = lotpChunkCount,
        lotp_chunk_payload_bytes     = lotpChunkPayload,
        lotp_first_offset            = lotpFirstOffset,
        lotp_file_size_expected      = lotpExpectedSize,
        lotp_status                  = "generated_not_load_tested",
        lotp_sha256                  = lotpSha256,
        geometry_model               = "32x32_chunk_grid_256x256_cell",
        geometry_status              = "strongly_supported_not_load_tested",
        remaining_unknowns = profile switch
        {
            "empty_grass_v3" => new[] { "objects_lua_format_acceptance", "spawn_region_null_cause", "loth_trailer_acceptance_at_eof", "lotp_zero_payload_load_acceptance", "chunkdata_zero_body_acceptance", "build42_load_test" },
            "empty_grass_v2" => new[] { "loth_generated_entry_acceptance", "loth_trailer_acceptance_at_eof", "lotp_zero_payload_load_acceptance", "chunkdata_zero_body_acceptance", "build42_load_test" },
            "empty_grass_v1" => new[] { "loth_generated_entry_acceptance", "lotp_zero_payload_load_acceptance", "chunkdata_zero_body_acceptance", "build42_load_test" },
            _                => new[] { "lotp_zero_payload_load_acceptance", "loth_minimum_entries_acceptance", "missing_trailer_acceptance", "build42_load_test" },
        },
    };

    var jsonOpts = new JsonSerializerOptions { WriteIndented = true };
    var reportPath = Path.Combine(versionedDir, "experimental-map-export-report.json");
    File.WriteAllText(reportPath, JsonSerializer.Serialize(report, jsonOpts), Encoding.UTF8);

    // ---- Report MD ----
    var mdRemainingUnknowns = profile switch
    {
        "empty_grass_v3" => "- objects_lua_format_acceptance\n- spawn_region_null_cause\n- loth_trailer_acceptance_at_eof\n- lotp_zero_payload_load_acceptance\n- chunkdata_zero_body_acceptance\n- build42_load_test",
        "empty_grass_v2" => "- loth_generated_entry_acceptance\n- loth_trailer_acceptance_at_eof\n- lotp_zero_payload_load_acceptance\n- chunkdata_zero_body_acceptance\n- build42_load_test",
        "empty_grass_v1" => "- loth_generated_entry_acceptance\n- lotp_zero_payload_load_acceptance\n- chunkdata_zero_body_acceptance\n- build42_load_test",
        _                => "- lotp_zero_payload_load_acceptance\n- loth_minimum_entries_acceptance\n- missing_trailer_acceptance\n- build42_load_test",
    };
    var md = $"""
# Build 42 Candidate Writer Report

Schema:    pzmapforge.build42-candidate-report.v0.1
Profile:   {profile}
Generated: {generatedAt}
Map ID:    {mapId}
Cell:      ({cellX}, {cellY})

## BOUNDARY

**BUILD42 CANDIDATE -- NOT VALIDATED**

Not a playable Project Zomboid map.
Not load-tested. Not a playable export. Candidate binary format only.
No PZ assets copied. No repo media/maps writes. Experimental only.

## Binary candidates

| File | Size | Format | Status |
|---|---|---|---|
| {cellCoord}.lotheader | {lothBytes.Length} | LOTH magic+version+{lothEntries.Length} {(lothEntries.Length == 1 ? "entry" : "entries")}{(lothTrailer.Length > 0 ? $"+{lothTrailer.Length}-byte stable trailer" : "")} | generated_not_load_tested |
| world_{cellCoord}.lotpack | {lotpExpectedSize} | LOTP magic+version+1024 chunks | generated_not_load_tested |
| chunkdata_{cellCoord}.bin | 1026 | 00 01 header + 1024 zero bytes | generated_not_load_tested |

## Remaining unknowns

{mdRemainingUnknowns}

## Non-claims

- Not playable.
- Not load-tested.
- No PZ assets copied or read.
- No repo media/maps writes.
- Candidate only. No compatibility claim.
""";
    File.WriteAllText(Path.Combine(versionedDir, "experimental-map-export-report.md"), md, Encoding.UTF8);

    Console.WriteLine($"Candidate dir:                   {candidateDir}");
    Console.WriteLine($"Profile:                         {profile}");
    Console.WriteLine($"Map ID:                          {mapId}");
    Console.WriteLine($"Cell:                            ({cellX}, {cellY})");
    Console.WriteLine($"lotheader size:                  {lothBytes.Length} bytes");
    Console.WriteLine($"lotpack size:                    {lotpExpectedSize} bytes");
    Console.WriteLine($"chunkdata size:                  1026 bytes");
    Console.WriteLine($"build42_candidate_writer:        true");
    Console.WriteLine($"writer_scope:                    candidate_only_not_load_tested");
    Console.WriteLine($"load_tested:                     false");
    Console.WriteLine($"playable_export_generated:       false");
    Console.WriteLine($"playable_export_claimed:         false");
    Console.WriteLine("Status:                          OK (BUILD42 CANDIDATE -- NOT VALIDATED)");
    return 0;
}

static (byte[] Bytes, string CandidateStatus, int EntryCount, string[] Entries)
    BuildLotheaderForCandidate(string candidate)
{
    // MAP-6C/MAP-6D candidate writer gate (MAP-4E format model).
    // Format: bytes 0-3 = version/reserved (U32=0, consistent in 16/16 observed files),
    //         bytes 4-7 = entry count (U32 LE),
    //         bytes 8+  = newline-terminated ASCII tileset pack names.
    // candidate_v0 (current_failed): known failing (MAP-6B: EOFException at IsoLot.readInt).
    // candidate_v1 (newline_tileset_table): MAP-4E format, 0 entries, same bytes as v0.
    // candidate_v2 (newline_tileset_table_minimal): MAP-4E format, 1 grass entry from MAP-4E evidence.
    const string grassEntry = "blends_grassoverlays_01_0"; // documented in MAP-4E evidence

    if (candidate == "newline_tileset_table_minimal")
    {
        // bytes 0-3: 00 00 00 00 (version = 0)
        // bytes 4-7: 01 00 00 00 (count = 1, U32 LE)
        // bytes 8+:  "blends_grassoverlays_01_0\n" (25 ASCII bytes + 0x0A newline = 26 bytes)
        // Total: 34 bytes
        var entryBytes = Encoding.ASCII.GetBytes(grassEntry + "\n");
        var bytes = new byte[8 + entryBytes.Length];
        bytes[4] = 1; // count = 1 (little-endian, byte index 0 of U32)
        entryBytes.CopyTo(bytes, 8);
        return (bytes, "generated_not_load_tested", 1, new[] { grassEntry });
    }

    var zeroBytes = new byte[8]; // version(U32=0) + count(U32LE=0) + empty entry table
    return candidate switch
    {
        "newline_tileset_table" => (zeroBytes, "generated_not_load_tested", 0, Array.Empty<string>()),
        _                       => (zeroBytes, "known_failing",             0, Array.Empty<string>()),
    };
}

static void WritePlaceholderPng(string path, string line1, string line2)
{
    // Generates a 256x64 solid-colour placeholder PNG with two text lines.
    // Uses System.Drawing which is already available in this project.
    using var bmp  = new System.Drawing.Bitmap(256, 64);
    using var g    = System.Drawing.Graphics.FromImage(bmp);
    g.Clear(System.Drawing.Color.FromArgb(255, 28, 48, 76));
    using var font = new System.Drawing.Font("Arial", 7f);
    g.DrawString(line1, font, System.Drawing.Brushes.White, 4f, 6f);
    if (!string.IsNullOrEmpty(line2))
        g.DrawString(line2, font, System.Drawing.Brushes.LightGray, 4f, 22f);
    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
}

// MAP-6Z: canonical 1048-byte simple-cell trailer from MAP-6Y reference research.
// Source: 80 Dru_map reference lotheader files, all_1048_blocks_identical=true (MAP-6Y).
// Structure: bytes 0-3 = U32LE 8, bytes 4-7 = U32LE 8, bytes 8-1047 = zero.
// SHA-256: 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7
// Not copied from PZ game assets. Derived from MAP-6Y analysis of reference data.
// Applies only to the experimental simple-cell candidate profile empty_grass_v2.
static byte[] BuildMap6yCanonicalTrailer()
{
    var t = new byte[1048];
    t[0] = 0x08; // first U32LE = 8
    t[4] = 0x08; // second U32LE = 8
    // bytes 8-1047: zero (initialized by new byte[])
    return t;
}

static int ImageCheckCommand(string[] args)
{
    var imagePath   = string.Empty;
    var palettePath = string.Empty;
    var resize      = false;

    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] is "--path"    or "-p" && i + 1 < args.Length) imagePath   = args[++i];
        else if (args[i] is "--palette" && i + 1 < args.Length)     palettePath = args[++i];
        else if (args[i] is "--resize")                              resize      = true;
    }

    if (string.IsNullOrWhiteSpace(imagePath))
    {
        Console.Error.WriteLine("image-check requires --path <image>");
        return 1;
    }
    if (string.IsNullOrWhiteSpace(palettePath))
    {
        Console.Error.WriteLine("image-check requires --palette <palette>");
        return 1;
    }

    ImageMapForgeResult result;
    try
    {
        var opts = new ImageMapForgeOptions { Resize = resize };
        result   = ImageMapForgeParser.Parse(imagePath, palettePath, opts);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Status:          INVALID");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    Console.WriteLine($"Image:           {imagePath}");
    Console.WriteLine($"Palette:         {palettePath}");
    Console.WriteLine($"Dimensions:      {result.Width}x{result.Height}");
    Console.WriteLine($"Resized:         {result.Resized}");
    Console.WriteLine($"Row count:       {result.Rows.Count}");
    Console.WriteLine($"Kind count:      {result.Counts.Count(c => c.Pixels > 0)}");
    Console.WriteLine($"Exact pixels:    {result.Matching.ExactPixels}");
    Console.WriteLine($"Nearest pixels:  {result.Matching.NearestPixels}");
    Console.WriteLine($"Unmapped:        {result.Matching.UnmappedExactColours}");
    Console.WriteLine($"Palette SHA-256: {result.PaletteSha256}");
    Console.WriteLine("Status:          OK");
    return 0;
}

static int ImageExportCommand(string[] args)
{
    var imagePath   = string.Empty;
    var palettePath = string.Empty;
    var outputDir   = string.Empty;
    var resize      = false;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--path"    or "-p" && i + 1 < args.Length) imagePath   = args[++i];
        else if (args[i] is "--palette" && i + 1 < args.Length)          palettePath = args[++i];
        else if (args[i] is "--output"  or "-o" && i + 1 < args.Length) outputDir   = args[++i];
        else if (args[i] is "--resize")                                   resize      = true;
    }

    if (string.IsNullOrWhiteSpace(imagePath))
    {
        Console.Error.WriteLine("image-export requires --path <image>");
        return 1;
    }
    if (string.IsNullOrWhiteSpace(palettePath))
    {
        Console.Error.WriteLine("image-export requires --palette <palette>");
        return 1;
    }

    // Default output: .local/mapforge relative to CWD
    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Path.Combine(Directory.GetCurrentDirectory(), ".local", "mapforge");

    // Safety: output must be inside a .local/ directory
    var outputFull  = Path.GetFullPath(outputDir);
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"image-export: refusing to write outside a .local/ directory: {outputFull}");
        return 1;
    }

    // Parse image
    ImageMapForgeResult parseResult;
    PaletteLoader.Load(palettePath); // early validation — actual load happens inside parser
    try
    {
        var opts    = new ImageMapForgeOptions { Resize = resize };
        parseResult = ImageMapForgeParser.Parse(imagePath, palettePath, opts);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status:   INVALID");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    // Load palette document for legend building
    var paletteResult = PaletteLoader.Load(palettePath);
    if (!paletteResult.IsValid)
    {
        Console.WriteLine("Status:   INVALID (palette)");
        foreach (var e in paletteResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }

    // Write artifact
    var jsonPath = ImageMapForgeArtifactWriter.Write(
        outputFull, imagePath, palettePath, paletteResult.Document!, parseResult);

    Console.WriteLine($"Parsed cell: {jsonPath}");
    Console.WriteLine($"Dimensions:  {parseResult.Width}x{parseResult.Height}");
    Console.WriteLine($"Resized:     {parseResult.Resized}");
    Console.WriteLine($"Kinds:       {parseResult.Counts.Count(c => c.Pixels > 0)}");
    Console.WriteLine($"Pixels:      {parseResult.Counts.Sum(c => c.Pixels)}");
    Console.WriteLine("Status:      OK");
    return 0;
}

static int FullPipelineCommand(string[] args)
{
    var imagePath   = string.Empty;
    var palettePath = string.Empty;
    var outputDir   = string.Empty;
    var resize      = false;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--path"    or "-p" && i + 1 < args.Length) imagePath   = args[++i];
        else if (args[i] is "--palette" && i + 1 < args.Length)          palettePath = args[++i];
        else if (args[i] is "--output"  or "-o" && i + 1 < args.Length) outputDir   = args[++i];
        else if (args[i] is "--resize")                                   resize      = true;
    }

    if (string.IsNullOrWhiteSpace(imagePath))   { Console.Error.WriteLine("full-pipeline requires --path <image>");   return 1; }
    if (string.IsNullOrWhiteSpace(palettePath)) { Console.Error.WriteLine("full-pipeline requires --palette <palette>"); return 1; }

    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Path.Combine(Directory.GetCurrentDirectory(), ".local", "mapforge");

    var outputFull  = Path.GetFullPath(outputDir);
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"full-pipeline: refusing to write outside a .local/ directory: {outputFull}");
        return 1;
    }

    var (planOpts, planErr) = ParsePlanningOptions(args);
    if (planErr != 0) return planErr;

    // --- Step 1: parse image ---
    ImageMapForgeResult parseResult;
    try
    {
        var imgOpts = new ImageMapForgeOptions { Resize = resize };
        parseResult = ImageMapForgeParser.Parse(imagePath, palettePath, imgOpts);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status:           INVALID (image parse failed)");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    // --- Step 2: write parsed-cell.json ---
    var paletteResult = PaletteLoader.Load(palettePath);
    if (!paletteResult.IsValid)
    {
        Console.WriteLine("Status:           INVALID (palette)");
        foreach (var e in paletteResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }
    var parsedCellPath = ImageMapForgeArtifactWriter.Write(
        outputFull, imagePath, palettePath, paletteResult.Document!, parseResult);

    // --- Step 3: build SemanticGrid ---
    var cellResult = ParsedCellLoader.Load(parsedCellPath);
    if (!cellResult.IsValid)
    {
        Console.WriteLine("Status:           INVALID (parsed-cell load failed)");
        foreach (var e in cellResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }
    var grid      = cellResult.Grid!;
    var codeToKind = cellResult.Document!.Counts
        .ToDictionary(c => c.Code[0], c => c.Kind)
        .AsReadOnly();

    // --- Steps 4-6: regions, primitives, planning ---
    PlanningRuleResult planResult;
    RegionExtractionResult regions;
    PrimitiveClassificationResult primitives;
    try
    {
        regions    = RegionExtractor.Extract(grid, codeToKind);
        primitives = PrimitiveClassifier.Classify(regions);
        planResult = PlanningRuleEngine.Evaluate(primitives, planOpts!);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status:           INVALID (pipeline failed)");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    // --- Step 7: write regions.json + regions-report.md ---
    var (regionsJsonPath, regionsMdPath) = RegionArtifactWriter.Write(
        outputFull, grid.Width, grid.Height,
        Path.GetFullPath(parsedCellPath), regions);

    // --- Step 8: write primitives.json + primitives-report.md ---
    var (primitivesJsonPath, primitivesMdPath) = PrimitiveArtifactWriter.Write(
        outputFull, grid.Width, grid.Height,
        regionsJsonPath, primitives);

    // --- Step 9: write plan artifacts ---
    var (planJsonPath, planMdPath) = PlanningArtifactWriter.Write(
        outputFull, grid.Width, grid.Height,
        Path.GetFullPath(parsedCellPath), "PZMapForge.Cli full-pipeline",
        planResult, planOpts);

    Console.WriteLine($"Parsed cell:      {parsedCellPath}");
    Console.WriteLine($"Regions JSON:     {regionsJsonPath}");
    Console.WriteLine($"Regions report:   {regionsMdPath}");
    Console.WriteLine($"Primitives JSON:  {primitivesJsonPath}");
    Console.WriteLine($"Primitives report:{primitivesMdPath}");
    Console.WriteLine($"Plan JSON:        {planJsonPath}");
    Console.WriteLine($"Plan report:      {planMdPath}");
    Console.WriteLine($"Dimensions:       {grid.Width}x{grid.Height}");
    Console.WriteLine($"Resized:          {parseResult.Resized}");
    Console.WriteLine($"Regions:          {regions.TotalRegions}");
    Console.WriteLine($"Primitives:       {primitives.PrimitiveCount}");
    Console.WriteLine($"Recommendations:  {planResult.RecommendationCount}");
    Console.WriteLine($"Warnings:         {planResult.Summary.WarningCount}");
    Console.WriteLine($"Tiny threshold:   {planOpts!.TinyBuildingPixelThreshold}");
    Console.WriteLine($"Large threshold:  {planOpts!.LargeGroundPixelThreshold}");
    Console.WriteLine("Status:           OK");
    return 0;
}

/// <summary>
/// Parses optional --tiny-threshold and --large-threshold from args.
/// Returns (options, 0) on success, (null, 1) on any parse or validation error.
/// </summary>
static (PlanningRuleOptions? Options, int ErrorCode) ParsePlanningOptions(string[] args)
{
    int? tinyThreshold  = null;
    int? largeThreshold = null;

    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == "--tiny-threshold")
        {
            if (!int.TryParse(args[i + 1], out var v))
            {
                Console.Error.WriteLine($"--tiny-threshold: expected integer, got '{args[i + 1]}'");
                return (null, 1);
            }
            tinyThreshold = v;
        }
        if (args[i] == "--large-threshold")
        {
            if (!int.TryParse(args[i + 1], out var v))
            {
                Console.Error.WriteLine($"--large-threshold: expected integer, got '{args[i + 1]}'");
                return (null, 1);
            }
            largeThreshold = v;
        }
    }

    try
    {
        var opts = new PlanningRuleOptions(
            tinyBuildingPixelThreshold: tinyThreshold  ?? PlanningRuleOptions.Default.TinyBuildingPixelThreshold,
            largeGroundPixelThreshold:  largeThreshold ?? PlanningRuleOptions.Default.LargeGroundPixelThreshold);
        return (opts, 0);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        var param = ex.ParamName ?? "threshold";
        Console.Error.WriteLine($"Invalid threshold value ({param}): must be >= 0.");
        return (null, 1);
    }
}

static int LayerPipelineCommand(string[] args)
{
    var manifestPath = string.Empty;
    var palettePath  = string.Empty;
    var outputDir    = string.Empty;
    var resize       = false;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--layers"  or "-l" && i + 1 < args.Length) manifestPath = args[++i];
        else if (args[i] is "--palette"          && i + 1 < args.Length) palettePath  = args[++i];
        else if (args[i] is "--output"  or "-o" && i + 1 < args.Length) outputDir    = args[++i];
        else if (args[i] is "--resize")                                   resize       = true;
    }

    if (string.IsNullOrWhiteSpace(manifestPath))
    { Console.Error.WriteLine("layer-pipeline requires --layers <manifest>"); return 1; }
    if (string.IsNullOrWhiteSpace(palettePath))
    { Console.Error.WriteLine("layer-pipeline requires --palette <palette>"); return 1; }

    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Path.Combine(Directory.GetCurrentDirectory(), ".local", "mapforge");

    var outputFull  = Path.GetFullPath(outputDir);
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"layer-pipeline: refusing to write outside a .local/ directory: {outputFull}");
        return 1;
    }

    var (planOpts, planErr) = ParsePlanningOptions(args);
    if (planErr != 0) return planErr;

    // --- Step 1: load palette ---
    var paletteResult = PaletteLoader.Load(palettePath);
    if (!paletteResult.IsValid)
    {
        Console.WriteLine("Status: INVALID (palette)");
        foreach (var e in paletteResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }

    // --- Step 2: merge layers ---
    var mergeOpts = new LayerMergeOptions { Resize = resize };
    var mergeResult = LayerMerger.Merge(manifestPath, palettePath, mergeOpts);
    if (!mergeResult.IsValid)
    {
        Console.WriteLine("Status: INVALID (layer merge failed)");
        foreach (var e in mergeResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }

    // --- Step 3: write parsed-cell.json + layer-merge-report.md ---
    var (parsedCellPath, mergeMdPath) = LayerMergeArtifactWriter.Write(
        outputFull, manifestPath, palettePath, paletteResult.Document!, mergeResult, mergeOpts);

    // --- Step 4: load parsed-cell.json for downstream pipeline ---
    var cellResult = ParsedCellLoader.Load(parsedCellPath);
    if (!cellResult.IsValid)
    {
        Console.WriteLine("Status: INVALID (parsed-cell load failed)");
        foreach (var e in cellResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }
    var grid       = cellResult.Grid!;
    var codeToKind = cellResult.Document!.Counts
        .ToDictionary(c => c.Code[0], c => c.Kind)
        .AsReadOnly();

    // --- Steps 5-7: regions, primitives, planning ---
    RegionExtractionResult       regions;
    PrimitiveClassificationResult primitives;
    PlanningRuleResult           planResult;
    try
    {
        regions    = RegionExtractor.Extract(grid, codeToKind);
        primitives = PrimitiveClassifier.Classify(regions);
        planResult = PlanningRuleEngine.Evaluate(primitives, planOpts!);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status: INVALID (pipeline failed)");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    // --- Step 8: write region artifacts ---
    var (regionsJsonPath, regionsMdPath) = RegionArtifactWriter.Write(
        outputFull, grid.Width, grid.Height,
        Path.GetFullPath(parsedCellPath), regions);

    // --- Step 9: write primitive artifacts ---
    var (primitivesJsonPath, primitivesMdPath) = PrimitiveArtifactWriter.Write(
        outputFull, grid.Width, grid.Height,
        regionsJsonPath, primitives);

    // --- Step 10: write plan artifacts ---
    var (planJsonPath, planMdPath) = PlanningArtifactWriter.Write(
        outputFull, grid.Width, grid.Height,
        Path.GetFullPath(parsedCellPath), "PZMapForge.Cli layer-pipeline",
        planResult, planOpts);

    Console.WriteLine($"Parsed cell:        {parsedCellPath}");
    Console.WriteLine($"Merge report:       {mergeMdPath}");
    Console.WriteLine($"Regions JSON:       {regionsJsonPath}");
    Console.WriteLine($"Regions report:     {regionsMdPath}");
    Console.WriteLine($"Primitives JSON:    {primitivesJsonPath}");
    Console.WriteLine($"Primitives report:  {primitivesMdPath}");
    Console.WriteLine($"Plan JSON:          {planJsonPath}");
    Console.WriteLine($"Plan report:        {planMdPath}");
    Console.WriteLine($"Dimensions:         {grid.Width}x{grid.Height}");
    Console.WriteLine($"Layers merged:      {mergeResult.Contributions.Count}");
    Console.WriteLine($"Conflicts:          {mergeResult.TotalConflictCount}");
    Console.WriteLine($"Regions:            {regions.TotalRegions}");
    Console.WriteLine($"Primitives:         {primitives.PrimitiveCount}");
    Console.WriteLine($"Recommendations:    {planResult.RecommendationCount}");
    Console.WriteLine($"Warnings:           {planResult.Summary.WarningCount}");
    Console.WriteLine("Status:             OK");
    return 0;
}

static int LayerValidateCommand(string[] args)
{
    var manifestPath = string.Empty;
    var palettePath  = string.Empty;
    var resize       = false;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--layers"  or "-l" && i + 1 < args.Length) manifestPath = args[++i];
        else if (args[i] is "--palette"          && i + 1 < args.Length) palettePath  = args[++i];
        else if (args[i] is "--resize")                                   resize       = true;
    }

    if (string.IsNullOrWhiteSpace(manifestPath))
    { Console.Error.WriteLine("layer-validate requires --layers <manifest>"); return 1; }
    if (string.IsNullOrWhiteSpace(palettePath))
    { Console.Error.WriteLine("layer-validate requires --palette <palette>"); return 1; }

    var mergeOpts = new LayerMergeOptions { Resize = resize };
    var result    = LayerValidator.Validate(manifestPath, palettePath, mergeOpts);

    Console.WriteLine($"Manifest:   {Path.GetFullPath(manifestPath)}");
    Console.WriteLine($"Palette:    {Path.GetFullPath(palettePath)}");

    if (result.Errors.Count > 0)
    {
        foreach (var e in result.Errors) Console.Error.WriteLine($"  error: {e}");
        Console.WriteLine("Status:     INVALID");
        return 1;
    }

    Console.WriteLine($"Layers:     {result.LayerResults.Count}");
    Console.WriteLine($"Precedence: {string.Join(" > ", result.Precedence)}");

    foreach (var lr in result.LayerResults)
    {
        var status = lr.IsValid ? "OK" : "INVALID";
        Console.WriteLine($"  {lr.LayerName,-12} {lr.FilePath,-24} {status,-8}" +
                          $" {lr.NonDefaultPixels,6} non-default  {lr.InvalidPixels,4} invalid");
        foreach (var e in lr.Errors) Console.Error.WriteLine($"    error: {e}");
    }

    if (!result.IsValid)
    {
        Console.WriteLine("Status:     INVALID");
        return 1;
    }

    Console.WriteLine("Status:     OK");
    return 0;
}

static int LocalTileSurveyCommand(string[] args)
{
    var configPath = string.Empty;
    var outputDir  = string.Empty;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--config" or "-c" && i + 1 < args.Length) configPath = args[++i];
        else if (args[i] is "--output" or "-o" && i + 1 < args.Length) outputDir  = args[++i];
    }

    if (string.IsNullOrWhiteSpace(configPath))
    { Console.Error.WriteLine("local-tile-survey requires --config <path>"); return 1; }

    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Path.Combine(Directory.GetCurrentDirectory(), ".local");

    // The output dir must be (or end with) a .local directory segment.
    // The writer appends nothing extra; it writes directly to this directory.
    var fullOutput = Path.GetFullPath(
        outputDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    var lastSegment = Path.GetFileName(fullOutput);
    if (!lastSegment.Equals(".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"local-tile-survey: --output must end with a .local directory: {fullOutput}");
        return 1;
    }

    // repoRoot is the parent of .local; the writer writes to <repoRoot>/.local/
    var repoRoot = Path.GetDirectoryName(fullOutput) ?? Directory.GetCurrentDirectory();

    var validationResult = LocalPzInstallValidator.Validate(configPath);

    if (!validationResult.IsValid)
    {
        Console.WriteLine("Status:                   INVALID (config validation failed)");
        foreach (var e in validationResult.Errors)
            Console.Error.WriteLine($"  error: {e}");
        return 1;
    }

    var survey   = LocalTileReferenceSurveyWriter.Write(validationResult, configPath, repoRoot);
    var jsonPath = Path.Combine(fullOutput, LocalTileReferenceSurveyWriter.JsonFileName);
    var mdPath   = Path.Combine(fullOutput, LocalTileReferenceSurveyWriter.MarkdownFileName);

    Console.WriteLine($"Schema:                   {survey.Schema}");
    Console.WriteLine($"Claim boundary:           {survey.ClaimBoundary}");
    Console.WriteLine($"Install root exists:      {survey.InstallRootExists}");
    Console.WriteLine($"Tiles root exists:        {survey.TilesRootExists}");
    Console.WriteLine($"Likely tile data present: {survey.LikelyTileDataPresent}");
    Console.WriteLine($"Survey JSON:              {jsonPath}");
    Console.WriteLine($"Survey MD:                {mdPath}");
    Console.WriteLine($"PZ assets copied:         {survey.PzAssetsCopied}");
    Console.WriteLine($"media/maps touched:       {survey.MediaMapsTouched}");
    Console.WriteLine($"Playable export claimed:  {survey.PlayableExportClaimed}");
    Console.WriteLine("Status:                   OK");
    return 0;
}

static int AppExportCommand(string[] args)
{
    var imagePath         = string.Empty;
    var palettePath       = string.Empty;
    var outputDir         = string.Empty;
    var runName           = string.Empty;
    var annotationPath    = string.Empty;
    var svgSelectionPath  = string.Empty;
    var resize            = false;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--path"          or "-p" && i + 1 < args.Length) imagePath        = args[++i];
        else if (args[i] is "--palette"                && i + 1 < args.Length) palettePath      = args[++i];
        else if (args[i] is "--output"        or "-o" && i + 1 < args.Length) outputDir        = args[++i];
        else if (args[i] is "--run-name"      or "-n" && i + 1 < args.Length) runName          = args[++i];
        else if (args[i] is "--annotation"    or "-a" && i + 1 < args.Length) annotationPath   = args[++i];
        else if (args[i] is "--svg-selection" or "-s" && i + 1 < args.Length) svgSelectionPath = args[++i];
        else if (args[i] is "--resize")                                         resize           = true;
    }

    if (string.IsNullOrWhiteSpace(imagePath))
    { Console.Error.WriteLine("app-export requires --path <image>");     return 1; }
    if (string.IsNullOrWhiteSpace(palettePath))
    { Console.Error.WriteLine("app-export requires --palette <palette>"); return 1; }

    if (!string.IsNullOrWhiteSpace(annotationPath) && !File.Exists(annotationPath))
    { Console.Error.WriteLine($"app-export: --annotation file not found: {annotationPath}"); return 1; }

    if (!string.IsNullOrWhiteSpace(svgSelectionPath) && !File.Exists(svgSelectionPath))
    { Console.Error.WriteLine($"app-export: --svg-selection file not found: {svgSelectionPath}"); return 1; }

    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Path.Combine(Directory.GetCurrentDirectory(), ".local", "app");

    if (!string.IsNullOrWhiteSpace(runName))
    {
        var sanitized = SanitizeRunName(runName);
        if (string.IsNullOrEmpty(sanitized))
        { Console.Error.WriteLine($"app-export: --run-name '{runName}' produces an empty name after sanitization"); return 1; }
        outputDir = Path.Combine(outputDir, sanitized);
    }

    var outputFull  = Path.GetFullPath(outputDir);
    var localMarker = Path.DirectorySeparatorChar + ".local" + Path.DirectorySeparatorChar;
    if (!outputFull.Contains(localMarker, StringComparison.OrdinalIgnoreCase) &&
        !outputFull.EndsWith(Path.DirectorySeparatorChar + ".local", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"app-export: refusing to write outside a .local/ directory: {outputFull}");
        return 1;
    }

    var artifactsDir = Path.Combine(outputFull, "artifacts");
    Directory.CreateDirectory(artifactsDir);

    var (planOpts, planErr) = ParsePlanningOptions(args);
    if (planErr != 0) return planErr;

    // Step 1: parse image
    ImageMapForgeResult parseResult;
    try
    {
        var imgOpts = new ImageMapForgeOptions { Resize = resize };
        parseResult = ImageMapForgeParser.Parse(imagePath, palettePath, imgOpts);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status:          INVALID (image parse failed)");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    // Step 2: write parsed-cell.json to artifacts/
    var paletteResult = PaletteLoader.Load(palettePath);
    if (!paletteResult.IsValid)
    {
        Console.WriteLine("Status:          INVALID (palette)");
        foreach (var e in paletteResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }
    var parsedCellPath = ImageMapForgeArtifactWriter.Write(
        artifactsDir, imagePath, palettePath, paletteResult.Document!, parseResult);

    // Step 3: load parsed cell -> SemanticGrid
    var cellResult = ParsedCellLoader.Load(parsedCellPath);
    if (!cellResult.IsValid)
    {
        Console.WriteLine("Status:          INVALID (parsed-cell load failed)");
        foreach (var e in cellResult.Errors) Console.Error.WriteLine($"  error: {e}");
        return 1;
    }
    var grid       = cellResult.Grid!;
    var codeToKind = cellResult.Document!.Counts
        .ToDictionary(c => c.Code[0], c => c.Kind)
        .AsReadOnly();

    // Steps 4-6: regions, primitives, planning
    PlanningRuleResult planResult;
    RegionExtractionResult regions;
    PrimitiveClassificationResult primitives;
    try
    {
        regions    = RegionExtractor.Extract(grid, codeToKind);
        primitives = PrimitiveClassifier.Classify(regions);
        planResult = PlanningRuleEngine.Evaluate(primitives, planOpts!);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine("Status:          INVALID (pipeline failed)");
        Console.Error.WriteLine($"  error: {ex.Message}");
        return 1;
    }

    // Step 7: write artifact files to artifacts/
    var (regionsJsonPath, _) = RegionArtifactWriter.Write(
        artifactsDir, grid.Width, grid.Height, Path.GetFullPath(parsedCellPath), regions);

    PrimitiveArtifactWriter.Write(
        artifactsDir, grid.Width, grid.Height, regionsJsonPath, primitives);

    PlanningArtifactWriter.Write(
        artifactsDir, grid.Width, grid.Height,
        Path.GetFullPath(parsedCellPath), "PZMapForge.Cli app-export",
        planResult, planOpts);

    // Step 8: copy input image to images/
    var imagesDir  = Path.Combine(outputFull, "images");
    Directory.CreateDirectory(imagesDir);
    var inputExt   = Path.GetExtension(imagePath).ToLowerInvariant();
    var copiedName = "input-image" + (string.IsNullOrEmpty(inputExt) ? ".png" : inputExt);
    File.Copy(imagePath, Path.Combine(imagesDir, copiedName), overwrite: true);
    var relativeImgSrc = "images/" + copiedName;

    // Step 8b: generate parsed-preview.png from snapped palette colors
    WriteParsedPreview(imagesDir, grid, cellResult.Document!);
    var relativeParsedSrc = "images/parsed-preview.png";

    // Step 8c: copy annotation if provided; detect SVG; write svg-reference-summary.json
    var relativeAnnotSrc         = string.Empty;
    var annotPanelLabel          = "Annotation Reference";
    var annotGuidanceHtml        = string.Empty;
    var svgStructureSectionHtml  = string.Empty;
    var svgCandidatesSectionHtml = string.Empty;
    var svgSelectionSectionHtml  = string.Empty;
    var svgReviewSectionHtml     = string.Empty;
    var svgManifestSectionHtml   = string.Empty;
    var svgParseStatus           = "absent";
    var svgManifestPresent       = false;

    if (!string.IsNullOrWhiteSpace(annotationPath))
    {
        var annotExt    = Path.GetExtension(annotationPath).ToLowerInvariant();
        var annotCopied = "annotation-image" + (string.IsNullOrEmpty(annotExt) ? ".png" : annotExt);
        File.Copy(annotationPath, Path.Combine(imagesDir, annotCopied), overwrite: true);
        relativeAnnotSrc = "images/" + annotCopied;

        if (annotExt == ".svg")
        {
            annotPanelLabel   = "SVG Vector Reference";
            annotGuidanceHtml = "<p class=\"svg-note\">SVG is displayed as a reference only. SVG is not parsed into map geometry. Streets, borough limits, IDs, labels, and paths are not converted in this slice.</p>";

            var svgSummary = new
            {
                schema                 = "pzmapforge.svg-reference-summary.v0.1",
                claim_boundary         = "planning_artifact_only_not_pz_load_tested",
                source_file_name       = Path.GetFileName(annotationPath),
                copied_file_name       = annotCopied,
                file_size_bytes        = new FileInfo(annotationPath).Length,
                extension              = annotExt,
                detected_svg           = true,
                parsed_as_geometry     = false,
                pz_assets_copied       = false,
                media_maps_touched     = false,
                playable_export_claimed = false,
            };
            File.WriteAllText(
                Path.Combine(artifactsDir, "svg-reference-summary.json"),
                JsonSerializer.Serialize(svgSummary, new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);

            var svgResult = WriteSvgStructure(annotationPath, artifactsDir);
            svgParseStatus           = svgResult.ParseStatus;
            svgStructureSectionHtml  = BuildSvgStructureHtml(svgResult);
            var svgCandidates        = WriteSvgLayerCandidates(svgResult, artifactsDir);
            svgCandidatesSectionHtml = BuildSvgLayerCandidatesHtml(svgCandidates);
            WriteSvgLayerSelectionTemplate(svgCandidates, svgResult.SourceFileName, artifactsDir);
            svgSelectionSectionHtml  = BuildSvgLayerSelectionHtml();
        }
    }

    // Step 9: write index.html
    var htmlPath = Path.Combine(outputFull, "index.html");
    if (!string.IsNullOrWhiteSpace(svgSelectionPath))
    {
        var selJson = File.ReadAllText(svgSelectionPath, Encoding.UTF8);
        var (selItems, selErr) = ReadSvgLayerSelection(selJson);
        if (selErr is not null)
        { Console.Error.WriteLine($"app-export: --svg-selection is not valid JSON: {selErr}"); return 1; }
        File.Copy(svgSelectionPath, Path.Combine(artifactsDir, "svg-layer-selection.input.json"), overwrite: true);
        WriteSvgLayerSelectionReview(selItems, Path.GetFileName(svgSelectionPath), artifactsDir);
        svgReviewSectionHtml = BuildSvgLayerSelectionReviewHtml(selItems);
        if (selItems.Count > 0)
        {
            WriteSvgPlanningManifest(selItems, Path.GetFileName(svgSelectionPath), artifactsDir);
            svgManifestSectionHtml = BuildSvgPlanningManifestHtml(selItems);
            svgManifestPresent     = true;
        }
        else
        {
            svgManifestSectionHtml = "\n    <h2>SVG Planning Manifest</h2>\n    <p class=\"section-note\">No selected SVG metadata was available for a planning manifest.</p>\n";
        }
    }

    var paletteClean          = cellResult.Document!.Matching is { } mhc && mhc.UnmappedExactColours == 0;
    var svgAnnotationPresent  = svgParseStatus != "absent";
    var svgCandidatesPresent  = !string.IsNullOrEmpty(svgCandidatesSectionHtml);
    var svgReviewPresent      = !string.IsNullOrWhiteSpace(svgSelectionPath);
    var runSummarySectionHtml = BuildRunSummaryHtml(
        paletteClean, svgAnnotationPresent, svgParseStatus,
        svgCandidatesPresent, svgReviewPresent, svgManifestPresent);

    var artifactIndexHtml = BuildArtifactIndexHtml(
        relativeImgSrc, relativeAnnotSrc,
        svgAnnotationPresent, svgCandidatesPresent, svgReviewPresent, svgManifestPresent);

    var html     = BuildAppHtml(
        relativeImgSrc, relativeParsedSrc, relativeAnnotSrc,
        annotPanelLabel, annotGuidanceHtml, svgStructureSectionHtml, svgCandidatesSectionHtml, svgSelectionSectionHtml, svgReviewSectionHtml, svgManifestSectionHtml,
        runSummarySectionHtml,
        artifactIndexHtml,
        imagePath, grid.Width, grid.Height, parseResult.Resized,
        regions.TotalRegions, primitives.PrimitiveCount,
        planResult.RecommendationCount, planResult.Summary.WarningCount,
        planOpts!, cellResult.Document!);
    File.WriteAllText(htmlPath, html, Encoding.UTF8);

    Console.WriteLine($"App index:        {htmlPath}");
    Console.WriteLine($"Artifacts:        {artifactsDir}");
    Console.WriteLine($"Dimensions:       {grid.Width}x{grid.Height}");
    Console.WriteLine($"Regions:          {regions.TotalRegions}");
    Console.WriteLine($"Primitives:       {primitives.PrimitiveCount}");
    Console.WriteLine($"Recommendations:  {planResult.RecommendationCount}");
    Console.WriteLine($"Warnings:         {planResult.Summary.WarningCount}");
    Console.WriteLine("Status:           OK");
    return 0;
}

static SvgStructureResult WriteSvgStructure(string annotationPath, string artifactsDir)
{
    const long MaxChars = 50_000_000;

    // DtdProcessing.Ignore: skip DOCTYPE declarations without processing them.
    // XmlResolver=null: block all external entity/resource resolution.
    var xmlSettings = new XmlReaderSettings
    {
        DtdProcessing           = DtdProcessing.Ignore,
        XmlResolver             = null,
        MaxCharactersInDocument = MaxChars,
    };

    List<XElement> elements;
    XElement?      root;
    string         parseStatus = "parsed";
    string         parseError  = string.Empty;

    try
    {
        using var reader = XmlReader.Create(annotationPath, xmlSettings);
        var doc  = XDocument.Load(reader);
        root     = doc.Root;
        elements = root?.DescendantsAndSelf().ToList() ?? [];
    }
    catch (Exception ex)
    {
        root        = null;
        elements    = [];
        parseStatus = "failed";
        parseError  = ex.Message;
    }

    var countByName = elements
        .GroupBy(e => e.Name.LocalName.ToLowerInvariant())
        .ToDictionary(g => g.Key, g => g.Count());

    // Sample lists (bounded to 20) for structure artifact display.
    var sampleIds = elements
        .Select(e => e.Attribute("id")?.Value)
        .Where(v => !string.IsNullOrEmpty(v))
        .Select(v => v!)
        .Distinct()
        .Take(20)
        .ToList();

    var sampleClasses = elements
        .Select(e => e.Attribute("class")?.Value)
        .Where(v => !string.IsNullOrEmpty(v))
        .SelectMany(v => v!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Distinct()
        .Take(20)
        .ToList();

    var sampleTextLabels = elements
        .Where(e => e.Name.LocalName == "text")
        .Select(e => e.Value.Trim())
        .Where(v => !string.IsNullOrEmpty(v))
        .Distinct()
        .Take(20)
        .ToList();

    // Full lists (bounded at 500, deduplicated) for complete candidate classification.
    var allIds = elements
        .Select(e => e.Attribute("id")?.Value)
        .Where(v => !string.IsNullOrEmpty(v))
        .Select(v => v!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(500)
        .ToList();

    var allClasses = elements
        .Select(e => e.Attribute("class")?.Value)
        .Where(v => !string.IsNullOrEmpty(v))
        .SelectMany(v => v!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(500)
        .ToList();

    var allTextLabels = elements
        .Where(e => e.Name.LocalName == "text")
        .Select(e => e.Value.Trim())
        .Where(v => !string.IsNullOrEmpty(v))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(500)
        .ToList();

    var structure = new
    {
        schema                       = "pzmapforge.svg-reference-structure.v0.1",
        claim_boundary               = "planning_artifact_only_not_pz_load_tested",
        parse_status                 = parseStatus,
        parse_error                  = parseError,
        max_characters_in_document   = MaxChars,
        source_file_name             = Path.GetFileName(annotationPath),
        file_size_bytes              = new FileInfo(annotationPath).Length,
        root_element                 = root?.Name.LocalName ?? "unknown",
        width                        = root?.Attribute("width")?.Value  ?? "",
        height                       = root?.Attribute("height")?.Value ?? "",
        viewBox                      = root?.Attribute("viewBox")?.Value ?? "",
        element_counts               = new
        {
            svg      = countByName.GetValueOrDefault("svg"),
            g        = countByName.GetValueOrDefault("g"),
            path     = countByName.GetValueOrDefault("path"),
            polyline = countByName.GetValueOrDefault("polyline"),
            polygon  = countByName.GetValueOrDefault("polygon"),
            line     = countByName.GetValueOrDefault("line"),
            rect     = countByName.GetValueOrDefault("rect"),
            circle   = countByName.GetValueOrDefault("circle"),
            ellipse  = countByName.GetValueOrDefault("ellipse"),
            text     = countByName.GetValueOrDefault("text"),
            image    = countByName.GetValueOrDefault("image"),
            use      = countByName.GetValueOrDefault("use"),
        },
        id_count                     = sampleIds.Count,
        class_count                  = sampleClasses.Count,
        sample_ids                   = sampleIds,
        sample_classes               = sampleClasses,
        sample_text_labels           = sampleTextLabels,
        likely_contains_text_labels  = sampleTextLabels.Count > 0,
        likely_contains_paths        = countByName.GetValueOrDefault("path") > 0,
        likely_contains_groups       = countByName.GetValueOrDefault("g") > 0,
        parsed_as_geometry           = false,
        converted_to_map_geometry    = false,
        pz_assets_copied             = false,
        media_maps_touched           = false,
        playable_export_claimed      = false,
    };

    File.WriteAllText(
        Path.Combine(artifactsDir, "svg-reference-structure.json"),
        JsonSerializer.Serialize(structure, new JsonSerializerOptions { WriteIndented = true }),
        Encoding.UTF8);

    return new SvgStructureResult
    {
        ParseStatus      = parseStatus,
        ParseError       = parseError,
        SourceFileName   = Path.GetFileName(annotationPath),
        FileSizeBytes    = new FileInfo(annotationPath).Length,
        RootElement      = root?.Name.LocalName ?? "unknown",
        Width            = root?.Attribute("width")?.Value  ?? "",
        Height           = root?.Attribute("height")?.Value ?? "",
        ViewBox          = root?.Attribute("viewBox")?.Value ?? "",
        CountG           = countByName.GetValueOrDefault("g"),
        CountPath        = countByName.GetValueOrDefault("path"),
        CountPolyline    = countByName.GetValueOrDefault("polyline"),
        CountPolygon     = countByName.GetValueOrDefault("polygon"),
        CountLine        = countByName.GetValueOrDefault("line"),
        CountRect        = countByName.GetValueOrDefault("rect"),
        CountText        = countByName.GetValueOrDefault("text"),
        SampleIds        = sampleIds.AsReadOnly(),
        SampleClasses    = sampleClasses.AsReadOnly(),
        SampleTextLabels = sampleTextLabels.AsReadOnly(),
        AllIds           = allIds.AsReadOnly(),
        AllClasses       = allClasses.AsReadOnly(),
        AllTextLabels    = allTextLabels.AsReadOnly(),
    };
}

static void WriteParsedPreview(
    string imagesDir,
    PZMapForge.Core.ParsedCell.SemanticGrid grid,
    PZMapForge.Core.ParsedCell.ParsedCellDocument doc)
{
    var codeToColor = doc.Legend
        .Where(e => e.Rgb.Length >= 3 && e.Code.Length > 0)
        .ToDictionary(
            e => e.Code[0],
            e => System.Drawing.Color.FromArgb(255, e.Rgb[0], e.Rgb[1], e.Rgb[2]));

    var fallback = System.Drawing.Color.FromArgb(255, 40, 40, 40);

    using var bmp = new System.Drawing.Bitmap(grid.Width, grid.Height);
    for (var y = 0; y < grid.Height; y++)
        for (var x = 0; x < grid.Width; x++)
        {
            var code  = grid.GetCode(x, y);
            bmp.SetPixel(x, y, codeToColor.TryGetValue(code, out var c) ? c : fallback);
        }

    bmp.Save(Path.Combine(imagesDir, "parsed-preview.png"),
        System.Drawing.Imaging.ImageFormat.Png);
}

static string BuildAppHtml(
    string relativeImgSrc,
    string relativeParsedSrc,
    string relativeAnnotSrc,
    string annotPanelLabel,
    string annotGuidanceHtml,
    string svgStructureSectionHtml,
    string svgCandidatesSectionHtml,
    string svgSelectionSectionHtml,
    string svgReviewSectionHtml,
    string svgManifestSectionHtml,
    string runSummarySectionHtml,
    string artifactIndexHtml,
    string imagePath, int width, int height, bool resized,
    int regions, int primitives, int recommendations, int warnings,
    PlanningRuleOptions planOpts,
    PZMapForge.Core.ParsedCell.ParsedCellDocument doc)
{
    var imageName    = HtmlEncode(Path.GetFileName(imagePath));
    var paletteName  = HtmlEncode(Path.GetFileName(doc.Palette));
    var kindCount    = doc.Legend.Count;
    var now          = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
    var warnClass    = warnings > 0 ? "warn" : "ok";
    var legendRows   = BuildLegendHtml(doc);
    var driftSection = BuildDriftHtml(doc);
    var matchSummary = doc.Matching is { } m
        ? $"Exact: {m.ExactPixels:N0} px &nbsp;|&nbsp; Nearest: {m.NearestPixels:N0} px &nbsp;|&nbsp; Unmapped colors: {m.UnmappedExactColours} &nbsp;|&nbsp; Unique source colors: {m.UniqueSourceColours}"
        : "Color match data not available.";

    string healthLabel, healthClass, healthGuidance;
    if (doc.Matching is { } mh && mh.UnmappedExactColours > 0)
    {
        healthLabel    = "Not palette-clean";
        healthClass    = "dirty";
        healthGuidance = $"{mh.UnmappedExactColours} off-palette color(s) snapped to nearest kind. Review the blockout for text overlays, anti-aliased edges, or colors outside the palette.";
    }
    else if (doc.Matching is not null)
    {
        healthLabel    = "Palette clean";
        healthClass    = "clean";
        healthGuidance = "All pixels matched the palette exactly.";
    }
    else
    {
        healthLabel    = "Unknown";
        healthClass    = "unknown";
        healthGuidance = string.Empty;
    }

    var annotColHtml = string.IsNullOrEmpty(relativeAnnotSrc) ? string.Empty :
        $"""

      <div class="preview-col">
        <div class="preview-lbl">{HtmlEncode(annotPanelLabel)}</div>
        <img class="preview-img" src="{relativeAnnotSrc}" alt="{HtmlEncode(annotPanelLabel)}">
      </div>
""";

    return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>PZMapForge &mdash; Map Workbench</title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:monospace;background:#111;color:#ccc;min-height:100vh}
.hdr{background:#162016;border-bottom:2px solid #2e4a2e;padding:.75em 2em;display:flex;align-items:center;gap:.8em}
.hdr h1{color:#9acc9a;font-size:1.2em;letter-spacing:.04em}
.badge{font-size:.72em;background:#1e3a14;border:1px solid #3d6628;color:#82bb55;padding:.15em .55em;border-radius:3px}
.boundary{background:#1e1e0e;border-left:3px solid #777711;padding:.5em 2em;font-size:.82em;color:#aaaa77}
.boundary code{color:#dddd55}
.workbench{display:grid;grid-template-columns:minmax(0,3fr) minmax(0,2fr);align-items:start}
.left-panel{padding:1.2em 1.5em 2em 2em;border-right:1px solid #1c1c1c;min-width:0}
.right-panel{padding:1.2em 2em 2em 1.5em;background:#0d0d0d;min-width:0}
h2{color:#779977;font-size:.82em;text-transform:uppercase;letter-spacing:.1em;border-bottom:1px solid #222;padding-bottom:.2em;margin:1.3em 0 .6em}
h2:first-child{margin-top:0}
.preview-row{display:flex;flex-wrap:wrap;gap:.8em;margin-bottom:.6em}
.preview-col{flex:1 1 180px;min-width:0}
.preview-lbl{font-size:.72em;color:#666;text-transform:uppercase;letter-spacing:.07em;margin-bottom:.3em}
.preview-img{display:block;image-rendering:pixelated;max-width:100%;border:1px solid #2a2a2a}
.cards{display:flex;flex-wrap:wrap;gap:.6em;margin:.5em 0 1em}
.card{background:#181818;border:1px solid #2a2a2a;border-radius:4px;padding:.6em 1em;min-width:90px}
.card .num{font-size:1.7em;font-weight:bold;line-height:1;color:#9acc9a}
.card .num.warn{color:#cc9944}
.card .num.ok{color:#77cc77}
.card .lbl{font-size:.68em;color:#666;margin-top:.25em;text-transform:uppercase;letter-spacing:.06em}
.meta-tbl{border-collapse:collapse;width:100%;margin-bottom:.5em}
.meta-tbl th,.meta-tbl td{border:1px solid #222;padding:.28em .6em;text-align:left;font-size:.84em}
.meta-tbl th{background:#181818;color:#777;width:9em}
.swatch{display:inline-block;width:13px;height:13px;border:1px solid #555;vertical-align:middle;margin-right:4px;border-radius:2px}
.legend-tbl{border-collapse:collapse;width:100%}
.legend-tbl th,.legend-tbl td{border:1px solid #222;padding:.28em .6em;font-size:.83em;text-align:left}
.legend-tbl th{background:#181818;color:#777}
.legend-tbl .px{text-align:right;color:#888}
.match-bar{font-size:.78em;color:#666;margin:.3em 0 .7em;padding:.38em .65em;background:#181818;border:1px solid #222;border-radius:3px}
.drift-tbl{border-collapse:collapse;width:100%;margin-top:.4em}
.drift-tbl th,.drift-tbl td{border:1px solid #222;padding:.25em .55em;font-size:.78em}
.drift-tbl th{background:#181818;color:#777}
.section-note{font-size:.78em;color:#555;margin:.25em 0 .7em}
.svg-note{font-size:.79em;color:#7799aa;margin:.5em 0 .3em;padding:.38em .65em;background:#0e1a22;border:1px solid #1e3a4a;border-radius:3px}
.svg-sub{font-size:.75em;color:#7799aa;text-transform:uppercase;letter-spacing:.08em;margin:.9em 0 .3em}
.review-item{margin:.2em 0 .35em;line-height:1.7}
.review-use{font-size:.78em;color:#88bb88;margin-left:.4em}
.review-note{font-size:.75em;color:#888;margin-left:.35em;font-style:italic}
.svg-chips{margin:.25em 0 .6em;line-height:1.9}
.svg-chip{display:inline-block;font-size:.76em;background:#162030;border:1px solid #2a4060;color:#88bbdd;padding:.1em .45em;border-radius:2px;margin:.1em .1em}
.health-badge{display:inline-block;font-size:.79em;font-weight:bold;padding:.28em .75em;border-radius:3px;margin:.35em 0 .65em}
.health-badge.clean{background:#162216;border:1px solid #2e5a2e;color:#77cc77}
.health-badge.dirty{background:#261616;border:1px solid #6a2a2a;color:#cc7766}
.health-badge.unknown{background:#1e1e1e;border:1px solid #444;color:#888}
.health-guidance{font-size:.8em;color:#cc9966;margin:.4em 0 .5em;padding:.4em .65em;background:#1e160a;border:1px solid #3a2e10;border-radius:3px}
.health-note{font-size:.77em;color:#666;margin:.3em 0 .2em}
.arts{display:grid;grid-template-columns:repeat(auto-fill,minmax(190px,1fr));gap:.4em;margin:.4em 0 .8em}
.art{background:#161616;border:1px solid #222;border-radius:3px;padding:.4em .65em}
.art a{color:#77b8d8;text-decoration:none;font-size:.84em}
.art a:hover{text-decoration:underline}
.art .desc{font-size:.7em;color:#555;margin-top:.1em}
.nc-list{list-style:none;font-size:.8em;color:#886644}
.nc-list li{padding:.15em 0}
.nc-list li::before{content:"-- "}
.cockpit{background:#0e0e0e;border-bottom:1px solid #1c1c1c;padding:.45em 2em;font-size:.78em}
.cockpit-row{display:flex;flex-wrap:wrap;align-items:center;gap:.35em .55em;margin:.22em 0}
.ck-lbl{color:#555;text-transform:uppercase;letter-spacing:.08em;font-size:.85em;min-width:8.5em}
.ck-ok{background:#12211a;border:1px solid #2a4a32;color:#88cc88;padding:.1em .45em;border-radius:2px}
.ck-warn{background:#221a12;border:1px solid #4a3a22;color:#cc9944;padding:.1em .45em;border-radius:2px}
.ck-absent{background:#1a1a1a;border:1px solid #2a2a2a;color:#555;padding:.1em .45em;border-radius:2px}
.ck-safe{background:#0e180e;border:1px solid #1e3a1e;color:#66aa66;padding:.1em .45em;border-radius:2px}
.artifacts-idx{background:#0e0e0e;border-bottom:1px solid #1c1c1c;padding:.45em 2em .65em}
.artifacts-idx-hdr{font-size:.75em;color:#557755;text-transform:uppercase;letter-spacing:.1em;margin-bottom:.3em}
.artifacts-idx-note{font-size:.7em;color:#444;margin-bottom:.4em}
footer{padding:.65em 2em;border-top:1px solid #1a1a1a;font-size:.72em;color:#444}
@media(max-width:860px){
.workbench{grid-template-columns:1fr}
.left-panel{border-right:none;border-bottom:1px solid #1c1c1c}
}
</style>
</head>
<body>

<div class="hdr">
  <h1>PZMapForge</h1>
  <span class="badge">Map Workbench</span>
  <span class="badge">planning artifact only</span>
</div>

<div class="boundary">
  Claim boundary: <code>planning_artifact_only_not_pz_load_tested</code> &mdash;
  local planning artifact only &mdash; not a playable Project Zomboid export
</div>

{{runSummarySectionHtml}}
{{artifactIndexHtml}}
<div class="workbench">

  <div class="left-panel">
    <h2>Map Preview</h2>
    <div class="preview-row">
      <div class="preview-col">
        <div class="preview-lbl">Analysis Input</div>
        <img class="preview-img" src="{{relativeImgSrc}}" alt="Analysis input: {{imageName}}">
      </div>
      <div class="preview-col">
        <div class="preview-lbl">Parsed Preview</div>
        <img class="preview-img" src="{{relativeParsedSrc}}" alt="Parsed preview">
      </div>{{annotColHtml}}
    </div>
    {{annotGuidanceHtml}}
    <h2>Visual Legend</h2>
    <div class="match-bar">{{matchSummary}}</div>
    <table class="legend-tbl">
      <tr><th>Color</th><th>Kind</th><th>Code</th><th class="px">Pixels</th></tr>
      {{legendRows}}
    </table>
    {{driftSection}}
  </div>

  <div class="right-panel">
    <h2>Summary</h2>
    <div class="cards">
      <div class="card"><div class="num">{{width}}&times;{{height}}</div><div class="lbl">Dimensions</div></div>
      <div class="card"><div class="num">{{regions}}</div><div class="lbl">Regions</div></div>
      <div class="card"><div class="num">{{primitives}}</div><div class="lbl">Primitives</div></div>
      <div class="card"><div class="num">{{recommendations}}</div><div class="lbl">Recs</div></div>
      <div class="card"><div class="num {{warnClass}}">{{warnings}}</div><div class="lbl">Warnings</div></div>
      <div class="card"><div class="num">{{kindCount}}</div><div class="lbl">Kinds</div></div>
    </div>
    <table class="meta-tbl">
      <tr><th>Image</th><td>{{imageName}}</td></tr>
      <tr><th>Palette</th><td>{{paletteName}} ({{kindCount}} kinds)</td></tr>
      <tr><th>Dimensions</th><td>{{width}} &times; {{height}} px</td></tr>
      <tr><th>Resized</th><td>{{resized}}</td></tr>
      <tr><th>Tiny px</th><td>{{planOpts.TinyBuildingPixelThreshold}}</td></tr>
      <tr><th>Large px</th><td>{{planOpts.LargeGroundPixelThreshold}}</td></tr>
      <tr><th>Generated</th><td>{{now}}</td></tr>
    </table>

    <h2>Palette Health</h2>
    <div class="health-badge {{healthClass}}">{{healthLabel}}</div>
    <p class="health-note">{{healthGuidance}}</p>
    <p class="health-note">Use <code>--path</code> for a <strong>clean palette-only analysis image</strong>. Text labels and antialiasing should not be part of the analysis image &mdash; they produce a <em>not palette-clean</em> result. Use <code>--annotation</code> to include a separate labeled reference image without affecting parsing.</p>

    <h2>Artifact Files</h2>
    <div class="arts">
      <div class="art"><a href="artifacts/parsed-cell.json">parsed-cell.json</a><div class="desc">Parsed cell grid</div></div>
      <div class="art"><a href="artifacts/regions.json">regions.json</a><div class="desc">Extracted regions</div></div>
      <div class="art"><a href="artifacts/primitives.json">primitives.json</a><div class="desc">Classified primitives</div></div>
      <div class="art"><a href="artifacts/plan-recommendations.json">plan-recommendations.json</a><div class="desc">Plan recommendations</div></div>
      <div class="art"><a href="artifacts/regions-report.md">regions-report.md</a><div class="desc">Regions report</div></div>
      <div class="art"><a href="artifacts/primitives-report.md">primitives-report.md</a><div class="desc">Primitives report</div></div>
      <div class="art"><a href="artifacts/plan-report.md">plan-report.md</a><div class="desc">Plan report</div></div>
    </div>

    {{svgStructureSectionHtml}}
    {{svgCandidatesSectionHtml}}
    {{svgSelectionSectionHtml}}
    {{svgReviewSectionHtml}}
    {{svgManifestSectionHtml}}
    <h2>Non-claims</h2>
    <ul class="nc-list">
      <li>Not a playable Project Zomboid map.</li>
      <li>No PZ assets copied or read.</li>
      <li>No media/maps directory written.</li>
      <li>No lotpack, lotheader, or bin files generated.</li>
      <li>Tile GIDs not assigned. No TileZed tile references.</li>
      <li>Build 41 / Build 42 compatibility not claimed.</li>
    </ul>
  </div>

</div>
<footer>Generated by PZMapForge &mdash; planning_artifact_only_not_pz_load_tested</footer>
</body>
</html>
""";
}

static string BuildArtifactIndexHtml(
    string relativeImgSrc,
    string relativeAnnotSrc,
    bool svgAnnotationPresent,
    bool svgCandidatesPresent,
    bool svgReviewPresent,
    bool svgManifestPresent)
{
    var sb = new StringBuilder();
    sb.Append("<div class=\"artifacts-idx\">\n");
    sb.Append("  <div class=\"artifacts-idx-hdr\">Evidence Artifacts</div>\n");
    sb.Append("  <div class=\"artifacts-idx-note\">planning artifact only &mdash; not a playable Project Zomboid export</div>\n");
    sb.Append("  <div class=\"arts\">\n");
    sb.Append($"    <div class=\"art\"><a href=\"{relativeImgSrc}\">Clean analysis image</a><div class=\"desc\">Analysis input (evidence output)</div></div>\n");
    sb.Append("    <div class=\"art\"><a href=\"images/parsed-preview.png\">Parsed preview</a><div class=\"desc\">Palette-snapped preview image</div></div>\n");
    sb.Append("    <div class=\"art\"><a href=\"artifacts/parsed-cell.json\">Parsed cell JSON</a><div class=\"desc\">Cell grid evidence output</div></div>\n");
    sb.Append("    <div class=\"art\"><a href=\"artifacts/regions.json\">Regions JSON</a><div class=\"desc\">Extracted regions</div></div>\n");
    sb.Append("    <div class=\"art\"><a href=\"artifacts/primitives.json\">Primitives JSON</a><div class=\"desc\">Classified primitives</div></div>\n");
    sb.Append("    <div class=\"art\"><a href=\"artifacts/plan-recommendations.json\">Plan recommendations JSON</a><div class=\"desc\">Planning recommendations</div></div>\n");
    if (!string.IsNullOrEmpty(relativeAnnotSrc))
        sb.Append($"    <div class=\"art\"><a href=\"{relativeAnnotSrc}\">Annotation image</a><div class=\"desc\">Reference annotation (not parsed)</div></div>\n");
    if (svgAnnotationPresent)
        sb.Append("    <div class=\"art\"><a href=\"artifacts/svg-reference-structure.json\">SVG structure report</a><div class=\"desc\">SVG element inventory</div></div>\n");
    if (svgCandidatesPresent)
    {
        sb.Append("    <div class=\"art\"><a href=\"artifacts/svg-layer-candidates.json\">SVG layer candidates</a><div class=\"desc\">Metadata-pattern classification</div></div>\n");
        sb.Append("    <div class=\"art\"><a href=\"artifacts/svg-layer-selection.template.json\">SVG layer selection template</a><div class=\"desc\">Operator-editable selection</div></div>\n");
    }
    if (svgReviewPresent)
        sb.Append("    <div class=\"art\"><a href=\"artifacts/svg-layer-selection-review.json\">SVG selection review</a><div class=\"desc\">Selection review artifact</div></div>\n");
    if (svgManifestPresent)
    {
        sb.Append("    <div class=\"art\"><a href=\"artifacts/svg-planning-manifest.json\">SVG planning manifest JSON</a><div class=\"desc\">Selected metadata manifest</div></div>\n");
        sb.Append("    <div class=\"art\"><a href=\"artifacts/svg-planning-manifest.md\">SVG planning manifest Markdown</a><div class=\"desc\">Human-readable manifest</div></div>\n");
    }
    sb.Append("  </div>\n");
    sb.Append("</div>\n");
    return sb.ToString();
}

static string BuildRunSummaryHtml(
    bool paletteClean, bool svgAnnotation, string svgParseStatus,
    bool svgCandidates, bool svgReview, bool svgManifest)
{
    static string Ck(bool present, string trueLabel, string falseLabel) =>
        present
            ? $"<span class=\"ck-ok\">{trueLabel}</span>"
            : $"<span class=\"ck-absent\">{falseLabel}</span>";

    var paletteItem = paletteClean
        ? "<span class=\"ck-ok\">Palette: clean</span>"
        : "<span class=\"ck-warn\">Palette: not clean</span>";

    var svgParseItem = svgParseStatus switch
    {
        "parsed" => "<span class=\"ck-ok\">SVG parse: parsed</span>",
        "failed" => "<span class=\"ck-warn\">SVG parse: failed</span>",
        _        => "<span class=\"ck-absent\">SVG parse: absent</span>",
    };

    var sb = new StringBuilder();
    sb.Append("<div class=\"cockpit\">\n");
    sb.Append("  <div class=\"cockpit-row\">");
    sb.Append("<span class=\"ck-lbl\">Run Summary</span>");
    sb.Append(paletteItem);
    sb.Append(Ck(svgAnnotation, "SVG annotation: present", "SVG annotation: absent"));
    sb.Append(svgParseItem);
    sb.Append(Ck(svgCandidates, "SVG candidates: present", "SVG candidates: absent"));
    sb.Append(Ck(svgReview, "SVG review: present", "SVG review: absent"));
    sb.Append(Ck(svgManifest, "Planning manifest: present", "Planning manifest: absent"));
    sb.Append("</div>\n");
    sb.Append("  <div class=\"cockpit-row\">");
    sb.Append("<span class=\"ck-lbl\">Safety</span>");
    sb.Append("<span class=\"ck-safe\">playable export generated: false</span>");
    sb.Append("<span class=\"ck-safe\">PZ assets copied/read: false</span>");
    sb.Append("<span class=\"ck-safe\">media/maps touched: false</span>");
    sb.Append("<span class=\"ck-safe\">claim_boundary: intact</span>");
    sb.Append("</div>\n");
    sb.Append("</div>\n");
    return sb.ToString();
}

static string BuildSvgStructureHtml(SvgStructureResult r)
{
    var sb = new StringBuilder();

    sb.Append("\n    <h2>SVG Structure Summary</h2>\n");

    var statusClass = r.ParseStatus == "parsed" ? "clean" : "dirty";
    sb.Append($"    <div class=\"health-badge {statusClass}\">parse_status: {HtmlEncode(r.ParseStatus)}</div>\n");

    if (!string.IsNullOrEmpty(r.ParseError))
        sb.Append($"    <p class=\"health-guidance\">{HtmlEncode(r.ParseError)}</p>\n");

    var sizeMb = r.FileSizeBytes / 1_000_000.0;
    sb.Append("    <table class=\"meta-tbl\">\n");
    sb.Append($"      <tr><th>File</th><td>{HtmlEncode(r.SourceFileName)}</td></tr>\n");
    sb.Append($"      <tr><th>Size</th><td>{sizeMb:F1} MB ({r.FileSizeBytes:N0} bytes)</td></tr>\n");
    sb.Append($"      <tr><th>Root</th><td>{HtmlEncode(r.RootElement)}</td></tr>\n");
    sb.Append($"      <tr><th>Width</th><td>{HtmlEncode(r.Width)}</td></tr>\n");
    sb.Append($"      <tr><th>Height</th><td>{HtmlEncode(r.Height)}</td></tr>\n");
    sb.Append($"      <tr><th>ViewBox</th><td>{HtmlEncode(r.ViewBox)}</td></tr>\n");
    sb.Append("    </table>\n");

    sb.Append("    <p class=\"svg-sub\">Element Counts</p>\n");
    sb.Append("    <table class=\"meta-tbl\">\n");
    sb.Append($"      <tr><td>g</td><td class=\"px\">{r.CountG:N0}</td></tr>\n");
    sb.Append($"      <tr><td>path</td><td class=\"px\">{r.CountPath:N0}</td></tr>\n");
    sb.Append($"      <tr><td>polyline</td><td class=\"px\">{r.CountPolyline:N0}</td></tr>\n");
    sb.Append($"      <tr><td>polygon</td><td class=\"px\">{r.CountPolygon:N0}</td></tr>\n");
    sb.Append($"      <tr><td>line</td><td class=\"px\">{r.CountLine:N0}</td></tr>\n");
    sb.Append($"      <tr><td>rect</td><td class=\"px\">{r.CountRect:N0}</td></tr>\n");
    sb.Append($"      <tr><td>text</td><td class=\"px\">{r.CountText:N0}</td></tr>\n");
    sb.Append("    </table>\n");

    if (r.SampleIds.Count > 0)
    {
        sb.Append("    <p class=\"svg-sub\">Sample IDs</p>\n");
        sb.Append("    <div class=\"svg-chips\">");
        foreach (var id in r.SampleIds)
            sb.Append($"<span class=\"svg-chip\">{HtmlEncode(id)}</span>");
        sb.Append("</div>\n");
    }

    if (r.SampleTextLabels.Count > 0)
    {
        sb.Append("    <p class=\"svg-sub\">Sample Text Labels</p>\n");
        sb.Append("    <div class=\"svg-chips\">");
        foreach (var lbl in r.SampleTextLabels)
            sb.Append($"<span class=\"svg-chip\">{HtmlEncode(lbl)}</span>");
        sb.Append("</div>\n");
    }

    sb.Append("    <p class=\"svg-note\">SVG structure is metadata only. Paths, streets, boroughs, and labels are not converted to map geometry.</p>\n");

    sb.Append("    <div class=\"arts\">\n");
    sb.Append("      <div class=\"art\"><a href=\"artifacts/svg-reference-structure.json\">svg-reference-structure.json</a><div class=\"desc\">SVG element counts, IDs, text labels (schema v0.1)</div></div>\n");
    sb.Append("      <div class=\"art\"><a href=\"artifacts/svg-reference-summary.json\">svg-reference-summary.json</a><div class=\"desc\">SVG reference summary</div></div>\n");
    sb.Append("    </div>\n");

    return sb.ToString();
}

static string BuildLegendHtml(PZMapForge.Core.ParsedCell.ParsedCellDocument doc)
{
    var sb = new StringBuilder();
    var pixelMap = doc.Counts.ToDictionary(c => c.Code, c => c.Pixels);
    foreach (var entry in doc.Legend)
    {
        var rgb = entry.Rgb.Length >= 3
            ? $"{entry.Rgb[0]},{entry.Rgb[1]},{entry.Rgb[2]}"
            : "128,128,128";
        pixelMap.TryGetValue(entry.Code, out var pixels);
        sb.Append($"  <tr><td><span class=\"swatch\" style=\"background:rgb({rgb})\"></span>{HtmlEncode(entry.Kind)}</td>");
        sb.Append($"<td>{HtmlEncode(entry.Code)}</td>");
        sb.Append($"<td class=\"px\">{pixels:N0}</td></tr>\n");
    }
    return sb.ToString();
}

static string BuildDriftHtml(PZMapForge.Core.ParsedCell.ParsedCellDocument doc)
{
    if (doc.NearestDrift.Count == 0)
        return "<p class=\"section-note\">Nearest color drift: none (all pixels matched exactly).</p>";

    var sb = new StringBuilder();
    sb.Append("<h2>Nearest Color Drift</h2>\n");
    sb.Append("<p class=\"section-note\">Pixels whose source color had no exact palette match; snapped to nearest kind.</p>\n");
    sb.Append("<table class=\"drift-tbl\">\n");
    sb.Append("  <tr><th>Source RGB</th><th>Count</th><th>Nearest Kind</th><th>Nearest RGB</th><th>Distance</th></tr>\n");
    foreach (var d in doc.NearestDrift)
    {
        sb.Append($"  <tr><td>{HtmlEncode(d.SourceRgb)}</td><td>{d.Count:N0}</td>");
        sb.Append($"<td>{HtmlEncode(d.NearestKind)}</td><td>{HtmlEncode(d.NearestRgb)}</td>");
        sb.Append($"<td>{d.Distance:F2}</td></tr>\n");
    }
    sb.Append("</table>\n");
    return sb.ToString();
}

static string SanitizeRunName(string raw)
{
    var sb = new StringBuilder(raw.Length);
    foreach (var c in raw)
        sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '-');
    var result = sb.ToString().Trim('-');
    // collapse consecutive hyphens
    while (result.Contains("--"))
        result = result.Replace("--", "-");
    return result;
}

static string HtmlEncode(string s) =>
    s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

static int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"Unknown command: {cmd}");
    Console.Error.WriteLine("Available commands: image-check, image-export, full-pipeline, " +
        "palette-check, parsed-cell-check, region-check, primitive-check, " +
        "plan-check, plan-export, layer-pipeline, layer-validate, local-tile-survey, app-export");
    return 1;
}

static bool MatchesAny(string s, string[] keywords) =>
    keywords.Any(k => s.Contains(k, StringComparison.OrdinalIgnoreCase));

// Word-boundary match: keyword must be delimited by non-letter chars or string edges.
static bool ContainsWord(string s, string word)
{
    var idx = s.IndexOf(word, StringComparison.OrdinalIgnoreCase);
    while (idx >= 0)
    {
        var before = idx == 0 || !char.IsLetter(s[idx - 1]);
        var after  = idx + word.Length >= s.Length || !char.IsLetter(s[idx + word.Length]);
        if (before && after) return true;
        idx = s.IndexOf(word, idx + 1, StringComparison.OrdinalIgnoreCase);
    }
    return false;
}

// All alphabetic characters in s are uppercase (transit/landmark heuristic).
static bool IsAllCapsAlpha(string s) =>
    s.Where(char.IsLetter).Any() && s.Where(char.IsLetter).All(char.IsUpper);

static SvgLayerCandidatesResult WriteSvgLayerCandidates(SvgStructureResult r, string artifactsDir)
{
    // Water: use word-boundary for "eau"/"lac" to avoid false matches like "Plateau".
    static bool IsWater(string s) =>
        ContainsWord(s, "eau") || ContainsWord(s, "eaux") || ContainsWord(s, "lac") ||
        MatchesAny(s, ["fleuve", "river", "canal", "water", "ruisseau", "lake"]);

    static bool IsOutline(string s) =>
        MatchesAny(s, ["outline", "contour", "boundary", "limite", "border"]);

    static bool IsTechnical(string s) =>
        ContainsWord(s, "fond") || ContainsWord(s, "base") || ContainsWord(s, "layer") ||
        MatchesAny(s, ["background", "template", "debug"]);

    static bool IsStreet(string s) =>
        MatchesAny(s, ["rue", "street", "route", "road", "boulevard", "avenue",
                        "ave", "chemin", "autoroute", "highway"]);

    static bool IsTransitLabel(string s) =>
        IsAllCapsAlpha(s) && s.Length >= 4 ||
        MatchesAny(s, ["station", "metro", "métro", "gare", "terminal", "transit"]);

    static bool IsParkLabel(string s) =>
        MatchesAny(s, ["pte", "parc", "park", "anse", "cap-", "trail", "garden",
                        "boise", "boisé", "bois", "vert", "green", "forest",
                        "forêt", "nature", "island", "ile", "île"]);

    var water     = new List<string>();
    var outline   = new List<string>();
    var technical = new List<string>();
    var street    = new List<string>();
    var borough   = new List<string>();

    foreach (var s in r.AllIds.Concat(r.AllClasses).Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if      (IsWater(s))     water.Add(s);
        else if (IsOutline(s))   outline.Add(s);
        else if (IsTechnical(s)) technical.Add(s);
        else if (IsStreet(s))    street.Add(s);
        else                     borough.Add(s);
    }

    var transit = new List<string>();
    var park    = new List<string>();
    var labels  = new List<string>();

    foreach (var t in r.AllTextLabels)
    {
        if      (IsTransitLabel(t)) transit.Add(t);
        else if (IsParkLabel(t))    park.Add(t);
        else                        labels.Add(t);
    }

    var totalIdsInspected     = r.AllIds.Count;
    var totalClassesInspected = r.AllClasses.Count;
    var totalLabelsInspected  = r.AllTextLabels.Count;

    var unknown = borough.Where(s => s.Length <= 1).Take(30).ToList();
    var trueBorough = borough.Where(s => s.Length > 1).ToList();

    var generationNotes = new[]
    {
        "Water: word-boundary match for eau/eaux/lac; substring match for fleuve/river/canal/water/lake.",
        "Outline: substring match for outline/contour/boundary/limite/border.",
        "Technical: word-boundary match for fond/base/layer; substring match for background/template.",
        "Street/Route: substring match for rue/street/route/road/boulevard/avenue/chemin/autoroute.",
        "Transit/Station: all-caps alphabetic labels (length >= 4) or station/metro/gare keywords.",
        "Park/Green: substring match for pte/parc/park/anse/trail/garden/bois/nature/forest/island.",
        "Borough/District: remaining IDs and classes not matched by the above patterns.",
        "Label: remaining text labels not classified as transit or park.",
    };

    var result = new SvgLayerCandidatesResult
    {
        WaterCandidates              = water.Take(30).ToList().AsReadOnly(),
        OutlineCandidates            = outline.Take(30).ToList().AsReadOnly(),
        TechnicalLayerCandidates     = technical.Take(30).ToList().AsReadOnly(),
        BoroughOrDistrictCandidates  = trueBorough.Take(30).ToList().AsReadOnly(),
        BoroughOrDistrictFullCount   = trueBorough.Count,
        StreetOrRouteCandidates      = street.Take(30).ToList().AsReadOnly(),
        TransitOrStationCandidates   = transit.Take(30).ToList().AsReadOnly(),
        ParkOrGreenSpaceCandidates   = park.Take(30).ToList().AsReadOnly(),
        LabelCandidates              = labels.Take(30).ToList().AsReadOnly(),
        UnknownCandidates            = unknown.AsReadOnly(),
        GenerationNotes              = generationNotes,
        TotalIdsInspected            = totalIdsInspected,
        TotalClassesInspected        = totalClassesInspected,
        TotalLabelsInspected         = totalLabelsInspected,
    };

    var artifact = new
    {
        schema                         = "pzmapforge.svg-layer-candidates.v0.1",
        claim_boundary                 = "planning_artifact_only_not_pz_load_tested",
        source_file_name               = r.SourceFileName,
        parse_status                   = r.ParseStatus,
        candidate_generation_method    = "metadata_name_pattern_only",
        candidate_generation_notes     = result.GenerationNotes,
        inspected_metadata_sources     = new[] { "ids", "classes", "text_labels" },
        total_id_values_inspected      = totalIdsInspected,
        total_class_values_inspected   = totalClassesInspected,
        total_text_labels_inspected    = totalLabelsInspected,
        total_metadata_values_inspected = totalIdsInspected + totalClassesInspected + totalLabelsInspected,
        parsed_as_geometry             = false,
        converted_to_map_geometry      = false,
        pz_assets_copied               = false,
        media_maps_touched             = false,
        playable_export_claimed        = false,
        water_candidates               = new { count = result.WaterCandidates.Count,             samples = result.WaterCandidates },
        outline_candidates             = new { count = result.OutlineCandidates.Count,            samples = result.OutlineCandidates },
        technical_layer_candidates     = new { count = result.TechnicalLayerCandidates.Count,     samples = result.TechnicalLayerCandidates },
        borough_or_district_candidates = new { count = result.BoroughOrDistrictFullCount,        samples = result.BoroughOrDistrictCandidates },
        street_or_route_candidates     = new { count = result.StreetOrRouteCandidates.Count,      samples = result.StreetOrRouteCandidates },
        transit_or_station_candidates  = new { count = result.TransitOrStationCandidates.Count,   samples = result.TransitOrStationCandidates },
        park_or_green_space_candidates = new { count = result.ParkOrGreenSpaceCandidates.Count,   samples = result.ParkOrGreenSpaceCandidates },
        label_candidates               = new { count = result.LabelCandidates.Count,              samples = result.LabelCandidates },
        unknown_candidates             = new { count = result.UnknownCandidates.Count,            samples = result.UnknownCandidates },
    };

    File.WriteAllText(
        Path.Combine(artifactsDir, "svg-layer-candidates.json"),
        JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true }),
        Encoding.UTF8);

    return result;
}

static (List<SelectedLayerItem> Items, string? Error) ReadSvgLayerSelection(string json)
{
    string[] bucketNames =
    [
        "water_candidates", "outline_candidates", "technical_layer_candidates",
        "borough_or_district_candidates", "street_or_route_candidates",
        "transit_or_station_candidates", "park_or_green_space_candidates",
        "label_candidates", "unknown_candidates",
    ];

    try
    {
        using var doc   = JsonDocument.Parse(json);
        var root        = doc.RootElement;
        var items       = new List<SelectedLayerItem>();

        foreach (var bucket in bucketNames)
        {
            if (!root.TryGetProperty(bucket, out var arr)) continue;
            if (arr.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in arr.EnumerateArray())
            {
                var selected = item.TryGetProperty("selected", out var sel)
                    && sel.ValueKind == JsonValueKind.True;
                if (!selected) continue;
                var value       = item.TryGetProperty("value",        out var v) ? v.GetString() ?? "" : "";
                var intendedUse = item.TryGetProperty("intended_use", out var u) ? u.GetString() ?? "" : "";
                var note        = item.TryGetProperty("operator_note",out var n) ? n.GetString() ?? "" : "";
                items.Add(new SelectedLayerItem(bucket, value, intendedUse, note));
            }
        }
        return (items, null);
    }
    catch (JsonException ex)
    {
        return ([], ex.Message);
    }
}

static void WriteSvgPlanningManifest(
    List<SelectedLayerItem> items, string sourceFileName, string artifactsDir)
{
    var byBucket = items
        .GroupBy(i => i.Bucket)
        .Select(g => new
        {
            bucket = g.Key,
            count  = g.Count(),
            items  = g.Select(i => new
            {
                value         = i.Value,
                intended_use  = i.IntendedUse,
                operator_note = i.OperatorNote,
            }).ToArray(),
        })
        .ToArray();

    var intendedUses = items
        .Select(i => i.IntendedUse)
        .Where(u => !string.IsNullOrWhiteSpace(u))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    var operatorNotes = items
        .Where(i => !string.IsNullOrWhiteSpace(i.OperatorNote))
        .Take(50)
        .Select(i => new { value = i.Value, note = i.OperatorNote })
        .ToArray();

    var manifest = new
    {
        schema                      = "pzmapforge.svg-planning-manifest.v0.1",
        claim_boundary              = "planning_artifact_only_not_pz_load_tested",
        source_selection_file_name  = sourceFileName,
        generated_from              = "svg-layer-selection-review.json",
        selected_count              = items.Count,
        selected_by_bucket          = byBucket,
        intended_uses               = intendedUses,
        operator_notes              = operatorNotes,
        planning_status             = "operator_selected_metadata_only",
        parsed_as_geometry          = false,
        converted_to_map_geometry   = false,
        exported_to_project_zomboid = false,
        pz_assets_copied            = false,
        media_maps_touched          = false,
        playable_export_claimed     = false,
    };

    File.WriteAllText(
        Path.Combine(artifactsDir, "svg-planning-manifest.json"),
        JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
        Encoding.UTF8);

    var md = BuildSvgPlanningManifestMarkdown(items, sourceFileName, intendedUses);
    File.WriteAllText(
        Path.Combine(artifactsDir, "svg-planning-manifest.md"),
        md,
        Encoding.UTF8);
}

static string BuildSvgPlanningManifestMarkdown(
    List<SelectedLayerItem> items, string sourceFileName, string[] intendedUses)
{
    var sb = new StringBuilder();
    sb.AppendLine("# SVG Planning Manifest");
    sb.AppendLine();
    sb.AppendLine("Claim boundary: planning_artifact_only_not_pz_load_tested");
    sb.AppendLine();
    sb.AppendLine($"Source: {sourceFileName}");
    sb.AppendLine($"Selected count: {items.Count}");
    sb.AppendLine("Planning status: operator_selected_metadata_only");
    sb.AppendLine();
    sb.AppendLine("## Selected Items");
    sb.AppendLine();
    foreach (var grp in items.GroupBy(i => i.Bucket))
    {
        sb.AppendLine($"### {grp.Key}");
        foreach (var item in grp)
        {
            var use  = string.IsNullOrEmpty(item.IntendedUse)  ? "" : $" | intended_use: {item.IntendedUse}";
            var note = string.IsNullOrEmpty(item.OperatorNote) ? "" : $" | note: {item.OperatorNote}";
            sb.AppendLine($"- {item.Value}{use}{note}");
        }
        sb.AppendLine();
    }
    if (intendedUses.Length > 0)
    {
        sb.AppendLine("## Intended Uses");
        sb.AppendLine();
        foreach (var u in intendedUses)
            sb.AppendLine($"- {u}");
        sb.AppendLine();
    }
    sb.AppendLine("## Non-claims");
    sb.AppendLine();
    sb.AppendLine("- No SVG geometry converted.");
    sb.AppendLine("- No SVG coordinates extracted.");
    sb.AppendLine("- No Project Zomboid export generated.");
    sb.AppendLine("- No media/maps writes.");
    sb.AppendLine("- No PZ assets copied or read.");
    return sb.ToString();
}

static string BuildSvgPlanningManifestHtml(List<SelectedLayerItem> items)
{
    var sb = new StringBuilder();
    sb.Append("\n    <h2>SVG Planning Manifest</h2>\n");
    sb.Append("    <p class=\"svg-note\">This is an inert planning manifest. It records selected SVG metadata only. It does not convert or export SVG geometry.</p>\n");

    sb.Append("    <table class=\"meta-tbl\">\n");
    sb.Append($"      <tr><th>selected_count</th><td>{items.Count}</td></tr>\n");
    sb.Append("      <tr><th>planning_status</th><td>operator_selected_metadata_only</td></tr>\n");
    sb.Append("    </table>\n");

    var intendedUses = items
        .Select(i => i.IntendedUse)
        .Where(u => !string.IsNullOrWhiteSpace(u))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (intendedUses.Count > 0)
    {
        sb.Append("    <p class=\"svg-sub\">Intended Uses</p>\n");
        sb.Append("    <div class=\"svg-chips\">");
        foreach (var u in intendedUses)
            sb.Append($"<span class=\"svg-chip\">{HtmlEncode(u)}</span>");
        sb.Append("</div>\n");
    }

    foreach (var grp in items.GroupBy(i => i.Bucket))
    {
        sb.Append($"    <p class=\"svg-sub\">{HtmlEncode(grp.Key)} ({grp.Count()})</p>\n");
        foreach (var item in grp)
        {
            sb.Append($"    <div class=\"review-item\"><span class=\"svg-chip\">{HtmlEncode(item.Value)}</span>");
            if (!string.IsNullOrEmpty(item.IntendedUse))
                sb.Append($" <span class=\"review-use\">{HtmlEncode(item.IntendedUse)}</span>");
            if (!string.IsNullOrEmpty(item.OperatorNote))
                sb.Append($" <span class=\"review-note\">{HtmlEncode(item.OperatorNote)}</span>");
            sb.Append("</div>\n");
        }
    }

    sb.Append("    <ul class=\"nc-list\">\n");
    sb.Append("      <li>No SVG geometry converted.</li>\n");
    sb.Append("      <li>No SVG coordinates extracted.</li>\n");
    sb.Append("      <li>No Project Zomboid export generated.</li>\n");
    sb.Append("      <li>No media/maps writes.</li>\n");
    sb.Append("      <li>No PZ assets copied or read.</li>\n");
    sb.Append("    </ul>\n");

    sb.Append("    <div class=\"arts\">\n");
    sb.Append("      <div class=\"art\"><a href=\"artifacts/svg-planning-manifest.json\">svg-planning-manifest.json</a><div class=\"desc\">SVG planning manifest (schema v0.1)</div></div>\n");
    sb.Append("      <div class=\"art\"><a href=\"artifacts/svg-planning-manifest.md\">svg-planning-manifest.md</a><div class=\"desc\">Human-readable planning manifest</div></div>\n");
    sb.Append("    </div>\n");

    return sb.ToString();
}

static void WriteSvgLayerSelectionReview(
    List<SelectedLayerItem> items, string sourceFileName, string artifactsDir)
{
    var artifact = new
    {
        schema                   = "pzmapforge.svg-layer-selection-review.v0.1",
        claim_boundary           = "planning_artifact_only_not_pz_load_tested",
        source_selection_file_name = sourceFileName,
        selection_status         = "reviewed",
        selected_count           = items.Count,
        selected_items           = items.Select(i => new
        {
            bucket        = i.Bucket,
            value         = i.Value,
            intended_use  = i.IntendedUse,
            operator_note = i.OperatorNote,
        }).ToArray(),
        parsed_as_geometry       = false,
        converted_to_map_geometry = false,
        pz_assets_copied         = false,
        media_maps_touched       = false,
        playable_export_claimed  = false,
    };

    File.WriteAllText(
        Path.Combine(artifactsDir, "svg-layer-selection-review.json"),
        JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true }),
        Encoding.UTF8);
}

static string BuildSvgLayerSelectionReviewHtml(List<SelectedLayerItem> items)
{
    var sb = new StringBuilder();
    sb.Append("\n    <h2>SVG Selection Review</h2>\n");
    sb.Append("    <p class=\"svg-note\">This review is metadata only. Selected candidates are not converted to SVG geometry. Selected candidates are not exported to Project Zomboid.</p>\n");
    sb.Append($"    <p class=\"section-note\">Selected: {items.Count} item(s)</p>\n");

    if (items.Count > 0)
    {
        var byBucket = items.GroupBy(i => i.Bucket);
        foreach (var grp in byBucket)
        {
            sb.Append($"    <p class=\"svg-sub\">{HtmlEncode(grp.Key)} ({grp.Count()})</p>\n");
            foreach (var item in grp)
            {
                sb.Append($"    <div class=\"review-item\"><span class=\"svg-chip\">{HtmlEncode(item.Value)}</span>");
                if (!string.IsNullOrEmpty(item.IntendedUse))
                    sb.Append($" <span class=\"review-use\">{HtmlEncode(item.IntendedUse)}</span>");
                if (!string.IsNullOrEmpty(item.OperatorNote))
                    sb.Append($" <span class=\"review-note\">{HtmlEncode(item.OperatorNote)}</span>");
                sb.Append("</div>\n");
            }
        }
    }

    sb.Append("    <div class=\"arts\">\n");
    sb.Append("      <div class=\"art\"><a href=\"artifacts/svg-layer-selection.input.json\">svg-layer-selection.input.json</a><div class=\"desc\">Operator-edited selection input</div></div>\n");
    sb.Append("      <div class=\"art\"><a href=\"artifacts/svg-layer-selection-review.json\">svg-layer-selection-review.json</a><div class=\"desc\">Selection review (schema v0.1)</div></div>\n");
    sb.Append("    </div>\n");

    return sb.ToString();
}

static void WriteSvgLayerSelectionTemplate(
    SvgLayerCandidatesResult c, string sourceFileName, string artifactsDir)
{
    static object[] ToItems(IReadOnlyList<string> candidates) =>
        [.. candidates.Select(v => new { value = v, selected = false, intended_use = "", operator_note = "" })];

    var template = new
    {
        schema                         = "pzmapforge.svg-layer-selection-template.v0.1",
        claim_boundary                 = "planning_artifact_only_not_pz_load_tested",
        source_file_name               = sourceFileName,
        selection_status               = "operator_review_required",
        generated_from                 = "svg-layer-candidates.json",
        candidate_generation_method    = "metadata_name_pattern_only",
        parsed_as_geometry             = false,
        converted_to_map_geometry      = false,
        pz_assets_copied               = false,
        media_maps_touched             = false,
        playable_export_claimed        = false,
        water_candidates               = ToItems(c.WaterCandidates),
        outline_candidates             = ToItems(c.OutlineCandidates),
        technical_layer_candidates     = ToItems(c.TechnicalLayerCandidates),
        borough_or_district_candidates = ToItems(c.BoroughOrDistrictCandidates),
        street_or_route_candidates     = ToItems(c.StreetOrRouteCandidates),
        transit_or_station_candidates  = ToItems(c.TransitOrStationCandidates),
        park_or_green_space_candidates = ToItems(c.ParkOrGreenSpaceCandidates),
        label_candidates               = ToItems(c.LabelCandidates),
        unknown_candidates             = ToItems(c.UnknownCandidates),
    };

    File.WriteAllText(
        Path.Combine(artifactsDir, "svg-layer-selection.template.json"),
        JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true }),
        Encoding.UTF8);
}

static string BuildSvgLayerSelectionHtml()
{
    var sb = new StringBuilder();
    sb.Append("\n    <h2>SVG Layer Selection Template</h2>\n");
    sb.Append("    <p class=\"svg-note\">Operator review required. Review the candidates and edit the template to mark intended layers.</p>\n");
    sb.Append("    <p class=\"section-note\">Selecting a candidate does not convert SVG geometry. This template is for future planning decisions only.</p>\n");
    sb.Append("    <div class=\"arts\">\n");
    sb.Append("      <div class=\"art\"><a href=\"artifacts/svg-layer-selection.template.json\">svg-layer-selection.template.json</a><div class=\"desc\">Operator-editable layer selection template (schema v0.1)</div></div>\n");
    sb.Append("    </div>\n");
    return sb.ToString();
}

static string BuildSvgLayerCandidatesHtml(SvgLayerCandidatesResult c)
{
    var sb = new StringBuilder();
    sb.Append("\n    <h2>SVG Layer Candidates</h2>\n");
    sb.Append("    <p class=\"svg-note\">These are metadata candidates only. No SVG geometry is converted. Candidates are derived from element IDs, class names, and text labels using name pattern matching only.</p>\n");
    var total = c.TotalIdsInspected + c.TotalClassesInspected + c.TotalLabelsInspected;
    sb.Append($"    <p class=\"section-note\">Metadata Values Inspected: {total:N0} ({c.TotalIdsInspected:N0} IDs &middot; {c.TotalClassesInspected:N0} class tokens &middot; {c.TotalLabelsInspected:N0} text labels)</p>\n");
    sb.Append("    <p class=\"section-note\">Method: metadata_name_pattern_only</p>\n");

    AppendCandidateBucket(sb, "Water",              c.WaterCandidates);
    AppendCandidateBucket(sb, "Outline / Boundary", c.OutlineCandidates);
    AppendCandidateBucket(sb, "Technical Layers",   c.TechnicalLayerCandidates);
    AppendCandidateBucket(sb, "Borough / District", c.BoroughOrDistrictCandidates, c.BoroughOrDistrictFullCount);
    AppendCandidateBucket(sb, "Street / Route",     c.StreetOrRouteCandidates);
    AppendCandidateBucket(sb, "Transit / Station",  c.TransitOrStationCandidates);
    AppendCandidateBucket(sb, "Park / Green Space", c.ParkOrGreenSpaceCandidates);
    AppendCandidateBucket(sb, "Text Labels",        c.LabelCandidates);
    AppendCandidateBucket(sb, "Unknown",            c.UnknownCandidates);

    sb.Append("    <div class=\"arts\">\n");
    sb.Append("      <div class=\"art\"><a href=\"artifacts/svg-layer-candidates.json\">svg-layer-candidates.json</a><div class=\"desc\">SVG layer candidate inventory (schema v0.1)</div></div>\n");
    sb.Append("    </div>\n");

    return sb.ToString();
}

static void AppendCandidateBucket(StringBuilder sb, string label, IReadOnlyList<string> items, int? fullCount = null)
{
    var displayCount = fullCount ?? items.Count;
    if (displayCount == 0) return;
    var truncNote = fullCount.HasValue && fullCount.Value > items.Count ? $" — showing {items.Count}" : string.Empty;
    sb.Append($"    <p class=\"svg-sub\">{HtmlEncode(label)} ({displayCount}{HtmlEncode(truncNote)})</p>\n");
    sb.Append("    <div class=\"svg-chips\">");
    foreach (var item in items)
        sb.Append($"<span class=\"svg-chip\">{HtmlEncode(item)}</span>");
    sb.Append("</div>\n");
}

sealed record SelectedLayerItem(string Bucket, string Value, string IntendedUse, string OperatorNote);

sealed class SvgLayerCandidatesResult
{
    public IReadOnlyList<string>  WaterCandidates              { get; init; } = [];
    public IReadOnlyList<string>  OutlineCandidates            { get; init; } = [];
    public IReadOnlyList<string>  TechnicalLayerCandidates     { get; init; } = [];
    public IReadOnlyList<string>  BoroughOrDistrictCandidates  { get; init; } = [];
    public int                    BoroughOrDistrictFullCount   { get; init; }
    public IReadOnlyList<string>  StreetOrRouteCandidates      { get; init; } = [];
    public IReadOnlyList<string>  TransitOrStationCandidates   { get; init; } = [];
    public IReadOnlyList<string>  ParkOrGreenSpaceCandidates   { get; init; } = [];
    public IReadOnlyList<string>  LabelCandidates              { get; init; } = [];
    public IReadOnlyList<string>  UnknownCandidates            { get; init; } = [];
    public IReadOnlyList<string>  GenerationNotes              { get; init; } = [];
    public int                    TotalIdsInspected            { get; init; }
    public int                    TotalClassesInspected        { get; init; }
    public int                    TotalLabelsInspected         { get; init; }
}

sealed class SvgStructureResult
{
    public string ParseStatus      { get; init; } = "parsed";
    public string ParseError       { get; init; } = string.Empty;
    public string SourceFileName   { get; init; } = string.Empty;
    public long   FileSizeBytes    { get; init; }
    public string RootElement      { get; init; } = "unknown";
    public string Width            { get; init; } = string.Empty;
    public string Height           { get; init; } = string.Empty;
    public string ViewBox          { get; init; } = string.Empty;
    public int    CountG           { get; init; }
    public int    CountPath        { get; init; }
    public int    CountPolyline    { get; init; }
    public int    CountPolygon     { get; init; }
    public int    CountLine        { get; init; }
    public int    CountRect        { get; init; }
    public int    CountText        { get; init; }
    public IReadOnlyList<string> SampleIds        { get; init; } = [];
    public IReadOnlyList<string> SampleClasses    { get; init; } = [];
    public IReadOnlyList<string> SampleTextLabels { get; init; } = [];
    // Full lists for classification (not written to structure JSON).
    public IReadOnlyList<string> AllIds           { get; init; } = [];
    public IReadOnlyList<string> AllClasses       { get; init; } = [];
    public IReadOnlyList<string> AllTextLabels    { get; init; } = [];
}
