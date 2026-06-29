using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GetFiles;

/// <summary>
/// Resolves the minimum console log level. Configuration
/// ("Logging:LogLevel:Default", e.g. from appsettings.json) sets the baseline;
/// the --verbose / --quiet command-line flags override it. This is what makes
/// the LogDebug diagnostics reachable and gives the logging configuration a real
/// consumer (L2-1.2 AC-3, "configurable verbosity levels").
/// </summary>
internal static class LogVerbosity
{
    public static LogLevel Resolve(IConfiguration configuration, IReadOnlyList<string> args)
    {
        var level = LogLevel.Information;

        // 1. Configuration baseline.
        var configured = configuration["Logging:LogLevel:Default"];
        if (!string.IsNullOrWhiteSpace(configured) && Enum.TryParse<LogLevel>(configured, ignoreCase: true, out var parsed))
        {
            level = parsed;
        }

        // 2. Command-line override (--verbose wins over --quiet).
        if (args.Contains("--verbose") || args.Contains("-v"))
        {
            level = LogLevel.Debug;
        }
        else if (args.Contains("--quiet") || args.Contains("-q"))
        {
            level = LogLevel.Warning;
        }

        return level;
    }
}
