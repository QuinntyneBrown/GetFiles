using System.CommandLine;
using System.CommandLine.Invocation;
using GetFiles.Commands;
using GetFiles.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ── Configuration ──────────────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .Build();

// ── Logging verbosity ──────────────────────────────────────────────
// The minimum level is resolved from configuration ("Logging:LogLevel:Default"
// in appsettings.json) and then overridden by the --verbose / --quiet flags.
// This is what makes the LogDebug diagnostics (e.g. "why was this file
// excluded?") reachable and satisfies the "configurable verbosity" requirement.
var minimumLevel = GetFiles.LogVerbosity.Resolve(configuration, args);

// ── Dependency Injection ───────────────────────────────────────────
var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(minimumLevel);
});

// Application services
services.AddSingleton<IFileDiscoveryService, FileDiscoveryService>();
services.AddSingleton<ICodeAggregatorService, CodeAggregatorService>();
services.AddSingleton<ICommentStripper, CommentStripper>();
services.AddSingleton<IWhitespaceStripper, WhitespaceStripper>();

// Command handler
services.AddTransient<AggregateCommandHandler>();

var serviceProvider = services.BuildServiceProvider();

// ── CLI Definition ─────────────────────────────────────────────────
var rootCommand = new RootCommand("GetFiles - Aggregate repository code for LLM consumption");

var aggregateCommand = new AggregateCommand();

aggregateCommand.SetHandler(
    (InvocationContext context) =>
    {
        var parse = context.ParseResult;
        var path = parse.GetValueForOption(aggregateCommand.PathOption)!;
        var output = parse.GetValueForOption(aggregateCommand.OutputOption)!;
        var stripComments = parse.GetValueForOption(aggregateCommand.StripCommentsOption);
        var stripWhitespace = parse.GetValueForOption(aggregateCommand.StripWhitespaceOption);
        var noStripComments = parse.GetValueForOption(aggregateCommand.NoStripCommentsOption);
        var noStripWhitespace = parse.GetValueForOption(aggregateCommand.NoStripWhitespaceOption);
        var ignore = parse.GetValueForOption(aggregateCommand.IgnoreOption) ?? Array.Empty<string>();

        // Warn instead of silently resolving contradictory flags. The
        // --no-* flag wins (effective = strip && !noStrip), but the user
        // should know that an explicit --strip-* request was overridden.
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("GetFiles");
        var tokens = parse.Tokens.Select(t => t.Value).ToHashSet(StringComparer.Ordinal);
        if (tokens.Contains("--strip-comments") && tokens.Contains("--no-strip-comments"))
            logger.LogWarning("Both --strip-comments and --no-strip-comments were specified; comment stripping is disabled.");
        if (tokens.Contains("--strip-whitespace") && tokens.Contains("--no-strip-whitespace"))
            logger.LogWarning("Both --strip-whitespace and --no-strip-whitespace were specified; whitespace stripping is disabled.");

        var effectiveStripComments = stripComments && !noStripComments;
        var effectiveStripWhitespace = stripWhitespace && !noStripWhitespace;

        var handler = serviceProvider.GetRequiredService<AggregateCommandHandler>();
        context.ExitCode = handler.Execute(path, output, effectiveStripComments, effectiveStripWhitespace, ignore);
    });

rootCommand.AddCommand(aggregateCommand);

// ── Default command ────────────────────────────────────────────────
// "aggregate" is the default subcommand: when the first argument is an
// option (or anything other than a known command) the tool behaves as if
// "aggregate" had been typed, so `gf -p ./repo` == `gf aggregate -p ./repo`.
// Help/version flags are passed through untouched so root-level help still works.
string[] knownCommands = { "aggregate" };
string[] passthroughFlags = { "-h", "--help", "-?", "/h", "/?", "--version" };
if (args.Length > 0
    && !knownCommands.Contains(args[0])
    && !passthroughFlags.Contains(args[0]))
{
    args = args.Prepend("aggregate").ToArray();
}

// ── Run ────────────────────────────────────────────────────────────
return await rootCommand.InvokeAsync(args);
