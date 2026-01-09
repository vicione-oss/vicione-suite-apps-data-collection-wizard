using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Core.Dataflow.DataModel;

namespace DataCollectionWizard.Client.Tests;

internal static class ClusterBuilderExtensions
{
    public static FunctionBlockDesign GetOrThrowFunctionBlockDesign(this ClusterBuilder builder, string functionBlockDesignName)
        => builder.Cache.FunctionBlockDesigns.Keys
            .Select(builder.ResolveFunctionBlockDesign)
            .First(design => design.Name == functionBlockDesignName);
}
