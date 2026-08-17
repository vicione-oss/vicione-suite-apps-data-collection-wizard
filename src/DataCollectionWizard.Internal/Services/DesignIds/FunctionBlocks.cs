namespace DataCollectionWizard.Internal.Services.DesignIds;

public static class FunctionBlocks
{
    public const int DefaultContainerSize = 20;
    public const int DefaultHorizontalSeparation = 560;
    public const int DefaultVerticalSeparation = 300;

    public static AnnaDataPort AnnaDataPort { get; } = new();
    public static AnnaObjectData AnnaObjectData { get; } = new();
    public static AnnaRawData AnnaRawData { get; } = new();
    public static BlobSubscriber BlobSubscriber { get; } = new();
    public static BooleanToDouble BooleanToDouble { get; } = new();
    public static ConstantString ConstantString { get; } = new();
    public static IntervalStatistic IntervalStatistic { get; } = new();
    public static DataFormatter DataFormatter { get; } = new();
    public static ErrorStateGuard ErrorStateGuard { get; } = new();
    public static IoLinkBooleanSubscriber IoLinkBooleanSubscriber { get; } = new();
    public static IoTCoreConfiguration IoTCoreConfiguration { get; } = new();
    public static IoLinkDoubleSubscriber IoLinkDoubleSubscriber { get; } = new();
    public static IoLinkMasterDiagnosticSubscriber IoLinkMasterDiagnosticSubscriber { get; } = new();
    public static IoLinkMasterScanner IoLinkMasterScanner { get; } = new();
    public static IoLinkStringSubscriber IoLinkStringSubscriber { get; } = new();
    public static LongToDouble LongToDouble { get; } = new();
    public static MqttDataPort MqttDataPort { get; } = new();
    public static RpmAtMinMaxTracker RpmAtMinMaxTracker { get; } = new();
    public static Scheduler Scheduler { get; } = new();
    public static VseAlarmSubscriber VseAlarmSubscriber { get; } = new();
    public static VseCounterSubscriber VseCounterSubscriber { get; } = new();
    public static VseDeviceTreeSubscriber VseDeviceTreeSubscriber { get; } = new();
    public static VseInputSubscriber VseInputSubscriber { get; } = new();
    public static VseObjectSubscriber VseObjectSubscriber { get; } = new();
    public static VseRawDataSubscriber VseRawDataSubscriber { get; } = new();
    public static VseScanner VseScanner { get; } = new();
    public static VseVariantSubscriber VseVariantSubscriber { get; } = new();
}
