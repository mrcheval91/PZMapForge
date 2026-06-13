using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlSpawnCenteredProofTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ScriptsDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts");

    private static string GenerateScript =>
        Path.Combine(ScriptsDir, "generate-spawn-centered-proof-pack.ps1");

    private static string RunScript =>
        Path.Combine(ScriptsDir, "run-spawn-centered-proof-build.ps1");

    private static string ReadmeFile =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");

    // -----------------------------------------------------------------------
    // Script existence
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateScript_Exists()
    {
        Assert.True(File.Exists(GenerateScript),
            $"Expected: {GenerateScript}");
    }

    [Fact]
    public void RunScript_Exists()
    {
        Assert.True(File.Exists(RunScript),
            $"Expected: {RunScript}");
    }

    // -----------------------------------------------------------------------
    // Generate script content
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateScript_ContainsMapId()
    {
        var text = File.ReadAllText(GenerateScript);
        Assert.Contains("deadmtl_spawn_centered_proof_v1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateScript_ContainsSpawnX()
    {
        var text = File.ReadAllText(GenerateScript);
        Assert.Contains("10650", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateScript_ContainsSpawnY()
    {
        var text = File.ReadAllText(GenerateScript);
        Assert.Contains("8250", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateScript_ContainsRoadWorldY_8248()
    {
        var text = File.ReadAllText(GenerateScript);
        Assert.Contains("8248", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateScript_ContainsRoadWorldY_8252()
    {
        var text = File.ReadAllText(GenerateScript);
        Assert.Contains("8252", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateScript_ContainsCorrectOriginX()
    {
        var text = File.ReadAllText(GenerateScript);
        Assert.Contains("10580", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateScript_ContainsCorrectOriginY()
    {
        var text = File.ReadAllText(GenerateScript);
        Assert.Contains("8200", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateScript_ContainsRoadPixelRect()
    {
        var text = File.ReadAllText(GenerateScript);
        // FillRectangle(b, 38, 48, 65, 5) — road pixel coords
        Assert.Contains("38, 48, 65, 5", text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Run script content
    // -----------------------------------------------------------------------

    [Fact]
    public void RunScript_ContainsInstallParameter()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("-Install", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_DefaultDoesNotInstall()
    {
        var text = File.ReadAllText(RunScript);
        // The default (no -Install) branch must exit 0 without writing to game folders.
        // Verify the guard: "if (-not $Install)" prevents unconditional install.
        Assert.Contains("if (-not $Install)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_ContainsMapId()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("deadmtl_spawn_centered_proof_v1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_ContainsSpawnReference()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("10650", text, StringComparison.Ordinal);
        Assert.Contains("8250",  text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_ContainsRoadCrossingReference()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("8248", text, StringComparison.Ordinal);
        Assert.Contains("8252", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_CallsGenerateScript()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("generate-spawn-centered-proof-pack.ps1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RunScript_CallsAuthoringBuildCommand()
    {
        var text = File.ReadAllText(RunScript);
        Assert.Contains("deadmtl-authoring-build", text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // README
    // -----------------------------------------------------------------------

    [Fact]
    public void Readme_ContainsSpawnCenteredSection()
    {
        var text = File.ReadAllText(ReadmeFile);
        Assert.Contains("Spawn-centered runtime proof", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Readme_ContainsRunProofBuildScript()
    {
        var text = File.ReadAllText(ReadmeFile);
        Assert.Contains("run-spawn-centered-proof-build.ps1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Readme_ContainsInstallFlag()
    {
        var text = File.ReadAllText(ReadmeFile);
        Assert.Contains("-Install", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Readme_ContainsRoadCoordinates()
    {
        var text = File.ReadAllText(ReadmeFile);
        Assert.Contains("8248", text, StringComparison.Ordinal);
        Assert.Contains("8252", text, StringComparison.Ordinal);
    }
}
