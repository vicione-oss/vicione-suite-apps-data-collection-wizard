using DataCollectionWizard.Internal.Extensions;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Tests;

public class SchedulerConfigurationExtensionTests
{
    public class ToFbSetting
    {
        [Fact]
        public void ReturnsCorrectString()
        {
            var config = new SchedulerConfiguration
            {
                DataGroupIdentifier = Guid.NewGuid(),
                Enabled = true,
                Times = [],
            };

            config.Times.Add(DayOfWeek.Monday, [new(5, 3, 0), new(12, 0, 10)]);
            config.Times.Add(DayOfWeek.Wednesday, [new(8, 0, 0), new(18, 15, 0)]);

            Assert.Equal("1#05:03;1#12:00;3#08:00;3#18:15", config.ToFbSetting());
        }
    }
}
