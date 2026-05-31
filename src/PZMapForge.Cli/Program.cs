using PZMapForge.Core.Palette;
using PZMapForge.Core.ParsedCell;

// Claim boundary: PZMapForge CLI is a planning tool only.
// It does not produce a playable Project Zomboid export.

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: pzmapforge <command> [options]");
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  palette-check    --palette <path>");
    Console.Error.WriteLine("  parsed-cell-check --path <path>");
    return 1;
}

return args[0] switch
{
    "palette-check"     => PaletteCheckCommand(args[1..]),
    "parsed-cell-check" => ParsedCellCheckCommand(args[1..]),
    _ => UnknownCommand(args[0]),
};

static int PaletteCheckCommand(string[] args)
{
    var palettePath = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--palette" or "-p")
        {
            palettePath = args[i + 1];
            break;
        }
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

    if (result.IsValid)
    {
        Console.WriteLine("Status:     OK");
        return 0;
    }

    Console.WriteLine("Status:     INVALID");
    foreach (var error in result.Errors)
        Console.Error.WriteLine($"  error: {error}");
    return 1;
}

static int ParsedCellCheckCommand(string[] args)
{
    var jsonPath = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--path" or "-p")
        {
            jsonPath = args[i + 1];
            break;
        }
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

    if (result.IsValid)
    {
        Console.WriteLine("Status:     OK");
        return 0;
    }

    Console.WriteLine("Status:     INVALID");
    foreach (var error in result.Errors)
        Console.Error.WriteLine($"  error: {error}");
    return 1;
}

static int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"Unknown command: {cmd}");
    Console.Error.WriteLine("Available commands: palette-check, parsed-cell-check");
    return 1;
}
