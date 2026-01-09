using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Extensions;

internal static class SettingEditorExtensions
{
    internal static void SetFunctionBlockSetting(this SettingEditor settingEditor, FunctionBlock functionBlock, Guid settingsId, object? value)
    {
        var setting = functionBlock.Settings.First(s => s.DesignId == settingsId);
        settingEditor.SetValue(setting, value);
    }
}
