using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Core.Dataflow.DataModel;

namespace DataCollectionWizard.Client.Tests;

internal static class ClusterBuilderExtensions
{
    public static FunctionBlockDesign GetOrThrowFunctionBlockDesign(this ClusterBuilder builder, string functionBlockDesignName)
    {
        var resolvedFunctionblocks = builder.Cache.FunctionBlockDesigns.Keys
            .Select(builder.ResolveFunctionBlockDesign);

        return resolvedFunctionblocks.First(design => design.Name == functionBlockDesignName);
    }
}
