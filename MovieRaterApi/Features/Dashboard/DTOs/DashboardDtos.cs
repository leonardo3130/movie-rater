namespace MovieRaterApi.Features.Dashboard.DTOs;

public class DashboardResponseDto
{
    public int MoviesWatched { get; set; }
    public int MoviesThisMonth { get; set; }
    public int MoviesThisYear { get; set; }
    public double AverageRating { get; set; }
    public List<GenreStatDto> FavoriteGenres { get; set; } = [];
    public List<GenreStatDto> MostWatchedGenres { get; set; } = [];
    public MovieStatDto? HighestRatedMovie { get; set; }
    public MovieStatDto? LowestRatedMovie { get; set; }
    public MovieStatDto? BiggestDisagreement { get; set; }
    public double AverageDisagreement { get; set; }
    public int RewatchCount { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public Guid? GroupId { get; set; }
}

public class GenreStatDto
{
    public string GenreName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageRating { get; set; }
}

public class MovieStatDto
{
    public Guid MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int WatchedCount { get; set; }
}
