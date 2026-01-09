using DataCollectionWizard.Client.Components.LiveGrid.Models;
using Microsoft.AspNetCore.Components;

namespace DataCollectionWizard.Client.Components.LiveGrid.GridCells;

public sealed partial class DeviceIdentifierCell : ComponentBase
{
    [Parameter, EditorRequired]
    public LiveGridRowModel Model { get; set; } = default!;
}
