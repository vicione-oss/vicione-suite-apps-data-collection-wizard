using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;

namespace DataCollectionWizard.Internal.Extensions;

internal static class ClusterBuilderExtensions
{
    public static void AddDataPortDesign(this ClusterBuilder builder, string dataPortDesignId)
    {
        var clusterDependency = builder.DependencyResolver.ResolveDataPortDesignDependency(dataPortDesignId);

        if (!builder.Cache.IsDependencyExisting(clusterDependency))
        {
            builder.Editors.Cluster.AddDependency(clusterDependency.Name, clusterDependency.Version);
        }
    }
}
