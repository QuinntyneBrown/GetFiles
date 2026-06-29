namespace GetFiles.Services;

/// <summary>
/// Path-presentation helpers shared across services. Centralizes the
/// "present paths with forward slashes" concern so it has a single name
/// and a single source of truth.
/// </summary>
internal static class PathUtil
{
    /// <summary>
    /// Converts any backslashes in a path to forward slashes for OS-independent presentation.
    /// </summary>
    public static string ToForwardSlashes(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Gets the relative path from <paramref name="rootPath"/> to
    /// <paramref name="targetPath"/>, using forward slashes.
    /// </summary>
    public static string GetRelativePath(string rootPath, string targetPath)
        => ToForwardSlashes(Path.GetRelativePath(rootPath, targetPath));
}
