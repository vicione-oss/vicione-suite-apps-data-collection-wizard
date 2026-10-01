using DataCollectionWizard.Client.Resources;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Ui.MonochromeIcons.Assets.Services;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace DataCollectionWizard.Client.Services;

internal sealed class DeviceTreeNodeIconProvider(IMonochromeIconSvgMarkupProvider monochromeIconSvgMarkupProvider)
{
    private readonly IIcon _diagnosticsIcon = new SvgIcon(SvgIcons.diagnostics);

    internal IIcon GetOrCreateIcon(MonochromeIconName iconName, MonochromeIconSize iconSize)
    {
        var svgMarkup = monochromeIconSvgMarkupProvider.GetSvgMarkup(iconName, iconSize);

        return string.IsNullOrEmpty(svgMarkup)
            ? new SvgIcon(string.Empty) { CssClasses = iconName.GetCssClasses(iconSize) }
            : new SvgIcon(svgMarkup);
    }

    private static IDeviceTreeBase? GetFirstNonStructureChildRecursively(IDeviceTreeBase nodeContext)
    {
        if (nodeContext is not DeviceTreeStructureNode)
            return nodeContext;

        if (nodeContext.Children.Count == 0)
            return nodeContext;

        IDeviceTreeBase? result = null;
        foreach (var child in nodeContext.Children)
        {
            result = GetFirstNonStructureChildRecursively(child);

            if (result is not DeviceTreeStructureNode)
                return result;
        }

        return result;
    }

    public IIcon GetIcon(IDeviceTreeBase? treeDevice)
    {
        if (treeDevice is DeviceTreeRoot or DeviceTreeIoLinkMaster or DeviceTreeVseDevice)
            return GetOrCreateIcon(MonochromeIconName.DeviceLight, MonochromeIconSize.SmallPlus2);

        if (treeDevice is DeviceTreeIoLinkMasterPort)
            return GetOrCreateIcon(MonochromeIconName.PortSolid, MonochromeIconSize.Small);

        if (treeDevice is DeviceTreeDevice)
            return GetOrCreateIcon(MonochromeIconName.SensorSolid, MonochromeIconSize.Small);

        if (treeDevice is DeviceTreeProcessData)
            return GetOrCreateIcon(MonochromeIconName.ProcessDataPoint, MonochromeIconSize.Small);

        if (treeDevice is IDeviceTreeSchedulableDataNode)
            return GetOrCreateIcon(MonochromeIconName.RawDataLight, MonochromeIconSize.SmallPlus2);

        if (treeDevice is DeviceTreeVseAlarm)
            return GetOrCreateIcon(MonochromeIconName.AlarmLight, MonochromeIconSize.SmallPlus2);

        if (treeDevice is DeviceTreeVseCounter)
            return GetOrCreateIcon(MonochromeIconName.CounterLight, MonochromeIconSize.SmallPlus2);

        if (treeDevice is DeviceTreeVseInput)
            return GetOrCreateIcon(MonochromeIconName.InputLight, MonochromeIconSize.SmallPlus2);

        if (treeDevice is DeviceTreeVseObject)
            return GetOrCreateIcon(MonochromeIconName.ObjectLight, MonochromeIconSize.SmallPlus2);

        if (treeDevice is DeviceTreeVseVariants)
            return GetOrCreateIcon(MonochromeIconName.Branch, MonochromeIconSize.SmallPlus2);

        if (treeDevice is DeviceTreeStructureNode)
        {
            // The master's "Diagnostics" group gets its own info icon rather than inheriting a child's.
            if (treeDevice.Id.EndsWith("/diagnostics", StringComparison.Ordinal))
                return _diagnosticsIcon;

            if (treeDevice.Children.Count > 0)
            {
                var relevantChild = GetFirstNonStructureChildRecursively(treeDevice);
                return GetIcon(relevantChild);
            }

            return GetOrCreateIcon(MonochromeIconName.Folder, MonochromeIconSize.SmallPlus2);
        }

        return GetOrCreateIcon(MonochromeIconName.UnknownNodeType, MonochromeIconSize.Small);
    }
}
