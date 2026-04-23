using DataCollectionWizard.Client.Components.LiveGrid.Services;
using DataCollectionWizard.Client.Extensions;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace DataCollectionWizard.Client.Components.LiveGrid.Sidebar;

public sealed partial class LiveViewSidebarSection : ComponentBase, IDisposable
{
    private static readonly string s_expandIconCssClass = MonochromeIconName.ExpanderLightDown.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated();
    private static readonly string s_collapseIconCssClass = MonochromeIconName.ExpanderLightTop.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated();

    private string? _filterText;

    [CascadingParameter]
    private LiveGridService Service { get; set; } = default!;

    public void Dispose()
        => Service.TreeBuilder.Selection.SelectionChanged -= OnTreeSelectionChangedAsync;

    private void OnFilterTextChanging(string? filterText)
    {
        _filterText = filterText;

        Service.TreeBuilder.ApplyFilter(_filterText);
    }

    private void OnExpandTreeButtonClick()
        => Service.TreeBuilder.ChangeExpansion(true);

    private void OnCollapseTreeButtonClick()
        => Service.TreeBuilder.ChangeExpansion(false);

    protected override void OnInitialized()
        => Service.TreeBuilder.Selection.SelectionChanged += OnTreeSelectionChangedAsync;

    private async void OnTreeSelectionChangedAsync(ITreeNode node, bool selected)
        => await InvokeAsync(StateHasChanged);
}
