using DataCollectionWizard.Backend.DbContext;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Testing.Backend;

namespace DataCollectionWizard.Backend.Tests;

public class DataCollectionWizardBackendModuleTests
{
    [Fact]
    public void Init_module_should_register_and_configure_services()
    {
        // Arrange
        var module = new DataCollectionWizardBackendModule();

        // Act
        var serviceProvider = module.TestModuleInitialization();

        // Assert
        Assert.NotNull(module.ModuleInitializer);
        Assert.NotNull(serviceProvider);
        Assert.NotNull(serviceProvider.GetService<IDataCollectionWizardDbContext>());
    }
}
