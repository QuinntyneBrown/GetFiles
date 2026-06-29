using GetFiles.Services;
using Microsoft.Extensions.Logging;

namespace GetFiles.Commands;

/// <summary>
/// Handles execution of the "aggregate" subcommand.
/// Receives all dependencies via constructor injection.
/// </summary>
public class AggregateCommandHandler
{
    private readonly IFileDiscoveryService _fileDiscoveryService;
    private readonly ICodeAggregatorService _codeAggregatorService;
    private readonly ILogger<AggregateCommandHandler> _logger;

    public AggregateCommandHandler(
        IFileDiscoveryService fileDiscoveryService,
        ICodeAggregatorService codeAggregatorService,
        ILogger<AggregateCommandHandler> logger)
    {
        _fileDiscoveryService = fileDiscoveryService;
        _codeAggregatorService = codeAggregatorService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the aggregate workflow: discover files, then aggregate them.
    /// The strip flags are already resolved to their effective values by the caller.
    /// </summary>
    public int Execute(string path, string output, bool stripComments, bool stripWhitespace, string[] ignore)
    {
        try
        {
            var repositoryPath = Path.GetFullPath(path);

            if (!Directory.Exists(repositoryPath))
            {
                _logger.LogError("The specified path does not exist: {Path}", repositoryPath);
                return 1;
            }

            _logger.LogInformation("Discovering files in {Path}...", repositoryPath);
            var files = _fileDiscoveryService.DiscoverFiles(repositoryPath, ignore);

            _logger.LogInformation("Found {Count} file(s). Aggregating...", files.Count);
            var outputPath = Path.GetFullPath(output);
            _codeAggregatorService.Aggregate(files, repositoryPath, outputPath, stripComments, stripWhitespace);

            _logger.LogInformation("Aggregation complete. Output written to {OutputPath}", outputPath);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during aggregation");
            return 1;
        }
    }
}
