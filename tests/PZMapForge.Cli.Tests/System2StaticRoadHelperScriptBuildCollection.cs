using Xunit;

// This test assembly's Process-based tests widely share fixed, non-random
// output paths under `.local\` (not just the System2StaticRoad build-invoking
// scripts). Running them under xUnit's default parallel-collections execution
// causes cross-test collisions on that shared state (confirmed empirically:
// re-enabling parallelism produced 165+ unrelated failures, e.g. CLI exit-code
// assertions, that have nothing to do with the `dotnet build` race below).
// Serializing the whole assembly is the verified-safe configuration.
//
// Separately, a handful of System2StaticRoad*HelperScriptTests classes shell
// out to .ps1 scripts that run `dotnet build` against the shared
// PZMapForge.Cli project. Even fully serialized, that build step retries up
// to 3 times with /nodeReuse:false in the .ps1 scripts themselves, to absorb
// the residual MSB3492 AssemblyInfoInputs.cache race between a `dotnet build`
// invocation and its own background compiler-server process's file handle.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
