using DataCollectionWizard.Internal.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sdk.Backend.Persistence;

namespace DataCollectionWizard.Backend.DbContext;

public class DataCollectionWizardAttributeDbContext : ModuleDbContext, IDataCollectionWizardDbContext
{
    internal const string DbSchemaName = "data-collection-wizard";

    public override string DefaultSchemaName => DbSchemaName;

    public DbSet<DeviceConnectorIds> DeviceConnectorIds { get; set; } = null!;
    public DbSet<DeviceTreeDbModel> Devices { get; set; } = null!;
    public DbSet<ValueMappingEntry> ValueMappings { get; set; } = null!;

    internal DataCollectionWizardAttributeDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceTreeDbModel>()
            .HasKey(t => t.DeviceAddress);

        modelBuilder.Entity<DeviceConnectorIds>()
            .HasKey(c => new { c.DeviceAddress, c.TriggerInput, c.DeviceTreeOutput });

        modelBuilder.Entity<ValueMappingEntry>()
            .HasKey(c => new { c.ProcessDataId });
    }
}

public sealed class DataCollectionWizardAttributeDbContextSqlite(DbContextOptions<DataCollectionWizardAttributeDbContextSqlite> options) : DataCollectionWizardAttributeDbContext(options), ISqliteDbContext
{
}

public sealed class DataCollectionWizardAttributeDbContextPostgres(DbContextOptions<DataCollectionWizardAttributeDbContextPostgres> options) : DataCollectionWizardAttributeDbContext(options), IPostgresDbContext
{
}

/// <summary>
/// needed for DbContext migration
/// </summary>
public sealed class DataCollectionWizardAttributeDbContextSqliteFactory : IDesignTimeDbContextFactory<DataCollectionWizardAttributeDbContextSqlite>
{
    public DataCollectionWizardAttributeDbContextSqlite CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataCollectionWizardAttributeDbContextSqlite>();
        optionsBuilder.UseSqlite();
        return new DataCollectionWizardAttributeDbContextSqlite(optionsBuilder.Options);
    }
}

/// <summary>
/// needed for DbContext migration
/// </summary>
public sealed class DataCollectionWizardAttributeDbContextPostgresFactory : IDesignTimeDbContextFactory<DataCollectionWizardAttributeDbContextPostgres>
{
    public DataCollectionWizardAttributeDbContextPostgres CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataCollectionWizardAttributeDbContextPostgres>();
        optionsBuilder.UseNpgsql();
        return new DataCollectionWizardAttributeDbContextPostgres(optionsBuilder.Options);
    }
}
