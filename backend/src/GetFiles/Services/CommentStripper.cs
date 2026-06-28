using System.Text;

namespace GetFiles.Services;

/// <summary>
/// Default implementation of <see cref="ICommentStripper"/>.
/// Uses a character-by-character state machine to correctly handle
/// string literals that contain comment-like patterns.
/// </summary>
public class CommentStripper : ICommentStripper
{
    private static readonly HashSet<string> CStyleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".js", ".scss", ".css"
    };

    /// <inheritdoc />
    public string StripComments(string content, string fileExtension)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var ext = fileExtension?.ToLowerInvariant() ?? string.Empty;

        if (CStyleExtensions.Contains(ext))
            return StripCStyleComments(content, ext);

        if (ext == ".html")
            return StripHtmlComments(content);

        return content;
    }

    /// <summary>
    /// Strips C-style single-line (//) and multi-line (/* */) comments using a
    /// character-by-character state machine. Preserves content inside string literals.
    /// </summary>
    private static string StripCStyleComments(string content, string extension)
    {
        var sb = new StringBuilder(content.Length);
        var i = 0;
        var length = content.Length;
        var supportsTemplateLiterals = extension == ".ts" || extension == ".js";

        while (i < length)
        {
            var c = content[i];

            // ── String literals ──────────────────────────────────────
            if (c == '"' || c == '\'' || (supportsTemplateLiterals && c == '`'))
            {
                i = EmitStringLiteral(content, i, c, sb);
                continue;
            }

            // ── Single-line comment: // ──────────────────────────────
            if (c == '/' && i + 1 < length && content[i + 1] == '/')
            {
                // Skip to end of line (don't consume the newline itself)
                i += 2;
                while (i < length && content[i] != '\n')
                    i++;
                continue;
            }

            // ── Multi-line comment: /* ... */ ────────────────────────
            if (c == '/' && i + 1 < length && content[i + 1] == '*')
            {
                i += 2;
                while (i < length)
                {
                    if (content[i] == '*' && i + 1 < length && content[i + 1] == '/')
                    {
                        i += 2;
                        break;
                    }
                    i++;
                }
                continue;
            }

            // ── Normal character ─────────────────────────────────────
            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Emits a complete string literal (including its delimiters) into the
    /// StringBuilder, handling escape sequences so that embedded quote characters
    /// don't prematurely end the string.
    /// </summary>
    /// <returns>The index of the character immediately after the closing quote.</returns>
    private static int EmitStringLiteral(string content, int start, char quoteChar, StringBuilder sb)
    {
        var length = content.Length;

        // Emit the opening quote
        sb.Append(quoteChar);
        var i = start + 1;

        while (i < length)
        {
            var c = content[i];

            // Escaped character: emit both the backslash and the next char
            if (c == '\\' && i + 1 < length)
            {
                sb.Append(c);
                sb.Append(content[i + 1]);
                i += 2;
                continue;
            }

            // Closing quote found
            if (c == quoteChar)
            {
                sb.Append(c);
                return i + 1;
            }

            // For non-template-literal strings, a newline ends the literal
            // (prevents runaway matching on malformed input)
            if (c == '\n' && quoteChar != '`')
            {
                sb.Append(c);
                return i + 1;
            }

            sb.Append(c);
            i++;
        }

        // Reached end of content without closing quote — return as-is
        return i;
    }

    /// <summary>
    /// Strips HTML comments (<!-- ... -->) from content.
    /// </summary>
    private static string StripHtmlComments(string content)
    {
        var sb = new StringBuilder(content.Length);
        var i = 0;
        var length = content.Length;

        while (i < length)
        {
            // Check for <!--
            if (i + 3 < length
                && content[i] == '<'
                && content[i + 1] == '!'
                && content[i + 2] == '-'
                && content[i + 3] == '-')
            {
                // Skip until -->
                i += 4;
                while (i < length)
                {
                    if (i + 2 < length
                        && content[i] == '-'
                        && content[i + 1] == '-'
                        && content[i + 2] == '>')
                    {
                        i += 3;
                        break;
                    }
                    i++;
                }
                continue;
            }

            sb.Append(content[i]);
            i++;
        }

        return sb.ToString();
    }
}
