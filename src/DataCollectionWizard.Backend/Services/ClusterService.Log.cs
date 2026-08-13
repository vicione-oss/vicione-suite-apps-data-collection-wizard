using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class ClusterService
{
    [LoggerMessage(LogLevel.Error, "Failed to apply DeviceTree")]
    private static partial void LogApplicationFailedError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Discarding update request for ticket {TicketId}")]
    partial void LogDiscardingUpdateRequestForTicket(Guid ticketId);

    [LoggerMessage(LogLevel.Debug, "Cluster service issued ticket: {TicketId}")]
    partial void LogIssuedTicket(Guid ticketId);

    [LoggerMessage(LogLevel.Debug, "Ticket timer elapsed")]
    partial void LogTicketTimerElapsed();
}
