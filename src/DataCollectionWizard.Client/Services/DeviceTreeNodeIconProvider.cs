using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.MonochromeIcons.Assets.Extensions;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace DataCollectionWizard.Client.Services;

internal static class DeviceTreeNodeIconProvider
{
    private static readonly IIcon s_deviceIcon = new SvgIcon(MonochromeIconName.DeviceLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.DeviceLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
    };

    private static readonly IIcon s_folderIcon = new SvgIcon(MonochromeIconName.Folder.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.Folder.GetCssClasses(MonochromeIconSize.SmallPlus2)
    };

    private static readonly IIcon s_portIcon = new SvgIcon(MonochromeIconName.PortSolid.GetSvgMarkup(MonochromeIconSize.Small) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.PortSolid.GetCssClasses(MonochromeIconSize.Small)
    };

    private static readonly IIcon s_processDataPointIcon = new SvgIcon(MonochromeIconName.ProcessDataPoint.GetSvgMarkup(MonochromeIconSize.Small) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.ProcessDataPoint.GetCssClasses(MonochromeIconSize.Small)
    };

    private static readonly IIcon s_rawDataIcon = new SvgIcon(MonochromeIconName.RawDataLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.RawDataLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
    };

    private static readonly IIcon s_sensorIcon = new SvgIcon(MonochromeIconName.SensorSolid.GetSvgMarkup(MonochromeIconSize.Small) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.SensorSolid.GetCssClasses(MonochromeIconSize.Small)
    };

    private static readonly IIcon s_unknownNodeTypeIcon = new SvgIcon(MonochromeIconName.UnknownNodeType.GetSvgMarkup(MonochromeIconSize.Small) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.UnknownNodeType.GetCssClasses(MonochromeIconSize.Small)
    };

    private static readonly IIcon s_vseAlarmIcon = new SvgIcon(MonochromeIconName.AlarmLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.AlarmLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
    };

    private static readonly IIcon s_vseCounterIcon = new SvgIcon(MonochromeIconName.CounterLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.CounterLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
    };

    private static readonly IIcon s_vseInputsIcon = new SvgIcon(MonochromeIconName.InputLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.InputLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
    };

    private static readonly IIcon s_vseObjectIcon = new SvgIcon(MonochromeIconName.ObjectLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.ObjectLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
    };

    private static readonly IIcon s_vseVariantsIcon = new SvgIcon(MonochromeIconName.Branch.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
    {
        CssClasses = MonochromeIconName.Branch.GetCssClasses(MonochromeIconSize.SmallPlus2)
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
