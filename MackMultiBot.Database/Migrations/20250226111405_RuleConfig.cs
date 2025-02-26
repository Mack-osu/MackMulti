using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MackMultiBot.Database.Migrations
{
    /// <inheritdoc />
    public partial class RuleConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LobbyRuleConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LobbyConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    LimitDifficulty = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumDifficulty = table.Column<float>(type: "REAL", nullable: false),
                    MaximumDifficulty = table.Column<float>(type: "REAL", nullable: false),
                    DifficultyMargin = table.Column<float>(type: "REAL", nullable: true),
                    LimitMapLength = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumMapLength = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumMapLength = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyRuleConfigurations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LobbyRuleConfigurations");
        }
    }
}
