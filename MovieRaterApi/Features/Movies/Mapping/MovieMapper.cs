using MovieRaterApi.Features.Movies.DTOs;
using MovieRaterApi.Infrastructure.Tmdb.Dtos.Responses;

namespace MovieRaterApi.Features.Movies.Mapping;

public static class MovieMapper
{
    public static MovieSummaryDto ToSummary(
        TmdbSearchMovieItem item,
        string? posterUrl,
        string? backdropUrl
    )
    {
        return new MovieSummaryDto
        {
            TmdbId = item.Id,
            Title = item.Title ?? item.OriginalTitle ?? "",
            PosterUrl = posterUrl,
            BackdropUrl = backdropUrl,
            Overview = item.Overview,
            ReleaseDate = item.ReleaseDate,
            VoteAverage = item.VoteAverage,
            VoteCount = item.VoteCount,
            GenreIds = item.GenreIds,
        };
    }

    public static MovieSummaryDto ToSummary(
        TmdbMovieDetails item,
        string? posterUrl,
        string? backdropUrl
    )
    {
        return new MovieSummaryDto
        {
            TmdbId = item.Id,
            Title = item.Title ?? item.OriginalTitle ?? "",
            PosterUrl = posterUrl,
            BackdropUrl = backdropUrl,
            Overview = item.Overview,
            ReleaseDate = item.ReleaseDate,
            VoteAverage = item.VoteAverage,
            VoteCount = item.VoteCount,
            GenreIds = item.Genres.Select(g => g.Id).ToList(),
        };
    }

    public static PagedMoviesResponseDto ToPagedResult(
        TmdbPagedResponse<TmdbSearchMovieItem> paged,
        Func<TmdbSearchMovieItem, string?> getPosterUrl,
        Func<TmdbSearchMovieItem, string?> getBackdropUrl
    )
    {
        return new PagedMoviesResponseDto
        {
            Page = paged.Page,
            TotalPages = paged.TotalPages,
            TotalResults = paged.TotalResults,
            Results = paged
                .Results.Select(item =>
                {
                    var dto = ToSummary(item, getPosterUrl(item), getBackdropUrl(item));
                    return dto;
                })
                .ToList(),
        };
    }

    public static PagedMoviesResponseDto ToPagedResult(
        TmdbPagedResponse<TmdbMovieDetails> paged,
        Func<TmdbMovieDetails, string?> getPosterUrl,
        Func<TmdbMovieDetails, string?> getBackdropUrl
    )
    {
        return new PagedMoviesResponseDto
        {
            Page = paged.Page,
            TotalPages = paged.TotalPages,
            TotalResults = paged.TotalResults,
            Results = paged
                .Results.Select(item =>
                {
                    var dto = ToSummary(item, getPosterUrl(item), getBackdropUrl(item));
                    return dto;
                })
                .ToList(),
        };
    }

    public static MovieDetailsResponseDto ToDetails(
        TmdbMovieDetails details,
        string? posterUrl,
        string? backdropUrl
    )
    {
        var dto = new MovieDetailsResponseDto
        {
            TmdbId = details.Id,
            Title = details.Title ?? details.OriginalTitle ?? "",
            PosterUrl = posterUrl,
            BackdropUrl = backdropUrl,
            Overview = details.Overview,
            ReleaseDate = details.ReleaseDate,
            Runtime = details.Runtime,
            Tagline = details.Tagline,
            Status = details.Status,
            ImdbId = details.ImdbId,
            Homepage = details.Homepage,
            Budget = details.Budget,
            Revenue = details.Revenue,
            VoteAverage = details.VoteAverage,
            VoteCount = details.VoteCount,
            Genres = details
                .Genres.Select(g => new GenreDto { TmdbId = g.Id, Name = g.Name })
                .ToList(),
        };

        return dto;
    }

    public static void PopulateCredits(
        MovieDetailsResponseDto dto,
        TmdbCreditsResponse credits,
        string? baseProfileUrl
    )
    {
        dto.Cast = credits
            .Cast.OrderBy(c => c.Order)
            .Take(20)
            .Select(c => new CastMemberDto
            {
                Id = c.Id,
                Name = c.Name,
                Character = c.Character,
                ProfileUrl = BuildProfileUrl(c.ProfilePath, baseProfileUrl),
                Order = c.Order,
            })
            .ToList();

        dto.Crew = credits
            .Crew.GroupBy(c => c.Department)
            .SelectMany(g => g.Take(5))
            .Take(20)
            .Select(c => new CrewMemberDto
            {
                Id = c.Id,
                Name = c.Name,
                Department = c.Department,
                Job = c.Job,
                ProfileUrl = BuildProfileUrl(c.ProfilePath, baseProfileUrl),
            })
            .ToList();
    }

    public static void PopulateVideos(MovieDetailsResponseDto dto, TmdbVideosResponse videos)
    {
        dto.Videos = videos
            .Results.Where(v => v.Site == "YouTube" && v.Type is "Trailer" or "Teaser")
            .Take(5)
            .Select(v => new VideoDto
            {
                Key = v.Key,
                Site = v.Site,
                Type = v.Type,
                Name = v.Name,
                Official = v.Official,
            })
            .ToList();
    }

    public static string? BuildPosterUrl(string? path, string baseUrl, string size = "w342")
    {
        return path is null ? null : $"{baseUrl}{size}{path}";
    }

    public static string? BuildBackdropUrl(string? path, string baseUrl, string size = "w780")
    {
        return path is null ? null : $"{baseUrl}{size}{path}";
    }

    private static string? BuildProfileUrl(string? path, string? baseUrl)
    {
        return path is null || baseUrl is null ? null : $"{baseUrl}w185{path}";
    }
}
