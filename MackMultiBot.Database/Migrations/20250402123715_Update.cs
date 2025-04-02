using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MackMultiBot.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "LobbyInstances");

            migrationBuilder.DropColumn(
                name: "LobbyIdentifier",
                table: "LobbyBehaviorData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "LobbyInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LobbyIdentifier",
                table: "LobbyBehaviorData",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
