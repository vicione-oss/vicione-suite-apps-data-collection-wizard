using Microsoft.AspNetCore.Components;
using Sdk.Connections.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class UncompressableCell : ComponentBase
{
    [Parameter, EditorRequired]
    public IDeviceTreeCompressableDataNode CompressableDataNode { get; set; } = default!;

    [Parameter, EditorRequired]
    public Connection Configuration { get; set; } = default!;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    private bool IsPoolingEnabled()
        // TODO: fängt ein paar Fehler in den Tests deren genauer Ursprung mit Jörg geklärt werden muss
        => (CompressableDataNode.CompressorConfigurations.Find(cc => cc.DataGroupIdentifier == Configuration.Id)?.Enabled).GetValueOrDefault();

    private void PoolingEnabledChanged(bool isEnabled)
    {
        CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Id)
            .Enabled = isEnabled;

        OnDeviceTreeChanged.InvokeAsync();
    }
}
