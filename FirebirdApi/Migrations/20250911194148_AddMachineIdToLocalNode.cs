using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirebirdApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineIdToLocalNode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "machine_id",
                table: "local_nodes",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_local_nodes_machine_id",
                table: "local_nodes",
                column: "machine_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_local_nodes_machine_id",
                table: "local_nodes");

            migrationBuilder.DropColumn(
                name: "machine_id",
                table: "local_nodes");
        }
    }
}
