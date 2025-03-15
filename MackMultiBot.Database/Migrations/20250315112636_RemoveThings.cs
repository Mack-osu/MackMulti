using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MackMultiBot.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveThings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LobbyRuleConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LobbyRuleConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DifficultyMargin = table.Column<float>(type: "REAL", nullable: true),
                    LimitDifficulty = table.Column<bool>(type: "INTEGER", nullable: false),
                    LimitMapLength = table.Column<bool>(type: "INTEGER", nullable: false),
                    LobbyIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    MaximumDifficulty = table.Column<float>(type: "REAL", nullable: false),
                    MaximumMapLength = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumDifficulty = table.Column<float>(type: "REAL", nullable: false),
                    MinimumMapLength = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyRuleConfigurations", x => x.Id);
                });
        }
    }
}
