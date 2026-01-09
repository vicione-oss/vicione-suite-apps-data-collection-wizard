using Microsoft.Extensions.Logging;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Commands;

public record AddDeviceTreeEngine(IEnumerable<DeviceEngineInfo> Infos, bool AllowUseExistingEngine, LogLevel LogLevel) : ICommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
