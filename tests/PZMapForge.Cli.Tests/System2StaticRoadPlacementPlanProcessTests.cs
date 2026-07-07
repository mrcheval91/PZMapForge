using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadPlacementPlanProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-plan-cli", Path.GetRandomFileName());

    public System2StaticRoadPlacementPlanProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private static string SampleExtractJson =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "system2-static-road-sample-extract", "system2_static_road_sample_extract.json");

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
        psi.ArgumentList.Add(CliProject);
        psi.ArgumentList.Add("--configuration");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("--no-build");
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    private static (int ExitCode, string Stdout, string Stderr) RunPowerShell(string script)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "powershell",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(script);

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Minimal extract JSON fixture for CLI command tests
    // -----------------------------------------------------------------------

    private string MakeMinimalExtractJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-extract.v1",
  "status": "EXTRACT_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_contract": "test",
  "origin_x": 10580, "origin_y": 8200, "width": 220, "height": 170,
  "scale": { "pixels_per_meter": 1, "meters_per_pixel": 1, "pz_tiles_per_pixel": 1 },
  "layers": [
    {
      "id": "static_roads_local", "file": "layers/local.png", "class": "local_street",
      "non_empty_pixels": 5, "intents": ["local_street_asphalt"],
      "runs": [
        { "y": 50, "x_start": 20, "x_end": 24,
          "world_y": 8250, "world_x_start": 10600, "world_x_end": 10604,
          "intent": "local_street_asphalt", "color": "#404040" }
      ],
      "nodes": []
    },
    {
      "id": "static_roads_alleys", "file": "layers/alleys.png", "class": "alley_ruelle",
      "non_empty_pixels": 3, "intents": ["alley_ruelle_asphalt"],
      "runs": [
        { "y": 70, "x_start": 25, "x_end": 27,
          "world_y": 8270, "world_x_start": 10605, "world_x_end": 10607,
          "intent": "alley_ruelle_asphalt", "color": "#303030" }
      ],
      "nodes": []
    },
    {
      "id": "static_roads_service", "file": "layers/service.png", "class": "service_lane",
      "non_empty_pixels": 2, "intents": ["service_lane"],
      "runs": [
        { "y": 40, "x_start": 130, "x_end": 131,
          "world_y": 8240, "world_x_start": 10710, "world_x_end": 10711,
          "intent": "service_lane", "color": "#505050" }
      ],
      "nodes": []
    },
    {
      "id": "static_roads_parking_access", "file": "layers/parking.png", "class": "parking_access",
      "non_empty_pixels": 2, "intents": ["parking_access"],
      "runs": [
        { "y": 80, "x_start": 140, "x_end": 141,
          "world_y": 8280, "world_x_start": 10720, "world_x_end": 10721,
          "intent": "parking_access", "color": "#606060" }
      ],
      "nodes": []
    },
    {
      "id": "static_pedestrian_cuts", "file": "layers/ped.png", "class": "pedestrian_cut",
      "non_empty_pixels": 3, "intents": ["sidewalk_or_pedestrian_cut"],
      "runs": [
        { "y": 90, "x_start": 60, "x_end": 62,
          "world_y": 8290, "world_x_start": 10640, "world_x_end": 10642,
          "intent": "sidewalk_or_pedestrian_cut", "color": "#B0B0B0" }
      ],
      "nodes": []
    },
    {
      "id": "static_road_nodes", "file": "layers/nodes.png", "class": "intersection_turn_deadend_nodes",
      "non_empty_pixels": 3, "intents": ["intersection_node","road_turn_node","dead_end_node"],
      "runs": [],
      "nodes": [
        { "pixel_x": 70, "pixel_y": 52, "world_x": 10650, "world_y": 8252,
          "intent": "intersection_node", "color": "#FF00FF" },
        { "pixel_x": 120, "pixel_y": 55, "world_x": 10700, "world_y": 8255,
          "intent": "road_turn_node", "color": "#00FFFF" },
        { "pixel_x": 115, "pixel_y": 72, "world_x": 10695, "world_y": 8272,
          "intent": "dead_end_node", "color": "#FF9900" }
      ]
    }
  ],
  "totals": { "layer_count": 6, "non_empty_pixels": 18, "unknown_opaque_pixels": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false, "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path = Path.Combine(_tempDir, "extract.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // CLI command: exit zero on valid fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnValidExtract()
    {
        var input      = MakeMinimalExtractJson();
        var outputJson = Path.Combine(_tempDir, ".local", "plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "plan-summary.txt");

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-placement-plan",
            "--input",   input,
            "--output",  outputJson,
            "--summary", summaryTxt);

        Assert.True(code == 0,
            $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // CLI command: output files
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var input      = MakeMinimalExtractJson();
        var outputJson = Path.Combine(_tempDir, ".local", "plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "plan-summary.txt");

        RunCli("system2-build-static-road-placement-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.True(File.Exists(outputJson), "plan.json not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input      = MakeMinimalExtractJson();
        var outputJson = Path.Combine(_tempDir, ".local", "plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "plan-summary.txt");

        RunCli("system2-build-static-road-placement-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.True(File.Exists(summaryTxt), "plan-summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // CLI command: JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("PLAN_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input      = MakeMinimalExtractJson();
        var outputJson = Path.Combine(_tempDir, ".local", "plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "plan-summary.txt");

        RunCli("system2-build-static-road-placement-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.Contains(expected, File.ReadAllText(outputJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: intent and role strings in JSON
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("local_street_asphalt")]
    [InlineData("alley_ruelle_asphalt")]
    [InlineData("road_surface")]
    [InlineData("pedestrian_cut")]
    [InlineData("road_node")]
    public void Cli_OutputJson_ContainsIntentOrRole(string expected)
    {
        var input      = MakeMinimalExtractJson();
        var outputJson = Path.Combine(_tempDir, ".local", "plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "plan-summary.txt");

        RunCli("system2-build-static-road-placement-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.Contains(expected, File.ReadAllText(outputJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: placement_count equals extract non_empty_pixels (no collapse)
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_PlacementCountEqualsExtractNonEmptyPixels()
    {
        var input      = MakeMinimalExtractJson();
        var outputJson = Path.Combine(_tempDir, ".local", "plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "plan-summary.txt");

        RunCli("system2-build-static-road-placement-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        using var extractDoc = JsonDocument.Parse(File.ReadAllText(input));
        using var planDoc    = JsonDocument.Parse(File.ReadAllText(outputJson));

        var extractNonEmpty  = extractDoc.RootElement.GetProperty("totals")
            .GetProperty("non_empty_pixels").GetInt32();
        var planCount = planDoc.RootElement.GetProperty("totals")
            .GetProperty("placement_count").GetInt32();

        Assert.Equal(extractNonEmpty, planCount);
    }

    // -----------------------------------------------------------------------
    // CLI command: missing args -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-placement-plan");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CLI command: output not under .local -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input     = MakeMinimalExtractJson();
        var badOutput = Path.Combine(_tempDir, "plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "plan-summary.txt");

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-placement-plan",
            "--input", input, "--output", badOutput, "--summary", summaryTxt);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command list
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsPlacementPlanCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-placement-plan", stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadPlacementPlanHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-placement-plan.ps1");

    private static string ScriptsDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts");

    private static string PlanDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-placement-plan");

    private static string PlanJson =>
        Path.Combine(PlanDir, "system2_static_road_placement_plan.json");

    private static string PlanSummary =>
        Path.Combine(PlanDir, "system2_static_road_placement_plan.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_SYSTEM2_STATIC_ROAD_PLACEMENT_PLAN.md");

    private static string ReadmePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");

    private static (int ExitCode, string Stdout, string Stderr) RunScript()
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "powershell",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(HelperScript);

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Static content: forbidden strings
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_DoesNotContainCompileWorldgen() =>
        Assert.DoesNotContain("compile-worldgen", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    [Fact]
    public void HelperScript_DoesNotContainWorldGenOverrideLua() =>
        Assert.DoesNotContain("WorldGenOverride.lua", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    [Fact]
    public void HelperScript_DoesNotContainLotpack() =>
        Assert.DoesNotContain(".lotpack", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Doc and README
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_MentionsNoRuntimeProof()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("NOT_RUNTIME_PROVEN", StringComparison.Ordinal) ||
            text.Contains("not runtime proof", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Runtime proof is NOT", StringComparison.OrdinalIgnoreCase),
            "doc should state no runtime proof");
    }

    [Fact]
    public void Doc_MentionsNoLotpack()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("lotpack", StringComparison.OrdinalIgnoreCase),
            "doc should mention lotpack claim boundary");
    }

    [Fact]
    public void Readme_MentionsRunPlacementPlanScript() =>
        Assert.Contains("run-system2-static-road-placement-plan.ps1",
            File.ReadAllText(ReadmePath), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Process: run helper script
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunScript();
        Assert.True(code == 0,
            $"Script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void HelperScript_CreatesPlacementPlanJson()
    {
        RunScript();
        Assert.True(File.Exists(PlanJson), $"Expected: {PlanJson}");
    }

    [Fact]
    public void HelperScript_CreatesPlacementPlanSummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(PlanSummary), $"Expected: {PlanSummary}");
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(PlanDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(PlanDir)
            ? Directory.GetFiles(PlanDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
