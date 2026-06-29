using GetFiles.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GetFiles.Tests;

public class FileDiscoveryServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileDiscoveryService _service;

    public FileDiscoveryServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GetFilesTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var logger = NullLogger<FileDiscoveryService>.Instance;
        _service = new FileDiscoveryService(logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Helpers

    private string CreateFile(string relativePath, string content = "")
    {
        var fullPath = Path.Combine(_tempDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    #endregion

    #region L2-2.1: File Discovery by Extension

    [Fact]
    public void DiscoverFiles_FindsFilesWithValidExtensions()
    {
        // Arrange
        CreateFile("app.ts", "export class App {}");
        CreateFile("index.html", "<html></html>");
        CreateFile("styles.scss", "body {}");
        CreateFile("main.css", "body {}");
        CreateFile("Program.cs", "class Program {}");
        CreateFile("Project.csproj", "<Project />");
        CreateFile("Solution.sln", "");
        CreateFile("config.json", "{}");
        CreateFile("deploy.yaml", "key: value");
        CreateFile("settings.yml", "key: value");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Equal(10, files.Count);
        Assert.Contains(files, f => f.EndsWith("app.ts"));
        Assert.Contains(files, f => f.EndsWith("index.html"));
        Assert.Contains(files, f => f.EndsWith("styles.scss"));
        Assert.Contains(files, f => f.EndsWith("main.css"));
        Assert.Contains(files, f => f.EndsWith("Program.cs"));
        Assert.Contains(files, f => f.EndsWith("Project.csproj"));
        Assert.Contains(files, f => f.EndsWith("Solution.sln"));
        Assert.Contains(files, f => f.EndsWith("config.json"));
        Assert.Contains(files, f => f.EndsWith("deploy.yaml"));
        Assert.Contains(files, f => f.EndsWith("settings.yml"));
    }

    [Fact]
    public void DiscoverFiles_ExcludesFilesWithInvalidExtensions()
    {
        // Arrange
        CreateFile("app.ts", "export class App {}");
        CreateFile("readme.md", "# Readme");
        CreateFile("image.png", "binary");
        CreateFile("script.py", "print('hi')");
        CreateFile("notes.txt", "notes");
        CreateFile("data.xml", "<root/>");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("app.ts"));
    }

    [Fact]
    public void DiscoverFiles_FindsFilesInSubdirectories()
    {
        // Arrange
        CreateFile("src/app/app.component.ts", "export class App {}");
        CreateFile("src/app/app.component.html", "<div></div>");
        CreateFile("src/styles/global.scss", "body {}");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void DiscoverFiles_ReturnsAbsolutePathsWithForwardSlashes()
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Single(files);
        var filePath = files[0];

        // Should be an absolute path. The service emits forward-slash paths on every
        // platform (e.g. "C:/repo/src/app.ts" on Windows, "/repo/src/app.ts" on Linux),
        // both of which Path.IsPathRooted recognizes — so assert on the path as-is.
        // (The previous Replace('/', '\\') hack failed on Linux, where a leading "\"
        // is not a root indicator.)
        Assert.True(Path.IsPathRooted(filePath),
            $"Path should be absolute: {filePath}");

        // Should use forward slashes
        Assert.DoesNotContain("\\", filePath);
    }

    [Fact]
    public void DiscoverFiles_ReturnsSortedResults()
    {
        // Arrange
        CreateFile("z-file.ts", "");
        CreateFile("a-file.ts", "");
        CreateFile("m-file.ts", "");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Equal(3, files.Count);
        var fileNames = files.Select(Path.GetFileName).ToList();
        Assert.Equal("a-file.ts", fileNames[0]);
        Assert.Equal("m-file.ts", fileNames[1]);
        Assert.Equal("z-file.ts", fileNames[2]);
    }

    #endregion

    #region L2-2.3: Hardcoded Directory Exclusions

    [Theory]
    [InlineData("node_modules")]
    [InlineData("dist")]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData(".git")]
    public void DiscoverFiles_ExcludesHardcodedDirectories(string excludedDir)
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");
        CreateFile($"{excludedDir}/package.json", "{}");
        CreateFile($"src/{excludedDir}/something.ts", "export class X {}");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
    }

    [Fact]
    public void DiscoverFiles_ExcludedNameInRepositoryRootPath_DoesNotExcludeFiles()
    {
        // Arrange — the repository root itself lives under a directory literally
        // named "bin". Exclusions are relative to the root, so files inside the
        // repository must still be discovered (the "bin" prefix is not the repo's).
        var repoRoot = Path.Combine(_tempDir, "bin", "myrepo");
        Directory.CreateDirectory(repoRoot);
        File.WriteAllText(Path.Combine(repoRoot, "app.ts"), "export class App {}");
        var srcDir = Path.Combine(repoRoot, "src");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "b.ts"), "export class B {}");

        // Act
        var files = _service.DiscoverFiles(repoRoot);

        // Assert
        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void DiscoverFiles_ExcludesHardcodedDirectoriesAtAnyDepth()
    {
        // Arrange
        CreateFile("src/app.ts", "");
        CreateFile("deep/nested/node_modules/lib/index.ts", "");
        CreateFile("another/path/bin/Debug/output.json", "");
        CreateFile("some/obj/cache.json", "");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
    }

    #endregion

    #region L2-2.2: .gitignore Filtering

    [Fact]
    public void DiscoverFiles_RespectsRootGitignorePatterns()
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");
        CreateFile("src/secret.ts", "secret stuff");
        CreateFile("coverage/lcov.json", "{}");
        CreateFile(".gitignore", "coverage/\nsrc/secret.ts\n");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
    }

    [Fact]
    public void DiscoverFiles_RespectsWildcardGitignorePatterns()
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");
        CreateFile("src/app.spec.ts", "test stuff");
        CreateFile("src/other.spec.ts", "more tests");
        CreateFile(".gitignore", "*.spec.ts\n");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
    }

    [Fact]
    public void DiscoverFiles_HandlesNoGitignoreGracefully()
    {
        // Arrange - no .gitignore
        CreateFile("src/app.ts", "export class App {}");
        CreateFile("src/other.cs", "class Other {}");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void DiscoverFiles_IgnoresCommentsAndEmptyLinesInGitignore()
    {
        // Arrange
        CreateFile("src/app.ts", "");
        CreateFile("src/keep.ts", "");
        CreateFile(".gitignore", "# This is a comment\n\n   \nsrc/app.ts\n");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/keep.ts"));
    }

    [Fact]
    public void DiscoverFiles_RespectsNestedGitignoreFiles()
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");
        CreateFile("src/generated/output.ts", "generated");
        CreateFile("src/generated/keep.ts", "keep this");
        CreateFile("src/generated/.gitignore", "output.ts\n");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
        Assert.Contains(files, f => f.EndsWith("src/generated/keep.ts"));
    }

    [Fact]
    public void DiscoverFiles_NestedGitignoreNegation_ReincludesNegatedFile()
    {
        // Arrange: a nested .gitignore ignores all .ts then re-includes keep.ts.
        // The leading "!" must be preserved when the pattern is prefixed with its
        // directory, otherwise the negation is lost and keep.ts is wrongly dropped.
        CreateFile("src/foo/output.ts", "generated");
        CreateFile("src/foo/keep.ts", "keep this");
        CreateFile("src/foo/.gitignore", "*.ts\n!keep.ts\n");

        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/foo/keep.ts"));
    }

    #endregion

    #region L2-2.4: User-Specified Ignore Paths

    [Fact]
    public void DiscoverFiles_IgnoreSingleDirectory_ExcludesMatchingDirectory()
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");
        CreateFile("tests/app.spec.ts", "test stuff");
        CreateFile("tests/helpers/setup.ts", "setup");

        // Act
        var files = _service.DiscoverFiles(_tempDir, new[] { "tests" });

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
    }

    [Fact]
    public void DiscoverFiles_IgnoreMultipleDirectories_ExcludesAll()
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");
        CreateFile("tests/app.spec.ts", "test stuff");
        CreateFile("docs/readme.html", "<html>docs</html>");

        // Act
        var files = _service.DiscoverFiles(_tempDir, new[] { "tests", "docs" });

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
    }

    [Fact]
    public void DiscoverFiles_IgnorePathWorksAtAnyDepth()
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");
        CreateFile("src/db/migrations/001_init.cs", "class Migration {}");
        CreateFile("migrations/schema.json", "{}");

        // Act
        var files = _service.DiscoverFiles(_tempDir, new[] { "migrations" });

        // Assert
        Assert.Single(files);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
    }

    [Fact]
    public void DiscoverFiles_NoIgnorePaths_BehaviorUnchanged()
    {
        // Arrange
        CreateFile("src/app.ts", "export class App {}");
        CreateFile("tests/app.spec.ts", "test stuff");
        CreateFile("docs/readme.html", "<html>docs</html>");

        // Act – no ignore paths provided
        var files = _service.DiscoverFiles(_tempDir);

        // Assert – all files should be present
        Assert.Equal(3, files.Count);
        Assert.Contains(files, f => f.EndsWith("src/app.ts"));
        Assert.Contains(files, f => f.EndsWith("tests/app.spec.ts"));
        Assert.Contains(files, f => f.EndsWith("docs/readme.html"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DiscoverFiles_ReturnsEmptyForNonexistentPath()
    {
        // Act
        var files = _service.DiscoverFiles(Path.Combine(_tempDir, "nonexistent"));

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void DiscoverFiles_ReturnsEmptyForEmptyDirectory()
    {
        // Act
        var files = _service.DiscoverFiles(_tempDir);

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void DiscoverFiles_IncludedExtensionsSetContainsAllExpectedExtensions()
    {
        // Verify the static set has exactly the expected extensions
        var expected = new HashSet<string>
        {
            ".ts", ".html", ".scss", ".css", ".cs",
            ".csproj", ".sln", ".json", ".yaml", ".yml"
        };

        Assert.Equal(expected.Count, FileDiscoveryService.IncludedExtensions.Count);
        foreach (var ext in expected)
        {
            Assert.Contains(ext, FileDiscoveryService.IncludedExtensions);
        }
    }

    [Fact]
    public void DiscoverFiles_ExcludedDirectoriesSetContainsAllExpectedDirs()
    {
        var expected = new HashSet<string> { "node_modules", "dist", "bin", "obj", ".git" };

        Assert.Equal(expected.Count, FileDiscoveryService.ExcludedDirectories.Count);
        foreach (var dir in expected)
        {
            Assert.Contains(dir, FileDiscoveryService.ExcludedDirectories);
        }
    }

    #endregion
}
