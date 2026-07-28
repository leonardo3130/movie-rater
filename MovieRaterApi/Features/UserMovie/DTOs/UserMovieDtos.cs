namespace MovieRaterApi.Features.UserMovie.DTOs;

public class UserMovieResponseDto
{
    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserMovieListRequestDto
{
    public bool? FavoritesOnly { get; set; }
    public bool? WatchlistOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UserMovieWithMovieDto
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PagedUserMoviesResponseDto
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
    public List<UserMovieWithMovieDto> Results { get; set; } = [];
}
