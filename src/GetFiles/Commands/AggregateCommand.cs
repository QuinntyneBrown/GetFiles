using System.CommandLine;

namespace GetFiles.Commands;

/// <summary>
/// Defines the "aggregate" subcommand and its CLI options.
/// </summary>
public class AggregateCommand : Command
{
    public AggregateCommand()
        : base("aggregate", "Aggregate repository source files into a single output file for LLM consumption")
    {
        PathOption = new Option<string>(
            name: "--path",
            description: "The root path of the repository to scan")
        {
            IsRequired = true
        };

        OutputOption = new Option<string>(
            name: "--output",
            description: "The output file path for the aggregated content",
            getDefaultValue: () => "codebase.txt");

        StripCommentsOption = new Option<bool>(
            name: "--strip-comments",
            description: "Remove comments from source files",
            getDefaultValue: () => false);

        StripWhitespaceOption = new Option<bool>(
            name: "--strip-whitespace",
            description: "Collapse unnecessary whitespace in source files",
            getDefaultValue: () => false);

        AddOption(PathOption);
        AddOption(OutputOption);
        AddOption(StripCommentsOption);
        AddOption(StripWhitespaceOption);
    }

    public Option<string> PathOption { get; }
    public Option<string> OutputOption { get; }
    public Option<bool> StripCommentsOption { get; }
    public Option<bool> StripWhitespaceOption { get; }
}
