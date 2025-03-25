using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenisDeMasa.Worker.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLikesViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Likes",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Views",
                table: "Tournaments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Likes",
                table: "Tournaments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Views",
                table: "Tournaments",
                type: "INTEGER",
                nullable: true);
        }
    }
}
