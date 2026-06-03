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
    Console.Error.WriteLine("  app-export        --path <image> --palette <palette> [--output <dir>] [--resize]");
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
    var resize      = false;

    for (var i = 0; i < args.Length; i++)
    {
        if      (args[i] is "--path"    or "-p" && i + 1 < args.Length) imagePath   = args[++i];
        else if (args[i] is "--palette"          && i + 1 < args.Length) palettePath = args[++i];
        else if (args[i] is "--output"  or "-o" && i + 1 < args.Length) outputDir   = args[++i];
        else if (args[i] is "--resize")                                   resize      = true;
    }

    if (string.IsNullOrWhiteSpace(imagePath))
    { Console.Error.WriteLine("app-export requires --path <image>");     return 1; }
    if (string.IsNullOrWhiteSpace(palettePath))
    { Console.Error.WriteLine("app-export requires --palette <palette>"); return 1; }

    if (string.IsNullOrWhiteSpace(outputDir))
        outputDir = Path.Combine(Directory.GetCurrentDirectory(), ".local", "app");

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

    // Step 8: write index.html
    var htmlPath = Path.Combine(outputFull, "index.html");
    var html     = BuildAppHtml(
        imagePath, grid.Width, grid.Height, parseResult.Resized,
        regions.TotalRegions, primitives.PrimitiveCount,
        planResult.RecommendationCount, planResult.Summary.WarningCount,
        planOpts!);
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

static string BuildAppHtml(
    string imagePath, int width, int height, bool resized,
    int regions, int primitives, int recommendations, int warnings,
    PlanningRuleOptions planOpts)
{
    var imageName = HtmlEncode(Path.GetFileName(imagePath));
    var now       = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
    var warnClass = warnings > 0 ? "warn" : "ok";

    return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>PZMapForge - Local Image-to-Map Report</title>
<style>
body { font-family: monospace; max-width: 900px; margin: 2em auto; padding: 0 1em; background: #1a1a1a; color: #d4d4d4; }
h1 { color: #a8d8a8; border-bottom: 1px solid #444; padding-bottom: 0.3em; }
h2 { color: #88b888; margin-top: 1.5em; }
.boundary { background: #2a2a1a; border: 1px solid #666633; padding: 0.7em 1em; margin: 1em 0; }
.boundary code { color: #cccc66; }
table { border-collapse: collapse; width: 100%; margin: 0.5em 0; }
th, td { border: 1px solid #444; padding: 0.4em 0.7em; text-align: left; }
th { background: #2a2a2a; color: #aaa; }
a { color: #88c4e8; }
.nonclaim { color: #cc9966; }
.ok { color: #88d888; }
.warn { color: #ddaa55; }
</style>
</head>
<body>
<h1>PZMapForge &mdash; Local Image-to-Map Report</h1>

<div class="boundary">
<strong>Claim boundary:</strong> <code>planning_artifact_only_not_pz_load_tested</code><br>
This report is a local planning artifact only. It is not a playable Project Zomboid export.
</div>

<h2>Input</h2>
<table>
<tr><th>Field</th><th>Value</th></tr>
<tr><td>Image</td><td>{{imageName}}</td></tr>
<tr><td>Dimensions</td><td>{{width}} x {{height}} px</td></tr>
<tr><td>Resized</td><td>{{resized}}</td></tr>
<tr><td>Generated</td><td>{{now}}</td></tr>
</table>

<h2>Pipeline Results</h2>
<table>
<tr><th>Stage</th><th>Result</th></tr>
<tr><td>Regions</td><td>{{regions}}</td></tr>
<tr><td>Primitives</td><td>{{primitives}}</td></tr>
<tr><td>Plan recommendations</td><td>{{recommendations}}</td></tr>
<tr><td>Warnings</td><td><span class="{{warnClass}}">{{warnings}}</span></td></tr>
<tr><td>Tiny threshold</td><td>{{planOpts.TinyBuildingPixelThreshold}} px</td></tr>
<tr><td>Large threshold</td><td>{{planOpts.LargeGroundPixelThreshold}} px</td></tr>
</table>

<h2>Artifacts</h2>
<table>
<tr><th>File</th><th>Description</th></tr>
<tr><td><a href="artifacts/parsed-cell.json">artifacts/parsed-cell.json</a></td><td>Parsed cell grid (schema v0.1)</td></tr>
<tr><td><a href="artifacts/regions.json">artifacts/regions.json</a></td><td>Extracted regions</td></tr>
<tr><td><a href="artifacts/regions-report.md">artifacts/regions-report.md</a></td><td>Regions report</td></tr>
<tr><td><a href="artifacts/primitives.json">artifacts/primitives.json</a></td><td>Classified primitives</td></tr>
<tr><td><a href="artifacts/primitives-report.md">artifacts/primitives-report.md</a></td><td>Primitives report</td></tr>
<tr><td><a href="artifacts/plan-recommendations.json">artifacts/plan-recommendations.json</a></td><td>Plan recommendations (schema v0.1)</td></tr>
<tr><td><a href="artifacts/plan-report.md">artifacts/plan-report.md</a></td><td>Plan report</td></tr>
</table>

<h2 class="nonclaim">Non-claims</h2>
<ul class="nonclaim">
<li>This report does not represent a playable Project Zomboid map.</li>
<li>No Project Zomboid assets were copied or read.</li>
<li>No media/maps directory was written.</li>
<li>No lotpack, lotheader, or bin files were generated.</li>
<li>Tile GIDs are not assigned. No TileZed tile references are included.</li>
<li>Build 41 / Build 42 compatibility is not claimed.</li>
</ul>

<hr>
<p style="color:#666">Generated by PZMapForge &mdash; planning_artifact_only_not_pz_load_tested</p>
</body>
</html>
""";
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
