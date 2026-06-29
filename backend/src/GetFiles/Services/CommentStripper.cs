using System.Text;

namespace GetFiles.Services;

/// <summary>
/// Default implementation of <see cref="ICommentStripper"/>.
/// Uses a character-by-character state machine to correctly handle
/// string literals that contain comment-like patterns.
/// </summary>
public class CommentStripper : ICommentStripper
{
    /// <summary>
    /// Extensions whose comment syntax is C-style: <c>//</c> line comments and
    /// <c>/* */</c> block comments, with language-specific string handling.
    /// </summary>
    private static readonly HashSet<string> CStyleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts"
    };

    /// <summary>
    /// Stylesheet extensions. CSS has only <c>/* */</c> block comments; SCSS adds
    /// <c>//</c> line comments. Both are handled with a <c>url(...)</c> guard so that
    /// unquoted/protocol-relative URLs are not mistaken for comments.
    /// </summary>
    private static readonly HashSet<string> StyleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".scss"
    };

    /// <inheritdoc />
    public string StripComments(string content, string fileExtension)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var ext = fileExtension ?? string.Empty;

        if (CStyleExtensions.Contains(ext))
            return StripCStyleComments(content, ext);

        if (StyleExtensions.Contains(ext))
        {
            // SCSS supports // line comments; CSS does not.
            var supportsLineComments = string.Equals(ext, ".scss", StringComparison.OrdinalIgnoreCase);
            return StripStyleComments(content, supportsLineComments);
        }

        if (string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase))
            return StripHtmlComments(content);

        return content;
    }

    /// <summary>
    /// Strips C-style single-line (//) and multi-line (/* */) comments using a
    /// character-by-character state machine. Preserves content inside string literals,
    /// including C# verbatim strings (<c>@"..."</c>) and TypeScript template literals.
    /// </summary>
    private static string StripCStyleComments(string content, string extension)
    {
        var sb = new StringBuilder(content.Length);
        var i = 0;
        var length = content.Length;
        var supportsTemplateLiterals = string.Equals(extension, ".ts", StringComparison.OrdinalIgnoreCase);
        var supportsVerbatim = string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase);

        while (i < length)
        {
            var c = content[i];

            // ── C# verbatim strings: @"...", @$"..." ─────────────────
            if (supportsVerbatim && c == '@')
            {
                if (i + 1 < length && content[i + 1] == '"')
                {
                    sb.Append('@');
                    i = EmitVerbatimStringLiteral(content, i + 1, sb);
                    continue;
                }
                if (i + 2 < length && content[i + 1] == '$' && content[i + 2] == '"')
                {
                    sb.Append("@$");
                    i = EmitVerbatimStringLiteral(content, i + 2, sb);
                    continue;
                }
                // Otherwise it's an escaped identifier (e.g. @class) — fall through.
            }

            // ── C# interpolated verbatim strings: $@"..." ────────────
            if (supportsVerbatim && c == '$'
                && i + 2 < length && content[i + 1] == '@' && content[i + 2] == '"')
            {
                sb.Append("$@");
                i = EmitVerbatimStringLiteral(content, i + 2, sb);
                continue;
            }

            // ── String literals (regular / interpolated / template) ──
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
    /// Strips stylesheet comments. CSS has only <c>/* */</c> block comments; SCSS also
    /// supports <c>//</c> line comments (enabled via <paramref name="supportsLineComments"/>).
    /// Quoted strings and <c>url(...)</c> tokens are preserved verbatim so that URLs
    /// containing <c>//</c> (protocol-relative or absolute) are not eaten as comments.
    /// </summary>
    private static string StripStyleComments(string content, bool supportsLineComments)
    {
        var sb = new StringBuilder(content.Length);
        var i = 0;
        var length = content.Length;

        while (i < length)
        {
            var c = content[i];

            // ── String literals ──────────────────────────────────────
            if (c == '"' || c == '\'')
            {
                i = EmitStringLiteral(content, i, c, sb);
                continue;
            }

            // ── url(...) token: copy verbatim to the closing ) ───────
            if ((c == 'u' || c == 'U') && IsUrlToken(content, i))
            {
                i = EmitUrlToken(content, i, sb);
                continue;
            }

            // ── Single-line comment: // (SCSS only) ──────────────────
            if (supportsLineComments && c == '/' && i + 1 < length && content[i + 1] == '/')
            {
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

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns true if the text at <paramref name="start"/> begins a CSS
    /// <c>url(</c> function token (case-insensitive), not preceded by an identifier
    /// character (so it is a standalone token, not the tail of a longer name).
    /// </summary>
    private static bool IsUrlToken(string content, int start)
    {
        if (start + 3 >= content.Length)
            return false;

        if ((content[start] | 0x20) != 'u'
            || (content[start + 1] | 0x20) != 'r'
            || (content[start + 2] | 0x20) != 'l'
            || content[start + 3] != '(')
            return false;

        return start == 0 || !IsIdentifierChar(content[start - 1]);
    }

    private static bool IsIdentifierChar(char c)
        => char.IsLetterOrDigit(c) || c == '-' || c == '_';

    /// <summary>
    /// Emits a CSS <c>url(...)</c> token verbatim, from the leading <c>url(</c> through
    /// the matching <c>)</c>, so comment-like sequences inside the URL are preserved.
    /// Quoted URL values are consumed as string literals so a <c>)</c> inside quotes
    /// does not end the token prematurely.
    /// </summary>
    /// <returns>The index immediately after the closing parenthesis.</returns>
    private static int EmitUrlToken(string content, int start, StringBuilder sb)
    {
        var length = content.Length;

        // Emit the leading "url("
        sb.Append(content, start, 4);
        var i = start + 4;

        while (i < length)
        {
            var c = content[i];

            if (c == '"' || c == '\'')
            {
                i = EmitStringLiteral(content, i, c, sb);
                continue;
            }

            sb.Append(c);
            i++;
            if (c == ')')
                break;
        }

        return i;
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
    /// Emits a C# verbatim string literal (<c>@"..."</c>). Verbatim strings span real
    /// newlines, do not honor backslash escapes, and use a doubled quote (<c>""</c>) to
    /// embed a quote character.
    /// </summary>
    /// <param name="start">Index of the opening quote.</param>
    /// <returns>The index immediately after the closing quote.</returns>
    private static int EmitVerbatimStringLiteral(string content, int start, StringBuilder sb)
    {
        var length = content.Length;

        // Emit the opening quote
        sb.Append('"');
        var i = start + 1;

        while (i < length)
        {
            var c = content[i];

            if (c == '"')
            {
                // Doubled quote is an escaped quote, not a terminator
                if (i + 1 < length && content[i + 1] == '"')
                {
                    sb.Append("\"\"");
                    i += 2;
                    continue;
                }

                // Closing quote
                sb.Append('"');
                return i + 1;
            }

            // Newlines and backslashes are literal in verbatim strings
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
