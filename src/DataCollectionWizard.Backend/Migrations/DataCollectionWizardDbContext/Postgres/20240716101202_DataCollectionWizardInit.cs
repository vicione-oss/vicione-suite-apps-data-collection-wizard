using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataCollectionWizard.Backend.Migrations.DataCollectionWizardDbContext.Postgres;

/// <inheritdoc />
public partial class DataCollectionWizardInit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "data-collection-wizard");

        migrationBuilder.CreateTable(
            name: "DeviceConnectorIds",
            schema: "data-collection-wizard",
            columns: table => new
            {
                DeviceAddress = table.Column<string>(type: "text", nullable: false),
                DeviceTreeOutput = table.Column<Guid>(type: "uuid", nullable: false),
                TriggerInput = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeviceConnectorIds", x => new { x.DeviceAddress, x.TriggerInput, x.DeviceTreeOutput });
            });

        migrationBuilder.CreateTable(
            name: "Devices",
            schema: "data-collection-wizard",
            columns: table => new
            {
                DeviceAddress = table.Column<string>(type: "text", nullable: false),
                DeviceTreeJson = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Devices", x => x.DeviceAddress);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DeviceConnectorIds",
            schema: "data-collection-wizard");

        migrationBuilder.DropTable(
            name: "Devices",
            schema: "data-collection-wizard");
    }
}
