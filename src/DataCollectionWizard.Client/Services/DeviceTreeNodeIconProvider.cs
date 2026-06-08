using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.MonochromeIcons.Assets.Extensions;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace DataCollectionWizard.Client.Services;

internal static class DeviceTreeNodeIconProvider
{
    private static readonly IIcon s_deviceIcon = CreateIcon(MonochromeIconName.DeviceLight, MonochromeIconSize.SmallPlus2);
    private static readonly IIcon s_folderIcon = CreateIcon(MonochromeIconName.Folder, MonochromeIconSize.SmallPlus2);
    private static readonly IIcon s_portIcon = CreateIcon(MonochromeIconName.PortSolid, MonochromeIconSize.Small);
    private static readonly IIcon s_processDataPointIcon = CreateIcon(MonochromeIconName.ProcessDataPoint, MonochromeIconSize.Small);
    private static readonly IIcon s_rawDataIcon = CreateIcon(MonochromeIconName.RawDataLight, MonochromeIconSize.SmallPlus2);
    private static readonly IIcon s_sensorIcon = CreateIcon(MonochromeIconName.SensorSolid, MonochromeIconSize.Small);
    private static readonly IIcon s_unknownNodeTypeIcon = CreateIcon(MonochromeIconName.UnknownNodeType, MonochromeIconSize.Small);
    private static readonly IIcon s_vseAlarmIcon = CreateIcon(MonochromeIconName.AlarmLight, MonochromeIconSize.SmallPlus2);
    private static readonly IIcon s_vseCounterIcon = CreateIcon(MonochromeIconName.CounterLight, MonochromeIconSize.SmallPlus2);
    private static readonly IIcon s_vseInputsIcon = CreateIcon(MonochromeIconName.InputLight, MonochromeIconSize.SmallPlus2);
    private static readonly IIcon s_vseObjectIcon = CreateIcon(MonochromeIconName.ObjectLight, MonochromeIconSize.SmallPlus2);
    private static readonly IIcon s_vseVariantsIcon = CreateIcon(MonochromeIconName.Branch, MonochromeIconSize.SmallPlus2);

    internal static SvgIcon CreateIcon(MonochromeIconName iconName, MonochromeIconSize iconSize)
        => new(iconName.GetSvgMarkup(iconSize) ?? string.Empty)
        {
            CssClasses = iconName.GetCssClasses(iconSize)
        };

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

    public static IIcon GetIcon(IDeviceTreeBase? treeDevice)
    {
        if (treeDevice is DeviceTreeRoot or DeviceTreeIoLinkMaster or DeviceTreeVseDevice)
            return s_deviceIcon;

        if (treeDevice is DeviceTreeIoLinkMasterPort)
            return s_portIcon;

        if (treeDevice is DeviceTreeDevice)
            return s_sensorIcon;

        if (treeDevice is DeviceTreeProcessData)
            return s_processDataPointIcon;

        if (treeDevice is IDeviceTreeSchedulableDataNode)
            return s_rawDataIcon;

        if (treeDevice is DeviceTreeVseAlarm)
            return s_vseAlarmIcon;

        if (treeDevice is DeviceTreeVseCounter)
            return s_vseCounterIcon;

        if (treeDevice is DeviceTreeVseInput)
            return s_vseInputsIcon;

        if (treeDevice is DeviceTreeVseObject)
            return s_vseObjectIcon;

        if (treeDevice is DeviceTreeVseVariants)
            return s_vseVariantsIcon;

        if (treeDevice is DeviceTreeStructureNode)
        {
            if (treeDevice.Children.Count > 0)
            {
                var relevantChild = GetFirstNonStructureChildRecursively(treeDevice);
                return GetIcon(relevantChild);
            }

            return s_folderIcon;
        }

        return s_unknownNodeTypeIcon;
    }
}
