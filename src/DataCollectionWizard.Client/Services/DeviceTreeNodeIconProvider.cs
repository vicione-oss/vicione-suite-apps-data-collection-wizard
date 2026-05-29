using DataCollectionWizard.Client.Resources;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.MonochromeIcons.Assets.Extensions;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;

namespace DataCollectionWizard.Client.Services;

internal static class DeviceTreeNodeIconProvider
{
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
        {
            return new SvgIcon(MonochromeIconName.DeviceLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.DeviceLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
            };
        }

        if (treeDevice is DeviceTreeIoLinkMasterPort)
        {
            return new SvgIcon(MonochromeIconName.PortSolid.GetSvgMarkup(MonochromeIconSize.Small) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.PortSolid.GetCssClasses(MonochromeIconSize.Small)
            };
        }

        if (treeDevice is DeviceTreeDevice)
        {
            return new SvgIcon(MonochromeIconName.SensorSolid.GetSvgMarkup(MonochromeIconSize.Small) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.SensorSolid.GetCssClasses(MonochromeIconSize.Small)
            };
        }

        if (treeDevice is DeviceTreeProcessData)
        {
            return new SvgIcon(MonochromeIconName.ProcessDataPoint.GetSvgMarkup(MonochromeIconSize.Small) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.ProcessDataPoint.GetCssClasses(MonochromeIconSize.Small)
            };
        }

        if (treeDevice is IDeviceTreeSchedulableDataNode)
        {
            return new SvgIcon(MonochromeIconName.RawDataLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.RawDataLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
            };
        }

        if (treeDevice is DeviceTreeVseAlarm)
        {
            return new SvgIcon(MonochromeIconName.AlarmLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.AlarmLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
            };
        }

        if (treeDevice is DeviceTreeVseCounter)
        {
            return new SvgIcon(MonochromeIconName.CounterLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.CounterLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
            };
        }

        if (treeDevice is DeviceTreeVseInput)
        {
            return new SvgIcon(MonochromeIconName.InputLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.InputLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
            };
        }

        if (treeDevice is DeviceTreeVseObject)
        {
            return new SvgIcon(MonochromeIconName.ObjectLight.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.ObjectLight.GetCssClasses(MonochromeIconSize.SmallPlus2)
            };
        }

        if (treeDevice is DeviceTreeVseVariants)
        {
            return new SvgIcon(MonochromeIconName.Recursive.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.Recursive.GetCssClasses(MonochromeIconSize.SmallPlus2)
            };
        }


        if (treeDevice is DeviceTreeStructureNode)
        {
            if (treeDevice.Children.Count > 0)
            {
                var relevantChild = GetFirstNonStructureChildRecursively(treeDevice);
                return GetIcon(relevantChild);
            }

            return new SvgIcon(MonochromeIconName.Folder.GetSvgMarkup(MonochromeIconSize.SmallPlus2) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.Folder.GetCssClasses(MonochromeIconSize.SmallPlus2)
            };
        }

        return new SvgIcon(MonochromeIconName.UnknownKnotType.GetSvgMarkup(MonochromeIconSize.Small) ?? string.Empty)
        {
            CssClasses = MonochromeIconName.UnknownKnotType.GetCssClasses(MonochromeIconSize.Small)
        };
    }
}
