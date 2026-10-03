using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieRaterApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTvSeasonSeasonNumberColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TvSeason_SeasonNumber",
                table: "Media",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TvSeason_SeasonNumber",
                table: "Media");
        }
    }
}
