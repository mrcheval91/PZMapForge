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
    Console.Error.WriteLine("  plan-check        --path <path> [--tiny-threshold <int>] [--large-threshold <int>]");
    Console.Error.WriteLine("  plan-export       --path <path> [--output <dir>] [--tiny-threshold <int>] [--large-threshold <int>]");
    return 1;
}

return args[0] switch
{
    "palette-check"     => PaletteCheckCommand(args[1..]),
    "parsed-cell-check" => ParsedCellCheckCommand(args[1..]),
    "region-check"      => RegionCheckCommand(args[1..]),
    "primitive-check"   => PrimitiveCheckCommand(args[1..]),
    "plan-check"        => PlanCheckCommand(args[1..]),
    "plan-export"       => PlanExportCommand(args[1..]),
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
        planResult);

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

static int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"Unknown command: {cmd}");
    Console.Error.WriteLine("Available commands: palette-check, parsed-cell-check, region-check, primitive-check, plan-check, plan-export");
    return 1;
}
