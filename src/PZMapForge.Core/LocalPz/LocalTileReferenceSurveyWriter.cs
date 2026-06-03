using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.LocalPz;

public static class LocalTileReferenceSurveyWriter
{
    public const string JsonFileName = "local-tile-reference-survey.json";
    public const string MarkdownFileName = "local-tile-reference-survey.md";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static LocalTileReferenceSurvey Create(
        LocalPzInstallValidationResult validationResult,
        string sourceConfigPath)
    {
        return Create(validationResult, sourceConfigPath, DateTimeOffset.UtcNow);
    }

    public static LocalTileReferenceSurvey Create(
        LocalPzInstallValidationResult validationResult,
        string sourceConfigPath,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        if (string.IsNullOrWhiteSpace(sourceConfigPath))
            throw new ArgumentException("Source config path is required.", nameof(sourceConfigPath));

        var summary = validationResult.Summary;

        var survey = new LocalTileReferenceSurvey
        {
            Schema = LocalTileReferenceSurvey.SchemaId,
            ClaimBoundary = LocalTileReferenceSurvey.ExpectedClaimBoundary,
            GeneratedAtUtc = generatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            SourceConfigPath = sourceConfigPath,
            InstallRootExists = summary.InstallRootExists,
            TilesRootExists = summary.TilesRootExists,
            LikelyTileDataPresent = summary.LikelyTileDataPresent,
            PngPresent = summary.PngPresent,
            PackPresent = summary.PackPresent,
            TilesPresent = summary.TilesPresent,
            LotPackPresent = summary.LotPackPresent,
            LotHeaderPresent = summary.LotHeaderPresent,
            BinPresent = summary.BinPresent,
            PzAssetsCopied = false,
            MediaMapsTouched = false,
            PlayableExportClaimed = false
        };

        foreach (var item in summary.ExtensionCounts)
            survey.ExtensionCounts[item.Key] = item.Value;

        return survey;
    }

    public static LocalTileReferenceSurvey Write(
        LocalPzInstallValidationResult validationResult,
        string sourceConfigPath,
        string repoRoot)
    {
        return Write(validationResult, sourceConfigPath, repoRoot, DateTimeOffset.UtcNow);
    }

    public static LocalTileReferenceSurvey Write(
        LocalPzInstallValidationResult validationResult,
        string sourceConfigPath,
        string repoRoot,
        DateTimeOffset generatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
            throw new ArgumentException("Repository root is required.", nameof(repoRoot));

        var fullRepoRoot = Path.GetFullPath(repoRoot);
        var localOutputRoot = Path.GetFullPath(Path.Combine(fullRepoRoot, ".local"));

        Directory.CreateDirectory(localOutputRoot);

        var survey = Create(validationResult, sourceConfigPath, generatedAtUtc);

        var jsonPath = Path.Combine(localOutputRoot, JsonFileName);
        var markdownPath = Path.Combine(localOutputRoot, MarkdownFileName);

        var json = JsonSerializer.Serialize(survey, JsonOptions) + Environment.NewLine;
        File.WriteAllText(jsonPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        File.WriteAllText(
            markdownPath,
            BuildMarkdown(survey),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return survey;
    }

    private static string BuildMarkdown(LocalTileReferenceSurvey survey)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Local tile reference survey");
        builder.AppendLine();
        builder.AppendLine("Claim boundary: planning_artifact_only_not_pz_load_tested");
        builder.AppendLine();
        builder.AppendLine("This is a local-only planning artifact.");
        builder.AppendLine("It is not a tile catalog.");
        builder.AppendLine("It does not claim a playable Project Zomboid export.");
        builder.AppendLine("It does not copy PZ assets.");
        builder.AppendLine("It does not read PZ asset contents.");
        builder.AppendLine("It does not touch media/maps.");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine($"| schema | {survey.Schema} |");
        builder.AppendLine($"| generated_at_utc | {survey.GeneratedAtUtc} |");
        builder.AppendLine($"| source_config_path | {survey.SourceConfigPath} |");
        builder.AppendLine($"| install_root_exists | {BoolText(survey.InstallRootExists)} |");
        builder.AppendLine($"| tiles_root_exists | {BoolText(survey.TilesRootExists)} |");
        builder.AppendLine($"| likely_tile_data_present | {BoolText(survey.LikelyTileDataPresent)} |");
        builder.AppendLine($"| png_present | {BoolText(survey.PngPresent)} |");
        builder.AppendLine($"| pack_present | {BoolText(survey.PackPresent)} |");
        builder.AppendLine($"| tiles_present | {BoolText(survey.TilesPresent)} |");
        builder.AppendLine($"| lotpack_present | {BoolText(survey.LotPackPresent)} |");
        builder.AppendLine($"| lotheader_present | {BoolText(survey.LotHeaderPresent)} |");
        builder.AppendLine($"| bin_present | {BoolText(survey.BinPresent)} |");
        builder.AppendLine($"| pz_assets_copied | {BoolText(survey.PzAssetsCopied)} |");
        builder.AppendLine($"| media_maps_touched | {BoolText(survey.MediaMapsTouched)} |");
        builder.AppendLine($"| playable_export_claimed | {BoolText(survey.PlayableExportClaimed)} |");
        builder.AppendLine();
        builder.AppendLine("## Extension counts");
        builder.AppendLine();
        builder.AppendLine("| Extension | Count |");
        builder.AppendLine("|---|---:|");

        if (survey.ExtensionCounts.Count == 0)
        {
            builder.AppendLine("| none | 0 |");
        }
        else
        {
            foreach (var item in survey.ExtensionCounts)
                builder.AppendLine($"| {item.Key} | {item.Value.ToString(CultureInfo.InvariantCulture)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Non-claims");
        builder.AppendLine();
        builder.AppendLine("- No CLI command is added by Slice 3A-3.");
        builder.AppendLine("- No tile catalog is generated.");
        builder.AppendLine("- No semantic kind mapping is generated.");
        builder.AppendLine("- No lotpack, lotheader, or bin output is generated.");
        builder.AppendLine("- No playable Project Zomboid export is claimed.");

        return builder.ToString();
    }

    private static string BoolText(bool value)
    {
        return value ? "true" : "false";
    }
}