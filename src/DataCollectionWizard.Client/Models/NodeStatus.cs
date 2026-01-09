namespace DataCollectionWizard.Client.Models;

[Flags]
internal enum NodeStatus
{
    None = 0b0000,
    New = 0b0001,
    Offline = 0b0010,
    Unknown = 0b0100,
    NotSupported = 0b1000,
}
