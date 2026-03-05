using AwesomeAssertions;
using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Backend.Extensions;
using DataCollectionWizard.Backend.Factories;
using DataCollectionWizard.Internal.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Testing.Backend;

namespace DataCollectionWizard.Backend.Tests.DbContext;

public class DataCollectionWizardDbContextTests : TestWithDbContextSqlite<DataCollectionWizardAttributeDbContextSqlite>
{
    public class DeviceTreeDbSet : DataCollectionWizardDbContextTests
    {
        private static DeviceTreeDbModel CreateDbModel(string deviceAddress)
            => new()
            {
                DeviceAddress = deviceAddress,
                DeviceTreeJson = FakeDeviceTreeFactory.CreateDeviceTree().SerializeToJson()
            };

        [Fact]
        public void Added_item_should_get_saved()
        {
            // Arrange
            using var dbContext = new ServiceCollection()
                .AddScoped(_ => (IDataCollectionWizardDbContext)TestDbContext)
                .BuildServiceProvider()
                .GetRequiredService<IDataCollectionWizardDbContext>();

            var dbModel = CreateDbModel("Address");

            // Act
            dbContext.Devices.Add(dbModel);

            // Assert
            dbContext.SaveChanges().Should().Be(1);
        }

        [Fact]
        public void Remove_existing_item_should_get_saved()
        {
            // Arrange
            using var dbContext = new ServiceCollection()
                .AddScoped(_ => (IDataCollectionWizardDbContext)TestDbContext)
                .BuildServiceProvider()
                .GetRequiredService<IDataCollectionWizardDbContext>();

            var dbModel = CreateDbModel("Address");
            dbContext.Devices.Add(dbModel);
            dbContext.SaveChanges();

            // Act
            dbContext.Devices.Remove(dbModel);

            // Assert
            dbContext.SaveChanges().Should().Be(1);
        }

        [Fact]
        public async Task Query_existing_should_return_item()
        {
            // Arrange
            using var dbContext = new ServiceCollection()
                .AddScoped(_ => (IDataCollectionWizardDbContext)TestDbContext)
                .BuildServiceProvider()
                .GetRequiredService<IDataCollectionWizardDbContext>();

            var dbModel = CreateDbModel("Address");
            dbContext.Devices.Add(dbModel);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act
            var entity = await dbContext.Devices.FirstOrDefaultAsync(k => k.DeviceAddress == dbModel.DeviceAddress, TestContext.Current.CancellationToken);

            // Assert
            entity.Should().NotBeNull();
        }
    }

    public class DeviceConnectorIdsDbSet : DataCollectionWizardDbContextTests
    {
        private static DeviceConnectorIds CreateDbModel(string deviceAddress)
            => new()
            {
                DeviceAddress = deviceAddress,
                DeviceTreeOutput = Guid.NewGuid(),
                TriggerInput = Guid.NewGuid(),
            };

        [Fact]
        public void Added_item_should_get_saved()
        {
            // Arrange
            using var dbContext = new ServiceCollection()
                .AddScoped(_ => (IDataCollectionWizardDbContext)TestDbContext)
                .BuildServiceProvider()
                .GetRequiredService<IDataCollectionWizardDbContext>();

            var dbModel = CreateDbModel("Address");

            // Act
            dbContext.DeviceConnectorIds.Add(dbModel);

            // Assert
            dbContext.SaveChanges().Should().Be(1);
        }


        [Fact]
        public void Remove_existing_item_should_get_saved()
        {
            // Arrange
            using var dbContext = new ServiceCollection()
                .AddScoped(_ => (IDataCollectionWizardDbContext)TestDbContext)
                .BuildServiceProvider()
                .GetRequiredService<IDataCollectionWizardDbContext>();

            var dbModel = CreateDbModel("Address");
            dbContext.DeviceConnectorIds.Add(dbModel);
            dbContext.SaveChanges();

            // Act
            dbContext.DeviceConnectorIds.Remove(dbModel);

            // Assert
            dbContext.SaveChanges().Should().Be(1);
        }

        [Fact]
        public async Task Query_existing_should_return_item()
        {
            // Arrange
            using var dbContext = new ServiceCollection()
                .AddScoped(_ => (IDataCollectionWizardDbContext)TestDbContext)
                .BuildServiceProvider()
                .GetRequiredService<IDataCollectionWizardDbContext>();

            var dbModel = CreateDbModel("Address");
            dbContext.DeviceConnectorIds.Add(dbModel);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act
            var entity = await dbContext.DeviceConnectorIds
                .FirstOrDefaultAsync(
                    k => k.DeviceAddress == dbModel.DeviceAddress && k.DeviceTreeOutput == dbModel.DeviceTreeOutput && k.TriggerInput == dbModel.TriggerInput,
                    TestContext.Current.CancellationToken
                );

            // Assert
            entity.Should().NotBeNull();
        }
    }
}
