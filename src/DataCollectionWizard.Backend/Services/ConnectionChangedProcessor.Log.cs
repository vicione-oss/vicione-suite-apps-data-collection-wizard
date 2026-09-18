using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class ConnectionChangedProcessor
{
    [LoggerMessage(LogLevel.Warning, "An error occurred while processing the queued connection changes")]
    private static partial void LogErrorAfterConnectionChange(ILogger logger, Exception exception);
}
