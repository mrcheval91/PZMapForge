using PZMapForge.Core.Palette;

// Claim boundary: PZMapForge CLI is a planning tool only.
// It does not produce a playable Project Zomboid export.

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: pzmapforge <command> [options]");
    Console.Error.WriteLine("Commands: palette-check --palette <path>");
    return 1;
}

return args[0] switch
{
    "palette-check" => PaletteCheckCommand(args[1..]),
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

static int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"Unknown command: {cmd}");
    Console.Error.WriteLine("Available commands: palette-check");
    return 1;
}
