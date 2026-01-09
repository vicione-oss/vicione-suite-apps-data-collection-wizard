using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Sdk.Connections.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Extensions;

public static class DeviceTreeDataNodeExtensions
{
    public static void AddConfigurations(this IDeviceTreeDataNode dataNode, IReadOnlyCollection<Connection> connections)
    {
        var annaCloudfilter = new AnnaCloudFilter();

        if (dataNode is IDeviceTreeCompressableDataNode compressableDataNode)
        {
            var configsToAdd = connections.Where(cfg => !compressableDataNode.CompressorConfigurations.Any(con => con.DataGroupIdentifier == cfg.Id));

            foreach (var config in configsToAdd)
            {
                compressableDataNode.CompressorConfigurations.Add(new CompressorConfiguration
                {
                    CompressionTime = annaCloudfilter.GetCloudConnections([config]).Any() ? (int)PoolingGrid.SecondsTen : (int)PoolingGrid.MinutesOne,
                    DataGroupIdentifier = config.Id,
                    Enabled = false,
                    PoolingMode = annaCloudfilter.GetCloudConnections([config]).Any() ? PoolingMode.MinMaxAvg : PoolingMode.Last,
                });
            }
        }
        else if (dataNode is IDeviceTreeSchedulableDataNode schedulableDataNode)
        {
            var configsToAdd = connections.Where(cfg => !schedulableDataNode.SchedulerConfigurations.Any(con => con.DataGroupIdentifier == cfg.Id));

            foreach (var config in configsToAdd)
            {
                var newSchedulerConfig = new SchedulerConfiguration
                {
                    DataGroupIdentifier = config.Id,
                    Enabled = false,
                };

                schedulableDataNode.SchedulerConfigurations.Add(newSchedulerConfig);
                newSchedulerConfig.Times[DayOfWeek.Monday] = [TimeSpan.FromSeconds(0)];
            }
        }

        if (dataNode is IDeviceTreeEventTriggerDataNode triggerDataNode)
        {
            foreach (var sensor in triggerDataNode.EventTriggerConfigurations)
            {
                foreach (var config in connections)
                {
                    if (sensor.Triggers.Any(t => t.DataGroupIdentifier == config.Id))
                    {
                        continue;
                    }

                    sensor.Triggers.Add(new()
                    {
                        DataGroupIdentifier = config.Id,
                        Enabled = false,
                        Delay = 1,
                    });
                }
            }
        }

        if (dataNode is IDeviceTreeConfigurableRawDataNode configurableRawDataNode)
        {
            var configsToAdd = connections.Where(cfg => !configurableRawDataNode.RawDataConfigurations.Any(con => con.Key == cfg.Id));

            foreach (var config in configsToAdd)
            {
                configurableRawDataNode.RawDataConfigurations.Add(config.Id, new RawDataSettings { Duration = 10000, Frequency = 100000, });
            }
        }
    }

    public static void RemoveConfigurations(this IDeviceTreeDataNode dataNode, IEnumerable<Guid> existingConfigurations)
    {
        if (dataNode is IDeviceTreeCompressableDataNode compressableDataNode)
            compressableDataNode.CompressorConfigurations.RemoveAll(con => existingConfigurations.All(cfg => cfg != con.DataGroupIdentifier));
        else if (dataNode is IDeviceTreeSchedulableDataNode schedulableDataNode)
            schedulableDataNode.SchedulerConfigurations.RemoveAll(con => existingConfigurations.All(cfg => cfg != con.DataGroupIdentifier));

        if (dataNode is IDeviceTreeEventTriggerDataNode triggerDataNode)
        {
            foreach (var sensor in triggerDataNode.EventTriggerConfigurations)
            {
                sensor.Triggers.RemoveAll(t => existingConfigurations.All(cfg => cfg != t.DataGroupIdentifier));
            }
        }
    }
}
