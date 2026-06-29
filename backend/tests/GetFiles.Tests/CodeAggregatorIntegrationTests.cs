using GetFiles.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GetFiles.Tests;

/// <summary>
/// End-to-end tests that compose the REAL <see cref="CommentStripper"/> and
/// <see cref="WhitespaceStripper"/> into a <see cref="CodeAggregatorService"/>.
/// The unit tests elsewhere stub the strippers and only verify wiring/ordering;
/// these close the L2-4.3 AC-1 composition gap ("both flags together produce
/// smaller output than either alone") and guard the C1 url() regression.
/// </summary>
public class CodeAggregatorIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoDir;
    private readonly CodeAggregatorService _service;

    public CodeAggregatorIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GetFilesIntegration_" + Guid.NewGuid().ToString("N"));
        _repoDir = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(_repoDir);

        _service = new CodeAggregatorService(
            NullLogger<CodeAggregatorService>.Instance,
            new CommentStripper(),
            new WhitespaceStripper());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string CreateFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_repoDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    private long Run(string suffix, bool stripComments, bool stripWhitespace, IReadOnlyList<string> files)
    {
        var outputPath = Path.Combine(_tempDir, $"out-{suffix}.txt");
        _service.Aggregate(files, _repoDir, outputPath, stripComments, stripWhitespace);
        return new FileInfo(outputPath).Length;
    }

    [Fact]
    public void Aggregate_BothStrippers_ProduceSmallerOutputThanEitherAlone()
    {
        var cs = CreateFile("Program.cs",
            "// header comment\n" +
            "using System;\n" +
            "\n" +
            "/* a block\n" +
            "   comment */\n" +
            "\n" +
            "\n" +
            "\n" +
            "var url = \"https://example.com\"; // trailing comment\n" +
            "Console.WriteLine(url);\n");

        var files = new List<string> { cs };

        var raw = Run("raw", stripComments: false, stripWhitespace: false, files);
        var commentsOnly = Run("comments", stripComments: true, stripWhitespace: false, files);
        var whitespaceOnly = Run("whitespace", stripComments: false, stripWhitespace: true, files);
        var both = Run("both", stripComments: true, stripWhitespace: true, files);

        // Each stripper alone shrinks the output; both together shrink it the most.
        Assert.True(commentsOnly < raw, $"comments-only ({commentsOnly}) should be < raw ({raw})");
        Assert.True(whitespaceOnly < raw, $"whitespace-only ({whitespaceOnly}) should be < raw ({raw})");
        Assert.True(both < commentsOnly, $"both ({both}) should be < comments-only ({commentsOnly})");
        Assert.True(both < whitespaceOnly, $"both ({both}) should be < whitespace-only ({whitespaceOnly})");
    }

    [Fact]
    public void Aggregate_RealStrippers_PreserveUnquotedUrlInStylesheet()
    {
        var scss = CreateFile("styles.scss",
            "/* header */\n" +
            "body {\n" +
            "  background: url(http://cdn.example.com/bg.png); // keep\n" +
            "}\n");

        var files = new List<string> { scss };
        var outputPath = Path.Combine(_tempDir, "scss-out.txt");
        _service.Aggregate(files, _repoDir, outputPath, stripComments: true, stripWhitespace: true);

        var output = File.ReadAllText(outputPath);
        Assert.Contains("url(http://cdn.example.com/bg.png)", output);
        Assert.DoesNotContain("/* header */", output); // block comment was stripped
        Assert.DoesNotContain("// keep", output);       // line comment was stripped
    }
}
