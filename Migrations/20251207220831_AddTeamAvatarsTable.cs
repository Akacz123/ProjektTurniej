using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EsportsTournament.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamAvatarsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Teams");

            migrationBuilder.AlterColumn<string>(
                name: "TeamName",
                table: "Teams",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Teams",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamAvatarId",
                table: "Teams",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeamAvatars",
                columns: table => new
                {
                    TeamAvatarId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamAvatars", x => x.TeamAvatarId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TeamAvatarId",
                table: "Teams",
                column: "TeamAvatarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_TeamAvatars_TeamAvatarId",
                table: "Teams",
                column: "TeamAvatarId",
                principalTable: "TeamAvatars",
                principalColumn: "TeamAvatarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_TeamAvatars_TeamAvatarId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "TeamAvatars");

            migrationBuilder.DropIndex(
                name: "IX_Teams_TeamAvatarId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TeamAvatarId",
                table: "Teams");

            migrationBuilder.AlterColumn<string>(
                name: "TeamName",
                table: "Teams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Teams",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Teams",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
