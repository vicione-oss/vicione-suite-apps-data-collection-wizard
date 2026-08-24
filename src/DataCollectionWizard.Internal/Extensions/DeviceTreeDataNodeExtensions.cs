using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Extensions;

public static class DeviceTreeDataNodeExtensions
{
    // What a configuration looks like the moment a data point is discovered. Kept here as factories rather than
    // inline in AddConfigurations, because resetting has to arrive at exactly the same values - two copies of
    // these numbers would drift apart the first time one of them is adjusted.

    /// <summary>
    /// A fresh compressor configuration for <paramref name="connection"/>.
    /// </summary>
    /// <remarks>
    /// Anna gets a ten-second window with min/max/avg, everything else a minute and the last value - the two
    /// clouds consume the data differently enough that one default would be wrong for one of them.
    /// </remarks>
    public static CompressorConfiguration CreateCompressorConfiguration(Connection connection)
    {
        var isAnna = AnnaCloudFilter.IsAnnaConnection(connection);

        return new CompressorConfiguration
        {
            Aggregation = isAnna ? AggregationFunction.MinMaxAvg : AggregationFunction.Last,
            CompressionTime = isAnna ? (int)AggregationInterval.SecondsTen : (int)AggregationInterval.MinutesOne,
            DataGroupIdentifier = connection.Id,
            Enabled = false,
        };
    }

    /// <summary>
    /// A fresh scheduler configuration for <paramref name="connection"/>: off, and set to run once at midnight
    /// on Monday, so enabling it has a schedule to enable rather than nothing.
    /// </summary>
    public static SchedulerConfiguration CreateSchedulerConfiguration(Connection connection)
    {
        var configuration = new SchedulerConfiguration
        {
            DataGroupIdentifier = connection.Id,
            Enabled = false,
        };

        configuration.Times[DayOfWeek.Monday] = [TimeSpan.FromSeconds(0)];

        return configuration;
    }

    /// <summary>
    /// A fresh event trigger for <paramref name="connection"/>: off, one hour of delay, no condition set.
    /// </summary>
    public static EventTrigger CreateEventTrigger(Connection connection)
        => new()
        {
            DataGroupIdentifier = connection.Id,
            Delay = 1,
            Enabled = false,
            OnDamage = false,
            OnWarning = false,
        };

    /// <summary>
    /// Fresh raw-data settings: ten seconds at 100 kHz.
    /// </summary>
    public static RawDataSettings CreateRawDataSettings()
        => new() { Duration = 10000, Frequency = 100000, };

    public static void AddConfigurations(this IDeviceTreeDataNode dataNode, IReadOnlyCollection<Connection> connections)
    {
        if (dataNode is IDeviceTreeCompressableDataNode compressableDataNode)
        {
            var configsToAdd = connections.Where(cfg => !compressableDataNode.CompressorConfigurations.Any(con => con.DataGroupIdentifier == cfg.Id));

            foreach (var config in configsToAdd)
                compressableDataNode.CompressorConfigurations.Add(CreateCompressorConfiguration(config));
        }
        else if (dataNode is IDeviceTreeSchedulableDataNode schedulableDataNode)
        {
            var configsToAdd = connections.Where(cfg => !schedulableDataNode.SchedulerConfigurations.Any(con => con.DataGroupIdentifier == cfg.Id));

            foreach (var config in configsToAdd)
                schedulableDataNode.SchedulerConfigurations.Add(CreateSchedulerConfiguration(config));
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

                    sensor.Triggers.Add(CreateEventTrigger(config));
                }
            }
        }

        if (dataNode is IDeviceTreeConfigurableRawDataNode configurableRawDataNode)
        {
            var configsToAdd = connections.Where(cfg => !configurableRawDataNode.RawDataConfigurations.Any(con => con.Key == cfg.Id));

            foreach (var config in configsToAdd)
                configurableRawDataNode.RawDataConfigurations.Add(config.Id, CreateRawDataSettings());
        }
    }

    /// <summary>
    /// Puts every configuration this node holds for <paramref name="connections"/> back to the values it would
    /// have been created with.
    /// </summary>
    /// <remarks>
    /// The existing objects are written rather than replaced: the grid's cells and the dataflow generators hold
    /// on to them, and swapping the instances out would leave those pointing at configurations no longer in the
    /// tree. Configurations for other connections are left alone, so resetting one cloud does not touch another.
    /// </remarks>
    /// <returns><see langword="true"/> if anything was written.</returns>
    public static bool ResetConfigurations(this IDeviceTreeDataNode dataNode, IReadOnlyCollection<Connection> connections)
    {
        var byId = connections.ToDictionary(connection => connection.Id);
        var changed = false;

        if (dataNode is IDeviceTreeCompressableDataNode compressableDataNode)
        {
            foreach (var configuration in compressableDataNode.CompressorConfigurations)
            {
                if (!byId.TryGetValue(configuration.DataGroupIdentifier, out var connection))
                    continue;

                var fresh = CreateCompressorConfiguration(connection);

                configuration.Aggregation = fresh.Aggregation;
                configuration.CompressionTime = fresh.CompressionTime;
                configuration.Enabled = fresh.Enabled;
                changed = true;
            }
        }
        else if (dataNode is IDeviceTreeSchedulableDataNode schedulableDataNode)
        {
            foreach (var configuration in schedulableDataNode.SchedulerConfigurations)
            {
                if (!byId.TryGetValue(configuration.DataGroupIdentifier, out var connection))
                    continue;

                var fresh = CreateSchedulerConfiguration(connection);

                configuration.Enabled = fresh.Enabled;
                configuration.Times.Clear();

                foreach (var (day, times) in fresh.Times)
                    configuration.Times[day] = times;

                changed = true;
            }
        }

        if (dataNode is IDeviceTreeEventTriggerDataNode triggerDataNode)
        {
            foreach (var sensor in triggerDataNode.EventTriggerConfigurations)
            {
                foreach (var trigger in sensor.Triggers)
                {
                    if (!byId.TryGetValue(trigger.DataGroupIdentifier, out var connection))
                        continue;

                    var fresh = CreateEventTrigger(connection);

                    trigger.Delay = fresh.Delay;
                    trigger.Enabled = fresh.Enabled;
                    trigger.OnDamage = fresh.OnDamage;
                    trigger.OnWarning = fresh.OnWarning;
                    changed = true;
                }
            }
        }

        if (dataNode is IDeviceTreeConfigurableRawDataNode configurableRawDataNode)
        {
            foreach (var (id, settings) in configurableRawDataNode.RawDataConfigurations)
            {
                if (!byId.ContainsKey(id))
                    continue;

                var fresh = CreateRawDataSettings();

                settings.Duration = fresh.Duration;
                settings.Frequency = fresh.Frequency;
                changed = true;
            }
        }

        return changed;
    }

    public static void RemoveConfigurations(this IDeviceTreeDataNode dataNode, IReadOnlySet<Guid> existingConfigurations)
    {
        if (dataNode is IDeviceTreeCompressableDataNode compressableDataNode)
            compressableDataNode.CompressorConfigurations.RemoveAll(con => !existingConfigurations.Contains(con.DataGroupIdentifier));
        else if (dataNode is IDeviceTreeSchedulableDataNode schedulableDataNode)
            schedulableDataNode.SchedulerConfigurations.RemoveAll(con => !existingConfigurations.Contains(con.DataGroupIdentifier));

        if (dataNode is IDeviceTreeEventTriggerDataNode triggerDataNode)
        {
            foreach (var sensor in triggerDataNode.EventTriggerConfigurations)
            {
                sensor.Triggers.RemoveAll(t => !existingConfigurations.Contains(t.DataGroupIdentifier));
            }
        }
    }
}
