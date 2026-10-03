using FluentValidation;
using MovieRaterApi.Features.TvShows.DTOs;

namespace MovieRaterApi.Features.TvShows.Validators;

public class SearchTvShowsRequestValidator : AbstractValidator<SearchTvShowsRequestDto>
{
    public SearchTvShowsRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Page).InclusiveBetween(1, 500).When(x => x.Page.HasValue);

        RuleFor(x => x.FirstAirDateYear)
            .Matches(@"^\d{4}$")
            .WithMessage("FirstAirDateYear must be a four-digit year.")
            .When(x => x.FirstAirDateYear is not null);

        RuleFor(x => x.Year)
            .Matches(@"^\d{4}$")
            .WithMessage("Year must be a four-digit year.")
            .When(x => x.Year is not null);
    }
}

public class DiscoverTvShowsRequestValidator : AbstractValidator<DiscoverTvShowsRequestDto>
{
    private static readonly string[] AllowedSortBy =
    [
        "popularity.desc",
        "popularity.asc",
        "vote_average.desc",
        "vote_average.asc",
        "first_air_date.desc",
        "first_air_date.asc",
        "original_name.asc",
        "original_name.desc",
    ];

    public DiscoverTvShowsRequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 500).When(x => x.Page.HasValue);

        RuleFor(x => x.FirstAirDateYear)
            .Matches(@"^\d{4}$")
            .WithMessage("FirstAirDateYear must be a four-digit year.")
            .When(x => x.FirstAirDateYear is not null);

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortBy.Contains(s))
            .WithMessage("SortBy must be one of: " + string.Join(", ", AllowedSortBy))
            .When(x => x.SortBy is not null);

        RuleFor(x => x.VoteAverageGte).InclusiveBetween(0, 10).When(x => x.VoteAverageGte.HasValue);
    }
}

public class TvShowDetailsRequestValidator : AbstractValidator<TvShowDetailsRequestDto>
{
    public TvShowDetailsRequestValidator()
    {
        RuleFor(x => x.TmdbId).GreaterThan(0);
    }
}

public class TvSeasonRequestValidator : AbstractValidator<TvSeasonRequestDto>
{
    public TvSeasonRequestValidator()
    {
        RuleFor(x => x.TmdbId).GreaterThan(0);
        RuleFor(x => x.SeasonNumber).InclusiveBetween(0, 1000);
    }
}

public class TvEpisodeRequestValidator : AbstractValidator<TvEpisodeRequestDto>
{
    public TvEpisodeRequestValidator()
    {
        RuleFor(x => x.TmdbId).GreaterThan(0);
        RuleFor(x => x.SeasonNumber).InclusiveBetween(0, 1000);
        RuleFor(x => x.EpisodeNumber).InclusiveBetween(1, 1000);
    }
}