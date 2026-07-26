using FluentValidation;
using MovieRaterApi.Features.WatchSessions.DTOs;

namespace MovieRaterApi.Features.WatchSessions.Validators;

public class CreateWatchSessionRequestValidator : AbstractValidator<CreateWatchSessionRequestDto>
{
    public CreateWatchSessionRequestValidator()
    {
        RuleFor(x => x.MovieId).NotEmpty();
        RuleFor(x => x.WatchedAt).NotEmpty();
        RuleFor(x => x.Location).MaximumLength(200).When(x => x.Location is not null);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}

public class HeatmapQueryValidator : AbstractValidator<HeatmapQueryDto>
{
    public HeatmapQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 730);
    }
}
