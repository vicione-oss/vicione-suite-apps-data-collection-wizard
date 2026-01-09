using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Public;
using Microsoft.AspNetCore.Components;
using Sdk.Authorization;
using Sdk.Client.Infrastructure;
using Sdk.Client.NavTiles.Attributes;
using Sdk.Client.NavTiles.Components;

namespace DataCollectionWizard.Client.Components.NavTiles;

[InitialNavTile<DataCollectionWizardClientModule>(LinkTarget = Constants.LiveViewPageRoute)]
[ModuleAuthorize<DataCollectionWizardClientModule>(AccessLevel.Partial)]
public partial class LiveViewNavTile : NavTileBase
{
    [Inject] private IUiMediator Mediator { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var response = await Mediator.Request<GetDevicesRequest, GetDevicesResponse>(new());
        if (response.RequestError is not null)
        {
            State.Enabled = false;
            return;
        }

        // only enabled if at least one devices is configured
        State.Enabled = response.Devices.Count > 0;
    }
}
