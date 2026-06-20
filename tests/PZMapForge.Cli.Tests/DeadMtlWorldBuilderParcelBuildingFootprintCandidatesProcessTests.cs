using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderParcelBuildingFootprintCandidatesProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map30a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderParcelBuildingFootprintCandidatesProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-parcel-building-footprint-candidates.ps1");

    private static readonly string s_canonicalPolicyPath = Path.Combine(
        RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder",
        "parcel-building-footprint-policies.json");

    private string LotFillJson   => Path.Combine(_tempDir, "lot-fill.local.json");
    private string PolicyJson    => Path.Combine(_tempDir, "policy.local.json");
    private string OutputRoot    => Path.Combine(_tempDir, "output.local");
    private string OutputJson    => Path.Combine(OutputRoot, "footprints.json");
    private string OutputCsv     => Path.Combine(OutputRoot, "footprints.csv");
    private string OutputChkCsv  => Path.Combine(OutputRoot, "footprints-checks.csv");
    private string OutputPng     => Path.Combine(OutputRoot, "footprints.png");
    private string OutputHtml    => Path.Combine(OutputRoot, "footprints.html");
    private string Summary       => Path.Combine(OutputRoot, "footprints-summary.txt");

    private void WriteFixtures()
    {
        // 14×27 NORTH BLUE lot — eligible, will get a footprint
        File.WriteAllText(LotFillJson, """
        {
          "lot_sizing_policy_version": "MAP29C_V1",
          "components": [
            { "component_id": "COMP_BLUE", "parcel_class": "BLUE_RESIDENTIAL" }
          ],
          "lots": [
            {
              "component_id": "COMP_BLUE",
              "lot_id": "LOT_BLUE_N",
              "frontage_direction": "NORTH",
              "x1": 0, "y1": 0, "x2": 13, "y2": 26,
              "width": 14, "height": 27, "tile_count": 378,
              "shade_r": 200, "shade_g": 168, "shade_b": 120
            }
          ]
        }
        """);

        File.WriteAllText(PolicyJson, """
        {
          "policy_version": "MAP30A_CLI_TEST_V1",
          "default_sector": "DEFAULT",
          "policies": [
            {
              "parcel_class": "BLUE_RESIDENTIAL",
              "neighborhood_sector": "DEFAULT",
              "min_lot_area_tiles": 96,
              "front_setback_tiles": 2,
              "rear_setback_tiles": 3,
              "side_setback_tiles": 1,
              "min_footprint_width_tiles": 6,
              "min_footprint_depth_tiles": 6,
              "max_lot_coverage_ratio": 0.60,
              "preferred_footprint_kind": "RESIDENTIAL_RECTANGLE"
            },
            {
              "parcel_class": "RED_RESIDENTIAL_OR_COMMERCIAL",
              "neighborhood_sector": "DEFAULT",
              "min_lot_area_tiles": 120,
              "front_setback_tiles": 0,
              "rear_setback_tiles": 2,
              "side_setback_tiles": 0,
              "min_footprint_width_tiles": 6,
              "min_footprint_depth_tiles": 6,
              "max_lot_coverage_ratio": 0.85,
              "preferred_footprint_kind": "COMMERCIAL_RECTANGLE"
            }
          ]
        }
        """);
    }

    private string[] MakeFullArgs() => new[]
    {
        "--lot-fill-json",            LotFillJson,
        "--building-footprint-policy", PolicyJson,
        "--output-root",              OutputRoot,
        "--output-json",              OutputJson,
        "--output-csv",               OutputCsv,
        "--output-checks-csv",        OutputChkCsv,
        "--output-png",               OutputPng,
        "--output-html",              OutputHtml,
        "--summary",                  Summary,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-parcel-building-footprint-candidates" }
            .Concat(extraArgs)
            .ToArray();

        var psi = new ProcessStartInfo("dotnet",
            $"run --project \"{CliProject}\" -- " +
            string.Join(" ", allArgs.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)))
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            WorkingDirectory       = RepoRoot,
        };

        using var proc = Process.Start(psi)!;
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(180_000);
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Exit codes
    // -----------------------------------------------------------------------

    [Fact]
    public void Command_MissingArgs_ExitsOne()
    {
        var (code, _, _) = RunCli();
        Assert.Equal(1, code);
    }

    [Fact]
    public void Command_NonLocalOutputRoot_ExitsOne()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(
            "--lot-fill-json",            LotFillJson,
            "--building-footprint-policy", PolicyJson,
            "--output-root",              Path.GetTempPath(),
            "--output-json",              Path.Combine(_tempDir, "out.json"),
            "--output-csv",               Path.Combine(_tempDir, "out.csv"),
            "--output-checks-csv",        Path.Combine(_tempDir, "chk.csv"),
            "--output-png",               Path.Combine(_tempDir, "out.png"),
            "--output-html",              Path.Combine(_tempDir, "out.html"),
            "--summary",                  Path.Combine(_tempDir, "sum.txt"));
        Assert.Equal(1, code);
        Assert.Contains(".local", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Command_MissingLotFillJson_ExitsOne()
    {
        WriteFixtures();
        var args = MakeFullArgs().ToList();
        // Replace lot-fill-json value with nonexistent path
        var idx = Array.IndexOf(args.ToArray(), "--lot-fill-json");
        args[idx + 1] = Path.Combine(_tempDir, "does_not_exist.json");
        var (code, _, _) = RunCli(args.ToArray());
        Assert.Equal(1, code);
    }

    [Fact]
    public void Command_MissingPolicyFile_ExitsOne()
    {
        WriteFixtures();
        var args = MakeFullArgs().ToList();
        var idx = Array.IndexOf(args.ToArray(), "--building-footprint-policy");
        args[idx + 1] = Path.Combine(_tempDir, "no_such_policy.json");
        var (code, _, _) = RunCli(args.ToArray());
        Assert.Equal(1, code);
    }

    [Fact]
    public void Command_ValidArgs_ExitsZero()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // Output files written
    // -----------------------------------------------------------------------

    [Fact]
    public void Command_AllOutputFilesWritten()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        Assert.True(File.Exists(OutputJson),   "JSON not written");
        Assert.True(File.Exists(OutputCsv),    "CSV not written");
        Assert.True(File.Exists(OutputChkCsv), "checks CSV not written");
        Assert.True(File.Exists(OutputPng),    "PNG not written");
        Assert.True(File.Exists(OutputHtml),   "HTML not written");
        Assert.True(File.Exists(Summary),      "summary not written");
    }

    // -----------------------------------------------------------------------
    // JSON correctness
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputJson_FootprintCount_IsPositive()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("footprint_count").GetInt32() > 0);
    }

    [Fact]
    public void OutputJson_PolicyLoaded_IsTrue()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("policy_loaded").GetBoolean());
    }

    [Fact]
    public void OutputJson_IsValid_IsTrue()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("is_valid").GetBoolean());
    }

    [Fact]
    public void OutputJson_SandboxOnly_IsTrue()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("sandbox_only").GetBoolean());
    }

    [Fact]
    public void OutputJson_WriterReady_IsFalse()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.False(doc.RootElement.GetProperty("writer_ready").GetBoolean());
    }

    [Fact]
    public void OutputJson_RuntimeValid_IsFalse()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.False(doc.RootElement.GetProperty("runtime_valid").GetBoolean());
    }

    [Fact]
    public void OutputJson_PolicyVersion_IsNonEmpty()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var v = doc.RootElement.GetProperty("policy_version").GetString();
        Assert.False(string.IsNullOrEmpty(v));
    }

    [Fact]
    public void OutputJson_AllFootprints_InsideLotBounds()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        foreach (var fp in doc.RootElement.GetProperty("footprints").EnumerateArray())
        {
            Assert.True(fp.GetProperty("fp_x1").GetInt32() >= fp.GetProperty("lot_x1").GetInt32());
            Assert.True(fp.GetProperty("fp_y1").GetInt32() >= fp.GetProperty("lot_y1").GetInt32());
            Assert.True(fp.GetProperty("fp_x2").GetInt32() <= fp.GetProperty("lot_x2").GetInt32());
            Assert.True(fp.GetProperty("fp_y2").GetInt32() <= fp.GetProperty("lot_y2").GetInt32());
        }
    }

    // -----------------------------------------------------------------------
    // No forbidden runtime artifacts
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputRoot_ContainsNoForbiddenArtifacts()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        foreach (var ext in new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" })
        {
            var found = Directory.Exists(OutputRoot)
                ? Directory.GetFiles(OutputRoot, ext, SearchOption.AllDirectories)
                : Array.Empty<string>();
            Assert.Empty(found);
        }
    }

    // -----------------------------------------------------------------------
    // Canonical policy file
    // -----------------------------------------------------------------------

    [Fact]
    public void ExternalPolicyFile_CanonicalFile_Exists()
    {
        Assert.True(File.Exists(s_canonicalPolicyPath),
            $"Canonical policy file not found: {s_canonicalPolicyPath}");
    }

    [Fact]
    public void Command_WithCanonicalPolicyFile_ExitsZero()
    {
        WriteFixtures();
        if (!File.Exists(s_canonicalPolicyPath)) return;
        var args = MakeFullArgs().ToList();
        var idx = Array.IndexOf(args.ToArray(), "--building-footprint-policy");
        args[idx + 1] = s_canonicalPolicyPath;
        var (code, _, err) = RunCli(args.ToArray());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // Helper script
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_Exists()
    {
        Assert.True(File.Exists(HelperScript),
            $"Helper script not found: {HelperScript}");
    }

    [Fact]
    public void HelperScript_DoesNotContainForbiddenRuntimeWriterCalls()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("compile-worldgen",     content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("media/maps",           content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("steamapps",            content, StringComparison.OrdinalIgnoreCase);
    }
}
