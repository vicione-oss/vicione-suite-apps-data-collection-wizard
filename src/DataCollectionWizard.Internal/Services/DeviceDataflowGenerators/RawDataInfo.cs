namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class RawDataInfo
{
    public Dictionary<Guid, EventTriggerRawDataInfo> EventTriggerSensors { get; } = [];
    public Dictionary<Guid, ScheduledRawDataInfo> SchedulerSensors { get; } = [];
}
