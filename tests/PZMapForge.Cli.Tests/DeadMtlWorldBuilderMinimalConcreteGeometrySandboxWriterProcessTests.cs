using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterProcessTests : IDisposable
{
    private readonly string _tempDir;
    private static readonly string s_cliProject =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj"));
    private static readonly string s_repoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterProcessTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pzmapforge-map27a-cli-test.local", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string WriteValidAdapterContract()
    {
        string[] canonicalFamilyIds =
        {
            "LOT_PACK_RUNTIME_BINARY", "LOT_HEADER_RUNTIME_BINARY", "WORLDGEN_OVERRIDE_LUA",
            "RUNTIME_LUA", "PROJECT_ZOMBOID_INSTALL_PATH", "STEAM_WORKSHOP_OUTPUT",
            "COMPILE_WORLDGEN_INVOCATION", "MAP_00_PNG_MUTATION"
        };
        string[] canonicalPatterns =
        {
            "*.lotpack", "*.lotheader", "WorldGenOverride.lua",
            "*.lua", "*/Project Zomboid/*", "*/steamapps/workshop/*",
            "compile-worldgen", "map_00.png"
        };
        var lots = Enumerable.Range(1, 7).Select(i => new
        {
            lot_order = i, lot_id = $"map_00_comp0001_lot_{i:D4}", component_id = "map_00_component_0001",
            min_x = 124 + (i - 1) * 13, min_y = 10, max_x = 124 + (i - 1) * 13 + 12, max_y = 69,
            width_px = 13, height_px = 60, frontage_side = "NORTH", rear_service_side = "EAST"
        }).ToArray();
        var slots = Enumerable.Range(1, 7).Select(i => new
        {
            slot_order = i, slot_id = $"map_00_comp0001_slot_{i:D4}", lot_id = $"map_00_comp0001_lot_{i:D4}",
            lot_order = i, component_id = "map_00_component_0001",
            min_x = 126 + (i - 1) * 13, min_y = 13, max_x = 134 + (i - 1) * 13, max_y = 66,
            width_px = 9, height_px = 54
        }).ToArray();
        var families = canonicalFamilyIds.Select((fid, idx) => new
        {
            family_order = idx + 1, family_id = fid, blocked_pattern = canonicalPatterns[idx], status = "FORBIDDEN"
        }).ToArray();

        var obj = new
        {
            format = "MAP-26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_COMPLETE",
            is_valid = true,
            adapter_contract_status = "NORMALIZED_DRY_RUN_RECORDS_ONLY",
            writer_ready = false, runtime_valid = false, materialized = false,
            approved_for_writer_experiment = false,
            writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL",
            normalized_component = new
            {
                source_record_id = "COMPONENT_WRITER_RECORD", target_component_id = "map_00_component_0001",
                bbox_min_x = 124, bbox_min_y = 10, bbox_max_x = 212, bbox_max_y = 69,
                bbox_width_px = 89, bbox_height_px = 60
            },
            normalized_lots = lots,
            normalized_building_slots = slots,
            normalized_access_records = new object[]
            {
                new { access_order = 1, access_id = "map_00_comp0001_frontage_access",
                      access_kind = "FRONTAGE_ACCESS", side = "NORTH",
                      component_id = "map_00_component_0023", contact_px = 89 },
                new { access_order = 2, access_id = "map_00_comp0001_rear_service_access",
                      access_kind = "REAR_SERVICE_ACCESS", side = "EAST",
                      component_id = "map_00_component_0030", contact_px = 60 }
            },
            forbidden_output_families = families
        };
        string path = Path.Combine(_tempDir, "map_00.minimal_concrete_geometry_writer_adapter_contract.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private (int exitCode, string stdout, string stderr) RunCli(string[] args)
    {
        var psi = new ProcessStartInfo("dotnet", $"run --project \"{s_cliProject}\" -- " + string.Join(" ", args))
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false
        };
        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private string[] BuildArgs(string adapterContract,
        string? outputJson = null, string? outputMd = null,
        string? outputCsv = null, string? summary = null,
        string? outputRoot = null)
    {
        string root = outputRoot ?? Path.Combine(_tempDir, "out.local");
        return new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0",
            "--adapter-contract", adapterContract,
            "--output-root",  root,
            "--output-json",  outputJson  ?? Path.Combine(root, "map_00.sandbox_writer_v0.json"),
            "--output-md",    outputMd    ?? Path.Combine(root, "map_00.sandbox_writer_v0.md"),
            "--output-csv",   outputCsv   ?? Path.Combine(root, "map_00.sandbox_writer_v0.csv"),
            "--summary",      summary     ?? Path.Combine(root, "map_00.sandbox_writer_v0.summary.txt")
        };
    }

    [Fact]
    public void ValidFixture_ExitCode0()
    {
        var contract = WriteValidAdapterContract();
        var (exitCode, _, _) = RunCli(BuildArgs(contract));
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (exitCode, _, stderr) = RunCli(new[] { "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0" });
        Assert.Equal(1, exitCode);
        Assert.Contains("--adapter-contract", stderr);
    }

    [Fact]
    public void MissingAdapterContract_ExitCode1()
    {
        var root = Path.Combine(_tempDir, "out.local");
        var (exitCode, _, _) = RunCli(new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0",
            "--adapter-contract", Path.Combine(_tempDir, "missing.json"),
            "--output-root", root,
            "--output-json", Path.Combine(root, "x.json"),
            "--output-md",   Path.Combine(root, "x.md"),
            "--output-csv",  Path.Combine(root, "x.csv"),
            "--summary",     Path.Combine(root, "x.txt")
        });
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        var contract = WriteValidAdapterContract();
        var badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-no-dotlocal-" + Guid.NewGuid().ToString("N")[..8]);
        var (exitCode, _, stderr) = RunCli(new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0",
            "--adapter-contract", contract,
            "--output-root", badRoot,
            "--output-json", Path.Combine(badRoot, "x.json"),
            "--output-md",   Path.Combine(badRoot, "x.md"),
            "--output-csv",  Path.Combine(badRoot, "x.csv"),
            "--summary",     Path.Combine(badRoot, "x.txt")
        });
        Assert.Equal(1, exitCode);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void ValidFixture_Writes4MainOutputs()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_v0.json");
        var outMd   = Path.Combine(root, "map_00.sandbox_writer_v0.md");
        var outCsv  = Path.Combine(root, "map_00.sandbox_writer_v0.csv");
        var outTxt  = Path.Combine(root, "map_00.sandbox_writer_v0.summary.txt");
        RunCli(BuildArgs(contract, outJson, outMd, outCsv, outTxt, root));
        Assert.True(File.Exists(outJson));
        Assert.True(File.Exists(outMd));
        Assert.True(File.Exists(outCsv));
        Assert.True(File.Exists(outTxt));
    }

    [Fact]
    public void ValidFixture_Writes5OperationFiles()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(contract, outputRoot: root));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_component_operations.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_lot_operations.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_building_slot_operations.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_access_operations.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_forbidden_output_guard.json")));
    }

    [Fact]
    public void OutputJson_ContainsOperationCount17()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_v0.json");
        RunCli(BuildArgs(contract, outJson, outputRoot: root));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal(17, doc.RootElement.GetProperty("operation_count").GetInt32());
    }

    [Fact]
    public void OutputJson_ContainsSandboxOnlyTrue()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_v0.json");
        RunCli(BuildArgs(contract, outJson, outputRoot: root));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.True(doc.RootElement.GetProperty("sandbox_only").GetBoolean());
    }

    [Fact]
    public void OutputJson_ContainsWriterStageSandboxWriterV0()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_v0.json");
        RunCli(BuildArgs(contract, outJson, outputRoot: root));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal("SANDBOX_WRITER_V0", doc.RootElement.GetProperty("writer_stage").GetString());
    }

    [Fact]
    public void OutputJson_ContainsRuntimeValidFalse()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_v0.json");
        RunCli(BuildArgs(contract, outJson, outputRoot: root));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.False(doc.RootElement.GetProperty("runtime_valid").GetBoolean());
    }

    [Fact]
    public void OutputJson_ContainsCorrectVerdict()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        var outJson = Path.Combine(root, "map_00.sandbox_writer_v0.json");
        RunCli(BuildArgs(contract, outJson, outputRoot: root));
        var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal("MAP27A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_V0_COMPLETE",
            doc.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public void OutputJson_NoLotpackInOperationRoot()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(contract, outputRoot: root));
        var lotpacks = Directory.GetFiles(root, "*.lotpack", SearchOption.AllDirectories);
        Assert.Empty(lotpacks);
    }

    [Fact]
    public void OutputJson_NoLuaInOperationRoot()
    {
        var contract = WriteValidAdapterContract();
        var root = Path.Combine(_tempDir, "out.local");
        RunCli(BuildArgs(contract, outputRoot: root));
        var lua = Directory.GetFiles(root, "*.lua", SearchOption.AllDirectories);
        Assert.Empty(lua);
    }

    [Fact]
    public void HelperScript_Exists()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0.ps1");
        Assert.True(File.Exists(scriptPath), $"Helper script not found: {scriptPath}");
    }

    [Fact]
    public void HelperScript_DoesNotContainCompileWorldgen()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0.ps1");
        string content = File.ReadAllText(scriptPath);
        Assert.DoesNotContain("compile-worldgen", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContainWorldGenOverrideLua()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0.ps1");
        string content = File.ReadAllText(scriptPath);
        Assert.DoesNotContain("WorldGenOverride.lua", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContainDotLotpack()
    {
        string scriptPath = Path.Combine(s_repoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-v0.ps1");
        string content = File.ReadAllText(scriptPath);
        Assert.DoesNotContain(".lotpack", content, StringComparison.OrdinalIgnoreCase);
    }
}
