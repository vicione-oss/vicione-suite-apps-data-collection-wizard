using DataCollectionWizard.Client.Components.LiveGrid.Models;
using Microsoft.AspNetCore.Components;

namespace DataCollectionWizard.Client.Components.LiveGrid.GridCells;

public sealed partial class DeviceIdentifierCell : ComponentBase
{
    /// <summary>
    /// Where this value sits in the tree, shown as a second line under its name.
    /// </summary>
    [Parameter]
    public string? Context { get; set; }

    [Parameter, EditorRequired]
    public LiveGridRowModel Model { get; set; } = default!;
}
