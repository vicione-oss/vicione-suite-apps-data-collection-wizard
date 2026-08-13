using System.Globalization;
using DataCollectionWizard.Client.Components.Localization;
using DataCollectionWizard.Client.Services;
using Microsoft.AspNetCore.Components;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.TreeNodeTemplates;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Only for visual display.")]
public sealed partial class DeviceTreeNodeTooltip
{
    private const string DeviceInfoSeparatorKey = "separator";

    private string _deviceImageDataBase64 = string.Empty;

    [Inject]
    private IoddImageProvider IoddImageProvider { get; set; } = default!;

    [Parameter, EditorRequired]
    public required IDeviceTreeBase Device { get; set; }

    [Parameter]
    public bool IsLiveView { get; set; }

    [Parameter]
    public int InheritedStatus { get; set; }

    private async Task<string> GetDeviceImage()
    {
        if (Device is not DeviceTreeDevice treeDevice)
            return string.Empty;

        if (treeDevice.Description is null || string.IsNullOrWhiteSpace(treeDevice.Description.Icon))
            return string.Empty;

        return await IoddImageProvider.GetIoddImageDataBase64Async(treeDevice.VendorId, treeDevice.DeviceId, treeDevice.Description.Icon);
    }

    private List<(string Key, string Value)> GetDeviceInfo()
    {
        var result = new List<(string Key, string Value)>();

        // only show properties if an alias is set
        if (Device is IDeviceTreeAliasNode aliasNode && !string.IsNullOrWhiteSpace(aliasNode.Alias))
        {
            AddInfo(DeviceTreeTooltip.InfoPropertyAlias, aliasNode.Alias);
        }

        if (Device.Description is not null && !string.IsNullOrWhiteSpace(Device.Description.Text))
        {
            AddSeparatorIfNeeded();
            AddInfo(DeviceTreeTooltip.InfoPropertyDescription, Device.Description.Text);
        }

        if (Device is IDeviceTreeMasterNode masterNode)
        {
            if (masterNode is DeviceTreeIoLinkMaster ioLinkMaster)
            {
                AddSeparatorIfNeeded();
                AddInfo(DeviceTreeTooltip.InfoPropertyMacAddress, ioLinkMaster.MacAddress);
            }
            else if (masterNode is DeviceTreeVseDevice vse)
            {
                AddSeparatorIfNeeded();
                AddInfo(DeviceTreeTooltip.InfoPropertyMacAddress, vse.MacAddress);
            }

            AddSeparatorIfNeeded();
            AddInfo(DeviceTreeTooltip.InfoPropertySerialNumber, masterNode.SerialNumber);
            AddInfo(DeviceTreeTooltip.InfoPropertyHardwareRevision, masterNode.HardwareRevision);
            AddInfo(DeviceTreeTooltip.InfoPropertySoftwareRevision, masterNode.SoftwareRevision);
            AddSeparator();
            AddInfo(DeviceTreeTooltip.InfoPropertyProductName, masterNode.ProductName);
            AddInfo(DeviceTreeTooltip.InfoPropertyDeviceFamily, masterNode.DeviceFamily);
            AddSeparator();
            AddInfo(DeviceTreeTooltip.InfoPropertyManufacturer, masterNode.Manufacturer);
        }

        if (Device is DeviceTreeDevice treeDevice)
        {
            AddSeparatorIfNeeded();
            AddInfo(DeviceTreeTooltip.InfoPropertyApplicationSpecificTag, treeDevice.ApplicationSpecificTag);
            AddInfo(DeviceTreeTooltip.InfoPropertyVendorId, treeDevice.VendorId.ToString(CultureInfo.InvariantCulture));
            AddInfo(DeviceTreeTooltip.InfoPropertyDeviceId, treeDevice.DeviceId.ToString(CultureInfo.InvariantCulture));
        }

        if (Device is DeviceTreeVseAlarm vseAlarm)
        {
            AddSeparatorIfNeeded();
            AddInfo(DeviceTreeTooltip.InfoPropertyPath, vseAlarm.Path);
            AddInfo(DeviceTreeTooltip.InfoPropertyType, vseAlarm.Type);
        }

        if (Device is DeviceTreeVseCounter vseCounter)
        {
            AddSeparatorIfNeeded();
            AddInfo(DeviceTreeTooltip.InfoPropertyPath, vseCounter.Path);
            AddInfo(DeviceTreeTooltip.InfoPropertyType, vseCounter.Type);
            AddInfo(DeviceTreeTooltip.InfoPropertyUnit, vseCounter.Unit);
        }

        if (Device is DeviceTreeVseInput vseInput)
        {
            AddSeparatorIfNeeded();
            AddInfo(DeviceTreeTooltip.InfoPropertyPath, vseInput.Path);
            AddInfo(DeviceTreeTooltip.InfoPropertyUnit, vseInput.Unit);
        }

        if (Device is DeviceTreeVseObject vseObject)
        {
            AddSeparatorIfNeeded();
            AddInfo(DeviceTreeTooltip.InfoPropertyInputType, vseObject.InputType);
            AddInfo(DeviceTreeTooltip.InfoPropertyPath, vseObject.Path);
            AddInfo(DeviceTreeTooltip.InfoPropertyType, vseObject.Type);
            AddInfo(DeviceTreeTooltip.InfoPropertyUnit, vseObject.Unit);
        }

        if (Device is DeviceTreeVseRawData vseRawData)
        {
            AddSeparatorIfNeeded();
            AddInfo(DeviceTreeTooltip.InfoPropertyIsWritable, vseRawData.IsWriteable.ToString().ToLowerInvariant());
            AddInfo(DeviceTreeTooltip.InfoPropertySensorType, vseRawData.SensorType);
        }

        return result;

        void AddInfo(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                result.Add((key, value));
        }

        void AddSeparator()
            => result.Add((DeviceInfoSeparatorKey, ""));

        void AddSeparatorIfNeeded()
        {
            if (result.Count > 0 && result.Last().Key != DeviceInfoSeparatorKey)
                AddSeparator();
        }
    }

    protected override async Task OnInitializedAsync()
        => _deviceImageDataBase64 = await GetDeviceImage();
}
