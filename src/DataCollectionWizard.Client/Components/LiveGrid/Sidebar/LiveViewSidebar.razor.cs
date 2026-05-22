using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Models;
using ViciOne.Ui.Blazor.Components.Sidebar.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;
using ViciOne.Ui.Localization.Resources;

namespace DataCollectionWizard.Client.Components.LiveGrid.Sidebar;

public sealed partial class LiveViewSidebar : ComponentBase
{
    private bool _compactMode;
    private ObservableCollection<ExpandableMenuEntry> _entries = [];
    private int? _sidebarFluidWidth;

    private SidebarMode GetSidebarMode()
        => _compactMode ? SidebarMode.Compact : SidebarMode.Fluid;

    protected override void OnInitialized()
        => _entries =
        [
            new()
            {
                ContentType = typeof(LiveViewSidebarSection),
                IconCssClass = MonochromeIconName.DataflowSolid.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated(),
                IsDefault = true,
                Label = CommonVocabulary.DevicePlural,
            },
        ];
}
