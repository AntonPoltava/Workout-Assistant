using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreatedAtFromFavoriteExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "FavoriteExercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FavoriteExercises",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "FavoriteExercises",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "FavoriteExercises",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "FavoriteExercises");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "FavoriteExercises");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "FavoriteExercises");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "FavoriteExercises");
        }
    }
}
