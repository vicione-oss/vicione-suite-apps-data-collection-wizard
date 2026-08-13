using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class UncompressableCell : ComponentBase
{
    [Parameter, EditorRequired]
    public IDeviceTreeCompressableDataNode CompressableDataNode { get; set; } = default!;

    [Parameter, EditorRequired]
    public PublishTargetInfo Configuration { get; set; } = default!;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    private bool IsCompressionEnabled()
        // TODO: fängt ein paar Fehler in den Tests deren genauer Ursprung mit Jörg geklärt werden muss
        => (CompressableDataNode.CompressorConfigurations.Find(cc => cc.DataGroupIdentifier == Configuration.Connection.Id)?.Enabled).GetValueOrDefault();

    private void CompressionEnabledChanged(bool isEnabled)
    {
        CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Connection.Id)
            .Enabled = isEnabled;

        OnDeviceTreeChanged.InvokeAsync();
    }
}
