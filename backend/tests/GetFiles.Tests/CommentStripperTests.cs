using GetFiles.Services;
using Xunit;

namespace GetFiles.Tests;

public class CommentStripperTests
{
    private readonly CommentStripper _sut = new();

    // ── 1. Single-line C# comment removal ────────────────────────────

    [Fact]
    public void StripComments_RemovesSingleLineComment_CSharp()
    {
        var input = "var x = 1; // this is a comment\nvar y = 2;";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("var x = 1; \nvar y = 2;", result);
    }

    [Fact]
    public void StripComments_RemovesEntireLineThatIsComment()
    {
        var input = "// full line comment\nvar x = 1;";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("\nvar x = 1;", result);
    }

    // ── 2. Multi-line C# comment removal ─────────────────────────────

    [Fact]
    public void StripComments_RemovesMultiLineComment_CSharp()
    {
        var input = "var x = 1; /* this is\na multi-line comment */ var y = 2;";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("var x = 1;  var y = 2;", result);
    }

    [Fact]
    public void StripComments_RemovesMultiLineComment_SpanningThreeLines()
    {
        var input = "before\n/* line1\nline2\nline3 */\nafter";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("before\n\nafter", result);
    }

    // ── 3. HTML comment removal ──────────────────────────────────────

    [Fact]
    public void StripComments_RemovesHtmlComment()
    {
        var input = "<div><!-- comment --></div>";
        var result = _sut.StripComments(input, ".html");
        Assert.Equal("<div></div>", result);
    }

    [Fact]
    public void StripComments_RemovesMultiLineHtmlComment()
    {
        var input = "<div><!-- multi\nline\ncomment --></div>";
        var result = _sut.StripComments(input, ".html");
        Assert.Equal("<div></div>", result);
    }

    // ── 4. String literal with URL preserved ─────────────────────────

    [Fact]
    public void StripComments_PreservesUrlInStringLiteral()
    {
        var input = "var url = \"https://example.com\";";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("var url = \"https://example.com\";", result);
    }

    [Fact]
    public void StripComments_PreservesUrlInStringLiteral_WithTrailingComment()
    {
        var input = "var url = \"https://example.com\"; // set the url";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("var url = \"https://example.com\"; ", result);
    }

    // ── 5. String literal with // inside preserved ───────────────────

    [Fact]
    public void StripComments_PreservesDoubleSlashInsideString()
    {
        var input = "var s = \"this // is not a comment\";";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("var s = \"this // is not a comment\";", result);
    }

    [Fact]
    public void StripComments_PreservesSlashStarInsideString()
    {
        var input = "var s = \"this /* is not */ a comment\";";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("var s = \"this /* is not */ a comment\";", result);
    }

    [Fact]
    public void StripComments_PreservesSingleQuotedString()
    {
        var input = "var c = '//';";
        var result = _sut.StripComments(input, ".ts");
        Assert.Equal("var c = '//';", result);
    }

    [Fact]
    public void StripComments_PreservesTemplateLiteral_TypeScript()
    {
        var input = "var s = `https://example.com`;";
        var result = _sut.StripComments(input, ".ts");
        Assert.Equal("var s = `https://example.com`;", result);
    }

    // ── 6. Mixed comments and code ───────────────────────────────────

    [Fact]
    public void StripComments_HandlesMixedCommentsAndCode()
    {
        var input =
            "using System; // import\n" +
            "/* block\n" +
            "   comment */\n" +
            "var url = \"https://example.com\"; // url\n" +
            "Console.WriteLine(url);";

        var expected =
            "using System; \n" +
            "\n" +
            "var url = \"https://example.com\"; \n" +
            "Console.WriteLine(url);";

        var result = _sut.StripComments(input, ".cs");
        Assert.Equal(expected, result);
    }

    // ── 7. File with no comments returns unchanged content ───────────

    [Fact]
    public void StripComments_NoComments_ReturnsUnchanged()
    {
        var input = "var x = 1;\nvar y = 2;";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal(input, result);
    }

    // ── Edge cases ───────────────────────────────────────────────────

    [Fact]
    public void StripComments_UnknownExtension_ReturnsUnchanged()
    {
        var input = "some content // with slashes";
        var result = _sut.StripComments(input, ".txt");
        Assert.Equal(input, result);
    }

    [Fact]
    public void StripComments_EmptyContent_ReturnsEmpty()
    {
        var result = _sut.StripComments("", ".cs");
        Assert.Equal("", result);
    }

    [Fact]
    public void StripComments_NullContent_ReturnsNull()
    {
        var result = _sut.StripComments(null!, ".cs");
        Assert.Null(result);
    }

    [Fact]
    public void StripComments_TsExtension_StripsComments()
    {
        var input = "let x = 1; // comment";
        var result = _sut.StripComments(input, ".ts");
        Assert.Equal("let x = 1; ", result);
    }

    [Fact]
    public void StripComments_JsExtension_NotDiscoveredSoLeftUnchanged()
    {
        // .js is intentionally not in the discovery/strip extension set (the tool
        // targets .ts for Angular). Unknown-to-the-stripper extensions pass through.
        var input = "let x = 1; // comment";
        var result = _sut.StripComments(input, ".js");
        Assert.Equal(input, result);
    }

    [Fact]
    public void StripComments_ScssExtension_StripsComments()
    {
        var input = "$color: red; // comment\n/* block */\nbody { }";
        var result = _sut.StripComments(input, ".scss");
        Assert.Equal("$color: red; \n\nbody { }", result);
    }

    [Fact]
    public void StripComments_CssExtension_StripsComments()
    {
        var input = "body { } /* comment */";
        var result = _sut.StripComments(input, ".css");
        Assert.Equal("body { } ", result);
    }

    [Fact]
    public void StripComments_EscapedQuoteInsideString_Preserved()
    {
        var input = "var s = \"she said \\\"hello\\\"\"; // comment";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal("var s = \"she said \\\"hello\\\"\"; ", result);
    }

    // ── C1: CSS/SCSS unquoted url() must survive the // path ──────────

    [Fact]
    public void StripComments_PreservesUnquotedAbsoluteUrl_Scss()
    {
        // SCSS supports // line comments, but a // inside url(...) is part of the URL.
        var input = "body { background: url(http://cdn.example.com/bg.png); }";
        var result = _sut.StripComments(input, ".scss");
        Assert.Equal(input, result);
    }

    [Fact]
    public void StripComments_PreservesProtocolRelativeImportUrl_Css()
    {
        // CSS has no // line comments at all; the protocol-relative URL must survive.
        var input = "@import url(//cdn.example.com/reset.css);";
        var result = _sut.StripComments(input, ".css");
        Assert.Equal(input, result);
    }

    [Fact]
    public void StripComments_DoesNotStripDoubleSlashInCss()
    {
        // CSS has no // comments, so a stray // is content, not a comment to delete.
        var input = "a { color: red; } // not-a-comment";
        var result = _sut.StripComments(input, ".css");
        Assert.Equal(input, result);
    }

    [Fact]
    public void StripComments_StripsLineCommentInScss()
    {
        var input = "$x: 1; // gone\n.a { color: red; }";
        var result = _sut.StripComments(input, ".scss");
        Assert.Equal("$x: 1; \n.a { color: red; }", result);
    }

    [Fact]
    public void StripComments_StripsBlockCommentInCss()
    {
        var input = "a { /* c */ color: red; }";
        var result = _sut.StripComments(input, ".css");
        Assert.Equal("a {  color: red; }", result);
    }

    // ── C2/C3: C# verbatim string awareness ──────────────────────────

    [Fact]
    public void StripComments_PreservesMultiLineVerbatimString_AndStripsTrailingComment()
    {
        // A real-newline verbatim string with an embedded URL and trailing comment.
        var input = "var json = @\"{\n  \"\"url\"\": \"\"http://example.com\"\"\n}\"; // trailing";
        var expected = "var json = @\"{\n  \"\"url\"\": \"\"http://example.com\"\"\n}\"; ";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void StripComments_VerbatimStringEndingInBackslash_ClosesCorrectly()
    {
        // @"C:\temp\" — the trailing backslash is literal; the next " closes the string.
        var input = "var dir = @\"C:\\temp\\\"; // comment";
        var expected = "var dir = @\"C:\\temp\\\"; ";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void StripComments_PreservesInterpolatedVerbatimString()
    {
        var input = "var s = $@\"path//{x}\"; // c";
        var expected = "var s = $@\"path//{x}\"; ";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void StripComments_DoesNotTreatEscapedIdentifierAsVerbatim()
    {
        // @class is an escaped identifier, not a verbatim string opener.
        var input = "var @class = 1; // gone";
        var expected = "var @class = 1; ";
        var result = _sut.StripComments(input, ".cs");
        Assert.Equal(expected, result);
    }
}
