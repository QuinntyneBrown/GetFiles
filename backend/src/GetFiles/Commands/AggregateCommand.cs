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
        PathOption.AddAlias("-p");

        OutputOption = new Option<string>(
            name: "--output",
            description: "The output file path for the aggregated content",
            getDefaultValue: () => "codebase.txt");
        OutputOption.AddAlias("-o");

        StripCommentsOption = new Option<bool>(
            name: "--strip-comments",
            description: "Remove comments from source files (default: true, use --no-strip-comments to disable)",
            getDefaultValue: () => true);

        StripWhitespaceOption = new Option<bool>(
            name: "--strip-whitespace",
            description: "Collapse unnecessary whitespace in source files (default: true, use --no-strip-whitespace to disable)",
            getDefaultValue: () => true);

        NoStripCommentsOption = new Option<bool>(
            name: "--no-strip-comments",
            description: "Disable comment stripping",
            getDefaultValue: () => false);

        NoStripWhitespaceOption = new Option<bool>(
            name: "--no-strip-whitespace",
            description: "Disable whitespace stripping",
            getDefaultValue: () => false);

        IgnoreOption = new Option<string[]>(
            name: "--ignore",
            description: "Additional folders/paths to exclude from file discovery (can be specified multiple times)",
            getDefaultValue: () => Array.Empty<string>());
        IgnoreOption.AddAlias("-i");
        IgnoreOption.AllowMultipleArgumentsPerToken = true;

        AddOption(PathOption);
        AddOption(OutputOption);
        AddOption(StripCommentsOption);
        AddOption(StripWhitespaceOption);
        AddOption(NoStripCommentsOption);
        AddOption(NoStripWhitespaceOption);
        AddOption(IgnoreOption);
    }

    public Option<string> PathOption { get; }
    public Option<string> OutputOption { get; }
    public Option<bool> StripCommentsOption { get; }
    public Option<bool> StripWhitespaceOption { get; }
    public Option<bool> NoStripCommentsOption { get; }
    public Option<bool> NoStripWhitespaceOption { get; }
    public Option<string[]> IgnoreOption { get; }
}
