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

    public FileDiscoveryService(ILogger<FileDiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> DiscoverFiles(string repositoryPath)
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

        var results = new List<string>();

        foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            // Normalize to forward slashes for consistency
            var normalizedFile = file.Replace('\\', '/');

            // Check if the file is inside an excluded directory
            if (IsInExcludedDirectory(normalizedFile))
            {
                continue;
            }

            // Check if the extension is in the included set
            var extension = Path.GetExtension(normalizedFile);
            if (!IncludedExtensions.Contains(extension))
            {
                continue;
            }

            // Check against .gitignore rules
            if (ignore != null)
            {
                var relativePath = GetRelativePath(rootPath, normalizedFile);
                if (ignore.IsIgnored(relativePath))
                {
                    _logger.LogDebug("Excluded by .gitignore: {Path}", relativePath);
                    continue;
                }
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
            .Where(f => !IsInExcludedDirectory(f.Replace('\\', '/')))
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
            var relativeDirPath = GetRelativePath(rootPath, gitignoreDir.Replace('\\', '/'));

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                // For nested .gitignore files, prefix patterns with the relative directory
                if (!string.IsNullOrEmpty(relativeDirPath) && relativeDirPath != ".")
                {
                    // If the pattern starts with /, it's relative to the .gitignore location
                    if (trimmed.StartsWith('/'))
                    {
                        ignore.Add(relativeDirPath + trimmed);
                    }
                    else
                    {
                        ignore.Add(relativeDirPath + "/" + trimmed);
                    }
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
    /// Checks whether any segment of the file path is a hardcoded excluded directory.
    /// </summary>
    private static bool IsInExcludedDirectory(string normalizedPath)
    {
        var segments = normalizedPath.Split('/');
        for (int i = 0; i < segments.Length - 1; i++) // Skip the last segment (it's the filename)
        {
            if (ExcludedDirectories.Contains(segments[i]))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the relative path from root to target using forward slashes.
    /// </summary>
    private static string GetRelativePath(string rootPath, string targetPath)
    {
        var relative = Path.GetRelativePath(rootPath, targetPath);
        return relative.Replace('\\', '/');
    }
}
