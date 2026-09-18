using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Models;
using ViciOne.Ui.Localization.Resources;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;

namespace DataCollectionWizard.Client.Components.LiveGrid.Sidebar;

public sealed partial class LiveViewSidebar : ComponentBase
{
    private ObservableCollection<ExpandableMenuEntry> _entries = [];

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
