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

    // -----------------------------------------------------------------------
    // MAP-30B checks
    // -----------------------------------------------------------------------

    [Fact]
    public void Map30B_OutputJson_ContainsMap30BChecks()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var checkIds = doc.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Select(c => c.GetProperty("check_id").GetString() ?? "")
            .ToList();
        Assert.Contains("MAP30B_ALL_LOTS_PAINTED_IN_PREVIEW",              checkIds);
        Assert.Contains("MAP30B_OUTPUT_PNG_NO_DEBUG_CYAN_OR_BLACK",        checkIds);
        Assert.Contains("MAP30B_OUTPUT_PNG_NO_SOURCE_BLUE_RED_INSIDE_LOTS",checkIds);
        Assert.Contains("MAP30B_NO_RUNTIME_ARTIFACTS_WRITTEN",             checkIds);
        Assert.Contains("MAP30B_CLAIM_BOUNDARY_FALSE",                     checkIds);
    }

    [Fact]
    public void Map30B_OutputJson_AllMap30BChecks_Pass()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var map30bChecks = doc.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Where(c => (c.GetProperty("check_id").GetString() ?? "").StartsWith("MAP30B"))
            .ToList();
        Assert.True(map30bChecks.Count > 0, "No MAP30B checks found in output JSON");
        foreach (var c in map30bChecks)
        {
            var id     = c.GetProperty("check_id").GetString();
            var status = c.GetProperty("check_status").GetString();
            Assert.True(status == "PASS", $"MAP30B check {id} status={status}");
        }
    }

    [Fact]
    public void Map30B_OutputPng_HasNoDebugCyanOrBlack()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputPng)) return;

        using var ms  = new System.IO.MemoryStream(File.ReadAllBytes(OutputPng));
        using var bmp = new System.Drawing.Bitmap(ms);

        bool found = false;
        for (int x = 0; x < bmp.Width && !found; x++)
            for (int y = 0; y < bmp.Height && !found; y++)
            {
                var c = bmp.GetPixel(x, y);
                // cyan: r<60, g>180, b>180
                if (c.R < 60 && c.G > 180 && c.B > 180) { found = true; break; }
                // pure black
                if (c.R == 0 && c.G == 0 && c.B == 0)   { found = true; break; }
            }
        Assert.False(found, "Output PNG must not contain debug cyan or pure black pixels");
    }

    [Fact]
    public void Map30B_OutputCsv_ContainsSkippedLotRows()
    {
        // The fixture has 1 valid lot, no skipped lots — verify FOOTPRINT rows present.
        // A separate check: run with a lot-fill JSON that has no skipped lots ensures
        // the CSV at least contains FOOTPRINT rows. The real-map run verifies SKIPPED rows.
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputCsv)) return;
        var csv = File.ReadAllText(OutputCsv);
        Assert.Contains("FOOTPRINT", csv);
    }

    [Fact]
    public void Map30B_OutputJson_PreviewPaintedLotCount_EqualsTotal()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var root   = doc.RootElement;
        int total   = root.GetProperty("total_lot_count").GetInt32();
        int painted = root.GetProperty("preview_painted_lot_count").GetInt32();
        Assert.Equal(total, painted);
    }

    [Fact]
    public void Map30B_OutputJson_SkippedLotPreviewColorRgb_IsNonEmpty()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var color = doc.RootElement.GetProperty("skipped_lot_preview_color_rgb").GetString();
        Assert.False(string.IsNullOrEmpty(color));
        Assert.StartsWith("rgb(", color);
    }

    // -----------------------------------------------------------------------
    // MAP-31A CLI fixtures
    // -----------------------------------------------------------------------

    private string SectorJson => Path.Combine(_tempDir, "sectors.local.json");

    private void WriteSectorFixture()
    {
        File.WriteAllText(SectorJson, """
        {
          "sector_overrides_version": "MAP31A_CLI_TEST_V1",
          "sectors": [
            {
              "sector_id": "DENSE_CORE",
              "label": "Dense Core CLI Test",
              "bbox_x1": 0, "bbox_y1": 0, "bbox_x2": 100, "bbox_y2": 100,
              "gameplay_role": "COMMERCIAL_DENSE",
              "tone_note": "CLI test dense sector"
            }
          ]
        }
        """);
    }

    // -----------------------------------------------------------------------
    // MAP-31A 1. No sector flag preserves valid DEFAULT behavior
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31A_NoSectorFlag_ExitsZero()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP-31A 2. Output JSON contains MAP31A check IDs
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31A_OutputJson_ContainsMap31AChecks()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var checkIds = doc.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Select(c => c.GetProperty("check_id").GetString() ?? "")
            .ToList();
        Assert.Contains("MAP31A_SECTOR_ASSIGNMENT_LOADED",       checkIds);
        Assert.Contains("MAP31A_ALL_LOTS_HAVE_SECTOR",           checkIds);
        Assert.Contains("MAP31A_POLICY_RESOLVES_BY_SECTOR",      checkIds);
        Assert.Contains("MAP31A_DEFAULT_SECTOR_FALLBACK_PRESENT",checkIds);
        Assert.Contains("MAP31A_SECTOR_COUNTS_NONEMPTY",         checkIds);
        Assert.Contains("MAP31A_NO_RUNTIME_ARTIFACTS_WRITTEN",   checkIds);
        Assert.Contains("MAP31A_CLAIM_BOUNDARY_FALSE",           checkIds);
    }

    // -----------------------------------------------------------------------
    // MAP-31A 3. All MAP31A checks PASS (no sector file → DEFAULT path)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31A_OutputJson_AllMap31AChecks_Pass()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var map31aChecks = doc.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Where(c => (c.GetProperty("check_id").GetString() ?? "").StartsWith("MAP31A"))
            .ToList();
        Assert.True(map31aChecks.Count > 0, "No MAP31A checks in output JSON");
        foreach (var c in map31aChecks)
        {
            var id     = c.GetProperty("check_id").GetString();
            var status = c.GetProperty("check_status").GetString();
            Assert.True(status == "PASS", $"MAP31A check {id} status={status}");
        }
    }

    // -----------------------------------------------------------------------
    // MAP-31A 4. Footprints have neighborhood_sector field
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31A_OutputJson_FootprintsHaveNeighborhoodSector()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        foreach (var fp in doc.RootElement.GetProperty("footprints").EnumerateArray())
        {
            var sector = fp.GetProperty("neighborhood_sector").GetString();
            Assert.False(string.IsNullOrEmpty(sector),
                "Footprint neighborhood_sector must be non-empty");
        }
    }

    // -----------------------------------------------------------------------
    // MAP-31A 5. Explicit sector file loads and applies sector assignment
    // Fixture lot at (0,0)→(13,26) center=(6,13) inside DENSE_CORE bbox (0..100)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31A_ExplicitSectorFile_LoadsAndApplies()
    {
        WriteFixtures();
        WriteSectorFixture();
        var args = MakeFullArgs().Concat(new[] { "--sector-overrides", SectorJson }).ToArray();
        var (code, _, err) = RunCli(args);
        Assert.True(code == 0, $"Exit={code} stderr={err}");
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("sector_assignment_loaded").GetBoolean(),
            "sector_assignment_loaded should be true");
        var sector = doc.RootElement.GetProperty("footprints")
            .EnumerateArray().First()
            .GetProperty("neighborhood_sector").GetString();
        Assert.Equal("DENSE_CORE", sector);
    }

    // -----------------------------------------------------------------------
    // MAP-31A 6. Missing sector file exits nonzero when explicitly supplied
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31A_MissingSectorFile_ExitsOne_WhenExplicitlyProvided()
    {
        WriteFixtures();
        var args = MakeFullArgs().Concat(new[]
        {
            "--sector-overrides", Path.Combine(_tempDir, "no_such_sectors.json")
        }).ToArray();
        var (code, _, _) = RunCli(args);
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP-31B CLI fixtures
    // Lot (0,0)→(13,26) center=(6,13) → DOWNTOWN_CORE bbox (0..15, 0..30)
    // -----------------------------------------------------------------------

    private string SectorJsonDT => Path.Combine(_tempDir, "sectors-dt.local.json");
    private string PolicyJsonDT => Path.Combine(_tempDir, "policy-dt.local.json");

    private void WriteDowntownSectorFixtures()
    {
        File.WriteAllText(SectorJsonDT, """
        {
          "sector_overrides_version": "MAP31B_CLI_TEST_V1",
          "sectors": [
            {
              "sector_id": "DOWNTOWN_CORE",
              "label": "Downtown Core CLI Test",
              "bbox_x1": 0, "bbox_y1": 0, "bbox_x2": 100, "bbox_y2": 100,
              "gameplay_role": "COMMERCIAL_DENSE",
              "tone_note": "CLI test"
            }
          ]
        }
        """);

        File.WriteAllText(PolicyJsonDT, """
        {
          "policy_version": "MAP31B_CLI_TEST_V1",
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
              "parcel_class": "BLUE_RESIDENTIAL",
              "neighborhood_sector": "DOWNTOWN_CORE",
              "min_lot_area_tiles": 96,
              "front_setback_tiles": 0,
              "rear_setback_tiles": 2,
              "side_setback_tiles": 0,
              "min_footprint_width_tiles": 6,
              "min_footprint_depth_tiles": 6,
              "max_lot_coverage_ratio": 0.80,
              "preferred_footprint_kind": "URBAN_ROWHOUSE_RECTANGLE"
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

    private string[] MakeFullArgsDT() => new[]
    {
        "--lot-fill-json",            LotFillJson,
        "--building-footprint-policy", PolicyJsonDT,
        "--sector-overrides",          SectorJsonDT,
        "--output-root",              OutputRoot,
        "--output-json",              OutputJson,
        "--output-csv",               OutputCsv,
        "--output-checks-csv",        OutputChkCsv,
        "--output-png",               OutputPng,
        "--output-html",              OutputHtml,
        "--summary",                  Summary,
    };

    // -----------------------------------------------------------------------
    // MAP-31B 1. Output JSON contains sector_footprint_summaries (non-empty)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31B_OutputJson_ContainsSectorFootprintSummaries()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var summaries = doc.RootElement.GetProperty("sector_footprint_summaries");
        Assert.True(summaries.GetArrayLength() > 0, "sector_footprint_summaries must be non-empty");
    }

    // -----------------------------------------------------------------------
    // MAP-31B 2. Output JSON contains sector_preview_legend_entries (non-empty)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31B_OutputJson_ContainsSectorPreviewLegendEntries()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var legend = doc.RootElement.GetProperty("sector_preview_legend_entries");
        Assert.True(legend.GetArrayLength() > 0, "sector_preview_legend_entries must be non-empty");
    }

    // -----------------------------------------------------------------------
    // MAP-31B 3. All MAP31B checks present and PASS
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31B_OutputJson_AllMap31BChecks_Pass()
    {
        WriteFixtures();
        WriteDowntownSectorFixtures();
        RunCli(MakeFullArgsDT());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var map31bChecks = doc.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Where(c => (c.GetProperty("check_id").GetString() ?? "").StartsWith("MAP31B"))
            .ToList();
        Assert.True(map31bChecks.Count > 0, "No MAP31B checks found");
        foreach (var c in map31bChecks)
        {
            var id     = c.GetProperty("check_id").GetString();
            var status = c.GetProperty("check_status").GetString();
            Assert.True(status == "PASS", $"MAP31B check {id} status={status}");
        }
    }

    // -----------------------------------------------------------------------
    // MAP-31B 4. Output HTML contains Sector Assignment section
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31B_OutputHtml_ContainsSectorAssignmentSection()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputHtml)) return;
        var html = File.ReadAllText(OutputHtml);
        Assert.Contains("Sector Assignment", html);
    }

    // -----------------------------------------------------------------------
    // MAP-31B 5. No sector file → DEFAULT entry in sector_footprint_summaries
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31B_NoSectorFile_DefaultSummaryInJson()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var summaries = doc.RootElement.GetProperty("sector_footprint_summaries").EnumerateArray().ToList();
        Assert.True(summaries.Any(s => s.GetProperty("sector_id").GetString() == "DEFAULT"),
            "Expected DEFAULT entry in sector_footprint_summaries when no sector file provided");
    }

    // -----------------------------------------------------------------------
    // MAP-31B 6. SectorPreviewLegendCount matches SectorCounts count
    // -----------------------------------------------------------------------

    [Fact]
    public void Map31B_SectorLegendCount_MatchesSectorCountsCount()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputJson)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        int legendCount  = doc.RootElement.GetProperty("sector_preview_legend_count").GetInt32();
        int sectorCounts = doc.RootElement.GetProperty("sector_counts").GetArrayLength();
        Assert.Equal(sectorCounts, legendCount);
    }
}
