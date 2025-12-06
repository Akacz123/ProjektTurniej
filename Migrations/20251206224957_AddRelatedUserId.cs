using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EsportsTournament.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RelatedUserId",
                table: "Notifications",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelatedUserId",
                table: "Notifications");
        }
    }
}
