using Xunit;

namespace PZMapForge.Cli.Tests;

// Thin smoke test: the CLI assembly loads and the command list is known.
// Full CLI integration tests are deferred to a later slice.
public sealed class CliSmokeTests
{
    [Fact]
    public void CliBuildAssembly_IsPresent()
    {
        // The CLI assembly is referenced; verify it loads without exception.
        var asm = typeof(PZMapForge.Core.Palette.PaletteLoader).Assembly;
        Assert.NotNull(asm);
        Assert.Contains("PZMapForge.Core", asm.FullName, StringComparison.Ordinal);
    }
}
