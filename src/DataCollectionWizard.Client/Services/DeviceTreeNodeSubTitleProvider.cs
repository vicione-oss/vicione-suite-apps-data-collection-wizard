using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Services;

public static class DeviceTreeNodeSubTitleProvider
{
    public static string? GetSubTitle(IDeviceTreeBase deviceTreeBase)
        => deviceTreeBase switch
        {
            DeviceTreeIoLinkMaster masterDevice => $"{masterDevice.Url.DnsSafeHost}:{masterDevice.Url.Port}",
            DeviceTreeVseDevice vseDevice => $"{vseDevice.Url.DnsSafeHost}:{vseDevice.Url.Port}",
            IDeviceTreeDeviceAliasNode aliasStructureNode => aliasStructureNode.Name,
            _ => null
        };
}
