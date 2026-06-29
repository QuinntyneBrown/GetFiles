using System.Text;
using Microsoft.Extensions.Logging;

namespace GetFiles.Services;

/// <summary>
/// Default implementation of <see cref="ICodeAggregatorService"/>.
/// </summary>
public class CodeAggregatorService : ICodeAggregatorService
{
    // ── Output delimiters (the wire contract a downstream LLM parses) ──
    // Defined once here so the format has a single source of truth.
    internal const string FileHeaderPrefix = "=== FILE: ";
    internal const string FileFooterPrefix = "=== END FILE: ";
    internal const string DelimiterSuffix = " ===";

    /// <summary>Builds the per-file header line, e.g. <c>=== FILE: src/A.cs ===</c>.</summary>
    internal static string FileHeader(string relativePath) => $"{FileHeaderPrefix}{relativePath}{DelimiterSuffix}";

    /// <summary>Builds the per-file footer line, e.g. <c>=== END FILE: src/A.cs ===</c>.</summary>
    internal static string FileFooter(string relativePath) => $"{FileFooterPrefix}{relativePath}{DelimiterSuffix}";

    private readonly ILogger<CodeAggregatorService> _logger;
    private readonly ICommentStripper _commentStripper;
    private readonly IWhitespaceStripper _whitespaceStripper;

    public CodeAggregatorService(
        ILogger<CodeAggregatorService> logger,
        ICommentStripper commentStripper,
        IWhitespaceStripper whitespaceStripper)
    {
        _logger = logger;
        _commentStripper = commentStripper;
        _whitespaceStripper = whitespaceStripper;
    }

    /// <inheritdoc />
    public void Aggregate(IReadOnlyList<string> files, string repositoryPath, string outputPath, bool stripComments, bool stripWhitespace)
    {
        var totalLines = 0;

        // Ensure the output directory exists
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // UTF-8 without a BOM: avoid a stray U+FEFF at the head of the LLM payload.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        using (var writer = new StreamWriter(outputPath, append: false, encoding))
        {
            for (var i = 0; i < files.Count; i++)
            {
                var filePath = files[i];
                var relativePath = PathUtil.GetRelativePath(repositoryPath, filePath);

                var content = File.ReadAllText(filePath);

                if (stripComments)
                {
                    var extension = Path.GetExtension(filePath);
                    content = _commentStripper.StripComments(content, extension);
                }

                if (stripWhitespace)
                {
                    content = _whitespaceStripper.StripWhitespace(content);
                }

                var lineCount = CountLines(content);
                totalLines += lineCount;

                writer.WriteLine(FileHeader(relativePath));
                writer.Write(content);
                if (content.Length > 0 && !content.EndsWith('\n'))
                {
                    writer.WriteLine();
                }
                writer.WriteLine(FileFooter(relativePath));

                // Blank line between files, but not after the last one
                if (i < files.Count - 1)
                {
                    writer.WriteLine();
                }
            }
        }

        var fileSize = new FileInfo(outputPath).Length;
        _logger.LogInformation("Total files processed: {FileCount}", files.Count);
        _logger.LogInformation("Total lines of code: {LineCount}", totalLines);
        _logger.LogInformation("Output file size: {FileSize}", FormatFileSize(fileSize));
    }

    /// <summary>
    /// Counts lines of content. A trailing newline does not start a new (empty) line,
    /// so "A\nB\n" counts as 2 lines, not 3.
    /// </summary>
    internal static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var newlines = content.AsSpan().Count('\n');
        return content[^1] == '\n' ? newlines : newlines + 1;
    }

    internal static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} bytes";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";

        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
