using DataCollectionWizard.Internal.Contracts;
using Microsoft.EntityFrameworkCore;
using Sdk.Backend.Persistence;

namespace DataCollectionWizard.Backend.DbContext;

public interface IDataCollectionWizardDbContext : IModuleDbContext
{
    DbSet<DeviceConnectorIds> DeviceConnectorIds { get; set; }
    DbSet<DeviceTreeDbModel> Devices { get; set; }
    DbSet<ValueMappingEntry> ValueMappings { get; set; }
}
