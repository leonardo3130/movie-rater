using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Dashboard.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class DashboardServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<DashboardService>>();
        _sut = new DashboardService(_db, _loggerMock.Object);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsZeroStats_WhenNoWatchSessions()
    {
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var member2Id = Guid.NewGuid();
        var member3Id = Guid.NewGuid();
        SeedUser(userId, "user");
        SeedUser(member2Id, "member2");
        SeedUser(member3Id, "member3");
        SeedGroup(groupId);
        SeedUserGroup(groupId, userId);
        SeedUserGroup(groupId, member2Id);
        SeedUserGroup(groupId, member3Id);

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.MoviesWatched.Should().Be(0);
        result.MoviesThisMonth.Should().Be(0);
        result.MoviesThisYear.Should().Be(0);
        result.AverageRating.Should().Be(0);
        result.FavoriteGenres.Should().BeEmpty();
        result.MostWatchedGenres.Should().BeEmpty();
        result.HighestRatedMovie.Should().BeNull();
        result.LowestRatedMovie.Should().BeNull();
        result.BiggestDisagreement.Should().BeNull();
        result.AverageDisagreement.Should().Be(0);
        result.RewatchCount.Should().Be(0);
        result.CurrentStreak.Should().Be(0);
        result.LongestStreak.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCorrectMoviesWatched()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var movieId = SeedMovie(1, "Movie 1");
        var movie2Id = SeedMovie(2, "Movie 2");
        SeedSession(groupId, movieId, userId, DateTime.UtcNow);
        SeedSession(groupId, movie2Id, userId, DateTime.UtcNow);

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.MoviesWatched.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_FiltersThisMonth_ByDate()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var movieId = SeedMovie(1, "M1");
        var movie2Id = SeedMovie(2, "M2");
        SeedSession(groupId, movieId, userId, DateTime.UtcNow);
        SeedSession(groupId, movie2Id, userId, DateTime.UtcNow.AddMonths(-2));

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.MoviesThisMonth.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_FiltersThisYear_ByDate()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var movieId = SeedMovie(1, "M1");
        var movie2Id = SeedMovie(2, "M2");
        SeedSession(groupId, movieId, userId, DateTime.UtcNow);
        SeedSession(groupId, movie2Id, userId, DateTime.UtcNow.AddYears(-2));

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.MoviesThisYear.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesAverageRating()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        var movieId = SeedMovie(1, "M1");
        var sessionId = SeedSession(groupId, movieId, userId, DateTime.UtcNow);
        SeedRating(sessionId, userId, 8);
        SeedRating(sessionId, partnerId, 6);

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.AverageRating.Should().Be(7.0);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsHighestAndLowestRatedMovie()
    {
        //var (groupId, userId, partnerId) = SeedCoupleWithUsers();
        var ids = SeedGroupWithUsers(3, "member");
        var groupId = ids[0];
        var member1Id = ids[1];
        var member2Id = ids[2];
        var member3Id = ids[3];
        var m1 = SeedMovie(1, "High");
        var m2 = SeedMovie(2, "Mid");
        var m3 = SeedMovie(3, "Low");
        var s1 = SeedSession(groupId, m1, member1Id, DateTime.UtcNow);
        var s3 = SeedSession(groupId, m3, member3Id, DateTime.UtcNow);
        SeedRating(s1, member1Id, 9);
        SeedRating(s1, member2Id, 8);
        SeedRating(s1, member3Id, 7);
        SeedRating(s3, member3Id, 5);
        SeedRating(s3, member1Id, 2);
        SeedRating(s3, member2Id, 2);

        var result = await _sut.GetDashboardAsync(member1Id, groupId);

        result.HighestRatedMovie.Should().NotBeNull();
        result.HighestRatedMovie!.Title.Should().Be("High");
        result.HighestRatedMovie.AverageRating.Should().Be(8.0);

        result.LowestRatedMovie.Should().NotBeNull();
        result.LowestRatedMovie!.Title.Should().Be("Low");
        result.LowestRatedMovie.AverageRating.Should().Be(3.0);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesRewatchCount()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var movieId = SeedMovie(1, "Repeat");
        SeedSession(groupId, movieId, userId, DateTime.UtcNow);
        SeedSession(groupId, movieId, userId, DateTime.UtcNow.AddDays(-7));
        SeedSession(groupId, movieId, userId, DateTime.UtcNow.AddDays(-10));

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.RewatchCount.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesDisagreement()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        var movieId = SeedMovie(1, "Disagree");
        var sessionId = SeedSession(groupId, movieId, userId, DateTime.UtcNow);
        SeedRating(sessionId, userId, 10);
        SeedRating(sessionId, partnerId, 1);

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.BiggestDisagreement.Should().NotBeNull();
        result.BiggestDisagreement!.Title.Should().Be("Disagree");
        result.AverageDisagreement.Should().Be(9.0);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesStreaks()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        var movieId = SeedMovie(1, "S1");
        var movie2Id = SeedMovie(2, "S2");
        var movie3Id = SeedMovie(3, "S3");

        var thisWeek = GetMondayOfWeek(DateTime.UtcNow);
        var lastWeek = thisWeek.AddDays(-7);
        var twoWeeksAgo = thisWeek.AddDays(-14);
        var longAgo = thisWeek.AddDays(-100);

        SeedSession(groupId, movieId, userId, thisWeek);
        SeedSession(groupId, movie2Id, userId, lastWeek);
        SeedSession(groupId, movie3Id, userId, twoWeeksAgo);
        SeedSession(groupId, movieId, userId, longAgo);

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.CurrentStreak.Should().Be(3);
        result.LongestStreak.Should().Be(3);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsSeriesGenres_WhenWatchingEpisode()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var seriesId = SeedSeries(100, "Breaking Bad", "Drama");
        var seasonId = SeedSeason(seriesId, 1);
        var episodeId = SeedEpisode(seasonId, 1, 1);
        SeedSession(groupId, episodeId, userId, DateTime.UtcNow);

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.MostWatchedGenres.Should().ContainSingle(g => g.GenreName == "Drama");
        result.MostWatchedGenres.Single(g => g.GenreName == "Drama").Count.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsSeriesGenres_WhenWatchingSeason()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var seriesId = SeedSeries(101, "The Office", "Comedy");
        var seasonId = SeedSeason(seriesId, 1);
        SeedSession(groupId, seasonId, userId, DateTime.UtcNow);

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.MostWatchedGenres.Should().ContainSingle(g => g.GenreName == "Comedy");
        result.MostWatchedGenres.Single(g => g.GenreName == "Comedy").Count.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsSeriesGenresInFavoriteGenres_WhenRatingEpisode()
    {
        var ids = SeedGroupWithUsers(2, "member");
        var groupId = ids[0];
        var userId = ids[1];
        var partnerId = ids[2];
        var seriesId = SeedSeries(102, "Fargo", "Crime");
        var seasonId = SeedSeason(seriesId, 1);
        var episodeId = SeedEpisode(seasonId, 1, 1);
        var sessionId = SeedSession(groupId, episodeId, userId, DateTime.UtcNow);
        SeedRating(sessionId, userId, 9);
        SeedRating(sessionId, partnerId, 7);

        var result = await _sut.GetDashboardAsync(userId, groupId);

        result.FavoriteGenres.Should().ContainSingle(g => g.GenreName == "Crime");
        result.FavoriteGenres.Single(g => g.GenreName == "Crime").Count.Should().Be(2);
        result.FavoriteGenres.Single(g => g.GenreName == "Crime").AverageRating.Should().Be(8.0);
    }

    private static DateTime GetMondayOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private void SeedUser(Guid id, string username)
    {
        _db.Users.Add(
            new User
            {
                Id = id,
                Username = username,
                Email = $"{username}@test.com",
                PasswordHash = "h",
            }
        );
        _db.SaveChanges();
    }

    private void SeedGroup(Guid groupId)
    {
        _db.Groups.Add(new Group { Id = groupId, CreatedAt = DateTime.UtcNow });
        _db.SaveChanges();
    }

    private void SeedUserGroup(Guid groupId, Guid userId)
    {
        _db.UserGroups.Add(
            new UserGroup
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = userId,
            }
        );
        _db.SaveChanges();
    }

    private List<Guid> SeedGroupWithUsers(int userCount, string prefix)
    {
        var groupId = Guid.NewGuid();
        SeedGroup(groupId);
        var ids = new List<Guid>() { groupId };
        for (int i = 0; i < userCount; i++)
        {
            var username = $"{prefix} {i}";
            var userId = Guid.NewGuid();
            SeedUser(userId, username);
            SeedUserGroup(groupId, userId);

            ids.Add(userId);
        }

        return ids;
    }

    private Guid SeedMovie(int tmdbId, string title)
    {
        var id = Guid.NewGuid();
        _db.Movies.Add(
            new Movie
            {
                Id = id,
                TmdbId = tmdbId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return id;
    }

    private static int _mediaTmdbCounter = -1000;

    private Guid SeedSeries(int tmdbId, string name, params string[] genres)
    {
        var id = Guid.NewGuid();
        _db.TvSeries.Add(
            new TvSeries
            {
                Id = id,
                TmdbId = tmdbId,
                Title = name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        foreach (var genreName in genres)
        {
            var genre = _db.Genres.FirstOrDefault(g => g.Name == genreName);
            if (genre is null)
            {
                genre = new Genre
                {
                    Id = Guid.NewGuid(),
                    TmdbId = _mediaTmdbCounter--,
                    Name = genreName,
                };
                _db.Genres.Add(genre);
            }
            _db.MediaGenres.Add(new MediaGenre { MediaId = id, GenreId = genre.Id });
        }
        _db.SaveChanges();
        return id;
    }

    private Guid SeedSeason(Guid seriesId, int seasonNumber)
    {
        var id = Guid.NewGuid();
        _db.TvSeasons.Add(
            new TvSeason
            {
                Id = id,
                SeriesId = seriesId,
                TmdbId = _mediaTmdbCounter--,
                SeasonNumber = seasonNumber,
                Title = $"Season {seasonNumber}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return id;
    }

    private Guid SeedEpisode(Guid seasonId, int seasonNumber, int episodeNumber)
    {
        var id = Guid.NewGuid();
        _db.TvEpisodes.Add(
            new TvEpisode
            {
                Id = id,
                SeasonId = seasonId,
                TmdbId = _mediaTmdbCounter--,
                SeasonNumber = seasonNumber,
                EpisodeNumber = episodeNumber,
                Title = $"Episode {episodeNumber}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return id;
    }

    private Guid SeedSession(Guid groupId, Guid movieId, Guid createdByUserId, DateTime watchedAt)
    {
        var id = Guid.NewGuid();
        _db.WatchSessions.Add(
            new WatchSession
            {
                Id = id,
                GroupId = groupId,
                MediaId = movieId,
                WatchedAt = watchedAt,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
        return id;
    }

    private void SeedRating(Guid sessionId, Guid userId, int ratingValue)
    {
        _db.Ratings.Add(
            new Rating
            {
                Id = Guid.NewGuid(),
                WatchSessionId = sessionId,
                UserId = userId,
                RatingValue = ratingValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        _db.SaveChanges();
    }
}
