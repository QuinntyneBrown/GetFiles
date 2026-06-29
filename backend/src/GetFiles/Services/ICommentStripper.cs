namespace GetFiles.Services;

/// <summary>
/// Removes comments from source code based on the file's language.
/// </summary>
public interface ICommentStripper
{
    /// <summary>
    /// Strips comments from the given source content.
    /// </summary>
    /// <param name="content">The raw file content.</param>
    /// <param name="fileExtension">The file extension (e.g. ".cs", ".ts") used to determine comment syntax.</param>
    /// <returns>The content with comments removed.</returns>
    string StripComments(string content, string fileExtension);
}
