using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilderTests : IDisposable
{
    private readonly string _tempDir;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pzmapforge-map27a-test.local", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string WriteValidAdapterContract(
        string verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_COMPLETE",
        bool isValid = true,
        string status = "NORMALIZED_DRY_RUN_RECORDS_ONLY",
        string sourceRecordId = "COMPONENT_WRITER_RECORD",
        string frontageAccessKind = "FRONTAGE_ACCESS",
        string rearAccessKind = "REAR_SERVICE_ACCESS",
        bool writerReady = false,
        bool runtimeValid = false,
        bool materialized = false,
        bool approved = false,
        string gateStatus = "LOCKED_PENDING_OPERATOR_APPROVAL",
        int lotCount = 7,
        int slotCount = 7,
        string forbiddenStatus = "FORBIDDEN")
    {
        var lots = Enumerable.Range(1, lotCount).Select(i => new
        {
            lot_order = i,
            lot_id = $"map_00_comp0001_lot_{i:D4}",
            component_id = "map_00_component_0001",
            min_x = 124 + (i - 1) * 13,
            min_y = 10,
            max_x = 124 + (i - 1) * 13 + 12,
            max_y = 69,
            width_px = 13,
            height_px = 60,
            frontage_side = "NORTH",
            rear_service_side = "EAST"
        }).ToArray();

        var slots = Enumerable.Range(1, slotCount).Select(i => new
        {
            slot_order = i,
            slot_id = $"map_00_comp0001_slot_{i:D4}",
            lot_id = $"map_00_comp0001_lot_{i:D4}",
            lot_order = i,
            component_id = "map_00_component_0001",
            min_x = 126 + (i - 1) * 13,
            min_y = 13,
            max_x = 134 + (i - 1) * 13,
            max_y = 66,
            width_px = 9,
            height_px = 54
        }).ToArray();

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
        var forbiddenFamilies = canonicalFamilyIds.Select((fid, idx) => new
        {
            family_order = idx + 1,
            family_id = fid,
            blocked_pattern = canonicalPatterns[idx],
            status = forbiddenStatus
        }).ToArray();

        var obj = new
        {
            format = "MAP-26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            verdict,
            is_valid = isValid,
            adapter_contract_status = status,
            writer_ready = writerReady,
            runtime_valid = runtimeValid,
            materialized,
            approved_for_writer_experiment = approved,
            writer_experiment_gate_status = gateStatus,
            normalized_component = new
            {
                record_kind = "NORMALIZED_COMPONENT",
                map_id = "map_00",
                target_component_id = "map_00_component_0001",
                source_record_id = sourceRecordId,
                bbox_min_x = 124, bbox_min_y = 10, bbox_max_x = 212, bbox_max_y = 69,
                bbox_width_px = 89, bbox_height_px = 60
            },
            normalized_lots = lots,
            normalized_building_slots = slots,
            normalized_access_records = new object[]
            {
                new
                {
                    access_order = 1,
                    access_id = "map_00_comp0001_frontage_access",
                    access_kind = frontageAccessKind,
                    side = "NORTH",
                    component_id = "map_00_component_0023",
                    contact_px = 89
                },
                new
                {
                    access_order = 2,
                    access_id = "map_00_comp0001_rear_service_access",
                    access_kind = rearAccessKind,
                    side = "EAST",
                    component_id = "map_00_component_0030",
                    contact_px = 60
                }
            },
            forbidden_output_families = forbiddenFamilies
        };

        string path = Path.Combine(_tempDir, "map_00.minimal_concrete_geometry_writer_adapter_contract.json");
        File.WriteAllText(path, JsonSerializer.Serialize(obj));
        return path;
    }

    private string OutputRoot() => Path.Combine(_tempDir, "out.local");

    [Fact]
    public void ValidFixture_Verdict_IsComplete()
    {
        var path = WriteValidAdapterContract();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder();
        var result = builder.Build(path, OutputRoot());
        Assert.Equal("MAP27A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_V0_COMPLETE", result.Verdict);
    }

    [Fact]
    public void ValidFixture_IsValid_True()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidFixture_CheckCount_Is33()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(33, result.CheckCount);
    }

    [Fact]
    public void ValidFixture_AllChecksPass()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(0, result.FailedCheckCount);
        Assert.Equal(33, result.PassedCheckCount);
    }

    [Fact]
    public void ValidFixture_OperationCount_Is17()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(17, result.OperationCount);
    }

    [Fact]
    public void ValidFixture_ComponentOperationCount_Is1()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(1, result.ComponentOperationCount);
    }

    [Fact]
    public void ValidFixture_LotOperationCount_Is7()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(7, result.LotOperationCount);
    }

    [Fact]
    public void ValidFixture_BuildingSlotOperationCount_Is7()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(7, result.BuildingSlotOperationCount);
    }

    [Fact]
    public void ValidFixture_AccessOperationCount_Is2()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(2, result.AccessOperationCount);
    }

    [Fact]
    public void ValidFixture_OperationFileCount_Is5()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(5, result.OperationFileCount);
    }

    [Fact]
    public void ValidFixture_AllOperationFiles_AreWritten()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.All(result.OperationFiles, f => Assert.True(f.Written));
        Assert.All(result.OperationFiles, f => Assert.True(File.Exists(f.FilePath)));
    }

    [Fact]
    public void ValidFixture_AllOperationFiles_HaveHashes()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.All(result.OperationFiles, f => Assert.Equal(64, f.Sha256.Length));
    }

    [Fact]
    public void ValidFixture_AllOperationsRuntimeEffect_IsNone()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        var allOps = result.OperationFiles; // check via JSON in files
        Assert.True(result.Checks.Any(c => c.CheckId == "ALL_OPERATIONS_RUNTIME_EFFECT_NONE" && c.CheckStatus == "PASS"));
    }

    [Fact]
    public void ValidFixture_WriterReady_IsFalse()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.False(result.WriterReady);
    }

    [Fact]
    public void ValidFixture_RuntimeValid_IsFalse()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.False(result.RuntimeValid);
    }

    [Fact]
    public void ValidFixture_Materialized_IsFalse()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.False(result.Materialized);
    }

    [Fact]
    public void ValidFixture_ApprovedForWriterExperiment_IsFalse()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.False(result.ApprovedForWriterExperiment);
    }

    [Fact]
    public void ValidFixture_GateStatus_IsLocked()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.StartsWith("LOCKED", result.WriterExperimentGateStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidFixture_SandboxOnly_IsTrue()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.True(result.SandboxOnly);
    }

    [Fact]
    public void ValidFixture_WriterStage_IsSandboxWriterV0()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal("SANDBOX_WRITER_V0", result.WriterStage);
    }

    [Fact]
    public void ValidFixture_RuntimeProofClaimed_IsFalse()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.False(result.RuntimeProofClaimed);
    }

    [Fact]
    public void ValidFixture_PublicPlayablePackagingClaimed_IsFalse()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.False(result.PublicPlayablePackagingClaimed);
    }

    [Fact]
    public void ValidFixture_ForbiddenOutputGuardCount_Is8()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Equal(8, result.ForbiddenOutputGuardCount);
        Assert.Equal(8, result.ForbiddenOutputGuard.Guards.Count);
    }

    [Fact]
    public void ValidFixture_ForbiddenOutputGuard_NoForbiddenArtifactsEmitted()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.True(result.ForbiddenOutputGuard.NoForbiddenArtifactsEmitted);
    }

    [Fact]
    public void ValidFixture_ComponentOperationFile_HasCorrectKind()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        var compFile = result.OperationFiles.FirstOrDefault(f => f.FileKind == "COMPONENT_OPERATIONS");
        Assert.NotNull(compFile);
        Assert.Equal(1, compFile!.OperationCount);
    }

    [Fact]
    public void WrongAdapterVerdict_FailsCheck()
    {
        var path = WriteValidAdapterContract(verdict: "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Contains(result.Checks, c => c.CheckId == "ADAPTER_CONTRACT_VERDICT_COMPLETE" && c.CheckStatus == "FAIL");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void WrongAdapterIsValid_FailsCheck()
    {
        var path = WriteValidAdapterContract(isValid: false);
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Contains(result.Checks, c => c.CheckId == "ADAPTER_CONTRACT_IS_VALID_TRUE" && c.CheckStatus == "FAIL");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void WrongSourceRecordId_FailsCheck()
    {
        var path = WriteValidAdapterContract(sourceRecordId: "OLD_RECORD_ID");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Contains(result.Checks, c => c.CheckId == "ADAPTER_CONTRACT_CANONICAL_COMPONENT_SOURCE_RECORD" && c.CheckStatus == "FAIL");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void WrongFrontageAccessKind_FailsCheck()
    {
        var path = WriteValidAdapterContract(frontageAccessKind: "FRONTAGE");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Contains(result.Checks, c => c.CheckId == "ADAPTER_CONTRACT_CANONICAL_ACCESS_KINDS" && c.CheckStatus == "FAIL");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void WrongRearAccessKind_FailsCheck()
    {
        var path = WriteValidAdapterContract(rearAccessKind: "REAR_SERVICE");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Contains(result.Checks, c => c.CheckId == "ADAPTER_CONTRACT_CANONICAL_ACCESS_KINDS" && c.CheckStatus == "FAIL");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void WrongForbiddenFamilyStatus_FailsCheck()
    {
        var path = WriteValidAdapterContract(forbiddenStatus: "BLOCKED");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Contains(result.Checks, c => c.CheckId == "ADAPTER_CONTRACT_FORBIDDEN_FAMILIES_8_FORBIDDEN" && c.CheckStatus == "FAIL");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void WrongGateStatus_FailsCheck()
    {
        var path = WriteValidAdapterContract(gateStatus: "UNLOCKED");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        Assert.Contains(result.Checks, c => c.CheckId == "GATE_STATUS_LOCKED" && c.CheckStatus == "FAIL");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MissingAdapterContract_IsInvalid()
    {
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder()
            .Build(Path.Combine(_tempDir, "missing.json"), OutputRoot());
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ValidFixture_OperationFiles_ListFiveFileNames()
    {
        var path = WriteValidAdapterContract();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, OutputRoot());
        var fileNames = result.OperationFiles.Select(f => f.FileName).ToList();
        Assert.Contains("map_00.sandbox_writer_component_operations.json", fileNames);
        Assert.Contains("map_00.sandbox_writer_lot_operations.json", fileNames);
        Assert.Contains("map_00.sandbox_writer_building_slot_operations.json", fileNames);
        Assert.Contains("map_00.sandbox_writer_access_operations.json", fileNames);
        Assert.Contains("map_00.sandbox_writer_forbidden_output_guard.json", fileNames);
    }

    [Fact]
    public void ValidFixture_NoLotpackInOutputRoot()
    {
        var path = WriteValidAdapterContract();
        var outRoot = OutputRoot();
        new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, outRoot);
        var lotpacks = Directory.GetFiles(outRoot, "*.lotpack", SearchOption.AllDirectories);
        Assert.Empty(lotpacks);
    }

    [Fact]
    public void ValidFixture_NoLuaInOutputRoot()
    {
        var path = WriteValidAdapterContract();
        var outRoot = OutputRoot();
        new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterBuilder().Build(path, outRoot);
        var luaFiles = Directory.GetFiles(outRoot, "*.lua", SearchOption.AllDirectories);
        Assert.Empty(luaFiles);
    }
}
