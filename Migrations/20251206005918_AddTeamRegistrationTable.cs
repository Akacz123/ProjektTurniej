using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EsportsTournament.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamRegistrationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentRegistrationsTeam_Tournaments_TournamentId",
                table: "TournamentRegistrationsTeam");

            migrationBuilder.RenameColumn(
                name: "RegistrationId",
                table: "TournamentRegistrationsTeam",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TournamentRegistrationsTeam",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "TournamentRegistrationsTeam",
                newName: "RegistrationId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TournamentRegistrationsTeam",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentRegistrationsTeam_Tournaments_TournamentId",
                table: "TournamentRegistrationsTeam",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "TournamentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
