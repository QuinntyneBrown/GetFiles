namespace GetFiles.Services;

/// <summary>
/// Aggregates the content of multiple source files into a single output file.
/// </summary>
public interface ICodeAggregatorService
{
    /// <summary>
    /// Reads each file, optionally strips comments and whitespace, and writes the combined output.
    /// </summary>
    /// <param name="files">The list of absolute file paths to aggregate.</param>
    /// <param name="repositoryPath">The root path of the repository (used for relative path headers).</param>
    /// <param name="outputPath">The destination file path for the aggregated output.</param>
    /// <param name="stripComments">Whether to strip comments from source files.</param>
    /// <param name="stripWhitespace">Whether to collapse unnecessary whitespace.</param>
    void Aggregate(IReadOnlyList<string> files, string repositoryPath, string outputPath, bool stripComments, bool stripWhitespace);
}
