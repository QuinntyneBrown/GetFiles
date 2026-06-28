namespace GetFiles.Services;

/// <summary>
/// Collapses and normalizes unnecessary whitespace in source content.
/// </summary>
public interface IWhitespaceStripper
{
    /// <summary>
    /// Strips excessive whitespace from the given content.
    /// </summary>
    /// <param name="content">The raw or comment-stripped content.</param>
    /// <returns>The content with whitespace normalized.</returns>
    string StripWhitespace(string content);
}
