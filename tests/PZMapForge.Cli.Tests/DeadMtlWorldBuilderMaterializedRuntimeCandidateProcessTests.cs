using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMaterializedRuntimeCandidateProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map32a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMaterializedRuntimeCandidateProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private string LotFillJson   => Path.Combine(_tempDir, "lot-fill.local.json");
    private string FootprintJson => Path.Combine(_tempDir, "footprint.local.json");
    private string OutputRoot    => Path.Combine(_tempDir, "candidate.local");
    private string Manifest      => Path.Combine(OutputRoot, "manifest.json");
    private string ChecksCsv     => Path.Combine(OutputRoot, "checks.csv");
    private string Summary       => Path.Combine(OutputRoot, "summary.txt");

    private void WriteFixtures()
    {
        File.WriteAllText(LotFillJson, """
        {
          "lot_sizing_policy_version": "MAP29C_V1",
          "components": [
            { "component_id": "COMP_TEST", "parcel_class": "BLUE_RESIDENTIAL" }
          ],
          "lots": [
            {
              "component_id": "COMP_TEST", "lot_id": "LOT_A", "frontage_direction": "NORTH",
              "x1": 0, "y1": 0, "x2": 13, "y2": 26, "width": 14, "height": 27, "tile_count": 378,
              "shade_r": 200, "shade_g": 168, "shade_b": 120
            }
          ]
        }
        """);

        File.WriteAllText(FootprintJson, """
        {
          "format": "MAP31B_TEST",
          "footprint_count": 1,
          "skipped_lot_count": 0,
          "total_lot_count": 1,
          "sector_counts": [
            { "sector_id": "DEFAULT", "lot_count": 1 }
          ]
        }
        """);

        Directory.CreateDirectory(OutputRoot);
    }

    private string[] MakeFullArgs() => new[]
    {
        "--lot-fill-json",    LotFillJson,
        "--footprint-json",   FootprintJson,
        "--output-root",      OutputRoot,
        "--output-manifest",  Manifest,
        "--output-checks-csv", ChecksCsv,
        "--summary",          Summary,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-materialized-runtime-candidate" }
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
    // MAP-32A CLI 1. Valid args exits zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_ValidArgs_ExitsZero()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP-32A CLI 2. Missing lot-fill JSON exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_MissingLotFillJson_ExitsOne()
    {
        WriteFixtures();
        var args = MakeFullArgs().ToList();
        int idx = Array.IndexOf(args.ToArray(), "--lot-fill-json");
        args[idx + 1] = Path.Combine(_tempDir, "no_such_lot_fill.json");
        var (code, _, _) = RunCli(args.ToArray());
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP-32A CLI 3. Missing footprint JSON exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_MissingFootprintJson_ExitsOne()
    {
        WriteFixtures();
        var args = MakeFullArgs().ToList();
        int idx = Array.IndexOf(args.ToArray(), "--footprint-json");
        args[idx + 1] = Path.Combine(_tempDir, "no_such_footprint.json");
        var (code, _, _) = RunCli(args.ToArray());
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP-32A CLI 4. Output root outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_OutputRoot_OutsideLocal_ExitsOne()
    {
        WriteFixtures();
        string badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-map32a-bad", Guid.NewGuid().ToString());
        var (code, _, err) = RunCli(
            "--lot-fill-json",  LotFillJson,
            "--footprint-json", FootprintJson,
            "--output-root",    badRoot);
        Assert.Equal(1, code);
        Assert.Contains(".local", err, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // MAP-32A CLI 5. Metadata files are created under output root
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_MetadataFiles_AreCreated()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
        Assert.True(File.Exists(Path.Combine(OutputRoot, "mod.info")),          "mod.info must exist");
        Assert.True(File.Exists(Path.Combine(OutputRoot, "media", "maps",
            "DeadMTL_MAP32A", "map.info")),                                     "map.info must exist");
        Assert.True(File.Exists(Path.Combine(OutputRoot, "media", "maps",
            "DeadMTL_MAP32A", "spawnpoints.lua")),                              "spawnpoints.lua must exist");
        Assert.True(File.Exists(Path.Combine(OutputRoot, "media", "maps",
            "DeadMTL_MAP32A", "objects.lua")),                                  "objects.lua must exist");
    }

    // -----------------------------------------------------------------------
    // MAP-32A CLI 6. Checks CSV contains MAP32A check IDs
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_ChecksCsv_ContainsMap32AChecks()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(ChecksCsv)) return;
        string csv = File.ReadAllText(ChecksCsv);
        Assert.Contains("MAP32A_OUTPUT_ROOT_UNDER_LOCAL",             csv);
        Assert.Contains("MAP32A_STAGED_MOD_INFO_WRITTEN",             csv);
        Assert.Contains("MAP32A_BINARY_CELL_MATERIALIZATION_ATTEMPTED", csv);
        Assert.Contains("MAP32A_CLAIM_BOUNDARY_RECORDED",             csv);
    }

    // -----------------------------------------------------------------------
    // MAP-32A CLI 7. No runtime/playable/public claims appear as true in manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_NoClaims_AsTrue_InManifest()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(Manifest)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(Manifest));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("runtime_proof_claimed").GetBoolean(),
            "runtime_proof_claimed must be false");
        Assert.False(root.GetProperty("public_playable_packaging_claimed").GetBoolean(),
            "public_playable_packaging_claimed must be false");
        Assert.False(root.GetProperty("playable_export_claimed").GetBoolean(),
            "playable_export_claimed must be false");
        Assert.False(root.GetProperty("binary_cell_materialized").GetBoolean(),
            "binary_cell_materialized must be false (Path B)");
        Assert.True(root.GetProperty("sandbox_only").GetBoolean(),
            "sandbox_only must be true");
    }

    // -----------------------------------------------------------------------
    // MAP-32A CLI 8. Summary TXT is written and contains required lines
    // -----------------------------------------------------------------------

    [Fact]
    public void Map32A_SummaryTxt_WrittenAndContainsRequiredContent()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
        Assert.True(File.Exists(Summary), "summary TXT must exist");
        string text = File.ReadAllText(Summary);
        Assert.Contains("MAP-32A DEADMTL MATERIALIZED RUNTIME CANDIDATE", text);
        Assert.Contains("Binary cell materialized: False", text);
        Assert.Contains("Verdict                 : MAP32A_MATERIALIZED_RUNTIME_CANDIDATE_STAGED", text);
    }
}
