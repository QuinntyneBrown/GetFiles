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

        var handler = serviceProvider.GetRequiredService<AggregateCommandHandler>();
        context.ExitCode = handler.Execute(path, output, stripComments, stripWhitespace);
    });

rootCommand.AddCommand(aggregateCommand);

// ── Run ────────────────────────────────────────────────────────────
return await rootCommand.InvokeAsync(args);
