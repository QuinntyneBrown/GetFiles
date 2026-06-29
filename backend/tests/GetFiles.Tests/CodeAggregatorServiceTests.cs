using GetFiles.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GetFiles.Tests;

/// <summary>
/// Stub comment stripper that records calls and returns input unchanged.
/// </summary>
internal class StubCommentStripper : ICommentStripper
{
    public List<(string Content, string Extension)> Calls { get; } = new();

    public string StripComments(string content, string fileExtension)
    {
        Calls.Add((content, fileExtension));
        return content;
    }
}

/// <summary>
/// Stub whitespace stripper that records calls and returns input unchanged.
/// </summary>
internal class StubWhitespaceStripper : IWhitespaceStripper
{
    public List<string> Calls { get; } = new();

    public string StripWhitespace(string content)
    {
        Calls.Add(content);
        return content;
    }
}

public class CodeAggregatorServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoDir;
    private readonly string _outputPath;
    private readonly StubCommentStripper _commentStripper;
    private readonly StubWhitespaceStripper _whitespaceStripper;
    private readonly CodeAggregatorService _service;

    public CodeAggregatorServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GetFilesTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _repoDir = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(_repoDir);

        _outputPath = Path.Combine(_tempDir, "output.txt");

        _commentStripper = new StubCommentStripper();
        _whitespaceStripper = new StubWhitespaceStripper();
        var logger = NullLogger<CodeAggregatorService>.Instance;

        _service = new CodeAggregatorService(logger, _commentStripper, _whitespaceStripper);
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
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    [Fact]
    public void Aggregate_OutputHasCorrectHeaderAndFooterDelimiters()
    {
        // Arrange
        var file = CreateFile("Program.cs", "Console.WriteLine(\"Hello\");\n");
        var files = new List<string> { file };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: false, stripWhitespace: false);

        // Assert
        var output = File.ReadAllText(_outputPath);
        Assert.Contains(CodeAggregatorService.FileHeader("Program.cs"), output);
        Assert.Contains(CodeAggregatorService.FileFooter("Program.cs"), output);
    }

    [Fact]
    public void DelimiterHelpers_ProduceTheExpectedWireFormat()
    {
        // Pins the single-source-of-truth delimiter format the downstream LLM parses.
        Assert.Equal("=== FILE: foo.cs ===", CodeAggregatorService.FileHeader("foo.cs"));
        Assert.Equal("=== END FILE: foo.cs ===", CodeAggregatorService.FileFooter("foo.cs"));
    }

    [Fact]
    public void Aggregate_RelativePathsUseForwardSlashes()
    {
        // Arrange
        var file = CreateFile(Path.Combine("src", "Models", "Foo.cs"), "class Foo {}\n");
        var files = new List<string> { file };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: false, stripWhitespace: false);

        // Assert
        var output = File.ReadAllText(_outputPath);
        Assert.Contains(CodeAggregatorService.FileHeader("src/Models/Foo.cs"), output);
        Assert.Contains(CodeAggregatorService.FileFooter("src/Models/Foo.cs"), output);
        Assert.DoesNotContain("\\", output);
    }

    [Fact]
    public void Aggregate_AllInputFilesAppearInOutput()
    {
        // Arrange
        var file1 = CreateFile("A.cs", "class A {}\n");
        var file2 = CreateFile("B.cs", "class B {}\n");
        var file3 = CreateFile(Path.Combine("sub", "C.cs"), "class C {}\n");
        var files = new List<string> { file1, file2, file3 };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: false, stripWhitespace: false);

        // Assert
        var output = File.ReadAllText(_outputPath);
        Assert.Contains(CodeAggregatorService.FileHeader("A.cs"), output);
        Assert.Contains("class A {}", output);
        Assert.Contains(CodeAggregatorService.FileFooter("A.cs"), output);

        Assert.Contains(CodeAggregatorService.FileHeader("B.cs"), output);
        Assert.Contains("class B {}", output);
        Assert.Contains(CodeAggregatorService.FileFooter("B.cs"), output);

        Assert.Contains(CodeAggregatorService.FileHeader("sub/C.cs"), output);
        Assert.Contains("class C {}", output);
        Assert.Contains(CodeAggregatorService.FileFooter("sub/C.cs"), output);
    }

    [Fact]
    public void Aggregate_StripCommentsFlag_CallsCommentStripper()
    {
        // Arrange
        var file = CreateFile("Test.cs", "// comment\ncode\n");
        var files = new List<string> { file };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: true, stripWhitespace: false);

        // Assert
        Assert.Single(_commentStripper.Calls);
        Assert.Equal(".cs", _commentStripper.Calls[0].Extension);
        Assert.Empty(_whitespaceStripper.Calls);
    }

    [Fact]
    public void Aggregate_StripWhitespaceFlag_CallsWhitespaceStripper()
    {
        // Arrange
        var file = CreateFile("Test.js", "  code  \n\n\n");
        var files = new List<string> { file };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: false, stripWhitespace: true);

        // Assert
        Assert.Single(_whitespaceStripper.Calls);
        Assert.Empty(_commentStripper.Calls);
    }

    [Fact]
    public void Aggregate_BothStripFlags_CallsCommentStripperBeforeWhitespaceStripper()
    {
        // Arrange
        var file = CreateFile("Test.cs", "// comment\n  code  \n");
        var files = new List<string> { file };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: true, stripWhitespace: true);

        // Assert
        Assert.Single(_commentStripper.Calls);
        Assert.Single(_whitespaceStripper.Calls);
        // Since stubs return input unchanged, whitespace stripper receives the same content
        Assert.Equal(_commentStripper.Calls[0].Content, _whitespaceStripper.Calls[0]);
    }

    [Fact]
    public void Aggregate_NoStripFlags_DoesNotCallStrippers()
    {
        // Arrange
        var file = CreateFile("Test.cs", "code\n");
        var files = new List<string> { file };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: false, stripWhitespace: false);

        // Assert
        Assert.Empty(_commentStripper.Calls);
        Assert.Empty(_whitespaceStripper.Calls);
    }

    [Fact]
    public void Aggregate_MultipleFiles_SeparatedByBlankLine()
    {
        // Arrange
        var file1 = CreateFile("A.cs", "class A {}\n");
        var file2 = CreateFile("B.cs", "class B {}\n");
        var files = new List<string> { file1, file2 };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: false, stripWhitespace: false);

        // Assert
        var lines = File.ReadAllLines(_outputPath);
        // Find the END FILE line for first file, next should be blank, then header of second file
        var endFileIndex = Array.FindIndex(lines, l => l.Contains(CodeAggregatorService.FileFooter("A.cs")));
        Assert.True(endFileIndex >= 0, "Could not find END FILE marker for A.cs");
        Assert.Equal("", lines[endFileIndex + 1]);
        Assert.Contains(CodeAggregatorService.FileHeader("B.cs"), lines[endFileIndex + 2]);
    }

    [Fact]
    public void Aggregate_OutputHasNoUtf8Bom()
    {
        // Arrange
        var file = CreateFile("Program.cs", "class A {}\n");
        var files = new List<string> { file };

        // Act
        _service.Aggregate(files, _repoDir, _outputPath, stripComments: false, stripWhitespace: false);

        // Assert — first bytes must be the delimiter, not EF BB BF.
        var bytes = File.ReadAllBytes(_outputPath);
        Assert.True(bytes.Length >= 3);
        Assert.False(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "Output file should not start with a UTF-8 BOM");
        Assert.Equal((byte)'=', bytes[0]);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("A", 1)]
    [InlineData("A\nB", 2)]
    [InlineData("A\nB\n", 2)]       // trailing newline does not add a line
    [InlineData("A\n", 1)]
    [InlineData("\n", 1)]
    public void CountLines_DoesNotOverCountTrailingNewline(string content, int expected)
    {
        Assert.Equal(expected, CodeAggregatorService.CountLines(content));
    }

    [Fact]
    public void FormatFileSize_Bytes()
    {
        Assert.Equal("0 bytes", CodeAggregatorService.FormatFileSize(0));
        Assert.Equal("512 bytes", CodeAggregatorService.FormatFileSize(512));
        Assert.Equal("1023 bytes", CodeAggregatorService.FormatFileSize(1023));
    }

    [Fact]
    public void FormatFileSize_Kilobytes()
    {
        Assert.Equal("1.0 KB", CodeAggregatorService.FormatFileSize(1024));
        Assert.Equal("1.5 KB", CodeAggregatorService.FormatFileSize(1536));
        Assert.Equal("1023.9 KB", CodeAggregatorService.FormatFileSize(1024 * 1024 - 100));
    }

    [Fact]
    public void FormatFileSize_Megabytes()
    {
        Assert.Equal("1.0 MB", CodeAggregatorService.FormatFileSize(1024 * 1024));
        Assert.Equal("2.5 MB", CodeAggregatorService.FormatFileSize((long)(2.5 * 1024 * 1024)));
    }
}
