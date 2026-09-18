using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;

namespace DataCollectionWizard.Internal.Extensions;

internal static class FunctionBlockExtensions
{
    internal static ConnectorInput GetInputByDesignId(this FunctionBlock functionBlock, Guid designId)
    {
        var input = functionBlock.GetAllInputs().FirstOrDefault(o => o.DesignId == designId);
        return input ?? throw new ArgumentException($"{functionBlock.Name} does not have an input connector with the DesignId {designId}");
    }

    internal static ConnectorInput? GetInputByName(this FunctionBlock functionBlock, string name)
        => functionBlock.GetAllInputs().FirstOrDefault(o => o.Name == name);

    internal static ConnectorOutput GetOutputByDesignId(this FunctionBlock functionBlock, Guid designId)
    {
        var output = functionBlock.GetAllOutputs().FirstOrDefault(o => o.DesignId == designId);
        return output ?? throw new ArgumentException($"{functionBlock.Name} does not have an output connector with the DesignId {designId}");
    }

    internal static ConnectorOutput? GetOutputByName(this FunctionBlock functionBlock, string name)
        => functionBlock.GetAllOutputs().FirstOrDefault(o => o.Name == name);
}
