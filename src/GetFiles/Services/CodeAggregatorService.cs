using Microsoft.Extensions.Logging;

namespace GetFiles.Services;

/// <summary>
/// Default implementation of <see cref="ICodeAggregatorService"/>.
/// </summary>
public class CodeAggregatorService : ICodeAggregatorService
{
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

        using (var writer = new StreamWriter(outputPath, append: false, encoding: System.Text.Encoding.UTF8))
        {
            for (var i = 0; i < files.Count; i++)
            {
                var filePath = files[i];
                var relativePath = Path.GetRelativePath(repositoryPath, filePath)
                    .Replace('\\', '/');

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

                writer.WriteLine($"=== FILE: {relativePath} ===");
                writer.Write(content);
                if (content.Length > 0 && !content.EndsWith('\n'))
                {
                    writer.WriteLine();
                }
                writer.WriteLine($"=== END FILE: {relativePath} ===");

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

    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var lines = 1;
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n')
                lines++;
        }

        return lines;
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
