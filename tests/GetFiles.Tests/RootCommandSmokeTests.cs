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
        Assert.Contains("--no-strip-comments", output);
        Assert.Contains("--no-strip-whitespace", output);
    }

    [Fact]
    public void StripCommentsOption_DefaultIsTrue()
    {
        // Arrange
        var command = new AggregateCommand();

        // Assert
        Assert.True(command.StripCommentsOption.Parse("").GetValueForOption(command.StripCommentsOption));
    }

    [Fact]
    public void StripWhitespaceOption_DefaultIsTrue()
    {
        // Arrange
        var command = new AggregateCommand();

        // Assert
        Assert.True(command.StripWhitespaceOption.Parse("").GetValueForOption(command.StripWhitespaceOption));
    }

    [Fact]
    public void NoStripCommentsOption_DefaultIsFalse()
    {
        // Arrange
        var command = new AggregateCommand();

        // Assert
        Assert.False(command.NoStripCommentsOption.Parse("").GetValueForOption(command.NoStripCommentsOption));
    }

    [Fact]
    public void NoStripWhitespaceOption_DefaultIsFalse()
    {
        // Arrange
        var command = new AggregateCommand();

        // Assert
        Assert.False(command.NoStripWhitespaceOption.Parse("").GetValueForOption(command.NoStripWhitespaceOption));
    }
}
