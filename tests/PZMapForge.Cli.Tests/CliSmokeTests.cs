using PZMapForge.Core.Primitives;
using Xunit;

namespace PZMapForge.Cli.Tests;

// Thin smoke tests confirming Core types used by each CLI command are accessible.
// Full process-level CLI integration tests are deferred to a later slice.
public sealed class CliSmokeTests
{
    [Fact]
    public void CliBuildAssembly_IsPresent()
    {
        var asm = typeof(PZMapForge.Core.Palette.PaletteLoader).Assembly;
        Assert.NotNull(asm);
        Assert.Contains("PZMapForge.Core", asm.FullName, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimitiveClassifier_IsAccessible()
    {
        // Confirm PrimitiveClassifier and PlanningPrimitiveType are visible,
        // covering the type surface exercised by the primitive-check command.
        Assert.True(PrimitiveClassifier.IsKnownKind("grass"));
        Assert.True(PrimitiveClassifier.IsKnownKind("spawn"));
        Assert.False(PrimitiveClassifier.IsKnownKind("unknown_kind"));

        // Enum completeness: all 7 primitive types are defined
        var types = Enum.GetValues<PlanningPrimitiveType>();
        Assert.Equal(7, types.Length);
    }
}
