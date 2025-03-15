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
                name: "LobbyId",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "LobbyConfigurationId",
                table: "LobbyRuleConfigurations");

            migrationBuilder.DropColumn(
                name: "LobbyConfigurationId",
                table: "LobbyInstances");

            migrationBuilder.DropColumn(
                name: "LobbyConfigurationId",
                table: "LobbyBehaviorData");

            migrationBuilder.AddColumn<string>(
                name: "LobbyIdentifier",
                table: "LobbyRuleConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "LobbyInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "LobbyConfigurations",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LobbyIdentifier",
                table: "LobbyRuleConfigurations");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "LobbyInstances");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "LobbyConfigurations");

            migrationBuilder.DropColumn(
                name: "LobbyIdentifier",
                table: "LobbyBehaviorData");

            migrationBuilder.AddColumn<int>(
                name: "LobbyId",
                table: "Scores",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LobbyConfigurationId",
                table: "LobbyRuleConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LobbyConfigurationId",
                table: "LobbyInstances",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LobbyConfigurationId",
                table: "LobbyBehaviorData",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
