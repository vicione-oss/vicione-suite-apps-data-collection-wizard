using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Sdk.Connections.Contracts;

namespace DataCollectionWizard.Internal.Extensions;

public static class PublishTargetsFilter
{
    public static HashSet<Connection> GetPublishTargets(IReadOnlyCollection<Connection> connections,
        IEnumerable<ICloudFilter> cloudFilters)
    {
        var publishTargets = new HashSet<Connection>();

        foreach (var cloudFilter in cloudFilters)
        {
            var cloudConnections = cloudFilter.GetCloudConnections(connections);
            foreach (var cloudConnection in cloudConnections)
                publishTargets.Add(cloudConnection);
        }

        return publishTargets;
    }
}
