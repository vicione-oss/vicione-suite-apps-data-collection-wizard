using DataCollectionWizard.Internal.Events;

namespace DataCollectionWizard.Backend.Services;

public interface IDeviceTreeGuard
{
    Task OnDeviceConnectorIdsChanged(List<DeviceConnectorIdsChangeItem> changedItems);
    Task OnDeviceTreeApplication();
}
