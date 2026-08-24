using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Models;
using ViciOne.Ui.Localization.Resources;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Sidebar;

public sealed partial class DataCollectionWizardSidebar : ComponentBase
{
    private ObservableCollection<ExpandableMenuEntry> _entries = [];

    protected override void OnInitialized()
        => _entries =
        [
            new()
            {
                ContentType = typeof(DeviceTreeSidebarSection),
                IconCssClass = MonochromeIconName.DataflowSolid.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated(),
                IsDefault = true,
                Label = CommonVocabulary.DevicePlural,
            },
            new()
            {
                ContentType = typeof(InfoPanel),
                IconCssClass = MonochromeIconName.InfoOutlined.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated(),
                IsSticky = true,
                Label = Localization.DataCollectionWizardPage.Information,
            },
        ];
}
