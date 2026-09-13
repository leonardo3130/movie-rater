namespace MovieRaterApi.Features.MovieLists.DTOs;

public class CreateMovieListRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; } = true;
    public bool CanGroupEdit { get; set; } = false;
    public List<Guid> GroupIds { get; set; } = [];
}

public class UpdateMovieListRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; } = true;
    public bool CanGroupEdit { get; set; } = false;
    public List<Guid> GroupIds { get; set; } = [];
}

public class MovieListSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public bool CanGroupEdit { get; set; }
    public bool IsOwner { get; set; }
    public int MovieCount { get; set; }
    public List<Guid> GroupIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class MovieListItemDto
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public DateTime AddedAt { get; set; }
}

public class MovieListResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public bool CanGroupEdit { get; set; }
    public bool IsOwner { get; set; }
    public List<Guid> GroupIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public List<MovieListItemDto> Movies { get; set; } = [];
}
