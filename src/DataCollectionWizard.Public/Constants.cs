using Sdk.Connections.Contracts;

namespace DataCollectionWizard.Public;

public sealed class Constants
{
    public const string DataCollectionWizardPageRoute = "/data-collection-wizard";
    public const string LiveViewPageRoute = "/data-collection-wizard-live";

    public static readonly Tag AnnaCloud = new("ANNA", new("947A7069-F4E4-412A-B7A8-147BA4388E77")) { Protected = true };
    public static readonly Tag MoneoConnectCloud = new("MoneoConnect", new("B1816973-5C6C-42C7-BAE5-4D3371A00135")) { Protected = true };
}
