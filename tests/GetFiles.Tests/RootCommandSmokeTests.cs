using System.CommandLine;
using System.CommandLine.IO;
using GetFiles.Commands;
using Xunit;

namespace GetFiles.Tests;

public class RootCommandSmokeTests
{
    [Fact]
    public async Task RootCommand_WithHelpOption_ReturnsZeroExitCode()
    {
        // Arrange
        var rootCommand = new RootCommand("GetFiles - Aggregate repository code for LLM consumption");
        rootCommand.AddCommand(new AggregateCommand());

        var console = new TestConsole();

        // Act
        var exitCode = await rootCommand.InvokeAsync("--help", console);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RootCommand_WithHelpOption_ShowsDescription()
    {
        // Arrange
        var rootCommand = new RootCommand("GetFiles - Aggregate repository code for LLM consumption");
        rootCommand.AddCommand(new AggregateCommand());

        var console = new TestConsole();

        // Act
        await rootCommand.InvokeAsync("--help", console);
        var output = console.Out.ToString()!;

        // Assert
        Assert.Contains("GetFiles", output);
        Assert.Contains("aggregate", output);
    }

    [Fact]
    public async Task AggregateCommand_WithHelpOption_ShowsAllOptions()
    {
        // Arrange
        var rootCommand = new RootCommand("GetFiles - Aggregate repository code for LLM consumption");
        rootCommand.AddCommand(new AggregateCommand());

        var console = new TestConsole();

        // Act
        await rootCommand.InvokeAsync("aggregate --help", console);
        var output = console.Out.ToString()!;

        // Assert
        Assert.Contains("--path", output);
        Assert.Contains("--output", output);
        Assert.Contains("--strip-comments", output);
        Assert.Contains("--strip-whitespace", output);
    }
}
