using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataCollectionWizard.Backend.Migrations.DataCollectionWizardDbContext.Sqlite;

/// <inheritdoc />
public partial class DataCollectionWizardAddOutputMapping : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "data-collection-wizard");

        migrationBuilder.CreateTable(
            name: "ValueMappings",
            schema: "data-collection-wizard",
            columns: table => new
            {
                ProcessDataId = table.Column<string>(type: "TEXT", nullable: false),
                UnitOutputId = table.Column<Guid>(type: "TEXT", nullable: true),
                ValueOutputId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ValueMappings", x => x.ProcessDataId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(
            name: "ValueMappings",
            schema: "data-collection-wizard");
}
