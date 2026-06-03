using System.Text;
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
    Console.Error.WriteLine("  app-export        --path <image> --palette <palette> [--output <dir>] [--run-name <name>] [--resize]");
    Console.Error.WriteLine("                    [--tiny-threshold <int>] [--large-threshold <int>]");
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
    var imagePath   = string.Empty;
    var palettePath = string.Empty;
    var outputDir   = string.Empty;
    var runName     = string.Empty;
    var resize      = false;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--path"      or "-p" && i + 1 < args.Length) imagePath   = args[++i];
        else if (args[i] is "--palette"            && i + 1 < args.Length) palettePath = args[++i];
        else if (args[i] is "--output"    or "-o" && i + 1 < args.Length) outputDir   = args[++i];
        else if (args[i] is "--run-name"  or "-n" && i + 1 < args.Length) runName     = args[++i];
        else if (args[i] is "--resize")                                     resize      = true;
    }

    if (string.IsNullOrWhiteSpace(imagePath))
    { Console.Error.WriteLine("app-export requires --path <image>");     return 1; }
    if (string.IsNullOrWhiteSpace(palettePath))
    { Console.Error.WriteLine("app-export requires --palette <palette>"); return 1; }

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

    // Step 9: write index.html
    var htmlPath = Path.Combine(outputFull, "index.html");
    var html     = BuildAppHtml(
        relativeImgSrc, relativeParsedSrc,
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
.preview-pair{display:grid;grid-template-columns:1fr 1fr;gap:.8em;margin-bottom:.6em}
.preview-col{}
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

<div class="workbench">

  <div class="left-panel">
    <h2>Map Preview</h2>
    <div class="preview-pair">
      <div class="preview-col">
        <div class="preview-lbl">Original Input</div>
        <img class="preview-img" src="{{relativeImgSrc}}" alt="Input: {{imageName}}">
      </div>
      <div class="preview-col">
        <div class="preview-lbl">Parsed Preview</div>
        <img class="preview-img" src="{{relativeParsedSrc}}" alt="Parsed preview">
      </div>
    </div>

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
    <p class="health-note">A blockout is <strong>not palette-clean</strong> when it contains colors outside the defined palette. Text labels and antialiasing can affect parsing. For accurate results, use a clean analysis image without text overlays or anti-aliased edges.</p>

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
