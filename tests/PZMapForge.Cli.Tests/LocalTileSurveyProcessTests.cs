using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for the local-tile-survey CLI command.
/// All tests use temporary fake install directories only.
/// No real PZ install is required.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public sealed class LocalTileSurveyProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-local-tile-survey-tests", Path.GetRandomFileName());

    public LocalTileSurveyProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    // Create a minimal fake PZ install: installRoot/ and installRoot/media/
    private string CreateFakeInstall()
    {
        var installRoot = Path.Combine(_tempDir, "fake-pz-install");
        Directory.CreateDirectory(Path.Combine(installRoot, "media"));
        return installRoot;
    }

    private string WriteConfig(string installRoot)
    {
        var configPath = Path.Combine(_tempDir, "pz-install-config.json");
        File.WriteAllText(configPath,
            $"{{" +
            $"\"schema\":\"pzmapforge.local-pz-install-config.v0.1\"," +
            $"\"claim_boundary\":\"planning_artifact_only_not_pz_load_tested\"," +
            $"\"pz_install_root\":\"{installRoot.Replace("\\", "\\\\")}\"," +
            $"\"tiles_root\":\"media\"," +
            $"\"allow_asset_copy\":false," +
            $"\"allow_media_maps_write\":false," +
            $"\"tile_reference_mode\":\"local_reference_only\"," +
            $"\"notes\":\"test config\"" +
            $"}}",
            Encoding.UTF8);
        return configPath;
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(CliProjectPath);
        psi.ArgumentList.Add("--configuration");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("--no-build");
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Test 1: valid fake config writes survey artifacts
    // -----------------------------------------------------------------------

    [Fact]
    public void LocalTileSurvey_ValidFakeConfig_WritesArtifacts()
    {
        var installRoot = CreateFakeInstall();
        var configPath  = WriteConfig(installRoot);
        var outputDir   = Path.Combine(_tempDir, ".local");

        var (code, stdout, stderr) = RunCli(
            "local-tile-survey",
            "--config", configPath,
            "--output", outputDir);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(Path.Combine(outputDir, "local-tile-reference-survey.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, "local-tile-reference-survey.md")));
        Assert.Contains("Status:                   OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 2: refuses non-.local output path
    // -----------------------------------------------------------------------

    [Fact]
    public void LocalTileSurvey_NonLocalOutput_ExitsOne()
    {
        var installRoot = CreateFakeInstall();
        var configPath  = WriteConfig(installRoot);
        var badOutput   = _tempDir; // no ".local" final segment

        var (code, _, stderr) = RunCli(
            "local-tile-survey",
            "--config", configPath,
            "--output", badOutput);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 3: exits nonzero when --config is missing
    // -----------------------------------------------------------------------

    [Fact]
    public void LocalTileSurvey_MissingConfig_ExitsOne()
    {
        var (code, _, stderr) = RunCli("local-tile-survey");

        Assert.Equal(1, code);
        Assert.Contains("--config", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 4: JSON contains required claim boundary fields
    // -----------------------------------------------------------------------

    [Fact]
    public void LocalTileSurvey_Json_ContainsClaimBoundaryFields()
    {
        var installRoot = CreateFakeInstall();
        var configPath  = WriteConfig(installRoot);
        var outputDir   = Path.Combine(_tempDir, ".local");

        var (code, _, _) = RunCli(
            "local-tile-survey",
            "--config", configPath,
            "--output", outputDir);

        Assert.Equal(0, code);

        var jsonPath = Path.Combine(outputDir, "local-tile-reference-survey.json");
        var doc      = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root     = doc.RootElement;

        Assert.Equal("pzmapforge.local-tile-reference-survey.v0.1",
            root.GetProperty("schema").GetString());
        Assert.Equal("planning_artifact_only_not_pz_load_tested",
            root.GetProperty("claim_boundary").GetString());
        Assert.False(root.GetProperty("pz_assets_copied").GetBoolean());
        Assert.False(root.GetProperty("media_maps_touched").GetBoolean());
        Assert.False(root.GetProperty("playable_export_claimed").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 5: markdown contains claim boundary and non-claims text
    // -----------------------------------------------------------------------

    [Fact]
    public void LocalTileSurvey_Markdown_ContainsClaimBoundaryAndNonClaims()
    {
        var installRoot = CreateFakeInstall();
        var configPath  = WriteConfig(installRoot);
        var outputDir   = Path.Combine(_tempDir, ".local");

        var (code, _, _) = RunCli(
            "local-tile-survey",
            "--config", configPath,
            "--output", outputDir);

        Assert.Equal(0, code);

        var mdPath  = Path.Combine(outputDir, "local-tile-reference-survey.md");
        var content = File.ReadAllText(mdPath);

        Assert.Contains("planning_artifact_only_not_pz_load_tested",
            content, StringComparison.Ordinal);
        Assert.Contains("No tile catalog is generated", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No playable Project Zomboid export", content, StringComparison.OrdinalIgnoreCase);
    }
}
