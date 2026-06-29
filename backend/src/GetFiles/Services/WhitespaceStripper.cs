namespace GetFiles.Services;

/// <summary>
/// Default implementation of <see cref="IWhitespaceStripper"/>.
/// </summary>
public class WhitespaceStripper : IWhitespaceStripper
{
    /// <inheritdoc />
    public string StripWhitespace(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Normalize line endings to \n for processing, restoring the dominant
        // ending on the way out.
        var useCrLf = content.Contains("\r\n");
        var lines = content.Replace("\r\n", "\n").Split('\n');

        // Defer blank-line emission rather than emitting-then-back-patching:
        // count a pending run of blank lines and flush it sized for the rule
        // (3+ blank lines collapse to one) only when the next content line
        // arrives. This avoids the off-by-one surgery the old version needed.
        var output = new List<string>(lines.Length);
        var pendingBlanks = 0;

        foreach (var rawLine in lines)
        {
            var line = TrimTrailingWhitespace(rawLine);

            if (line.Length == 0)
            {
                pendingBlanks++;
                continue;
            }

            // A run of 3+ blank lines collapses to a single blank line; 1-2 are kept.
            AppendBlankLines(output, pendingBlanks >= 3 ? 1 : pendingBlanks);
            pendingBlanks = 0;

            output.Add(line);
        }

        // A trailing blank run is kept but capped at 2 (a trailing run is never
        // collapsed to a single line — it is simply truncated at the cap).
        AppendBlankLines(output, Math.Min(pendingBlanks, 2));

        return string.Join(useCrLf ? "\r\n" : "\n", output);
    }

    private static void AppendBlankLines(List<string> output, int count)
    {
        for (var i = 0; i < count; i++)
            output.Add(string.Empty);
    }

    /// <summary>
    /// Trims trailing whitespace (spaces and tabs) from a line while
    /// preserving leading indentation.
    /// </summary>
    private static string TrimTrailingWhitespace(string line)
    {
        var end = line.Length;
        while (end > 0 && (line[end - 1] == ' ' || line[end - 1] == '\t'))
            end--;

        return end == line.Length ? line : line[..end];
    }
}
