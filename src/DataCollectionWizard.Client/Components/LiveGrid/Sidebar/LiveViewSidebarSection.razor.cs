using DataCollectionWizard.Client.Components.LiveGrid.Services;
using DataCollectionWizard.Client.Extensions;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace DataCollectionWizard.Client.Components.LiveGrid.Sidebar;

public sealed partial class LiveViewSidebarSection : ComponentBase, IDisposable
{
    [CascadingParameter]
    private LiveGridService Service { get; set; } = default!;

    public void Dispose()
        => Service.TreeBuilder.Selection.SelectionChanged -= OnTreeSelectionChangedAsync;

    private void OnFilterTextChanged(string filterText)
        => Service.TreeBuilder.ApplyFilter(filterText);

    private void OnChangeTreeExpansionClicked(bool expand)
        => Service.TreeBuilder.ChangeExpansion(expand);

    protected override void OnInitialized()
        => Service.TreeBuilder.Selection.SelectionChanged += OnTreeSelectionChangedAsync;

    private async void OnTreeSelectionChangedAsync(ITreeNode node, bool selected)
        => await InvokeAsync(StateHasChanged);
}
