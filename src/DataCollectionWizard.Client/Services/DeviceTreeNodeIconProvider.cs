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
        if (treeDevice is DeviceTreeRoot)
            return new SvgIcon(RoccoSvgIcons.device);

        if (treeDevice is DeviceTreeIoLinkMaster)
            return new SvgIcon(RoccoSvgIcons.device);

        if (treeDevice is DeviceTreeVseDevice)
            return new SvgIcon(RoccoSvgIcons.device);

        if (treeDevice is DeviceTreeIoLinkMasterPort)
            return new SvgIcon(SvgIcons.iolm_port);

        if (treeDevice is DeviceTreeDevice)
            return new SvgIcon(SvgIcons.iolm_sensor);

        if (treeDevice is DeviceTreeProcessData)
            return new SvgIcon(SvgIcons.iolm_processdata);

        if (treeDevice is IDeviceTreeSchedulableDataNode)
            return new SvgIcon(RoccoSvgIcons.vse_rawdata);

        if (treeDevice is DeviceTreeVseAlarm)
        {
            return new SvgIcon(MonochromeIconName.AlarmLight.GetSvgMarkup(MonochromeIconSize.SmallMedium) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.AlarmLight.GetCssClasses(MonochromeIconSize.SmallMedium)
            };
        }

        if (treeDevice is DeviceTreeVseCounter)
            return new SvgIcon(RoccoSvgIcons.vse_counter);

        if (treeDevice is DeviceTreeVseInput)
            return new SvgIcon(RoccoSvgIcons.vse_input);

        if (treeDevice is DeviceTreeVseObject)
            return new SvgIcon(RoccoSvgIcons.vse_object);

        if (treeDevice is DeviceTreeVseVariants)
            return new SvgIcon(RoccoSvgIcons.vse_variant);

        if (treeDevice is DeviceTreeStructureNode)
        {
            if (treeDevice.Children.Count > 0)
            {
                var relevantChild = GetFirstNonStructureChildRecursively(treeDevice);
                return GetIcon(relevantChild);
            }

            return new SvgIcon(MonochromeIconName.Folder.GetSvgMarkup(MonochromeIconSize.SmallMedium) ?? string.Empty)
            {
                CssClasses = MonochromeIconName.Folder.GetCssClasses(MonochromeIconSize.SmallMedium)
            };
        }

        return new SvgIcon(SvgIcons.iolm_unknown);
    }
}
