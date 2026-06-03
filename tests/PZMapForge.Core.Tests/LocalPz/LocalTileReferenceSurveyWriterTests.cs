using System.Text.Json;
using PZMapForge.Core.LocalPz;
using Xunit;

namespace PZMapForge.Core.Tests.LocalPz;

public sealed class LocalTileReferenceSurveyWriterTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-local-tile-reference-survey-tests", Path.GetRandomFileName());

    public LocalTileReferenceSurveyWriterTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    [Fact]
    public void Create_SetsSchemaClaimBoundarySourceAndTimestamp()
    {
        var result = CreateValidationResult();
        var timestamp = new DateTimeOffset(2026, 6, 2, 12, 34, 56, TimeSpan.Zero);

        var survey = LocalTileReferenceSurveyWriter.Create(
            result,
            ".local/pzmapforge/pz-install-config.json",
            timestamp);

        Assert.Equal("pzmapforge.local-tile-reference-survey.v0.1", survey.Schema);
        Assert.Equal("planning_artifact_only_not_pz_load_tested", survey.ClaimBoundary);
        Assert.Equal("2026-06-02T12:34:56.0000000Z", survey.GeneratedAtUtc);
        Assert.Equal(".local/pzmapforge/pz-install-config.json", survey.SourceConfigPath);
    }

    [Fact]
    public void Create_CopiesExtensionCountsDeterministically()
    {
        var result = CreateValidationResult();
        result.Summary.ExtensionCounts.Clear();
        result.Summary.ExtensionCounts[".png"] = 2;
        result.Summary.ExtensionCounts[".tiles"] = 1;
        result.Summary.ExtensionCounts[".pack"] = 3;

        var survey = LocalTileReferenceSurveyWriter.Create(
            result,
            "config.json",
            DateTimeOffset.UnixEpoch);

        Assert.Equal(new[] { ".pack", ".png", ".tiles" }, survey.ExtensionCounts.Keys);
        Assert.Equal(3, survey.ExtensionCounts[".pack"]);
        Assert.Equal(2, survey.ExtensionCounts[".png"]);
        Assert.Equal(1, survey.ExtensionCounts[".tiles"]);
    }

    [Fact]
    public void Create_CopiesPresenceFlagsFromValidationSummary()
    {
        var result = CreateValidationResult();

        var survey = LocalTileReferenceSurveyWriter.Create(
            result,
            "config.json",
            DateTimeOffset.UnixEpoch);

        Assert.True(survey.InstallRootExists);
        Assert.True(survey.TilesRootExists);
        Assert.True(survey.LikelyTileDataPresent);
        Assert.True(survey.PngPresent);
        Assert.True(survey.PackPresent);
        Assert.True(survey.TilesPresent);
        Assert.True(survey.LotPackPresent);
        Assert.True(survey.LotHeaderPresent);
        Assert.True(survey.BinPresent);
    }

    [Fact]
    public void Create_ForcesSafetyFlagsFalse()
    {
        var result = CreateValidationResult();
        result.Summary.PzAssetsCopied = true;
        result.Summary.MediaMapsTouched = true;

        var survey = LocalTileReferenceSurveyWriter.Create(
            result,
            "config.json",
            DateTimeOffset.UnixEpoch);

        Assert.False(survey.PzAssetsCopied);
        Assert.False(survey.MediaMapsTouched);
        Assert.False(survey.PlayableExportClaimed);
    }

    [Fact]
    public void Write_WritesJsonAndMarkdownUnderLocalOnly()
    {
        var result = CreateValidationResult();

        LocalTileReferenceSurveyWriter.Write(
            result,
            "config.json",
            _tempDir,
            DateTimeOffset.UnixEpoch);

        var localRoot = Path.Combine(_tempDir, ".local");
        var jsonPath = Path.Combine(localRoot, "local-tile-reference-survey.json");
        var markdownPath = Path.Combine(localRoot, "local-tile-reference-survey.md");

        Assert.True(File.Exists(jsonPath));
        Assert.True(File.Exists(markdownPath));
        Assert.StartsWith(
            Path.GetFullPath(localRoot),
            Path.GetFullPath(jsonPath),
            StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(
            Path.GetFullPath(localRoot),
            Path.GetFullPath(markdownPath),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_DoesNotCreateFilesOutsideLocalOrMediaMaps()
    {
        var result = CreateValidationResult();

        LocalTileReferenceSurveyWriter.Write(
            result,
            "config.json",
            _tempDir,
            DateTimeOffset.UnixEpoch);

        var rootFiles = Directory.EnumerateFiles(_tempDir, "*", SearchOption.TopDirectoryOnly).ToArray();

        Assert.Empty(rootFiles);
        Assert.False(Directory.Exists(Path.Combine(_tempDir, "media")));
        Assert.False(Directory.Exists(Path.Combine(_tempDir, "media", "maps")));
    }

    [Fact]
    public void Write_JsonRoundTrips()
    {
        var result = CreateValidationResult();

        LocalTileReferenceSurveyWriter.Write(
            result,
            "config.json",
            _tempDir,
            DateTimeOffset.UnixEpoch);

        var jsonPath = Path.Combine(_tempDir, ".local", "local-tile-reference-survey.json");
        var json = File.ReadAllText(jsonPath);
        var survey = JsonSerializer.Deserialize<LocalTileReferenceSurvey>(json);

        Assert.NotNull(survey);
        Assert.Equal("pzmapforge.local-tile-reference-survey.v0.1", survey.Schema);
        Assert.Equal("planning_artifact_only_not_pz_load_tested", survey.ClaimBoundary);
        Assert.False(survey.PzAssetsCopied);
        Assert.False(survey.MediaMapsTouched);
        Assert.False(survey.PlayableExportClaimed);
    }

    [Fact]
    public void Write_MarkdownStatesBoundaryAndNonClaims()
    {
        var result = CreateValidationResult();

        LocalTileReferenceSurveyWriter.Write(
            result,
            "config.json",
            _tempDir,
            DateTimeOffset.UnixEpoch);

        var markdownPath = Path.Combine(_tempDir, ".local", "local-tile-reference-survey.md");
        var markdown = File.ReadAllText(markdownPath);

        Assert.Contains("planning_artifact_only_not_pz_load_tested", markdown, StringComparison.Ordinal);
        Assert.Contains("It is not a tile catalog.", markdown, StringComparison.Ordinal);
        Assert.Contains("It does not claim a playable Project Zomboid export.", markdown, StringComparison.Ordinal);
        Assert.Contains("| pz_assets_copied | false |", markdown, StringComparison.Ordinal);
        Assert.Contains("| media_maps_touched | false |", markdown, StringComparison.Ordinal);
        Assert.Contains("| playable_export_claimed | false |", markdown, StringComparison.Ordinal);
    }

    private static LocalPzInstallValidationResult CreateValidationResult()
    {
        var result = new LocalPzInstallValidationResult
        {
            ClaimBoundary = LocalPzInstallValidationResult.ExpectedClaimBoundary
        };

        result.Summary.InstallRootExists = true;
        result.Summary.TilesRootExists = true;
        result.Summary.LikelyTileDataPresent = true;
        result.Summary.PngPresent = true;
        result.Summary.PackPresent = true;
        result.Summary.TilesPresent = true;
        result.Summary.LotPackPresent = true;
        result.Summary.LotHeaderPresent = true;
        result.Summary.BinPresent = true;
        result.Summary.PzAssetsCopied = false;
        result.Summary.MediaMapsTouched = false;
        result.Summary.ExtensionCounts[".png"] = 2;
        result.Summary.ExtensionCounts[".pack"] = 1;
        result.Summary.ExtensionCounts[".tiles"] = 1;
        result.Summary.ExtensionCounts[".lotpack"] = 1;
        result.Summary.ExtensionCounts[".lotheader"] = 1;
        result.Summary.ExtensionCounts[".bin"] = 1;

        return result;
    }
}