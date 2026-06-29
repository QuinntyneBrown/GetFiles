using GetFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GetFiles.Tests;

/// <summary>
/// Directly validates L2-1.2 AC-3 ("console logging outputs at configurable
/// verbosity levels"): configuration sets the baseline, the --verbose/--quiet
/// flags override it, and --verbose makes the LogDebug diagnostics reachable.
/// </summary>
public class LogVerbosityTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] entries)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();

    [Fact]
    public void Default_IsInformation()
        => Assert.Equal(LogLevel.Information, LogVerbosity.Resolve(Config(), Array.Empty<string>()));

    [Theory]
    [InlineData("--verbose")]
    [InlineData("-v")]
    public void VerboseFlag_SetsDebug(string flag)
        => Assert.Equal(LogLevel.Debug, LogVerbosity.Resolve(Config(), new[] { flag, "-p", "." }));

    [Theory]
    [InlineData("--quiet")]
    [InlineData("-q")]
    public void QuietFlag_SetsWarning(string flag)
        => Assert.Equal(LogLevel.Warning, LogVerbosity.Resolve(Config(), new[] { flag }));

    [Fact]
    public void ConfigurationDefault_IsHonored()
        => Assert.Equal(LogLevel.Warning, LogVerbosity.Resolve(Config(("Logging:LogLevel:Default", "Warning")), Array.Empty<string>()));

    [Fact]
    public void Flag_OverridesConfiguration()
        => Assert.Equal(LogLevel.Debug, LogVerbosity.Resolve(Config(("Logging:LogLevel:Default", "Warning")), new[] { "--verbose" }));

    [Fact]
    public void VerboseWins_WhenBothFlagsPresent()
        => Assert.Equal(LogLevel.Debug, LogVerbosity.Resolve(Config(), new[] { "--verbose", "--quiet" }));
}
