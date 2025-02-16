using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MackMultiBot.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LobbyConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: true),
                    TeamMode = table.Column<int>(type: "INTEGER", nullable: true),
                    ScoreMode = table.Column<int>(type: "INTEGER", nullable: true),
                    Mods = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<int>(type: "INTEGER", nullable: true),
                    Password = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LobbyInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LobbyConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Channel = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoSkip = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LobbyConfigurations");

            migrationBuilder.DropTable(
                name: "LobbyInstances");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
