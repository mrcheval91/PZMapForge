using System.Runtime.Versioning;
using System.Text;

namespace PZMapForge.Core.WorldGen;

/// <summary>
/// Orchestrates the DeadMTL authoring build pipeline:
///   validate layer pack → compile project → compile Lua → byte-check → write proof.
///
/// Claim boundary: authoring artifact only.
/// No public mod packaging. No Project Zomboid load test performed.
/// Windows-only: delegates to DeadMtlLayerPackValidator which uses System.Drawing.
/// </summary>
[SupportedOSPlatform("windows")]
public static class DeadMtlAuthoringBuilder
{
    public static DeadMtlAuthoringBuildResult Build(string packRoot, string outputDir)
    {
        var result = new DeadMtlAuthoringBuildResult();

        // --- 1. Validate layer pack ---
        var validation = DeadMtlLayerPackValidator.Validate(packRoot);
        result.ValidationChecks = validation.ChecksRun;
        result.Warnings.AddRange(validation.Warnings);

        if (!validation.IsValid)
        {
            result.Errors.AddRange(validation.Errors);
            return result;
        }

        // --- 2. Compile project manifest ---
        var projectPath   = Path.Combine(packRoot, "deadmtl_worldgen_project.json");
        var projectResult = WorldGenProjectCompiler.Compile(projectPath);

        if (!projectResult.IsValid)
        {
            result.Errors.AddRange(projectResult.Errors);
            return result;
        }

        // --- 3. Compile Lua from in-memory manifest ---
        var luaResult = WorldGenCompiler.CompileManifest(projectResult.Manifest!);

        if (!luaResult.IsValid)
        {
            result.Errors.AddRange(luaResult.Errors);
            return result;
        }

        result.MapId       = luaResult.MapId;
        result.ModuleCount = luaResult.ModuleCount;

        // --- 4. Write output files ---
        Directory.CreateDirectory(outputDir);

        var manifestPath = Path.Combine(outputDir, "worldgen_layers.json");
        var manifestJson = WorldGenPngCompiler.SerializeManifest(projectResult.Manifest!);
        File.WriteAllText(manifestPath, manifestJson, Encoding.UTF8);
        result.ManifestJsonPath = manifestPath;

        var luaPath = Path.Combine(outputDir, "WorldGenOverride.lua");
        File.WriteAllText(luaPath, luaResult.Lua!, Encoding.ASCII);
        result.LuaPath = luaPath;

        // --- 5. Byte-check Lua ---
        var luaBytes = File.ReadAllBytes(luaPath);
        result.LuaHasBom      = luaBytes.Length >= 3 &&
                                luaBytes[0] == 0xEF && luaBytes[1] == 0xBB && luaBytes[2] == 0xBF;
        result.LuaHasNonAscii = luaBytes.Any(b => b > 127);

        // --- 6. Write proof summary ---
        var proofPath    = Path.Combine(outputDir, "proof-summary.txt");
        var proofContent = BuildProofSummary(packRoot, outputDir, validation, result);
        File.WriteAllText(proofPath, proofContent, Encoding.UTF8);
        result.ProofSummaryPath = proofPath;

        return result;
    }

    private static string BuildProofSummary(
        string packRoot, string outputDir,
        DeadMtlLayerPackValidationResult validation,
        DeadMtlAuthoringBuildResult build)
    {
        var sb        = new StringBuilder();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        sb.AppendLine("PZMapForge DeadMTL Authoring Build Proof");
        sb.AppendLine($"Generated:          {timestamp}");
        sb.AppendLine($"Input pack:         {packRoot}");
        sb.AppendLine($"Output dir:         {outputDir}");
        sb.AppendLine();
        sb.AppendLine("=== Validation ===");
        sb.AppendLine($"Checks run:         {validation.ChecksRun}");
        sb.AppendLine($"Errors:             {validation.Errors.Count}");
        sb.AppendLine($"Warnings:           {validation.Warnings.Count}");
        foreach (var w in validation.Warnings)
            sb.AppendLine($"  warning: {w}");
        sb.AppendLine();
        sb.AppendLine("=== Compilation ===");
        sb.AppendLine($"Map ID:             {build.MapId}");
        sb.AppendLine($"Module count:       {build.ModuleCount}");
        sb.AppendLine();
        sb.AppendLine("=== Lua Byte Check ===");
        sb.AppendLine($"BOM:                {build.LuaHasBom.ToString().ToLowerInvariant()}");
        sb.AppendLine($"Non-ASCII bytes:    {build.LuaHasNonAscii.ToString().ToLowerInvariant()}");
        sb.AppendLine();
        sb.AppendLine("=== Claim Boundary ===");
        sb.AppendLine("This is an authoring artifact only.");
        sb.AppendLine("No public mod packaging is claimed.");
        sb.AppendLine("No Project Zomboid load test has been performed.");
        sb.AppendLine("Output must not be installed into game folders without explicit runtime verification.");

        return sb.ToString();
    }
}
