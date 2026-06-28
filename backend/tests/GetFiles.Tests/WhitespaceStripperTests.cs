using GetFiles.Services;
using Xunit;

namespace GetFiles.Tests;

public class WhitespaceStripperTests
{
    private readonly WhitespaceStripper _sut = new();

    // ── 1. Consecutive blank lines collapsed ─────────────────────────

    [Fact]
    public void StripWhitespace_CollapsesThreeBlankLinesIntoOne()
    {
        var input = "line1\n\n\n\nline2";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("line1\n\nline2", result);
    }

    [Fact]
    public void StripWhitespace_CollapsesFiveBlankLinesIntoOne()
    {
        var input = "line1\n\n\n\n\n\nline2";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("line1\n\nline2", result);
    }

    [Fact]
    public void StripWhitespace_CollapsesManyBlankLinesIntoOne_CRLF()
    {
        var input = "line1\r\n\r\n\r\n\r\nline2";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("line1\r\n\r\nline2", result);
    }

    // ── 2. Trailing whitespace trimmed ───────────────────────────────

    [Fact]
    public void StripWhitespace_TrimsTrailingSpaces()
    {
        var input = "hello   \nworld  ";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("hello\nworld", result);
    }

    [Fact]
    public void StripWhitespace_TrimsTrailingTabs()
    {
        var input = "hello\t\t\nworld\t";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("hello\nworld", result);
    }

    // ── 3. Leading indentation preserved ─────────────────────────────

    [Fact]
    public void StripWhitespace_PreservesLeadingSpaces()
    {
        var input = "    indented line\n        double indented";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("    indented line\n        double indented", result);
    }

    [Fact]
    public void StripWhitespace_PreservesLeadingTabs()
    {
        var input = "\tindented\n\t\tdouble";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("\tindented\n\t\tdouble", result);
    }

    [Fact]
    public void StripWhitespace_PreservesLeadingButTrimsTrailing()
    {
        var input = "    code   \n\tmore code\t  ";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("    code\n\tmore code", result);
    }

    // ── 4. Single blank lines NOT removed ────────────────────────────

    [Fact]
    public void StripWhitespace_PreservesSingleBlankLine()
    {
        var input = "line1\n\nline2";
        var result = _sut.StripWhitespace(input);
        Assert.Equal("line1\n\nline2", result);
    }

    [Fact]
    public void StripWhitespace_PreservesTwoConsecutiveBlankLines()
    {
        var input = "line1\n\n\nline2";
        var result = _sut.StripWhitespace(input);
        // 2 blank lines (3 newlines) stays as-is since it's < 3 blank lines
        // Actually: split gives ["line1","","","line2"], so 2 blank lines
        // The requirement says "3+ consecutive blank lines" get collapsed.
        // 2 blank lines = ok to keep
        Assert.Equal("line1\n\n\nline2", result);
    }

    // ── 5. Content with no excessive whitespace returns unchanged ────

    [Fact]
    public void StripWhitespace_NoExcessiveWhitespace_ReturnsUnchanged()
    {
        var input = "line1\nline2\nline3";
        var result = _sut.StripWhitespace(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void StripWhitespace_AlreadyClean_ReturnsUnchanged()
    {
        var input = "using System;\n\nnamespace Foo\n{\n    class Bar { }\n}";
        var result = _sut.StripWhitespace(input);
        Assert.Equal(input, result);
    }

    // ── Edge cases ───────────────────────────────────────────────────

    [Fact]
    public void StripWhitespace_EmptyContent_ReturnsEmpty()
    {
        var result = _sut.StripWhitespace("");
        Assert.Equal("", result);
    }

    [Fact]
    public void StripWhitespace_NullContent_ReturnsNull()
    {
        var result = _sut.StripWhitespace(null!);
        Assert.Null(result);
    }

    [Fact]
    public void StripWhitespace_OnlyBlankLines_CollapsedToSingleBlank()
    {
        var input = "\n\n\n\n\n";
        var result = _sut.StripWhitespace(input);
        // All blank lines; collapse 5 blanks to 1 blank (= 2 newlines including surrounding)
        // Split gives 6 empty strings. After collapsing 3+ consecutive blanks: just 1 blank line.
        Assert.Equal("\n", result);
    }
}
