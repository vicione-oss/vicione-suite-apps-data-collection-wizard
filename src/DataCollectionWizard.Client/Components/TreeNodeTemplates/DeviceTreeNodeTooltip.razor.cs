using System.Collections.Generic;
using System.Threading.Tasks;
using DataCollectionWizard.Client.Components.Localization;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Client.Services;
using Microsoft.AspNetCore.Components;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.TreeNodeTemplates;

public sealed partial class DeviceTreeNodeTooltip
{
    private string _deviceImageDataBase64 = string.Empty;

    [Inject]
    private IoddImageProvider IoddImageProvider { get; set; } = default!;

    [Parameter, EditorRequired]
    public required IDeviceTreeBase Device { get; set; }

    [Parameter]
    public bool IsLiveView { get; set; }

    [Parameter]
    public int InheritedStatus { get; set; }

    // "Manufacturer · Device family" under the name (master nodes only; the provider decides).
    private string? Subtitle => DeviceTooltipProviders.For(Device)?.GetSubtitle(Device);

    // Whether the master reports that it wants credentials - shown the same way as in the device scan list.
    private bool RequiresAuthentication
        => Device is DeviceTreeIoLinkMaster ioLinkMaster && ioLinkMaster.Security.RequiresAuthentication;

    // The tooltip body. A type-specific provider decides the sections and where the description sits; a node without
    // a provider (e.g. a structure/folder or a plain data node) just shows its description, if any.
    private List<TooltipSection> BuildSections()
    {
        var provider = DeviceTooltipProviders.For(Device);
        if (provider is not null)
            return [.. provider.GetSections(Device)];

        var description = Device.Description?.Text;
        return string.IsNullOrWhiteSpace(description)
            ? []
            : [TooltipSection.Description(description!)];
    }

    // The status footer: the node's own status first, then the status inherited from its subtree ("Contains …"),
    // each mapped to a severity class (err / warn / new) used for the badge colour and the top hairline.
    private List<(string Text, string Severity)> GetStatusFlags()
    {
        var flags = new List<(string, string)>();
        var nodeStatus = Device.GetStatus(IsLiveView);
        var inheritedStatus = (NodeStatus)InheritedStatus & ~nodeStatus;

        void Add(NodeStatus status, NodeStatus flag, string text, string severity)
        {
            if (status.HasFlag(flag))
                flags.Add((text, severity));
        }

        Add(nodeStatus, NodeStatus.Offline, DeviceTreeTooltip.DeviceStatusOffline, "err");
        Add(nodeStatus, NodeStatus.NotSupported, DeviceTreeTooltip.DeviceStatusNotSupported, "warn");
        Add(nodeStatus, NodeStatus.Unknown, DeviceTreeTooltip.DeviceStatusUnknown, "warn");
        Add(nodeStatus, NodeStatus.New, DeviceTreeTooltip.DeviceStatusNewlyCreated, "new");

        Add(inheritedStatus, NodeStatus.Offline, DeviceTreeTooltip.InheritedStatusOffline, "err");
        Add(inheritedStatus, NodeStatus.NotSupported, DeviceTreeTooltip.InheritedStatusNotSupported, "warn");
        Add(inheritedStatus, NodeStatus.Unknown, DeviceTreeTooltip.InheritedStatusUnknown, "warn");
        Add(inheritedStatus, NodeStatus.New, DeviceTreeTooltip.InheritedStatusNewlyCreated, "new");

        return flags;
    }

    // Severity class for the coloured hairline on top of the tooltip (most severe status of node + subtree wins).
    private string TopStateClass
    {
        get
        {
            var status = Device.GetStatus(IsLiveView) | (NodeStatus)InheritedStatus;

            if (status.HasFlag(NodeStatus.Offline))
                return "st-off";

            if (status.HasFlag(NodeStatus.NotSupported) || status.HasFlag(NodeStatus.Unknown))
                return "st-warn";

            if (status.HasFlag(NodeStatus.New))
                return "st-new";

            return string.Empty;
        }
    }

    private async Task<string> GetDeviceImage()
    {
        if (Device is not DeviceTreeDevice treeDevice)
            return string.Empty;

        if (treeDevice.Description is null || string.IsNullOrWhiteSpace(treeDevice.Description.Icon))
            return string.Empty;

        return await IoddImageProvider.GetIoddImageDataBase64Async(treeDevice.VendorId, treeDevice.DeviceId, treeDevice.Description.Icon);
    }

    protected override async Task OnInitializedAsync()
        => _deviceImageDataBase64 = await GetDeviceImage();
}
