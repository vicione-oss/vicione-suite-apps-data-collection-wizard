using Sdk.Client.Modules.Localization;

namespace DataCollectionWizard.Client.Localization;

internal class DataCollectionWizardLocalizer : IClientModuleLocalizer<DataCollectionWizardClientModule>
{
    public string GetTitle() => "Data Collection Wizard";
}
