using System.Globalization;
using DataCollectionWizard.Client.Components.Localization;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.TreeNodeTemplates;

// ─── Tooltip content model ───────────────────────────────────────────────────
// The tooltip body is a list of sections. A section is either a group of key/value rows, a free-text description
// block, or a path block. The header/subtitle and the status footer are handled by the component itself.

internal enum TooltipSectionKind
{
    Rows,
    // Each row is rendered as a stacked label-over-value pair (heading like "Description"), used for the fields
    // shown next to the device image.
    KeyValues,
    Description,
    Path,
}

internal sealed record TooltipRow(string Label, string Value);

internal sealed record TooltipSection(TooltipSectionKind Kind, string? Label, IReadOnlyList<TooltipRow> Rows, string? Text)
{
    public static TooltipSection Group(string? label, IReadOnlyList<TooltipRow> rows)
        => new(TooltipSectionKind.Rows, label, rows, null);

    public static TooltipSection KeyValues(IReadOnlyList<TooltipRow> rows)
        => new(TooltipSectionKind.KeyValues, null, rows, null);

    public static TooltipSection Description(string text)
        => new(TooltipSectionKind.Description, DeviceTreeTooltip.InfoPropertyDescription, [], text);

    public static TooltipSection Path(string text)
        => new(TooltipSectionKind.Path, DeviceTreeTooltip.InfoPropertyPath, [], text);
}

// Collects key/value rows, skipping empty values.
internal sealed class TooltipRowBuilder
{
    private readonly List<TooltipRow> _rows = [];

    public bool HasRows => _rows.Count > 0;

    public TooltipRowBuilder Add(string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _rows.Add(new TooltipRow(label, value!));

        return this;
    }

    public IReadOnlyList<TooltipRow> Build() => _rows;
}

// ─── Per-capability providers ────────────────────────────────────────────────
// Each provider describes how a kind of node fills the tooltip body (and, optionally, the subtitle). Matching is
// anchored on device-tree *interfaces* wherever one exists (IDeviceTreeMasterNode, IDeviceTreeVseDataParent,
// IDeviceTreeSchedulableDataNode), so a new concrete type that implements the interface is handled automatically.
// Only fields that live solely on concrete classes (Type, InputType, Unit, MAC, Vendor/Device ID) are read via a
// cast. To support an entirely new node shape, add a provider and register it in DeviceTooltipProviders.All.

internal interface IDeviceTooltipProvider
{
    bool CanHandle(IDeviceTreeBase device);

    string? GetSubtitle(IDeviceTreeBase device) => null;

    IEnumerable<TooltipSection> GetSections(IDeviceTreeBase device);
}

internal static class DeviceTooltipFormat
{
    // Joins the given parts with " · ", skipping empty ones (e.g. "ifm electronic · IO-Link Master").
    public static string? Join(params string?[] parts)
    {
        var joined = string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    // A master URL such as "http://192.168.0.90:3321/" is shown as the bare host ("192.168.0.90"). The port is left
    // out deliberately: Uri.Authority hides it only when it is the scheme's default, so an IO-Link master (http, 80)
    // showed none while a VSE (3321) did - the same field reading differently per device type.
    public static string? Address(Uri? url)
    {
        if (url is null)
            return null;

        return string.IsNullOrEmpty(url.Host) ? url.ToString() : url.Host;
    }

    public static string? Alias(IDeviceTreeBase device)
        => device is IDeviceTreeAliasNode aliasNode ? aliasNode.Alias : null;

    // An unset application-specific tag comes back from the device empty or as all-asterisks ("***"); treat both as
    // "no tag" so the row is skipped and the following field moves up.
    public static string? Tag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        var trimmed = tag.Trim();
        return trimmed.All(character => character == '*') ? null : trimmed;
    }

    // The node's free-text description as a section, or null when it has none.
    public static TooltipSection? DescriptionSection(IDeviceTreeBase device)
    {
        var text = device.Description?.Text;
        return string.IsNullOrWhiteSpace(text) ? null : TooltipSection.Description(text!);
    }

    public static string? Mac(IDeviceTreeMasterNode master)
        => master switch
        {
            DeviceTreeIoLinkMaster ioLinkMaster => ioLinkMaster.MacAddress,
            DeviceTreeVseDevice vseDevice => vseDevice.MacAddress,
            _ => null,
        };
}

// IO-Link master and VSE device (both IDeviceTreeMasterNode): address/MAC and hardware; manufacturer + family form
// the subtitle.
internal sealed class MasterTooltipProvider : IDeviceTooltipProvider
{
    public bool CanHandle(IDeviceTreeBase device) => device is IDeviceTreeMasterNode;

    public string? GetSubtitle(IDeviceTreeBase device)
    {
        var master = (IDeviceTreeMasterNode)device;
        return DeviceTooltipFormat.Join(master.Manufacturer, master.DeviceFamily);
    }

    public IEnumerable<TooltipSection> GetSections(IDeviceTreeBase device)
    {
        var master = (IDeviceTreeMasterNode)device;

        if (DeviceTooltipFormat.DescriptionSection(device) is { } description)
            yield return description;

        var network = new TooltipRowBuilder()
            .Add(DeviceTreeTooltip.InfoPropertyAddress, DeviceTooltipFormat.Address(master.Url))
            .Add(DeviceTreeTooltip.InfoPropertyMacAddress, DeviceTooltipFormat.Mac(master));
        if (network.HasRows)
            yield return TooltipSection.Group(DeviceTreeTooltip.GroupNetwork, network.Build());

        var hardware = new TooltipRowBuilder()
            .Add(DeviceTreeTooltip.InfoPropertySerialNumber, master.SerialNumber)
            .Add(DeviceTreeTooltip.InfoPropertyHardwareRevision, master.HardwareRevision)
            .Add(DeviceTreeTooltip.InfoPropertySoftwareRevision, master.SoftwareRevision);
        if (hardware.HasRows)
            yield return TooltipSection.Group(DeviceTreeTooltip.GroupHardware, hardware.Build());
    }
}

// IO-Link device: identity (name/tag, vendor/device id, serial). No richer interface exists, so it stays concrete.
internal sealed class IoLinkDeviceTooltipProvider : IDeviceTooltipProvider
{
    public bool CanHandle(IDeviceTreeBase device) => device is DeviceTreeDevice;

    public IEnumerable<TooltipSection> GetSections(IDeviceTreeBase device)
    {
        var treeDevice = (DeviceTreeDevice)device;

        // Order: name/tag/serial, then the description, then the vendor/device identity.
        var primary = new TooltipRowBuilder()
            .Add(DeviceTreeTooltip.InfoPropertyAlias, DeviceTooltipFormat.Alias(device))
            .Add(DeviceTreeTooltip.InfoPropertyApplicationSpecificTag, DeviceTooltipFormat.Tag(treeDevice.ApplicationSpecificTag))
            .Add(DeviceTreeTooltip.InfoPropertySerialNumber, treeDevice.SerialNumber);
        if (primary.HasRows)
            yield return TooltipSection.KeyValues(primary.Build());

        if (DeviceTooltipFormat.DescriptionSection(device) is { } description)
            yield return description;

        var identity = new TooltipRowBuilder()
            .Add(DeviceTreeTooltip.InfoPropertyVendorId, treeDevice.VendorId.ToString(CultureInfo.InvariantCulture))
            .Add(DeviceTreeTooltip.InfoPropertyDeviceId, treeDevice.DeviceId.ToString(CultureInfo.InvariantCulture));
        if (identity.HasRows)
            yield return TooltipSection.Group(DeviceTreeTooltip.GroupIdentity, identity.Build());
    }
}

// Any VSE data-model node (object, counter, input, alarm, variants): matched via IDeviceTreeVseDataParent. Name and
// Path come from interfaces; Input type / Type / Unit only exist on the concrete classes and are read per type - a
// new VSE node type is still shown (name + path) even before its type-specific fields are wired up here.
internal sealed class VseDataParentTooltipProvider : IDeviceTooltipProvider
{
    public bool CanHandle(IDeviceTreeBase device) => device is IDeviceTreeVseDataParent;

    public IEnumerable<TooltipSection> GetSections(IDeviceTreeBase device)
    {
        var vseParent = (IDeviceTreeVseDataParent)device;

        if (DeviceTooltipFormat.DescriptionSection(device) is { } description)
            yield return description;

        var data = new TooltipRowBuilder()
            .Add(DeviceTreeTooltip.InfoPropertyAlias, DeviceTooltipFormat.Alias(device))
            .Add(DeviceTreeTooltip.InfoPropertyInputType, InputTypeOf(device))
            .Add(DeviceTreeTooltip.InfoPropertyType, TypeOf(device))
            .Add(DeviceTreeTooltip.InfoPropertyUnit, UnitOf(device));
        if (data.HasRows)
            yield return TooltipSection.Group(DeviceTreeTooltip.GroupData, data.Build());

        if (!string.IsNullOrWhiteSpace(vseParent.Path))
            yield return TooltipSection.Path(vseParent.Path);
    }

    private static string? InputTypeOf(IDeviceTreeBase device)
        => device is DeviceTreeVseObject vseObject ? vseObject.InputType : null;

    private static string? TypeOf(IDeviceTreeBase device)
        => device switch
        {
            DeviceTreeVseObject vseObject => vseObject.Type,
            DeviceTreeVseCounter vseCounter => vseCounter.Type,
            DeviceTreeVseAlarm vseAlarm => vseAlarm.Type,
            _ => null,
        };

    private static string? UnitOf(IDeviceTreeBase device)
        => device switch
        {
            DeviceTreeVseObject vseObject => vseObject.Unit,
            DeviceTreeVseCounter vseCounter => vseCounter.Unit,
            DeviceTreeVseInput vseInput => vseInput.Unit,
            _ => null,
        };
}

// Raw-data / schedulable nodes (IDeviceTreeSchedulableDataNode) expose their sensor type.
internal sealed class RawDataTooltipProvider : IDeviceTooltipProvider
{
    public bool CanHandle(IDeviceTreeBase device) => device is IDeviceTreeSchedulableDataNode;

    public IEnumerable<TooltipSection> GetSections(IDeviceTreeBase device)
    {
        var schedulable = (IDeviceTreeSchedulableDataNode)device;

        if (DeviceTooltipFormat.DescriptionSection(device) is { } description)
            yield return description;

        var sensor = new TooltipRowBuilder()
            .Add(DeviceTreeTooltip.InfoPropertySensorType, schedulable.SensorType);
        if (sensor.HasRows)
            yield return TooltipSection.Group(DeviceTreeTooltip.GroupSensor, sensor.Build());
    }
}

internal static class DeviceTooltipProviders
{
    // Order matters: the first provider whose CanHandle returns true builds the type-specific sections. The
    // interface groups are mutually exclusive, so order only fixes precedence for future overlaps. Add a new
    // provider here to support a new node shape.
    public static readonly IReadOnlyList<IDeviceTooltipProvider> All =
    [
        new MasterTooltipProvider(),
        new IoLinkDeviceTooltipProvider(),
        new VseDataParentTooltipProvider(),
        new RawDataTooltipProvider(),
    ];

    public static IDeviceTooltipProvider? For(IDeviceTreeBase device)
        => All.FirstOrDefault(provider => provider.CanHandle(device));
}
