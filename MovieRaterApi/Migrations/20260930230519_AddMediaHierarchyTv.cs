using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieRaterApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaHierarchyTv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WatchSessions_Movies_MovieId",
                table: "WatchSessions");

            // MovieGenres -> MediaGenres (rename, data preserved)
            migrationBuilder.DropForeignKey(
                name: "FK_MovieGenres_Genres_GenreId",
                table: "MovieGenres");
            migrationBuilder.DropForeignKey(
                name: "FK_MovieGenres_Movies_MovieId",
                table: "MovieGenres");
            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieGenres",
                table: "MovieGenres");
            migrationBuilder.DropIndex(
                name: "IX_MovieGenres_GenreId",
                table: "MovieGenres");
            migrationBuilder.RenameTable(
                name: "MovieGenres",
                newName: "MediaGenres");
            migrationBuilder.RenameColumn(
                name: "MovieId",
                table: "MediaGenres",
                newName: "MediaId");

            // UserMovies -> UserMedias (rename, data preserved)
            migrationBuilder.DropForeignKey(
                name: "FK_UserMovies_Movies_MovieId",
                table: "UserMovies");
            migrationBuilder.DropForeignKey(
                name: "FK_UserMovies_Users_UserId",
                table: "UserMovies");
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserMovies",
                table: "UserMovies");
            migrationBuilder.DropIndex(
                name: "IX_UserMovies_MovieId",
                table: "UserMovies");
            migrationBuilder.RenameTable(
                name: "UserMovies",
                newName: "UserMedias");
            migrationBuilder.RenameColumn(
                name: "MovieId",
                table: "UserMedias",
                newName: "MediaId");

            // MovieListMovies -> MediaListMedias (rename, data preserved)
            migrationBuilder.DropForeignKey(
                name: "FK_MovieListMovies_MovieLists_MovieListId",
                table: "MovieListMovies");
            migrationBuilder.DropForeignKey(
                name: "FK_MovieListMovies_Movies_MovieId",
                table: "MovieListMovies");
            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieListMovies",
                table: "MovieListMovies");
            migrationBuilder.DropIndex(
                name: "IX_MovieListMovies_MovieId",
                table: "MovieListMovies");
            migrationBuilder.RenameTable(
                name: "MovieListMovies",
                newName: "MediaListMedias");
            migrationBuilder.RenameColumn(
                name: "MovieId",
                table: "MediaListMedias",
                newName: "MediaId");
            migrationBuilder.RenameColumn(
                name: "MovieListId",
                table: "MediaListMedias",
                newName: "MediaListId");

            // Movies -> Media (rename, data preserved)
            migrationBuilder.DropPrimaryKey(
                name: "PK_Movies",
                table: "Movies");
            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies");
            migrationBuilder.RenameTable(
                name: "Movies",
                newName: "Media");

            migrationBuilder.RenameColumn(
                name: "MovieId",
                table: "WatchSessions",
                newName: "MediaId");
            migrationBuilder.RenameIndex(
                name: "IX_WatchSessions_MovieId",
                table: "WatchSessions",
                newName: "IX_WatchSessions_MediaId");

            migrationBuilder.AddColumn<int>(
                name: "EpisodeNumber",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastAirDate",
                table: "Media",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "Media",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfEpisodes",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfSeasons",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SeasonId",
                table: "Media",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonNumber",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SeriesId",
                table: "Media",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Media",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Media",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Media",
                table: "Media",
                column: "Id");

            // Media hierarchy self-referencing FKs (series -> seasons -> episodes)
            migrationBuilder.CreateIndex(
                name: "IX_Media_MediaType_TmdbId",
                table: "Media",
                columns: new[] { "MediaType", "TmdbId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_SeasonId",
                table: "Media",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_SeriesId",
                table: "Media",
                column: "SeriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Media_SeasonId",
                table: "Media",
                column: "SeasonId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Media_SeriesId",
                table: "Media",
                column: "SeriesId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Rebuild renamed join-table constraints
            migrationBuilder.AddPrimaryKey(
                name: "PK_MediaGenres",
                table: "MediaGenres",
                columns: new[] { "MediaId", "GenreId" });
            migrationBuilder.CreateIndex(
                name: "IX_MediaGenres_GenreId",
                table: "MediaGenres",
                column: "GenreId");
            migrationBuilder.AddForeignKey(
                name: "FK_MediaGenres_Genres_GenreId",
                table: "MediaGenres",
                column: "GenreId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_MediaGenres_Media_MediaId",
                table: "MediaGenres",
                column: "MediaId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MediaListMedias",
                table: "MediaListMedias",
                columns: new[] { "MediaListId", "MediaId" });
            migrationBuilder.CreateIndex(
                name: "IX_MediaListMedias_MediaId",
                table: "MediaListMedias",
                column: "MediaId");
            migrationBuilder.AddForeignKey(
                name: "FK_MediaListMedias_MovieLists_MediaListId",
                table: "MediaListMedias",
                column: "MediaListId",
                principalTable: "MovieLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_MediaListMedias_Media_MediaId",
                table: "MediaListMedias",
                column: "MediaId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserMedias",
                table: "UserMedias",
                columns: new[] { "UserId", "MediaId" });
            migrationBuilder.CreateIndex(
                name: "IX_UserMedias_MediaId",
                table: "UserMedias",
                column: "MediaId");
            migrationBuilder.AddForeignKey(
                name: "FK_UserMedias_Users_UserId",
                table: "UserMedias",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_UserMedias_Media_MediaId",
                table: "UserMedias",
                column: "MediaId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WatchSessions_Media_MediaId",
                table: "WatchSessions",
                column: "MediaId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_Media_SeasonId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Media_SeriesId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchSessions_Media_MediaId",
                table: "WatchSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaGenres_Genres_GenreId",
                table: "MediaGenres");
            migrationBuilder.DropForeignKey(
                name: "FK_MediaGenres_Media_MediaId",
                table: "MediaGenres");
            migrationBuilder.DropPrimaryKey(
                name: "PK_MediaGenres",
                table: "MediaGenres");
            migrationBuilder.DropIndex(
                name: "IX_MediaGenres_GenreId",
                table: "MediaGenres");
            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "MediaGenres",
                newName: "MovieId");
            migrationBuilder.RenameTable(
                name: "MediaGenres",
                newName: "MovieGenres");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaListMedias_MovieLists_MediaListId",
                table: "MediaListMedias");
            migrationBuilder.DropForeignKey(
                name: "FK_MediaListMedias_Media_MediaId",
                table: "MediaListMedias");
            migrationBuilder.DropPrimaryKey(
                name: "PK_MediaListMedias",
                table: "MediaListMedias");
            migrationBuilder.DropIndex(
                name: "IX_MediaListMedias_MediaId",
                table: "MediaListMedias");
            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "MediaListMedias",
                newName: "MovieId");
            migrationBuilder.RenameColumn(
                name: "MediaListId",
                table: "MediaListMedias",
                newName: "MovieListId");
            migrationBuilder.RenameTable(
                name: "MediaListMedias",
                newName: "MovieListMovies");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMedias_Users_UserId",
                table: "UserMedias");
            migrationBuilder.DropForeignKey(
                name: "FK_UserMedias_Media_MediaId",
                table: "UserMedias");
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserMedias",
                table: "UserMedias");
            migrationBuilder.DropIndex(
                name: "IX_UserMedias_MediaId",
                table: "UserMedias");
            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "UserMedias",
                newName: "MovieId");
            migrationBuilder.RenameTable(
                name: "UserMedias",
                newName: "UserMovies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Media",
                table: "Media");
            migrationBuilder.DropIndex(
                name: "IX_Media_MediaType_TmdbId",
                table: "Media");
            migrationBuilder.DropIndex(
                name: "IX_Media_SeasonId",
                table: "Media");
            migrationBuilder.DropIndex(
                name: "IX_Media_SeriesId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "EpisodeNumber",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "LastAirDate",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "NumberOfEpisodes",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "NumberOfSeasons",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "SeasonNumber",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Media");
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Media");

            migrationBuilder.RenameTable(
                name: "Media",
                newName: "Movies");
            migrationBuilder.AddPrimaryKey(
                name: "PK_Movies",
                table: "Movies",
                column: "Id");
            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId",
                unique: true);

            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "WatchSessions",
                newName: "MovieId");
            migrationBuilder.RenameIndex(
                name: "IX_WatchSessions_MediaId",
                table: "WatchSessions",
                newName: "IX_WatchSessions_MovieId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieGenres",
                table: "MovieGenres",
                columns: new[] { "MovieId", "GenreId" });
            migrationBuilder.CreateIndex(
                name: "IX_MovieGenres_GenreId",
                table: "MovieGenres",
                column: "GenreId");
            migrationBuilder.AddForeignKey(
                name: "FK_MovieGenres_Genres_GenreId",
                table: "MovieGenres",
                column: "GenreId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_MovieGenres_Movies_MovieId",
                table: "MovieGenres",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieListMovies",
                table: "MovieListMovies",
                columns: new[] { "MovieListId", "MovieId" });
            migrationBuilder.CreateIndex(
                name: "IX_MovieListMovies_MovieId",
                table: "MovieListMovies",
                column: "MovieId");
            migrationBuilder.AddForeignKey(
                name: "FK_MovieListMovies_MovieLists_MovieListId",
                table: "MovieListMovies",
                column: "MovieListId",
                principalTable: "MovieLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_MovieListMovies_Movies_MovieId",
                table: "MovieListMovies",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserMovies",
                table: "UserMovies",
                columns: new[] { "UserId", "MovieId" });
            migrationBuilder.CreateIndex(
                name: "IX_UserMovies_MovieId",
                table: "UserMovies",
                column: "MovieId");
            migrationBuilder.AddForeignKey(
                name: "FK_UserMovies_Movies_MovieId",
                table: "UserMovies",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_UserMovies_Users_UserId",
                table: "UserMovies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WatchSessions_Movies_MovieId",
                table: "WatchSessions",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}