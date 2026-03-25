using System.Text;

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

        // Normalize line endings to \n for consistent processing, then restore
        // the dominant line ending at the end.
        var useCrLf = content.Contains("\r\n");
        var normalized = content.Replace("\r\n", "\n");

        var lines = normalized.Split('\n');
        var sb = new StringBuilder(content.Length);
        var consecutiveBlankLines = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = TrimTrailingWhitespace(lines[i]);

            if (line.Length == 0)
            {
                consecutiveBlankLines++;

                // Emit at most 1 blank line (collapse 3+ into 1, keep single blanks)
                if (consecutiveBlankLines <= 2)
                {
                    if (i > 0)
                        sb.Append(useCrLf ? "\r\n" : "\n");
                }
                // When consecutiveBlankLines >= 3, skip the line entirely
            }
            else
            {
                // If we had 3+ blank lines, we already emitted none of the extras.
                // We need exactly 1 blank line separator, which means one newline
                // before this content line in addition to the newline ending the
                // last content line.
                if (consecutiveBlankLines >= 3 && sb.Length > 0)
                {
                    // We emitted 2 blank-line newlines above (for count 1 and 2).
                    // We need to remove the second one and replace with just the
                    // separator for this content line.
                    // Actually, let's simplify: we emitted newlines for blank counts
                    // 1 and 2. That means after the last content line we have:
                    //   \n  (end of content line)
                    //   \n  (blank line 1)
                    //   \n  (blank line 2)
                    // We want only one blank line:
                    //   \n  (end of content line)
                    //   \n  (blank line = one blank line)
                    // So remove the last \n from sb.
                    var ending = useCrLf ? "\r\n" : "\n";
                    if (sb.Length >= ending.Length)
                    {
                        sb.Remove(sb.Length - ending.Length, ending.Length);
                    }
                }

                consecutiveBlankLines = 0;

                if (i > 0)
                    sb.Append(useCrLf ? "\r\n" : "\n");

                sb.Append(line);
            }
        }

        return sb.ToString();
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
