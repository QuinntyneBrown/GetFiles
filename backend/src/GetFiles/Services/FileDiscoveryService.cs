using Microsoft.Extensions.Logging;

namespace GetFiles.Services;

/// <summary>
/// Default implementation of <see cref="IFileDiscoveryService"/>.
/// Walks the file system and applies .gitignore-style filtering.
/// </summary>
public class FileDiscoveryService : IFileDiscoveryService
{
    private readonly ILogger<FileDiscoveryService> _logger;

    /// <summary>
    /// File extensions to include during discovery.
    /// </summary>
    public static readonly IReadOnlySet<string> IncludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".ts",
        ".html",
        ".scss",
        ".css",
        ".cs",
        ".csproj",
        ".sln",
        ".json",
        ".yaml",
        ".yml"
    };

    /// <summary>
    /// Directory names that are always excluded, regardless of .gitignore content.
    /// </summary>
    public static readonly IReadOnlySet<string> ExcludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules",
        "dist",
        "bin",
        "obj",
        ".git"
    };

    /// <summary>
    /// File names that are always excluded, regardless of extension or .gitignore content.
    /// <c>package-lock.json</c> is generated, enormous, and noise for an LLM payload.
    /// </summary>
    public static readonly IReadOnlySet<string> ExcludedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "package-lock.json"
    };

    public FileDiscoveryService(ILogger<FileDiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> DiscoverFiles(string repositoryPath, IReadOnlyList<string>? additionalIgnorePaths = null)
    {
        var rootPath = Path.GetFullPath(repositoryPath);

        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("Repository path does not exist: {Path}", rootPath);
            return Array.Empty<string>();
        }

        _logger.LogInformation("Discovering files in {Path}", rootPath);

        // Build the gitignore filter from root and nested .gitignore files
        var ignore = BuildIgnoreFilter(rootPath);

        // Build a set of user-specified ignore directory names for fast lookup
        var userIgnoreSet = additionalIgnorePaths != null && additionalIgnorePaths.Count > 0
            ? new HashSet<string>(additionalIgnorePaths, StringComparer.OrdinalIgnoreCase)
            : null;

        var results = new List<string>();

        foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            // Normalize to forward slashes for consistency
            var normalizedFile = PathUtil.ToForwardSlashes(file);

            // Directory exclusions are matched on the path *relative to the repository
            // root*, not the absolute path — otherwise an excluded-dir name in the root
            // prefix (e.g. a repo living under ".../bin/...") would wrongly exclude every
            // file in the repository.
            var relativePath = PathUtil.GetRelativePath(rootPath, normalizedFile);

            // Check if the file is inside an excluded directory
            if (PathHasSegmentIn(relativePath, ExcludedDirectories))
            {
                continue;
            }

            // Check if the file name itself is always excluded (e.g. package-lock.json)
            if (ExcludedFileNames.Contains(Path.GetFileName(normalizedFile)))
            {
                continue;
            }

            // Check if the file is inside a user-specified ignore directory
            if (userIgnoreSet != null && PathHasSegmentIn(relativePath, userIgnoreSet))
            {
                _logger.LogDebug("Excluded by --ignore: {Path}", normalizedFile);
                continue;
            }

            // Check if the extension is in the included set
            var extension = Path.GetExtension(normalizedFile);
            if (!IncludedExtensions.Contains(extension))
            {
                continue;
            }

            // Check against .gitignore rules
            if (ignore != null && ignore.IsIgnored(relativePath))
            {
                _logger.LogDebug("Excluded by .gitignore: {Path}", relativePath);
                continue;
            }

            results.Add(normalizedFile);
        }

        results.Sort(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Discovered {Count} files", results.Count);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Builds a combined Ignore filter from root and nested .gitignore files.
    /// Returns null if no .gitignore files are found.
    /// </summary>
    private Ignore.Ignore? BuildIgnoreFilter(string rootPath)
    {
        var gitignoreFiles = Directory.EnumerateFiles(rootPath, ".gitignore", SearchOption.AllDirectories)
            .Where(f => !PathHasSegmentIn(PathUtil.GetRelativePath(rootPath, f), ExcludedDirectories))
            .ToList();

        if (gitignoreFiles.Count == 0)
        {
            _logger.LogDebug("No .gitignore files found in {Path}", rootPath);
            return null;
        }

        var ignore = new Ignore.Ignore();

        foreach (var gitignoreFile in gitignoreFiles)
        {
            var lines = File.ReadAllLines(gitignoreFile);
            var gitignoreDir = Path.GetDirectoryName(gitignoreFile)!;
            var relativeDirPath = PathUtil.GetRelativePath(rootPath, gitignoreDir);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip blank lines and comments. This is NOT redundant with the Ignore
                // library's own comment/blank handling: for nested .gitignore files we
                // rewrite each pattern with a directory prefix below, and a blank or
                // "#"-prefixed line would otherwise be turned into a bogus pattern
                // (e.g. "" -> "src/foo/", silently ignoring the whole directory).
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                // For nested .gitignore files, prefix patterns with the relative directory.
                if (!string.IsNullOrEmpty(relativeDirPath) && relativeDirPath != ".")
                {
                    ignore.Add(PrefixPattern(trimmed, relativeDirPath));
                }
                else
                {
                    ignore.Add(trimmed);
                }
            }

            _logger.LogDebug("Loaded .gitignore from {Path}", gitignoreFile);
        }

        return ignore;
    }

    /// <summary>
    /// Prefixes a nested .gitignore pattern with its directory (relative to the repo root).
    /// A leading "!" negation is preserved so re-include rules keep working — e.g.
    /// "!keep.ts" in "src/foo" becomes "!src/foo/keep.ts", not "src/foo/!keep.ts"
    /// (which the Ignore library would treat as a literal filename, not a negation).
    /// </summary>
    private static string PrefixPattern(string pattern, string relativeDirPath)
    {
        var negated = pattern.StartsWith('!');
        if (negated)
        {
            pattern = pattern[1..];
        }

        // A leading slash anchors the pattern to the .gitignore's own directory.
        var prefixed = pattern.StartsWith('/')
            ? relativeDirPath + pattern
            : relativeDirPath + "/" + pattern;

        return negated ? "!" + prefixed : prefixed;
    }

    /// <summary>
    /// Checks whether any directory segment of the file path (excluding the filename)
    /// is contained in <paramref name="names"/>. Serves both the hardcoded exclusions
    /// and user-specified ignore directories.
    /// </summary>
    private static bool PathHasSegmentIn(string normalizedPath, IReadOnlySet<string> names)
    {
        var segments = normalizedPath.Split('/');
        for (int i = 0; i < segments.Length - 1; i++) // Skip the last segment (it's the filename)
        {
            if (names.Contains(segments[i]))
            {
                return true;
            }
        }
        return false;
    }
}
