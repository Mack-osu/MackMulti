using System;
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
                name: "LobbyBehaviorData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LobbyIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    BehaviorName = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyBehaviorData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LobbyInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Identifier = table.Column<string>(type: "TEXT", nullable: false),
                    Channel = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayedMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BeatmapId = table.Column<long>(type: "INTEGER", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayedMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoSkip = table.Column<bool>(type: "INTEGER", nullable: false),
                    Playtime = table.Column<int>(type: "INTEGER", nullable: false),
                    Playcount = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchWins = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OsuScoreId = table.Column<long>(type: "INTEGER", nullable: true),
                    BeatmapId = table.Column<long>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MapId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalScore = table.Column<long>(type: "INTEGER", nullable: false),
                    MaxCombo = table.Column<int>(type: "INTEGER", nullable: false),
                    Count300 = table.Column<int>(type: "INTEGER", nullable: false),
                    Count100 = table.Column<int>(type: "INTEGER", nullable: false),
                    Count50 = table.Column<int>(type: "INTEGER", nullable: false),
                    CountMiss = table.Column<int>(type: "INTEGER", nullable: false),
                    Mods = table.Column<int>(type: "INTEGER", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scores_PlayedMaps_MapId",
                        column: x => x.MapId,
                        principalTable: "PlayedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scores_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scores_MapId",
                table: "Scores",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_UserId",
                table: "Scores",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LobbyBehaviorData");

            migrationBuilder.DropTable(
                name: "LobbyInstances");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "PlayedMaps");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
