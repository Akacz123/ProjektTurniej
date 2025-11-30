using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EsportsTournament.API.Migrations
{
    /// <inheritdoc />
    public partial class FixRegistrationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentRegistrationsIndividual_Tournaments_TournamentId",
                table: "TournamentRegistrationsIndividual");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentRegistrationsIndividual_Users_UserId",
                table: "TournamentRegistrationsIndividual");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TournamentRegistrationsIndividual",
                table: "TournamentRegistrationsIndividual");

            migrationBuilder.RenameTable(
                name: "TournamentRegistrationsIndividual",
                newName: "tournament_registrations_individual");

            migrationBuilder.RenameIndex(
                name: "IX_TournamentRegistrationsIndividual_UserId",
                table: "tournament_registrations_individual",
                newName: "IX_tournament_registrations_individual_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TournamentRegistrationsIndividual_TournamentId_UserId",
                table: "tournament_registrations_individual",
                newName: "IX_tournament_registrations_individual_TournamentId_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tournament_registrations_individual",
                table: "tournament_registrations_individual",
                column: "RegistrationId");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedId = table.Column<int>(type: "integer", nullable: true),
                    RelatedType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStatistics",
                columns: table => new
                {
                    StatId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    MatchesWon = table.Column<int>(type: "integer", nullable: false),
                    MatchesLost = table.Column<int>(type: "integer", nullable: false),
                    TournamentsParticipated = table.Column<int>(type: "integer", nullable: false),
                    TournamentsWon = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStatistics", x => x.StatId);
                    table.ForeignKey(
                        name: "FK_PlayerStatistics_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "GameId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerStatistics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamStatistics",
                columns: table => new
                {
                    StatId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    MatchesWon = table.Column<int>(type: "integer", nullable: false),
                    MatchesLost = table.Column<int>(type: "integer", nullable: false),
                    TournamentsParticipated = table.Column<int>(type: "integer", nullable: false),
                    TournamentsWon = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamStatistics", x => x.StatId);
                    table.ForeignKey(
                        name: "FK_TeamStatistics_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "GameId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamStatistics_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReporterId = table.Column<int>(type: "integer", nullable: false),
                    ReportedUserId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MatchId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_UserReports_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "MatchId");
                    table.ForeignKey(
                        name: "FK_UserReports_Users_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserReports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_GameId",
                table: "PlayerStatistics",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_UserId_GameId",
                table: "PlayerStatistics",
                columns: new[] { "UserId", "GameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamStatistics_GameId",
                table: "TeamStatistics",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStatistics_TeamId_GameId",
                table: "TeamStatistics",
                columns: new[] { "TeamId", "GameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_MatchId",
                table: "UserReports",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReportedUserId",
                table: "UserReports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReporterId",
                table: "UserReports",
                column: "ReporterId");

            migrationBuilder.AddForeignKey(
                name: "FK_tournament_registrations_individual_Tournaments_TournamentId",
                table: "tournament_registrations_individual",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "TournamentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tournament_registrations_individual_Users_UserId",
                table: "tournament_registrations_individual",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tournament_registrations_individual_Tournaments_TournamentId",
                table: "tournament_registrations_individual");

            migrationBuilder.DropForeignKey(
                name: "FK_tournament_registrations_individual_Users_UserId",
                table: "tournament_registrations_individual");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PlayerStatistics");

            migrationBuilder.DropTable(
                name: "TeamStatistics");

            migrationBuilder.DropTable(
                name: "UserReports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tournament_registrations_individual",
                table: "tournament_registrations_individual");

            migrationBuilder.RenameTable(
                name: "tournament_registrations_individual",
                newName: "TournamentRegistrationsIndividual");

            migrationBuilder.RenameIndex(
                name: "IX_tournament_registrations_individual_UserId",
                table: "TournamentRegistrationsIndividual",
                newName: "IX_TournamentRegistrationsIndividual_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_tournament_registrations_individual_TournamentId_UserId",
                table: "TournamentRegistrationsIndividual",
                newName: "IX_TournamentRegistrationsIndividual_TournamentId_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TournamentRegistrationsIndividual",
                table: "TournamentRegistrationsIndividual",
                column: "RegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentRegistrationsIndividual_Tournaments_TournamentId",
                table: "TournamentRegistrationsIndividual",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "TournamentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentRegistrationsIndividual_Users_UserId",
                table: "TournamentRegistrationsIndividual",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
