using FluentValidation;
using MovieRaterApi.Features.Movies.DTOs;

namespace MovieRaterApi.Features.Movies.Validators;

public class SearchMoviesRequestValidator : AbstractValidator<SearchMoviesRequestDto>
{
    public SearchMoviesRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Page).InclusiveBetween(1, 500).When(x => x.Page.HasValue);

        RuleFor(x => x.Year).Length(4).Matches(@"^\d{4}$").When(x => x.Year is not null);

        RuleFor(x => x.PrimaryReleaseYear)
            .Length(4)
            .Matches(@"^\d{4}$")
            .When(x => x.PrimaryReleaseYear is not null);

        RuleFor(x => x.Region).Length(2).When(x => x.Region is not null);
    }
}

public class DiscoverMoviesRequestValidator : AbstractValidator<DiscoverMoviesRequestDto>
{
    private static readonly string[] AllowedSortBy =
    [
        "popularity.desc",
        "popularity.asc",
        "vote_average.desc",
        "vote_average.asc",
        "primary_release_date.desc",
        "primary_release_date.asc",
        "revenue.desc",
        "revenue.asc",
        "original_title.asc",
        "original_title.desc",
    ];

    public DiscoverMoviesRequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 500).When(x => x.Page.HasValue);

        RuleFor(x => x.PrimaryReleaseYear)
            .Length(4)
            .Matches(@"^\d{4}$")
            .When(x => x.PrimaryReleaseYear is not null);

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortBy.Contains(s))
            .WithMessage("SortBy must be one of: " + string.Join(", ", AllowedSortBy))
            .When(x => x.SortBy is not null);

        RuleFor(x => x.VoteAverageGte).InclusiveBetween(0, 10).When(x => x.VoteAverageGte.HasValue);

        RuleFor(x => x.Region).Length(2).When(x => x.Region is not null);
    }
}

public class MovieListRequestValidator : AbstractValidator<MovieListRequestDto>
{
    public MovieListRequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 500).When(x => x.Page.HasValue);

        RuleFor(x => x.Region).Length(2).When(x => x.Region is not null);
    }
}

public class MovieRecommendationsRequestValidator
    : AbstractValidator<MovieRecommendationsRequestDto>
{
    public MovieRecommendationsRequestValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 500).When(x => x.Page.HasValue);
    }
}
