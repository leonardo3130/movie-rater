using FluentValidation;
using MovieRaterApi.Features.MovieLists.DTOs;

namespace MovieRaterApi.Features.MovieLists.Validators;

public class CreateMovieListRequestValidator : AbstractValidator<CreateMovieListRequestDto>
{
    // Bounds the membership lookup done for every shared group.
    internal const int MaxSharedGroups = 50;

    public CreateMovieListRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.GroupIds).NotEmpty().When(x => !x.IsPrivate);
        RuleFor(x => x.GroupIds.Count)
            .LessThanOrEqualTo(MaxSharedGroups)
            .OverridePropertyName("GroupIds");
    }
}

public class UpdateMovieListRequestValidator : AbstractValidator<UpdateMovieListRequestDto>
{
    public UpdateMovieListRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.GroupIds).NotEmpty().When(x => !x.IsPrivate);
        RuleFor(x => x.GroupIds.Count)
            .LessThanOrEqualTo(CreateMovieListRequestValidator.MaxSharedGroups)
            .OverridePropertyName("GroupIds");
    }
}
