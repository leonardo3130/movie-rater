using FluentValidation;
using MovieRaterApi.Features.MovieLists.DTOs;

namespace MovieRaterApi.Features.MovieLists.Validators;

public class CreateMovieListRequestValidator : AbstractValidator<CreateMovieListRequestDto>
{
    public CreateMovieListRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.GroupIds).NotEmpty().When(x => !x.IsPrivate);
    }
}

public class UpdateMovieListRequestValidator : AbstractValidator<UpdateMovieListRequestDto>
{
    public UpdateMovieListRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.GroupIds).NotEmpty().When(x => !x.IsPrivate);
    }
}
