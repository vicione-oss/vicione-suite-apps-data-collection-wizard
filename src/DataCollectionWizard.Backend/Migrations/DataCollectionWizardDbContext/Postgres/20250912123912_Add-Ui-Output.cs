using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataCollectionWizard.Backend.Migrations.DataCollectionWizardDbContext.Postgres
{
    /// <inheritdoc />
    public partial class AddUiOutput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ValueOutputId",
                schema: "data-collection-wizard",
                table: "ValueMappings",
                newName: "ValueOutputIdUI");

            migrationBuilder.AddColumn<Guid>(
                name: "ValueOutputIdLogging",
                schema: "data-collection-wizard",
                table: "ValueMappings",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueOutputIdLogging",
                schema: "data-collection-wizard",
                table: "ValueMappings");

            migrationBuilder.RenameColumn(
                name: "ValueOutputIdUI",
                schema: "data-collection-wizard",
                table: "ValueMappings",
                newName: "ValueOutputId");
        }
    }
}
