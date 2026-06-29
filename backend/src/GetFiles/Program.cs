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

// ── Dependency Injection ───────────────────────────────────────────
var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
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
        var path = context.ParseResult.GetValueForOption(aggregateCommand.PathOption)!;
        var output = context.ParseResult.GetValueForOption(aggregateCommand.OutputOption)!;
        var stripComments = context.ParseResult.GetValueForOption(aggregateCommand.StripCommentsOption);
        var stripWhitespace = context.ParseResult.GetValueForOption(aggregateCommand.StripWhitespaceOption);
        var noStripComments = context.ParseResult.GetValueForOption(aggregateCommand.NoStripCommentsOption);
        var noStripWhitespace = context.ParseResult.GetValueForOption(aggregateCommand.NoStripWhitespaceOption);
        var ignore = context.ParseResult.GetValueForOption(aggregateCommand.IgnoreOption) ?? Array.Empty<string>();

        var handler = serviceProvider.GetRequiredService<AggregateCommandHandler>();
        context.ExitCode = handler.Execute(path, output, stripComments, stripWhitespace, noStripComments, noStripWhitespace, ignore);
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
