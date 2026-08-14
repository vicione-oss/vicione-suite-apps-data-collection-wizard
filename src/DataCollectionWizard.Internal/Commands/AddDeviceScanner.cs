using Microsoft.Extensions.Logging;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Commands;

public record AddDeviceScanner(LogLevel LogLevel) : ICommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
