namespace GetFiles.Services;

/// <summary>
/// Discovers files in a repository, respecting .gitignore rules and other exclusion patterns.
/// </summary>
public interface IFileDiscoveryService
{
    /// <summary>
    /// Walks the repository tree and returns a list of file paths to include in aggregation.
    /// </summary>
    /// <param name="repositoryPath">The root path of the repository to scan.</param>
    /// <returns>An ordered, read-only list of absolute file paths.</returns>
    IReadOnlyList<string> DiscoverFiles(string repositoryPath);
}
