using DataCollectionWizard.Public;
using Sdk.Authorization;
using Sdk.Client.NavTiles.Attributes;
using Sdk.Client.NavTiles.Components;

namespace DataCollectionWizard.Client.Components.NavTiles;

[InitialNavTile<DataCollectionWizardClientModule>(LinkTarget = Constants.DataCollectionWizardPageRoute)]
[ModuleAuthorize<DataCollectionWizardClientModule>(AccessLevel.Full)]
public partial class DataCollectionWizardNavTile : NavTileBase
{ }
