namespace MovieRaterApi.Features.Movies.DTOs;

public class SearchMoviesRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int? Page { get; set; }
    public string? Year { get; set; }
    public string? PrimaryReleaseYear { get; set; }
    public bool? IncludeAdult { get; set; }
    public string? Region { get; set; }
    public string? Language { get; set; }
}

public class DiscoverMoviesRequestDto
{
    public int? Page { get; set; }
    public string? GenreIds { get; set; }
    public string? PrimaryReleaseYear { get; set; }
    public string? PrimaryReleaseDateGte { get; set; }
    public string? PrimaryReleaseDateLte { get; set; }
    public string? SortBy { get; set; }
    public double? VoteAverageGte { get; set; }
    public bool? IncludeAdult { get; set; }
    public string? Region { get; set; }
    public string? Language { get; set; }
}

public class MovieListRequestDto
{
    public int? Page { get; set; }
    public string? Language { get; set; }
    public string? Region { get; set; }
}

public class MovieDetailsRequestDto
{
    public string? Language { get; set; }
}

public class MovieRecommendationsRequestDto
{
    public int? Page { get; set; }
    public string? Language { get; set; }
}

public class GenresRequestDto
{
    public string? Language { get; set; }
}

public class PagedMoviesResponseDto
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
    public List<MovieSummaryDto> Results { get; set; } = [];
}

public class MovieSummaryDto
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? Overview { get; set; }
    public string? ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public List<int> GenreIds { get; set; } = [];
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public int WatchedCount { get; set; }
}

public class MovieDetailsResponseDto
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? Overview { get; set; }
    public string? ReleaseDate { get; set; }
    public int? Runtime { get; set; }
    public string? Tagline { get; set; }
    public string? Status { get; set; }
    public string? ImdbId { get; set; }
    public string? Homepage { get; set; }
    public long Budget { get; set; }
    public long Revenue { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public List<GenreDto> Genres { get; set; } = [];
    public List<CastMemberDto> Cast { get; set; } = [];
    public List<CrewMemberDto> Crew { get; set; } = [];
    public List<VideoDto> Videos { get; set; } = [];
    public bool IsFavorite { get; set; }
    public bool IsInWatchlist { get; set; }
    public int WatchedCount { get; set; }
}

public class GenreDto
{
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CastMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Character { get; set; }
    public string? ProfileUrl { get; set; }
    public int Order { get; set; }
}

public class CrewMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Job { get; set; }
    public string? ProfileUrl { get; set; }
}

public class VideoDto
{
    public string? Key { get; set; }
    public string? Site { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public bool Official { get; set; }
}

public class GenresResponseDto
{
    public List<GenreDto> Genres { get; set; } = [];
}
